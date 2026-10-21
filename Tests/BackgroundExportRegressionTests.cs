using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class BackgroundExportRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "background-export"); Directory.CreateDirectory(folder);
        string input = Path.Combine(folder, "backgrounds.png");
        using var source = new RasterImage(3, 2);
        source[0, 0] = new(240, 20, 90); source[1, 0] = new(0, 0, 0); source[2, 0] = new(240, 20, 90, 128);
        source[0, 1] = new(240, 20, 90, 127); source[1, 1] = new(40, 180, 220, 0); source[2, 1] = new(241, 20, 90);
        source.SaveAsPng(input);
        var service = new ColorSeparationService();
        var analysis = service.Analyze(source, 10, CancellationToken.None, 128);
        var pink = analysis.Groups.Single(g => g.R == 240);
        foreach (bool color in new[] { false, true })
        foreach (var background in Enum.GetValues<ExportBackground>())
        {
            var options = new ExportOptions(color, background);
            string export = service.Export(analysis, folder, input, null, CancellationToken.None, options: options);
            foreach (var group in analysis.Groups)
            {
                string path = Directory.GetFiles(export).Single(p => p.EndsWith(group.Hex[1..] + ".png"));
                using var actual = RasterImageCodec.Load(path);
                using var preview = RasterImageCodec.Load(service.MaskPreview(analysis, group.Index, fullResolution: true, options: options));
                check(actual.Width == 3 && actual.Height == 2, "Every background/color combination retains source dimensions");
                for (int y = 0; y < 2; y++)
                for (int x = 0; x < 3; x++)
                {
                    var expected = Expected(background, color, group, analysis.Labels[y * 3 + x] == group.Index);
                    check(actual[x, y] == expected && preview[x, y] == expected,
                        $"Exact RGBA in export and preview: background={background}, color={color}, group={group.Hex}, pixel={x}/{y}");
                }
                if (!color && background != ExportBackground.Transparent)
                    check(File.ReadAllBytes(path)[25] == 0, "Opaque white-mask export retains the compact grayscale PNG format for every solid background");
            }
            var batch = new BatchExportService().Export([input], folder, 10, null, CancellationToken.None, 128,
                new Dictionary<string, IReadOnlySet<string>> { [input] = new HashSet<string> { pink.Hex } }, options);
            using var batchMask = RasterImageCodec.Load(Directory.GetFiles(batch.Folder, "*.png", SearchOption.AllDirectories).Single());
            check(batchMask[0, 0] == Expected(background, color, pink, true)
                && batchMask[1, 0] == Expected(background, color, pink, false)
                && batchMask[0, 1] == Expected(background, color, pink, false),
                "Batch background respects selected groups and the opacity threshold");
        }
        var settings = new SettingsService(Path.Combine(folder, "settings"));
        var detail = new Details();
        using var vm = new MainViewModel(new Dialogs(folder), settings, detail);
        var window = new MainWindow { DataContext = vm }; window.Show();
        Task load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted);
        vm.Tolerance = 10; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        vm.SelectedGroup = vm.Groups.Single(g => g.Hex == pink.Hex);
        foreach (var group in vm.Groups) group.ExportEnabled = group == vm.SelectedGroup;
        var selected = vm.SelectedGroup;
        var combo = window.GetVisualDescendants().OfType<ComboBox>().Single(c => c.Items.OfType<ComboBoxItem>().Count() == 4);
        check(combo.SelectedIndex == 0 && !vm.ExportInColor, "New settings default to white areas on black background");
        foreach (bool color in new[] { false, true })
        foreach (var background in Enum.GetValues<ExportBackground>())
        {
            vm.ExportInColor = color; combo.SelectedIndex = (int)background; Dispatcher.UIThread.RunJobs();
            using var preview = Read(vm.Mask!);
            check(vm.ExportBackgroundIndex == (int)background && preview[0, 0] == Expected(background, color, pink, true)
                && preview[1, 0] == Expected(background, color, pink, false), "Background dropdown immediately updates exact preview pixels");
            check(vm.SelectedGroup == selected && vm.ExportColorCount == 1 && vm.Groups.Count == 2,
                "Changing appearance retains recognized groups and export selections");
        }
        vm.LanguageIndex = 1; Dispatcher.UIThread.RunJobs();
        check(combo.SelectedIndex == 3 && Equals(combo.Items.OfType<ComboBoxItem>().First().Content, "Black")
            && vm.ExportOutputHelp.Contains("Transparent"), "Language change updates background labels without resetting the selection");
        vm.ExportCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var actual = RasterImageCodec.Load(Directory.GetFiles(vm.LastExportFolder).Single()))
            check(actual[0, 0] == new RgbaPixel(240, 20, 90) && actual[1, 0].A == 0, "View-model single export passes color plus transparent background");
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var actual = RasterImageCodec.Load(Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories).Single()))
            check(actual[0, 0] == new RgbaPixel(240, 20, 90) && actual[1, 0].A == 0, "View-model batch export passes color plus transparent background");
        vm.DetailPreviewCommand.Execute(null); pump(() => !vm.IsBusy);
        using (var preview = detail.Preview!)
        using (var mask = Read(preview.Mask)) check(mask[0, 0] == new RgbaPixel(240, 20, 90) && mask[1, 0].A == 0,
            "Detail-view snapshot includes actual transparency and group color");
        check(settings.Load() is { ExportInColor: true, ExportBackground: ExportBackground.Transparent }, "Background and color preferences are saved together");
        using (var restored = new MainViewModel(new Dialogs(folder), settings))
            check(restored.ExportBackgroundIndex == 3 && restored.ExportInColor, "Appearance preferences survive a restart");
        vm.ExportBackgroundIndex = -1; vm.ExportBackgroundIndex = 99;
        check(vm.ExportBackgroundIndex == 3, "Transient or invalid combo indexes do not corrupt the saved background");
        window.Close();
        string invalidDirectory = Path.Combine(folder, "invalid-settings"); Directory.CreateDirectory(invalidDirectory);
        File.WriteAllText(Path.Combine(invalidDirectory, "settings.json"), "{\"ExportBackground\":99,\"ExportInColor\":true}");
        check(new SettingsService(invalidDirectory).Load() is { ExportBackground: ExportBackground.Black, ExportInColor: true },
            "Invalid saved background recovers to black without losing other settings");
        File.WriteAllText(Path.Combine(invalidDirectory, "settings.json"), "{\"Language\":\"en\"}");
        check(new SettingsService(invalidDirectory).Load() is { ExportBackground: ExportBackground.Black, ExportInColor: false },
            "Older settings without appearance preferences retain the original defaults");
    }

    private static RgbaPixel Expected(ExportBackground background, bool color, ColorGroup group, bool selected)
    {
        if (selected) return color ? new(group.R, group.G, group.B) : new(255, 255, 255);
        return background switch
        {
            ExportBackground.Black => new(0, 0, 0), ExportBackground.Gray => new(128, 128, 128),
            ExportBackground.White => new(255, 255, 255), _ => new(0, 0, 0, 0)
        };
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
