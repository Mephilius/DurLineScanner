namespace DurLineScanner.Scanning;

/// <summary>
/// Wählt pro Ebene den Transponder, der sich am besten als Referenz für die Live-Pegelmessung eignet:
/// sicherer Lock, hohe MER, Pegel nahe am Ebenen-Median (also kein Ausreißer) und ZF in der Bandmitte,
/// damit die Kabel-Schräglage den Wert nicht verfälscht.
/// </summary>
public static class ReferenceSelector
{
    private const double IfCenterMHz = 1550;

    public static ScanResult? Suggest(Plane plane, IEnumerable<ScanResult> results)
    {
        var candidates = results
            .Where(r => Plane.Of(r) == plane && r.Locked && r.MerDb.HasValue && r.LevelDbuV.HasValue)
            .ToList();
        if (candidates.Count == 0) return null;

        var levels = candidates.Select(r => r.LevelDbuV!.Value).Order().ToList();
        double median = levels[levels.Count / 2];

        return candidates.MaxBy(r => Score(r, plane, median));
    }

    public static double Score(ScanResult r, Plane plane, double medianLevel)
    {
        double ifMHz = r.FrequencyMHz - (plane.HighBand ? 10600 : 9750);
        return r.MerDb!.Value                                   // Hauptkriterium: sauberes Signal
               - 0.5 * Math.Abs(r.LevelDbuV!.Value - medianLevel) // repräsentativ für die Ebene
               - Math.Abs(ifMHz - IfCenterMHz) / 300             // Bandmitte bevorzugen
               - Math.Min(r.LockTimeSeconds ?? 0, 5) * 0.3;      // schneller Lock = stabil
    }
}
