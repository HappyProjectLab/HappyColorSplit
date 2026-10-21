using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ColorExportRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "color-export"); Directory.CreateDirectory(folder);
        string input = Path.Combine(folder, "colors.png");
        using var source = new RasterImage(4, 2);
        source[0, 0] = new(30, 180, 220); source[1, 0] = new(31, 180, 220);
        source[2, 0] = new(250, 40, 100); source[3, 0] = new(0, 0, 0);
        source[0, 1] = new(30, 180, 220); source[1, 1] = new(30, 180, 220, 128);
        source[2, 1] = new(250, 40, 100, 127); source[3, 1] = new(250, 40, 100, 0);
        source.SaveAsPng(input);
        var service = new ColorSeparationService();
        var analysis = service.Analyze(source, 10, CancellationToken.None, 128);
        var options = new ExportOptions(Area: ExportArea.GroupColor);
        string export = service.Export(analysis, folder, input, null, CancellationToken.None, options: options);
        foreach (var group in analysis.Groups)
        {
            string path = Directory.GetFiles(export).Single(p => p.EndsWith(group.Hex[1..] + ".png"));
            using var actual = RasterImageCodec.Load(path);
            using var preview = RasterImageCodec.Load(service.MaskPreview(analysis, group.Index, fullResolution: true, options: options));
            check(actual.Width == 4 && actual.Height == 2, "Color export preserves original dimensions");
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 4; x++)
            {
                var expected = analysis.Labels[y * 4 + x] == group.Index
                    ? new RgbaPixel(group.R, group.G, group.B) : new RgbaPixel(0, 0, 0);
                check(actual[x, y] == expected && preview[x, y] == expected,
                    "Color export and full-resolution preview use exact group RGB with opaque black outside the mask");
            }
        }
        var turquoise = analysis.Groups.Single(g => g.R == 30);
        string single = service.Export(analysis, folder, input, null, CancellationToken.None, new HashSet<int> { turquoise.Index }, options);
        check(Directory.GetFiles(single).Length == 1, "Color export retains selected-group filtering");
        var batch = new BatchExportService().Export([input], folder, 10, null, CancellationToken.None, 128,
            new Dictionary<string, IReadOnlySet<string>> { [input] = new HashSet<string> { turquoise.Hex } }, options);
        using (var actual = RasterImageCodec.Load(Directory.GetFiles(batch.Folder, "*.png", SearchOption.AllDirectories).Single()))
            check(actual[1, 0] == new RgbaPixel(30, 180, 220) && actual[2, 0] == new RgbaPixel(0, 0, 0),
                "Batch export uses the chosen reference color and selected groups");
        var settings = new SettingsService(Path.Combine(folder, "settings"));
        var detail = new Details();
        using var vm = new MainViewModel(new Dialogs(folder), settings, detail);
        var window = new MainWindow { DataContext = vm }; window.Show();
        Task load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted);
        vm.Tolerance = 10; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        foreach (var group in vm.Groups) group.ExportEnabled = group.Hex == turquoise.Hex;
        vm.SelectedGroup = vm.Groups.Single(g => g.Hex == turquoise.Hex);
        var selected = vm.SelectedGroup;
        var control = window.FindControl<ComboBox>("ExportAreaSelector")!;
        check(vm.ExportAreaIndex == 1 && control.SelectedIndex == 1, "Default export remains white rather than color");
        control.SelectedIndex = 3; Dispatcher.UIThread.RunJobs();
        check(vm.ExportAreaIndex == 3 && vm.ExportColorCount == 1 && vm.SelectedGroup == selected && vm.Groups.Count == 3,
            "Area selector preserves analysis and checked groups");
        using (var preview = Read(vm.Mask!)) check(preview[1, 0] == new RgbaPixel(30, 180, 220), "Toggling color updates the visible mask immediately");
        vm.ExportCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var actual = RasterImageCodec.Load(Directory.GetFiles(vm.LastExportFolder).Single()))
            check(actual[1, 0] == new RgbaPixel(30, 180, 220), "View-model single export passes the selected color option");
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var actual = RasterImageCodec.Load(Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories).Single()))
            check(actual[1, 0] == new RgbaPixel(30, 180, 220), "View-model batch export passes the selected color option");
        vm.DetailPreviewCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var preview = detail.Preview!)
        using (var mask = Read(preview.Mask)) check(mask[1, 0] == new RgbaPixel(30, 180, 220), "New detail views use the same color export settings");
        check(settings.Load().ExportArea == ExportArea.GroupColor, "Color export preference is saved as JSON");
        control.SelectedIndex = 1; Dispatcher.UIThread.RunJobs();
        using (var preview = Read(vm.Mask!)) check(preview[1, 0] == new RgbaPixel(255, 255, 255), "Switching back to white restores the original mask display");
        window.Close();
    }

    private static RasterImage Read(Bitmap bitmap)
    {
        using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return RasterImageCodec.Load(stream.ToArray());
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
