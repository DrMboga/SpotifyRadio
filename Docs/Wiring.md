# TFT Display And PICO Wiring

## TFT

| Display Pin | Function        | Raspberry Pi Pin |
|------------|---------------|-----------------|
| LEDA       | Backlight      | 3.3V |
| GND        | Ground         | GND |
| VSS        | Power Supply   | 5V |
| CS         | SPI Chip Select    | GPIO 8 (CE0) |
| SDA        | SPI MOSI           | GPIO 10 (MOSI) |
| SCK        | SPI Clock          | GPIO 11 (SCLK) |
| **RS (D/CX)**  | Data/Command  | GPIO 23 |
| **RES**     | Reset         | GPIO 24 |

---

## UART 

| Function  | Raspberry Pi 4 (UART) | Raspberry Pi Pico |
|-----------|----------------|-----------------|
| TX  | GPIO15 (RX)        | GP4 (TX)    |
| RX  | GPIO14 (TX)        | GP5 (RX)    |
| Interrupt | GPIO 26        | GP14 (OUT)       |
| Get Buttons Status interrupt | | |
| Play/pause button | -        | GP15 (IN)       |

---

## Buttons (resistors ladder)
| Function  |  Raspberry Pi Pico |
|-----------|-----------------|


---

## Capacitance

| Function  |  Raspberry Pi Pico |
|-----------|-----------------|
| R1  |  GP16    |
| R2  | GP17    |
| Voltmeter |  A1       |
| GND |  ADC GND       |