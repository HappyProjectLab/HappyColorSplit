using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class TutorialRegressionTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        var settings = new SettingsService(Path.Combine(root, "tutorial-settings"));
        settings.Save(new AppSettings { Workspace = new() { ReduceMotion = true, ImagesCollapsed = true, RightCollapsed = true } });
        var dialogs = new Dialogs();
        using (var vm = new MainViewModel(dialogs, settings))
        {
            vm.IsLaserMode = true;
            var window = new MainWindow { DataContext = vm }; window.Show(); Layout();
            try
            {
                var guide = vm.Tutorial;
                var card = window.FindControl<Border>("TutorialCard")!;
                var spotlight = window.FindControl<TutorialSpotlight>("TutorialSpotlight")!;
                check(!guide.IsVisible && !card.IsVisible, "Guide does not open just by constructing a view model or test window");
                guide.OfferOnStartup(); Layout();
                check(guide.IsWelcome && card.IsVisible && vm.IsLaserMode && vm.ImagesSidebarCollapsed && vm.RightSidebarCollapsed, "First-start welcome is offered without changing the workspace");
                Save(window, "tutorial-welcome.png");
                Press(window, "TutorialStartButton");
                check(guide.IsRunning && guide.StepIndex == 0 && !vm.IsLaserMode && !spotlight.IsHitTestVisible, "Starting the tour opens color layers and leaves the spotlight transparent to input");
                check(!guide.BackCommand.CanExecute(null), "Back is disabled at the first step");
                CheckFocus(window, spotlight, "OpenImagesButton", check);
                Press(window, "TutorialNextButton");
                check(guide.StepIndex == 1 && !vm.ImagesSidebarCollapsed, "Image-list step reveals a collapsed image sidebar");
                Press(window, "TutorialBackButton");
                check(guide.StepIndex == 0, "Back returns to the previous step");
                Press(window, "TutorialSkipButton");
                check(!guide.IsVisible && vm.IsLaserMode && vm.ImagesSidebarCollapsed && vm.RightSidebarCollapsed, "Skipping restores the original mode and sidebar states");
                check(!settings.Load().TutorialCompleted && !settings.Load().TutorialTipsDisabled, "Skipping does not mark the tour completed or permanently disable tips");
                guide.OfferOnStartup(); Layout();
                check(guide.IsWelcome, "A skipped guide can be offered again");
                Press(window, "TutorialStartButton");
                window.Width = 1120; window.Height = 740; Layout();
                var importPoint = window.FindControl<Button>("OpenImagesButton")!.TranslatePoint(new Point(25, 15), window)!.Value;
                dialogs.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
                window.MouseDown(importPoint, MouseButton.Left); window.MouseUp(importPoint, MouseButton.Left); Layout();
                check(dialogs.Calls == 1 && vm.IsBusy && !guide.NextCommand.CanExecute(null) && !guide.SkipCommand.CanExecute(null), "Highlighted controls remain clickable; tour navigation pauses during processing");
                dialogs.Pending.SetResult([]); pump(() => !vm.IsBusy); dialogs.Pending = null;
                check(guide.NextCommand.CanExecute(null), "Tour navigation resumes after cancelling the file picker");
                for (int step = 0; step < guide.StepCount; step++)
                {
                    check(guide.StepIndex == step, "Guide has a stable sequential step index " + step);
                    vm.LanguageIndex = 0; Layout();
                    check(guide.ProgressLabel == $"Schritt {step + 1} von {guide.StepCount}" && !guide.Description.StartsWith("Guide"), "German step text is translated");
                    CheckCard(window, card, check);
                    if (step == 8)
                    {
                        check(guide.HasHint && guide.Hint.Contains("Bild"), "Image-size step explains the missing-image prerequisite");
                        CheckFocus(window, spotlight, "OpenImagesButton", check);
                        Save(window, "tutorial-missing-image.png");
                    }
                    else CheckFocus(window, spotlight, guide.TargetName, check);
                    if (step == 6)
                    {
                        check(!vm.RightSidebarCollapsed, "Color-list step reveals the color sidebar");
                        vm.RightSidebarCollapsed = true; Layout();
                        check(guide.HasHint, "Collapsing the active sidebar shows an explanatory hint");
                        vm.RightSidebarCollapsed = false; Layout();
                    }
                    if (step == 5)
                    {
                        vm.IsLaserMode = true; Layout();
                        check(guide.HasHint && guide.Hint.Contains("Farbauszüge"), "Changing to Laser during a color step explains how to return");
                        CheckFocus(window, spotlight, "WorkspaceSelector", check);
                        vm.IsLaserMode = false; Layout();
                    }
                    vm.LanguageIndex = 1; Layout();
                    check(guide.ProgressLabel == $"Step {step + 1} of {guide.StepCount}" && !guide.Description.StartsWith("Guide"), "English guide text updates without losing the current step");
                    CheckCard(window, card, check);
                    if (step == 10) Save(window, "tutorial-small-export.png");
                    if (step != guide.StepCount - 1) Press(window, "TutorialNextButton");
                }
                check(guide.NextLabel == "Finish", "Final step offers completion");
                Press(window, "TutorialNextButton");
                check(!guide.IsVisible && settings.Load().TutorialCompleted && vm.IsLaserMode && vm.ImagesSidebarCollapsed && vm.RightSidebarCollapsed, "Completing the guide persists completion and restores the workspace");
                guide.OfferOnStartup();
                check(!guide.IsVisible, "Completed guide is not automatically offered again");
                window.FindControl<Button>("TutorialButton")!.Command!.Execute(null); Layout();
                check(guide.IsRunning && guide.StepIndex == 0, "Toolbar guide button restarts a completed introduction");
                Press(window, "TutorialDisableButton");
                check(!guide.IsVisible && settings.Load().TutorialTipsDisabled, "No tips needed persists the opt-out");
            }
            finally { window.Close(); }
        }
        using (var disabled = new MainViewModel(new Dialogs(), settings))
        {
            disabled.Tutorial.OfferOnStartup();
            check(!disabled.Tutorial.IsVisible, "Opt-out survives a new application session");
            disabled.Tutorial.StartCommand.Execute(null);
            check(disabled.Tutorial.IsRunning, "Opt-out still allows explicitly starting the guide");
            disabled.Tutorial.SkipCommand.Execute(null);
            check(settings.Load().TutorialTipsDisabled, "Manually viewing and skipping the guide does not revoke the opt-out");
        }
        var declinedSettings = new SettingsService(Path.Combine(root, "tutorial-declined-before-start"));
        using (var declined = new MainViewModel(new Dialogs(), declinedSettings))
        {
            declined.Tutorial.OfferOnStartup();
            declined.Tutorial.DisableCommand.Execute(null);
            check(declinedSettings.Load().TutorialTipsDisabled && !declinedSettings.Load().TutorialCompleted, "Declining before starting saves opt-out without falsely recording completion");
        }
        using (var declinedAgain = new MainViewModel(new Dialogs(), declinedSettings))
        {
            declinedAgain.Tutorial.OfferOnStartup();
            check(!declinedAgain.Tutorial.IsVisible, "Opt-out alone prevents an automatic offer in the next session");
        }
        string input = Path.Combine(root, "tutorial-image.png");
        using (var image = new RasterImage(60, 40, new RgbaPixel(220, 40, 80))) { image[0, 0] = new(30, 90, 200); image.SaveAsPng(input); }
        using (var vm = new MainViewModel(new Dialogs(), new SettingsService(Path.Combine(root, "tutorial-with-image"))))
        {
            vm.ReduceMotion = true;
            var window = new MainWindow { DataContext = vm }; window.Show();
            try
            {
                var add = vm.AddImagesAsync([input]); pump(() => add.IsCompleted && !vm.IsBusy);
                vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
                vm.Groups[0].ExportEnabled = false; var selected = vm.SelectedGroup; var choices = vm.Groups.Select(g => g.ExportEnabled).ToArray();
                vm.OriginalViewport.Zoom = 2;
                vm.Tutorial.Begin();
                for (int i = 0; i < vm.Tutorial.StepCount; i++)
                {
                    Layout();
                    CheckFocus(window, window.FindControl<TutorialSpotlight>("TutorialSpotlight")!, vm.Tutorial.TargetName, check);
                    if (i == 8) check(!vm.Tutorial.HasHint, "Loaded-image size step highlights the real size button");
                    if (i == 5) Save(window, "tutorial-with-image.png");
                    check(vm.Groups.Select(g => g.ExportEnabled).SequenceEqual(choices) && vm.SelectedGroup == selected && vm.OriginalViewport.Zoom == 2,
                        "Guide navigation preserves export choices, selected color and zoom");
                    vm.Tutorial.NextCommand.Execute(null);
                }
                check(!vm.Tutorial.IsVisible, "Guide also completes with real detected image content");
            }
            finally { window.Close(); }
        }
    }
    private static void Press(MainWindow window, string name) { window.FindControl<Button>(name)!.Command!.Execute(null); Layout(); }
    private static void Layout() => Dispatcher.UIThread.RunJobs();
    private static void CheckFocus(MainWindow window, TutorialSpotlight spotlight, string targetName, Action<bool, string> check)
    {
        Layout(); var target = window.FindControl<Control>(targetName)!;
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), spotlight)!.Value;
        check(spotlight.HighlightBounds.Width > 0 && spotlight.HighlightBounds.Contains(center), "Spotlight tracks target " + targetName);
        check(spotlight.HighlightBounds.Left >= 0 && spotlight.HighlightBounds.Top >= 0 && spotlight.HighlightBounds.Right <= window.Bounds.Width && spotlight.HighlightBounds.Bottom <= window.Bounds.Height,
            "Spotlight stays inside the window");
    }
    private static void CheckCard(MainWindow window, Border card, Action<bool, string> check)
    {
        var location = card.TranslatePoint(default, window)!.Value;
        check(location.X >= 0 && location.Y >= 0 && location.X + card.Bounds.Width <= window.Bounds.Width && location.Y + card.Bounds.Height <= window.Bounds.Height,
            "Guide card fits within the small window");
        foreach (string name in new[] { "TutorialNextButton", "TutorialBackButton", "TutorialSkipButton", "TutorialDisableButton" })
        {
            var button = window.FindControl<Button>(name)!;
            var point = button.TranslatePoint(default, window)!.Value;
            check(point.X >= location.X && point.Y >= location.Y && point.X + button.Bounds.Width <= location.X + card.Bounds.Width && point.Y + button.Bounds.Height <= location.Y + card.Bounds.Height,
                "Navigation button fits within the guide card: " + name);
        }
    }
    private static void Save(Window window, string name)
    {
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height)); bitmap.Render(window);
        string folder = Path.Combine(Directory.GetCurrentDirectory(), "artifacts"); Directory.CreateDirectory(folder);
        using var output = File.Create(Path.Combine(folder, name)); bitmap.Save(output, PngBitmapEncoderOptions.Default);
    }
    private sealed class Dialogs : IFileDialogService
    {
        public TaskCompletionSource<IReadOnlyList<string>>? Pending;
        public int Calls;
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) { Calls++; return Pending?.Task ?? Task.FromResult<IReadOnlyList<string>>([]); }
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
}
