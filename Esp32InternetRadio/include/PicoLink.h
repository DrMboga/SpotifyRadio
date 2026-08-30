#pragma once

#include <stddef.h>
#include <stdint.h>

// The Pico I/O board over UART2 (Architecture.md §4, D11).
//
// `RadioIO/` is frozen, so this side has to fit the contract exactly as it is
// rather than the one it would have been given a choice:
//
//   * the Pico writes JSON with **no terminator**, so frames are found by
//     counting braces, not by readStringUntil('\n') — which would hang;
//   * the boot-time `State` snapshot is requested by holding a pin high, and
//     the Pico answers on its own poll cycle with no acknowledgement, so the
//     handshake needs a retry and has to tolerate duplicates;
//   * that request pin is never initialised on the Pico, so the ESP32 must
//     drive its end low explicitly and early — a floating line makes the Pico
//     stream `State` forever.
//
// Everything here runs on core 0, polled from loop(). Nothing blocks: the
// handshake is a state machine so a Pico that is unplugged or unpowered costs
// nothing but a log line, and the serial console keeps working without it.
namespace PicoLink {

enum class Command : uint8_t {
  ButtonPressed,  // a toggle button changed
  PlayPause,      // the play/pause button changed
  NewFrequency,   // the tuning capacitor moved
  State           // full snapshot, the answer to a state request
};

// One decoded message. Fields not carried by the command are left at the
// defaults below, so reading `frequency` off a ButtonPressed gives 0 rather
// than a stale value from the previous frame.
struct Message {
  Command command = Command::ButtonPressed;
  int16_t buttonIndex = -1;  // -1 none, 0 Phono (unused), 1 L, 2 M, 3 K, 4 U
  uint8_t frequency = 0;     // 87–105
  bool isPause = false;      // true = paused
};

// Drives the request-state pin low, opens UART2 and arms the data-ready pin.
//
// Call this **first** in setup(), before the display and before WiFi. GPIO 33
// is high-Z until configured and the Pico reads it every poll, so any delay
// here is time spent with the request line floating (§4).
void begin();

// Pushes the four frozen message shapes, and a handful of malformed ones,
// through the real framing and decode path and reports whether they came out
// as expected. Returns true if every case passed.
//
// It is here because §4 is frozen and the parser is hand-written: M5 was
// planned around ArduinoJson and this reads the wire itself instead, so the
// argument for that ("the messages cannot vary") is worth checking rather than
// asserting. It runs on the target, on the shipped code, in about a
// millisecond, and prints one line unless something fails.
bool selfTest();

// Starts the boot-time state handshake: raise the request pin, take one
// `State`, drop it. Retries until answered — a radio that cannot see its own
// controls has nothing useful to do, and the Pico gives no acknowledgement to
// wait on. Progress is on the serial log.
void requestState();

// True once a `State` snapshot has been parsed, i.e. the radio knows where the
// knobs are sitting. False while the handshake is still outstanding.
bool haveState();

// Takes one decoded message, or returns false if none is complete yet. Drives
// the framing, the parse and the handshake, so call it every pass of loop()
// until it returns false.
bool poll(Message& out);

// `buttonIndex` -> bank letter, or '\0' for -1 (no button) and 0 (Phono).
// Phono is a real position on the SABA and means "not the radio", which is why
// it maps to no bank rather than to a default one.
char bankLetter(int16_t buttonIndex);

// Diagnostics for the `h` line and for bring-up. `readyPulses()` counts falling
// edges on the Pico's data-ready pin, which is the one signal that arrives
// without the RX wire: pulses but no frames means the UART line is the fault,
// no pulses at all means the Pico is not running or the ground is not shared.
uint32_t framesRead();
uint32_t framesRejected();

// Bytes seen outside any frame. Non-zero is normal exactly once — a message cut
// in half by a reset — and a steadily climbing count means the line is picking
// up noise or the two ends disagree about the baud rate.
uint32_t strayBytes();

// Falling edges on the Pico's data-ready pin. An indicator that the Pico is
// alive and that wire is connected, *not* a message count: GPIO 34 is
// input-only and has no internal pull, so it collects a few spurious edges a
// minute on a breadboard.
uint32_t readyPulses();

uint32_t stateRequests();
bool readyPinLevel();  // idles high; low while the Pico is writing

}  // namespace PicoLink
