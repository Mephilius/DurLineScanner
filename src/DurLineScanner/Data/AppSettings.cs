using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DurLineScanner.Data;

public enum ReceiverKind
{
    None,

    /// <summary>Enigma2-Receiver (Dreambox, VU+, Gigablue …) mit OpenWebif.</summary>
    Enigma2,

    /// <summary>TVHeadend-Server.</summary>
    Tvheadend,
}

/// <summary>Einstellungen unter %AppData%\DurLineScanner\settings.json; das Passwort ist per DPAPI verschlüsselt.</summary>
public sealed class AppSettings
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Sprache der Oberfläche; wirkt nach dem Neustart.</summary>
    public Localization.AppLanguage Language { get; set; }

    public ReceiverKind ReceiverKind { get; set; }

    /// <summary>Name oder IP des Receivers, z.B. "192.168.1.20" oder "tv.example.de", optional mit Pfad ("server/tvheadend").</summary>
    public string ReceiverAddress { get; set; } = "";

    /// <summary>Port; null = Standard (TVHeadend 9981, Enigma2 80, mit HTTPS 443).</summary>
    public int? ReceiverPort { get; set; }

    /// <summary>Verschlüsselte Verbindung (HTTPS), z.B. hinter einem Reverse-Proxy.</summary>
    public bool ReceiverHttps { get; set; }

    [JsonIgnore]
    public int ReceiverDefaultPort => ReceiverHttps ? 443 : ReceiverKind == ReceiverKind.Tvheadend ? 9981 : 80;

    /// <summary>
    /// Zerlegt eine vollständig eingegebene Adresse wie "https://server:8443/tvh" in Adresse, Port und HTTPS.
    /// Auch für ältere Einstellungen, in denen alles im Adressfeld stand.
    /// </summary>
    public void SplitReceiverAddress()
    {
        var text = ReceiverAddress.Trim().TrimEnd('/');
        if (text.Length == 0) return;
        bool hasScheme = text.Contains("://");
        if (!Uri.TryCreate(hasScheme ? text : "http://" + text, UriKind.Absolute, out var uri)) return;
        if (hasScheme) ReceiverHttps = uri.Scheme == Uri.UriSchemeHttps;
        // Port nur übernehmen, wenn er ausdrücklich angegeben war
        if (!uri.IsDefaultPort || text.Contains($":{uri.Port}")) ReceiverPort = uri.Port;
        ReceiverAddress = uri.Host + uri.AbsolutePath.TrimEnd('/');
    }

    public string ReceiverUser { get; set; } = "";

    public string ReceiverPasswordProtected { get; set; } = "";

    /// <summary>Senderliste beim Programmstart vom Receiver aktualisieren.</summary>
    public bool LoadChannelsOnStart { get; set; }

    public Guid? LastSiteId { get; set; }

    public Scanning.Weather? LastWeather { get; set; }

    [JsonIgnore]
    public bool ReceiverConfigured => ReceiverKind != ReceiverKind.None && ReceiverAddress.Trim().Length > 0;

    [JsonIgnore]
    public string ReceiverPassword
    {
        get
        {
            if (ReceiverPasswordProtected.Length == 0) return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(ReceiverPasswordProtected), null, DataProtectionScope.CurrentUser));
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException)
            {
                return "";
            }
        }
        set => ReceiverPasswordProtected = value.Length == 0
            ? ""
            : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
    }

    public static AppSettings Load()
    {
        try
        {
            var settings = File.Exists(Path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path), JsonOptions) ?? new() : new();
            settings.SplitReceiverAddress();
            return settings;
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
