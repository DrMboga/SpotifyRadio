#include "pico/stdlib.h"
#include "hardware/uart.h"
#include "hardware/adc.h"

#include "Config.h"
#include "HardwareManager.h"

void HardwareManager::init() {
    // General system init
    stdio_init_all();

    // Initialize Analog pins
    adc_init();

    // ------------
    // Set up UART
    uart_init(UART_ID, BAUD_RATE);
    // Set the TX and RX pins by using the function select on the GPIO
    // Set datasheet for more information on function select
    gpio_set_function(UART_TX_PIN, GPIO_FUNC_UART);
    gpio_set_function(UART_RX_PIN, GPIO_FUNC_UART);

    gpio_init(INTERRUPT_PIN);              // Initialize interrupt GPIO
    gpio_set_dir(INTERRUPT_PIN, GPIO_OUT); // Set as output
    gpio_put(INTERRUPT_PIN, 1);            // Default HIGH (inactive)

    // ------------
    // Buttons ladder voltage pin
    adc_gpio_init(VOLTAGE_PIN);

    // ------------
    // Capacitance measurement init
    adc_gpio_init(CAPACITANCE_VOLTAGE_PIN);
    gpio_init(CHARGE_PIN);
    gpio_init(DISCHARGE_PIN);
}
