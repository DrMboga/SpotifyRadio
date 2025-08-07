using RadioApp.Hardware.Model;

namespace RadioApp.Hardware.Helpers;

internal record ColorArgb(byte Red, byte Green, byte Blue);

public static class BmpHelper
{
    /// <summary>
    /// Method converts bitmap data to the matrix of pixels in RGB565 format
    /// </summary>
    /// <param name="initialData"> Represents a bitmap file.
    /// BMP has several different formats of storing pixels.
    /// The format depends on `BitsPerPixel` parameter which is stored in the 28-th index of BMP byte array
    /// In case of 24 bits per pixel:
    /// <ul>
    ///     <li> First 54 bytes contain metadata </li>
    ///     <li> Each pixel is stored as BGR (Blue, Green, Red) </li>
    ///     <li> Rows are stored bottom-up (last row in file = first row on display) </li>
    ///     <li> Each row is padded to a multiple of 4 bytes </li>
    /// </ul>
    /// In case of 8 bits per pixel:
    /// <ul>
    ///     <li> First 54 bytes contain metadata (14 bytes header and 40 bytes DIB header) </li>
    ///     <li> From 54-th byte and until `Data Offset` - the color palette. Which is representation of all colors used in the BMP. It is padded to a multiple of 4 bytes</li>
    ///     <li> After `Data Offset` index are the pixels. Each pixel is one byte which is the index referenced to the color palette. </li>
    ///     <li> Rows are stored bottom-up (last row in file = first row on display) </li>
    /// </ul>
    /// </param>
    /// <returns>Data structure contains an array of pixels in direct order. Each pixel represents an RGB code for pixel color</returns>
    public static BmpRgb565Data ToRgb565(this byte[] initialData)
    {
        var rgbData = new BmpRgb565Data();
        if (initialData.Length < 55)
        {
            return rgbData;
        }

        // First 54 bytes contain metadata:
        // Pixel data offset: Stored at 0x0A (10 bytes from start)
        // It stored as 4 bytes, that means it takes 10-14 elements. So, BitConverter.ToInt32 does the right trick to read whole number
        rgbData.DataOffset = BitConverter.ToInt32(initialData, 10);
        // Width: Stored at offset 0x12 (18 bytes from start)
        rgbData.Width = BitConverter.ToInt32(initialData, 18); // 4 bytes
        // Height: Stored at offset 0x16 (22 bytes from start)
        rgbData.Height = BitConverter.ToInt32(initialData, 22); // 4 bytes
        // Bits per pixel (bpp): Stored at 0x1C (28 bytes from start)
        rgbData.BitsPerPixel = BitConverter.ToUInt16(initialData, 28); // 2 bytes

        int bytesPerPixel = rgbData.BitsPerPixel / 8;
        // In case of 3 bytes per pixel, each row is padded to a multiple of 4 bytes. In case of 1 byte per pixel, row size is equal to image width
        rgbData.RowSize = ((rgbData.Width * bytesPerPixel + 3) / 4) * 4;

        switch (rgbData.BitsPerPixel)
        {
            case 24:
                // After offset, bitmap array contains bitmap information.
                rgbData.Rgb565Pixels = Convert24BitBmpToRgb565(initialData, rgbData.DataOffset, rgbData.Width,
                    rgbData.Height, rgbData.RowSize);
                break;
            case 8:
                rgbData.Rgb565Pixels = Convert8BitBmpToRgb565(initialData, rgbData.DataOffset, rgbData.Width,
                    rgbData.Height, rgbData.RowSize);
                break;
            default:
                throw new NotSupportedException(
                    $"BMP has {rgbData.BitsPerPixel} bits per pixel which is not supported yet.");
        }

        return rgbData;
    }

    /// <summary>
    /// Algorithm expects 24-bit BMP = 3 bytes per pixel (BGR).
    /// </summary>
    private static ushort[] Convert24BitBmpToRgb565(byte[] initialData, int dataOffset, int width, int height,
        int rowSize)
    {
        var rgb565Pixels = new ushort[width * height];

        // Iterating over the initial bmp data.
        // The array just contains a sequential set of pixels, but we know that every "rowSize" begins a new row
        // And these rows are bottom up.
        // So, we should read the first "rowSize" items from initial array, 
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

    /// <summary>
    /// Algorithm expects 8-bit BMP = 1 byte per pixel (address of the color in the palette defined in the header).
    /// In case of 8-bit, there is a color palette stored in the beginning of the file (limited by the dataOffset).
    /// And the bitmap array itself contains only one byte - index to the color palette
    /// </summary>
    private static ushort[] Convert8BitBmpToRgb565(byte[] initialData, int dataOffset, int width, int height,
        int rowSize)
    {
        var rgb565Pixels = new ushort[width * height];
        
        // Find the palette place in the bitmap array.
        // Palette starts after the bmp header (first 14bytes) and DIB header (which size is defined in bytes 14–17)
        int dibHeaderSize = BitConverter.ToInt32(initialData, 14); // bytes 14–17
        int paletteStart = 14 + dibHeaderSize;
        // And ends in the end of dataOffset
        int paletteSize = dataOffset - paletteStart;
        int paletteEntries = paletteSize / 4;
        // Read palette.
        var palette = new ColorArgb[paletteEntries];
        for (int i = 0; i < paletteEntries; i++)
        {
            byte blue = initialData[paletteStart + i * 4];
            byte green = initialData[paletteStart + i * 4 + 1];
            byte red = initialData[paletteStart + i * 4 + 2];
            palette[i] = new ColorArgb(red, green, blue);
        }

        // Iterating over the initial bmp data.
        // The array just contains a sequential set of pixels, but we know that every "rowSize" begins a new row
        // And these rows are bottom up.
        // So, we should read the first "rowSize" items from initial array, 
        // Convert them to set of rgb pixels,
        // and write this row to the end of destination array
        // Amount of rows in both arrays should match the Height of the image
        // But in this case, each pixel in BMP is an INDEX to the colors palette
        for (int row = 0; row < height; row++)
        {
            var rowOffset = row * rowSize;
            for (int col = 0; col < width; col++)
            {
                int bmpIndex = col + rowOffset + dataOffset;
                int paletteIndex = initialData[bmpIndex];
                if (paletteIndex >= paletteEntries)
                {
                    throw new IndexOutOfRangeException(
                        $"Palette index {paletteIndex} is greater than palette entries {paletteEntries}. Bmp row {row}, column {col}. Bmp index {bmpIndex}");
                }

                var color = palette[paletteIndex];

                // Converting
                ushort rgbColor = ConvertToRgb565(color.Blue, color.Green, color.Red);

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