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

void UARTMessenger::sendPuttonPushedCommand(int buttonIndex) {
    char message[64];
    sprintf(message, "{\"command\":\"ButtonPressed\",\"buttonIndex\":%d}", buttonIndex);
    sendMessage(message);
}

void UARTMessenger::sendPlayPauseCommand(bool isPause) {
    char message[64];
    sprintf(message, "{\"command\":\"PlayPause\",\"isPause\":%u}", isPause);

    sendMessage(message);
}

void UARTMessenger::sendNewFrequencyCommand(int frequency) {
    char message[64];
    sprintf(message, "{\"command\":\"NewFrequency\",\"frequency\":%d}", frequency);

    sendMessage(message);
}

void UARTMessenger::sendWholeStateCommand(int buttonIndex, bool isPause, int frequency) {
    char message[64];
    sprintf(message, "{\"buttonIndex\":%d,\"isPause\":%u,\"frequency\":%d}", buttonIndex, isPause, frequency);

    sendMessage(message);
}