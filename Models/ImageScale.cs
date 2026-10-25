namespace HappyColorSplit.Models;

public readonly record struct ImageDimensions(int Width, int Height);

public sealed record ImageScale(decimal Percent = 100, int Dpi = 300)
{
    public const long MaxPixels = 16_000_000;

    public ImageDimensions GetSize(int sourceWidth, int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 || Percent <= 0 || Dpi is < 1 or > 2400)
            throw new ArgumentOutOfRangeException(nameof(Percent));
        // Bound before multiplying or allocating, including extremely large numeric input.
        if (Percent > (decimal)MaxPixels * 100 / Math.Max(sourceWidth, sourceHeight)) throw new ExportSizeLimitException();
        int width = Math.Max(1, (int)decimal.Round(sourceWidth * Percent / 100, 0, MidpointRounding.AwayFromZero));
        int height = Math.Max(1, (int)decimal.Round(sourceHeight * Percent / 100, 0, MidpointRounding.AwayFromZero));
        if ((long)width * height > MaxPixels) throw new ExportSizeLimitException();
        return new(width, height);
    }

    public decimal WidthCm(int sourceWidth) => sourceWidth * Percent / 100 * 2.54m / Dpi;
    public decimal HeightCm(int sourceHeight) => sourceHeight * Percent / 100 * 2.54m / Dpi;
}

public sealed class ExportSizeLimitException : Exception;
