using System.Text.Json;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

internal static class LaserOptimizationRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var settings = new LaserSettings { WidthCm = 4, HeightCm = 3, Dpi = 254,
            Materials = [new("Red", "#FF0000"), new("Blue", "#0000FF")] };
        var sizes = new[] { (177, 136), (114, 86), (106, 136), (177, 110), (183, 157), (136, 146), (55, 116), (89, 63) };
        var parts = sizes.Select((size, index) => Part(index + 1, index + 1, 0, size.Item1, size.Item2)).ToArray();
        var service = new LaserLayoutService();
        var first = service.Pack(parts, settings, null, CancellationToken.None, optimize: false);
        var progress = new List<int>();
        var optimized = service.Pack(parts, settings, new Reporter(progress.Add), CancellationToken.None);
        check(first.Pages.Count == 3 && optimized.Pages.Count == 2,
            "The automatic second pass saves one material sheet for a concrete eight-part arrangement");
        check(progress.Contains(80) && progress[^1] == 100 && progress.SequenceEqual(progress.Order()),
            "Both arrangement passes share monotonic progress with a distinct optimization phase");
        Validate(optimized, parts, check);
        var repeat = service.Pack(parts, settings, null, CancellationToken.None);
        check(Fingerprint(optimized) == Fingerprint(repeat), "The second pass and winner selection are deterministic");
        var random = new Random(6318);
        for (int trial = 0; trial < 10; trial++)
        {
            var stock = settings.Snapshot(); stock.MarginCm = .1m; stock.ElementGapCm = trial % 3 * .1m;
            var varied = Enumerable.Range(1, 20).Select(id => Part(id, (id + 1) / 2, id % 2, random.Next(15, 190), random.Next(15, 160))).ToArray();
            var baseline = service.Pack(varied, stock, null, CancellationToken.None, optimize: false);
            var result = service.Pack(varied, stock, null, CancellationToken.None);
            for (int material = 0; material < 2; material++)
            {
                var a = baseline.Pages.Where(p => p.Placements[0].Part.MaterialIndex == material).ToArray();
                var b = result.Pages.Where(p => p.Placements[0].Part.MaterialIndex == material).ToArray();
                check(Score(b).CompareTo(Score(a)) <= 0, "Optimization never worsens sheet count or occupied bounds for any individual material");
            }
            Validate(result, varied, check);
        }
        using var cancellation = new CancellationTokenSource();
        try
        {
            service.Pack(parts, settings, new Reporter(value => { if (value >= 80) cancellation.Cancel(); }), cancellation.Token);
            check(false, "Cancellation between passes must not return a partial result");
        }
        catch (OperationCanceledException) { check(true, "Cancellation between passes never returns a partially optimized layout"); }
    }

    private static void Validate(LaserLayout layout, IReadOnlyList<LaserPart> parts, Action<bool, string> check)
    {
        var positions = layout.Pages.SelectMany(p => p.Placements).ToArray();
        check(positions.Length == parts.Count && positions.Select(p => p.Part).ToHashSet().SetEquals(parts),
            "Optimization retains every original part and its source/region identity exactly once");
        check(layout.Pages.Select(p => p.Index).SequenceEqual(Enumerable.Range(1, layout.Pages.Count)), "Optimized pages retain consecutive global numbering");
        foreach (var group in layout.Pages.GroupBy(p => p.Placements[0].Part.MaterialIndex))
            check(group.Select(p => p.MaterialPage).SequenceEqual(Enumerable.Range(1, group.Count())), "Each material retains its own consecutive page numbering");
        foreach (var page in layout.Pages)
        {
            check(page.Placements.Select(p => p.Part.MaterialIndex).Distinct().Count() == 1, "An optimized page contains exactly one material");
            int margin = page.Settings.MarginPixels, gap = page.Settings.ElementGapPixels;
            foreach (var p in page.Placements)
                check(p.X >= margin && p.Y >= margin && p.X + p.Width <= page.Size.Width - margin
                    && p.Y + p.Height <= page.Size.Height - margin && p.Turns is >= 0 and <= 3,
                    "Optimized parts retain the edge margin, physical size and quarter turns");
            for (int i = 0; i < page.Placements.Count; i++) for (int j = 0; j < i; j++)
            {
                var a = page.Placements[i]; var b = page.Placements[j];
                int dx = Math.Max(0, Math.Max(a.X - b.X - b.Width, b.X - a.X - a.Width));
                int dy = Math.Max(0, Math.Max(a.Y - b.Y - b.Height, b.Y - a.Y - a.Height));
                bool overlaps = a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                check(!overlaps && (long)dx * dx + (long)dy * dy >= (long)gap * gap,
                    "The second pass preserves spacing and forbids overlap, including when the chosen gap is zero");
            }
        }
        foreach (var chunk in positions.GroupBy(p => p.SourceChunk))
            check(chunk.Key > 0 && chunk.Select(p => (p.Part.SourceIndex, p.Part.MaterialIndex)).Distinct().Count() == 1,
                "Combining the winning material layouts cannot mix source-file chunk identifiers");
    }
    private static (int Pages, long Area, long Height) Score(IReadOnlyList<LaserPage> pages)
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
    private static LaserPart Part(int id, int source, int material, int width, int height) => new(id, source, "source.png", material, 254,
        new(0, 0, width, height), Enumerable.Range(0, height).Select(y => new LaserRun(y, 0, width)).ToArray(), width, height) { Region = 1 };
    private static string Fingerprint(LaserLayout layout) => JsonSerializer.Serialize(layout.Pages.Select(p => new { p.Index, p.MaterialPage,
        Parts = p.Placements.Select(q => new { q.Part.Id, q.SourceChunk, q.X, q.Y, q.Turns }) }));
    private sealed class Reporter(Action<int> report) : IProgress<int> { public void Report(int value) => report(value); }
}
