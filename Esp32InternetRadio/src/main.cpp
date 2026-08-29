// M4 — display.
//
// ESP32 + PCM5102A + ST7735 on a breadboard. Connects to WiFi, parses
// `data/stations.csv` off LittleFS into 4 banks × 19 dial positions, and
// drives both the audio task and the screen from the serial console — so the
// whole selection path is exercised without the Pico. Typing `M 92` is what
// the toggle button and the tuning capacitor will do at M5.
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
#include "Secrets.h"
#include "StationCatalogue.h"
#include "SwitchStorm.h"
#include "TestStations.h"

namespace {

// The full evening, compressed: ~60 changes is one session's worth (§7.3).
constexpr uint16_t kStormChanges = 60;

constexpr uint32_t kWifiTimeoutMs = 30000;
constexpr uint32_t kReportIntervalMs = 30000;

// The slot the screen is currently showing. A stop has to redraw the
// frequency it stopped on (§6: paused and empty look the same), and `r`
// re-decodes the logo of whatever is up, so both need to know. '\0' means no
// slot has been selected yet — the bench-station world, which has no dial
// position to show.
char currentBank = '\0';
uint8_t currentFrequency = 0;

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
}

void printHelp() {
  Log::println("[cmd] M 92  play a dial slot: bank L/M/K/U + frequency 87-105");
  Log::println("[cmd] 1-9   play bench station     s  stop");
  Log::println("[cmd] n     next bench station     h  heap now");
  Log::printf("[cmd] t     switch storm (%u changes)   x  abort storm\n",
                kStormChanges);
  Log::println("[cmd] l     list the catalogue      b  list bench stations");
  Log::println("[cmd] r     redraw the current slot, audio untouched");
  Log::println("[cmd] ?     this help");
}

void listBenchStations() {
  for (size_t i = 0; i < kTestStationCount; i++) {
    Log::printf("[cmd] %u  %-20s %s\n", (unsigned)(i + 1),
                  kTestStations[i].name, kTestStations[i].url);
  }
}

// Draws a slot and nothing else — no audio, no reconnect.
//
// Separated out because this is the half of a station change M4 needs to be
// able to run alone. A TLS handshake and a PNG decode both cost heap, and a
// measurement that always runs them together cannot say which of the two
// moved the largest free block. The `r` command is this function.
void drawSlot(char bank, uint8_t frequency,
              const StationCatalogue::Station* station) {
  if (station == nullptr) {
    Display::showFrequencyOnly(bank, frequency);
    return;
  }

  // 64 bytes holds "/logos/" plus the longest `.565` filename the build
  // script emits with room to spare; logoPath() reports the overflow it
  // cannot produce rather than truncating into an open() that fails
  // obscurely.
  char logoPath[64];
  const bool haveLogo =
      StationCatalogue::logoPath(*station, logoPath, sizeof(logoPath));

  Display::showStation(bank, frequency, station->name,
                       haveLogo ? logoPath : nullptr);
}

// Selects a dial slot, exactly as the Pico will at M5.
//
// An empty slot is not an error (§6): it stops audio and says so once. That is
// the behaviour the real dial needs — 76 positions, and turning past an unfilled
// one has to go quiet rather than keep the previous station playing or complain.
void playSlot(char button, uint8_t frequency) {
  const StationCatalogue::Station* station =
      StationCatalogue::find(button, frequency);

  currentBank = (char)toupper((unsigned char)button);
  currentFrequency = frequency;

  if (station == nullptr) {
    Log::printf("[cmd] %c %u is empty\n", currentBank, (unsigned)frequency);
    AudioEngine::stop();
    drawSlot(currentBank, currentFrequency, nullptr);
    return;
  }

  Log::printf("[cmd] %c %u  %s\n", currentBank, (unsigned)frequency,
              station->name);

  // Draw before queueing the stream, not after. playUrl() returns straight
  // away but the audio task then sits on a TLS handshake for well over a
  // second (see attemptConnect), and a screen that only changes once the
  // sound starts makes the radio look like it ignored the dial.
  drawSlot(currentBank, currentFrequency, station);

  // playUrl() only fails on an over-long URL or a full queue. The catalogue
  // already rejected the first at boot, so this is worth reporting rather than
  // ignoring: it means commands are arriving faster than the audio task retires
  // them, which is the debounce problem M5 has to solve for the tuning dial.
  if (!AudioEngine::playUrl(station->url)) {
    Log::println("[cmd] busy - command queue full, try again");
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

  playSlot(line[0], (uint8_t)frequency);
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
      line[1] != '\0' || line[0] == 's' || line[0] == 'n' ||
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
      Log::println("[cmd] stop");
      AudioEngine::stop();
      // §6: a pause is a disconnect, and it looks the same as an empty slot
      // — frequency on black. With no slot selected there is no frequency to
      // draw, so the screen simply goes dark.
      if (currentBank != '\0') {
        Display::showFrequencyOnly(currentBank, currentFrequency);
      } else {
        Display::showBenchStation("");
      }
      break;

    case 'r':
      // Redraw only: the logo is decoded again and the whole layout is
      // repainted, but the stream is left playing. Run it against `h` to see
      // what the PNG path alone does to the largest free block, with no TLS
      // session being torn down underneath the measurement.
      if (currentBank == '\0') {
        Log::println("[cmd] no slot selected - pick one with e.g. M 92");
      } else {
        drawSlot(currentBank, currentFrequency,
                 StationCatalogue::find(currentBank, currentFrequency));
      }
      break;

    case 't':
      SwitchStorm::start(kStormChanges);
      break;

    case 'x':
      SwitchStorm::stop();
      break;

    case 'h':
      printStatus();
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

}  // namespace

void setup() {
  Serial.begin(115200);
  Log::begin();
  delay(1000);

  Log::println();
  Log::println("ESP32 internet radio - M4 display");

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
    Log::println("[cat] no catalogue - bench stations only (keys 1-9)");
    listBenchStations();
    playBenchStation(0);
    return;
  }

  if (StationCatalogue::rejectedRows() > 0) {
    Log::printf("[cat] %u rows were rejected - see the lines above\n",
                (unsigned)StationCatalogue::rejectedRows());
  }

  // L 87 is the first filled slot in the shipped catalogue, and starting on a
  // real station means the boot log shows a genuine connect rather than
  // silence that has to be told apart from a fault.
  playSlot('L', 87);
}

void loop() {
  pollSerial();
  pollStreamTitle();
  SwitchStorm::update();
  reportStatusPeriodically();
  delay(20);
}
