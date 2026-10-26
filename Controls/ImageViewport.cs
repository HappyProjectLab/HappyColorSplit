using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace HappyColorSplit.Controls;

public sealed class ImageViewport : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty = AvaloniaProperty.Register<ImageViewport, IImage?>(nameof(Source));
    public static readonly StyledProperty<double> ZoomProperty = AvaloniaProperty.Register<ImageViewport, double>(nameof(Zoom), 1);
    public static readonly StyledProperty<double> CenterXProperty = AvaloniaProperty.Register<ImageViewport, double>(nameof(CenterX), .5);
    public static readonly StyledProperty<double> CenterYProperty = AvaloniaProperty.Register<ImageViewport, double>(nameof(CenterY), .5);
    static ImageViewport() => AffectsRender<ImageViewport>(SourceProperty, ZoomProperty, CenterXProperty, CenterYProperty);
    public IImage? Source { get => GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public double Zoom { get => GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public double CenterX { get => GetValue(CenterXProperty); set => SetValue(CenterXProperty, value); }
    public double CenterY { get => GetValue(CenterYProperty); set => SetValue(CenterYProperty, value); }
    private IPointer? _pointer;
    private Point _start;
    private double _startX, _startY;

    public static Rect ImageBounds(Size viewport, Size image, double zoom, double centerX, double centerY)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0 || image.Width <= 0 || image.Height <= 0) return default;
        double scale = Math.Min(viewport.Width / image.Width, viewport.Height / image.Height) * Math.Clamp(zoom, 1, 16);
        double w = image.Width * scale, h = image.Height * scale;
        double cx = Center(centerX, viewport.Width, w), cy = Center(centerY, viewport.Height, h);
        return new(viewport.Width / 2 - cx * w, viewport.Height / 2 - cy * h, w, h);
    }
    private static double Center(double value, double viewport, double image) => image <= viewport ? .5 : Math.Clamp(value, viewport / (2 * image), 1 - viewport / (2 * image));
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        // A transparent hit surface lets the entire preview receive navigation gestures.
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));
        if (Source is not { } image) return;
        using var clip = context.PushClip(new Rect(Bounds.Size));
        var rect = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY);
        if (rect.Width > 0 && rect.Height > 0) context.DrawImage(image, new Rect(image.Size), rect);
    }
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (Source is not { } image || e.Delta.Y == 0) return;
        EndPan(); var old = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY);
        if (old.Width <= 0 || old.Height <= 0) return;
        var point = e.GetPosition(this); double next = Math.Clamp(Zoom * Math.Pow(1.25, Math.Clamp(e.Delta.Y, -4, 4)), 1, 16);
        double u = (point.X - old.X) / old.Width, v = (point.Y - old.Y) / old.Height;
        var resized = ImageBounds(Bounds.Size, image.Size, next, .5, .5);
        SetCurrentValue(ZoomProperty, next);
        SetCurrentValue(CenterXProperty, Center(u - (point.X - Bounds.Width / 2) / resized.Width, Bounds.Width, resized.Width));
        SetCurrentValue(CenterYProperty, Center(v - (point.Y - Bounds.Height / 2) / resized.Height, Bounds.Height, resized.Height));
        Focusable = true; Focus(); e.Handled = true;
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Source is not { } image || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2) { Fit(); e.Handled = true; return; }
        var rect = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        _start = e.GetPosition(this); _startX = Center(CenterX, Bounds.Width, rect.Width); _startY = Center(CenterY, Bounds.Height, rect.Height);
        _pointer = e.Pointer; e.Pointer.Capture(this); Cursor = new Cursor(StandardCursorType.SizeAll); Focusable = true; Focus(); e.Handled = true;
    }
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer || Source is not { } image) return;
        var rect = ImageBounds(Bounds.Size, image.Size, Zoom, CenterX, CenterY); var p = e.GetPosition(this);
        if (rect.Width <= 0 || rect.Height <= 0) return;
        SetCurrentValue(CenterXProperty, Center(_startX - (p.X - _start.X) / rect.Width, Bounds.Width, rect.Width));
        SetCurrentValue(CenterYProperty, Center(_startY - (p.Y - _start.Y) / rect.Height, Bounds.Height, rect.Height)); e.Handled = true;
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    { base.OnPointerReleased(e); if (_pointer == e.Pointer) { EndPan(); e.Handled = true; } }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) { base.OnPointerCaptureLost(e); EndPan(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { EndPan(); base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    { base.OnPropertyChanged(change); if (change.Property == SourceProperty || change.Property == BoundsProperty) EndPan(); }
    private void EndPan() { var pointer = _pointer; _pointer = null; Cursor = null; if (pointer?.Captured == this) pointer.Capture(null); }
    private void Fit() { EndPan(); SetCurrentValue(ZoomProperty, 1d); SetCurrentValue(CenterXProperty, .5); SetCurrentValue(CenterYProperty, .5); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e); if (Source is null) return;
        if (e.Key is Key.Home or Key.F) { Fit(); e.Handled = true; }
        else if (e.Key is Key.Add or Key.OemPlus or Key.Subtract or Key.OemMinus)
        { SetCurrentValue(ZoomProperty, Math.Clamp(Zoom * (e.Key is Key.Add or Key.OemPlus ? 1.25 : .8), 1, 16)); e.Handled = true; }
    }
}
