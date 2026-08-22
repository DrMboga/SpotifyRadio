#pragma once

// Serialised logging.
//
// Two cores write to the same UART: the audio task on core 1 (stream events,
// ICY titles, and the ESP-IDF driver's own ESP_LOG output) and loop() on core 0
// (heap statistics, storm measurements, the command console). Nothing
// serialises them, so lines interleave mid-write and arrive as a burst of
// garbage followed by a half-printed line.
//
// That is cosmetic right up until it eats a `[stat]` line in the middle of an
// unattended three-hour soak, which is the one output that run produces. So
// every line is formatted first and then written under a lock, and ESP_LOG is
// routed through the same lock (§7.1).
namespace Log {

// Call once from setup(), after Serial.begin().
void begin();

void printf(const char* format, ...) __attribute__((format(printf, 1, 2)));
void print(const char* text);
void println(const char* text = "");

}  // namespace Log
