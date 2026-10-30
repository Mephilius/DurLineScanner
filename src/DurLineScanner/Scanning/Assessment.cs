using DurLineScanner.Data;
using DurLineScanner.Device;

namespace DurLineScanner.Scanning;

public enum Light
{
    None,
    Green,
    Yellow,
    Red,
}

/// <summary>Traffic-light result with headline and reasons.</summary>
public sealed record Verdict(Light Light, string Headline, IReadOnlyList<string> Reasons)
{
    public static readonly Verdict Empty = new(Light.None, L.T("Noch keine Messung"), []);
}

/// <summary>Measurement conditions: site (dish, LNB, coordinates), weather and satellite.</summary>
public sealed record Conditions(Site? Site, Weather? Weather, double SatelliteLongitude);

/// <summary>
/// Traffic light for live values and scans. Rates the MER margin above the reception threshold, the margin for
/// worse weather and – with site data – the measured MER against the expectation from the link budget.
/// </summary>
public static class Assessment
{
    /// <summary>From this many dB below the expectation the system counts as needing improvement or faulty.</summary>
    public const double DeficitYellowDb = 4;
    public const double DeficitRedDb = 8;

    public static Verdict Live(StatusReading? reading, Transponder? tp, Conditions c)
    {
        if (reading is null || tp is null) return Verdict.Empty;
        if (!reading.IsLocked)
            return new Verdict(Light.Red, L.T("Kein Empfang"),
                [L.T("Der Transponder wird nicht empfangen – Ausrichtung, LNB, Kabel und Multischalter prüfen.")]);

        var builder = new Builder();
        var (threshold, basis) = SignalLimits.Threshold(tp.DvbSystem, tp.Modulation, tp.Fec);
        if (reading.MerDb is { } mer)
            CheckSignal(builder, mer, threshold, basis, tp.FrequencyMHz, tp.SymbolRateKs, c, "MER");
        if (SignalLimits.Level.Evaluate(reading.LevelDbuV) is Grade.Bad or Grade.VeryBad)
            builder.Add(Light.Yellow, L.F("Pegel {0:0.0} dBµV außerhalb 47–77 dBµV.", reading.LevelDbuV));
        return builder.Build(c);
    }

    public static Verdict Scan(ScanSession session)
    {
        var relevant = session.Results.Where(r => !r.ProbablyInactive).ToList();
        if (relevant.Count == 0) return Verdict.Empty;
        var c = new Conditions(session.Site, session.Weather, session.SatelliteLongitude);
        var builder = new Builder();

        int holes = session.HoleCount;
        double holeRate = (double)holes / relevant.Count;
        if (holes > 0)
            builder.Add(holeRate >= 0.1 ? Light.Red : Light.Yellow,
                L.F("{0} von {1} Transpondern ohne Lock ({2:P0}).", holes, relevant.Count, holeRate));

        foreach (var plane in PlaneAnalysis.Analyze(session).Where(p => p.Total > 0))
        {
            if (plane.Locked == 0)
                builder.Add(Light.Red, L.F("Ebene {0} fehlt ganz – LNB-Ausgang, Kabel oder Multischalter prüfen.", plane.PlaneName));
            else if (plane.Severity == Severity.Warning)
                builder.Add(Light.Yellow, L.F("Ebene {0}: {1}.", plane.PlaneName, plane.Assessment));
        }

        var locked = relevant.Where(r => r.Locked && r.MerDb.HasValue).ToList();
        if (locked.Count > 0)
        {
            // Typical transponder for margin and expectation, weak ones separately
            var margins = locked.Select(r => r.MerDb!.Value - r.Threshold.Db).Order().ToList();
            var typical = locked.OrderBy(r => r.MerDb!.Value - r.Threshold.Db).ElementAt(locked.Count / 2);
            CheckSignal(builder, typical.MerDb!.Value, typical.Threshold.Db, L.T("Median aller Transponder"),
                typical.FrequencyMHz, typical.SymbolRateKs, c, L.T("Median-MER"));

            double weakest = margins[(int)(margins.Count * 0.1)];
            if (weakest < 1 && margins[margins.Count / 2] >= 1)
                builder.Add(Light.Yellow, L.F("Schwächste Transponder nur {0:+0.0;-0.0} dB über der Empfangsschwelle.", weakest));
        }
        return builder.Build(c);
    }

    private static void CheckSignal(Builder b, double mer, double threshold, string basis, int freq, int sr, Conditions c, string label)
    {
        double margin = mer - threshold;
        if (margin < 1)
            b.Add(Light.Red, L.F("{0} {1:0.0} dB – nur {2:+0.0;-0.0} dB über der Empfangsschwelle ({3:0.0} dB, {4}).", label, mer, margin, threshold, basis));
        else if (margin < 3)
            b.Add(Light.Yellow, L.F("{0} {1:0.0} dB – Reserve {2:+0.0} dB, knapp ({3}).", label, mer, margin, basis));
        else
            b.Info(L.F("{0} {1:0.0} dB – Reserve {2:+0.0} dB über der Empfangsschwelle.", label, mer, margin));

        if (c.Site is not { } site)
        {
            b.Info(L.T("Kein Standort gewählt – ohne Schüsselgröße keine Erwartung und keine Wetterreserve."));
            return;
        }
        var weather = c.Weather ?? Weather.PartlyCloudy;
        var expected = LinkBudget.Expect(site, c.SatelliteLongitude, freq, sr, weather);
        double deficit = expected.MerDb - mer;
        if (deficit >= DeficitRedDb)
            b.Add(Light.Red, L.F("{0:0.0} dB unter der Erwartung für {1} cm ({2:0.0} dB) – Ausrichtung, LNB oder Kabel stark fehlerhaft.", deficit, site.DishCm, expected.MerDb));
        else if (deficit >= DeficitYellowDb)
            b.Add(Light.Yellow, L.F("{0:0.0} dB unter der Erwartung für {1} cm ({2:0.0} dB) – Feinausrichtung (Azimut, Elevation, Skew) prüfen.", deficit, site.DishCm, expected.MerDb));
        else
            b.Info(L.F("Erwartung für {0} cm bei „{1}“: ca. {2:0.0} dB – erreicht.", site.DishCm, WeatherInfo.Text(weather), expected.MerDb));

        // Margin for worse weather
        if (!WeatherInfo.IsWet(weather))
        {
            double rain = LinkBudget.ExtraLossDb(site, c.SatelliteLongitude, freq, sr, weather, Weather.Rain);
            double storm = LinkBudget.ExtraLossDb(site, c.SatelliteLongitude, freq, sr, weather, Weather.Thunderstorm);
            if (margin > 0 && margin < rain + 1)
                b.Add(Light.Yellow, L.F("Reserve reicht bei Regen kaum (Regen kostet hier ca. {0:0.0} dB).", rain));
            else if (margin < storm)
                b.Info(L.F("Bei Gewitter-Starkregen (ca. {0:0.0} dB) sind kurze Aussetzer möglich.", storm));
            else
                b.Info(L.F("Reserve deckt auch Gewitter-Starkregen (ca. {0:0.0} dB).", storm));
        }
        else
        {
            b.Info(L.F("Gemessen bei „{0}“ – Dämpfung ca. {1:0.0} dB steckt bereits im Messwert.", WeatherInfo.Text(weather), expected.FadeDb));
        }
    }

    private sealed class Builder
    {
        private readonly List<(Light Light, string Text)> _items = [];

        public void Add(Light light, string text) => _items.Add((light, text));

        public void Info(string text) => _items.Add((Light.Green, text));

        public Verdict Build(Conditions c)
        {
            var light = _items.Count == 0 ? Light.Green : _items.Max(i => i.Light);
            string weather = c.Weather is { } w ? $" · {WeatherInfo.Text(w)}" : "";
            string headline = light switch
            {
                Light.Red => L.T("Empfang gestört"),
                Light.Yellow => L.T("Empfang verbesserungswürdig"),
                _ => L.T("Empfang in Ordnung"),
            } + weather;
            // Problems first, then hints
            var reasons = _items.OrderByDescending(i => i.Light).Select(i => i.Text).Distinct().ToList();
            return new Verdict(light, headline, reasons);
        }
    }
}
