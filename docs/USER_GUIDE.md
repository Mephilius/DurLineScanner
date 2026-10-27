# DUR-line Scanner – User Guide

The DUR-line Scanner is a Windows program for the **DUR-line SF 4000 BT** satellite meter. It connects to the device
via Bluetooth, automatically measures all transponders of a satellite and presents the results as scans. This lets
you see at a glance where there are reception gaps, whether an IF plane arrives too weak or too strong, and how well
an installation performs compared with what is expected.

*Deutsche Fassung: [BENUTZERHANDBUCH.md](BENUTZERHANDBUCH.md)*

---

## Contents

1. [Tested hardware and requirements](#1-tested-hardware-and-requirements)
2. [Installation](#2-installation)
3. [First start and connection](#3-first-start-and-connection)
4. [Settings](#4-settings)
5. [The top area: conditions and measuring point](#5-the-top-area-conditions-and-measuring-point)
6. [Running a scan](#6-running-a-scan)
7. [Overview tab](#7-overview-tab)
8. [Live measurement tab](#8-live-measurement-tab)
9. [Planes & levels tab](#9-planes--levels-tab)
10. [Compare tab](#10-compare-tab)
11. [Table tab](#11-table-tab)
12. [Managing, exporting and importing scans](#12-managing-exporting-and-importing-scans)
13. [Transponder list and channel list](#13-transponder-list-and-channel-list)
14. [How the metrics are rated](#14-how-the-metrics-are-rated)
15. [The traffic light](#15-the-traffic-light)
16. [In practice: typical tasks](#16-in-practice-typical-tasks)
17. [Files and storage locations](#17-files-and-storage-locations)
18. [Troubleshooting](#18-troubleshooting)
19. [Limits and notes](#19-limits-and-notes)

---

## 1. Tested hardware and requirements

> **Important:** The program was **tested with only a single device**: the DUR-line SF 4000 BT, which identifies
> itself via Bluetooth as **"Finder S2-…"** and reports the model ID **260B**. Other devices, product lines or
> firmware versions have not been tested. They may or may not work.

Requirements:

- Windows 10 (version 2004 or later) or Windows 11, 64-bit
- Bluetooth Low Energy (built in or USB dongle)
- For coordinate lookup, updating the transponder list and loading the channel list from the receiver: internet or network access

The program only sends commands to the meter that query values or tune a transponder. Commands that modify lists
in the device or reset it are never sent.

## 2. Installation

**Setup (recommended):** Download `DurLineScanner-<Version>-Setup.exe` from the release page and run it.
The setup does not need administrator rights (installation for all users is optional), creates a Start menu entry
and, if you wish, a desktop shortcut. A separate .NET installation is not required.

**Portable:** Unzip `DurLineScanner-<Version>-win-x64-portable.zip` and start `DurLineScanner.exe`.

**Uninstalling:** via *Settings → Apps* in Windows. Scans and settings are kept
(see [Files and storage locations](#17-files-and-storage-locations)).

**From source:** Install the .NET 10 SDK, then run `dotnet run --project src/DurLineScanner` in the project folder.

## 3. First start and connection

### Pairing the device with Windows

1. Switch on the meter. Disconnect or close any phone app that may be connected – the device can only be connected
   to one partner at a time.
2. In the DUR-line Scanner, click **Connect** at the top right. The program searches for a device named "Finder …".
3. If Windows asks for a **PIN**, enter `000001`.

After the first connection the program remembers the Bluetooth address. The green dot next to the device name
shows an active connection; **Disconnect** ends it.

If the device is not found: see [Troubleshooting](#18-troubleshooting).

### Language

The program starts in the language of Windows (German on a German Windows, otherwise English). Under
**⚙ Settings → General** the language can be set permanently; it takes effect after a restart.

### Help

**? Help** at the top right (or **F1**) opens this guide inside the program, plus **About** with version number,
license and sources, and the full **license text**. In the guide, **Ctrl+F** searches; clicking the table of
contents jumps to the section. The help window can stay open while you work. The version number is also shown at
the top next to the program name.

## 4. Settings

**⚙ Settings** at the top right opens a window with three tabs. Changes are saved when you close it
(**Done**).

### 4.1 Sites

A site describes the reception installation. It is saved with every scan and is the basis for the expected values
used by the traffic light.

| Field | Meaning |
|---|---|
| Site name | free choice, e.g. "Home", "Holiday house", "Customer Miller" |
| Address | street, house number, postal code, town |
| Latitude / Longitude | coordinates in degrees (north and east positive). Determined automatically |
| Dish size (cm) | diameter; for offset dishes, the width |
| LNB gain (dB) | conversion gain according to the data sheet, typically 50–65 dB |

**Finding coordinates:** When you leave the address field (or with **📍 Find coordinates**), the program looks up
the address at OpenStreetMap (Nominatim). The location found is shown below the field. If nothing is found, check
the spelling or enter the coordinates by hand – both comma and period are accepted.

In the box below, the program calculates the following for the satellite selected above:

- **Azimuth** (compass bearing, without magnetic declination), **elevation** (tilt) and **LNB skew**
  (rotation of the LNB) – the values for aligning the dish,
- the **distance** to the satellite,
- the **expected C/N and MER** in sun, rain and thunderstorm, as well as the **level at the LNB output**.

**+ New** creates another site, **Delete** removes the selected one. Scans that have already been saved keep their
site data (they contain a copy).

### 4.2 Receiver / channel list

The program can load directly from your own receiver which channels are on which transponder.

| Receiver | Default port | Access |
|---|---|---|
| **Enigma2** (Dreambox, VU+, Gigablue, Octagon … with OpenWebif) | 80 | User/password of the web interface, if set. If OpenWebif blocks file access, FTP is tried (user `root`) |
| **TVHeadend** | 9981 | User with admin rights, if access is protected |

The connection is entered in three fields:

- **Address (name or IP)** – without `http://`, e.g. `192.168.1.30`, `vuplus.local` or `tv.example.com`.
  If the receiver sits behind a proxy in a sub-path, append it: `server.example.com/tvheadend`.
- **Port** – leave empty for the default port (see table; 443 with HTTPS).
- **Encrypted (HTTPS)** – tick if the receiver is reached via `https://`, e.g. behind a reverse proxy with a
  certificate.

Below, the window shows the resulting connection, e.g. `https://tv.example.com/`. If you paste a full address
such as `https://tv.example.com:8443` into the address field, the program splits it into the three fields
automatically.

**⟳ Test connection and load channel list** checks the settings and loads the list immediately. With
**Update channel list at every start** the list stays up to date automatically. The password is stored encrypted
for your own Windows account.

### 4.3 General

Language of the user interface: *Same as Windows*, *Deutsch* or *English*. Takes effect after a restart.

## 5. The top area: conditions and measuring point

**First row – conditions of the measurement**

| Field | Meaning |
|---|---|
| Site | reception installation from the settings; **…** opens the site management |
| Weather now | Sunny, Partly cloudy, Overcast, Rain, Snow, Thunderstorm |
| Satellite | orbital position from the transponder list, e.g. "19.2E Astra 1N/1P" |
| Note | free text, shown in the list of scans |

The weather is saved with every scan. Rain and wet snow attenuate the signal in the Ku band by a few dB – scans
are therefore only truly comparable when the weather is comparable.

**Second row – measuring point**

| Field | Wall outlet (home) | LNB port (at the dish) |
|---|---|---|
| Room | Room of the outlet from the list (living room, kitchen, children's room, basement, garden house …) or a custom name | – |
| Outlet | Number of the outlet in the room (1–12) | Number of the LNB output (1–16) |
| Planes | normally *all* | with a **Quattro LNB**, each port carries one fixed plane: select that one, and only it is measured |

**The third row** shows the progress of a scan and the transponder list and channel list in use, with the buttons
for updating them (see [Chapter 13](#13-transponder-list-and-channel-list)).

## 6. Running a scan

1. Connect.
2. Select site and weather.
3. Select the measuring point, room and outlet (or port).
4. **▶ Start scan**.

The program tunes each transponder in turn, waits for lock and averages two measurements. For
Astra 19.2° East (about 90 transponders), a scan takes roughly 8–9 minutes; the progress bar shows the
remaining time. **■ Stop** aborts – transponders that have already been measured are still saved.

After the scan, the scan is saved automatically, selected in the list on the left, and the **Overview** with the
**traffic light** is displayed.

## 7. Overview tab

- **Traffic light** at the top: overall verdict for the displayed scan with reasons (see [Chapter 15](#15-the-traffic-light)).
- **Metric**: MER, C/N, level, quality or strength. Hovering the mouse over the selection explains the value and
  its rating ranges.
- **Two charts** – horizontal and vertical: one bar per transponder over frequency, colored by rating
  (dark green very good … dark red very poor). The dashed line at 11,700 MHz separates the low band from the
  high band.
- **Red background** = reception gap (no lock). **Gray background** = probably switched-off transponder: no lock,
  but the level is as good as at its neighbors – so the installation is not the cause.
- Scans ticked in the list on the left are overlaid as **lines**.

## 8. Live measurement tab

For aligning the dish and observing individual transponders.

**Selection**

- **Search channel**: type a name (e.g. "ZDF") and choose from the list – plane and transponder are set
  automatically. Requires a channel list.
- **Plane**: Low V, Low H, High V, High H.
- **Transponder**: all transponders of the plane. When a scan is displayed, the **★ suggestion** is preselected –
  a transponder with good MER, typical level and medium IF, ideal as a reference. Hovering the mouse over the
  selection shows the channels on that transponder.

**▶ Start live** measures about once per second. Changing the plane or transponder takes effect immediately.

**Gauges**: MER, C/N, level, quality, strength and BER, each with colored rating ranges, needle, numeric value and
rating. For MER and C/N, the **margin** above the reception threshold is shown below.
The **blue marker** on the outside shows the best value since the start – handy for alignment: move the dish
slowly and return to the marker. Hovering over a gauge explains the metric with all its ranges.

**◔ Mini scan plane**: measures up to five transponders evenly distributed across the selected plane and shows
minimum and maximum as a **violet arc** and as numbers in the gauges. In about 30 seconds this shows whether the
whole plane arrives well, not just one transponder. Transponders that had no lock in the displayed scan are
skipped. Afterwards, the live measurement continues on the previous transponder.

**Reset markers** clears peak values and the mini-scan range.

**Traffic light and history**: at the bottom left the traffic light for the current value, on the right the history
of MER and level over the last 10 minutes.

**⛶ Full screen** (or **F11**) shows the live measurement filling the screen; **Esc** or **F11** returns.
The measurement continues without interruption.

## 9. Planes & levels tab

Analysis of the four IF planes across all transponders with lock – important for multiswitches and amplifiers
where the planes can be adjusted separately.

| Column | Meaning |
|---|---|
| Lock / of | transponders with lock / measured |
| Level avg, min, max | average, lowest and highest level of the plane |
| Dev. median dB | deviation from the median of all planes. From 4 dB: equalize the plane at the amplifier/multiswitch |
| Slope dB/100 MHz | level drop across the IF (cable attenuation increases with frequency). From about 8 dB over 950–2150 MHz: check the equalizer (slope) |
| MER avg | average MER |
| Rating | plain text; yellow background = notice, red = warning |

The chart shows the level of each transponder over the IF frequency at the cable, one color per plane, the mean
as a dashed line, and the level rating in the background.

## 10. Compare tab

Compares the displayed scan with all ticked ones: measuring point, date, site, weather, traffic light, lock, gaps,
average level per plane, average MER and notices. The bar chart shows the average level per plane and scan.

**Tick same conditions** automatically ticks all scans with the same site, satellite and weather as the one
displayed.

Typical use: comparing LNB ports against outlets. What arrives at the port and is missing at the outlet is lost in
the distribution network (cable, splitter, multiswitch, outlet).

## 11. Table tab

All measured values of the displayed scan, sortable by clicking the column header.

- **MER, level and quality** as colored data bars – the trend across the transponders is immediately visible.
- **Margin dB** = MER minus the reception threshold of the transponder, **Rating** as a colored label.
- Hovering over **frequency, pol, SR or modulation** shows the **channels** of that transponder: separated into
  TV and radio, with SD/HD/UHD, encryption and provider (channel list required).
- Hovering over a column header explains the metric.
- Rows with a red background = reception gap, gray text = probably switched off.

## 12. Managing, exporting and importing scans

The list on the left shows the scans of the **site selected at the top**, newest first (switching the site switches the list; a site without scans shows an empty list): colored **traffic-light dot**, measuring point, date,
lock and gaps, weather and site, note.

- **Select** = display, **tick** = include in the comparison.
- **Filter** at the top: by measuring point and by weather.
- **CSV export**: the selected scan as CSV (semicolon-separated with decimal commas, opens directly in German
  Excel).
- **Delete**: removes the selected scan (with confirmation).
- **Folder**: opens the storage folder.
- **📦 Export …**: packs the site selected above with **all its scans** into a ZIP file:
  `standort.json`, `messungen/*.json` (complete, can be read back in) and `csv/*.csv` (for Excel).
  Good for backing up, passing on or moving to another PC.
- **Import …**: reads such a ZIP file. The site is created if it does not exist yet; scans that already exist are
  not imported twice.

## 13. Transponder list and channel list

**Transponder list** – which transponders exist on a satellite. The app uses the `satellites.xml` from OpenPLi
(generated from LyngSat data). It is not shipped with the app but **downloaded automatically at first start**.
**⟳ Update transponder list** fetches the current version later on, **From file …** takes over the list of an
Enigma2 receiver (`/etc/tuxbox/satellites.xml`) – this also works without internet.
The LNB frequencies are default values: universal LNB 9750/10600 MHz, C band 5150 MHz.

**Channel list** – which channels are on which transponder. Sources:

- **⟳ From receiver** – online from the Enigma2 receiver or TVHeadend (see [4.2](#42-receiver--channel-list)),
- **lamedb file …** – an Enigma2 `lamedb` (format 4 or 5) or a channel list package as ZIP.

The channel list is needed for the channel display in the table and for the channel search in the live measurement.

## 14. How the metrics are rated

All values are rated in five levels: **very good, good, marginal, poor, very poor**.

| Metric | Rating |
|---|---|
| **MER** (modulation error ratio) and **C/N** (carrier/noise) | by the *margin* above the reception threshold of the transponder. The threshold depends on modulation and error protection (e.g. DVB-S QPSK 3/4: 6.9 dB, DVB-S2 8PSK 3/4: 7.9 dB) and follows the ETSI standards EN 300 421 and EN 302 307. Below the threshold very poor, below +1 dB poor, +1…+3 dB marginal, +3…+6 dB good, from +6 dB very good |
| **Level** | 60–74 dBµV very good, 52–60 good, 47–52 or 74–77 marginal, outside 47–77 dBµV (EN 60728-1) poor |
| **Quality / strength** | device readouts in %; strength only shows that signal is arriving, not whether it is clean |
| **BER** (bit error rate) | rated for DVB-S; for DVB-S2 the device does not deliver plausible values – they are only displayed |

MER is the most meaningful value. The meter's level is not calibrated: comparisons between planes, outlets and
scans are more meaningful than the absolute values.

## 15. The traffic light

| Color | Meaning |
|---|---|
| 🟢 Green | Reception OK |
| 🟡 Yellow | Reception could be improved |
| 🔴 Red | Reception impaired |

The traffic light appears **live** (for the tuned transponder), **after the scan** in the Overview, and as a dot on
each scan. Hovering over the traffic light shows all the reasons.

The following are evaluated:

1. **MER margin** above the reception threshold: below +1 dB red, below +3 dB yellow. In a scan, the median of all
   transponders counts; individual weak transponders produce a notice.
2. **Reception gaps** (scan only): from 10 % of transponders red, otherwise yellow. If a whole plane is missing: red.
3. **Conspicuous planes** (scan only): warnings from *Planes & levels* result in yellow.
4. **Expectation** (only with a site): From dish size, location and weather, the program calculates which MER is
   to be expected. If the measurement is 4 dB below: yellow (check fine alignment), from 8 dB: red.
5. **Weather margin** (only with a site, in dry weather): If the margin is not enough for rain, yellow. If it is
   not enough for thunderstorm heavy rain, a notice is given (brief dropouts in heavy rain are normal).

**How the expectation is calculated:** a rough link budget with 51 dBW transmit power (typical for
Astra/Hotbird in Central Europe), 65 % antenna efficiency, 90 K noise temperature in a clear sky and
weather-dependent attenuation that increases at low elevation. The transponder's own MER (about 18 dB) limits the
result. The expectation is a guideline – outside Central Europe or for other satellites the transmit power differs.

Without a site, the traffic light only rates margin, gaps and planes.

## 16. In practice: typical tasks

### Aligning the dish

1. Create a site with address and dish size; read azimuth, elevation and skew in the site window.
2. Align the dish roughly, connect the meter at the LNB, measuring point *LNB port*.
3. **Live measurement**, choose a plane and a transponder (e.g. the ★ suggestion or search for a known channel).
4. **▶ Start live**, watch the MER in full screen (**F11**). Move the dish slowly in azimuth, then in
   elevation; the blue marker shows the best result so far. Finally optimize the LNB skew.
5. **◔ Mini scan plane** for all four planes – all should be green.
6. Finally, a complete scan at the LNB port.

### Checking the distribution network

1. Scan at each LNB port.
2. Scan at each outlet (select room and outlet).
3. **Compare**: tick ports and outlets. Large level differences between port and outlet or between outlets show
   losses in the cable, splitter or multiswitch; gaps at only one outlet point to the outlet or its cable.

### Setting amplifier or multiswitch levels

Scan at the outlet, **Planes & levels** tab: deviation from the median and slope show which plane should be raised
or lowered and whether the equalization (slope) is right.

### Comparing measurements in the same weather

Filter the list on the left by weather, or click **Tick same conditions** in **Compare**.

## 17. Files and storage locations

| Location | Contents |
|---|---|
| `Documents\DurLineScanner\Messungen` | Scans (one JSON file per scan, with site copy, weather, room and outlet) |
| `%AppData%\DurLineScanner\settings.json` | Settings: language, receiver (password encrypted), last selected site and weather |
| `%AppData%\DurLineScanner\sites.json` | Sites |
| `%AppData%\DurLineScanner\device.txt` | Bluetooth address of the meter |
| `%AppData%\DurLineScanner\satellites.xml` | updated or imported transponder list |
| `%AppData%\DurLineScanner\lamedb` | loaded channel list |

## 18. Troubleshooting

| Problem | Solution |
|---|---|
| "No SF 4000 found" | Switch on the device, close or disconnect the phone app, turn on Bluetooth in Windows. If necessary, remove the device in the Windows Bluetooth settings and pair it again (PIN `000001`) |
| Connection drops | Reduce the distance; do not connect another app to the device during the scan |
| Many transponders without lock, level normal | probably an outdated transponder list: **⟳ Update transponder list** |
| A whole plane is missing | Check the LNB, cable or multiswitch input of that plane; with a Quattro LNB at the port, select the correct plane |
| "Address not found" | Check the spelling (e.g. "Hauptstraße 1, 12345 Town") or enter the coordinates by hand |
| Channel list from the receiver does not load | Open the connection shown below the address field in a browser. If it does not respond: check port and the HTTPS box (behind a proxy usually HTTPS without port). Check user and password; TVHeadend requires admin rights |
| Channels are missing in the table | Load a channel list; it must contain the selected satellite |

## 19. Limits and notes

- **Tested with only one device** (DUR-line SF 4000 BT, "Finder S2", model 260B).
- The meter's level is not calibrated; absolute values can deviate by a few dB.
- BER for DVB-S2 is not reliable and is not rated.
- The traffic light's expected values are guidelines from a simplified link budget.
- Circularly polarized transponders are not measured (linear Ku- and C-band LNBs only).
- Transponder list data: OpenPLi / LyngSat. Map data for the address search: © OpenStreetMap contributors.
