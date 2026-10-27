using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;

namespace HappyColorSplit.Views;
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _folderTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private WindowsIntegrationWindow? _integrationWindow;
    public MainWindow()
    {
        InitializeComponent();
        OriginalViewportControl.ColorGroupHighlighted += (_, index) => (DataContext as MainViewModel)?.HighlightOriginalColor(index);
        OriginalViewportControl.ColorGroupClicked += (_, index) =>
        {
            if (DataContext is not MainViewModel vm) return;
            vm.SelectOriginalColor(index);
            if (vm.SelectedGroup is { } group) ColorsList.ScrollIntoView(group);
        };
        OriginalViewportControl.ColorGroupExportToggleRequested += (_, index) =>
        {
            if (DataContext is not MainViewModel vm || vm.ToggleOriginalColorExport(index) is not { } group) return;
            vm.RightSidebarCollapsed = false;
            Dispatcher.UIThread.Post(() =>
            {
                if (!IsVisible || DataContext != vm || !vm.Groups.Contains(group)) return;
                ColorsList.ScrollIntoView(group);
                group.FlashExportChange(vm.ReduceMotion);
            }, DispatcherPriority.Loaded);
        };
        InitializeWorkspaceLayout();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver, RoutingStrategies.Bubble, handledEventsToo: true);
        AddHandler(DragDrop.DropEvent, OnDrop, RoutingStrategies.Bubble, handledEventsToo: true);
        _folderTimer.Tick += (_, _) => (DataContext as MainViewModel)?.RefreshLastExportFolder();
        Opened += (_, _) => _folderTimer.Start();
        Activated += (_, _) => (DataContext as MainViewModel)?.RefreshLastExportFolder();
        Closed += (_, _) => _folderTimer.Stop();
    }

    private async void WindowsIntegration_OnClick(object? sender, RoutedEventArgs args)
    {
        if (_integrationWindow is not null || DataContext is not MainViewModel { CanIntegrateWithWindows: true } vm) return;
        _integrationWindow = new WindowsIntegrationWindow { DataContext = vm };
        WindowsIntegrationButton.Classes.Add("open");
        try { await _integrationWindow.ShowDialog(this); }
        finally { _integrationWindow = null; WindowsIntegrationButton.Classes.Remove("open"); }
    }

    private static string[] GetDroppedPaths(DragEventArgs args) => args.DataTransfer.TryGetFiles()?
        .OfType<IStorageFile>().Select(file => file.TryGetLocalPath()).OfType<string>().ToArray() ?? [];

    private void OnDragOver(object? sender, DragEventArgs args)
    {
        args.DragEffects = DataContext is MainViewModel { IsIdle: true }
            && GetDroppedPaths(args).Any(SupportedImageFiles.IsSupported) ? DragDropEffects.Copy : DragDropEffects.None;
        args.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs args)
    {
        args.Handled = true;
        args.DragEffects = DragDropEffects.None;
        if (DataContext is not MainViewModel { IsIdle: true } vm) return;
        string[] paths = GetDroppedPaths(args);
        if (!paths.Any(SupportedImageFiles.IsSupported)) return;
        args.DragEffects = DragDropEffects.Copy;
        await vm.AddImagesAsync(paths);
    }
}
