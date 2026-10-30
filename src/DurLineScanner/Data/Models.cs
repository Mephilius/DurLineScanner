namespace DurLineScanner.Data;

public enum Polarization
{
    Horizontal,
    Vertical,
}

/// <summary>Satellite with LNB oscillator frequencies (MHz).</summary>
public sealed record Satellite(int Id, string Name, int LofLow, int LofHigh, double Longitude)
{
    public override string ToString() => Name;
}

/// <summary>
/// Transponder: frequency in MHz, symbol rate in kSym/s. System/modulation only known if the list
/// comes from a satellites.xml (older measurement series do not contain them).
/// </summary>
public sealed record Transponder(
    int FrequencyMHz,
    Polarization Polarization,
    int SymbolRateKs,
    string? DvbSystem = null,
    string? Modulation = null,
    string? Fec = null)
{
    public string PolarizationShort => Polarization == Polarization.Horizontal ? "H" : "V";

    public override string ToString() => $"{FrequencyMHz} {PolarizationShort} {SymbolRateKs}";
}
