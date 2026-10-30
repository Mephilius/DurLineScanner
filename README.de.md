# DUR-line Scanner

**Windows-Programm für das Sat-Messgerät DUR-line SF 4000 BT** – automatische Transponder-Scans, Messreihen,
Pegelauswertung pro ZF-Ebene, Live-Ansicht mit Gauges zum Ausrichten und eine Ampel-Bewertung anhand von
Schüsselgröße, Standort und Wetter.

*English: [README.md](README.md)*

> **Nur mit einem Gerät getestet:** DUR-line SF 4000 BT, meldet sich per Bluetooth als „Finder S2-…“, Modellkennung
> 260B. Andere Modelle oder Firmware-Stände wurden nicht getestet.

## Funktionen

- **Komplette Scans** aller Transponder eines Satelliten per Bluetooth LE, gespeichert als Messreihen
- **Übersicht** je Polarisation, eingefärbt nach Bewertung; Empfangslöcher und vermutlich abgeschaltete Transponder markiert
- **Ebenen & Pegel**: Pegel, Ungleichgewicht und Schräglage je ZF-Ebene (Low/High × V/H) – zum Einstellen von Verstärker und Multischalter
- **Diagnose**: erkennt Fehlerbilder (schlechter F-Stecker, Reflexionen, Einstrahlung, Übersteuerung, vertauschte Eingänge …) und verortet sie aus mehreren Scans in der Anlage – LNB, Stammleitung/Multischalter oder einzelne Dosenleitungen
- **Live-Messung** mit Gauges (MER, C/N, Pegel, Qualität, Stärke, BER), Spitzenwert-Marken, Verlauf und Vollbild
- **Mini-Scan**: bis zu 5 über eine Ebene verteilte Transponder, Minimum und Maximum in den Gauges
- **Standorte**: Adresse mit automatischer Koordinatensuche (OpenStreetMap), Schüsselgröße, LNB-Verstärkung; Azimut, Elevation, Skew und zu erwartende MER
- **Wetter, Raum und Anschluss** werden mit jedem Scan gespeichert; Scans bei gleichen Bedingungen filtern und vergleichen
- **Ampel** für Live-Werte und Scans: MER-Reserve, Empfangslöcher, Ebenen, Erwartung und Regenreserve
- **Senderliste** vom Enigma2-Receiver (OpenWebif/FTP) oder TVHeadend bzw. aus einer `lamedb`: Programme je Transponder in der Tabelle und Sendersuche in der Live-Messung
- **Export/Import** eines Standorts mit allen Messreihen als ZIP; CSV-Export für Excel
- Oberfläche **Deutsch und Englisch**

## Download

Aktuelle Version auf der Seite [Releases](../../releases):

- `DurLineScanner-<Version>-Setup.exe` – Installationsprogramm (ohne Administratorrechte)
- `DurLineScanner-<Version>-win-x64-portable.zip` – portabel, entpacken und starten

Voraussetzungen: Windows 10 (ab 2004) oder Windows 11, 64 Bit, Bluetooth LE. Eine separate .NET-Installation ist nicht nötig.

## Dokumentation

- [Benutzerhandbuch](docs/BENUTZERHANDBUCH.md)
- [User guide (English)](docs/USER_GUIDE.md)
- [Bluetooth-Protokoll](docs/PROTOKOLL.md)

## Aus dem Quelltext bauen

```powershell
dotnet build DurLineScanner.slnx
dotnet test tests/DurLineScanner.Tests
dotnet run --project src/DurLineScanner
```

Benötigt das .NET-10-SDK unter Windows.

| Pfad | Inhalt |
|---|---|
| `src/DurLineScanner` | WPF-Anwendung |
| `tests/DurLineScanner.Tests` | Tests (Protokoll, Bewertung, Linkbudget, Listen, Archiv) |
| `tools/BleExplorer` | Konsolenwerkzeug für Bluetooth-Tests (`scan`, `dump`, `monitor`, `send`) |
| `installer/DurLineScanner.iss` | Inno-Setup-Skript |
| `docs/` | Handbücher und Protokollbeschreibung |

### CI und Releases

- `.github/workflows/build.yml` baut und testet jeden Push und Pull Request.
- `.github/workflows/release.yml` läuft bei einem Tag wie `v1.0.0`: Tests, eigenständiges Publish für win-x64,
  portables ZIP, Inno-Setup-Installer und ein GitHub-Release mit beiden Dateien. Manuell gestartet entstehen nur
  die Artefakte.

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Sicherheit

Das Programm sendet nur Befehle, die Werte abfragen oder einen Transponder einstellen. Befehle, die Listen im
Gerät ändern oder es zurücksetzen, werden nie gesendet.

## Lizenz

© 2026 DUR-line Scanner contributors. Lizenziert unter der [GNU General Public License v3.0](LICENSE).

## Daten von Dritten

- Transponderliste: `satellites.xml` von [OpenPLi tuxbox-xml](https://github.com/OpenPLi/tuxbox-xml), aus
  LyngSat-Daten erzeugt – nicht enthalten, die App lädt sie beim ersten Start herunter
- Adresssuche: [Nominatim](https://nominatim.org/) – Kartendaten © OpenStreetMap-Mitwirkende
- Diagramme: [ScottPlot](https://scottplot.net/)
