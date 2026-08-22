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

// D17: a stream server hanging up mid-song happens during ordinary listening
// and has to heal by itself. Retry quickly at first — most drops are a single
// bad moment on the CDN and the next connect works — then back off so a
// genuinely dead station does not hammer the network for the rest of the
// evening. There is no attempt limit on purpose: a radio left on the wrong
// station should start playing again when the station comes back.
constexpr uint32_t kFirstRetryDelayMs = 1000;
constexpr uint32_t kMaxRetryDelayMs = 30000;

// Drop detection is armed only after the stream has been up this long.
// connecttohost() returns as soon as the response headers parse, and a server
// that closes immediately afterwards would otherwise be indistinguishable from
// one that never opened — producing a retry loop with no delay in it.
constexpr uint32_t kDropGraceMs = 2000;

enum class CommandType : uint8_t { PlayUrl, Stop };

struct Command {
  CommandType type;
  char url[AudioEngine::kMaxUrlLength];
};

Audio audio;
QueueHandle_t commandQueue = nullptr;
TaskHandle_t audioTaskHandle = nullptr;

// --- Audio task state. Touched only on core 1, except the counters, which are
// --- read (never written) from core 0.
char desiredUrl[AudioEngine::kMaxUrlLength] = {0};
AudioEngine::State streamState = AudioEngine::State::Idle;
uint32_t retryAtMs = 0;
uint32_t nextRetryDelayMs = kFirstRetryDelayMs;
uint32_t playingSinceMs = 0;

volatile uint32_t connectAttemptCount = 0;
volatile uint32_t connectFailureCount = 0;
volatile uint32_t streamDropCount = 0;

// millis() wraps after ~49 days. Comparing a *difference* against a bound is
// correct across the wrap; comparing the timestamps directly is not.
bool hasElapsed(uint32_t sinceMs, uint32_t intervalMs) {
  return (millis() - sinceMs) >= intervalMs;
}

void scheduleRetry() {
  streamState = AudioEngine::State::Reconnecting;
  retryAtMs = millis() + nextRetryDelayMs;

  Serial.printf("[audio] reconnect in %u ms\n", nextRetryDelayMs);

  nextRetryDelayMs *= 2;
  if (nextRetryDelayMs > kMaxRetryDelayMs) {
    nextRetryDelayMs = kMaxRetryDelayMs;
  }
}

void attemptConnect() {
  // Tear the old session down first — one TLS connection at a time (§7.2).
  audio.stopSong();

  // Publish Connecting *before* the blocking call, not after it. connecttohost()
  // sits on a TLS handshake for well over a second, and until it returns the
  // state variable still says whatever the previous station left behind — so a
  // core-0 reader polling for "Playing" sees the old station's value and
  // concludes the new one connected instantly. That is exactly what happened to
  // the M2 switch storm, which logged connect=20ms against handshakes the audio
  // log timed at 1356 ms.
  streamState = AudioEngine::State::Connecting;

  connectAttemptCount++;
  Serial.printf("[audio] connecting to %s\n", desiredUrl);

  if (audio.connecttohost(desiredUrl)) {
    streamState = AudioEngine::State::Playing;
    playingSinceMs = millis();
    nextRetryDelayMs = kFirstRetryDelayMs;
    return;
  }

  connectFailureCount++;
  Serial.println("[audio] connect failed");
  scheduleRetry();
}

void handleCommand(const Command& command) {
  switch (command.type) {
    case CommandType::PlayUrl:
      strlcpy(desiredUrl, command.url, sizeof(desiredUrl));
      // A deliberate station change starts the backoff ladder from the bottom
      // again, even if the previous station was in a long retry cycle.
      nextRetryDelayMs = kFirstRetryDelayMs;
      attemptConnect();
      break;

    case CommandType::Stop:
      // Clearing the URL is what ends supervision — otherwise the drop check
      // below would treat a deliberate stop as a failure and reconnect.
      desiredUrl[0] = '\0';
      streamState = AudioEngine::State::Idle;
      audio.stopSong();
      Serial.println("[audio] stopped");
      break;
  }
}

// Runs every pass of the audio loop. This is the whole of D17's stream-drop
// recovery: notice that a stream we believe is playing has gone away, and get
// it back without anyone touching the radio.
void superviseStream() {
  switch (streamState) {
    case AudioEngine::State::Playing:
      if (!audio.isRunning() && hasElapsed(playingSinceMs, kDropGraceMs)) {
        streamDropCount++;
        Serial.println("[audio] stream dropped");
        scheduleRetry();
      }
      break;

    case AudioEngine::State::Reconnecting:
      // Signed difference, so this stays correct across the millis() wrap.
      if ((int32_t)(millis() - retryAtMs) >= 0) {
        attemptConnect();
      }
      break;

    case AudioEngine::State::Connecting:
      // attemptConnect() blocks, so this state is only ever observed from the
      // other core. Nothing to supervise here.
      break;

    case AudioEngine::State::Idle:
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
    superviseStream();
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
// M1/M2 they only reach the serial monitor; M4 routes the station and title
// lines to the display instead.

void audio_info(const char* info) { Serial.printf("[audio] %s\n", info); }

void audio_showstation(const char* info) { Serial.printf("[station] %s\n", info); }

// ICY metadata — `Artist - Title`, pushed by the server whenever the track
// changes. M4 draws this on the bottom line of the screen (§6).
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

State state() { return streamState; }

uint32_t connectAttempts() { return connectAttemptCount; }

uint32_t connectFailures() { return connectFailureCount; }

uint32_t streamDrops() { return streamDropCount; }

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
