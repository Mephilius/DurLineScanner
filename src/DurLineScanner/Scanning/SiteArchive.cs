using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>
/// Standort mit allen seinen Messreihen als ZIP – zum Weitergeben, Sichern oder Übernehmen auf einen anderen PC.
/// Aufbau: <c>standort.json</c>, <c>messungen/*.json</c> (vollständig, wieder importierbar) und
/// <c>csv/*.csv</c> (zum Öffnen in Excel).
/// </summary>
public static class SiteArchive
{
    private const string SiteEntry = "standort.json";
    private const string SessionFolder = "messungen/";
    private const string CsvFolder = "csv/";

    public sealed record ImportResult(Site? Site, bool SiteAdded, int Imported, int Skipped);

    /// <summary>Messreihen eines Standorts (über die in der Messreihe gespeicherte Standort-Kopie).</summary>
    public static IEnumerable<(string Path, ScanSession Session)> SessionsOf(Site site, IEnumerable<(string Path, ScanSession Session)> all) =>
        all.Where(s => s.Session.Site?.Id == site.Id);

    public static int Export(Site site, IEnumerable<(string Path, ScanSession Session)> sessions, string zipPath)
    {
        var list = sessions.ToList();
        var temp = zipPath + ".tmp";
        File.Delete(temp);
        using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
        {
            WriteText(zip, SiteEntry, JsonSerializer.Serialize(site, SessionStore.JsonOptions));
            foreach (var (path, session) in list)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                // Gespeicherte Datei unverändert übernehmen, damit nichts verloren geht
                zip.CreateEntryFromFile(path, SessionFolder + name + ".json");
                var csv = Path.GetTempFileName();
                try
                {
                    SessionStore.ExportCsv(session, csv);
                    zip.CreateEntryFromFile(csv, CsvFolder + name + ".csv");
                }
                finally
                {
                    File.Delete(csv);
                }
            }
        }
        File.Move(temp, zipPath, overwrite: true);
        return list.Count;
    }

    /// <summary>
    /// Übernimmt Messreihen in den Speicherordner. Vorhandene gleiche Dateien werden übersprungen,
    /// abweichende unter neuem Namen abgelegt. Der Standort wird zurückgegeben, aber nicht gespeichert.
    /// </summary>
    public static ImportResult Import(string zipPath, SessionStore store, IReadOnlyCollection<Site> knownSites)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        Site? site = null;
        if (zip.GetEntry(SiteEntry) is { } siteEntry)
            site = JsonSerializer.Deserialize<Site>(ReadText(siteEntry), SessionStore.JsonOptions);

        int imported = 0, skipped = 0;
        var sessionEntries = zip.Entries
            .Where(e => e.FullName.StartsWith(SessionFolder, StringComparison.OrdinalIgnoreCase)
                        && e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (site is null && sessionEntries.Count == 0)
            throw new InvalidDataException(L.T("Die ZIP-Datei ist kein Standort-Export des DUR-line Scanners."));

        foreach (var entry in sessionEntries)
        {
            var text = ReadText(entry);
            ScanSession? session;
            try
            {
                session = JsonSerializer.Deserialize<ScanSession>(text, SessionStore.JsonOptions);
            }
            catch (JsonException)
            {
                session = null;
            }
            if (session is null)
            {
                skipped++;
                continue;
            }
            site ??= session.Site;

            // Nur den Dateinamen verwenden – Pfade aus dem ZIP nie übernehmen
            var target = Path.Combine(store.Folder, Path.GetFileName(entry.Name));
            if (File.Exists(target))
            {
                if (File.ReadAllText(target) == text)
                {
                    skipped++;
                    continue;
                }
                target = Path.Combine(store.Folder, Path.GetFileNameWithoutExtension(entry.Name) + "_import.json");
                if (File.Exists(target))
                {
                    skipped++;
                    continue;
                }
            }
            File.WriteAllText(target, text);
            imported++;
        }

        bool added = site is not null && knownSites.All(s => s.Id != site.Id);
        return new ImportResult(site, added, imported, skipped);
    }

    private static void WriteText(ZipArchive zip, string name, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static string ReadText(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
