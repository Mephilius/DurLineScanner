namespace DurLineScanner.Scanning;

/// <summary>Bewertungsstufe eines Messwerts.</summary>
public enum Grade
{
    VeryBad,
    Bad,
    Marginal,
    Good,
    VeryGood,
}

/// <summary>Bereich [From, To) einer Skala mit Bewertung und Bedeutung.</summary>
public sealed record GradeBand(double From, double To, Grade Grade, string Meaning);

/// <summary>Skala eines Messwerts: Anzeigebereich, Bewertungsbereiche und Erklärung.</summary>
public sealed record MetricScale(
    string Title,
    string Unit,
    double Min,
    double Max,
    IReadOnlyList<GradeBand> Bands,
    string Explanation,
    string Basis)
{
    /// <summary>Wandelt den angezeigten Skalenwert in Text um (z.B. BER-Exponent → "1E-5").</summary>
    public Func<double, string> Format { get; init; } = v => v.ToString("0.#");

    /// <summary>Eigene Bereichsbeschriftung, z.B. für die umgekehrte BER-Skala.</summary>
    public Func<GradeBand, string>? RangeFormatter { get; init; }

    /// <summary>false: Wert wird nur angezeigt, nicht bewertet (keine Farbbereiche).</summary>
    public bool Graded { get; init; } = true;

    public Grade? Evaluate(double? value)
    {
        if (value is not { } v || !Graded) return null;
        foreach (var band in Bands)
        {
            if (v >= band.From && v < band.To) return band.Grade;
        }
        return v < Bands[0].From ? Bands[0].Grade : Bands[^1].Grade;
    }

    /// <summary>Bereichstext für den Tooltip, z.B. "60 – 74 dBµV" oder "unter 47 dBµV".</summary>
    public string RangeText(GradeBand band)
    {
        if (RangeFormatter is not null) return RangeFormatter(band);
        string unit = Unit.Length > 0 ? " " + Unit : "";
        if (band.From == double.MinValue) return L.F("unter {0}{1}", Format(band.To), unit);
        if (band.To == double.MaxValue) return L.F("ab {0}{1}", Format(band.From), unit);
        return $"{Format(band.From)} – {Format(band.To)}{unit}";
    }
}

/// <summary>
/// Richtwerte für die Bewertung von Sat-ZF-Signalen.
///
/// MER und C/N: Empfangsschwelle (QEF, quasi fehlerfrei) nach ETSI EN 302 307-1 Tab. 13 (DVB-S2) bzw.
/// EN 300 421 (DVB-S, Eb/N0 nach Viterbi in Es/N0 umgerechnet). Reale Empfänger brauchen etwa 1 dB mehr.
/// Bewertung nach Reserve über der Schwelle.
///
/// Pegel: Bereich an der Teilnehmerdose nach EN 60728-1 für DVB-S/S2: 47–77 dBµV.
/// Der Pegel des SF 4000 ist nicht kalibriert.
/// </summary>
public static class SignalLimits
{
    public const double MarginUnstable = 1;
    public const double MarginMarginal = 3;
    public const double MarginGood = 6;

    // Es/N0 in dB für QEF
    private static readonly Dictionary<(string System, string Modulation, string Fec), double> Thresholds = new()
    {
        [("DVB-S", "QPSK", "1/2")] = 4.1,
        [("DVB-S", "QPSK", "2/3")] = 5.9,
        [("DVB-S", "QPSK", "3/4")] = 6.9,
        [("DVB-S", "QPSK", "5/6")] = 7.9,
        [("DVB-S", "QPSK", "7/8")] = 8.4,

        [("DVB-S2", "QPSK", "1/2")] = 1.0,
        [("DVB-S2", "QPSK", "3/5")] = 2.2,
        [("DVB-S2", "QPSK", "2/3")] = 3.1,
        [("DVB-S2", "QPSK", "3/4")] = 4.0,
        [("DVB-S2", "QPSK", "4/5")] = 4.7,
        [("DVB-S2", "QPSK", "5/6")] = 5.2,
        [("DVB-S2", "QPSK", "8/9")] = 6.2,
        [("DVB-S2", "QPSK", "9/10")] = 6.4,
        [("DVB-S2", "8PSK", "3/5")] = 5.5,
        [("DVB-S2", "8PSK", "2/3")] = 6.6,
        [("DVB-S2", "8PSK", "3/4")] = 7.9,
        [("DVB-S2", "8PSK", "5/6")] = 9.4,
        [("DVB-S2", "8PSK", "8/9")] = 10.7,
        [("DVB-S2", "8PSK", "9/10")] = 11.0,
        [("DVB-S2", "16APSK", "2/3")] = 9.0,
        [("DVB-S2", "16APSK", "3/4")] = 10.2,
        [("DVB-S2", "16APSK", "4/5")] = 11.0,
        [("DVB-S2", "16APSK", "5/6")] = 11.6,
        [("DVB-S2", "16APSK", "8/9")] = 12.9,
        [("DVB-S2", "16APSK", "9/10")] = 13.1,
        [("DVB-S2", "32APSK", "3/4")] = 12.7,
        [("DVB-S2", "32APSK", "4/5")] = 13.6,
        [("DVB-S2", "32APSK", "5/6")] = 14.3,
        [("DVB-S2", "32APSK", "8/9")] = 15.7,
        [("DVB-S2", "32APSK", "9/10")] = 16.1,
    };

    /// <summary>
    /// Empfangsschwelle in dB und deren Beschreibung. Fehlende Angaben werden vorsichtig ergänzt:
    /// ohne FEC die höchste übliche Coderate der Modulation, ganz ohne Angaben DVB-S2 8PSK 3/4.
    /// </summary>
    public static (double Db, string Description) Threshold(string? system, string? modulation, string? fec)
    {
        if (system is null && modulation is not null && modulation != "QPSK") system = "DVB-S2";
        if (modulation is null && system == "DVB-S") modulation = "QPSK";

        if (system is not null && modulation is not null && fec is not null
            && Thresholds.TryGetValue((system, modulation, fec), out var exact))
            return (exact, $"{system} {modulation} {fec}");

        if (system is not null && modulation is not null)
        {
            var worst = Thresholds.Where(kv => kv.Key.System == system && kv.Key.Modulation == modulation)
                .Select(kv => kv.Value).Where(v => v <= 11).DefaultIfEmpty(7.9).Max();
            return (worst, L.F("{0} {1}, FEC unbekannt", system, modulation));
        }
        return (7.9, L.T("Modulation unbekannt, angenommen DVB-S2 8PSK 3/4"));
    }

    public static MetricScale MerScale(double thresholdDb, string basis) => SnrScale(
        "MER", thresholdDb, basis,
        L.T("Modulation Error Ratio – wie sauber die Signalpunkte beim Empfänger ankommen. Der wichtigste Wert " +
            "für die Empfangsqualität und zum Ausrichten der Schüssel: auf maximale MER einstellen. Ob ein Wert " +
            "reicht, hängt von der Modulation ab – bewertet wird die Reserve über der Empfangsschwelle des " +
            "Transponders. Die Reserve fängt Regen und Schnee ab (Regendämpfung bis ca. 3–5 dB)."));

    public static MetricScale CnScale(double thresholdDb, string basis) => SnrScale(
        "C/N", thresholdDb, basis,
        L.T("Carrier-to-Noise – Abstand des Signals zum Rauschen. Ähnlich der MER, erfasst aber nur das Rauschen, " +
            "keine Verzerrungen. Liegt C/N deutlich über der MER, stören eher Reflexionen, Übersteuerung oder " +
            "schlechte Verbindungen als zu wenig Signal."));

    private static MetricScale SnrScale(string title, double t, string basis, string explanation) => new(
        title, "dB", 0, Math.Max(20, Math.Ceiling((t + MarginGood + 6) / 5) * 5),
        [
            new(double.MinValue, t, Grade.VeryBad, L.T("unter der Empfangsschwelle – kein Bild")),
            new(t, t + MarginUnstable, Grade.Bad, L.T("an der Schwelle – Aussetzer, Klötzchen")),
            new(t + MarginUnstable, t + MarginMarginal, Grade.Marginal, L.T("Empfang ohne Reserve – fällt bei Regen aus")),
            new(t + MarginMarginal, t + MarginGood, Grade.Good, L.T("stabiler Empfang mit Regenreserve")),
            new(t + MarginGood, double.MaxValue, Grade.VeryGood, L.T("reichlich Reserve, auch bei Unwetter")),
        ],
        explanation,
        L.F("Empfangsschwelle {0:0.0} dB für {1} (ETSI EN 302 307 / EN 300 421, Reserve +1 / +3 / +6 dB)", t, basis));

    /// <summary>Allgemeine MER/C/N-Erklärung ohne konkreten Transponder (Tabellenköpfe).</summary>
    public static MetricScale GenericSnr(string title) => title == "MER"
        ? MerScale(Threshold(null, null, null).Db, L.T("DVB-S2 8PSK 3/4 als Beispiel"))
        : CnScale(Threshold(null, null, null).Db, L.T("DVB-S2 8PSK 3/4 als Beispiel"));

    /// <summary>Beispielschwellen für den Tooltip.</summary>
    public static string ThresholdExamples => L.T(
        "Empfangsschwellen: DVB-S QPSK 3/4: 6,9 dB · DVB-S2 QPSK 3/4: 4,0 dB · 8PSK 2/3: 6,6 dB · " +
        "8PSK 3/4: 7,9 dB · 8PSK 5/6: 9,4 dB");

    public static readonly MetricScale Level = new(
        L.T("Pegel"), "dBµV", 30, 90,
        [
            new(double.MinValue, 40, Grade.VeryBad, L.T("viel zu wenig Signal")),
            new(40, 47, Grade.Bad, L.T("unter der Norm – Empfänger kommt evtl. nicht mehr mit")),
            new(47, 52, Grade.Marginal, L.T("knapp – keine Reserve für weitere Dosen/Kabel")),
            new(52, 60, Grade.Good, L.T("ausreichend")),
            new(60, 74, Grade.VeryGood, L.T("optimaler Bereich")),
            new(74, 77, Grade.Marginal, L.T("hoch – noch zulässig")),
            new(77, 82, Grade.Bad, L.T("zu hoch – Übersteuerung möglich")),
            new(82, double.MaxValue, Grade.VeryBad, L.T("viel zu hoch – Übersteuerung, Verstärkung senken")),
        ],
        L.T("Signalspannung am Eingang des Messgeräts. Zu wenig Pegel verschlechtert die MER, zu viel übersteuert " +
            "Empfänger und Multischalter. Die vier ZF-Ebenen sollten auf wenige dB gleich sein. Hinweis: Der Pegel " +
            "des SF 4000 ist nicht kalibriert – Vergleiche zwischen Dosen, Ebenen und Messungen sind aussagekräftiger " +
            "als die Absolutwerte."),
        L.T("EN 60728-1: 47–77 dBµV an der Teilnehmerdose (DVB-S/S2)"));

    public static readonly MetricScale Quality = new(
        L.T("Qualität"), "%", 0, 100,
        [
            new(double.MinValue, 20, Grade.VeryBad, L.T("kein brauchbarer Empfang")),
            new(20, 40, Grade.Bad, L.T("Aussetzer wahrscheinlich")),
            new(40, 60, Grade.Marginal, L.T("grenzwertig")),
            new(60, 80, Grade.Good, L.T("gut")),
            new(80, double.MaxValue, Grade.VeryGood, L.T("sehr gut (bei sauberem Signal 98 %)")),
        ],
        L.T("Geräteeigene Qualitätsanzeige des SF 4000. Hängt eng mit der MER " +
            "zusammen, ist aber weniger fein abgestuft – für genaue Beurteilung die MER verwenden."),
        L.T("Geräteanzeige SF 4000"));

    public static readonly MetricScale Strength = new(
        L.T("Stärke"), "%", 0, 100,
        [
            new(double.MinValue, 20, Grade.VeryBad, L.T("fast kein Signal")),
            new(20, 30, Grade.Bad, L.T("sehr schwach")),
            new(30, 45, Grade.Marginal, L.T("schwach")),
            new(45, 60, Grade.Good, L.T("ausreichend")),
            new(60, double.MaxValue, Grade.VeryGood, L.T("kräftig")),
        ],
        L.T("Geräteeigene Signalstärke des SF 4000. Zeigt nur, dass " +
            "Signal ankommt – nicht, ob es sauber ist. Auch ein falscher Satellit kann viel Stärke haben."),
        L.T("Geräteanzeige SF 4000"));

    /// <summary>Bitfehlerrate vor der Fehlerkorrektur, als Exponent (-log10) dargestellt: Skala 1E-1 … 1E-8.</summary>
    public static readonly MetricScale BerExponent = new MetricScale(
        "BER", "", 0, 8,
        [
            new(double.MinValue, 1, Grade.VeryBad, L.T("Fehlerkorrektur überfordert – kein Bild")),
            new(1, 2, Grade.Bad, L.T("an der Grenze der Fehlerkorrektur")),
            new(2, 3, Grade.Marginal, L.T("viele Fehler, Korrektur arbeitet stark")),
            new(3, 4, Grade.Good, L.T("gut")),
            new(4, double.MaxValue, Grade.VeryGood, L.T("sehr gut")),
        ],
        L.T("Bitfehlerrate vor der Fehlerkorrektur: Anteil falsch empfangener Bits. Die Fehlerkorrektur des " +
            "Empfängers repariert diese Fehler bis etwa 1E-2. Je kleiner der Wert (mehr Nullen hinter dem Komma), " +
            "desto besser. Reagiert träger als die MER."),
        L.T("vor Fehlerkorrektur (Viterbi/LDPC)"))
    {
        Format = v => v <= 0 ? "1" : $"1E-{v:0}",
        RangeFormatter = b =>
            b.From == double.MinValue ? L.F("über 1E-{0:0}", b.To)
            : b.To == double.MaxValue ? L.F("unter 1E-{0:0}", b.From)
            : $"1E-{b.From:0} … 1E-{b.To:0}",
    };

    /// <summary>
    /// Bei DVB-S2 liefert das SF 4000 BER-Werte, die nicht zur MER passen (z.B. 4E-2 bei MER 17 dB) –
    /// vermutlich eine andere interne Größe. Daher nur anzeigen, nicht bewerten.
    /// </summary>
    public static readonly MetricScale BerUngraded = BerExponent with
    {
        Graded = false,
        Explanation = BerExponent.Explanation + " " + L.T("Bei DVB-S2-Transpondern meldet das SF 4000 Werte, die nicht zur " +
                      "gemessenen MER passen; die BER wird dort deshalb nicht bewertet – maßgeblich ist die MER."),
        Basis = L.T("bei DVB-S2 nicht bewertet (Gerätewert nicht plausibel)"),
    };

    public static MetricScale BerScale(string? system) => system == "DVB-S2" ? BerUngraded : BerExponent;

    public static double? BerToExponent(double? ber) => ber is > 0 ? -Math.Log10(ber.Value) : null;

    public static string GradeText(Grade grade) => grade switch
    {
        Grade.VeryBad => L.T("sehr schlecht"),
        Grade.Bad => L.T("schlecht"),
        Grade.Marginal => L.T("grenzwertig"),
        Grade.Good => L.T("gut"),
        _ => L.T("sehr gut"),
    };
}
