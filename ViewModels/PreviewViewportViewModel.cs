namespace HappyColorSplit.ViewModels;

public sealed class PreviewViewportViewModel : ObservableObject
{
    private double _zoom = 1, _centerX = .5, _centerY = .5;
    public PreviewViewportViewModel()
    {
        ZoomInCommand = new(() => { Zoom *= 1.25; return Task.CompletedTask; }, () => Zoom < 16);
        ZoomOutCommand = new(() => { Zoom /= 1.25; return Task.CompletedTask; }, () => Zoom > 1);
        FitCommand = new(() => { SetViewport(1, .5, .5); return Task.CompletedTask; }, () => true);
    }
    public double Zoom { get => _zoom; set => SetViewport(value, CenterX, CenterY); }
    public double CenterX { get => _centerX; set => SetViewport(Zoom, value, CenterY); }
    public double CenterY { get => _centerY; set => SetViewport(Zoom, CenterX, value); }
    public string ZoomLabel => $"{Zoom:0.##}×";
    public AsyncCommand ZoomInCommand { get; }
    public AsyncCommand ZoomOutCommand { get; }
    public AsyncCommand FitCommand { get; }

    public void SetViewport(double zoom, double centerX, double centerY)
    {
        zoom = double.IsFinite(zoom) ? Math.Clamp(zoom, 1, 16) : 1;
        centerX = double.IsFinite(centerX) ? Math.Clamp(centerX, 0, 1) : .5;
        centerY = double.IsFinite(centerY) ? Math.Clamp(centerY, 0, 1) : .5;
        bool z = _zoom != zoom, x = _centerX != centerX, y = _centerY != centerY;
        (_zoom, _centerX, _centerY) = (zoom, centerX, centerY);
        if (z) { Changed(nameof(Zoom)); Changed(nameof(ZoomLabel)); ZoomInCommand.Refresh(); ZoomOutCommand.Refresh(); }
        if (x) Changed(nameof(CenterX)); if (y) Changed(nameof(CenterY));
    }
}
