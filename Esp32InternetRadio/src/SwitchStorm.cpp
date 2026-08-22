#include "SwitchStorm.h"

#include <Arduino.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "TestStations.h"

namespace {

// Long enough that the connection is fully established and the input ring
// buffer has actually filled — a change that is torn down before the buffer
// allocates would not exercise the thing being measured. The dwell is timed
// from when the stream is *playing*, not from when it was requested, so a slow
// TLS handshake does not eat into it.
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

// Heap is measured per station, so the table needs a fixed bound.
constexpr size_t kMaxTrackedStations = 8;

enum class Phase : uint8_t { Idle, Connecting, Dwelling };

Phase phase = Phase::Idle;

uint16_t targetChanges = 0;
uint16_t completedChanges = 0;
uint16_t connectFailures = 0;
size_t stationIndex = 0;
size_t trackedStations = 0;

uint32_t phaseStartedAt = 0;
uint32_t connectMs = 0;

// Snapshot of AudioEngine::connectAttempts() taken when the change was
// requested. playUrl() only *queues* the change, so for a few milliseconds
// afterwards the engine is still reporting the previous station as Playing.
// Waiting for the attempt counter to move as well is what stops the storm
// timing a connect it never made.
uint32_t attemptsAtRequest = 0;

// Per-station heap baselines. A single global baseline is worthless here: the
// four bench stations sit up to 43 KB apart from each other simply because
// ELDORADIO is plain HTTP and has no ~40 KB TLS session to pay for, so a
// global delta swings +43000/-3000 from one change to the next and says
// nothing about whether anything leaked. A leak only shows up by comparing a
// station against *itself* on an earlier visit.
uint32_t firstLargest[kMaxTrackedStations];
uint32_t lastLargest[kMaxTrackedStations];
uint32_t minLargest[kMaxTrackedStations];
uint16_t samples[kMaxTrackedStations];

uint32_t largestFreeBlock() {
  return heap_caps_get_largest_free_block(MALLOC_CAP_8BIT | MALLOC_CAP_INTERNAL);
}

void beginChange() {
  stationIndex = completedChanges % trackedStations;

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

  int32_t totalNet = 0;
  uint16_t stationsCompared = 0;

  for (size_t i = 0; i < trackedStations; i++) {
    if (samples[i] == 0) {
      continue;
    }

    const int32_t net = (int32_t)lastLargest[i] - (int32_t)firstLargest[i];
    Serial.printf("[storm]   %-20s visits=%u first=%u last=%u min=%u net=%+d\n",
                  kTestStations[i].name, samples[i], firstLargest[i],
                  lastLargest[i], minLargest[i], net);

    if (samples[i] >= 2) {
      totalNet += net;
      stationsCompared++;
    }
  }

  if (stationsCompared == 0) {
    Serial.println("[storm] not enough repeat visits to compare - run more changes");
    return;
  }

  // The first visit to each station only establishes its baseline, so the
  // changes that can actually show a loss are the ones after that.
  const int32_t measuredChanges =
      (int32_t)completedChanges - (int32_t)stationsCompared;
  if (measuredChanges <= 0) {
    Serial.println("[storm] not enough repeat visits to compare - run more changes");
    return;
  }

  const int32_t perChange = totalNet / measuredChanges;

  Serial.printf("[storm] net %+d bytes across %d measured changes = %+d bytes/change\n",
                totalNet, measuredChanges, perChange);
  Serial.printf("[storm] stream drops during storm: %u\n", AudioEngine::streamDrops());

  // The number the milestone turns on, converted into the unit that actually
  // matters: what a single evening of listening costs.
  Serial.printf("[storm] projected over a 60-change session: %+d bytes\n",
                perChange * 60);

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
  const uint32_t largest = largestFreeBlock();
  const size_t i = stationIndex;

  if (samples[i] == 0) {
    firstLargest[i] = largest;
    minLargest[i] = largest;
  } else if (largest < minLargest[i]) {
    minLargest[i] = largest;
  }
  lastLargest[i] = largest;
  samples[i]++;

  completedChanges++;

  Serial.printf(
      "[storm] %u/%u %s connect=%ums heap=%u largest=%u vs-own-first=%+d drops=%u\n",
      completedChanges, targetChanges, kTestStations[i].name, connectMs,
      ESP.getFreeHeap(), largest, (int32_t)largest - (int32_t)firstLargest[i],
      AudioEngine::streamDrops());

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

  trackedStations = kTestStationCount < kMaxTrackedStations ? kTestStationCount
                                                            : kMaxTrackedStations;

  targetChanges = changes;
  completedChanges = 0;
  connectFailures = 0;

  for (size_t i = 0; i < kMaxTrackedStations; i++) {
    firstLargest[i] = 0;
    lastLargest[i] = 0;
    minLargest[i] = 0;
    samples[i] = 0;
  }

  Serial.printf(
      "[storm] starting %u station changes across %u stations, %u ms dwell\n",
      changes, (unsigned)trackedStations, kDwellMs);
  Serial.println("[storm] playback keys are ignored until it finishes - press x to abort");

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
        // The dwell is timed from here, so every station gets the same 8 s of
        // steady-state playing regardless of how long its handshake took.
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
