#pragma once

#include <stdint.h>

// ESP32 pin map — the single source of truth in code for Architecture.md §3.1.
// Correct this file (and §3.1) if the as-built wiring ever differs.
//
// Strapping pins (0, 2, 12, 15) and flash pins (6-11) are avoided. GPIO 16/17
// are deliberately left free so a WROVER swap stays a platformio.ini change
// (§9.1 rung 6).

// --- I2S audio out — PCM5102A (M1) -----------------------------------------
constexpr uint8_t kPinI2sBclk = 26;  // PCM5102A BCK
constexpr uint8_t kPinI2sLrck = 25;  // PCM5102A LCK  (word select)
constexpr uint8_t kPinI2sData = 22;  // PCM5102A DIN

// --- ST7735 TFT over VSPI (M4) ---------------------------------------------
constexpr uint8_t kPinTftSck = 18;   // ST7735 SCK
constexpr uint8_t kPinTftMosi = 23;  // ST7735 SDA
constexpr uint8_t kPinTftCs = 5;     // ST7735 CS
constexpr uint8_t kPinTftDc = 21;    // ST7735 RS (data/command)
constexpr uint8_t kPinTftReset = 4;  // ST7735 RES

// --- Pico I/O board over UART2 (M5) ----------------------------------------
constexpr uint8_t kPinUartRx = 27;         // <- Pico GP4 (TX)
constexpr uint8_t kPinUartTx = 14;         // -> Pico GP5 (RX), wired but unused
constexpr uint8_t kPinPicoDataReady = 34;  // <- Pico GP14, input-only pin
constexpr uint8_t kPinRequestState = 33;   // -> Pico GP22
