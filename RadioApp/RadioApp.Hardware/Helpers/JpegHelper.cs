using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RadioApp.Hardware.Helpers;

public static class JpegHelper
{
    /// <summary>
    /// Converts Jpeg to Bmp format
    /// </summary>
    public static async Task<byte[]> GetBmpFromJpeg(this byte[] jpegData, int desiredHeight)
    {
        using var image = Image.Load(jpegData);
        decimal ratio = (decimal)desiredHeight / image.Height;
        int desiredWidth = Convert.ToInt32(ratio * image.Width);

        image.Mutate(c => c.Resize(desiredWidth, desiredHeight));

        using var stream = new MemoryStream();
        await image.SaveAsBmpAsync(stream);

        return stream.ToArray();
    }
}