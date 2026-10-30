namespace DurLineScanner.Localization;

/// <summary>English texts for analysis, data, device and controls.</summary>
public static partial class L
{
    static partial void AddCore(Dictionary<string, string> d)
    {
        // ---------------------------------------------------------------- Rating (SignalLimits)
        d["sehr schlecht"] = "very poor";
        d["schlecht"] = "poor";
        d["grenzwertig"] = "marginal";
        d["gut"] = "good";
        d["sehr gut"] = "very good";
        d["Sehr gut"] = "Very good";

        d["unter {0}{1}"] = "below {0}{1}";
        d["ab {0}{1}"] = "{0}{1} and above";
        d["über 1E-{0:0}"] = "above 1E-{0:0}";
        d["unter 1E-{0:0}"] = "below 1E-{0:0}";

        d["{0} {1}, FEC unbekannt"] = "{0} {1}, FEC unknown";
        d["Modulation unbekannt, angenommen DVB-S2 8PSK 3/4"] = "Modulation unknown, assuming DVB-S2 8PSK 3/4";
        d["DVB-S2 8PSK 3/4 als Beispiel"] = "DVB-S2 8PSK 3/4 as an example";

        d["Modulation Error Ratio – wie sauber die Signalpunkte beim Empfänger ankommen. Der wichtigste Wert für die Empfangsqualität und zum Ausrichten der Schüssel: auf maximale MER einstellen. Ob ein Wert reicht, hängt von der Modulation ab – bewertet wird die Reserve über der Empfangsschwelle des Transponders. Die Reserve fängt Regen und Schnee ab (Regendämpfung bis ca. 3–5 dB)."] =
            "Modulation Error Ratio – how cleanly the signal points arrive at the receiver. The most important value for reception quality and for aligning the dish: adjust for maximum MER. Whether a value is sufficient depends on the modulation – what is rated is the margin above the transponder's reception threshold. The margin absorbs rain and snow (rain fade up to approx. 3–5 dB).";
        d["Carrier-to-Noise – Abstand des Signals zum Rauschen. Ähnlich der MER, erfasst aber nur das Rauschen, keine Verzerrungen. Liegt C/N deutlich über der MER, stören eher Reflexionen, Übersteuerung oder schlechte Verbindungen als zu wenig Signal."] =
            "Carrier-to-Noise – distance of the signal from the noise. Similar to MER, but covers only noise, not distortion. If C/N is well above the MER, the problem is more likely reflections, overload or bad connections than too little signal.";

        d["unter der Empfangsschwelle – kein Bild"] = "below the reception threshold – no picture";
        d["an der Schwelle – Aussetzer, Klötzchen"] = "at the threshold – dropouts, blocking";
        d["Empfang ohne Reserve – fällt bei Regen aus"] = "reception without margin – fails in rain";
        d["stabiler Empfang mit Regenreserve"] = "stable reception with rain margin";
        d["reichlich Reserve, auch bei Unwetter"] = "plenty of margin, even in severe weather";
        d["Empfangsschwelle {0:0.0} dB für {1} (ETSI EN 302 307 / EN 300 421, Reserve +1 / +3 / +6 dB)"] =
            "Reception threshold {0:0.0} dB for {1} (ETSI EN 302 307 / EN 300 421, margin +1 / +3 / +6 dB)";

        d["Empfangsschwellen: DVB-S QPSK 3/4: 6,9 dB · DVB-S2 QPSK 3/4: 4,0 dB · 8PSK 2/3: 6,6 dB · 8PSK 3/4: 7,9 dB · 8PSK 5/6: 9,4 dB"] =
            "Reception thresholds: DVB-S QPSK 3/4: 6.9 dB · DVB-S2 QPSK 3/4: 4.0 dB · 8PSK 2/3: 6.6 dB · 8PSK 3/4: 7.9 dB · 8PSK 5/6: 9.4 dB";

        d["Pegel"] = "Level";
        d["viel zu wenig Signal"] = "far too little signal";
        d["unter der Norm – Empfänger kommt evtl. nicht mehr mit"] = "below the standard – receiver may no longer cope";
        d["knapp – keine Reserve für weitere Dosen/Kabel"] = "tight – no margin for further outlets/cables";
        d["ausreichend"] = "sufficient";
        d["optimaler Bereich"] = "optimal range";
        d["hoch – noch zulässig"] = "high – still permissible";
        d["zu hoch – Übersteuerung möglich"] = "too high – overload possible";
        d["viel zu hoch – Übersteuerung, Verstärkung senken"] = "far too high – overload, reduce gain";
        d["Signalspannung am Eingang des Messgeräts. Zu wenig Pegel verschlechtert die MER, zu viel übersteuert Empfänger und Multischalter. Die vier ZF-Ebenen sollten auf wenige dB gleich sein. Hinweis: Der Pegel des SF 4000 ist nicht kalibriert – Vergleiche zwischen Dosen, Ebenen und Messungen sind aussagekräftiger als die Absolutwerte."] =
            "Signal voltage at the meter input. Too little level degrades the MER, too much overloads receivers and multiswitches. The four IF planes should be within a few dB of each other. Note: the level of the SF 4000 is not calibrated – comparisons between outlets, planes and measurements are more meaningful than the absolute values.";
        d["EN 60728-1: 47–77 dBµV an der Teilnehmerdose (DVB-S/S2)"] = "EN 60728-1: 47–77 dBµV at the subscriber outlet (DVB-S/S2)";

        d["Qualität"] = "Quality";
        d["kein brauchbarer Empfang"] = "no usable reception";
        d["Aussetzer wahrscheinlich"] = "dropouts likely";
        d["sehr gut (bei sauberem Signal 98 %)"] = "very good (98 % with a clean signal)";
        d["Geräteeigene Qualitätsanzeige des SF 4000. Hängt eng mit der MER zusammen, ist aber weniger fein abgestuft – für genaue Beurteilung die MER verwenden."] =
            "The SF 4000's own quality indicator. Closely related to the MER, but less finely graded – use the MER for an accurate assessment.";
        d["Geräteanzeige SF 4000"] = "SF 4000 device reading";

        d["Stärke"] = "Strength";
        d["fast kein Signal"] = "almost no signal";
        d["sehr schwach"] = "very weak";
        d["schwach"] = "weak";
        d["kräftig"] = "strong";
        d["Geräteeigene Signalstärke des SF 4000. Zeigt nur, dass Signal ankommt – nicht, ob es sauber ist. Auch ein falscher Satellit kann viel Stärke haben."] =
            "The SF 4000's own signal strength. Only shows that a signal arrives – not whether it is clean. Even a wrong satellite can show high strength.";

        d["Fehlerkorrektur überfordert – kein Bild"] = "error correction overwhelmed – no picture";
        d["an der Grenze der Fehlerkorrektur"] = "at the limit of error correction";
        d["viele Fehler, Korrektur arbeitet stark"] = "many errors, correction working hard";
        d["Bitfehlerrate vor der Fehlerkorrektur: Anteil falsch empfangener Bits. Die Fehlerkorrektur des Empfängers repariert diese Fehler bis etwa 1E-2. Je kleiner der Wert (mehr Nullen hinter dem Komma), desto besser. Reagiert träger als die MER."] =
            "Bit error rate before error correction: share of incorrectly received bits. The receiver's error correction repairs these errors up to about 1E-2. The smaller the value (more zeros after the decimal point), the better. Responds more slowly than the MER.";
        d["vor Fehlerkorrektur (Viterbi/LDPC)"] = "before error correction (Viterbi/LDPC)";
        d["Bei DVB-S2-Transpondern meldet das SF 4000 Werte, die nicht zur gemessenen MER passen; die BER wird dort deshalb nicht bewertet – maßgeblich ist die MER."] =
            "For DVB-S2 transponders the SF 4000 reports values that do not match the measured MER; the BER is therefore not rated there – the MER is decisive.";
        d["bei DVB-S2 nicht bewertet (Gerätewert nicht plausibel)"] = "not rated for DVB-S2 (device value not plausible)";

        // ---------------------------------------------------------------- Planes (PlaneAnalysis)
        d["nicht gemessen"] = "not measured";
        d["kein Transponder empfangen – Ebene fehlt (LNB, Kabel, Multischalter?)"] = "no transponder received – plane missing (LNB, cable, multiswitch?)";
        d["nur {0:P0} Lock"] = "only {0:P0} lock";
        d["Pegel zu niedrig (Ø {0:0.0} dBµV) – Verstärkung erhöhen"] = "Level too low (avg. {0:0.0} dBµV) – increase gain";
        d["Pegel knapp (Ø {0:0.0} dBµV)"] = "Level tight (avg. {0:0.0} dBµV)";
        d["Pegel zu hoch (Ø {0:0.0} dBµV) – Übersteuerung möglich, Verstärkung senken"] = "Level too high (avg. {0:0.0} dBµV) – overload possible, reduce gain";
        d["{0:0.0} dB höher als die anderen Ebenen – Ebene am Verstärker angleichen"] = "{0:0.0} dB higher than the other planes – balance the plane at the amplifier";
        d["{0:0.0} dB niedriger als die anderen Ebenen – Ebene am Verstärker angleichen"] = "{0:0.0} dB lower than the other planes – balance the plane at the amplifier";
        d["Schräglage {0:+0.0;-0.0} dB über 950–2150 MHz – Entzerrung (Slope) prüfen"] = "Tilt {0:+0.0;-0.0} dB across 950–2150 MHz – check equalisation (slope)";
        d["MER niedrig (Ø {0:0.0} dB)"] = "MER low (avg. {0:0.0} dB)";
        d["in Ordnung"] = "OK";

        // ---------------------------------------------------------------- Traffic light (Assessment)
        d["Noch keine Messung"] = "No measurement yet";
        d["Kein Empfang"] = "No reception";
        d["Der Transponder wird nicht empfangen – Ausrichtung, LNB, Kabel und Multischalter prüfen."] =
            "The transponder is not received – check alignment, LNB, cable and multiswitch.";
        d["Pegel {0:0.0} dBµV außerhalb 47–77 dBµV."] = "Level {0:0.0} dBµV outside 47–77 dBµV.";
        d["{0} von {1} Transpondern ohne Lock ({2:P0})."] = "{0} of {1} transponders without lock ({2:P0}).";
        d["Ebene {0} fehlt ganz – LNB-Ausgang, Kabel oder Multischalter prüfen."] = "Plane {0} is missing entirely – check LNB output, cable or multiswitch.";
        d["Ebene {0}: {1}."] = "Plane {0}: {1}.";
        d["Median aller Transponder"] = "median of all transponders";
        d["Median-MER"] = "Median MER";
        d["Schwächste Transponder nur {0:+0.0;-0.0} dB über der Empfangsschwelle."] = "Weakest transponders only {0:+0.0;-0.0} dB above the reception threshold.";
        d["{0} {1:0.0} dB – nur {2:+0.0;-0.0} dB über der Empfangsschwelle ({3:0.0} dB, {4})."] =
            "{0} {1:0.0} dB – only {2:+0.0;-0.0} dB above the reception threshold ({3:0.0} dB, {4}).";
        d["{0} {1:0.0} dB – Reserve {2:+0.0} dB, knapp ({3})."] = "{0} {1:0.0} dB – margin {2:+0.0} dB, tight ({3}).";
        d["{0} {1:0.0} dB – Reserve {2:+0.0} dB über der Empfangsschwelle."] = "{0} {1:0.0} dB – margin {2:+0.0} dB above the reception threshold.";
        d["Kein Standort gewählt – ohne Schüsselgröße keine Erwartung und keine Wetterreserve."] =
            "No site selected – without the dish size there is no expected value and no weather margin.";
        d["{0:0.0} dB unter der Erwartung für {1} cm ({2:0.0} dB) – Ausrichtung, LNB oder Kabel stark fehlerhaft."] =
            "{0:0.0} dB below the expected value for {1} cm ({2:0.0} dB) – alignment, LNB or cable seriously faulty.";
        d["{0:0.0} dB unter der Erwartung für {1} cm ({2:0.0} dB) – Feinausrichtung (Azimut, Elevation, Skew) prüfen."] =
            "{0:0.0} dB below the expected value for {1} cm ({2:0.0} dB) – check fine alignment (azimuth, elevation, skew).";
        d["Erwartung für {0} cm bei „{1}“: ca. {2:0.0} dB – erreicht."] = "Expected value for {0} cm in “{1}”: approx. {2:0.0} dB – reached.";
        d["Reserve reicht bei Regen kaum (Regen kostet hier ca. {0:0.0} dB)."] = "Margin barely sufficient in rain (rain costs approx. {0:0.0} dB here).";
        d["Bei Gewitter-Starkregen (ca. {0:0.0} dB) sind kurze Aussetzer möglich."] = "Short dropouts are possible in thunderstorm downpours (approx. {0:0.0} dB).";
        d["Reserve deckt auch Gewitter-Starkregen (ca. {0:0.0} dB)."] = "Margin also covers thunderstorm downpours (approx. {0:0.0} dB).";
        d["Gemessen bei „{0}“ – Dämpfung ca. {1:0.0} dB steckt bereits im Messwert."] = "Measured in “{0}” – attenuation of approx. {1:0.0} dB is already included in the reading.";
        d["Empfang gestört"] = "Reception impaired";
        d["Empfang verbesserungswürdig"] = "Reception could be improved";
        d["Empfang in Ordnung"] = "Reception OK";

        // ---------------------------------------------------------------- Diagnosis
        d["Keine Auffälligkeiten"] = "Nothing conspicuous";
        d["Pegel und MER verhalten sich auf allen Ebenen unauffällig."] = "Level and MER look normal on all planes.";
        d["{0} von {1} Transpondern ohne Lock, obwohl Pegel anliegt (Ø {2:0.0} dBµV)."] = "{0} of {1} transponders without lock although level is present (avg. {2:0.0} dBµV).";
        d["{0} von {1} Transpondern ohne Lock, kaum Pegel."] = "{0} of {1} transponders without lock, hardly any level.";
        d["Alle Ebenen steigen in den ersten 300 MHz um {0:0}–{1:0} dB."] = "All planes rise by {0:0}–{1:0} dB within the first 300 MHz.";
        d["Pegel steigt ab {0} MHz ZF in den ersten 300 MHz um {1:0} dB ({2:0.0} → {3:0.0} dBµV), die anderen Ebenen um {4:0} dB."] =
            "Level rises by {1:0} dB within the first 300 MHz from {0} MHz IF ({2:0.0} → {3:0.0} dBµV), the other planes by {4:0} dB.";
        d["Ø {0:0.0} dBµV, {1:+0.0;-0.0} dB gegenüber den anderen Ebenen (Ø {2:0.0} dBµV)."] = "Avg. {0:0.0} dBµV, {1:+0.0;-0.0} dB compared with the other planes (avg. {2:0.0} dBµV).";
        d["{0} Transponder unter {1:0} dBµV, Minimum {2:0.0} dBµV bei {3} MHz."] = "{0} transponders below {1:0} dBµV, minimum {2:0.0} dBµV at {3} MHz.";
        d["{0} Transponder ab {1:0} dBµV, Maximum {2:0.0} dBµV."] = "{0} transponders at {1:0} dBµV or more, maximum {2:0.0} dBµV.";
        d["MER aller Ebenen im Mittel {0:0.0} dB unter dem besten bisher gemessenen Wert je Transponder (umgerechnet auf Schüssel und Wetter)."] =
            "MER of all planes on average {0:0.0} dB below the best value measured so far per transponder (converted to dish and weather).";
        d["MER im Mittel {0:0.0} dB unter dem Bestwert je Transponder, die anderen Ebenen {1:0.0} dB."] = "MER on average {0:0.0} dB below the best value per transponder, the other planes {1:0.0} dB.";
        d["{0} von {1} Transpondern im Bereich {2}–{3} MHz ZF mit MER-Einbruch: {4}."] = "{0} of {1} transponders in the range {2}–{3} MHz IF with an MER dip: {4}.";
        d["{0} von {1} Transpondern mit MER-Einbruch um mehr als {2:0.#} dB bei normalem Pegel: {3}."] = "{0} of {1} transponders with an MER dip of more than {2:0.#} dB at normal level: {3}.";
        d["Transponder mit Gegenstück auf der anderen Polarisation {0:0.0} dB schlechter als die übrigen ({1} gegen {2} Transponder)."] =
            "Transponders with a counterpart on the other polarization {0:0.0} dB worse than the rest ({1} vs. {2} transponders).";
        d["Unterschiedliches Wetter"] = "Different weather";
        d["Die Scans stammen von verschiedenem Wetter ({0}). Die MER wird darauf umgerechnet, Regen und Schnee bleiben aber eine Unsicherheit."] =
            "The scans were taken in different weather ({0}). The MER is converted accordingly, but rain and snow remain an uncertainty.";
        d["Für genaue Vergleiche bei gleichem Wetter messen."] = "For exact comparisons measure in the same weather.";
        d["{0}: weniger Pegel als die anderen Dosen"] = "{0}: less level than the other outlets";
        d["Ø {0:0.0} dBµV, {1:0.0} dB unter dem Median der anderen Dosen ({2:0.0} dBµV)."] = "Avg. {0:0.0} dBµV, {1:0.0} dB below the median of the other outlets ({2:0.0} dBµV).";
        d["Dosenleitung (Länge, Kabelqualität), zusätzlicher Verteiler oder Durchgangsdose, Stecker dieser Dose."] =
            "Outlet cable (length, cable quality), an additional splitter or loop-through outlet, connectors of this outlet.";
        d["Leitung und Stecker prüfen; reicht der Pegel nicht, den Multischalter-Ausgang für diese Dose höher einstellen."] =
            "Check cable and connectors; if the level is not sufficient, set the multiswitch output for this outlet higher.";
        d["{0}: mehr Pegel als die anderen Dosen"] = "{0}: more level than the other outlets";
        d["Ø {0:0.0} dBµV, {1:0.0} dB über dem Median der anderen Dosen ({2:0.0} dBµV)."] = "Avg. {0:0.0} dBµV, {1:0.0} dB above the median of the other outlets ({2:0.0} dBµV).";
        d["Kurze Dosenleitung oder höher eingestellter Multischalter-Ausgang."] = "Short outlet cable or a multiswitch output set higher.";
        d["Bei über 70 dBµV den Ausgangspegel für diese Dose senken."] = "Above 70 dBµV lower the output level for this outlet.";
        d["{0}: mehr Pegel, aber schlechtere MER – Übersteuerung"] = "{0}: more level but worse MER – overload";
        d["Je 1 dB mehr Pegel rund {0:0.0} dB weniger MER über {1} Dosen. Am höchsten: {2}."] = "Each dB more level costs about {0:0.0} dB MER across {1} outlets. Highest: {2}.";
        d["{0} ({1:0.0} dBµV, MER −{2:0.0} dB)"] = "{0} ({1:0.0} dBµV, MER −{2:0.0} dB)";
        d["Multischalter-Ausgang zu hoch eingestellt – oder Eingangsstufe des Empfängers übersteuert."] = "Multiswitch output set too high – or the receiver's input stage is overloaded.";
        d["Ausgangspegel für diese Ebene bzw. Dosen um einige dB senken (Ziel 60–70 dBµV) und erneut messen."] =
            "Lower the output level for this plane or these outlets by a few dB (target 60–70 dBµV) and measure again.";
        d["{0}: Messwerte schwanken"] = "{0}: readings fluctuate";
        d["{0} Messungen am selben Ort: MER schwankt je Transponder typisch um {1:0.0} dB, {2} Transponder mal mit, mal ohne Lock."] =
            "{0} measurements at the same location: MER per transponder typically varies by {1:0.0} dB, {2} transponders sometimes with, sometimes without lock.";
        d["Wackelkontakt oder Feuchtigkeit in einem Stecker – oder das Wetter hat sich zwischen den Messungen geändert."] =
            "Loose contact or moisture in a connector – or the weather changed between the measurements.";
        d["Stecker und Kabel bewegen, während die Live-Messung läuft: Springt die MER, ist die Stelle gefunden."] =
            "Move connectors and cables while the live measurement runs: where the MER jumps, the spot is found.";
        d["Schwächer ausgeprägt auch: {0}."] = "Less pronounced also at: {0}.";
        d["{0}: kein Signal"] = "{0}: no signal";
        d["{0}: Pegel da, aber kein Empfang – falsche Ebene?"] = "{0}: level present but no reception – wrong plane?";
        d["{0}: tiefe Frequenzen gedämpft (Hochpass)"] = "{0}: low frequencies attenuated (high-pass)";
        d["{0}: Pegel weicht von den anderen Ebenen ab"] = "{0}: level differs from the other planes";
        d["{0}: Pegel zu niedrig"] = "{0}: level too low";
        d["{0}: Pegel sehr hoch"] = "{0}: level very high";
        d["{0}: MER schlechter als auf den anderen Ebenen"] = "{0}: MER worse than on the other planes";
        d["MER auf allen Ebenen unter den Bestwerten"] = "MER on all planes below the best values";
        d["{0}: Störung im Bereich {1} – Einstrahlung?"] = "{0}: interference in the {1} range – ingress?";
        d["{0}: MER-Einbrüche bei einzelnen Frequenzen"] = "{0}: MER dips at single frequencies";
        d["Schlechte Polarisationsentkopplung"] = "Poor cross-polar isolation";
        d["LNB-Skew (Verdrehung im Halter) oder LNB mit schlechter Polarisationsentkopplung."] = "LNB skew (rotation in the holder) or an LNB with poor cross-polar isolation.";
        d["Schon am LNB-Port: LNB, Schüssel oder Ausrichtung – oder Kabel und Stecker bis zum Messpunkt."] =
            "Already at the LNB port: LNB, dish or alignment – or cable and connectors up to the measuring point.";
        d["An allen {0} Dosen"] = "At all {0} outlets";
        d["An {0} von {1} Dosen"] = "At {0} of {1} outlets";
        d["{0}, aber nicht am LNB-Port: zwischen LNB und Multischalter – Stammleitung, F-Stecker oder Multischalter-Eingang {1}."] =
            "{0}, but not at the LNB port: between LNB and multiswitch – trunk line, F connector or multiswitch input {1}.";
        d["{0}: gemeinsamer Teil der Anlage – Schüssel, LNB, Stammleitungen oder Multischalter."] = "{0}: shared part of the system – dish, LNB, trunk lines or multiswitch.";
        d["{0}: vor den Multischalter-Ausgängen – LNB-Ausgang, Stammleitung, F-Stecker oder Multischalter-Eingang {1}."] =
            "{0}: before the multiswitch outputs – LNB output, trunk line, F connector or multiswitch input {1}.";
        d["Nur an {0} von {1} Dosen ({2}): Dosenleitung, Dose oder Multischalter-Ausgang dieser Dosen."] =
            "Only at {0} of {1} outlets ({2}): outlet cable, outlet or multiswitch output of these outlets.";
        d["LNB-Ausgang {0} – oder Kabel und Stecker bis zum Messpunkt."] = "LNB output {0} – or cable and connectors up to the measuring point.";
        d["Schüssel, Ausrichtung oder LNB."] = "Dish, alignment or LNB.";
        d["Nur Ebene {0}: im Zweig dieser Ebene vor dem Multischalter – LNB-Ausgang, Stammleitung, F-Stecker oder Multischalter-Eingang."] =
            "Only plane {0}: in the branch of this plane before the multiswitch – LNB output, trunk line, F connector or multiswitch input.";
        d["Beide Polarisationen im {0}-Band: LNB-Oszillator dieses Bandes oder Band-Umschaltung (22 kHz) im Multischalter."] =
            "Both polarizations in the {0} band: LNB oscillator of this band or band switching (22 kHz) in the multiswitch.";
        d["Beide Bänder {0}: Polarisationszweig im LNB, Skew oder die Stammleitungen dieser Polarisation."] =
            "Both bands {0}: polarization branch in the LNB, skew or the trunk lines of this polarization.";
        d["Alle Ebenen: gemeinsamer Weg – Dosenleitung, Dose, Anschlusskabel – oder Schüssel und LNB. Weitere Messorte ankreuzen, um das zu trennen."] =
            "All planes: shared path – outlet cable, outlet, patch cable – or dish and LNB. Tick further measuring points to tell them apart.";
        d["LNB-Ausgang, Stammleitung und Multischalter-Eingang dieser Ebene prüfen; liefert der Multischalter die LNB-Versorgung?"] =
            "Check LNB output, trunk line and multiswitch input of this plane; does the multiswitch supply power to the LNB?";
        d["Belegung der Multischalter-Eingänge prüfen (VL/HL/VH/HH vertauscht?) – am LNB-Port nachmessen."] =
            "Check the assignment of the multiswitch inputs (VL/HL/VH/HH swapped?) – measure again at the LNB port.";
        d["Ein Quad-LNB braucht einen quadtauglichen Multischalter, der jeden Ausgang auf die Ebene seines Eingangs schaltet – sonst kommt die falsche Ebene an."] =
            "A quad LNB needs a quad-capable multiswitch that switches each output to the plane of its input – otherwise the wrong plane arrives.";
        d["Belegung der Multischalter-Eingänge prüfen (VL/HL/VH/HH vertauscht?) – am LNB-Port nachmessen. Bei einem Quad-LNB: Ist der Multischalter quadtauglich?"] =
            "Check the assignment of the multiswitch inputs (VL/HL/VH/HH swapped?) – measure again at the LNB port. With a quad LNB: is the multiswitch quad-capable?";
        d["F-Stecker der Leitung prüfen: Innenleiter 1–2 mm überstehend, keine Korrosion, fest verschraubt."] =
            "Check the F connectors of the line: centre conductor protruding 1–2 mm, no corrosion, screwed on tightly.";
        d["Zum Eingrenzen die Stammleitungen {0} und {1} an beiden Enden tauschen (am LNB und am Multischalter) – nur an einem Ende getauscht, käme an der Dose die falsche Ebene an. Wandert der Fehler zu {1}, liegt er im Kabel oder an dessen Steckern; bleibt er bei {0}, am LNB-Ausgang oder Multischalter-Eingang – dann die Leitung am Multischalter abziehen und direkt messen (Messort LNB-Port)."] =
            "To narrow it down, swap the trunk lines {0} and {1} at both ends (at the LNB and at the multiswitch) – swapped at one end only, the outlet would receive the wrong plane. If the fault moves to {1}, it is in the cable or its connectors; if it stays on {0}, at the LNB output or multiswitch input – then disconnect the line at the multiswitch and measure it directly (measuring point LNB port).";
        d["Bei Unicable führt ein Kabel alle Ebenen: Ein Fehler auf nur einer Ebene liegt im LNB bzw. im Unicable-Umsetzer."] =
            "With Unicable one cable carries all planes: a fault on one plane only is in the LNB or the Unicable converter.";
        d["Zum Eingrenzen am Multischalter die Kabel an den Eingängen {0} und {1} tauschen – ein quadtauglicher Multischalter schaltet jeden LNB-Ausgang auf die Ebene seines Eingangs. Wandert der Fehler zu {1}, liegt er im Kabel oder am LNB-Ausgang; bleibt er bei {0}, am Multischalter-Eingang. Hängt der LNB ohne Multischalter am Receiver, führt jedes Kabel alle Ebenen: Ein Fehler auf nur einer Ebene liegt dann im LNB."] =
            "To narrow it down, swap the cables at the multiswitch inputs {0} and {1} – a quad-capable multiswitch switches each LNB output to the plane of its input. If the fault moves to {1}, it is in the cable or at the LNB output; if it stays on {0}, at the multiswitch input. If the LNB feeds the receiver directly without a multiswitch, each cable carries all planes: a fault on one plane only is then in the LNB.";
        d["Zum Eingrenzen bei einem Quattro-LNB die Stammleitungen {0} und {1} an beiden Enden tauschen (am LNB und am Multischalter); bei einem Quad-LNB am quadtauglichen Multischalter reicht der Tausch an den Eingängen. Wandert der Fehler zu {1}, liegt er im Kabel; bleibt er bei {0}, am LNB-Ausgang oder Multischalter-Eingang. Mit dem LNB-Typ in den Standort-Einstellungen wird dieser Hinweis genauer."] =
            "To narrow it down with a quattro LNB, swap the trunk lines {0} and {1} at both ends (at the LNB and at the multiswitch); with a quad LNB on a quad-capable multiswitch, swapping at the inputs is enough. If the fault moves to {1}, it is in the cable; if it stays on {0}, at the LNB output or multiswitch input. With the LNB type in the site settings this hint becomes more precise.";
        d["F-Stecker an Dose und Anschlusskabel prüfen. Der Frequenzgang des Messgeräts kann einen Teil erklären – am LNB-Port gegenmessen."] =
            "Check the F connectors at the outlet and patch cable. The meter's own frequency response may explain part of it – cross-check at the LNB port.";
        d["Liegen auf dieser Ebene noch andere Auffälligkeiten vor, zuerst die Leitung prüfen; sonst den Pegel am Multischalter oder Verstärker angleichen."] =
            "If this plane shows other anomalies too, check the line first; otherwise match the level at the multiswitch or amplifier.";
        d["Verstärkung erhöhen oder Leitung verbessern; Ziel 55–70 dBµV an der Dose."] = "Increase the gain or improve the line; target 55–70 dBµV at the outlet.";
        d["Ausgangspegel senken – über 77 dBµV übersteuern viele Receiver."] = "Lower the output level – above 77 dBµV many receivers are overloaded.";
        d["Am LNB-Port dieser Ebene messen: Ist die MER dort gut, liegt der Fehler in Leitung oder Multischalter."] =
            "Measure at the LNB port of this plane: if the MER is good there, the fault is in the line or multiswitch.";
        d["Schüssel fein ausrichten (Live-Messung, auf maximale MER), Skew prüfen, LNB und Schüssel auf Verschmutzung prüfen."] =
            "Fine-align the dish (live measurement, for maximum MER), check skew, check LNB and dish for dirt.";
        d["Kabel, Dosen und Anschlusskabel mit Schirmungsklasse A/A+ verwenden, offene Ausgänge mit 75 Ω abschließen, Stecker festziehen."] =
            "Use cables, outlets and patch cables of screening class A/A+, terminate open outputs with 75 Ω, tighten connectors.";
        d["Leitung auf Knicke, Quetschungen und lose oder schlecht montierte Stecker prüfen; offene Abzweige mit 75 Ω abschließen."] =
            "Check the line for kinks, crushing and loose or badly fitted connectors; terminate open taps with 75 Ω.";
        d["Skew nach dem Wert in den Standort-Einstellungen einstellen; hilft das nicht, den LNB tauschen."] =
            "Set the skew to the value shown in the site settings; if that does not help, replace the LNB.";
        d["Alle Ebenen"] = "All planes";
        d["horizontal"] = "horizontal";
        d["vertikal"] = "vertical";
        d["Quattro (für Multischalter)"] = "Quattro (for multiswitches)";
        d["{0:0.#} dB"] = "{0:0.#} dB";
        d["{0:0.#}–{1:0.#} dB"] = "{0:0.#}–{1:0.#} dB";
        d["mind. {0:0.#} dB"] = "at least {0:0.#} dB";
        d["max. {0:0.#} dB"] = "max. {0:0.#} dB";
        d["Verstärkung {0}"] = "gain {0}";
        d["Rauschmaß {0}"] = "noise figure {0}";
        d["Herstellerangabe"] = "manufacturer data";
        d["Pegel ändert sich über die ZF um {0:+0.0;-0.0} dB je GHz ({1:+0;-0} dB von 950 bis 2150 MHz)."] = "Level changes across the IF by {0:+0.0;-0.0} dB per GHz ({1:+0;-0} dB from 950 to 2150 MHz).";
        d["Leitung und Stecker prüfen; ohne Verstärkung im Multischalter brauchen lange Leitungen besseres Kabel oder einen Verstärker."] =
            "Check cable and connectors; without gain in the multiswitch, long lines need better cable or an amplifier.";
        d["Leitung und Stecker prüfen; reicht der Pegel nicht, am Multischalter höher regeln – das hebt meist alle Dosen an."] =
            "Check cable and connectors; if the level is not sufficient, turn it up at the multiswitch – that usually raises all outlets.";
        d["Leitung und Stecker prüfen."] = "Check cable and connectors.";
        d["Für diese Dose ein Dämpfungsglied von ca. {0:0} dB vorsehen – ein Regler am Multischalter wirkt meist auf alle Dosen."] =
            "Fit an attenuator of approx. {0:0} dB for this outlet – a control on the multiswitch usually affects all outlets.";
        d["Unbedenklich, solange der Pegel unter 70 dBµV bleibt."] = "Harmless as long as the level stays below 70 dBµV.";
        d["Danach erneut messen."] = "Then measure again.";
        d["Pegel fällt zu hohen Frequenzen ab – Kabeldämpfung"] = "Level falls towards high frequencies – cable attenuation";
        d["Pegel steigt zu hohen Frequenzen an – Schräglage überkompensiert?"] = "Level rises towards high frequencies – slope over-equalized?";
        d["Nur Ebene {0}: Ohne Multischalter führt das Kabel alle Ebenen – die Ursache liegt im LNB."] = "Only plane {0}: without a multiswitch the cable carries all planes – the cause is in the LNB.";
        d["Der Multischalter ist laut Einstellung nicht quadtauglich – mit einem Quad-LNB kommen an seinen Eingängen falsche Ebenen an. Ein Quattro-LNB ist nötig."] =
            "According to the settings the multiswitch is not quad-capable – with a quad LNB its inputs receive wrong planes. A quattro LNB is needed.";
        d["Kabel an den Multischalter-Eingängen prüfen: Der Multischalter schaltet jeden LNB-Ausgang auf die Ebene seines Eingangs – steckt ein Kabel lose oder fehlt die Steuerspannung?"] =
            "Check the cables at the multiswitch inputs: the multiswitch switches each LNB output to the plane of its input – is a cable loose or the control voltage missing?";
        d["Ohne Multischalter führt das Kabel alle Ebenen – ein Unterschied zwischen den Ebenen entsteht im LNB."] =
            "Without a multiswitch the cable carries all planes – a difference between the planes arises in the LNB.";
        d["Der Multischalter regelt je Ebene: den Pegel dieser Ebene angleichen. Zeigt die Ebene noch andere Auffälligkeiten, zuerst die Leitung prüfen."] =
            "The multiswitch adjusts per plane: match the level of this plane. If the plane shows other anomalies too, check the line first.";
        d["Der Multischalter regelt nur je Band: Weicht nur eine Polarisation ab, hilft der Regler nicht – die Ursache liegt vor dem Multischalter (LNB-Ausgang, Stammleitung, Eingang). Weichen beide Polarisationen eines Bandes ab, den Regler dieses Bandes anpassen."] =
            "The multiswitch adjusts per band only: if only one polarization differs, the control does not help – the cause is before the multiswitch (LNB output, trunk line, input). If both polarizations of a band differ, adjust the control of this band.";
        d["Der Multischalter regelt alle Ebenen gemeinsam – ein Unterschied zwischen den Ebenen lässt sich damit nicht ausgleichen. Die Ursache liegt vor dem Multischalter (LNB-Ausgang, Stammleitung, Eingang)."] =
            "The multiswitch adjusts all planes together – a difference between the planes cannot be evened out with it. The cause is before the multiswitch (LNB output, trunk line, input).";
        d["Der Multischalter ist nicht regelbar – ein Unterschied zwischen den Ebenen kommt vom LNB oder den Stammleitungen."] =
            "The multiswitch is not adjustable – a difference between the planes comes from the LNB or the trunk lines.";
        d["Passiver Multischalter: Der Pegel kommt nur vom LNB und sinkt mit jedem Meter Kabel und jedem Abzweig – Leitung kürzen, besseres Kabel oder einen aktiven Multischalter bzw. Verstärker einsetzen."] =
            "Passive multiswitch: the level comes from the LNB only and drops with every metre of cable and every tap – shorten the line, use better cable or an active multiswitch or amplifier.";
        d["Ohne Multischalter kommt der Pegel direkt vom LNB: Leitung kürzen, besseres Kabel oder einen Inline-Verstärker einsetzen."] =
            "Without a multiswitch the level comes directly from the LNB: shorten the line, use better cable or an inline amplifier.";
        d["Ausgangspegel am Multischalter höher regeln (Ziel 55–70 dBµV an der Dose); reicht das nicht, die Leitung verbessern."] =
            "Turn up the output level at the multiswitch (target 55–70 dBµV at the outlet); if that is not enough, improve the line.";
        d["Der Multischalter ist nicht regelbar: Leitung verbessern oder einen Verstärker einsetzen; Ziel 55–70 dBµV an der Dose."] =
            "The multiswitch is not adjustable: improve the line or fit an amplifier; target 55–70 dBµV at the outlet.";
        d["Schräglage (Slope) am Multischalter erhöhen."] = "Increase the slope at the multiswitch.";
        d["Der Multischalter hat keine Slope-Regelung: Kabel mit geringerer Dämpfung oder einen Verstärker mit Entzerrung einsetzen."] =
            "The multiswitch has no slope control: use cable with lower attenuation or an amplifier with equalization.";
        d["Schräglage (Slope) am Multischalter erhöhen, falls vorhanden – sonst Kabel mit geringerer Dämpfung oder einen Verstärker mit Entzerrung einsetzen."] =
            "Increase the slope at the multiswitch if available – otherwise use cable with lower attenuation or an amplifier with equalization.";
        d["Schräglage (Slope) am Multischalter zurücknehmen."] = "Reduce the slope at the multiswitch.";
        d["Prüfen, ob ein Verstärker oder Multischalter mit Entzerrung zu stark eingestellt ist. Ein Teil kann vom Frequenzgang des Messgeräts kommen – am LNB-Port gegenmessen."] =
            "Check whether an amplifier or multiswitch with equalization is set too high. Part of it may come from the meter's own frequency response – cross-check at the LNB port.";
        d["Pegel dieser Ebene am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV)."] = "Turn the level of this plane down at the multiswitch by approx. {0:0} dB (target 60–70 dBµV).";
        d["Pegel am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV) – der Regler wirkt auf beide Polarisationen des Bandes."] =
            "Turn the level down at the multiswitch by approx. {0:0} dB (target 60–70 dBµV) – the control affects both polarizations of the band.";
        d["Ausgangspegel am Multischalter um ca. {0:0} dB zurückregeln (Ziel 60–70 dBµV) – der Regler wirkt auf alle Ebenen."] =
            "Turn the output level down at the multiswitch by approx. {0:0} dB (target 60–70 dBµV) – the control affects all planes.";
        d["Der Multischalter ist nicht regelbar: ein Dämpfungsglied von ca. {0:0} dB vor die Dose oder den Receiver setzen."] =
            "The multiswitch is not adjustable: fit an attenuator of approx. {0:0} dB before the outlet or the receiver.";
        d["Ein Dämpfungsglied von ca. {0:0} dB vor die Dose oder den Receiver setzen."] = "Fit an attenuator of approx. {0:0} dB before the outlet or the receiver.";
        d["Pegel um ca. {0:0} dB senken (Ziel 60–70 dBµV) – am Regler des Multischalters oder mit einem Dämpfungsglied."] =
            "Lower the level by approx. {0:0} dB (target 60–70 dBµV) – at the multiswitch control or with an attenuator.";
        d["Der LNB hängt ohne Multischalter am Receiver: Jedes Kabel führt alle Ebenen – ein Fehler auf nur einer Ebene liegt im LNB."] =
            "The LNB feeds the receiver without a multiswitch: each cable carries all planes – a fault on one plane only is in the LNB.";
        d["keiner – LNB direkt am Receiver"] = "none – LNB directly at the receiver";
        d["passiv"] = "passive";
        d["aktiv (mit Verstärker)"] = "active (with amplifier)";
        d["nicht regelbar"] = "not adjustable";
        d["gemeinsam für alle Ebenen"] = "together for all planes";
        d["je Band (Low/High)"] = "per band (Low/High)";
        d["je Ebene"] = "per plane";
        d["ja"] = "yes";
        d["nein"] = "no";

        // ---------------------------------------------------------------- Weather
        d["Sonnig"] = "Sunny";
        d["Leicht bewölkt"] = "Partly cloudy";
        d["Geschlossene Wolkendecke"] = "Overcast";
        d["Regen"] = "Rain";
        d["Schnee"] = "Snow";
        d["Gewitter"] = "Thunderstorm";
        d["Wetter unbekannt"] = "Weather unknown";

        // ---------------------------------------------------------------- Measurement series (ScanSession, SessionStore, SiteArchive)
        d["Fehler"] = "Error";
        d["vermutl. abgeschaltet"] = "prob. switched off";
        d["vermutlich abgeschaltet"] = "probably switched off";
        d["kein Lock"] = "no lock";
        d["Dose {0}"] = "Wall outlet {0}";
        d["LNB-Port {0}"] = "LNB port {0}";
        d["{0:dd.MM.yyyy HH:mm}  {1}\n{2}  ({3}/{4} Lock, {5} Löcher)"] = "{0:g}  {1}\n{2}  ({3}/{4} lock, {5} gaps)";
        d["# Standort {0};{1};{2};{3};Schüssel {4} cm;LNB {5} dB"] = "# Site {0};{1};{2};{3};Dish {4} cm;LNB {5} dB";
        d["# Wetter {0}"] = "# Weather {0}";
        d["Frequenz MHz;Pol;Symbolrate kS;Band;Status;Stärke %;Qualität %;C/N dB;Pegel dBµV;MER dB;BER;Lock-Zeit s;Zeit"] =
            "Frequency MHz;Pol;Symbol rate kS;Band;Status;Strength %;Quality %;C/N dB;Level dBµV;MER dB;BER;Lock time s;Time";
        d["Die ZIP-Datei ist kein Standort-Export des DUR-line Scanners."] = "The ZIP file is not a DUR-line Scanner site export.";

        // ---------------------------------------------------------------- Channel and transponder list
        d["Daten"] = "Data";
        d["verschlüsselt"] = "encrypted";
        d["Im ZIP wurde keine lamedb gefunden."] = "No lamedb was found in the ZIP.";
        d["Keine Enigma2-lamedb (erste Zeile muss „eDVB services /4/“ oder „/5/“ sein)."] =
            "Not an Enigma2 lamedb (the first line must be “eDVB services /4/” or “/5/”).";
        d["Die lamedb enthält keine Sat-Sender."] = "The lamedb contains no satellite channels.";
        d["{0} vom {1:dd.MM.yyyy} ({2} Sender)"] = "{0} of {1:d} ({2} channels)";
        d["Die Datei enthält keine Satelliten (kein satellites.xml-Format?)."] = "The file contains no satellites (not in satellites.xml format?).";
        d["satellites.xml vom {0:dd.MM.yyyy}"] = "satellites.xml of {0:d}";
        d["keine Transponderliste"] = "no transponder list";
        d["Kein Receiver eingestellt."] = "No receiver configured.";
        d["OpenWebif verlangt Benutzer und Passwort."] = "OpenWebif requires a user name and password.";
        d["Senderliste nicht erhalten ({0})."] = "Channel list not received ({0}).";
        d["Datei ist keine lamedb."] = "File is not a lamedb.";
        d["TVHeadend lieferte keine Sat-Sender (Orbitalposition der Netzwerke eingetragen?)."] =
            "TVHeadend returned no satellite channels (orbital position of the networks entered?).";
        d["TVHeadend verweigert den Zugriff – Benutzer/Passwort oder Rechte (Admin) prüfen."] =
            "TVHeadend denies access – check user/password or permissions (admin).";

        // ---------------------------------------------------------------- Site
        d["(ohne Namen)"] = "(unnamed)";

        // Room suggestions
        d["Wohnzimmer"] = "Living room";
        d["Esszimmer"] = "Dining room";
        d["Küche"] = "Kitchen";
        d["Wohnküche"] = "Kitchen-living room";
        d["Schlafzimmer"] = "Bedroom";
        d["Elternschlafzimmer"] = "Master bedroom";
        d["Kinderzimmer"] = "Children's room";
        d["Kinderzimmer 2"] = "Children's room 2";
        d["Jugendzimmer"] = "Teenager's room";
        d["Gästezimmer"] = "Guest room";
        d["Arbeitszimmer"] = "Study";
        d["Büro"] = "Office";
        d["Bibliothek"] = "Library";
        d["Heimkino"] = "Home cinema";
        d["Musikzimmer"] = "Music room";
        d["Bad"] = "Bathroom";
        d["Gäste-WC"] = "Guest WC";
        d["Ankleide"] = "Dressing room";
        d["Flur"] = "Hallway";
        d["Diele"] = "Entrance hall";
        d["Treppenhaus"] = "Stairwell";
        d["Galerie"] = "Gallery";
        d["Wintergarten"] = "Conservatory";
        d["Balkon"] = "Balcony";
        d["Terrasse"] = "Terrace";
        d["Dachgeschoss"] = "Top floor";
        d["Dachboden"] = "Attic";
        d["Spitzboden"] = "Loft";
        d["Keller"] = "Basement";
        d["Hobbyraum"] = "Hobby room";
        d["Partyraum"] = "Party room";
        d["Fitnessraum"] = "Gym";
        d["Sauna"] = "Sauna";
        d["Werkstatt"] = "Workshop";
        d["Hauswirtschaftsraum"] = "Utility room";
        d["Abstellraum"] = "Storage room";
        d["Technikraum"] = "Plant room";
        d["Hausanschlussraum"] = "Service entry room";
        d["Garage"] = "Garage";
        d["Carport"] = "Carport";
        d["Gartenhaus"] = "Garden house";
        d["Poolhaus"] = "Pool house";
        d["Einliegerwohnung"] = "Granny flat";
        d["Ferienwohnung"] = "Holiday flat";
        d["Praxis"] = "Practice";
        d["Laden"] = "Shop";
        d["Wohnmobil"] = "Motorhome";
        d["Wohnwagen"] = "Caravan";

        // ---------------------------------------------------------------- Controls
        d["Grundlage: {0}"] = "Basis: {0}";
        d["Keine Senderliste geladen. Über „Senderliste importieren …“ die lamedb eines Enigma2-Receivers oder ein Senderlisten-Paket (ZIP) laden."] =
            "No channel list loaded. Use “Import channel list …” to load the lamedb of an Enigma2 receiver or a channel list package (ZIP).";
        d["In der Senderliste sind für diesen Transponder keine Programme eingetragen."] = "The channel list contains no channels for this transponder.";
        d["{0} Datendienste"] = "{0} data services";
        d["Fernsehen"] = "TV";
        d["… und {0} weitere"] = "… and {0} more";
        d["Quelle: {0}"] = "Source: {0}";
        d["… {0} weitere (Maus darüber)"] = "… {0} more (hover to see)";
        d["Rot = Empfang gestört, Gelb = verbesserungswürdig, Grün = in Ordnung. Grundlage: MER-Reserve über der Empfangsschwelle, Reserve für Regen/Gewitter und Erwartung aus Schüsselgröße, Standort und Wetter."] =
            "Red = reception impaired, yellow = could be improved, green = OK. Basis: MER margin above the reception threshold, margin for rain/thunderstorms and expected value from dish size, site and weather.";

        // ---------------------------------------------------------------- Device (SF 4000)
        d["Gerät nicht gefunden. Ist das SF 4000 eingeschaltet und nicht mit dem Handy verbunden?"] =
            "Device not found. Is the SF 4000 switched on and not connected to a phone?";
        d["Datendienst FF12 nicht gefunden ({0})."] = "Data service FF12 not found ({0}).";
        d["Sende-/Empfangskanal FF01/FF02 nicht gefunden."] = "Transmit/receive channel FF01/FF02 not found.";
        d["Abonnieren der Antworten fehlgeschlagen ({0})."] = "Subscribing to responses failed ({0}).";
        d["Transponder {0} ist mit LOF {1}/{2} nicht empfangbar."] = "Transponder {0} cannot be received with LOF {1}/{2}.";
        d["Ungültige Statusantwort: {0}"] = "Invalid status response: {0}";
        d["Nicht verbunden."] = "Not connected.";
        d["Keine Antwort auf {0}."] = "No response to {0}.";
    }
}
