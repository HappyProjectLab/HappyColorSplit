using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class ImportActivityRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "import-activity"); Directory.CreateDirectory(folder);
        string input = Path.Combine(folder, "image.png");
        using (var image = new RasterImage(4, 2, new RgbaPixel(20, 150, 200))) image.SaveAsPng(input);
        var dialogs = new PendingDialogs();
        using var vm = new MainViewModel(dialogs, new SettingsService(Path.Combine(folder, "settings")));
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            var backdrop = window.FindControl<ImportActivityBackdrop>("ImportBackdrop")!;
            var progress = window.FindControl<ProgressBar>("OperationProgress")!;
            vm.OpenCommand.Execute(null); Dispatcher.UIThread.RunJobs();
            check(vm.IsBusy && vm.IsImporting && backdrop.IsActive && !progress.IsVisible, "A pending image picker activates the full-window backdrop instead of the progress line");
            check(!backdrop.IsHitTestVisible && backdrop.Bounds.Size == window.ClientSize, "Import activity spans the background without capturing input");
            check(!window.GetVisualDescendants().OfType<Button>().Any(b => b.Command == vm.CancelCommand), "The unusable main-window cancel button is removed");
            vm.ReduceMotion = true; Dispatcher.UIThread.RunJobs();
            check(backdrop.ReduceMotion, "Import background honors the saved reduce-motion preference");
            dialogs.Pending.SetResult([]); pump(() => !vm.IsBusy && !vm.IsImporting);
            check(!backdrop.IsActive && vm.Images.Count == 0, "Cancelling the native picker ends background activity without importing an image");

            dialogs.Reset(); vm.OpenCommand.Execute(null);
            dialogs.Pending.SetResult([input]); pump(() => !vm.IsBusy && !vm.IsImporting);
            check(vm.HasImage && !backdrop.IsActive && !progress.IsVisible, "Successful import stops its activity after loading the image");
            bool normalProgress = false;
            vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.IsBusy) && vm.ShowProgressBar) normalProgress = true; };
            vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
            check(normalProgress && !vm.IsImporting, "Color analysis retains its normal progress indication");

            dialogs.Reset(); vm.OpenCommand.Execute(null);
            dialogs.Pending.SetException(new IOException("picker failed")); pump(() => !vm.IsBusy && !vm.IsImporting);
            check(!backdrop.IsActive && vm.Status.Contains("picker failed"), "Picker failures clear activity and report the error");
        }
        finally { window.Close(); }
    }
    private sealed class PendingDialogs : IFileDialogService
    {
        public TaskCompletionSource<IReadOnlyList<string>> Pending { get; private set; } = new();
        public void Reset() => Pending = new();
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Pending.Task;
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
}
