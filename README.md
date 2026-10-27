# DurLine Scanner

**Windows app for the Durline SF 4000 BT satellite meter** – automated transponder scans, measurement series,
level analysis per IF plane, live alignment view with gauges, and a traffic-light verdict based on dish size,
location and weather.

*Deutsch: [README.de.md](README.de.md)*

> **Tested with one device only:** Durline SF 4000 BT, reporting via Bluetooth as “Finder S2-…”, model ID 260B.
> Other models or firmware versions have not been tested.

## Features

- **Full scans** of all transponders of a satellite via Bluetooth LE, stored as measurement series
- **Overview charts** per polarisation, coloured by rating; reception gaps and probably switched-off transponders marked
- **Planes & levels**: level, imbalance and slope per IF plane (Low/High × V/H) – for amplifier and multiswitch tuning
- **Live measurement** with gauges (MER, C/N, level, quality, strength, BER), peak markers, history and full-screen mode
- **Mini scan**: up to 5 transponders spread across a plane, min/max shown in the gauges
- **Sites**: address with automatic geocoding (OpenStreetMap), dish size, LNB gain; azimuth, elevation, skew and expected MER
- **Weather, room and outlet** stored with every scan; filter and compare scans under the same conditions
- **Traffic light** for live values and scans: MER margin, reception gaps, planes, expected value and rain margin
- **Channel list** from an Enigma2 receiver (OpenWebif/FTP) or TVHeadend, or from a `lamedb` file: channels per
  transponder in the table and channel search in the live view
- **Export/import** a site with all its scans as ZIP; CSV export for Excel
- **German and English** user interface

## Download

Get the latest release from the [Releases](../../releases) page:

- `DurLineScanner-<version>-Setup.exe` – installer (no admin rights required)
- `DurLineScanner-<version>-win-x64-portable.zip` – portable, unzip and run

Requirements: Windows 10 (2004+) or Windows 11, 64-bit, Bluetooth LE. No separate .NET installation needed.

## Documentation

- [User guide (English)](docs/USER_GUIDE.md)
- [Benutzerhandbuch (Deutsch)](docs/BENUTZERHANDBUCH.md)
- [Bluetooth protocol (German)](docs/PROTOKOLL.md)

## Building from source

```powershell
dotnet build DurLineScanner.slnx
dotnet test tests/DurLineScanner.Tests
dotnet run --project src/DurLineScanner
```

Requires the .NET 10 SDK on Windows.

| Path | Content |
|---|---|
| `src/DurLineScanner` | WPF application |
| `tests/DurLineScanner.Tests` | unit tests (protocol, rating, link budget, lists, archive) |
| `tools/BleExplorer` | console tool for Bluetooth tests (`scan`, `dump`, `monitor`, `send`) |
| `installer/DurLineScanner.iss` | Inno Setup script |
| `docs/` | user guides and protocol description |

### CI and releases

- `.github/workflows/build.yml` builds and tests every push and pull request.
- `.github/workflows/release.yml` runs on a tag like `v1.0.0`: tests, self-contained publish for win-x64,
  portable ZIP, Inno Setup installer, and a GitHub release with both files. It can also be started manually
  (artifacts only).

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Safety

The app only sends commands that read values or tune a transponder. Commands that change lists in the device
or reset it are never sent.

## License

© 2026 DurLine Scanner contributors. Licensed under the [GNU General Public License v3.0](LICENSE).

## Third-party data

- Transponder list: `satellites.xml` from [OpenPLi tuxbox-xml](https://github.com/OpenPLi/tuxbox-xml), generated
  from LyngSat data – not included, downloaded by the app at first start
- Geocoding: [Nominatim](https://nominatim.org/) – map data © OpenStreetMap contributors
- Charts: [ScottPlot](https://scottplot.net/)
