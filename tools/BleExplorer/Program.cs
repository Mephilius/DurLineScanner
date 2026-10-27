// BLE-Explorer für das Durline SF 4000 BT.
// Zweck: Services/Characteristics des Geräts ermitteln und den Datenverkehr mitschneiden,
// damit das (undokumentierte) Protokoll analysiert werden kann.
//
// Aufruf:
//   BleExplorer scan [sekunden]          - BLE-Geräte in der Nähe auflisten
//   BleExplorer classic                  - klassische Bluetooth-Geräte (SPP/RFCOMM) auflisten
//   BleExplorer send <adresse> <hex>...  - Hex-Befehle an FF01 senden, Antworten von FF02 anzeigen
//   BleExplorer dump <adresse>          - Services/Characteristics auflisten und lesbare Werte lesen
//   BleExplorer monitor <adresse> [sek]  - alle Notify/Indicate abonnieren, Hex-Log, interaktiv schreiben
//                                          (mit [sek]: nur passiv mithören, ohne Eingabe)
//
// <adresse> im Format AA:BB:CC:DD:EE:FF (wird bei "scan" angezeigt).

using System.Collections.Concurrent;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

var logPath = Path.Combine(AppContext.BaseDirectory, $"ble-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
var logWriter = new StreamWriter(logPath, append: false, Encoding.UTF8) { AutoFlush = true };

void Log(string line)
{
    var stamped = $"{DateTime.Now:HH:mm:ss.fff}  {line}";
    Console.WriteLine(stamped);
    lock (logWriter) logWriter.WriteLine(stamped);
}

if (args.Length == 0)
{
    Console.WriteLine("Verwendung: BleExplorer scan [sek] | classic | dump <adresse> | monitor <adresse>");
    return 1;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "scan":
            await Scan(args.Length > 1 ? int.Parse(args[1]) : 10);
            break;
        case "classic":
            await ScanClassic();
            break;
        case "send":
            await Send(ParseAddress(args[1]), args.Skip(2).ToArray());
            break;
        case "dump":
            await Dump(ParseAddress(args[1]), monitor: false);
            break;
        case "monitor":
            await Dump(ParseAddress(args[1]), monitor: true, listenSeconds: args.Length > 2 ? int.Parse(args[2]) : 0);
            break;
        default:
            Console.WriteLine($"Unbekannter Befehl: {args[0]}");
            return 1;
    }
}
finally
{
    Console.WriteLine($"\nLog gespeichert: {logPath}");
    logWriter.Dispose();
}
return 0;

async Task Scan(int seconds)
{
    // Advertisement und Scan-Response kommen getrennt an; pro Adresse sammeln und nur Neues ausgeben.
    var seen = new ConcurrentDictionary<ulong, BleSeen>();
    var watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
    watcher.Received += (_, e) =>
    {
        var entry = seen.GetOrAdd(e.BluetoothAddress, _ => new BleSeen());
        bool isNew;
        lock (entry)
        {
            entry.Rssi = e.RawSignalStrengthInDBm;
            isNew = entry.Count++ == 0;
            if (!string.IsNullOrEmpty(e.Advertisement.LocalName) && entry.Name != e.Advertisement.LocalName)
            {
                entry.Name = e.Advertisement.LocalName;
                isNew = true;
            }
            foreach (var uuid in e.Advertisement.ServiceUuids)
                isNew |= entry.Services.Add(uuid.ToString());
            foreach (var m in e.Advertisement.ManufacturerData)
                isNew |= entry.Manufacturer.Add($"{m.CompanyId:X4}:{ToHex(m.Data)}");
        }
        if (isNew)
            Log($"{FormatAddress(e.BluetoothAddress)}  RSSI {e.RawSignalStrengthInDBm,4} dBm  {entry}");
    };
    Log($"Scanne BLE {seconds} s ...");
    watcher.Start();
    await Task.Delay(TimeSpan.FromSeconds(seconds));
    watcher.Stop();

    Log($"--- {seen.Count} BLE-Gerät(e) ---");
    foreach (var (address, entry) in seen.OrderByDescending(kv => kv.Value.Rssi))
        Log($"{FormatAddress(address)}  RSSI {entry.Rssi,4} dBm  {entry}");
}

async Task Send(ulong address, string[] commands)
{
    // Sendet Hex-Befehle an FF01 (UART-Eingang des Messgeräts) und protokolliert die Antworten von FF02.
    var serviceUuid = Guid.Parse("0000ff12-0000-1000-8000-00805f9b34fb");
    using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
        ?? throw new InvalidOperationException("Gerät nicht gefunden.");
    var services = await device.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Uncached);
    var service = services.Services.FirstOrDefault()
        ?? throw new InvalidOperationException($"Service FF12 nicht gefunden: {services.Status}");
    var chars = (await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached)).Characteristics;
    var tx = chars.First(c => c.Uuid == Guid.Parse("0000ff01-0000-1000-8000-00805f9b34fb"));
    var rx = chars.First(c => c.Uuid == Guid.Parse("0000ff02-0000-1000-8000-00805f9b34fb"));

    rx.ValueChanged += (_, e) => Log($"<< {ToHex(e.CharacteristicValue)}");
    Log($"Notify FF02: {await rx.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify)}");

    foreach (var command in commands)
    {
        var bytes = Convert.FromHexString(command);
        var status = await tx.WriteValueAsync(bytes.AsBuffer(), GattWriteOption.WriteWithoutResponse);
        Log($">> {command.ToUpperInvariant()}  ({status})");
        await Task.Delay(1500);
    }
}

async Task ScanClassic()
{
    // Klassisches Bluetooth (BR/EDR), z.B. serielle SPP-Geräte. Taucht im BLE-Scan nicht auf.
    Log("Suche klassische Bluetooth-Geräte (dauert ca. 10-30 s) ...");
    foreach (var (label, paired) in new[] { ("gekoppelt", true), ("nicht gekoppelt", false) })
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(paired);
        var devices = await DeviceInformation.FindAllAsync(selector);
        Log($"--- {devices.Count} Gerät(e) {label} ---");
        foreach (var info in devices)
        {
            using var device = await BluetoothDevice.FromIdAsync(info.Id);
            if (device is null)
            {
                Log($"   {info.Name}  (nicht abrufbar)");
                continue;
            }
            Log($"   {FormatAddress(device.BluetoothAddress)}  {device.Name}  Klasse: {device.ClassOfDevice.MajorClass}/{device.ClassOfDevice.MinorClass}");
            var rfcomm = await device.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
            foreach (var service in rfcomm.Services)
                Log($"      RFCOMM-Dienst {service.ServiceId.Uuid}");
        }
    }
}

async Task Dump(ulong address, bool monitor, int listenSeconds = 0)
{
    Log($"Verbinde mit {FormatAddress(address)} ...");
    using var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
        ?? throw new InvalidOperationException("Gerät nicht gefunden (eingeschaltet und nicht mit dem Handy verbunden?).");
    Log($"Gerät: {device.Name}  Status: {device.ConnectionStatus}");

    var servicesResult = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
    if (servicesResult.Status != GattCommunicationStatus.Success)
        throw new InvalidOperationException($"Services konnten nicht gelesen werden: {servicesResult.Status}");

    var writable = new List<GattCharacteristic>();
    foreach (var service in servicesResult.Services)
    {
        Log($"SERVICE {service.Uuid}");
        var charsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
        if (charsResult.Status != GattCommunicationStatus.Success)
        {
            Log($"   (Characteristics nicht lesbar: {charsResult.Status})");
            continue;
        }
        foreach (var ch in charsResult.Characteristics)
        {
            var props = ch.CharacteristicProperties;
            Log($"   CHAR {ch.Uuid}  Handle 0x{ch.AttributeHandle:X4}  [{props}]");

            if (props.HasFlag(GattCharacteristicProperties.Read))
            {
                var r = await ch.ReadValueAsync(BluetoothCacheMode.Uncached);
                if (r.Status == GattCommunicationStatus.Success)
                    Log($"      Wert: {ToHex(r.Value)}  \"{ToAscii(r.Value)}\"");
                else
                    Log($"      Lesen fehlgeschlagen: {r.Status}");
            }

            if (props.HasFlag(GattCharacteristicProperties.Write) || props.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                writable.Add(ch);

            if (monitor && (props.HasFlag(GattCharacteristicProperties.Notify) || props.HasFlag(GattCharacteristicProperties.Indicate)))
            {
                var uuid = ch.Uuid;
                ch.ValueChanged += (_, e) => Log($"<< {uuid}  {ToHex(e.CharacteristicValue)}  \"{ToAscii(e.CharacteristicValue)}\"");
                var mode = props.HasFlag(GattCharacteristicProperties.Notify)
                    ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                    : GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                try
                {
                    var status = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(mode);
                    Log($"      Abonniert ({mode}): {status}");
                }
                catch (UnauthorizedAccessException)
                {
                    // z.B. 2A05 "Service Changed" - wird von Windows selbst verwaltet
                    Log("      Abonnieren nicht erlaubt (von Windows reserviert)");
                }
            }
        }
    }

    if (!monitor) return;

    if (listenSeconds > 0)
    {
        Log($"Höre {listenSeconds} s passiv mit ...");
        await Task.Delay(TimeSpan.FromSeconds(listenSeconds));
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Monitor läuft. Eingaben:");
    Console.WriteLine("  w <nr> <hex>   - Hex-Bytes an beschreibbare Characteristic senden, z.B.  w 0 AA 01 FF");
    Console.WriteLine("  t <nr> <text>  - ASCII-Text senden (ohne Zeilenende)");
    Console.WriteLine("  q              - beenden");
    for (int i = 0; i < writable.Count; i++)
        Console.WriteLine($"  [{i}] {writable[i].Uuid}  [{writable[i].CharacteristicProperties}]");

    while (Console.ReadLine() is { } line)
    {
        line = line.Trim();
        if (line == "q") break;
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || (parts[0] != "w" && parts[0] != "t") || !int.TryParse(parts[1], out var idx) || idx < 0 || idx >= writable.Count)
        {
            Console.WriteLine("Ungültige Eingabe.");
            continue;
        }
        try
        {
            var bytes = parts[0] == "w"
                ? Convert.FromHexString(parts[2].Replace(" ", ""))
                : Encoding.ASCII.GetBytes(parts[2]);
            var ch = writable[idx];
            var option = ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Write)
                ? GattWriteOption.WriteWithResponse
                : GattWriteOption.WriteWithoutResponse;
            var status = await ch.WriteValueAsync(bytes.AsBuffer(), option);
            Log($">> {ch.Uuid}  {Convert.ToHexString(bytes)}  ({status})");
        }
        catch (FormatException)
        {
            Console.WriteLine("Ungültige Hex-Zeichenfolge.");
        }
    }
}

static ulong ParseAddress(string s) => Convert.ToUInt64(s.Replace(":", "").Replace("-", ""), 16);

static string FormatAddress(ulong a) =>
    string.Join(":", Enumerable.Range(0, 6).Select(i => ((a >> (8 * (5 - i))) & 0xFF).ToString("X2")));

static byte[] ToBytes(IBuffer buffer)
{
    var data = new byte[buffer.Length];
    using var reader = DataReader.FromBuffer(buffer);
    reader.ReadBytes(data);
    return data;
}

static string ToHex(IBuffer buffer) => Convert.ToHexString(ToBytes(buffer));

static string ToAscii(IBuffer buffer) =>
    new string(ToBytes(buffer).Select(b => b is >= 0x20 and < 0x7F ? (char)b : '.').ToArray());

class BleSeen
{
    public string? Name;
    public short Rssi;
    public int Count;
    public HashSet<string> Services = new();
    public HashSet<string> Manufacturer = new();

    public override string ToString() =>
        $"{Name ?? "(ohne Name)"}  Services: {string.Join(",", Services)}  Herstellerdaten: {string.Join(" ", Manufacturer)}";
}

static class BufferExtensions
{
    public static IBuffer AsBuffer(this byte[] bytes)
    {
        using var writer = new DataWriter();
        writer.WriteBytes(bytes);
        return writer.DetachBuffer();
    }
}
