using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

public class HardwareManager : IHardwareManager
{
    private const uint RequestStatusInterruptPin = 16;
    private readonly ILogger<HardwareManager> _logger;
    private readonly IGpioManager _gpioManager;

    public int UartHandle { get; private set; }
    public int SpiHandle { get; private set; }

    public HardwareManager(ILogger<HardwareManager> logger, IGpioManager gpioManager)
    {
        _logger = logger;
        _gpioManager = gpioManager;
    }

    public void Init()
    {
        lock (_gpioManager)
        {
            _gpioManager.GpioInitialize();
            _logger.LogInformation("--== GPIO Initialized ==--");

            // To get serial name, run: ls -l /dev/serial*
            UartHandle = _gpioManager.UartInitialize();
            _logger.LogInformation("--== UART Initialized ==--");

            _gpioManager.SetPinMode(RequestStatusInterruptPin, GpioMode.Output);
            // On start, we need to know the current radio status. Which buttons are pushed and frequency selected
            // To do so, we need to low down the appropriate GPIO pin.
            // Then PICO will read it as status request and will send the current status via UART
            // When UART receiver read the status message, it will set the pin to HIGH
            // So, initially we set this value as LOW
            _gpioManager.SetPinValue(RequestStatusInterruptPin, GpioLevel.Low);
            _logger.LogInformation("--== Pin {Pin} set in output mode and LOW level ==--", RequestStatusInterruptPin);

            // Initialize SPI channel for screen communication
            // _gpioManager.SetPinMode(ScreenGpioParameters.RsPin, GpioMode.Output);
            // _gpioManager.SetPinMode(ScreenGpioParameters.ResPin, GpioMode.Output);
            // SpiHandle = _gpioManager.SpiInitialize(ScreenGpioParameters.SpiChannel, ScreenGpioParameters.SpiSpeed);
        }
        _logger.LogInformation("--== SPI Initialized ==--");
    }

    public void Teardown()
    {
        lock (_gpioManager)
        {
            _gpioManager.UartTerminate(UartHandle);
            _logger.LogInformation("--== UART Terminated ==--");

            _gpioManager.SpiTerminate(SpiHandle);
            _logger.LogInformation("--== SPI Terminated ==--");

            _gpioManager.GpioTerminate();
            _logger.LogInformation("--== GPIO Terminated ==--");
        }
    }

    public void SetStatusRequestPin(bool isRequestStatus)
    {
        lock (_gpioManager)
        {
            _gpioManager.SetPinValue(RequestStatusInterruptPin, isRequestStatus ? GpioLevel.Low : GpioLevel.High);
        }
    }
}