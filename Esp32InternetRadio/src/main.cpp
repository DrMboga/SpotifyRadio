// M2 — stream robustness.
//
// ESP32 + PCM5102A on a breadboard, nothing else. Connects to WiFi and drives
// the audio task from single-key serial commands, so the whole selection path
// is exercised without the Pico or the screen.
//
// M2 asks whether this board survives a listening *session* (Architecture.md
// §7.3, D17): 2–3 hours, then switched off, and dominated by station changes —
// 30–60 of them an evening. So the milestone's primary test is the switch
// storm (`t`), not sitting on one stream. Reconnect-after-drop lives in
// AudioEngine and needs no command: pull a stream's plug and it comes back.
//
// This loop() runs on core 0 (-DARDUINO_RUNNING_CORE=0); the audio task owns
// core 1. See Architecture.md §7.1 and D14.

#include <Arduino.h>
#include <WiFi.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "Log.h"
#include "Secrets.h"
#include "SwitchStorm.h"
#include "TestStations.h"

namespace {

// The full evening, compressed: ~60 changes is one session's worth (§7.3).
constexpr uint16_t kStormChanges = 60;

constexpr uint32_t kWifiTimeoutMs = 30000;
constexpr uint32_t kReportIntervalMs = 30000;

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

void printStatus() {
  uint8_t vuLeft = 0;
  uint8_t vuRight = 0;
  AudioEngine::vuLevel(vuLeft, vuRight);

  Log::printf(
      "[stat] playing=%d vu=%u/%u heap=%u min=%u largest=%u buf=%u/%u "
      "stack_free=%u connects=%u fails=%u drops=%u rssi=%d\n",
      AudioEngine::isPlaying() ? 1 : 0, vuLeft, vuRight, ESP.getFreeHeap(),
      ESP.getMinFreeHeap(),
      heap_caps_get_largest_free_block(MALLOC_CAP_8BIT | MALLOC_CAP_INTERNAL),
      AudioEngine::streamBufferFilled(), AudioEngine::streamBufferSize(),
      AudioEngine::taskStackFreeBytes(), AudioEngine::connectAttempts(),
      AudioEngine::connectFailures(), AudioEngine::streamDrops(),
      AudioEngine::decodeErrors(), WiFi.RSSI());
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
  Log::println("[cmd] 1-9  play station        s  stop");
  Log::println("[cmd] n    next station        h  heap now");
  Log::printf("[cmd] t    switch storm (%u changes)   x  abort storm\n",
                kStormChanges);
  Log::println("[cmd] l    list stations       ?  this help");
}

void listStations() {
  for (size_t i = 0; i < kTestStationCount; i++) {
    Log::printf("[cmd] %u  %-20s %s\n", (unsigned)(i + 1),
                  kTestStations[i].name, kTestStations[i].url);
  }
}

void playStation(size_t index) {
  if (index >= kTestStationCount) {
    Log::printf("[cmd] no station %u\n", (unsigned)(index + 1));
    return;
  }

  Log::printf("[cmd] play %s\n", kTestStations[index].name);
  AudioEngine::playUrl(kTestStations[index].url);
}

// Single characters rather than a line protocol: the storm has to keep being
// driven from loop() while a command is typed, and nothing here is worth a
// parser.
void handleSerialCommand(char key) {
  static size_t currentStation = 0;

  // A manual station change during a storm silently corrupts the run: the
  // measurement that follows is taken against a stream the storm did not open,
  // and the dwell it was timing is gone. Refuse rather than quietly produce a
  // wrong number.
  if (SwitchStorm::isRunning() && (key == 's' || key == 'n' || (key >= '1' && key <= '9'))) {
    Log::println("[cmd] storm running - press x to abort it first");
    return;
  }

  if (key >= '1' && key <= '9') {
    currentStation = (size_t)(key - '1');
    playStation(currentStation);
    return;
  }

  switch (key) {
    case 'n':
      currentStation = (currentStation + 1) % kTestStationCount;
      playStation(currentStation);
      break;

    case 's':
      Log::println("[cmd] stop");
      AudioEngine::stop();
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
      listStations();
      break;

    case '?':
      printHelp();
      break;

    default:
      break;  // stray newlines from the monitor
  }
}

void pollSerial() {
  while (Serial.available() > 0) {
    handleSerialCommand((char)Serial.read());
  }
}

}  // namespace

void setup() {
  Serial.begin(115200);
  Log::begin();
  delay(1000);

  Log::println();
  Log::println("ESP32 internet radio - M2 stream robustness");

  AudioEngine::begin();

  if (!connectWifi()) {
    // M7 gives this a real retry policy and a screen to say so. For M2, say it
    // and sit still rather than pretending to play.
    return;
  }

  listStations();
  printHelp();

  playStation(0);
}

void loop() {
  pollSerial();
  SwitchStorm::update();
  reportStatusPeriodically();
  delay(20);
}
