namespace DurLineScanner.Localization;

/// <summary>Englische Texte der Fenster (MainWindow, SettingsWindow). Schlüssel = deutscher Text.</summary>
public static partial class L
{
    static partial void AddUi(Dictionary<string, string> d)
    {
        // ------------------------------------------------------------ MainWindow.xaml
        d["⚙  Einstellungen"] = "⚙  Settings";
        d["Standorte verwalten, Receiver für die Senderliste eintragen"] = "Manage sites, set up the receiver for the channel list";
        d["Sat-Messung mit dem SF 4000 BT"] = "Satellite measurement with the SF 4000 BT";
        d["Entwicklungsversion"] = "development build";

        // Hilfe
        d["?  Hilfe"] = "?  Help";
        d["Benutzerhandbuch, Version und Lizenz (F1)"] = "User guide, version and license (F1)";
        d["Hilfe"] = "Help";
        d["Schließen"] = "Close";
        d["Benutzerhandbuch"] = "User guide";
        d["Suchen: Strg+F · Zoom: Strg+Mausrad · Inhaltsverzeichnis anklicken springt zum Abschnitt"] =
            "Search: Ctrl+F · Zoom: Ctrl+mouse wheel · click the table of contents to jump to a section";
        d["Über"] = "About";
        d["Lizenz"] = "License";
        d["Windows-Programm für das Sat-Messgerät DUR-line SF 4000 BT: Transponder-Scans, Messreihen, Pegelauswertung je Ebene, Live-Messung mit Ampel."] =
            "Windows app for the DUR-line SF 4000 BT satellite meter: transponder scans, measurement series, level analysis per plane, live measurement with traffic light.";
        d["Getestet nur mit einem Gerät: DUR-line SF 4000 BT („Finder S2“, Modell 260B)."] =
            "Tested with one device only: DUR-line SF 4000 BT (“Finder S2”, model 260B).";
        d["Kein Produkt des Geräteherstellers. Markennamen gehören ihren Inhabern."] =
            "Not a product of the device manufacturer. Trademarks belong to their owners.";
        d["Freie Software unter der GNU General Public License Version 3 (GPL-3.0): Sie dürfen das Programm nutzen, weitergeben und verändern; veränderte Fassungen müssen ebenfalls unter der GPL-3.0 mit Quelltext weitergegeben werden. Es gibt keinerlei Gewährleistung. Den vollständigen Text zeigt die Registerkarte „Lizenz“."] =
            "Free software under the GNU General Public License version 3 (GPL-3.0): you may use, share and modify the program; modified versions must also be distributed under the GPL-3.0 with source code. There is no warranty whatsoever. The full text is shown on the “License” tab.";
        d["Quelltext und Versionen"] = "Source code and releases";
        d["Daten und Bibliotheken von Dritten"] = "Third-party data and libraries";
        d["• Transponderliste: satellites.xml von OpenPLi (aus LyngSat-Daten erzeugt)"] = "• Transponder list: satellites.xml from OpenPLi (generated from LyngSat data)";
        d["• Adresssuche: Nominatim – Kartendaten © OpenStreetMap-Mitwirkende (ODbL)"] = "• Address search: Nominatim – map data © OpenStreetMap contributors (ODbL)";
        d["• Diagramme: ScottPlot (MIT-Lizenz)"] = "• Charts: ScottPlot (MIT license)";
        d["• Laufzeit: .NET und WPF von Microsoft (MIT-Lizenz)"] = "• Runtime: .NET and WPF by Microsoft (MIT license)";
        d["nicht verbunden"] = "not connected";
        d["Verbinden"] = "Connect";
        d["Bluetooth-Gerät, mit dem verbunden wird"] = "Bluetooth device to connect to";
        d["Gekoppelte und sendende Bluetooth-Geräte neu suchen"] = "Search again for paired and advertising Bluetooth devices";
        d["Trennen"] = "Disconnect";
        d["Standort"] = "Site";
        d["Standorte verwalten: Adresse, Koordinaten, Schüsselgröße, LNB"] = "Manage sites: address, coordinates, dish size, LNB";
        d["Standort mit Schüsselgröße und LNB – wird mit dem Scan gespeichert und für die Ampel verwendet"] =
            "Site with dish size and LNB – saved with the scan and used for the traffic light";
        d["Wetter jetzt"] = "Current weather";
        d["Wird mit dem Scan gespeichert – so lassen sich Scans bei gleichem Wetter vergleichen"] =
            "Saved with the scan – lets you compare scans taken in the same weather";
        d["Satellit"] = "Satellite";
        d["Notiz (optional)"] = "Note (optional)";
        d["z.B. „vor dem Ausrichten“ – erscheint in der Liste der Messreihen"] = "e.g. “before aligning” – shown in the list of scans";
        d["▶  Scan starten"] = "▶  Start scan";
        d["■  Stopp"] = "■  Stop";
        d["Messort"] = "Measuring point";
        d["Sat-Dose (Wohnung)"] = "Wall outlet (home)";
        d["LNB-Port (an der Schüssel)"] = "LNB port (at the dish)";
        d["Raum"] = "Room";
        d["Raum wählen oder eigenen Namen eintippen – bereits gemessene Räume stehen oben"] =
            "Choose a room or type your own name – rooms already measured are listed first";
        d["Anschluss im Raum"] = "Outlet in room";
        d["Ebenen"] = "Planes";
        d["Bei einem Quattro-LNB führt jeder Port fest eine Ebene – dann nur diese scannen"] =
            "With a Quattro LNB each port carries one fixed plane – then scan only that one";
        d["alle"] = "all";
        d["⟳  Transponderliste aktualisieren"] = "⟳  Update transponder list";
        d["Lädt die aktuelle satellites.xml von OpenPLi (aus LyngSat erzeugt)"] = "Downloads the current satellites.xml from OpenPLi (generated from LyngSat)";
        d["Aus Datei …"] = "From file …";
        d["satellites.xml vom eigenen Receiver übernehmen (/etc/tuxbox/satellites.xml)"] = "Use satellites.xml from your own receiver (/etc/tuxbox/satellites.xml)";
        d["⟳  Vom Receiver"] = "⟳  From receiver";
        d["Senderliste online vom Enigma2-Receiver oder TVHeadend laden (in den Einstellungen eintragen)"] =
            "Load the channel list online from the Enigma2 receiver or TVHeadend (set up in Settings)";
        d["lamedb-Datei …"] = "lamedb file …";
        d["Enigma2-lamedb (/etc/enigma2/lamedb oder lamedb5) oder ein Senderlisten-Paket (ZIP) laden – zeigt die Programme je Transponder"] =
            "Load an Enigma2 lamedb (/etc/enigma2/lamedb or lamedb5) or a channel list package (ZIP) – shows the programmes per transponder";
        d["Messreihen"] = "Scans";
        d["Auswählen = anzeigen · Häkchen = zum Vergleich einblenden"] = "Select = show · Tick = overlay for comparison";
        d["Messreihen nach Messort filtern"] = "Filter scans by measuring point";
        d["Messreihen nach Wetter filtern – Scans bei gleichem Wetter vergleichen"] = "Filter scans by weather – compare scans taken in the same weather";
        d["📦  Exportieren …"] = "📦  Export …";
        d["Standort (oben gewählt) mit allen seinen Messreihen als ZIP speichern – JSON zum Wiedereinlesen und CSV für Excel"] =
            "Save the site (selected above) with all its scans as a ZIP – JSON for re-importing and CSV for Excel";
        d["Importieren …"] = "Import …";
        d["Standort-ZIP einlesen: Standort und Messreihen werden übernommen, vorhandene nicht doppelt"] =
            "Read a site ZIP: the site and its scans are imported, existing ones are not duplicated";
        d["CSV-Export"] = "CSV export";
        d["Löschen"] = "Delete";
        d["Ordner"] = "Folder";
        d["Speicherordner öffnen"] = "Open storage folder";
        d["Zum Vergleich einblenden"] = "Overlay for comparison";
        d["Übersicht"] = "Overview";
        d["Messwert"] = "Measurement";
        d["Pegel (dBµV)"] = "Level (dBµV)";
        d["Qualität (%)"] = "Quality (%)";
        d["Stärke (%)"] = "Strength (%)";
        d["Live-Messung"] = "Live measurement";
        d["Sender suchen (Ebene und Transponder folgen)"] = "Find channel (plane and transponder follow)";
        d["Namen eintippen, z.B. „ZDF“ – dann aus der Liste wählen"] = "Type a name, e.g. “ZDF” – then pick from the list";
        d["Ebene"] = "Plane";
        d["Transponder (★ = Vorschlag aus der Messreihe)"] = "Transponder (★ = suggestion from the scan)";
        d["▶  Live starten"] = "▶  Start live";
        d["◔  Mini-Scan Ebene"] = "◔  Mini scan plane";
        d["Misst bis zu 5 gleichmäßig über die Ebene verteilte Transponder und zeigt Minimum und Maximum als violetten Bogen in den Gauges"] =
            "Measures up to 5 transponders spread evenly across the plane and shows minimum and maximum as a violet arc in the gauges";
        d["Marken zurücksetzen"] = "Reset markers";
        d["Blaue Marke = bester Wert seit dem Start (hilfreich beim Ausrichten), violetter Bogen = Bereich des Mini-Scans"] =
            "Blue marker = best value since the start (helpful when aligning), violet arc = range of the mini scan";
        d["⛶  Vollbild"] = "⛶  Full screen";
        d["Live-Messung im Vollbild anzeigen (F11) – Esc kehrt zurück"] = "Show the live measurement in full screen (F11) – Esc returns";
        d["bereit"] = "ready";
        d["Ebenen & Pegel"] = "Planes & levels";
        d["Auswertung der vier ZF-Ebenen über alle Transponder mit Lock. Die Ebenen sollten auf wenige dB gleich sein; die Schräglage zeigt die Kabeldämpfung über die Frequenz. Der Pegel des SF 4000 ist nicht kalibriert – Vergleiche sind aussagekräftiger als Absolutwerte."] =
            "Analysis of the four IF planes across all transponders with lock. The planes should match within a few dB; the slope shows the cable attenuation across frequency. The level of the SF 4000 is not calibrated – comparisons are more meaningful than absolute values.";
        d["Lock"] = "Lock";
        d["von"] = "of";
        d["Pegel Ø"] = "Level Ø";
        d["Abw. Median dB"] = "Dev. median dB";
        d["Schräglage dB/100 MHz"] = "Slope dB/100 MHz";
        d["Bewertung"] = "Rating";
        d["Vergleich"] = "Compare";
        d["Gleiche Bedingungen ankreuzen"] = "Tick same conditions";
        d["Hakt alle Messreihen mit demselben Standort, Satelliten und Wetter wie die angezeigte an"] =
            "Ticks all scans with the same site, satellite and weather as the one shown";
        d["Angezeigte Messreihe plus alle mit Häkchen – z.B. LNB-Ports gegen Dosen: Was am Port ankommt und an der Dose fehlt, geht im Verteilnetz verloren. Für faire Vergleiche gleiches Wetter wählen."] =
            "The scan shown plus all ticked ones – e.g. LNB ports versus outlets: whatever arrives at the port but is missing at the outlet is lost in the distribution network. Choose the same weather for fair comparisons.";
        d["Datum"] = "Date";
        d["Wetter"] = "Weather";
        d["Ampel"] = "Traffic light";
        d["Löcher"] = "Gaps";
        d["Hinweise"] = "Notes";
        d["Tabelle"] = "Table";
        d["Frequenz"] = "Frequency";
        d["Reserve dB"] = "Margin dB";
        d["Pegel dBµV"] = "Level dBµV";
        d["Qualität %"] = "Quality %";
        d["Stärke %"] = "Strength %";
        d["Lock-Zeit s"] = "Lock time s";
        d["Zeit"] = "Time";
        d["Bereit."] = "Ready.";

        // ------------------------------------------------------------ MainWindow.xaml.cs
        d["verbinde …"] = "connecting …";
        d["Suche SF 4000 („Finder …“) …"] = "Searching for SF 4000 (“Finder …”) …";
        d["Kein SF 4000 gefunden. Gerät einschalten und Handy-App trennen."] = "No SF 4000 found. Switch the device on and disconnect the phone app.";
        d["Verbinde …"] = "Connecting …";
        d["Kein SF 4000 gefunden."] = "No SF 4000 found.";
        d["Suche Bluetooth-Geräte …"] = "Searching for Bluetooth devices …";
        d["Keine Bluetooth-Geräte gefunden. Gerät einschalten und Handy-App trennen."] = "No Bluetooth devices found. Switch the device on and disconnect the phone app.";
        d["{0} Bluetooth-Geräte gefunden."] = "{0} Bluetooth devices found.";
        d["Gerätesuche fehlgeschlagen: {0}"] = "Device search failed: {0}";
        d["{0} ({1}, gekoppelt)"] = "{0} ({1}, paired)";
        d["Verbunden mit {0}, Modell {1}."] = "Connected to {0}, model {1}.";
        d["Verbindung fehlgeschlagen: {0}"] = "Connection failed: {0}";
        d["{0} · Modell {1}"] = "{0} · model {1}";
        d["Verbindung verloren."] = "Connection lost.";
        d["Noch kein Standort – mit „…“ anlegen (Adresse, Schüsselgröße, LNB)"] = "No site yet – create one with “…” (address, dish size, LNB)";
        d["{0:0.000}° N, {1:0.000}° O"] = "{0:0.000}° N, {1:0.000}° E";
        d["ohne Koordinaten"] = "without coordinates";
        d["{0}\n{1}\n{2}\nSchüssel {3} cm · LNB {4:0.#} dB"] = "{0}\n{1}\n{2}\nDish {3} cm · LNB {4:0.#} dB";
        d["Senderliste jetzt von {0} {1} laden"] = "Load the channel list from {0} {1} now";
        d["Senderliste online vom Enigma2-Receiver oder TVHeadend laden – zuerst in den Einstellungen eintragen"] =
            "Load the channel list online from the Enigma2 receiver or TVHeadend – set it up in Settings first";
        d["Lade Senderliste von {0} …"] = "Loading channel list from {0} …";
        d["Senderliste geladen: {0}, davon {1} auf {2}."] = "Channel list loaded: {0}, {1} of them on {2}.";
        d["Senderliste vom Receiver nicht geladen: {0}"] = "Channel list not loaded from receiver: {0}";
        d["Transponderliste: {0}"] = "Transponder list: {0}";
        d["{0}: {1} Transponder in der Liste."] = "{0}: {1} transponders in the list.";
        d["Lade aktuelle Transponderliste von OpenPLi …"] = "Loading current transponder list from OpenPLi …";
        d["Transponderliste: noch keine – bitte „Transponderliste aktualisieren“ (Internet nötig)"] = "Transponder list: none yet – please click “Update transponder list” (internet required)";
        d["Aktualisieren fehlgeschlagen: {0}"] = "Update failed: {0}";
        d["satellites.xml|*.xml|Alle Dateien|*.*"] = "satellites.xml|*.xml|All files|*.*";
        d["Import fehlgeschlagen: {0}"] = "Import failed: {0}";
        d["Enigma2-Senderliste wählen"] = "Choose Enigma2 channel list";
        d["Enigma2-Senderliste (lamedb, lamedb5, ZIP)|lamedb;lamedb5;*.zip|Alle Dateien|*.*"] =
            "Enigma2 channel list (lamedb, lamedb5, ZIP)|lamedb;lamedb5;*.zip|All files|*.*";
        d["Senderliste übernommen: {0}, davon {1} auf {2}."] = "Channel list imported: {0}, {1} of them on {2}.";
        d["Import der Senderliste fehlgeschlagen: {0}"] = "Channel list import failed: {0}";
        d["Senderliste: {0}"] = "Channel list: {0}";
        d["keine Senderliste"] = "no channel list";
        d["Zuerst eine Senderliste importieren (lamedb eines Enigma2-Receivers)"] = "Import a channel list first (lamedb of an Enigma2 receiver)";
        d["Keine empfangbaren Transponder für diesen Satelliten/LNB."] = "No receivable transponders for this satellite/LNB.";
        d["Bitte den Raum der Sat-Dose wählen (z.B. „Wohnzimmer“)."] = "Please choose the room of the wall outlet (e.g. “Living room”).";
        d["{0} · Anschluss {1}"] = "{0} · Outlet {1}";
        d["Hinweis: kein Standort gewählt – die Ampel bewertet dann ohne Schüsselgröße und Wetterreserve."] =
            "Note: no site selected – the traffic light then rates without dish size and weather margin.";
        d["{0} von {1} Transpondern · noch ca. {2:m\\:ss} min"] = "{0} of {1} transponders · approx. {2:m\\:ss} min left";
        d["{0} {1}: {2}, MER {3} dB, Pegel {4} dBµV"] = "{0} {1}: {2}, MER {3} dB, level {4} dBµV";
        d["Scan fertig: {0} von {1} Transpondern mit Lock, {2} Empfangslöcher – Ampel: {3}."] =
            "Scan finished: {0} of {1} transponders with lock, {2} reception gaps – traffic light: {3}.";
        d["Scan abgebrochen nach {0} Transpondern."] = "Scan cancelled after {0} transponders.";
        d["Scan unterbrochen: {0}"] = "Scan interrupted: {0}";
        d["Messreihe gespeichert"] = "Scan saved";
        d["kein Lock"] = "no lock";
        d["nicht gescannt"] = "not scanned";
        d["{0}: {1} auf Ebene {2}."] = "{0}: {1} on plane {2}.";
        d["{0} Programme"] = "{0} programmes";
        d["{0}  ·  {1}  ·  Empfangsschwelle {2:0.0} dB"] = "{0}  ·  {1}  ·  reception threshold {2:0.0} dB";
        d["Mini-Scan abgebrochen."] = "Mini scan cancelled.";
        d["Mini-Scan unterbrochen: {0}"] = "Mini scan interrupted: {0}";
        d["Keine Transponder für den Mini-Scan auf {0}."] = "No transponders for the mini scan on {0}.";
        d["Mini-Scan {0}/{1}"] = "Mini scan {0}/{1}";
        d["Mini-Scan {0}: {1} …"] = "Mini scan {0}: {1} …";
        d["Mini-Scan {0}: {1} von {2} Transpondern mit Lock"] = "Mini scan {0}: {1} of {2} transponders with lock";
        d[", Pegel {0:0.0} … {1:0.0} dBµV"] = ", level {0:0.0} … {1:0.0} dBµV";
        d["Mini: kein Lock"] = "Mini: no lock";
        d["stelle ein …"] = "tuning …";
        d["Gerät hat {0} nicht bestätigt."] = "Device did not confirm {0}.";
        d["Live-Messung unterbrochen: {0}"] = "Live measurement interrupted: {0}";
        d["gestoppt"] = "stopped";
        d["Live-Messung läuft im Vollbild – Esc oder F11 kehrt zurück."] = "Live measurement is running in full screen – Esc or F11 returns.";
        d["DUR-line Scanner – Live-Messung"] = "DUR-line Scanner – Live measurement";
        d["✕  Vollbild beenden (Esc)"] = "✕  Exit full screen (Esc)";
        d["Reserve {0:+0.0;-0.0} dB"] = "Margin {0:+0.0;-0.0} dB";
        d["sucht …"] = "searching …";
        d["Verlauf {0}"] = "History {0}";
        d["Zeit (s)"] = "Time (s)";
        d["Bitte zuerst eine Messreihe auswählen."] = "Please select a scan first.";
        d["Exportiert: {0}"] = "Exported: {0}";
        d["Messreihe löschen?\n\n{0}\n{1}"] = "Delete scan?\n\n{0}\n{1}";
        d["Bitte oben zuerst einen Standort wählen."] = "Please choose a site above first.";
        d["Für „{0}“ gibt es noch keine Messreihen (nur Scans mit gewähltem Standort zählen)."] =
            "There are no scans for “{0}” yet (only scans with a selected site count).";
        d["Standort exportieren"] = "Export site";
        d["Standort-Archiv (ZIP)|*.zip"] = "Site archive (ZIP)|*.zip";
        d["Standort „{0}“ mit {1} Messreihen exportiert: {2}"] = "Site “{0}” exported with {1} scans: {2}";
        d["Export fehlgeschlagen: {0}"] = "Export failed: {0}";
        d["Standort importieren"] = "Import site";
        d["Importiert: Standort „{0}“ ({1}), "] = "Imported: site “{0}” ({1}), ";
        d["neu angelegt"] = "newly created";
        d["war schon vorhanden"] = "already existed";
        d["{0} Messreihen übernommen"] = "{0} scans imported";
        d[", {0} schon vorhanden bzw. unlesbar"] = ", {0} already present or unreadable";
        d["Alle Messorte"] = "All measuring points";
        d["Port führt"] = "Port carries";
        d["LNB-Port"] = "LNB port";
        d["Anschluss {0}"] = "Outlet {0}";
        d["alle (Universal-/Quad-LNB)"] = "all (Universal/Quad LNB)";
        d["nur {0} (Quattro-LNB)"] = "only {0} (Quattro LNB)";
        d["Jedes Wetter"] = "Any weather";
        d["Dose"] = "Outlet";
        d["Bitte zuerst eine Messreihe anzeigen."] = "Please show a scan first.";
        d["Keine weitere Messreihe mit gleichem Standort und Wetter ({0})."] = "No other scan with the same site and weather ({0}).";
        d["{0} Messreihen mit gleichem Standort und Wetter ({1}) zum Vergleich angehakt."] = "{0} scans with the same site and weather ({1}) ticked for comparison.";
        d["DVB-S2 8PSK 3/4 als Beispiel"] = "DVB-S2 8PSK 3/4 as an example";
        d["{0}  {1:dd.MM. HH:mm}"] = "{0}  {1:dd/MM HH:mm}";
        d["Vertikal"] = "Vertical";
        d["{0} von {1} mit Lock"] = "{0} of {1} with lock";
        d["{0} Empfangslöcher"] = "{0} reception gaps";
        d["Frequenz (MHz)"] = "Frequency (MHz)";
        d["Empfangsloch"] = "Reception gap";
        d["vermutl. abgeschaltet"] = "prob. switched off";
        d["Linien = Vergleich"] = "Lines = compare";
        d["Pegel je Ebene über der ZF   ·   gestrichelt = Mittelwert, Hintergrund = Bewertung"] =
            "Level per plane across the IF   ·   dashed = mean, background = rating";
        d["ZF-Frequenz am Kabel (MHz)"] = "IF frequency on the cable (MHz)";
        d["Mittlerer Pegel je Ebene und Messort"] = "Mean level per plane and measuring point";
        d["● rot"] = "● red";
        d["● gelb"] = "● yellow";
        d["● grün"] = "● green";
        d["Reserve = gemessene MER minus Empfangsschwelle des Transponders."] = "Margin = measured MER minus the transponder's reception threshold.";
        d["Bewertung der MER-Reserve."] = "Rating of the MER margin.";
        d["Low V  (10,7–11,7 GHz, vertikal)"] = "Low V  (10.7–11.7 GHz, vertical)";
        d["Low H  (10,7–11,7 GHz, horizontal)"] = "Low H  (10.7–11.7 GHz, horizontal)";
        d["High V  (11,7–12,75 GHz, vertikal)"] = "High V  (11.7–12.75 GHz, vertical)";
        d["High H  (11,7–12,75 GHz, horizontal)"] = "High H  (11.7–12.75 GHz, horizontal)";
        d["{0:dd.MM.yyyy HH:mm}  ·  {1}/{2} Lock  ·  {3} Löcher"] = "{0:dd/MM/yyyy HH:mm}  ·  {1}/{2} lock  ·  {3} gaps";

        // ------------------------------------------------------------ SettingsWindow.xaml
        d["Einstellungen"] = "Settings";
        d["Fertig"] = "Done";
        d["Änderungen werden beim Schließen gespeichert."] = "Changes are saved when closing.";
        d["Standorte"] = "Sites";
        d["+  Neu"] = "+  New";
        d["Name des Standorts"] = "Site name";
        d["z.B. „Zuhause“, „Ferienhaus“ oder „Kunde Müller“"] = "e.g. “Home”, “Holiday home” or “Customer Miller”";
        d["Adresse"] = "Address";
        d["📍  Koordinaten ermitteln"] = "📍  Find coordinates";
        d["Sucht die Adresse bei OpenStreetMap (Nominatim) – geschieht auch automatisch beim Verlassen des Felds"] =
            "Looks up the address on OpenStreetMap (Nominatim) – also happens automatically when leaving the field";
        d["Straße, Hausnummer, PLZ und Ort"] = "Street, house number, postcode and town";
        d["Breite (° Nord)"] = "Latitude (° north)";
        d["Länge (° Ost)"] = "Longitude (° east)";
        d["Größe der Schüssel (cm)"] = "Dish size (cm)";
        d["Durchmesser bzw. bei Offset-Schüsseln die Breite"] = "Diameter, or the width for offset dishes";
        d["Verstärkung des LNB (dB)"] = "LNB gain (dB)";
        d["Konversionsgewinn laut Datenblatt, typisch 50–65 dB"] = "Conversion gain from the data sheet, typically 50–65 dB";
        d["Allgemein"] = "General";
        d["Receiver / Senderliste"] = "Receiver / channel list";
        d["Die Senderliste (welche Programme auf welchem Transponder liegen) kann direkt vom eigenen Receiver geladen werden. Enigma2: über OpenWebif, notfalls per FTP (Benutzer root). TVHeadend: über die Web-API (Port 9981, Benutzer mit Admin-Recht)."] =
            "The channel list (which programmes are on which transponder) can be loaded directly from your own receiver. Enigma2: via OpenWebif, or via FTP as a fallback (user root). TVHeadend: via the web API (port 9981, user with admin rights).";
        d["kein Receiver"] = "no receiver";
        d["Enigma2 (Dreambox, VU+, Gigablue, Octagon … mit OpenWebif)"] = "Enigma2 (Dreambox, VU+, Gigablue, Octagon … with OpenWebif)";
        d["Benutzer (optional)"] = "User (optional)";
        d["Passwort (optional)"] = "Password (optional)";
        d["Wird verschlüsselt für Ihr Windows-Konto gespeichert"] = "Stored encrypted for your Windows account";
        d["Senderliste bei jedem Programmstart aktualisieren"] = "Update the channel list every time the app starts";
        d["⟳  Verbindung testen und Senderliste laden"] = "⟳  Test connection and load channel list";

        // ------------------------------------------------------------ SettingsWindow.xaml.cs
        d["Zuhause"] = "Home";
        d["Standort {0}"] = "Site {0}";
        d["Standort „{0}“ löschen?\n\nGespeicherte Messreihen behalten ihre Standortdaten."] = "Delete site “{0}”?\n\nSaved scans keep their site data.";
        d["Noch kein Standort – mit „+ Neu“ anlegen."] = "No site yet – create one with “+ New”.";
        d["Bitte zuerst eine Adresse eingeben."] = "Please enter an address first.";
        d["Suche Adresse bei OpenStreetMap …"] = "Looking up address on OpenStreetMap …";
        d["Adresse nicht gefunden – Schreibweise prüfen oder Koordinaten von Hand eintragen."] =
            "Address not found – check the spelling or enter the coordinates by hand.";
        d["Gefunden: {0}  (Daten © OpenStreetMap-Mitwirkende)"] = "Found: {0}  (data © OpenStreetMap contributors)";
        d["Suche fehlgeschlagen: {0}"] = "Search failed: {0}";
        d["Blickrichtung"] = "Look angles";
        d["Blickrichtung zu {0}"] = "Look angles to {0}";
        d["Azimut {0:0.0}° (Kompass, ohne Missweisung)  ·  Elevation {1:0.0}°  ·  LNB-Skew {2:+0.0;-0.0}°  ·  Entfernung {3:N0} km"] =
            "Azimuth {0:0.0}° (compass, without declination)  ·  elevation {1:0.0}°  ·  LNB skew {2:+0.0;-0.0}°  ·  distance {3:N0} km";
        d["Der Satellit steht von hier aus unter dem Horizont."] = "From here the satellite is below the horizon.";
        d["Ohne Koordinaten wird Mitteleuropa angenommen (Elevation 30°)."] = "Without coordinates, Central Europe is assumed (elevation 30°).";
        d["Erwartung für {0} cm bei 11.700 MHz / 27.500 kS/s:  C/N {1:0.0} dB, MER ca. {2:0.0} dB bei Sonne, {3:0.0} dB bei Regen, {4:0.0} dB bei Gewitter."] =
            "Expectation for {0} cm at 11,700 MHz / 27,500 kS/s:  C/N {1:0.0} dB, MER approx. {2:0.0} dB in sunshine, {3:0.0} dB in rain, {4:0.0} dB in a thunderstorm.";
        d["Pegel am LNB-Ausgang ca. {0:0} dBµV (LNB {1:0.#} dB) – an der Dose weniger, je nach Kabel und Verteilung."] =
            "Level at the LNB output approx. {0:0} dBµV (LNB {1:0.#} dB) – less at the outlet, depending on cable and distribution.";
        d["Richtwerte mit {0:0} dBW Sendeleistung (Mitteleuropa), Wirkungsgrad {1:P0}, Rauschtemperatur {2:0} K."] =
            "Guide values with {0:0} dBW transmit power (Central Europe), efficiency {1:P0}, noise temperature {2:0} K.";
        d["Adresse (Name oder IP)"] = "Address (name or IP)";
        d["Leer lassen für den Standard-Port"] = "Leave empty for the default port";
        d["Verschlüsselt (HTTPS)"] = "Encrypted (HTTPS)";
        d["Für einen Receiver hinter einem Reverse-Proxy mit Zertifikat, z.B. https://tv.example.de"] =
            "For a receiver behind a reverse proxy with a certificate, e.g. https://tv.example.com";
        d["z.B. 192.168.1.20 oder vuplus.local"] = "e.g. 192.168.1.20 or vuplus.local";
        d["z.B. 192.168.1.30 oder tv.example.de"] = "e.g. 192.168.1.30 or tv.example.com";
        d["Name oder IP ohne http:// ({0}). Port leer = Standard {1}."] = "Name or IP without http:// ({0}). Empty port = default {1}.";
        d["Verbindung: {0}"] = "Connection: {0}";
        d["Bitte die Adresse des Receivers eintragen."] = "Please enter the receiver's address.";
        d["Verbinde mit {0} …"] = "Connecting to {0} …";
        d["✓ Senderliste geladen: {0}"] = "✓ Channel list loaded: {0}";
        d[", davon {0} auf {1}."] = ", {0} of them on {1}.";
    }
}
