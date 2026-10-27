# Durline SF 4000 BT – Bluetooth-Protokoll

Beschreibung der Bluetooth-LE-Schnittstelle, wie sie der DurLine Scanner verwendet. Geprüft am Gerät
(meldet sich als „Finder S2“, Modell `260B`) am 27.10.2026. Mit anderen Geräten oder Firmware-Ständen
wurde nicht getestet. Der Scanner verwendet nur `A5F0`, `A5F2`, `A5F3`, `A5F5` und `A5F6` – alle am Gerät
geprüft. Die übrigen Befehle sind nur der Vollständigkeit halber aufgeführt; Befehle, die ins Gerät
schreiben, werden nie gesendet.

## Verbindung

| | |
|---|---|
| Name | `Finder S2-xxxxxx` |
| Funkmodul | Lierda LSD4BT0476 (BLE-UART-Brücke) |
| Kopplungs-PIN | `000001` (steht auch in Characteristic `FF05`) |
| Service | `0000ff12-0000-1000-8000-00805f9b34fb` |
| Senden (TX) | `0000ff01-…` – Write Without Response |
| Empfangen (RX) | `0000ff02-…` – Notify |

`FF03`–`FF0A` sind Einstellungen des Funkmoduls (Name, PIN usw.), **nicht anfassen**.

Alle Befehle sind Binärdaten. Byte 0 ist immer `A5`, Byte 1 der Befehlscode.
Die Antwort beginnt mit denselben zwei Bytes. Es gibt keine Prüfsumme und keine Längenangabe.
Das Gerät antwortet manchmal nicht oder erst nach mehr als 1 s, deshalb mit Timeout und Wiederholung arbeiten.

Antwort-Statusbytes: `B0` = OK, `B1` = Fehler, `BF` = leer.

## Befehle

| Code | Name | Anfrage | Antwort | Gefahr |
|---|---|---|---|---|
| `A5F0` | Test/Kennung | `A5F0` | `A5F0 260B 03F2` → Modell `260B` (8 Sat.) | lesen |
| `A5F1` | Chip-ID | `A5F1` | `A5F1 300004100500454E4231` | lesen |
| `A5F2` | Transponder einstellen | `A5F2` + 4 Byte TP | `A5F2B0` | harmlos |
| `A5F3` | Status/Messwerte | `A5F3` | siehe unten | lesen |
| `A5F4` | TP-Liste im Gerät ändern | … | | **schreibt ins Gerät** |
| `A5F5` | Detailmodus („More“) an | `A5F5` | `A5F5B0` | harmlos |
| `A5F6` | Detailmodus aus | `A5F6` | | harmlos |
| `A5F7` | Werksreset | `A5F7` | | **nie senden** |
| `A5F8`/`A5F9` | Summer setzen/lesen | | | |
| `A5C1` | LNB-Liste setzen (260) | | | **schreibt ins Gerät** |
| `A5C2`/`A5C3` | LNB-/TP-Liste lesen (260), Index 1-basiert | `A5C2 nn` | | lesen |

## Transponder-Kodierung (4 Byte)

```
Bit 31     Polarisation  1 = Horizontal (18 V), 0 = Vertikal (13 V)
Bit 30     22 kHz        1 = High-Band, 0 = Low-Band
Bit 29..16 ZF-Frequenz in MHz (950..2150)
Bit 15..0  Symbolrate in kSym/s
```

ZF und 22 kHz ergeben sich aus dem LNB (Universal: LOF 9750/10600, Umschaltung bei 11700 MHz):
- Ku-Band: `ZF = Frequenz − LOF`
- C-Band (LOF < 8000): `ZF = LOF − Frequenz`

Symbolrate in kSym/s, also 22000 = `0x55F0` (nicht 22!).

Beispiele (Astra 19,2° O, Universal-LNB, am Gerät verifiziert mit Lock):
- 11494 H 22000: ZF = 11494 − 9750 = 1744 = `0x6D0`, Low-Band → `A5F2 86D0 55F0`
- 12188 H 27500: ZF = 12188 − 10600 = 1588 = `0x634`, High-Band → `A5F2 C634 6B6C`

## Status `A5F3`

Normal: `A5F3 ss pp qq`
- `ss` – Status: `02` = **Lock**; `04` = sucht noch (direkt nach dem Einstellen);
  `01` = kein Lock gefunden bzw. noch kein TP eingestellt (z.B. abgeschalteter Transponder: Pegel normal, C/N/MER 0)
- `pp` – Signalstärke roh 0..100 (Anzeige mit Aufschlag: >90 +5, >80 +8, >70 +9, >60 +8, >50 +11, >0 +13, höchstens 100)
- `qq` – Qualität 0..100 %

Im Detailmodus (`A5F5`) folgen 12 weitere Bytes, alle big-endian:

| Bytes | Wert | Anzeige |
|---|---|---|
| 2 | C/N | /10 dB |
| 2 | Pegel | Rohwert + Korrektur (>900 +50, >800 +80, >700 +90, >600 +80, >500 +110, >0 +130), dann /10 dBµV |
| 4 | MER | /10 dB |
| 4 | BER | Ziffern als Mantisse, Stellenzahl−1 als negativer Exponent (z.B. 364413 → 3,6E-5), `989680` = 10.000.000 → kein Lock, 0 → < 1E-7 |

Gemessen (11494 H, Lock): `A5F3 02 37 62 | 00BE 01A5 000000C5 00058F7D`
→ Lock, Stärke 55, Qualität 98 %, C/N 19,0 dB, Pegel 42,1, MER 19,7 dB, BER 364413

Ohne Lock (Symbolrate falsch): `A5F3 04 33 00 | 0000 0184 00000000 00989680`
→ Pegel wird trotzdem gemessen (38,8), C/N/MER 0.

## Ablauf für eine Messung

1. Verbinden, `FF02` abonnieren
2. `A5F0` → Modell prüfen
3. `A5F5` → Detailmodus
4. Pro Transponder: `A5F2…` senden, ca. 1–2 s warten, mehrmals `A5F3` abfragen
5. Am Ende `A5F6`

`A5F3` lässt sich etwa einmal pro Sekunde abfragen.

## Transponderliste

Der Scanner verwendet die `satellites.xml` im Enigma2-Format von OpenPLi
(https://github.com/OpenPLi/tuxbox-xml), aus LyngSat-Daten erzeugt. Die LNB-Frequenzen sind Standardwerte:
Universal-LNB 9750/10600 MHz mit Umschaltung bei 11700 MHz, C-Band 5150 MHz.
