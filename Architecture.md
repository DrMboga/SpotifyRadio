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

### 3.1 ESP32 pin map

The I2S trio is wired and verified (M1). The rest is still proposed — to be validated during the
remaining milestones and corrected here as the single source of truth. Avoids strapping pins
(0, 2, 12, 15) and flash pins (6–11).

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
`VIN`→5 V (the board has its own regulator), `GND`→GND.

The mono mix **already exists inside the cabinet** and is reused unchanged, so there is no resistor pair
to build: the Pi's 3.5 mm output fed two 2.2 kΩ summing resistors into a 47 kΩ shunt and an audio
transformer whose secondary drives the SABA amplifier (see [`Docs/Wiring.md`](Docs/Wiring.md)). A 3.5 mm
male-to-male cable from the DAC's own jack replaces the Pi's. The network is passive and unchanged, so
the only thing that moves is level — the PCM5102A's 2.1 Vrms full scale is roughly 5× the Pi's
~0.4 Vrms, about +14 dB, which the fixed software gain absorbs (**D12**; `kFixedGain` ≈ 9 on the
library's 0–21 square curve reproduces the Pi's level).

**Verified at M1:** the I2S pinout above works and the **PCM5102A is the output stage** — music plays
out of its line output into headphones, over HTTPS, from the real firmware. The first module tried was
simply a bad board: it accepted valid I2S on all three pins and converted nothing. A second module of
the same type, on the same wiring and the same binary, played immediately. See `DeliveryPlan.md` M1 for
the elimination trail — worth keeping, because it is also the procedure for bringing up the replacement
in the cabinet at M6.

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
- **No quoting, so no field may contain a comma.** The parser is hand-rolled and splits on commas; a
  comma in a name or a URL silently shifts every field after it. Real URLs do carry them — Pinguin
  Vintage’s `f=mp3,any&br=192000,any` is one — so the strip pass in §5.4 is what keeps this true, and
  `Tools/StationMining/build-data.mjs` refuses to emit a row that would break it.
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
| `Countries` rows (the scraper's *worklist* of places) | **139**, each with its MyTuner entry URL |
| …of those 139 scraped into `RadioStationInfos` | **5**, after the August 2026 Russia/USA run |
| Rows per country | UK 578 · Germany 449 · **Russia 184** · Luxembourg 17 · **United States 10,573** |
| Rows carrying a non-zero `Rating` | 284 of 1044 — **760 are `0`, meaning unrated, not bad** |
| `Likes` / `Dislikes` | present on every row; likes run 0–1056, long-tailed |
| `Genres` | free text, pipe-separated — 103 rows tagged `Rock`, 56 `Classic Rock`, 270 `Pop Music`, **2 `Metal`** |
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

⚠️ **Filter every query on `StationProcessed = 1`.** The USA run enumerated all 10,573 stations but was
stopped after detailing **529** of them; the remaining 10,044 rows are names and detail URLs with no
stream URL, rating or genre. They also **resume by themselves**: `MyTunerCachingBackgroundService` calls
`CheckForUncachedStations` on startup, so merely launching the v1 backend for any reason restarts that
scrape. Processed rows with a usable stream URL: Germany 424 · UK 438 · **USA 382** · **Russia 115** ·
Luxembourg 12.

**Two tables carry a country, and they mean different things** — worth stating plainly, because the
database looks globally stocked at a glance and is not:

- `Countries` is the scraper's **worklist**: 139 countries, each with the MyTuner URL to walk. Russia
  and the United States are both in it, with valid URLs.
- `RadioStationInfos` is the **result**: 1044 station rows, all of them from **three** countries.
  `select count(*) … where Country='United States'` returns 0, and so does Russia.

The v1 scraper covered 3 of its 139 countries before v1 was finished with. Every row it did produce is
marked `StationProcessed = 1`, so this is not an interrupted run to be resumed — it is a worklist that
was never started for the other 136.

**The two gaps that leaves**, which set the shape of MD:

- **No USA and no Russia rows**, though both are named in `Countries` with a usable entry URL. Two of
  the four target countries have to come from outside `RadioStationInfos`.
- **Almost no metal.** Two rows carry the `Metal` tag. Rock proper is genuinely covered (103 + 56
  rows); metal is not.

Everything found outside the database joins the same candidate table and goes through the identical
probe — the point of the probe is that no URL reaches `stations.csv` unverified, whatever its origin.

### 5.2 Prefer the MP3 variant of the same station

Many networks expose the same programme on several endpoints — Rock Antenne and Antenne Bayern serve
both `…/stream/mp3` and `…/stream/aacp`, and the v1 slot list contains both forms. When a station offers
a choice, prefer **MP3 at ~128 kbps** over AAC. This is a data-set rule, not a code rule.

⚠️ **The original reason for this rule — "AAC is the heavier decoder" — was measured at M3 and is wrong.**
Taking the free-heap delta across decoder init on the board:

| | before init | after init | decoder cost |
|---|---|---|---|
| AAC (`L 92`, 22.05 kHz, 59.8 kbps HE-AAC) | 107,372 | 82,300 | **25,072** |
| MP3 (`L 96`, 44.1 kHz, 128 kbps) | 105,268 | 77,104 | **28,164** |

AAC came out **3,092 bytes cheaper**, not dearer. One caveat keeps this from settling the question: the
two streams differ in sample rate as well as codec, and decoder buffers scale with sample rate, so this
is not a clean codec-vs-codec comparison. It is enough to retire the stated justification, not enough to
reverse the rule.

**The rule stands on a different footing.** MP3 endpoints in this catalogue are consistently the higher
bitrate and the more widely served, and the shipped set is already 46 MP3 to 14 AAC, so nothing is
gained by re-picking endpoints. **Both codecs are verified working on the board (D15)** — the choice is
now a preference rather than a constraint, and an AAC-only station is no longer a reason to reject a
candidate. If the question ever matters, measure both at the same sample rate.

Note this rule replaced an earlier idea of preferring `http://` endpoints, which the numbers above
killed: only 116 of 874 candidates are HTTP at all, and only 43 are HTTP *and* MP3. Too small and too
arbitrary a pool to select from.

### 5.3 Ranking candidates: use the like ratio, not the like count

MyTuner's own numbers are in the database, but neither field can be sorted on naively.

`Rating` is `0` for 73 % of rows, and `0` means *unrated*, not *bad* — Radio X London carries 355 likes,
12 dislikes and a `Rating` of `0`. Sorting by `Rating` descending silently buries the unrated stations;
treating `0` as a score sorts them to the bottom, which is worse. **`Rating = 0` must be read as
missing.**

`Likes` is an absolute count over a long tail, so sorting by it ranks by *audience size*, not quality:
the top of that list is London pop — Heart, Capital, Smooth — because London is large, not because those
stations suit this radio.

What actually ranks well is the **ratio, discounted by confidence**: the lower bound of a Wilson score
interval on `Likes / (Likes + Dislikes)`. A station with 40 likes and 0 dislikes then outranks one with
600 likes and 70, while a station with 2 likes and 0 dislikes does not outrank either — which is the
behaviour wanted, since 76 slots need *good* stations, not *famous* ones. Where `Rating` is non-zero it
enters as a mild tiebreak, never as the primary key.

Applied after filtering by country and genre, never before: a top-ranked Luxembourgish talk station is
not a better pick than a mid-ranked German rock station.

**And the genre filter has to speak two languages.** MyTuner changed its taxonomy between the original
UK/German scrape and the August 2026 Russia/USA one, and the vocabularies barely overlap:

| Old (UK, Germany) | New (Russia, USA) |
|---|---|
| `Pop Music` — 289 rows, 0 new | `Pop / Top 40` — 0 old rows, 94 new |
| `Alternative Rock` — 21 rows, 0 new | `Alternative / Indie` — 0 old rows, 42 new |
| `Classic Rock` | `Classic Rock` (one of the few that survived) |

A filter written for either vocabulary alone silently drops half the catalogue — and silently is the
problem: the query returns rows, just not the right ones. Match both.

### 5.4 URL hygiene: what the firmware will actually accept

A URL that plays in a browser is not automatically one this radio can use, and both failure modes are
silent.

**192 bytes is a hard cap.** `AudioEngine::kMaxUrlLength` is 192 and `playUrl()` returns `false` past it,
so an over-long URL is a station that never plays and never says why. **26 of the scraped URLs are over
the cap**, the longest 1707 characters — MyTuner stores the URL with every tracking parameter the player
page attached.

**Most of that query string is disposable, but not all of it.** Aggregator tags, consent blobs, listener
ids and cache-busters strip cleanly. Timestamped auth tokens do not — and a URL that works today because
it carries a fresh token is a station that dies next month. So the rule is *strip, then re-probe*: keep
the shortened URL only if it still plays, and fall back to the original if the token turned out to be
load-bearing.

**German public radio needs its canonical host.** ARD stations are scraped as token-issued edge hosts
(`f131.rndfnk.com/ard/rbb/fritz/live/mp3/128/stream.mp3?token=…&tvf=…`), which are both over the cap and
token-dependent. The canonical form is `https://dispatcher.rndfnk.com/{broadcaster}/{station}/mp3/mid` —
around 50 characters, no token, same audio. That recovered Bayern 1 and 3, Fritz, radioeins, Inforadio,
rbb 88.8 and Antenne Brandenburg. Radio Bremen and MDR use their own hosts
(`icecast.radiobremen.de`, `mdr-…sslcast.mdr.de`) in the same spirit.

After this pass the whole verified set has a **median URL of 47 characters** against the 192 cap, which
also means `stations.csv` stays small enough to parse into RAM at boot (§5).

## 6. Screen

ST7735, 160×128 landscape (MADCTL `0xA0`), RGB565, black background. The layout is carried over exactly
from `RadioApp.Hardware/DisplayManager.cs` so the radio looks unchanged — **minus the country flag**,
which is dropped.

```
 (0,0)                                    (159,0)
   ┌────────────────────────────────────────┐
   │                            L 92 MHz    │  frequency: x=100, y=2, white 0xFFFF
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

- The frequency line is `"{bank} {frequency} MHz"`, which is what
  `InternetRadioPlayerProcessor.Reset()` built. The bank letter matters: the same frequency exists
  on all four banks, so without it the top-right corner cannot tell `L 92` from `U 92`. Nine
  characters at `x=100` end at 154, inside the screen.
- Font is the 5×7 bitmap from `RadioApp.Hardware/Helpers/Font5x7.cs` (6 px advance, clipped at the right
  edge). Port it rather than inventing a new one, so text metrics match.
- **That font is ASCII-only, which makes it a rule about the data, not just the code.** It has no glyph
  above U+007F, so a station name carrying one draws as rubbish — and the CSV parser is byte-transparent,
  so nothing complains until the screen does. `Tools/StationMining/build-data.mjs` fails the build on it.
- At `x=21` with a 6 px advance on a 160 px screen, **23 characters fit** and the rest is clipped. That is
  the intended behaviour, but it does cut the distinguishing half off the longer names, so the build
  reports every name that will clip.
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
| Active decoder — MP3 or AAC, never both at once (**D15**) | **measured at M3: 28.2 KB MP3, 25.1 KB AAC** — AAC is the cheaper of the two, not the dearer (§5.2) |
| Stream ring buffer | whatever is left, and it is the thing that absorbs network jitter |
| Logo band buffer, static (**D5 as revised at M4**) | 4,232 bytes — 23 of the 92 rows |
| Image decoder | **none.** PNGdec needed 45,604 contiguous bytes and was removed at M4 |

**There is no image decoder in that table, and its absence is the main thing M4 established.**
PNGdec was the plan (**D5**) and does not fit. `PNGIMAGE` embeds zlib's 32 KB sliding window plus
its inflate state, a 1 KB palette and a 2 KB file buffer, so `sizeof(PNG)` is 45,604 bytes however
the object is created — and the board has a block that size only at boot. It was tried in all
three places it could go, and the board rejected each in a different way:

- **In `.bss` it boot-loops.** Static DRAM here is 124,580 bytes, not the 532,480 `pio` reports:
  `.data` + `.bss` live in `dram0_0_seg` (`org 0x3ffbdb5c, len 0x1e6a4` in the map), and the rest
  of what `pio` counts is address space the linker cannot use. 45,604 bytes of `.bss` left 27,040
  free in that segment against M3's 73,320, and ESP-IDF could no longer allocate its main task
  stack — `assert failed: esp_startup_start_app_common port_common.c:81`, before `setup()` ran.
- **On the heap for the session it starves TLS.** Allocated once in `begin()` the board booted and
  decoded a logo in 94 ms, but every connect then failed with `BIGNUM - Memory allocation failed`
  at `heap=101304 largest=36852`. mbedTLS needs those 45 KB more than the screen does.
- **Per decode it is unavailable when it is wanted.** With an HTTPS stream live the largest free
  block is 14,324–19,444, and *even with the stream stopped* it is 38,900 — the input ring buffer
  is not released by `stopSong()`. So the allocation succeeds at boot and on plain-HTTP stations
  and nowhere else, and the catalogue is 87 % HTTPS (§5.1).

The logos are therefore converted to raw RGB565 by `Tools/StationMining/build-data.mjs` and read a
band at a time off LittleFS (**D5**). What is left on the device is a 4,232-byte static buffer and
a file read. **TFT_eSPI costs 672 bytes** — it keeps no framebuffer — so M4's entire RAM cost is
under 5 KB.

Two things worth carrying forward from how this was found:

> **`pio`'s RAM percentage is measured against the wrong denominator.** It divides by 532,480, the
> whole DRAM address space. The number that decides whether the board boots is `_bss_end` against
> the end of `dram0_0_seg`, and it is in `.pio/build/*/firmware.map`. A build reporting a
> comfortable 18.3 % was 27 KB from the edge.
>
> **Free heap is not the constraint; the largest free block is.** Every failure above happened with
> 74–124 KB free. §7.3 already said this about fragmentation across station changes; M4 is the same
> lesson arriving as a single allocation that is simply too big to place.

The price is paid in flash rather than RAM: the payload goes from 748 KB to **~1,208 KB of the
1.87 MB LittleFS partition** (60 × 16,928 bytes, rounded up to 4 KB blocks), against 26 KB of
application flash given back with the library. **D6** still holds with room to spare.

Both decoders live in flash, but only one has a RAM working set at a time, so the budget to plan against
is the **AAC** case. The §5.2 variant rule keeps the radio on the lighter MP3 path most of the time.

The stream buffer is the shock absorber, so **every KB saved elsewhere buys stability**. Concretely:
`setInsecure()` rather than a CA bundle (**D13**); one TLS connection at a time — fully stop the old
stream before opening the next; no framebuffer and no image decoder (TFT_eSPI draws straight out, and
the logos arrive pre-rendered a band at a time); avoid `String` churn in the UART and CSV paths.

### 7.3 The real risk, sized against how the radio is actually used

Not "does it play" — it will. The risk is **heap fragmentation across TLS connect/disconnect cycles**.
But how bad that is depends entirely on the duty cycle, so the duty cycle is written down here rather
than assumed:

| | |
|---|---|
| Session length | **2–3 hours**, then switched off at the mains |
| Station changes while hunting | **5–15 in a row** |
| Station changes per ad break | **3–5 in a row**, several times a session |
| Station changes per session | **~30–60** |
| Days of continuous uptime expected | **none** |

So the dominant operation is **switching**, not streaming, and the unit of survival is a session, not a
week (**D17**). This reframes the fragmentation risk usefully: what matters is the heap cost *per
station change*, because that is the thing that repeats 60 times an evening. A leak of 200 bytes per
change is 12 KB by the end of a session — survivable, and cleared by the power switch. The same leak
would be fatal to an appliance expected to run for weeks, and this radio is not one.

It also says where to spend effort: **reconnect-after-drop matters, WiFi-loss recovery does not.** A
stream server hanging up mid-song happens during normal listening; a router outage does not, and if one
happens the answer is the power switch.

That is why M2 leads with a 60-change switch storm rather than an overnight soak, and why M7 re-runs the
same profile in the finished cabinet with the display and UART also running.

**Measured at M2 (August 2026): no fragmentation.** 60 changes across four stations drifted by −55 to
+512 bytes per station, in both directions — see `DeliveryPlan.md` M2. Two things to know before
reading any such measurement, because both produced a wrong answer first:

- The largest free block is **quantised to 1024 bytes**, so any threshold finer than 1 KB is measuring
  rounding rather than the heap.
- **Discard each station's first sample.** The heap is least fragmented immediately after boot, so a
  first reading runs about 2 KB high and makes a flat run look like a slow leak.

And compare a station only against *itself*: the bench stations sit up to 43 KB apart because a plain
HTTP stream pays no TLS cost, which swamps any real signal.

## 8. Decisions

| # | Decision | Rationale / consequence |
|---|---|---|
| D1 | **ESP32-WROOM-32** (AZ-Delivery DevKit C V4), 4 MB flash, **no PSRAM**. No new hardware. | Already bought and smoke-tested — the scaffold streamed 512 KB over HTTPS successfully. Explicit project constraint: make it work on this board. PSRAM is the *last* rung of the §9 ladder, not a plan. |
| D2 | Audio via **ESP32-audioI2S** (schreibfaul1) | Native `https://` support, follows CDN redirects, gives ICY titles and reconnects for free. Supersedes an earlier choice of ESP8266Audio, which was made when the stream list was assumed HTTP-only and which would need a hand-written `WiFiClientSecure` injection to do TLS on ESP32. **Pinned to tag 3.0.12** — the last release supporting Arduino core 2.x / ESP-IDF 4.4, which is what `platform = espressif32` installs here. 3.1.0+ requires Arduino core 3.x and will not compile against this framework. |
| D3 | **HTTPS is the norm** | Measured, not assumed: 758 of 874 usable candidate URLs (87 %) are HTTPS (§5.1). Costs ~40 KB of RAM for the TLS session, which is the whole reason §7 exists. Selecting for HTTP is not a viable lever — only 116 candidates are HTTP. |
| D4 | Station data as **CSV + logos on LittleFS** | Update stations with `pio run -t uploadfs`, no firmware rebuild. The logos are `.565` rather than PNG since M4 — see the revised **D5** — so the payload is ~1.2 MB and the firmware carries no decoder. |
| D5 | ~~Logos decoded at runtime with **PNGdec**~~ → **Logos pre-rendered to raw RGB565 by `build-data.mjs`; no decoder on the device.** Revised at M4. | The original kept the workflow "drop a PNG in `data/logos/`", and PNGdec's line callback avoided ever holding a whole frame. Both were true and it still could not work: the decoder object itself is 45,604 contiguous bytes, and the board has a block that size only at boot (§7.2 has the three failures). A `.565` file is 92×92 little-endian RGB565 and nothing else — 16,928 bytes, no header — read in four bands into a 4,232-byte static buffer, so the logo path allocates nothing and cannot fail for want of memory. **Costs ~1.2 MB of the 1.87 MB LittleFS partition** (up from 748 KB) and gives back 26 KB of flash. The workflow survives in a weaker form: the PNG still drops into `Tools/StationMining/Assets/`, but it now has to go through `node build-data.mjs` rather than straight into `data/logos/`. Drawing costs 67–174 ms depending on what the audio task is doing to the flash cache — slower than PNGdec's 93–108 ms, which is the honest price of the change. |
| D6 | **Custom partition table**, `Esp32InternetRadio/partitions_radio.csv`: 2 MB `factory` app, 1.87 MB LittleFS, no OTA slot. Provisional until M3 measures the real logo set. | 4 MB must cover firmware + LittleFS (+ optionally an OTA slot). ⚠️ It landed at M1, not M6, exactly as feared: the M0 WiFi+HTTPS spike already filled 69.6 % of the default 1.25 MB app partition, and merely adding ESP32-audioI2S took that to **95.7 %** — before TFT_eSPI, PNGdec or ArduinoJson. That also settles D7. The filesystem partition keeps the `spiffs` *subtype* (the IDF 4.4 partition generator and PlatformIO's `uploadfs` both key off it) while being formatted LittleFS via `board_build.filesystem`. **Repartitioning wipes LittleFS**, so the numbers must stop moving before the radio is reassembled — see M6. |
| D7 | **No OTA in v1**; USB reflash | Revisit at D6 time. If OTA is wanted, two ~1.3 MB app slots leave ~1.2 MB for logos. |
| D8 | WiFi credentials in a **gitignored `include/Secrets.h`** with a committed `Secrets.h.example` | Simplest thing that works. Changing networks = edit + reflash. **The current scaffold has real credentials inline in `src/main.cpp` — fixing that is M0 and must land before the directory is committed.** |
| D9 | **TFT_eSPI** for the display | Faster than Adafruit_ST7735 and configured entirely from `platformio.ini` build flags, which keeps the pinout in one place. Revisitable at M3 if its setup-header handling gets in the way. |
| D10 | Screen layout reproduced from the Pi version, **flag dropped** | Continuity with the videos; removing the flag also frees the top-right corner the frequency occupies. |
| D11 | `RadioIO/`, `RadioApp/`, `RadioFrontend/` are **frozen** | People arrive from four YouTube videos expecting that code to exist and match. New work goes in `Esp32InternetRadio/`. |
| D12 | Volume is the SABA's **analog pot** + a fixed software gain | No spare control input, and the Pico is frozen. |
| D13 | TLS with **`setInsecure()`** — no certificate verification | The payload is public audio; there is nothing to steal and no credentials in flight. A baked-in CA bundle costs RAM and flash *and* eventually expires, which would silently kill every station at once on a radio sitting in a cabinet. The scaffold already does this. |
| D14 | **Audio pinned to core 1**, everything else on core 0, joined by a command queue | PNG decode and TLS handshake both block for long enough to underrun the stream, and both happen exactly at station change. See §7.1. |
| D15 | **AAC is required**, alongside MP3 | Resolved by evidence rather than deferred. The aggregate pool looks 91 % MP3, but the 14 stations actually curated for v1 tell a different story: 3 are explicitly AAC (`…/stream/aacp` ×2, `SAM03AAC226_SC`) and 2 more almost certainly are (Global's `media-ssl.musicradio.com` endpoints) — roughly a third of real picks. Dropping AAC would cut ROCK ANTENNE, which is the demo station. Mitigated by the §5.2 variant rule, so AAC is the exception rather than the common path. |
| D16 | **Output stage settled: PCM5102A**, line-level stereo DAC into the SABA's existing mono mixer. Closed at M1. | Briefly reopened when the first PCM5102A module produced ~5 mV from valid I2S; a MAX98357A was wired to the same three pins purely to prove the ESP32 side, and it played. A **second PCM5102A** then played too, so the first board was faulty and nothing about the design was wrong. The MAX98357A is out of the project: as a mono class-D *amplifier* its output is a bridged PWM pair with no ground reference, so it cannot feed the SABA's 2.2 kΩ mixer and transformer (§3.1) — it would have to drive the speaker directly, bypassing the SABA amplifier and losing the analog volume pot. Practical consequences: keep a spare PCM5102A, and treat "valid I2S in, no audio out" as a suspect board before suspecting firmware. |
| D17 | **Availability target is session-scale, not appliance-scale**: 2–3 hours of use, ~30–60 station changes, then switched off. Reconnect-after-stream-drop is required; **WiFi-loss recovery is best-effort and a power cycle is an acceptable remedy**. | Set from how the radio is actually used (§7.3), and it changes what the engineering has to prove. The dominant cost is the heap delta *per station change* — 60 of those an evening — not drift over days of uptime. Slow decay measured in KB-per-day is therefore not a defect: the mains switch resets it nightly. This is what lets M2 be a switch storm instead of an overnight soak, keeps §9.1 rung 5 (reboot-on-low-heap) as a backstop that will probably never be needed, and stops effort going into WiFi state-machine recovery that would be exercised perhaps twice a year. |

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

- **Status/error indicator placement** on a layout that is already full (§6). M4 built the screen
  without one, so a connect in progress, a retry and a dead slot are all still indistinguishable
  from a station that simply has nothing to say.
- ~~**Whether to keep decoding PNG on the device at all.**~~ **Closed at M4:** it cannot be done —
  see §7.2 and the revised **D5**. Logos are pre-rendered to RGB565 by the build.
- ~~**Logo draw time under load.**~~ **Closed at M4:** 67 ms when core 1 is blocked on a network
  handshake, 133 ms while it decodes a steady MP3, 174 ms when both happen at once — the spread is
  flash-cache contention between the two cores rather than CPU. **Inaudible**, confirmed by ear, which
  is D14 earning its keep.
- **Partition table** — pending the real logo set measurement (**D6**).
- **Reconnect policy** — the retry ladder is settled and implemented at M2: 1 s doubling to 30 s, no
  attempt limit, cancelled only by an explicit stop. **Untested in the field**: 158 minutes of M2
  produced no drop at all. What the *screen* shows during a retry is still open (M4/M7), as is whether a
  permanently failed slot falls back to silence.
- **Boot behaviour** before WiFi is up: splash screen, and what happens if the network never arrives.
- ~~**AAC** — counted at M3, decided then (**D15**).~~ **Closed at M3:** both decoders initialise and
  play from the catalogue on the board. AAC cost 25.1 KB against MP3's 28.2 KB, so the §5.2 preference
  for MP3 keeps its effect but loses its original reason.
