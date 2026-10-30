using DurLineScanner.Data;

namespace DurLineScanner.Scanning;

/// <summary>A diagnosis result: what stands out, the measured evidence, where the cause probably sits and what to check.</summary>
public sealed record Finding(Severity Severity, string Title, string Evidence, string Where, string NextStep);

/// <summary>
/// Fault diagnosis from the selected scans of one satellite. A single scan sees the whole path from the LNB to its
/// measuring point: it tells the kind of fault and – via the four IF planes – roughly where it sits. Several scans
/// locate it in the system: already at the LNB port → LNB/dish, at all outlets only → trunk line or multiswitch input,
/// at single outlets → their cables.
/// MER is compared per transponder with the best value measured so far (converted to the dish and weather of the
/// scan), because every transponder has its own MER.
/// </summary>
public static class Diagnosis
{
    /// <summary>Level rise within the lowest 300 MHz of a plane that counts as high-pass behaviour …</summary>
    public const double HighPassRiseDb = 8;

    /// <summary>… and by how much more than the other planes (the meter's own response affects all planes alike).</summary>
    public const double HighPassRelativeDb = 6;

    /// <summary>Level difference of a plane from the others that is reported.</summary>
    public const double ImbalanceDb = 6;

    /// <summary>A transponder counts as MER dip if it is this much further below its best value than its plane.</summary>
    public const double DipDb = 2.5;

    /// <summary>A plane whose MER is this much further below the best values than the other planes.</summary>
    public const double PlaneMerDb = 2.5;

    /// <summary>All planes this much below the best values.</summary>
    public const double CommonMerDb = 3;

    public const double LowLevelDbuV = 47;
    public const double HighLevelDbuV = 76;

    /// <summary>Level change across the IF range (dB per GHz) that counts as cable tilt; the meter itself adds a few dB.</summary>
    public const double TiltDbPerGhz = 10;

    /// <summary>Outlet whose mean level differs this much from the other outlets.</summary>
    public const double OutletLevelDb = 6;

    /// <summary>Share of outlets from which a symptom counts as common to all of them.</summary>
    private const double CommonShare = 0.5;

    /// <summary>
    /// Factor on the thresholds for locating with several scans: an outlet just below a threshold shows the same
    /// fault, only weaker – otherwise a common fault would look like a problem of single outlet cables.
    /// </summary>
    private const double Relaxed = 0.6;

    /// <summary>Radio services in the sat IF range (MHz) that leak into poorly shielded cables and outlets.</summary>
    private static readonly (string Name, int From, int To)[] RadioBands =
    [
        ("GSM 900", 925, 960),
        ("LTE 1500 (Band 32)", 1452, 1496),
        ("LTE 1800", 1805, 1880),
        ("DECT", 1880, 1900),
        ("UMTS/LTE 2100", 2110, 2170),
    ];

    private enum Kind
    {
        NoSignal,
        WrongPlane,
        HighPass,
        Imbalance,
        LowLevel,
        HighLevel,
        PlaneMer,
        CommonMer,
        Ingress,
        Reflections,
        CrossPol,
        Tilt,
    }

    private sealed record Point(ScanResult R, Plane Plane, int If, double Level, double Mer, double? Deficit);

    /// <param name="Amount">Key figure for the advice, e.g. the highest level.</param>
    private sealed record Symptom(Kind Kind, Plane[] Planes, ScanSession Session, Severity Severity, string Evidence, string Extra = "", double Amount = 0)
    {
        public string Key => $"{Kind}|{string.Join(",", Planes)}|{Extra}";
    }

    /// <param name="selected">Scans to diagnose (same satellite).</param>
    /// <param name="history">All stored scans of the satellite – reference for the best MER per transponder.</param>
    /// <param name="setup">Site with LNB and multiswitch – decides how a fault can be narrowed down and fixed.</param>
    public static IReadOnlyList<Finding> Analyze(IReadOnlyList<ScanSession> selected, IEnumerable<ScanSession> history, Site? setup = null)
    {
        var site = setup ?? new Site();
        var sessions = selected.Where(s => s.Results.Any(r => r.Locked)).Distinct().ToList();
        if (sessions.Count == 0) return [];

        var reference = new Reference(history.Concat(sessions).Distinct());
        var symptoms = sessions.SelectMany(s => Examine(s, reference, 1)).ToList();
        var weaker = sessions.Count > 1 ? sessions.SelectMany(s => Examine(s, reference, Relaxed)).ToList() : [];
        var findings = symptoms.GroupBy(s => s.Key)
            .Select(g => Locate(g.ToList(), weaker.Where(w => w.Key == g.Key && !g.Any(x => x.Session == w.Session)).ToList(), sessions, site))
            .ToList();
        if (sessions.Count > 1) findings.AddRange(CompareScans(sessions, reference, site));

        if (findings.Count == 0)
            findings.Add(new Finding(Severity.Ok, L.T("Keine Auffälligkeiten"),
                L.T("Pegel und MER verhalten sich auf allen Ebenen unauffällig."), "", ""));
        return findings.OrderByDescending(f => f.Severity).ToList();
    }

    // ---------------------------------------------------------------- one scan

    /// <param name="k">Factor on the thresholds (1 = report, below 1 = also weaker cases for locating).</param>
    private static IEnumerable<Symptom> Examine(ScanSession s, Reference reference, double k)
    {
        var relevant = s.Results.Where(r => !r.ProbablyInactive).ToList();
        var measured = Plane.All.Where(p => relevant.Count(r => Plane.Of(r) == p) >= 3).ToArray();
        var points = relevant.Where(r => r.Locked && r.LevelDbuV.HasValue && r.MerDb.HasValue)
            .Select(r => new Point(r, Plane.Of(r), IfOf(r), r.LevelDbuV!.Value, r.MerDb!.Value, reference.Deficit(s, r)))
            .ToList();
        var byPlane = measured.ToDictionary(p => p, p => points.Where(x => x.Plane == p).OrderBy(x => x.If).ToList());

        // Plane without reception: signal present but nothing locks → wrong plane on this input
        foreach (var plane in measured)
        {
            var all = relevant.Where(r => Plane.Of(r) == plane).ToList();
            if (all.Count < 5 || all.Count(r => r.Locked) >= all.Count * 0.3) continue;
            var unlocked = all.Where(r => !r.Locked).ToList();
            var withSignal = unlocked.Where(r => r.TuneAccepted && r.LevelDbuV >= 50).ToList();
            yield return withSignal.Count >= unlocked.Count * 0.6
                ? new Symptom(Kind.WrongPlane, [plane], s, Severity.Warning,
                    L.F("{0} von {1} Transpondern ohne Lock, obwohl Pegel anliegt (Ø {2:0.0} dBµV).",
                        unlocked.Count, all.Count, withSignal.Average(r => r.LevelDbuV!.Value)))
                : new Symptom(Kind.NoSignal, [plane], s, Severity.Warning,
                    L.F("{0} von {1} Transpondern ohne Lock, kaum Pegel.", unlocked.Count, all.Count));
        }

        foreach (var symptom in HighPass(s, byPlane, measured, k)) yield return symptom;
        foreach (var symptom in Levels(s, byPlane, measured, k)) yield return symptom;
        foreach (var symptom in Mer(s, byPlane, measured, k)) yield return symptom;
    }

    /// <summary>
    /// Low IF end attenuated, then flat: a capacitive contact (F connector whose centre conductor is too short,
    /// corroded or not seated) acts as a high-pass filter.
    /// </summary>
    private static IEnumerable<Symptom> HighPass(ScanSession s, Dictionary<Plane, List<Point>> byPlane, Plane[] measured, double k)
    {
        var rises = new Dictionary<Plane, (double Rise, double Low, double High, int From)>();
        foreach (var (plane, pts) in byPlane)
        {
            if (pts.Count < 6) continue;
            int first = pts[0].If;
            var upper = pts.Where(p => p.If >= first + 300 && p.If <= first + 700).Select(p => p.Level).ToList();
            if (upper.Count < 3) continue;
            double low = (pts[0].Level + pts[1].Level) / 2, high = Median(upper);
            rises[plane] = (high - low, low, high, first);
        }

        if (rises.Count >= 3 && rises.Values.All(x => x.Rise >= 10 * k))
        {
            yield return new Symptom(Kind.HighPass, measured, s, Severity.Hint,
                L.F("Alle Ebenen steigen in den ersten 300 MHz um {0:0}–{1:0} dB.", rises.Values.Min(x => x.Rise), rises.Values.Max(x => x.Rise)));
            yield break;
        }

        foreach (var (plane, x) in rises)
        {
            var others = rises.Where(o => o.Key != plane).Select(o => o.Value.Rise).ToList();
            double othersRise = others.Count > 0 ? Median(others) : 0;
            if (x.Rise < HighPassRiseDb * k || x.Rise - othersRise < HighPassRelativeDb * k) continue;
            yield return new Symptom(Kind.HighPass, [plane], s, x.Rise >= 12 ? Severity.Warning : Severity.Hint,
                L.F("Pegel steigt ab {0} MHz ZF in den ersten 300 MHz um {1:0} dB ({2:0.0} → {3:0.0} dBµV), die anderen Ebenen um {4:0} dB.",
                    x.From, x.Rise, x.Low, x.High, othersRise));
        }
    }

    private static IEnumerable<Symptom> Levels(ScanSession s, Dictionary<Plane, List<Point>> byPlane, Plane[] measured, double k)
    {
        var means = byPlane.Where(p => p.Value.Count >= 3).ToDictionary(p => p.Key, p => p.Value.Average(x => x.Level));
        if (means.Count >= 3)
        {
            foreach (var (plane, mean) in means)
            {
                double others = Median(means.Where(m => m.Key != plane).Select(m => m.Value).ToList());
                double dev = mean - others;
                if (Math.Abs(dev) < ImbalanceDb * k) continue;
                yield return new Symptom(Kind.Imbalance, [plane], s, Math.Abs(dev) >= 10 ? Severity.Warning : Severity.Hint,
                    L.F("Ø {0:0.0} dBµV, {1:+0.0;-0.0} dB gegenüber den anderen Ebenen (Ø {2:0.0} dBµV).", mean, dev, others));
            }
        }

        double lowLevel = LowLevelDbuV + (1 - k) * 10, highLevel = HighLevelDbuV - (1 - k) * 10;
        var low = byPlane.Where(p => p.Value.Count(x => x.Level < lowLevel) >= 3).Select(p => p.Key).ToArray();
        foreach (var group in Group(low, measured))
        {
            var pts = group.SelectMany(p => byPlane[p]).Where(x => x.Level < lowLevel).OrderBy(x => x.Level).ToList();
            yield return new Symptom(Kind.LowLevel, group, s, pts[0].Level < 43 ? Severity.Warning : Severity.Hint,
                L.F("{0} Transponder unter {1:0} dBµV, Minimum {2:0.0} dBµV bei {3} MHz.", pts.Count, LowLevelDbuV, pts[0].Level, pts[0].R.FrequencyMHz));
        }

        var high = byPlane.Where(p => p.Value.Count(x => x.Level >= highLevel) >= 2).Select(p => p.Key).ToArray();
        foreach (var group in Group(high, measured))
        {
            var pts = group.SelectMany(p => byPlane[p]).Where(x => x.Level >= highLevel).ToList();
            yield return new Symptom(Kind.HighLevel, group, s, Severity.Hint,
                L.F("{0} Transponder ab {1:0} dBµV, Maximum {2:0.0} dBµV.", pts.Count, HighLevelDbuV, pts.Max(x => x.Level)), Amount: pts.Max(x => x.Level));
        }

        // Level over the whole IF range at an outlet: falling = cable attenuation, rising = over-equalized
        var all = byPlane.Values.SelectMany(p => p).ToList();
        if (s.LocationKind == LocationKind.Socket && measured.Length >= 3 && all.Count >= 20)
        {
            var (_, slope) = Regression(all.Select(x => (double)x.If).ToList(), all.Select(x => x.Level).ToList());
            double perGhz = slope * 1000;
            if (Math.Abs(perGhz) >= TiltDbPerGhz * k)
                yield return new Symptom(Kind.Tilt, measured, s, Severity.Hint,
                    L.F("Pegel ändert sich über die ZF um {0:+0.0;-0.0} dB je GHz ({1:+0;-0} dB von 950 bis 2150 MHz).", perGhz, perGhz * 1.2),
                    perGhz < 0 ? "down" : "up");
        }
    }

    private static IEnumerable<Symptom> Mer(ScanSession s, Dictionary<Plane, List<Point>> byPlane, Plane[] measured, double k)
    {
        // Deficits exist only where another scan of the transponder serves as reference
        var withRef = byPlane.Where(p => p.Value.Count(x => x.Deficit.HasValue) >= 5)
            .ToDictionary(p => p.Key, p => p.Value.Where(x => x.Deficit.HasValue).ToList());
        if (withRef.Count == 0) yield break;
        var planeDeficit = withRef.ToDictionary(p => p.Key, p => Median(p.Value.Select(x => x.Deficit!.Value).ToList()));

        // Whole system below the best values
        if (planeDeficit.Count == measured.Length && planeDeficit.Values.All(d => d >= CommonMerDb * k))
        {
            double common = Median(planeDeficit.Values.ToList());
            yield return new Symptom(Kind.CommonMer, measured, s, common >= 6 ? Severity.Warning : Severity.Hint,
                L.F("MER aller Ebenen im Mittel {0:0.0} dB unter dem besten bisher gemessenen Wert je Transponder (umgerechnet auf Schüssel und Wetter).", common));
        }

        // Single planes worse than the others
        if (planeDeficit.Count >= 3)
        {
            var worse = planeDeficit.Where(p => p.Value - Median(planeDeficit.Where(o => o.Key != p.Key).Select(o => o.Value).ToList()) >= PlaneMerDb * k)
                .Select(p => p.Key).ToArray();
            foreach (var group in Group(worse, measured))
            {
                double d = Median(group.Select(p => planeDeficit[p]).ToList());
                double others = Median(planeDeficit.Where(p => !group.Contains(p.Key)).Select(p => p.Value).ToList());
                yield return new Symptom(Kind.PlaneMer, group, s, d - others >= 5 ? Severity.Warning : Severity.Hint,
                    L.F("MER im Mittel {0:0.0} dB unter dem Bestwert je Transponder, die anderen Ebenen {1:0.0} dB.", d, others));
            }
        }

        // Single transponders far below their plane although the level is fine
        var dips = new List<Point>();
        foreach (var (plane, pts) in withRef)
        {
            double level = Median(pts.Select(x => x.Level).ToList());
            dips.AddRange(pts.Where(x => x.Deficit >= planeDeficit[plane] + DipDb * k && x.Level >= 52 && x.Level >= level - 6));
        }
        var all = withRef.Values.SelectMany(p => p).ToList();

        // Clustered in the band of a radio service → ingress
        foreach (var (name, from, to) in RadioBands)
        {
            bool InBand(Point p) => p.If + Bandwidth(p.R) / 2 > from && p.If - Bandwidth(p.R) / 2 < to;
            var inBand = all.Where(InBand).ToList();
            var hit = dips.Where(InBand).ToList();
            int outside = all.Count - inBand.Count;
            if (hit.Count < 2 || hit.Count < inBand.Count * 0.5) continue;
            if (outside > 0 && (double)(dips.Count - hit.Count) / outside > 0.2) continue;
            yield return new Symptom(Kind.Ingress, hit.Select(x => x.Plane).Distinct().OrderBy(PlaneOrder).ToArray(), s, Severity.Hint,
                L.F("{0} von {1} Transpondern im Bereich {2}–{3} MHz ZF mit MER-Einbruch: {4}.", hit.Count, inBand.Count, from, to, List(hit)), name);
            dips.RemoveAll(InBand);
        }

        // Scattered dips on a plane → reflections from a mismatch (kinked cable, open tap, moisture)
        var reflecting = withRef.Keys.Where(p => dips.Count(x => x.Plane == p) is var k && k >= 3 && k >= withRef[p].Count * 0.12).ToArray();
        foreach (var group in Group(reflecting, measured, merge: 3))
        {
            var hit = dips.Where(x => group.Contains(x.Plane)).OrderBy(x => x.R.FrequencyMHz).ToList();
            int n = group.Sum(p => withRef[p].Count);
            yield return new Symptom(Kind.Reflections, group, s, hit.Count >= n * 0.25 ? Severity.Warning : Severity.Hint,
                L.F("{0} von {1} Transpondern mit MER-Einbruch um mehr als {2:0.#} dB bei normalem Pegel: {3}.", hit.Count, n, DipDb, List(hit)));
        }

        // Worse where a transponder of the other polarization sits on the same frequency → poor cross-polar isolation
        var coChannel = all.Where(p => s.Results.Any(o => o.Polarization != p.R.Polarization
            && Math.Abs(o.FrequencyMHz - p.R.FrequencyMHz) <= (Bandwidth(o) + Bandwidth(p.R)) / 8)).ToList();
        var clear = all.Except(coChannel).ToList();
        if (coChannel.Count >= 6 && clear.Count >= 6)
        {
            double withNeighbour = Median(coChannel.Select(x => x.Deficit!.Value).ToList());
            double without = Median(clear.Select(x => x.Deficit!.Value).ToList());
            if (withNeighbour - without >= 2.5 * k)
                yield return new Symptom(Kind.CrossPol, measured, s, Severity.Hint,
                    L.F("Transponder mit Gegenstück auf der anderen Polarisation {0:0.0} dB schlechter als die übrigen ({1} gegen {2} Transponder).",
                        withNeighbour - without, coChannel.Count, clear.Count));
        }
    }

    // ---------------------------------------------------------------- several scans

    private static IEnumerable<Finding> CompareScans(List<ScanSession> sessions, Reference reference, Site site)
    {
        if (sessions.Select(s => s.Weather).Distinct().Count() > 1)
            yield return new Finding(Severity.Ok, L.T("Unterschiedliches Wetter"),
                L.F("Die Scans stammen von verschiedenem Wetter ({0}). Die MER wird darauf umgerechnet, Regen und Schnee bleiben aber eine Unsicherheit.",
                    string.Join(", ", sessions.Where(s => s.Weather.HasValue).Select(s => WeatherInfo.Text(s.Weather!.Value)).Distinct())),
                "", L.T("Für genaue Vergleiche bei gleichem Wetter messen."));

        var outlets = sessions.Where(s => s.LocationKind == LocationKind.Socket && s.Planes == "alle").ToList();

        // An outlet with clearly less or more level than the others → its cable
        var levels = outlets.Select(s => (Session: s, Level: s.Results.Where(r => r.Locked && r.LevelDbuV.HasValue).Select(r => r.LevelDbuV!.Value).DefaultIfEmpty().Average()))
            .Where(x => x.Level > 0).ToList();
        if (levels.Count >= 3)
        {
            foreach (var (session, level) in levels)
            {
                double others = Median(levels.Where(l => l.Session != session).Select(l => l.Level).ToList());
                double dev = level - others;
                if (Math.Abs(dev) < OutletLevelDb) continue;
                yield return dev < 0
                    ? new Finding(Severity.Hint, L.F("{0}: weniger Pegel als die anderen Dosen", session.LocationLabel),
                        L.F("Ø {0:0.0} dBµV, {1:0.0} dB unter dem Median der anderen Dosen ({2:0.0} dBµV).", level, -dev, others),
                        L.T("Dosenleitung (Länge, Kabelqualität), zusätzlicher Verteiler oder Durchgangsdose, Stecker dieser Dose."),
                        site.Multiswitch is MultiswitchKind.Passive or MultiswitchKind.None
                            ? L.T("Leitung und Stecker prüfen; ohne Verstärkung im Multischalter brauchen lange Leitungen besseres Kabel oder einen Verstärker.")
                            : MultiswitchTexts.IsAdjustable(site.MultiswitchLevel)
                                ? L.T("Leitung und Stecker prüfen; reicht der Pegel nicht, am Multischalter höher regeln – das hebt meist alle Dosen an.")
                                : L.T("Leitung und Stecker prüfen."))
                    : new Finding(Severity.Hint, L.F("{0}: mehr Pegel als die anderen Dosen", session.LocationLabel),
                        L.F("Ø {0:0.0} dBµV, {1:0.0} dB über dem Median der anderen Dosen ({2:0.0} dBµV).", level, dev, others),
                        L.T("Kurze Dosenleitung oder höher eingestellter Multischalter-Ausgang."),
                        level > 70
                            ? L.F("Für diese Dose ein Dämpfungsglied von ca. {0:0} dB vorsehen – ein Regler am Multischalter wirkt meist auf alle Dosen.", Math.Max(3, Math.Round(level - 66)))
                            : L.T("Unbedenklich, solange der Pegel unter 70 dBµV bleibt."));
            }
        }

        // More level but worse MER across the outlets → overload
        foreach (var plane in Plane.All)
        {
            var rows = outlets.Select(s =>
            {
                var pts = s.Results.Where(r => r.Locked && !r.ProbablyInactive && Plane.Of(r) == plane && r.LevelDbuV.HasValue)
                    .Select(r => (Level: r.LevelDbuV!.Value, Deficit: reference.Deficit(s, r))).Where(x => x.Deficit.HasValue).ToList();
                return pts.Count < 5 ? default : (Session: s, Level: pts.Average(x => x.Level), Deficit: Median(pts.Select(x => x.Deficit!.Value).ToList()));
            }).Where(x => x.Session is not null).ToList();
            if (rows.Count < 3 || rows.Max(x => x.Level) < 70) continue;
            var (r, slope) = Regression(rows.Select(x => x.Level).ToList(), rows.Select(x => x.Deficit).ToList());
            if (r < 0.6 || slope < 0.15) continue;
            var hot = rows.Where(x => x.Level >= 70).OrderByDescending(x => x.Level).ToList();
            yield return new Finding(Severity.Hint, L.F("{0}: mehr Pegel, aber schlechtere MER – Übersteuerung", plane),
                L.F("Je 1 dB mehr Pegel rund {0:0.0} dB weniger MER über {1} Dosen. Am höchsten: {2}.", slope, rows.Count,
                    string.Join(", ", hot.Select(x => L.F("{0} ({1:0.0} dBµV, MER −{2:0.0} dB)", x.Session.LocationLabel, x.Level, x.Deficit)))),
                L.T("Multischalter-Ausgang zu hoch eingestellt – oder Eingangsstufe des Empfängers übersteuert."),
                LowerLevel(site, hot[0].Level - 66) + " " + L.T("Danach erneut messen."));
        }

        // Same location measured several times: MER should repeat within a dB or two
        foreach (var repeat in sessions.GroupBy(s => (s.LocationKind, s.LocationName)).Where(g => g.Count() >= 2))
        {
            var list = repeat.ToList();
            var spreads = list[0].Results.Where(r => r.Locked && r.MerDb.HasValue)
                .Select(r => list.Select(s => s.Results.FirstOrDefault(o => o.Polarization == r.Polarization && Math.Abs(o.FrequencyMHz - r.FrequencyMHz) <= 2))
                    .Where(o => o is { Locked: true, MerDb: not null }).Select(o => o!.MerDb!.Value).ToList())
                .Where(m => m.Count == list.Count).Select(m => m.Max() - m.Min()).ToList();
            int flips = list[0].Results.Count(r => list.Skip(1).Any(s => s.Results.FirstOrDefault(o => o.Polarization == r.Polarization
                && Math.Abs(o.FrequencyMHz - r.FrequencyMHz) <= 2) is { } o && o.Locked != r.Locked && !o.ProbablyInactive && !r.ProbablyInactive));
            double spread = spreads.Count > 0 ? Median(spreads) : 0;
            if (spread < 2 && flips < 3) continue;
            yield return new Finding(Severity.Hint, L.F("{0}: Messwerte schwanken", list[0].LocationLabel),
                L.F("{0} Messungen am selben Ort: MER schwankt je Transponder typisch um {1:0.0} dB, {2} Transponder mal mit, mal ohne Lock.", list.Count, spread, flips),
                L.T("Wackelkontakt oder Feuchtigkeit in einem Stecker – oder das Wetter hat sich zwischen den Messungen geändert."),
                L.T("Stecker und Kabel bewegen, während die Live-Messung läuft: Springt die MER, ist die Stelle gefunden."));
        }
    }

    // ---------------------------------------------------------------- location and advice

    /// <param name="group">The same symptom in one or more scans.</param>
    /// <param name="weaker">The symptom below the reporting threshold in further scans – counts for locating.</param>
    private static Finding Locate(List<Symptom> group, List<Symptom> weaker, List<ScanSession> sessions, Site site)
    {
        var first = group[0];
        string scope = ScopeName(first.Planes);
        string evidence = sessions.Count == 1
            ? first.Evidence
            : string.Join("\n", group.Select(g => $"{g.Session.LocationLabel}: {g.Evidence}"));
        var weakerSessions = weaker.Select(w => w.Session).Distinct().ToList();
        if (weakerSessions.Count > 0)
            evidence += "\n" + L.F("Schwächer ausgeprägt auch: {0}.", string.Join(", ", weakerSessions.Select(s => s.LocationLabel)));
        var hit = group.Select(g => g.Session).Concat(weakerSessions).ToList();
        return new Finding(group.Max(g => g.Severity), Title(first.Kind, scope, first.Extra), evidence,
            Where(first.Kind, first.Planes, hit, sessions, site), NextStep(first.Kind, first.Planes, site, first.Extra, group.Max(g => g.Amount)));
    }

    private static string Title(Kind kind, string scope, string extra) => kind switch
    {
        Kind.NoSignal => L.F("{0}: kein Signal", scope),
        Kind.WrongPlane => L.F("{0}: Pegel da, aber kein Empfang – falsche Ebene?", scope),
        Kind.HighPass => L.F("{0}: tiefe Frequenzen gedämpft (Hochpass)", scope),
        Kind.Imbalance => L.F("{0}: Pegel weicht von den anderen Ebenen ab", scope),
        Kind.LowLevel => L.F("{0}: Pegel zu niedrig", scope),
        Kind.HighLevel => L.F("{0}: Pegel sehr hoch", scope),
        Kind.PlaneMer => L.F("{0}: MER schlechter als auf den anderen Ebenen", scope),
        Kind.CommonMer => L.T("MER auf allen Ebenen unter den Bestwerten"),
        Kind.Ingress => L.F("{0}: Störung im Bereich {1} – Einstrahlung?", scope, extra),
        Kind.Reflections => L.F("{0}: MER-Einbrüche bei einzelnen Frequenzen", scope),
        Kind.CrossPol => L.T("Schlechte Polarisationsentkopplung"),
        Kind.Tilt => extra == "down"
            ? L.T("Pegel fällt zu hohen Frequenzen ab – Kabeldämpfung")
            : L.T("Pegel steigt zu hohen Frequenzen an – Schräglage überkompensiert?"),
        _ => scope,
    };

    private static string Where(Kind kind, Plane[] planes, List<ScanSession> hit, List<ScanSession> sessions, Site site)
    {
        if (kind == Kind.CrossPol)
            return L.T("LNB-Skew (Verdrehung im Halter) oder LNB mit schlechter Polarisationsentkopplung.");

        // Scans that measured these planes at all
        var candidates = sessions.Where(s => planes.All(p => s.Results.Count(r => Plane.Of(r) == p) >= 3)).ToList();
        var ports = candidates.Where(s => s.LocationKind == LocationKind.LnbPort).ToList();
        var outlets = candidates.Where(s => s.LocationKind == LocationKind.Socket).ToList();
        int hitPorts = ports.Count(hit.Contains), hitOutlets = outlets.Count(hit.Contains);
        bool allPlanes = planes.Length == 4;

        if (candidates.Count <= 1)
            return PathOf(planes, hit[0].LocationKind, site);
        if (hitPorts > 0)
            return L.T("Schon am LNB-Port: LNB, Schüssel oder Ausrichtung – oder Kabel und Stecker bis zum Messpunkt.");
        if (outlets.Count >= 2 && hitOutlets >= Math.Max(2, outlets.Count * CommonShare))
        {
            string count = hitOutlets == outlets.Count
                ? L.F("An allen {0} Dosen", outlets.Count)
                : L.F("An {0} von {1} Dosen", hitOutlets, outlets.Count);
            if (ports.Count > 0)
                return L.F("{0}, aber nicht am LNB-Port: zwischen LNB und Multischalter – Stammleitung, F-Stecker oder Multischalter-Eingang {1}.", count, ScopeName(planes));
            return allPlanes
                ? L.F("{0}: gemeinsamer Teil der Anlage – Schüssel, LNB, Stammleitungen oder Multischalter.", count)
                : L.F("{0}: vor den Multischalter-Ausgängen – LNB-Ausgang, Stammleitung, F-Stecker oder Multischalter-Eingang {1}.", count, ScopeName(planes));
        }
        if (outlets.Count >= 2 && hitOutlets > 0)
            return L.F("Nur an {0} von {1} Dosen ({2}): Dosenleitung, Dose oder Multischalter-Ausgang dieser Dosen.",
                hitOutlets, outlets.Count, string.Join(", ", outlets.Where(hit.Contains).Select(s => s.LocationName)));
        return PathOf(planes, hit[0].LocationKind, site);
    }

    /// <summary>Where a fault on these planes sits along a single signal path (plane matrix).</summary>
    private static string PathOf(Plane[] planes, LocationKind kind, Site site)
    {
        if (kind == LocationKind.Socket && planes.Length == 1 && site.Multiswitch == MultiswitchKind.None)
            return L.F("Nur Ebene {0}: Ohne Multischalter führt das Kabel alle Ebenen – die Ursache liegt im LNB.", planes[0]);
        if (kind == LocationKind.LnbPort)
            return planes.Length == 1
                ? L.F("LNB-Ausgang {0} – oder Kabel und Stecker bis zum Messpunkt.", planes[0])
                : L.T("Schüssel, Ausrichtung oder LNB.");
        return planes.Length switch
        {
            1 => L.F("Nur Ebene {0}: im Zweig dieser Ebene vor dem Multischalter – LNB-Ausgang, Stammleitung, F-Stecker oder Multischalter-Eingang.", planes[0]),
            2 when planes[0].HighBand == planes[1].HighBand =>
                L.F("Beide Polarisationen im {0}-Band: LNB-Oszillator dieses Bandes oder Band-Umschaltung (22 kHz) im Multischalter.", planes[0].HighBand ? "High" : "Low"),
            2 when planes[0].Polarization == planes[1].Polarization =>
                L.F("Beide Bänder {0}: Polarisationszweig im LNB, Skew oder die Stammleitungen dieser Polarisation.", PolarizationName(planes[0].Polarization)),
            _ => L.T("Alle Ebenen: gemeinsamer Weg – Dosenleitung, Dose, Anschlusskabel – oder Schüssel und LNB. Weitere Messorte ankreuzen, um das zu trennen."),
        };
    }

    private static string NextStep(Kind kind, Plane[] planes, Site site, string extra, double amount) => kind switch
    {
        Kind.NoSignal => L.T("LNB-Ausgang, Stammleitung und Multischalter-Eingang dieser Ebene prüfen; liefert der Multischalter die LNB-Versorgung?"),
        Kind.WrongPlane => site.LnbType switch
        {
            LnbType.Quattro => L.T("Belegung der Multischalter-Eingänge prüfen (VL/HL/VH/HH vertauscht?) – am LNB-Port nachmessen."),
            _ when LnbTypes.IsUniversal(site.LnbType) && site.MultiswitchQuad == false =>
                L.T("Der Multischalter ist laut Einstellung nicht quadtauglich – mit einem Quad-LNB kommen an seinen Eingängen falsche Ebenen an. Ein Quattro-LNB ist nötig."),
            _ when LnbTypes.IsUniversal(site.LnbType) && site.MultiswitchQuad == true =>
                L.T("Kabel an den Multischalter-Eingängen prüfen: Der Multischalter schaltet jeden LNB-Ausgang auf die Ebene seines Eingangs – steckt ein Kabel lose oder fehlt die Steuerspannung?"),
            _ when LnbTypes.IsUniversal(site.LnbType) => L.T("Ein Quad-LNB braucht einen quadtauglichen Multischalter, der jeden Ausgang auf die Ebene seines Eingangs schaltet – sonst kommt die falsche Ebene an."),
            _ => L.T("Belegung der Multischalter-Eingänge prüfen (VL/HL/VH/HH vertauscht?) – am LNB-Port nachmessen. Bei einem Quad-LNB: Ist der Multischalter quadtauglich?"),
        },
        Kind.HighPass => planes.Length == 1
            ? L.T("F-Stecker der Leitung prüfen: Innenleiter 1–2 mm überstehend, keine Korrosion, fest verschraubt.") + " " + Isolate(planes[0], site)
            : L.T("F-Stecker an Dose und Anschlusskabel prüfen. Der Frequenzgang des Messgeräts kann einen Teil erklären – am LNB-Port gegenmessen."),
        Kind.Imbalance => site.Multiswitch == MultiswitchKind.None
            ? L.T("Ohne Multischalter führt das Kabel alle Ebenen – ein Unterschied zwischen den Ebenen entsteht im LNB.")
            : site.MultiswitchLevel switch
            {
                LevelControl.PerPlane => L.T("Der Multischalter regelt je Ebene: den Pegel dieser Ebene angleichen. Zeigt die Ebene noch andere Auffälligkeiten, zuerst die Leitung prüfen."),
                LevelControl.PerBand => L.T("Der Multischalter regelt nur je Band: Weicht nur eine Polarisation ab, hilft der Regler nicht – die Ursache liegt vor dem Multischalter (LNB-Ausgang, Stammleitung, Eingang). Weichen beide Polarisationen eines Bandes ab, den Regler dieses Bandes anpassen."),
                LevelControl.Common => L.T("Der Multischalter regelt alle Ebenen gemeinsam – ein Unterschied zwischen den Ebenen lässt sich damit nicht ausgleichen. Die Ursache liegt vor dem Multischalter (LNB-Ausgang, Stammleitung, Eingang)."),
                LevelControl.None => L.T("Der Multischalter ist nicht regelbar – ein Unterschied zwischen den Ebenen kommt vom LNB oder den Stammleitungen."),
                _ => L.T("Liegen auf dieser Ebene noch andere Auffälligkeiten vor, zuerst die Leitung prüfen; sonst den Pegel am Multischalter oder Verstärker angleichen."),
            },
        Kind.LowLevel => site.Multiswitch switch
        {
            MultiswitchKind.Passive => L.T("Passiver Multischalter: Der Pegel kommt nur vom LNB und sinkt mit jedem Meter Kabel und jedem Abzweig – Leitung kürzen, besseres Kabel oder einen aktiven Multischalter bzw. Verstärker einsetzen."),
            MultiswitchKind.None => L.T("Ohne Multischalter kommt der Pegel direkt vom LNB: Leitung kürzen, besseres Kabel oder einen Inline-Verstärker einsetzen."),
            _ when MultiswitchTexts.IsAdjustable(site.MultiswitchLevel) => L.T("Ausgangspegel am Multischalter höher regeln (Ziel 55–70 dBµV an der Dose); reicht das nicht, die Leitung verbessern."),
            _ when site.MultiswitchLevel == LevelControl.None => L.T("Der Multischalter ist nicht regelbar: Leitung verbessern oder einen Verstärker einsetzen; Ziel 55–70 dBµV an der Dose."),
            _ => L.T("Verstärkung erhöhen oder Leitung verbessern; Ziel 55–70 dBµV an der Dose."),
        },
        Kind.HighLevel => LowerLevel(site, amount - 68),
        Kind.PlaneMer => L.T("Am LNB-Port dieser Ebene messen: Ist die MER dort gut, liegt der Fehler in Leitung oder Multischalter."),
        Kind.CommonMer => L.T("Schüssel fein ausrichten (Live-Messung, auf maximale MER), Skew prüfen, LNB und Schüssel auf Verschmutzung prüfen."),
        Kind.Ingress => L.T("Kabel, Dosen und Anschlusskabel mit Schirmungsklasse A/A+ verwenden, offene Ausgänge mit 75 Ω abschließen, Stecker festziehen."),
        Kind.Reflections => L.T("Leitung auf Knicke, Quetschungen und lose oder schlecht montierte Stecker prüfen; offene Abzweige mit 75 Ω abschließen.")
                            + (planes.Length == 1 ? " " + Isolate(planes[0], site) : ""),
        Kind.CrossPol => L.T("Skew nach dem Wert in den Standort-Einstellungen einstellen; hilft das nicht, den LNB tauschen."),
        Kind.Tilt => (extra, site.MultiswitchSlope) switch
        {
            ("down", true) => L.T("Schräglage (Slope) am Multischalter erhöhen."),
            ("down", false) => L.T("Der Multischalter hat keine Slope-Regelung: Kabel mit geringerer Dämpfung oder einen Verstärker mit Entzerrung einsetzen."),
            ("down", _) => L.T("Schräglage (Slope) am Multischalter erhöhen, falls vorhanden – sonst Kabel mit geringerer Dämpfung oder einen Verstärker mit Entzerrung einsetzen."),
            (_, true) => L.T("Schräglage (Slope) am Multischalter zurücknehmen."),
            _ => L.T("Prüfen, ob ein Verstärker oder Multischalter mit Entzerrung zu stark eingestellt ist. Ein Teil kann vom Frequenzgang des Messgeräts kommen – am LNB-Port gegenmessen."),
        },
        _ => "",
    };

    /// <summary>How to bring the level down by about this many dB with the multiswitch at hand.</summary>
    private static string LowerLevel(Site site, double excessDb)
    {
        double db = Math.Max(3, Math.Round(excessDb));
        return site.MultiswitchLevel switch
        {
            LevelControl.PerPlane => L.F("Pegel dieser Ebene am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV).", db),
            LevelControl.PerBand => L.F("Pegel am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV) – der Regler wirkt auf beide Polarisationen des Bandes.", db),
            LevelControl.Common => L.F("Ausgangspegel am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV) – der Regler wirkt auf alle Ebenen.", db),
            LevelControl.None => L.F("Der Multischalter ist nicht regelbar: ein Dämpfungsglied von ca. {0:0} dB vor die Dose oder den Receiver setzen.", db),
            _ when site.Multiswitch is MultiswitchKind.Passive or MultiswitchKind.None => L.F("Ein Dämpfungsglied von ca. {0:0} dB vor die Dose oder den Receiver setzen.", db),
            _ => L.F("Pegel um ca. {0:0} dB senken (Ziel 60–70 dBµV) – am Regler des Multischalters oder mit einem Dämpfungsglied.", db),
        };
    }

    /// <summary>
    /// How to tell cable from LNB and multiswitch for a fault on one plane. A quattro LNB has one fixed plane per
    /// output, so its trunk lines must be swapped at both ends; a quad LNB on a quad-capable multiswitch gets its
    /// plane from the input, so swapping at the inputs is enough.
    /// </summary>
    private static string Isolate(Plane plane, Site site) => site.LnbType switch
    {
        _ when site.Multiswitch == MultiswitchKind.None =>
            L.T("Der LNB hängt ohne Multischalter am Receiver: Jedes Kabel führt alle Ebenen – ein Fehler auf nur einer Ebene liegt im LNB."),
        LnbType.Quattro => L.F("Zum Eingrenzen die Stammleitungen {0} und {1} an beiden Enden tauschen (am LNB und am Multischalter) – nur an einem Ende getauscht, käme an der Dose die falsche Ebene an. Wandert der Fehler zu {1}, liegt er im Kabel oder an dessen Steckern; bleibt er bei {0}, am LNB-Ausgang oder Multischalter-Eingang – dann die Leitung am Multischalter abziehen und direkt messen (Messort LNB-Port).", plane, Sister(plane)),
        LnbType.Unicable => L.T("Bei Unicable führt ein Kabel alle Ebenen: Ein Fehler auf nur einer Ebene liegt im LNB bzw. im Unicable-Umsetzer."),
        _ when LnbTypes.IsUniversal(site.LnbType) => L.F("Zum Eingrenzen am Multischalter die Kabel an den Eingängen {0} und {1} tauschen – ein quadtauglicher Multischalter schaltet jeden LNB-Ausgang auf die Ebene seines Eingangs. Wandert der Fehler zu {1}, liegt er im Kabel oder am LNB-Ausgang; bleibt er bei {0}, am Multischalter-Eingang. Hängt der LNB ohne Multischalter am Receiver, führt jedes Kabel alle Ebenen: Ein Fehler auf nur einer Ebene liegt dann im LNB.", plane, Sister(plane)),
        _ => L.F("Zum Eingrenzen bei einem Quattro-LNB die Stammleitungen {0} und {1} an beiden Enden tauschen (am LNB und am Multischalter); bei einem Quad-LNB am quadtauglichen Multischalter reicht der Tausch an den Eingängen. Wandert der Fehler zu {1}, liegt er im Kabel; bleibt er bei {0}, am LNB-Ausgang oder Multischalter-Eingang. Mit dem LNB-Typ in den Standort-Einstellungen wird dieser Hinweis genauer.", plane, Sister(plane)),
    };

    // ---------------------------------------------------------------- helpers

    /// <summary>Best MER per transponder over all stored scans, converted to the dish and weather of the scan in question.</summary>
    private sealed class Reference(IEnumerable<ScanSession> sessions)
    {
        private readonly ILookup<(Polarization, int), (ScanSession Session, ScanResult Result)> _byTp = sessions
            .SelectMany(s => s.Results.Where(r => r.Locked && r.MerDb.HasValue).Select(r => (s, r)))
            .ToLookup(x => (x.r.Polarization, x.r.FrequencyMHz));

        private readonly Dictionary<(ScanSession, int, int), double> _expected = [];

        public double? Deficit(ScanSession session, ScanResult r)
        {
            if (r.MerDb is not { } mer) return null;
            double? best = null;
            for (int f = r.FrequencyMHz - 2; f <= r.FrequencyMHz + 2; f++)
                foreach (var (s, other) in _byTp[(r.Polarization, f)])
                {
                    double value = other.MerDb!.Value + Correction(s, session, r);
                    if (best is null || value > best) best = value;
                }
            // Without a second scan of the transponder there is nothing to compare
            return best is null || _byTp[(r.Polarization, r.FrequencyMHz)].All(x => x.Session == session) ? null : Math.Round(best.Value - mer, 1);
        }

        private double Correction(ScanSession from, ScanSession to, ScanResult r) =>
            from == to || from.Site is null || to.Site is null ? 0 : Expected(to, r) - Expected(from, r);

        private double Expected(ScanSession s, ScanResult r)
        {
            var key = (s, r.FrequencyMHz, r.SymbolRateKs);
            if (!_expected.TryGetValue(key, out var mer))
                _expected[key] = mer = LinkBudget.Expect(s.Site!, s.SatelliteLongitude, r.FrequencyMHz, r.SymbolRateKs, s.Weather ?? Weather.PartlyCloudy).MerDb;
            return mer;
        }
    }

    /// <summary>Combines planes by the plane matrix: both of a band, both of a polarization, or all.</summary>
    private static IEnumerable<Plane[]> Group(Plane[] planes, Plane[] measured, int merge = 4)
    {
        if (planes.Length == 0) yield break;
        if (planes.Length >= Math.Min(merge, measured.Length) && measured.Length >= 3)
        {
            yield return measured;
            yield break;
        }
        if (planes.Length == 2 && (planes[0].HighBand == planes[1].HighBand || planes[0].Polarization == planes[1].Polarization))
        {
            yield return planes.OrderBy(PlaneOrder).ToArray();
            yield break;
        }
        foreach (var p in planes) yield return [p];
    }

    private static string ScopeName(Plane[] planes) => planes.Length switch
    {
        4 => L.T("Alle Ebenen"),
        1 => planes[0].ToString(),
        _ => string.Join(" + ", planes.Select(p => p.ToString())),
    };

    private static string PolarizationName(Polarization p) => p == Polarization.Horizontal ? L.T("horizontal") : L.T("vertikal");

    private static Plane Sister(Plane p) =>
        new(p.HighBand, p.Polarization == Polarization.Horizontal ? Polarization.Vertical : Polarization.Horizontal);

    private static int PlaneOrder(Plane p) => Array.IndexOf(Plane.All, p);

    private static string List(List<Point> points) =>
        string.Join(", ", points.OrderBy(x => x.R.FrequencyMHz).Select(x => $"{x.R.FrequencyMHz} {x.R.PolarizationShort}"));

    /// <summary>Occupied bandwidth in MHz (roll-off 0.35).</summary>
    private static double Bandwidth(ScanResult r) => r.SymbolRateKs / 1000.0 * 1.35;

    private static int IfOf(ScanResult r) => r.FrequencyMHz - (r.FrequencyMHz >= 11700 ? 10600 : 9750);

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.Order().ToList();
        int mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }

    /// <summary>Correlation coefficient and slope of y over x.</summary>
    private static (double R, double Slope) Regression(List<double> x, List<double> y)
    {
        double mx = x.Average(), my = y.Average();
        double sxy = x.Zip(y).Sum(p => (p.First - mx) * (p.Second - my));
        double sxx = x.Sum(v => (v - mx) * (v - mx)), syy = y.Sum(v => (v - my) * (v - my));
        return sxx == 0 || syy == 0 ? (0, 0) : (sxy / Math.Sqrt(sxx * syy), sxy / sxx);
    }
}
