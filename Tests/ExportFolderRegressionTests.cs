using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ExportFolderRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string directory = Path.Combine(root, "last-export-tests");
        Directory.CreateDirectory(directory);
        string input = Path.Combine(directory, "sample.png");
        using (var image = new RasterImage(2, 1, new RgbaPixel(250, 30, 10))) image.SaveAsPng(input);
        byte[] original = File.ReadAllBytes(input);
        var dialogs = new Dialogs { Parent = directory };
        var opener = new FolderOpener();
        var settings = new SettingsService(Path.Combine(directory, "settings"));
        using var vm = new MainViewModel(dialogs, settings, exportFolderService: opener);
        var window = new MainWindow { DataContext = vm };
        window.Show(); Dispatcher.UIThread.RunJobs();
        var button = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Letzten Export öffnen"));
        check(button.IsVisible && !button.IsEnabled && vm.LastExportFolder == "", "Last-export button is visible and disabled before the first export");
        var load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted);
        vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        vm.ExportCommand.Execute(null); pump(() => !vm.IsBusy);
        string first = vm.LastExportFolder;
        check(Directory.Exists(first) && Path.GetDirectoryName(first) == directory && first != directory,
            "Single export remembers the newly created child folder instead of its selected parent");
        check(button.IsVisible && button.IsEnabled && vm.OpenLastExportFolderCommand.CanExecute(null), "Last-export button becomes enabled after a completed export");
        vm.OpenLastExportFolderCommand.Execute(null);
        check(opener.Opened == first, "Open-last-export command opens the exact remembered folder");
        dialogs.Parent = null;
        vm.ExportCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.LastExportFolder == first && vm.CanOpenLastExportFolder, "Cancelling the destination picker retains the last export target");
        string invalidParent = Path.Combine(directory, "not-a-directory");
        File.WriteAllText(invalidParent, "file"); dialogs.Parent = invalidParent;
        vm.ExportCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.LastExportFolder == first, "Failed export does not replace the last successful export folder");
        dialogs.Parent = directory;
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        string batch = vm.LastExportFolder;
        check(batch != first && Path.GetDirectoryName(batch) == directory && Path.GetFileName(batch).StartsWith("HappyColorSplit_Batch_"),
            "Batch export updates the target to its created batch folder without nesting the next export in the old target");
        File.Delete(input);
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.LastExportFolder == batch && vm.HasErrors, "Batch with no completed images retains the previous successful export folder");
        File.WriteAllBytes(input, original);
        using (var restored = new MainViewModel(dialogs, settings, exportFolderService: opener))
            check(restored.LastExportFolder == batch && restored.CanOpenLastExportFolder, "Last successful export folder survives restarting the view model");
        check(Path.GetFullPath(batch).StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Folder-deletion fixture is confined to the temporary test workspace");
        Directory.Delete(batch, true); vm.RefreshLastExportFolder(); Dispatcher.UIThread.RunJobs();
        check(button.IsVisible && !button.IsEnabled && !vm.OpenLastExportFolderCommand.CanExecute(null), "Deleted last-export folder disables the visible button");
        Directory.CreateDirectory(batch); vm.RefreshLastExportFolder(); Dispatcher.UIThread.RunJobs();
        check(button.IsEnabled, "Restored export folder re-enables the button");
        opener.Fail = true; vm.OpenLastExportFolderCommand.Execute(null);
        check(vm.Status.Contains("folder opener failed") && !vm.IsBusy, "Explorer launch failures are reported without crashing or starting image processing");
        window.Close();
    }

    private sealed class Dialogs : IFileDialogService
    {
        public string? Parent { get; set; }
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult(Parent);
    }

    private sealed class FolderOpener : IExportFolderService
    {
        public string? Opened { get; private set; }
        public bool Fail { get; set; }
        public void Open(string folder) { if (Fail) throw new IOException("folder opener failed"); Opened = folder; }
    }
}
