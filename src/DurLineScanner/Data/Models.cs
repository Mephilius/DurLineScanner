namespace DurLineScanner.Data;

public enum Polarization
{
    Horizontal,
    Vertical,
}

/// <summary>Satellit mit LNB-Oszillatorfrequenzen (MHz).</summary>
public sealed record Satellite(int Id, string Name, int LofLow, int LofHigh, double Longitude)
{
    public override string ToString() => Name;
}

/// <summary>
/// Transponder: Frequenz in MHz, Symbolrate in kSym/s. System/Modulation nur bekannt, wenn die Liste
/// aus einer satellites.xml stammt (ältere Messreihen enthalten sie nicht).
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
