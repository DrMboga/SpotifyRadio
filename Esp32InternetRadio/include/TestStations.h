#pragma once

#include <stddef.h>

// The bench station list — four stations, compiled in.
//
// M3 was going to delete this in favour of `data/stations.csv` (D4, §5), and
// the catalogue has indeed taken over playback. This stayed, for two reasons
// that only became clear once the catalogue existed:
//
//   1. The switch storm measures a station against *itself* on an earlier visit
//      (SwitchStorm.cpp), because the four bench stations sit up to 43 KB apart
//      from each other. Run over the 60-station catalogue, 60 changes would
//      give one sample each and the comparison would have nothing to compare.
//   2. It is the fallback when LittleFS will not mount, which keeps the board
//      diagnosable — and the storm runnable — with no filesystem at all.
//
// The four entries are chosen to spread the load rather than to sound good.
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
