# Delivery Plan — ESP32 Internet Radio

Incremental path from the current scaffold (a WiFi + HTTPS connectivity test that prints
`Received: 512585 bytes`) to a finished radio. Design decisions referenced as **D1–D16** live in
[`Architecture.md`](Architecture.md).

Each milestone is independently testable and ends in a state you could stop at. Nothing outside
`Esp32InternetRadio/` and the repo-root docs is touched (**D11**).

```
M0 docs + secrets
      │
      ▼
M1 first sound ──▶ M2 stream robustness ──▶ M3 station catalogue ──▶ M4 display
      │                                          ▲                       │
      └── MD data mining (laptop, parallel) ─────┘    M5 Pico UART ◀─────┘
                                                          │
                                                          ▼
                                                  M6 install in cabinet
                                                          │
                                                          ▼
                                                     M7 polish
```

---

## M0 — Repo hygiene and documentation

*No hardware. Unblocks committing the ESP32 project at all.*

- Move the WiFi SSID/password out of `src/main.cpp` into a gitignored `include/Secrets.h`, add
  `include/Secrets.h.example`, extend `.gitignore` (**D8**).
- Commit `Esp32InternetRadio/` (currently untracked).
- Add the README paragraph about the project's new direction; land `Architecture.md`,
  `DeliveryPlan.md`, updated `CLAUDE.md`.

**Done when:** a fresh clone builds after the developer copies `Secrets.h.example` → `Secrets.h`, and
`git log -p` contains no credentials.

**Measured while verifying the build** — carry these into M1 as the baseline:

| | |
|---|---|
| Flash | 912,721 bytes — **69.6 % of the default 1.25 MB app partition** |
| Static RAM | 46,776 bytes — 8.8 % of 532 KB |

The flash number is the surprise: that is the *bare* WiFi + HTTPS spike, with no audio library, no
display and no JSON parsing. A custom partition table is therefore an **M1 problem, not an M6 one**
(**D6**), and it all but settles D7 against OTA.

---

## M1 — First sound ✅

*Breadboard only: ESP32 + I2S DAC. No UART, no TFT, no station data.*

**Status: done, August 2026.** Music plays over HTTPS out of a **PCM5102A** into headphones — the
design's intended output stage, at the design's intended pinout. Getting there cost a detour through a
dead DAC module; see *What actually happened* below.

- Wire the DAC per the pin map (**§3.1**), including the `SCK`/`FMT`/`XSMT` straps.
- Replace the HTTP byte-counter spike with ESP32-audioI2S (**D2**): `setPinout()` +
  `connecttohost()`.
- Use **the HTTPS URL you already smoke-tested** — `https://s1-webradio.rockantenne.de/80er-rock/stream/mp3`.
  Testing the easy HTTP case first would only hide the risk this milestone exists to expose (**D3**).
- `setInsecure()`, no CA bundle (**D13**). Fixed software gain (**D12**).
- Set up the core split now, not later: audio task pinned to core 1, `loop()` on core 0 (**D14**). It is
  far cheaper to start this way than to retrofit it in M4 when the display starts stealing time.
- Lock in a **custom partition table** (`partitions_radio.csv`) — D6's warning came true immediately.

**Done when:** recognisable music comes out of the DAC into headphones or a test amp, over HTTPS. ✅

### Measured at build time

ESP32-audioI2S 3.0.12, no display, no station data:

| | |
|---|---|
| Flash | 1,196,657 bytes — **95.7 % of the default 1.25 MB app partition**, 57.1 % of the new 2 MB one |
| Static RAM | 48,964 bytes — 9.2 % of 532 KB |

The audio library alone costs ~284 KB of flash over the M0 spike and only ~2 KB of static RAM; its real
cost is heap, allocated at connect time. The default partition table had ~53 KB of headroom left, so
`partitions_radio.csv` (2 MB app / 1.87 MB LittleFS / no OTA) landed here rather than at M6. It stays
**provisional** until the M3 logo measurement confirms the split — repartitioning wipes LittleFS, so it
must not move after M6.

### Measured while playing — the M2 baseline

| | |
|---|---|
| Stream buffer | **27,951 bytes** usable (30,000 requested via `setBufsize`; library default is 16,000) |
| Free heap while streaming | ~85,000 bytes, steady |
| **Largest free block** | **26,600–28,700 bytes**, steady across a run |
| Free heap after TLS handshake | 116,960 bytes |
| Free heap after MP3 decoder init | 88,400 bytes |
| Audio task stack headroom | 4,716 bytes free of 10,000 |
| Input buffer fill | pegged at 27,953/27,952, one dip to 27,535 |

These are the numbers M2's soak is compared against. The largest-free-block figure is the one that
matters (**§7.3**) — a slow decline in it over hours is the failure mode, not the average free heap.

### What actually happened — a dead PCM5102A module

The firmware was right on the first flash; the DAC board was not. The elimination order is worth
recording, because M6 re-runs parts of it against the cabinet wiring:

1. **The ESP32 side was proven independently of the DAC.** A `vu=` readout taken from the library's
   per-sample VU meter — sampled *before* the gain stage, so it cannot be faked by a volume setting —
   showed live music levels, and `i2s_write()` kept returning on schedule, which only happens if the
   peripheral is actually consuming 44,100 frames/s.
2. **Every input to the PCM5102A measured correct:** `SCK` grounded, `XSMT` 3.3 V (un-muted), `FMT` 0 V
   (I2S), `FLT`/`DEMP` 0 V, `A3V3` 3.3 V, and `BCK`/`DIN`/`LCK` all toggling at ~1.6 V DC.
3. **Output at `LOUT`/`ROUT` was 4–5 mV** against AGND, unloaded, where ~1 V was expected.
4. `src/ToneTest.cpp` (env `tone-test`) removed WiFi, TLS, the MP3 decoder and ESP32-audioI2S entirely
   and fed the I2S peripheral a generated 440 Hz sine. Still silent.
5. A **MAX98357A** on the same three pins played immediately — proving the ESP32 side conclusively.
6. **A second PCM5102A module** — same type, same wiring, same binary — played immediately too, into
   headphones from its line output.

So the fault was that one board, and nothing in the design or the firmware. The likely mechanism is its
hand-fitted header: the module ships with bare plated holes, and an unsoldered pin reads a healthy
1.6 V from the breadboard side while the chip sees nothing.

**The output stage is therefore settled as the PCM5102A** (**D16**), and the MAX98357A leaves the
project — it is a mono class-D *amplifier*, not a line-level DAC, and its bridged PWM output has no
ground reference to feed the SABA's 2.2 kΩ mixer and transformer.

Two things carry forward: **keep a spare PCM5102A**, and when a stage goes silent, work down this list
before touching firmware — `pio run -e tone-test` reaches step 4 in one command.

---

## M2 — Stream robustness — **go/no-go for the whole hardware choice**

*Same breadboard. This is the riskiest milestone in the project; everything after it assumes it passed.*

The question is not "does HTTPS play" — M1 answered that. It is whether a no-PSRAM WROOM survives **a
listening session**, and a session is defined by the usage profile in **§7.3**, not by uptime: 2–3 hours,
switched off afterwards, and dominated by station changes — 5–15 in a row while hunting for something
good, then 3–5 more every time an ad break starts. Call it **30–60 TLS teardown/setup cycles per
session**. That is the load this milestone has to survive, and it is a much sharper test than sitting on
one stream all evening.

- **The switch storm is the primary gate.** Automate 60 station changes back to back across 3–4 real
  stations, logging free heap and largest free block after each. Compare the largest block at change 60
  against change 1. A flat line passes. A steady per-change loss is the failure — and its *slope* is the
  number that matters, because it converts directly into "how many switches until the radio wedges".
- **Then hold for three hours** on one station, with the same 30 s heap log, to confirm nothing decays
  while merely playing. Secondary to the storm, but it is the actual use case.
- **Reconnect on stream drop, with backoff.** A server hanging up mid-song is routine and must recover
  by itself. **WiFi-loss recovery is explicitly best-effort** (**D17**) — the network here is reliable
  and power-cycling the radio is an acceptable answer.
- ICY metadata: hook the title callback and print `Artist - Title` to serial (M4 consumes it).
- Three or four real candidate stations, not just the M1 one, so the storm exercises different servers,
  bitrates and both codecs.

**Done when:** 60 consecutive station changes end with the largest free block where they started, a
three-hour hold on one station stays flat, and a dropped stream reconnects without a power cycle.

### Switch storm — passed, August 2026

60/60 changes, **0 connect failures, 0 stream drops**, across four stations spanning both codecs, TLS
and plain HTTP, and 48/128/256 kbps. Largest free block per station, 15 visits each:

| Station | 1st-half mean | 2nd-half mean | Drift |
|---|--:|--:|--:|
| ROCK ANTENNE 80er (HTTPS MP3 128) | 28,075 | 28,020 | −55 |
| WDR 4 (HTTPS MP3 128, redirects) | 27,490 | 27,636 | +146 |
| Radio X London (HTTPS AAC 48) | 29,684 | 29,684 | 0 |
| ELDORADIO (HTTP MP3 256) | 65,524 | 66,036 | +512 |

No trend in either direction. **The heap does not fragment across station changes on this board**, which
is the question M2 existed to answer.

### Three-hour hold — 72 minutes in, flat

ROCK ANTENNE 80er (HTTPS, MP3 128k), single connection, no reconnects at all:

| | |
|---|---|
| Largest free block | 25,588–28,660, first-half mean 27,664 → second-half 27,857 (**+193, upward**) |
| Min free heap | settled at **68,728 after ~5 minutes and never moved again** |
| `connects` / `fails` / `drops` | **1 / 0 / 0** across 72 minutes |
| Input buffer | 94–100 % full; dips of 1–2 KB lasting a minute or two, always recovering |

**A prediction that was wrong, usefully.** Four consecutive samples showed the buffer draining in
regular 418-byte steps — a 0.09 % deficit against a 16 kB/s stream, which looked like I2S playback
clock drift against the server's encoder, and implied an underrun about 35 minutes later. It never
came: the buffer refilled to 27,953 and thereafter dipped and recovered repeatedly, never below 94 %.
So the drain was transient network jitter that happened to look periodic over four samples. **The
regularity was in the sampling, not the mechanism** — worth remembering before reading a trend into a
handful of points from a 30-second logger.

### The decode-error bursts are periodic

Seven bursts in 72 minutes (~5.8/hour). Each resyncs in ~50 ms, is barely audible, and never drops the
connection — `drops=0` throughout, so the reconnect path correctly stays out of it. But the intervals
are not random:

```
17m39s · 4m35s · 13m02s · 4m35s · 4m34s · 4m34s
```

**274–275 s, four times consecutively**, and the resync offset is `pos 49` or `pos 50` on five of the
seven. A fixed period and a fixed offset mean something structural in the stream, not random packet
loss — most likely on the station's side. Unidentified; the test is whether a different station shows a
different period or none, which is what the `decerr=` counter is for.

Two measurement lessons, both learned by getting a wrong answer first:

- **The largest free block is quantised to 1024 bytes.** Every reading in a whole run came from the set
  {26612, 27636, 28660, 29684, 30708}. A verdict threshold finer than that step measures its own
  rounding — the first version used 32 bytes/change and called this dead-flat run MARGINAL.
- **A station's first visit is not a baseline.** Right after boot the heap is at its least fragmented,
  so the first sample reads high (30,708 here, never seen again) and every later sample looks like a
  loss. The warm-up visit is now discarded, and the verdict comes from first-half versus second-half
  means rather than two endpoints.

**Explicitly not required:** surviving days of uptime, or riding out a router reboot. See **D17**.

**If it fails:** work down the ladder in **§9.1** in order — buffer size, easier streams, verify the core
split, then the more expensive rungs. Do not proceed to M3 on a stream that is already marginal; every
later milestone only adds pressure to the same heap.

---

## MD — Station data mining *(laptop-only, runs in parallel with M1/M2)*

*No ESP32 involved. Off the critical path, but M3 is far cheaper if this is done first.*

The v1 scraper database (`RadioApp/RadioApp/Data/RadioSettings.db`, **read-only** — it is part of the
frozen tree) holds 874 usable candidates, MyTuner's own ratings and like counts, and 14 already-assigned
slots (**§5.1**). Target countries, in priority order: **Germany, United Kingdom, USA, Russia**; genres
of interest **rock/metal** and **pop**. Everything else is filler.

The database covers only two of those four countries (**§5.1**), so MD has two sources, not one.

**MD-1 — rank what the database already has.** Germany and the UK come almost entirely from here. Score
candidates by the §5.3 formula rather than raw likes, filter to the genres of interest, and emit a
ranked table per country and genre.

**MD-2 — fill what the database cannot supply.** ✅ *Done, August 2026.* The v1 scraper was re-run for
**Russia** (184 rows, complete) and **USA** (10,573 enumerated, 529 detailed before being stopped — see
the `StationProcessed` warning in **§5.1**). Because the US slice is an arbitrary 3.6 %, the well-known
US rock/metal and pop stations were researched separately and verified by probe; iHeart's public API
(`us.api.iheart.com/api/v2/content/liveStations?callLetters=…`) resolves real endpoints, where guessing
StreamTheWorld mount names does not. **SomaFM and WSOU cover the metal gap** the database never could —
only two of its rows carry a `Metal` tag.

**MD-3 — probe every candidate, from both sources.** ✅ `Tools/StationMining/probe.mjs`. Follows
redirects, records the `Content-Type` (the only reliable codec signal — 45 % of URLs reveal nothing) and
the ICY headers. **451 of 502 ranked candidates verified alive.** Three traps, each of which produced a
confidently wrong answer first:

- A live stream never ends, so curl always hits its time limit and exits non-zero. Treating that as
  failure marks *every working station* dead — it briefly looked like the US broadcasters were
  geo-blocking Germany. Judge the response headers, ignore the exit code.
- HLS manifests are served as `application/vnd.apple.mpegurl`, which contains `mpeg` and classifies as
  playable MP3 unless playlists are tested first. ESP32-audioI2S cannot follow HLS.
- The big broadcasters set a session cookie on their redirect hop and stall without it.

Record each server's `icy-name` beside the catalogue name: where they disagree the URL points at a
different station than the label claims, which is how the URL circulating as KNAC.com was caught
identifying itself as *J-Pop Powerplay Kawaii*.

**MD-3b — refine the surviving URLs** (`refine.mjs`, **§5.4**). Strip tracking parameters and re-probe;
swap `aacp` for its `mp3` sibling; rewrite ARD stations to `dispatcher.rndfnk.com`. Without this, 26
stations exceed `kMaxUrlLength` and `playUrl()` rejects them silently. Median URL afterwards: 47 bytes.

**MD-4 — migrate the 14 existing slots**: export button, frequency, name, URL and the base64 logo from
the `RadioStation` table into `stations.csv` + 92×92 PNGs. Free, real, already-curated content, and it
exercises the M3 pipeline before anything is hand-picked.

The output of MD-3 is a list of stations **confirmed to be playing right now**. Picking from it, and
cropping the logos, is a manual step — deliberately, because "is this station any good" is not a
question a script can answer.

**Tools** live in `Tools/StationMining/`: `rank.mjs` (§5.3 scoring) → `probe.mjs` → `refine.mjs` →
`shortlist.mjs`, with the verified output in `verified-stations.json` and `shortlist.md`.

**Done when:** there is a ranked, probe-verified candidate table covering all four countries, and a
`stations.csv` + logo set for the 14 v1 slots that M3 can load unmodified.
**Also produces:** a realistic per-logo byte size, which is the input to the partition decision
(**D6**) — the v1 base64 logos run 6–48 KB each, so a full 76-slot set plausibly lands near 0.5–1 MB.

---

## M3 — Station catalogue

*Still no UART and no screen — stations are selected by typing into the serial monitor.*

- LittleFS mount; `data/stations.csv` + `data/logos/` (**D4**).
- CSV parser → in-memory index of 4 banks × 19 slots (**§5**). Hand-rolled; no library needed.
- Serial debug command (e.g. `M 92`) to switch slots, so the whole selection path is exercised without
  the Pico.
- Empty slot = stop audio, no error (**§6**).
- Start from the MD output: load the migrated 14-slot `stations.csv` before hand-curating anything, so
  the pipeline is proven against real data.
- Prepare the **real** logo set: crop/scale to 92×92 PNG, then **measure the total size** — this is the
  input to the partition decision (**D6**).
- Verify an AAC station plays as well as an MP3 one (**D15**) — ROCK ANTENNE's `aacp` endpoint is the
  obvious test case, and its `mp3` sibling is the §5.2 comparison.

**Done when:** typing a bank+frequency plays the right stream, unknown slots go quiet, both codecs work,
and you know how many KB the full logo set occupies.

---

## M4 — Display

*Add the ST7735 to the breadboard; still driven by serial commands.*

- TFT_eSPI configured from `platformio.ini` build flags (**D9**).
- Port the 5×7 font from `RadioApp.Hardware/Helpers/Font5x7.cs` so text metrics match the Pi version.
- Reproduce the layout exactly: frequency `(100,2)`, logo 92×92 at `y=13` centred, name `(21,107)`, song
  `(3,117)`; independent blank-and-redraw of the song line (**§6**, **D10**).
- PNG logo decode with PNGdec (**D5**), on core 0 only — never on the audio core (**D14**).
- Empty-slot and paused states both render as frequency-only on black.

**Done when:** switching slots over serial redraws logo, name and frequency correctly, live ICY titles
appear on the bottom line and update as tracks change, and — the real test — **a station change produces
no audible glitch** while the new logo is being decoded and drawn.

---

## M5 — Pico UART integration

*Bench assembly: ESP32 + DAC + TFT + the existing Pico I/O board. Everything electrically loose.*

- UART2 at 115200; **brace-counting frame reader** — the Pico sends no terminator (**§4**).
- Parse the four commands with ArduinoJson; map `buttonIndex` → `L/M/K/U`, ignore `-1` and `0`.
- Raise the request-state pin once at boot to pull a full `State` snapshot, then drop it.
- Debounce `NewFrequency` (~1 s settle) so spinning the dial does not open 19 streams (**§4**).
- `PlayPause`: disconnect / reconnect at the live edge; paused renders frequency-only.

**Done when:** the physical dial and the four toggle buttons drive station changes on the bench, the
radio comes up already tuned to wherever the controls are sitting, and fast dial spins produce exactly
one stream connection.

---

## M6 — Install in the cabinet

*The irreversible-ish one. Do it only after M5 is solid.*

- **Lock the partition table** using the M3 measurement; decide OTA in or out (**D6**, **D7**). Note the
  interaction with **§9.1** rung 4: switching the build to ESP-IDF later would also disturb the flash
  layout, so if that rung was needed, do it before locking this.
  Repartitioning wipes LittleFS, so this comes before final assembly, not after.
- Plug the PCM5102A's 3.5 mm output into the summing network already in the cabinet — the resistor
  pair and transformer the Pi fed are reused unchanged — and set the fixed software gain for a clean
  level (**D12**, `Docs/SabaCircuit.md`).
- Rewire the TFT from the Pi's SPI header to the ESP32; remove the Pi 4.
- Mount and power the ESP32 from the radio's supply.
- Write `Docs/Esp32Wiring.md` with the **as-built** pin map, and correct **§3.1** if reality differed.

**Done when:** the radio plays through its own speaker, standalone, with only mains power connected.

---

## M7 — Polish and robustness

- Status/error indicator: connecting, buffering, stream failed, WiFi lost — placed without disturbing
  the layout (open item, **§9.2**).
- Boot splash and a sensible screen while WiFi is still connecting; defined behaviour if it never
  arrives.
- Watchdog / auto-recovery so a wedged stream cannot require a power cycle. If M2 showed slow heap decay
  that no ladder rung fully cured, this is where reboot-on-low-heap goes (**§9.1** rung 5).
- Multi-hour soak in the finished cabinet — the same heap logging as M2, now with the display and UART
  both running.
- Fill out the full 76-slot station set and re-upload with `pio run -t uploadfs`.

**Done when:** the radio survives being switched on and used like a radio for a day without a laptop
nearby.

---

## Not in this plan

- The replacement media device — separate project, separate repository (**§1**).
- Any change to `RadioApp/`, `RadioFrontend/`, `RadioIO/` (**D11**).
- A web UI or any hosted service for editing stations. Station updates are a `uploadfs` from a laptop.
