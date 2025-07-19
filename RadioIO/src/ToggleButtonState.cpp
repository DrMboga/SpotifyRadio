#include <stdio.h>
#include <math.h>

#include "pico/stdlib.h"
#include "hardware/adc.h"

#include "Config.h"
#include "ToggleButtonState.h"

#define VOLTAGE_THRESHOLD 0.1 // Voltage change threshold

#define LOW_VOLTAGE_GATE 1.5
// Button 5 -> 10KΩ; V = 1.65V
#define BUTTON_5_VOLTAGE_GATE 1.75
// Button 4 -> 4.7KΩ; V = 2.2V
#define BUTTON_4_VOLTAGE_GATE 2.3
// Button 3 -> 2.2KΩ V = 2.7V
#define BUTTON_3_VOLTAGE_GATE 2.8
// Button 2 -> 1KΩ V = 3V
#define BUTTON_2_VOLTAGE_GATE 3.1
// Button 1 -> 220Ω V = 3.2V
#define BUTTON_1_VOLTAGE_GATE 3.3

int ToggleButtonState::getCurrentButtonIndex() const {
    return _currentButtonIndex;
}

ToggleButtonState::ToggleButtonState() {
    _currentButtonIndex = -1;
    updateState();
}

bool ToggleButtonState::updateState() {
    // Measure the voltage in resistors ladder and define which button is pressed
    float voltage = readVoltage();
    if (voltageChanged(voltage)) {
        printf("Volatage: %.4f, ", voltage);
        _currentButtonIndex = recognizeButton(voltage);
        printf("button %d pressed.\n", _currentButtonIndex);
        return true;
    }

    return false;
}

float ToggleButtonState::readVoltage() {
    adc_select_input(VOLTAGE_PIN_ADC);

    // 12-bit conversion, assume max value == ADC_VREF == 3.3 V
    const float conversionFactor = 3.3f / (1 << 12);
    uint16_t rawValue = adc_read(); // ADC value (0-4095 for Pico)
    return rawValue * conversionFactor;
}

bool ToggleButtonState::voltageChanged(float voltage) {
    static float lastVoltage = 0;
    if (fabs(voltage - lastVoltage) > VOLTAGE_THRESHOLD)
    { // Change threshold
        lastVoltage = voltage;
        return true;
    }
    return false;
}

int ToggleButtonState::recognizeButton(float voltage)
{
    if (voltage < LOW_VOLTAGE_GATE)
    {
        return -1;
    }
    if (voltage <= BUTTON_5_VOLTAGE_GATE)
    {
        return 4;
    }
    if (voltage <= BUTTON_4_VOLTAGE_GATE)
    {
        return 3;
    }
    if (voltage <= BUTTON_3_VOLTAGE_GATE)
    {
        return 2;
    }
    if (voltage <= BUTTON_2_VOLTAGE_GATE)
    {
        return 1;
    }
    // Voltage over 3.1 means first button is pressed
    return 0;
}