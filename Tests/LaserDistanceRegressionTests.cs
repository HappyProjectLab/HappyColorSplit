using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;

internal static class LaserDistanceRegressionTests
{
    public static void Run(string root, Action<bool, string> check)
    {
        string folder = Path.Combine(root, "laser-distances"); Directory.CreateDirectory(folder);
        var store = new SettingsService(folder);
        File.WriteAllText(Path.Combine(folder, "settings.json"), """{"Laser":{"WidthCm":6,"HeightCm":5,"Dpi":254}}""");
        var legacy = store.Load().Laser;
        check(legacy.MarginCm == 0 && legacy.CavityGapCm == .5m && legacy.ElementGapCm == .5m,
            "Existing laser settings keep the previous distances when new JSON fields are missing");
        using (var vm = new MainViewModel(new Dialogs(), store))
        {
            var laser = vm.Laser;
            laser.MarginCm = .2m; laser.CavityGapCm = .1m; laser.ElementGapCm = .8m;
            var saved = store.Load().Laser;
            check(saved.MarginCm == .2m && saved.CavityGapCm == .1m && saved.ElementGapCm == .8m,
                "The three independent laser distances persist through the real settings view model");
            laser.MarginCm = 3; laser.CavityGapCm = -1; laser.ElementGapCm = 501;
            check(laser.MarginCm == .2m && laser.CavityGapCm == .1m && laser.ElementGapCm == .8m,
                "Invalid distances retain all previous valid settings instead of leaving a partial change");
        }
        File.WriteAllText(Path.Combine(folder, "settings.json"), """{"Laser":{"WidthCm":6,"HeightCm":5,"Dpi":254,"MarginCm":-1,"CavityGapCm":501,"ElementGapCm":-2}}""");
        var recovered = store.Load().Laser;
        check(recovered.WidthCm == 6 && recovered.HeightCm == 5 && recovered.MarginCm == 0
            && recovered.CavityGapCm == .5m && recovered.ElementGapCm == .5m, "Invalid saved distances recover independently without changing valid sheet dimensions");

        var settings = new LaserSettings { WidthCm = 3.048m, HeightCm = 3.048m, Dpi = 25,
            MarginCm = .2032m, CavityGapCm = .1016m, ElementGapCm = .8128m, Materials = [new("Red", "#FF0000")] };
        var ring = Part(1, 26, 26, true); var smallA = Part(2, 5, 5); var smallB = Part(3, 5, 5);
        var service = new LaserLayoutService();
        var layout = service.Pack([ring, smallA, smallB], settings, null, CancellationToken.None);
        check(layout.Pages.Count == 1 && layout.Pages[0].Placements.Count(p => p.NestedInPartId == 1) == 2,
            "Cavity clearance can be smaller than the mutual spacing of nested small parts");
        CheckPixels(layout, check);
        settings.CavityGapCm = .8128m; settings.ElementGapCm = .1016m;
        var largerCavityGap = service.Pack([ring, Part(2, 3, 3), Part(3, 3, 3)], settings, null, CancellationToken.None);
        check(largerCavityGap.Pages.Count == 1 && largerCavityGap.Pages[0].Placements.Count(p => p.NestedInPartId == 1) == 2,
            "Cavity clearance can also be larger than the mutual spacing of small parts");
        CheckPixels(largerCavityGap, check);
        var output = new LaserExportService().Export(layout.Pages, folder, new(), null, CancellationToken.None);
        string manifest = File.ReadAllText(Path.Combine(output, "Laser_Layout.json"));
        check(manifest.Contains("\"MarginCm\": 0.2032") && manifest.Contains("\"CavityGapCm\": 0.1016")
            && manifest.Contains("\"ElementGapCm\": 0.8128") && manifest.Contains("\"NestedInPartId\": 1"),
            "Export records all actual distances and each nested part's surrounding part");
        settings.MarginCm = settings.CavityGapCm = settings.ElementGapCm = 0;
        CheckPixels(service.Pack([Part(1, 15, 30), Part(2, 15, 30)], settings, null, CancellationToken.None), check);
        var rounded = new LaserSettings { Dpi = 254, MarginCm = .005m, CavityGapCm = .011m, ElementGapCm = .021m };
        check(rounded.MarginPixels == 1 && rounded.CavityGapPixels == 2 && rounded.ElementGapPixels == 3,
            "All three distances round up independently to the laser output raster");
        try { service.Pack([Part(1, 30, 30)], withMargin(), null, CancellationToken.None); check(false, "Oversized part must respect the edge margin"); }
        catch (LaserPartTooLargeException) { check(true, "Oversized part must respect the edge margin"); }
        LaserSettings withMargin() { var value = settings.Snapshot(); value.MarginCm = .2032m; return value; }
    }

    private static LaserPart Part(int id, int width, int height, bool ring = false)
    {
        var runs = new List<LaserRun>();
        for (int y = 0; y < height; y++)
            if (!ring || y == 0 || y == height - 1) runs.Add(new(y, 0, width));
            else { runs.Add(new(y, 0, 1)); runs.Add(new(y, width - 1, 1)); }
        return new(id, 1, "source.png", 0, 25, new(0, 0, width, height), runs, width, height) { Region = 1 };
    }

    private static void CheckPixels(LaserLayout layout, Action<bool, string> check)
    {
        foreach (var page in layout.Pages)
        {
            var masks = new List<HashSet<(int X, int Y)>>();
            foreach (var placement in page.Placements)
            {
                using var image = RasterImageCodec.Load(new LaserPageRenderer().Render(page with { Placements = [placement] },
                    new(ExportArea.White, ExportBackground.Black), CancellationToken.None));
                var pixels = new HashSet<(int X, int Y)>();
                for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                    if (image[x, y].R == 255) pixels.Add((x, y));
                int margin = page.Settings.MarginPixels;
                check(pixels.All(p => p.X >= margin && p.Y >= margin && p.X < image.Width - margin && p.Y < image.Height - margin),
                    "Every exported filled pixel respects the configured sheet-edge margin");
                masks.Add(pixels);
            }
            for (int i = 0; i < masks.Count; i++) for (int j = 0; j < i; j++)
            {
                var a = page.Placements[i]; var b = page.Placements[j];
                bool nested = a.NestedInPartId == b.Part.Id || b.NestedInPartId == a.Part.Id;
                int gap = nested ? page.Settings.CavityGapPixels : page.Settings.ElementGapPixels;
                long distance = long.MaxValue;
                foreach (var p in masks[i]) foreach (var q in masks[j])
                {
                    int dx = Math.Max(0, Math.Abs(p.X - q.X) - 1), dy = Math.Max(0, Math.Abs(p.Y - q.Y) - 1);
                    distance = Math.Min(distance, (long)dx * dx + (long)dy * dy);
                }
                check(!masks[i].Overlaps(masks[j]) && distance >= (long)gap * gap,
                    nested ? "Nested parts respect the cavity gap in actual exported pixels" : "Other parts respect their independently configured mutual gap");
            }
        }
    }
    private sealed class Dialogs : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
}
