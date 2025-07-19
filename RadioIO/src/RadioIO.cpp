#include <stdio.h>
#include "pico/stdlib.h"

#include "Config.h"
#include "HardwareManager.h"
#include "UARTMessenger.h"
#include "ToggleButtonState.h"

int main()
{
    // Global hardware setup
    HardwareManager hardwareManager;
    hardwareManager.init();

    ToggleButtonState toggleButtonsState;
    UARTMessenger uartMessenger;

    uartMessenger.sendMessage("Hello, UART!\n");

    // Main loop
    while (true) {
        // Check if button from buttons ladder pressed
        if(toggleButtonsState.updateState()) {
            uartMessenger.sendPuttonPushedCommand(toggleButtonsState.getCurrentButtonIndex());
        }

        // Check if Play/Pause button pressed

        // Check if capacitance changed

        // Check if main unit requests device status

        sleep_ms(200);
    }
}
