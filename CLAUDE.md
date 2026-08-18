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
`Architecture.md` §5.1 has the breakdown.

### Commands (from `Esp32InternetRadio/`)

```bash
pio run                  # build
pio run -t upload        # flash firmware over USB (COM3, see platformio.ini)
pio run -t uploadfs      # flash the LittleFS data partition (stations + logos) — separate step
pio device monitor       # serial monitor, 115200
pio run -e tone-test -t upload   # DAC bring-up: 440 Hz sine, no WiFi, no audio library
```

Station data changes need only `uploadfs`; firmware changes need `upload`. They are independent.

### Key constraints — these are the things that bite

- **No PSRAM, and no new hardware.** Making HTTPS streaming stable on this exact board is an explicit
  project constraint, not a default. TLS costs ~40 KB, the MP3 decoder ~30 KB, and whatever is left is
  the stream buffer that absorbs network jitter — so every KB saved elsewhere buys stability. When
  something is marginal, work down the ladder in `Architecture.md` §9.1; a WROVER is its last rung and
  has been ruled out for now.
- **The output stage part is not settled.** M1 got first sound from a MAX98357A after the PCM5102A
  module failed to convert; they are not interchangeable in the cabinet (`Architecture.md` D16). Use
  `pio run -e tone-test` — a 440 Hz sine with no WiFi and no audio library — before suspecting the
  firmware for any silence.
- **Never block the audio task.** PNG decode and TLS handshakes both stall long enough to underrun the
  stream, and both happen at station change. Audio runs pinned to core 1, everything else on core 0,
  joined by a command queue (`Architecture.md` §7.1). A "network glitch" at station change is usually
  this instead.
- **Heap fragmentation is the real long-run risk**, not throughput. Measure the *largest free block*
  over hours of connect/disconnect cycles, not average free heap.
- **The Pico sends JSON with no trailing newline.** `readStringUntil('\n')` will hang forever. Frame by
  counting braces. The Pico cannot be changed to fix this.
- **The dial streams `NewFrequency` continuously while turning.** Debounce (~1 s settle) or a single
  spin of the dial opens 19 stream connections.
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

Polls the button ladder, play/pause button and tuning capacitor every 200 ms; on change pulls its
interrupt pin low and writes a JSON message over UART1 at 115200. The full wire contract, including the
missing-newline framing problem, is in `Architecture.md` §4. `RadioIO/src/UARTMessenger.cpp` is the
source of truth for the message shapes.
