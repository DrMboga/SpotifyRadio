namespace RadioApp.Hardware.PiGpio;

public class GpioManager: IGpioManager
{
    /// <summary>
    /// Initialises the library.
    /// </summary>
    /// <exception cref="GpioException"></exception>
    public void GpioInitialize()
    {
        int initialized = PiGpioInterop.gpioInitialise();
        if (initialized < 0)
        {
            throw new GpioException("initialization error", initialized);
        }
    }

    /// <summary>
    /// Terminates the library
    /// </summary>
    public void GpioTerminate()
    {
        PiGpioInterop.gpioTerminate();
    }

    /// <summary>
    /// Opens UART serial protocol
    /// </summary>
    /// <returns>Protocol session handler</returns>
    public int UartInitialize()
    {
        var uartHandle = PiGpioInterop.serOpen("/dev/serial0", 115200, 0);
        if (uartHandle < 0)
        {
            throw new GpioException($"UART open failed", uartHandle);
        }
        return uartHandle;
    }

    /// <summary>
    /// Terminates the UART serial protocol
    /// </summary>
    /// <param name="uartHandle">Protocol session handler</param>
    public void UartTerminate(int uartHandle)
    {
        int uartClosed = PiGpioInterop.serClose((uint)uartHandle);
        if (uartClosed < 0)
        {
            throw new GpioException($"UART close failed", uartClosed);
        }
    }

    /// <summary>
    /// This method is used for set up the typical pull-up configuration for a button.
    /// That means that it sets up the pull-up resistor so the PIN reads High by default.
    /// If this pin is connected to the button then other leg of the button should be connected to GND.
    /// When button is pressed, it will connect the pin to GND, causing the input pin to read Low.
    /// </summary>
    public void InitInputPinAsPullUp(uint inputPin)
    {
        // Set input mode
        int result = PiGpioInterop.gpioSetMode(inputPin, (uint) GpioMode.Input);
        if (result < 0)
        {
            throw new GpioException($"set PIN {inputPin} as input error", result);
        }
        
        // Set pull up resistor
        result = PiGpioInterop.gpioSetPullUpDown(inputPin, (uint)GpioPullMode.PullUp);
        if (result < 0)
        {
            throw new GpioException($"set PIN {inputPin} pull up error", result);
        }
    }
    
    public void RegisterPinCallbackFunction(uint inputPin, PiGpioInterop.gpioAlertCallback? alertFunction)
    {
        // Register callback
        int result = PiGpioInterop.gpioSetAlertFunc(inputPin, alertFunction);
        if (result < 0)
        {
            throw new GpioException($"set PIN {inputPin} callback function error", result);
        }
    }

    public void UnregisterPinCallbackFunction(uint inputPin)
    {
        // Unregister callback
        int result = PiGpioInterop.gpioSetAlertFunc(inputPin, null);
        if (result < 0)
        {
            throw new GpioException($"Unset PIN {inputPin} callback function error", result);
        }
    }
}