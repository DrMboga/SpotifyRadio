#include "PicoLink.h"

#include <Arduino.h>
#include <stdio.h>
#include <string.h>

#include "Log.h"
#include "Pins.h"

namespace {

// Long enough for the longest message the Pico can produce — the `State`
// snapshot is 63 characters — with room for a corrupted frame to be recognised
// as too long rather than silently truncated into something that still parses.
constexpr size_t kMaxFrameLength = 96;

// A frame that stops half way is line noise, or a Pico that reset mid-write.
// Without this the brace counter would wait forever for a closing brace that is
// never coming, and every later message would be swallowed into the same frame.
constexpr uint32_t kFrameIdleMs = 250;

// How long the request pin stays high per attempt.
//
// §4 calls the Pico's poll 200 ms, which is what `RadioIO.cpp`'s sleep_ms(200)
// suggests, but the same loop calls CapacitanceState::updateState() every pass
// and that measures the tuning capacitor with a sleep_ms(400) discharge inside
// it — so a real pass is 600 ms plus the charge time, and holding for one second
// buys roughly one snapshot rather than the five §4 predicts. 1500 ms gives two
// chances per attempt, which keeps a single missed pass from becoming a visible
// boot delay.
constexpr uint32_t kRequestHoldMs = 1500;

// Between attempts. The pin has to actually be low for a while for the next
// raise to mean anything to a Pico that samples it at 600 ms intervals.
constexpr uint32_t kRetryGapMs = 400;

enum class Handshake : uint8_t {
  Idle,     // no snapshot wanted yet
  Holding,  // request pin high, waiting for the Pico's next pass
  Gap,      // dropped, about to raise again
  Settled   // a State has been parsed; the pin stays low
};

Handshake handshake = Handshake::Idle;
uint32_t handshakeAt = 0;
uint32_t stateRequestCount = 0;

char frame[kMaxFrameLength];
size_t frameLength = 0;
uint8_t depth = 0;
uint32_t lastByteAt = 0;

uint32_t framesReadCount = 0;
uint32_t framesRejectedCount = 0;
uint32_t strayByteCount = 0;

// Set only while selfTest() runs, and it does two jobs. Its negative cases are
// supposed to be rejected, and a boot log full of `[pico] rejected` lines that
// mean "working as intended" is worse than no test at all. And one of its cases
// is a `State`, which on the live path settles the handshake — a test that
// convinced the radio it had already read the knobs would be worse still.
bool selfTestRunning = false;

volatile uint32_t readyPulseCount = 0;

// The Pico brackets every write by pulling GP14 low for ~10 ms either side, so
// a falling edge means "a message is on its way". Nothing depends on it —
// polling Serial2 is enough and simpler (§4) — but counting the edges is what
// tells a dead Pico apart from a miswired RX line during bring-up, and it costs
// four instructions in an ISR to know.
//
// The count is an indicator, not a message tally. GPIO 34 is one of the ESP32's
// input-only pins and those have **no internal pull-up or pull-down**, so on a
// bench with long jumpers the line picks up the odd spurious edge: a 150-second
// idle run measured 6 pulses against 1 actual message. Read it as "is the Pico
// alive and is that wire connected", never as "how many messages were sent".
void IRAM_ATTR onDataReady() { readyPulseCount++; }

// --- The smallest JSON reader that can read this contract --------------------
//
// §4 froze four message shapes, all of them produced by sprintf() from a
// literal format string: flat objects, no nesting, no arrays, no escapes, no
// floats. A general parser buys nothing against input that cannot vary, and
// ArduinoJson would put a heap allocation on the path a station change already
// competes for — §7.2's largest free block is 14–19 KB with a stream live, and
// D5 is the standing reminder of what happens when something assumes there is
// room. The three functions below allocate nothing.
//
// Anything that is not one of the four shapes is rejected rather than guessed
// at, so a garbled frame is a counted reject and a log line, never a station
// change nobody asked for.

// Points just past the colon that follows "key", or nullptr if the key is
// absent or is not followed by one.
const char* fieldValue(const char* json, const char* key) {
  char needle[20];
  const int written = snprintf(needle, sizeof(needle), "\"%s\"", key);

  if (written < 0 || (size_t)written >= sizeof(needle)) {
    return nullptr;
  }

  const char* at = strstr(json, needle);
  if (at == nullptr) {
    return nullptr;
  }

  const char* cursor = at + written;
  while (*cursor == ' ') {
    cursor++;
  }

  if (*cursor != ':') {
    return nullptr;
  }

  cursor++;
  while (*cursor == ' ') {
    cursor++;
  }
  return cursor;
}

bool intField(const char* json, const char* key, long& out) {
  const char* value = fieldValue(json, key);
  if (value == nullptr) {
    return false;
  }

  char* end = nullptr;
  const long parsed = strtol(value, &end, 10);

  if (end == value) {
    return false;
  }

  out = parsed;
  return true;
}

bool stringField(const char* json, const char* key, char* out, size_t outSize) {
  const char* value = fieldValue(json, key);
  if (value == nullptr || *value != '"') {
    return false;
  }

  value++;
  size_t length = 0;

  while (value[length] != '"') {
    if (value[length] == '\0' || length + 1 >= outSize) {
      return false;
    }
    length++;
  }

  memcpy(out, value, length);
  out[length] = '\0';
  return true;
}

void reject(const char* reason, const char* json) {
  framesRejectedCount++;

  if (!selfTestRunning) {
    Log::printf("[pico] rejected (%s): %s\n", reason, json);
  }
}

// -1 no button, 0 Phono, 1-4 the toggle banks. Anything else is corruption:
// ToggleButtonState::recognizeButton cannot return it.
bool validButtonIndex(long value) { return value >= -1 && value <= 4; }

// CapacitanceState::getFrequency only ever returns 87-105, so a value outside
// that did not come from a healthy Pico.
bool validFrequency(long value) { return value >= 87 && value <= 105; }

bool decodeFrame(const char* json, PicoLink::Message& out) {
  char command[20];

  if (!stringField(json, "command", command, sizeof(command))) {
    reject("no command", json);
    return false;
  }

  long buttonIndex = 0;
  long frequency = 0;
  long isPause = 0;

  if (strcmp(command, "ButtonPressed") == 0) {
    if (!intField(json, "buttonIndex", buttonIndex) ||
        !validButtonIndex(buttonIndex)) {
      reject("bad buttonIndex", json);
      return false;
    }

    out = PicoLink::Message{};
    out.command = PicoLink::Command::ButtonPressed;
    out.buttonIndex = (int16_t)buttonIndex;
    return true;
  }

  if (strcmp(command, "PlayPause") == 0) {
    if (!intField(json, "isPause", isPause)) {
      reject("bad isPause", json);
      return false;
    }

    out = PicoLink::Message{};
    out.command = PicoLink::Command::PlayPause;
    out.isPause = isPause != 0;
    return true;
  }

  if (strcmp(command, "NewFrequency") == 0) {
    if (!intField(json, "frequency", frequency) || !validFrequency(frequency)) {
      reject("bad frequency", json);
      return false;
    }

    out = PicoLink::Message{};
    out.command = PicoLink::Command::NewFrequency;
    out.frequency = (uint8_t)frequency;
    return true;
  }

  if (strcmp(command, "State") == 0) {
    if (!intField(json, "buttonIndex", buttonIndex) ||
        !validButtonIndex(buttonIndex)) {
      reject("bad buttonIndex", json);
      return false;
    }

    if (!intField(json, "frequency", frequency) || !validFrequency(frequency)) {
      reject("bad frequency", json);
      return false;
    }

    if (!intField(json, "isPause", isPause)) {
      reject("bad isPause", json);
      return false;
    }

    out = PicoLink::Message{};
    out.command = PicoLink::Command::State;
    out.buttonIndex = (int16_t)buttonIndex;
    out.frequency = (uint8_t)frequency;
    out.isPause = isPause != 0;
    return true;
  }

  reject("unknown command", json);
  return false;
}

// --- The state request (§4) --------------------------------------------------

void dropRequestPin() { digitalWrite(kPinRequestState, LOW); }

void raiseRequestPin() {
  digitalWrite(kPinRequestState, HIGH);
  handshake = Handshake::Holding;
  handshakeAt = millis();
  stateRequestCount++;

  // Loud for the first few, then rare. A Pico that never answers is a wiring
  // fault worth seeing at boot, but it must not bury the rest of the log for
  // however long the bench is left running afterwards.
  if (stateRequestCount <= 3 || stateRequestCount % 20 == 0) {
    Log::printf("[pico] requesting state, attempt %u\n",
                (unsigned)stateRequestCount);
  }
}

// The Pi dropped the pin on *any* message read while a request was outstanding,
// not only on a State (§4, and UartIoListener.ReadUartMessage). The reason is
// that a button press can be in flight when the request goes up, and leaving
// the pin high through it means the Pico keeps answering while the ESP32 is
// busy with something else. Drop, then raise again out of the gap.
void onFrameFramed() {
  if (selfTestRunning) {
    return;
  }

  if (handshake == Handshake::Holding) {
    dropRequestPin();
    handshake = Handshake::Gap;
    handshakeAt = millis();
  }
}

void onStateDecoded() {
  if (selfTestRunning) {
    return;
  }

  if (handshake == Handshake::Settled) {
    return;  // a duplicate snapshot: idempotent, and not a fault (§4)
  }

  dropRequestPin();
  handshake = Handshake::Settled;
  Log::printf("[pico] state taken after %u request(s)\n",
              (unsigned)stateRequestCount);
}

void updateHandshake() {
  if (handshake == Handshake::Holding &&
      millis() - handshakeAt >= kRequestHoldMs) {
    dropRequestPin();
    handshake = Handshake::Gap;
    handshakeAt = millis();
    return;
  }

  if (handshake == Handshake::Gap && millis() - handshakeAt >= kRetryGapMs) {
    raiseRequestPin();
  }
}

void resetFrame() {
  frameLength = 0;
  depth = 0;
}

void discardStalePartialFrame() {
  if (depth == 0 || millis() - lastByteAt < kFrameIdleMs) {
    return;
  }

  frame[frameLength] = '\0';
  framesRejectedCount++;
  Log::printf("[pico] partial frame timed out after %u bytes: %s\n",
              (unsigned)frameLength, frame);
  resetFrame();
}

// One byte into the framer; true when a complete, valid message came out.
//
// The whole of the receive path lives here rather than inline in poll() so that
// selfTest() can drive the real thing byte by byte instead of a copy of it.
bool consumeByte(char c, PicoLink::Message& out) {
  // Outside a frame, anything that is not an opening brace is noise — the tail
  // of a message that was cut by a reset, or a stray level on a line that was
  // just plugged in. Resynchronising on the opening brace is what makes the
  // framing self-healing when there is no terminator to look for.
  if (depth == 0 && c != '{') {
    strayByteCount++;
    return false;
  }

  if (frameLength + 1 >= sizeof(frame)) {
    framesRejectedCount++;
    Log::println("[pico] frame too long, resynchronising");
    resetFrame();
    return false;
  }

  frame[frameLength++] = c;

  if (c == '{') {
    depth++;
    return false;
  }

  if (c != '}') {
    return false;
  }

  depth--;
  if (depth != 0) {
    return false;
  }

  frame[frameLength] = '\0';

  // Framed counts as "read" for the handshake regardless of what it turns out
  // to say, which is the ordering the Pi used and §4 records.
  onFrameFramed();

  const bool decoded = decodeFrame(frame, out);
  resetFrame();

  if (!decoded) {
    return false;
  }

  framesReadCount++;

  if (out.command == PicoLink::Command::State) {
    onStateDecoded();
  }

  return true;
}

}  // namespace

namespace PicoLink {

void begin() {
  // Order matters, and it is why begin() is the first thing setup() does.
  // GPIO 33 is high-Z out of reset and the Pico never initialises its end
  // (§4): that line reads low only because the RP2040 powers pads up with a
  // pull-down. Every millisecond before this one is a millisecond in which the
  // Pico may decide a state request is outstanding and start streaming
  // snapshots at the ESP32 before it is listening.
  pinMode(kPinRequestState, OUTPUT);
  digitalWrite(kPinRequestState, LOW);

  pinMode(kPinPicoDataReady, INPUT);
  attachInterrupt(digitalPinToInterrupt(kPinPicoDataReady), onDataReady,
                  FALLING);

  // UART2 on 27/14 rather than the default 16/17, which §3.1 keeps free so a
  // WROVER swap stays a platformio.ini change.
  Serial2.begin(115200, SERIAL_8N1, kPinUartRx, kPinUartTx);

  resetFrame();
  lastByteAt = millis();

  Log::printf("[pico] uart2 rx=%u tx=%u, ready=%u %s, request=%u driven low\n",
              (unsigned)kPinUartRx, (unsigned)kPinUartTx,
              (unsigned)kPinPicoDataReady,
              digitalRead(kPinPicoDataReady) == HIGH ? "high (idle)"
                                                     : "LOW (writing, or not "
                                                       "wired)",
              (unsigned)kPinRequestState);
}

bool selfTest() {
  // Every literal below is the output of one of the four sprintf() calls in
  // RadioIO/src/UARTMessenger.cpp, character for character. If this file ever
  // stops matching that one, this is where it shows up.
  struct Case {
    const char* bytes;
    int expectedMessages;
    Command command;
    int16_t buttonIndex;
    uint8_t frequency;
    bool isPause;
    const char* what;
  };

  static const Case cases[] = {
      {"{\"command\":\"ButtonPressed\",\"buttonIndex\":2}", 1,
       Command::ButtonPressed, 2, 0, false, "ButtonPressed M"},
      {"{\"command\":\"ButtonPressed\",\"buttonIndex\":-1}", 1,
       Command::ButtonPressed, -1, 0, false, "ButtonPressed none"},
      {"{\"command\":\"PlayPause\",\"isPause\":1}", 1, Command::PlayPause, -1, 0,
       true, "PlayPause paused"},
      {"{\"command\":\"NewFrequency\",\"frequency\":92}", 1,
       Command::NewFrequency, -1, 92, false, "NewFrequency 92"},
      {"{\"command\":\"State\",\"buttonIndex\":2,\"isPause\":0,\"frequency\":"
       "92}",
       1, Command::State, 2, 92, false, "State snapshot"},

      // The wire condition that makes framing the problem it is: two messages
      // back to back with nothing between them, because the Pico writes no
      // terminator (§4).
      {"{\"command\":\"NewFrequency\",\"frequency\":87}{\"command\":"
       "\"NewFrequency\",\"frequency\":105}",
       2, Command::NewFrequency, -1, 105, false, "two frames, no separator"},

      // Resynchronisation: the tail of something that was cut, then a good
      // message.
      {"ncy\":99}{\"command\":\"PlayPause\",\"isPause\":0}", 1,
       Command::PlayPause, -1, 0, false, "garbage prefix"},

      // Values the Pico cannot produce. Rejected rather than acted on: a bad
      // frequency that got through would be a station change nobody asked for.
      {"{\"command\":\"NewFrequency\",\"frequency\":200}", 0,
       Command::NewFrequency, -1, 0, false, "frequency out of range"},
      {"{\"command\":\"ButtonPressed\",\"buttonIndex\":9}", 0,
       Command::ButtonPressed, -1, 0, false, "buttonIndex out of range"},
      {"{\"command\":\"Nonsense\",\"buttonIndex\":2}", 0, Command::ButtonPressed,
       -1, 0, false, "unknown command"},
      {"{\"command\":\"NewFrequency\"}", 0, Command::NewFrequency, -1, 0, false,
       "missing field"},
  };

  // The test drives the same counters and the same handshake hooks the live
  // path does, so both are put back afterwards. It runs before requestState(),
  // but leaving the numbers wrong would quietly poison the `[pico]` line for
  // the rest of the session.
  const uint32_t savedRead = framesReadCount;
  const uint32_t savedRejected = framesRejectedCount;
  const uint32_t savedStray = strayByteCount;

  selfTestRunning = true;
  resetFrame();

  uint16_t failures = 0;

  for (const Case& testCase : cases) {
    Message out{};
    Message last{};
    int messages = 0;

    for (const char* cursor = testCase.bytes; *cursor != '\0'; cursor++) {
      if (consumeByte(*cursor, out)) {
        messages++;
        last = out;
      }
    }

    bool ok = messages == testCase.expectedMessages;

    if (ok && testCase.expectedMessages > 0) {
      ok = last.command == testCase.command &&
           last.buttonIndex == testCase.buttonIndex &&
           last.frequency == testCase.frequency &&
           last.isPause == testCase.isPause;
    }

    if (!ok) {
      failures++;
      Log::printf("[pico] SELF-TEST FAIL: %s (got %d message(s))\n",
                  testCase.what, messages);
    }

    resetFrame();
  }

  selfTestRunning = false;
  framesReadCount = savedRead;
  framesRejectedCount = savedRejected;
  strayByteCount = savedStray;

  Log::printf("[pico] self-test: %u of %u cases passed\n",
              (unsigned)((sizeof(cases) / sizeof(cases[0])) - failures),
              (unsigned)(sizeof(cases) / sizeof(cases[0])));

  return failures == 0;
}

void requestState() {
  // Anything already buffered predates the request. It is a real message, but
  // it describes the knobs as they were up to thirty seconds ago — the WiFi
  // connect sits between begin() and here — and taking it would defeat the
  // point of asking.
  while (Serial2.available() > 0) {
    Serial2.read();
  }

  resetFrame();
  raiseRequestPin();
}

bool haveState() { return handshake == Handshake::Settled; }

bool poll(Message& out) {
  updateHandshake();

  while (Serial2.available() > 0) {
    const int byteRead = Serial2.read();
    if (byteRead < 0) {
      break;
    }

    lastByteAt = millis();

    if (consumeByte((char)byteRead, out)) {
      return true;
    }
  }

  discardStalePartialFrame();
  return false;
}

char bankLetter(int16_t buttonIndex) {
  switch (buttonIndex) {
    case 1:
      return 'L';
    case 2:
      return 'M';
    case 3:
      return 'K';
    case 4:
      return 'U';
    default:
      return '\0';  // -1 no button, 0 Phono — neither selects a bank
  }
}

uint32_t framesRead() { return framesReadCount; }
uint32_t framesRejected() { return framesRejectedCount; }
uint32_t strayBytes() { return strayByteCount; }
uint32_t readyPulses() { return readyPulseCount; }
uint32_t stateRequests() { return stateRequestCount; }
bool readyPinLevel() { return digitalRead(kPinPicoDataReady) == HIGH; }

}  // namespace PicoLink
