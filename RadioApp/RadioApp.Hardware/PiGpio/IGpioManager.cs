namespace RadioApp.Hardware.PiGpio;

public interface IGpioManager
{
    /// <summary>
    /// Initialises the library.
    /// </summary>
    /// <exception cref="GpioException"></exception>
    void GpioInitialize();

    /// <summary>
    /// Terminates the library
    /// </summary>
    void GpioTerminate();

    /// <summary>
    /// Opens UART serial protocol
    /// </summary>
    /// <returns>Protocol session handler</returns>
    int UartInitialize();

    /// <summary>
    /// Terminates the UART serial protocol
    /// </summary>
    /// <param name="uartHandle">Protocol session handler</param>
    void UartTerminate(int uartHandle);

    /// <summary>
    /// This method is used for set up the typical pull-up configuration for a button.
    /// That means that it sets up the pull-up resistor so the PIN reads High by default.
    /// If this pin is connected to the button then other leg of the button should be connected to GND.
    /// When button is pressed, it will connect the pin to GND, causing the input pin to read Low.
    /// </summary>
    void InitInputPinAsPullUp(uint inputPin);

    /// <summary>
    /// Sets pin as input or as output
    /// </summary>
    void SetPinMode(uint pin, GpioMode mode);

    /// <summary>
    /// Sets pin level. High or Low
    /// </summary>
    void SetPinValue(uint pin, GpioLevel level);

    void RegisterPinCallbackFunction(uint inputPin, PiGpioInterop.gpioAlertCallback? alertFunction);
    void UnregisterPinCallbackFunction(uint inputPin);

    /// <summary>
    /// Opens SPI serial protocol
    /// </summary>
    /// <returns>Protocol session handler</returns>
    int SpiInitialize(uint spiChannel, uint spiSpeed);

    /// <summary>
    /// Terminates the SPI serial protocol
    /// </summary>
    /// <param name="spiHandle">Protocol session handler</param>
    void SpiTerminate(int spiHandle);
    
    /// <summary>
    /// Writes a value to the output GPIO pin
    /// </summary>
    void Write(uint pin, GpioLevel level);
}