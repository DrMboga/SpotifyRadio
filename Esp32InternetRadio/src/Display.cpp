#include "Display.h"

#include <Arduino.h>
#include <LittleFS.h>
#include <TFT_eSPI.h>

#include "Font5x7.h"
#include "Log.h"
#include "Pins.h"

namespace {

// --- Layout (Architecture.md §6) --------------------------------------------
// Every number here is carried over from RadioApp.Hardware/DisplayManager.cs.
// They are not round numbers and they are not meant to be improved: the point
// is that the radio in the cabinet looks the same as it did on the Pi.

constexpr int16_t kScreenWidth = 160;
constexpr int16_t kScreenHeight = 128;

constexpr uint16_t kBackgroundColor = 0x0000;  // black
constexpr uint16_t kFrequencyColor = 0xFFFF;   // white
constexpr uint16_t kStationNameColor = 0x8DF7;
constexpr uint16_t kSongTitleColor = 0xFFFF;

constexpr int16_t kFrequencyX = 100;
constexpr int16_t kFrequencyY = 2;

constexpr int16_t kLogoSize = 92;
constexpr int16_t kLogoY = 13;

// Not (160 - 92) / 2 = 34. DisplayManager.DrawImage centres against
// ScreenGpioParameters.DisplayWidth, which is 0x9F = 159 — the *last column*,
// not the width — so the Pi's logo sits at 33 and is one pixel left of centre.
// §6 records 33 for the same reason. Reproducing the off-by-one is the whole
// job here; "fixing" it would move the logo relative to four YouTube videos.
constexpr int16_t kLogoX = 33;

constexpr int16_t kStationNameX = 21;
constexpr int16_t kStationNameY = 107;

constexpr int16_t kSongTitleX = 3;
constexpr int16_t kSongTitleY = 117;
constexpr int16_t kSongTitleHeight = kScreenHeight - kSongTitleY;  // 11 rows

// TFT_eSPI is configured from platformio.ini (D9), which means the pin numbers
// exist twice — there as preprocessor macros the library reads, here as the
// constexprs everything else in the firmware uses. These asserts are what stops
// the two copies drifting: change one without the other and the build fails
// instead of the screen quietly staying dark.
static_assert(TFT_MOSI == kPinTftMosi, "TFT_MOSI != Pins.h kPinTftMosi");
static_assert(TFT_SCLK == kPinTftSck, "TFT_SCLK != Pins.h kPinTftSck");
static_assert(TFT_CS == kPinTftCs, "TFT_CS != Pins.h kPinTftCs");
static_assert(TFT_DC == kPinTftDc, "TFT_DC != Pins.h kPinTftDc");
static_assert(TFT_RST == kPinTftReset, "TFT_RST != Pins.h kPinTftReset");

TFT_eSPI tft;

// The logo is moved in horizontal bands: 23 rows is 92/4, so the image is
// exactly four reads and four pushes with no remainder to special-case, for a
// 4,232-byte static buffer. Whole-image would be 16,928 bytes — the very
// allocation §7.2 rules out — while this is small enough to sit in .bss
// nowhere near the boot-time limit PNGdec's 45 KB broke.
//
// Banding was expected to be the whole story and is not. A row at a time was
// the first version and cost 133–173 ms; four bands cost 67–174 ms for the
// same images, so collapsing 92 SPI address windows into 4 bought perhaps
// 15 %. It is kept because it is free, not because it is the fix.
//
// **What actually sets the time is contention with the audio task**, and the
// spread says so plainly. The same 16,928 bytes took 67 ms while core 1 sat
// on a TLS handshake (blocked on the network, not the flash), 133 ms while it
// decoded a steady MP3, and 174 ms during a station change with both
// happening at once. Both cores fetch code through one flash cache, and the
// MP3 decoder is not a small working set — so a LittleFS read is competing
// with it for the cache rather than for CPU. The pixels themselves are ~5 ms
// at 27 MHz; everything above that is waiting for flash.
//
// That makes this slower in wall time than PNGdec was (93–108 ms), which is
// worth stating plainly rather than filing under "faster because simpler".
// It buys the thing that matters instead: no allocation, so it works at all.
// It is core-0 work against a ~1.7 s stream buffer, so D14 says it should be
// inaudible — and D14 is a claim about this exact case, so it is the ear test
// at the end of M4 that settles it, not this comment.
constexpr int16_t kLogoRowsPerBand = 23;
static_assert(kLogoSize % kLogoRowsPerBand == 0,
              "band height must divide the logo height exactly");

uint16_t logoBand[kLogoSize * kLogoRowsPerBand];

// A `.565` file is exactly this many bytes and carries no header, so its size
// is the only thing there is to check — which is why it is checked rather than
// assumed. The payload is uploaded separately from the firmware, and a short
// file would otherwise be drawn as a torn image with nothing said about it.
constexpr size_t kLogoBytes = (size_t)kLogoSize * kLogoSize * 2;

uint32_t logoDrawMs = 0;
bool logoDrawOk = false;

// --- Text ------------------------------------------------------------------

// One character, background included, as a single 5x7 push. DisplayManager
// drew it the same way — the glyph's off pixels are painted in the background
// colour rather than skipped, which is what lets text redraw over itself
// without blanking first.
void drawChar(int16_t x, int16_t y, char c, uint16_t color) {
  const uint8_t* rows = Font5x7::glyph(c);

  uint16_t block[Font5x7::kWidth * Font5x7::kHeight];
  size_t index = 0;

  for (uint8_t row = 0; row < Font5x7::kHeight; row++) {
    for (uint8_t column = 0; column < Font5x7::kWidth; column++) {
      // Bit 0 is the leftmost pixel; see the note in Font5x7.cpp.
      const bool lit = (rows[row] & (1 << column)) != 0;
      block[index++] = lit ? color : kBackgroundColor;
    }
  }

  tft.pushImage(x, y, Font5x7::kWidth, Font5x7::kHeight, block);
}

// Draws until the string ends or the right edge is reached. The cut-off is not
// a nicety: ten station names are longer than the 23 characters that fit at
// x=21, and §6 accepts clipping them rather than scrolling or shrinking.
//
// The bound matches DisplayManager.DrawText exactly — it advances first and
// stops once x is past the last column, so a character starting at 159 is
// drawn and clipped by the panel rather than dropped.
void drawText(int16_t x, int16_t y, const char* text, uint16_t color) {
  if (text == nullptr) {
    return;
  }

  for (const char* cursor = text; *cursor != '\0'; cursor++) {
    drawChar(x, y, *cursor, color);

    x += Font5x7::kAdvance;
    if (x > kScreenWidth - 1) {
      return;
    }
  }
}

// "L 92 MHz" — bank letter included, as InternetRadioPlayerProcessor.Reset()
// built it. The §6 sketch shows the frequency alone, but the same frequency
// exists on four banks and the Pi always said which one, so this follows the
// code rather than the sketch. Nine characters at x=100 ends at 154.
void drawFrequency(char bank, uint8_t frequency) {
  char text[16];
  snprintf(text, sizeof(text), "%c %u MHz", bank, (unsigned)frequency);
  drawText(kFrequencyX, kFrequencyY, text, kFrequencyColor);
}

// --- Logo -------------------------------------------------------------------
//
// There is no image decoder on this board, and that is an M4 measurement rather
// than a shortcut. PNGdec needs a 45,604-byte contiguous allocation for zlib's
// sliding window, and the ESP32 only ever has one at boot: with an HTTPS stream
// live the largest free block measures 16,372–19,444, and even with the stream
// stopped it is 38,900. Decoding on the device therefore worked at boot and
// nowhere else, and holding the decoder for the whole session instead made
// every TLS handshake fail with `BIGNUM - Memory allocation failed`.
//
// So `Tools/StationMining/build-data.mjs` does the decoding, once, on a laptop.
// A `.565` file is 92x92 little-endian RGB565 pixels and nothing else, which
// makes this a file read and four pushes: no allocation, no library, and
// nothing that can fail for want of memory. The price is flash — see D5 and
// §7.2.
//
// Still core 0 only.
bool drawLogo(const char* path) {
  const uint32_t startedAt = millis();
  logoDrawOk = false;

  File file = LittleFS.open(path, "r");

  if (!file) {
    Log::printf("[tft] logo %s: not on the filesystem\n", path);
    logoDrawMs = millis() - startedAt;
    return false;
  }

  if (file.size() != kLogoBytes) {
    Log::printf("[tft] logo %s: %u bytes, expected %u - skipped\n", path,
                (unsigned)file.size(), (unsigned)kLogoBytes);
    file.close();
    logoDrawMs = millis() - startedAt;
    return false;
  }

  for (int16_t row = 0; row < kLogoSize; row += kLogoRowsPerBand) {
    if (file.read((uint8_t*)logoBand, sizeof(logoBand)) !=
        (int)sizeof(logoBand)) {
      // The size check above makes this unreachable on a healthy filesystem,
      // which is precisely why it is handled: reaching it means LittleFS is
      // returning short reads, and part of a logo on screen is a confusing way
      // to find that out.
      Log::printf("[tft] logo %s: short read at row %d\n", path, (int)row);
      file.close();
      logoDrawMs = millis() - startedAt;
      return false;
    }

    tft.pushImage(kLogoX, kLogoY + row, kLogoSize, kLogoRowsPerBand, logoBand);
  }

  file.close();
  logoDrawMs = millis() - startedAt;
  logoDrawOk = true;
  return true;
}

}  // namespace

namespace Display {

void begin() {
  tft.init();

  // Rotation 1 on a black-tab ST7735 is MADCTL 0xA0 with no column or row
  // offset — the exact register write DisplayManager.Handle(InitDisplay) made.
  tft.setRotation(1);

  // The `.565` files and every colour constant in this file are ordinary
  // little-endian RGB565; TFT_eSPI ships with the opposite assumption.
  tft.setSwapBytes(true);

  tft.fillScreen(kBackgroundColor);

  Log::printf("[tft] ST7735 %dx%d, %u-byte logo band buffer, no decoder\n",
              tft.width(), tft.height(), (unsigned)sizeof(logoBand));
}

void showStation(char bank, uint8_t frequency, const char* name,
                 const char* logoPath) {
  // Clear, frequency, logo, name — the order Reset() then Play() produced on
  // the Pi. It matters only in that the screen goes black before the logo is
  // drawn rather than holding the previous station's behind the new name.
  tft.fillScreen(kBackgroundColor);
  drawFrequency(bank, frequency);

  if (logoPath != nullptr) {
    drawLogo(logoPath);
  }

  drawText(kStationNameX, kStationNameY, name, kStationNameColor);
}

void showFrequencyOnly(char bank, uint8_t frequency) {
  tft.fillScreen(kBackgroundColor);
  drawFrequency(bank, frequency);
}

void showBenchStation(const char* name) {
  tft.fillScreen(kBackgroundColor);
  drawText(kStationNameX, kStationNameY, name, kStationNameColor);
}

void showSongTitle(const char* title) {
  // Blank the strip and redraw it, rather than overwriting in place: the new
  // title is usually shorter than the old one, and the tail of the old one
  // would survive. This is CleanRadioSongInfoNotification, which the Pi version
  // published before every title change for the same reason.
  tft.fillRect(0, kSongTitleY, kScreenWidth, kSongTitleHeight,
               kBackgroundColor);

  if (title == nullptr || title[0] == '\0') {
    return;
  }

  drawText(kSongTitleX, kSongTitleY, title, kSongTitleColor);
}

uint32_t lastLogoDrawMs() { return logoDrawMs; }

bool lastLogoDrawOk() { return logoDrawOk; }

}  // namespace Display
