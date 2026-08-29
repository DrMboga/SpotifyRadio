# Delivery Plan — ESP32 Internet Radio

Incremental path from the original scaffold (a WiFi + HTTPS connectivity test that printed
`Received: 512585 bytes`) to a finished radio. Design decisions referenced as **D1–D17** live in
[`Architecture.md`](Architecture.md).

Each milestone is independently testable and ends in a state you could stop at. Nothing outside
`Esp32InternetRadio/`, `Tools/` and the repo-root docs is touched (**D11**).

**Where things stand (August 2026): M0–M4 and MD are done.**
The radio plays internet radio over HTTPS through a PCM5102A and draws the Pi version's screen —
frequency, 92×92 logo, station name and live ICY titles, on an ST7735 driven from the serial console.
All **76 dial slots are filled**, station changes on a live HTTPS stream draw the new logo with
`fails=0`, the 60-change storm passes with the screen running, and **station changes are inaudible**.

M4 cost **D5**: there is no image decoder on the board any more. PNGdec needs a 45,604-byte contiguous
allocation and the ESP32 has one only at boot, so the logos are pre-rendered to raw RGB565 by
`build-data.mjs` — see *The 45 KB that would not fit*.

Next is **M5**: the Pico UART, which replaces the serial console with the real dial and buttons.

```
M0 ✅ docs + secrets
      │
      ▼
M1 ✅ first sound ──▶ M2 ✅ stream robustness ──▶ M3 ✅ catalogue ──▶ M4 ✅ display
      │                                          ▲                       │
      └── MD ✅ data mining (laptop, parallel) ──┘    M5 Pico UART ◀─────┘
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

## M2 — Stream robustness ✅ — **go/no-go for the whole hardware choice**

*Same breadboard. This was the riskiest milestone in the project; everything after it assumed it passed.*

**Status: passed, August 2026.** The 60-change switch storm and a 98-minute TLS hold both came out flat,
with zero connect failures and zero stream drops across either. **A no-PSRAM WROOM-32 can run this
radio** — the open question the whole ESP32 plan rested on, and the reason §9.1's fallback ladder
exists. No rung of it was needed.

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

**Outcome:** the first two are met (details below). The third was never exercised — across 158 minutes
of running, **not one stream drop occurred**, so the reconnect ladder is implemented and reasoned about
but unproven in the field. M7's cabinet soak is the next chance to see it fire; if it still has not,
force one by blocking the stream at the router.

The hold ran **98 minutes rather than three hours**, stopped deliberately. Min free heap had been static
for 70 of them, and the only question left open — the 274 s decode-error period below — cannot be
answered by more of the same station.

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

### Three-hour hold — 98 minutes, flat

ROCK ANTENNE 80er (HTTPS, MP3 128k), single connection, no reconnects at all:

| | |
|---|---|
| Largest free block | 25,588–28,660 throughout; half-over-half drift **+193 then +461, both upward** |
| Min free heap | 68,728 from minute 5, held **~70 minutes**, then one 676-byte step to 68,052 and held again |
| `connects` / `fails` / `drops` | **1 / 0 / 0** across the whole 98 minutes — the connection was never re-established |
| Input buffer | 90–100 % full; dips of 1–3 KB lasting a minute or two, always recovering |

**A prediction that was wrong, usefully.** Four consecutive samples showed the buffer draining in
regular 418-byte steps — a 0.09 % deficit against a 16 kB/s stream, which looked like I2S playback
clock drift against the server's encoder, and implied an underrun about 35 minutes later. It never
came: the buffer refilled to 27,953 and thereafter dipped and recovered repeatedly, never below 94 %.
So the drain was transient network jitter that happened to look periodic over four samples. **The
regularity was in the sampling, not the mechanism** — worth remembering before reading a trend into a
handful of points from a 30-second logger.

### The decode-error bursts are periodic

Eleven bursts in 98 minutes (~6.7/hour). Each resyncs in ~50 ms, is barely audible, and never drops the
connection — `drops=0` throughout, so the reconnect path correctly stays out of it. The intervals are
emphatically not random:

```
1059 · 275 · 782 · 275 · 274 · 274 · 783 · 274 · (812) · 245     seconds
        ^^^         ^^^   ^^^   ^^^         ^^^
```

**Seven intervals at 274–275 s.** The long gaps cluster too — 782, 783, and 1057/1059 twice — and
1057 − 782.5 = 274.5, so the same quantum shows up in the gaps between missed events. (The 812/245 pair
straddles a host-side serial flush and its timestamps are unreliable; the 20:30 → 20:48 span is 1057 s,
matching the other long gap.)

What does *not* fit a simple "some events missed" model: 782.5 is 2.85 × 274.5, not an integer multiple.
So there is certainly a ~274.5 s quantum, and it is not fully explained.

**Correction to an earlier reading:** the resync offset is *not* fixed. Over 11 bursts it is
`pos 49`/`50` eight times, but also `17` and `417` twice. Consistent, not constant — the first seven
samples made it look tighter than it is.

A period this clean is structural, and most likely on the station's side rather than the network's. The
test is whether another station shows a different period or none, which is what the decode-error count
is for — and more of the same station will not answer it.

**Carried forward as an open item.** Cheap to settle: 30 minutes on WDR 4, comparing `decode=`. If the
period follows the radio it is local and worth chasing; if it changes or vanishes it belongs to ROCK
ANTENNE, and the answer is §5.2's — prefer a different endpoint.

⚠️ **This needs the M3 firmware, not the M2 firmware.** The count was never on the `[stat]` line: it
was passed to `Log::printf` but no specifier consumed it, so it was printed under the label `rssi=`
and the real signal strength was dropped. The burst *timings* above are unaffected — they were read
from the timestamped `[audio]` lines, not from the counter — but any earlier reading of `rssi=` is a
decode-error count, and any reading of a field called `decerr=` is a memory of one, because no build
ever emitted that name. M3 adds `decode=` and fixes `rssi=`.

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

**MD-5 — pick the slots and build the payload.** ✅ *Done, August 2026.* The picks live in
`RadioStationsList.md` (button, frequency, country, name, site, stream, logo) with the artwork in
`Assets/`, already cropped to 92×92. `build-data.mjs` turns that table into
`Esp32InternetRadio/data/`, expanding a frequency range like `100-101` into one CSV row per dial
position. It **refuses to write** on anything the firmware would swallow silently — a duplicate
`(button, frequency)`, a URL over `kMaxUrlLength`, an HLS URL, a logo that is missing or not 92×92,
or a comma in any field. `--force` downgrades those to warnings and leaves the offending slots empty.

That last check earned its place immediately: Pinguin Vintage’s URL carried `f=mp3,any&br=192000,any`,
and the CSV parser M3 is about to write splits on commas with no quoting. Stripping the parameters
(§5.4) fixed it and eight others. Two more traps the build caught that a human reading the table would
not: Europa Plus was an HLS `.m3u8` manifest, and two logos were still at their source resolution —
there is no runtime scaler, so both would have failed at the slot rather than at build time.

**Done when:** ✅ there is a ranked, probe-verified candidate table covering all four countries, and a
`stations.csv` + logo set M3 can load unmodified.

**What it produced**, measured August 2026 — this is the **D6** partition input M3 was waiting for:

| | |
|---|---|
| `stations.csv` | **76/76 slots**, 60 distinct stations, 6839 bytes |
| `logos/` | 60 PNGs, all 92×92, **588.2 KB** (largest 23.1 KB) |
| **LittleFS total** | **594.9 KB** against the provisional 1.87 MB partition |
| Stream URLs | median 47 → **49 chars**, max 128, cap 192 |
| Codecs | 46 MP3 / 14 AAC |
| Re-probed at build | **60 of 60 alive** |

594.9 KB lands at the bottom of the 0.5–1 MB the plan guessed, so the provisional split holds and
**D6** can be closed at M6 without moving it.

**Link rot is real and it is fast.** KWUL probed alive on 22 August and 404s a week later on every
variant. Budget for a re-probe before the cabinet goes back together, not just before M3.

**And `icy-name` disagreeing with the label is not proof the URL is wrong.** The KNAC stream reports
itself as *Highway Rock 96.9 / 94.9*, which looks exactly like the J-Pop trap in `us-shortlist.md` —
but knac.fm’s own player embeds that very URL. A mislabelled encoder and a wrong URL read identically
from the headers; the tiebreak is whether the station’s own site points at it.

---

## M3 — Station catalogue

*Still no UART and no screen — stations are selected by typing into the serial monitor.*

- LittleFS mount; `data/stations.csv` + `data/logos/` (**D4**).
- CSV parser → in-memory index of 4 banks × 19 slots (**§5**). Hand-rolled; no library needed.
- Serial debug command (e.g. `M 92`) to switch slots, so the whole selection path is exercised without
  the Pico.
- Empty slot = stop audio, no error (**§6**).
- Load the MD output as-is: `data/` is already complete at 76/76 slots and 594.9 KB, generated by
  `Tools/StationMining/build-data.mjs`. Do not hand-edit `data/` — edit `RadioStationsList.md` and
  re-run the build, which re-checks the slot, URL-length, HLS, comma and 92×92 rules.
- The logo set is prepared and measured (**D6** input, above); nothing to crop at this milestone.
- **Re-probe before trusting the catalogue.** Link rot cost one station in a single week during MD.
- Verify an AAC station plays as well as an MP3 one (**D15**) — ROCK ANTENNE's `aacp` endpoint is the
  obvious test case, and its `mp3` sibling is the §5.2 comparison.

### Built, August 2026

`StationCatalogue` mounts LittleFS, reads the CSV into **one ~7 KB allocation** and points 76 slots
into it. One block rather than 228 small strings is the §7.3 choice: fragmentation is the risk, and
228 allocations interleaved with the first TLS session is how the largest free block gets carved up.

The firmware re-checks every rule `build-data.mjs` enforces, because the CSV is uploaded separately and
can be edited without a recompile. A bad row is dropped with a reason on the log instead of
half-working: wrong field count, button outside L/M/K/U, frequency outside 87–105, empty field, URL
over `kMaxUrlLength`, duplicate slot. It also tolerates CRLF, which `.gitattributes` should prevent but
an image built from another checkout would not.

The console now reads lines instead of single keys, so `M 92` works; M2's keys all survive. A missing
filesystem falls back to the compiled-in bench stations rather than looking like 76 empty slots — which
is also why `TestStations.h` was not deleted, the other reason being that the switch storm compares a
station against *itself* and would get one sample each across a 60-station catalogue.

```
firmware   RAM 9.6 % (51260 B, +1000 over M2)   Flash 59.4 % (1246009 B, +44.5 KB for LittleFS)
littlefs   594.9 KB of content in the 1.87 MB partition — D6 confirmed, ~32 % used
csv        76/76 rows accepted against the firmware's own rules, 0 rejected
```

### Measured on the board, August 2026 — it runs

First execution of the parser, and it was clean: `76/76 slots filled, 0 rows rejected`. `L 87` played
on boot, ICY titles arrived, and the two fields the M2 firmware got wrong now report properly —
`decode=0` exists at all, and `rssi=-56` is a signal strength rather than the decode count.

| | M2 baseline | M3 on the board | Δ |
|---|---|---|---|
| Free heap while streaming | ~85,000 | ~75,300 | −9,700 |
| **Largest free block** | **26,600–28,700** | **14,836–24,564** | **−4,100 to −11,800** |
| Free heap after MP3 decoder init | 88,400 | 77,296 | −11,100 |
| Stream buffer | 27,951 | 27,952 | unchanged |
| Audio task stack headroom | 4,716 | 3,836 | −880 |

The −9,700 of total heap is the ~6.7 KB catalogue blob plus LittleFS mount buffers, which is the price
of the milestone and was expected.

**Nothing accumulates across station changes.** After eleven of them the heap read 81,560 — *higher*
than the 75,216 measured 90 seconds after boot — with `fails=0` and the largest free block back up to
24,564. Eleven TLS teardowns therefore left no lasting fragmentation, which is the §7.3 question and a
good answer to it. `drops=2` over the same span means D17's reconnect fired twice and recovered without
intervention.

What does vary, a lot, is the largest free block: **14,836 to 24,564** depending on codec, bitrate and
how recently a stream was torn down. That spread is what constrains M4 below, and it is worth
remembering that the low end came from the quietest possible moment — one stream, just after boot.

**LittleFS reports 765,952 of 1,966,080 bytes used — 39 %, not the 30 % the file sizes predict.** 61
files rounded up to 4 KB blocks is 741,376 bytes against 608,992 of actual content, and metadata
accounts for the remaining 24 KB. **748 KB is the real D6 number**, not 594.7 KB; the partition still
holds comfortably, but the overhead is 25 % and scales with file *count*, so it is worth re-checking if
the logo set ever grows.

### This sets a hard constraint on M4

A 92×92 RGB565 framebuffer is **16,928 bytes**. The largest free block ranges **14,836–24,564** across
boot and eleven station changes. **16,928 sits inside that range** — so such a buffer would succeed some
of the time and fail the rest, which is the worst way for it to fail. The first three samples, all taken
within 90 s of boot, read 14,836–16,372 and made this look like a fixed ceiling; it is not, and the
variance is the finding.

So M4 must decode PNG **straight to the TFT via PNGdec's line callback**, never into an image buffer.
That was always the sensible design; it is now the only one that works. Check the largest free block
again once the display is in, because M4 adds TFT_eSPI's own buffers on top of this.

> **What M4 found, recorded here because this paragraph sends you the wrong way.** The conclusion was
> right about the *frame* and wrong about the *decoder*: PNGdec's own object is 45,604 contiguous
> bytes, nearly three times the frame this section rules out, and the board has a block that size only
> at boot. There is no on-device PNG decoding at all now — see **M4** and the revised **D5**. And
> TFT_eSPI, the suspect named in the last sentence, turned out to cost 672 bytes.

### Two observations from the boot log, neither a fault

- **Most connects cost two TLS handshakes.** `stream.rockantenne.de` 302s to a numbered edge host —
  `s8-` once, `s2-` a few minutes later — so the catalogue URL pays roughly 1.4 s twice, plus two rounds
  of buffer alloc/free, per station change. A direct-connecting station like Pinguin Aardschok pays one.

  **Storing the resolved URL to skip the second handshake is now ruled out, not merely deferred.** `L 92`
  resolves to a **209-character** URL — over `kMaxUrlLength` by 17 — carrying `skey:1788017261`, which is
  a session key with a lifetime. It works today only because ESP32-audioI2S follows the redirect through
  its own buffer rather than through `playUrl()`. Pinning it would exceed the cap *and* expire, which is
  exactly the trap §5.4 describes. The redirect stays.
- `[E][WiFiClient.cpp:320] setSocketOption(): fail on 0, errno: 9, "Bad file number"` appears before
  every TLS connect. It is the Arduino core setting a socket option before the socket exists — noise
  from a pinned dependency (**D2**), harmless, and not worth chasing.

**D15 is verified.** `L 92` initialised the AAC decoder (ADTS, MPEG-4 LC, 22.05 kHz) and `L 96` the MP3
one, both playing from the catalogue, both with `fails=0`. The heap cost went the opposite way to the
prediction — see §5.2, whose stated rationale this measurement retired.

**One check still open, and it needs a data round trip.** The empty slot **cannot be tested from the
shipped catalogue** — every one of the 76 is filled. Delete a row from `RadioStationsList.md`, run
`node Tools/StationMining/build-data.mjs`, then `pio run -t uploadfs` (no firmware reflash), and confirm
the slot stops audio and prints `is empty` with no error. **Reasonable to carry into M4**, which has to
render the empty-slot state anyway and so exercises the same path.

**Carried into M4 rather than held here:** the M2 decode-error period on WDR 4, now that `decode=` is a
real field. Nothing about it blocks the display work, and M4's soak produces the same data anyway.

**Done when:** typing a bank+frequency plays the right stream, unknown slots go quiet, both codecs work,
and you know how many KB the full logo set occupies. **Three of the four are met**; only the empty slot
is unproven, and it is untestable without editing the data set.

### Station names are ASCII, and the build enforces it

Two names carried Cyrillic — *Ultra 100.5 (Радио Ультра)* and *DFM Радио 101.2 FM (DFM Radio)*. The
catalogue parser is byte-transparent and did not care, but §6's font is `Font5x7.cs` ported from v1 and
has no glyphs above U+007F, so both would have drawn as rubbish at M4. Renamed to **Radio Ultra 100.5**
and **DFM Radio 101.2 FM**; each already carried its Latin form in parentheses, so nothing was lost but
the duplication. The font keeps the v1 metrics exactly, which is the whole point of porting it rather
than substituting one.

`build-data.mjs` now **fails** on non-ASCII in a name and names the offending characters, because a
line containing them looks perfectly ordinary in every editor. The catalogue is pure ASCII end to end.

**Names are shortened where the screen made them ambiguous.** The name is drawn at `x=21` in a
6 px-advance font on a 160 px screen, so 23 characters fit and the rest is clipped — which §6 defines as
the behaviour, not a fault. Clipping only matters when it removes the part that tells two stations
apart, and it did: the ten Antenne Bayern channels are identical for their first 15 characters, so
`ANTENNE BAYERN Greatest| Hits` and `ANTENNE BAYERN Workout |Mix` were about to render as the same
line. They are now `AB Greatest Hits`, `AB Workout Mix` and so on, matching the `AB_*.png` the logo set
already used.

Ten names still clip, all from different networks, and **no two clipped forms collide** — all 60 names
stay distinguishable on screen. The build lists the survivors after every run so the check is cheap to
repeat when stations change.

> **One, after M4.** Seeing the real screen prompted the same treatment for ROCK ANTENNE that ANTENNE
> BAYERN got here, and then a pass over the rest — see M4.

---

## M4 — Display ✅

**Status: done, August 2026.** The screen is a pixel-for-pixel port of the Pi layout, confirmed against
photographs of the real panel, and a station change is inaudible.

*Add the ST7735 to the breadboard; still driven by serial commands.*

- TFT_eSPI configured from `platformio.ini` build flags (**D9**).
- Port the 5×7 font from `RadioApp.Hardware/Helpers/Font5x7.cs` so text metrics match the Pi version.
- Reproduce the layout exactly: frequency `(100,2)`, logo 92×92 at `y=13` centred, name `(21,107)`, song
  `(3,117)`; independent blank-and-redraw of the song line (**§6**, **D10**).
- ~~PNG logo decode with PNGdec (**D5**), on core 0 only~~ — **superseded.** Logo drawing is still
  core 0 only (**D14**), but there is no decoder: PNGdec needs 45,604 contiguous bytes the board does
  not have, so the logos are pre-rendered to raw RGB565 by the build. **D5** was revised here.
- ~~**Decode through PNGdec's line callback, never into a full-image buffer.**~~ The reasoning held —
  M3 measured the largest free block at 14,836–24,564 and a 92×92 RGB565 frame is 16,928, inside that
  range — but it aimed at the wrong object. The frame was never the problem; the *decoder* was.
  Re-checking `largest=` was the right instruction and TFT_eSPI was the wrong suspect: it costs 672
  bytes.
- Empty-slot and paused states both render as frequency-only on black.
- Ten station names are longer than the 23 characters that fit at `x=21`. None collide once clipped, but
  this is the milestone where a real screen can say whether the clipping looks acceptable. **It did, and
  the answer was to shorten them** — see *Names, once there was a screen to read them on*; one clips now.

**Done when:** switching slots over serial redraws logo, name and frequency correctly, live ICY titles
appear on the bottom line and update as tracks change, and — the real test — **a station change produces
no audible glitch** while the new logo is being decoded and drawn. ✅ **All three met.**

### Built and running, August 2026

On the board: boot, panel init, catalogue, WiFi, TLS, audio, station changes over serial, logos, and
live ICY titles. It took three flashes to get there and the two failures are the interesting part —
both are in *The 45 KB that would not fit* below.

`Display` owns the panel and runs entirely on core 0 (**D14**). The layout constants are lifted from
`DisplayManager.cs` including its off-by-one — the Pi centres the logo against `DisplayWidth = 0x9F`,
the *last column* rather than the width, so the logo sits at `x=33` and not 34. Reproducing that is the
job; "fixing" it would move the logo relative to four YouTube videos.

The font is generated, not retyped: `Tools/gen-font5x7.py` parses the C# dictionary and emits
`src/Font5x7.cpp`. 95 printable ASCII glyphs are kept; the 73 umlaut and Cyrillic entries are dropped,
because §6 makes ASCII a rule about the *data* and `build-data.mjs` already enforces it on names. An
ICY title is not under that rule, so anything outside 0x20–0x7E draws as `?` — one per UTF-8 byte.
The bit order is the thing to get wrong here: bit 0 is the *leftmost* pixel, and reading the table
MSB-first mirrors every glyph, which only the asymmetric characters would reveal.

TFT_eSPI is configured entirely from `platformio.ini` (**D9**), so the pin numbers exist twice — as
preprocessor macros there and as the `constexpr`s in `include/Pins.h` everything else uses. `static_assert`
ties the two together, so a divergence fails the build instead of leaving the screen dark. `ST7735_BLACKTAB`
is not a guess: it means no column/row offset and RGB order, which makes TFT_eSPI's rotation-1 MADCTL
work out to **0xA0** with address windows of 0..159 / 0..127 — byte for byte what `DisplayManager` wrote
to this same panel.

The ICY title is the one piece of state that crosses cores the other way. ESP32-audioI2S raises
`audio_showstreamtitle()` on the audio task, and drawing there would put a TFT write inside the decode
loop, so the string is copied into a mutex-guarded buffer and collected by `loop()` on core 0. A station
change publishes an *empty* title rather than merely dropping the flag, because the screen has to be told
to blank the line — and it happens before the blocking connect, so the old track is not still up during
the handshake.

The logo path is a file read and four SPI pushes, with a 4,232-byte static band buffer and no image
decoder at all — which is not what this milestone set out to build. See below.

### The 45 KB that would not fit — and took D5 with it

M3 closed by saying to re-check the largest free block once the display was in, because M4 adds
"TFT_eSPI's own buffers" on top. It does not: **TFT_eSPI keeps no framebuffer and costs 672 bytes.**
The cost is PNGdec, and it is 68× larger.

`PNGIMAGE` embeds zlib's 32 KB sliding window plus its inflate state, a 1 KB palette and a 2 KB file
buffer, so `sizeof(PNG)` is **45,604 bytes** however the object is created — three times the next
largest symbol in the firmware. It was tried in all three places it could go, and the board rejected
each in a different way.

**1. In `.bss` — boot loop.** The reasoning was that a static object is carved out at link time and
cannot fail, whereas a 45 KB `malloc` against M3's largest free block of 14,836–24,564 obviously can.
Both halves are true; the conclusion was wrong. The board panicked before `setup()` ran and rebooted
every 0.6 s:

```
assert failed: esp_startup_start_app_common port_common.c:81 (res == pdTRUE)
```

— `xTaskCreatePinnedToCore` failing to allocate the ESP-IDF main task's stack. **Static DRAM on this
chip is 124,580 bytes, not the 532,480 `pio` prints:** `.data` + `.bss` live in `dram0_0_seg`
(`org 0x3ffbdb5c, len 0x1e6a4` in the map file) and the rest of what `pio` counts is address space the
linker cannot use. A build reporting a comfortable 18.3 % was 27 KB from the edge.

| | `_bss_end` | free in `dram0_0_seg` |
|---|---|---|
| M3 | `0x3ffca398` | 73,320 |
| M4, PNG in `.bss` | `0x3ffd5860` | **27,040 — will not boot** |
| M4, PNG off `.bss` | `0x3ffca640` | 72,640 |

**2. On the heap, held for the session — no TLS.** Allocated once in `begin()` before WiFi, it booted
cleanly, the screen came up and a logo decoded in 94 ms. Then every connect failed:

```
[E][ssl_client.cpp:37] _handle_error(): (-16) BIGNUM - Memory allocation failed
[stat] ... heap=101304 min=52216 largest=36852 ... connects=5 fails=5
```

**3. Per decode — available at boot and almost nowhere else.** The measurement that settled it:

| moment | free heap | largest free block | 45 KB available? |
|---|---|---|---|
| boot, before WiFi | 249,244 | **110,580** | yes |
| streaming plain HTTP (Eldoradio) | 116,324 | 57,332 | yes |
| streaming HTTPS (Pinguin) | 74,432 | **19,444** | no |
| after `s` (stop), buffer still held | 124,264 | **38,900** | no |

The last row is the one that closes it. Sequencing the decode into the gap between teardown and
connect would not have rescued it either — `stopSong()` does not release the 28 KB input ring buffer,
so even a fully stopped radio is 7 KB short. And the catalogue is 87 % HTTPS (§5.1), so in normal use
the logo would have been skipped on nearly every change. The board says so itself:
`[tft] no room for the png decoder (45604 bytes, largest free block 16372) - logo skipped`.

**So D5 changed.** `build-data.mjs` converts each 92×92 PNG to a headerless `.565` file — raw
little-endian RGB565, 16,928 bytes — and the firmware reads it in four 23-row bands into a 4,232-byte
static buffer. No allocation, no library, nothing that can fail for want of memory. The conversion is a
~90-line PNG reader built on `node:zlib`, which keeps the script's no-dependency property; it handles
exactly the 8-bit RGBA non-interlaced files in `Assets/` and throws on anything else.

```
firmware   RAM 10.5 % (55952 B, +4692 over M3)   Flash 60.4 % (1267461 B, −26 KB with PNGdec gone)
littlefs   998 KB of content, ~1208 KB in 4 KB blocks, of 1920 KB — 63 %, up from 39 %
```

Two lessons worth more than the milestone. **`pio`'s RAM percentage divides by the wrong number** —
check `_bss_end` in `.pio/build/*/firmware.map` instead. And **free heap is not the constraint, the
largest free block is**: every failure above happened with 74–124 KB free. §7.3 already said that
about fragmentation across station changes; this is the same lesson arriving as one allocation too big
to place.

### Measured on the board, with the screen running

Four dial slots, two codecs, TLS and plain HTTP, driven over the serial console:

| | M3 | M4 with the screen |
|---|---|---|
| Free heap while streaming HTTPS | ~75,300 | **69,908–74,240** |
| Largest free block, same | 14,836–24,564 | **14,324–15,860** |
| Stream buffer | 27,952 | 27,952 unchanged |
| Audio task stack headroom | 3,836 | 4,508 |
| Connect failures | 0 | **0** over 3 changes |

So the screen costs **~1–5 KB of heap**, not the 45 KB the first design would have. The largest free
block sits at the bottom of M3's range rather than below it.

**Logo draw is 67–174 ms, and that is slower than PNGdec's 93–108** — worth stating plainly rather
than filing under "faster because simpler". The spread is the interesting part, because it is not
about the image:

| what core 1 was doing | logo draw |
|---|---|
| blocked on a TLS handshake (network, not flash) | **67 ms** |
| decoding a steady MP3 | **133 ms** |
| station change — handshake *and* decode | **174 ms** |

Same 16,928 bytes every time. Both cores fetch code through one flash cache and the MP3 decoder is not
a small working set, so a LittleFS read competes with it for the cache rather than for CPU — the
pixels themselves are ~5 ms at 27 MHz. Reading in four bands instead of 92 rows bought about 15 %,
which says the SPI address windows were never the cost.

**The 60-change switch storm still passes with the screen in it.**

```
[storm] changes completed: 60 of 60 (0 connect failures)
[storm] stream drops during storm: 0
[storm] mean drift across 4 stations, over 60 changes: -292 bytes
```

M2 measured −55 to +512 bytes per station without a display; −292 over an evening's worth of
switching is the same answer, and well under the 1024-byte quantisation §7.3 warns the largest free
block is measured in. **No fragmentation, and the screen did not introduce any** — which is what
D17 asks of a session.

One caveat on that run: the storm drives the four *bench* stations, which have no dial position and
no logo, so each change repaints the name line but never touches the logo path — `logo=` stayed at
the boot value of 79 ms for all 60. It measures the redraw and the TLS churn together, not the file
read. Exercising 60 real slots would need a driver that walks the catalogue, and §7.3's reason for
the bench set — comparing a station against *itself* — argues against changing it.

### Carried into M5 and M7

Nothing here blocks the milestone; the first two items are struck through because the bench answered
them.

1. ~~**No audible glitch at a station change.**~~ **Confirmed by ear.** 174 ms of core-0 flash traffic
   against a ~1.7 s stream buffer is inaudible, which is **D14** doing exactly what it was written to
   do: the decode-and-draw work never touches core 1.
2. ~~**Does the layout match the Pi's?**~~ **Confirmed from photographs of the panel.** Logo centred at
   `x=33`, frequency top right, name at `y=107`, title at `y=117`. The black-tab assumption was right —
   no offset, no red/blue swap — so `ST7735_BLACKTAB` stands.
3. ~~**Whether clipping at 23 characters looks acceptable.**~~ **Answered on the bench:** nine of the ten
   were shortened instead. One is left, over by a single character.
4. **A 60-change storm against real slots**, rather than the bench set — the run above repaints the
   name line 60 times but never reads a logo, so the file-read path is unmeasured across a session.
5. **The empty slot**, carried from M3 and still needing a data round trip: delete a row from
   `RadioStationsList.md`, `node Tools/StationMining/build-data.mjs`, `pio run -t uploadfs`. It should
   stop the audio and render frequency-only (§6) — M4 renders that state, so the check is cheap now.
6. **The WDR 4 decode-error period** carried from M2, now that `decode=` is a real field.
7. **Non-ASCII in ICY titles.** `Megaherz - Himmelsstürmer` arrived from ROCK ANTENNE within seconds of
   the first boot. §6 makes ASCII a rule about the *station data*, which the build enforces, but a stream
   title is not under that rule — the font answers `?` per byte, so that one drew as `Himmelsst??rmer`.
   Legible, not pretty; worth deciding whether it deserves a UTF-8 → Latin-1 fold before M7.

### Names, once there was a screen to read them on

M3 shortened the ten ANTENNE BAYERN channels because the build script said they would clip, and left
the other ten clippers alone on the grounds that no two collided. Photographs of the real screen
changed that judgement: a name clipped mid-word looks like a bug even when it is unambiguous, and
`ROCK ANTENNE Symphonic |Rock` tells you the network and not the station.

**ROCK ANTENNE → RA**, the same treatment ANTENNE BAYERN got and for the same reason — a long shared
prefix eating the distinguishing word. All seven channels, matching the `RA_*.png` the logo set already
used: **RA Deutschrock**, **RA Heavy Metal**, **RA Symphonic Rock**, **RA Coversongs**, **RA Hair
Metal**, **RA Hard Rock**, **RA Modern Metal**. Four of them were clipping; all seven now fit with room
to spare.

Then the rest, each trimmed of whatever it could spare rather than by a rule:

| | was | now |
|---|---|---|
| L 92 | Best of Rock.FM - Hard rock | Best of Rock.FM - Hard |
| L 104-105 | Radio Bob Ozzy Ousbourne | Radio Bob Ozzy |
| K 93 | 181.FM The Beatles Channel | The Beatles Channel |
| K 94 | 181.FM Power 181 (Top 40) | 181.FM Power (Top 40) |
| K 97 | 104.6 RTL Das Beste der 80er | RTL Das Beste der 80er |
| K 104-105 | Radio F 94.5 - Made in Germany | F 94.5 - Made in Germany |

**The clipping list goes from ten names to one.** The survivor is `K 104-105`, at 24 characters — over
by exactly one, so it draws as `F 94.5 - Made in German|y`. Dropping the dash would clear it.

All of this is data: `node Tools/StationMining/build-data.mjs` then `pio run -t uploadfs`, no firmware
reflash. The build reports the clipping list after every run, so the check stays cheap when stations
change.

`ROCK ANTENNE 80er` in `TestStations.cpp` keeps its full name deliberately. It is a compiled-in bench
station rather than a dial slot, it never reaches the screen, and M2 recorded its per-station heap
drift under that name — renaming it would break the link to a measurement that cannot be re-run.

### Deliberately not built

- **No status or error indicator.** A connect in progress, a retry and a dead slot all look like a
  station with nothing to say. §9.2 keeps the placement open on a layout that is already full, and
  guessing at it now would be the one part of the screen with no Pi original to port.
- **No splash or boot screen.** `Display::begin()` paints black and stops. §9.2 lists boot behaviour as
  undecided, and a WiFi failure still only reaches the serial log — which M7 was already going to fix.
- **No UTF-8 handling for ICY titles**, per the item above.

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
