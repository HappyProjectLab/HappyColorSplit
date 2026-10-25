using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

internal static class LaserMarginPreviewTests
{
    public static void Run(Action<bool, string> check)
    {
        var settings = new LaserSettings { WidthCm = 3, HeightCm = 2, Dpi = 254, MarginCm = .2m };
        var page = new LaserPage(1, 1, new("Red", "#FF0000"), settings, settings.GetSize(), []);
        var canvas = new LaserWorkspaceCanvas { Page = page, SheetBackground = Brushes.White };
        var window = new Window { Width = 600, Height = 400, Content = canvas }; window.Show();
        try
        {
            using var frame = Capture(window);
            var sheet = LaserWorkspaceCanvas.SheetBounds(canvas.Bounds.Size, page.Size);
            var usable = LaserWorkspaceCanvas.UsableBounds(sheet, page.Size, settings.MarginPixels);
            var samples = new[]
            {
                new Point((sheet.Left + usable.Left) / 2, sheet.Center.Y), new Point((sheet.Right + usable.Right) / 2, sheet.Center.Y),
                new Point(sheet.Center.X, (sheet.Top + usable.Top) / 2), new Point(sheet.Center.X, (sheet.Bottom + usable.Bottom) / 2),
                new Point((sheet.Left + usable.Left) / 2, sheet.Top - 12), new Point((sheet.Right + usable.Right) / 2, sheet.Top - 12),
                new Point(sheet.Left - 20, (sheet.Top + usable.Top) / 2), new Point(sheet.Left - 20, (sheet.Bottom + usable.Bottom) / 2)
            };
            foreach (var sample in samples)
            {
                var point = canvas.TranslatePoint(sample, window)!.Value;
                check(frame[(int)point.X, (int)point.Y] == new RgbaPixel(74, 78, 86),
                    "The actual rendered sheet and both rulers grey out the same reserved edge bands");
            }
            var center = canvas.TranslatePoint(usable.Center, window)!.Value;
            check(frame[(int)center.X, (int)center.Y] == new RgbaPixel(255, 255, 255), "Reserved edge shading leaves the usable sheet visible");
            var noMargin = settings.Snapshot(); noMargin.MarginCm = 0; canvas.Page = page with { Settings = noMargin };
            using var plain = Capture(window);
            var oldBand = canvas.TranslatePoint(samples[0], window)!.Value;
            check(plain[(int)oldBand.X, (int)oldBand.Y] == new RgbaPixel(255, 255, 255), "Removing the edge margin clears the old shading immediately");
            using var png = RasterImageCodec.Load(new LaserPageRenderer().Render(page, new(ExportArea.White, ExportBackground.Transparent), CancellationToken.None));
            check(png[0, 0].A == 0 && png[20, 20].A == 0, "Grey preview-only margin shading never appears in exported PNGs");
        }
        finally { window.Close(); }
    }

    private static RasterImage Capture(Window window)
    {
        for (int i = 0; i < 3; i++) { Dispatcher.UIThread.RunJobs(); using var update = window.CaptureRenderedFrame(); }
        using var bitmap = window.CaptureRenderedFrame()!;
        using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        return RasterImageCodec.Load(stream.ToArray());
    }
}
