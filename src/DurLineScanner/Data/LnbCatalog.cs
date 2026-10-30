using System.Globalization;

namespace DurLineScanner.Data;

/// <summary>
/// LNB model with the values the manufacturer publishes (datasheet or product page). Missing values were not found
/// and are left out rather than estimated.
/// </summary>
/// <param name="GainMin">Lower end of the conversion gain; alone it means "at least".</param>
/// <param name="GainMax">Upper end; alone it means "at most".</param>
/// <param name="NoiseFigure">Noise figure as published, e.g. "0.2", "&lt; 0.3", "max. 1.0" (dot as decimal separator).</param>
/// <param name="Note">Addition such as "Monoblock 6°" or the number of Unicable user bands.</param>
public sealed record LnbModel(
    string Manufacturer,
    string Model,
    LnbType Type,
    double? GainMin = null,
    double? GainMax = null,
    string NoiseFigure = "",
    string Note = "")
{
    /// <summary>Gain to prefill: the middle of a range, otherwise the single value.</summary>
    public double? Gain => GainMin is { } min && GainMax is { } max ? (min + max) / 2 : GainMin ?? GainMax;

    public string GainText => (GainMin, GainMax) switch
    {
        ({ } min, { } max) when min == max => L.F("{0:0.#} dB", min),
        ({ } min, { } max) => L.F("{0:0.#}–{1:0.#} dB", min, max),
        ({ } min, null) => L.F("mind. {0:0.#} dB", min),
        (null, { } max) => L.F("max. {0:0.#} dB", max),
        _ => "",
    };

    public string NoiseText => NoiseFigure.Length == 0
        ? ""
        : NoiseFigure.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator) + " dB";

    /// <summary>One line for the settings window, e.g. "Quad · Verstärkung 60–70 dB · Rauschmaß 0,2 dB".</summary>
    public string Summary => string.Join(" · ", new[]
    {
        LnbTypes.Text(Type) + (Note.Length > 0 ? $" ({Note})" : ""),
        GainText.Length > 0 ? L.F("Verstärkung {0}", GainText) : "",
        NoiseText.Length > 0 ? L.F("Rauschmaß {0}", NoiseText) : "",
    }.Where(s => s.Length > 0)) + " – " + L.T("Herstellerangabe");

    public override string ToString() => Model;
}

/// <summary>
/// Common LNBs of the last 20 years with manufacturer data (researched 10/2026). Only values from datasheets or the
/// manufacturer's product pages; models known only from shop listings are not included. Many manufacturers advertise
/// 0.1 dB noise figure – that is a marketing value, realistic Ku-band LNBs reach about 0.3–0.7 dB.
/// </summary>
public static class LnbCatalog
{
    private const LnbType S = LnbType.Single, T = LnbType.Twin, Q = LnbType.Quad, O = LnbType.Octo,
        QT = LnbType.Quattro, U = LnbType.Unicable;

    public static readonly IReadOnlyList<LnbModel> All =
    [
        // Axing (axing.com)
        new("Axing", "SCO 4-10", QT, 55, 55, "0.5"),
        new("Axing", "SCO 4-11", Q, 55, 55, "0.5"),
        new("Axing", "SCO 2-24", U, 60, 60, "0.1", "dCSS, 24 UB + 1 legacy"),

        // DUR-line (dur-line.de, ELV datasheets)
        new("DUR-line", "Blue ECO Single", S, 54, 66, "0.1"),
        new("DUR-line", "Blue ECO Twin", T, 54, 66, "0.1"),
        new("DUR-line", "Blue ECO Quad", Q, 54, 60, "0.1"),
        new("DUR-line", "Blue ECO Quattro", QT, 54, 60, "0.1"),
        new("DUR-line", "Blue ECO Octo", O, 60, 60, "0.1"),
        new("DUR-line", "+Ultra Single", S, 52, 65, "0.1"),
        new("DUR-line", "+Ultra Twin", T, 52, 65, "0.1"),
        new("DUR-line", "+Ultra Quad", Q, 52, 65, "0.1"),
        new("DUR-line", "+Ultra Quattro", QT, 52, 65, "0.1"),
        new("DUR-line", "+Ultra Octo", O, 52, 65, "0.1"),
        new("DUR-line", "UK 104", U, 52, 65, "0.1", "4 SCR + 3 legacy"),
        new("DUR-line", "UK 124", U, null, 65, "0.1", "dCSS, 24 UB"),
        new("DUR-line", "UK 124-3L", U, null, null, "0.1", "dCSS, 24 UB + 3 legacy"),
        new("DUR-line", "MB6-US", S, 50, 65, "0.1", "Monoblock 6°"),
        new("DUR-line", "MB6-TW", T, 53, 68, "0.1", "Monoblock 6°"),
        new("DUR-line", "MB6-QS", Q, 53, 68, "0.1", "Monoblock 6°"),
        new("DUR-line", "MB6-UK", U, null, 55, "", "Monoblock, dCSS"),

        // Edision (edision.gr, gain as minimum)
        new("Edision", "GT-SLRC2", S, 57, null, "0.1"),
        new("Edision", "GT-TL1", T, 60, null, "0.1"),
        new("Edision", "GT-TL2", T, 57, null, "0.1"),
        new("Edision", "TL-2 TWIN SE", T, 55, null, "0.1"),
        new("Edision", "GT-QDL2", Q, 57, null, "0.1"),
        new("Edision", "QSL-4 QUAD SE", Q, null, null, "0.1"),
        new("Edision", "GT-QTL2", QT, 57, null, "0.1"),
        new("Edision", "GT-OCL1", O, 60, null, "0.1"),

        // Fracarro (fracarro.com, no noise figure published)
        new("Fracarro", "UX-S LTE", S, 60, 60),
        new("Fracarro", "UX-SN LTE", S, 60, 60),
        new("Fracarro", "UX-TW LTE", T, 55, 55),
        new("Fracarro", "UX-TW LTE FR", T, 60, 60),
        new("Fracarro", "UX-QD LTE", Q, 55, 55),
        new("Fracarro", "UX-QT LTE", QT, 57, 57),
        new("Fracarro", "UX-QTN LTE", QT, 57, 57),
        new("Fracarro", "UX-OCTO LTE", O, 55, 55),
        new("Fracarro", "UX-MBS6 LTE", S, 55, 55, "", "Monoblock 6°"),
        new("Fracarro", "SCD2-16LNB", U, 65, 65, "", "4 SCR + 12 dCSS"),

        // Fuba (datasheets via Zajadacz)
        new("Fuba", "DEK 117", S, 52, 65),
        new("Fuba", "DEK 217", T, 47, 49),
        new("Fuba", "DEK 417", Q, 47, 49),
        new("Fuba", "DEK 417 C", Q, 55, 55, "0.5"),
        new("Fuba", "DEK 407", QT, 47, 49),
        new("Fuba", "DEK 407 C", QT, 55, 55, "0.5"),
        new("Fuba", "DEK 817", O, 50, 52),
        new("Fuba", "DEK 124", U, 55, 55, "0.9", "24 UB"),
        new("Fuba", "DEK 584", U, 55, 55, "0.6", "SCR + 4 legacy"),

        // Global Invacom
        new("Global Invacom", "Universal dCSS LNB", U, null, null, "0.5", "dCSS, 24/32 UB"),

        // Inverto (inverto.tv)
        new("Inverto", "IDLT-SNL412-ULTRA-OPN", S, 60, 70, "0.2"),
        new("Inverto", "IDLT-TWL412-ULTRA-OPN", T, 60, 70, "0.2"),
        new("Inverto", "IDLT-QDL412-ULTRA-OPN", Q, 60, 70, "0.2"),
        new("Inverto", "IDLT-QTL412-ULTRA-OPN", QT, 60, 70, "0.2"),
        new("Inverto", "IDLB-TWNL40-ULTRA-OPP (Black Ultra)", T, 60, null, "0.2"),
        new("Inverto", "IDLB-QUDL40-ULTRA-OPP (Black Ultra)", Q, 60, null, "0.2"),
        new("Inverto", "IDLB-QUTL40-ULTRA-OPP (Black Ultra)", QT, 60, null, "0.2"),
        new("Inverto", "IDLB-TWNL40-PREMU-OPP (Black Premium)", T, 55, null, "0.2"),
        new("Inverto", "IDLP-SNL413-PREMU-OPN", S, 57, 67, "0.2"),
        new("Inverto", "IDLP-TWL413-PREMU-OPN", T, 57, 67, "0.2"),
        new("Inverto", "IDLP-QDL413-PREMU-OPN", Q, 57, 67, "0.2"),
        new("Inverto", "IDLP-QTL413-PREMU-OPN", QT, 57, 67, "0.2"),
        new("Inverto", "IDLP-SNL413-HMPRO-OPX (Home Pro)", S, 58, 65, "0.4"),
        new("Inverto", "IDLP-TWL413-HMPRO-OPX (Home Pro)", T, 55, 65, "0.4"),
        new("Inverto", "IDLP-QDL413-HMPRO-OPN (Home Pro)", Q, 55, null, "0.3"),
        new("Inverto", "IDLP-QTL413-HMPRO-OPN (Home Pro)", QT, 55, null, "0.3"),
        new("Inverto", "IDLH-OCT410-HMPRO-OPN (Home Pro)", O, 55, null, "0.7"),
        new("Inverto", "IDLU-32L412-UNBRR-OPN", U, 55, null, "max. 1.0", "dCSS, 32 UB"),
        new("Inverto", "IDLM-TWM610-MNOO3-8PN", T, 50, 60, "0.2", "Monoblock 3°"),
        new("Inverto", "IDLR-SINS45-CLASC-OSP (Classic)", S, 52, 62),

        // Kathrein (datasheets give no noise figure)
        new("Kathrein", "UAS 571", S, 50, null),
        new("Kathrein", "UAS 584", QT, 50, null),
        new("Kathrein", "UAS 684", QT, 50, null),
        new("Kathrein", "UAS 685", Q, 50, null),

        // Megasat (megasat.tv)
        new("Megasat", "HD-Profi Single", S, 60, 60, "0.1"),
        new("Megasat", "HD-Profi Twin", T, 60, 60, "0.1"),
        new("Megasat", "HD-Profi Quad", Q, 60, 60, "0.1"),
        new("Megasat", "HD-Profi Quattro", QT, 60, 60, "0.1"),
        new("Megasat", "HD-Profi Octo", O, 60, 60, "0.1"),
        new("Megasat", "Diavolo Single", S, 60, 60, "0.1"),
        new("Megasat", "Diavolo Twin", T, 60, 60, "0.1"),
        new("Megasat", "Diavolo Quad", Q, 60, 60, "0.1"),
        new("Megasat", "Diavolo Quattro", QT, 60, 60, "0.1"),
        new("Megasat", "Diavolo Octo", O, 60, 60, "0.1"),
        new("Megasat", "High Gain Single", S, 70, 70, "0.1"),
        new("Megasat", "High Gain Twin", T, 70, 70, "0.1"),
        new("Megasat", "Unicable LNB Diavolo", U, 56, 62, "0.1", "4 SCR + 2 legacy"),
        new("Megasat", "Unicable II LNB Diavolo", U, 60, 60, "0.1", "JESS, 24 UB"),
        new("Megasat", "Monoblock Single Diavolo", S, 60, 60, "0.1", "Monoblock"),
        new("Megasat", "Monoblock Twin Diavolo", T, 60, 60, "0.1", "Monoblock"),
        new("Megasat", "Monoblock Quad Diavolo", Q, 60, 60, "0.1", "Monoblock"),

        // Polytron (polytron.de)
        new("Polytron", "OSP-AP 10", S, null, 60, "< 0.4"),
        new("Polytron", "OSP-AP 20", T, null, 60, "< 0.3"),
        new("Polytron", "OSP-AP 40", Q, null, 60, "< 0.3"),
        new("Polytron", "OSP-AP 95", QT, null, 60, "< 0.3"),

        // Schwaiger (product information sheets, no noise figure)
        new("Schwaiger", "SPS7944 531 (Sun Protect)", Q, 55, null),
        new("Schwaiger", "SPS6918 531 (Sun Protect)", QT, 55, null),

        // Sharp (LNB line-up 2006, 8th generation)
        new("Sharp", "Universal Single (8. Generation)", S, null, null, "0.6"),
        new("Sharp", "Universal Twin (8. Generation)", T, null, null, "0.6"),
        new("Sharp", "Universal Quad (8. Generation)", Q, null, null, "0.6"),
        new("Sharp", "Universal Quattro (8. Generation)", QT, null, null, "0.6"),

        // Smart (catalogue sheet)
        new("Smart", "Titanium TS", S, 50, 50, "0.1"),
        new("Smart", "Titanium TT", T, null, null, "0.1"),
        new("Smart", "Titanium TQ", QT, null, null, "0.1"),

        // TechniSat (technisat.de; TechniSat calls a quad "Quattro-Switch")
        new("TechniSat", "Universal-Twin-LNB schwarz (0000/8382)", T, 58, 64, "0.9"),
        new("TechniSat", "Universal-Twin-LNB weiß (0020/8882)", T, 58, 64, "0.1"),
        new("TechniSat", "UNYSAT Universal-Twin-LNB (0010/8382)", T, 58, 64, "0.9"),
        new("TechniSat", "Universal-Quattro-Switch-LNB schwarz (0020/8980)", Q, 55, 67, "0.9"),
        new("TechniSat", "Quattro-Switch-LNB 40 mm (0001/7980)", Q, 60, 60, "0.3"),
        new("TechniSat", "UNYSAT Universal-Quattro-Switch-LNB (0010/8980)", Q, 60, 65, "0.75"),
        new("TechniSat", "Universal-Quattro-LNB schwarz (0000/8380)", QT, 60, 67, "0.75"),
        new("TechniSat", "UNYSAT Universal-Quattro-LNB (0010/8880)", QT, 60, 67, "0.75"),
        new("TechniSat", "SlimFeed Universal-Quattro-LNB (0009/8880)", QT, 60, 64, "0.1"),
        new("TechniSat", "Universal-Octo-LNB (0000/8398)", O, 58, 65, "1.0"),
        new("TechniSat", "UNYSAT Universal-Octo-LNB (0010/8988)", O, 58, 65, "1.0"),
        new("TechniSat", "Universal-SCR 4+4 LNB (0000/8388)", U, 57, 57, "0.7", "4 UB + 4 legacy"),
        new("TechniSat", "Universal-SCR 8+2 LNB (0000/8312)", U, 55, null, "1.0", "8 UB + 2 legacy"),
        new("TechniSat", "UNYSAT Universal-SCR 8+6 LNB (0010/8986)", U, 55, 55, "0.8", "8 UB + 6 legacy"),

        // Televes (televes.com)
        new("Televes", "7475", S, 58, 58, "0.3"),
        new("Televes", "747802", T, 60, 60, "0.3"),
        new("Televes", "747701 (LNB44)", QT, 60, 60, "0.3"),
        new("Televes", "7613", O, 60, 60, "0.3"),
        new("Televes", "747321 dLNB", U, 50, null, "", "dCSS, 30 UB"),
        new("Televes", "747322 dLNB", U, 50, null, "", "8 SCR/dCSS + 8 dCSS"),

        // Telestar (telestar.de)
        new("Telestar", "SKYSINGLE HC", S, 60, 60, "0.1"),
        new("Telestar", "SKYTWIN HC", T, 60, 60, "0.1"),
        new("Telestar", "SKYQUAD HC", Q, 60, 60, "0.1"),
        new("Telestar", "SKYQUATRO HC", QT, 60, 60, "0.1"),
        new("Telestar", "SKYOCTO HC", O, 60, 60, "0.1"),
        new("Telestar", "SKYWIRE HC", U, 60, 60, "0.1", "4 SCR"),

        // Triax (datasheets)
        new("Triax", "TSI 100 Gold", S, 57, 65, "max. 0.3"),
        new("Triax", "TQD 404 Gold", Q, 53, 68, "max. 0.3"),
        new("Triax", "TQDD", Q, 55, 65, "0.3"),
        new("Triax", "TQT", QT, 55, 65),
        new("Triax", "TQTD", QT, 55, 65, "0.3"),

        // Wisi (datasheets via Zajadacz)
        new("Wisi", "OC 01 E", S, 55, null, "0.3"),
        new("Wisi", "OC 02 E", T, 55, null, "0.3"),
        new("Wisi", "OC 06 E", Q, 55, null, "0.3"),
    ];

    public static IReadOnlyList<string> Manufacturers { get; } = All.Select(m => m.Manufacturer).Distinct().Order(StringComparer.OrdinalIgnoreCase).ToList();

    public static IEnumerable<LnbModel> Of(string manufacturer) => All.Where(m => m.Manufacturer == manufacturer);

    public static LnbModel? Find(string manufacturer, string model) =>
        All.FirstOrDefault(m => m.Manufacturer == manufacturer && m.Model == model);
}
