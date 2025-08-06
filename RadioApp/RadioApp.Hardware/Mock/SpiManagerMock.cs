using Microsoft.Extensions.Logging;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware.Mock;

public class SpiManagerMock: ISpiManager
{
    private readonly ILogger<SpiManagerMock> _logger;

    public SpiManagerMock(ILogger<SpiManagerMock> logger)
    {
        _logger = logger;
    }

    public void SendCommand(int spiHandle, uint rsPin, byte command)
    {
        _logger.LogDebug("Sending command {SPIHandle} {Command}", spiHandle, command);
    }

    public void SendData(int spiHandle, uint rsPin, byte data)
    {
        
    }
}