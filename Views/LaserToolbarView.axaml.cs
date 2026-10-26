using Avalonia.Controls;

namespace HappyColorSplit.Views;

public partial class LaserToolbarView : UserControl
{
    public LaserToolbarView()
    {
        InitializeComponent();
        foreach (var button in new[] { LaserSettingsButton, LaserEditButton })
        {
            button.Flyout!.Opened += (_, _) => button.Classes.Add("open");
            button.Flyout.Closed += (_, _) => button.Classes.Remove("open");
        }
    }
}
