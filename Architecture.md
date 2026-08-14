# Architecture — ESP32 Internet Radio

> This document describes the **current, active** design: a standalone ESP32 internet radio living in
> the SABA Mainau cabinet. The Raspberry Pi 4 based system that preceded it (`RadioApp/`,
> `RadioFrontend/`) is frozen and documented in [`README.md`](README.md) and [`Docs/`](Docs); it is kept
> intact for the YouTube series and is not part of this design. See [`DeliveryPlan.md`](DeliveryPlan.md)
> for how we get there.

---

## 1. Why the change

The SABA's original mono transistor amplifier is a poor fit for a general-purpose media device. Rather
than compromise both, the project splits:

- **This repo** keeps the radio and downgrades it to what the cabinet is actually good at: a physically
  controlled internet radio. The Raspberry Pi 4, the .NET service, the Angular admin UI, Spotify Connect
  and the MyTuner scraper are all removed from the running device.
- **A proper media device** is a separate project in its own repository, out of scope here.

What survives from the Pi era, physically and logically:

| Kept | Why |
|------|-----|
| Raspberry Pi Pico + `RadioIO/` firmware | The I/O board works. Firmware is **frozen** — same pins, same JSON, unchanged. |
| 1.77" ST7735 TFT (160×128) | Already mounted in the front panel. Rewired from the Pi's SPI to the ESP32's. |
| Button ladder, tuning capacitor, play/pause button | Unchanged, still read by the Pico. |
| SABA transistor amplifier | Still the final audio stage. |
| Screen layout | Deliberately reproduced pixel-for-pixel from the Pi implementation (§6). |

What is new: an **ESP32** (AZ-Delivery DevKit C V4, ESP32-WROOM-32) replaces the Pi 4, and a **PCM5102A
I2S DAC** replaces the Pi's 3.5 mm headphone output.

## 2. Functional change: no more Spotify

With Spotify gone, the `L` button is freed up. All four toggle buttons now select **internet radio
banks**:

```
             87 88 89 ... 104 105 MHz   (19 dial positions)
   L bank    [ ][ ][ ] ... [ ][ ]
   M bank    [ ][ ][ ] ... [ ][ ]
   K bank    [ ][ ][ ] ... [ ][ ]
   U bank    [ ][ ][ ] ... [ ][ ]
```

**4 banks × 19 dial positions = 76 channel slots.** Each filled slot holds a stream URL, a station name
and a logo. Empty slots are legal and common.

## 3. Hardware

```
   ┌──────────────┐  buttons / dial / play-pause (analog + GPIO)
   │  SABA panel  │──────────────┐
   └──────────────┘              ▼
                         ┌───────────────┐
                         │  Pi Pico      │  RadioIO/ — FROZEN
                         │  (I/O board)  │
                         └───────┬───────┘
                        UART1 + 2 signal pins
                                 ▼
   ┌──────────┐  SPI    ┌───────────────┐  I2S   ┌────────────┐  L/R
   │ ST7735   │◀────────│    ESP32      │───────▶│ PCM5102A   │──┐
   │ 160×128  │         │  WROOM-32     │        │ I2S DAC    │  │
   └──────────┘         └───────┬───────┘        └────────────┘  │
                                │ WiFi                    resistor mix → mono
                                ▼                                │
                      internet MP3 streams            ┌──────────▼─────────┐
                                                      │ SABA transistor    │
                                                      │ amplifier + speaker│
                                                      └────────────────────┘
```

### 3.1 Proposed ESP32 pin map

Not yet wired — to be validated during the delivery milestones and corrected here as the single source
of truth. Avoids strapping pins (0, 2, 12, 15) and flash pins (6–11).

| Function | ESP32 pin | Other end |
|---|---|---|
| I2S BCLK | GPIO 26 | PCM5102A `BCK` |
| I2S LRCK / WS | GPIO 25 | PCM5102A `LCK` |
| I2S DATA | GPIO 22 | PCM5102A `DIN` |
| TFT SCK | GPIO 18 (VSPI) | ST7735 `SCK` |
| TFT MOSI | GPIO 23 (VSPI) | ST7735 `SDA` |
| TFT CS | GPIO 5 | ST7735 `CS` |
| TFT D/C (RS) | GPIO 21 | ST7735 `RS` |
| TFT RESET | GPIO 4 | ST7735 `RES` |
| UART2 RX | GPIO 27 | Pico `GP4` (TX) |
| UART2 TX | GPIO 14 | Pico `GP5` (RX) — wired but unused |
| Pico data-ready interrupt | GPIO 34 (input-only) | Pico `GP14` (out) |
| Request-state out | GPIO 33 | Pico `GP22` (in) |

Notes:

- GPIO 26/25/22 are the conventional I2S trio for this board, avoiding a custom pinout.
- **GPIO 16/17 are deliberately avoided** even though they are free on a WROOM-32: PSRAM claims them on
  WROVER modules, and this keeps a board swap a pure `platformio.ini` change if §9 ever forces one.
- The UART link is **one-way in practice**. `RadioIO` only ever writes; nothing on the Pico reads its
  RX line. ESP32 TX is wired for future use only, which is why GPIO 14 (which emits boot noise) is
  harmless here.

PCM5102A jumpers/straps: `SCK`→GND (use the internal PLL), `FMT`→GND (I2S), `XSMT`→3.3 V (unmute),
`VIN`→5 V (the board has its own regulator), `GND`→GND. Its `LOUT`/`ROUT` are summed to mono through
two resistors and injected into the SABA amplifier exactly where the Pi's 3.5 mm mix went — see
[`Docs/SabaCircuit.md`](Docs/SabaCircuit.md).

Volume stays analog: the SABA's own potentiometer. Software gain is a fixed compile-time constant tuned
once for a clean level into the amplifier — there is no spare control input for a digital volume, and
`RadioIO` is frozen.

## 4. UART contract (frozen by `RadioIO`)

The Pico writes newline-**less** JSON to UART1 at **115200 8N1**, 3.3 V, and brackets each write by
pulling its interrupt pin low for ~10 ms before and after. Source of truth:
`RadioIO/src/UARTMessenger.cpp`.

```json
{"command":"ButtonPressed","buttonIndex":2}
{"command":"PlayPause","isPause":1}
{"command":"NewFrequency","frequency":92}
{"command":"State","buttonIndex":2,"isPause":0,"frequency":92}
```

- **`buttonIndex`**: `-1` no button, `0` Phono (unused), `1` L, `2` M, `3` K, `4` U.
- **`frequency`**: integer 87–105, 19 discrete positions (`RadioIO/src/CapacitanceState.cpp`).
- **`isPause`**: `1` = paused, `0` = playing.
- **`State`** is a full snapshot, sent only while the ESP32 holds the request-state pin high. The ESP32
  raises it once at boot to learn where the physical controls already are, then drops it.

Two consequences the ESP32 must handle, both because the Pico cannot be changed:

1. **No message terminator.** Frame by counting braces (or by an idle-gap timeout), not by `readStringUntil('\n')`.
2. **The dial streams `NewFrequency` while turning.** Debounce before acting — the Pi version used a
   ~1 s settle timer (`PlayerProcessorDebounceFrequencyService`). Without it, spinning the dial from 87
   to 105 would try to open 19 streams.

The data-ready interrupt pin is available but likely unnecessary: polling `Serial2.available()` in the
main loop is simpler and the ESP32 has nothing better to do. Keep the pin wired; treat using it as an
optimisation, not a requirement.

## 5. Station catalogue

Stored on a **LittleFS** partition, uploaded independently of the firmware with `pio run -t uploadfs`.
Editing stations never requires a recompile.

```
data/
  stations.csv
  logos/
    rockantenne.png
    swr3.png
```

`stations.csv` — one row per filled slot; unlisted slots are empty:

```csv
button,frequency,name,url,logo
L,87,Rock Antenne 80er,http://s1-webradio.rockantenne.de/80er-rock/stream/mp3,rockantenne.png
L,88,SWR3,http://liveradio.swr.de/...,swr3.png
```

Rules:

- `button` ∈ `L|M|K|U`; `frequency` ∈ 87–105. Duplicate `(button, frequency)` pairs are a data error.
- Logos are **pre-cropped and pre-scaled to 92×92 PNG** as part of preparing the data set. There is no
  runtime scaler; a logo that is not 92×92 is a data error. Source artwork from MyTuner is typically
  square and much larger (512×512 is common), so scaling is part of the MD milestone, not the firmware.
- Parsed once at boot into an in-memory index of 76 slots. 76 × (name + URL + filename) is a few KB —
  no need to keep the CSV open or re-read it.
- Stream URLs may be `http://` or `https://`; HTTPS is expected to be the majority (**D3**). Where a
  station offers both, **prefer the `http://` URL** — it is free RAM. Same for bitrate: prefer a 128 kbps
  variant over 320 kbps.
- Both MP3 and AAC are supported (**D15**). Where a station publishes both variants, **use the MP3 one**
  — see §5.2.

### 5.1 Where the station data comes from

The v1 Playwright scraper left a populated SQLite database at
`RadioApp/RadioApp/Data/RadioSettings.db` (read-only; it belongs to the frozen v1 tree). Measured
August 2026:

| | |
|---|---|
| `RadioStationInfos` rows (MyTuner candidates) | **1044** |
| …with a usable stream URL | **874** (170 have none) |
| …with a logo URL | 1044 (remote URLs, not stored images) |
| Scheme, of the 874 | **758 https / 116 http** → 87 % HTTPS |
| Codec guessed from the URL | 436 mp3 · 42 aac · 3 hls · 2 ogg · **391 unknown** |
| Countries covered | 3 |
| `RadioStation` rows already mapped to dial slots | **14**, with logos stored as base64 |

Two things this database is good for:

1. **A ready shortlist.** 874 candidates for ~76 slots is an 11× surplus, so the list can be filtered
   hard on technical grounds without losing stations worth having.
2. **A head start on content.** The 14 already-assigned rows carry button, frequency, name, stream URL
   *and* an embedded logo — enough to generate a first `stations.csv` plus logo files mechanically, and
   to exercise the whole data pipeline on real content before hand-curating anything.

Caveats that matter: the data is ~5 months old, so expect link rot; 45 % of URLs reveal no codec, so the
only reliable source of truth is the `Content-Type` header at connect time. Both are why M3 starts with
a validation pass rather than a hand-written CSV.

### 5.2 Prefer the MP3 variant of the same station

Many networks expose the same programme on several endpoints — Rock Antenne and Antenne Bayern serve
both `…/stream/mp3` and `…/stream/aacp`, and the v1 slot list contains both forms. When a station offers
a choice, prefer **MP3 at ~128 kbps** over AAC: it is the lighter decoder on a board with no RAM to
spare. This is a data-set rule, not a code rule — it costs nothing and shrinks the number of times the
heavier path is exercised.

Note this replaces the earlier idea of preferring `http://` endpoints, which the numbers above killed:
only 116 of 874 candidates are HTTP at all, and only 43 are HTTP *and* MP3. Too small and too arbitrary
a pool to select from.

## 6. Screen

ST7735, 160×128 landscape (MADCTL `0xA0`), RGB565, black background. The layout is carried over exactly
from `RadioApp.Hardware/DisplayManager.cs` so the radio looks unchanged — **minus the country flag**,
which is dropped.

```
 (0,0)                                    (159,0)
   ┌────────────────────────────────────────┐
   │                              87 MHz    │  frequency: x=100, y=2, white 0xFFFF
   │            ┌──────────┐                │
   │            │          │                │  logo: 92×92, y=13..104,
   │            │   LOGO   │                │        x centred = 33
   │            │          │                │
   │            └──────────┘                │
   │      Rock Antenne 80er                 │  station name: x=21, y=107, 0x8DF7
   │   AC/DC - Thunderstruck                │  song info:    x=3,  y=117, white
   └────────────────────────────────────────┘
 (0,127)                                (159,127)
```

- Font is the 5×7 bitmap from `RadioApp.Hardware/Helpers/Font5x7.cs` (6 px advance, clipped at the right
  edge). Port it rather than inventing a new one, so text metrics match.
- The song line is refreshed independently by blanking `y = 117..127` and redrawing — the Pi version's
  `CleanRadioSongInfoNotification`. If the stream sends no ICY title, the line stays empty.
- **Empty slot:** black screen, frequency only. No audio.
- **Paused:** black screen, frequency only. No audio. (Pausing a live stream means disconnecting; on
  play we reconnect at the live edge.)
- A status indicator for connecting / buffering / stream error / WiFi lost is wanted, but it must not
  disturb the layout above. Treat placement as an open item (§9.2).

## 7. Concurrency and the RAM budget

This is the tightest part of the design and the main technical risk, so it is specified rather than left
to emerge.

### 7.1 Two cores, two jobs

`audio.loop()` must be serviced continuously or the stream underruns. Two things in this design block for
a long time: **decoding a 92×92 PNG** and **opening a TLS connection**. Both happen exactly when a
station changes — i.e. exactly when audio is also starting. Running everything in `loop()` guarantees
audible glitches.

```
   Core 1  ── audio task (high priority) ──────────────────────
             ESP32-audioI2S: TLS read → decode → I2S
             owns the stream connection; nothing else runs here

   Core 0  ── everything else ─────────────────────────────────
             Arduino loop(): UART framing, debounce, CSV lookup,
             PNG decode, TFT drawing, WiFi events
```

The two sides communicate by a small command queue (change station / stop / play), never by shared
mutable state. The display side must never call into the audio object directly.

### 7.2 Where the RAM goes

ESP32-WROOM-32 has 320 KB DRAM; after Arduino and the WiFi stack, expect roughly 180–200 KB of usable
heap. Measured baseline (M0, the WiFi+HTTPS spike, statically allocated before WiFi connects):
**46,776 bytes — 8.8 %**. Rough shape of the budget while playing:

| Consumer | Approx. |
|---|---|
| TLS session (mbedTLS record buffers + state) | ~40 KB |
| Active decoder — MP3 or AAC, never both at once (**D15**) | ~30 KB MP3, more for AAC |
| Stream ring buffer | whatever is left, and it is the thing that absorbs network jitter |
| PNG decode line buffers (transient, station change only) | a few KB |

Both decoders live in flash, but only one has a RAM working set at a time, so the budget to plan against
is the **AAC** case. The §5.2 variant rule keeps the radio on the lighter MP3 path most of the time.

The stream buffer is the shock absorber, so **every KB saved elsewhere buys stability**. Concretely:
`setInsecure()` rather than a CA bundle (**D13**); one TLS connection at a time — fully stop the old
stream before opening the next; no framebuffer (TFT_eSPI draws straight out, PNGdec decodes line by
line); avoid `String` churn in the UART and CSV paths.

### 7.3 The real long-run risk

Not "does it play" — it will. The risk is **heap fragmentation over hours** of TLS connect/disconnect
cycles, which shows up as a radio that dies overnight rather than one that fails on the bench. That is
why M2 is a soak test with heap logging, and why M7 soaks again in the finished cabinet.

## 8. Decisions

| # | Decision | Rationale / consequence |
|---|---|---|
| D1 | **ESP32-WROOM-32** (AZ-Delivery DevKit C V4), 4 MB flash, **no PSRAM**. No new hardware. | Already bought and smoke-tested — the scaffold streamed 512 KB over HTTPS successfully. Explicit project constraint: make it work on this board. PSRAM is the *last* rung of the §9 ladder, not a plan. |
| D2 | Audio via **ESP32-audioI2S** (schreibfaul1) | Native `https://` support, follows CDN redirects, gives ICY titles and reconnects for free. Supersedes an earlier choice of ESP8266Audio, which was made when the stream list was assumed HTTP-only and which would need a hand-written `WiFiClientSecure` injection to do TLS on ESP32. |
| D3 | **HTTPS is the norm** | Measured, not assumed: 758 of 874 usable candidate URLs (87 %) are HTTPS (§5.1). Costs ~40 KB of RAM for the TLS session, which is the whole reason §7 exists. Selecting for HTTP is not a viable lever — only 116 candidates are HTTP. |
| D4 | Station data as **CSV + PNG on LittleFS** | Update stations with `pio run -t uploadfs`, no firmware rebuild. Costs ~40 KB flash for PNGdec. |
| D5 | Logos decoded at runtime with **PNGdec** (bitbank2), pre-sized to 92×92 | Keeps the workflow "drop a PNG in `data/logos/`". No runtime scaling. |
| D6 | **Partition table deferred** to after the logo set is measured | 4 MB must cover firmware + LittleFS (+ optionally an OTA slot). Locking it in before knowing what 76 logos cost risks a wrong split. **Repartitioning wipes LittleFS**, so this must be settled before the radio is reassembled — see M6. ⚠️ Measured at M0: the bare WiFi+HTTPS spike already fills **69.6 % of the default 1.25 MB app partition**. Adding the audio library with two decoders, TFT_eSPI, PNGdec and ArduinoJson will not fit. Expect to need a custom table *early* — likely at M1 — rather than at M6, and note this makes D7 (no OTA) close to forced. |
| D7 | **No OTA in v1**; USB reflash | Revisit at D6 time. If OTA is wanted, two ~1.3 MB app slots leave ~1.2 MB for logos. |
| D8 | WiFi credentials in a **gitignored `include/Secrets.h`** with a committed `Secrets.h.example` | Simplest thing that works. Changing networks = edit + reflash. **The current scaffold has real credentials inline in `src/main.cpp` — fixing that is M0 and must land before the directory is committed.** |
| D9 | **TFT_eSPI** for the display | Faster than Adafruit_ST7735 and configured entirely from `platformio.ini` build flags, which keeps the pinout in one place. Revisitable at M3 if its setup-header handling gets in the way. |
| D10 | Screen layout reproduced from the Pi version, **flag dropped** | Continuity with the videos; removing the flag also frees the top-right corner the frequency occupies. |
| D11 | `RadioIO/`, `RadioApp/`, `RadioFrontend/` are **frozen** | People arrive from four YouTube videos expecting that code to exist and match. New work goes in `Esp32InternetRadio/`. |
| D12 | Volume is the SABA's **analog pot** + a fixed software gain | No spare control input, and the Pico is frozen. |
| D13 | TLS with **`setInsecure()`** — no certificate verification | The payload is public audio; there is nothing to steal and no credentials in flight. A baked-in CA bundle costs RAM and flash *and* eventually expires, which would silently kill every station at once on a radio sitting in a cabinet. The scaffold already does this. |
| D14 | **Audio pinned to core 1**, everything else on core 0, joined by a command queue | PNG decode and TLS handshake both block for long enough to underrun the stream, and both happen exactly at station change. See §7.1. |
| D15 | **AAC is required**, alongside MP3 | Resolved by evidence rather than deferred. The aggregate pool looks 91 % MP3, but the 14 stations actually curated for v1 tell a different story: 3 are explicitly AAC (`…/stream/aacp` ×2, `SAM03AAC226_SC`) and 2 more almost certainly are (Global's `media-ssl.musicradio.com` endpoints) — roughly a third of real picks. Dropping AAC would cut ROCK ANTENNE, which is the demo station. Mitigated by the §5.2 variant rule, so AAC is the exception rather than the common path. |

## 9. Open items and the WROOM fallback ladder

### 9.1 If HTTPS streaming stutters on the WROOM

The constraint is no new hardware (**D1**), so this is the order to work through if M2's soak test fails.
Cheapest and least invasive first; each rung is a real lever, not a hope.

1. **Grow the stream buffer.** ESP32-audioI2S sizes its input buffer small when no PSRAM is present.
   Raise it as far as free heap allows and re-measure — this is the single biggest lever.
2. **Pick easier streams.** Prefer the MP3 variant over AAC where the same station offers both, and
   ~128 kbps over 320 (**§5.2**). Pure data-set discipline, no code. Note this is *not* "prefer HTTP" —
   the candidate pool is 87 % HTTPS, so that lever does not exist (**§5.1**).
3. **Confirm the core split is real** (**D14**). A blocking PNG decode or TFT redraw on the audio core
   looks exactly like a network problem.
4. **Trim TLS record buffers.** mbedTLS reserves 16 KB in and 16 KB out by default; a streaming server
   rarely needs that. Not reachable from the Arduino framework's prebuilt config — this rung means
   moving the build to ESP-IDF, so it is a real project cost, not a flag flip.
5. **Reboot-on-low-heap.** Ugly, but an appliance that restarts in 3 seconds once a week beats one that
   dies silently overnight. Acceptable as a backstop, not as a fix.
6. **ESP32-WROVER** (same chip + 4 MB PSRAM). Explicitly ruled out for now; the pin map already avoids
   GPIO 16/17 so this stays a `platformio.ini` change if it ever becomes unavoidable.

### 9.2 Still undecided

- **Status/error indicator placement** on a layout that is already full (§6).
- **Partition table** — pending the real logo set measurement (**D6**).
- **Reconnect policy**: how long to retry a dead stream, what the screen shows meanwhile, and whether a
  failed slot falls back to silence.
- **Boot behaviour** before WiFi is up: splash screen, and what happens if the network never arrives.
- **AAC** — counted at M3, decided then (**D15**).
