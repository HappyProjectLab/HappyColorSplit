using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

internal static class ExportImageRenderer
{
    public static SKBitmap CreateBitmap(int width, int height, ExportOptions options) => new(new SKImageInfo(
        width, height, NeedsRgba(options) ? SKColorType.Rgba8888 : SKColorType.Gray8,
        NeedsRgba(options) ? SKAlphaType.Unpremul : SKAlphaType.Opaque));

    private static bool NeedsRgba(ExportOptions options) => options.Area is ExportArea.GroupColor or ExportArea.Transparent
        || options.Background == ExportBackground.Transparent;

    public static void Fill(SKBitmap target, ColorAnalysis analysis, int groupIndex, ExportOptions options,
        CancellationToken token = default, ImageDimensions? exportSize = null)
        => FillCore(target, analysis, groupIndex, options, token, exportSize, null);

    public static void FillCombined(SKBitmap target, ColorAnalysis analysis, IReadOnlySet<int> selectedGroups, ExportOptions options,
        CancellationToken token = default, ImageDimensions? exportSize = null)
        => FillCore(target, analysis, -1, options, token, exportSize, selectedGroups);

    private static void FillCore(SKBitmap target, ColorAnalysis analysis, int groupIndex, ExportOptions options,
        CancellationToken token, ImageDimensions? exportSize, IReadOnlySet<int>? combinedGroups)
    {
        ColorGroup? group = options.Area == ExportArea.GroupColor && combinedGroups is null ? analysis.Groups.Single(g => g.Index == groupIndex) : null;
        var palette = options.Area == ExportArea.GroupColor && combinedGroups is not null ? analysis.Groups.ToDictionary(g => g.Index) : null;
        byte background = options.Background switch
        {
            ExportBackground.Black or ExportBackground.Transparent => 0,
            ExportBackground.Gray => 128,
            ExportBackground.White => 255,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        byte backgroundAlpha = options.Background == ExportBackground.Transparent ? (byte)0 : (byte)255;
        RgbaPixel area = options.Area switch
        {
            ExportArea.Black => new(0, 0, 0),
            ExportArea.White => new(255, 255, 255),
            ExportArea.Transparent => new(0, 0, 0, 0),
            ExportArea.GroupColor => group is not null ? new(group.R, group.G, group.B) : new(0, 0, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        bool rgba = NeedsRgba(options);
        var size = exportSize ?? new ImageDimensions(analysis.Width, analysis.Height);
        var smoothing = options.SmoothContours ? new HardContourSampler(analysis, analysis.Groups
            .ToDictionary(g => g.Index, g => combinedGroups is null || combinedGroups.Contains(g.Index) ? g.Index : -1)) : null;
        bool originalSize = target.Width == analysis.Width && target.Height == analysis.Height
            && size.Width == analysis.Width && size.Height == analysis.Height;
        for (int y = 0; y < target.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            int sourceY = originalSize ? y : ImageResampling.Source(y, target.Height, size.Height, analysis.Height);
            var row = target.GetPixelSpan().Slice(y * target.RowBytes, target.RowBytes);
            for (int x = 0; x < target.Width; x++)
            {
                int sourceX = originalSize ? x : ImageResampling.Source(x, target.Width, size.Width, analysis.Width);
                int label = smoothing is null ? analysis.Labels[sourceY * analysis.Width + sourceX]
                    : smoothing.Sample((int)(x * (long)size.Width / target.Width), (int)(y * (long)size.Height / target.Height), size.Width, size.Height);
                // Union of transparent-background, black-foreground masks first; style only afterwards.
                bool selected = label >= 0 && (combinedGroups?.Contains(label) ?? label == groupIndex);
                RgbaPixel foreground = area;
                if (selected && palette is not null)
                {
                    var color = palette[label]; foreground = new(color.R, color.G, color.B);
                }
                if (!rgba) row[x] = selected ? foreground.R : background;
                else
                {
                    int offset = x * 4;
                    row[offset] = selected ? foreground.R : background;
                    row[offset + 1] = selected ? foreground.G : background;
                    row[offset + 2] = selected ? foreground.B : background;
                    row[offset + 3] = selected ? foreground.A : backgroundAlpha;
                }
            }
        }
    }
}
