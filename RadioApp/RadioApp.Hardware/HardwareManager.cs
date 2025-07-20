using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

public class HardwareManager : IHardwareManager
{
    private readonly ILogger<HardwareManager> _logger;

    public int UartHandle { get; private set; }

    public HardwareManager(ILogger<HardwareManager> logger)
    {
        _logger = logger;
    }

    public void Init()
    {
        GpioManager.GpioInitialize();
        _logger.LogInformation("--== GPIO Initialized ==--");
        
        // To get serial name, run: ls -l /dev/serial*
        UartHandle = PiGpioInterop.serOpen("/dev/serial0", 115200, 0);
        if (UartHandle < 0)
        {
            throw new GpioException($"UART open failed", UartHandle);
        }
        _logger.LogInformation("--== UART Initialized ==--");
    }

    public void Teardown()
    {
        GpioManager.GpioTerminate();
        _logger.LogInformation("--== GPIO Terminated ==--");
        
        int uartClosed = PiGpioInterop.serClose((uint)UartHandle);
        if (uartClosed < 0)
        {
            throw new GpioException($"UART close failed", uartClosed);
        }
        _logger.LogInformation("--== UART Terminated ==--");
    }
}