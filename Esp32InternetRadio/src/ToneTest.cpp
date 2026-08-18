// Hardware bring-up aid — NOT part of the radio firmware.
//
// Plays a 440 Hz sine straight out of the ESP32's I2S peripheral. No WiFi, no
// TLS, no MP3 decoder, no ESP32-audioI2S — just the raw IDF driver and a
// generated waveform, so the only things under test are the three I2S pins and
// the DAC board.
//
//   pio run -e tone-test -t upload      flash this
//   pio run -t upload                   back to the real radio firmware
//
// It cycles through four two-second phases so a single listen tells you which
// channel is which, and so silence is proved to be deliberate rather than
// broken:
//
//   BOTH -> LEFT only -> RIGHT only -> SILENCE -> repeat
//
// Expected on a working DAC: a clear steady tone, and ~0.3-0.5 V on a DMM in
// AC volts between the long-side L or R pin and G during the BOTH phase.

#include <Arduino.h>
#include <driver/i2s.h>
#include <math.h>

#include "Pins.h"

namespace {

constexpr i2s_port_t kI2sPort = I2S_NUM_0;
constexpr int kSampleRate = 44100;
constexpr float kToneHz = 440.0f;

// -12 dBFS. Deliberately not full scale: loud enough to be unmistakable,
// quiet enough that a working DAC into headphones will not hurt.
constexpr int16_t kAmplitude = 8000;

constexpr size_t kFramesPerChunk = 256;
constexpr uint32_t kPhaseMs = 2000;

enum class Phase : uint8_t { Both = 0, LeftOnly = 1, RightOnly = 2, Silence = 3 };

const char* phaseName(Phase phase) {
  switch (phase) {
    case Phase::Both: return "BOTH";
    case Phase::LeftOnly: return "LEFT only";
    case Phase::RightOnly: return "RIGHT only";
    case Phase::Silence: return "SILENCE";
  }
  return "?";
}

int16_t buffer[kFramesPerChunk * 2];  // interleaved L,R
float phaseAccumulator = 0.0f;

void fillBuffer(Phase phase) {
  const float step = 2.0f * (float)M_PI * kToneHz / (float)kSampleRate;

  for (size_t frame = 0; frame < kFramesPerChunk; ++frame) {
    const int16_t sample =
        (int16_t)(sinf(phaseAccumulator) * (float)kAmplitude);

    phaseAccumulator += step;
    if (phaseAccumulator >= 2.0f * (float)M_PI) {
      phaseAccumulator -= 2.0f * (float)M_PI;
    }

    const bool left = (phase == Phase::Both || phase == Phase::LeftOnly);
    const bool right = (phase == Phase::Both || phase == Phase::RightOnly);

    buffer[frame * 2 + 0] = left ? sample : 0;
    buffer[frame * 2 + 1] = right ? sample : 0;
  }
}

}  // namespace

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("I2S tone test - 440 Hz sine, no WiFi, no audio library");
  Serial.printf("pins: bclk=%u lrck=%u data=%u\n", kPinI2sBclk, kPinI2sLrck,
                kPinI2sData);

  i2s_config_t config = {};
  config.mode = (i2s_mode_t)(I2S_MODE_MASTER | I2S_MODE_TX);
  config.sample_rate = kSampleRate;
  config.bits_per_sample = I2S_BITS_PER_SAMPLE_16BIT;
  config.channel_format = I2S_CHANNEL_FMT_RIGHT_LEFT;
  config.communication_format = I2S_COMM_FORMAT_STAND_I2S;
  config.intr_alloc_flags = ESP_INTR_FLAG_LEVEL1;
  config.dma_buf_count = 8;
  config.dma_buf_len = 256;
  config.use_apll = false;
  config.tx_desc_auto_clear = true;

  esp_err_t err = i2s_driver_install(kI2sPort, &config, 0, nullptr);
  Serial.printf("i2s_driver_install: %s\n", esp_err_to_name(err));

  i2s_pin_config_t pins = {};
  pins.mck_io_num = I2S_PIN_NO_CHANGE;
  pins.bck_io_num = kPinI2sBclk;
  pins.ws_io_num = kPinI2sLrck;
  pins.data_out_num = kPinI2sData;
  pins.data_in_num = I2S_PIN_NO_CHANGE;

  err = i2s_set_pin(kI2sPort, &pins);
  Serial.printf("i2s_set_pin: %s\n", esp_err_to_name(err));

  i2s_zero_dma_buffer(kI2sPort);

  Serial.println("playing...");
}

void loop() {
  static Phase lastPhase = Phase::Silence;

  const Phase phase = (Phase)((millis() / kPhaseMs) % 4);

  if (phase != lastPhase) {
    lastPhase = phase;
    Serial.printf("[tone] %s\n", phaseName(phase));
  }

  fillBuffer(phase);

  size_t written = 0;
  i2s_write(kI2sPort, buffer, sizeof(buffer), &written, portMAX_DELAY);
}
