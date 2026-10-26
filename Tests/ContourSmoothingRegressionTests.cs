using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ContourSmoothingRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        var service = new ColorSeparationService();
        using var source = new RasterImage(8, 8);
        using var unified = new RasterImage(8, 8);
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
        {
            bool filled = (x - 3.5) * (x - 3.5) + (y - 3.5) * (y - 3.5) <= 10;
            source[x, y] = filled ? x < 4 ? new(255, 0, 0) : new(0, 0, 255) : new(0, 0, 0);
            unified[x, y] = filled ? new(255, 0, 0) : new(0, 0, 0);
        }
        var analysis = service.Analyze(source, 0, CancellationToken.None);
        var solid = service.Analyze(unified, 0, CancellationToken.None);
        int red = analysis.Groups.Single(g => g.Hex == "#FF0000").Index;
        var selected = analysis.Groups.Where(g => g.Hex != "#000000").Select(g => g.Index).ToHashSet();
        int solidRed = solid.Groups.Single(g => g.Hex == "#FF0000").Index;
        var sizing = new ImageScale(400, 254);
        byte[] nearest = service.CombinedPreview(analysis, selected, sizing: sizing, fullResolution: true);
        byte[] smooth = service.CombinedPreview(analysis, selected, new(SmoothContours: true), sizing, true);
        check(!nearest.SequenceEqual(smooth), "Hard contour reconstruction changes enlarged stair steps instead of simply copying enlarged source squares");
        check(!service.CombinedPreview(analysis, selected, new(SmoothContours: true), new(300), true)
            .SequenceEqual(service.CombinedPreview(analysis, selected, new(), new(300), true)), "The one-click threefold resolution actually changes contour stair steps on the tested low-resolution curve");
        check(smooth.SequenceEqual(service.MaskPreview(solid, solidRed, true, new(SmoothContours: true), sizing)),
            "Merged layers are united before contour smoothing instead of smoothing each selected color in isolation");
        check(service.CombinedPreview(analysis, selected, new(SmoothContours: true), new(100), true)
            .SequenceEqual(service.CombinedPreview(analysis, selected, new(), new(100), true)),
            "Source-resolution mask pixels remain exact when smoothing is enabled without enlargement");
        var separated = analysis.Groups.Select(g => RasterImageCodec.Load(service.MaskPreview(analysis, g.Index, true, new(SmoothContours: true), new(350)))).ToArray();
        try
        {
            for (int y = 0; y < separated[0].Height; y++) for (int x = 0; x < separated[0].Width; x++)
                check(separated.Count(mask => mask[x, y].R == 255) == 1, "Individually smoothed color layers assign every opaque source position to one color without overlaps, including multi-color junctions");
        }
        finally { foreach (var mask in separated) mask.Dispose(); }
        using (var detailOverlay = RasterImageCodec.Load(service.OverlayPreview(analysis, red, CancellationToken.None, sizing, true)))
        using (var detailMask = RasterImageCodec.Load(service.MaskPreview(analysis, red, true, new(SmoothContours: true), sizing)))
            for (int y = 0; y < detailMask.Height; y++) for (int x = 0; x < detailMask.Width; x++)
                check((detailOverlay[x, y].A == 255) == (detailMask[x, y].R == 255), "The single-layer detail overlay follows the same unique color assignment as its smoothed export");

        foreach (var area in Enum.GetValues<ExportArea>()) foreach (var background in Enum.GetValues<ExportBackground>())
        {
            var options = new ExportOptions(area, background, true);
            using var image = RasterImageCodec.Load(service.CombinedPreview(analysis, selected, options, sizing, true));
            var allowed = new HashSet<RgbaPixel> { Background(background) };
            if (area == ExportArea.GroupColor) { allowed.Add(new(255, 0, 0)); allowed.Add(new(0, 0, 255)); }
            else allowed.Add(area switch { ExportArea.Black => new(0, 0, 0), ExportArea.White => new(255, 255, 255), _ => default });
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                check(allowed.Contains(image[x, y]) && image[x, y].A is 0 or 255,
                    "Every smoothed output pixel is an exact foreground or background value with no soft alpha or mixed edge colors");
        }
        using (var tiny = new RasterImage(8, 8))
        {
            tiny[1, 1] = new(255, 0, 0);
            for (int y = 3; y < 7; y++) for (int x = 3; x < 7; x++) if (x is 3 or 6 || y is 3 or 6) tiny[x, y] = new(255, 0, 0);
            var detail = service.Analyze(tiny, 0, CancellationToken.None);
            using var output = RasterImageCodec.Load(service.MaskPreview(detail, detail.Groups.Single().Index, true, new(SmoothContours: true), new(200)));
            check(output[2, 2].R == 255 && output[3, 3].R == 255, "A source-pixel-sized isolated detail survives the tested twofold contour enlargement");
            check(output[8, 8].R == 0 && output[9, 9].R == 0, "A small interior hole remains open in the tested enlarged mask");
        }
        var largeScale = new ImageScale(15000, 254);
        using (var full = RasterImageCodec.Load(service.MaskPreview(analysis, red, true, new(SmoothContours: true), largeScale)))
        using (var preview = RasterImageCodec.Load(service.MaskPreview(analysis, red, false, new(SmoothContours: true), largeScale)))
        {
            for (int y = 0; y < preview.Height; y += 67) for (int x = 0; x < preview.Width; x += 61)
                check(preview[x, y] == full[x * full.Width / preview.Width, y * full.Height / preview.Height],
                    "Reduced preview samples the final smoothed export grid rather than creating a different contour");
        }
        string folder = Path.Combine(root, "contours"); Directory.CreateDirectory(folder);
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            try { service.MaskPreview(analysis, red, true, new(SmoothContours: true), sizing, cancellation.Token); check(false, "Cancelled smoothed preview must stop"); }
            catch (OperationCanceledException) { check(true, "Full-resolution contour reconstruction responds to cancellation"); }
            try { service.Export(analysis, folder, "cancelled.png", null, cancellation.Token, selected, new(SmoothContours: true), sizing); check(false, "Cancelled contour export must stop"); }
            catch (OperationCanceledException) { check(!Directory.GetDirectories(folder).Any(), "Cancelled contour export creates no partial export folder"); }
        }
        string input = Path.Combine(folder, "circle.png"); File.WriteAllBytes(input, RasterImageCodec.EncodePng(source));
        var laserSettings = new LaserSettings { WidthCm = .32m, HeightCm = .32m, Dpi = 254, Materials = [new("Red", "#FF0000")] };
        var parts = new LaserExtractionService().Extract(new(1, input, sizing, selected.Select(i => analysis.Groups.Single(g => g.Index == i).Hex).ToHashSet(), true),
            0, 128, laserSettings, CancellationToken.None);
        var laserPage = new LaserPage(1, 1, laserSettings.Materials[0], laserSettings, laserSettings.GetSize(),
            parts.Select(p => new LaserPlacement(p, p.NativeBounds.X, p.NativeBounds.Y, 0, 1)).ToArray());
        using (var laser = RasterImageCodec.Load(new LaserPageRenderer().Render(laserPage, new(), CancellationToken.None)))
        using (var layer = RasterImageCodec.Load(smooth))
            for (int y = 0; y < layer.Height; y++) for (int x = 0; x < layer.Width; x++)
                check(laser[x, y] == layer[x, y], "Laser components use the same smoothed material union before spacing and placement are calculated");
        string exported = service.Export(analysis, folder, "circle.png", null, CancellationToken.None, selected, new(SmoothContours: true), sizing);
        check(Directory.GetFiles(exported, "*.png").Length == 2, "Individual smoothed layers export only the two selected colors");
        string merged = service.Export(analysis, folder, "circle.png", null, CancellationToken.None, selected, new(SmoothContours: true), sizing, true);
        using (var exportedImage = RasterImageCodec.Load(File.ReadAllBytes(Directory.GetFiles(merged, "*.png").Single())))
        using (var reference = RasterImageCodec.Load(smooth))
            check(PixelsEqual(exportedImage, reference), "Merged file export uses exactly the same contour as its full-resolution preview");
        CheckUi(folder, input, check, pump);
    }

    private static void CheckUi(string folder, string input, Action<bool, string> check, Action<Func<bool>> pump)
    {
        var store = new SettingsService(Path.Combine(folder, "settings"));
        using var vm = new MainViewModel(new Dialogs(folder), store);
        check(!vm.SmoothContours && !vm.TripleContourResolutionCommand.CanExecute(null), "Contour smoothing defaults off and cannot upscale an absent image");
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            var load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted);
            vm.Tolerance = 0; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy); Dispatcher.UIThread.RunJobs();
            foreach (var group in vm.Groups) group.ExportEnabled = group.Hex != "#000000";
            var button = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, vm.Text["Contours"]));
            button.Flyout!.ShowAt(button); Dispatcher.UIThread.RunJobs();
            var panel = (StackPanel)((Flyout)button.Flyout).Content!;
            var toggle = panel.Children.OfType<CheckBox>().Single(); toggle.IsChecked = true; Dispatcher.UIThread.RunJobs();
            check(vm.SmoothContours && store.Load().SmoothContours, "The real contour checkbox enables and persists smoothing for exports");
            var upscale = panel.Children.OfType<Button>().Single(b => b.Command == vm.TripleContourResolutionCommand);
            decimal width = vm.ScaleWidthCm, height = vm.ScaleHeightCm, dpi = vm.ScaleDpi;
            upscale.Command!.Execute(null); Dispatcher.UIThread.RunJobs();
            check(vm.ScalePercent == 300 && vm.ScaleDpi == dpi * 3 && vm.ScaleWidthCm == width && vm.ScaleHeightCm == height,
                "The contour-resolution button triples pixels and DPI while preserving the selected image's physical dimensions");
            check(store.Load().DefaultExportDpi == dpi, "Improving one image's contour resolution does not change the default physical size of future imports");
            check(vm.Images[0].ColorsDetected && vm.ExportColorCount == 2, "Smoothing and resolution changes preserve detected colors and per-image selection");
            button.Flyout.Hide();
            vm.Laser.Dpi = 254; vm.Laser.MaterialHex = "#FF0000"; vm.Laser.AddMaterialCommand.Execute(null); vm.WorkspaceIndex = 1;
            vm.Laser.ArrangeCommand.Execute(null); pump(() => !vm.IsBusy);
            check(vm.Laser.IsCurrent, "Laser arrangement accepts smoothed source masks");
            vm.SmoothContours = false;
            check(vm.Laser.IsStale && !vm.Laser.ExportAllCommand.CanExecute(null), "Changing contour geometry invalidates previously arranged laser pages");
            vm.ScaleDpi = 2400;
            check(!vm.TripleContourResolutionCommand.CanExecute(null), "The resolution shortcut respects the output DPI limit");
        }
        finally { window.Close(); }
    }
    private static bool PixelsEqual(RasterImage a, RasterImage b)
    {
        if (a.Width != b.Width || a.Height != b.Height) return false;
        for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) if (a[x, y] != b[x, y]) return false;
        return true;
    }
    private static RgbaPixel Background(ExportBackground value) => value switch
    { ExportBackground.Black => new(0, 0, 0), ExportBackground.White => new(255, 255, 255), ExportBackground.Gray => new(128, 128, 128), _ => default };
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
}
