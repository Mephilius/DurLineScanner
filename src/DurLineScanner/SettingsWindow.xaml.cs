using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DurLineScanner.Data;
using DurLineScanner.Scanning;

namespace DurLineScanner;

/// <summary>Standortverwaltung und Receiver für die Senderliste.</summary>
public partial class SettingsWindow : Window
{
    private static readonly int[] DishSizes = [45, 55, 60, 65, 75, 80, 85, 90, 100, 110, 120, 150, 180];

    private readonly AppSettings _settings;
    private readonly Satellite? _satellite;
    private bool _loading;
    private string _geocodedAddress = "";

    public SettingsWindow(AppSettings settings, List<Site> sites, Satellite? satellite, bool showReceiver = false)
    {
        InitializeComponent();
        L.Translate(this);
        _settings = settings;
        _satellite = satellite;
        Sites = sites;

        SiteDishBox.ItemsSource = DishSizes;
        SiteList.ItemsSource = Sites;
        if (Sites.Count > 0) SiteList.SelectedIndex = 0;
        else UpdateSiteForm();

        ReceiverKindBox.SelectedIndex = (int)settings.ReceiverKind;
        ReceiverAddressBox.Text = settings.ReceiverAddress;
        ReceiverPortBox.Text = settings.ReceiverPort?.ToString() ?? "";
        ReceiverHttpsBox.IsChecked = settings.ReceiverHttps;
        ReceiverUserBox.Text = settings.ReceiverUser;
        ReceiverPasswordBox.Password = settings.ReceiverPassword;
        LoadOnStartBox.IsChecked = settings.LoadChannelsOnStart;
        LanguageBox.SelectedIndex = (int)settings.Language;
        LanguageHint.Visibility = Visibility.Collapsed;
        UpdateReceiverHint();

        if (showReceiver) Tabs.SelectedItem = ReceiverTab;
    }

    public List<Site> Sites { get; }

    /// <summary>Wurde hier eine neue Senderliste geladen?</summary>
    public bool ChannelsLoaded { get; private set; }

    private Site? SelectedSite => SiteList.SelectedItem as Site;

    // ---------------------------------------------------------------- Standorte

    private void NewSiteButton_Click(object sender, RoutedEventArgs e)
    {
        var site = new Site { Name = Sites.Count == 0 ? L.T("Zuhause") : L.F("Standort {0}", Sites.Count + 1) };
        Sites.Add(site);
        SiteList.Items.Refresh();
        SiteList.SelectedItem = site;
        SiteNameBox.Focus();
        SiteNameBox.SelectAll();
    }

    private void DeleteSiteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSite is not { } site) return;
        if (MessageBox.Show(this, L.F("Standort „{0}“ löschen?\n\nGespeicherte Messreihen behalten ihre Standortdaten.", site), L.T("Löschen"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        Sites.Remove(site);
        SiteList.Items.Refresh();
        SiteList.SelectedIndex = Sites.Count > 0 ? 0 : -1;
    }

    private void SiteList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSiteForm();

    private void UpdateSiteForm()
    {
        _loading = true;
        var site = SelectedSite;
        SiteForm.IsEnabled = site is not null;
        DeleteSiteButton.IsEnabled = site is not null;
        SiteNameBox.Text = site?.Name ?? "";
        SiteAddressBox.Text = site?.Address ?? "";
        SiteLatBox.Text = site?.Latitude?.ToString("0.00000", CultureInfo.CurrentCulture) ?? "";
        SiteLonBox.Text = site?.Longitude?.ToString("0.00000", CultureInfo.CurrentCulture) ?? "";
        SiteDishBox.Text = site?.DishCm.ToString() ?? "";
        SiteGainBox.Text = site?.LnbGainDb.ToString("0.#", CultureInfo.CurrentCulture) ?? "";
        GeocodeText.Text = site is null ? L.T("Noch kein Standort – mit „+ Neu“ anlegen.") : "";
        _geocodedAddress = site?.HasCoordinates == true ? site.Address : "";
        _loading = false;
        UpdateLookInfo();
    }

    private void SiteField_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading || SelectedSite is not { } site) return;
        bool renamed = site.Name != SiteNameBox.Text.Trim();
        site.Name = SiteNameBox.Text.Trim();
        site.Address = SiteAddressBox.Text.Trim();
        site.Latitude = ParseNumber(SiteLatBox.Text, -90, 90);
        site.Longitude = ParseNumber(SiteLonBox.Text, -180, 180);
        if (ParseNumber(SiteDishBox.Text, 20, 500) is { } dish) site.DishCm = (int)Math.Round(dish);
        if (ParseNumber(SiteGainBox.Text, 0, 90) is { } gain) site.LnbGainDb = gain;
        if (renamed) SiteList.Items.Refresh();
        UpdateLookInfo();
    }

    private static double? ParseNumber(string text, double min, double max)
    {
        text = text.Trim();
        // Komma und Punkt annehmen, damit auch aus Karten kopierte Koordinaten passen
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var v)
            && !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
            return null;
        return v >= min && v <= max ? v : null;
    }

    private async void SiteAddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Automatisch nur, wenn sich die Adresse geändert hat
        if (SelectedSite is { Address.Length: > 3 } site && site.Address != _geocodedAddress)
            await GeocodeAsync(site);
    }

    private async void GeocodeButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSite is { } site) await GeocodeAsync(site);
    }

    private async Task GeocodeAsync(Site site)
    {
        if (site.Address.Length == 0)
        {
            GeocodeText.Text = L.T("Bitte zuerst eine Adresse eingeben.");
            return;
        }
        GeocodeButton.IsEnabled = false;
        GeocodeText.Text = L.T("Suche Adresse bei OpenStreetMap …");
        _geocodedAddress = site.Address;
        try
        {
            var hit = await Geocoder.LookupAsync(site.Address);
            if (hit is null)
            {
                GeocodeText.Text = L.T("Adresse nicht gefunden – Schreibweise prüfen oder Koordinaten von Hand eintragen.");
                return;
            }
            site.Latitude = Math.Round(hit.Latitude, 5);
            site.Longitude = Math.Round(hit.Longitude, 5);
            if (SelectedSite == site)
            {
                _loading = true;
                SiteLatBox.Text = site.Latitude.Value.ToString("0.00000", CultureInfo.CurrentCulture);
                SiteLonBox.Text = site.Longitude.Value.ToString("0.00000", CultureInfo.CurrentCulture);
                _loading = false;
                GeocodeText.Text = L.F("Gefunden: {0}  (Daten © OpenStreetMap-Mitwirkende)", hit.DisplayName);
                UpdateLookInfo();
            }
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            GeocodeText.Text = L.F("Suche fehlgeschlagen: {0}", ex.Message);
        }
        finally
        {
            GeocodeButton.IsEnabled = true;
        }
    }

    /// <summary>Blickrichtung zum Satelliten und Empfangserwartung des gewählten Standorts.</summary>
    private void UpdateLookInfo()
    {
        var site = SelectedSite;
        var sat = _satellite;
        LookTitle.Text = sat is null ? L.T("Blickrichtung") : L.F("Blickrichtung zu {0}", sat.Name);
        if (site is null || sat is null)
        {
            LookText.Text = "";
            return;
        }

        var lines = new List<string>();
        if (site.HasCoordinates)
        {
            var look = LinkBudget.Look(site.Latitude!.Value, site.Longitude!.Value, sat.Longitude);
            lines.Add(look.Visible
                ? L.F("Azimut {0:0.0}° (Kompass, ohne Missweisung)  ·  Elevation {1:0.0}°  ·  LNB-Skew {2:+0.0;-0.0}°  ·  Entfernung {3:N0} km",
                      look.AzimuthDeg, look.ElevationDeg, look.SkewDeg, look.DistanceKm)
                : L.T("Der Satellit steht von hier aus unter dem Horizont."));
        }
        else
        {
            lines.Add(L.T("Ohne Koordinaten wird Mitteleuropa angenommen (Elevation 30°)."));
        }

        var clear = LinkBudget.Expect(site, sat.Longitude, 11700, 27500, Weather.Sunny);
        var rain = LinkBudget.Expect(site, sat.Longitude, 11700, 27500, Weather.Rain);
        var storm = LinkBudget.Expect(site, sat.Longitude, 11700, 27500, Weather.Thunderstorm);
        lines.Add(L.F("Erwartung für {0} cm bei 11.700 MHz / 27.500 kS/s:  C/N {1:0.0} dB, MER ca. {2:0.0} dB bei Sonne, {3:0.0} dB bei Regen, {4:0.0} dB bei Gewitter.",
            site.DishCm, clear.CnDb, clear.MerDb, rain.MerDb, storm.MerDb));
        lines.Add(L.F("Pegel am LNB-Ausgang ca. {0:0} dBµV (LNB {1:0.#} dB) – an der Dose weniger, je nach Kabel und Verteilung.", clear.LevelDbuV, site.LnbGainDb));
        lines.Add(L.F("Richtwerte mit {0:0} dBW Sendeleistung (Mitteleuropa), Wirkungsgrad {1:P0}, Rauschtemperatur {2:0} K.",
            LinkBudget.EirpDbw, LinkBudget.DishEfficiency, LinkBudget.ClearSkyNoiseK));
        LookText.Text = string.Join("\n", lines);
    }

    // ---------------------------------------------------------------- Receiver

    private void ReceiverKindBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateReceiverHint();

    // ---------------------------------------------------------------- Allgemein

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized) LanguageHint.Visibility = Visibility.Visible;
    }

    private void ReceiverConnection_Changed(object sender, RoutedEventArgs e) => UpdateReceiverHint();

    /// <summary>Vollständig eingefügte Adresse wie "https://server:8443" auf Adresse, Port und HTTPS verteilen.</summary>
    private void ReceiverAddressBox_LostFocus(object sender, RoutedEventArgs e)
    {
        var text = ReceiverAddressBox.Text.Trim();
        if (!text.Contains("://") && !text.Contains(':')) return;
        var split = new AppSettings { ReceiverAddress = text, ReceiverHttps = ReceiverHttpsBox.IsChecked == true, ReceiverPort = ParsePort() };
        split.SplitReceiverAddress();
        ReceiverAddressBox.Text = split.ReceiverAddress;
        ReceiverPortBox.Text = split.ReceiverPort?.ToString() ?? "";
        ReceiverHttpsBox.IsChecked = split.ReceiverHttps;
        UpdateReceiverHint();
    }

    private int? ParsePort() =>
        int.TryParse(ReceiverPortBox.Text.Trim(), out int port) && port is > 0 and < 65536 ? port : null;

    private void UpdateReceiverHint()
    {
        if (!IsInitialized) return;
        var kind = (ReceiverKind)ReceiverKindBox.SelectedIndex;
        bool active = kind != ReceiverKind.None;
        ReceiverAddressBox.IsEnabled = ReceiverPortBox.IsEnabled = ReceiverHttpsBox.IsEnabled = active;
        ReceiverUserBox.IsEnabled = ReceiverPasswordBox.IsEnabled = active;
        ReceiverLoadButton.IsEnabled = LoadOnStartBox.IsEnabled = active;
        if (!active)
        {
            ReceiverAddressHint.Text = "";
            return;
        }

        var preview = new AppSettings
        {
            ReceiverKind = kind,
            ReceiverAddress = ReceiverAddressBox.Text.Trim(),
            ReceiverPort = ParsePort(),
            ReceiverHttps = ReceiverHttpsBox.IsChecked == true,
        };
        var example = kind == ReceiverKind.Enigma2 ? L.T("z.B. 192.168.1.20 oder vuplus.local") : L.T("z.B. 192.168.1.30 oder tv.example.de");
        ReceiverAddressHint.Text = L.F("Name oder IP ohne http:// ({0}). Port leer = Standard {1}.", example, preview.ReceiverDefaultPort)
                                   + (preview.ReceiverAddress.Length > 0 ? "  " + L.F("Verbindung: {0}", ReceiverChannelSource.BaseUri(preview)) : "");
    }

    private void ApplyReceiverFields()
    {
        _settings.ReceiverKind = (ReceiverKind)ReceiverKindBox.SelectedIndex;
        _settings.ReceiverAddress = ReceiverAddressBox.Text.Trim();
        _settings.ReceiverPort = ParsePort();
        _settings.ReceiverHttps = ReceiverHttpsBox.IsChecked == true;
        _settings.SplitReceiverAddress();
        _settings.ReceiverUser = ReceiverUserBox.Text.Trim();
        _settings.ReceiverPassword = ReceiverPasswordBox.Password;
        _settings.LoadChannelsOnStart = LoadOnStartBox.IsChecked == true;
        _settings.Language = (AppLanguage)Math.Max(0, LanguageBox.SelectedIndex);
    }

    private async void ReceiverLoadButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyReceiverFields();
        if (!_settings.ReceiverConfigured)
        {
            ReceiverStatusText.Text = L.T("Bitte die Adresse des Receivers eintragen.");
            return;
        }
        ReceiverLoadButton.IsEnabled = false;
        ReceiverStatusText.Text = L.F("Verbinde mit {0} …", ReceiverChannelSource.BaseUri(_settings));
        try
        {
            ChannelList.Current = await ReceiverChannelSource.LoadAsync(_settings);
            ChannelsLoaded = true;
            int here = _satellite is null ? 0 : ChannelList.Current.AllAt((int)Math.Round(_satellite.Longitude * 10)).Count;
            ReceiverStatusText.Text = L.F("✓ Senderliste geladen: {0}", ChannelList.Current.Description)
                                      + (_satellite is null ? "" : L.F(", davon {0} auf {1}.", here, _satellite.Name));
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or InvalidOperationException
                                       or InvalidDataException or System.Text.Json.JsonException or UriFormatException)
        {
            ReceiverStatusText.Text = $"✗ {ex.Message}";
        }
        finally
        {
            ReceiverLoadButton.IsEnabled = true;
        }
    }

    // ----------------------------------------------------------------

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        ApplyReceiverFields();
        _settings.Save();
        SiteStore.Save(Sites);
    }
}
