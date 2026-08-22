#pragma once

#include <stddef.h>

// The M2 bench station list.
//
// Temporary: M3 replaces this with `data/stations.csv` on LittleFS (D4, §5).
// It exists so the switch storm has real servers to hammer, and so manual
// listening has something to switch between.
//
// The four entries are chosen to spread the load rather than to sound good —
// two codecs, TLS and plain HTTP, four unrelated CDNs, 48/128/256 kbps. If a
// heap leak turns out to be specific to TLS, or to the AAC decoder, this set
// makes that visible in the storm log instead of hiding it behind one station
// tested four times. All four were probed alive in August 2026.
struct TestStation {
  const char* name;
  const char* url;
};

extern const TestStation kTestStations[];
extern const size_t kTestStationCount;
