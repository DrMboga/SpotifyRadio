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

// What the audio task is currently doing. `Playing` means a stream is actually
// running, not merely requested — the gap between the two is a TLS handshake,
// which is why the storm driver in M2 waits on this rather than on playUrl()
// returning.
enum class State : uint8_t {
  Idle,         // nothing requested, or explicitly stopped
  Connecting,   // connecttohost() is in progress on the audio task
  Playing,      // connected and decoding
  Reconnecting  // a stream was requested but is not up; a retry is scheduled
};

// Creates the command queue and starts the audio task. Call once from setup().
void begin();

// Queues a station change: the current stream is torn down before the new one
// opens, so only one TLS session exists at a time (§7.2). Returns false if the
// URL is too long or the queue is full.
//
// A stream started this way is *supervised*: if the server hangs up, the audio
// task reconnects on its own with backoff (**D17**). Only stop() ends that.
bool playUrl(const char* url);

// Queues a stop — used for empty slots and for pause, both of which mean
// disconnecting (§6). Also cancels reconnect supervision.
bool stop();

// Longest ICY title kept. Only about 26 characters reach the screen at x=3
// (§6), but the whole line is held so the truncation stays a display decision
// rather than something baked in on the audio side.
constexpr size_t kMaxStreamTitleLength = 128;

// Takes the most recent ICY title, if one has arrived since the last call.
// Returns false and leaves `out` untouched when there is nothing new, so the
// caller can poll it every pass of loop() and only redraw when it says true.
//
// This is the one piece of state that flows core 1 -> core 0. ESP32-audioI2S
// raises audio_showstreamtitle() on the audio task, and drawing from there
// would put a TFT write inside the decode loop — the exact stall D14 exists to
// prevent. So the title is copied into a mutex-protected buffer and collected
// from loop() instead.
//
// A station change clears any title not yet collected: an ICY line that arrived
// while the previous stream was being torn down belongs to the previous
// station, and drawing it under the new station's name would be wrong for as
// long as the new one took to announce a track — which for some streams is the
// whole song.
bool takeStreamTitle(char* out, size_t outSize);

// Diagnostics, safe to read from core 0.
bool isPlaying();
State state();

// Counters for the M2 switch storm. Written on core 1, read on core 0; they
// are 32-bit aligned, so a torn read is not possible on this architecture and
// no lock is needed for numbers that are only ever reported.
uint32_t connectAttempts();  // including retries
uint32_t connectFailures();  // connecttohost() returned false
uint32_t streamDrops();      // was Playing, then the server went away

// Frames the decoder threw out. A short burst of these is a corrupted stretch
// of stream - usually packet loss - that the library resyncs past on its own
// without the connection ever dropping, so it never reaches streamDrops(). It
// is audible as a brief glitch, which makes it the thing to count over a long
// soak rather than something to scroll for.
uint32_t decodeErrors();

// Level of the decoded PCM, left and right, 0..127 each. Sampled in the output
// path just before the gain stage, so a non-zero changing value proves the
// decoder is running and feeding I2S regardless of what the DAC does with it.
void vuLevel(uint8_t& left, uint8_t& right);

uint32_t streamBufferSize();    // bytes allocated for the input ring buffer
uint32_t streamBufferFilled();  // bytes currently buffered
uint32_t taskStackFreeBytes();  // audio task stack high-water mark

}  // namespace AudioEngine
