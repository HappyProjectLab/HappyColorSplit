using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

internal static class ExportImageRenderer
{
    public static SKBitmap CreateBitmap(int width, int height, ExportOptions options) => new(new SKImageInfo(
        width, height, options.UseGroupColor ? SKColorType.Rgba8888 : SKColorType.Gray8,
        options.UseGroupColor ? SKAlphaType.Unpremul : SKAlphaType.Opaque));

    public static void Fill(SKBitmap target, ColorAnalysis analysis, int groupIndex, ExportOptions options,
        CancellationToken token = default, double scale = 1)
    {
        ColorGroup? group = options.UseGroupColor ? analysis.Groups.Single(g => g.Index == groupIndex) : null;
        for (int y = 0; y < target.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            int sourceY = Math.Min(analysis.Height - 1, (int)(y / scale));
            var row = target.GetPixelSpan().Slice(y * target.RowBytes, target.RowBytes);
            for (int x = 0; x < target.Width; x++)
            {
                int sourceX = Math.Min(analysis.Width - 1, (int)(x / scale));
                bool selected = analysis.Labels[sourceY * analysis.Width + sourceX] == groupIndex;
                if (!options.UseGroupColor) row[x] = selected ? (byte)255 : (byte)0;
                else
                {
                    int offset = x * 4;
                    row[offset] = selected ? group!.R : (byte)0;
                    row[offset + 1] = selected ? group!.G : (byte)0;
                    row[offset + 2] = selected ? group!.B : (byte)0;
                    row[offset + 3] = 255;
                }
            }
        }
    }
}
