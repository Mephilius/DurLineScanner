using System.Runtime.InteropServices.WindowsRuntime;
using DurLineScanner.Data;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace DurLineScanner.Device;

/// <summary>Entry in the device selection.</summary>
public sealed record BleDeviceInfo(ulong Address, string Name, bool Paired)
{
    public bool IsFinder => Name.StartsWith(Sf4000Client.DeviceNamePrefix, StringComparison.OrdinalIgnoreCase);

    public string AddressText => string.Join(":", Enumerable.Range(0, 6).Select(i => ((Address >> (40 - 8 * i)) & 0xFF).ToString("X2")));

    public string Label => Paired ? L.F("{0} ({1}, gekoppelt)", Name, AddressText) : $"{Name} ({AddressText})";
}

/// <summary>
/// Bluetooth LE connection to the SF 4000 BT ("Finder S2-…"), Lierda UART bridge:
/// commands to FF01, responses via notify from FF02.
/// </summary>
public sealed class Sf4000Client : IAsyncDisposable
{
    public const string DeviceNamePrefix = "Finder";

    private static readonly Guid ServiceUuid = Guid.Parse("0000ff12-0000-1000-8000-00805f9b34fb");
    private static readonly Guid TxUuid = Guid.Parse("0000ff01-0000-1000-8000-00805f9b34fb");
    private static readonly Guid RxUuid = Guid.Parse("0000ff02-0000-1000-8000-00805f9b34fb");

    // The device sometimes does not respond at all or only after >1 s
    private static readonly TimeSpan ResponseTimeout = TimeSpan.FromMilliseconds(1800);
    private const int Retries = 3;

    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private BluetoothLEDevice? _device;
    private GattCharacteristic? _tx;
    private GattCharacteristic? _rx;
    private byte _pendingCode;
    private TaskCompletionSource<byte[]>? _pending;

    public event EventHandler<bool>? ConnectionChanged;

    public string? DeviceName => _device?.Name;

    public ulong? Address => _device?.BluetoothAddress;

    public bool IsConnected => _device?.ConnectionStatus == BluetoothConnectionStatus.Connected;

    /// <summary>Scans advertisements for a device whose name starts with "Finder".</summary>
    public static async Task<(ulong Address, string Name)?> FindDeviceAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        var found = new TaskCompletionSource<(ulong, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        watcher.Received += (_, e) =>
        {
            var name = e.Advertisement.LocalName;
            if (!string.IsNullOrEmpty(name) && name.StartsWith(DeviceNamePrefix, StringComparison.OrdinalIgnoreCase))
                found.TrySetResult((e.BluetoothAddress, name));
        };
        watcher.Start();
        try
        {
            var winner = await Task.WhenAny(found.Task, Task.Delay(timeout, ct));
            return winner == found.Task ? found.Task.Result : null;
        }
        finally
        {
            watcher.Stop();
        }
    }

    /// <summary>
    /// Bluetooth LE devices to choose from: devices paired in Windows plus all that advertise a name
    /// during <paramref name="scanTime"/>. Devices with a "Finder" name come first.
    /// </summary>
    public static async Task<List<BleDeviceInfo>> ListDevicesAsync(TimeSpan scanTime, CancellationToken ct = default)
    {
        var devices = new Dictionary<ulong, BleDeviceInfo>();
        var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
        watcher.Received += (_, e) =>
        {
            var name = e.Advertisement.LocalName;
            if (string.IsNullOrEmpty(name)) return;
            lock (devices)
                if (!devices.ContainsKey(e.BluetoothAddress))
                    devices[e.BluetoothAddress] = new BleDeviceInfo(e.BluetoothAddress, name, Paired: false);
        };
        watcher.Start();
        try
        {
            var paired = await DeviceInformation.FindAllAsync(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
            foreach (var info in paired)
            {
                using var device = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (device is null) continue;
                var name = string.IsNullOrEmpty(device.Name) ? info.Name : device.Name;
                lock (devices)
                    devices[device.BluetoothAddress] = new BleDeviceInfo(device.BluetoothAddress, name, Paired: true);
            }
            await Task.Delay(scanTime, ct);
        }
        finally
        {
            watcher.Stop();
        }

        lock (devices)
            return devices.Values
                .OrderByDescending(d => d.IsFinder)
                .ThenByDescending(d => d.Paired)
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public async Task ConnectAsync(ulong address)
    {
        await DisconnectAsync();

        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
            ?? throw new InvalidOperationException(L.T("Gerät nicht gefunden. Ist das SF 4000 eingeschaltet und nicht mit dem Handy verbunden?"));

        var services = await device.GetGattServicesForUuidAsync(ServiceUuid, BluetoothCacheMode.Uncached);
        var service = services.Services.FirstOrDefault();
        if (service is null)
        {
            device.Dispose();
            throw new InvalidOperationException(L.F("Datendienst FF12 nicht gefunden ({0}).", services.Status));
        }

        var chars = (await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)).Characteristics;
        _tx = chars.FirstOrDefault(c => c.Uuid == TxUuid);
        _rx = chars.FirstOrDefault(c => c.Uuid == RxUuid);
        if (_tx is null || _rx is null)
        {
            device.Dispose();
            throw new InvalidOperationException(L.T("Sende-/Empfangskanal FF01/FF02 nicht gefunden."));
        }

        _rx.ValueChanged += OnValueChanged;
        var status = await _rx.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify);
        if (status != GattCommunicationStatus.Success)
        {
            device.Dispose();
            throw new InvalidOperationException(L.F("Abonnieren der Antworten fehlgeschlagen ({0}).", status));
        }

        _device = device;
        _device.ConnectionStatusChanged += OnConnectionStatusChanged;
        ConnectionChanged?.Invoke(this, true);
    }

    public Task DisconnectAsync()
    {
        if (_rx is not null) _rx.ValueChanged -= OnValueChanged;
        if (_device is not null)
        {
            _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _device.Dispose();
            _device = null;
            ConnectionChanged?.Invoke(this, false);
        }
        _tx = null;
        _rx = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();

    /// <summary>Device ID, e.g. "260B".</summary>
    public async Task<string> IdentifyAsync(CancellationToken ct = default)
    {
        var response = await RequestAsync(Sf4000Protocol.Command(Sf4000Protocol.CmdIdentify), ct);
        return response.Length >= 4 ? Convert.ToHexString(response, 2, 2) : Convert.ToHexString(response);
    }

    public async Task<bool> TuneAsync(Transponder tp, Satellite satellite, CancellationToken ct = default)
    {
        var command = Sf4000Protocol.EncodeTune(tp, satellite.LofLow, satellite.LofHigh)
            ?? throw new ArgumentException(L.F("Transponder {0} ist mit LOF {1}/{2} nicht empfangbar.", tp, satellite.LofLow, satellite.LofHigh));
        var response = await RequestAsync(command, ct);
        return response.Length >= 3 && response[2] == Sf4000Protocol.RespondOk;
    }

    public async Task SetDetailModeAsync(bool enabled, CancellationToken ct = default) =>
        await RequestAsync(Sf4000Protocol.Command(enabled ? Sf4000Protocol.CmdDetailOn : Sf4000Protocol.CmdDetailOff), ct);

    public async Task<StatusReading> GetStatusAsync(CancellationToken ct = default)
    {
        var response = await RequestAsync(Sf4000Protocol.Command(Sf4000Protocol.CmdStatus), ct);
        return Sf4000Protocol.ParseStatus(response)
            ?? throw new InvalidDataException(L.F("Ungültige Statusantwort: {0}", Convert.ToHexString(response)));
    }

    /// <summary>Sends a command and waits for the response with the same command code.</summary>
    private async Task<byte[]> RequestAsync(byte[] command, CancellationToken ct)
    {
        await _requestLock.WaitAsync(ct);
        try
        {
            var tx = _tx ?? throw new InvalidOperationException(L.T("Nicht verbunden."));
            for (int attempt = 1; ; attempt++)
            {
                var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingCode = command[1];
                Volatile.Write(ref _pending, tcs);

                await tx.WriteValueAsync(command.AsBuffer(), GattWriteOption.WriteWithoutResponse);
                var winner = await Task.WhenAny(tcs.Task, Task.Delay(ResponseTimeout, ct));
                Volatile.Write(ref _pending, null);
                ct.ThrowIfCancellationRequested();

                if (winner == tcs.Task) return tcs.Task.Result;
                if (attempt >= Retries)
                    throw new TimeoutException(L.F("Keine Antwort auf {0}.", Convert.ToHexString(command)));
            }
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private void OnValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        var data = new byte[args.CharacteristicValue.Length];
        using (var reader = DataReader.FromBuffer(args.CharacteristicValue))
            reader.ReadBytes(data);

        // Late responses to other commands are ignored
        if (data.Length >= 2 && data[0] == Sf4000Protocol.Prefix && data[1] == _pendingCode)
            Volatile.Read(ref _pending)?.TrySetResult(data);
    }

    private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args) =>
        ConnectionChanged?.Invoke(this, sender.ConnectionStatus == BluetoothConnectionStatus.Connected);
}
