#include "AudioEngine.h"

#include <Arduino.h>
#include <Audio.h>

#include "Pins.h"

namespace {

// The input ring buffer is the shock absorber for network jitter, and on a
// no-PSRAM WROOM it is whatever heap is left after TLS (~40 KB) and the
// decoder (~30 KB) — see §7.2. The library defaults to 16000 bytes; this is
// the M1 starting point and the first lever to pull if M2's soak stutters
// (§9.1 rung 1). The size actually allocated is logged at connect time.
constexpr int kStreamBufferBytes = 30000;

// D12: volume is the SABA's analog pot. This is a fixed software gain, 0..21,
// tuned once. M6 sets the real value for the amplifier injection point; for M1
// on headphones or a test amp, mid-scale is a safe place to start.
constexpr uint8_t kFixedGain = 15;

// D14: core 1 is the audio task's alone. Priority 2 puts it above Arduino's
// loopTask (priority 1), which -DARDUINO_RUNNING_CORE=0 has moved to core 0.
constexpr BaseType_t kAudioCore = 1;
constexpr UBaseType_t kTaskPriority = 2;
constexpr uint32_t kTaskStackBytes = 10000;
constexpr UBaseType_t kQueueDepth = 4;

enum class CommandType : uint8_t { PlayUrl, Stop };

struct Command {
  CommandType type;
  char url[AudioEngine::kMaxUrlLength];
};

Audio audio;
QueueHandle_t commandQueue = nullptr;
TaskHandle_t audioTaskHandle = nullptr;

void handleCommand(const Command& command) {
  switch (command.type) {
    case CommandType::PlayUrl:
      // Tear the old session down first — one TLS connection at a time (§7.2).
      audio.stopSong();
      Serial.printf("[audio] connecting to %s\n", command.url);
      if (!audio.connecttohost(command.url)) {
        Serial.println("[audio] connect failed");
      }
      break;

    case CommandType::Stop:
      audio.stopSong();
      Serial.println("[audio] stopped");
      break;
  }
}

void audioTask(void*) {
  // Both of these must happen before the first connect: setPinout brings up
  // the I2S driver, and setBufsize is ignored once the input buffer exists.
  audio.setBufsize(kStreamBufferBytes, -1);

  if (!audio.setPinout(kPinI2sBclk, kPinI2sLrck, kPinI2sData)) {
    // Worth shouting about: a silent DAC otherwise looks identical to a
    // muted one, and the rest of the log stays perfectly healthy.
    Serial.println("[audio] setPinout FAILED - I2S driver did not start");
  }

  audio.setVolume(kFixedGain);

  // Read the gain back rather than echoing what we passed in, so the log
  // reflects what the library actually stored.
  Serial.printf("[audio] i2s bclk=%u lrck=%u data=%u, gain=%u/%u\n",
                kPinI2sBclk, kPinI2sLrck, kPinI2sData, audio.getVolume(),
                audio.maxVolume());

  for (;;) {
    Command command;
    while (xQueueReceive(commandQueue, &command, 0) == pdTRUE) {
      handleCommand(command);
    }

    audio.loop();
    vTaskDelay(1);
  }
}

bool enqueue(const Command& command) {
  if (commandQueue == nullptr) {
    return false;
  }

  return xQueueSend(commandQueue, &command, 0) == pdTRUE;
}

}  // namespace

// --- ESP32-audioI2S callbacks ----------------------------------------------
// These are weak symbols in Audio.h and are invoked from the audio task. For
// M1 they only reach the serial monitor; M4 routes the station and title lines
// to the display instead.

void audio_info(const char* info) { Serial.printf("[audio] %s\n", info); }

void audio_showstation(const char* info) { Serial.printf("[station] %s\n", info); }

void audio_showstreamtitle(const char* info) { Serial.printf("[title] %s\n", info); }

void audio_bitrate(const char* info) { Serial.printf("[bitrate] %s\n", info); }

// --- Public API -------------------------------------------------------------

namespace AudioEngine {

void begin() {
  commandQueue = xQueueCreate(kQueueDepth, sizeof(Command));

  if (commandQueue == nullptr) {
    Serial.println("[audio] failed to create command queue");
    return;
  }

  xTaskCreatePinnedToCore(audioTask, "audio", kTaskStackBytes, nullptr,
                          kTaskPriority, &audioTaskHandle, kAudioCore);
}

bool playUrl(const char* url) {
  if (url == nullptr || strlen(url) >= kMaxUrlLength) {
    return false;
  }

  Command command;
  command.type = CommandType::PlayUrl;
  strlcpy(command.url, url, kMaxUrlLength);

  return enqueue(command);
}

bool stop() {
  Command command;
  command.type = CommandType::Stop;
  command.url[0] = '\0';

  return enqueue(command);
}

bool isPlaying() { return audio.isRunning(); }

void vuLevel(uint8_t& left, uint8_t& right) {
  const uint16_t vu = audio.getVUlevel();
  left = vu >> 8;
  right = vu & 0xFF;
}

uint32_t streamBufferSize() { return audio.inBufferSize(); }

uint32_t streamBufferFilled() { return audio.inBufferFilled(); }

uint32_t taskStackFreeBytes() {
  if (audioTaskHandle == nullptr) {
    return 0;
  }

  // ESP-IDF's FreeRTOS reports the high-water mark in bytes, not words.
  return uxTaskGetStackHighWaterMark(audioTaskHandle);
}

}  // namespace AudioEngine
