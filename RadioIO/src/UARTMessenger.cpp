#include "pico/stdlib.h"
#include "hardware/uart.h"
#include "hardware/gpio.h"

#include "Config.h"
#include "UARTMessenger.h"

void UARTMessenger::sendMessage (const std::string& message) {
    gpio_put(INTERRUPT_PIN, 0);
    sleep_ms(10);
    uart_puts(UART_ID, message.c_str());
    sleep_ms(10);
    gpio_put(INTERRUPT_PIN, 1);
}