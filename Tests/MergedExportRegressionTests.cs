using System.Buffers.Binary;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class MergedExportRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "merged-export"); Directory.CreateDirectory(folder);
        using var source = new RasterImage(4, 2);
        source[0, 0] = source[0, 1] = new(240, 20, 90);
        source[1, 0] = source[1, 1] = new(20, 170, 220);
        source[2, 0] = new(10, 200, 30); source[2, 1] = new(0, 0, 0);
        source[3, 0] = new(250, 250, 250, 0); source[3, 1] = new(240, 20, 90, 127);
        var service = new ColorSeparationService();
        var analysis = service.Analyze(source, 0, CancellationToken.None, 128);
        var selected = analysis.Groups.Where(g => g.R == 240 || g.B == 220).Select(g => g.Index).ToHashSet();
        foreach (var area in Enum.GetValues<ExportArea>())
        foreach (var background in Enum.GetValues<ExportBackground>())
        foreach (decimal percent in new[] { 100m, 150m })
        {
            var options = new ExportOptions(area, background); var sizing = new ImageScale(percent, 300);
            string export = service.Export(analysis, folder, "subject.png", null, CancellationToken.None, selected, options, sizing, combineGroups: true);
            string path = Directory.GetFiles(export).Single();
            check(Path.GetFileName(path) == "subject_merged.png", "Merged export produces one clearly named PNG instead of separate color files");
            using var actual = RasterImageCodec.Load(path);
            using var preview = RasterImageCodec.Load(service.CombinedPreview(analysis, selected, options, sizing, fullResolution: true));
            var size = sizing.GetSize(4, 2);
            check(actual.Width == size.Width && actual.Height == size.Height, "Merged export applies the image's proportional target size");
            check(HasDpi(File.ReadAllBytes(path), 11811), "Merged PNG preserves physical print resolution");
            for (int y = 0; y < size.Height; y++)
            for (int x = 0; x < size.Width; x++)
            {
                int label = analysis.Labels[(y * 2 / size.Height) * 4 + x * 4 / size.Width];
                RgbaPixel expected = selected.Contains(label) ? Foreground(area, analysis.Groups.Single(g => g.Index == label)) : Background(background);
                check(actual[x, y] == expected && preview[x, y] == expected,
                    "Union first, styling last: every merged pixel matches selected memberships, foreground, background and alpha");
            }
        }
        string first = service.Export(analysis, folder, "repeat.png", null, CancellationToken.None, selected, combineGroups: true);
        string second = service.Export(analysis, folder, "repeat.png", null, CancellationToken.None, selected, combineGroups: true);
        check(first != second && Directory.GetFiles(first).Length == 1, "Repeated merged export creates a fresh folder instead of overwriting earlier output");
        Expect<NoColorsSelectedException>(() => service.Export(analysis, folder, "empty.png", null, CancellationToken.None, new HashSet<int>(), combineGroups: true), check);
        using (var cancellation = new CancellationTokenSource())
        {
            int count = Directory.GetDirectories(folder).Length;
            Expect<OperationCanceledException>(() => service.Export(analysis, folder, "cancelled.png", new Reporter(_ => cancellation.Cancel()),
                cancellation.Token, selected, combineGroups: true), check);
            check(Directory.GetDirectories(folder).Length == count && !Directory.GetDirectories(folder, "*.tmp").Any(),
                "Cancellation after merging but before commit removes temporary output without publishing a partial PNG");
        }
        string[] inputs = Enumerable.Range(1, 3).Select(i => Path.Combine(folder, $"image-{i}.png")).ToArray();
        foreach (string path in inputs) File.WriteAllBytes(path, RasterImageCodec.EncodePng(source));
        var details = new Details();
        using var vm = new MainViewModel(new Dialogs(folder), new SettingsService(Path.Combine(folder, "settings")), details);
        var window = new MainWindow { DataContext = vm }; window.Show(); vm.LanguageIndex = 1;
        Task load = vm.AddImagesAsync(inputs); pump(() => load.IsCompleted);
        check(!vm.ExportCombinedBatchCommand.CanExecute(null), "Merged export is disabled before any image has checked colors");
        vm.Tolerance = 0; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        Select(vm, g => g.R == 240 || g.B == 220); vm.ScalePercent = 200;
        vm.ExportAreaIndex = (int)ExportArea.Black; vm.ExportBackgroundIndex = (int)ExportBackground.Transparent;
        var previewToggle = window.FindControl<Avalonia.Controls.Primitives.ToggleButton>("CombinedPreviewToggle")!;
        check(Grid.GetColumn(previewToggle) == 1 && Equals(previewToggle.Content, "Merge selected colors"),
            "Merged preview is a right-aligned toolbar button with a meaningful localized label");
        previewToggle.IsChecked = true; Dispatcher.UIThread.RunJobs();
        using (var mask = Read(vm.Mask!))
            check(mask[0, 0] == new RgbaPixel(0, 0, 0) && mask[2, 0] == new RgbaPixel(0, 0, 0) && mask[4, 0].A == 0,
                "Merged preview combines all checked colors without painting an opaque background over an earlier layer");
        vm.Groups.Single(g => g.Group.B == 220).ExportEnabled = false;
        using (var mask = Read(vm.Mask!)) check(mask[2, 0].A == 0 && mask[0, 0].A == 255, "Checkbox edits update the merged preview immediately");
        vm.Groups.Single(g => g.Group.B == 220).ExportEnabled = true;
        vm.DetailPreviewCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var detail = details.Preview!)
        using (var mask = Read(detail.Mask))
        using (var overlay = Read(detail.Overlay))
            check(mask.Width == 8 && mask.Height == 4 && mask[2, 0].A == 255 && overlay[2, 0].G == 255 && overlay[4, 0].A == 0,
                "Merged detail view and green overlay use the same selected union at final export size");
        vm.SelectedImage = vm.Images[1]; pump(() => !vm.IsBusy);
        Select(vm, g => g.G == 200); vm.ScalePercent = 50;
        vm.SelectedImage = vm.Images[2]; pump(() => !vm.IsBusy);
        vm.DeselectAllColorsCommand.Execute(null);
        check(!vm.HasMask && !vm.DetailPreviewCommand.CanExecute(null) && vm.ExportCombinedBatchCommand.CanExecute(null),
            "Empty active selection clears its merged preview while other selected images remain exportable");
        vm.ExportCombinedBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        var files = Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories);
        check(files.Length == 2 && files.All(p => p.EndsWith("_merged.png")) && vm.Status.Contains("1 skipped") && !vm.HasErrors,
            "Merged batch writes exactly one PNG per selected image and skips an image with no checked colors");
        foreach (string path in files)
        {
            using var mask = RasterImageCodec.Load(path);
            bool a = Path.GetFileName(path).StartsWith("image-1_");
            check(mask.Width == (a ? 8 : 2) && mask.Height == (a ? 4 : 1), "Merged batch respects each image's own size");
            if (a) check(mask[0, 0].A == 255 && mask[2, 0].A == 255 && mask[4, 0].A == 0, "First merged image contains only its own two checked areas");
            else check(mask[0, 0].A == 0 && mask[1, 0].A == 255, "Second merged image contains only its own differently checked area");
        }
        vm.LanguageIndex = 0; Dispatcher.UIThread.RunJobs();
        check(window.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Kombiniert exportieren")),
            "Merged export button follows the selected UI language");
        vm.SelectedImage = vm.Images[0]; pump(() => !vm.IsBusy); vm.DeselectAllColorsCommand.Execute(null);
        vm.SelectedImage = vm.Images[1]; pump(() => !vm.IsBusy); vm.DeselectAllColorsCommand.Execute(null);
        check(!vm.ExportCombinedBatchCommand.CanExecute(null) && !vm.ExportBatchCommand.CanExecute(null),
            "Both batch export modes are disabled when all images have no checked colors");
        window.Close();
    }

    private static void Select(MainViewModel vm, Func<ColorGroup, bool> predicate)
    { foreach (var group in vm.Groups) group.ExportEnabled = predicate(group.Group); }
    private static RgbaPixel Foreground(ExportArea area, ColorGroup group) => area switch
    {
        ExportArea.Black => new(0, 0, 0), ExportArea.White => new(255, 255, 255),
        ExportArea.Transparent => new(0, 0, 0, 0), _ => new(group.R, group.G, group.B)
    };
    private static RgbaPixel Background(ExportBackground background) => background switch
    {
        ExportBackground.Black => new(0, 0, 0), ExportBackground.Gray => new(128, 128, 128),
        ExportBackground.White => new(255, 255, 255), _ => new(0, 0, 0, 0)
    };
    private static bool HasDpi(byte[] png, uint expected)
    {
        for (int i = 8; i < png.Length; i += checked((int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(i))) + 12)
            if (png.AsSpan(i + 4, 4).SequenceEqual("pHYs"u8))
                return BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(i + 8)) == expected
                    && BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(i + 12)) == expected && png[i + 16] == 1;
        return false;
    }
    private static RasterImage Read(Bitmap bitmap)
    { using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default); return RasterImageCodec.Load(stream.ToArray()); }
    private static void Expect<T>(Action action, Action<bool, string> check) where T : Exception
    { try { action(); } catch (T) { check(true, "Expected " + typeof(T).Name); return; } throw new Exception("Expected " + typeof(T).Name); }
    private sealed class Reporter(Action<int> report) : IProgress<int> { public void Report(int value) => report(value); }
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
