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
            var vm = new MainViewModel(new FileDialogService(window), new SettingsService(), new DetailPreviewService(window), new LicenseWindowService(window));
            window.DataContext = vm;
            window.Closing += (_, e) => { if (vm.IsBusy) { vm.Cancel(); e.Cancel = true; } };
            window.Closed += (_, _) => vm.Dispose();
            desktop.MainWindow = window;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
