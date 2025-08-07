using MediatR;
using Microsoft.Extensions.Logging;
using RadioApp.Common.Hardware;
using RadioApp.Common.Messages.Hardware.Display;
using RadioApp.Hardware.Helpers;
using RadioApp.Hardware.Model;
using RadioApp.Hardware.PiGpio;

namespace RadioApp.Hardware;

/// <summary>
/// Represents TFT 160x128 SPI display interaction
/// </summary>
public class DisplayManager : INotificationHandler<InitDisplayNotification>,
    INotificationHandler<ClearScreenNotification>,
    INotificationHandler<ShowStaticImageNotification>
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

    /// <summary>
    /// Loads a bmp file from Assets folder and shows it on the display
    /// </summary>
    public async Task Handle(ShowStaticImageNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Show static image '{notification.AssetName}'");
        var fileName = Path.Combine(Environment.CurrentDirectory, "Assets", notification.AssetName);

        if (!File.Exists(fileName))
        {
            _logger.LogError("Static image '{fileName}' not found");
        }

        try
        {
            var bmp = await File.ReadAllBytesAsync(fileName, cancellationToken);
            var imgAsRgb565 = bmp.ToRgb565();
            DrawImage(imgAsRgb565, notification.TopMargin);
        }
        catch (Exception e)
        {
            _logger.LogError("Error showing static image '{fileName}'", e);
        }
    }

    /// <summary>
    /// Draws an image. Data should be converted to RGB 565 pixels array
    /// </summary>
    private void DrawImage(BmpRgb565Data imageData, int topMargin)
    {
        if (imageData.Rgb565Pixels == null || imageData.Rgb565Pixels.Length == 0)
        {
            return;
        }

        int y1 = topMargin; // Top margin
        int y2 = y1 + imageData.Height - 1;
        // Placing a picture in the middle of the screen
        int x1 = (ScreenGpioParameters.DisplayWidth - imageData.Width) / 2;
        int x2 = x1 + imageData.Width - 1;

        InitDrawArea(x1, y1, x2, y2);

        for (int i = 0; i < imageData.Rgb565Pixels.Length; i++)
        {
            DrawPixel(imageData.Rgb565Pixels[i]);
        }
    }

    /// <summary>
    /// Sends to display memory a rectangle on the screen which should be filled 
    /// consequently pixel by pixel from left to right from top to bottom
    /// </summary>
    private void InitDrawArea(int x0, int y0, int x1, int y1)
    {
        SendCommand(0x2A); // Column address set
        SendData(0x00); SendData(Convert.ToByte(x0)); // X start
        SendData(0x00); SendData(Convert.ToByte(x1)); // X end

        SendCommand(0x2B); // Row address set
        SendData(0x00); SendData(Convert.ToByte(y0)); // Y start
        SendData(0x00); SendData(Convert.ToByte(y1)); // Y end

        SendCommand(0x2C);
    }

    /// <summary>
    /// Draws a pixel
    /// </summary>
    private void DrawPixel(ushort color)
    {
        SendData(Convert.ToByte(color >> 8));
        SendData(Convert.ToByte(color & 0xFF));
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