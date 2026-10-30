namespace DurLineScanner.Scanning;

/// <summary>
/// Picks, per plane, the transponder best suited as reference for live level measurement:
/// reliable lock, high MER, level close to the plane median (i.e. no outlier) and IF in mid-band,
/// so the cable tilt does not skew the value.
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
        return r.MerDb!.Value                                   // main criterion: clean signal
               - 0.5 * Math.Abs(r.LevelDbuV!.Value - medianLevel) // representative of the plane
               - Math.Abs(ifMHz - IfCenterMHz) / 300             // prefer mid-band
               - Math.Min(r.LockTimeSeconds ?? 0, 5) * 0.3;      // fast lock = stable
    }
}
