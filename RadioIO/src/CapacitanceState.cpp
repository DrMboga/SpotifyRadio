#include <stdio.h>
#include "pico/stdlib.h"
#include "hardware/adc.h"
#include "hardware/gpio.h"

#include "Config.h"
#include "CapacitanceState.h"

#define CHARGING_RESISTOR 1000000 // R1 = 1MOhm
#define CHARGE_LEVEL 2500         // This is used to check when capacitor charged to 63.2%. Full voltage level is 4095 which corresponds to 3,3 V. So, 2588 is 63.2% from 4095
#define PARASITIC_CAPACITANCE 50  // GP pins, wires and breadboard has the parasitic capacitance. I calculated this value just measuring the time of raising voltage over R1 without any capacitor
#define STATIONS_COUNT 18

int CapacitanceState::getCurrentFrequency() const {
    return _currentFrequency;
}

CapacitanceState::CapacitanceState() {
    _currentFrequency = -1;
    discharge();
    updateState();
}

bool CapacitanceState::updateState() {
    int32_t currentCapacitance = getCapacitance();
    int frequency = getFrequency(currentCapacitance);
    if (_currentFrequency != frequency) {
        _currentFrequency = frequency;
        return true;
    }

    return false;
}

int32_t CapacitanceState::getCapacitance() {
    charge();
    adc_select_input(1);
    uint32_t stop = 0;
    uint16_t rawValue;
    size_t index = 0;
    uint32_t start = time_us_32();

    for (size_t i = 0; i < 100000000000; i++)
    {
        rawValue = adc_read(); // ADC value (0-4095 for Pico)
        if (rawValue >= CHARGE_LEVEL)
        {
            stop = time_us_32();
            index = i;
            break;
        }
    }

    int32_t chargeTimeUs = stop - start - PARASITIC_CAPACITANCE;
    discharge();
    sleep_ms(300);
    if (chargeTimeUs < 0)
    {
        printf("Unable to measure capacitance. Start: %d, stop %d, chargeTimeUs %d mikroseconds, rawValue: %d; index: %d\n", start, stop, chargeTimeUs, rawValue, index);
        return -1;
    }

    // Formula for capacitance is chargeTime/Resistance. As soon as charge time is in microseconds which is 10^-6 seconds.
    // And resitor R1 = 1 MOhm which is 10^6 Ohm. The result will be (chargeTime * 10^-12).
    // So, the time we measured in mikroseconds equals to the capacitance in pico Farads
    return chargeTimeUs;
}

void CapacitanceState::charge()
{
    // Making DISCHARGE_PIN "disconnected"
    gpio_set_dir(DISCHARGE_PIN, GPIO_IN);
    gpio_disable_pulls(DISCHARGE_PIN);

    // Setting high to charge pin to charge the capacitor
    gpio_set_dir(CHARGE_PIN, GPIO_OUT);
    gpio_put(CHARGE_PIN, 1);
}

void CapacitanceState::discharge()
{
    // Setting discharge pin as out and set it to LOW, this will discharge the capasitor wia R2
    gpio_set_dir(DISCHARGE_PIN, GPIO_OUT);
    gpio_put(DISCHARGE_PIN, 0);

    // Making charge pin "disconnected"
    gpio_set_dir(CHARGE_PIN, GPIO_IN);
    gpio_disable_pulls(CHARGE_PIN);
}

int CapacitanceState::getFrequency(int32_t capacitance) {
    static int stations[STATIONS_COUNT] = {
        105,
        104,
        103,
        102,
        101,
        100,
        99,
        98,
        97,
        96,
        95,
        94,
        93,
        92,
        91,
        90,
        89,
        88};
    static int32_t threshold[STATIONS_COUNT] = {32, 43, 60, 78, 95, 120, 155, 190, 230, 270, 315, 360, 400, 450, 500, 560, 610, 690};
    for (size_t i = 0; i < STATIONS_COUNT; i++)
    {
        if (capacitance <= threshold[i])
        {
            return stations[i];
        }
    }
    return 87;
}