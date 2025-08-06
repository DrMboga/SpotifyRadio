using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

/// <summary>
/// Represents TFT 160x128 SPI display interaction
/// </summary>
public class DisplayManager : INotificationHandler<InitDisplayNotification>,
    INotificationHandler<ClearScreenNotification>
{
    private readonly ILogger<DisplayManager> _logger;
    private readonly IHardwareManager _hardwareManager;
    private readonly IGpioManager _gpioManager;
    private readonly ISpiManager _spiManager;

    public DisplayManager(
        ILogger<DisplayManager> logger,
        IHardwareManager hardwareManager,
        IGpioManager gpioManager,
        ISpiManager spiManager)
    {
        _logger = logger;
        _hardwareManager = hardwareManager;
        _gpioManager = gpioManager;
        _spiManager = spiManager;
    }

    /// <summary>
    /// Init display
    /// </summary>
    public async Task Handle(InitDisplayNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Init display");

        await DisplayHardReset();
        SendCommand(0x11); // Sleep Out
        await Task.Delay(120, cancellationToken); // 120ms delay

        SendCommand(0x3A); // Set color mode
        SendData(0x05); // 16-bit color (RGB565)

        SendCommand(0x36);
        SendData(0xA0); // Rotate 90° (landscape, RGB)

        SendCommand(0x29); // Display On
    }

    /// <summary>
    /// Clear screen
    /// </summary>
    public Task Handle(ClearScreenNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Clear screen");
        const ushort color = ScreenGpioParameters.BackgroundColor;
        SendCommand(0x2A); // Column address set
        SendData(0x00); SendData(0x00); // X start
        SendData(0x00); SendData(ScreenGpioParameters.DisplayWidth); // X end (160 pixels)

        SendCommand(0x2B); // Row address set
        SendData(0x00); SendData(0x00); // Y start
        SendData(0x00); SendData(ScreenGpioParameters.DisplayHeight); // Y end (128 pixels)

        SendCommand(0x2C); // Write Memory Start
        for (int i = 0; i < (ScreenGpioParameters.DisplayWidth + 1) * (ScreenGpioParameters.DisplayHeight + 1); i++)
        {
            SendData(Convert.ToByte(color >> 8));
            SendData(Convert.ToByte(color & 0xFF));
        }

        return Task.CompletedTask;
    }

    private void SendCommand(byte command)
    {
        _spiManager.SendCommand(_hardwareManager.SpiHandle, ScreenGpioParameters.RsPin, command);
    }

    private void SendData(byte data)
    {
        _spiManager.SendData(_hardwareManager.SpiHandle, ScreenGpioParameters.RsPin, data);
    }

    /// <summary>
    /// Hardware resets display by pulsing LOW to the RES pin
    /// </summary>
    private async Task DisplayHardReset()
    {
        _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.High);
        await Task.Delay(50);
        _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.Low);
        await Task.Delay(50);
        _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.High);
        await Task.Delay(50);
    }
}