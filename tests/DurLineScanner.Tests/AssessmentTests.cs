using DurLineScanner.Data;
using DurLineScanner.Device;
using DurLineScanner.Scanning;

namespace DurLineScanner.Tests;

public class AssessmentTests
{
    private static readonly Site Frankfurt = new() { Name = "Test", Latitude = 50.11, Longitude = 8.68, DishCm = 80, LnbGainDb = 55 };

    [Fact]
    public void Look_FrankfurtToAstra192()
    {
        // Reference values from common dish alignment calculators: azimuth ≈ 166°, elevation ≈ 31°
        var look = LinkBudget.Look(50.11, 8.68, 19.2);
        Assert.InRange(look.AzimuthDeg, 165, 168);
        Assert.InRange(look.ElevationDeg, 30.5, 32);
        Assert.InRange(Math.Abs(look.SkewDeg), 6, 10);
        Assert.InRange(look.DistanceKm, 38000, 39000);
    }

    [Fact]
    public void Expect_BiggerDishAndBetterWeatherGiveMore()
    {
        var sun80 = LinkBudget.Expect(Frankfurt, 19.2, 11700, 27500, Weather.Sunny);
        var sun60 = LinkBudget.Expect(With60(), 19.2, 11700, 27500, Weather.Sunny);
        var storm80 = LinkBudget.Expect(Frankfurt, 19.2, 11700, 27500, Weather.Thunderstorm);

        Assert.InRange(sun80.CnDb, 15, 21);
        Assert.True(sun80.CnDb - sun60.CnDb is > 2 and < 3); // 20·log(80/60) ≈ 2.5 dB
        Assert.True(storm80.CnDb < sun80.CnDb - 5);
        Assert.True(sun80.MerDb < LinkBudget.SatelliteMerDb);
    }

    private static Site With60()
    {
        var s = Frankfurt.Copy();
        s.DishCm = 60;
        return s;
    }

    [Fact]
    public void Live_MarginAndDeficitDecide()
    {
        var tp = new Transponder(11494, Polarization.Horizontal, 22000, "DVB-S2", "8PSK", "3/4");
        var c = new Conditions(Frankfurt, Weather.Sunny, 19.2);
        StatusReading Locked(double mer) => new(StatusReading.StateLocked, 80, 80) { MerRaw = (long)(mer * 10) };

        Assert.Equal(Light.Green, Assessment.Live(Locked(15), tp, c).Light);
        // enough margin above the threshold, but far below the expectation for 80 cm → yellow
        Assert.Equal(Light.Yellow, Assessment.Live(Locked(11.5), tp, c).Light);
        Assert.Equal(Light.Red, Assessment.Live(Locked(8.0), tp, c).Light);
    }

    [Fact]
    public void Live_NoLockIsRed()
    {
        var tp = new Transponder(11494, Polarization.Horizontal, 22000, "DVB-S2", "8PSK", "2/3");
        var verdict = Assessment.Live(new StatusReading(StatusReading.StateNoTransponder, 0, 0), tp, new(Frankfurt, Weather.Sunny, 19.2));
        Assert.Equal(Light.Red, verdict.Light);
    }

    [Fact]
    public void Scan_GradesByMarginHolesAndExpectation()
    {
        var good = Session(mer: 15, holes: 0);
        Assert.Equal(Light.Green, Assessment.Scan(good).Light);

        // 8PSK 3/4 needs 7.9 dB – 10 dB is a margin of 2.1 dB → yellow
        Assert.Equal(Light.Yellow, Assessment.Scan(Session(mer: 10, holes: 0)).Light);
        Assert.Equal(Light.Red, Assessment.Scan(Session(mer: 8.2, holes: 0)).Light);
        // 3 of 20 without lock → more than 10 % → red
        Assert.Equal(Light.Red, Assessment.Scan(Session(mer: 15, holes: 3)).Light);
    }

    [Fact]
    public void Scan_WithoutSiteStillGrades()
    {
        var s = Session(mer: 15, holes: 0);
        s.Site = null;
        var verdict = Assessment.Scan(s);
        Assert.Equal(Light.Green, verdict.Light);
        Assert.Contains(verdict.Reasons, r => r.Contains("Kein Standort"));
    }

    private static ScanSession Session(double mer, int holes)
    {
        var session = new ScanSession { Site = Frankfurt.Copy(), Weather = Weather.Sunny, SatelliteLongitude = 19.2 };
        for (int i = 0; i < 20; i++)
        {
            bool locked = i >= holes;
            session.Results.Add(new ScanResult
            {
                FrequencyMHz = 10700 + i * 100,
                Polarization = i % 2 == 0 ? Polarization.Horizontal : Polarization.Vertical,
                SymbolRateKs = 27500,
                DvbSystem = "DVB-S2",
                Modulation = "8PSK",
                Fec = "3/4",
                TuneAccepted = true,
                Locked = locked,
                MerDb = locked ? mer : null,
                // Holes with a weak level so they don't count as switched off
                LevelDbuV = locked ? 65 : 45,
            });
        }
        session.ClassifyMissingTransponders();
        return session;
    }
}
