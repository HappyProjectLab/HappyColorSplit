using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace HappyColorSplit.Controls;

public sealed class ImportActivityBackdrop : Control
{
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<ImportActivityBackdrop, bool>(nameof(IsActive));
    public static readonly StyledProperty<bool> ReduceMotionProperty = AvaloniaProperty.Register<ImportActivityBackdrop, bool>(nameof(ReduceMotion));
    public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    public bool ReduceMotion { get => GetValue(ReduceMotionProperty); set => SetValue(ReduceMotionProperty, value); }
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private bool _attached;

    public ImportActivityBackdrop()
    {
        IsHitTestVisible = false;
        _timer.Tick += (_, _) => InvalidateVisual();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsActiveProperty && change.Property != ReduceMotionProperty) return;
        if (change.Property == IsActiveProperty && IsActive) _clock.Restart();
        UpdateTimer(); InvalidateVisual();
    }
    private void UpdateTimer()
    {
        if (_attached && IsActive && !ReduceMotion) _timer.Start(); else _timer.Stop();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; UpdateTimer(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; _timer.Stop(); base.OnDetachedFromVisualTree(e); }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (!IsActive || ReduceMotion || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        double width = Bounds.Width * .45;
        double x = (_clock.Elapsed.TotalSeconds % 6 / 6) * (Bounds.Width + width) - width;
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = [new(Color.Parse("#007DD3FC"), 0), new(Color.Parse("#247DD3FC"), .5), new(Color.Parse("#007DD3FC"), 1)]
        };
        using var clip = context.PushClip(new Rect(Bounds.Size));
        context.FillRectangle(brush, new Rect(x, 0, width, Bounds.Height));
    }
}
