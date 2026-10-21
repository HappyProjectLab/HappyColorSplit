using HappyColorSplit.Models;
using HappyColorSplit.Services;
using SkiaSharp;

internal static class TestImageExtensions
{
    public static void SaveAsPng(this RasterImage image, string path) => File.WriteAllBytes(path, RasterImageCodec.EncodePng(image));
    public static void SaveAsJpeg(this RasterImage image, string path)
    {
        using var bitmap = SKBitmap.Decode(RasterImageCodec.EncodePng(image)) ?? throw new InvalidDataException("Test image decode failed.");
        using var pixels = bitmap.PeekPixels() ?? throw new InvalidDataException("Test image has no pixels.");
        using var data = pixels.Encode(SKEncodedImageFormat.Jpeg, 90) ?? throw new IOException("Test JPEG encode failed.");
        File.WriteAllBytes(path, data.ToArray());
    }
}
