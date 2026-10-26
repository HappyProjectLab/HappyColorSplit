using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Threading;

namespace HappyColorSplit.Controls;

public sealed class CollapsibleSidebar : ContentControl
{
    public const double RailWidth = 44;
    public static readonly StyledProperty<bool> IsCollapsedProperty = AvaloniaProperty.Register<CollapsibleSidebar, bool>(nameof(IsCollapsed));
    public static readonly StyledProperty<bool> ReduceMotionProperty = AvaloniaProperty.Register<CollapsibleSidebar, bool>(nameof(ReduceMotion));
    public static readonly StyledProperty<bool> IsRightProperty = AvaloniaProperty.Register<CollapsibleSidebar, bool>(nameof(IsRight));
    public static readonly StyledProperty<bool> HasNoticeProperty = AvaloniaProperty.Register<CollapsibleSidebar, bool>(nameof(HasNotice));
    public static readonly StyledProperty<double> ExpandedWidthProperty = AvaloniaProperty.Register<CollapsibleSidebar, double>(nameof(ExpandedWidth), 220);
    public static readonly StyledProperty<string> RailLabelProperty = AvaloniaProperty.Register<CollapsibleSidebar, string>(nameof(RailLabel), "");
    public static readonly StyledProperty<string> CollapseLabelProperty = AvaloniaProperty.Register<CollapsibleSidebar, string>(nameof(CollapseLabel), "");
    public bool IsCollapsed { get => GetValue(IsCollapsedProperty); set => SetValue(IsCollapsedProperty, value); }
    public bool ReduceMotion { get => GetValue(ReduceMotionProperty); set => SetValue(ReduceMotionProperty, value); }
    public bool IsRight { get => GetValue(IsRightProperty); set => SetValue(IsRightProperty, value); }
    public bool HasNotice { get => GetValue(HasNoticeProperty); set => SetValue(HasNoticeProperty, value); }
    public double ExpandedWidth { get => GetValue(ExpandedWidthProperty); set => SetValue(ExpandedWidthProperty, value); }
    public string RailLabel { get => GetValue(RailLabelProperty); set => SetValue(RailLabelProperty, value); }
    public string CollapseLabel { get => GetValue(CollapseLabelProperty); set => SetValue(CollapseLabelProperty, value); }
    private readonly DispatcherTimer _animation = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _elapsed = new();
    private ContentPresenter? _presenter;
    private Button? _rail, _collapse;
    private TextBlock? _railText, _arrow, _notice;
    private SidebarBeacon? _beacon;
    private bool _attached;
    private double _from, _target;

    public CollapsibleSidebar()
    {
        ClipToBounds = true;
        Template = new FuncControlTemplate<CollapsibleSidebar>((owner, _) => owner.Build());
        _animation.Tick += (_, _) =>
        {
            double t = Math.Clamp(_elapsed.Elapsed.TotalMilliseconds / 280, 0, 1);
            Width = _from + (_target - _from) * (1 - Math.Pow(1 - t, 3));
            if (t >= 1) _animation.Stop(); UpdateVisuals();
        };
    }
    private Control Build()
    {
        var grid = new Grid { ClipToBounds = true };
        _presenter = new ContentPresenter(); _presenter.Bind(ContentPresenter.ContentProperty, new Binding(nameof(Content)) { Source = this });
        grid.Children.Add(_presenter);
        _collapse = new Button { Width = 20, Padding = new Thickness(0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch, VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center, Background = Brush("#B0182433"), Foreground = Brush("#A9CCE0") };
        _collapse.Click += (_, _) => SetCurrentValue(IsCollapsedProperty, !IsCollapsed);
        grid.Children.Add(_collapse);
        _railText = new TextBlock { FontSize = 12, Foreground = Brush("#C3D9EC") };
        _arrow = new TextBlock { FontSize = 24, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        _notice = new TextBlock { Text = "●", FontSize = 12, Foreground = Brush("#FFC25C"), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        var label = new LayoutTransformControl { LayoutTransform = new RotateTransform(-90), Child = _railText,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        var railContent = new Grid { RowDefinitions = new("Auto,*,Auto"), Margin = new Thickness(0, 8) };
        railContent.Children.Add(_arrow); Grid.SetRow(label, 1); railContent.Children.Add(label); Grid.SetRow(_notice, 2); railContent.Children.Add(_notice);
        _rail = new Button { Content = railContent, Width = RailWidth, Padding = new Thickness(0), CornerRadius = new CornerRadius(10), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            Background = Brush("#E3182433"), Foreground = Brush("#C3D9EC") };
        _rail.Click += (_, _) => SetCurrentValue(IsCollapsedProperty, false);
        _rail.PointerEntered += (_, _) => _rail.Background = Brush("#EF263C50");
        _rail.PointerExited += (_, _) => _rail.Background = Brush("#E3182433");
        grid.Children.Add(_rail); _beacon = new SidebarBeacon { Width = 2 }; grid.Children.Add(_beacon);
        ApplyState(false); return grid;
    }
    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCollapsedProperty) ApplyState(_attached && !ReduceMotion);
        else if (change.Property == ExpandedWidthProperty || change.Property == ReduceMotionProperty) ApplyState(false);
        else if (change.Property == IsRightProperty || change.Property == RailLabelProperty || change.Property == CollapseLabelProperty || change.Property == HasNoticeProperty) UpdateVisuals();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); _attached = true; ApplyState(false); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { _attached = false; _animation.Stop(); _beacon?.SetRunning(false); base.OnDetachedFromVisualTree(e); }
    private void ApplyState(bool animate)
    {
        _target = IsCollapsed ? RailWidth : ExpandedWidth;
        _from = double.IsFinite(Width) ? Width : _target;
        if (animate && Math.Abs(_from - _target) > .1) { _elapsed.Restart(); _animation.Start(); }
        else { _animation.Stop(); Width = _target; }
        UpdateVisuals();
    }
    private void UpdateVisuals()
    {
        if (_presenter is null || _rail is null || _collapse is null || _beacon is null) return;
        double progress = double.IsFinite(Width) ? Math.Clamp((ExpandedWidth - Width) / Math.Max(1, ExpandedWidth - RailWidth), 0, 1) : IsCollapsed ? 1 : 0;
        _presenter.Width = ExpandedWidth - 20;
        _presenter.HorizontalAlignment = IsRight ? Avalonia.Layout.HorizontalAlignment.Right : Avalonia.Layout.HorizontalAlignment.Left;
        _presenter.Opacity = 1 - progress; _presenter.IsVisible = progress < 1; _presenter.IsHitTestVisible = !IsCollapsed && !_animation.IsEnabled;
        _collapse.HorizontalAlignment = IsRight ? Avalonia.Layout.HorizontalAlignment.Left : Avalonia.Layout.HorizontalAlignment.Right;
        _collapse.Content = IsRight ? "›" : "‹"; _collapse.IsVisible = progress < 1;
        ToolTip.SetTip(_collapse, CollapseLabel); Avalonia.Automation.AutomationProperties.SetName(_collapse, CollapseLabel);
        _rail.HorizontalAlignment = IsRight ? Avalonia.Layout.HorizontalAlignment.Left : Avalonia.Layout.HorizontalAlignment.Right;
        _rail.IsVisible = progress > 0; _rail.Opacity = progress; _rail.IsHitTestVisible = IsCollapsed;
        _railText!.Text = RailLabel; _arrow!.Text = IsRight ? "‹" : "›"; _notice!.IsVisible = HasNotice;
        ToolTip.SetTip(_rail, RailLabel); Avalonia.Automation.AutomationProperties.SetName(_rail, RailLabel);
        _beacon.HorizontalAlignment = IsRight ? Avalonia.Layout.HorizontalAlignment.Left : Avalonia.Layout.HorizontalAlignment.Right;
        _beacon.IsVisible = progress > 0; _beacon.SetRunning(_attached && IsCollapsed && !ReduceMotion);
    }
}
