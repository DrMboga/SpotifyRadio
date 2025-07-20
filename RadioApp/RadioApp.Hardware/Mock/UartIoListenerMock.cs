using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;

namespace RadioApp.Hardware.Mock;

public class UartIoListenerMock: IUartIoListener
{
    private readonly ILogger<UartIoListenerMock> _logger;

    public UartIoListenerMock(ILogger<UartIoListenerMock> logger)
    {
        _logger = logger;
    }

    public void StartListenIoChannel()
    {
        _logger.LogInformation("Start listen to IO channel");
    }
}