using Microsoft.Extensions.Logging;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware.Mock;

public class UartManagerMock: IUartManager
{
    private readonly ILogger<UartManagerMock> _logger;

    public UartManagerMock(ILogger<UartManagerMock> logger)
    {
        _logger = logger;
    }

    public string? ReadUartMessage(int uartHandle)
    {
        _logger.LogDebug("Mock ReadUartMessage. Handle: {UartHandle}", uartHandle);
        return "Hi there";
    }
}