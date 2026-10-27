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

    /// <summary>Adresse wie "192.168.1.20", "vuplus.local" oder "http://server:9981".</summary>
    public string ReceiverAddress { get; set; } = "";

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
            return File.Exists(Path) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Path), JsonOptions) ?? new() : new();
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
