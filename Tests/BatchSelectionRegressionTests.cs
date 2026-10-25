using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class BatchSelectionRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "batch-selections"); Directory.CreateDirectory(folder);
        string[] paths = Enumerable.Range(1, 3).Select(i => Path.Combine(folder, $"image-{i}.png")).ToArray();
        using (var source = new RasterImage(4, 2))
        {
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 4; x++) source[x, y] = x < 2 ? new(240, 20, 90) : new(20, 170, 220);
            foreach (string path in paths) File.WriteAllBytes(path, RasterImageCodec.EncodePng(source));
        }
        using var vm = new MainViewModel(new Dialogs(folder), new SettingsService(Path.Combine(folder, "settings")));
        var window = new MainWindow { DataContext = vm }; window.Show(); vm.LanguageIndex = 1;
        Task load = vm.AddImagesAsync(paths.Take(2)); pump(() => load.IsCompleted);
        check(!vm.ExportBatchCommand.CanExecute(null), "Batch export cannot run before any image has recognized, checked colors");
        vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.Images.All(i => i.ColorsDetected && i.ExportColors?.Count == 2), "One Find colors command initializes every loaded image's independent palette selection");
        SetChecks(window, "#F0145A", check);
        vm.ScalePercent = 200;
        vm.SelectedImage = vm.Images[1]; pump(() => !vm.IsBusy);
        check(vm.HasGroups && vm.Groups.Count == 2 && vm.ExportColorCount == 2, "Switching images automatically restores the already recognized palette");
        SetChecks(window, "#14AADC", check);
        vm.ScalePercent = 50;
        vm.SelectedImage = vm.Images[0]; pump(() => !vm.IsBusy);
        check(vm.ExportColorCount == 1 && vm.Groups.Single(g => g.ExportEnabled).Hex == "#F0145A", "Image-specific checked colors survive switching");
        vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.Images[0].ExportColors!.SetEquals(new[] { "#F0145A" }) && vm.Images[1].ExportColors!.SetEquals(new[] { "#14AADC" }),
            "Repeating Find colors retains each image's checked colors instead of selecting all again");
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        var masks = Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories);
        check(masks.Length == 2, "Batch creates exactly the two checked masks across two images");
        foreach (string path in masks)
        {
            bool first = Path.GetFileName(path).StartsWith("image-1_");
            check(path.EndsWith(first ? "F0145A.png" : "14AADC.png"), "Batch files use only the checked reference color for their own image");
            using var mask = RasterImageCodec.Load(path);
            check(mask.Width == (first ? 8 : 2) && mask.Height == (first ? 4 : 1), "Per-image scale survives the new all-image detection workflow");
        }
        Task add = vm.AddImagesAsync([paths[2]]); pump(() => add.IsCompleted);
        check(!vm.SelectedImage!.ColorsDetected && !vm.HasGroups, "A newly added image is not silently treated as having a color selection");
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        check(Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories).Length == 2 && vm.Status.Contains("1 skipped"),
            "An unrecognized new image is skipped instead of exporting all its colors");
        vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.Images.All(i => i.ColorsDetected) && vm.Images[2].ExportColors!.Count == 2 && vm.Images[0].ExportColors!.Count == 1,
            "Find colors includes new images while preserving earlier choices");
        vm.DeselectAllColorsCommand.Execute(null);
        vm.ExportBatchCommand.Execute(null); pump(() => !vm.IsBusy);
        check(Directory.GetFiles(vm.LastExportFolder, "*.png", SearchOption.AllDirectories).Length == 2 && vm.Status.Contains("1 skipped") && !vm.HasErrors,
            "An image with all colors unchecked is intentionally skipped without being reported as an error");
        File.WriteAllText(paths[1], "broken image");
        vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
        check(vm.HasErrors && vm.ErrorDetails.Contains("image-2.png") && !vm.Images[1].ColorsDetected && vm.Images[0].ColorsDetected,
            "All-image detection reports one failed image and continues recognizing other files");
        vm.Tolerance = 1;
        check(vm.Images.All(i => !i.ColorsDetected && i.ExportColors is null) && !vm.ExportBatchCommand.CanExecute(null),
            "Changing detection settings invalidates all image selections and requires Find colors again");
        window.Close();

        using var cancelled = new MainViewModel(new Dialogs(folder), new SettingsService(Path.Combine(folder, "cancel-settings")));
        load = cancelled.AddImagesAsync([paths[0], paths[2]]); pump(() => load.IsCompleted);
        cancelled.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(MainViewModel.Progress) && cancelled.Progress > 0) cancelled.Cancel(); };
        cancelled.AnalyzeCommand.Execute(null); pump(() => !cancelled.IsBusy);
        check(cancelled.Images[0].ColorsDetected && !cancelled.Images[1].ColorsDetected,
            "Cancelling all-image detection preserves the completed image and leaves unprocessed images unselected");
    }

    private static void SetChecks(Window window, string selected, Action<bool, string> check)
    {
        Dispatcher.UIThread.RunJobs();
        var controls = window.GetVisualDescendants().OfType<CheckBox>().Where(c => c.DataContext is ColorGroupViewModel).ToArray();
        foreach (var checkbox in controls) checkbox.IsChecked = ((ColorGroupViewModel)checkbox.DataContext!).Hex == selected;
        Dispatcher.UIThread.RunJobs();
        var vm = (MainViewModel)window.DataContext!;
        check(controls.Length == 2 && vm.SelectedImage!.ExportColors!.SetEquals(new[] { selected }), "Real UI checkboxes persist exactly the selected color for the active image");
    }
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
}
