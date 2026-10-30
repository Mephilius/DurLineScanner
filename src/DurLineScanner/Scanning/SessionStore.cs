using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>Stores measurement series as JSON under Documents\DurLineScanner\Messungen.</summary>
public sealed class SessionStore
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static readonly string DefaultFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DurLineScanner", "Messungen");

    public string Folder { get; }

    public SessionStore(string? folder = null)
    {
        Folder = folder ?? DefaultFolder;
        Directory.CreateDirectory(Folder);
    }

    public string Save(ScanSession session, string? existingPath = null)
    {
        var path = existingPath ?? Path.Combine(Folder, $"{session.Started:yyyyMMdd-HHmmss}_{Sanitize(session.SatelliteName)}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(session, JsonOptions));
        return path;
    }

    public IEnumerable<(string Path, ScanSession Session)> LoadAll()
    {
        foreach (var path in Directory.EnumerateFiles(Folder, "*.json").OrderByDescending(p => p))
        {
            ScanSession? session = null;
            try
            {
                session = JsonSerializer.Deserialize<ScanSession>(File.ReadAllText(path), JsonOptions);
            }
            catch (JsonException)
            {
                // Skip corrupt file
            }
            if (session is not null) yield return (path, session);
        }
    }

    /// <summary>CSV with semicolons and German decimal comma, so Excel opens it directly.</summary>
    public static void ExportCsv(ScanSession session, string path)
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        string F(double? v) => v?.ToString(culture) ?? "";

        var sb = new StringBuilder();
        sb.AppendLine($"# {session.SatelliteName};{session.Started:yyyy-MM-dd HH:mm:ss};{session.LocationLabel};{session.Note}");
        if (session.Site is { } site)
            sb.AppendLine(L.F("# Standort {0};{1};{2};{3};Schüssel {4} cm;LNB {5} dB",
                site.Name, site.Address, F(site.Latitude), F(site.Longitude), site.DishCm, F(site.LnbGainDb)));
        if (session.Weather is { } weather)
            sb.AppendLine(L.F("# Wetter {0}", WeatherInfo.Text(weather)));
        sb.AppendLine(L.T("Frequenz MHz;Pol;Symbolrate kS;Band;Status;Stärke %;Qualität %;C/N dB;Pegel dBµV;MER dB;BER;Lock-Zeit s;Zeit"));
        foreach (var r in session.Results)
        {
            sb.AppendLine(string.Join(";",
                r.FrequencyMHz, r.PolarizationShort, r.SymbolRateKs, r.Band, r.LockText,
                r.StrengthPercent, r.QualityPercent, F(r.CnDb), F(r.LevelDbuV), F(r.MerDb),
                r.Ber?.ToString("0.00E+0", culture) ?? "", F(r.LockTimeSeconds is { } t ? Math.Round(t, 1) : null),
                r.Timestamp.ToString("HH:mm:ss")));
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Length > 40 ? cleaned[..40] : cleaned;
    }
}
