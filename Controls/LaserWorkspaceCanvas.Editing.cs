using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.Controls;

public sealed class LaserMoveEventArgs(LaserTransformRequest request) : EventArgs
{ public LaserTransformRequest Request { get; } = request; }
public sealed class LaserLassoEventArgs(LaserLassoRequest request) : EventArgs
{ public LaserLassoRequest Request { get; } = request; }

public sealed partial class LaserWorkspaceCanvas
{
    public static readonly StyledProperty<bool> CanEditProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, bool>(nameof(CanEdit));
    public static readonly StyledProperty<LaserSelection?> SelectionProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, LaserSelection?>(nameof(Selection));
    public bool CanEdit { get => GetValue(CanEditProperty); set => SetValue(CanEditProperty, value); }
    public LaserSelection? Selection { get => GetValue(SelectionProperty); set => SetValue(SelectionProperty, value); }
    public event EventHandler<LaserMoveEventArgs>? MoveRequested;
    public event EventHandler<LaserLassoEventArgs>? LassoRequested;
    private static readonly Cursor DragCursor = new(StandardCursorType.SizeAll);
    private static readonly Cursor LassoCursor = new(StandardCursorType.Cross);
    private readonly LaserManualPlacementService _editor = new();
    private IReadOnlyList<LaserPlacement> _selected = [], _dragParts = [];
    private LaserPage? _gesturePage;
    private IPointer? _pointer;
    private Point _start;
    private int _dx, _dy;
    private bool _lasso;
    private readonly List<LaserPoint> _points = [];
    private MouseButton _button;
    private Bitmap? _ghost;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PageProperty || change.Property == BoundsProperty || (change.Property == CanEditProperty && !CanEdit)) EndGesture();
        if (change.Property == PageProperty || change.Property == SelectionProperty)
        {
            _selected = Page is { } page && ReferenceEquals(Selection?.Page, page) ? _editor.SelectMany(page, Selection.Parts) : [];
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e); var buttons = e.GetCurrentPoint(this).Properties;
        if (!CanEdit || Page is not { } page || _pointer is not null || (!buttons.IsLeftButtonPressed && !buttons.IsRightButtonPressed)) return;
        var point = SheetPoint(e.GetPosition(this), page, false);
        if (point is null) return;
        var hit = _editor.HitTest(page, (int)point.Value.X, (int)point.Value.Y);
        bool right = buttons.IsRightButtonPressed;
        _lasso = !right && (hit is null || e.KeyModifiers.HasFlag(KeyModifiers.Shift));
        if (right && hit is null) return;
        if (!_lasso)
        {
            if (right || !_selected.Any(p => p.Part == hit!.Part))
            {
                var parts = _editor.Select(page, hit!.Part.SourceIndex, hit.Part.Id, right);
                SetCurrentValue(SelectionProperty, new LaserSelection(page, Keys(parts)));
            }
            _dragParts = _selected;
        }
        else { _points.Clear(); _points.Add(new(point.Value.X, point.Value.Y)); }
        _gesturePage = page; _start = point.Value; _button = right ? MouseButton.Right : MouseButton.Left;
        _pointer = e.Pointer; e.Pointer.Capture(this); Focusable = true; Focus();
        Cursor = _lasso ? LassoCursor : DragCursor; e.Handled = true; InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer || _gesturePage is not { } page) return;
        UpdateGesture(e.GetPosition(this), page);
        if (!_lasso && _ghost is null && (_dx != 0 || _dy != 0))
        {
            byte[] png = new LaserPageRenderer().Render(page with { Placements = _dragParts },
                new(ExportArea.GroupColor, ExportBackground.Transparent), CancellationToken.None, 900, 700);
            using var stream = new MemoryStream(png); _ghost = new Bitmap(stream);
        }
        e.Handled = true; InvalidateVisual();
    }

    private void UpdateGesture(Point position, LaserPage page)
    {
        var point = SheetPoint(position, page, true);
        if (point is null) return;
        if (_lasso)
        {
            var last = _points[^1]; var sheet = SheetBounds(Bounds.Size, page.Size);
            double dx = (point.Value.X - last.X) * sheet.Width / page.Size.Width, dy = (point.Value.Y - last.Y) * sheet.Height / page.Size.Height;
            if ((dx * dx + dy * dy >= 4 || _points.Count == 1) && _points.Count < 1024) _points.Add(new(point.Value.X, point.Value.Y));
        }
        else
        {
            _dx = (int)Math.Round(point.Value.X - _start.X, MidpointRounding.AwayFromZero);
            _dy = (int)Math.Round(point.Value.Y - _start.Y, MidpointRounding.AwayFromZero);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer || e.InitialPressMouseButton != _button || _gesturePage is not { } page) return;
        UpdateGesture(e.GetPosition(this), page);
        bool lasso = _lasso; var polygon = _points.ToArray();
        var move = new LaserTransformRequest(page, Keys(_dragParts), _dx, _dy);
        EndGesture(); e.Handled = true;
        if (!CanEdit || !ReferenceEquals(Page, page)) return;
        if (lasso) LassoRequested?.Invoke(this, new(new(page, polygon)));
        else if (move.DeltaX != 0 || move.DeltaY != 0) MoveRequested?.Invoke(this, new(move));
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    { base.OnPointerCaptureLost(e); EndGesture(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            if (_pointer is not null) EndGesture(); else SetCurrentValue(SelectionProperty, null);
            e.Handled = true;
        }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { EndGesture(); base.OnDetachedFromVisualTree(e); }

    private Point? SheetPoint(Point point, LaserPage page, bool allowOutside)
    {
        var sheet = SheetBounds(Bounds.Size, page.Size);
        if (sheet.Width <= 0 || sheet.Height <= 0 || (!allowOutside && !sheet.Contains(point))) return null;
        double x = (point.X - sheet.X) * page.Size.Width / sheet.Width, y = (point.Y - sheet.Y) * page.Size.Height / sheet.Height;
        return allowOutside ? new Point(Math.Clamp(x, -page.Size.Width, 2L * page.Size.Width), Math.Clamp(y, -page.Size.Height, 2L * page.Size.Height)) : new Point(x, y);
    }
    private static LaserPartKey[] Keys(IReadOnlyList<LaserPlacement> parts) => parts.Select(p => new LaserPartKey(p.Part.SourceIndex, p.Part.Id)).ToArray();
    private void EndGesture()
    {
        var pointer = _pointer; _pointer = null; _gesturePage = null; _dragParts = []; _points.Clear(); _dx = _dy = 0;
        _ghost?.Dispose(); _ghost = null; Cursor = null;
        if (pointer?.Captured == this) pointer.Capture(null);
        InvalidateVisual();
    }
    private void DrawInteraction(DrawingContext context, Rect sheet, LaserPage page)
    {
        using var clip = context.PushClip(sheet);
        bool active = ReferenceEquals(page, _gesturePage);
        double dx = active && !_lasso ? _dx * sheet.Width / page.Size.Width : 0;
        double dy = active && !_lasso ? _dy * sheet.Height / page.Size.Height : 0;
        if (_ghost is { } ghost && active)
        {
            using var opacity = context.PushOpacity(.65);
            context.DrawImage(ghost, new Rect(ghost.Size), new Rect(sheet.X + dx, sheet.Y + dy, sheet.Width, sheet.Height));
        }
        var selected = active && !_lasso ? _dragParts : _selected;
        int margin = page.Settings.MarginPixels;
        bool outside = active && !_lasso && selected.Any(p => p.X + _dx < margin || p.Y + _dy < margin
            || p.X + _dx + p.Width > page.Size.Width - margin || p.Y + _dy + p.Height > page.Size.Height - margin);
        var brush = new SolidColorBrush(Color.Parse(outside ? "#FF7D86" : "#7DD3FC")); var pen = new Pen(brush, 2);
        foreach (var p in selected)
            context.DrawRectangle(null, pen, new Rect(sheet.X + p.X * sheet.Width / page.Size.Width + dx,
                sheet.Y + p.Y * sheet.Height / page.Size.Height + dy, p.Width * sheet.Width / page.Size.Width, p.Height * sheet.Height / page.Size.Height));
        if (active && _lasso && _points.Count > 1)
        {
            var geometry = new StreamGeometry();
            using (var path = geometry.Open())
            {
                Point Map(LaserPoint p) => new(sheet.X + p.X * sheet.Width / page.Size.Width, sheet.Y + p.Y * sheet.Height / page.Size.Height);
                path.BeginFigure(Map(_points[0]), true);
                foreach (var point in _points.Skip(1)) path.LineTo(Map(point));
                path.EndFigure(true);
            }
            context.DrawGeometry(new SolidColorBrush(Color.FromArgb(35, 125, 211, 252)), new Pen(brush, 1), geometry);
        }
    }
}
