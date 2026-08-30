#pragma once

#include <stddef.h>
#include <stdint.h>

// The ST7735 screen (Architecture.md §6, D9, D10).
//
// The layout is a port of the Pi version's, pixel for pixel, so the radio looks
// unchanged in the cabinet: frequency top right, 92x92 logo centred, station
// name, and an ICY song title on the bottom line that is blanked and redrawn
// independently of everything above it.
//
// **Everything in here runs on core 0** (D14). Decoding a logo takes long
// enough to underrun the stream if it ever ran on the audio core, so no
// function below may be called from an ESP32-audioI2S callback — those execute
// on core 1. The ICY title reaches this side through
// AudioEngine::takeStreamTitle() and is drawn from loop() like everything else.
namespace Display {

// Initialises the panel and paints it black. Call once from setup(), before
// WiFi: a screen that stays dark is the first symptom of miswiring, and it
// should be visible before the 30-second WiFi timeout rather than after it.
void begin();

// A full station change: black screen, then frequency, logo and name (§6).
//
// `logoPath` is a LittleFS path as StationCatalogue::logoPath() builds it, or
// nullptr for a station with no logo. It points at a `.565` file — 92x92
// little-endian RGB565 pixels, no header — produced by build-data.mjs (D5, as
// revised at M4: there is no image decoder on the board). A logo that is
// missing or the wrong size leaves that part of the screen black and is
// reported on the serial log; the rest of the layout still draws, because a
// missing picture is not a reason to lose the station name.
void showStation(char bank, uint8_t frequency, const char* name,
                 const char* logoPath);

// Rewrites the frequency in the top-right corner and touches nothing else.
//
// For the one case where the dial moved but the station did not: sixteen
// catalogue entries span two dial positions, so 102 → 103 is a new frequency on
// the same stream. Repainting the whole layout for that would blank the screen
// for ~110 ms and redraw an identical logo, which is a visible flash in
// exchange for nothing.
void updateFrequency(char bank, uint8_t frequency);

// Empty slot, and paused: black screen with the frequency and nothing else
// (§6). Both mean the same thing on screen because both mean silence.
void showFrequencyOnly(char bank, uint8_t frequency);

// The bench stations (TestStations.h) have no dial position and no logo, so
// they get the name line alone. This exists so the M2 switch storm exercises a
// screen redraw per change now that there is a screen — the heap cost of one
// station change is the number §7.3 cares about, and from M4 on that cost
// includes drawing.
void showBenchStation(const char* name);

// The bottom line, blanked and redrawn on its own (§6, the Pi version's
// CleanRadioSongInfoNotification). Passing nullptr or "" just clears it, which
// is what a station with no ICY metadata should look like.
void showSongTitle(const char* title);

// How long the last logo took to draw, in milliseconds, and whether it worked.
// Both are for the serial `h` line: this is the operation D14 predicts would
// break the audio if it ran on the wrong core, so its cost is worth watching
// next to the heap numbers rather than being invisible. It is also the number
// that shows what dropping the PNG decoder bought — M4 measured 93–108 ms
// decoding PNG against a few ms reading `.565`.
uint32_t lastLogoDrawMs();
bool lastLogoDrawOk();

}  // namespace Display
