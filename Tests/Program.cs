using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

var root = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "test-runs", "HappyColorSplit_tests_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks = 0;
try
{
    var service = new ColorSeparationService();
    using var fixture = new RasterImage(4, 2);
    fixture[0, 0] = new(255, 0, 0); fixture[1, 0] = new(250, 3, 0);
    fixture[2, 0] = new(0, 0, 255); fixture[3, 0] = new(0, 0, 255);
    fixture[0, 1] = new(255, 0, 0); fixture[1, 1] = new(255, 0, 0, 128);
    fixture[2, 1] = new(0, 0, 0); fixture[3, 1] = new(0, 255, 0, 0);
    string input = Path.Combine(root, "fixture.png"); fixture.SaveAsPng(input);
    using var loaded = service.Load(input);
    Check(loaded.Width == 4 && loaded.Height == 2, "PNG loading");
    var result = service.Analyze(loaded, 10, CancellationToken.None);
    Check(result.Groups.Count == 3, "Similar reds form one group, blue and black remain separate");
    Check(result.Groups.Sum(g => g.PixelCount) == 7, "Transparent pixels excluded, semitransparent included");
    Check(result.Labels[7] == -1, "Transparent pixel has no label");
    Check(result.Labels[0] == result.Labels[1] && result.Labels[0] == result.Labels[5], "Near colors and partial alpha assigned correctly");
    Check(service.Analyze(loaded, 0, CancellationToken.None).Groups.Count == 4, "Zero tolerance preserves distinct RGB colors");
    var again = service.Analyze(loaded, 10, CancellationToken.None);
    Check(result.Labels.SequenceEqual(again.Labels), "Grouping is reproducible");
    using (var roundtrip = new RasterImage(17, 3))
    {
        byte[] alphaValues = [1, 2, 31, 127, 128, 254, 255];
        for (int y = 0; y < roundtrip.Height; y++)
        for (int x = 0; x < roundtrip.Width; x++)
            roundtrip[x, y] = new((byte)(x * 13 + y), (byte)(255 - x * 11), (byte)(y * 77 + x), alphaValues[(x + y) % alphaValues.Length]);
        using var decoded = RasterImageCodec.Load(RasterImageCodec.EncodePng(roundtrip));
        for (int y = 0; y < roundtrip.Height; y++)
            Check(roundtrip.GetRowSpan(y).SequenceEqual(decoded.GetRowSpan(y)), "PNG roundtrip retains exact straight-alpha RGB even at alpha 1");
    }
    using (var firstFrame = new RasterImage(2, 1, new RgbaPixel(255, 0, 0)))
    using (var secondFrame = new RasterImage(2, 1, new RgbaPixel(0, 255, 0)))
    {
        byte[] apng = EncodedImageFixtures.AnimatedPng(RasterImageCodec.EncodePng(firstFrame), RasterImageCodec.EncodePng(secondFrame));
        using var decoded = RasterImageCodec.Load(apng);
        Check(decoded[0, 0] == new RgbaPixel(255, 0, 0) && decoded[1, 0] == new RgbaPixel(255, 0, 0), "Animated PNG uses first frame only");
    }
    using (var alphaFixture = new RasterImage(6, 1))
    {
        byte[] alphas = [0, 1, 127, 128, 254, 255];
        for (int x = 0; x < 6; x++) alphaFixture[x, 0] = new(255, 0, 0, alphas[x]);
        var filtered = service.Analyze(alphaFixture, 10, CancellationToken.None, 128);
        Check(filtered.Groups.Single().PixelCount == 3 && filtered.Labels.SequenceEqual([-1, -1, -1, 0, 0, 0]), "Opacity threshold is inclusive and excludes faint pixels");
        Check(service.Analyze(alphaFixture, 10, CancellationToken.None, 254).Groups.Single().PixelCount == 2, "Near-opaque threshold retains alpha 254 and 255");
        using var allFaint = new RasterImage(1, 1, new RgbaPixel(255, 0, 0, 127));
        Throws<EmptyImageException>(() => service.Analyze(allFaint, 10, CancellationToken.None, 128));
    }
    string export = service.Export(result, root, input, null, CancellationToken.None);
    var files = Directory.GetFiles(export, "*.png").Order().ToArray();
    Check(files.Length == 3, "One PNG per group");
    for (int i = 0; i < files.Length; i++)
    {
        using var mask = RasterImageCodec.Load(files[i]);
        byte[] pngHeader = File.ReadAllBytes(files[i]);
        Check(pngHeader[24] == 8 && pngHeader[25] == 0, "Export remains an 8-bit grayscale PNG");
        Check(mask.Width == 4 && mask.Height == 2, "Export preserves dimensions");
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 4; x++)
        {
            byte expected = result.Labels[y * 4 + x] == i ? (byte)255 : (byte)0;
            var p = mask[x, y];
            Check(p.R == expected && p.G == expected && p.B == expected && p.A == 255, "Pixel-perfect opaque white-on-black mask");
        }
    }
    Check(service.Export(result, root, input, null, CancellationToken.None) != export, "Repeated export never overwrites");
    int blueGroup = result.Groups.Single(g => g.B == 255).Index;
    var exportProgress = new List<int>();
    string selectedExport = service.Export(result, root, input, new TestProgress<int>(p => exportProgress.Add(p)), CancellationToken.None, new HashSet<int> { blueGroup });
    Check(Directory.GetFiles(selectedExport, "*.png").Length == 1 && Directory.GetFiles(selectedExport).Single().EndsWith("0000FF.png"), "Only checked color is exported with original color identity");
    Check(exportProgress.SequenceEqual([100]), "Filtered export progress reaches 100 percent");
    using (var blueMask = RasterImageCodec.Load(Directory.GetFiles(selectedExport).Single()))
        Check(blueMask[2, 0].R == 255 && blueMask[0, 0].R == 0, "Unchecked colors remain black rather than being reassigned");
    Throws<NoColorsSelectedException>(() => service.Export(result, root, input, null, CancellationToken.None, new HashSet<int>()));
    using (var cancelled = new CancellationTokenSource())
    {
        cancelled.Cancel();
        Throws<OperationCanceledException>(() => service.Analyze(loaded, 10, cancelled.Token));
        Throws<OperationCanceledException>(() => service.Export(result, root, input, null, cancelled.Token));
        Check(!Directory.GetDirectories(root, "*.tmp").Any(), "Cancelled export cleans staging directory");
    }
    using (var transparent = new RasterImage(2, 2))
        Throws<EmptyImageException>(() => service.Analyze(transparent, 10, CancellationToken.None));
    using (var colors = new RasterImage(257, 1))
    {
        for (int x = 0; x < 257; x++) colors[x, 0] = new((byte)(x & 255), (byte)(x >> 8), 0);
        Throws<ColorLimitException>(() => service.Analyze(colors, 0, CancellationToken.None));
    }
    using (var large = new RasterImage(4001, 4000))
    {
        string largePath = Path.Combine(root, "large.png"); large.SaveAsPng(largePath);
        Throws<ImageLimitException>(() => service.Load(largePath).Dispose());
    }
    string jpgPath = Path.Combine(root, "fixture.jpg"); fixture.SaveAsJpeg(jpgPath);
    using (var jpg = service.Load(jpgPath)) Check(jpg.Width == 4 && jpg.Height == 2, "JPG loading");
    using (var orientationFixture = new RasterImage(3, 2))
    {
        for (int y = 0; y < 2; y++)
        for (int x = 0; x < 3; x++) orientationFixture[x, y] = new((byte)(x * 85), (byte)(y * 180), (byte)(x * 30 + y * 40));
        string orientationPath = Path.Combine(root, "orientation.jpg"); orientationFixture.SaveAsJpeg(orientationPath);
        byte[] jpegBytes = File.ReadAllBytes(orientationPath);
        using var original = RasterImageCodec.Load(jpegBytes);
        int[][] expected = [[0,1,2,3,4,5], [2,1,0,5,4,3], [5,4,3,2,1,0], [3,4,5,0,1,2],
            [0,3,1,4,2,5], [3,0,4,1,5,2], [5,2,4,1,3,0], [2,5,1,4,0,3]];
        for (byte orientation = 1; orientation <= 8; orientation++)
        {
            using var oriented = RasterImageCodec.Load(EncodedImageFixtures.WithExifOrientation(jpegBytes, orientation));
            Check(oriented.Width == (orientation >= 5 ? 2 : 3) && oriented.Height == (orientation >= 5 ? 3 : 2), "EXIF orientation dimensions: " + orientation);
            for (int i = 0; i < 6; i++)
            {
                int source = expected[orientation - 1][i];
                Check(oriented[i % oriented.Width, i / oriented.Width] == original[source % 3, source / 3], "EXIF rotation/mirroring preserves pixels: " + orientation);
            }
        }
    }
    using (var preview = RasterImageCodec.Load(service.MaskPreview(result, 0)))
        Check(preview[0, 0].R == 255 && preview[3, 1].R == 0, "Mask preview matches export");
    using (var overlay = RasterImageCodec.Load(service.OverlayPreview(result, 0, CancellationToken.None)))
        Check(overlay[0, 0].A == 255 && overlay[0, 0].G == 255 && overlay[2, 0].A == 0 && overlay[3, 1].A == 0, "Overlay marks selected group only; other pixels are transparent");

    string otherDirectory = Path.Combine(root, "other"); Directory.CreateDirectory(otherDirectory);
    string sameName = Path.Combine(otherDirectory, "fixture.png");
    using (var green = new RasterImage(2, 1, new RgbaPixel(0, 255, 0))) green.SaveAsPng(sameName);
    string badFile = Path.Combine(root, "broken.png"); File.WriteAllText(badFile, "not an image");
    var batchService = new BatchExportService();
    var batch = batchService.Export([input, badFile, sameName], root, 10, null, CancellationToken.None);
    Check(batch.ExportedImages == 2 && batch.ExportedMasks == 4, "Batch exports independent palettes for all valid images");
    Check(batch.Failures.Count == 1 && batch.Failures[0].FilePath == badFile, "Batch reports corrupt file and continues");
    Check(Directory.GetDirectories(batch.Folder).Length == 2, "Identical basenames receive separate output folders");
    Check(Directory.GetFiles(batch.Folder, "*.png", SearchOption.AllDirectories).Length == 4, "Batch writes all masks without overwrites");
    var selections = new Dictionary<string, IReadOnlySet<string>> { [input] = new HashSet<string> { "#0000FF" } };
    var filteredBatch = batchService.Export([input, sameName], root, 10, null, CancellationToken.None, 128, selections);
    Check(filteredBatch.ExportedImages == 1 && filteredBatch.ExportedMasks == 1 && filteredBatch.Failures.Count == 0 && filteredBatch.SkippedImages == 1, "Batch exports only explicit per-image selections and skips unreviewed images");
    var emptySelectionBatch = batchService.Export([input, sameName], root, 10, null, CancellationToken.None, 128,
        new Dictionary<string, IReadOnlySet<string>> { [input] = new HashSet<string>() });
    Check(emptySelectionBatch.ExportedImages == 0 && emptySelectionBatch.Failures.Count == 0 && emptySelectionBatch.SkippedImages == 2, "Batch skips empty and missing selections without exporting fallback colors");
    string faintPath = Path.Combine(root, "faint.png");
    using (var faint = new RasterImage(2, 1))
    {
        faint[0, 0] = new(255, 0, 0, 127); faint[1, 0] = new(255, 0, 0, 128);
        faint.SaveAsPng(faintPath);
    }
    var alphaBatch = batchService.Export([faintPath], root, 10, null, CancellationToken.None, 128);
    using (var mask = RasterImageCodec.Load(Directory.GetFiles(alphaBatch.Folder, "*.png", SearchOption.AllDirectories).Single()))
        Check(mask[0, 0].R == 0 && mask[1, 0].R == 255, "Batch export applies opacity threshold to actual output pixels");
    foreach (string maskPath in Directory.GetFiles(batch.Folder, "*.png", SearchOption.AllDirectories))
    {
        using var mask = RasterImageCodec.Load(maskPath);
        if (mask.Width == 2)
            Check(mask[0, 0] == new RgbaPixel(255, 255, 255) && mask[1, 0] == new RgbaPixel(255, 255, 255), "Second image mask is white on its own canvas");
        else
        {
            var group = result.Groups.Single(g => maskPath.EndsWith(g.Hex[1..] + ".png"));
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 4; x++)
                Check(mask[x, y].R == (result.Labels[y * 4 + x] == group.Index ? 255 : 0), "Batch preserves exact first-image mask pixels");
        }
    }
    using (var cancellation = new CancellationTokenSource())
    {
        var progress = new TestProgress<BatchProgress>(p => { if (p.FileNumber == 2) cancellation.Cancel(); });
        var partial = batchService.Export([input, sameName], root, 10, progress, cancellation.Token);
        Check(partial.Cancelled && partial.ExportedImages == 1 && partial.ExportedMasks == 3, "Batch cancellation preserves finished images and reports counts");
        Check(Directory.GetDirectories(partial.Folder).Length == 1 && !Directory.GetDirectories(partial.Folder, "*.tmp").Any(), "Cancellation leaves no incomplete image exports");
    }

    var settings = new SettingsService(Path.Combine(root, "settings"));
    Check(settings.Save(new AppSettings { Language = "en", Tolerance = 17, MinimumAlpha = 201 }), "JSON settings saved");
    Check(settings.Load() is { Language: "en", Tolerance: 17, MinimumAlpha: 201 }, "JSON settings with opacity restored");
    File.WriteAllText(Path.Combine(root, "settings", "settings.json"), "{\"MinimumAlpha\":1000}");
    Check(settings.Load().MinimumAlpha == 254, "Opacity setting validated on load");
    File.WriteAllText(Path.Combine(root, "settings", "settings.json"), "invalid");
    Check(settings.Load().Language == "de", "Invalid settings recover safely");
    string legacySettingsDirectory = Path.Combine(root, "legacy-settings");
    var legacySettings = new SettingsService(legacySettingsDirectory);
    Check(legacySettings.Save(new AppSettings { Language = "en", Tolerance = 140, MinimumAlpha = 177 }), "Legacy settings fixture saved");
    var renamedSettings = new SettingsService(Path.Combine(root, "renamed-settings"), legacySettingsDirectory);
    var inherited = renamedSettings.Load();
    Check(inherited is { Language: "en", Tolerance: 140, MinimumAlpha: 177 }, "Previous application settings inherited when new settings are absent");
    inherited.MinimumAlpha = 128;
    Check(renamedSettings.Save(inherited) && renamedSettings.Load().MinimumAlpha == 128, "New application settings take precedence after save");
    Check(legacySettings.Load().MinimumAlpha == 177, "Migrating settings leaves previous settings intact");

    AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).UseSkia().WithInterFont().SetupWithoutStarting();
    var dialogs = new TestDialogs(input, root) { Inputs = [input, jpgPath] };
    var detailService = new TestDetailPreviewService();
    var licenseService = new TestLicenseWindowService();
    using var vm = new MainViewModel(dialogs, settings, detailService, licenseService);
    Check(vm.MinimumAlpha == 128, "New opacity setting defaults to 128");
    var window = new MainWindow { DataContext = vm };
    window.Show(); Dispatcher.UIThread.RunJobs();
    Check(window.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Bilder hinzufügen")), "AXAML bindings display German text");
    vm.LanguageIndex = 1; Dispatcher.UIThread.RunJobs();
    Check(window.GetVisualDescendants().OfType<Button>().Any(b => Equals(b.Content, "Add images")), "Language switch updates AXAML bindings");
    var licenseButton = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Licenses & notices"));
    licenseButton.Command!.Execute(null);
    Check(licenseService.Language == "en" && !vm.IsBusy, "Main window opens license viewer in current language without starting image processing");
    LicenseRegressionTests.Run(window, root, Check, Pump);
    Check(!vm.ExportCommand.CanExecute(null), "Export disabled before analysis");
    vm.OpenCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.HasImage && vm.Original is not null && vm.AnalyzeCommand.CanExecute(null), "Open command loads preview and enables analysis");
    Check(vm.Images.Count == 2 && vm.Images.All(i => i.Thumbnail.PixelSize.Width > 0), "Multiple selected files appear with thumbnails");
    vm.Tolerance = 10;
    vm.AnalyzeCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.Groups.Count == 3 && vm.HasMask && vm.ExportCommand.CanExecute(null), "Analysis command populates groups, mask and export");
    vm.ExportCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.Status.StartsWith("3 PNGs saved:"), "Export command completes and reports output");
    vm.DetailPreviewCommand.Execute(null); Pump(() => !vm.IsBusy);
    using (var detail = detailService.Preview!)
    {
        Check(detail is not null && detail.Original.PixelSize.Width == 4 && detail.Mask.PixelSize.Height == 2, $"Detail view retains full source resolution; status={vm.Status}; original={detail?.Original.PixelSize}; mask={detail?.Mask.PixelSize}");
        var detailWindow = new DetailPreviewWindow { DataContext = detail };
        detailWindow.Show(); Dispatcher.UIThread.RunJobs();
        detail!.Zoom = 400;
        Check(detail.ImageWidth == 16 && detail.ImageHeight == 8, "Zoom updates canvas dimensions in original pixel units");
        detail.ShowOverlay = false; Dispatcher.UIThread.RunJobs();
        Check(detailWindow.GetVisualDescendants().OfType<Avalonia.Controls.Image>().Count(image => image.IsVisible) == 1, "Black-and-white detail mode hides original and overlay");
        detail.ShowOverlay = true; Dispatcher.UIThread.RunJobs();
        Check(detailWindow.GetVisualDescendants().OfType<Avalonia.Controls.Image>().Count(image => image.IsVisible) == 2, "Overlay mode displays original and selected overlay");
        detailWindow.Close();
    }
    vm.DeselectAllColorsCommand.Execute(null);
    Check(vm.ExportColorCount == 0 && !vm.ExportCommand.CanExecute(null), "Export disabled when no colors are checked");
    vm.Groups.Single(g => g.Group.B == 255).ExportEnabled = true;
    Check(vm.ExportColorCount == 1 && vm.ExportCommand.CanExecute(null), "Checking one color enables single-color export");
    vm.SelectedImage = vm.Images[1]; Pump(() => !vm.IsBusy);
    Check(vm.FileName == "fixture.jpg" && vm.HasImage && vm.HasMask && vm.Groups.Count > 0, "Image selection restores its own recognized colors and mask without another Find colors click");
    vm.AnalyzeCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.HasGroups && vm.HasMask, "Second image can be analyzed independently");
    vm.SelectedImage = vm.Images[0]; Pump(() => !vm.IsBusy);
    vm.AnalyzeCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.ExportColorCount == 1 && vm.Groups.Single(g => g.ExportEnabled).Group.B == 255, "Per-image color selection survives switching and reanalysis");
    vm.MinimumAlpha = 200;
    Check(vm.Groups.Count == 0 && vm.Images.All(i => i.ExportColors is null), "Opacity change invalidates all cached color selections");
    vm.MinimumAlpha = 128;
    vm.Tolerance = 20;
    Check(vm.Groups.Count == 0 && !vm.HasMask && !vm.ExportCommand.CanExecute(null), "Changing tolerance invalidates stale analysis");
    Check(!vm.ExportBatchCommand.CanExecute(null), "Batch export requires checked colors after detection settings invalidate selections");
    vm.AnalyzeCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.ExportBatchCommand.CanExecute(null), "One Find colors command prepares all images for batch export");
    vm.ExportBatchCommand.Execute(null);
    Check(vm.IsBusy && !vm.ClearImagesCommand.CanExecute(null), "Cannot change batch list during export");
    Pump(() => !vm.IsBusy);
    Check(vm.Status.StartsWith("2 images ·") && !vm.HasErrors, "Batch command exports both images directly");
    dialogs.Inputs = [input, badFile];
    vm.OpenCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.Images.Count == 2 && vm.HasErrors && vm.ErrorDetails.Contains("broken.png"), "Adding files ignores duplicates and exposes load failures");
    dialogs.Inputs = [];
    vm.OpenCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.Images.Count == 2, "Cancelling multi-file picker preserves list");
    vm.RemoveImageCommand.Execute(null); Pump(() => !vm.IsBusy);
    Check(vm.Images.Count == 1 && vm.HasImage && vm.FileName == "fixture.jpg", "Removing selected image switches to remaining image");
    vm.ClearImagesCommand.Execute(null);
    Check(vm.Images.Count == 0 && !vm.HasImage && !vm.HasMask && !vm.ExportBatchCommand.CanExecute(null), "Clearing list releases images and disables export");
    window.Close();
    ImageInputRegressionTests.Run(root, Check, Pump);
    ExportFolderRegressionTests.Run(root, Check, Pump);
    ColorExportRegressionTests.Run(root, Check, Pump);
    BackgroundExportRegressionTests.Run(root, Check, Pump);
    ScalingRegressionTests.Run(root, Check, Pump);
    BatchSelectionRegressionTests.Run(root, Check, Pump);
    MergedExportRegressionTests.Run(root, Check, Pump);
    LaserRegressionTests.Run(root, Check, Pump);
    LaserNestingRegressionTests.Run(Check);
    LaserRulerRegressionTests.Run(Check);
    if (args.Contains("--capture-baseline") || args.Contains("--verify-baseline"))
    {
        string importDirectory = Path.GetFullPath(Path.Combine("..", "Import"));
        string baselinePath = Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "image-processing-baseline.json");
        var fingerprints = new SortedDictionary<string, string>();
        foreach (string path in Directory.GetFiles(importDirectory, "*.png").Order())
        {
            using var source = service.Load(path);
            foreach (byte alpha in new byte[] { 1, 128 })
            {
                var analysis = service.Analyze(source, 140, CancellationToken.None, alpha);
                byte[] labels = new byte[analysis.Labels.Length * sizeof(int)];
                Buffer.BlockCopy(analysis.Labels, 0, labels, 0, labels.Length);
                string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(labels));
                fingerprints[$"{Path.GetFileName(path)}|alpha={alpha}"] = $"{analysis.Width}x{analysis.Height}|{hash}|" +
                    string.Join(";", analysis.Groups.Select(g => $"{g.Index}:{g.Hex}:{g.PixelCount}"));
            }
        }
        if (args.Contains("--capture-baseline"))
            File.WriteAllText(baselinePath, System.Text.Json.JsonSerializer.Serialize(fingerprints, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        else
        {
            var expected = System.Text.Json.JsonSerializer.Deserialize<SortedDictionary<string, string>>(File.ReadAllText(baselinePath))!;
            Check(fingerprints.Count == expected.Count, "Regression baseline image inventory matches");
            foreach (var entry in fingerprints)
                Check(expected.TryGetValue(entry.Key, out string? value) && value == entry.Value, "Exact image regression match: " + entry.Key);
        }
        Console.WriteLine($"Image regression baseline: {fingerprints.Count} full-image label and palette fingerprints.");
    }
    Console.WriteLine($"PASS: {checks} checks including image grouping, exact export pixels, batch export, cancellation, settings and multi-image UI workflow.");
}
finally { Directory.Delete(root, true); }

void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    checks++;
}
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { checks++; return; }
    throw new Exception("FAIL: expected " + typeof(T).Name);
}
static void Pump(Func<bool> completed)
{
    var timeout = DateTime.UtcNow.AddSeconds(30);
    while (!completed())
    {
        Dispatcher.UIThread.RunJobs();
        if (DateTime.UtcNow > timeout) throw new TimeoutException("UI operation did not finish");
        Thread.Sleep(10);
    }
    Dispatcher.UIThread.RunJobs();
}
sealed class TestDialogs(string input, string output) : IFileDialogService
{
    public IReadOnlyList<string> Inputs { get; set; } = [input];
    public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult(Inputs);
    public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(output);
}
sealed class TestProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}
sealed class TestDetailPreviewService : IDetailPreviewService
{
    public DetailPreviewViewModel? Preview { get; private set; }
    public void Show(DetailPreviewViewModel preview) => Preview = preview;
}
sealed class TestLicenseWindowService : ILicenseWindowService
{
    public string? Language { get; private set; }
    public Task ShowAsync(Texts texts) { Language = texts.Language; return Task.CompletedTask; }
}
