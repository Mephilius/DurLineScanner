namespace DurLineScanner.Data;

/// <summary>
/// Satellites and transponders from the transponder list (satellites.xml): updated or imported
/// copy (downloaded from OpenPLi on first start).
/// </summary>
public sealed class TransponderDatabase
{
    public static TransponderDatabase OpenDefault() => new() { Catalog = SatellitesXml.LoadUserCopy() };

    public SatellitesXml? Catalog { get; set; }

    public string SourceDescription(Satellite satellite) => Catalog?.Description ?? L.T("keine Transponderliste");

    public IReadOnlyList<Satellite> GetSatellites() => Catalog?.Satellites ?? [];

    public IReadOnlyList<Transponder> GetTransponders(Satellite satellite)
    {
        bool cBand = satellite.LofLow < 8000;
        var all = Catalog?.GetTransponders((int)Math.Round(satellite.Longitude * 10)) ?? [];
        return Order(all.Where(t => SatellitesXml.IsCBand(t.FrequencyMHz) == cBand));
    }

    private static List<Transponder> Order(IEnumerable<Transponder> transponders) =>
        // Remove duplicate entries; ordering by band, polarization, frequency minimizes LNB switching
        transponders
            .DistinctBy(t => (t.FrequencyMHz, t.Polarization))
            .OrderBy(t => t.FrequencyMHz >= 11700)
            .ThenBy(t => t.Polarization)
            .ThenBy(t => t.FrequencyMHz)
            .ToList();
}
