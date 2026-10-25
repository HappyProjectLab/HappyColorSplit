using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.Controls;

public sealed class LaserMoveEventArgs(LaserMoveRequest request) : EventArgs
{ public LaserMoveRequest Request { get; } = request; }

public sealed partial class LaserWorkspaceCanvas
{
    public static readonly StyledProperty<bool> CanEditProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, bool>(nameof(CanEdit));
    public bool CanEdit { get => GetValue(CanEditProperty); set => SetValue(CanEditProperty, value); }
    public event EventHandler<LaserMoveEventArgs>? MoveRequested;
    private static readonly Cursor DragCursor = new(StandardCursorType.SizeAll);
    private readonly LaserManualPlacementService _editor = new();
    private IReadOnlyList<LaserPlacement> _selection = [];
    private LaserPlacement? _hit;
    private LaserPage? _dragPage;
    private IPointer? _pointer;
    private Point _start;
    private int _dx, _dy, _dragMode;
    private MouseButton _button;
    private Bitmap? _ghost;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PageProperty || change.Property == BoundsProperty
            || (change.Property == CanEditProperty && !CanEdit)) CancelDrag();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var buttons = e.GetCurrentPoint(this).Properties;
        if (!CanEdit || Page is not { } page || _pointer is not null || (!buttons.IsLeftButtonPressed && !buttons.IsRightButtonPressed)) return;
        CancelDrag();
        var point = SheetPoint(e.GetPosition(this), page);
        if (point is null) return;
        var hit = _editor.HitTest(page, (int)point.Value.X, (int)point.Value.Y);
        if (hit is null) return;
        _hit = hit; _dragPage = page; _dragMode = buttons.IsRightButtonPressed ? 1 : 0;
        _button = buttons.IsRightButtonPressed ? MouseButton.Right : MouseButton.Left;
        _selection = _editor.Select(page, hit.Part.SourceIndex, hit.Part.Id, _dragMode == 1);
        // A translucent material-only ghost leaves the original position visible as a reference.
        byte[] png = new LaserPageRenderer().Render(page with { Placements = _selection },
            new(ExportArea.GroupColor, ExportBackground.Transparent), CancellationToken.None, 900, 700);
        using var stream = new MemoryStream(png); _ghost = new Bitmap(stream);
        _start = point.Value; _pointer = e.Pointer; e.Pointer.Capture(this);
        Focusable = true; Focus(); Cursor = DragCursor; e.Handled = true; InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_pointer != e.Pointer || _dragPage is not { } page) return;
        UpdateDelta(e.GetPosition(this), page);
        e.Handled = true; InvalidateVisual();
    }

    private void UpdateDelta(Point position, LaserPage page)
    {
        var sheet = SheetBounds(Bounds.Size, page.Size);
        if (sheet.Width <= 0 || sheet.Height <= 0) return;
        double x = (position.X - sheet.X) * page.Size.Width / sheet.Width;
        double y = (position.Y - sheet.Y) * page.Size.Height / sheet.Height;
        _dx = (int)Math.Clamp(Math.Round(x - _start.X, MidpointRounding.AwayFromZero), -page.Size.Width, page.Size.Width);
        _dy = (int)Math.Clamp(Math.Round(y - _start.Y, MidpointRounding.AwayFromZero), -page.Size.Height, page.Size.Height);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pointer != e.Pointer || e.InitialPressMouseButton != _button || _dragPage is not { } page || _hit is not { } hit) return;
        UpdateDelta(e.GetPosition(this), page);
        var request = new LaserMoveRequest(page, hit.Part.SourceIndex, hit.Part.Id, _dragMode == 1, _dx, _dy);
        CancelDrag(); e.Handled = true;
        if (CanEdit && ReferenceEquals(Page, page) && (request.DeltaX != 0 || request.DeltaY != 0))
            MoveRequested?.Invoke(this, new(request));
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    { base.OnPointerCaptureLost(e); CancelDrag(); }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && _pointer is not null) { CancelDrag(); e.Handled = true; }
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    { CancelDrag(); base.OnDetachedFromVisualTree(e); }

    private Point? SheetPoint(Point point, LaserPage page)
    {
        var sheet = SheetBounds(Bounds.Size, page.Size);
        return sheet.Width > 0 && sheet.Height > 0 && sheet.Contains(point)
            ? new Point((point.X - sheet.X) * page.Size.Width / sheet.Width, (point.Y - sheet.Y) * page.Size.Height / sheet.Height) : null;
    }
    private void CancelDrag()
    {
        var pointer = _pointer; _pointer = null; _dragPage = null; _hit = null; _selection = []; _dx = _dy = 0;
        _ghost?.Dispose(); _ghost = null; Cursor = null;
        if (pointer?.Captured == this) pointer.Capture(null);
        InvalidateVisual();
    }
    private void DrawInteraction(DrawingContext context, Rect sheet, LaserPage page)
    {
        if (!ReferenceEquals(page, _dragPage) || _selection.Count == 0) return;
        using var clip = context.PushClip(sheet);
        double dx = _dx * sheet.Width / page.Size.Width, dy = _dy * sheet.Height / page.Size.Height;
        if (_ghost is { } ghost)
        {
            using var opacity = context.PushOpacity(.65);
            context.DrawImage(ghost, new Rect(ghost.Size), new Rect(sheet.X + dx, sheet.Y + dy, sheet.Width, sheet.Height));
        }
        int margin = page.Settings.MarginPixels;
        bool outside = _selection.Any(p => p.X + _dx < margin || p.Y + _dy < margin
            || p.X + _dx + p.Width > page.Size.Width - margin || p.Y + _dy + p.Height > page.Size.Height - margin);
        var pen = new Pen(new SolidColorBrush(Color.Parse(outside ? "#FF7D86" : "#7DD3FC")), 2);
        foreach (var p in _selection)
            context.DrawRectangle(null, pen, new Rect(sheet.X + (p.X + _dx) * sheet.Width / page.Size.Width,
                sheet.Y + (p.Y + _dy) * sheet.Height / page.Size.Height,
                p.Width * sheet.Width / page.Size.Width, p.Height * sheet.Height / page.Size.Height));
    }
}
