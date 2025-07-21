using Microsoft.Extensions.Logging;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware.Mock;

public class GpioManagerMock: IGpioManager
{
    private readonly ILogger<GpioManagerMock> _logger;

    public GpioManagerMock(ILogger<GpioManagerMock> logger)
    {
        _logger = logger;
    }

    public void GpioInitialize()
    {
        _logger.LogDebug("Mock GpioInitialize");
    }

    public void GpioTerminate()
    {
        _logger.LogDebug("Mock GpioTerminate");
    }

    public int UartInitialize()
    {
        _logger.LogDebug("Mock GpioTerminate");
        return 42;
    }

    public void UartTerminate(int uartHandle)
    {
        _logger.LogDebug("Mock UartTerminate. uartHandle: {UartHandle}", uartHandle);
    }

    public void InitInputPinAsPullUp(uint inputPin)
    {
        _logger.LogDebug("Mock InitInputPinAsPullUp. inputPin: {InputPin}", inputPin);
    }

    public void SetPinMode(uint pin, GpioMode mode)
    {
        _logger.LogDebug("Mock SetPinMode. pin: {Pin}; mode: {Mode}", pin, mode);
    }

    public void SetPinValue(uint pin, GpioLevel level)
    {
        _logger.LogDebug("Mock SetPinValue. pin: {Pin}; mode: {Level}", pin, level);
    }

    public void RegisterPinCallbackFunction(uint inputPin, PiGpioInterop.gpioAlertCallback? alertFunction)
    {
        _logger.LogDebug("Mock RegisterPinCallbackFunction. inputPin: {InputPin}", inputPin);
    }

    public void UnregisterPinCallbackFunction(uint inputPin)
    {
        _logger.LogDebug("Mock UnregisterPinCallbackFunction. inputPin: {InputPin}", inputPin);
    }
}