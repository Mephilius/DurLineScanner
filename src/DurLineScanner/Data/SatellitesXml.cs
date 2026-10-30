using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DurLineScanner.Data;

/// <summary>
/// Transponder list in Enigma2 format (satellites.xml), e.g. from OpenPLi (generated from LyngSat)
/// or from your own receiver (/etc/tuxbox/satellites.xml).
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
    /// Receivable satellites, one entry per position and band, sorted by orbital position.
    /// LNB frequencies are the usual defaults: universal LNB 9750/10600 MHz in Ku band, 5150 MHz in C band.
    /// </summary>
    public IReadOnlyList<Satellite> Satellites { get; }

    /// <summary>Source and date for display, e.g. "satellites.xml vom 27.10.2026".</summary>
    public string Description { get; }

    public int SatelliteCount => _byPosition.Count;

    /// <summary>
    /// Downloaded or imported list. No list is bundled – if it is missing, the app downloads it
    /// from OpenPLi on startup. A corrupt copy is ignored.
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
                // Circular polarization (2/3) cannot be received with a linear Ku LNB
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

            // First name per position and band wins, e.g. "19.2E Astra 1N/1P"
            var name = (string?)sat.Attribute("name") ?? $"{position / 10.0:0.0}";
            foreach (bool cBand in list.Skip(before).Select(t => IsCBand(t.FrequencyMHz)).Distinct())
                names.TryAdd((position, cBand), name);
        }

        if (byPosition.Count == 0)
            throw new InvalidDataException(L.T("Die Datei enthält keine Satelliten (kein satellites.xml-Format?)."));

        // OpenPLi writes the creation date into a comment at the top
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

    /// <summary>C band 3.4–4.2 GHz; everything else is treated as Ku band.</summary>
    public static bool IsCBand(int frequencyMHz) => frequencyMHz < 5000;

    /// <summary>Downloads the current OpenPLi list, validates it and saves it as the user copy.</summary>
    public static async Task<SatellitesXml> DownloadAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var bytes = await http.GetByteArrayAsync(OpenPliUrl, ct);
        var temp = UserCopyPath + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(UserCopyPath)!);
        await File.WriteAllBytesAsync(temp, bytes, ct);
        var parsed = Load(temp);   // validate first, then replace the old copy
        File.Move(temp, UserCopyPath, overwrite: true);
        return parsed;
    }

    /// <summary>Adopts a satellites.xml (e.g. from the receiver) as the user copy.</summary>
    public static SatellitesXml Import(string path)
    {
        var parsed = Load(path);
        Directory.CreateDirectory(Path.GetDirectoryName(UserCopyPath)!);
        File.Copy(path, UserCopyPath, overwrite: true);
        return parsed;
    }

    /// <summary>Transponders of an orbital position (tenths of a degree, west negative), ±0.2° tolerance.</summary>
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

    // Enigma2 encoding of fec_inner
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
