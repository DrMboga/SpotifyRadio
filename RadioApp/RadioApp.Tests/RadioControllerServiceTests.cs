using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.Common.Hardware;
using RadioApp.Hardware;
using RadioApp.Hardware.PiGpio;
using RadioApp.RadioController;

namespace RadioApp.Tests;

// https://xunit.net/docs/getting-started/v2/getting-started
// https://medium.com/bina-nusantara-it-division/a-comprehensive-guide-to-implementing-xunit-tests-in-c-net-b2eea43b48b

public class RadioControllerServiceTests : IDisposable
{
    private readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>();

    private readonly Mock<ILogger<RadioControllerService>> _loggerRadioControllerServiceMock =
        new Mock<ILogger<RadioControllerService>>();

    private readonly Mock<ILogger<HardwareManager>> _loggerHardwareManagerMock = new Mock<ILogger<HardwareManager>>();
    private readonly Mock<ILogger<UartIoListener>> _loggerUartIoListenerMock = new Mock<ILogger<UartIoListener>>();

    private readonly Mock<IGpioManager> _gpioManagerMock = new Mock<IGpioManager>();
    private readonly Mock<IUartManager> _uartManagerMock = new Mock<IUartManager>();

    private readonly IHardwareManager _hardwareManager;
    private readonly IUartIoListener _uartIoListener;

    private readonly RadioControllerService _radioControllerService;

    public RadioControllerServiceTests()
    {
        _hardwareManager = new HardwareManager(_loggerHardwareManagerMock.Object, _gpioManagerMock.Object);
        _uartIoListener = new UartIoListener(_loggerUartIoListenerMock.Object, _hardwareManager,
            _gpioManagerMock.Object, _uartManagerMock.Object, _mediatorMock.Object);

        _radioControllerService =
            new RadioControllerService(_loggerRadioControllerServiceMock.Object, _hardwareManager, _uartIoListener);
    }

    public void Dispose()
    {
    }

    [Fact]
    public async Task ShouldInitHardwareAndStartListenToUartOnServiceStart()
    {
        await _radioControllerService.StartAsync(CancellationToken.None);
        
        // Assert Init
        _gpioManagerMock.Verify(m => m.GpioInitialize(), Times.Once);
        _gpioManagerMock.Verify(m => m.UartInitialize(), Times.Once);
        _gpioManagerMock.Verify(m => m.SetPinMode(16, GpioMode.Output), Times.Once);
        _gpioManagerMock.Verify(m => m.SetPinValue(16, GpioLevel.Low), Times.Once);
        
        // Assert listen IO channel
        _gpioManagerMock.Verify(m => m.InitInputPinAsPullUp(26), Times.Once);
        // TODO: _gpioManager.RegisterPinCallbackFunction
    }
}