namespace RadioApp.Hardware.Model;

/// <summary>
/// Represents a matrix of pixels in RGB565 format
/// </summary>
public class BmpRgb565Data
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int BytesPerPixel { get; set; }
    public int DataOffset { get; set; }
    public ushort[]? Rgb565Pixels { get; set; }
    public int RowSize { get; set; }
}