using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DurLineScanner.Data;

/// <summary>
/// Lädt die Senderliste online vom eigenen Receiver:
/// Enigma2 über OpenWebif (/file?file=/etc/enigma2/lamedb5, sonst lamedb, notfalls per FTP),
/// TVHeadend über die JSON-API (Muxe und Services).
/// </summary>
public static partial class ReceiverChannelSource
{
    public static async Task<ChannelList> LoadAsync(AppSettings settings, CancellationToken ct = default) =>
        settings.ReceiverKind switch
        {
            ReceiverKind.Enigma2 => await LoadEnigma2Async(settings, ct),
            ReceiverKind.Tvheadend => await LoadTvheadendAsync(settings, ct),
            _ => throw new InvalidOperationException(L.T("Kein Receiver eingestellt.")),
        };

    public static Uri BaseUri(AppSettings settings)
    {
        // Schema und Port kommen aus eigenen Feldern; die Adresse ist nur Name/IP, optional mit Pfad
        var address = settings.ReceiverAddress.Trim().Trim('/');
        int slash = address.IndexOf('/');
        var uri = new UriBuilder(
            settings.ReceiverHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            slash < 0 ? address : address[..slash],
            settings.ReceiverPort ?? settings.ReceiverDefaultPort,
            slash < 0 ? "/" : address[slash..] + "/");
        return uri.Uri;
    }

    private static HttpClient CreateClient(AppSettings settings, Uri baseUri)
    {
        var handler = new HttpClientHandler();
        if (settings.ReceiverUser.Length > 0)
        {
            // CredentialCache beantwortet Basic- und Digest-Anmeldung (TVHeadend nutzt Digest)
            var credential = new NetworkCredential(settings.ReceiverUser, settings.ReceiverPassword);
            handler.Credentials = new CredentialCache
            {
                { baseUri, "Basic", credential },
                { baseUri, "Digest", credential },
            };
            handler.PreAuthenticate = true;
        }
        return new HttpClient(handler) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30) };
    }

    // ---------------------------------------------------------------- Enigma2

    private static async Task<ChannelList> LoadEnigma2Async(AppSettings settings, CancellationToken ct)
    {
        var baseUri = BaseUri(settings);
        string source = $"Enigma2 {baseUri.Host}";
        var errors = new List<string>();
        using (var http = CreateClient(settings, baseUri))
        {
            foreach (var file in new[] { "/etc/enigma2/lamedb5", "/etc/enigma2/lamedb" })
            {
                try
                {
                    using var response = await http.GetAsync("file?file=" + Uri.EscapeDataString(file), ct);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new InvalidOperationException(L.T("OpenWebif verlangt Benutzer und Passwort."));
                    var text = await response.Content.ReadAsStringAsync(ct);
                    if (response.IsSuccessStatusCode && text.StartsWith("eDVB services /"))
                        return ChannelList.ImportText(text, source);
                    errors.Add($"{file}: {(int)response.StatusCode}");
                }
                catch (HttpRequestException ex)
                {
                    errors.Add(ex.Message);
                    break; // Receiver nicht erreichbar – FTP versuchen
                }
            }
        }

        // Manche OpenWebif-Versionen sperren den Dateizugriff – dann direkt per FTP (Benutzer root)
        try
        {
            return ChannelList.ImportText(await FtpDownloadAsync(settings, baseUri.Host, "/etc/enigma2/lamedb", ct), source);
        }
        catch (Exception ex) when (ex is WebException or InvalidDataException)
        {
            errors.Add("FTP: " + ex.Message);
        }
        throw new InvalidOperationException(L.F("Senderliste nicht erhalten ({0}).", string.Join("; ", errors)));
    }

#pragma warning disable SYSLIB0014 // FtpWebRequest ist veraltet, aber die einfachste Möglichkeit ohne Zusatzpaket
    private static async Task<string> FtpDownloadAsync(AppSettings settings, string host, string file, CancellationToken ct)
    {
        var request = (FtpWebRequest)WebRequest.Create($"ftp://{host}{file}");
        request.Method = WebRequestMethods.Ftp.DownloadFile;
        request.Credentials = new NetworkCredential(
            settings.ReceiverUser.Length > 0 ? settings.ReceiverUser : "root", settings.ReceiverPassword);
        request.Timeout = 15000;
        using var registration = ct.Register(request.Abort);
        using var response = await request.GetResponseAsync();
        using var reader = new StreamReader(response.GetResponseStream());
        var text = await reader.ReadToEndAsync(ct);
        if (!text.StartsWith("eDVB services /")) throw new InvalidDataException(L.T("Datei ist keine lamedb."));
        return text;
    }
#pragma warning restore SYSLIB0014

    // ---------------------------------------------------------------- TVHeadend

    private static async Task<ChannelList> LoadTvheadendAsync(AppSettings settings, CancellationToken ct)
    {
        var baseUri = BaseUri(settings);
        using var http = CreateClient(settings, baseUri);
        using var muxDoc = await GetJsonAsync(http, "api/mpegts/mux/grid?limit=100000", ct);
        using var serviceDoc = await GetJsonAsync(http, "api/mpegts/service/grid?limit=100000", ct);

        var muxes = new Dictionary<string, (int Position, int FrequencyMHz, Polarization Pol, int SymbolRateKs)>();
        foreach (var mux in Entries(muxDoc))
        {
            if (Str(mux, "uuid") is not { } uuid) continue;
            var pol = Str(mux, "polarisation")?.ToUpperInvariant() switch
            {
                "H" => Polarization.Horizontal,
                "V" => Polarization.Vertical,
                _ => (Polarization?)null, // zirkular oder kein Sat-Mux
            };
            if (pol is null || Num(mux, "frequency") is not { } freq) continue;
            int? position = ParseOrbital(Str(mux, "orbital")) ?? ParseOrbital(Str(mux, "network"));
            if (position is null) continue;
            // TVHeadend speichert DVB-S-Frequenzen in kHz
            int mhz = (int)Math.Round(freq > 1e8 ? freq / 1e6 : freq > 1e5 ? freq / 1e3 : freq);
            int sr = (int)Math.Round((Num(mux, "symbolrate") ?? 27500000) / 1000);
            muxes[uuid] = (position.Value, mhz, pol.Value, sr);
        }

        var entries = new List<(int, int, Polarization, int, Channel)>();
        foreach (var service in Entries(serviceDoc))
        {
            if (Str(service, "multiplex_uuid") is not { } muxId || !muxes.TryGetValue(muxId, out var mux)) continue;
            var name = Str(service, "svcname");
            if (string.IsNullOrWhiteSpace(name)) continue;
            int type = (int)(Num(service, "dvb_servicetype") ?? 1);
            bool encrypted = service.TryGetProperty("encrypted", out var enc) && enc.ValueKind == JsonValueKind.True
                             || !string.IsNullOrEmpty(Str(service, "caid"));
            var kind = type switch
            {
                2 or 10 => ChannelKind.Radio,
                25 or 27 or 28 => ChannelKind.TvHd,
                31 or 32 => ChannelKind.TvUhd,
                1 or 4 or 5 or 17 or 22 or 24 => ChannelKind.Tv,
                _ => ChannelKind.Data,
            };
            entries.Add((mux.Position, mux.FrequencyMHz, mux.Pol, mux.SymbolRateKs,
                new Channel(name.Trim(), Str(service, "provider")?.Trim() ?? "", kind, encrypted)));
        }

        if (entries.Count == 0)
            throw new InvalidDataException(L.T("TVHeadend lieferte keine Sat-Sender (Orbitalposition der Netzwerke eingetragen?)."));
        return ChannelList.ImportText(ChannelList.ToLamedb5(entries), $"TVHeadend {baseUri.Host}");
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient http, string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new InvalidOperationException(L.T("TVHeadend verweigert den Zugriff – Benutzer/Passwort oder Rechte (Admin) prüfen."));
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    private static IEnumerable<JsonElement> Entries(JsonDocument doc) =>
        doc.RootElement.TryGetProperty("entries", out var e) && e.ValueKind == JsonValueKind.Array ? e.EnumerateArray() : [];

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        } : null;

    private static double? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    /// <summary>"19.2E", "19,2° Ost", "Astra 19.2E", "0.8W" → Zehntelgrad, West negativ.</summary>
    internal static int? ParseOrbital(string? text)
    {
        if (text is null) return null;
        var m = OrbitalRegex().Match(text);
        if (!m.Success) return null;
        double deg = double.Parse(m.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        bool west = m.Groups[2].Value.StartsWith('W') || m.Groups[2].Value.StartsWith('w');
        return (int)Math.Round((west ? -deg : deg) * 10);
    }

    [GeneratedRegex(@"(\d{1,3}(?:[.,]\d)?)\s*°?\s*(E|O|W|Ost|West)\b", RegexOptions.IgnoreCase)]
    private static partial Regex OrbitalRegex();
}
