using SkiaSharp;

namespace HappyColorSplit.Models;

public readonly record struct RgbaPixel(byte R, byte G, byte B, byte A = 255);

/// <summary>Owns straight-alpha RGBA pixels. Color classification must not use premultiplied RGB.</summary>
public sealed class RasterImage : IDisposable
{
    private SKBitmap? _bitmap;
    internal SKBitmap Bitmap => _bitmap ?? throw new ObjectDisposedException(nameof(RasterImage));
    public int Width => Bitmap.Width;
    public int Height => Bitmap.Height;

    public RasterImage(int width, int height, RgbaPixel fill = default)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        _bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        if (_bitmap.GetPixels() == IntPtr.Zero) { _bitmap.Dispose(); _bitmap = null; throw new OutOfMemoryException(); }
        _bitmap.GetPixelSpan().Clear();
        if (fill != default)
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) this[x, y] = fill;
    }

    public Span<byte> GetRowSpan(int y)
    {
        if ((uint)y >= (uint)Height) throw new ArgumentOutOfRangeException(nameof(y));
        return Bitmap.GetPixelSpan().Slice(y * Bitmap.RowBytes, Width * 4);
    }

    public RgbaPixel this[int x, int y]
    {
        get
        {
            if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
            var pixel = GetRowSpan(y).Slice(x * 4, 4);
            return new(pixel[0], pixel[1], pixel[2], pixel[3]);
        }
        set
        {
            if ((uint)x >= (uint)Width) throw new ArgumentOutOfRangeException(nameof(x));
            var pixel = GetRowSpan(y).Slice(x * 4, 4);
            pixel[0] = value.R; pixel[1] = value.G; pixel[2] = value.B; pixel[3] = value.A;
        }
    }

    public void Dispose() { _bitmap?.Dispose(); _bitmap = null; }
}
