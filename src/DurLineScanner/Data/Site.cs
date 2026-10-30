using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DurLineScanner.Data;

/// <summary>Reception site with dish and LNB. Stored as a copy in the measurement series when scanning.</summary>
public sealed class Site
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";

    /// <summary>Geographic latitude, north positive.</summary>
    public double? Latitude { get; set; }

    /// <summary>Geographic longitude, east positive.</summary>
    public double? Longitude { get; set; }

    /// <summary>Dish diameter in cm.</summary>
    public int DishCm { get; set; } = 80;

    /// <summary>LNB gain (conversion gain) in dB, typically 50–65 dB.</summary>
    public double LnbGainDb { get; set; } = 55;

    /// <summary>LNB type – decides how a fault can be narrowed down (swapping cables, which plane each output carries).</summary>
    public LnbType LnbType { get; set; }

    /// <summary>LNB manufacturer and model from <see cref="LnbCatalog"/>; empty = unknown.</summary>
    public string LnbManufacturer { get; set; } = "";

    public string LnbModel { get; set; } = "";

    /// <summary>Multiswitch between LNB and outlets – decides which level and slope advice fits.</summary>
    public MultiswitchKind Multiswitch { get; set; }

    public LevelControl MultiswitchLevel { get; set; }

    /// <summary>Slope (tilt equalization) adjustable at the multiswitch; null = unknown.</summary>
    public bool? MultiswitchSlope { get; set; }

    /// <summary>Multiswitch can drive a quad LNB (switches each LNB output to the plane of its input); null = unknown.</summary>
    public bool? MultiswitchQuad { get; set; }

    /// <summary>Free text for documentation, e.g. "Spaun SMS 5803 NF".</summary>
    public string MultiswitchModel { get; set; } = "";

    [JsonIgnore]
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    public Site Copy() => (Site)MemberwiseClone();

    public override string ToString() => Name.Length > 0 ? Name : L.T("(ohne Namen)");
}

public enum LnbType
{
    Unknown,

    /// <summary>Universal outputs: each output delivers the plane the receiver selects (13/18 V, 22 kHz).</summary>
    Single,
    Twin,
    Quad,
    Octo,

    /// <summary>Four fixed outputs, one plane each – for multiswitches.</summary>
    Quattro,

    /// <summary>Unicable/JESS: all channels over one cable.</summary>
    Unicable,
}

public static class LnbTypes
{
    public static readonly LnbType[] All = Enum.GetValues<LnbType>();

    public static string Text(LnbType type) => type switch
    {
        LnbType.Single => "Single",
        LnbType.Twin => "Twin",
        LnbType.Quad => "Quad",
        LnbType.Octo => "Octo",
        LnbType.Quattro => L.T("Quattro (für Multischalter)"),
        LnbType.Unicable => "Unicable / JESS",
        _ => L.T("unbekannt"),
    };

    /// <summary>Universal outputs whose plane is switched by the receiver or a quad-capable multiswitch.</summary>
    public static bool IsUniversal(LnbType type) => type is LnbType.Single or LnbType.Twin or LnbType.Quad or LnbType.Octo;
}

public enum MultiswitchKind
{
    Unknown,

    /// <summary>LNB connected directly to the receivers.</summary>
    None,
    Passive,
    Active,
}

/// <summary>Which output levels of the multiswitch can be adjusted.</summary>
public enum LevelControl
{
    Unknown,
    None,
    Common,
    PerBand,
    PerPlane,
}

public static class MultiswitchTexts
{
    public static string Text(MultiswitchKind kind) => kind switch
    {
        MultiswitchKind.None => L.T("keiner – LNB direkt am Receiver"),
        MultiswitchKind.Passive => L.T("passiv"),
        MultiswitchKind.Active => L.T("aktiv (mit Verstärker)"),
        _ => L.T("unbekannt"),
    };

    public static string Text(LevelControl control) => control switch
    {
        LevelControl.None => L.T("nicht regelbar"),
        LevelControl.Common => L.T("gemeinsam für alle Ebenen"),
        LevelControl.PerBand => L.T("je Band (Low/High)"),
        LevelControl.PerPlane => L.T("je Ebene"),
        _ => L.T("unbekannt"),
    };

    public static string Text(bool? value) => value switch
    {
        true => L.T("ja"),
        false => L.T("nein"),
        _ => L.T("unbekannt"),
    };

    public static bool IsAdjustable(LevelControl control) => control is LevelControl.Common or LevelControl.PerBand or LevelControl.PerPlane;
}

/// <summary>Sites stored in %AppData%\DurLineScanner\sites.json.</summary>
public static class SiteStore
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "sites.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<Site> Load()
    {
        try
        {
            return File.Exists(Path) ? JsonSerializer.Deserialize<List<Site>>(File.ReadAllText(Path)) ?? [] : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static void Save(IEnumerable<Site> sites)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(sites.ToList(), JsonOptions));
    }
}

/// <summary>Address → coordinates via OpenStreetMap Nominatim (on request only, one query per address).</summary>
public static class Geocoder
{
    public sealed record Result(double Latitude, double Longitude, string DisplayName);

    public static async Task<Result?> LookupAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        // Nominatim's usage policy requires a unique identification of the application
        http.DefaultRequestHeaders.UserAgent.ParseAdd("DurLineScanner/1.0 (Windows desktop app)");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd(L.IsEnglish ? "en" : "de");
        var url = "https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=" + Uri.EscapeDataString(address.Trim());
        using var doc = JsonDocument.Parse(await http.GetStringAsync(url, ct));
        if (doc.RootElement.GetArrayLength() == 0) return null;
        var hit = doc.RootElement[0];
        return new Result(
            double.Parse(hit.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture),
            double.Parse(hit.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture),
            hit.TryGetProperty("display_name", out var name) ? name.GetString() ?? "" : "");
    }
}

/// <summary>Room suggestions for sat wall outlets.</summary>
public static class Rooms
{
    /// <summary>Room names in the UI language.</summary>
    public static readonly string[] All = German().Select(L.T).ToArray();

    private static string[] German() =>
    [
        "Wohnzimmer", "Esszimmer", "Küche", "Wohnküche", "Schlafzimmer", "Elternschlafzimmer",
        "Kinderzimmer", "Kinderzimmer 2", "Jugendzimmer", "Gästezimmer", "Arbeitszimmer", "Büro",
        "Bibliothek", "Heimkino", "Musikzimmer", "Bad", "Gäste-WC", "Ankleide", "Flur", "Diele",
        "Treppenhaus", "Galerie", "Wintergarten", "Balkon", "Terrasse", "Dachgeschoss", "Dachboden",
        "Spitzboden", "Keller", "Hobbyraum", "Partyraum", "Fitnessraum", "Sauna", "Werkstatt",
        "Hauswirtschaftsraum", "Abstellraum", "Technikraum", "Hausanschlussraum", "Garage",
        "Carport", "Gartenhaus", "Poolhaus", "Einliegerwohnung", "Ferienwohnung", "Praxis", "Laden",
        "Wohnmobil", "Wohnwagen",
    ];
}
