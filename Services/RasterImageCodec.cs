using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

public static class RasterImageCodec
{
    public static RasterImage Load(string path)
    {
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream) ?? throw new InvalidDataException("Invalid or unsupported image.");
        return Decode(codec);
    }

    public static RasterImage Load(byte[] bytes)
    {
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data) ?? throw new InvalidDataException("Invalid or unsupported image.");
        return Decode(codec);
    }

    private static RasterImage Decode(SKCodec codec)
    {
        if (codec.EncodedFormat is not (SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg))
            throw new UnsupportedImageFormatException();
        int width = codec.Info.Width, height = codec.Info.Height;
        if (width <= 0 || height <= 0) throw new InvalidDataException("Invalid image dimensions.");
        if ((long)width * height > ColorSeparationService.MaxPixels) throw new ImageLimitException();
        var image = new RasterImage(width, height);
        try
        {
            // Decode the first frame directly to straight alpha, retaining faint RGB values.
            var result = codec.GetPixels(image.Bitmap.Info, image.Bitmap.GetPixels(), image.Bitmap.RowBytes, new SKCodecOptions(0));
            if (result != SKCodecResult.Success) throw new InvalidDataException($"Image decoding failed: {result}.");
            if (codec.EncodedOrigin == SKEncodedOrigin.TopLeft) return image;
            var oriented = ApplyOrientation(image, codec.EncodedOrigin);
            image.Dispose();
            return oriented;
        }
        catch { image.Dispose(); throw; }
    }

    private static RasterImage ApplyOrientation(RasterImage source, SKEncodedOrigin origin)
    {
        bool swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var target = new RasterImage(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
        try
        {
            for (int y = 0; y < source.Height; y++)
            {
                var sourceRow = source.GetRowSpan(y);
                for (int x = 0; x < source.Width; x++)
                {
                    var (dx, dy) = origin switch
                    {
                        SKEncodedOrigin.TopRight => (source.Width - 1 - x, y),
                        SKEncodedOrigin.BottomRight => (source.Width - 1 - x, source.Height - 1 - y),
                        SKEncodedOrigin.BottomLeft => (x, source.Height - 1 - y),
                        SKEncodedOrigin.LeftTop => (y, x),
                        SKEncodedOrigin.RightTop => (source.Height - 1 - y, x),
                        SKEncodedOrigin.RightBottom => (source.Height - 1 - y, source.Width - 1 - x),
                        SKEncodedOrigin.LeftBottom => (y, source.Width - 1 - x),
                        _ => (x, y)
                    };
                    sourceRow.Slice(x * 4, 4).CopyTo(target.GetRowSpan(dy).Slice(dx * 4, 4));
                }
            }
            return target;
        }
        catch { target.Dispose(); throw; }
    }

    public static byte[] EncodePng(RasterImage image) => EncodePng(image.Bitmap);

    internal static byte[] EncodePng(SKBitmap bitmap)
    {
        // Encode the pixmap directly; a drawing surface could premultiply low-alpha RGB.
        using var pixels = bitmap.PeekPixels() ?? throw new IOException("Image has no pixels.");
        using var data = pixels.Encode(SKEncodedImageFormat.Png, 100) ?? throw new IOException("PNG encoding failed.");
        return data.ToArray();
    }
}

public sealed class UnsupportedImageFormatException : Exception;
