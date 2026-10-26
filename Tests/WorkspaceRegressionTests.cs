using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class WorkspaceRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "workspace"); Directory.CreateDirectory(folder);
        string input = Path.Combine(folder, "landscape.png"), second = Path.Combine(folder, "portrait.png");
        using (var image = new RasterImage(120, 60))
        {
            for (int y = 0; y < 60; y++) for (int x = 0; x < 120; x++) image[x, y] = x < 60 ? new(230, 10, 80) : new(10, 150, 200);
            File.WriteAllBytes(input, RasterImageCodec.EncodePng(image));
        }
        using (var image = new RasterImage(30, 60, new RgbaPixel(230, 10, 80))) File.WriteAllBytes(second, RasterImageCodec.EncodePng(image));
        string settingsFolder = Path.Combine(folder, "settings"); Directory.CreateDirectory(settingsFolder);
        string settingsPath = Path.Combine(settingsFolder, "settings.json");
        File.WriteAllText(settingsPath, "{\"Language\":\"en\",\"Workspace\":null}");
        var settings = new SettingsService(settingsFolder);
        check(settings.Load().Workspace is { SideBySide: false, ImagesCollapsed: false, HorizontalSplit: .5 }, "Legacy/null workspace preferences use the original stacked layout");
        File.WriteAllText(settingsPath, "{\"Language\":\"en\",\"Workspace\":{\"HorizontalSplit\":8,\"VerticalSplit\":-2}}");
        check(settings.Load().Workspace is { HorizontalSplit: .85, VerticalSplit: .15 }, "Saved divider ratios cannot hide either preview");
        settings.Save(new AppSettings { Language = "en" });
        using var vm = new MainViewModel(new Dialogs(folder), settings);
        var window = new MainWindow { DataContext = vm }; window.Show(); Layout();
        try
        {
            var original = window.FindControl<ImageViewport>("OriginalViewportControl")!;
            var mask = window.FindControl<ImageViewport>("ExportViewportControl")!;
            var panels = window.FindControl<Grid>("PreviewPanels")!;
            var divider = window.FindControl<GridSplitter>("PreviewDivider")!;
            var left = window.FindControl<CollapsibleSidebar>("ImagesSidebar")!;
            var right = window.FindControl<CollapsibleSidebar>("RightSidebar")!;
            var selector = window.FindControl<ComboBox>("PreviewLayoutSelector")!;
            check(panels.RowDefinitions.Count == 3 && panels.ColumnDefinitions.Count == 1, "Default view is stacked");
            selector.SelectedIndex = 1; Layout();
            check(vm.SideBySide && panels.ColumnDefinitions.Count == 3 && panels.RowDefinitions.Count == 1, "The manual selector places original left and export right");
            window.Width = 1120; window.Height = 740; Layout();
            check(vm.SideBySide && panels.ColumnDefinitions.Count == 3, "Window resize never changes the chosen orientation");
            window.Width = 1380; window.Height = 870; Layout();
            var start = divider.TranslatePoint(new Point(divider.Bounds.Width / 2, divider.Bounds.Height / 2), window)!.Value;
            window.MouseDown(start, MouseButton.Left); window.MouseMove(start + new Vector(60, 0)); Layout(); window.MouseUp(start + new Vector(60, 0), MouseButton.Left); Layout();
            double horizontalRatio = vm.HorizontalPreviewSplit;
            check(horizontalRatio > .54 && horizontalRatio < .8, "Dragging the divider persists its actual horizontal ratio");
            selector.SelectedIndex = 0; Layout();
            check(vm.VerticalPreviewSplit == .5, "Stacked and side-by-side divider positions are independent");
            divider.Focus(); window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Layout();
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null); Layout();
            check(vm.VerticalPreviewSplit > .5, "Keyboard arrows resize and save the focused divider");
            selector.SelectedIndex = 1; Layout();
            check(Math.Abs(panels.ColumnDefinitions[0].Width.Value - horizontalRatio) < .001, "Returning to an orientation restores its divider ratio");

            var viewMenu = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "View"));
            var viewFlyout = (Flyout)viewMenu.Flyout!; viewFlyout.ShowAt(viewMenu); Layout();
            ((StackPanel)viewFlyout.Content!).Children.OfType<CheckBox>().Single().IsChecked = true;
            viewFlyout.Hide(); vm.ImagesSidebarCollapsed = true; Layout();
            check(vm.ReduceMotion, "The View flyout controls the saved motion preference");
            check(left.Width == CollapsibleSidebar.RailWidth, "Reduced motion collapses immediately to a labelled rail");
            Task load = vm.AddImagesAsync([input]); pump(() => load.IsCompleted); Layout();
            check(vm.ImagesHasNotice && vm.ImagesRailLabel.Contains("1"), "A hidden image pane indicates new images and their count");
            var rail = left.GetVisualDescendants().OfType<Button>().Single(b => Equals(ToolTip.GetTip(b), vm.ImagesRailLabel));
            var railPoint = rail.TranslatePoint(new Point(22, 25), window)!.Value;
            window.MouseMove(railPoint); Layout();
            check(vm.ImagesSidebarCollapsed, "Hover highlights the rail without opening it");
            window.MouseDown(railPoint, MouseButton.Left); window.MouseUp(railPoint, MouseButton.Left); Layout();
            check(!vm.ImagesSidebarCollapsed && !vm.ImagesHasNotice && left.Width == left.ExpandedWidth, "Clicking the labelled rail restores the pane and clears its notice");
            vm.ImagesSidebarCollapsed = true;
            vm.PreviewOnlyCommand.Execute(null); Layout();
            check(vm.IsPreviewOnly && vm.PreviewOnlyLabel == "Show sidebars", "Previews-only closes both sidebars");
            vm.PreviewOnlyCommand.Execute(null); Layout();
            check(vm.ImagesSidebarCollapsed && !vm.RightSidebarCollapsed, "Previews-only restores the previous individual sidebar states");
            vm.ReduceMotion = false; vm.ImagesSidebarCollapsed = false;
            check(left.Width < left.ExpandedWidth, "Normal opening starts an animated width transition");
            // Dispatcher timers are serviced by the real event loop, not RunJobs alone.
            using (var finish = new CancellationTokenSource(TimeSpan.FromMilliseconds(450))) Dispatcher.UIThread.MainLoop(finish.Token);
            Layout();
            check(left.Width == left.ExpandedWidth, "The animated transition finishes at the expanded width");
            vm.ReduceMotion = true; vm.RightSidebarCollapsed = true; Layout();
            vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy); Layout();
            check(vm.RightHasNotice && right.HasNotice && vm.RightRailLabel.Contains("2/2"), "A hidden color pane indicates completed analysis and checked color count");
            vm.RightSidebarCollapsed = false; Layout();
            check(!vm.RightHasNotice, "Opening color groups acknowledges the new content");

            var exportSize = vm.SelectedImage!.Scale.GetSize(120, 60);
            var viewPoint = original.TranslatePoint(new Point(original.Bounds.Width / 2, original.Bounds.Height / 2), window)!.Value;
            window.MouseWheel(viewPoint, new Vector(0, 3)); Layout();
            check(vm.OriginalViewport.Zoom > 1 && vm.ExportViewport.Zoom == 1 && original.Zoom == vm.OriginalViewport.Zoom, "Real mouse-wheel zoom updates only the active preview by default");
            var sync = window.FindControl<CheckBox>("SyncPreviewsToggle")!;
            sync.IsChecked = true; Layout();
            check(vm.SynchronizePreviews && mask.Zoom == original.Zoom, "Enabling synchronization copies the most recently used viewport");
            window.MouseDown(viewPoint, MouseButton.Left); window.MouseMove(viewPoint + new Vector(25, 18)); window.MouseUp(viewPoint + new Vector(25, 18), MouseButton.Left); Layout();
            check(vm.OriginalViewport.CenterX < .5 && vm.OriginalViewport.CenterY < .5
                && vm.ExportViewport.CenterX == vm.OriginalViewport.CenterX && vm.ExportViewport.CenterY == vm.OriginalViewport.CenterY,
                "Dragging a zoomed preview synchronizes normalized image positions");
            vm.ExportViewport.FitCommand.Execute(null); Layout();
            check(original.Zoom == 1 && mask.Zoom == 1 && vm.OriginalViewport.CenterX == .5 && vm.OriginalViewport.CenterY == .5,
                "Fit resets both previews when synchronization is enabled");
            check(vm.SelectedImage.Scale.GetSize(120, 60) == exportSize, "View zoom and pan never alter exported physical or pixel dimensions");
            vm.SynchronizePreviews = false; vm.ExportViewport.Zoom = 4; Layout();
            check(original.Zoom == 1 && mask.Zoom == 4, "Unlinked previews navigate independently again");
            var add = vm.AddImagesAsync([second]); pump(() => add.IsCompleted); Layout();
            check(original.Zoom == 1 && mask.Zoom == 1 && vm.SelectedImage.FilePath == second, "Loading a new source resets both viewport positions");

            vm.RightSidebarCollapsed = true; vm.WorkspaceIndex = 1; Layout();
            check(vm.RightSidebarCollapsed && vm.RightRailLabel.StartsWith("Expand pages") && !panels.IsVisible, "Laser mode uses the full center and the same collapsed rail for pages");
            vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
            vm.Laser.MaterialHex = "#E60A50"; vm.Laser.AddMaterialCommand.Execute(null);
            vm.Laser.ArrangeCommand.Execute(null); pump(() => !vm.IsBusy); Layout();
            check(vm.Laser.Pages.Count > 0 && vm.RightHasNotice && right.HasNotice && vm.RightRailLabel.Contains(vm.Laser.Pages.Count.ToString()),
                "New laser pages update the hidden page rail and its notification");
            vm.WorkspaceIndex = 0; Layout();
            check(vm.SideBySide && panels.IsVisible && vm.RightRailLabel.StartsWith("Expand colors"), "Returning from Laser retains the selected color-preview layout");
            vm.LanguageIndex = 0; Layout();
            check(selector.SelectedIndex == 1 && selector.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Nebeneinander"),
                "Switching languages updates the visible layout label without losing the selected orientation");
            check(vm.RightRailLabel.StartsWith("Farben ausklappen") && vm.ImagesRailLabel.StartsWith("Bilder ausklappen"), "Collapsed labels follow the selected language");
            vm.ImagesSidebarCollapsed = true;
            var saved = settings.Load().Workspace;
            check(saved.SideBySide && saved.ImagesCollapsed && saved.RightCollapsed && saved.ReduceMotion && !saved.SynchronizePreviews
                && Math.Abs(saved.HorizontalSplit - horizontalRatio) < .001, "All workspace preferences round-trip through JSON");
        }
        finally { window.Close(); }
        using var restarted = new MainViewModel(new Dialogs(folder), settings);
        var restored = new MainWindow { DataContext = restarted }; restored.Show(); Layout();
        try
        {
            check(restarted.SideBySide && restored.FindControl<CollapsibleSidebar>("ImagesSidebar")!.Width == 44
                && restored.FindControl<CollapsibleSidebar>("RightSidebar")!.Width == 44, "A new window restores the chosen layout and sidebar state immediately");
            restarted.PreviewOnlyCommand.Execute(null); Layout();
            check(!restarted.ImagesSidebarCollapsed && !restarted.RightSidebarCollapsed, "After restart, Show sidebars reopens both panes");
        }
        finally { restored.Close(); }
        var rect = ImageViewport.ImageBounds(new(400, 300), new(800, 400), 4, 0, 1);
        check(rect.Left == 0 && rect.Bottom == 300 && rect.Width / rect.Height == 2, "Preview panning clamps image edges without distortion or exposing empty image space");
    }

    private static void Layout() => Dispatcher.UIThread.RunJobs();
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
}
