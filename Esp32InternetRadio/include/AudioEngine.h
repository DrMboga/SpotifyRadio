#pragma once

#include <stddef.h>
#include <stdint.h>

// The audio side of the radio (Architecture.md §7.1, D14).
//
// Everything in here runs on a FreeRTOS task pinned to core 1. That task owns
// the Audio object and the one live stream connection; nothing else may touch
// them. Core 0 — UART framing, PNG decode, TFT drawing — talks to it only
// through the queued commands below, all of which return immediately.
namespace AudioEngine {

// Longest stream URL accepted by playUrl(). Commands carry the URL by value so
// no ownership crosses the core boundary and no String churn happens on the
// hot path (§7.2).
constexpr size_t kMaxUrlLength = 192;

// Creates the command queue and starts the audio task. Call once from setup().
void begin();

// Queues a station change: the current stream is torn down before the new one
// opens, so only one TLS session exists at a time (§7.2). Returns false if the
// URL is too long or the queue is full.
bool playUrl(const char* url);

// Queues a stop — used for empty slots and for pause, both of which mean
// disconnecting (§6).
bool stop();

// Diagnostics, safe to read from core 0.
bool isPlaying();

// Level of the decoded PCM, left and right, 0..127 each. Sampled in the output
// path just before the gain stage, so a non-zero changing value proves the
// decoder is running and feeding I2S regardless of what the DAC does with it.
void vuLevel(uint8_t& left, uint8_t& right);

uint32_t streamBufferSize();    // bytes allocated for the input ring buffer
uint32_t streamBufferFilled();  // bytes currently buffered
uint32_t taskStackFreeBytes();  // audio task stack high-water mark

}  // namespace AudioEngine
