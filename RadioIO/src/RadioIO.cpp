#include <stdio.h>
#include "pico/stdlib.h"

#include "Config.h"
#include "HardwareManager.h"
#include "UARTMessenger.h"
#include "ToggleButtonState.h"
#include "PlayButtonState.h"

int main()
{
    // Global hardware setup
    HardwareManager hardwareManager;
    hardwareManager.init();

    ToggleButtonState toggleButtonsState;
    PlayButtonState playButtonState;
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

        // Check if main unit requests device status

        sleep_ms(200);
    }
}
