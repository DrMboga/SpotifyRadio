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
}