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
    INotificationHandler<ShowStaticImageNotification>,
    IRequestHandler<ShowSongInfoRequest, bool>,
    INotificationHandler<ShowProgressNotification>,
    INotificationHandler<ShowFrequencyInfoNotification>
{
    private readonly ILogger<DisplayManager> _logger;
    private readonly IHardwareManager _hardwareManager;
    private readonly IGpioManager _gpioManager;
    private readonly ISpiManager _spiManager;
    private readonly Dictionary<char, byte[]> _font;

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
        _font = Font5x7.GetFont();
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
            _logger.LogError($"Static image '{fileName}' not found");
        }

        try
        {
            var bmp = await File.ReadAllBytesAsync(fileName, cancellationToken);
            var imgAsRgb565 = bmp.ToRgb565();
            DrawImage(imgAsRgb565, notification.TopMargin);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error showing static image '{fileName}'");
        }
    }
    
    /// <summary>
    /// Shows Album cover and song name
    /// </summary>
    public async Task<bool> Handle(ShowSongInfoRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Show song info: '{request.SongName}' by '{(request.ArtistName ?? "null")}'. Album cover is {(request.AlbumCoverJpeg == null ? "absent" : "present")}");
        try
        {
            // Clear screen
            await Handle(new ClearScreenNotification(),  cancellationToken);
            
            // Convert and show image
            if (request.AlbumCoverJpeg != null)
            {
                var albumCoverBmp = await request.AlbumCoverJpeg.GetBmpFromJpeg(ScreenGpioParameters.AlbumCoverSizeInPixels);
                var albumCoverRgb565 = albumCoverBmp.ToRgb565();
                DrawImage(albumCoverRgb565, 2);
            }
            
            DrawText(3, 107, request.SongName, ScreenGpioParameters.SongNameColor);
            if (!string.IsNullOrEmpty(request.ArtistName))
            {
                DrawText(3, 117, request.ArtistName, ScreenGpioParameters.AlbumNameColor);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e,"Error showing song info");
        }
        return false;
    }
    
    /// <summary>
    /// Handles Progress bar state notification
    /// </summary>
    public Task Handle(ShowProgressNotification notification, CancellationToken cancellationToken)
    {
        DrawProgress(
            notification.PercentComplete, 
            ScreenGpioParameters.ProgressBarTopPosition, 
            ScreenGpioParameters.ProgressBarHeight, 
            ScreenGpioParameters.ProgressColor,
            ScreenGpioParameters.ProgressBackgroundColor);
        return Task.CompletedTask;
    }
    
    
    /// <summary>
    /// Shows text in the upper right corner
    /// </summary>
    public Task Handle(ShowFrequencyInfoNotification notification, CancellationToken cancellationToken)
    {
        DrawText(60, 2, notification.FrequencyInfo, ScreenGpioParameters.White);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Draws a text by coordinates
    /// </summary>
    /// <param name="x">Coordinate X</param>
    /// <param name="y">Coordinate Y</param>
    /// <param name="text">TextToDraw</param>
    /// <param name="color">Color to draw</param>
    private void DrawText(int x, int y, string text, ushort color)
    {
        foreach (var symbol in text)
        {
            DrawChar(x, y, symbol, color);
            x += 6; // Pixels distance between characters
        }
    }
    
    /// <summary>
    /// Draws one text character
    /// </summary>
    private void DrawChar(int x, int y, char symbol, ushort color) 
    {
        SendCommand(0x2A);  // Set column
        SendData(Convert.ToByte(0x00)); SendData(Convert.ToByte(x));
        SendData(Convert.ToByte(0x00)); SendData(Convert.ToByte(x + 4));

        SendCommand(0x2B);  // Set row
        SendData(Convert.ToByte(0x00)); SendData(Convert.ToByte(y));
        SendData(Convert.ToByte(0x00)); SendData(Convert.ToByte(y + 6));

        SendCommand(0x2C);  // Write pixels

        byte[] bitmapChar = _font.ContainsKey(symbol) ? _font[symbol] : _font['?'];

        for (int line = 0; line < bitmapChar.Length; line++)
        {
            for (int i = 0; i < 5; i++)
            {
                var pixel = bitmapChar[line] & (1 << i);
                if (pixel == 0) {
                    SendData(Convert.ToByte(ScreenGpioParameters.BackgroundColor >> 8));
                    SendData(Convert.ToByte(ScreenGpioParameters.BackgroundColor & 0xFF));
                } else {
                    SendData(Convert.ToByte(color >> 8));
                    SendData(Convert.ToByte(color & 0xFF));
                }
            }
        }
    }

    /// <summary>
    /// Draws a progress line
    /// </summary>
    /// <param name="progress">Progress in percent</param>
    /// <param name="y">Top coordinate to draw</param>
    /// <param name="height">Height of the progress bar</param>
    /// <param name="progressColor">Color of the progress bar</param>
    /// <param name="progressBackgroundColor">Background color of the progress bar</param>
    private void DrawProgress(int progress, int y, int height, ushort progressColor, ushort progressBackgroundColor)
    {
        int margin = 3;
        int x1 = margin;
        int x2 = ScreenGpioParameters.DisplayWidth - margin;

        int progressX2 = (progress * (ScreenGpioParameters.DisplayWidth - 2 * margin)) / 100;

        // Progress:
        if (progressX2 > margin)
        {
            InitDrawArea(x1, y, progressX2 - 1, y + height - 1);
            for (int i = 0; i < height * progressX2; i++)
            {
                DrawPixel(progressColor);
            }
        }

        // Background:
        InitDrawArea(Math.Max(progressX2, margin), y, x2 - 1, y + height - 1);
        for (int i = 0; i < height * (x2 - progressX2); i++)
        {
            DrawPixel(progressBackgroundColor);
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
        lock (_gpioManager)
        {
            _spiManager.SendCommand(_hardwareManager.SpiHandle, ScreenGpioParameters.RsPin, command);
        }
    }

    private void SendData(byte data)
    {
        lock (_gpioManager)
        {
            _spiManager.SendData(_hardwareManager.SpiHandle, ScreenGpioParameters.RsPin, data);
        }
    }

    /// <summary>
    /// Hardware resets display by pulsing LOW to the RES pin
    /// </summary>
    private async Task DisplayHardReset()
    {
        lock (_gpioManager)
        {
            _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.High);
        }
        await Task.Delay(50);
        lock (_gpioManager)
        {
            _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.Low);
        }
        await Task.Delay(50);
        lock (_gpioManager)
        {
            _gpioManager.Write(ScreenGpioParameters.ResPin, GpioLevel.High);
        }
        await Task.Delay(50);
    }
}