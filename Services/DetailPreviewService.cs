using Avalonia.Controls;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

namespace HappyColorSplit.Services;

public interface IDetailPreviewService
{
    void Show(DetailPreviewViewModel preview);
}

public sealed class DetailPreviewService(Window owner) : IDetailPreviewService
{
    public void Show(DetailPreviewViewModel preview)
    {
        var window = new DetailPreviewWindow { DataContext = preview };
        window.Closed += (_, _) => preview.Dispose();
        try { window.Show(owner); }
        catch { preview.Dispose(); throw; }
    }
}
