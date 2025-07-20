namespace RadioApp.Hardware.PiGpio;

public static class GpioManager
{
    /// <summary>
    /// Initialises the library.
    /// </summary>
    /// <exception cref="GpioException"></exception>
    public static void GpioInitialize()
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
    public static void GpioTerminate()
    {
        PiGpioInterop.gpioTerminate();
    }
    
    /// <summary>
    /// This method is used for set up the typical pull-up configuration for a button.
    /// That means that it sets up the pull-up resistor so the PIN reads High by default.
    /// If this pin is connected to the button then other leg of the button should be connected to GND.
    /// When button is pressed, it will connect the pin to GND, causing the input pin to read Low.
    /// </summary>
    public static void InitInputPinAsPullUp(uint inputPin)
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
    
    public static void RegisterPinCallbackFunction(uint inputPin, PiGpioInterop.gpioAlertCallback? alertFunction)
    {
        // Register callback
        int result = PiGpioInterop.gpioSetAlertFunc(inputPin, alertFunction);
        if (result < 0)
        {
            throw new GpioException($"set PIN {inputPin} callback function error", result);
        }
    }

    public static void UnregisterPinCallbackFunction(uint inputPin)
    {
        // Unregister callback
        int result = PiGpioInterop.gpioSetAlertFunc(inputPin, null);
        if (result < 0)
        {
            throw new GpioException($"Unset PIN {inputPin} callback function error", result);
        }
    }
}