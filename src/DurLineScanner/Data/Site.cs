using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DurLineScanner.Data;

/// <summary>Empfangsstandort mit Schüssel und LNB. Wird beim Scan als Kopie in der Messreihe abgelegt.</summary>
public sealed class Site
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";

    /// <summary>Geografische Breite, Nord positiv.</summary>
    public double? Latitude { get; set; }

    /// <summary>Geografische Länge, Ost positiv.</summary>
    public double? Longitude { get; set; }

    /// <summary>Durchmesser der Schüssel in cm.</summary>
    public int DishCm { get; set; } = 80;

    /// <summary>Verstärkung (Konversionsgewinn) des LNB in dB, typisch 50–65 dB.</summary>
    public double LnbGainDb { get; set; } = 55;

    [JsonIgnore]
    public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;

    public Site Copy() => (Site)MemberwiseClone();

    public override string ToString() => Name.Length > 0 ? Name : L.T("(ohne Namen)");
}

/// <summary>Standorte unter %AppData%\DurLineScanner\sites.json.</summary>
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

/// <summary>Adresse → Koordinaten über OpenStreetMap Nominatim (nur auf Anforderung, eine Abfrage je Adresse).</summary>
public static class Geocoder
{
    public sealed record Result(double Latitude, double Longitude, string DisplayName);

    public static async Task<Result?> LookupAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        // Die Nutzungsbedingungen von Nominatim verlangen eine eindeutige Kennung der Anwendung
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

/// <summary>Raumvorschläge für Sat-Dosen.</summary>
public static class Rooms
{
    /// <summary>Raumnamen in der Oberflächensprache.</summary>
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
