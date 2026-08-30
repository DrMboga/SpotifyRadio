#include "RadioController.h"

#include <Arduino.h>
#include <ctype.h>
#include <string.h>

#include "AudioEngine.h"
#include "Display.h"
#include "Log.h"
#include "StationCatalogue.h"

namespace {

// How long the dial has to hold still before the reading is acted on.
//
// Two settles, because the bench showed one number cannot do both jobs.
//
// The Pico samples the tuning capacitor every ~610 ms (§4), and it only reports
// when that sample crosses into a new dial position. So the gap between two
// messages *during a single continuous turn* is not one sample — it is n of
// them, and n depends on how far apart the thresholds are at that part of the
// scale. Measured over a real sweep, the gaps land on 0.61, 1.22 and 1.84 s and
// nothing in between; a dial that has actually stopped goes quiet for 3 s or
// more. The two populations are cleanly separated, which is what makes this
// work at all.
//
// A single settle of 1 s therefore split one sweep of the dial into **five**
// station changes, because it expired inside every 1.22 and 1.84 s gap. The
// answer is not simply a longer settle: 2 s applied to a single deliberate
// click of the knob is sluggish on a radio whose whole point is that it feels
// like a radio.
//
// So: **1 s after a reading that stands alone, 2 s once the dial is clearly
// being swept** — a second reading arriving within kSweepWindowMs of the last
// one is what "clearly" means. 2 s covers the 1.84 s worst case, so a whole
// sweep coalesces into one station change, while a click-and-stop still acts in
// a second.
constexpr uint32_t kFrequencySettleMs = 1000;
constexpr uint32_t kSweepSettleMs = 2000;
constexpr uint32_t kSweepWindowMs = 2000;

// What the controls say.
bool haveDesired = false;
char desiredBank = '\0';
uint8_t desiredFrequency = 0;
bool desiredPaused = false;

// What is on screen and in the audio task.
bool haveApplied = false;
char appliedBank = '\0';
uint8_t appliedFrequency = 0;
bool appliedPaused = false;

// Absolute deadline, compared with a signed difference so millis() rollover is
// not a special case.
uint32_t applyAt = 0;

uint32_t frequencyUpdateCount = 0;
uint32_t changeCount = 0;
uint32_t reusedStreamCount = 0;

// When the last NewFrequency arrived, and whether one ever has. Together they
// answer the only question the two settles need: is the dial mid-turn?
bool haveFrequencyMessage = false;
uint32_t lastFrequencyMessageAt = 0;

// --- Chatter: a dial parked on a capacitance threshold -----------------------
//
// No settle time can fix this one, which is why it needs its own mechanism. A
// knob left sitting on the boundary between two positions reports 100, 101,
// 100, 101 for as long as it is left there — and unlike the tight 0.6 s pairs
// the first bench test produced, the excursions can be 6-8 s apart. Any settle
// short enough to feel responsive expires inside one of those gaps, on whichever
// value happened to arrive last. Measured at M5: **nine station changes in 73
// seconds with nobody touching the radio.**
//
// The tell is the *shape* of the sequence, not its timing. Chatter alternates
// between two adjacent positions — X, Y, X — while a real turn is monotonic.
// Two readings of history is all it takes to tell them apart.
//
// Once spotted, the two positions are treated as one place: the value being
// reported when it was spotted is applied, and further readings inside the pair
// are ignored until the dial goes somewhere else. The cost is that nudging
// deliberately between exactly those two positions does nothing — but they are
// the two the hardware cannot distinguish anyway, and turning past either one
// releases it immediately.
constexpr uint8_t kChatterSpan = 1;  // adjacent positions only

bool haveLastReported = false;
bool havePreviousReported = false;
uint8_t lastReported = 0;
uint8_t previousReported = 0;

bool chattering = false;
uint8_t chatterLow = 0;
uint8_t chatterHigh = 0;
uint32_t chatterIgnoredCount = 0;

void rememberReported(uint8_t frequency) {
  previousReported = lastReported;
  havePreviousReported = haveLastReported;
  lastReported = frequency;
  haveLastReported = true;
}

void clearChatter() {
  chattering = false;
  haveLastReported = false;
  havePreviousReported = false;
}

// The URL the audio task was last asked to play, or nullptr if it was last
// asked to stop. It points into the catalogue blob, which outlives everything
// (§5), so it is safe to hold — and it is the only way to know that two
// different dial positions are the same station.
const char* playingUrl = nullptr;

bool pending() {
  if (!haveDesired) {
    return false;
  }

  return !haveApplied || desiredBank != appliedBank ||
         desiredFrequency != appliedFrequency || desiredPaused != appliedPaused;
}

// Draws a slot and nothing else — no audio, no reconnect.
//
// Kept separate for the reason M4 gave it: a TLS handshake and a logo read both
// cost heap, and a measurement that always runs them together cannot say which
// of the two moved the largest free block. The console's `r` is this function.
void drawSlot(char bank, uint8_t frequency,
              const StationCatalogue::Station* station) {
  if (station == nullptr) {
    Display::showFrequencyOnly(bank, frequency);
    return;
  }

  // 64 bytes holds "/logos/" plus the longest `.565` filename the build script
  // emits with room to spare; logoPath() reports the overflow it cannot produce
  // rather than truncating into an open() that fails obscurely.
  char logoPath[64];
  const bool haveLogo =
      StationCatalogue::logoPath(*station, logoPath, sizeof(logoPath));

  Display::showStation(bank, frequency, station->name,
                       haveLogo ? logoPath : nullptr);
}

// The Pi's Reset(): stop, clear, show the frequency, and play only if the
// radio is meant to be playing. Everything the controls can do ends up here,
// which is why there is exactly one place that talks to AudioEngine.
void apply() {
  appliedBank = desiredBank;
  appliedFrequency = desiredFrequency;
  appliedPaused = desiredPaused;
  haveApplied = true;
  changeCount++;

  // No bank latched, or Phono. Phono is a real button on the cabinet and it
  // means "not the radio", so silence is the correct answer rather than a
  // default station — and the screen says so by having nothing on it.
  if (appliedBank == '\0') {
    Log::println("[radio] no bank selected (Phono, or no button latched)");
    AudioEngine::stop();
    playingUrl = nullptr;
    Display::showBenchStation("");
    return;
  }

  if (appliedPaused) {
    Log::printf("[radio] %c %u paused\n", appliedBank,
                (unsigned)appliedFrequency);
    AudioEngine::stop();
    playingUrl = nullptr;
    // §6: a pause is a disconnect, and it looks the same as an empty slot —
    // frequency on black.
    Display::showFrequencyOnly(appliedBank, appliedFrequency);
    return;
  }

  const StationCatalogue::Station* station =
      StationCatalogue::find(appliedBank, appliedFrequency);

  // An empty slot is not an error (§6): it means silence. That is the
  // behaviour the real dial needs — 76 positions, and turning past an unfilled
  // one has to go quiet rather than keep the previous station playing.
  if (station == nullptr) {
    Log::printf("[radio] %c %u is empty\n", appliedBank,
                (unsigned)appliedFrequency);
    AudioEngine::stop();
    playingUrl = nullptr;
    drawSlot(appliedBank, appliedFrequency, nullptr);
    return;
  }

  // Sixteen entries in the shipped catalogue span two dial positions — the
  // `100-101` and `102-103` rows in RadioStationsList.md — so one click of the
  // dial lands on the same station about a fifth of the time. Restarting the
  // stream for that is a TLS teardown and re-handshake, a second of silence and
  // a fresh 40 KB allocation, all to arrive back where it started. Only the
  // frequency in the corner actually changed, so only that is redrawn: the
  // logo and the name are already the right ones, and the song title on the
  // bottom line belongs to a stream that never stopped.
  if (playingUrl != nullptr && strcmp(playingUrl, station->url) == 0) {
    reusedStreamCount++;
    Log::printf("[radio] %c %u  %s (same stream, left playing)\n", appliedBank,
                (unsigned)appliedFrequency, station->name);
    Display::updateFrequency(appliedBank, appliedFrequency);
    return;
  }

  Log::printf("[radio] %c %u  %s\n", appliedBank, (unsigned)appliedFrequency,
              station->name);

  // Draw before queueing the stream, not after. playUrl() returns straight
  // away but the audio task then sits on a TLS handshake for well over a
  // second, and a screen that only changes once the sound starts makes the
  // radio look like it ignored the dial.
  drawSlot(appliedBank, appliedFrequency, station);

  // playUrl() only fails on an over-long URL or a full queue. The catalogue
  // rejected the first at boot, so this means commands are arriving faster than
  // the audio task retires them — which the settle timer above exists to stop,
  // so seeing it is a sign the debounce is not doing its job.
  if (AudioEngine::playUrl(station->url)) {
    playingUrl = station->url;
  } else {
    playingUrl = nullptr;
    Log::println("[radio] command queue full - the change was dropped");
  }
}

void scheduleApply(uint32_t settleMs) {
  haveDesired = true;
  applyAt = millis() + settleMs;
}

}  // namespace

namespace RadioController {

void begin() {
  haveDesired = false;
  haveApplied = false;
}

void setBank(char bank) {
  const char upper = bank == '\0' ? '\0' : (char)toupper((unsigned char)bank);

  if (haveDesired && upper == desiredBank) {
    return;
  }

  desiredBank = upper;
  scheduleApply(0);
}

void setFrequency(uint8_t frequency) {
  const uint32_t now = millis();

  frequencyUpdateCount++;

  // Already known to be sitting on a boundary. The two positions are one place
  // until the dial leaves them.
  if (chattering) {
    if (frequency == chatterLow || frequency == chatterHigh) {
      chatterIgnoredCount++;
      rememberReported(frequency);
      haveFrequencyMessage = true;
      lastFrequencyMessageAt = now;
      return;
    }

    Log::printf("[radio] dial left the %u/%u boundary\n",
                (unsigned)chatterLow, (unsigned)chatterHigh);
    chattering = false;
  }

  // A reading that follows hard on the last one means the knob is still moving,
  // so wait out the long gap a continuous turn can contain rather than acting
  // inside it. See the settle constants at the top.
  const bool midTurn =
      haveFrequencyMessage && (now - lastFrequencyMessageAt) <= kSweepWindowMs;

  // X, Y, X across two adjacent positions is the boundary signature. A real
  // turn does not double back on itself one position at a time.
  const int step = (int)lastReported - (int)frequency;

  if (havePreviousReported && frequency == previousReported &&
      (step == kChatterSpan || step == -kChatterSpan)) {
    chattering = true;
    chatterLow = frequency < lastReported ? frequency : lastReported;
    chatterHigh = frequency < lastReported ? lastReported : frequency;
    Log::printf("[radio] dial is chattering between %u and %u - holding %u\n",
                (unsigned)chatterLow, (unsigned)chatterHigh,
                (unsigned)frequency);
  }

  rememberReported(frequency);
  haveFrequencyMessage = true;
  lastFrequencyMessageAt = now;

  // Restart the settle timer on every reading, including one that repeats the
  // current desired value: the dial passing back through a position it has
  // already reported is still the dial moving, and acting a second later is
  // exactly the point.
  //
  // This runs on the reading that *detected* chatter too, which is deliberate:
  // it is how the dial turned onto a boundary still ends up on the station the
  // user turned to, rather than frozen on the one they left.
  desiredFrequency = frequency;
  scheduleApply(midTurn ? kSweepSettleMs : kFrequencySettleMs);
}

void setPaused(bool paused) {
  if (haveDesired && paused == desiredPaused) {
    return;
  }

  desiredPaused = paused;
  scheduleApply(0);
}

void applySnapshot(char bank, uint8_t frequency, bool paused) {
  // A snapshot is an authoritative reading of where the knobs are, so any
  // boundary the dial was judged to be sitting on is stale.
  clearChatter();

  desiredBank = bank == '\0' ? '\0' : (char)toupper((unsigned char)bank);
  desiredFrequency = frequency;
  desiredPaused = paused;
  haveDesired = true;
  applyAt = millis();

  // A snapshot is applied even when it matches, because it is also the first
  // thing that puts anything on the screen at boot.
  haveApplied = false;
}

void selectNow(char bank, uint8_t frequency) {
  // Typed on the console, so it overrides whatever the dial was doing.
  clearChatter();

  desiredBank = (char)toupper((unsigned char)bank);
  desiredFrequency = frequency;
  haveDesired = true;
  applyAt = millis();

  // Re-applied even when it matches what is already playing, and the
  // same-stream shortcut in apply() is stood down for this one change. Typing
  // the same slot twice is how the bench forces a reconnect, and M2's
  // measurements depend on that still working.
  haveApplied = false;
  playingUrl = nullptr;
}

void update() {
  if (!pending()) {
    return;
  }

  if ((int32_t)(millis() - applyAt) < 0) {
    return;
  }

  apply();
}

void redraw() {
  if (!haveApplied || appliedBank == '\0') {
    Log::println("[radio] nothing tuned - no slot to redraw");
    return;
  }

  if (appliedPaused) {
    Display::showFrequencyOnly(appliedBank, appliedFrequency);
    return;
  }

  drawSlot(appliedBank, appliedFrequency,
           StationCatalogue::find(appliedBank, appliedFrequency));
}

void forgetStream() { playingUrl = nullptr; }

bool isTuned() { return haveApplied; }

char bank() { return appliedBank; }

uint8_t frequency() { return appliedFrequency; }

bool isPaused() { return appliedPaused; }

uint32_t frequencyUpdates() { return frequencyUpdateCount; }

uint32_t changesApplied() { return changeCount; }

uint32_t reusedStreams() { return reusedStreamCount; }

uint32_t chatterIgnored() { return chatterIgnoredCount; }

bool isChattering() { return chattering; }

bool isSettling() { return pending(); }

}  // namespace RadioController
