using Avalonia.Media.Imaging;
using HappyColorSplit.Models;

namespace HappyColorSplit.ViewModels;

public sealed class ImageFileViewModel(string path, Bitmap thumbnail, int sourceWidth, int sourceHeight, int dpi = 300, int scaleUnit = 0) : IDisposable
{
    public string FilePath { get; } = path;
    public string FileName => Path.GetFileName(FilePath);
    public Bitmap Thumbnail { get; } = thumbnail;
    public IReadOnlySet<string>? ExportColors { get; set; }
    public bool ColorsDetected { get; set; }
    public int SourceWidth { get; private set; } = sourceWidth;
    public int SourceHeight { get; private set; } = sourceHeight;
    public ImageScale Scale { get; set; } = new(Dpi: dpi);
    public int ScaleUnit { get; set; } = scaleUnit;
    public void UpdateSourceSize(int width, int height) { SourceWidth = width; SourceHeight = height; }
    public void Dispose() => Thumbnail.Dispose();
}
