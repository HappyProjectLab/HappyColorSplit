using System.Collections;
using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed class LaserExtractionService
{
    public const int MaxParts = 5000;
    public const int MaxRuns = 1_000_000;

    public List<LaserPart> Extract(LaserSource source, double tolerance, byte minimumAlpha, LaserSettings settings, CancellationToken token)
    {
        var pageSize = settings.GetSize();
        using var image = new ColorSeparationService().Load(source.FilePath);
        var analysis = new ColorSeparationService().Analyze(image, tolerance, token, minimumAlpha);
        var size = source.Scale.GetSize(analysis.Width, analysis.Height);
        var map = analysis.Groups.ToDictionary(g => g.Index,
            g => source.Colors.Contains(g.Hex) ? ClosestMaterial(new(g.R, g.G, g.B), settings.Materials) : -1);
        var smoothing = source.SmoothContours ? new HardContourSampler(analysis, map) : null;
        int Read(int x, int y)
        {
            if (smoothing is not null) return smoothing.Sample(x, y, size.Width, size.Height);
            int sx = (int)(x * (long)analysis.Width / size.Width), sy = (int)(y * (long)analysis.Height / size.Height);
            int label = analysis.Labels[sy * analysis.Width + sx];
            return label >= 0 ? map[label] : -1;
        }
        var visited = new BitArray(checked(size.Width * size.Height));
        var parts = new List<LaserPart>(); int runCount = 0;
        for (int y = 0; y < size.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (int x = 0; x < size.Width; x++)
            {
                if (visited[y * size.Width + x]) continue;
                int material = Read(x, y);
                if (material < 0) { visited[y * size.Width + x] = true; continue; }
                var queue = new Queue<(int X, int Y)>(); queue.Enqueue((x, y));
                var runs = new List<LaserRun>();
                int minX = x, minY = y, maxX = x, maxY = y;
                while (queue.TryDequeue(out var seed))
                {
                    token.ThrowIfCancellationRequested();
                    if (visited[seed.Y * size.Width + seed.X]) continue;
                    int left = seed.X, right = seed.X;
                    while (left > 0 && !visited[seed.Y * size.Width + left - 1] && Read(left - 1, seed.Y) == material) left--;
                    while (right + 1 < size.Width && !visited[seed.Y * size.Width + right + 1] && Read(right + 1, seed.Y) == material) right++;
                    for (int px = left; px <= right; px++) visited[seed.Y * size.Width + px] = true;
                    runs.Add(new(seed.Y, left, right - left + 1));
                    if (++runCount > MaxRuns) throw new LaserComplexityException();
                    minX = Math.Min(minX, left); maxX = Math.Max(maxX, right); minY = Math.Min(minY, seed.Y); maxY = Math.Max(maxY, seed.Y);
                    foreach (int ny in new[] { seed.Y - 1, seed.Y + 1 })
                    {
                        if (ny < 0 || ny >= size.Height) continue;
                        int px = Math.Max(0, left - 1), end = Math.Min(size.Width - 1, right + 1);
                        while (px <= end)
                        {
                            if (!visited[ny * size.Width + px] && Read(px, ny) == material)
                            {
                                queue.Enqueue((px, ny));
                                do { px++; } while (px <= end && !visited[ny * size.Width + px] && Read(px, ny) == material);
                            }
                            else px++;
                        }
                    }
                }
                int nw = maxX - minX + 1, nh = maxY - minY + 1;
                decimal targetW = decimal.Round(nw * (decimal)settings.Dpi / source.Scale.Dpi, 0, MidpointRounding.AwayFromZero);
                decimal targetH = decimal.Round(nh * (decimal)settings.Dpi / source.Scale.Dpi, 0, MidpointRounding.AwayFromZero);
                if (targetW > Math.Max(pageSize.Width, pageSize.Height) || targetH > Math.Max(pageSize.Width, pageSize.Height))
                    throw new LaserPartTooLargeException(Path.GetFileName(source.FilePath), nw * 2.54m / source.Scale.Dpi, nh * 2.54m / source.Scale.Dpi);
                int width = Math.Max(1, (int)targetW), height = Math.Max(1, (int)targetH);
                var normalized = runs.Select(r => new LaserRun(r.Y - minY, r.X - minX, r.Length)).OrderBy(r => r.Y).ThenBy(r => r.X).ToArray();
                parts.Add(new(parts.Count + 1, source.Index, Path.GetFileName(source.FilePath), material, source.Scale.Dpi,
                    new(minX, minY, nw, nh), normalized, width, height));
                if (parts.Count > MaxParts) throw new LaserComplexityException();
            }
        }
        AssignRegions(parts, token);
        return parts;
    }

    private static void AssignRegions(List<LaserPart> parts, CancellationToken token)
    {
        var parent = Enumerable.Range(0, parts.Count).ToArray();
        int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
        for (int i = 0; i < parts.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            double gap = (double)LaserSettings.RegionDistanceCm * parts[i].NativeDpi / 2.54;
            for (int j = 0; j < i; j++)
            {
                var a = parts[i].NativeBounds; var b = parts[j].NativeBounds;
                int dx = Math.Max(0, Math.Max(a.X - b.X - b.Width, b.X - a.X - a.Width));
                int dy = Math.Max(0, Math.Max(a.Y - b.Y - b.Height, b.Y - a.Y - a.Height));
                if (dx * (double)dx + dy * (double)dy <= gap * gap) parent[Root(i)] = Root(j);
            }
        }
        var regions = new Dictionary<int, int>();
        for (int i = 0; i < parts.Count; i++)
        { int root = Root(i); if (!regions.ContainsKey(root)) regions[root] = regions.Count + 1; parts[i].Region = regions[root]; }
    }

    public static int ClosestMaterial(RgbaPixel color, IReadOnlyList<LaserMaterial> materials)
    {
        if (materials.Count == 0) throw new LaserSettingsException();
        var lab = Lab(color); double best = double.PositiveInfinity; int index = 0;
        for (int i = 0; i < materials.Count; i++)
        {
            var candidate = Lab(materials[i].Color);
            double d = Math.Pow(lab.L - candidate.L, 2) + Math.Pow(lab.A - candidate.A, 2) + Math.Pow(lab.B - candidate.B, 2);
            if (d < best) { best = d; index = i; }
        }
        return index;
    }
    private static (double L, double A, double B) Lab(RgbaPixel c)
    {
        static double Linear(byte v) { double s = v / 255.0; return s <= .04045 ? s / 12.92 : Math.Pow((s + .055) / 1.055, 2.4); }
        static double F(double v) => v > 216.0 / 24389 ? Math.Cbrt(v) : 841.0 / 108 * v + 4.0 / 29;
        double r = Linear(c.R), g = Linear(c.G), b = Linear(c.B);
        double x = F((.4124564 * r + .3575761 * g + .1804375 * b) / .95047);
        double y = F(.2126729 * r + .7151522 * g + .072175 * b);
        double z = F((.0193339 * r + .119192 * g + .9503041 * b) / 1.08883);
        return (116 * y - 16, 500 * (x - y), 200 * (y - z));
    }
}
