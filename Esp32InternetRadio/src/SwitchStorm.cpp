#include "SwitchStorm.h"

#include <Arduino.h>
#include <esp_heap_caps.h>

#include "AudioEngine.h"
#include "Log.h"
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

// Verdict thresholds, in bytes of drift between the first and second half of
// the run. They are expressed in whole kilobytes on purpose: the allocator
// hands out the largest free block in 1024-byte steps, so a measured run only
// ever reports values like 26612 / 27636 / 28660. A threshold finer than that
// step is measuring its own rounding — an earlier version used 32 bytes/change
// and called a dead-flat run MARGINAL.
//
// Anchored to the usage profile (§7.3): a session is ~60 changes, so losing
// under 1 KB across a whole run is noise, and 8 KB would be roughly a third of
// the stream buffer's headroom gone in one evening.
constexpr int32_t kPassDriftBytes = -1024;
constexpr int32_t kMarginalDriftBytes = -8192;

// Heap is measured per station, so the table needs a fixed bound.
constexpr size_t kMaxTrackedStations = 8;

// 60 changes over 4 stations is 15 visits each; leave room for longer runs on
// fewer stations. Costs 1 KB of static RAM, which buys a real trend instead of
// two endpoints.
constexpr size_t kMaxSamplesPerStation = 32;

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
uint32_t largestAt[kMaxTrackedStations][kMaxSamplesPerStation];
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

// Mean of samples[from, to), rounded. Integer maths only — no float on the
// audio-adjacent path, and the values are all well inside 32 bits.
int32_t meanOf(size_t station, size_t from, size_t to) {
  if (to <= from) {
    return 0;
  }

  int64_t sum = 0;
  for (size_t i = from; i < to; i++) {
    sum += largestAt[station][i];
  }
  return (int32_t)(sum / (int64_t)(to - from));
}

void printSummary() {
  Log::println("[storm] ---------------- summary ----------------");
  Log::printf("[storm] changes completed: %u of %u (%u connect failures)\n",
                completedChanges, targetChanges, connectFailures);
  Log::println("[storm] the largest free block moves in 1024-byte steps - treat anything under 1 KB as noise");
  Log::println("[storm] each station's first visit is a warm-up and is excluded (the heap is least fragmented right after boot)");

  int32_t totalDrift = 0;
  uint16_t stationsCompared = 0;

  for (size_t i = 0; i < trackedStations; i++) {
    if (samples[i] == 0) {
      continue;
    }

    uint32_t lowest = largestAt[i][0];
    for (size_t s = 0; s < samples[i]; s++) {
      if (largestAt[i][s] < lowest) {
        lowest = largestAt[i][s];
      }
    }

    // Drop the warm-up visit, then compare the two halves of what is left. A
    // half-versus-half comparison survives the 1 KB quantisation that makes any
    // single pair of endpoints unreliable.
    const size_t usable = samples[i] > 1 ? samples[i] - 1 : 0;
    const size_t mid = 1 + usable / 2;
    const int32_t firstHalf = meanOf(i, 1, mid);
    const int32_t secondHalf = meanOf(i, mid, samples[i]);
    const int32_t drift = (usable >= 2) ? (secondHalf - firstHalf) : 0;

    Log::printf(
        "[storm]   %-20s visits=%u base=%u last=%u min=%u 1st-half=%d 2nd-half=%d drift=%+d\n",
        kTestStations[i].name, samples[i], largestAt[i][usable ? 1 : 0],
        largestAt[i][samples[i] - 1], lowest, firstHalf, secondHalf, drift);

    if (usable >= 2) {
      totalDrift += drift;
      stationsCompared++;
    }
  }

  Log::printf("[storm] stream drops during storm: %u\n", AudioEngine::streamDrops());

  if (stationsCompared == 0) {
    Log::println("[storm] not enough repeat visits to compare - run more changes");
    return;
  }

  const int32_t meanDrift = totalDrift / stationsCompared;
  Log::printf("[storm] mean drift across %u stations, over %u changes: %+d bytes\n",
                stationsCompared, completedChanges, meanDrift);

  if (meanDrift >= kPassDriftBytes) {
    Log::println("[storm] verdict: PASS - no trend beyond measurement noise");
  } else if (meanDrift >= kMarginalDriftBytes) {
    Log::println(
        "[storm] verdict: MARGINAL - survives a session, but see Architecture "
        "9.1 rung 1 (stream buffer size)");
  } else {
    Log::println(
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

  if (samples[i] < kMaxSamplesPerStation) {
    largestAt[i][samples[i]] = largest;
    samples[i]++;
  }

  completedChanges++;

  // Compare against this station's warm-up-excluded baseline where one exists,
  // so the running line agrees with the summary.
  const uint32_t baseline = largestAt[i][samples[i] > 1 ? 1 : 0];

  Log::printf(
      "[storm] %u/%u %s connect=%ums heap=%u largest=%u vs-own-base=%+d drops=%u\n",
      completedChanges, targetChanges, kTestStations[i].name, connectMs,
      ESP.getFreeHeap(), largest, (int32_t)largest - (int32_t)baseline,
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
    samples[i] = 0;
  }

  Log::printf(
      "[storm] starting %u station changes across %u stations, %u ms dwell\n",
      changes, (unsigned)trackedStations, kDwellMs);
  Log::println("[storm] playback keys are ignored until it finishes - press x to abort");

  beginChange();
}

void stop() {
  if (phase == Phase::Idle) {
    return;
  }

  Log::println("[storm] stopped early");
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
        Log::printf("[storm] %s did not start within %u ms - skipping\n",
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
