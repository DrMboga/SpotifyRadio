#include <stdio.h>
#include "pico/stdlib.h"
#include "hardware/gpio.h"

#include "Config.h"
#include "HardwareManager.h"
#include "UARTMessenger.h"
#include "ToggleButtonState.h"
#include "PlayButtonState.h"
#include "CapacitanceState.h"

int main()
{
    // Global hardware setup
    HardwareManager hardwareManager;
    hardwareManager.init();

    ToggleButtonState toggleButtonsState;
    PlayButtonState playButtonState;
    CapacitanceState capacitanceState;
    UARTMessenger uartMessenger;

    // Main loop
    while (true) {
        // Check if button from buttons ladder pressed
        if(toggleButtonsState.updateState()) {
            uartMessenger.sendPuttonPushedCommand(toggleButtonsState.getCurrentButtonIndex());
        }

        // Check if Play/Pause button pressed
        if(playButtonState.updateState()) {
            uartMessenger.sendPlayPauseCommand(playButtonState.getPlayState());
        }

        // Check if capacitance changed
        if(capacitanceState.updateState()) {
            uartMessenger.sendNewFrequencyCommand(capacitanceState.getCurrentFrequency());
        }

        // Check if main unit requests device status
        bool stateRequest = gpio_get(REQUEST_STATE_PIN);
        if(stateRequest) {
            uartMessenger.sendWholeStateCommand(
                toggleButtonsState.getCurrentButtonIndex(), 
                playButtonState.getPlayState(), 
                capacitanceState.getCurrentFrequency());
        }

        sleep_ms(200);
    }
}
