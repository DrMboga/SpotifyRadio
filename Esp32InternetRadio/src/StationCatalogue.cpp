#include "StationCatalogue.h"

#include <Arduino.h>
#include <LittleFS.h>
#include <ctype.h>

#include "AudioEngine.h"
#include "Log.h"

namespace {

constexpr const char* kCsvPath = "/stations.csv";
constexpr const char* kLogoDir = "/logos";

// The generator caps URLs at AudioEngine::kMaxUrlLength and the measured
// catalogue is 6839 bytes, so a file far past this is a corrupted upload rather
// than an ambitious catalogue. Bounding the read means a truncated or garbage
// file cannot eat the heap before the audio task has even started.
constexpr size_t kMaxFileBytes = 32768;

// One allocation, made once at boot and never freed. The whole file is held and
// NUL-terminated in place, and every Station points into it — so the catalogue
// costs one ~7 KB block instead of 228 small strings. That matters more than
// the bytes do: §7.3 says fragmentation is the risk, and 228 little allocations
// interleaved with the first TLS session is exactly how the largest free block
// gets carved up.
char* blob = nullptr;

StationCatalogue::Station slots[StationCatalogue::kBankCount]
                               [StationCatalogue::kFrequencyCount];

size_t filledSlotCount = 0;
size_t rejectedRowCount = 0;

// Bank letter -> row in `slots`, or -1. Case-insensitive because the serial
// console is typed by hand and `m 92` is not a mistake worth punishing.
int bankIndex(char button) {
  const char upper = (char)toupper((unsigned char)button);

  for (uint8_t i = 0; i < StationCatalogue::kBankCount; i++) {
    if (StationCatalogue::kBankLetters[i] == upper) {
      return (int)i;
    }
  }
  return -1;
}

void reject(uint16_t lineNumber, const char* reason, const char* detail) {
  rejectedRowCount++;
  Log::printf("[cat] line %u rejected: %s (%s)\n", lineNumber, reason, detail);
}

// Parses one CSV row into `slots`. `line` is modified in place: the commas
// become NULs and the field pointers point back into the blob.
//
// §5 forbids a comma inside any field and there is no quoting, so splitting on
// commas is the whole parser. build-data.mjs refuses to emit a row containing
// one; a hand-edited file that breaks the rule arrives here as the wrong field
// count and is rejected, which is the intended way to find out.
void parseRow(char* line, uint16_t lineNumber) {
  char* fields[5] = {nullptr, nullptr, nullptr, nullptr, nullptr};
  size_t fieldCount = 0;
  char* cursor = line;

  while (fieldCount < 5) {
    fields[fieldCount++] = cursor;

    char* comma = strchr(cursor, ',');
    if (comma == nullptr) {
      cursor = nullptr;
      break;
    }

    *comma = '\0';
    cursor = comma + 1;
  }

  if (fieldCount != 5) {
    reject(lineNumber, "expected 5 fields", line);
    return;
  }

  // Five fields found and a comma still left in the last one: the row has more
  // separators than the format allows, so the fields are not what they look
  // like. Reject rather than keep the first five.
  if (cursor != nullptr && strchr(fields[4], ',') != nullptr) {
    reject(lineNumber, "more than 5 fields", line);
    return;
  }

  const char* button = fields[0];
  const char* frequencyText = fields[1];
  const char* name = fields[2];
  const char* url = fields[3];
  const char* logo = fields[4];

  const int bank = (strlen(button) == 1) ? bankIndex(button[0]) : -1;
  if (bank < 0) {
    reject(lineNumber, "button is not L/M/K/U", button);
    return;
  }

  // strtol rather than atoi: "92x" and "" both have to be caught, and atoi
  // reports neither.
  char* end = nullptr;
  const long frequency = strtol(frequencyText, &end, 10);
  if (end == frequencyText || *end != '\0' ||
      frequency < StationCatalogue::kMinFrequency ||
      frequency > StationCatalogue::kMaxFrequency) {
    reject(lineNumber, "frequency outside 87-105", frequencyText);
    return;
  }

  if (*name == '\0' || *url == '\0' || *logo == '\0') {
    reject(lineNumber, "empty name, url or logo", name);
    return;
  }

  // AudioEngine::playUrl() rejects an over-long URL by returning false, which
  // reads as "nothing happened" at the dial. Catching it here means the slot is
  // reported dead once at boot rather than being mysteriously silent every time
  // it is selected (§5.4).
  if (strlen(url) >= AudioEngine::kMaxUrlLength) {
    reject(lineNumber, "url over the length cap", url);
    return;
  }

  const size_t slot = (size_t)frequency - StationCatalogue::kMinFrequency;

  // Two rows on one dial position is a data error (§5). Keeping the first is
  // arbitrary but stable; what matters is saying so, because the second station
  // would otherwise simply never be reachable and nothing would say why.
  if (slots[bank][slot].name != nullptr) {
    reject(lineNumber, "duplicate slot", name);
    return;
  }

  slots[bank][slot].name = name;
  slots[bank][slot].url = url;
  slots[bank][slot].logo = logo;
  filledSlotCount++;
}

// Reads the CSV into `blob`. Separate from parsing so that a file which will
// not open or will not fit is reported as such, rather than as 76 bad rows.
bool loadFile() {
  File file = LittleFS.open(kCsvPath, "r");
  if (!file) {
    Log::printf("[cat] %s not found - run `pio run -t uploadfs`\n", kCsvPath);
    return false;
  }

  const size_t size = file.size();
  if (size == 0 || size > kMaxFileBytes) {
    Log::printf("[cat] %s is %u bytes, refusing to parse\n", kCsvPath,
                (unsigned)size);
    file.close();
    return false;
  }

  blob = (char*)malloc(size + 1);
  if (blob == nullptr) {
    Log::printf("[cat] out of memory for %u bytes\n", (unsigned)size);
    file.close();
    return false;
  }

  const size_t read = file.readBytes(blob, size);
  blob[read] = '\0';
  file.close();

  if (read != size) {
    Log::printf("[cat] short read: %u of %u bytes\n", (unsigned)read,
                (unsigned)size);
    return false;
  }

  return true;
}

}  // namespace

namespace StationCatalogue {

const char kBankLetters[kBankCount + 1] = "LMKU";

bool begin() {
  for (uint8_t bank = 0; bank < kBankCount; bank++) {
    for (uint8_t slot = 0; slot < kFrequencyCount; slot++) {
      slots[bank][slot].name = nullptr;
    }
  }

  filledSlotCount = 0;
  rejectedRowCount = 0;

  // `false` = do not format on failure. Formatting would turn "the data
  // partition was never uploaded" — far and away the likeliest cause — into a
  // blank filesystem that looks like a merely empty catalogue, and would throw
  // away a good one if the mount ever failed for a transient reason.
  if (!LittleFS.begin(false)) {
    Log::println("[cat] LittleFS mount failed - run `pio run -t uploadfs`");
    return false;
  }

  Log::printf("[cat] littlefs %u/%u bytes used\n",
              (unsigned)LittleFS.usedBytes(), (unsigned)LittleFS.totalBytes());

  if (!loadFile()) {
    return false;
  }

  // Walk the blob line by line, NUL-terminating each in place. CRLF is handled
  // as well as LF: .gitattributes pins the committed file to LF, but an image
  // built from a different checkout would arrive with CRLF and weld a trailing
  // '\r' onto every logo filename — a fault that shows up only at M4, as
  // artwork that will not open.
  uint16_t lineNumber = 0;
  char* cursor = blob;

  while (*cursor != '\0') {
    char* row = cursor;
    char* lineEnd = strpbrk(cursor, "\r\n");

    if (lineEnd != nullptr) {
      const bool crlf = (lineEnd[0] == '\r' && lineEnd[1] == '\n');
      *lineEnd = '\0';
      cursor = lineEnd + (crlf ? 2 : 1);
    } else {
      cursor = row + strlen(row);
    }

    lineNumber++;

    if (*row == '\0') {
      continue;  // blank line, including the newline that ends the file
    }

    // Skip the header by content rather than by line number, so a CSV written
    // without one still parses.
    if (lineNumber == 1 && strncasecmp(row, "button,", 7) == 0) {
      continue;
    }

    parseRow(row, lineNumber);
  }

  Log::printf("[cat] %u/%u slots filled, %u rows rejected\n",
              (unsigned)filledSlotCount,
              (unsigned)(kBankCount * kFrequencyCount),
              (unsigned)rejectedRowCount);

  return true;
}

const Station* find(char button, uint8_t frequency) {
  const int bank = bankIndex(button);
  if (bank < 0 || frequency < kMinFrequency || frequency > kMaxFrequency) {
    return nullptr;
  }

  const Station& station = slots[bank][frequency - kMinFrequency];
  return station.name != nullptr ? &station : nullptr;
}

bool logoPath(const Station& station, char* path, size_t pathSize) {
  const int written = snprintf(path, pathSize, "%s/%s", kLogoDir, station.logo);

  return written > 0 && (size_t)written < pathSize;
}

size_t filledSlots() { return filledSlotCount; }

size_t rejectedRows() { return rejectedRowCount; }

void list() {
  for (uint8_t bank = 0; bank < kBankCount; bank++) {
    for (uint8_t slot = 0; slot < kFrequencyCount; slot++) {
      const Station& station = slots[bank][slot];
      if (station.name == nullptr) {
        continue;
      }

      Log::printf("[cat] %c %3u  %-32s %s\n", kBankLetters[bank],
                  (unsigned)(kMinFrequency + slot), station.name, station.url);
    }
  }
}

}  // namespace StationCatalogue
