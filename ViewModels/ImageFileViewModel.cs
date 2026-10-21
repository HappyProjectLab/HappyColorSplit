using Avalonia.Media.Imaging;

namespace HappyColorSplit.ViewModels;

public sealed class ImageFileViewModel(string path, Bitmap thumbnail) : IDisposable
{
    public string FilePath { get; } = path;
    public string FileName => Path.GetFileName(FilePath);
    public Bitmap Thumbnail { get; } = thumbnail;
    public IReadOnlySet<string>? ExportColors { get; set; }
    public void Dispose() => Thumbnail.Dispose();
}
