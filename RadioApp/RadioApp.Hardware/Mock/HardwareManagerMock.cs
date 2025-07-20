using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;

namespace RadioApp.Hardware.Mock;

public class HardwareManagerMock: IHardwareManager
{
    private readonly ILogger<HardwareManagerMock> _logger;

    public HardwareManagerMock(ILogger<HardwareManagerMock> logger)
    {
        _logger = logger;
    }

    public void Init()
    {
        _logger.LogInformation("Hardware interfaces initialized.");
    }

    public void Teardown()
    {
        _logger.LogInformation("Hardware interfaces terminated.");
    }
}