using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.Common.Hardware;
using RadioApp.Hardware;
using RadioApp.Hardware.PiGpio;
using Xunit.Abstractions;

namespace RadioApp.Tests;

public class HardwareFixture : IDisposable
{
    private readonly Mock<ILogger<HardwareManager>> _loggerHardwareManagerMock = new Mock<ILogger<HardwareManager>>();
    private readonly Mock<ILogger<UartIoListener>> _loggerUartIoListenerMock = new Mock<ILogger<UartIoListener>>();

    public IHardwareManager HardwareManager { get; }
    public IUartIoListener UartIoListener { get; }

    public Mock<IGpioManager> GpioManagerMock { get; } = new Mock<IGpioManager>();
    public Mock<IUartManager> UartManagerMock { get; } = new Mock<IUartManager>();

    public Mock<IMediator> MediatorMock { get; } = new Mock<IMediator>();

    public HardwareFixture(ITestOutputHelper output)
    {
        _loggerHardwareManagerMock.RegisterTestOutputHelper(output);
        _loggerUartIoListenerMock.RegisterTestOutputHelper(output);
        
        HardwareManager = new HardwareManager(_loggerHardwareManagerMock.Object, GpioManagerMock.Object);
        UartIoListener = new UartIoListener(_loggerUartIoListenerMock.Object, HardwareManager,
            GpioManagerMock.Object, UartManagerMock.Object, MediatorMock.Object);
    }
    
    public void Dispose()
    {
    }
}