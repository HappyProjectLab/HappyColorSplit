using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

internal static class ExportImageRenderer
{
    public static SKBitmap CreateBitmap(int width, int height, ExportOptions options) => new(new SKImageInfo(
        width, height, NeedsRgba(options) ? SKColorType.Rgba8888 : SKColorType.Gray8,
        NeedsRgba(options) ? SKAlphaType.Unpremul : SKAlphaType.Opaque));

    private static bool NeedsRgba(ExportOptions options) => options.UseGroupColor || options.Background == ExportBackground.Transparent;

    public static void Fill(SKBitmap target, ColorAnalysis analysis, int groupIndex, ExportOptions options,
        CancellationToken token = default, double scale = 1)
    {
        ColorGroup? group = options.UseGroupColor ? analysis.Groups.Single(g => g.Index == groupIndex) : null;
        byte background = options.Background switch
        {
            ExportBackground.Black or ExportBackground.Transparent => 0,
            ExportBackground.Gray => 128,
            ExportBackground.White => 255,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        byte backgroundAlpha = options.Background == ExportBackground.Transparent ? (byte)0 : (byte)255;
        byte red = group?.R ?? 255, green = group?.G ?? 255, blue = group?.B ?? 255;
        bool rgba = NeedsRgba(options), originalSize = scale == 1;
        for (int y = 0; y < target.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            int sourceY = originalSize ? y : Math.Min(analysis.Height - 1, (int)(y / scale));
            var row = target.GetPixelSpan().Slice(y * target.RowBytes, target.RowBytes);
            for (int x = 0; x < target.Width; x++)
            {
                int sourceX = originalSize ? x : Math.Min(analysis.Width - 1, (int)(x / scale));
                bool selected = analysis.Labels[sourceY * analysis.Width + sourceX] == groupIndex;
                if (!rgba) row[x] = selected ? (byte)255 : background;
                else
                {
                    int offset = x * 4;
                    row[offset] = selected ? red : background;
                    row[offset + 1] = selected ? green : background;
                    row[offset + 2] = selected ? blue : background;
                    row[offset + 3] = selected ? (byte)255 : backgroundAlpha;
                }
            }
        }
    }
}
