using DurLineScanner.Data;
using DurLineScanner.Scanning;

namespace DurLineScanner.Tests;

public class SiteArchiveTests
{
    [Fact]
    public void ExportAndImport_RoundTripWithoutDuplicates()
    {
        var root = Path.Combine(Path.GetTempPath(), "DurLineArchiveTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var site = new Site { Name = "Ferienhaus", DishCm = 60 };
            var source = new SessionStore(Path.Combine(root, "a"));
            var mine = new ScanSession { SatelliteName = "Astra", Started = new DateTime(2026, 10, 27, 12, 0, 0), Site = site.Copy(), Weather = Weather.Rain };
            mine.Results.Add(new ScanResult { FrequencyMHz = 11494, Locked = true, TuneAccepted = true, MerDb = 14 });
            var other = new ScanSession { SatelliteName = "Astra", Started = new DateTime(2026, 10, 27, 13, 0, 0) };
            source.Save(mine);
            source.Save(other);

            var zip = Path.Combine(root, "export.zip");
            var sessions = SiteArchive.SessionsOf(site, source.LoadAll()).ToList();
            Assert.Equal(1, SiteArchive.Export(site, sessions, zip));

            var target = new SessionStore(Path.Combine(root, "b"));
            var result = SiteArchive.Import(zip, target, []);
            Assert.True(result.SiteAdded);
            Assert.Equal("Ferienhaus", result.Site!.Name);
            Assert.Equal(1, result.Imported);
            var loaded = Assert.Single(target.LoadAll()).Session;
            Assert.Equal(Weather.Rain, loaded.Weather);
            Assert.Equal(14, loaded.Results[0].MerDb);

            // Zweiter Import: nichts doppelt, Standort schon bekannt
            var again = SiteArchive.Import(zip, target, [result.Site]);
            Assert.False(again.SiteAdded);
            Assert.Equal(0, again.Imported);
            Assert.Equal(1, again.Skipped);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
