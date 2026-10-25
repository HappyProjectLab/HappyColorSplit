using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class LaserRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "laser"); Directory.CreateDirectory(folder);
        var settings = new LaserSettings { WidthCm = 3, HeightCm = 2.5m, Dpi = 254,
            Materials = [new("Red", "#FF0000"), new("Blue", "#0000FF")] };
        check(LaserExtractionService.ClosestMaterial(new(235, 8, 10), settings.Materials) == 0
            && LaserExtractionService.ClosestMaterial(new(10, 20, 235), settings.Materials) == 1, "Laser colors use the nearest available perceptual material color");
        string input = Path.Combine(folder, "source.png");
        using (var image = new RasterImage(200, 100))
        {
            image[0, 0] = image[0, 1] = image[1, 1] = image[2, 1] = new(235, 8, 10);
            for (int y = 0; y < 3; y++) for (int x = 10; x < 13; x++)
                if (y != 1 || x != 11) image[x, y] = new(235, 8, 10);
            image[150, 0] = image[199, 99] = new(10, 20, 235);
            File.WriteAllBytes(input, RasterImageCodec.EncodePng(image));
        }
        var colors = new HashSet<string> { "#EB080A", "#0A14EB" };
        var source = new LaserSource(1, input, new(100, 254), colors);
        var extractor = new LaserExtractionService(); var parts = extractor.Extract(source, 0, 128, settings, CancellationToken.None);
        check(parts.Count == 4 && parts.Count(p => p.MaterialIndex == 0) == 2, "Laser extraction retains all connected parts and maps them to material masks");
        var ring = parts.Single(p => p.NativeBounds.X == 10);
        check(ring.Runs.Sum(r => r.Length) == 8 && ring.NativeBounds.Width == 3, "Connected-component masks preserve transparent interior holes");
        check(parts[0].Region == ring.Region && parts.Single(p => p.NativeBounds.X == 150).Region != ring.Region,
            "Nearby parts form a region while distant parts of the same source remain separate regions");
        var twice = extractor.Extract(source with { Scale = new(200, 254) }, 0, 128, settings, CancellationToken.None);
        check(twice.Count == 4 && twice[0].NativeBounds.Width == 6 && twice[0].Width == 6,
            "Laser extraction uses the already scaled version of each import");
        var half = extractor.Extract(source with { Scale = new(50, 254) }, 0, 128, settings, CancellationToken.None);
        check(half.Count == 3, "Parts lost by nearest-neighbor downscaling are not reintroduced from the unscaled source");
        var otherDpi = settings.Snapshot(); otherDpi.Dpi = 127;
        var converted = extractor.Extract(source with { Scale = new(200, 254) }, 0, 128, otherDpi, CancellationToken.None);
        check(converted[0].Width == 3 && converted[0].Height == 2, "Laser resolution changes pixel density without changing imported physical part sizes");
        check(extractor.Extract(source with { Colors = new HashSet<string>() }, 0, 128, settings, CancellationToken.None).Count == 0,
            "Unselected image colors never become laser parts");
        string adjacent = Path.Combine(folder, "adjacent.png");
        using (var image = new RasterImage(2, 1))
        {
            image[0, 0] = new(235, 8, 10); image[1, 0] = new(240, 10, 12);
            File.WriteAllBytes(adjacent, RasterImageCodec.EncodePng(image));
        }
        var joined = extractor.Extract(new(2, adjacent, new(100, 254), new HashSet<string> { "#EB080A", "#F00A0C" }), 0, 128, settings, CancellationToken.None);
        check(joined.Count == 1 && joined[0].Width == 2, "Touching selected colors assigned to the same material form one connected physical part");

        var packing = new LaserLayoutService();
        var many = Enumerable.Range(1, 24).Select(i => Part(i, 1 + i / 6, 1 + i % 3, i % 2, 30 + i % 4 * 10, 25 + i % 3 * 10)).ToArray();
        var layout = packing.Pack(many, settings, null, CancellationToken.None);
        Validate(layout, many.Length, check);
        var again = packing.Pack(many, settings, null, CancellationToken.None);
        check(Fingerprint(layout) == Fingerprint(again), "Laser packing is deterministic and retains every part exactly once");
        var random = new Random(6817);
        for (int trial = 0; trial < 12; trial++)
        {
            var varied = Enumerable.Range(1, 30).Select(i => Part(i, 1 + i / 6, 1 + i % 3, i % 2, random.Next(5, 110), random.Next(5, 100))).ToArray();
            Validate(packing.Pack(varied, settings, null, CancellationToken.None), varied.Length, check);
        }
        var small = settings.Snapshot(); small.WidthCm = small.HeightCm = 1.5m;
        var paginated = packing.Pack(Enumerable.Range(1, 4).Select(i => Part(i, 1, 1, 0, 100, 100)).ToArray(), small, null, CancellationToken.None);
        check(paginated.Pages.Count == 4 && paginated.Pages.Select(p => p.MaterialPage).SequenceEqual(new[] { 1, 2, 3, 4 }), "Additional material pages are created until all parts fit with the required spacing");
        Validate(paginated, 4, check);
        var narrow = settings.Snapshot(); narrow.WidthCm = 1; narrow.HeightCm = 1.5m;
        var rotated = packing.Pack([Part(1, 1, 1, 0, 120, 60)], narrow, null, CancellationToken.None);
        check(rotated.Pages.Single().Placements.Single().Turns % 2 == 1, "A part is automatically rotated when that is required to fit the sheet");
        Expect<LaserPartTooLargeException>(() => packing.Pack([Part(1, 1, 1, 0, 401, 301)], settings, null, CancellationToken.None), check);
        Expect<LaserSettingsException>(() => new LaserSettings { WidthCm = 500, HeightCm = 500, Dpi = 2400 }.GetSize(), check);
        CheckLimits(packing, settings, check);
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Expect<OperationCanceledException>(() => new LaserLayoutService().Arrange([source], 0, 128, settings, null, cancellation.Token), check);
        }

        var asymmetric = new LaserPart(1, 1, "shape.png", 0, 254, new(0, 0, 3, 2), [new(0, 0, 1), new(1, 0, 3)], 3, 2) { Region = 1 };
        var tiny = settings.Snapshot(); tiny.WidthCm = tiny.HeightCm = .12m;
        var renderer = new LaserPageRenderer();
        for (int turn = 0; turn < 4; turn++)
        {
            var placement = new LaserPlacement(asymmetric, 4, 4, turn, 1);
            var page = new LaserPage(1, 1, settings.Materials[0], tiny, tiny.GetSize(), [placement]);
            var expected = new HashSet<(int X, int Y)>();
            foreach (var point in new[] { (0, 0), (0, 1), (1, 1), (2, 1) })
            {
                var p = turn switch { 0 => point, 1 => (1 - point.Item2, point.Item1), 2 => (2 - point.Item1, 1 - point.Item2), _ => (point.Item2, 2 - point.Item1) };
                expected.Add((p.Item1 + 4, p.Item2 + 4));
            }
            foreach (var area in Enum.GetValues<ExportArea>())
            foreach (var background in Enum.GetValues<ExportBackground>())
            {
                using var actual = RasterImageCodec.Load(renderer.Render(page, new(area, background), CancellationToken.None));
                for (int y = 0; y < 12; y++) for (int x = 0; x < 12; x++)
                {
                    RgbaPixel bg = background switch { ExportBackground.Black => new(0, 0, 0), ExportBackground.White => new(255, 255, 255), ExportBackground.Gray => new(128, 128, 128), _ => default };
                    RgbaPixel fg = area switch { ExportArea.Black => new(0, 0, 0), ExportArea.White => new(255, 255, 255), ExportArea.GroupColor => new(255, 0, 0), _ => default };
                    check(actual[x, y] == (expected.Contains((x, y)) ? fg : bg), "Every rotation/style laser pixel is correct without mirroring the asymmetric part");
                }
            }
        }
        var exportPage = new LaserPage(1, 1, settings.Materials[0], tiny, tiny.GetSize(), [new(asymmetric, 4, 4, 0, 1)]);
        byte[] plain = renderer.Render(exportPage, new(), CancellationToken.None);
        renderer.Render(exportPage, new(), CancellationToken.None, showGroups: true);
        check(plain.SequenceEqual(renderer.Render(exportPage, new(), CancellationToken.None)), "Preview group outlines never leak into laser PNG exports");
        string output = new LaserExportService().Export([exportPage], folder, new(), null, CancellationToken.None);
        check(Directory.GetFiles(output, "*.png").Length == 1 && File.Exists(Path.Combine(output, "Laser_Layout.json")), "Laser export contains the selected pages and source/region placement manifest");
        string json = File.ReadAllText(Path.Combine(output, "Laser_Layout.json"));
        check(!json.Contains(Path.GetFullPath(folder)) && json.Contains("shape.png") && json.Contains("RotationDegrees"), "Export manifest preserves grouping and rotations without exposing absolute import paths");
        CheckCancelledExport(exportPage, folder, check);

        var settingsService = new SettingsService(Path.Combine(folder, "settings"));
        using var vm = new MainViewModel(new Dialogs(folder), settingsService);
        var window = new MainWindow { DataContext = vm }; window.Show(); vm.LanguageIndex = 1;
        Task load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted);
        vm.ScaleDpi = 254; vm.Tolerance = 0; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        vm.WorkspaceIndex = 1; var laser = vm.Laser;
        check(vm.IsLaserMode && !laser.ArrangeCommand.CanExecute(null), "Laser workspace requires explicit available materials before arranging");
        laser.Dpi = 254; laser.WidthCm = 3; laser.HeightCm = 2.5m;
        laser.MaterialName = "Red sheet"; laser.MaterialHex = "ff0000"; laser.AddMaterialCommand.Execute(null);
        laser.MaterialName = "Blue sheet"; laser.MaterialHex = "#0000FF"; laser.AddMaterialCommand.Execute(null);
        laser.MaterialHex = "#FF0000"; laser.AddMaterialCommand.Execute(null);
        check(laser.Materials.Count == 2 && vm.Status.Contains("already"), "Material hex input is normalized and duplicates are rejected");
        laser.MaterialHex = "bad"; laser.AddMaterialCommand.Execute(null);
        check(laser.Materials.Count == 2 && vm.Status.Contains("#RRGGBB"), "Invalid material colors cannot enter the material list");
        check(settingsService.Load().Laser.Materials.Count == 2 && settingsService.Load().Laser.WidthCm == 3, "Laser materials and physical workspace settings persist as JSON");
        Dispatcher.UIThread.RunJobs();
        var arrange = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Arrange"));
        check(arrange.Command == laser.ArrangeCommand, "Laser workspace controls bind to their own view model");
        arrange.Command!.Execute(null); pump(() => !vm.IsBusy);
        check(laser.IsCurrent && laser.Pages.Count == 2 && laser.Preview is not null && laser.ExportAllCommand.CanExecute(null), "Arrangement creates separate pages per material and enables laser exports");
        Dispatcher.UIThread.RunJobs();
        var rulerCanvas = window.GetVisualDescendants().OfType<HappyColorSplit.Controls.LaserWorkspaceCanvas>().Single();
        check(rulerCanvas.Page == laser.SelectedPage!.Page && ReferenceEquals(rulerCanvas.Preview, laser.Preview),
            "Real workspace rulers and preview bind to the selected material page");
        Validate(new(laser.Pages.Select(p => p.Page).ToArray(), 4), 4, check);
        laser.ExportAllCommand.Execute(null); pump(() => !vm.IsBusy);
        check(Directory.GetFiles(vm.LastExportFolder, "*.png").Length == 2 && vm.LastExportFolder.Contains("HappyColorSplit_Laser_"), "Usual export-folder command tracks the generated laser export");
        laser.SelectedPage = laser.Pages[1]; pump(() => !vm.IsBusy);
        Dispatcher.UIThread.RunJobs();
        check(rulerCanvas.Page == laser.Pages[1].Page && ReferenceEquals(rulerCanvas.Preview, laser.Preview),
            "Changing the selected laser page updates the ruler's dimensions and bitmap together");
        laser.ExportPageCommand.Execute(null); pump(() => !vm.IsBusy);
        check(Directory.GetFiles(vm.LastExportFolder, "*.png").Length == 1, "Export page writes only the selected material page");
        laser.ExportMergedCommand.Execute(null); pump(() => !vm.IsBusy);
        check(Directory.GetFiles(vm.LastExportFolder, "*_merged.png").Length == 2, "Merged laser export writes one combined PNG for every material page");
        vm.ExportAreaIndex = (int)ExportArea.Black; pump(() => !vm.IsBusy);
        check(laser.IsCurrent, "Changing foreground appearance updates the laser preview without discarding valid geometry");
        laser.WidthCm = 3.5m;
        check(laser.IsStale && !laser.ExportAllCommand.CanExecute(null), "Changing the workspace disables stale laser exports until rearranging");
        laser.ArrangeCommand.Execute(null); pump(() => !vm.IsBusy);
        vm.ScalePercent = 200;
        check(laser.IsStale && !laser.ExportPageCommand.CanExecute(null), "Changing an imported image's scale invalidates laser geometry");
        laser.ArrangeCommand.Execute(null); pump(() => !vm.IsBusy);
        vm.Groups[0].ExportEnabled = false;
        check(laser.IsStale, "Changing an image's checked colors invalidates the laser arrangement");
        laser.Dpi = 1.5m;
        check(laser.Dpi == 254, "Fractional laser DPI cannot change the stored physical resolution");
        var savedWidth = laser.WidthCm; laser.WidthCm = 501;
        check(laser.WidthCm == savedWidth, "Oversized sheet settings retain the last valid workspace");
        vm.LanguageIndex = 0; Dispatcher.UIThread.RunJobs();
        check(window.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Anordnen")), "Laser mode supports German and English labels");
        check(laser.PageInfo.Contains("Seite"), "The selected laser page caption changes to German together with the page list");
        vm.LanguageIndex = 1;
        check(laser.PageInfo.Contains("Page ") && laser.PageInfo.Contains("Parts:"), "The selected laser page caption changes back to English without reselection");
        vm.ClearImagesCommand.Execute(null);
        check(!laser.HasPages && laser.Preview is null && !laser.ExportAllCommand.CanExecute(null), "Clearing imported images also clears the old laser workspace");
        window.Close();
    }

    private static void CheckLimits(LaserLayoutService packing, LaserSettings settings, Action<bool, string> check)
    {
        Expect<LaserComplexityException>(() => packing.Pack(Enumerable.Range(1, 5001).Select(i => Part(i, 1, 1, 0, 1, 1)).ToArray(), settings, null, CancellationToken.None), check);
        var tooManyPages = settings.Snapshot(); tooManyPages.WidthCm = tooManyPages.HeightCm = .12m;
        Expect<LaserComplexityException>(() => packing.Pack(Enumerable.Range(1, 201).Select(i => Part(i, 1, 1, 0, 1, 1)).ToArray(), tooManyPages, null, CancellationToken.None), check);
        Expect<LaserSettingsException>(() => packing.Pack([Part(1, 1, 1, 5, 1, 1)], settings, null, CancellationToken.None), check);
    }
    private static void CheckCancelledExport(LaserPage exportPage, string folder, Action<bool, string> check)
    {
        using (var cancellation = new CancellationTokenSource())
        {
            int directories = Directory.GetDirectories(folder).Length;
            Expect<OperationCanceledException>(() => new LaserExportService().Export([exportPage, exportPage with { Index = 2 }], folder,
                new(), new Reporter(_ => cancellation.Cancel()), cancellation.Token), check);
            check(Directory.GetDirectories(folder).Length == directories && !Directory.GetDirectories(folder, "*.tmp").Any(),
                "Cancelled laser jobs remove their private staging directory and never publish a partial job");
        }

    }
    private static LaserPart Part(int id, int source, int region, int material, int width, int height) =>
        new(id, source, $"file-{source}.png", material, 254, new(0, 0, width, height), Enumerable.Range(0, height).Select(y => new LaserRun(y, 0, width)).ToArray(), width, height) { Region = region };
    private static void Validate(LaserLayout layout, int count, Action<bool, string> check)
    {
        var placements = layout.Pages.SelectMany(p => p.Placements).ToArray();
        check(placements.Length == count && placements.Select(p => (p.Part.SourceIndex, p.Part.Id)).Distinct().Count() == count, "Packing retains each source part exactly once");
        foreach (var page in layout.Pages)
        {
            int gap = (int)decimal.Ceiling(LaserSettings.GapCm * page.Settings.Dpi / 2.54m);
            check(page.Placements.Select(p => p.Part.MaterialIndex).Distinct().Count() == 1, "Each page contains only one material color");
            foreach (var p in page.Placements)
                check(p.X >= 0 && p.Y >= 0 && p.X + p.Width <= page.Size.Width && p.Y + p.Height <= page.Size.Height && p.Turns is >= 0 and <= 3,
                    "Every part is within its physical page and uses only valid rotations");
            var chunks = page.Placements.GroupBy(p => p.SourceChunk).ToArray();
            foreach (var chunk in chunks)
                check(chunk.Key > 0 && chunk.Select(p => p.Part.SourceIndex).Distinct().Count() == 1,
                    "Each source block belongs to exactly one imported file");
            var boxes = chunks.Select(g => new LaserRect(g.Min(p => p.X), g.Min(p => p.Y),
                g.Max(p => p.X + p.Width) - g.Min(p => p.X), g.Max(p => p.Y + p.Height) - g.Min(p => p.Y))).ToArray();
            for (int i = 0; i < boxes.Length; i++) for (int j = 0; j < i; j++)
            {
                var a = boxes[i]; var b = boxes[j];
                int dx = Math.Max(0, Math.Max(a.X - b.X - b.Width, b.X - a.X - a.Width));
                int dy = Math.Max(0, Math.Max(a.Y - b.Y - b.Height, b.Y - a.Y - a.Height));
                check((long)dx * dx + (long)dy * dy >= (long)gap * gap, "Source groups remain spatially separate rather than interleaving individual parts");
            }
            for (int i = 0; i < page.Placements.Count; i++) for (int j = 0; j < i; j++)
            {
                var a = page.Placements[i]; var b = page.Placements[j];
                int dx = Math.Max(0, Math.Max(a.X - b.X - b.Width, b.X - a.X - a.Width));
                int dy = Math.Max(0, Math.Max(a.Y - b.Y - b.Height, b.Y - a.Y - a.Height));
                check((long)dx * dx + (long)dy * dy >= (long)gap * gap, "Laser parts never overlap and retain at least 0.5 cm physical separation");
            }
        }
    }
    private static string Fingerprint(LaserLayout layout) => JsonSerializer.Serialize(layout.Pages.Select(p => new { p.Index, p.Material.Hex,
        Parts = p.Placements.Select(q => new { q.Part.Id, q.Part.SourceIndex, q.Part.Region, q.X, q.Y, q.Turns, q.SourceChunk }) }));
    private static void Expect<T>(Action action, Action<bool, string> check) where T : Exception
    { try { action(); } catch (T) { check(true, "Expected " + typeof(T).Name); return; } throw new Exception("Expected " + typeof(T).Name); }
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
    private sealed class Reporter(Action<int> report) : IProgress<int> { public void Report(int value) => report(value); }
}
