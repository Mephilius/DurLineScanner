# DurLine Scanner – Benutzerhandbuch

Der DurLine Scanner ist ein Windows-Programm für das Sat-Messgerät **Durline SF 4000 BT**. Es verbindet sich
per Bluetooth mit dem Gerät, misst automatisch alle Transponder eines Satelliten und stellt die Ergebnisse als
Messreihen dar. So sieht man auf einen Blick, wo es Empfangslöcher gibt, ob eine ZF-Ebene zu schwach oder zu stark
ankommt und wie gut eine Anlage im Vergleich zur Erwartung arbeitet.

*English version: [USER_GUIDE.md](USER_GUIDE.md)*

---

## Inhalt

1. [Getestete Hardware und Voraussetzungen](#1-getestete-hardware-und-voraussetzungen)
2. [Installation](#2-installation)
3. [Erster Start und Verbindung](#3-erster-start-und-verbindung)
4. [Einstellungen](#4-einstellungen)
5. [Der obere Bereich: Bedingungen und Messort](#5-der-obere-bereich-bedingungen-und-messort)
6. [Einen Scan durchführen](#6-einen-scan-durchführen)
7. [Registerkarte Übersicht](#7-registerkarte-übersicht)
8. [Registerkarte Live-Messung](#8-registerkarte-live-messung)
9. [Registerkarte Ebenen & Pegel](#9-registerkarte-ebenen--pegel)
10. [Registerkarte Vergleich](#10-registerkarte-vergleich)
11. [Registerkarte Tabelle](#11-registerkarte-tabelle)
12. [Messreihen verwalten, exportieren und importieren](#12-messreihen-verwalten-exportieren-und-importieren)
13. [Transponderliste und Senderliste](#13-transponderliste-und-senderliste)
14. [So werden die Messwerte bewertet](#14-so-werden-die-messwerte-bewertet)
15. [Die Ampel](#15-die-ampel)
16. [Praxis: typische Aufgaben](#16-praxis-typische-aufgaben)
17. [Dateien und Speicherorte](#17-dateien-und-speicherorte)
18. [Fehlerbehebung](#18-fehlerbehebung)
19. [Grenzen und Hinweise](#19-grenzen-und-hinweise)

---

## 1. Getestete Hardware und Voraussetzungen

> **Wichtig:** Das Programm wurde **nur mit einem einzigen Gerät getestet**: Durline SF 4000 BT, das sich per
> Bluetooth als **„Finder S2-…“** meldet und die Modellkennung **260B** liefert. Andere Geräte, Baureihen oder
> Firmware-Stände wurden nicht geprüft. Sie funktionieren möglicherweise, möglicherweise auch nicht.

Voraussetzungen:

- Windows 10 (ab Version 2004) oder Windows 11, 64 Bit
- Bluetooth Low Energy (eingebaut oder USB-Stick)
- Für Koordinatensuche, Aktualisierung der Transponderliste und Senderliste vom Receiver: Internet bzw. Netzwerk

Das Programm sendet an das Messgerät nur Befehle, die Werte abfragen oder einen Transponder einstellen.
Befehle, die Listen im Gerät verändern oder es zurücksetzen, werden nie gesendet.

## 2. Installation

**Setup (empfohlen):** `DurLineScanner-<Version>-Setup.exe` von der Release-Seite herunterladen und ausführen.
Das Setup braucht keine Administratorrechte (wahlweise Installation für alle Benutzer), legt einen Eintrag im
Startmenü und auf Wunsch eine Desktop-Verknüpfung an. Eine separate .NET-Installation ist nicht nötig.

**Portable:** `DurLineScanner-<Version>-win-x64-portable.zip` entpacken und `DurLineScanner.exe` starten.

**Deinstallieren:** über *Einstellungen → Apps* von Windows. Messreihen und Einstellungen bleiben erhalten
(siehe [Dateien und Speicherorte](#17-dateien-und-speicherorte)).

**Aus dem Quelltext:** .NET-10-SDK installieren, dann im Projektordner `dotnet run --project src/DurLineScanner`.

## 3. Erster Start und Verbindung

### Gerät mit Windows koppeln

1. Messgerät einschalten. Eine eventuell verbundene Handy-App trennen oder schließen – das Gerät kann nur mit
   einem Partner gleichzeitig verbunden sein.
2. Im DurLine Scanner oben rechts **Verbinden** klicken. Das Programm sucht nach einem Gerät namens „Finder …“.
3. Fragt Windows nach einer **PIN**, `000001` eingeben.

Nach der ersten Verbindung merkt sich das Programm die Bluetooth-Adresse. Der grüne Punkt neben dem Gerätenamen
zeigt eine bestehende Verbindung; **Trennen** beendet sie.

Wird das Gerät nicht gefunden: siehe [Fehlerbehebung](#18-fehlerbehebung).

### Sprache

Das Programm startet in der Sprache von Windows (Deutsch bei deutschem Windows, sonst Englisch). Unter
**⚙ Einstellungen → Allgemein** lässt sich die Sprache fest einstellen; sie gilt nach einem Neustart.

## 4. Einstellungen

**⚙ Einstellungen** oben rechts öffnet ein Fenster mit drei Registerkarten. Änderungen werden beim Schließen
(**Fertig**) gespeichert.

### 4.1 Standorte

Ein Standort beschreibt die Empfangsanlage. Er wird mit jedem Scan gespeichert und ist die Grundlage für die
Erwartungswerte der Ampel.

| Feld | Bedeutung |
|---|---|
| Name des Standorts | frei wählbar, z.B. „Zuhause“, „Ferienhaus“, „Kunde Müller“ |
| Adresse | Straße, Hausnummer, PLZ, Ort |
| Breite / Länge | Koordinaten in Grad (Nord und Ost positiv). Werden automatisch ermittelt |
| Größe der Schüssel (cm) | Durchmesser, bei Offset-Schüsseln die Breite |
| Verstärkung des LNB (dB) | Konversionsgewinn laut Datenblatt, typisch 50–65 dB |

**Koordinaten ermitteln:** Beim Verlassen des Adressfelds (oder mit **📍 Koordinaten ermitteln**) sucht das
Programm die Adresse bei OpenStreetMap (Nominatim). Der gefundene Ort wird unter dem Feld angezeigt. Wird nichts
gefunden, die Schreibweise prüfen oder die Koordinaten von Hand eintragen – Komma und Punkt werden akzeptiert.

Im Kasten darunter berechnet das Programm für den oben gewählten Satelliten:

- **Azimut** (Kompassrichtung, ohne magnetische Missweisung), **Elevation** (Neigung) und **LNB-Skew**
  (Verdrehung des LNB) – die Werte zum Ausrichten der Schüssel,
- die **Entfernung** zum Satelliten,
- die **zu erwartende C/N und MER** bei Sonne, Regen und Gewitter sowie den **Pegel am LNB-Ausgang**.

**+ Neu** legt einen weiteren Standort an, **Löschen** entfernt den gewählten. Bereits gespeicherte Messreihen
behalten ihre Standortdaten (sie enthalten eine Kopie).

### 4.2 Receiver / Senderliste

Welche Programme auf welchem Transponder liegen, kann das Programm direkt vom eigenen Receiver laden.

| Receiver | Standard-Port | Zugang |
|---|---|---|
| **Enigma2** (Dreambox, VU+, Gigablue, Octagon … mit OpenWebif) | 80 | Benutzer/Passwort des Webinterfaces, falls gesetzt. Sperrt OpenWebif den Dateizugriff, wird FTP versucht (Benutzer `root`) |
| **TVHeadend** | 9981 | Benutzer mit Admin-Recht, falls der Zugriff geschützt ist |

Die Verbindung wird in drei Feldern eingetragen:

- **Adresse (Name oder IP)** – ohne `http://`, z.B. `192.168.1.30`, `vuplus.local` oder `tv.example.de`.
  Liegt der Receiver hinter einem Proxy in einem Unterpfad, diesen anhängen: `server.example.de/tvheadend`.
- **Port** – leer lassen für den Standard-Port (siehe Tabelle, mit HTTPS 443).
- **Verschlüsselt (HTTPS)** – anhaken, wenn der Receiver über `https://` erreichbar ist, z.B. hinter einem
  Reverse-Proxy mit Zertifikat.

Darunter zeigt das Fenster die daraus gebildete Verbindung, z.B. `https://tv.example.de/`. Wird eine vollständige
Adresse wie `https://tv.example.de:8443` ins Adressfeld eingefügt, verteilt das Programm sie automatisch auf die
drei Felder.

**⟳ Verbindung testen und Senderliste laden** prüft die Einstellungen und lädt die Liste sofort. Mit
**Senderliste bei jedem Programmstart aktualisieren** bleibt die Liste automatisch aktuell. Das Passwort wird
verschlüsselt für das eigene Windows-Konto gespeichert.

### 4.3 Allgemein

Sprache der Oberfläche: *Wie Windows*, *Deutsch* oder *English*. Wirkt nach dem Neustart.

## 5. Der obere Bereich: Bedingungen und Messort

**Erste Zeile – Bedingungen der Messung**

| Feld | Bedeutung |
|---|---|
| Standort | Empfangsanlage aus den Einstellungen; **…** öffnet die Standortverwaltung |
| Wetter jetzt | Sonnig, Leicht bewölkt, Geschlossene Wolkendecke, Regen, Schnee, Gewitter |
| Satellit | Orbitposition aus der Transponderliste, z.B. „19.2E Astra 1N/1P“ |
| Notiz | freier Text, erscheint in der Liste der Messreihen |

Das Wetter wird mit jedem Scan gespeichert. Regen und nasser Schnee dämpfen das Signal im Ku-Band um einige dB –
Scans sind deshalb nur bei vergleichbarem Wetter wirklich vergleichbar.

**Zweite Zeile – Messort**

| Feld | Sat-Dose (Wohnung) | LNB-Port (an der Schüssel) |
|---|---|---|
| Raum | Raum der Dose aus der Liste (Wohnzimmer, Küche, Kinderzimmer, Keller, Gartenhaus …) oder eigener Name | – |
| Anschluss | Nummer der Dose im Raum (1–12) | Nummer des LNB-Ausgangs (1–16) |
| Ebenen | normalerweise *alle* | bei einem **Quattro-LNB** führt jeder Port fest eine Ebene: dann diese wählen, es wird nur sie gemessen |

**Dritte Zeile** zeigt den Fortschritt eines Scans und die verwendete Transponder- und Senderliste mit den
Knöpfen zum Aktualisieren (siehe [Kapitel 13](#13-transponderliste-und-senderliste)).

## 6. Einen Scan durchführen

1. Verbinden.
2. Standort und Wetter wählen.
3. Messort, Raum und Anschluss (bzw. Port) wählen.
4. **▶ Scan starten**.

Das Programm stellt jeden Transponder nacheinander ein, wartet auf den Lock und mittelt zwei Messungen. Für
Astra 19,2° Ost (rund 90 Transponder) dauert ein Scan etwa 8–9 Minuten; der Fortschrittsbalken zeigt die
verbleibende Zeit. **■ Stopp** bricht ab – bereits gemessene Transponder werden trotzdem gespeichert.

Nach dem Scan wird die Messreihe automatisch gespeichert, in der Liste links ausgewählt und die **Übersicht**
mit der **Ampel** angezeigt.

## 7. Registerkarte Übersicht

- **Ampel** oben: Gesamturteil zur angezeigten Messreihe mit Begründungen (siehe [Kapitel 15](#15-die-ampel)).
- **Messwert**: MER, C/N, Pegel, Qualität oder Stärke. Die Maus über der Auswahl erklärt den Wert und seine
  Bewertungsbereiche.
- **Zwei Diagramme** – horizontal und vertikal: ein Balken je Transponder über der Frequenz, eingefärbt nach
  Bewertung (dunkelgrün sehr gut … dunkelrot sehr schlecht). Die gestrichelte Linie bei 11.700 MHz trennt Low- und
  High-Band.
- **Rot hinterlegt** = Empfangsloch (kein Lock). **Grau hinterlegt** = vermutlich abgeschalteter Transponder: kein
  Lock, aber der Pegel ist so gut wie bei den Nachbarn – dann liegt es nicht an der Anlage.
- Messreihen mit Häkchen in der Liste links werden als **Linien** überlagert.

## 8. Registerkarte Live-Messung

Zum Ausrichten der Schüssel und zum Beobachten einzelner Transponder.

**Auswahl**

- **Sender suchen**: Namen eintippen (z.B. „ZDF“) und aus der Liste wählen – Ebene und Transponder stellen sich
  automatisch ein. Benötigt eine Senderliste.
- **Ebene**: Low V, Low H, High V, High H.
- **Transponder**: alle Transponder der Ebene. Mit angezeigter Messreihe ist der **★-Vorschlag** vorausgewählt –
  ein Transponder mit guter MER, typischem Pegel und mittlerer ZF, ideal als Referenz. Die Maus über der Auswahl
  zeigt die Programme des Transponders.

**▶ Live starten** misst etwa einmal pro Sekunde. Ein Wechsel von Ebene oder Transponder wirkt sofort.

**Gauges**: MER, C/N, Pegel, Qualität, Stärke und BER, jeweils mit farbigen Bewertungsbereichen, Zeiger,
Zahlenwert und Bewertung. Bei MER und C/N steht darunter die **Reserve** über der Empfangsschwelle.
Die **blaue Marke** außen zeigt den besten Wert seit dem Start – praktisch beim Ausrichten: Schüssel langsam
bewegen und zur Marke zurückkehren. Die Maus über einem Gauge erklärt den Messwert mit allen Bereichen.

**◔ Mini-Scan Ebene**: misst bis zu fünf gleichmäßig über die gewählte Ebene verteilte Transponder und zeigt
Minimum und Maximum als **violetten Bogen** und als Zahlen in den Gauges. So sieht man in etwa 30 Sekunden, ob die
ganze Ebene gut ankommt, nicht nur ein Transponder. Transponder, die in der angezeigten Messreihe keinen Lock
hatten, werden übersprungen. Danach geht die Live-Messung auf dem vorherigen Transponder weiter.

**Marken zurücksetzen** löscht Spitzenwerte und Mini-Scan-Bereich.

**Ampel und Verlauf**: unten links die Ampel für den aktuellen Wert, rechts der Verlauf von MER und Pegel der
letzten 10 Minuten.

**⛶ Vollbild** (oder **F11**) zeigt die Live-Messung bildschirmfüllend; **Esc** oder **F11** kehrt zurück.
Die Messung läuft dabei ohne Unterbrechung weiter.

## 9. Registerkarte Ebenen & Pegel

Auswertung der vier ZF-Ebenen über alle Transponder mit Lock – wichtig für Multischalter und Verstärker, bei denen
sich die Ebenen getrennt einstellen lassen.

| Spalte | Bedeutung |
|---|---|
| Lock / von | Transponder mit Lock / gemessen |
| Pegel Ø, min, max | mittlerer, kleinster, größter Pegel der Ebene |
| Abw. Median dB | Abweichung vom Median aller Ebenen. Ab 4 dB: Ebene am Verstärker/Multischalter angleichen |
| Schräglage dB/100 MHz | Pegelabfall über die ZF (Kabeldämpfung steigt mit der Frequenz). Ab ca. 8 dB über 950–2150 MHz: Entzerrer (Slope) prüfen |
| MER Ø | mittlere MER |
| Bewertung | Klartext; gelb hinterlegt = Hinweis, rot = Warnung |

Das Diagramm zeigt den Pegel jedes Transponders über der ZF-Frequenz am Kabel, eine Farbe je Ebene, gestrichelt
den Mittelwert, im Hintergrund die Pegelbewertung.

## 10. Registerkarte Vergleich

Vergleicht die angezeigte Messreihe mit allen angehakten: Messort, Datum, Standort, Wetter, Ampel, Lock, Löcher,
mittlerer Pegel je Ebene, mittlere MER und Hinweise. Das Balkendiagramm zeigt den mittleren Pegel je Ebene und
Messreihe.

**Gleiche Bedingungen ankreuzen** hakt automatisch alle Messreihen mit demselben Standort, Satelliten und Wetter wie
die angezeigte an.

Typisch: LNB-Ports gegen Dosen vergleichen. Was am Port ankommt und an der Dose fehlt, geht im Verteilnetz verloren
(Kabel, Verteiler, Multischalter, Dose).

## 11. Registerkarte Tabelle

Alle Messwerte der angezeigten Messreihe, sortierbar per Klick auf die Spaltenüberschrift.

- **MER, Pegel und Qualität** als farbige Datenbalken – der Verlauf über die Transponder ist sofort sichtbar.
- **Reserve dB** = MER minus Empfangsschwelle des Transponders, **Bewertung** als farbiges Etikett.
- Die Maus über **Frequenz, Pol, SR oder Modulation** zeigt die **Programme** dieses Transponders: getrennt nach
  Fernsehen und Radio, mit SD/HD/UHD, Verschlüsselung und Anbieter (Senderliste nötig).
- Die Maus über einer Spaltenüberschrift erklärt den Messwert.
- Rot hinterlegte Zeilen = Empfangsloch, graue Schrift = vermutlich abgeschaltet.

## 12. Messreihen verwalten, exportieren und importieren

Die Liste links zeigt alle gespeicherten Messreihen, neueste zuerst: farbiger **Ampel-Punkt**, Messort, Datum,
Lock und Löcher, Wetter und Standort, Notiz.

- **Auswählen** = anzeigen, **Häkchen** = zum Vergleich einblenden.
- **Filter** oben: nach Messort und nach Wetter.
- **CSV-Export**: die ausgewählte Messreihe als CSV (Semikolon, deutsches Dezimalkomma) – öffnet direkt in Excel.
- **Löschen**: entfernt die ausgewählte Messreihe (mit Rückfrage).
- **Ordner**: öffnet den Speicherordner.
- **📦 Standort exportieren …**: packt den oben gewählten Standort mit **allen seinen Messreihen** in eine ZIP-Datei:
  `standort.json`, `messungen/*.json` (vollständig, wieder einlesbar) und `csv/*.csv` (für Excel).
  Gut zum Sichern, Weitergeben oder Umziehen auf einen anderen PC.
- **Importieren …**: liest eine solche ZIP-Datei ein. Der Standort wird angelegt, falls er noch fehlt; Messreihen,
  die schon vorhanden sind, werden nicht doppelt übernommen.

## 13. Transponderliste und Senderliste

**Transponderliste** – welche Transponder es auf einem Satelliten gibt. Verwendet wird die `satellites.xml` von
OpenPLi (aus LyngSat-Daten erzeugt). Sie wird nicht mitgeliefert, sondern **beim ersten Start automatisch aus dem
Internet geladen**. **⟳ Transponderliste aktualisieren** holt später die aktuelle Fassung, **Aus Datei …**
übernimmt die Liste eines Enigma2-Receivers (`/etc/tuxbox/satellites.xml`) – das geht auch ohne Internet.
Die LNB-Frequenzen sind Standardwerte: Universal-LNB 9750/10600 MHz, C-Band 5150 MHz.

**Senderliste** – welche Programme auf welchem Transponder liegen. Quellen:

- **⟳ Vom Receiver** – online vom Enigma2-Receiver oder TVHeadend (siehe [4.2](#42-receiver--senderliste)),
- **lamedb-Datei …** – eine Enigma2-`lamedb` (Format 4 oder 5) oder ein Senderlisten-Paket als ZIP.

Die Senderliste wird für die Programmanzeige in der Tabelle und für die Sendersuche in der Live-Messung gebraucht.

## 14. So werden die Messwerte bewertet

Alle Werte werden in fünf Stufen bewertet: **sehr gut, gut, grenzwertig, schlecht, sehr schlecht**.

| Messwert | Bewertung |
|---|---|
| **MER** (Modulationsfehler) und **C/N** (Träger/Rauschen) | nach der *Reserve* über der Empfangsschwelle des Transponders. Die Schwelle hängt von Modulation und Fehlerschutz ab (z.B. DVB-S QPSK 3/4: 6,9 dB, DVB-S2 8PSK 3/4: 7,9 dB) und folgt den ETSI-Normen EN 300 421 und EN 302 307. Unter der Schwelle sehr schlecht, unter +1 dB schlecht, +1…+3 dB grenzwertig, +3…+6 dB gut, ab +6 dB sehr gut |
| **Pegel** | 60–74 dBµV sehr gut, 52–60 gut, 47–52 bzw. 74–77 grenzwertig, außerhalb 47–77 dBµV (EN 60728-1) schlecht |
| **Qualität / Stärke** | Geräteanzeigen in %; die Stärke zeigt nur, dass Signal ankommt, nicht ob es sauber ist |
| **BER** (Bitfehlerrate) | bei DVB-S bewertet; bei DVB-S2 liefert das Gerät keine plausiblen Werte – sie werden nur angezeigt |

Die MER ist der aussagekräftigste Wert. Der Pegel des Messgeräts ist nicht kalibriert: Vergleiche zwischen Ebenen,
Dosen und Messreihen sind aussagekräftiger als die Absolutwerte.

## 15. Die Ampel

| Farbe | Bedeutung |
|---|---|
| 🟢 Grün | Empfang in Ordnung |
| 🟡 Gelb | Empfang verbesserungswürdig |
| 🔴 Rot | Empfang gestört |

Die Ampel erscheint **live** (für den eingestellten Transponder), **nach dem Scan** in der Übersicht und als Punkt
an jeder Messreihe. Die Maus über der Ampel zeigt alle Begründungen.

Bewertet werden:

1. **MER-Reserve** über der Empfangsschwelle: unter +1 dB rot, unter +3 dB gelb. Beim Scan zählt der Median aller
   Transponder; einzelne schwache Transponder ergeben einen Hinweis.
2. **Empfangslöcher** (nur Scan): ab 10 % der Transponder rot, sonst gelb. Fehlt eine ganze Ebene: rot.
3. **Auffällige Ebenen** (nur Scan): Warnungen aus *Ebenen & Pegel* ergeben gelb.
4. **Erwartung** (nur mit Standort): Aus Schüsselgröße, Standort und Wetter berechnet das Programm, welche MER zu
   erwarten ist. Liegt die Messung 4 dB darunter: gelb (Feinausrichtung prüfen), ab 8 dB: rot.
5. **Wetterreserve** (nur mit Standort, bei trockenem Wetter): Reicht die Reserve nicht für Regen, gelb. Reicht sie
   nicht für Gewitter-Starkregen, gibt es einen Hinweis (kurze Aussetzer bei Starkregen sind normal).

**Wie die Erwartung berechnet wird:** ein überschlägiges Linkbudget mit 51 dBW Sendeleistung (typisch für
Astra/Hotbird in Mitteleuropa), 65 % Antennenwirkungsgrad, 90 K Rauschtemperatur bei klarem Himmel und
wetterabhängiger Dämpfung, die bei flacher Elevation zunimmt. Die MER des Transponders selbst (ca. 18 dB) begrenzt
das Ergebnis. Die Erwartung ist ein Richtwert – außerhalb Mitteleuropas oder bei anderen Satelliten weicht die
Sendeleistung ab.

Ohne Standort bewertet die Ampel nur Reserve, Löcher und Ebenen.

## 16. Praxis: typische Aufgaben

### Schüssel ausrichten

1. Standort mit Adresse und Schüsselgröße anlegen, Azimut, Elevation und Skew im Standortfenster ablesen.
2. Schüssel grob ausrichten, Messgerät am LNB anschließen, Messort *LNB-Port*.
3. **Live-Messung**, Ebene und einen Transponder wählen (z.B. ★-Vorschlag oder einen bekannten Sender suchen).
4. **▶ Live starten**, im Vollbild (**F11**) die MER beobachten. Schüssel langsam im Azimut, dann in der Elevation
   bewegen; die blaue Marke zeigt das bisher beste Ergebnis. Zuletzt den LNB-Skew optimieren.
5. **◔ Mini-Scan Ebene** für alle vier Ebenen – alle sollten grün sein.
6. Zum Abschluss einen vollständigen Scan am LNB-Port.

### Verteilnetz prüfen

1. Scan an jedem LNB-Port.
2. Scan an jeder Dose (Raum und Anschluss wählen).
3. **Vergleich**: Ports und Dosen anhaken. Große Pegelunterschiede zwischen Port und Dose oder zwischen Dosen zeigen
   Verluste im Kabel, Verteiler oder Multischalter; Löcher nur an einer Dose deuten auf die Dose oder ihr Kabel.

### Verstärker- oder Multischalter-Pegel einstellen

Scan an der Dose, Registerkarte **Ebenen & Pegel**: Abweichung vom Median und Schräglage zeigen, welche Ebene
angehoben oder abgesenkt werden sollte und ob die Entzerrung (Slope) passt.

### Messungen bei gleichem Wetter vergleichen

Linke Liste nach Wetter filtern oder in **Vergleich** auf **Gleiche Bedingungen ankreuzen** klicken.

## 17. Dateien und Speicherorte

| Ort | Inhalt |
|---|---|
| `Dokumente\DurLineScanner\Messungen` | Messreihen (eine JSON-Datei je Scan, mit Standort-Kopie, Wetter, Raum und Anschluss) |
| `%AppData%\DurLineScanner\settings.json` | Einstellungen: Sprache, Receiver (Passwort verschlüsselt), zuletzt gewählter Standort und Wetter |
| `%AppData%\DurLineScanner\sites.json` | Standorte |
| `%AppData%\DurLineScanner\device.txt` | Bluetooth-Adresse des Messgeräts |
| `%AppData%\DurLineScanner\satellites.xml` | aktualisierte oder importierte Transponderliste |
| `%AppData%\DurLineScanner\lamedb` | geladene Senderliste |

## 18. Fehlerbehebung

| Problem | Lösung |
|---|---|
| „Kein SF 4000 gefunden“ | Gerät einschalten, Handy-App schließen bzw. trennen, Bluetooth von Windows einschalten. Gegebenenfalls das Gerät in den Bluetooth-Einstellungen von Windows entfernen und neu verbinden (PIN `000001`) |
| Verbindung bricht ab | Abstand verringern; während des Scans keine andere App mit dem Gerät verbinden |
| Viele Transponder ohne Lock, Pegel normal | wahrscheinlich veraltete Transponderliste: **⟳ Transponderliste aktualisieren** |
| Eine ganze Ebene fehlt | LNB, Kabel oder Multischalter-Eingang dieser Ebene prüfen; bei Quattro-LNB am Port die richtige Ebene wählen |
| „Adresse nicht gefunden“ | Schreibweise prüfen (z.B. „Hauptstraße 1, 12345 Ort“) oder Koordinaten von Hand eintragen |
| Senderliste vom Receiver lädt nicht | Die unter dem Adressfeld angezeigte Verbindung im Browser öffnen. Antwortet sie nicht: Port und HTTPS-Haken prüfen (hinter einem Proxy meist HTTPS ohne Port). Benutzer und Passwort prüfen; bei TVHeadend Admin-Recht nötig |
| Programme in der Tabelle fehlen | Senderliste laden; sie muss den gewählten Satelliten enthalten |

## 19. Grenzen und Hinweise

- **Nur mit einem Gerät getestet** (Durline SF 4000 BT, „Finder S2“, Modell 260B).
- Der Pegel des Messgeräts ist nicht kalibriert; Absolutwerte können um einige dB abweichen.
- Die BER bei DVB-S2 ist nicht verlässlich und wird nicht bewertet.
- Die Erwartungswerte der Ampel sind Richtwerte aus einem vereinfachten Linkbudget.
- Zirkular polarisierte Transponder werden nicht gemessen (nur lineare Ku- und C-Band-LNBs).
- Daten der Transponderliste: OpenPLi / LyngSat. Kartendaten der Adresssuche: © OpenStreetMap-Mitwirkende.
