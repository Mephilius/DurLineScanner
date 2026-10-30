using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DurLineScanner.Data;

public enum ReceiverKind
{
    None,

    /// <summary>Enigma2-Receiver (Dreambox, VU+, Gigablue …) with OpenWebif.</summary>
    Enigma2,

    /// <summary>TVHeadend server.</summary>
    Tvheadend,
}

/// <summary>Settings in %AppData%\DurLineScanner\settings.json; the password is encrypted with DPAPI.</summary>
public sealed class AppSettings
{
    public static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DurLineScanner", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>UI language; takes effect after restart.</summary>
    public Localization.AppLanguage Language { get; set; }

    public ReceiverKind ReceiverKind { get; set; }

    /// <summary>Name or IP of the receiver, e.g. "192.168.1.20" or "tv.example.de", optionally with a path ("server/tvheadend").</summary>
    public string ReceiverAddress { get; set; } = "";

    /// <summary>Port; null = default (TVHeadend 9981, Enigma2 80, with HTTPS 443).</summary>
    public int? ReceiverPort { get; set; }

    /// <summary>Encrypted connection (HTTPS), e.g. behind a reverse proxy.</summary>
    public bool ReceiverHttps { get; set; }

    [JsonIgnore]
    public int ReceiverDefaultPort => ReceiverHttps ? 443 : ReceiverKind == ReceiverKind.Tvheadend ? 9981 : 80;

    /// <summary>
    /// Splits a fully entered address like "https://server:8443/tvh" into address, port and HTTPS.
    /// Also for older settings where everything was in the address field.
    /// </summary>
    public void SplitReceiverAddress()
    {
        var text = ReceiverAddress.Trim().TrimEnd('/');
        if (text.Length == 0) return;
        bool hasScheme = text.Contains("://");
        if (!Uri.TryCreate(hasScheme ? text : "http://" + text, UriKind.Absolute, out var uri)) return;
        if (hasScheme) ReceiverHttps = uri.Scheme == Uri.UriSchemeHttps;
        // Only take the port if it was given explicitly
        if (!uri.IsDefaultPort || text.Contains($":{uri.Port}")) ReceiverPort = uri.Port;
        ReceiverAddress = uri.Host + uri.AbsolutePath.TrimEnd('/');
    }

    public string ReceiverUser { get; set; } = "";

    public string ReceiverPasswordProtected { get; set; } = "";

    /// <summary>Update the channel list from the receiver on program start.</summary>
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
