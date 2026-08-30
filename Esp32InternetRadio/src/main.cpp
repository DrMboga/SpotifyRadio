// M5 — Pico UART.
//
// ESP32 + PCM5102A + ST7735 + the frozen Pico I/O board on a bench. Connects to
// WiFi, parses `data/stations.csv` off LittleFS into 4 banks × 19 dial
// positions, then asks the Pico where the knobs are sitting and tunes there.
// From that point the physical toggle buttons, tuning capacitor and play/pause
// button drive everything (PicoLink → RadioController).
//
// The serial console is still here and still does the same job: it is the only
// way to run M2's measurements, and it is what keeps the board useful when the
// Pico is unplugged. It now feeds RadioController through the same setters the
// Pico does, so the bench and the cabinet cannot drift apart.
//
// M2's tooling is still here and still earns its place: the switch storm (`t`)
// is the heap measurement (§7.3, D17), and it deliberately runs against the
// four-station bench set rather than the catalogue — see TestStations.h.
// Reconnect-after-drop lives in AudioEngine and needs no command: pull a
// stream's plug and it comes back.
//
// This loop() runs on core 0 (-DARDUINO_RUNNING_CORE=0); the audio task owns
// core 1. See Architecture.md §7.1 and D14. Everything the screen does — the
// PNG decode above all — happens on this side of that split, which is why the
// ICY title is collected here by polling rather than drawn by the callback
// that delivers it.

#include <Arduino.h>
#include <WiFi.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "Display.h"
#include "Log.h"
#include "PicoLink.h"
#include "RadioController.h"
#include "Secrets.h"
#include "StationCatalogue.h"
#include "SwitchStorm.h"
#include "TestStations.h"

namespace {

// The full evening, compressed: ~60 changes is one session's worth (§7.3).
constexpr uint16_t kStormChanges = 60;

constexpr uint32_t kWifiTimeoutMs = 30000;
constexpr uint32_t kReportIntervalMs = 30000;

// No catalogue on the filesystem: the board falls back to the compiled-in
// bench stations so M2's storm still runs with nothing uploaded. The Pico is
// still read and still logged in that world — seeing `[pico] frequency 92`
// scroll past is how the UART wiring gets checked before `uploadfs` has ever
// run — but it drives nothing, because every slot it could ask for is empty.
bool benchOnly = false;

bool connectWifi() {
  Log::printf("[wifi] connecting to %s", WIFI_SSID);

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  const uint32_t startedAt = millis();

  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - startedAt > kWifiTimeoutMs) {
      Log::println();
      Log::println("[wifi] timed out");
      return false;
    }

    delay(250);
    Log::print(".");
  }

  Log::println();
  Log::printf("[wifi] connected, ip=%s rssi=%d dBm\n",
                WiFi.localIP().toString().c_str(), WiFi.RSSI());
  return true;
}

// WiFi.RSSI() returns 0 when esp_wifi_sta_get_ap_info() fails. Zero is not a
// plausible signal strength, so it means "no reading", and printing it throws
// away the last good one. Signal strength is the first thing to check when
// decode errors show up, so hold on to it.
//
// The soak that prompted this - "rssi=0 on all 73 samples" - turned out not to
// be evidence of it. That field was printing decodeErrors(), which was
// genuinely 0, because printStatus() passed one more argument than its format
// string had specifiers (see the note there). This guard is still correct, but
// until the format string was fixed its result never reached the log at all,
// so the failure it defends against remains unobserved on this board.
int8_t currentRssi() {
  static int8_t lastGood = 0;

  const int8_t rssi = WiFi.RSSI();
  if (rssi != 0) {
    lastGood = rssi;
  }
  return lastGood;
}

void printStatus() {
  uint8_t vuLeft = 0;
  uint8_t vuRight = 0;
  AudioEngine::vuLevel(vuLeft, vuRight);

  // Every field here has a matching specifier. It did not always: this line
  // passed 14 arguments to 13 specifiers, so `rssi=` printed decodeErrors() and
  // currentRssi() was discarded before it ever reached the log. That is what
  // "a 40-minute soak logged rssi=0 on all 73 samples" actually was — the decode
  // error count, which really was 0 — and not the WiFi.RSSI() fault it was
  // recorded as. Nothing warned, because the build does not pass -Wall; it now
  // does, via build_src_flags in platformio.ini.
  Log::printf(
      "[stat] playing=%d vu=%u/%u heap=%u min=%u largest=%u buf=%u/%u "
      "stack_free=%u connects=%u fails=%u drops=%u decode=%u rssi=%d "
      "logo=%ums/%s\n",
      AudioEngine::isPlaying() ? 1 : 0, vuLeft, vuRight, ESP.getFreeHeap(),
      ESP.getMinFreeHeap(),
      heap_caps_get_largest_free_block(MALLOC_CAP_8BIT | MALLOC_CAP_INTERNAL),
      AudioEngine::streamBufferFilled(), AudioEngine::streamBufferSize(),
      AudioEngine::taskStackFreeBytes(), AudioEngine::connectAttempts(),
      AudioEngine::connectFailures(), AudioEngine::streamDrops(),
      AudioEngine::decodeErrors(), currentRssi(),
      (unsigned)Display::lastLogoDrawMs(),
      Display::lastLogoDrawOk() ? "ok" : "-");
}

// The Pico link, on its own line rather than appended to `[stat]`.
//
// Two reasons for the separate line. The `[stat]` format string has already
// been miscounted once — see the note above — and it is now at fourteen
// specifiers. And these fields answer a different question: `[stat]` is the
// heap and the stream, this is whether the radio can see its own controls.
//
// Reading it during bring-up: `pulses` climbing with `frames` flat means GP14
// is landing but the UART line is not, `frames` with no `pulses` means the
// interrupt wire is loose, and neither moving at all means the Pico is
// unpowered or the ground is not shared. A few stray pulses per minute are
// normal and are not messages — see the ISR. `freq_msgs` against `changes` is
// the debounce working: a spin of the dial should move the first by many and
// the second by one.
void printPicoStatus() {
  Log::printf(
      "[pico] frames=%u rejected=%u stray=%u pulses=%u ready=%d requests=%u "
      "state=%s freq_msgs=%u changes=%u reused=%u chatter=%u/%d settling=%d "
      "tuned=%c%u%s\n",
      PicoLink::framesRead(), PicoLink::framesRejected(),
      PicoLink::strayBytes(), PicoLink::readyPulses(),
      PicoLink::readyPinLevel() ? 1 : 0,
      PicoLink::stateRequests(), PicoLink::haveState() ? "taken" : "pending",
      RadioController::frequencyUpdates(), RadioController::changesApplied(),
      RadioController::reusedStreams(), RadioController::chatterIgnored(),
      RadioController::isChattering() ? 1 : 0,
      RadioController::isSettling() ? 1 : 0,
      RadioController::isTuned() && RadioController::bank() != '\0'
          ? RadioController::bank()
          : '-',
      (unsigned)RadioController::frequency(),
      RadioController::isPaused() ? " paused" : "");
}

// The three-hour hold half of M2 reads these lines. Largest free block is the
// one that matters (§7.3); every 30 s is dense enough to see a slope and sparse
// enough that a whole evening still fits in a text file.
void reportStatusPeriodically() {
  static uint32_t lastReportAt = 0;

  if (millis() - lastReportAt < kReportIntervalMs) {
    return;
  }

  lastReportAt = millis();
  printStatus();
  printPicoStatus();
}

void printHelp() {
  Log::println("[cmd] M 92  play a dial slot: bank L/M/K/U + frequency 87-105");
  Log::println("[cmd] 1-9   play bench station     s  pause (stop the stream)");
  Log::println("[cmd] n     next bench station     g  resume playing");
  Log::printf("[cmd] t     switch storm (%u changes)   x  abort storm\n",
                kStormChanges);
  Log::println("[cmd] l     list the catalogue      b  list bench stations");
  Log::println("[cmd] r     redraw the current slot, audio untouched");
  Log::println("[cmd] h     heap and stream now     p  re-read the Pico state");
  Log::println("[cmd] ?     this help");
}

void listBenchStations() {
  for (size_t i = 0; i < kTestStationCount; i++) {
    Log::printf("[cmd] %u  %-20s %s\n", (unsigned)(i + 1),
                  kTestStations[i].name, kTestStations[i].url);
  }
}

// Parses "M 92", "m92", "K105". Returns false if this is not a slot command,
// leaving the caller to treat the line as something else.
bool parseSlotCommand(const char* line) {
  if (line[0] == '\0') {
    return false;
  }

  const char* cursor = line + 1;
  while (*cursor == ' ') {
    cursor++;
  }

  char* end = nullptr;
  const long frequency = strtol(cursor, &end, 10);

  if (end == cursor || *end != '\0') {
    return false;
  }

  if (frequency < StationCatalogue::kMinFrequency ||
      frequency > StationCatalogue::kMaxFrequency) {
    Log::printf("[cmd] frequency %ld is outside %u-%u\n", frequency,
                (unsigned)StationCatalogue::kMinFrequency,
                (unsigned)StationCatalogue::kMaxFrequency);
    return true;  // recognised, just wrong — do not fall through to help
  }

  // No settle timer: a typed slot is not a dial being spun.
  RadioController::selectNow(line[0], (uint8_t)frequency);
  return true;
}

void playBenchStation(size_t index) {
  if (index >= kTestStationCount) {
    Log::printf("[cmd] no station %u\n", (unsigned)(index + 1));
    return;
  }

  Log::printf("[cmd] play %s\n", kTestStations[index].name);
  Display::showBenchStation(kTestStations[index].name);
  AudioEngine::playUrl(kTestStations[index].url);
}

// A slot command is two tokens ("M 92"), so the console reads lines rather than
// single keys. Everything still returns immediately — the line is assembled a
// character at a time across loop() passes, so the storm keeps being driven
// while a command is half-typed.
void handleCommandLine(char* line) {
  static size_t currentStation = 0;

  if (line[0] == '\0') {
    return;  // bare Enter, and the second half of a CRLF
  }

  // `r` is in here with the playback commands even though it touches no
  // audio: it repaints the screen and re-decodes a logo, which lands in the
  // middle of the heap sample the storm is in the process of taking.
  const bool disturbsAMeasurement =
      line[1] != '\0' || line[0] == 's' || line[0] == 'g' || line[0] == 'n' ||
      line[0] == 'r' || (line[0] >= '1' && line[0] <= '9');

  // A manual station change during a storm silently corrupts the run: the
  // measurement that follows is taken against a stream the storm did not open,
  // and the dwell it was timing is gone. Refuse rather than quietly produce a
  // wrong number.
  if (SwitchStorm::isRunning() && disturbsAMeasurement) {
    Log::println("[cmd] storm running - press x to abort it first");
    return;
  }

  // Try the slot form first. It is the only multi-character command, so a line
  // that is not one falls through to the single-key table below.
  if (line[1] != '\0' && parseSlotCommand(line)) {
    return;
  }

  if (line[1] != '\0') {
    Log::printf("[cmd] don't understand \"%s\"\n", line);
    printHelp();
    return;
  }

  const char key = line[0];

  if (key >= '1' && key <= '9') {
    currentStation = (size_t)(key - '1');
    playBenchStation(currentStation);
    return;
  }

  switch (key) {
    case 'n':
      currentStation = (currentStation + 1) % kTestStationCount;
      playBenchStation(currentStation);
      break;

    case 's':
      // The play/pause button, typed. It goes through RadioController rather
      // than straight to AudioEngine so the console and the Pico cannot end up
      // disagreeing about whether the radio is paused.
      Log::println("[cmd] pause");
      if (benchOnly) {
        AudioEngine::stop();
        Display::showBenchStation("");
      } else {
        RadioController::setPaused(true);
      }
      break;

    case 'g':
      Log::println("[cmd] play");
      if (benchOnly) {
        Log::println("[cmd] no catalogue - pick a bench station with 1-9");
      } else {
        RadioController::setPaused(false);
      }
      break;

    case 'r':
      // Redraw only: the logo is read again and the whole layout is repainted,
      // but the stream is left playing. Run it against `h` to see what the
      // drawing path alone does to the largest free block, with no TLS session
      // being torn down underneath the measurement.
      RadioController::redraw();
      break;

    case 'p':
      // Re-run the boot handshake by hand. Useful when the Pico is plugged in
      // after the ESP32 has already booted, which on a bench is most of the
      // time.
      Log::println("[cmd] asking the Pico for its state");
      PicoLink::requestState();
      break;

    case 't':
      // The storm drives AudioEngine itself, so whatever RadioController
      // thinks is playing stops being true the moment it starts.
      RadioController::forgetStream();
      SwitchStorm::start(kStormChanges);
      break;

    case 'x':
      SwitchStorm::stop();
      break;

    case 'h':
      printStatus();
      printPicoStatus();
      break;

    case 'l':
      StationCatalogue::list();
      break;

    case 'b':
      listBenchStations();
      break;

    case '?':
      printHelp();
      break;

    default:
      Log::printf("[cmd] unknown key '%c'\n", key);
      printHelp();
      break;
  }
}

// Long enough for "M 105" many times over; a line that overruns it is line
// noise on the UART, so the buffer resets rather than dispatching a truncated
// command that might happen to parse.
constexpr size_t kCommandBufferSize = 32;

void pollSerial() {
  static char buffer[kCommandBufferSize];
  static size_t length = 0;

  while (Serial.available() > 0) {
    const char c = (char)Serial.read();

    if (c == '\n' || c == '\r') {
      buffer[length] = '\0';
      handleCommandLine(buffer);
      length = 0;
      continue;
    }

    if (length + 1 >= sizeof(buffer)) {
      length = 0;
      Log::println("[cmd] line too long, ignored");
      continue;
    }

    buffer[length++] = c;
  }
}

// The bottom line of the screen (§6). The title arrives on the audio task
// and is collected here, on core 0, because drawing it where it arrives
// would put a TFT write inside the decode loop (D14).
//
// An empty title is a real event rather than a no-op: it is what the engine
// publishes when the station changes, and it means "blank the line".
void pollStreamTitle() {
  char title[AudioEngine::kMaxStreamTitleLength];

  if (AudioEngine::takeStreamTitle(title, sizeof(title))) {
    Display::showSongTitle(title);
  }
}

// The physical controls (§4). Every decoded message is logged before it is
// acted on, because during bring-up the interesting question is usually "did
// the knob produce anything at all" rather than what the radio did about it.
void pollPico() {
  static bool saidStormIsRunning = false;

  if (!SwitchStorm::isRunning()) {
    saidStormIsRunning = false;
  }

  PicoLink::Message message;

  while (PicoLink::poll(message)) {
    switch (message.command) {
      case PicoLink::Command::ButtonPressed:
        Log::printf("[pico] button %d (%c)\n", (int)message.buttonIndex,
                    PicoLink::bankLetter(message.buttonIndex) == '\0'
                        ? '-'
                        : PicoLink::bankLetter(message.buttonIndex));
        break;

      case PicoLink::Command::NewFrequency:
        Log::printf("[pico] frequency %u\n", (unsigned)message.frequency);
        break;

      case PicoLink::Command::PlayPause:
        Log::printf("[pico] %s\n", message.isPause ? "pause" : "play");
        break;

      case PicoLink::Command::State:
        Log::printf("[pico] state: button %d (%c) frequency %u %s\n",
                    (int)message.buttonIndex,
                    PicoLink::bankLetter(message.buttonIndex) == '\0'
                        ? '-'
                        : PicoLink::bankLetter(message.buttonIndex),
                    (unsigned)message.frequency,
                    message.isPause ? "paused" : "playing");
        break;
    }

    if (benchOnly) {
      continue;  // logged, but there are no slots for it to select
    }

    // A station change the storm did not make corrupts its measurement exactly
    // as a typed one does — and unlike the console, the dial cannot be told to
    // wait. Say so once per storm rather than once per message.
    if (SwitchStorm::isRunning()) {
      if (!saidStormIsRunning) {
        Log::println("[pico] ignoring the controls - switch storm running, press x to abort");
        saidStormIsRunning = true;
      }
      continue;
    }

    switch (message.command) {
      case PicoLink::Command::ButtonPressed:
        RadioController::setBank(PicoLink::bankLetter(message.buttonIndex));
        break;

      case PicoLink::Command::NewFrequency:
        RadioController::setFrequency(message.frequency);
        break;

      case PicoLink::Command::PlayPause:
        RadioController::setPaused(message.isPause);
        break;

      case PicoLink::Command::State:
        RadioController::applySnapshot(PicoLink::bankLetter(message.buttonIndex),
                                       message.frequency, message.isPause);
        break;
    }
  }
}

}  // namespace

void setup() {
  Serial.begin(115200);
  Log::begin();
  delay(1000);

  Log::println();
  Log::println("ESP32 internet radio - M5 Pico UART");

  // First, before the screen and long before WiFi. GPIO 33 is high-Z out of
  // reset and the Pico polls it without ever initialising its own end (§4), so
  // until this line runs the request-state line is floating and the Pico may
  // already be streaming State snapshots at a board that is not listening.
  PicoLink::begin();

  // One line unless it fails. §4 is frozen and the parser is hand-written
  // against it, so this is the cheapest possible check that the two still
  // agree — and it runs before anything else can be blamed for a dial that
  // does nothing.
  PicoLink::selfTest();

  RadioController::begin();

  // Before WiFi and before the catalogue: a screen that stays black is the
  // first symptom of miswiring, and it should be visible immediately rather
  // than after a 30-second WiFi timeout.
  Display::begin();

  AudioEngine::begin();

  // Before WiFi: the catalogue is a local file, and knowing whether it loaded
  // is worth having even on a board that never gets online.
  const bool catalogueLoaded = StationCatalogue::begin();

  if (!connectWifi()) {
    // M7 gives this a real retry policy and a screen to say so. For M3, say it
    // and sit still rather than pretending to play.
    return;
  }

  printHelp();

  if (!catalogueLoaded || StationCatalogue::filledSlots() == 0) {
    // The bench stations are compiled in, so the board is still useful for the
    // storm and for M2's measurements with no filesystem at all. Say which
    // world we are in rather than looking like a catalogue full of empty slots.
    benchOnly = true;
    Log::println("[cat] no catalogue - bench stations only (keys 1-9)");
    Log::println("[cat] the Pico is still read and logged, but drives nothing");
    listBenchStations();
    playBenchStation(0);
    return;
  }

  if (StationCatalogue::rejectedRows() > 0) {
    Log::printf("[cat] %u rows were rejected - see the lines above\n",
                (unsigned)StationCatalogue::rejectedRows());
  }

  // No default station any more. Until M4 the boot log wanted a genuine connect
  // to look at, so it played L 87; from M5 the answer to "which station" comes
  // from the knobs, and playing anything else first would be both wrong and
  // audible. The handshake runs in loop(), so a Pico that is unplugged costs a
  // retry line every couple of seconds and nothing else — the console still
  // works, and `p` re-runs this by hand once it is plugged in.
  PicoLink::requestState();
}

void loop() {
  pollSerial();
  pollPico();
  RadioController::update();
  pollStreamTitle();
  SwitchStorm::update();
  reportStatusPeriodically();
  delay(20);
}
