using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace HappyColorSplit.Controls;

internal sealed class SidebarBeacon : Control
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private bool _attached, _running;
    public SidebarBeacon() { IsHitTestVisible = false; _timer.Tick += (_, _) => InvalidateVisual(); }
    public void SetRunning(bool running) { _running = running; UpdateTimer(); InvalidateVisual(); }
    private void UpdateTimer() { if (_attached && _running) _timer.Start(); else _timer.Stop(); }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; UpdateTimer(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; _timer.Stop(); base.OnDetachedFromVisualTree(e); }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#32689DB8")), new Rect(Bounds.Size));
        if (!_running || Bounds.Height <= 0) return;
        double length = Math.Clamp(Bounds.Height * .18, 24, 70), y = (_clock.Elapsed.TotalSeconds % 8 / 8) * (Bounds.Height + 2 * length) - length;
        var glow = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = [new GradientStop(Color.Parse("#007DD3FC"), 0), new GradientStop(Color.Parse("#707DD3FC"), .5), new GradientStop(Color.Parse("#007DD3FC"), 1)] };
        using var clip = context.PushClip(new Rect(Bounds.Size)); context.FillRectangle(glow, new Rect(0, y, Bounds.Width, length));
    }
}
