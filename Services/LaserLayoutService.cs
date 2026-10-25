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

    public LaserLayout Pack(IReadOnlyList<LaserPart> parts, LaserSettings settings, IProgress<int>? progress, CancellationToken token, bool optimize = true)
    {
        token.ThrowIfCancellationRequested(); settings = settings.Snapshot();
        if (!optimize) return PackPass(parts, settings, progress, token, false);
        LaserLayout? initial = null;
        try { initial = PackPass(parts, settings, new ProgressSegment(progress, 60), token, false); }
        catch (LaserComplexityException) { /* The other strategy may fit within the page limit. */ }
        token.ThrowIfCancellationRequested();
        LaserLayout alternative;
        try { alternative = PackPass(parts, settings, new ProgressSegment(progress, 80), token, true); }
        catch (LaserComplexityException) when (initial is not null) { progress?.Report(100); return initial; }
        if (initial is null) return alternative;

        var selected = new List<LaserPage>();
        // Choose independently per material: an improvement for one stock colour must not worsen another.
        for (int material = 0; material < settings.Materials.Count; material++)
        {
            token.ThrowIfCancellationRequested();
            var first = initial.Pages.Where(p => p.Placements[0].Part.MaterialIndex == material).ToArray();
            var second = alternative.Pages.Where(p => p.Placements[0].Part.MaterialIndex == material).ToArray();
            selected.AddRange(Quality(second).CompareTo(Quality(first)) < 0 ? second : first);
        }
        var chunks = new Dictionary<(int Material, int Chunk), int>();
        var pages = selected.Select((page, index) => page with
        {
            Index = index + 1,
            Placements = page.Placements.Select(p =>
            {
                var key = (p.Part.MaterialIndex, p.SourceChunk);
                if (!chunks.TryGetValue(key, out int chunk)) chunks.Add(key, chunk = chunks.Count + 1);
                return p with { SourceChunk = chunk };
            }).ToArray()
        }).ToArray();
        return new(pages, parts.Count);
    }

    private static (int Pages, long Area, long Height) Quality(IReadOnlyList<LaserPage> pages)
    {
        long area = 0, height = 0;
        foreach (var page in pages)
        {
            int w = page.Placements.Max(p => p.X + p.Width) - page.Settings.MarginPixels;
            int h = page.Placements.Max(p => p.Y + p.Height) - page.Settings.MarginPixels;
            area += (long)w * h; height += h;
        }
        return (pages.Count, area, height);
    }

    private sealed class ProgressSegment(IProgress<int>? target, int start) : IProgress<int>
    { public void Report(int value) => target?.Report(start + (value - 60) / 2); }

    private LaserLayout PackPass(IReadOnlyList<LaserPart> parts, LaserSettings settings, IProgress<int>? progress, CancellationToken token, bool alternate)
    {
        var fullSize = settings.GetSize(); int margin = settings.MarginPixels;
        var size = new ImageDimensions(fullSize.Width - 2 * margin, fullSize.Height - 2 * margin);
        if (parts.Count == 0) throw new LaserNoPartsException();
        if (settings.Materials.Count == 0 || settings.Materials.Any(m => !LaserMaterial.TryRgb(m.Hex, out _))
            || parts.Any(p => p.MaterialIndex < 0 || p.MaterialIndex >= settings.Materials.Count)) throw new LaserSettingsException();
        if (parts.Count > LaserExtractionService.MaxParts) throw new LaserComplexityException();
        int gap = settings.ElementGapPixels;
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
                    var ordered = alternate
                        ? region.OrderByDescending(p => Math.Max(p.Width, p.Height)).ThenByDescending(p => Math.Min(p.Width, p.Height)).ThenBy(p => p.Id)
                        : region.OrderByDescending(p => (long)p.Width * p.Height).ThenBy(p => p.Id);
                    var bins = Place(ordered.Select(p => (p, p.Width, p.Height)), size, gap, token, alternate);
                    foreach (var bin in bins)
                    {
                        var positions = bin.Items.Select(s => new LaserPlacement(s.Item, s.X, s.Y, s.Rotated ? 1 : 0)).ToArray();
                        regions.Add(Bounds(positions));
                    }
                }
                var sourceBins = Place(OrderBlocks(regions, alternate).Select(b => (b, b.Width, b.Height)), size, gap, token, alternate)
                    .Select(bin => bin.Items.SelectMany(s => Transform(s.Item, s.X, s.Y, s.Rotated)).ToList()).ToList();
                new LaserCavityNesting(settings.CavityGapPixels, gap, token).Compact(sourceBins);
                foreach (var positions in sourceBins.Where(bin => bin.Count > 0))
                {
                    int chunk = ++chunkId;
                    sourceBlocks.Add(Bounds(positions.Select(p => p with { SourceChunk = chunk }).ToArray()));
                }
            }
            var pageBins = Place(OrderBlocks(sourceBlocks, alternate).Select(b => (b, b.Width, b.Height)), size, gap, token, alternate);
            for (int i = 0; i < pageBins.Count; i++)
            {
                var positions = pageBins[i].Items.SelectMany(s => Transform(s.Item, s.X + margin, s.Y + margin, s.Rotated)).ToArray();
                pages.Add(new(pages.Count + 1, i + 1, settings.Materials[material], settings.Snapshot(), fullSize, positions));
                if (pages.Count > MaxPages) throw new LaserComplexityException();
            }
            progress?.Report(60 + (material + 1) * 40 / settings.Materials.Count);
        }
        return new(pages, parts.Count);
    }

    private static bool Fits(int w, int h, int width, int height) => w > 0 && h > 0
        && ((w <= width && h <= height) || (h <= width && w <= height));
    private static IEnumerable<Block> OrderBlocks(IEnumerable<Block> blocks, bool alternate) => alternate
        ? blocks.OrderByDescending(b => (long)b.Width * b.Height).ThenByDescending(b => Math.Max(b.Width, b.Height)) : blocks;
    private static Block Bounds(IReadOnlyList<LaserPlacement> parts)
    {
        int x = parts.Min(p => p.X), y = parts.Min(p => p.Y);
        return new(parts.Max(p => p.X + p.Width) - x, parts.Max(p => p.Y + p.Height) - y,
            parts.Select(p => p with { X = p.X - x, Y = p.Y - y }).ToArray());
    }
    private static IEnumerable<LaserPlacement> Transform(Block block, int x, int y, bool rotated) => block.Parts.Select(p => rotated
        ? p with { X = x + block.Height - p.Y - p.Height, Y = y + p.X, Turns = (p.Turns + 1) % 4 }
        : p with { X = p.X + x, Y = p.Y + y });

    private static List<Bin<T>> Place<T>(IEnumerable<(T Item, int Width, int Height)> items, ImageDimensions size, int gap, CancellationToken token, bool alternate)
    {
        var bins = new List<Bin<T>>();
        foreach (var item in items)
        {
            token.ThrowIfCancellationRequested();
            int bestBin = -1; LaserRect best = default; bool turn = false; long score = long.MaxValue; int secondaryScore = int.MaxValue;
            void Search(int binIndex)
            {
                foreach (var free in bins[binIndex].Free)
                for (int rotation = 0; rotation < 2; rotation++)
                {
                    int w = (rotation == 0 ? item.Width : item.Height) + gap;
                    int h = (rotation == 0 ? item.Height : item.Width) + gap;
                    if (w > free.Width || h > free.Height) continue;
                    int shortSide = Math.Min(free.Width - w, free.Height - h);
                    long rank = alternate ? (long)free.Width * free.Height - (long)w * h : shortSide;
                    int secondary = alternate ? shortSide : Math.Max(free.Width - w, free.Height - h);
                    if (rank < score || (rank == score && (secondary < secondaryScore
                        || (secondary == secondaryScore && (free.Y < best.Y || (free.Y == best.Y && free.X < best.X))))))
                    { score = rank; secondaryScore = secondary; bestBin = binIndex; best = new(free.X, free.Y, w, h); turn = rotation == 1; }
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
