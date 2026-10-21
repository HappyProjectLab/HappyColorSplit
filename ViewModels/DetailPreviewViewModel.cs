using Avalonia.Media.Imaging;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed class DetailPreviewViewModel : ObservableObject, IDisposable
{
    private double _zoom = 100, _overlayOpacity = 0.6;
    private bool _showOverlay = true;
    public DetailPreviewViewModel(byte[] original, byte[] mask, byte[] overlay, Texts texts, string title)
    {
        Original = Decode(original); Mask = Decode(mask); Overlay = Decode(overlay);
        Text = texts; Title = $"HappyColorSplit · {title}";
    }
    public Texts Text { get; }
    public string Title { get; }
    public Bitmap Original { get; }
    public Bitmap Mask { get; }
    public Bitmap Overlay { get; }
    public double Zoom
    {
        get => _zoom;
        set { if (Set(ref _zoom, Math.Clamp(value, 25, 800))) { Changed(nameof(ImageWidth)); Changed(nameof(ImageHeight)); } }
    }
    public double ImageWidth => Original.PixelSize.Width * Zoom / 100;
    public double ImageHeight => Original.PixelSize.Height * Zoom / 100;
    public double OverlayOpacity { get => _overlayOpacity; set => Set(ref _overlayOpacity, Math.Clamp(value, 0, 1)); }
    public bool ShowOverlay { get => _showOverlay; set => Set(ref _showOverlay, value); }
    private static Bitmap Decode(byte[] bytes) { using var stream = new MemoryStream(bytes); return new Bitmap(stream); }
    public void Dispose() { Original.Dispose(); Mask.Dispose(); Overlay.Dispose(); }
}
