using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HappyColorSplit.Views;

public partial class LicensesWindow : Window
{
    public LicensesWindow() => InitializeComponent();
    private void Close_OnClick(object? sender, RoutedEventArgs e) => Close();
}
