using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using HappyColorSplit.ViewModels;

namespace HappyColorSplit.Views;

public partial class MainWindow
{
    private MainViewModel? _workspaceOwner;

    private void InitializeWorkspaceLayout()
    {
        DataContextChanged += (_, _) =>
        {
            if (_workspaceOwner is not null) _workspaceOwner.PropertyChanged -= WorkspacePreferenceChanged;
            _workspaceOwner = DataContext as MainViewModel;
            if (_workspaceOwner is not null) _workspaceOwner.PropertyChanged += WorkspacePreferenceChanged;
            ApplyPreviewLayout();
        };
        PreviewDivider.AddHandler(Thumb.DragCompletedEvent, (_, _) => SavePreviewSplit(), RoutingStrategies.Bubble, handledEventsToo: true);
        PreviewDivider.KeyUp += (_, e) => { if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down) SavePreviewSplit(); };
        Closed += (_, _) => { if (_workspaceOwner is not null) _workspaceOwner.PropertyChanged -= WorkspacePreferenceChanged; _workspaceOwner = null; };
        ApplyPreviewLayout();
    }

    private void WorkspacePreferenceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SideBySide)) ApplyPreviewLayout();
    }

    private void ApplyPreviewLayout()
    {
        bool side = _workspaceOwner?.SideBySide ?? false;
        double ratio = side ? _workspaceOwner?.HorizontalPreviewSplit ?? .5 : _workspaceOwner?.VerticalPreviewSplit ?? .5;
        var columns = new ColumnDefinitions(); var rows = new RowDefinitions();
        if (side)
        {
            columns.Add(new ColumnDefinition(new GridLength(ratio, GridUnitType.Star)) { MinWidth = 150 });
            columns.Add(new ColumnDefinition(new GridLength(8)));
            columns.Add(new ColumnDefinition(new GridLength(1 - ratio, GridUnitType.Star)) { MinWidth = 150 });
            rows.Add(new RowDefinition(GridLength.Star));
        }
        else
        {
            columns.Add(new ColumnDefinition(GridLength.Star));
            rows.Add(new RowDefinition(new GridLength(ratio, GridUnitType.Star)) { MinHeight = 80 });
            rows.Add(new RowDefinition(new GridLength(8)));
            rows.Add(new RowDefinition(new GridLength(1 - ratio, GridUnitType.Star)) { MinHeight = 80 });
        }
        PreviewPanels.ColumnDefinitions = columns; PreviewPanels.RowDefinitions = rows;
        Grid.SetRow(PreviewDivider, side ? 0 : 1); Grid.SetColumn(PreviewDivider, side ? 1 : 0);
        Grid.SetRow(ExportPreviewPanel, side ? 0 : 2); Grid.SetColumn(ExportPreviewPanel, side ? 2 : 0);
        PreviewDivider.ResizeDirection = side ? GridResizeDirection.Columns : GridResizeDirection.Rows;
    }

    private void SavePreviewSplit()
    {
        if (_workspaceOwner is not { } vm) return;
        double a = vm.SideBySide ? OriginalPreviewPanel.Bounds.Width : OriginalPreviewPanel.Bounds.Height;
        double b = vm.SideBySide ? ExportPreviewPanel.Bounds.Width : ExportPreviewPanel.Bounds.Height;
        if (a + b <= 0) return;
        if (vm.SideBySide) vm.HorizontalPreviewSplit = a / (a + b); else vm.VerticalPreviewSplit = a / (a + b);
        ApplyPreviewLayout();
    }
}
