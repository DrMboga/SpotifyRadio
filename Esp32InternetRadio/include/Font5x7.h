#pragma once

#include <stdint.h>

// The Pi version's 5x7 bitmap font, ported rather than replaced (§6).
//
// A library font would have been less work and would have been wrong: the
// station data is validated against *these* metrics. "23 characters fit at
// x=21" — the rule build-data.mjs reports clipping against — is 21 + 23 * 6
// = 159, and it only holds for a 6 px advance. Swap the font and every name in
// the catalogue is re-measured against a screen nobody re-checked.
//
// The table itself is generated; see src/Font5x7.cpp and Tools/gen-font5x7.py.
namespace Font5x7 {

// A glyph is 5 px wide and drawn on a 6 px pitch, so the sixth column is the
// inter-character gap and is painted in the background colour like any other
// background pixel. DisplayManager.DrawChar did the same, which is why text
// redraws over itself cleanly with no explicit blanking.
constexpr uint8_t kWidth = 5;
constexpr uint8_t kHeight = 7;
constexpr uint8_t kAdvance = 6;

constexpr uint8_t kFirstChar = 0x20;
constexpr uint8_t kLastChar = 0x7E;
constexpr uint8_t kGlyphCount = kLastChar - kFirstChar + 1;  // 95

// Seven rows, one byte each, bit 0 = leftmost pixel. Never returns nullptr:
// anything outside the printable ASCII range draws as '?', which is how a
// UTF-8 ICY title degrades — one '?' per byte rather than a crash or a gap.
const uint8_t* glyph(char c);

}  // namespace Font5x7
