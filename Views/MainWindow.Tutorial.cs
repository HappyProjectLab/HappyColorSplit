using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using HappyColorSplit.ViewModels;

namespace HappyColorSplit.Views;

public partial class MainWindow
{
    private MainViewModel? _tutorialOwner;
    private void InitializeTutorial()
    {
        DataContextChanged += (_, _) =>
        {
            if (_tutorialOwner is not null) _tutorialOwner.Tutorial.PropertyChanged -= TutorialChanged;
            _tutorialOwner = DataContext as MainViewModel;
            if (_tutorialOwner is not null) _tutorialOwner.Tutorial.PropertyChanged += TutorialChanged;
            UpdateTutorialLayout();
        };
        LayoutUpdated += (_, _) => UpdateTutorialLayout();
        Closed += (_, _) =>
        {
            if (_tutorialOwner is not null) _tutorialOwner.Tutorial.PropertyChanged -= TutorialChanged;
            _tutorialOwner = null;
        };
    }
    private void TutorialChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName is nameof(TutorialViewModel.TargetName) or nameof(TutorialViewModel.IsVisible)) UpdateTutorialLayout(); }
    private void UpdateTutorialLayout()
    {
        if (_tutorialOwner is not { } owner || !owner.Tutorial.IsVisible) return;
        var tutorial = owner.Tutorial;
        Control? target = tutorial.IsRunning ? this.FindControl<Control>(tutorial.TargetName) : null;
        string hint = "";
        if (tutorial.IsRunning && tutorial.TargetName == "ImageSizeButton" && !owner.HasImage)
        { target = OpenImagesButton; hint = "GuideNeedImage"; }
        else if (tutorial.IsRunning && owner.IsLaserMode && tutorial.TargetName is not ("WorkspaceSelector" or "AppVersionLabel"))
        { target = WorkspaceSelector; hint = "GuideNeedColorMode"; }
        else if (target is not null && !target.IsEffectivelyVisible)
        {
            if (tutorial.TargetName is "LoadedImagesList" or "ImageSizeButton") { target = ImagesSidebar; hint = "GuideOpenImages"; }
            else if (tutorial.TargetName == "ColorsList") { target = RightSidebar; hint = "GuideOpenColors"; }
            else { target = null; hint = "GuideUnavailable"; }
        }
        tutorial.SetHint(hint);
        TutorialSpotlight.SetTarget(target);

        const double margin = 20;
        double width = TutorialCard.Bounds.Width, height = TutorialCard.Bounds.Height;
        var defaultCard = new Rect(Math.Max(margin, Bounds.Width - width - margin), Math.Max(margin, Bounds.Height - height - margin), width, height);
        var focus = TutorialSpotlight.HighlightBounds;
        double bottom = margin;
        // Keep the guide at the lower right when possible, and lift it above a target it would cover.
        if (tutorial.IsRunning && focus.Width > 0 && focus.Intersects(defaultCard) && focus.Top - height - 12 >= margin)
            bottom = Bounds.Height - focus.Top + 12;
        var next = new Thickness(0, 0, margin, bottom);
        if (TutorialCard.Margin != next) TutorialCard.Margin = next;
    }
}
