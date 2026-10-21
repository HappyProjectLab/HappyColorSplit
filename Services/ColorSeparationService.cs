using HappyColorSplit.Models;
using SkiaSharp;

namespace HappyColorSplit.Services;

public sealed class ColorSeparationService
{
    public const long MaxPixels = 16_000_000;
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

    public byte[] OriginalPreview(RasterImage source, int width = 1100, int height = 750)
    {
        double scale = Math.Min(1, Math.Min((double)width / source.Width, (double)height / source.Height));
        if (scale == 1) return RasterImageCodec.EncodePng(source);
        using var preview = new RasterImage(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));
        if (!source.Bitmap.ScalePixels(preview.Bitmap, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None)))
            throw new IOException("Image resizing failed.");
        return RasterImageCodec.EncodePng(preview);
    }

    public byte[] MaskPreview(ColorAnalysis analysis, int group, bool fullResolution = false, ExportOptions? options = null)
    {
        options ??= new();
        // Nearest-neighbor sampling bounds memory and retains hard mask edges.
        double scale = fullResolution ? 1 : Math.Min(1, Math.Min(1100.0 / analysis.Width, 750.0 / analysis.Height));
        int width = Math.Max(1, (int)(analysis.Width * scale)), height = Math.Max(1, (int)(analysis.Height * scale));
        using var mask = ExportImageRenderer.CreateBitmap(width, height, options);
        ExportImageRenderer.Fill(mask, analysis, group, options, scale: scale);
        return RasterImageCodec.EncodePng(mask);
    }

    public byte[] OverlayPreview(ColorAnalysis analysis, int group, CancellationToken token)
    {
        using var overlay = new RasterImage(analysis.Width, analysis.Height);
        for (int y = 0; y < analysis.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            var row = overlay.GetRowSpan(y);
            for (int x = 0; x < analysis.Width; x++)
            {
                if (analysis.Labels[y * analysis.Width + x] != group) continue;
                int offset = x * 4;
                row[offset] = 80; row[offset + 1] = 255; row[offset + 2] = 120; row[offset + 3] = 255;
            }
        }
        return RasterImageCodec.EncodePng(overlay);
    }

    public string Export(ColorAnalysis analysis, string parent, string sourceName, IProgress<int>? progress, CancellationToken token,
        IReadOnlySet<int>? selectedGroups = null, ExportOptions? options = null)
    {
        options ??= new();
        var groups = analysis.Groups.Where(g => selectedGroups is null || selectedGroups.Contains(g.Index)).ToArray();
        if (groups.Length == 0) throw new NoColorsSelectedException();
        string name = Path.GetFileNameWithoutExtension(sourceName);
        string suffix = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        string final = Path.Combine(parent, $"{name}_HappyColorSplit_{suffix}");
        string staging = Path.Combine(parent, $".HappyColorSplit_{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(staging);
        try
        {
            using var mask = ExportImageRenderer.CreateBitmap(analysis.Width, analysis.Height, options);
            int completed = 0;
            foreach (var group in groups)
            {
                token.ThrowIfCancellationRequested();
                ExportImageRenderer.Fill(mask, analysis, group.Index, options, token);
                File.WriteAllBytes(Path.Combine(staging, $"{name}_{group.Index + 1:D3}_{group.Hex[1..]}.png"), RasterImageCodec.EncodePng(mask));
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

}
