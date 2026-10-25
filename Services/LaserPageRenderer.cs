using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

public sealed class LaserPageRenderer
{
    public byte[] Render(LaserPage page, ExportOptions options, CancellationToken token,
        int maxWidth = int.MaxValue, int maxHeight = int.MaxValue, bool showGroups = false)
    {
        double scale = Math.Min(1, Math.Min((double)maxWidth / page.Size.Width, (double)maxHeight / page.Size.Height));
        int width = Math.Max(1, (int)(page.Size.Width * scale)), height = Math.Max(1, (int)(page.Size.Height * scale));
        using var bitmap = showGroups ? new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul))
            : ExportImageRenderer.CreateBitmap(width, height, options);
        RgbaPixel background = options.Background switch
        {
            ExportBackground.Black => new(0, 0, 0), ExportBackground.Gray => new(128, 128, 128),
            ExportBackground.White => new(255, 255, 255), ExportBackground.Transparent => default,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        RgbaPixel foreground = options.Area switch
        {
            ExportArea.Black => new(0, 0, 0), ExportArea.White => new(255, 255, 255),
            ExportArea.Transparent => default, ExportArea.GroupColor => page.Material.Color,
            _ => throw new ArgumentOutOfRangeException(nameof(options))
        };
        bitmap.Erase(new SKColor(background.R, background.G, background.B, background.A));
        bool gray = bitmap.ColorType == SKColorType.Gray8;
        foreach (var placement in page.Placements)
        {
            token.ThrowIfCancellationRequested();
            if (placement.Turns is < 0 or > 3 || placement.X < 0 || placement.Y < 0
                || (long)placement.X + placement.Width > page.Size.Width || (long)placement.Y + placement.Height > page.Size.Height)
                throw new ArgumentOutOfRangeException(nameof(page));
            var part = placement.Part;
            var rows = part.Runs.GroupBy(r => r.Y).ToDictionary(g => g.Key, g => g.OrderBy(r => r.X).ToArray());
            int left = CeilingMap(placement.X, width, page.Size.Width), right = CeilingMap(placement.X + placement.Width, width, page.Size.Width);
            int top = CeilingMap(placement.Y, height, page.Size.Height), bottom = CeilingMap(placement.Y + placement.Height, height, page.Size.Height);
            for (int y = top; y < bottom; y++)
            {
                token.ThrowIfCancellationRequested();
                int localY = (int)(y * (long)page.Size.Height / height) - placement.Y;
                var row = bitmap.GetPixelSpan().Slice(y * bitmap.RowBytes, bitmap.RowBytes);
                for (int x = left; x < right; x++)
                {
                    int localX = (int)(x * (long)page.Size.Width / width) - placement.X;
                    var (px, py) = placement.Turns switch
                    {
                        0 => (localX, localY), 1 => (localY, part.Height - 1 - localX),
                        2 => (part.Width - 1 - localX, part.Height - 1 - localY),
                        _ => (part.Width - 1 - localY, localX)
                    };
                    int nx = (int)(px * (long)part.NativeBounds.Width / part.Width), ny = (int)(py * (long)part.NativeBounds.Height / part.Height);
                    if (!rows.TryGetValue(ny, out var runs) || !Contains(runs, nx)) continue;
                    if (gray) row[x] = foreground.R;
                    else { int offset = x * 4; row[offset] = foreground.R; row[offset + 1] = foreground.G; row[offset + 2] = foreground.B; row[offset + 3] = foreground.A; }
                }
            }
        }
        if (showGroups)
        {
            using var canvas = new SKCanvas(bitmap);
            using var sourcePaint = new SKPaint { Color = new SKColor(125, 211, 252, 190), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            using var dash = SKPathEffect.CreateDash([4, 3], 0);
            using var regionPaint = new SKPaint { Color = new SKColor(255, 194, 92, 180), Style = SKPaintStyle.Stroke, StrokeWidth = 1, PathEffect = dash };
            foreach (var group in page.Placements.GroupBy(p => p.SourceChunk)) DrawBounds(canvas, group, sourcePaint, width, height, page.Size);
            foreach (var group in page.Placements.GroupBy(p => (p.SourceChunk, p.Part.Region))) DrawBounds(canvas, group, regionPaint, width, height, page.Size);
        }
        byte[] png = RasterImageCodec.EncodePng(bitmap);
        return PngResolution.WithDpi(png, page.Settings.Dpi);
    }
    private static int CeilingMap(int value, int target, int full) => (int)((value * (long)target + full - 1) / full);
    private static bool Contains(LaserRun[] runs, int x)
    {
        int lo = 0, hi = runs.Length - 1;
        while (lo <= hi)
        {
            int mid = lo + (hi - lo) / 2; var run = runs[mid];
            if (x < run.X) hi = mid - 1; else if (x >= run.X + run.Length) lo = mid + 1; else return true;
        }
        return false;
    }
    private static void DrawBounds(SKCanvas canvas, IEnumerable<LaserPlacement> positions, SKPaint paint, int width, int height, ImageDimensions size)
    {
        var list = positions.ToArray();
        float x = (float)list.Min(p => p.X) * width / size.Width, y = (float)list.Min(p => p.Y) * height / size.Height;
        float right = (float)list.Max(p => p.X + p.Width) * width / size.Width, bottom = (float)list.Max(p => p.Y + p.Height) * height / size.Height;
        canvas.DrawRect(new SKRect(x + .5f, y + .5f, right - .5f, bottom - .5f), paint);
    }
}
