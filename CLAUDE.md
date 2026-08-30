# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Read this first: the repo has two eras

A vintage SABA Mainau transistor radio converted into an internet radio. The project was rebuilt around
a different brain partway through, and **both generations live in this repo at once**:

| Era | Trees | Status |
|---|---|---|
| **Pi 4 era** (v1) | `RadioApp/`, `RadioFrontend/` | **Frozen. Do not modify.** |
| **Shared hardware** | `RadioIO/` (Pico I/O firmware) | **Frozen. Do not modify.** Still running in the radio. |
| **ESP32 era** (v2, active) | `Esp32InternetRadio/` | Where all new work goes. |

`RadioApp/` and `RadioFrontend/` are the .NET 10 service and Angular admin UI from the Raspberry Pi 4
build. Four YouTube videos link to this repo expecting that code to exist and match what viewers saw, so
it is kept working and unchanged — not deleted, not refactored, not "tidied up". `RadioIO/` is frozen for
a second reason: it is the Pico firmware still physically in the radio, and the ESP32 has to speak its
protocol as-is.

Treat any request that would edit those three trees as a probable misunderstanding and ask first.

**Current design and decisions: [`Architecture.md`](Architecture.md). Roadmap: [`DeliveryPlan.md`](DeliveryPlan.md).**
Those two are the source of truth for the ESP32 work; this file only orients you.

---

## Part 1 — ESP32 internet radio (active)

An ESP32 (AZ-Delivery DevKit C V4, ESP32-WROOM-32, 4 MB flash, **no PSRAM**) replaces the Pi 4. It reads
the physical controls over UART from the unchanged Pico, plays mostly-HTTPS MP3 streams through an
I2S audio board into the SABA's amplifier, and drives the same ST7735 TFT. Audio is ESP32-audioI2S. No
Spotify, no web UI, no hosted service.

All four toggle buttons (`L`/`M`/`K`/`U`) now select internet radio banks: 4 banks × 19 dial positions
(87–105) = 76 channel slots, each with a stream URL, station name and 92×92 logo.

Station content does not start from scratch: the v1 scraper database
(`RadioApp/RadioApp/Data/RadioSettings.db`) holds 874 vetted candidate URLs and 14 slots already mapped
to button+frequency with embedded logos. **Read it, never write it** — it belongs to the frozen tree.
`Architecture.md` §5.1 has the breakdown. Three things to know before querying it: (1) two tables carry
a country and they are not the same — `Countries` is the scraper's 139-country **worklist** (Russia and
the US are in it), while `RadioStationInfos` is the **result** and holds only UK, Germany and
Luxembourg, so a US or Russian station query returns 0 rows; (2) only two rows are tagged `Metal`;
(3) `Rating = 0` on 73 % of rows means *unrated*, not *bad* — rank by the like ratio per §5.3.

### Commands (from `Esp32InternetRadio/`)

```bash
pio run                  # build
pio run -t upload        # flash firmware over USB (COM3, see platformio.ini)
pio run -t uploadfs      # flash the LittleFS data partition (stations + logos) — separate step
pio device monitor       # serial monitor, 115200
pio run -e tone-test -t upload   # DAC bring-up: 440 Hz sine, no WiFi, no audio library
```

Station data changes need only `uploadfs`; firmware changes need `upload`. They are independent.

`data/` is generated — run `node Tools/StationMining/build-data.mjs` after editing
`RadioStationsList.md` or dropping a logo into `Tools/StationMining/Assets/`, then `uploadfs`. The
script converts each 92×92 PNG to a headerless `.565` file (raw little-endian RGB565); the board has
no image decoder (**D5**, revised at M4).

### Key constraints — these are the things that bite

- **No PSRAM, and no new hardware.** Making HTTPS streaming stable on this exact board is an explicit
  project constraint, not a default. TLS costs ~40 KB, the MP3 decoder ~30 KB, and whatever is left is
  the stream buffer that absorbs network jitter — so every KB saved elsewhere buys stability. When
  something is marginal, work down the ladder in `Architecture.md` §9.1; a WROVER is its last rung and
  has been ruled out for now.
- **Free heap is not the constraint — the largest free block is, and there is no 45 KB one.** With an
  HTTPS stream live it is 14–19 KB, and even after a stop it is only ~39 KB, because `stopSong()` does
  not release the input ring buffer. This is what killed **D5**'s on-device PNG decoder at M4 (whose
  `PNG` object is 45,604 contiguous bytes) and it will kill anything else that size. Logos are now
  pre-rendered to raw RGB565 by `build-data.mjs`; there is no image decoder on the board.
- **Static DRAM is 124,580 bytes, not the 532,480 `pio` reports.** `.data` + `.bss` live in
  `dram0_0_seg`; the rest of what `pio` counts is address space the linker cannot use, so its RAM
  percentage is measured against the wrong denominator and reads reassuring right up to a boot loop —
  `assert failed: esp_startup_start_app_common port_common.c:81`, before `setup()` runs. `_bss_end`
  against the end of `dram0_0_seg` in `.pio/build/*/firmware.map` is the number to check, not the
  build summary.
- **The output stage is a PCM5102A** (`Architecture.md` D16, closed at M1). The first module was dead —
  valid I2S in, ~5 mV out — and a second one of the same type worked on the same wiring and binary, so
  keep a spare. Before suspecting the firmware for any silence, run `pio run -e tone-test`: a 440 Hz
  sine with no WiFi and no audio library, which isolates the board in one command.
- **Never block the audio task.** PNG decode and TLS handshakes both stall long enough to underrun the
  stream, and both happen at station change. Audio runs pinned to core 1, everything else on core 0,
  joined by a command queue (`Architecture.md` §7.1). A "network glitch" at station change is usually
  this instead.
- **Heap fragmentation is the real risk**, not throughput — and the thing to measure is the *largest
  free block* **per station change**, not average free heap and not drift over days. The radio is used
  in 2–3 hour sessions with ~30–60 station changes and is switched off afterwards (`Architecture.md`
  §7.3, **D17**), so switching is the dominant operation and the mains switch clears slow decay. Stream
  drops must self-heal; WiFi-loss recovery is explicitly best-effort.
- **The Pico sends JSON with no trailing newline.** `readStringUntil('\n')` will hang forever. Frame by
  counting braces. The Pico cannot be changed to fix this.
- **The dial streams `NewFrequency` while turning — and also while standing still.** A knob parked on a
  capacitance threshold reports 95, 94, 95, 94 indefinitely: 28 messages in 75 idle seconds, measured at
  M5. `RadioController` waits for the reading to hold still before acting, with **two** settle times —
  1 s for a reading that stands alone, 2 s once a second reading has arrived within 2 s of the last.
  Both numbers are measured, not chosen: gaps *within one continuous turn* land on 0.61, 1.22 and 1.84 s
  (one, two or three Pico passes) while a stopped dial goes quiet for ≥3 s, so a flat 1 s splits a sweep
  into five station changes and a flat 2 s makes a single click feel sluggish. Do not replace this with
  the Pi's `PlayerProcessorDebounceFrequencyService`, which is a 500 ms *leading-edge* throttle and
  would act on all 28 idle messages.
- **Some thresholds chatter on a schedule no settle time can catch** — 6–8 s between excursions, which
  produced nine unprompted station changes in 73 s on a paused radio. `RadioController` detects the
  **X, Y, X** shape across two adjacent positions and then treats the pair as one place. Timing cannot
  distinguish chatter from tuning here; only the sequence shape can.
- **Sixteen of the 76 slots are the second half of a spanning entry** (`100-101`, `102-103` in
  `RadioStationsList.md` become two identical CSV rows), so one dial click in five lands on the station
  already playing. `RadioController` compares URLs and leaves the stream alone, redrawing only the
  frequency. Anything that changes station must go through it, or that check is bypassed.
- **`buttonIndex` mapping**: `-1` none, `0` Phono (unused), `1` L, `2` M, `3` K, `4` U.
- **Repartitioning wipes LittleFS.** The partition table is deliberately still undecided; settle it
  before the radio is reassembled (M6).
- **WiFi credentials** belong in a gitignored `include/Secrets.h`. The scaffold currently has real
  credentials inline in `src/main.cpp` — that is a known M0 task, not a pattern to copy.

### Screen layout is a port, not a redesign

The TFT layout is reproduced pixel-for-pixel from the Pi implementation so the radio looks unchanged:
frequency at `(100,2)`, 92×92 logo at `y=13` horizontally centred, station name at `(21,107)`, ICY song
title at `(3,117)`. The 5×7 font comes from `RadioApp.Hardware/Helpers/Font5x7.cs` — port it rather than
substituting a library font, or the metrics drift. Exact spec in `Architecture.md` §6. The country flag
from v1 is dropped.

When implementing display or protocol behaviour, **read the v1 C# implementation for reference** —
`RadioApp.Hardware/DisplayManager.cs` and `RadioApp/PlayerProcessors/InternetRadioPlayerProcessor.cs`
solved these problems already. Read them; do not edit them.

---

## Part 2 — Pi 4 era, frozen (`RadioApp/`, `RadioFrontend/`)

Kept building and passing tests. CI (`.github/workflows/`) still runs both on every push/PR to `main`.
Enough detail to work out what v2 needs to reproduce:

```bash
# RadioApp/          .NET 10 solution
dotnet build && dotnet test
dotnet test --filter "FullyQualifiedName~RadioControllerServiceTests"
dotnet run --project RadioApp/RadioApp.csproj      # http://localhost:5262

# RadioFrontend/     Angular 19 + NgRx Signals, Jest
npm start            # :4200, talks to the backend on :5262
npm test
npm run build        # writes into ../RadioApp/RadioApp/wwwroot
```

**Input → playback pipeline.** Pico UART → `UartIoListener` (pigpio pin callback, `RadioApp.Hardware`)
→ `CommandsParserHelper` → MediatR `ProcessCommandNotification` → `CommandsProcessor` → `RadioStatus`
(singleton holding all device state, signals a `TaskCompletionSource` on change) → `PlayerProcessorService`
(`BackgroundService` awaiting that TCS) → the current `IPlayerProcessor` (Idle / Spotify / InternetRadio).
Processors publish further MediatR messages: display notifications handled by `DisplayManager`, Spotify
Web API by `SpotifyApi`, persistence by `DataAccessService`, plus direct `IRadioVlcPlayer` calls.

Other things worth knowing if you ever have to run it:

- MediatR handlers are registered from `AppDomain.CurrentDomain.GetAssemblies()`, so a handler only
  works if its assembly is actually loaded.
- `Program.cs` switches on `Environment.OSVersion.Platform`: pigpio P/Invoke on Unix, mocks
  (`RadioApp.Hardware/Mock/`) elsewhere — so it runs on Windows with no hardware. Tests mock the same
  interfaces (xUnit + Moq, `HardwareFixture`).
- Angular builds into `RadioApp/RadioApp/wwwroot`; client routes are hardcoded twice — in
  `app.routes.ts` and in `Program.cs`'s `MapFallback` list.
- SQLite at `./Data/RadioSettings.db`, `EnsureCreated()` on startup despite migrations existing.
- `SpotifyApiEndpoints` and `ScreenApiEndpoints` exist but their `Map…` calls are commented out.
- Serilog writes to `radioService.log`, exposed at `GET /logs`.

`Docs/` documents the v1 hardware (wiring, Pico control, SABA circuit, Pi deployment). It stays accurate
for v1; ESP32 wiring gets its own document once as-built (M6).

---

## Part 3 — `RadioIO/` (Pico firmware, frozen)

C++ / Pico SDK 2.1.1, built through the VS Code Raspberry Pi Pico extension (`ninja -C build`; flash with
`picotool load` or OpenOCD — see `.vscode/tasks.json`).

Polls the button ladder, play/pause button and tuning capacitor, then pulls its interrupt pin low and
writes a JSON message over UART1 at 115200 for anything that changed. The full wire contract, including
the missing-newline framing problem, is in `Architecture.md` §4. `RadioIO/src/UARTMessenger.cpp` is the
source of truth for the message shapes.

**That poll is ~650 ms, not the `sleep_ms(200)` at the bottom of `RadioIO.cpp`'s loop.** Every pass also
measures the tuning capacitor, and `CapacitanceState::getCapacitance()` ends in a `sleep_ms(400)`
discharge plus the charge time. Measured at M5. It sets two numbers on the ESP32 side: the request-state
pin must be held ≥1 pass (1500 ms is used, for two chances), and the frequency debounce must settle for
longer than one pass or a dial parked on a threshold changes station on its own.
