#include "SwitchStorm.h"

#include <Arduino.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "TestStations.h"

namespace {

// Long enough that the connection is fully established and the input ring
// buffer has actually filled — a change that is torn down before the buffer
// allocates would not exercise the thing being measured. Short enough that 60
// changes finish in about ten minutes.
constexpr uint32_t kDwellMs = 8000;

// A connect that has not produced audio by now is counted as a failure and the
// storm moves on. Generous on purpose: a slow CDN is not what is being tested,
// and stalling the whole run on one bad station would waste the measurement.
constexpr uint32_t kConnectTimeoutMs = 20000;

// Verdict thresholds, in bytes of largest-free-block lost per station change.
// Anchored to the usage profile (§7.3): a session is ~60 changes, so 32
// bytes/change is 2 KB an evening — noise. 256 bytes/change is 15 KB, which is
// over half the stream buffer's headroom and would make the radio unreliable
// well before bedtime.
constexpr int32_t kPassBytesPerChange = 32;
constexpr int32_t kMarginalBytesPerChange = 256;

enum class Phase : uint8_t { Idle, Connecting, Dwelling };

Phase phase = Phase::Idle;

uint16_t targetChanges = 0;
uint16_t completedChanges = 0;
uint16_t connectFailures = 0;
size_t stationIndex = 0;

uint32_t phaseStartedAt = 0;
uint32_t connectMs = 0;

// Snapshot of AudioEngine::connectAttempts() taken when the change was
// requested. playUrl() only *queues* the change, so for a few milliseconds
// afterwards the engine is still reporting the previous station as Playing.
// Waiting for the attempt counter to move as well is what stops the storm
// timing a connect it never made.
uint32_t attemptsAtRequest = 0;

uint32_t firstLargestBlock = 0;
uint32_t lastLargestBlock = 0;
uint32_t minLargestBlock = 0;
uint32_t dropsAtStart = 0;

uint32_t largestFreeBlock() {
  return heap_caps_get_largest_free_block(MALLOC_CAP_8BIT | MALLOC_CAP_INTERNAL);
}

void beginChange() {
  stationIndex = completedChanges % kTestStationCount;

  phase = Phase::Connecting;
  phaseStartedAt = millis();
  connectMs = 0;
  attemptsAtRequest = AudioEngine::connectAttempts();

  AudioEngine::playUrl(kTestStations[stationIndex].url);
}

void printSummary() {
  Serial.println("[storm] ---------------- summary ----------------");
  Serial.printf("[storm] changes completed: %u of %u (%u connect failures)\n",
                completedChanges, targetChanges, connectFailures);

  if (completedChanges == 0) {
    Serial.println("[storm] no completed changes - nothing to compare");
    return;
  }

  const int32_t net = (int32_t)lastLargestBlock - (int32_t)firstLargestBlock;
  const int32_t perChange = net / (int32_t)completedChanges;

  Serial.printf("[storm] largest free block: first=%u last=%u min=%u\n",
                firstLargestBlock, lastLargestBlock, minLargestBlock);
  Serial.printf("[storm] net %+d bytes over %u changes = %+d bytes/change\n",
                net, completedChanges, perChange);
  Serial.printf("[storm] stream drops during storm: %u\n",
                AudioEngine::streamDrops() - dropsAtStart);

  // The number the milestone turns on, converted into the unit that actually
  // matters: what a single evening of listening costs.
  const int32_t perSession = perChange * 60;
  Serial.printf("[storm] projected over a 60-change session: %+d bytes\n",
                perSession);

  if (-perChange <= kPassBytesPerChange) {
    Serial.println("[storm] verdict: PASS - no meaningful per-change loss");
  } else if (-perChange <= kMarginalBytesPerChange) {
    Serial.println(
        "[storm] verdict: MARGINAL - survives a session, but see Architecture "
        "9.1 rung 1 (stream buffer size)");
  } else {
    Serial.println(
        "[storm] verdict: FAIL - work down the Architecture 9.1 ladder before "
        "M3");
  }
}

void finish() {
  phase = Phase::Idle;
  printSummary();
}

// Called once per completed change, after the dwell.
void recordChange() {
  completedChanges++;
  lastLargestBlock = largestFreeBlock();

  if (firstLargestBlock == 0) {
    firstLargestBlock = lastLargestBlock;
    minLargestBlock = lastLargestBlock;
  } else if (lastLargestBlock < minLargestBlock) {
    minLargestBlock = lastLargestBlock;
  }

  Serial.printf(
      "[storm] %u/%u %s connect=%ums heap=%u largest=%u delta=%+d drops=%u\n",
      completedChanges, targetChanges, kTestStations[stationIndex].name,
      connectMs, ESP.getFreeHeap(), lastLargestBlock,
      (int32_t)lastLargestBlock - (int32_t)firstLargestBlock,
      AudioEngine::streamDrops() - dropsAtStart);

  if (completedChanges >= targetChanges) {
    finish();
    return;
  }

  beginChange();
}

}  // namespace

namespace SwitchStorm {

void start(uint16_t changes) {
  if (changes == 0) {
    return;
  }

  targetChanges = changes;
  completedChanges = 0;
  connectFailures = 0;
  firstLargestBlock = 0;
  lastLargestBlock = 0;
  minLargestBlock = 0;
  dropsAtStart = AudioEngine::streamDrops();

  Serial.printf(
      "[storm] starting %u station changes across %u stations, %u ms dwell\n",
      changes, (unsigned)kTestStationCount, kDwellMs);

  beginChange();
}

void stop() {
  if (phase == Phase::Idle) {
    return;
  }

  Serial.println("[storm] stopped early");
  finish();
}

bool isRunning() { return phase != Phase::Idle; }

void update() {
  switch (phase) {
    case Phase::Idle:
      break;

    case Phase::Connecting:
      if (AudioEngine::connectAttempts() != attemptsAtRequest &&
          AudioEngine::state() == AudioEngine::State::Playing) {
        connectMs = millis() - phaseStartedAt;
        phase = Phase::Dwelling;
        phaseStartedAt = millis();
        break;
      }

      if (millis() - phaseStartedAt >= kConnectTimeoutMs) {
        connectFailures++;
        Serial.printf("[storm] %s did not start within %u ms - skipping\n",
                      kTestStations[stationIndex].name, kConnectTimeoutMs);
        // Counted as a completed change even though it failed: the TLS setup
        // still happened and still cost heap, which is what is being measured.
        connectMs = kConnectTimeoutMs;
        recordChange();
      }
      break;

    case Phase::Dwelling:
      if (millis() - phaseStartedAt >= kDwellMs) {
        recordChange();
      }
      break;
  }
}

}  // namespace SwitchStorm
