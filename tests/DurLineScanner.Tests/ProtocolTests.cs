using DurLineScanner.Data;
using DurLineScanner.Device;
using DurLineScanner.Scanning;

namespace DurLineScanner.Tests;

/// <summary>Test data are real responses from the SF 4000 BT (Astra 19.2° E, 2026-10-27).</summary>
public class ProtocolTests
{
    [Theory]
    [InlineData(11494, Polarization.Horizontal, 22000, "A5F286D055F0")] // verified with lock on the device
    [InlineData(12188, Polarization.Horizontal, 27500, "A5F2C6346B6C")] // verified with lock on the device
    [InlineData(10744, Polarization.Horizontal, 22000, "A5F283E255F0")]
    [InlineData(11836, Polarization.Vertical, 27500, "A5F244D46B6C")]
    public void EncodeTune_UniversalLnb(int freq, Polarization pol, int sr, string expected)
    {
        var bytes = Sf4000Protocol.EncodeTune(new Transponder(freq, pol, sr), 9750, 10600);
        Assert.Equal(expected, Convert.ToHexString(bytes!));
    }

    [Fact]
    public void EncodeTune_OutOfLnbRange_ReturnsNull()
    {
        Assert.Null(Sf4000Protocol.EncodeTune(new Transponder(13000, Polarization.Vertical, 27500), 9750, 10600));
        Assert.Null(Sf4000Protocol.EncodeTune(new Transponder(11494, Polarization.Vertical, 500), 9750, 10600));
    }

    [Fact]
    public void ParseStatus_LockedDetail()
    {
        var r = Sf4000Protocol.ParseStatus(Convert.FromHexString("A5F302376200BE01A5000000C500058F7D"))!;
        Assert.True(r.IsLocked);
        Assert.Equal(55, r.StrengthRaw);
        Assert.Equal(66, r.StrengthPercent);
        Assert.Equal(98, r.Quality);
        Assert.Equal(19.0, r.CnDb);
        Assert.Equal(55.1, r.LevelDbuV!.Value, 1);
        Assert.Equal(19.7, r.MerDb!.Value, 1);
        Assert.Equal(3.64413e-5, r.Ber!.Value, 9);
    }

    [Fact]
    public void ParseStatus_NoLock()
    {
        var r = Sf4000Protocol.ParseStatus(Convert.FromHexString("A5F3043300000001840000000000989680"))!;
        Assert.False(r.IsLocked);
        Assert.Equal(0, r.CnRaw);
        Assert.Equal(388, r.LevelRaw);
        Assert.Equal(0.1, r.Ber);
    }

    [Fact]
    public void ParseStatus_Basic()
    {
        var r = Sf4000Protocol.ParseStatus(Convert.FromHexString("A5F3010D00"))!;
        Assert.Equal(StatusReading.StateNoTransponder, r.State);
        Assert.Null(r.LevelDbuV);
    }

    [Fact]
    public void PlaneAnalysis_FlagsWeakPlane()
    {
        var session = new ScanSession();
        foreach (var plane in Plane.All)
        {
            double baseLevel = plane == new Plane(true, Polarization.Vertical) ? 45 : 62;
            for (int i = 0; i < 6; i++)
            {
                session.Results.Add(new ScanResult
                {
                    FrequencyMHz = (plane.HighBand ? 11800 : 10800) + i * 100,
                    Polarization = plane.Polarization,
                    TuneAccepted = true,
                    Locked = true,
                    LevelDbuV = baseLevel,
                    MerDb = 14,
                });
            }
        }

        var stats = PlaneAnalysis.Analyze(session).ToDictionary(s => s.Plane);
        var weak = stats[new Plane(true, Polarization.Vertical)];
        Assert.Equal(Severity.Warning, weak.Severity);
        Assert.Equal(-17, weak.DeviationFromOthers);
        Assert.Equal(Severity.Ok, stats[new Plane(false, Polarization.Horizontal)].Severity);
    }

    [Fact]
    public void ClassifyMissing_GoodLevelInWorkingPlane_IsInactiveNotHole()
    {
        ScanResult R(int f, bool locked, double level) => new()
        {
            FrequencyMHz = f, Polarization = Polarization.Horizontal, TuneAccepted = true, Locked = locked, LevelDbuV = level,
        };
        // Values as measured on the device: 10744 H without lock at 56 dBµV, neighbors with lock around 55 dBµV
        var session = new ScanSession
        {
            Results = [R(10744, false, 56.0), R(11303, true, 55.1), R(11494, true, 55.2), R(11597, true, 54.0), R(11685, false, 41.0)],
        };

        session.ClassifyMissingTransponders();

        Assert.True(session.Results[0].ProbablyInactive);
        Assert.False(session.Results[4].ProbablyInactive); // level clearly too low → real hole
        Assert.Equal(1, session.HoleCount);
    }

    [Fact]
    public void ReferenceSelector_PrefersHighMerTypicalLevel()
    {
        var plane = new Plane(false, Polarization.Horizontal);
        ScanResult R(int f, double level, double mer) => new()
        {
            FrequencyMHz = f, Polarization = Polarization.Horizontal, TuneAccepted = true, Locked = true,
            LevelDbuV = level, MerDb = mer, LockTimeSeconds = 1,
        };
        var results = new[] { R(10744, 60, 12), R(11303, 61, 16), R(11494, 75, 17), R(11600, 60, 15) };

        // 11494 has the highest MER, but its level is an outlier
        Assert.Equal(11303, ReferenceSelector.Suggest(plane, results)!.FrequencyMHz);
    }
}
