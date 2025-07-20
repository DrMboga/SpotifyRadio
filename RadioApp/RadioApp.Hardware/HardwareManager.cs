using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

public class HardwareManager: IHardwareManager
{
    private readonly ILogger<HardwareManager> _logger;

    public HardwareManager(ILogger<HardwareManager> logger)
    {
        _logger = logger;
    }

    public void Init()
    {
        GpioManager.GpioInitialize();
        _logger.LogInformation("--== GPIO Initialized ==--");
    }

    public void Teardown()
    {
        GpioManager.GpioTerminate();
        _logger.LogInformation("--== GPIO Terminated ==--");
    }
}