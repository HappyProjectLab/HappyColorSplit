using Avalonia.Media;
using Avalonia.Threading;
using System.Diagnostics;
using HappyColorSplit.Models;

namespace HappyColorSplit.ViewModels;

public sealed class ColorGroupViewModel(ColorGroup group, long total) : ObservableObject, IDisposable
{
    private bool _exportEnabled = true;
    private bool _isHighlighted;
    public bool IsHighlighted { get => _isHighlighted; set { if (Set(ref _isHighlighted, value)) Changed(nameof(HighlightBackground)); } }
    public IBrush HighlightBackground => ExportChangeStrength > 0 ? new SolidColorBrush(Color.FromArgb((byte)(150 * ExportChangeStrength), 70, 200, 255)) : IsHighlighted ? HighlightBrush : Brushes.Transparent;
    public IBrush ExportChangeBorder => new SolidColorBrush(Color.FromArgb((byte)(255 * ExportChangeStrength), 125, 225, 255));
    public double ExportChangeStrength { get; private set; }
    private DispatcherTimer? _feedbackTimer;
    public void FlashExportChange(bool reduceMotion)
    {
        _feedbackTimer?.Stop();
        var clock = Stopwatch.StartNew();
        SetFeedback(1);
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _feedbackTimer.Tick += (_, _) =>
        {
            double t = Math.Clamp((clock.Elapsed.TotalMilliseconds - 140) / 480, 0, 1);
            SetFeedback(reduceMotion ? t >= 1 ? 0 : 1 : 1 - t * t * (3 - 2 * t));
            if (t >= 1) { _feedbackTimer.Stop(); _feedbackTimer = null; }
        };
        _feedbackTimer.Start();
    }
    private void SetFeedback(double strength)
    {
        ExportChangeStrength = strength;
        Changed(nameof(ExportChangeStrength)); Changed(nameof(HighlightBackground)); Changed(nameof(ExportChangeBorder));
    }
    public void Dispose() { _feedbackTimer?.Stop(); _feedbackTimer = null; }
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#3049BCE8"));
    public bool ExportEnabled { get => _exportEnabled; set => Set(ref _exportEnabled, value); }
    public ColorGroup Group { get; } = group;
    public string Number => $"{Group.Index + 1:D3}";
    public string Hex => Group.Hex;
    public string Coverage => $"{100.0 * Group.PixelCount / total:0.0}%";
    public string PixelCount => Group.PixelCount.ToString("N0");
    public IBrush Swatch => new SolidColorBrush(Color.FromRgb(Group.R, Group.G, Group.B));
}
