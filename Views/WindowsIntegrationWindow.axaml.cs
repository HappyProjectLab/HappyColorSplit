using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HappyColorSplit.Views;

public partial class WindowsIntegrationWindow : Window
{
    public WindowsIntegrationWindow() => InitializeComponent();
    private void Close_OnClick(object? sender, RoutedEventArgs e) => Close();
}
