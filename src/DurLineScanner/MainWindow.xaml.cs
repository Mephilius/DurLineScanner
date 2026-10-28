using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DurLineScanner.Controls;
using DurLineScanner.Data;
using DurLineScanner.Device;
using DurLineScanner.Scanning;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.WPF;
using Brush = System.Windows.Media.Brush;
using Color = ScottPlot.Color;
using Colors = ScottPlot.Colors;
using MediaColor = System.Windows.Media.Color;
using Palette = DurLineScanner.Controls.Palette;
using VerticalAlignment = System.Windows.VerticalAlignment;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

namespace DurLineScanner;

public partial class MainWindow : Window
{
    private static readonly string AddressFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "device.txt");

    private static readonly Color HoleColor = Palette.ToPlot(Palette.Bad).WithAlpha(0.13);
    private static readonly Color InactiveColor = Color.FromHex("#9E9E9E").WithAlpha(0.13);
    private static readonly Color GridColor = Color.FromHex("#EDEDED");
    private static readonly Color AxisColor = Color.FromHex("#616161");

    // Reihenfolge entspricht Plane.All: Low V, Low H, High V, High H
    private static readonly Color[] SeriesColors =
    [
        Color.FromHex("#005FB8"), Color.FromHex("#C239B3"), Color.FromHex("#00A2AD"),
        Color.FromHex("#CA5010"), Color.FromHex("#6B69D6"), Color.FromHex("#498205"),
    ];

    private readonly TransponderDatabase _database = TransponderDatabase.OpenDefault();
    private readonly SessionStore _store = new();
    private readonly Sf4000Client _client = new();
    private readonly ObservableCollection<SessionItem> _sessions = [];
    private readonly ObservableCollection<ScanResult> _results = [];

    private string _deviceModel = "";
    private ScanSession? _shownSession;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _liveCts;
    private Transponder? _liveTarget;
    private readonly LivePeaks _peaks = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private List<Site> _sites = SiteStore.Load();
    private StatusReading? _lastReading;
    private bool _miniScanRequested;

    public MainWindow()
    {
        InitializeComponent();
        L.Translate(this);
        VersionText.Text = VersionLabel();

        SessionList.ItemsSource = _sessions;
        ResultGrid.ItemsSource = _results;
        _client.ConnectionChanged += (_, connected) => Dispatcher.Invoke(() => UpdateConnectionUi(connected));

        foreach (var plot in new[] { PlotH, PlotV, PlotPlanes, PlotCompare, PlotLive })
            StylePlot(plot.Plot);

        AttachColumnToolTips();
        LivePlaneBox.ItemsSource = Plane.All.Select(p => new PlaneOption(p)).ToList();

        RefreshSatellites();

        WeatherBox.ItemsSource = WeatherInfo.All.Select(w => new WeatherOption(w)).ToList();
        WeatherBox.SelectedIndex = (int)(_settings.LastWeather ?? Weather.Sunny);
        WeatherFilterBox.ItemsSource = new object[] { AllWeather }.Concat(WeatherInfo.All.Select(w => new WeatherOption(w))).ToList();
        WeatherFilterBox.SelectedIndex = 0;
        UpdateSiteBox(_settings.LastSiteId);

        LocationKindBox_SelectionChanged(LocationKindBox, null!);
        LoadSessions();
        LivePlaneBox.SelectedIndex = 0;
        ConfigureGauges(null);
        RenderCharts();
        ApplySessionFilter();
        ShowFirstSessionOfSite(); // neueste Messreihe des Standorts anzeigen
        Loaded += async (_, _) =>
        {
            _ = RefreshDevicesAsync();
            // Die Transponderliste wird nicht mitgeliefert – beim ersten Start herunterladen
            if (_database.Catalog is null) await DownloadTransponderListAsync();
            if (_settings.LoadChannelsOnStart && _settings.ReceiverConfigured) await LoadChannelsFromReceiverAsync();
        };
    }

    /// <summary>"v0.9.2" aus der beim Release gesetzten Version; lokale Builds (0.0.0-dev) als Entwicklungsversion.</summary>
    private static string VersionLabel()
    {
        var info = System.Reflection.Assembly.GetExecutingAssembly()
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>().FirstOrDefault()?.InformationalVersion ?? "";
        var version = info.Split('+')[0]; // Commit-Kennung abschneiden
        return version.Length == 0 || version.EndsWith("-dev") ? L.T("Entwicklungsversion") : "v" + version;
    }

    private Satellite? SelectedSatellite => SatelliteBox.SelectedItem as Satellite;

    private Site? SelectedSite => SiteBox.SelectedItem as Site;

    private Weather? SelectedWeather => (WeatherBox.SelectedItem as WeatherOption)?.Weather;

    private Conditions CurrentConditions => new(SelectedSite, SelectedWeather, SelectedSatellite?.Longitude ?? 19.2);

    // ---------------------------------------------------------------- Verbindung

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_client.IsConnected)
        {
            await StopAllAsync();
            await _client.DisconnectAsync();
            return;
        }

        ConnectButton.IsEnabled = false;
        DeviceBox.IsEnabled = RefreshDevicesButton.IsEnabled = false;
        ConnectionText.Text = L.T("verbinde …");
        try
        {
            ulong? address = (DeviceBox.SelectedItem as BleDeviceInfo)?.Address ?? ReadSavedAddress();
            if (address is null)
            {
                SetStatus(L.T("Suche SF 4000 („Finder …“) …"));
                var found = await Sf4000Client.FindDeviceAsync(TimeSpan.FromSeconds(15));
                if (found is null)
                {
                    SetStatus(L.T("Kein SF 4000 gefunden. Gerät einschalten und Handy-App trennen."));
                    UpdateConnectionUi(false);
                    return;
                }
                address = found.Value.Address;
            }

            SetStatus(L.T("Verbinde …"));
            try
            {
                await _client.ConnectAsync(address.Value);
            }
            catch (InvalidOperationException) when (DeviceBox.SelectedItem is null && ReadSavedAddress() is not null)
            {
                // Gespeicherte Adresse passt nicht mehr – neu suchen
                File.Delete(AddressFile);
                var found = await Sf4000Client.FindDeviceAsync(TimeSpan.FromSeconds(15))
                            ?? throw new InvalidOperationException(L.T("Kein SF 4000 gefunden."));
                await _client.ConnectAsync(found.Address);
            }
            SaveAddress(_client.Address!.Value);

            _deviceModel = await _client.IdentifyAsync();
            SetStatus(L.F("Verbunden mit {0}, Modell {1}.", _client.DeviceName, _deviceModel));
            UpdateConnectionUi(true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            SetStatus(L.F("Verbindung fehlgeschlagen: {0}", ex.Message));
            await _client.DisconnectAsync();
            UpdateConnectionUi(false);
        }
        finally
        {
            ConnectButton.IsEnabled = true;
            DeviceBox.IsEnabled = RefreshDevicesButton.IsEnabled = !_client.IsConnected;
        }
    }

    private async void RefreshDevicesButton_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    /// <summary>Geräteliste neu füllen; vorausgewählt wird das SF 4000 („Finder …“), bevorzugt das zuletzt benutzte.</summary>
    private async Task RefreshDevicesAsync()
    {
        RefreshDevicesButton.IsEnabled = false;
        SetStatus(L.T("Suche Bluetooth-Geräte …"));
        try
        {
            var previous = (DeviceBox.SelectedItem as BleDeviceInfo)?.Address;
            var devices = await Sf4000Client.ListDevicesAsync(TimeSpan.FromSeconds(4));
            var saved = ReadSavedAddress();
            DeviceBox.ItemsSource = devices;
            DeviceBox.SelectedItem =
                devices.FirstOrDefault(d => d.Address == previous)
                ?? devices.FirstOrDefault(d => d.IsFinder && d.Address == saved)
                ?? devices.FirstOrDefault(d => d.IsFinder)
                ?? devices.FirstOrDefault(d => d.Address == saved)
                ?? devices.FirstOrDefault();
            SetStatus(devices.Count == 0
                ? L.T("Keine Bluetooth-Geräte gefunden. Gerät einschalten und Handy-App trennen.")
                : L.F("{0} Bluetooth-Geräte gefunden.", devices.Count));
        }
        catch (Exception ex)
        {
            SetStatus(L.F("Gerätesuche fehlgeschlagen: {0}", ex.Message));
        }
        finally
        {
            RefreshDevicesButton.IsEnabled = !_client.IsConnected;
        }
    }

    private void UpdateConnectionUi(bool connected)
    {
        DeviceBox.IsEnabled = RefreshDevicesButton.IsEnabled = !connected;
        ConnectionText.Text = connected ? L.F("{0} · Modell {1}", _client.DeviceName, _deviceModel) : L.T("nicht verbunden");
        ConnectionDot.Fill = connected ? (Brush)FindResource("GoodBrush") : Palette.Brush(MediaColor.FromRgb(0xB0, 0xB0, 0xB0));
        ConnectButton.Content = connected ? L.T("Trennen") : L.T("Verbinden");
        bool busy = _scanCts is not null || _liveCts is not null;
        ScanButton.IsEnabled = connected && !busy;
        LiveStartButton.IsEnabled = connected && !busy;
        MiniScanButton.IsEnabled = connected && _scanCts is null;
        if (!connected && busy) SetStatus(L.T("Verbindung verloren."));
    }

    private static ulong? ReadSavedAddress()
    {
        try
        {
            return File.Exists(AddressFile) ? Convert.ToUInt64(File.ReadAllText(AddressFile).Trim(), 16) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static void SaveAddress(ulong address)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(AddressFile)!);
        File.WriteAllText(AddressFile, address.ToString("X12"));
    }

    // ---------------------------------------------------------------- Einstellungen, Standort und Wetter

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettings(showReceiver: false);

    private void HelpButton_Click(object sender, RoutedEventArgs e) => OpenHelp();

    private HelpWindow? _help;

    /// <summary>Hilfe nicht modal, damit man beim Lesen weiterarbeiten kann; nur ein Fenster gleichzeitig.</summary>
    private void OpenHelp()
    {
        if (_help is not null)
        {
            _help.Activate();
            return;
        }
        _help = new HelpWindow(VersionLabel()) { Owner = this };
        _help.Closed += (_, _) => _help = null;
        _help.Show();
    }

    private void ManageSitesButton_Click(object sender, RoutedEventArgs e) => OpenSettings(showReceiver: false);

    private void OpenSettings(bool showReceiver)
    {
        var window = new SettingsWindow(_settings, _sites, SelectedSatellite, showReceiver) { Owner = this };
        window.ShowDialog();
        _sites = window.Sites;
        UpdateSiteBox(SelectedSite?.Id ?? _sites.FirstOrDefault()?.Id);
        if (window.ChannelsLoaded) UpdateChannelSource();
        UpdateReceiverButton();
    }

    private void UpdateSiteBox(Guid? selectId)
    {
        SiteBox.ItemsSource = null;
        SiteBox.ItemsSource = _sites;
        SiteBox.SelectedItem = _sites.FirstOrDefault(s => s.Id == selectId) ?? _sites.FirstOrDefault();
        if (_sites.Count == 0) SiteBox.ToolTip = L.T("Noch kein Standort – mit „…“ anlegen (Adresse, Schüsselgröße, LNB)");
        UpdateReceiverButton();
    }

    private void SiteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedSite is { } site)
        {
            _settings.LastSiteId = site.Id;
            _settings.Save();
            var where = site.HasCoordinates
                ? L.F("{0:0.000}° N, {1:0.000}° O", site.Latitude, site.Longitude)
                : L.T("ohne Koordinaten");
            SiteBox.ToolTip = L.F("{0}\n{1}\n{2}\nSchüssel {3} cm · LNB {4:0.#} dB", site.Name, site.Address, where, site.DishCm, site.LnbGainDb);
        }
        UpdateLocationLists();
        ApplySessionFilter();
        ShowFirstSessionOfSite();
        RefreshLiveLight();
    }

    private void WeatherBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || SelectedWeather is not { } weather) return;
        _settings.LastWeather = weather;
        _settings.Save();
        RefreshLiveLight();
    }

    private void UpdateReceiverButton()
    {
        ReceiverButton.ToolTip = _settings.ReceiverConfigured
            ? L.F("Senderliste jetzt von {0} {1} laden", _settings.ReceiverKind, ReceiverChannelSource.BaseUri(_settings))
            : L.T("Senderliste online vom Enigma2-Receiver oder TVHeadend laden – zuerst in den Einstellungen eintragen");
    }

    private async void ReceiverButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_settings.ReceiverConfigured)
        {
            OpenSettings(showReceiver: true);
            return;
        }
        await LoadChannelsFromReceiverAsync();
    }

    private async Task LoadChannelsFromReceiverAsync()
    {
        ReceiverButton.IsEnabled = false;
        SetStatus(L.F("Lade Senderliste von {0} …", ReceiverChannelSource.BaseUri(_settings)));
        try
        {
            ChannelList.Current = await ReceiverChannelSource.LoadAsync(_settings);
            UpdateChannelSource();
            SetStatus(L.F("Senderliste geladen: {0}, davon {1} auf {2}.", ChannelList.Current.Description, ChannelList.Current.AllAt(PositionTenths).Count, SelectedSatellite?.Name));
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or InvalidOperationException
                                       or InvalidDataException or System.Text.Json.JsonException or UriFormatException)
        {
            SetStatus(L.F("Senderliste vom Receiver nicht geladen: {0}", ex.Message));
        }
        finally
        {
            ReceiverButton.IsEnabled = true;
        }
    }

    // ---------------------------------------------------------------- Satellit und Transponderliste

    private void SatelliteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedSatellite is not { } sat) return;
        var tps = _database.GetTransponders(sat);
        ListSourceText.Text = L.F("Transponderliste: {0}", _database.SourceDescription(sat));
        UpdateChannelSource();
        UpdateLiveTransponders();
        SetStatus(L.F("{0}: {1} Transponder in der Liste.", sat.Name, tps.Count));
    }

    /// <summary>Satellitenliste aus der Transponderliste neu aufbauen; die Auswahl bleibt bei derselben Position.</summary>
    private void RefreshSatellites()
    {
        var previous = SelectedSatellite;
        var satellites = _database.GetSatellites();
        SatelliteBox.ItemsSource = satellites;
        SatelliteBox.SelectedItem =
            satellites.FirstOrDefault(s => previous is not null && Math.Abs(s.Longitude - previous.Longitude) < 0.15 && s.LofHigh == previous.LofHigh)
            ?? satellites.FirstOrDefault(s => Math.Abs(s.Longitude - 19.2) < 0.15 && s.LofHigh >= 8000)
            ?? satellites.FirstOrDefault();
        if (satellites.Count == 0)
            ListSourceText.Text = L.T("Transponderliste: noch keine – bitte „Transponderliste aktualisieren“ (Internet nötig)");
    }

    private async void UpdateListButton_Click(object sender, RoutedEventArgs e) => await DownloadTransponderListAsync();

    private async Task DownloadTransponderListAsync()
    {
        UpdateListButton.IsEnabled = false;
        SetStatus(L.T("Lade aktuelle Transponderliste von OpenPLi …"));
        try
        {
            _database.Catalog = await SatellitesXml.DownloadAsync();
            RefreshSatellites();
            SatelliteBox_SelectionChanged(SatelliteBox, null!);
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException
                                       or InvalidDataException or System.Xml.XmlException or IOException)
        {
            SetStatus(L.F("Aktualisieren fehlgeschlagen: {0}", ex.Message));
        }
        finally
        {
            UpdateListButton.IsEnabled = true;
        }
    }

    private void ImportListButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = L.T("satellites.xml|*.xml|Alle Dateien|*.*") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _database.Catalog = SatellitesXml.Import(dialog.FileName);
            RefreshSatellites();
            SatelliteBox_SelectionChanged(SatelliteBox, null!);
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException or IOException)
        {
            SetStatus(L.F("Import fehlgeschlagen: {0}", ex.Message));
        }
    }

    // ---------------------------------------------------------------- Senderliste

    private int PositionTenths => (int)Math.Round((SelectedSatellite?.Longitude ?? 19.2) * 10);

    private void ImportChannelsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = L.T("Enigma2-Senderliste wählen"),
            Filter = L.T("Enigma2-Senderliste (lamedb, lamedb5, ZIP)|lamedb;lamedb5;*.zip|Alle Dateien|*.*"),
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            ChannelList.Current = ChannelList.Import(dialog.FileName);
            UpdateChannelSource();
            SetStatus(L.F("Senderliste übernommen: {0}, davon {1} auf {2}.", ChannelList.Current.Description, ChannelList.Current.AllAt(PositionTenths).Count, SelectedSatellite?.Name));
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            SetStatus(L.F("Import der Senderliste fehlgeschlagen: {0}", ex.Message));
        }
    }

    private void UpdateChannelSource()
    {
        ChannelSourceText.Text = ChannelList.Current is { } list ? L.F("Senderliste: {0}", list.Description) : L.T("keine Senderliste");
        var channels = ChannelList.Current?.AllAt(PositionTenths) ?? [];
        LiveChannelBox.ItemsSource = channels.ToList();
        LiveChannelBox.IsEnabled = channels.Count > 0;
        LiveChannelBox.Text = "";
        if (channels.Count == 0) LiveChannelBox.ToolTip = L.T("Zuerst eine Senderliste importieren (lamedb eines Enigma2-Receivers)");
    }

    private void TransponderCell_ToolTipOpening(object sender, ToolTipEventArgs e)
    {
        if (sender is DataGridCell { DataContext: ScanResult r } cell)
            cell.ToolTip = new ToolTip { Content = ChannelToolTip.Build(r.FrequencyMHz, r.Polarization, r.SymbolRateKs, r.ModulationText) };
    }

    // ---------------------------------------------------------------- Scan

    private IReadOnlyList<Transponder> TranspondersToScan(Satellite sat)
    {
        var all = _database.GetTransponders(sat)
            .Where(t => Sf4000Protocol.EncodeTune(t, sat.LofLow, sat.LofHigh) is not null);
        return PlaneFilterBox.SelectedIndex switch
        {
            1 => all.Where(t => t.FrequencyMHz < 11700 && t.Polarization == Polarization.Vertical).ToList(),
            2 => all.Where(t => t.FrequencyMHz < 11700 && t.Polarization == Polarization.Horizontal).ToList(),
            3 => all.Where(t => t.FrequencyMHz >= 11700 && t.Polarization == Polarization.Vertical).ToList(),
            4 => all.Where(t => t.FrequencyMHz >= 11700 && t.Polarization == Polarization.Horizontal).ToList(),
            _ => all.ToList(),
        };
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSatellite is not { } sat) return;
        var transponders = TranspondersToScan(sat);
        if (transponders.Count == 0)
        {
            SetStatus(L.T("Keine empfangbaren Transponder für diesen Satelliten/LNB."));
            return;
        }

        bool port = SelectedLocationKind == LocationKind.LnbPort;
        var room = RoomBox.Text.Trim();
        if (!port && room.Length == 0)
        {
            SetStatus(L.T("Bitte den Raum der Sat-Dose wählen (z.B. „Wohnzimmer“)."));
            RoomBox.Focus();
            RoomBox.IsDropDownOpen = true;
            return;
        }
        int outlet = Math.Max(0, OutletBox.SelectedIndex) + 1;
        var locationName = port ? L.F("Port {0}", outlet) : L.F("{0} · Anschluss {1}", room, outlet);
        if (SelectedSite is null)
            SetStatus(L.T("Hinweis: kein Standort gewählt – die Ampel bewertet dann ohne Schüsselgröße und Wetterreserve."));

        var session = new ScanSession
        {
            SatelliteName = sat.Name,
            SatelliteLongitude = sat.Longitude,
            Started = DateTime.Now,
            DeviceModel = _deviceModel,
            Note = NoteBox.Text.Trim(),
            LocationKind = SelectedLocationKind,
            LocationName = locationName,
            Room = port ? null : room,
            Outlet = outlet,
            Site = SelectedSite?.Copy(),
            Weather = SelectedWeather,
            Planes = ((ComboBoxItem)PlaneFilterBox.Items[PlaneFilterBox.SelectedIndex]).Tag as string ?? "alle",
        };
        ShowSession(session);

        _scanCts = new CancellationTokenSource();
        SetBusy(scanning: true);
        ScanProgress.Maximum = transponders.Count;
        ScanProgress.Value = 0;
        var started = Stopwatch.StartNew();

        var progress = new Progress<(int Done, int Total, ScanResult Result)>(p =>
        {
            _results.Add(p.Result);
            ScanProgress.Value = p.Done;
            var remaining = TimeSpan.FromSeconds(started.Elapsed.TotalSeconds / p.Done * (p.Total - p.Done));
            ProgressText.Text = L.F("{0} von {1} Transpondern · noch ca. {2:m\\:ss} min", p.Done, p.Total, remaining);
            SetStatus(L.F("{0} {1}: {2}, MER {3} dB, Pegel {4} dBµV", p.Result.FrequencyMHz, p.Result.PolarizationShort, p.Result.LockText, p.Result.MerDb, p.Result.LevelDbuV));
            RenderCharts();
        });

        string? path = null;
        try
        {
            await new Scanner(_client, new ScanOptions()).RunAsync(session, sat, transponders, progress, _scanCts.Token);
            var verdict = Assessment.Scan(session);
            SetStatus(L.F("Scan fertig: {0} von {1} Transpondern mit Lock, {2} Empfangslöcher – Ampel: {3}.", session.LockedCount, session.Results.Count, session.HoleCount, verdict.Headline));
            OverviewTab.IsSelected = true;
        }
        catch (OperationCanceledException)
        {
            SetStatus(L.F("Scan abgebrochen nach {0} Transpondern.", session.Results.Count));
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            SetStatus(L.F("Scan unterbrochen: {0}", ex.Message));
        }
        finally
        {
            if (session.Results.Count > 0)
            {
                path = _store.Save(session);
                var item = new SessionItem(path, session);
                _sessions.Insert(0, item);
                UpdateLocationLists();
                SessionList.SelectedItem = item;
            }
            _scanCts.Dispose();
            _scanCts = null;
            SetBusy(scanning: false);
            ProgressText.Text = path is null ? "" : L.T("Messreihe gespeichert");
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _scanCts?.Cancel();
        _liveCts?.Cancel();
    }

    private async Task StopAllAsync()
    {
        _scanCts?.Cancel();
        _liveCts?.Cancel();
        while (_scanCts is not null || _liveCts is not null)
            await Task.Delay(100);
    }

    private void SetBusy(bool scanning)
    {
        bool busy = _scanCts is not null || _liveCts is not null;
        ScanButton.IsEnabled = !busy && _client.IsConnected;
        StopButton.IsEnabled = scanning;
        LiveStartButton.IsEnabled = !busy && _client.IsConnected;
        LiveStopButton.IsEnabled = _liveCts is not null;
        MiniScanButton.IsEnabled = _client.IsConnected && _scanCts is null && !_miniScanRequested;
        SatelliteBox.IsEnabled = !busy;
        PlaneFilterBox.IsEnabled = !busy;
    }

    // ---------------------------------------------------------------- Live-Messung

    private void LivePlaneBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ClearMiniRange();
        UpdateLiveTransponders();
    }

    /// <summary>Transponder der gewählten Ebene; aus der angezeigten Messreihe mit ★-Vorschlag, sonst aus der Liste.</summary>
    private void UpdateLiveTransponders()
    {
        if (!IsInitialized || SelectedSatellite is not { } sat || LivePlaneBox.SelectedItem is not PlaneOption { Plane: var plane }) return;

        var session = _shownSession is { Results.Count: > 0 } s && SameSatellite(s, sat) ? s : null;
        var results = session?.Results.Where(r => Plane.Of(r) == plane).ToList() ?? [];
        var suggestion = ReferenceSelector.Suggest(plane, results);

        List<ReferenceOption> options;
        if (results.Count > 0)
        {
            options = results.OrderBy(r => r.FrequencyMHz)
                .Select(r => new ReferenceOption(
                    new Transponder(r.FrequencyMHz, r.Polarization, r.SymbolRateKs, r.DvbSystem, r.Modulation, r.Fec),
                    (r == suggestion ? "★  " : "     ") + $"{r.FrequencyMHz} {r.PolarizationShort} {r.SymbolRateKs}"
                        + (r.Locked ? $"   ·  MER {r.MerDb:0.0} dB  ·  {r.LevelDbuV:0.0} dBµV" : "   ·  " + L.T("kein Lock"))))
                .ToList();
        }
        else
        {
            options = _database.GetTransponders(sat)
                .Where(t => new Plane(t.FrequencyMHz >= 11700, t.Polarization) == plane)
                .OrderBy(t => t.FrequencyMHz)
                .Select(t => new ReferenceOption(t, $"{t} {t.DvbSystem} {t.Modulation} {t.Fec}".TrimEnd()))
                .ToList();
        }

        LiveTransponderBox.ItemsSource = options;
        LiveTransponderBox.SelectedItem = suggestion is null
            ? options.FirstOrDefault()
            : options.FirstOrDefault(o => o.Transponder.FrequencyMHz == suggestion.FrequencyMHz);
    }

    private bool _jumpingToChannel;

    /// <summary>Filtert die Senderliste beim Tippen (Teilstring, ohne Groß-/Kleinschreibung).</summary>
    private void LiveChannelBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key is System.Windows.Input.Key.Up or System.Windows.Input.Key.Down or System.Windows.Input.Key.Enter
            or System.Windows.Input.Key.Escape or System.Windows.Input.Key.Tab) return;
        var text = LiveChannelBox.Text;
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(LiveChannelBox.ItemsSource);
        if (view is null) return;
        view.Filter = string.IsNullOrWhiteSpace(text) || LiveChannelBox.SelectedItem is not null
            ? null
            : o => ((ChannelOnTransponder)o).Channel.Name.Contains(text.Trim(), StringComparison.CurrentCultureIgnoreCase);
        LiveChannelBox.IsDropDownOpen = true;

        // Cursor ans Ende, sonst markiert die ComboBox den getippten Text
        if (LiveChannelBox.Template.FindName("PART_EditableTextBox", LiveChannelBox) is TextBox box)
            box.SelectionStart = box.Text.Length;
    }

    /// <summary>Sender gewählt: Ebene und Transponder automatisch umstellen.</summary>
    private void LiveChannelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LiveChannelBox.SelectedItem is not ChannelOnTransponder channel || SelectedSatellite is not { } sat) return;

        _jumpingToChannel = true;
        try
        {
            var plane = new Plane(channel.FrequencyMHz >= 11700, channel.Pol);
            LivePlaneBox.SelectedItem = ((IEnumerable<PlaneOption>)LivePlaneBox.ItemsSource).First(p => p.Plane == plane);

            var options = (LiveTransponderBox.ItemsSource as IEnumerable<ReferenceOption> ?? []).ToList();
            var match = options.FirstOrDefault(o => o.Transponder.Polarization == channel.Pol
                                                    && Math.Abs(o.Transponder.FrequencyMHz - channel.FrequencyMHz) <= 2);
            if (match is null)
            {
                // Transponder nicht in der Messreihe – aus der Transponderliste ergänzen
                var tp = _database.GetTransponders(sat).FirstOrDefault(t => t.Polarization == channel.Pol
                                                                            && Math.Abs(t.FrequencyMHz - channel.FrequencyMHz) <= 2)
                         ?? new Transponder(channel.FrequencyMHz, channel.Pol, 27500);
                match = new ReferenceOption(tp, $"     {tp} {tp.DvbSystem} {tp.Modulation} {tp.Fec}   ·  {L.T("nicht gescannt")}".TrimEnd());
                options.Add(match);
                LiveTransponderBox.ItemsSource = options.OrderBy(o => o.Transponder.FrequencyMHz).ToList();
            }
            LiveTransponderBox.SelectedItem = match;
            SetStatus(L.F("{0}: {1} auf Ebene {2}.", channel.Channel.Name, match.Transponder, plane));
        }
        finally
        {
            _jumpingToChannel = false;
        }
    }

    private void LiveTransponderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tp = (LiveTransponderBox.SelectedItem as ReferenceOption)?.Transponder;
        ConfigureGauges(tp);
        if (!_jumpingToChannel && LiveChannelBox.SelectedItem is not null)
        {
            // Transponder von Hand gewechselt – gewählter Sender passt nicht mehr
            LiveChannelBox.SelectedItem = null;
            LiveChannelBox.Text = "";
        }
        if (tp is not null)
        {
            LiveTransponderBox.ToolTip = new ToolTip { Content = ChannelToolTip.Build(tp.FrequencyMHz, tp.Polarization, tp.SymbolRateKs, $"{tp.DvbSystem} {tp.Modulation} {tp.Fec}".Trim()) };
            var count = ChannelList.Current?.Get(PositionTenths, tp.FrequencyMHz, tp.Polarization).Count(c => c.Kind != ChannelKind.Data) ?? 0;
            if (count > 0) LiveInfoText.Text += "  ·  " + L.F("{0} Programme", count);
        }
        // Während der Messung sofort umschalten
        if (_liveCts is not null) _liveTarget = tp;
    }

    /// <summary>Skalen der Gauges für den Transponder: MER/C/N hängen von Modulation und FEC ab.</summary>
    private void ConfigureGauges(Transponder? tp)
    {
        var (threshold, basis) = SignalLimits.Threshold(tp?.DvbSystem, tp?.Modulation, tp?.Fec);
        GaugeMer.Scale = SignalLimits.MerScale(threshold, basis);
        GaugeCn.Scale = SignalLimits.CnScale(threshold, basis);
        GaugeLevel.Scale = SignalLimits.Level;
        GaugeQuality.Scale = SignalLimits.Quality;
        GaugeStrength.Scale = SignalLimits.Strength;
        GaugeBer.Scale = SignalLimits.BerScale(tp?.DvbSystem);
        GaugeMer.TooltipExtra = SignalLimits.ThresholdExamples;
        GaugeCn.TooltipExtra = SignalLimits.ThresholdExamples;
        LiveInfoText.Text = tp is null ? "" : L.F("{0}  ·  {1}  ·  Empfangsschwelle {2:0.0} dB", tp, basis, threshold);
        ShowReading(null);
        _peaks.Reset();
    }

    private void ResetPeaksButton_Click(object sender, RoutedEventArgs e)
    {
        _peaks.Reset();
        foreach (var g in Gauges)
            g.Peak = null;
        ClearMiniRange();
    }

    private GaugeControl[] Gauges => [GaugeMer, GaugeCn, GaugeLevel, GaugeQuality, GaugeStrength, GaugeBer];

    // ---------------------------------------------------------------- Mini-Scan der Ebene

    private async void MiniScanButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSatellite is not { } sat || !_client.IsConnected) return;
        if (_liveCts is not null)
        {
            // Die laufende Live-Schleife führt den Mini-Scan aus und stellt danach zurück
            _miniScanRequested = true;
            MiniScanButton.IsEnabled = false;
            return;
        }

        _scanCts = new CancellationTokenSource();
        SetBusy(scanning: true);
        try
        {
            await _client.SetDetailModeAsync(true, _scanCts.Token);
            await RunMiniScanAsync(sat, _scanCts.Token);
        }
        catch (OperationCanceledException)
        {
            SetStatus(L.T("Mini-Scan abgebrochen."));
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            SetStatus(L.F("Mini-Scan unterbrochen: {0}", ex.Message));
        }
        finally
        {
            try
            {
                await _client.SetDetailModeAsync(false, CancellationToken.None);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
            {
            }
            _scanCts.Dispose();
            _scanCts = null;
            SetLock(null, L.T("bereit"));
            SetBusy(scanning: false);
        }
    }

    /// <summary>Bis zu 5 Transponder gleichmäßig über die Frequenz der Ebene; bekannte Löcher und abgeschaltete werden übersprungen.</summary>
    private List<Transponder> MiniScanTransponders(Satellite sat, Plane plane)
    {
        var session = _shownSession is { Results.Count: > 0 } s && SameSatellite(s, sat) ? s : null;
        var measured = session?.Results.Where(r => Plane.Of(r) == plane).ToList() ?? [];
        bool Same(Transponder t, ScanResult r) => t.Polarization == r.Polarization && Math.Abs(t.FrequencyMHz - r.FrequencyMHz) <= 2;

        // Liste der Ebene ohne Transponder, die in der Messreihe keinen Lock hatten
        var all = _database.GetTransponders(sat)
            .Where(t => new Plane(t.FrequencyMHz >= 11700, t.Polarization) == plane)
            .Where(t => !measured.Any(r => !r.Locked && Same(t, r)))
            .ToList();
        // Gelockte Transponder der Messreihe, die in der Liste fehlen (ältere Liste), ergänzen
        all.AddRange(measured.Where(r => r.Locked && !all.Any(t => Same(t, r)))
            .Select(r => new Transponder(r.FrequencyMHz, r.Polarization, r.SymbolRateKs, r.DvbSystem, r.Modulation, r.Fec)));
        all = all.Where(t => Sf4000Protocol.EncodeTune(t, sat.LofLow, sat.LofHigh) is not null).OrderBy(t => t.FrequencyMHz).ToList();

        int n = Math.Min(5, all.Count);
        if (n <= 1) return all.Take(n).ToList();
        return Enumerable.Range(0, n).Select(i => all[(int)Math.Round(i * (all.Count - 1) / (double)(n - 1))]).Distinct().ToList();
    }

    private async Task RunMiniScanAsync(Satellite sat, CancellationToken ct)
    {
        if (LivePlaneBox.SelectedItem is not PlaneOption { Plane: var plane }) return;
        var transponders = MiniScanTransponders(sat, plane);
        if (transponders.Count == 0)
        {
            SetStatus(L.F("Keine Transponder für den Mini-Scan auf {0}.", plane));
            return;
        }

        ClearMiniRange();
        var scanner = new Scanner(_client, new ScanOptions { LockTimeout = TimeSpan.FromSeconds(4) });
        var results = new List<ScanResult>();
        for (int i = 0; i < transponders.Count; i++)
        {
            SetLock(null, L.F("Mini-Scan {0}/{1}", i + 1, transponders.Count));
            SetStatus(L.F("Mini-Scan {0}: {1} …", plane, transponders[i]));
            results.Add(await scanner.MeasureAsync(transponders[i], sat, ct));
            ShowMiniRange(results);
        }

        var locked = results.Where(r => r.Locked).ToList();
        var mers = locked.Where(r => r.MerDb.HasValue).Select(r => r.MerDb!.Value).ToList();
        var levels = results.Where(r => r.LevelDbuV.HasValue).Select(r => r.LevelDbuV!.Value).ToList();
        SetStatus(L.F("Mini-Scan {0}: {1} von {2} Transpondern mit Lock", plane, locked.Count, results.Count)
                  + (mers.Count > 0 ? $", MER {mers.Min():0.0} … {mers.Max():0.0} dB" : "")
                  + (levels.Count > 0 ? L.F(", Pegel {0:0.0} … {1:0.0} dBµV", levels.Min(), levels.Max()) : "")
                  + $"  ({string.Join(", ", results.Select(r => $"{r.FrequencyMHz} {r.PolarizationShort}{(r.Locked ? "" : " ✗")}"))})");
    }

    /// <summary>Minimum und Maximum des Mini-Scans als Bereich in den Gauges.</summary>
    private void ShowMiniRange(List<ScanResult> results)
    {
        var locked = results.Where(r => r.Locked).ToList();
        var tuned = results.Where(r => r.TuneAccepted).ToList();
        Set(GaugeMer, locked.Select(r => r.MerDb), v => v.ToString("0.0"));
        Set(GaugeCn, locked.Select(r => r.CnDb), v => v.ToString("0.0"));
        Set(GaugeLevel, tuned.Select(r => r.LevelDbuV), v => v.ToString("0.0"));
        Set(GaugeQuality, tuned.Select(r => (double?)r.QualityPercent), v => v.ToString("0"));
        Set(GaugeStrength, tuned.Select(r => (double?)r.StrengthPercent), v => v.ToString("0"));
        // Exponent → BER; der größere Exponent ist die kleinere (bessere) Fehlerrate
        Set(GaugeBer, locked.Select(r => SignalLimits.BerToExponent(r.Ber)), v => Math.Pow(10, -v).ToString("0.0E+0"));

        static void Set(GaugeControl g, IEnumerable<double?> values, Func<double, string> format)
        {
            var list = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
            g.RangeMin = list.Count > 0 ? list.Min() : null;
            g.RangeMax = list.Count > 0 ? list.Max() : null;
            g.RangeText = list.Count > 0 ? $"Mini {format(list.Min())} … {format(list.Max())}" : L.T("Mini: kein Lock");
        }
    }

    private void ClearMiniRange()
    {
        if (!IsInitialized) return;
        foreach (var g in Gauges)
        {
            g.RangeMin = g.RangeMax = null;
            g.RangeText = null;
        }
    }

    private async void LiveStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSatellite is not { } sat || LiveTransponderBox.SelectedItem is not ReferenceOption option) return;

        _liveCts = new CancellationTokenSource();
        var ct = _liveCts.Token;
        SetBusy(scanning: false);
        _liveTarget = option.Transponder;
        Transponder? tuned = null;
        var history = new LiveHistory();
        var clock = Stopwatch.StartNew();

        try
        {
            await _client.SetDetailModeAsync(true, ct);
            while (!ct.IsCancellationRequested)
            {
                if (_miniScanRequested)
                {
                    try
                    {
                        await RunMiniScanAsync(sat, ct);
                    }
                    finally
                    {
                        _miniScanRequested = false;
                        SetBusy(scanning: false);
                    }
                    tuned = null; // zurück auf den Live-Transponder
                    continue;
                }

                if (_liveTarget is { } target && target != tuned)
                {
                    tuned = target;
                    history.Clear();
                    _peaks.Reset();
                    SetLock(null, L.T("stelle ein …"));
                    if (!await _client.TuneAsync(target, sat, ct))
                        SetStatus(L.F("Gerät hat {0} nicht bestätigt.", target));
                    await Task.Delay(500, ct);
                }

                StatusReading r;
                try
                {
                    r = await _client.GetStatusAsync(ct);
                }
                catch (TimeoutException)
                {
                    continue;
                }

                ShowReading(r);
                history.Add(clock.Elapsed.TotalSeconds, r);
                RenderLive(history, tuned!);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
        {
            SetStatus(L.F("Live-Messung unterbrochen: {0}", ex.Message));
        }
        finally
        {
            try
            {
                await _client.SetDetailModeAsync(false, CancellationToken.None);
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException)
            {
            }
            _liveCts.Dispose();
            _liveCts = null;
            _liveTarget = null;
            _miniScanRequested = false;
            SetLock(null, L.T("gestoppt"));
            SetBusy(scanning: false);
        }
    }

    private void LiveStopButton_Click(object sender, RoutedEventArgs e) => _liveCts?.Cancel();

    // ---------------------------------------------------------------- Vollbild der Live-Messung

    private Window? _fullscreen;

    private void FullscreenButton_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F11 && LiveTab.IsSelected)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F1)
        {
            OpenHelp();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Nimmt den Inhalt der Live-Registerkarte in ein randloses Vollbildfenster auf dem aktuellen Bildschirm
    /// und gibt ihn beim Schließen an die Registerkarte zurück. Dieselben Steuerelemente werden verschoben,
    /// daher läuft eine laufende Messung ohne Unterbrechung weiter.
    /// </summary>
    private void ToggleFullscreen()
    {
        if (_fullscreen is not null)
        {
            _fullscreen.Close();
            return;
        }

        LiveTab.Content = new TextBlock
        {
            Text = L.T("Live-Messung läuft im Vollbild – Esc oder F11 kehrt zurück."),
            Style = (Style)FindResource("Hint"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var host = new Border { Padding = new Thickness(20), Child = LiveContent };
        _fullscreen = new Window
        {
            Title = L.T("DUR-line Scanner – Live-Messung"),
            Owner = this,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Background = (Brush)FindResource("AppBackground"),
            FontFamily = FontFamily,
            FontSize = 15,
            Content = host,
            ShowInTaskbar = false,
            // Auf dem Bildschirm des Hauptfensters maximieren
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = Left + 50,
            Top = Top + 50,
        };
        _fullscreen.Loaded += (_, _) => _fullscreen!.WindowState = WindowState.Maximized;
        _fullscreen.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is System.Windows.Input.Key.Escape or System.Windows.Input.Key.F11)
            {
                _fullscreen?.Close();
                e.Handled = true;
            }
        };
        _fullscreen.Closed += (_, _) =>
        {
            host.Child = null;
            LiveTab.Content = LiveContent;
            _fullscreen = null;
            FullscreenButton.Content = L.T("⛶  Vollbild");
            Activate();
        };
        FullscreenButton.Content = L.T("✕  Vollbild beenden (Esc)");
        _fullscreen.Show();
    }

    private void ShowReading(StatusReading? r)
    {
        double? berExp = SignalLimits.BerToExponent(r?.IsLocked == true ? r.Ber : null);
        double? mer = r?.IsLocked == true ? r.MerDb : null;
        double? cn = r?.IsLocked == true ? r.CnDb : null;

        Set(GaugeMer, mer, _peaks.Mer = Max(_peaks.Mer, mer));
        Set(GaugeCn, cn, _peaks.Cn = Max(_peaks.Cn, cn));
        Set(GaugeLevel, r?.LevelDbuV, _peaks.Level = Max(_peaks.Level, r?.LevelDbuV));
        Set(GaugeQuality, r?.Quality, _peaks.Quality = Max(_peaks.Quality, r?.Quality));
        Set(GaugeStrength, r?.StrengthPercent, _peaks.Strength = Max(_peaks.Strength, r?.StrengthPercent));
        Set(GaugeBer, berExp, _peaks.Ber = Max(_peaks.Ber, berExp));
        GaugeBer.ValueText = r?.IsLocked == true && r.Ber is { } ber ? (ber <= 1e-8 ? "< 1E-7" : ber.ToString("0.0E+0")) : "–";
        GaugeQuality.ValueText = r is null ? "–" : r.Quality.ToString();
        GaugeStrength.ValueText = r is null ? "–" : r.StrengthPercent.ToString();

        var threshold = GaugeMer.Scale!.Bands[1].From;
        GaugeMer.Caption = mer is { } m ? L.F("Reserve {0:+0.0;-0.0} dB", m - threshold) : null;
        GaugeCn.Caption = cn is { } c ? L.F("Reserve {0:+0.0;-0.0} dB", c - threshold) : null;

        if (r is not null) SetLock(r.IsLocked, r.IsLocked ? L.T("Lock") : r.State == StatusReading.StateSearching ? L.T("sucht …") : L.T("kein Lock"));
        _lastReading = r;
        RefreshLiveLight();

        static void Set(GaugeControl g, double? value, double? peak)
        {
            g.Value = value;
            g.Peak = peak;
        }
    }

    private void RefreshLiveLight()
    {
        if (!IsInitialized) return;
        var tp = _liveTarget ?? (LiveTransponderBox.SelectedItem as ReferenceOption)?.Transponder;
        LiveLight.Show(Assessment.Live(_lastReading, tp, CurrentConditions));
    }

    private static double? Max(double? a, double? b) => a is null ? b : b is null ? a : Math.Max(a.Value, b.Value);

    private void SetLock(bool? locked, string text)
    {
        LiveLockText.Text = text;
        LiveLockBadge.Background = locked switch
        {
            true => (Brush)FindResource("GoodBrush"),
            false => (Brush)FindResource("BadBrush"),
            null => Palette.Brush(MediaColor.FromRgb(0xB0, 0xB0, 0xB0)),
        };
    }

    private void RenderLive(LiveHistory history, Transponder tp)
    {
        var plot = PlotLive.Plot;
        plot.Clear();
        if (history.Times.Count > 0)
        {
            var mer = plot.Add.Scatter(history.Times.ToArray(), history.Mers.ToArray(), SeriesColors[0]);
            mer.LegendText = "MER dB";
            mer.MarkerSize = 0;
            mer.LineWidth = 2;
            var level = plot.Add.Scatter(history.Times.ToArray(), history.Levels.ToArray(), SeriesColors[3]);
            level.LegendText = L.T("Pegel dBµV");
            level.MarkerSize = 0;
            level.LineWidth = 2;
            level.Axes.YAxis = plot.Axes.Right;
        }
        plot.Axes.Title.Label.Text = L.F("Verlauf {0}", tp);
        plot.XLabel(L.T("Zeit (s)"));
        plot.Axes.Left.Label.Text = "MER dB";
        plot.Axes.Right.Label.Text = L.T("Pegel dBµV");
        plot.ShowLegend(Alignment.LowerLeft);
        plot.Axes.AutoScale();
        PlotLive.Refresh();
    }

    // ---------------------------------------------------------------- Messreihen

    private void LoadSessions()
    {
        _sessions.Clear();
        foreach (var (path, session) in _store.LoadAll())
        {
            AddMissingModulation(session);
            session.ClassifyMissingTransponders();
            _sessions.Add(new SessionItem(path, session));
        }
        UpdateLocationLists();
    }

    /// <summary>Messreihe vom selben Satelliten? Verglichen wird die Orbitposition, der Name hängt von der Liste ab.</summary>
    private static bool SameSatellite(ScanSession session, Satellite sat) =>
        Math.Abs(session.SatelliteLongitude - sat.Longitude) < 0.15;

    /// <summary>Ältere Messreihen (Liste ohne Modulation) aus der aktuellen Transponderliste ergänzen.</summary>
    private void AddMissingModulation(ScanSession session)
    {
        var sat = (SatelliteBox.ItemsSource as IEnumerable<Satellite>)?.FirstOrDefault(s => SameSatellite(session, s) && s.LofHigh >= 8000);
        if (sat is null) return;
        var list = _database.GetTransponders(sat);
        foreach (var r in session.Results.Where(r => r.Modulation is null || r.Fec is null))
        {
            var match = list.FirstOrDefault(t => t.Polarization == r.Polarization && t.SymbolRateKs == r.SymbolRateKs
                                                 && Math.Abs(t.FrequencyMHz - r.FrequencyMHz) <= 2);
            if (match is null) continue;
            r.DvbSystem = match.DvbSystem;
            r.Modulation = match.Modulation;
            r.Fec = match.Fec;
        }
    }

    private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_scanCts is not null) return;
        if (SessionList.SelectedItem is SessionItem item) ShowSession(item.Session);
    }

    private void CompareChanged(object sender, RoutedEventArgs e) => RenderCharts();

    private void MetricBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;
        MetricToolTip.Attach(MetricBox, Metric.Scale(null));
        RenderCharts();
    }

    private void ShowSession(ScanSession session)
    {
        _shownSession = session;
        ChannelToolTip.PositionTenths = session.SatelliteLongitude != 0 ? (int)Math.Round(session.SatelliteLongitude * 10) : PositionTenths;
        _results.Clear();
        foreach (var r in session.Results) _results.Add(r);
        ScanLight.Show(Assessment.Scan(session));
        RenderCharts();
        if (_liveCts is null) UpdateLiveTransponders();
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionList.SelectedItem is not SessionItem item)
        {
            SetStatus(L.T("Bitte zuerst eine Messreihe auswählen."));
            return;
        }
        var dialog = new SaveFileDialog
        {
            Filter = "CSV (Excel)|*.csv",
            FileName = System.IO.Path.GetFileNameWithoutExtension(item.Path) + ".csv",
            InitialDirectory = _store.Folder,
        };
        if (dialog.ShowDialog(this) != true) return;
        SessionStore.ExportCsv(item.Session, dialog.FileName);
        SetStatus(L.F("Exportiert: {0}", dialog.FileName));
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SessionList.SelectedItem is not SessionItem item) return;
        if (MessageBox.Show(this, L.F("Messreihe löschen?\n\n{0}\n{1}", item.Session.LocationLabel, item.Summary), L.T("Löschen"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        File.Delete(item.Path);
        _sessions.Remove(item);
        UpdateLocationLists();
        if (_shownSession == item.Session) ShowSession(new ScanSession());
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo(_store.Folder) { UseShellExecute = true });

    // ---------------------------------------------------------------- Standort-Archiv (ZIP)

    private void ExportSiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSite is not { } site)
        {
            SetStatus(L.T("Bitte oben zuerst einen Standort wählen."));
            return;
        }
        var sessions = SiteArchive.SessionsOf(site, _sessions.Select(s => (s.Path, s.Session))).ToList();
        if (sessions.Count == 0)
        {
            SetStatus(L.F("Für „{0}“ gibt es noch keine Messreihen (nur Scans mit gewähltem Standort zählen).", site.Name));
            return;
        }
        var dialog = new SaveFileDialog
        {
            Title = L.T("Standort exportieren"),
            Filter = L.T("Standort-Archiv (ZIP)|*.zip"),
            FileName = $"DurLine_{string.Concat(site.Name.Split(System.IO.Path.GetInvalidFileNameChars()))}_{DateTime.Now:yyyyMMdd}.zip",
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            int count = SiteArchive.Export(site, sessions, dialog.FileName);
            SetStatus(L.F("Standort „{0}“ mit {1} Messreihen exportiert: {2}", site.Name, count, dialog.FileName));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus(L.F("Export fehlgeschlagen: {0}", ex.Message));
        }
    }

    private void ImportSiteButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = L.T("Standort importieren"), Filter = L.T("Standort-Archiv (ZIP)|*.zip") };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var result = SiteArchive.Import(dialog.FileName, _store, _sites);
            if (result.Site is { } site && result.SiteAdded)
            {
                _sites.Add(site);
                SiteStore.Save(_sites);
            }
            LoadSessions();
            if (result.Site is { } imported) UpdateSiteBox(imported.Id);
            SetStatus(L.F("Importiert: Standort „{0}“ ({1}), ", result.Site?.Name ?? "?", L.T(result.SiteAdded ? "neu angelegt" : "war schon vorhanden"))
                      + L.F("{0} Messreihen übernommen", result.Imported)
                      + (result.Skipped > 0 ? L.F(", {0} schon vorhanden bzw. unlesbar", result.Skipped) : "") + ".");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            SetStatus(L.F("Import fehlgeschlagen: {0}", ex.Message));
        }
    }

    // ---------------------------------------------------------------- Messorte

    private static string AllLocations => L.T("Alle Messorte");

    private LocationKind SelectedLocationKind =>
        LocationKindBox.SelectedIndex == 1 ? LocationKind.LnbPort : LocationKind.Socket;

    private void LocationKindBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized) return;
        bool port = SelectedLocationKind == LocationKind.LnbPort;
        PlaneLabel.Text = port ? L.T("Port führt") : L.T("Ebenen");
        RoomPanel.Visibility = port ? Visibility.Hidden : Visibility.Visible;
        OutletLabel.Text = port ? L.T("LNB-Port") : L.T("Anschluss im Raum");
        int outlet = Math.Max(0, OutletBox.SelectedIndex);
        OutletBox.ItemsSource = Enumerable.Range(1, port ? 16 : 12).Select(i => port ? L.F("Port {0}", i) : L.F("Anschluss {0}", i)).ToList();
        OutletBox.SelectedIndex = Math.Min(outlet, OutletBox.Items.Count - 1);

        // Tag = gespeicherter Wert, Content = Anzeige
        string[] tags = ["alle", "Low V", "Low H", "High V", "High H"];
        for (int i = 0; i < PlaneFilterBox.Items.Count; i++)
        {
            var item = (ComboBoxItem)PlaneFilterBox.Items[i];
            item.Tag = tags[i];
            item.Content = i == 0
                ? port ? L.T("alle (Universal-/Quad-LNB)") : L.T("alle")
                : port ? L.F("nur {0} (Quattro-LNB)", tags[i]) : tags[i];
        }
        UpdateLocationLists();
    }

    private static string AllWeather => L.T("Jedes Wetter");

    /// <summary>Raumvorschläge (bereits gemessene zuerst) und Filterliste aus den bisherigen Messreihen.</summary>
    private void UpdateLocationLists()
    {
        // Vorschläge und Filter nur aus den Messreihen des gewählten Standorts
        var ofSite = _sessions.Where(s => OfSelectedSite(s.Session)).ToList();
        var typed = RoomBox.Text;
        var measured = ofSite.Select(s => s.Session.Room).OfType<string>()
            .Distinct(StringComparer.CurrentCultureIgnoreCase).Order().ToList();
        RoomBox.ItemsSource = measured.Concat(Rooms.All.Where(r => !measured.Contains(r, StringComparer.CurrentCultureIgnoreCase))).ToList();
        RoomBox.Text = typed;

        var selectedFilter = SessionFilterBox.SelectedItem as string;
        var filters = new List<string> { AllLocations };
        filters.AddRange(ofSite.Select(s => LocationKey(s.Session)).Distinct().Order());
        SessionFilterBox.ItemsSource = filters;
        SessionFilterBox.SelectedItem = filters.Contains(selectedFilter ?? "") ? selectedFilter : AllLocations;
    }

    private static string LocationKey(ScanSession s) =>
        (s.LocationKind == LocationKind.Socket ? L.T("Dose") : L.T("LNB-Port")) + " " + (s.LocationName.Length > 0 ? s.LocationName : "?");

    private void SessionFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplySessionFilter();

    /// <summary>Messreihe gehört zum oben gewählten Standort? Ohne gewählten Standort zählen alle.</summary>
    private bool OfSelectedSite(ScanSession s) => SelectedSite is not { } site || s.Site?.Id == site.Id;

    /// <summary>Liste der Messreihen: nur der gewählte Standort, dazu die Filter nach Messort und Wetter.</summary>
    private void ApplySessionFilter()
    {
        if (!IsInitialized) return;
        var location = SessionFilterBox.SelectedItem as string;
        var weather = (WeatherFilterBox.SelectedItem as WeatherOption)?.Weather;
        bool allLocations = location is null || location == AllLocations;
        System.Windows.Data.CollectionViewSource.GetDefaultView(_sessions).Filter = o =>
        {
            var s = ((SessionItem)o).Session;
            return OfSelectedSite(s) && (allLocations || LocationKey(s) == location) && (weather is null || s.Weather == weather);
        };
    }

    /// <summary>Nach einem Standortwechsel: neueste Messreihe des Standorts zeigen, bei leerem Standort nichts.</summary>
    private void ShowFirstSessionOfSite()
    {
        if (_scanCts is not null) return; // ein laufender Scan bleibt sichtbar
        foreach (var item in _sessions.Where(i => i.IsCompared && !OfSelectedSite(i.Session)))
            item.IsCompared = false;

        var first = _sessions.FirstOrDefault(i => OfSelectedSite(i.Session));
        SessionList.SelectedItem = first;
        if (first is null) ShowSession(new ScanSession());
    }

    /// <summary>Alle Messreihen mit Standort, Satellit und Wetter der angezeigten zum Vergleich anhaken.</summary>
    private void SameConditionsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_shownSession is not { Results.Count: > 0 } shown)
        {
            SetStatus(L.T("Bitte zuerst eine Messreihe anzeigen."));
            return;
        }
        int count = 0;
        foreach (var item in _sessions)
        {
            var s = item.Session;
            bool same = s != shown && Math.Abs(s.SatelliteLongitude - shown.SatelliteLongitude) < 0.15 && s.Weather == shown.Weather
                        && s.Site?.Id == shown.Site?.Id;
            item.IsCompared = same;
            if (same) count++;
        }
        RenderCharts();
        SetStatus(count == 0
            ? L.F("Keine weitere Messreihe mit gleichem Standort und Wetter ({0}).", shown.WeatherText)
            : L.F("{0} Messreihen mit gleichem Standort und Wetter ({1}) zum Vergleich angehakt.", count, shown.WeatherText));
    }

    // ---------------------------------------------------------------- Diagramme

    private sealed record MetricInfo(
        string Label,
        Func<ScanResult, double?> Value,
        Func<ScanResult, Grade?> Grade,
        Func<ScanResult?, MetricScale> Scale,
        double Min,
        double Max);

    private MetricInfo Metric => MetricBox.SelectedIndex switch
    {
        1 => new("C/N (dB)", r => r.CnDb, r => r.CnGrade,
            r => SignalLimits.CnScale(r?.Threshold.Db ?? 7.9, r?.Threshold.Description ?? L.T("DVB-S2 8PSK 3/4 als Beispiel")), 0, 22),
        2 => new(L.T("Pegel (dBµV)"), r => r.LevelDbuV, r => r.LevelGrade, _ => SignalLimits.Level, 30, 90),
        3 => new(L.T("Qualität (%)"), r => r.QualityPercent, r => r.QualityGrade, _ => SignalLimits.Quality, 0, 100),
        4 => new(L.T("Stärke (%)"), r => r.StrengthPercent, r => r.StrengthGrade, _ => SignalLimits.Strength, 0, 100),
        _ => new("MER (dB)", r => r.MerDb, r => r.MerGrade,
            r => SignalLimits.MerScale(r?.Threshold.Db ?? 7.9, r?.Threshold.Description ?? L.T("DVB-S2 8PSK 3/4 als Beispiel")), 0, 22),
    };

    private void RenderCharts()
    {
        if (!IsInitialized) return;
        var session = _shownSession ?? new ScanSession();
        var compared = _sessions.Where(s => s.IsCompared && s.Session != session).ToList();

        RenderPolarization(PlotH, Polarization.Horizontal, session, compared);
        RenderPolarization(PlotV, Polarization.Vertical, session, compared);
        RenderLegend(compared.Count > 0);
        RenderPlanes(session);
        RenderCompare(session, compared);
    }

    private void RenderPolarization(WpfPlot control, Polarization pol, ScanSession session, List<SessionItem> compared)
    {
        var metric = Metric;
        var plot = control.Plot;
        plot.Clear();

        var results = session.Results.Where(r => r.Polarization == pol).ToList();
        double top = metric.Max;

        // Empfangslöcher rot hinterlegt, vermutlich abgeschaltete Transponder grau
        var background = results.Where(r => !r.Locked).Select(r => new Bar
        {
            Position = r.FrequencyMHz,
            Value = top,
            ValueBase = metric.Min,
            Size = 10,
            FillColor = r.ProbablyInactive ? InactiveColor : HoleColor,
            LineWidth = 0,
        });
        var bars = results.Where(r => metric.Value(r).HasValue).Select(r => new Bar
        {
            Position = r.FrequencyMHz,
            Value = Math.Max(metric.Value(r)!.Value, metric.Min),
            ValueBase = metric.Min,
            Size = 7,
            FillColor = r.ProbablyInactive ? Color.FromHex("#BDBDBD") : Palette.ToPlot(Palette.For(metric.Grade(r) ?? Grade.VeryBad)),
            LineWidth = 0,
        });
        plot.Add.Bars(background.Concat(bars).ToList());

        for (int i = 0; i < compared.Count; i++)
        {
            var points = compared[i].Session.Results.Where(r => r.Polarization == pol && metric.Value(r).HasValue)
                .OrderBy(r => r.FrequencyMHz).ToList();
            if (points.Count == 0) continue;
            var line = plot.Add.Scatter(
                points.Select(r => (double)r.FrequencyMHz).ToArray(),
                points.Select(r => metric.Value(r)!.Value).ToArray(),
                SeriesColors[i % SeriesColors.Length]);
            line.MarkerSize = 4;
            line.LineWidth = 1.5f;
            line.LegendText = L.F("{0}  {1:dd.MM. HH:mm}", compared[i].Session.LocationLabel, compared[i].Session.Started);
        }

        // Grenze Low/High-Band
        var split = plot.Add.VerticalLine(11700, 1, Color.FromHex("#9E9E9E"));
        split.LinePattern = LinePattern.Dashed;
        split.Text = "Low | High";
        split.LabelFontSize = 10;

        int locked = results.Count(r => r.Locked);
        int holes = results.Count(r => !r.Locked && !r.ProbablyInactive);
        plot.Axes.Title.Label.Text = $"{(pol == Polarization.Horizontal ? L.T("Horizontal") : L.T("Vertikal"))}   ·   " + L.F("{0} von {1} mit Lock", locked, results.Count)
                                     + (holes > 0 ? "   ·   " + L.F("{0} Empfangslöcher", holes) : "");
        plot.XLabel(L.T("Frequenz (MHz)"));
        plot.YLabel(metric.Label);
        plot.Axes.SetLimits(10650, 12800, metric.Min, top);
        if (compared.Count > 0) plot.ShowLegend(Alignment.LowerLeft);
        else plot.HideLegend();
        control.Refresh();
    }

    private void RenderLegend(bool hasComparison)
    {
        LegendPanel.Children.Clear();
        foreach (var grade in new[] { Grade.VeryGood, Grade.Good, Grade.Marginal, Grade.Bad, Grade.VeryBad })
            AddLegend(Palette.For(grade), SignalLimits.GradeText(grade));
        AddLegend(MediaColor.FromArgb(0x40, Palette.Bad.R, Palette.Bad.G, Palette.Bad.B), L.T("Empfangsloch"));
        AddLegend(MediaColor.FromRgb(0xD5, 0xD5, 0xD5), L.T("vermutl. abgeschaltet"));
        if (hasComparison) AddLegend(Palette.Accent, L.T("Linien = Vergleich"));

        void AddLegend(MediaColor color, string text)
        {
            LegendPanel.Children.Add(new Rectangle
            {
                Width = 10, Height = 10, RadiusX = 2, RadiusY = 2, Fill = Palette.Brush(color),
                Margin = new Thickness(12, 0, 5, 0), VerticalAlignment = VerticalAlignment.Center,
            });
            LegendPanel.Children.Add(new TextBlock { Text = text, Foreground = (Brush)FindResource("TextSecondary"), VerticalAlignment = VerticalAlignment.Center });
        }
    }

    private void RenderPlanes(ScanSession session)
    {
        var stats = PlaneAnalysis.Analyze(session);
        PlaneGrid.ItemsSource = stats;

        // Pegel je Transponder über ZF, eine Farbe je Ebene – Unterschiede und Schräglage sind direkt sichtbar
        var plot = PlotPlanes.Plot;
        plot.Clear();
        AddLevelBands(plot);
        for (int i = 0; i < Plane.All.Length; i++)
        {
            var plane = Plane.All[i];
            var points = session.Results.Where(r => r.Locked && r.LevelDbuV.HasValue && Plane.Of(r) == plane)
                .OrderBy(r => r.FrequencyMHz).ToList();
            if (points.Count == 0) continue;
            int lof = plane.HighBand ? 10600 : 9750;
            var scatter = plot.Add.Scatter(
                points.Select(r => (double)(r.FrequencyMHz - lof)).ToArray(),
                points.Select(r => r.LevelDbuV!.Value).ToArray(),
                SeriesColors[i]);
            scatter.LineWidth = 0;
            scatter.MarkerSize = 7;
            scatter.LegendText = plane.ToString();

            if (stats.First(s => s.Plane == plane).LevelMean is { } m)
            {
                var avg = plot.Add.Line(950, m, 2150, m);
                avg.Color = SeriesColors[i];
                avg.LinePattern = LinePattern.Dashed;
            }
        }

        plot.Axes.Title.Label.Text = L.T("Pegel je Ebene über der ZF   ·   gestrichelt = Mittelwert, Hintergrund = Bewertung");
        plot.XLabel(L.T("ZF-Frequenz am Kabel (MHz)"));
        plot.YLabel(L.T("Pegel (dBµV)"));
        plot.Axes.SetLimits(900, 2200, 40, 85);
        plot.ShowLegend(Alignment.LowerRight);
        PlotPlanes.Refresh();
    }

    /// <summary>Farbige Hintergrundbänder für die Pegelbewertung.</summary>
    private static void AddLevelBands(Plot plot)
    {
        foreach (var band in SignalLimits.Level.Bands)
        {
            double from = Math.Max(band.From, 30), to = Math.Min(band.To, 100);
            var span = plot.Add.HorizontalSpan(from, to);
            span.FillStyle.Color = Palette.ToPlot(Palette.For(band.Grade)).WithAlpha(0.07);
            span.LineStyle.Width = 0;
        }
    }

    private void RenderCompare(ScanSession session, List<SessionItem> compared)
    {
        var sessions = new[] { session }.Concat(compared.Select(c => c.Session)).Where(s => s.Results.Count > 0).ToList();
        var rows = sessions.Select(s =>
        {
            var stats = PlaneAnalysis.Analyze(s).ToDictionary(p => p.Plane.ToString());
            double? Mean(string plane) => stats[plane].LevelMean;
            var mers = s.Results.Where(r => r.Locked && r.MerDb.HasValue).Select(r => r.MerDb!.Value).ToList();
            var notes = stats.Values.Where(p => p.Severity != Severity.Ok).Select(p => $"{p.PlaneName}: {p.Assessment}");
            return new CompareRow(
                s.LocationLabel, s.Started, s.Site?.Name ?? "–", s.WeatherText, LightText(Assessment.Scan(s).Light),
                $"{s.LockedCount}/{s.Results.Count}", s.HoleCount,
                Mean("Low V"), Mean("Low H"), Mean("High V"), Mean("High H"),
                mers.Count > 0 ? Math.Round(mers.Average(), 1) : null,
                string.Join(" | ", notes));
        }).ToList();
        CompareGrid.ItemsSource = rows;

        // Gruppierte Balken: pro Ebene ein Balken je Messreihe
        var plot = PlotCompare.Plot;
        plot.Clear();
        AddLevelBands(plot);
        double groupWidth = 0.8;
        double barWidth = groupWidth / Math.Max(1, rows.Count);
        for (int si = 0; si < rows.Count; si++)
        {
            double?[] values = [rows[si].LowV, rows[si].LowH, rows[si].HighV, rows[si].HighH];
            var bars = values.Select((v, pi) => new Bar
            {
                Position = pi - groupWidth / 2 + barWidth * (si + 0.5),
                Value = v ?? 40,
                ValueBase = 40,
                Size = barWidth * 0.88,
                FillColor = SeriesColors[si % SeriesColors.Length],
                LineWidth = 0,
            }).ToList();
            var barPlot = plot.Add.Bars(bars);
            barPlot.LegendText = L.F("{0}  {1:dd.MM. HH:mm}", rows[si].Location, rows[si].Started);
        }
        plot.Axes.Bottom.SetTicks([0, 1, 2, 3], ["Low V", "Low H", "High V", "High H"]);
        plot.Axes.Title.Label.Text = L.T("Mittlerer Pegel je Ebene und Messort");
        plot.YLabel(L.T("Pegel (dBµV)"));
        plot.Axes.SetLimits(-0.6, 3.6, 40, 85);
        if (rows.Count > 0) plot.ShowLegend(Alignment.UpperRight);
        PlotCompare.Refresh();
    }

    internal static string LightText(Light light) => light switch
    {
        Light.Red => L.T("● rot"),
        Light.Yellow => L.T("● gelb"),
        Light.Green => L.T("● grün"),
        _ => "–",
    };

    private static void StylePlot(Plot plot)
    {
        plot.Font.Set("Segoe UI");
        plot.FigureBackground.Color = Colors.White;
        plot.DataBackground.Color = Colors.White;
        plot.Axes.Color(AxisColor);
        plot.Axes.FrameColor(Color.FromHex("#D0D0D0"));
        plot.Grid.MajorLineColor = GridColor;
        plot.Axes.Title.Label.FontSize = 15;
        plot.Axes.Title.Label.Bold = false;
        foreach (var axis in new IAxis[] { plot.Axes.Bottom, plot.Axes.Left, plot.Axes.Right })
        {
            axis.Label.FontSize = 13;
            axis.Label.Bold = false;
            axis.TickLabelStyle.FontSize = 12;
        }
        plot.Legend.FontSize = 12;
        plot.Legend.OutlineColor = Color.FromHex("#E0E0E0");
    }

    private void AttachColumnToolTips()
    {
        var mer = SignalLimits.GenericSnr("MER");
        SetHeaderTip(ColMer, mer, SignalLimits.ThresholdExamples);
        SetHeaderTip(ColMargin, mer, L.T("Reserve = gemessene MER minus Empfangsschwelle des Transponders."));
        SetHeaderTip(ColGrade, mer, L.T("Bewertung der MER-Reserve."));
        SetHeaderTip(ColCn, SignalLimits.GenericSnr("C/N"), SignalLimits.ThresholdExamples);
        SetHeaderTip(ColLevel, SignalLimits.Level);
        SetHeaderTip(ColQuality, SignalLimits.Quality);
        SetHeaderTip(ColStrength, SignalLimits.Strength);
        SetHeaderTip(ColBer, SignalLimits.BerExponent);
        MetricToolTip.Attach(MetricBox, Metric.Scale(null));

        static void SetHeaderTip(DataGridColumn column, MetricScale scale, string? extra = null)
        {
            var header = new TextBlock { Text = column.Header?.ToString() };
            MetricToolTip.Attach(header, scale, extra);
            column.Header = header;
        }
    }

    // ----------------------------------------------------------------

    private void SetStatus(string text) => StatusText.Text = text;

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        _fullscreen?.Close();
        _scanCts?.Cancel();
        _liveCts?.Cancel();
        await _client.DisposeAsync();
    }

    private sealed class LivePeaks
    {
        public double? Mer, Cn, Level, Quality, Strength, Ber;

        public void Reset() => Mer = Cn = Level = Quality = Strength = Ber = null;
    }

    /// <summary>Verlauf der Live-Messung, begrenzt auf die letzten 10 Minuten bei ~1 Messung/s.</summary>
    private sealed class LiveHistory
    {
        private const int MaxPoints = 600;

        public List<double> Times { get; } = [];
        public List<double> Mers { get; } = [];
        public List<double> Levels { get; } = [];

        public void Add(double t, StatusReading r)
        {
            Times.Add(t);
            Mers.Add(r.IsLocked ? r.MerDb ?? 0 : 0);
            Levels.Add(r.LevelDbuV ?? 0);
            if (Times.Count <= MaxPoints) return;
            Times.RemoveAt(0);
            Mers.RemoveAt(0);
            Levels.RemoveAt(0);
        }

        public void Clear()
        {
            Times.Clear();
            Mers.Clear();
            Levels.Clear();
        }
    }
}

public sealed record PlaneOption(Plane Plane)
{
    public override string ToString() => Plane.ToString() switch
    {
        "Low V" => L.T("Low V  (10,7–11,7 GHz, vertikal)"),
        "Low H" => L.T("Low H  (10,7–11,7 GHz, horizontal)"),
        "High V" => L.T("High V  (11,7–12,75 GHz, vertikal)"),
        _ => L.T("High H  (11,7–12,75 GHz, horizontal)"),
    };
}

public sealed record ReferenceOption(Transponder Transponder, string Label)
{
    public override string ToString() => Label;
}

public sealed record CompareRow(
    string Location, DateTime Started, string Site, string Weather, string Light, string LockText, int Holes,
    double? LowV, double? LowH, double? HighV, double? HighH, double? Mer, string Notes);

public sealed class SessionItem(string path, ScanSession session) : INotifyPropertyChanged
{
    private bool _isCompared;

    public string Path { get; } = path;

    public ScanSession Session { get; } = session;

    public string Summary =>
        L.F("{0:dd.MM.yyyy HH:mm}  ·  {1}/{2} Lock  ·  {3} Löcher", Session.Started, Session.LockedCount, Session.Results.Count, Session.HoleCount);

    public string Conditions => $"{Session.WeatherText}" + (Session.Site is { } site ? $"  ·  {site.Name}, {site.DishCm} cm" : "");

    public Verdict Verdict => field ??= Assessment.Scan(Session);

    public Brush LightBrush => Verdict.Light switch
    {
        Light.Red => Palette.Brush(Palette.Bad),
        Light.Yellow => Palette.Brush(MediaColor.FromRgb(0xF2, 0xB1, 0x0C)),
        Light.Green => Palette.Brush(Palette.VeryGood),
        _ => Palette.Brush(MediaColor.FromRgb(0xC8, 0xC8, 0xC8)),
    };

    public bool IsCompared
    {
        get => _isCompared;
        set
        {
            _isCompared = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompared)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
