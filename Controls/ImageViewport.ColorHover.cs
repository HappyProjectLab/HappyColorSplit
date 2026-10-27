using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using HappyColorSplit.Models;

namespace HappyColorSplit.Controls;

public sealed partial class ImageViewport
{
    public static readonly StyledProperty<ColorAnalysis?> AnalysisProperty = AvaloniaProperty.Register<ImageViewport, ColorAnalysis?>(nameof(Analysis));
    public static readonly StyledProperty<bool> ColorPickingEnabledProperty = AvaloniaProperty.Register<ImageViewport, bool>(nameof(ColorPickingEnabled));
    public static readonly StyledProperty<bool> ReduceMotionProperty = AvaloniaProperty.Register<ImageViewport, bool>(nameof(ReduceMotion));
    public ColorAnalysis? Analysis { get => GetValue(AnalysisProperty); set => SetValue(AnalysisProperty, value); }
    public bool ColorPickingEnabled { get => GetValue(ColorPickingEnabledProperty); set => SetValue(ColorPickingEnabledProperty, value); }
    public bool ReduceMotion { get => GetValue(ReduceMotionProperty); set => SetValue(ReduceMotionProperty, value); }
    public int HighlightedGroupIndex { get; private set; } = -1;
    public event EventHandler<int>? ColorGroupHighlighted;
    public event EventHandler<int>? ColorGroupClicked;
    public event EventHandler<int>? ColorGroupExportToggleRequested;
    private bool _dragging;
    private readonly List<ColorHighlight> _highlights = [];
    private readonly Stopwatch _highlightClock = new();
    private readonly DispatcherTimer _highlightTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };

    public ImageViewport() => _highlightTimer.Tick += (_, _) => AdvanceHighlight();

    public int ColorGroupAt(Point point)
    {
        if (!ColorPickingEnabled || Analysis is not { } analysis || Source is not { } image) return -1;
        var rect = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY);
        if (!new Rect(Bounds.Size).Contains(point) || rect.Width <= 0 || rect.Height <= 0) return -1;
        double u = (point.X - rect.X) / rect.Width, v = (point.Y - rect.Y) / rect.Height;
        if (u < 0 || v < 0 || u >= 1 || v >= 1) return -1;
        return analysis.Labels[(int)(v * analysis.Height) * analysis.Width + (int)(u * analysis.Width)];
    }

    private void SelectColorHighlight(int group)
    {
        if (group == HighlightedGroupIndex) return;
        AdvanceHighlight();
        HighlightedGroupIndex = group;
        if (group >= 0 && !_highlights.Any(layer => layer.Group == group)) _highlights.Add(new(group));
        foreach (var layer in _highlights)
        {
            layer.From = layer.Opacity;
            layer.Target = layer.Group == group ? 1 : 0;
        }
        // Prepare the new surface before starting the clock, so first-use rendering cannot consume the transition.
        if (Analysis is { } analysis && Source is { } image)
        {
            var rect = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY);
            if (rect.Width > 0 && rect.Height > 0)
                foreach (var layer in _highlights.Where(layer => layer.Overlay is null)) BuildHighlightMasks(layer, analysis, rect.Size);
        }
        _highlightClock.Restart();
        if (ReduceMotion) AdvanceHighlight(); else _highlightTimer.Start();
        InvalidateVisual();
        ColorGroupHighlighted?.Invoke(this, group);
    }

    private void AdvanceHighlight()
    {
        double t = ReduceMotion ? 1 : Math.Clamp(_highlightClock.Elapsed.TotalMilliseconds / 120, 0, 1);
        double eased = t * t * (3 - 2 * t);
        foreach (var layer in _highlights) layer.Opacity = layer.From + (layer.Target - layer.From) * eased;
        if (t >= 1)
        {
            _highlightTimer.Stop();
            foreach (var layer in _highlights.Where(layer => layer.Target == 0).ToArray()) { layer.Dispose(); _highlights.Remove(layer); }
        }
        InvalidateVisual();
    }

    private void ClearColorHighlight()
    {
        _highlightTimer.Stop(); _highlightClock.Reset();
        foreach (var layer in _highlights) layer.Dispose();
        _highlights.Clear();
        bool changed = HighlightedGroupIndex != -1;
        HighlightedGroupIndex = -1;
        InvalidateVisual();
        if (changed) ColorGroupHighlighted?.Invoke(this, -1);
    }

    private void InvalidateHighlightMasks()
    {
        foreach (var layer in _highlights) layer.Dispose();
        InvalidateVisual();
    }

    private void DrawColorHighlight(DrawingContext context, IImage image, Rect rect)
    {
        if (Analysis is not { } analysis || rect.Width <= 0 || rect.Height <= 0 || _highlights.Count == 0) return;
        // Draw only a transparent tint over the original using exactly the same destination.
        // No image brushes, separate mask origins or copied source pixels can offset the effect.
        foreach (var layer in _highlights)
        {
            if (layer.Opacity <= 0) continue;
            if (layer.Overlay is null) BuildHighlightMasks(layer, analysis, rect.Size);
            using var opacity = context.PushOpacity(layer.Opacity);
            context.DrawImage(layer.Overlay!, new Rect(layer.Overlay!.Size), rect);
        }
    }

    private static void BuildHighlightMasks(ColorHighlight layer, ColorAnalysis analysis, Size size)
    {
        double scale = Math.Min(1, 1400d / Math.Max(size.Width, size.Height));
        int w = Math.Max(1, (int)Math.Ceiling(size.Width * scale)), h = Math.Max(1, (int)Math.Ceiling(size.Height * scale));
        var overlay = new byte[w * h * 4];
        var selected = new bool[w * h];
        var distance = new byte[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int label = analysis.Labels[Math.Min(analysis.Height - 1, (int)((y + .5) * analysis.Height / h)) * analysis.Width +
                Math.Min(analysis.Width - 1, (int)((x + .5) * analysis.Width / w))];
            int i = y * w + x;
            distance[i] = label == layer.Group ? (byte)0 : (byte)5;
            selected[i] = distance[i] == 0;
        }
        // Bounded distance transform builds a soft contact shadow in linear time.
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (x > 0) distance[i] = (byte)Math.Min(distance[i], distance[i - 1] + 1);
            if (y > 0) distance[i] = (byte)Math.Min(distance[i], distance[i - w] + 1);
        }
        for (int y = h - 1; y >= 0; y--)
        for (int x = w - 1; x >= 0; x--)
        {
            int i = y * w + x;
            if (x + 1 < w) distance[i] = (byte)Math.Min(distance[i], distance[i + 1] + 1);
            if (y + 1 < h) distance[i] = (byte)Math.Min(distance[i], distance[i + w] + 1);
        }
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && selected[y * w + x];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x, p = i * 4;
            if (distance[i] > 0)
            {
                double contact = Math.Max(0, 5 - distance[i]) * 22 / 255d;
                overlay[p + 3] = (byte)Math.Round(255 * (1 - .76 * (1 - contact)));
            }
            else
            {
                overlay[p] = overlay[p + 1] = overlay[p + 2] = 255;
                overlay[p + 3] = 16;
                bool light = !Inside(x, y - 1) || !Inside(x - 1, y);
                bool dark = !Inside(x, y + 1) || !Inside(x + 1, y);
                if (!light && !dark) continue;
                byte c = light ? (byte)255 : (byte)15;
                overlay[p] = overlay[p + 1] = overlay[p + 2] = c;
                overlay[p + 3] = light ? (byte)225 : (byte)175;
            }
        }
        layer.Overlay = CreateHighlightBitmap(w, h, overlay);
    }

    private static WriteableBitmap CreateHighlightBitmap(int width, int height, byte[] pixels)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        for (int y = 0; y < height; y++) Marshal.Copy(pixels, y * width * 4, buffer.Address + y * buffer.RowBytes, width * 4);
        return bitmap;
    }

    private sealed class ColorHighlight(int group) : IDisposable
    {
        public int Group { get; } = group;
        public double From, Target, Opacity;
        public WriteableBitmap? Overlay;
        public void Dispose() { Overlay?.Dispose(); Overlay = null; }
    }
}
