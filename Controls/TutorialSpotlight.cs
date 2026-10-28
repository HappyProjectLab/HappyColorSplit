using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace HappyColorSplit.Controls;

public sealed class TutorialSpotlight : Control
{
    private Rect _target;
    public Rect HighlightBounds => _target;
    public TutorialSpotlight() => IsHitTestVisible = false;
    public void SetTarget(Control? target)
    {
        Rect rect = default;
        if (target is { IsEffectivelyVisible: true } && target.Bounds.Width > 0 && target.Bounds.Height > 0 && target.TranslatePoint(default, this) is { } point)
            rect = new Rect(point, target.Bounds.Size).Inflate(6).Intersect(new Rect(Bounds.Size).Deflate(5));
        if (rect == _target) return;
        _target = rect; InvalidateVisual();
    }
    public override void Render(DrawingContext context)
    {
        if (_target.Width <= 0 || _target.Height <= 0) return;
        var shade = new SolidColorBrush(Color.Parse("#50101824"));
        context.FillRectangle(shade, new Rect(0, 0, Bounds.Width, _target.Top));
        context.FillRectangle(shade, new Rect(0, _target.Bottom, Bounds.Width, Math.Max(0, Bounds.Height - _target.Bottom)));
        context.FillRectangle(shade, new Rect(0, _target.Top, _target.Left, _target.Height));
        context.FillRectangle(shade, new Rect(_target.Right, _target.Top, Math.Max(0, Bounds.Width - _target.Right), _target.Height));
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#407DD3FC")), 8), _target, 10, 10);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#B7E9FF")), 3), _target, 10, 10);
    }
}
