using Avalonia.Controls;
using HappyColorSplit.Controls;
using HappyColorSplit.ViewModels;
namespace HappyColorSplit.Views;
public partial class LaserWorkspaceView : UserControl
{
    public LaserWorkspaceView() => InitializeComponent();
    private async void OnMoveRequested(object? sender, LaserMoveEventArgs e)
    { if (DataContext is LaserWorkspaceViewModel vm) await vm.MoveSelectionAsync(e.Request); }
    private async void OnLassoRequested(object? sender, LaserLassoEventArgs e)
    { if (DataContext is LaserWorkspaceViewModel vm) await vm.SelectPolygonAsync(e.Request); }
}
