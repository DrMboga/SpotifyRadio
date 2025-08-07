using RadioApp.Hardware.Model;

namespace RadioApp.Hardware.Helpers;

public static class BmpHelper
{
    /// <summary>
    /// Method converts bitmap data to the matrix of pixels in RGB565 format
    /// </summary>
    /// <param name="initialData"> Represents a bitmap file. It should contain:
    /// <ul>
    ///     <li> First 54 bytes contain metadata </li>
    ///     <li> Each pixel is stored as BGR (Blue, Green, Red) </li>
    ///     <li> Rows are stored bottom-up (last row in file = first row on display) </li>
    ///     <li> Each row is padded to a multiple of 4 bytes </li>
    /// </ul>
    /// </param>
    /// <returns>Data structure contains an array of pixels in direct order. Each pixel represents an RGB code for pixel color</returns>
    /// <exception cref="ApplicationException">Throws an exception in case of wrong incoming bmp format</exception>
    public static BmpRgb565Data ToRgb565(this byte[] initialData)
    {
        var rgbData = new BmpRgb565Data();
        if (initialData.Length < 55)
        {
            return rgbData;
        }

        // First 54 bytes contain metadata:
        // Pixel data offset: Stored at 0x0A (10 bytes from start)
        rgbData.DataOffset = initialData[10];
        // Width: Stored at offset 0x12 (18 bytes from start)
        rgbData.Width = initialData[18];
        // Height: Stored at offset 0x16 (22 bytes from start)
        rgbData.Height = initialData[22];
        // Bits per pixel (bpp): Stored at 0x1C (28 bytes from start)
        rgbData.BytesPerPixel = initialData[28];

        switch (rgbData.BytesPerPixel)
        {
            case 24:
                // After offset of 54 elements, bitmap array contains bitmap information.
                // Each pixel is stored as 3 sequential items - BGR (Blue, Green, Red).
                // Each row is padded to a multiple of 4 bytes.
                rgbData.RowSize = Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(rgbData.Width) * 3 / 4)) * 4;
                rgbData.Rgb565Pixels = Convert24BitBmpToRgb565(initialData, rgbData.DataOffset,  rgbData.Width, rgbData.Height, rgbData.RowSize);
                break;
            case 8:
                // TODO Define
                break;
        }

        return rgbData;
    }

    /// <summary>
    /// Algorithm expects 24-bit BMP = 3 bytes per pixel (BGR).
    /// </summary>
    private static ushort[] Convert24BitBmpToRgb565(byte[] initialData, int dataOffset, int width, int height, int rowSize)
    {
        var rgb565Pixels = new ushort[width * height];

        // Iterating over the initial bmp data.
        // The array just contains a sequential set of pixels, but we know that every rgbData.RowSize begins a new row
        // And these rows are bottom up.
        // So, we should read the first rgbData.RowSize items from initial array, 
        // Convert them to set of rgb pixels,
        // and write this row to the end of destination array
        // Amount of rows in both arrays should match the Height of the image
        for (int row = 0; row < height; row++)
        {
            var rowOffset = row * rowSize;
            for (int col = 0; col < width; col++)
            {
                int index = col * 3 + rowOffset + dataOffset;
                // Reading 3 items of initial bitmap array,
                byte blue = initialData[index];
                byte green = initialData[index + 1];
                byte red = initialData[index + 2];

                // Converting
                ushort rgbColor = ConvertToRgb565(blue, green, red);

                var pixelIndex = (height - 1 - row) * width + col;
                rgb565Pixels[pixelIndex] = rgbColor;
            }
        }

        return rgb565Pixels;
    }

    private static ushort ConvertToRgb565(byte blue, byte green, byte red)
    {
        // red & 0xF8 → Extracts the top 5 bits of the red component.
        // green & 0xFC → Extracts the top 6 bits of the green component.
        // blue >> 3 → Extracts the top 5 bits of the blue component.
        // Combines the bits into a ushort (16-bit value) for RGB565 format.
        return (ushort)(((red & 0xF8) << 8) | ((green & 0xFC) << 3) | (blue >> 3));
    }
}