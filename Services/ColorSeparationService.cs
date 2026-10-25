using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

public sealed class ColorSeparationService
{
    public const long MaxPixels = ImageScale.MaxPixels;
    public const int MaxGroups = 256;

    public RasterImage Load(string path) => RasterImageCodec.Load(path);

    public ColorAnalysis Analyze(RasterImage image, double tolerance, CancellationToken token, byte minimumAlpha = 1)
    {
        var histogram = new Dictionary<int, int>();
        for (int y = 0; y < image.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            var row = image.GetRowSpan(y);
            for (int x = 0; x < row.Length; x += 4)
            {
                if (row[x + 3] == 0 || row[x + 3] < minimumAlpha) continue;
                int rgb = row[x] << 16 | row[x + 1] << 8 | row[x + 2];
                histogram.TryGetValue(rgb, out int count);
                histogram[rgb] = count + 1;
            }
        }
        if (histogram.Count == 0) throw new EmptyImageException();
        var palette = new List<(int R, int G, int B)>();
        var counts = new List<int>();
        var assignments = new Dictionary<int, int>(histogram.Count);
        // Fixed seeds: frequent colors win; ties use RGB order for reproducible exports.
        foreach (var item in histogram.OrderByDescending(x => x.Value).ThenBy(x => x.Key))
        {
            token.ThrowIfCancellationRequested();
            int r = item.Key >> 16, g = item.Key >> 8 & 255, b = item.Key & 255;
            int best = -1;
            double nearest = tolerance * tolerance;
            for (int i = 0; i < palette.Count; i++)
            {
                var seed = palette[i];
                int d = (r - seed.R) * (r - seed.R) + (g - seed.G) * (g - seed.G) + (b - seed.B) * (b - seed.B);
                if (d <= nearest && (best < 0 || d < nearest)) { nearest = d; best = i; }
            }
            if (best < 0)
            {
                if (palette.Count >= MaxGroups) throw new ColorLimitException();
                best = palette.Count;
                palette.Add((r, g, b));
                counts.Add(0);
            }
            counts[best] += item.Value;
            assignments.Add(item.Key, best);
        }
        var labels = new int[checked(image.Width * image.Height)];
        for (int y = 0; y < image.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            var row = image.GetRowSpan(y);
            for (int x = 0; x < image.Width; x++)
            {
                int offset = x * 4;
                byte alpha = row[offset + 3];
                labels[y * image.Width + x] = alpha == 0 || alpha < minimumAlpha ? -1
                    : assignments[row[offset] << 16 | row[offset + 1] << 8 | row[offset + 2]];
            }
        }
        var groups = palette.Select((p, i) => new ColorGroup(i, (byte)p.R, (byte)p.G, (byte)p.B, counts[i])).ToArray();
        return new(image.Width, image.Height, labels, groups);
    }

    public byte[] OriginalPreview(RasterImage source, int width = 1100, int height = 750, ImageScale? sizing = null)
    {
        if (sizing is not null)
        {
            var size = sizing.GetSize(source.Width, source.Height);
            double previewScale = Math.Min(1, Math.Min((double)width / size.Width, (double)height / size.Height));
            using var resized = new RasterImage(Math.Max(1, (int)(size.Width * previewScale)), Math.Max(1, (int)(size.Height * previewScale)));
            for (int y = 0; y < resized.Height; y++)
            {
                int sourceY = ImageResampling.Source(y, resized.Height, size.Height, source.Height);
                var sourceRow = source.GetRowSpan(sourceY);
                var row = resized.GetRowSpan(y);
                for (int x = 0; x < resized.Width; x++)
                {
                    int sourceX = ImageResampling.Source(x, resized.Width, size.Width, source.Width);
                    sourceRow.Slice(sourceX * 4, 4).CopyTo(row.Slice(x * 4, 4));
                }
            }
            return RasterImageCodec.EncodePng(resized);
        }
        double scale = Math.Min(1, Math.Min((double)width / source.Width, (double)height / source.Height));
        if (scale == 1) return RasterImageCodec.EncodePng(source);
        using var preview = new RasterImage(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
        if (!source.Bitmap.ScalePixels(preview.Bitmap, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None)))
            throw new IOException("Image resizing failed.");
        return RasterImageCodec.EncodePng(preview);
    }

    public byte[] MaskPreview(ColorAnalysis analysis, int group, bool fullResolution = false, ExportOptions? options = null,
        ImageScale? sizing = null)
    {
        options ??= new();
        // Nearest-neighbor sampling bounds memory and retains hard mask edges.
        var size = sizing?.GetSize(analysis.Width, analysis.Height) ?? new(analysis.Width, analysis.Height);
        double scale = fullResolution ? 1 : Math.Min(1, Math.Min(1100.0 / size.Width, 750.0 / size.Height));
        int width = Math.Max(1, (int)(size.Width * scale)), height = Math.Max(1, (int)(size.Height * scale));
        using var mask = ExportImageRenderer.CreateBitmap(width, height, options);
        ExportImageRenderer.Fill(mask, analysis, group, options, exportSize: size);
        return RasterImageCodec.EncodePng(mask);
    }

    public byte[] OverlayPreview(ColorAnalysis analysis, int group, CancellationToken token, ImageScale? sizing = null)
        => OverlayPreview(analysis, new HashSet<int> { group }, token, sizing);

    public byte[] OverlayPreview(ColorAnalysis analysis, IReadOnlySet<int> groups, CancellationToken token, ImageScale? sizing = null)
    {
        var size = sizing?.GetSize(analysis.Width, analysis.Height) ?? new(analysis.Width, analysis.Height);
        using var overlay = new RasterImage(size.Width, size.Height);
        for (int y = 0; y < size.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            var row = overlay.GetRowSpan(y);
            int sourceY = ImageResampling.Source(y, size.Height, size.Height, analysis.Height);
            for (int x = 0; x < size.Width; x++)
            {
                int sourceX = ImageResampling.Source(x, size.Width, size.Width, analysis.Width);
                if (!groups.Contains(analysis.Labels[sourceY * analysis.Width + sourceX])) continue;
                int offset = x * 4;
                row[offset] = 80; row[offset + 1] = 255; row[offset + 2] = 120; row[offset + 3] = 255;
            }
        }
        return RasterImageCodec.EncodePng(overlay);
    }

    public string Export(ColorAnalysis analysis, string parent, string sourceName, IProgress<int>? progress, CancellationToken token,
        IReadOnlySet<int>? selectedGroups = null, ExportOptions? options = null, ImageScale? sizing = null, bool combineGroups = false)
    {
        token.ThrowIfCancellationRequested();
        options ??= new();
        var size = sizing?.GetSize(analysis.Width, analysis.Height) ?? new(analysis.Width, analysis.Height);
        var groups = analysis.Groups.Where(g => selectedGroups is null || selectedGroups.Contains(g.Index)).ToArray();
        if (groups.Length == 0) throw new NoColorsSelectedException();
        string name = Path.GetFileNameWithoutExtension(sourceName);
        string suffix = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        string final = Path.Combine(parent, $"{name}_HappyColorSplit_{suffix}");
        string staging = Path.Combine(parent, $".HappyColorSplit_{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(staging);
        try
        {
            using var mask = ExportImageRenderer.CreateBitmap(size.Width, size.Height, options);
            void Save(string fileName)
            {
                byte[] png = RasterImageCodec.EncodePng(mask);
                if (sizing is not null) png = PngResolution.WithDpi(png, sizing.Dpi);
                File.WriteAllBytes(Path.Combine(staging, fileName), png);
            }
            int completed = 0;
            if (combineGroups)
            {
                ExportImageRenderer.FillCombined(mask, analysis, groups.Select(g => g.Index).ToHashSet(), options, token, size);
                Save($"{name}_merged.png"); progress?.Report(100);
            }
            else foreach (var group in groups)
            {
                token.ThrowIfCancellationRequested();
                ExportImageRenderer.Fill(mask, analysis, group.Index, options, token, size);
                Save($"{name}_{group.Index + 1:D3}_{group.Hex[1..]}.png");
                progress?.Report(++completed * 100 / groups.Length);
            }
            token.ThrowIfCancellationRequested();
            Directory.Move(staging, final);
            return final;
        }
        finally
        {
            // This directory is uniquely created by this invocation; never remove the destination.
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    public byte[] CombinedPreview(ColorAnalysis analysis, IReadOnlySet<int> groups, ExportOptions? options = null,
        ImageScale? sizing = null, bool fullResolution = false)
    {
        if (!analysis.Groups.Any(g => groups.Contains(g.Index))) throw new NoColorsSelectedException();
        options ??= new();
        var size = sizing?.GetSize(analysis.Width, analysis.Height) ?? new(analysis.Width, analysis.Height);
        double scale = fullResolution ? 1 : Math.Min(1, Math.Min(1100.0 / size.Width, 750.0 / size.Height));
        using var mask = ExportImageRenderer.CreateBitmap(Math.Max(1, (int)(size.Width * scale)), Math.Max(1, (int)(size.Height * scale)), options);
        ExportImageRenderer.FillCombined(mask, analysis, groups, options, exportSize: size);
        return RasterImageCodec.EncodePng(mask);
    }

}
