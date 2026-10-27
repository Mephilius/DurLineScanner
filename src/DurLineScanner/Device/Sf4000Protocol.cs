using DurLineScanner.Data;

namespace DurLineScanner.Device;

/// <summary>
/// Befehlskodierung und Antwortauswertung des SF 4000 BT. Details: docs/PROTOKOLL.md.
/// </summary>
public static class Sf4000Protocol
{
    public const byte Prefix = 0xA5;
    public const byte CmdIdentify = 0xF0;
    public const byte CmdTune = 0xF2;
    public const byte CmdStatus = 0xF3;
    public const byte CmdDetailOn = 0xF5;
    public const byte CmdDetailOff = 0xF6;

    public const byte RespondOk = 0xB0;

    private const int IfMin = 950;
    private const int IfMax = 2150;
    private const int UniversalSwitchMHz = 11700;

    public static byte[] Command(byte code, params byte[] payload) => [Prefix, code, .. payload];

    /// <summary>
    /// Ermittelt 22-kHz-Band und ZF für Universal- bzw. C-Band-LNB.
    /// Gibt null zurück, wenn der Transponder mit diesem LNB nicht empfangbar ist.
    /// </summary>
    public static (bool HighBand, int IfMHz)? GetBandAndIf(int frequencyMHz, int lofLow, int lofHigh)
    {
        static bool InRange(int ifq) => ifq is >= IfMin and <= IfMax;

        if (lofHigh < 8000)
        {
            // C-Band: ZF = LOF - Frequenz
            if (InRange(lofLow - frequencyMHz)) return (false, lofLow - frequencyMHz);
            if (InRange(lofHigh - frequencyMHz)) return (lofHigh != lofLow, lofHigh - frequencyMHz);
            return null;
        }

        (bool High, int Lof)[] candidates = frequencyMHz >= UniversalSwitchMHz
            ? [(true, lofHigh), (false, lofLow)]
            : [(false, lofLow), (true, lofHigh)];
        foreach (var (high, lof) in candidates)
        {
            if (InRange(frequencyMHz - lof))
                return (high && lofHigh != lofLow, frequencyMHz - lof);
        }
        return null;
    }

    /// <summary>
    /// A5F2 + 4 Byte: Bit31 Pol (1 = H), Bit30 22 kHz, Bit29..16 ZF (MHz), Bit15..0 Symbolrate (kS/s).
    /// </summary>
    public static byte[]? EncodeTune(Transponder tp, int lofLow, int lofHigh)
    {
        if (tp.SymbolRateKs is < 1000 or > 45000) return null;
        var band = GetBandAndIf(tp.FrequencyMHz, lofLow, lofHigh);
        if (band is null) return null;

        uint word = (tp.Polarization == Polarization.Horizontal ? 1u << 31 : 0)
                    | (band.Value.HighBand ? 1u << 30 : 0)
                    | ((uint)band.Value.IfMHz << 16)
                    | (uint)tp.SymbolRateKs;
        return Command(CmdTune, (byte)(word >> 24), (byte)(word >> 16), (byte)(word >> 8), (byte)word);
    }

    /// <summary>Wertet eine A5F3-Antwort aus (3 oder 15 Nutzbytes).</summary>
    public static StatusReading? ParseStatus(ReadOnlySpan<byte> data)
    {
        if (data.Length < 5 || data[0] != Prefix || data[1] != CmdStatus) return null;
        var reading = new StatusReading(data[2], data[3], data[4]);
        if (data.Length >= 17)
        {
            // Detailwerte: die letzten 12 Bytes, big-endian
            var d = data[^12..];
            reading = reading with
            {
                CnRaw = (d[0] << 8) | d[1],
                LevelRaw = (d[2] << 8) | d[3],
                MerRaw = ReadUInt32(d[4..8]),
                BerRaw = ReadUInt32(d[8..12]),
            };
        }
        return reading;
    }

    private static long ReadUInt32(ReadOnlySpan<byte> b) =>
        ((long)b[0] << 24) | ((long)b[1] << 16) | ((long)b[2] << 8) | b[3];
}

/// <summary>Messwerte einer A5F3-Abfrage. Umrechnungen in Anzeigewerte wie am Gerät.</summary>
public sealed record StatusReading(byte State, int StrengthRaw, int Quality)
{
    public const byte StateNoTransponder = 0x01;
    public const byte StateLocked = 0x02;
    public const byte StateSearching = 0x04;

    public int? CnRaw { get; init; }
    public int? LevelRaw { get; init; }
    public long? MerRaw { get; init; }
    public long? BerRaw { get; init; }

    public bool IsLocked => State == StateLocked;

    /// <summary>Signalstärke in %, mit bereichsabhängigem Korrekturaufschlag.</summary>
    public int StrengthPercent
    {
        get
        {
            int i = StrengthRaw;
            i += i switch
            {
                > 90 => 5,
                > 80 => 8,
                > 70 => 9,
                > 60 => 8,
                > 50 => 11,
                > 0 => 13,
                _ => 0,
            };
            return Math.Min(i, 100);
        }
    }

    public double? CnDb => CnRaw / 10.0;

    public double? MerDb => MerRaw / 10.0;

    /// <summary>Pegel in dBµV mit bereichsabhängiger Korrektur.</summary>
    public double? LevelDbuV
    {
        get
        {
            if (LevelRaw is not { } j) return null;
            j += j switch
            {
                > 900 => 50,
                > 800 => 80,
                > 700 => 90,
                > 600 => 80,
                > 500 => 110,
                > 0 => 130,
                _ => 0,
            };
            return j / 10.0;
        }
    }

    /// <summary>
    /// Bitfehlerrate: Ziffern als Mantisse, Stellenzahl-1 als negativer Exponent.
    /// Unter 300 gilt als &lt; 1E-7, über 950000 als &gt; 1E-1.
    /// </summary>
    public double? Ber
    {
        get
        {
            if (BerRaw is not { } j) return null;
            if (j < 300) return 1e-8;
            if (j > 950000) return 1e-1;
            int exponent = j.ToString().Length - 1;
            double mantissa = j / Math.Pow(10, exponent);
            return mantissa * Math.Pow(10, -exponent);
        }
    }
}
