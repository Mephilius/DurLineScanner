using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DurLineScanner.Data;

/// <summary>
/// Transponderliste im Enigma2-Format (satellites.xml), z.B. von OpenPLi (aus LyngSat erzeugt)
/// oder vom eigenen Receiver (/etc/tuxbox/satellites.xml).
/// </summary>
public sealed partial class SatellitesXml
{
    public const string OpenPliUrl = "https://raw.githubusercontent.com/OpenPLi/tuxbox-xml/master/xml/satellites.xml";

    public static readonly string UserCopyPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "satellites.xml");

    private readonly Dictionary<int, List<Transponder>> _byPosition;

    private SatellitesXml(Dictionary<int, List<Transponder>> byPosition, List<Satellite> satellites, string description)
    {
        _byPosition = byPosition;
        Satellites = satellites;
        Description = description;
    }

    /// <summary>
    /// Empfangbare Satelliten, je Position und Band ein Eintrag, nach Orbitposition sortiert.
    /// LNB-Frequenzen sind die üblichen Standardwerte: Universal-LNB 9750/10600 MHz im Ku-Band, 5150 MHz im C-Band.
    /// </summary>
    public IReadOnlyList<Satellite> Satellites { get; }

    /// <summary>Herkunft und Stand für die Anzeige, z.B. "satellites.xml vom 27.10.2026".</summary>
    public string Description { get; }

    public int SatelliteCount => _byPosition.Count;

    /// <summary>
    /// Heruntergeladene oder importierte Liste. Es wird keine Liste mitgeliefert – fehlt sie, lädt die App
    /// sie beim Start von OpenPLi. Eine beschädigte Kopie wird ignoriert.
    /// </summary>
    public static SatellitesXml? LoadUserCopy()
    {
        if (!File.Exists(UserCopyPath)) return null;
        try
        {
            return Load(UserCopyPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException)
        {
            return null;
        }
    }

    public static SatellitesXml Load(string path)
    {
        var text = File.ReadAllText(path);
        var doc = XDocument.Parse(text);
        var byPosition = new Dictionary<int, List<Transponder>>();
        var names = new Dictionary<(int Position, bool CBand), string>();

        foreach (var sat in doc.Root?.Elements("sat") ?? [])
        {
            if (!int.TryParse((string?)sat.Attribute("position"), out int position)) continue;
            var list = byPosition.TryGetValue(position, out var existing) ? existing : byPosition[position] = [];
            int before = list.Count;
            foreach (var tp in sat.Elements("transponder"))
            {
                // Zirkulare Polarisation (2/3) ist mit einem linearen Ku-LNB nicht empfangbar
                int pol = (int?)tp.Attribute("polarization") ?? -1;
                if (pol is not (0 or 1)) continue;
                long freq = (long?)tp.Attribute("frequency") ?? 0;
                long symbolRate = (long?)tp.Attribute("symbol_rate") ?? 0;
                if (freq <= 0 || symbolRate <= 0) continue;

                list.Add(new Transponder(
                    (int)Math.Round(freq / 1000.0),
                    pol == 0 ? Polarization.Horizontal : Polarization.Vertical,
                    (int)Math.Round(symbolRate / 1000.0),
                    (int?)tp.Attribute("system") == 1 ? "DVB-S2" : "DVB-S",
                    ModulationName((int?)tp.Attribute("modulation")),
                    FecName((int?)tp.Attribute("fec_inner"))));
            }

            // Erster Name je Position und Band gewinnt, z.B. "19.2E Astra 1N/1P"
            var name = (string?)sat.Attribute("name") ?? $"{position / 10.0:0.0}";
            foreach (bool cBand in list.Skip(before).Select(t => IsCBand(t.FrequencyMHz)).Distinct())
                names.TryAdd((position, cBand), name);
        }

        if (byPosition.Count == 0)
            throw new InvalidDataException(L.T("Die Datei enthält keine Satelliten (kein satellites.xml-Format?)."));

        // OpenPLi schreibt das Erstellungsdatum in einen Kommentar am Anfang
        var created = CreatedRegex().Match(text);
        var stand = created.Success && DateTime.TryParse(created.Groups[1].Value, out var date)
            ? date
            : File.GetLastWriteTime(path);
        var satellites = names
            .Select(n => new Satellite(
                n.Key.Position * 10 + (n.Key.CBand ? 1 : 0),
                n.Value,
                n.Key.CBand ? 5150 : 9750,
                n.Key.CBand ? 5150 : 10600,
                n.Key.Position / 10.0))
            .OrderBy(s => s.Longitude)
            .ToList();
        return new SatellitesXml(byPosition, satellites, L.F("satellites.xml vom {0:dd.MM.yyyy}", stand));
    }

    /// <summary>C-Band 3,4–4,2 GHz; alles andere wird als Ku-Band behandelt.</summary>
    public static bool IsCBand(int frequencyMHz) => frequencyMHz < 5000;

    /// <summary>Lädt die aktuelle OpenPLi-Liste herunter, prüft sie und speichert sie als Benutzerkopie.</summary>
    public static async Task<SatellitesXml> DownloadAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var bytes = await http.GetByteArrayAsync(OpenPliUrl, ct);
        var temp = UserCopyPath + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(UserCopyPath)!);
        await File.WriteAllBytesAsync(temp, bytes, ct);
        var parsed = Load(temp);   // erst prüfen, dann die alte Kopie ersetzen
        File.Move(temp, UserCopyPath, overwrite: true);
        return parsed;
    }

    /// <summary>Übernimmt eine satellites.xml (z.B. vom Receiver) als Benutzerkopie.</summary>
    public static SatellitesXml Import(string path)
    {
        var parsed = Load(path);
        Directory.CreateDirectory(Path.GetDirectoryName(UserCopyPath)!);
        File.Copy(path, UserCopyPath, overwrite: true);
        return parsed;
    }

    /// <summary>Transponder einer Orbitposition (Zehntelgrad, West negativ), ±0,2° Toleranz.</summary>
    public IReadOnlyList<Transponder>? GetTransponders(int positionTenths)
    {
        var matches = _byPosition.Where(kv => Math.Abs(kv.Key - positionTenths) <= 2).SelectMany(kv => kv.Value).ToList();
        return matches.Count > 0 ? matches : null;
    }

    private static string? ModulationName(int? modulation) => modulation switch
    {
        1 => "QPSK",
        2 => "8PSK",
        3 => "QAM16",
        4 => "16APSK",
        5 => "32APSK",
        _ => null,
    };

    // Enigma2-Kodierung von fec_inner
    private static string? FecName(int? fec) => fec switch
    {
        1 => "1/2",
        2 => "2/3",
        3 => "3/4",
        4 => "5/6",
        5 => "7/8",
        6 => "8/9",
        7 => "3/5",
        8 => "4/5",
        9 => "9/10",
        _ => null,
    };

    [GeneratedRegex(@"File created on (\S+)")]
    private static partial Regex CreatedRegex();
}
