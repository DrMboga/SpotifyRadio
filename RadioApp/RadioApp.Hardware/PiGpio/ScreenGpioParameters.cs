namespace RadioApp.Hardware.PiGpio;

public static class ScreenGpioParameters
{
    /// <summary>
    /// SPI0 (CE0)
    /// </summary>
    public const uint SpiChannel = 0;

    /// <summary>
    /// 32MHz
    /// </summary>
    public const uint SpiSpeed = 32000000;

    /// <summary>
    /// D/CX pin
    /// </summary>
    public const uint RsPin = 23;

    /// <summary>
    /// Reset pin
    /// </summary>
    public const uint ResPin = 24;

    /// <summary>
    /// 160 pixels
    /// </summary>
    public const byte DisplayWidth = 0x9F;

    /// <summary>
    /// 128 pixels
    /// </summary>
    public const byte DisplayHeight = 0x7F;

    /// <summary>
    /// Black
    /// </summary>
    public const ushort BackgroundColor = 0x0000;
    
    public const int AlbumCoverSizeInPixels = 96;
    public const ushort SongNameColor = 0x8DF7;
    public const ushort AlbumNameColor = 0x29A8;
    
    public const ushort ProgressColor = 0x8DF7;
    public const ushort ProgressBackgroundColor = 0x29A8;

    public const int ProgressBarTopPosition = 100;
    public const int ProgressBarHeight = 4;
}