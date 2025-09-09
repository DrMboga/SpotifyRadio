namespace RadioApp.Hardware.Helpers;

public static class ImageHelper
{
    public static async Task<byte[]> GetBmpImageFromBase64(this string base64Image, int desiredHeight)
    {
        int commaIndex = base64Image.IndexOf(',');
        if (commaIndex < 0)
        {
            return [];
        }
        string imageData =  base64Image.Substring(commaIndex + 1);
        var imageBytes = Convert.FromBase64String(imageData);
        return await imageBytes.GetBmpFromJpeg(desiredHeight);
    }
}