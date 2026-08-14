# Delivery Plan — ESP32 Internet Radio

Incremental path from the current scaffold (a WiFi + HTTPS connectivity test that prints
`Received: 512585 bytes`) to a finished radio. Design decisions referenced as **D1–D12** live in
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

## M1 — First sound

*Breadboard only: ESP32 + PCM5102A. No UART, no TFT, no station data.*

- Wire the DAC per the pin map (**§3.1**), including the `SCK`/`FMT`/`XSMT` straps.
- Replace the HTTP byte-counter spike with ESP32-audioI2S (**D2**): `setPinout()` +
  `connecttohost()`.
- Use **the HTTPS URL you already smoke-tested** — `https://s1-webradio.rockantenne.de/80er-rock/stream/mp3`.
  Testing the easy HTTP case first would only hide the risk this milestone exists to expose (**D3**).
- `setInsecure()`, no CA bundle (**D13**). Fixed software gain (**D12**).
- Set up the core split now, not later: audio task pinned to core 1, `loop()` on core 0 (**D14**). It is
  far cheaper to start this way than to retrofit it in M4 when the display starts stealing time.

**Done when:** recognisable music comes out of the DAC into headphones or a test amp, over HTTPS.
**Record before moving on:** free heap while playing, and the stream buffer size you settled on. These
are the baseline every later measurement is compared against.

---

## M2 — Stream robustness — **go/no-go for the whole hardware choice**

*Same breadboard. This is the riskiest milestone in the project; everything after it assumes it passed.*

The question is not "does HTTPS play" — M1 answered that. It is whether a no-PSRAM WROOM can hold a TLS
stream open for hours without fragmenting its heap to death (**§7.3**).

- **Multi-hour soak with heap logging.** Print free heap and largest free block every 30 s. A slow
  downward drift in the largest block is the failure mode to watch for — not average free heap.
- Force 20+ station changes in a row (each one a TLS teardown and setup) and check the heap returns to
  its baseline. This is where fragmentation shows up fastest.
- Reconnect on stream drop and on WiFi loss, with backoff. Write down the retry policy (**§9.2**).
- ICY metadata: hook the title callback and print `Artist - Title` to serial (M4 consumes it).
- Two or three real candidate stations, not just the M1 one.

**Done when:** a multi-hour soak holds with a stable largest-free-block, repeated station changes do not
leak, and pulling the router mid-stream recovers without a reboot.

**If it fails:** work down the ladder in **§9.1** in order — buffer size, easier streams, verify the core
split, then the more expensive rungs. Do not proceed to M3 on a stream that is already marginal; every
later milestone only adds pressure to the same heap.

---

## MD — Station data mining *(laptop-only, runs in parallel with M1/M2)*

*No ESP32 involved. Off the critical path, but M3 is far cheaper if this is done first.*

The v1 scraper database (`RadioApp/RadioApp/Data/RadioSettings.db`, **read-only** — it is part of the
frozen tree) holds 874 usable candidates and 14 already-assigned slots (**§5.1**). Turn that into a
vetted shortlist with a throwaway script, in whatever language is convenient:

- **Probe each candidate URL**: follow redirects, record the final URL, the `Content-Type` (the only
  reliable codec signal — 45 % of URLs reveal nothing), and the ICY headers `icy-name` and `icy-br`.
  Flag dead links; the data is ~5 months old.
- **Emit a candidate table** — name, country, final URL, real codec, bitrate, alive — to pick from.
  Where a station exposes both `…/stream/mp3` and `…/stream/aacp`, keep the MP3 one (**§5.2**).
- **Migrate the 14 existing slots**: export button, frequency, name, URL and the base64 logo from the
  `RadioStation` table into `stations.csv` + 92×92 PNGs. Free, real starting content.

**Done when:** you have a filtered candidate list with real codecs, and a `stations.csv` + logo set for
the 14 v1 slots that M3 can load unmodified.
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
- Mix `LOUT`/`ROUT` to mono through the resistor pair and inject into the SABA amplifier; set the fixed
  software gain for a clean level (**D12**, `Docs/SabaCircuit.md`).
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
