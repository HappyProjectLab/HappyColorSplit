using Avalonia.Media;
using HappyColorSplit.Models;

namespace HappyColorSplit.ViewModels;

public sealed class ColorGroupViewModel(ColorGroup group, long total) : ObservableObject
{
    private bool _exportEnabled = true;
    public bool ExportEnabled { get => _exportEnabled; set => Set(ref _exportEnabled, value); }
    public ColorGroup Group { get; } = group;
    public string Number => $"{Group.Index + 1:D3}";
    public string Hex => Group.Hex;
    public string Coverage => $"{100.0 * Group.PixelCount / total:0.0}%";
    public string PixelCount => Group.PixelCount.ToString("N0");
    public IBrush Swatch => new SolidColorBrush(Color.FromRgb(Group.R, Group.G, Group.B));
}
