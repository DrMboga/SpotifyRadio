#!/usr/bin/env python3
"""Regenerate Esp32InternetRadio/src/Font5x7.cpp from the Pi version's font.

Architecture.md §6 requires the ESP32 screen to use the same 5x7 bitmap the
Pi build used, so that text metrics — and therefore the "23 characters fit at
x=21" rule the station data is validated against — stay identical. Retyping 95
glyphs by hand would be 665 opportunities to introduce a difference nobody
would notice until a letter looked wrong on a 128-pixel screen, so the table is
transcribed mechanically instead.

    python Tools/gen-font5x7.py

Reads   RadioApp/RadioApp.Hardware/Helpers/Font5x7.cs   (frozen tree, read-only)
Writes  Esp32InternetRadio/src/Font5x7.cpp

Only ASCII 0x20..0x7E is emitted. The C# dictionary also holds German umlauts
and a full Cyrillic set; §6 makes ASCII a rule about the station data, and
Font5x7::glyph() answers '?' for everything else.
"""

import io
import os
import re
import sys

BACKSLASH = chr(92)

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(REPO_ROOT, 'RadioApp', 'RadioApp.Hardware', 'Helpers',
                      'Font5x7.cs')
TARGET = os.path.join(REPO_ROOT, 'Esp32InternetRadio', 'src', 'Font5x7.cpp')

FIRST_CHAR = 0x20
LAST_CHAR = 0x7E

# { 'A', [0x04, 0x0A, ...] } — the char literal may be an escape, so '\'' and
# '\\' have to survive the match.
ENTRY = re.compile(r"\{\s*'((?:" + BACKSLASH + BACKSLASH + r".)|[^'])'\s*,"
                   r"\s*\[([^\]]*)\]\s*\}")

HEADER = '''#include "Font5x7.h"

// Generated from RadioApp.Hardware/Helpers/Font5x7.cs by Tools/gen-font5x7.py.
// Transcribed rather than retyped so the metrics cannot drift (Architecture.md
// §6). Regenerate rather than edit.
//
// One row per glyph line, bit 0 = leftmost pixel. That bit order is not
// cosmetic: DisplayManager.DrawChar walks `bitmap[line] & (1 << i)` for i=0..4
// while streaming pixels left to right, so bit 0 is column 0. Reading the table
// MSB-first mirrors every glyph - "(" comes out as ")" - and the asymmetric
// characters are the only place you would notice.
//
// ASCII 0x20..0x7E only. The C# dictionary also carries German umlauts and a
// full Cyrillic set; both are deliberately dropped, because §6 makes ASCII a
// rule about the *data* and build-data.mjs enforces it on station names. An ICY
// title arriving off a stream is not under that rule, so glyph() answers "?"
// for anything outside the range - including every byte of a UTF-8 sequence.

namespace {

const uint8_t kGlyphs[Font5x7::kGlyphCount][Font5x7::kHeight] = {
'''

FOOTER = '''};

}  // namespace

namespace Font5x7 {

const uint8_t* glyph(char c) {
  const uint8_t code = (uint8_t)c;

  if (code < kFirstChar || code > kLastChar) {
    return kGlyphs['?' - kFirstChar];
  }
  return kGlyphs[code - kFirstChar];
}

}  // namespace Font5x7
'''


def parse_font(text):
    font = {}

    for match in ENTRY.finditer(text):
        char = match.group(1)
        if char == BACKSLASH + "'":
            char = "'"
        elif char == BACKSLASH + BACKSLASH:
            char = BACKSLASH

        rows = [int(value.strip(), 16) for value in match.group(2).split(',')]
        if len(rows) != 7:
            raise ValueError('glyph %r has %d rows, expected 7'
                             % (char, len(rows)))
        if any(row > 0x1F for row in rows):
            raise ValueError('glyph %r has a row wider than 5 pixels' % char)

        font[char] = rows

    return font


def main():
    font = parse_font(io.open(SOURCE, encoding='utf-8-sig').read())

    missing = [chr(c) for c in range(FIRST_CHAR, LAST_CHAR + 1)
               if chr(c) not in font]
    if missing:
        sys.exit('Font5x7.cs is missing printable ASCII: %r' % missing)

    lines = []
    for code in range(FIRST_CHAR, LAST_CHAR + 1):
        char = chr(code)
        label = {' ': 'space', BACKSLASH: 'backslash'}.get(char, char)
        rows = ', '.join('0x%02X' % row for row in font[char])
        lines.append('    {%s},  // 0x%02X  %s' % (rows, code, label))

    io.open(TARGET, 'w', encoding='utf-8', newline='\n').write(
        HEADER + '\n'.join(lines) + '\n' + FOOTER)

    dropped = len([c for c in font if ord(c) > LAST_CHAR])
    print('%s: %d ASCII glyphs, %d non-ASCII glyphs dropped'
          % (os.path.relpath(TARGET, REPO_ROOT), len(lines), dropped))


if __name__ == '__main__':
    main()
