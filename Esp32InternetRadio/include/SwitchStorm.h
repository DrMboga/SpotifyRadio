#pragma once

#include <stdint.h>

// M2's primary gate: N station changes back to back, measuring what each one
// costs the heap (Architecture.md §7.3, D17).
//
// The radio is used in 2–3 hour sessions with roughly 30–60 station changes in
// them, and is switched off afterwards. So the question M2 has to answer is not
// "does the heap drift over days" but "what does one station change cost, and
// does that cost accumulate" — 60 changes is a whole evening's worth compressed
// into about ten minutes.
//
// Each change is a full TLS teardown and setup, which is the operation
// suspected of fragmenting the heap. The driver waits for the stream to be
// genuinely playing before it starts the dwell, so every cycle is a real
// connect rather than a queued command that failed quietly.
//
// Runs entirely on core 0, driven from loop(); it never blocks and never
// touches the Audio object (D14).
namespace SwitchStorm {

// Kicks off `changes` station changes, cycling through kTestStations. Any
// storm already running is abandoned.
void start(uint16_t changes);

// Stops early. The summary is still printed, over however many changes ran.
void stop();

bool isRunning();

// Call every pass of loop().
void update();

}  // namespace SwitchStorm
