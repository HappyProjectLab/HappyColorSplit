using Avalonia.Controls;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

namespace HappyColorSplit.Services;

public interface ILicenseWindowService
{
    Task ShowAsync(Texts texts);
}

public sealed class LicenseWindowService(Window owner) : ILicenseWindowService
{
    public async Task ShowAsync(Texts texts)
    {
        using var vm = new LicensesViewModel(texts, new LicenseDocumentService());
        var window = new LicensesWindow { DataContext = vm };
        await window.ShowDialog(owner);
    }
}
