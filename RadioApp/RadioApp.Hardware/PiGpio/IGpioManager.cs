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

    void RegisterPinCallbackFunction(uint inputPin, PiGpioInterop.gpioAlertCallback? alertFunction);
    void UnregisterPinCallbackFunction(uint inputPin);
}