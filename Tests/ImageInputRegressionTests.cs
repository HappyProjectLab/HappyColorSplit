using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ImageInputRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "image input with spaces ä");
        Directory.CreateDirectory(folder);
        string png = Path.Combine(folder, "first image.PNG"), jpg = Path.Combine(folder, "second image.JPEG");
        using (var image = new RasterImage(2, 2, new RgbaPixel(10, 120, 200)))
        {
            image.SaveAsPng(png);
            image.SaveAsJpeg(jpg);
        }
        string unsupported = Path.Combine(folder, "renamed.txt");
        File.Copy(png, unsupported);
        string disguised = Path.Combine(folder, "gif disguised as.png");
        // Valid 1x1 GIF: extensions alone must not allow another encoded image format.
        File.WriteAllBytes(disguised, Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"));
        string broken = Path.Combine(folder, "broken.jpg");
        File.WriteAllText(broken, "not an image");
        string directory = Path.Combine(folder, "directory.png");
        Directory.CreateDirectory(directory);
        var integration = new TestWindowsIntegration();
        var dialogs = new InputDialogs { Paths = [png, unsupported, jpg] };
        using var vm = new MainViewModel(dialogs, new SettingsService(Path.Combine(folder, "settings")), windowsIntegration: integration);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        vm.LanguageIndex = 1;
        vm.OpenCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.Images.Count == 2 && vm.HasErrors && vm.ErrorDetails.Contains("Unsupported image format"),
            "Picker and external input share the PNG/JPG/JPEG allowlist; mixed input retains valid images");
        vm.ClearImagesCommand.Execute(null);
        Task add = vm.AddImagesAsync([png, jpg, Path.Combine(folder, ".", "first image.PNG"),
            png.ToLowerInvariant(), unsupported, disguised, broken, directory, Path.Combine(folder, "missing.png")]);
        check(vm.IsBusy && !vm.EnableWindowsIntegrationCommand.CanExecute(null), "External import disables other operations while loading");
        vm.AddImagesAsync([png]).GetAwaiter().GetResult();
        pump(() => add.IsCompleted); add.GetAwaiter().GetResult();
        check(vm.Images.Count == 2 && vm.HasImage && vm.FileName == "first image.PNG",
            "Multi-file external input supports spaces, Unicode and uppercase extensions, normalizes duplicates and selects first new image");
        check(vm.ErrorDetails.Contains("renamed.txt") && vm.ErrorDetails.Contains("gif disguised as.png")
            && vm.ErrorDetails.Contains("broken.jpg") && vm.ErrorDetails.Contains("directory.png") && vm.ErrorDetails.Contains("missing.png"),
            "Unsupported, disguised, corrupt, directory and missing inputs are reported without preventing valid images from loading");
        vm.LanguageIndex = 0;
        check(vm.ErrorDetails.Contains("Nicht unterstütztes Bildformat"), "Import errors follow the selected language");
        Task duplicates = vm.AddImagesAsync([png, jpg]); pump(() => duplicates.IsCompleted);
        check(vm.Images.Count == 2 && !vm.HasErrors, "Repeated Explorer inputs do not duplicate images and clear old import failures");
        vm.ClearImagesCommand.Execute(null);
        Task invalid = vm.AddImagesAsync([unsupported]); pump(() => invalid.IsCompleted);
        check(vm.Images.Count == 0 && !vm.HasImage && vm.HasErrors, "Unsupported-only external launch does not open a file");

        var button = window.FindControl<Button>("WindowsIntegrationButton")!;
        button.Flyout!.ShowAt(button);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        var content = (StackPanel)((Flyout)button.Flyout).Content!;
        var enable = content.Children.OfType<Button>().First();
        check(enable.Command == vm.EnableWindowsIntegrationCommand, "Explorer setup flyout binds to the application view model");
        enable.Command!.Execute(null);
        check(integration.Enabled && vm.Status.Contains("eingerichtet"), "Explorer integration is explicitly enabled by a user command");
        vm.RemoveWindowsIntegrationCommand.Execute(null);
        check(!integration.Enabled && vm.Status.Contains("entfernt"), "Explorer integration can be removed from the app");
        integration.Fail = true;
        vm.EnableWindowsIntegrationCommand.Execute(null);
        check(vm.Status.Contains("test failure"), "Windows setup failures are shown without crashing the app");
        button.Flyout.Hide();
        vm.ClearImagesCommand.Execute(null);
        using var transfer = new DataTransfer();
        var storagePng = window.StorageProvider.TryGetFileFromPathAsync(new Uri(png)).GetAwaiter().GetResult();
        check(storagePng is not null, "Test storage provider resolves a local file for a real routed drop");
        transfer.Add(DataTransferItem.CreateFile(storagePng!));
        var drag = new DragEventArgs(DragDrop.DragOverEvent, transfer, window, default, KeyModifiers.None);
        window.RaiseEvent(drag);
        check(drag.DragEffects == DragDropEffects.Copy, "Window accepts supported files during drag-over");
        var drop = new DragEventArgs(DragDrop.DropEvent, transfer, window, default, KeyModifiers.None);
        window.RaiseEvent(drop); pump(() => !vm.IsBusy);
        check(vm.Images.Count == 1 && vm.HasImage && drop.DragEffects == DragDropEffects.Copy,
            "Actual routed file drop loads the image into the window");
        using var invalidTransfer = new DataTransfer();
        var storageInvalid = window.StorageProvider.TryGetFileFromPathAsync(new Uri(unsupported)).GetAwaiter().GetResult()!;
        invalidTransfer.Add(DataTransferItem.CreateFile(storageInvalid));
        var invalidDrag = new DragEventArgs(DragDrop.DragOverEvent, invalidTransfer, window, default, KeyModifiers.None);
        window.RaiseEvent(invalidDrag);
        check(invalidDrag.DragEffects == DragDropEffects.None, "Unsupported-only drag shows a rejected drop cursor");
        var invalidDrop = new DragEventArgs(DragDrop.DropEvent, invalidTransfer, window, default, KeyModifiers.None);
        window.RaiseEvent(invalidDrop);
        check(vm.Images.Count == 1 && !vm.IsBusy, "Unsupported-only drop leaves loaded images unchanged");
        transfer.Add(DataTransferItem.CreateFile(storageInvalid));
        var mixedDrop = new DragEventArgs(DragDrop.DropEvent, transfer, window, default, KeyModifiers.None);
        window.RaiseEvent(mixedDrop); pump(() => !vm.IsBusy);
        check(vm.Images.Count == 1 && vm.HasErrors && vm.ErrorDetails.Contains("renamed.txt"),
            "Mixed routed drop keeps valid images and reports unsupported files");
        window.Close();
        if (OperatingSystem.IsWindows()) WindowsIntegrationRegressionTests.Run(folder, check);
    }

    private sealed class InputDialogs : IFileDialogService
    {
        public IReadOnlyList<string> Paths { get; init; } = [];
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult(Paths);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }

    private sealed class TestWindowsIntegration : IWindowsIntegrationService
    {
        public bool IsAvailable => true;
        public bool Enabled { get; private set; }
        public bool Fail { get; set; }
        public void Enable() { if (Fail) throw new IOException("test failure"); Enabled = true; }
        public void Remove() => Enabled = false;
    }
}
