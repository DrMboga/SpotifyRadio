#pragma once

#include <stdint.h>

// What the radio is tuned to, and the one place that decides when to act on it.
//
// This is the ESP32's `RadioStatus` + `InternetRadioPlayerProcessor.Reset()`
// from the Pi version, collapsed into one module because there is only ever one
// player now (§2: no Spotify). Two things feed it — the Pico over PicoLink, and
// the serial console — and both go through the same setters so the bench and
// the cabinet cannot drift apart.
//
// It holds two copies of the same three values: what the *controls* say
// (desired) and what is *playing* (applied). They differ only for as long as
// the settle timer below runs, and closing that gap is the whole of update().
//
// Why a settle timer rather than the Pi's throttle: the dial streams
// NewFrequency the whole time it is turning (§4), and
// PlayerProcessorDebounceFrequencyService acted on the *first* of those and
// muted the next 500 ms — leading edge, so a slow spin from 87 to 105 still
// opened a stream every half second. Waiting for the reading to hold still
// instead means one spin costs exactly one connection, which is what M5 has to
// show.
//
// Core 0 only: it calls Display and AudioEngine, and AudioEngine's calls all
// return immediately (D14).
namespace RadioController {

// Nothing is tuned until a control says so. The radio deliberately comes up
// silent and dark rather than guessing at a station — §4's boot-time State
// handshake exists so that guess is never needed.
void begin();

// The toggle buttons. '\0' means no bank is selected — `buttonIndex` -1 (no
// button latched) or 0 (Phono). Applied immediately: a button press is a
// discrete event, and the Pico has already debounced it by only reporting
// changes.
void setBank(char bank);

// The tuning dial. Applied once the reading has held still for the settle time
// — see the note above.
void setFrequency(uint8_t frequency);

// The play/pause button. `true` disconnects the stream and leaves the frequency
// on screen (§6: paused and empty look the same). Applied immediately.
void setPaused(bool paused);

// A whole `State` snapshot: all three at once, applied immediately and as a
// unit. This is what makes the radio come up already tuned to wherever the
// knobs are sitting, paused included.
void applySnapshot(char bank, uint8_t frequency, bool paused);

// The serial console's slot command. Bank and frequency together with no
// settle, because a typed "M 92" is not a dial being spun.
void selectNow(char bank, uint8_t frequency);

// Call every pass of loop(). Applies a pending change once it has settled.
void update();

// Repaints the applied slot, audio untouched — the console's `r`. Separated so
// M4's measurement of the draw path alone survives into M5.
void redraw();

// Forgets what is playing, without stopping it. Call this before anything else
// drives AudioEngine directly — the M2 switch storm is the only such thing —
// so the "same station, leave the stream alone" shortcut cannot skip a
// reconnect on the strength of a URL that something else has since replaced.
void forgetStream();

// True once something has been applied, i.e. the screen shows a slot.
bool isTuned();

char bank();
uint8_t frequency();
bool isPaused();

// For the `h` line. `frequencyUpdates()` counts NewFrequency messages accepted,
// `changesApplied()` counts the station changes they turned into — the gap
// between the two is what the settle timer absorbed, which is the number M5 is
// judged on.
uint32_t frequencyUpdates();
uint32_t changesApplied();

// Slot changes that landed on the station already playing and so left the
// stream alone. Sixteen catalogue entries span two dial positions, so this is
// a normal event, not a curiosity — each one is a TLS teardown avoided.
uint32_t reusedStreams();

// Dial readings dropped because the knob is parked on the boundary between two
// positions and reporting both. A climbing count with the radio untouched is
// the mechanism working, not a fault — see the note in the .cpp.
uint32_t chatterIgnored();
bool isChattering();

// True while a change is waiting on the settle timer.
bool isSettling();

}  // namespace RadioController
