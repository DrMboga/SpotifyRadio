#include "Log.h"

#include <Arduino.h>
#include <esp_log.h>
#include <stdarg.h>

namespace {

// Recursive so that a nested log call from inside a driver cannot deadlock the
// task that already holds it.
SemaphoreHandle_t serialMutex = nullptr;

// Long enough for the worst real line: the audio library prints the full
// redirect URL, and the ARD token query strings run past 300 characters.
constexpr size_t kLineBufferBytes = 384;

void lock() {
  if (serialMutex != nullptr) {
    xSemaphoreTakeRecursive(serialMutex, portMAX_DELAY);
  }
}

void unlock() {
  if (serialMutex != nullptr) {
    xSemaphoreGiveRecursive(serialMutex);
  }
}

// ESP_LOG writes straight to stdout from whichever task raised it — including
// the WiFi and TLS drivers running on the audio core. Routing it through the
// same lock is what stops `[E][WiFiClient.cpp]` landing in the middle of a
// `[stat]` line.
int lockedVprintf(const char* format, va_list args) {
  lock();
  const int written = vprintf(format, args);
  unlock();
  return written;
}

}  // namespace

namespace Log {

void begin() {
  serialMutex = xSemaphoreCreateRecursiveMutex();
  esp_log_set_vprintf(lockedVprintf);
}

void printf(const char* format, ...) {
  char line[kLineBufferBytes];

  va_list args;
  va_start(args, format);
  vsnprintf(line, sizeof(line), format, args);
  va_end(args);

  // Formatted first, written once: holding the lock across a single write is
  // what makes the line atomic. Formatting inside the lock would serialise the
  // cores for longer and buy nothing.
  print(line);
}

void print(const char* text) {
  lock();
  Serial.print(text);
  unlock();
}

void println(const char* text) {
  lock();
  Serial.println(text);
  unlock();
}

}  // namespace Log
