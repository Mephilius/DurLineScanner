using DurLineScanner.Data;
using DurLineScanner.Scanning;

namespace DurLineScanner.Tests;

public class DiagnosisTests
{
    [Fact]
    public void HealthyScan_NothingConspicuous()
    {
        var clean = Scan("Wohnzimmer");
        var findings = Diagnosis.Analyze([clean], [clean, Scan("Küche")]);

        var only = Assert.Single(findings);
        Assert.Equal(Severity.Ok, only.Severity);
    }

    [Fact]
    public void LowEndAttenuatedOnOnePlane_HighPassBeforeMultiswitch()
    {
        // Bad F connector on the Low V trunk line: −15 dB at 950 MHz, gone after 250 MHz
        var scan = Scan("Wohnzimmer", level: (plane, ifMHz) => plane == LowV ? 63 - Math.Max(0, 15 - (ifMHz - 950) * 0.06) : 63);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan]), f => f.Title.Contains("Hochpass"));
        Assert.StartsWith("Low V", finding.Title);
        Assert.Contains("vor dem Multischalter", finding.Where);
        Assert.Contains("Low H", finding.NextStep); // swap with the sister input
    }

    [Theory]
    [InlineData(LnbType.Quattro, "an beiden Enden tauschen")]
    [InlineData(LnbType.Quad, "an den Eingängen Low V und Low H tauschen")]
    [InlineData(LnbType.Unicable, "im Unicable-Umsetzer")]
    [InlineData(LnbType.Unknown, "bei einem Quad-LNB")]
    public void HighPassAdvice_DependsOnLnbType(LnbType lnb, string expected)
    {
        var scan = Scan("Wohnzimmer", level: (plane, ifMHz) => plane == LowV ? 63 - Math.Max(0, 15 - (ifMHz - 950) * 0.06) : 63);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan], new Site { LnbType = lnb }), f => f.Title.Contains("Hochpass"));
        Assert.Contains(expected, finding.NextStep);
    }

    [Theory]
    [InlineData(LevelControl.PerBand, "nur je Band")]
    [InlineData(LevelControl.Common, "alle Ebenen gemeinsam")]
    [InlineData(LevelControl.PerPlane, "den Pegel dieser Ebene angleichen")]
    public void ImbalanceAdvice_DependsOnLevelControl(LevelControl control, string expected)
    {
        var scan = Scan("Wohnzimmer", level: (plane, _) => plane == LowV ? 55 : 63);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan], new Site { Multiswitch = MultiswitchKind.Active, MultiswitchLevel = control }),
            f => f.Title.Contains("Pegel weicht"));
        Assert.Contains(expected, finding.NextStep);
    }

    [Theory]
    [InlineData(LevelControl.None, "Dämpfungsglied von ca. 10 dB")]
    [InlineData(LevelControl.Common, "um ca. 10 dB zurückregeln")]
    public void HighLevelAdvice_DependsOnLevelControl(LevelControl control, string expected)
    {
        var scan = Scan("Wohnzimmer", level: (_, _) => 78);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan], new Site { Multiswitch = MultiswitchKind.Active, MultiswitchLevel = control }),
            f => f.Title.Contains("Pegel sehr hoch"));
        Assert.Contains(expected, finding.NextStep);
    }

    [Theory]
    [InlineData(true, "Schräglage (Slope) am Multischalter erhöhen.")]
    [InlineData(false, "keine Slope-Regelung")]
    public void FallingLevel_CableTilt(bool slopeAdjustable, string expected)
    {
        // −24 dB from 950 to 2150 MHz
        var scan = Scan("Wohnzimmer", level: (_, ifMHz) => 72 - (ifMHz - 950) * 0.02);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan], new Site { MultiswitchSlope = slopeAdjustable }), f => f.Title.Contains("Kabeldämpfung"));
        Assert.Contains(expected, finding.NextStep);
    }

    [Fact]
    public void PlaneWithLevelButNoLock_WrongPlane()
    {
        var scan = Scan("Wohnzimmer", locked: plane => plane != HighH);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan]), f => f.Title.Contains("falsche Ebene"));
        Assert.Equal(Severity.Warning, finding.Severity);
        Assert.StartsWith("High H", finding.Title);
    }

    [Fact]
    public void FaultAtAllOutletsButNotAtLnbPort_TrunkLineOrMultiswitchInput()
    {
        Func<Plane, int, double> highPass = (plane, ifMHz) => plane == LowV ? 63 - Math.Max(0, 15 - (ifMHz - 950) * 0.06) : 63;
        List<ScanSession> sessions =
        [
            Scan("Wohnzimmer", level: highPass),
            Scan("Küche", level: highPass),
            Scan("Bad", level: highPass),
            Scan("Port 1", kind: LocationKind.LnbPort),
        ];

        var finding = Assert.Single(Diagnosis.Analyze(sessions, sessions), f => f.Title.Contains("Hochpass"));
        Assert.Contains("An allen 3 Dosen", finding.Where);
        Assert.Contains("nicht am LNB-Port", finding.Where);
    }

    [Fact]
    public void FaultAtOneOutletOnly_ThatOutletsCable()
    {
        List<ScanSession> sessions =
        [
            Scan("Wohnzimmer", level: (_, ifMHz) => 50 - (ifMHz - 950) * 0.01), // whole outlet weak
            Scan("Küche"),
            Scan("Bad"),
            Scan("Flur"),
        ];

        var findings = Diagnosis.Analyze(sessions, sessions);

        var weak = Assert.Single(findings, f => f.Title.Contains("weniger Pegel als die anderen Dosen"));
        Assert.StartsWith("Dose Wohnzimmer", weak.Title);
        var low = Assert.Single(findings, f => f.Title.Contains("Pegel zu niedrig"));
        Assert.Contains("Nur an 1 von 4 Dosen (Wohnzimmer)", low.Where);
    }

    [Fact]
    public void MerDipsAtNormalLevel_Reflections()
    {
        var reference = Scan("Küche");
        // Every third High V transponder 5 dB worse than in the reference scan
        var scan = Scan("Wohnzimmer", mer: (plane, ifMHz) => plane == HighV && ifMHz / 30 % 3 == 0 ? 11 : 16);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan, reference]), f => f.Title.Contains("MER-Einbrüche"));
        Assert.StartsWith("High V", finding.Title);
    }

    [Fact]
    public void MerDipsInMobileBand_Ingress()
    {
        var reference = Scan("Küche");
        // LTE 1800 leaking into the cable: all planes worse between 1805 and 1880 MHz IF
        var scan = Scan("Wohnzimmer", mer: (_, ifMHz) => ifMHz is > 1790 and < 1895 ? 10 : 16);

        var finding = Assert.Single(Diagnosis.Analyze([scan], [scan, reference]), f => f.Title.Contains("Einstrahlung"));
        Assert.Contains("LTE 1800", finding.Title);
    }

    [Fact]
    public void MoreLevelWorseMerAcrossOutlets_Overload()
    {
        var reference = Scan("Keller");
        List<ScanSession> sessions =
        [
            Scan("Wohnzimmer", level: (p, _) => p == LowH ? 64 : 63, mer: (p, _) => p == LowH ? 15.5 : 16),
            Scan("Küche", level: (p, _) => p == LowH ? 68 : 63, mer: (p, _) => p == LowH ? 14.5 : 16),
            Scan("Bad", level: (p, _) => p == LowH ? 72 : 63, mer: (p, _) => p == LowH ? 13 : 16),
            Scan("Flur", level: (p, _) => p == LowH ? 75 : 63, mer: (p, _) => p == LowH ? 12 : 16),
        ];

        var finding = Assert.Single(Diagnosis.Analyze(sessions, [.. sessions, reference]), f => f.Title.Contains("Übersteuerung"));
        Assert.StartsWith("Low H", finding.Title);
        Assert.Contains("Flur", finding.Evidence);
    }

    // ---------------------------------------------------------------- synthetic scans

    private static readonly Plane LowV = new(false, Polarization.Vertical);
    private static readonly Plane LowH = new(false, Polarization.Horizontal);
    private static readonly Plane HighV = new(true, Polarization.Vertical);
    private static readonly Plane HighH = new(true, Polarization.Horizontal);

    /// <summary>
    /// Scan with 20 transponders per plane every 50 MHz IF; V and H are offset by 25 MHz like on Astra.
    /// Defaults: flat 63 dBµV, MER 16 dB, everything locked.
    /// </summary>
    private static ScanSession Scan(
        string location,
        Func<Plane, int, double>? level = null,
        Func<Plane, int, double>? mer = null,
        Func<Plane, bool>? locked = null,
        LocationKind kind = LocationKind.Socket)
    {
        var session = new ScanSession
        {
            SatelliteName = "19.2E",
            SatelliteLongitude = 19.2,
            LocationKind = kind,
            LocationName = location,
            Weather = Weather.Sunny,
        };
        foreach (var plane in Plane.All)
        {
            int lof = plane.HighBand ? 10600 : 9750;
            int start = plane.HighBand ? 1110 : 960;
            int offset = plane.Polarization == Polarization.Vertical ? 0 : 25;
            for (int i = 0; i < 20; i++)
            {
                int ifMHz = start + offset + i * 50;
                bool isLocked = locked?.Invoke(plane) ?? true;
                session.Results.Add(new ScanResult
                {
                    FrequencyMHz = lof + ifMHz,
                    Polarization = plane.Polarization,
                    SymbolRateKs = 22000,
                    DvbSystem = "DVB-S2",
                    Modulation = "8PSK",
                    Fec = "3/4",
                    TuneAccepted = true,
                    Locked = isLocked,
                    LevelDbuV = level?.Invoke(plane, ifMHz) ?? 63,
                    MerDb = isLocked ? mer?.Invoke(plane, ifMHz) ?? 16 : null,
                });
            }
        }
        session.ClassifyMissingTransponders();
        return session;
    }
}
