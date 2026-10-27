using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HappyColorSplit.Views;

public partial class UpdatePromptWindow : Window
{
    public UpdatePromptWindow() => InitializeComponent();
    private void Cancel_OnClick(object? sender, RoutedEventArgs e) => Close(false);
    private void Open_OnClick(object? sender, RoutedEventArgs e) => Close(true);
}
