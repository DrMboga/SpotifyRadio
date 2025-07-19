#pragma once

// UART defines
// By default the stdout UART is `uart0`, so we will use the second one
#define UART_ID uart1
#define BAUD_RATE 115200

// Use pins 4 and 5 for UART1
// Pins can be changed, see the GPIO function select table in the datasheet for information on GPIO assignments
#define UART_TX_PIN 4
#define UART_RX_PIN 5
#define INTERRUPT_PIN 14

#define VOLTAGE_PIN 26 // GPIO26 (ADC0)
#define VOLTAGE_PIN_ADC 0 // GP26->A0

#define PLAY_PAUSE_BUTTON_PIN 15

#define CHARGE_PIN 16             // Charge pin used to charge resistor R1
#define DISCHARGE_PIN 17          // Discharge pin used to discharge capacitor via resistor R2
#define CAPACITANCE_VOLTAGE_PIN 27            // GPIO27 (ADC1)
#define CAPACITANCE_VOLTAGE_PIN_ADC 1         // GP27->A1
