using System.Buffers.Binary;
using Avalonia.Controls;
using Avalonia.Threading;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ScalingRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "scaling"); Directory.CreateDirectory(folder);
        var service = new ColorSeparationService();
        using var source = new RasterImage(4, 2);
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 4; x++) source[x, y] = x < 2 ? new(240, 20, 90) : new(20, 170, 220);
        source[3, 1] = new(240, 20, 90, 0);
        var analysis = service.Analyze(source, 0, CancellationToken.None, 128);
        foreach (decimal percent in new[] { 50m, 100m, 150m, 200m })
        {
            var sizing = new ImageScale(percent, 300);
            var size = sizing.GetSize(4, 2);
            string export = service.Export(analysis, folder, "sample.png", null, CancellationToken.None, sizing: sizing);
            foreach (var group in analysis.Groups)
            {
                string path = Directory.GetFiles(export).Single(p => p.EndsWith(group.Hex[1..] + ".png"));
                using var mask = RasterImageCodec.Load(path);
                using var preview = RasterImageCodec.Load(service.MaskPreview(analysis, group.Index, true, sizing: sizing));
                check(mask.Width == size.Width && mask.Height == size.Height, "Scaled PNG dimensions match proportional rounding");
                CheckDpi(File.ReadAllBytes(path), 300, check);
                for (int y = 0; y < size.Height; y++)
                for (int x = 0; x < size.Width; x++)
                {
                    int sx = x * 4 / size.Width, sy = y * 2 / size.Height;
                    var expected = analysis.Labels[sy * 4 + sx] == group.Index ? new RgbaPixel(255, 255, 255) : new RgbaPixel(0, 0, 0);
                    check(mask[x, y] == expected && preview[x, y] == expected,
                        "Scaled export and preview retain exact nearest-neighbor membership without edge mixtures");
                }
            }
        }
        foreach (var area in Enum.GetValues<ExportArea>())
        foreach (var background in Enum.GetValues<ExportBackground>())
        {
            var group = analysis.Groups[0]; var sizing = new ImageScale(150, 254);
            var options = new ExportOptions(area, background);
            string export = service.Export(analysis, folder, "appearance.png", null, CancellationToken.None,
                new HashSet<int> { group.Index }, options, sizing);
            string path = Directory.GetFiles(export).Single();
            using var original = RasterImageCodec.Load(service.MaskPreview(analysis, group.Index, true, options));
            using var actual = RasterImageCodec.Load(path);
            check(actual.Width == 6 && actual.Height == 3, "Every appearance option supports individual scaling");
            CheckDpi(File.ReadAllBytes(path), 254, check);
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 6; x++)
                check(actual[x, y] == original[x * 4 / 6, y * 2 / 3], "Scaling preserves group RGB and both area/background alpha values");
        }
        var largeAnalysis = new ColorAnalysis(1200, 2, Enumerable.Range(0, 2400).Select(i => i % 1200 < 600 ? 0 : -1).ToArray(), [new(0, 240, 20, 90, 1200)]);
        using (var preview = RasterImageCodec.Load(service.MaskPreview(largeAnalysis, 0, sizing: new ImageScale(200))))
            check(preview.Width == 1100 && preview[0, 0].R == 255 && preview[1099, 0].R == 0,
                "Large scaled mask preview remains bounded while retaining its final sampling grid");
        check(new ImageScale(100).GetSize(3, 7) == new ImageDimensions(3, 7), "100 percent preserves exact original dimensions");
        check(new ImageScale(50).GetSize(3, 7) == new ImageDimensions(2, 4), "Fractional output sizes round to the nearest pixel");
        check(new ImageScale(100, 254).WidthCm(1000) == 10, "Centimeters follow DPI conversion exactly at 254 DPI");
        Expect<ExportSizeLimitException>(() => new ImageScale(decimal.MaxValue).GetSize(4, 2), check);
        Expect<ExportSizeLimitException>(() => new ImageScale(200).GetSize(4000, 4000), check);
        Expect<ArgumentOutOfRangeException>(() => new ImageScale(0).GetSize(4, 2), check);
        Expect<ArgumentOutOfRangeException>(() => new ImageScale(100, 0).GetSize(4, 2), check);
        int oldFolders = Directory.GetDirectories(folder).Length;
        Expect<ExportSizeLimitException>(() => service.Export(analysis, folder, "large.png", null, CancellationToken.None, sizing: new ImageScale(1_000_000)), check);
        check(Directory.GetDirectories(folder).Length == oldFolders, "Invalid export size is rejected before creating temporary or final folders");
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            Expect<OperationCanceledException>(() => service.Export(analysis, folder, "cancelled.png", null, cancellation.Token, sizing: new ImageScale(200)), check);
            check(!Directory.GetDirectories(folder, "*.tmp").Any(), "Cancelled scaled export leaves no temporary image directory");
        }

        string first = Path.Combine(folder, "first.png"), second = Path.Combine(folder, "second.png");
        File.WriteAllBytes(first, RasterImageCodec.EncodePng(source));
        using (var other = new RasterImage(2, 4, new RgbaPixel(10, 200, 30))) File.WriteAllBytes(second, RasterImageCodec.EncodePng(other));
        byte[] unchanged = File.ReadAllBytes(first);
        var settings = new SettingsService(Path.Combine(folder, "settings"));
        var detail = new Details();
        using var vm = new MainViewModel(new Dialogs(folder), settings, detail);
        var window = new MainWindow { DataContext = vm }; window.Show();
        Task load = vm.AddImagesAsync([first, second]); pump(() => load.IsCompleted);
        var sizeButton = window.FindControl<Button>("ImageSizeButton")!;
        sizeButton.Flyout!.ShowAt(sizeButton); Dispatcher.UIThread.RunJobs();
        var percentControl = window.FindControl<NumericUpDown>("ScalePercentInput")!;
        var unit = window.FindControl<ComboBox>("ScaleUnitSelector")!;
        check(vm.Images.Count == 2 && vm.Images.All(i => i.Scale == new ImageScale()) && vm.ScaleDpi == 300,
            "Each imported image starts independently at 100 percent and default 300 DPI");
        percentControl.Value = 150; Dispatcher.UIThread.RunJobs();
        check(vm.Images[0].Scale.Percent == 150 && vm.Images[1].Scale.Percent == 100, "Editing percentage affects only the selected image");
        vm.SelectedImage = vm.Images[1]; pump(() => !vm.IsBusy);
        percentControl.Value = 250; Dispatcher.UIThread.RunJobs();
        vm.SelectedImage = vm.Images[0]; pump(() => !vm.IsBusy);
        check(vm.ScalePercent == 150 && percentControl.Value == 150 && vm.Images[1].Scale.Percent == 250,
            "Changing images restores their individual size controls");
        vm.Tolerance = 0; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        foreach (var group in vm.Groups) group.ExportEnabled = group == vm.SelectedGroup;
        var selected = vm.SelectedGroup;
        unit.SelectedIndex = 1; Dispatcher.UIThread.RunJobs();
        var widthControl = window.FindControl<NumericUpDown>("ScaleWidthInput")!;
        var heightControl = window.FindControl<NumericUpDown>("ScaleHeightInput")!;
        widthControl.Value = 2.54m; Dispatcher.UIThread.RunJobs();
        check(vm.ScaleWidthCm == 2.54m && vm.ScaleHeightCm == 1.27m && heightControl.Value == 1.27m,
            "Changing width in centimeters immediately changes height with locked aspect ratio");
        heightControl.Value = 2.54m; Dispatcher.UIThread.RunJobs();
        check(vm.ScaleWidthCm == 5.08m && vm.ScaleHeightCm == 2.54m && widthControl.Value == 5.08m,
            "Changing height in centimeters immediately changes width with locked aspect ratio");
        vm.ScaleDpi = 100;
        check(vm.ScaleWidthCm == 5.08m && vm.ScaleHeightCm == 2.54m && vm.Images[0].Scale.GetSize(4, 2) == new ImageDimensions(200, 100),
            "DPI changes in cm mode preserve physical dimensions while updating target pixels");
        vm.ScaleWidthCm = 2.54m; vm.ScaleUnitIndex = 0; vm.ScaleDpi = 200;
        check(vm.Images[0].Scale.GetSize(4, 2) == new ImageDimensions(100, 50) && vm.ScaleWidthCm == 1.27m,
            "DPI changes in percent mode preserve pixel dimensions while updating physical size");
        vm.ScaleUnitIndex = 1; vm.ScaleWidthCm = 2.54m;
        check(vm.Images[0].Scale.GetSize(4, 2) == new ImageDimensions(200, 100) && vm.SelectedGroup == selected && vm.ExportColorCount == 1,
            "Scaling does not reset recognized colors or export selection");
        check(settings.Load() is { DefaultExportDpi: 200, ScaleUnit: 1 }, "Default DPI for new imports and selected unit are saved");
        decimal previous = vm.ScalePercent;
        vm.ScalePercent = 0; check(vm.ScalePercent == previous && vm.Status.Contains("positive"), "Zero size is rejected without losing the previous scale");
        vm.ScalePercent = 1_000_000; check(vm.ScalePercent == previous && vm.Status.Contains("16 Millionen"), "Oversized dimensions are rejected with a localized explanation");
        vm.ScaleDpi = 2401; check(vm.ScaleDpi == 200, "Unsupported DPI input does not corrupt image settings");
        vm.LanguageIndex = 1; Dispatcher.UIThread.RunJobs();
        check(Equals(sizeButton.Content, "Image size") && vm.ImageSizeSummary.StartsWith("Original:")
            && vm.ScalePercent == previous && vm.ScaleUnitIndex == 1,
            "Switching language preserves each image's size and unit");
        vm.ExportCommand.Execute(null);
        vm.ScalePercent = 50;
        check(vm.ScalePercent == previous, "Scale edits cannot change an in-progress export");
        pump(() => !vm.IsBusy);
        string single = Directory.GetFiles(vm.LastExportFolder).Single();
        using (var actual = RasterImageCodec.Load(single)) check(actual.Width == 200 && actual.Height == 100, "Single export applies the selected image's cm-derived size");
        CheckDpi(File.ReadAllBytes(single), 200, check);
        vm.DetailPreviewCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var preview = detail.Preview!)
            check(preview.Original.PixelSize.Width == 200 && preview.Mask.PixelSize.Height == 100 && preview.Overlay.PixelSize.Width == 200,
                "Detail image, mask and overlay share the actual scaled export grid");
        vm.SelectedImage = vm.Images[1]; pump(() => !vm.IsBusy);
        check(vm.ScalePercent == 250 && vm.ScaleDpi == 300 && vm.ScaleUnitIndex == 0,
            "Second image retains its own percentage, DPI and input unit after first-image edits");
        vm.ScalePercent = 50;
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        string batchFolder = vm.LastExportFolder;
        var masks = Directory.GetFiles(batchFolder, "*.png", SearchOption.AllDirectories);
        check(masks.Length == 2, "Batch export retains independent per-image color selections");
        foreach (string path in masks)
        {
            bool isFirst = Path.GetFileName(path).StartsWith("first_");
            using var mask = RasterImageCodec.Load(path);
            check(mask.Width == (isFirst ? 200 : 1) && mask.Height == (isFirst ? 100 : 2), "Batch export applies each image's individual size");
            CheckDpi(File.ReadAllBytes(path), isFirst ? 200 : 300, check);
        }
        vm.ResetScaleCommand.Execute(null);
        check(vm.ScalePercent == 100 && vm.ScaleDpi == 300 && vm.Images[0].Scale.GetSize(4, 2) == new ImageDimensions(200, 100),
            "100 percent resets only the selected image and retains its DPI");
        check(File.ReadAllBytes(first).SequenceEqual(unchanged), "Scaling and export never rewrite the imported source");
        string tinyPath = Path.Combine(folder, "tiny.png");
        using (var tiny = new RasterImage(1, 1, new RgbaPixel(10, 200, 30))) File.WriteAllBytes(tinyPath, RasterImageCodec.EncodePng(tiny));
        var tinyLoad = vm.AddImagesAsync([tinyPath]); pump(() => tinyLoad.IsCompleted);
        check(vm.ScaleDpi == 200 && vm.Images[1].Scale.Dpi == 300, "New imports use the saved DPI default without changing previous images");
        vm.ScaleDpi = 300; widthControl.Value = 10; Dispatcher.UIThread.RunJobs();
        check(vm.ScalePercent > 100000 && vm.Images.Last().Scale.GetSize(1, 1) == new ImageDimensions(1181, 1181),
            "Valid cm sizes for very small originals are not clamped by the hidden percentage control");
        window.Close();
        var mixed = new BatchExportService().Export([first, second], folder, 0, null, CancellationToken.None, imageScales:
            new Dictionary<string, ImageScale> { [first] = new(1_000_000), [second] = new(50, 254) });
        check(mixed.ExportedImages == 1 && mixed.Failures.Single().Error is ExportSizeLimitException,
            "Batch reports an invalid individual scale and continues exporting other images");
        string legacy = Path.Combine(folder, "legacy-settings"); Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "{\"Language\":\"en\"}");
        check(new SettingsService(legacy).Load() is { DefaultExportDpi: 300, ScaleUnit: 0 }, "Existing settings without scale preferences retain safe defaults");
        File.WriteAllText(Path.Combine(legacy, "settings.json"), "{\"DefaultExportDpi\":5000,\"ScaleUnit\":99}");
        check(new SettingsService(legacy).Load() is { DefaultExportDpi: 2400, ScaleUnit: 0 }, "Invalid saved DPI and unit are bounded when loading settings");
    }

    private static void CheckDpi(byte[] png, int dpi, Action<bool, string> check)
    {
        int found = 0;
        for (int offset = 8; offset < png.Length;)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset)));
            if (png.AsSpan(offset + 4, 4).SequenceEqual("pHYs"u8))
            {
                uint expected = (uint)Math.Round(dpi / 0.0254, MidpointRounding.AwayFromZero);
                check(length == 9 && BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 8)) == expected
                    && BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 12)) == expected && png[offset + 16] == 1,
                    "PNG stores matching horizontal and vertical resolution in pixels per meter");
                if (dpi is 300 or 254)
                    check(BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + 17)) == (dpi == 300 ? 0x78A53F76u : 0x94695119u),
                        "Resolution metadata CRC matches independently calculated known values");
                found++;
            }
            offset += length + 12;
        }
        check(found == 1, "Exported PNG contains exactly one physical-resolution chunk");
    }
    private static void Expect<T>(Action action, Action<bool, string> check) where T : Exception
    {
        try { action(); } catch (T) { check(true, "Expected " + typeof(T).Name); return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
    private sealed class Details : IDetailPreviewService
    {
        public DetailPreviewViewModel? Preview { get; private set; }
        public void Show(DetailPreviewViewModel preview) => Preview = preview;
    }
}
