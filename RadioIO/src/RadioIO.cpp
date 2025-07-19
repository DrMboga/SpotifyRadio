#include <stdio.h>
#include "pico/stdlib.h"

#include "Config.h"
#include "HardwareManager.h"
#include "UARTMessenger.h"

int main()
{
    // Global hardware setup
    HardwareManager hardwareManager;
    hardwareManager.init();

    UARTMessenger uartMessenger;

    uartMessenger.sendMessage("Hello, UART!\n");

    while (true) {
        printf("Hello, world!\n");
        sleep_ms(1000);
    }
}
