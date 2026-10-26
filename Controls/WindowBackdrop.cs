using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HappyColorSplit.Services;
using SkiaSharp;

namespace HappyColorSplit.Controls;

public sealed class WindowBackdrop : Border
{
    private static readonly Lazy<Bitmap> Collage = new(LoadCollage);

    public WindowBackdrop()
    {
        IsHitTestVisible = false;
        Background = new ImageBrush { Source = Collage.Value, Stretch = Stretch.UniformToFill };
    }

    private static Bitmap LoadCollage()
    {
        using var source = AssetLoader.Open(new Uri("avares://HappyColorSplit/docs/images/01_Titelcollage.png"));
        using var original = SKBitmap.Decode(source) ?? throw new IOException("Title collage could not be decoded.");
        using var gray = original.Copy(SKColorType.Gray8) ?? throw new IOException("Title collage could not be desaturated.");
        using var png = new MemoryStream(RasterImageCodec.EncodePng(gray));
        // Shared for the lifetime of the UI; opening another window does not decode the artwork again.
        return new Bitmap(png);
    }
}
