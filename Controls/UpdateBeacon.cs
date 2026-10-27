using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace HappyColorSplit.Controls;

public sealed class UpdateBeacon : Control
{
    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<UpdateBeacon, bool>(nameof(IsActive));
    public static readonly StyledProperty<bool> ReduceMotionProperty = AvaloniaProperty.Register<UpdateBeacon, bool>(nameof(ReduceMotion));
    public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    public bool ReduceMotion { get => GetValue(ReduceMotionProperty); set => SetValue(ReduceMotionProperty, value); }
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private bool _attached;
    public UpdateBeacon() { IsHitTestVisible = false; _timer.Tick += (_, _) => InvalidateVisual(); }
    private void UpdateTimer() { if (_attached && IsActive && !ReduceMotion) _timer.Start(); else _timer.Stop(); InvalidateVisual(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    { base.OnPropertyChanged(change); if (change.Property == IsActiveProperty || change.Property == ReduceMotionProperty) UpdateTimer(); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; UpdateTimer(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; _timer.Stop(); base.OnDetachedFromVisualTree(e); }
    public override void Render(DrawingContext context)
    {
        if (!IsActive || Bounds.Width <= 1 || Bounds.Height <= 1) return;
        var bounds = new Rect(Bounds.Size);
        using var clip = context.PushClip(new RoundedRect(bounds, 8));
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#807DD3FC")), 1), bounds.Deflate(.5), 8, 8);
        if (ReduceMotion) return;
        double width = Math.Max(60, Bounds.Width * .45), x = (_clock.Elapsed.TotalSeconds % 5 / 5) * (Bounds.Width + width) - width;
        var glow = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
            GradientStops = [new(Color.Parse("#007DD3FC"), 0), new(Color.Parse("#307DD3FC"), .5), new(Color.Parse("#007DD3FC"), 1)] };
        context.FillRectangle(glow, new Rect(x, 0, width, Bounds.Height));
        using var brighter = context.PushOpacity(.8);
        context.FillRectangle(glow, new Rect(x, 0, width, 2));
        context.FillRectangle(glow, new Rect(x, Bounds.Height - 2, width, 2));
    }
}
