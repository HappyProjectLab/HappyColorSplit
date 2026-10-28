using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

namespace HappyColorSplit;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            var vm = new MainViewModel(new FileDialogService(window), new SettingsService(), new DetailPreviewService(window), new LicenseWindowService(window),
                updatePrompt: new UpdatePromptService(window));
            window.DataContext = vm;
            window.Closing += (_, e) => { if (vm.IsBusy) { vm.Cancel(); e.Cancel = true; } };
            window.Closed += (_, _) => vm.Dispose();
            window.Opened += async (_, _) => await vm.CheckForUpdatesAsync();
            window.Opened += (_, _) => vm.Tutorial.OfferOnStartup();
            desktop.MainWindow = window;
            string[] paths = desktop.Args ?? [];
            if (paths.Length > 0)
                window.Opened += async (_, _) => await vm.AddImagesAsync(paths);
        }
        base.OnFrameworkInitializationCompleted();
    }
}
