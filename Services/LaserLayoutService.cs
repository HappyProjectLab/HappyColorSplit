using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed class LaserLayoutService
{
    public const int MaxPages = 200;
    private sealed record Block(int Width, int Height, IReadOnlyList<LaserPlacement> Parts);
    private sealed record Slot<T>(T Item, int X, int Y, int Width, int Height, bool Rotated);
    private sealed class Bin<T>(int width, int height)
    {
        public List<LaserRect> Free { get; } = [new(0, 0, width, height)];
        public List<Slot<T>> Items { get; } = [];
    }

    public LaserLayout Arrange(IReadOnlyList<LaserSource> sources, double tolerance, byte minimumAlpha,
        LaserSettings settings, IProgress<int>? progress, CancellationToken token)
    {
        settings = settings.Snapshot(); settings.GetSize();
        if (settings.Materials.Count == 0) throw new LaserSettingsException();
        var parts = new List<LaserPart>(); int runs = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var extracted = new LaserExtractionService().Extract(sources[i], tolerance, minimumAlpha, settings, token);
            parts.AddRange(extracted); runs += extracted.Sum(p => p.Runs.Count);
            if (parts.Count > LaserExtractionService.MaxParts || runs > LaserExtractionService.MaxRuns) throw new LaserComplexityException();
            progress?.Report((i + 1) * 60 / Math.Max(1, sources.Count));
        }
        return Pack(parts, settings, progress, token);
    }

    public LaserLayout Pack(IReadOnlyList<LaserPart> parts, LaserSettings settings, IProgress<int>? progress, CancellationToken token)
    {
        var size = settings.GetSize();
        if (parts.Count == 0) throw new LaserNoPartsException();
        if (settings.Materials.Count == 0 || settings.Materials.Any(m => !LaserMaterial.TryRgb(m.Hex, out _))
            || parts.Any(p => p.MaterialIndex < 0 || p.MaterialIndex >= settings.Materials.Count)) throw new LaserSettingsException();
        if (parts.Count > LaserExtractionService.MaxParts) throw new LaserComplexityException();
        int gap = (int)decimal.Ceiling(LaserSettings.GapCm * settings.Dpi / 2.54m);
        foreach (var part in parts)
        {
            if (!Fits(part.Width, part.Height, size.Width, size.Height))
                throw new LaserPartTooLargeException(part.SourceName, part.Width * 2.54m / settings.Dpi, part.Height * 2.54m / settings.Dpi);
        }
        var pages = new List<LaserPage>(); int chunkId = 0;
        for (int material = 0; material < settings.Materials.Count; material++)
        {
            token.ThrowIfCancellationRequested();
            var sourceBlocks = new List<Block>();
            foreach (var source in parts.Where(p => p.MaterialIndex == material).GroupBy(p => p.SourceIndex).OrderBy(g => g.Key))
            {
                var regions = new List<Block>();
                foreach (var region in source.GroupBy(p => p.Region).OrderBy(g => g.Key))
                {
                    var bins = Place(region.OrderByDescending(p => (long)p.Width * p.Height).ThenBy(p => p.Id)
                        .Select(p => (p, p.Width, p.Height)), size, gap, token);
                    foreach (var bin in bins)
                    {
                        var positions = bin.Items.Select(s => new LaserPlacement(s.Item, s.X, s.Y, s.Rotated ? 1 : 0)).ToArray();
                        regions.Add(Bounds(positions));
                    }
                }
                foreach (var bin in Place(regions.Select(b => (b, b.Width, b.Height)), size, gap, token))
                {
                    var positions = bin.Items.SelectMany(s => Transform(s.Item, s.X, s.Y, s.Rotated)).ToArray();
                    int chunk = ++chunkId;
                    sourceBlocks.Add(Bounds(positions.Select(p => p with { SourceChunk = chunk }).ToArray()));
                }
            }
            var pageBins = Place(sourceBlocks.Select(b => (b, b.Width, b.Height)), size, gap, token);
            for (int i = 0; i < pageBins.Count; i++)
            {
                var positions = pageBins[i].Items.SelectMany(s => Transform(s.Item, s.X, s.Y, s.Rotated)).ToArray();
                pages.Add(new(pages.Count + 1, i + 1, settings.Materials[material], settings.Snapshot(), size, positions));
                if (pages.Count > MaxPages) throw new LaserComplexityException();
            }
            progress?.Report(60 + (material + 1) * 40 / settings.Materials.Count);
        }
        return new(pages, parts.Count);
    }

    private static bool Fits(int w, int h, int width, int height) => w > 0 && h > 0
        && ((w <= width && h <= height) || (h <= width && w <= height));
    private static Block Bounds(IReadOnlyList<LaserPlacement> parts) => new(parts.Max(p => p.X + p.Width), parts.Max(p => p.Y + p.Height), parts);
    private static IEnumerable<LaserPlacement> Transform(Block block, int x, int y, bool rotated) => block.Parts.Select(p => rotated
        ? p with { X = x + block.Height - p.Y - p.Height, Y = y + p.X, Turns = (p.Turns + 1) % 4 }
        : p with { X = p.X + x, Y = p.Y + y });

    private static List<Bin<T>> Place<T>(IEnumerable<(T Item, int Width, int Height)> items, ImageDimensions size, int gap, CancellationToken token)
    {
        var bins = new List<Bin<T>>();
        foreach (var item in items)
        {
            token.ThrowIfCancellationRequested();
            int bestBin = -1; LaserRect best = default; bool turn = false; long score = long.MaxValue;
            void Search(int binIndex)
            {
                foreach (var free in bins[binIndex].Free)
                for (int rotation = 0; rotation < 2; rotation++)
                {
                    int w = (rotation == 0 ? item.Width : item.Height) + gap;
                    int h = (rotation == 0 ? item.Height : item.Width) + gap;
                    if (w > free.Width || h > free.Height) continue;
                    long rank = ((long)Math.Min(free.Width - w, free.Height - h) << 32) + Math.Max(free.Width - w, free.Height - h);
                    if (rank < score || (rank == score && (free.Y < best.Y || (free.Y == best.Y && free.X < best.X))))
                    { score = rank; bestBin = binIndex; best = new(free.X, free.Y, w, h); turn = rotation == 1; }
                }
            }
            for (int b = 0; b < bins.Count; b++) Search(b);
            if (bestBin < 0)
            {
                bins.Add(new(size.Width + gap, size.Height + gap)); Search(bins.Count - 1);
                if (bestBin < 0) throw new LaserComplexityException();
            }
            var bin = bins[bestBin];
            bin.Items.Add(new(item.Item, best.X, best.Y, best.Width - gap, best.Height - gap, turn));
            var freeRects = new List<LaserRect>();
            foreach (var free in bin.Free)
            {
                if (!Intersects(free, best)) { freeRects.Add(free); continue; }
                if (best.X > free.X) freeRects.Add(new(free.X, free.Y, best.X - free.X, free.Height));
                if (best.X + best.Width < free.X + free.Width) freeRects.Add(new(best.X + best.Width, free.Y, free.X + free.Width - best.X - best.Width, free.Height));
                if (best.Y > free.Y) freeRects.Add(new(free.X, free.Y, free.Width, best.Y - free.Y));
                if (best.Y + best.Height < free.Y + free.Height) freeRects.Add(new(free.X, best.Y + best.Height, free.Width, free.Y + free.Height - best.Y - best.Height));
            }
            for (int i = freeRects.Count - 1; i >= 0; i--)
                if (freeRects.Where((_, j) => j != i).Any(r => Contains(r, freeRects[i]))) freeRects.RemoveAt(i);
            if (freeRects.Count > 10000 || bins.Count > MaxPages) throw new LaserComplexityException();
            bin.Free.Clear(); bin.Free.AddRange(freeRects);
        }
        return bins;
    }
    private static bool Intersects(LaserRect a, LaserRect b) => a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
    private static bool Contains(LaserRect a, LaserRect b) => a.X <= b.X && a.Y <= b.Y && a.X + a.Width >= b.X + b.Width && a.Y + a.Height >= b.Y + b.Height;
}
