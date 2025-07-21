using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

public class HardwareManager : IHardwareManager
{
    private readonly ILogger<HardwareManager> _logger;
    private readonly IGpioManager _gpioManager;

    public int UartHandle { get; private set; }

    public HardwareManager(ILogger<HardwareManager> logger, IGpioManager gpioManager)
    {
        _logger = logger;
        _gpioManager = gpioManager;
    }

    public void Init()
    {
        _gpioManager.GpioInitialize();
        _logger.LogInformation("--== GPIO Initialized ==--");
        
        // To get serial name, run: ls -l /dev/serial*
        UartHandle = _gpioManager.UartInitialize();
        _logger.LogInformation("--== UART Initialized ==--");
    }

    public void Teardown()
    {
        _gpioManager.GpioTerminate();
        _logger.LogInformation("--== GPIO Terminated ==--");
        
        _gpioManager.UartTerminate(UartHandle);
        _logger.LogInformation("--== UART Terminated ==--");
    }
}