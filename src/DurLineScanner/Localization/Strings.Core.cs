namespace DurLineScanner.Localization;

/// <summary>Englische Texte für Auswertung, Daten, Gerät und Steuerelemente.</summary>
public static partial class L
{
    static partial void AddCore(Dictionary<string, string> d)
    {
        // ---------------------------------------------------------------- Bewertung (SignalLimits)
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

        // ---------------------------------------------------------------- Ebenen (PlaneAnalysis)
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

        // ---------------------------------------------------------------- Ampel (Assessment)
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

        // ---------------------------------------------------------------- Wetter
        d["Sonnig"] = "Sunny";
        d["Leicht bewölkt"] = "Partly cloudy";
        d["Geschlossene Wolkendecke"] = "Overcast";
        d["Regen"] = "Rain";
        d["Schnee"] = "Snow";
        d["Gewitter"] = "Thunderstorm";
        d["Wetter unbekannt"] = "Weather unknown";

        // ---------------------------------------------------------------- Messreihe (ScanSession, SessionStore, SiteArchive)
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

        // ---------------------------------------------------------------- Sender- und Transponderliste
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

        // ---------------------------------------------------------------- Standort
        d["(ohne Namen)"] = "(unnamed)";

        // Raumvorschläge
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

        // ---------------------------------------------------------------- Steuerelemente
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

        // ---------------------------------------------------------------- Gerät (SF 4000)
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
