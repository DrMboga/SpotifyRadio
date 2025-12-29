using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RadioApp.Common.Hardware;
using RadioApp.Common.IoCommands;
using RadioApp.Common.Messages.Hardware;
using RadioApp.Hardware;
using RadioApp.Hardware.PiGpio;
using RadioApp.RadioController;
using Xunit.Abstractions;

namespace RadioApp.Tests;

// https://xunit.net/docs/getting-started/v2/getting-started
// https://medium.com/bina-nusantara-it-division/a-comprehensive-guide-to-implementing-xunit-tests-in-c-net-b2eea43b48b

public class RadioControllerServiceTests
{
    private readonly Mock<ILogger<RadioControllerService>> _loggerRadioControllerServiceMock =
        new Mock<ILogger<RadioControllerService>>();

    private readonly HardwareFixture _hardwareFixture;

    private PiGpioInterop.gpioAlertCallback? _gpioAlertCallback = null;

    public RadioControllerServiceTests(ITestOutputHelper output)
    {
        _loggerRadioControllerServiceMock.RegisterTestOutputHelper(output);
        // This does not keep the shared context and creates all mocks for each test as new
        _hardwareFixture = new HardwareFixture(output);

        _hardwareFixture.GpioManagerMock.Setup(g =>
                g.RegisterPinCallbackFunction(It.IsAny<uint>(), It.IsAny<PiGpioInterop.gpioAlertCallback>()))
            .Callback<uint, PiGpioInterop.gpioAlertCallback>((pin, cb) => _gpioAlertCallback = cb);
    }

    [Fact]
    public async Task ShouldInitHardwareAndStartListenToUartOnServiceStart()
    {
        // Setup
        var radioControllerService =
            new RadioControllerService(_loggerRadioControllerServiceMock.Object, _hardwareFixture.HardwareManager,
                _hardwareFixture.UartIoListener);

        // Act
        await radioControllerService.StartAsync(CancellationToken.None);
        await Task.Delay(500);

        // Assert Init function
        _hardwareFixture.GpioManagerMock.Verify(m => m.GpioInitialize(), Times.Once);
        _hardwareFixture.GpioManagerMock.Verify(m => m.UartInitialize(), Times.Once);
        _hardwareFixture.GpioManagerMock.Verify(m => m.SetPinMode(16, GpioMode.Output), Times.Once);
        _hardwareFixture.GpioManagerMock.Verify(m => m.SetPinValue(16, GpioLevel.Low), Times.Exactly(2));

        // Assert listen IO channel
        _hardwareFixture.GpioManagerMock.Verify(m => m.InitInputPinAsPullUp(26), Times.Once);
        _hardwareFixture.GpioManagerMock.Verify(
            m => m.RegisterPinCallbackFunction(It.IsAny<uint>(), It.IsAny<PiGpioInterop.gpioAlertCallback>()),
            Times.Once);
        
        Assert.NotNull(_gpioAlertCallback);
    }

    [Fact]
    public async Task ShouldGetMessageFromUartAndSendAppropriateCommand()
    {
        // Setup
        var radioControllerService =
            new RadioControllerService(_loggerRadioControllerServiceMock.Object, _hardwareFixture.HardwareManager,
                _hardwareFixture.UartIoListener);
        await radioControllerService.StartAsync(CancellationToken.None);

        await Task.Delay(100);
        
        _hardwareFixture.GpioManagerMock.Verify(
            m => m.RegisterPinCallbackFunction(It.IsAny<uint>(), It.IsAny<PiGpioInterop.gpioAlertCallback>()),
            Times.Once);
        SpinWait.SpinUntil(() => _gpioAlertCallback != null, 500); // wait up to 500ms
        Assert.NotNull(_gpioAlertCallback);

        _hardwareFixture.UartManagerMock.Setup(u => u.ReadUartMessage(It.IsAny<int>()))
            .Returns("{\"command\":\"ButtonPressed\",\"buttonIndex\":2}");

        // Act
        _gpioAlertCallback!(41, 0, 0);
        await Task.Delay(500);

        // Assert
        _hardwareFixture.MediatorMock.Verify(m =>
                m.Publish(
                    It.Is<ProcessCommandNotification>(c =>
                        c.Command.Type == CommandType.ToggleButtonPressed &&
                        (c.Command as ToggleButtonPressedCommand)!.ButtonIndex == 2), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}