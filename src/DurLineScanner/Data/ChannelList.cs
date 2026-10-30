using System.Globalization;
using System.IO.Compression;

namespace DurLineScanner.Data;

public enum ChannelKind
{
    Tv,
    TvHd,
    TvUhd,
    Radio,
    Data,
}

public sealed record Channel(string Name, string Provider, ChannelKind Kind, bool Encrypted)
{
    public string KindText => Kind switch
    {
        ChannelKind.TvHd => "HD",
        ChannelKind.TvUhd => "UHD",
        ChannelKind.Radio => "Radio",
        ChannelKind.Data => L.T("Daten"),
        _ => "SD",
    };
}

/// <summary>Channel with its transponder – for channel selection in the live measurement.</summary>
public sealed record ChannelOnTransponder(Channel Channel, int FrequencyMHz, Polarization Pol)
{
    public override string ToString() =>
        $"{Channel.Name}   ·   {Channel.KindText}{(Channel.Encrypted ? " · " + L.T("verschlüsselt") : "")}   ·   {FrequencyMHz} {(Pol == Polarization.Horizontal ? "H" : "V")}";
}

/// <summary>
/// Channel list from an Enigma2 <c>lamedb</c> (format 4 or 5), e.g. from your own receiver
/// (/etc/enigma2/lamedb) or from a channel list package (ZIP).
/// </summary>
public sealed class ChannelList
{
    /// <summary>Saved channel list; can be redirected to a temporary file in tests.</summary>
    public static string UserCopyPath { get; internal set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "lamedb");

    /// <summary>Currently loaded list (for tooltips in the table).</summary>
    public static ChannelList? Current { get; set; } = TryLoadUserCopy();

    private readonly List<(int Position, int FrequencyMHz, Polarization Pol, List<Channel> Channels)> _transponders;

    private ChannelList(List<(int, int, Polarization, List<Channel>)> transponders, string description)
    {
        _transponders = transponders;
        Description = description;
    }

    public string Description { get; }

    public int ChannelCount => _transponders.Sum(t => t.Channels.Count);

    /// <summary>Channels of a transponder; position in tenths of a degree (west negative), frequency ±2 MHz.</summary>
    public IReadOnlyList<Channel> Get(int positionTenths, int frequencyMHz, Polarization pol) =>
        _transponders
            .Where(t => t.Pol == pol && Math.Abs(t.FrequencyMHz - frequencyMHz) <= 2 && Math.Abs(t.Position - positionTenths) <= 2)
            .SelectMany(t => t.Channels)
            .Where(c => c.Name.Any(char.IsLetterOrDigit))
            .DistinctBy(c => (c.Name, c.Kind))
            .ToList();

    /// <summary>All channels of an orbital position with their transponder, alphabetically.</summary>
    public IReadOnlyList<ChannelOnTransponder> AllAt(int positionTenths) =>
        _transponders
            .Where(t => Math.Abs(t.Position - positionTenths) <= 2)
            .SelectMany(t => t.Channels.Select(c => new ChannelOnTransponder(c, t.FrequencyMHz, t.Pol)))
            .Where(c => c.Channel.Kind != ChannelKind.Data && c.Channel.Name.Any(char.IsLetterOrDigit))
            .DistinctBy(c => (c.Channel.Name, c.FrequencyMHz, c.Pol))
            .OrderBy(c => c.Channel.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public static ChannelList? TryLoadUserCopy()
    {
        try
        {
            return File.Exists(UserCopyPath) ? Parse(File.ReadAllText(UserCopyPath), File.GetLastWriteTime(UserCopyPath)) : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Adopts a lamedb or a ZIP containing a lamedb as the user copy.</summary>
    public static ChannelList Import(string path) => ImportText(ReadLamedb(path), null);

    /// <summary>Validates a lamedb and saves it as the user copy, optionally with its source for display.</summary>
    public static ChannelList ImportText(string text, string? source)
    {
        if (source is not null)
        {
            // Source as a comment after the header line; both formats ignore such lines
            int lineEnd = text.IndexOf('\n');
            text = lineEnd < 0 ? text : text[..(lineEnd + 1)] + $"{SourcePrefix}{source}\n" + text[(lineEnd + 1)..];
        }
        var list = Parse(text, DateTime.Now);
        Directory.CreateDirectory(Path.GetDirectoryName(UserCopyPath)!);
        File.WriteAllText(UserCopyPath, text);
        return list;
    }

    private const string SourcePrefix = "# Quelle: ";

    /// <summary>List from individual records (e.g. TVHeadend) as lamedb5 text, so it is saved like an imported list.</summary>
    public static string ToLamedb5(IEnumerable<(int PositionTenths, int FrequencyMHz, Polarization Pol, int SymbolRateKs, Channel Channel)> entries)
    {
        var sb = new System.Text.StringBuilder("eDVB services /5/\n");
        int tsid = 0, sid = 0;
        foreach (var tp in entries.GroupBy(e => (e.PositionTenths, e.FrequencyMHz, e.Pol)))
        {
            // Synthetic keys: namespace from the position, sequential transport ID
            int pos = tp.Key.PositionTenths < 0 ? 3600 + tp.Key.PositionTenths : tp.Key.PositionTenths;
            string key = $"{pos << 16:x8}:{++tsid:x4}:0001";
            sb.Append($"t:{key},s:{tp.Key.FrequencyMHz * 1000}:{tp.First().SymbolRateKs * 1000}:{(tp.Key.Pol == Polarization.Horizontal ? 0 : 1)}:0:{pos}:2:0\n");
            foreach (var e in tp)
            {
                int type = e.Channel.Kind switch
                {
                    ChannelKind.TvHd => 25,
                    ChannelKind.TvUhd => 31,
                    ChannelKind.Radio => 2,
                    ChannelKind.Data => 12,
                    _ => 1,
                };
                var name = e.Channel.Name.Replace("\"", "'");
                var provider = e.Channel.Provider.Replace(",", " ");
                sb.Append($"s:{++sid:x4}:{key}:{type}:0,\"{name}\",p:{provider}{(e.Channel.Encrypted ? ",C:0000" : "")}\n");
            }
        }
        return sb.ToString();
    }

    private static string ReadLamedb(string path)
    {
        if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return File.ReadAllText(path);

        using var zip = ZipFile.OpenRead(path);
        // Prefer lamedb5, else lamedb – in any subfolder
        var entry = zip.Entries.FirstOrDefault(e => e.Name.Equals("lamedb5", StringComparison.OrdinalIgnoreCase))
                    ?? zip.Entries.FirstOrDefault(e => e.Name.Equals("lamedb", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException(L.T("Im ZIP wurde keine lamedb gefunden."));
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    public static ChannelList Parse(string text, DateTime stand)
    {
        var lines = text.Replace("\r", "").Split('\n');
        if (lines.Length == 0 || !lines[0].StartsWith("eDVB services /"))
            throw new InvalidDataException(L.T("Keine Enigma2-lamedb (erste Zeile muss „eDVB services /4/“ oder „/5/“ sein)."));

        // Transponder key "namespace:tsid:onid" → (position, frequency, polarization)
        var transponders = new Dictionary<string, (int Position, int FrequencyMHz, Polarization Pol)>(StringComparer.OrdinalIgnoreCase);
        var services = new List<(string Key, Channel Channel)>();

        if (lines[0].Contains("/5/"))
            ParseV5(lines, transponders, services);
        else
            ParseV4(lines, transponders, services);

        var byTransponder = services
            .Where(s => transponders.ContainsKey(s.Key))
            .GroupBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var tp = transponders[g.Key];
                var channels = g.Select(s => s.Channel).OrderBy(c => c.Kind is ChannelKind.Radio or ChannelKind.Data).ThenBy(c => c.Name).ToList();
                return (tp.Position, tp.FrequencyMHz, tp.Pol, channels);
            })
            .ToList();

        if (byTransponder.Count == 0)
            throw new InvalidDataException(L.T("Die lamedb enthält keine Sat-Sender."));
        int count = byTransponder.Sum(t => t.channels.Count);
        var source = lines.FirstOrDefault(l => l.StartsWith(SourcePrefix))?[SourcePrefix.Length..].Trim() ?? "lamedb";
        return new ChannelList(byTransponder, L.F("{0} vom {1:dd.MM.yyyy} ({2} Sender)", source, stand, count));
    }

    private static void ParseV4(string[] lines, Dictionary<string, (int, int, Polarization)> transponders, List<(string, Channel)> services)
    {
        int i = Array.IndexOf(lines, "transponders") + 1;
        while (i > 0 && i < lines.Length && lines[i] != "end")
        {
            string key = lines[i].Trim();
            string data = i + 1 < lines.Length ? lines[i + 1].Trim() : "";
            if (ParseSatTransponder(data) is { } tp) transponders[NormalizeKey(key)] = tp;
            i += 3; // key, data, "/"
        }

        i = Array.IndexOf(lines, "services") + 1;
        while (i > 0 && i + 2 < lines.Length && lines[i] != "end")
        {
            if (ParseServiceHeader(lines[i]) is { } header)
                services.Add((header.Key, MakeChannel(lines[i + 1], lines[i + 2], header.Type)));
            i += 3; // header, name, provider
        }
    }

    private static void ParseV5(string[] lines, Dictionary<string, (int, int, Polarization)> transponders, List<(string, Channel)> services)
    {
        foreach (var line in lines)
        {
            if (line.StartsWith("t:"))
            {
                // t:00c00000:0437:0001,s:11797500:27500000:0:2:192:2:0
                int comma = line.IndexOf(',');
                if (comma < 0) continue;
                if (ParseSatTransponder(line[(comma + 1)..]) is { } tp)
                    transponders[NormalizeKey(line[2..comma])] = tp;
            }
            else if (line.StartsWith("s:"))
            {
                // s:0083:00c00000:0437:0001:1:0,"Das Erste HD",p:ARD,C:...
                int comma = line.IndexOf(',');
                if (comma < 0 || ParseServiceHeader(line[2..comma]) is not { } header) continue;
                string rest = line[(comma + 1)..];
                string name = rest, provider = "";
                if (rest.StartsWith('"') && rest.IndexOf('"', 1) is var end and > 0)
                {
                    name = rest[1..end];
                    provider = rest.Length > end + 1 ? rest[(end + 2)..] : "";
                }
                services.Add((header.Key, MakeChannel(name, provider, header.Type)));
            }
        }
    }

    /// <summary>"s freq:sr:pol:fec:pos:…" (v4, with tab/"s ") or "s:freq:…" (v5).</summary>
    private static (int Position, int FrequencyMHz, Polarization Pol)? ParseSatTransponder(string data)
    {
        if (data.Length < 3 || data[0] != 's' || (data[1] != ' ' && data[1] != ':')) return null;
        var f = data[2..].Split(':');
        if (f.Length < 5 || !int.TryParse(f[0], out int freq) || !int.TryParse(f[2], out int pol) || !int.TryParse(f[4], out int pos))
            return null;
        if (pol is not (0 or 1)) return null; // circular
        // lamedb stores west as 3600 - x
        if (pos > 1800) pos -= 3600;
        return (pos, (int)Math.Round(freq / 1000.0), pol == 0 ? Polarization.Horizontal : Polarization.Vertical);
    }

    private static (string Key, int Type)? ParseServiceHeader(string header)
    {
        var f = header.Trim().Split(':');
        if (f.Length < 5 || !int.TryParse(f[4], out int type)) return null;
        return (NormalizeKey($"{f[1]}:{f[2]}:{f[3]}"), type);
    }

    private static string NormalizeKey(string key)
    {
        // Compare keys as numbers; leading zeros differ depending on the generator
        var parts = key.Split(':');
        return string.Join(":", parts.Select(p => long.TryParse(p, NumberStyles.HexNumber, null, out var v) ? v.ToString("x") : p));
    }

    private static Channel MakeChannel(string name, string providerLine, int type)
    {
        var fields = providerLine.Split(',');
        string provider = fields.FirstOrDefault(f => f.StartsWith("p:"))?[2..] ?? "";
        bool encrypted = fields.Any(f => f.StartsWith("C:"));
        var kind = type switch
        {
            2 or 10 => ChannelKind.Radio,
            25 or 27 or 28 => ChannelKind.TvHd,
            31 or 32 => ChannelKind.TvUhd,
            1 or 4 or 5 or 17 or 22 or 24 => ChannelKind.Tv,
            _ => ChannelKind.Data,
        };
        return new Channel(name.Trim(), provider.Trim(), kind, encrypted);
    }
}
