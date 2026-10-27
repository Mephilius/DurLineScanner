using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>Eine der vier Sat-ZF-Ebenen (Low/High-Band × H/V), wie sie Multischalter und Verstärker führen.</summary>
public readonly record struct Plane(bool HighBand, Polarization Polarization)
{
    public static readonly Plane[] All =
    [
        new(false, Polarization.Vertical),
        new(false, Polarization.Horizontal),
        new(true, Polarization.Vertical),
        new(true, Polarization.Horizontal),
    ];

    public static Plane Of(ScanResult r) => new(r.FrequencyMHz >= 11700, r.Polarization);

    public override string ToString() =>
        $"{(HighBand ? "High" : "Low")} {(Polarization == Polarization.Horizontal ? "H" : "V")}";
}

public sealed record PlaneStats(
    Plane Plane,
    int Total,
    int Locked,
    double? LevelMean,
    double? LevelMin,
    double? LevelMax,
    double? MerMean,
    double? SlopeDbPer100MHz,
    double? DeviationFromOthers)
{
    public string PlaneName => Plane.ToString();

    public string Assessment { get; init; } = "";

    public Severity Severity { get; init; }
}

public enum Severity
{
    Ok,
    Hint,
    Warning,
}

/// <summary>
/// Pegel pro Ebene über alle gelockten Transponder: zu hoch/zu niedrig, Ungleichgewicht zwischen
/// den Ebenen (Verstärker-/Multischalter-Einstellung) und Schräglage über die Frequenz (Kabeldämpfung).
/// </summary>
public static class PlaneAnalysis
{
    // Richtwerte für die Teilnehmerdose nach EN 60728 (DVB-S/S2): 47..77 dBµV, gut 60..70 dBµV.
    // Der Pegel des SF 4000 ist nicht kalibriert, daher großzügig.
    public const double LevelTooLow = 50;
    public const double LevelLowish = 55;
    public const double LevelHigh = 77;
    public const double ImbalanceDb = 4;
    // Der Frequenzgang des SF 4000 selbst erzeugt schon einige dB Schräglage (am LNB-Port gemessen: ±7 dB)
    public const double SlopeDb = 8;

    private const int LofLow = 9750;
    private const int LofHigh = 10600;

    public static IReadOnlyList<PlaneStats> Analyze(ScanSession session)
    {
        // Vermutlich abgeschaltete Transponder zählen nicht als Empfangsproblem
        var raw = Plane.All
            .Select(p => Compute(p, session.Results.Where(r => Plane.Of(r) == p && !r.ProbablyInactive).ToList()))
            .ToList();

        // Abweichung jeder Ebene vom Median aller Ebenen – robust, eine einzelne Ausreißer-Ebene
        // verschiebt die Referenz der anderen nicht
        var means = raw.Where(s => s.LevelMean.HasValue).Select(s => s.LevelMean!.Value).Order().ToList();
        double? median = means.Count switch
        {
            < 2 => null,
            _ when means.Count % 2 == 0 => (means[means.Count / 2 - 1] + means[means.Count / 2]) / 2,
            _ => means[means.Count / 2],
        };
        var withDeviation = raw.Select(s => s with
        {
            DeviationFromOthers = s.LevelMean.HasValue && median.HasValue ? Math.Round(s.LevelMean.Value - median.Value, 1) : null,
        }).ToList();

        return withDeviation.Select(Assess).ToList();
    }

    private static PlaneStats Compute(Plane plane, List<ScanResult> results)
    {
        var locked = results.Where(r => r.Locked).ToList();
        var levels = locked.Where(r => r.LevelDbuV.HasValue).ToList();
        var mers = locked.Where(r => r.MerDb.HasValue).Select(r => r.MerDb!.Value).ToList();

        double? slope = null;
        if (levels.Count >= 4)
        {
            // Lineare Regression Pegel über ZF – zeigt die Kabel-Schräglage
            var xs = levels.Select(r => (double)IfOf(r)).ToArray();
            var ys = levels.Select(r => r.LevelDbuV!.Value).ToArray();
            double mx = xs.Average(), my = ys.Average();
            double sxx = xs.Sum(x => (x - mx) * (x - mx));
            if (sxx > 0)
                slope = Math.Round(xs.Zip(ys).Sum(p => (p.First - mx) * (p.Second - my)) / sxx * 100, 2);
        }

        return new PlaneStats(
            plane,
            results.Count,
            locked.Count,
            levels.Count > 0 ? Math.Round(levels.Average(r => r.LevelDbuV!.Value), 1) : null,
            levels.Count > 0 ? levels.Min(r => r.LevelDbuV) : null,
            levels.Count > 0 ? levels.Max(r => r.LevelDbuV) : null,
            mers.Count > 0 ? Math.Round(mers.Average(), 1) : null,
            slope,
            null);
    }

    private static PlaneStats Assess(PlaneStats s)
    {
        var notes = new List<string>();
        var severity = Severity.Ok;

        void Add(Severity level, string text)
        {
            notes.Add(text);
            if (level > severity) severity = level;
        }

        if (s.Total == 0)
            return s with { Assessment = L.T("nicht gemessen") };
        if (s.Locked == 0)
            return s with { Assessment = L.T("kein Transponder empfangen – Ebene fehlt (LNB, Kabel, Multischalter?)"), Severity = Severity.Warning };

        double lockRate = (double)s.Locked / s.Total;
        if (lockRate < 0.5)
            Add(Severity.Warning, L.F("nur {0:P0} Lock", lockRate));

        if (s.LevelMean is { } level)
        {
            if (level < LevelTooLow) Add(Severity.Warning, L.F("Pegel zu niedrig (Ø {0:0.0} dBµV) – Verstärkung erhöhen", level));
            else if (level < LevelLowish) Add(Severity.Hint, L.F("Pegel knapp (Ø {0:0.0} dBµV)", level));
            else if (level > LevelHigh) Add(Severity.Warning, L.F("Pegel zu hoch (Ø {0:0.0} dBµV) – Übersteuerung möglich, Verstärkung senken", level));
        }

        if (s.DeviationFromOthers is { } dev && Math.Abs(dev) >= ImbalanceDb)
            Add(Severity.Hint, dev > 0
                ? L.F("{0:0.0} dB höher als die anderen Ebenen – Ebene am Verstärker angleichen", Math.Abs(dev))
                : L.F("{0:0.0} dB niedriger als die anderen Ebenen – Ebene am Verstärker angleichen", Math.Abs(dev)));

        if (s.SlopeDbPer100MHz is { } slope && Math.Abs(slope * 12) >= SlopeDb)
            Add(Severity.Hint, L.F("Schräglage {0:+0.0;-0.0} dB über 950–2150 MHz – Entzerrung (Slope) prüfen", slope * 12));

        if (s.MerMean is { } mer && mer < 9)
            Add(Severity.Hint, L.F("MER niedrig (Ø {0:0.0} dB)", mer));

        return s with { Assessment = notes.Count == 0 ? L.T("in Ordnung") : string.Join("; ", notes), Severity = severity };
    }

    private static int IfOf(ScanResult r) => r.FrequencyMHz - (r.FrequencyMHz >= 11700 ? LofHigh : LofLow);
}
