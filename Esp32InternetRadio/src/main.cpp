// M1 — first sound.
//
// ESP32 + PCM5102A on a breadboard, nothing else. Connects to WiFi, hands one
// hard-coded HTTPS stream to the audio task and reports the numbers M2 will be
// compared against. No UART, no TFT, no station catalogue yet.
//
// This loop() runs on core 0 (-DARDUINO_RUNNING_CORE=0); the audio task owns
// core 1. See Architecture.md §7.1 and D14.

#include <Arduino.h>
#include <WiFi.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "Secrets.h"

namespace {

// D3: the milestone deliberately starts on HTTPS. Proving the easy HTTP case
// first would only hide the risk M1 exists to expose. This is the same URL the
// M0 spike streamed 512 KB from.
constexpr char kTestStreamUrl[] =
    "https://s1-webradio.rockantenne.de/80er-rock/stream/mp3";

constexpr uint32_t kWifiTimeoutMs = 30000;
constexpr uint32_t kReportIntervalMs = 10000;

bool connectWifi() {
  Serial.printf("[wifi] connecting to %s", WIFI_SSID);

  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  const uint32_t startedAt = millis();

  while (WiFi.status() != WL_CONNECTED) {
    if (millis() - startedAt > kWifiTimeoutMs) {
      Serial.println();
      Serial.println("[wifi] timed out");
      return false;
    }

    delay(250);
    Serial.print(".");
  }

  Serial.println();
  Serial.printf("[wifi] connected, ip=%s rssi=%d dBm\n",
                WiFi.localIP().toString().c_str(), WiFi.RSSI());
  return true;
}

// The M1 baseline. Free heap answers "does it play", largest free block is the
// one that matters over hours (§7.3) — start recording it now so M2's soak has
// something to compare against.
void reportStatus() {
  static uint32_t lastReportAt = 0;

  if (millis() - lastReportAt < kReportIntervalMs) {
    return;
  }

  lastReportAt = millis();

  uint8_t vuLeft = 0;
  uint8_t vuRight = 0;
  AudioEngine::vuLevel(vuLeft, vuRight);

  Serial.printf(
      "[stat] playing=%d vu=%u/%u heap=%u min=%u largest=%u buf=%u/%u "
      "stack_free=%u\n",
      AudioEngine::isPlaying() ? 1 : 0, vuLeft, vuRight, ESP.getFreeHeap(),
      ESP.getMinFreeHeap(),
      heap_caps_get_largest_free_block(MALLOC_CAP_8BIT | MALLOC_CAP_INTERNAL),
      AudioEngine::streamBufferFilled(), AudioEngine::streamBufferSize(),
      AudioEngine::taskStackFreeBytes());
}

}  // namespace

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("ESP32 internet radio - M1 first sound");

  AudioEngine::begin();

  if (!connectWifi()) {
    // M7 gives this a real retry policy and a screen to say so. For M1, say it
    // and sit still rather than pretending to play.
    return;
  }

  AudioEngine::playUrl(kTestStreamUrl);
}

void loop() {
  reportStatus();
  delay(50);
}
