using System.Runtime.InteropServices;

namespace RadioApp.Hardware.PiGpio;

// Library documentation: https://abyz.me.uk/rpi/pigpio/cif.html#gpioWrite

public static partial class PiGpioInterop
{
    public const uint PI_CFG_NOSIGHANDLER = 1u << 10; // 1024
    
    /// <summary>
    /// Input GPIO Alert callback function delegate
    /// </summary>
    public delegate void gpioAlertCallback(int gpio, int level, uint tick);

    /// <summary>
    /// Initialises the library.
    /// </summary>
    /// <returns>pigpio version number if OK, otherwise <see cref="GpioErrorCode"/></returns>
    [LibraryImport("pigpio")]
    public static partial int gpioInitialise();
    
    /// <summary>
    /// This function returns the current library internal configuration settings.
    /// </summary>
    [LibraryImport("pigpio")]
    public static partial uint gpioCfgGetInternals();
    
    /// <summary>
    /// This function sets the current library internal configuration settings.
    /// </summary>
    [LibraryImport("pigpio")]
    public static partial int gpioCfgSetInternals(uint cfgVal);

    /// <summary>
    /// Sets the GPIO mode, typically input or output.
    /// </summary>
    /// <returns>0 if OK, otherwise <see cref="GpioErrorCode"/></returns>
    [LibraryImport("pigpio")]
    public static partial int gpioSetMode(uint gpio, uint mode);

    /// <summary>
    /// Sets the GPIO level, on or off.
    /// </summary>
    /// <returns>0 if OK, otherwise <see cref="GpioErrorCode"/></returns>
    [LibraryImport("pigpio")]
    public static partial int gpioWrite(uint gpio, uint level);

    /// <summary>
    /// Sets or clears resistor pull ups or downs on the GPIO.
    /// </summary>
    /// <returns>0 if OK, otherwise <see cref="GpioErrorCode"/></returns>
    [LibraryImport("pigpio")]
    public static partial int gpioSetPullUpDown(uint gpio, uint pud);

    /// <summary>
    /// Registers a function to be called (a callback) when the specified GPIO changes state.
    /// </summary>
    /// <returns>0 if OK, otherwise <see cref="GpioErrorCode"/></returns>
    [LibraryImport("pigpio")]
    public static partial int gpioSetAlertFunc(uint gpio, gpioAlertCallback? alertFunction);

    /// <summary>
    /// Terminates the library.
    /// This function resets the used DMA channels, releases memory, and terminates any running threads.
    /// </summary>
    [LibraryImport("pigpio")]
    public static partial void gpioTerminate();

    /// <summary>
    /// This function returns a handle for the SPI device on the channel. 
    /// Data will be transferred at baud bits per second. 
    /// The flags may be used to modify the default behavior of 4-wire operation, mode 0, active low chip select. 
    /// </summary>
    /// <returns>
    /// Returns a handle (>=0) if OK, 
    /// otherwise PI_BAD_SPI_CHANNEL, PI_BAD_SPI_SPEED, PI_BAD_FLAGS, PI_NO_AUX_SPI, or PI_SPI_OPEN_FAILED.
    /// </returns>
    [LibraryImport("pigpio")]
    public static partial int spiOpen(uint spiChan, uint baud, uint spiFlags);

    /// <summary>
    /// This functions closes the SPI device identified by the handle. 
    /// </summary>
    /// <returns>
    /// Returns 0 if OK, otherwise PI_BAD_HANDLE.
    /// </returns>
    [LibraryImport("pigpio")]
    public static partial int spiClose(uint handle);

    /// <summary>
    /// This function writes count bytes of data from buf to the SPI device associated with the handle. 
    /// </summary>
    /// <returns>
    /// Returns the number of bytes transferred if OK, otherwise PI_BAD_HANDLE, PI_BAD_SPI_COUNT, or PI_SPI_XFER_FAILED.
    /// </returns>
    [LibraryImport("pigpio")]
    public static partial int spiWrite(uint handle, IntPtr buf, uint count);

    /// <summary>
    /// This function opens a serial device at a specified baud rate and with specified flags.
    /// </summary>
    /// <param name="sertty">The device name must start with /dev/tty or /dev/serial.</param>
    /// <param name="baud">The baud rate must be one of
    /// 50, 75, 110, 134, 150, 200, 300, 600, 1200, 1800,
    /// 2400, 4800, 9600, 19200, 38400, 57600, 115200, or 230400.</param>
    /// <param name="serFlags">No flags are currently defined. This parameter should be set to zero.</param>
    /// <returns>Returns a handle (>=0) if OK, otherwise PI_NO_HANDLE, or PI_SER_OPEN_FAILED.</returns>
    [LibraryImport("pigpio", StringMarshalling =  StringMarshalling.Utf8)]
    public static partial int serOpen(string sertty, uint baud, uint serFlags);

    /// <summary>
    /// This function closes the serial device associated with handle.
    /// </summary>
    /// <returns>Returns 0 if OK, otherwise PI_BAD_HANDLE.</returns>
    [LibraryImport("pigpio")]
    public static partial int serClose(uint handle);

    /// <summary>
    /// This function reads up count bytes from the serial port associated with handle and writes them to buf.
    /// </summary>
    /// <returns>Returns the number of bytes read (>0=) if OK, otherwise PI_BAD_HANDLE, PI_BAD_PARAM, or PI_SER_READ_NO_DATA.
    /// If no data is ready zero is returned.</returns>
    [LibraryImport("pigpio")]
    public static partial int serRead(uint handle, IntPtr buf, uint count);
}
