using System.Collections.Specialized;
using System.ComponentModel;

namespace HappyColorSplit.ViewModels;

public sealed partial class MainViewModel
{
    private bool _synchronizingPreviews, _imagesNotice, _colorsNotice, _pagesNotice;
    private PreviewViewportViewModel? _lastViewport;
    public PreviewViewportViewModel OriginalViewport { get; } = new();
    public PreviewViewportViewModel ExportViewport { get; } = new();
    public AsyncCommand PreviewOnlyCommand { get; private set; } = null!;
    public bool SideBySide
    {
        get => _settings.Workspace.SideBySide;
        set { if (value == SideBySide) return; _settings.Workspace.SideBySide = value; Changed(); SaveSettings(); }
    }
    public bool ImagesSidebarCollapsed
    {
        get => _settings.Workspace.ImagesCollapsed;
        set
        {
            if (value == ImagesSidebarCollapsed) return;
            _settings.Workspace.ImagesCollapsed = value;
            if (!value) _imagesNotice = false;
            SidebarChanged(nameof(ImagesSidebarCollapsed));
        }
    }
    public bool RightSidebarCollapsed
    {
        get => _settings.Workspace.RightCollapsed;
        set
        {
            if (value == RightSidebarCollapsed) return;
            _settings.Workspace.RightCollapsed = value;
            if (!value) { if (IsLaserMode) _pagesNotice = false; else _colorsNotice = false; }
            SidebarChanged(nameof(RightSidebarCollapsed));
        }
    }
    public bool ReduceMotion
    {
        get => _settings.Workspace.ReduceMotion;
        set { if (value == ReduceMotion) return; _settings.Workspace.ReduceMotion = value; Changed(); SaveSettings(); }
    }
    public bool SynchronizePreviews
    {
        get => _settings.Workspace.SynchronizePreviews;
        set
        {
            if (value == SynchronizePreviews) return;
            _settings.Workspace.SynchronizePreviews = value; Changed();
            if (value) SynchronizeFrom(_lastViewport ?? OriginalViewport);
            SaveSettings();
        }
    }
    public double HorizontalPreviewSplit
    { get => _settings.Workspace.HorizontalSplit; set => SaveSplit(value, true); }
    public double VerticalPreviewSplit
    { get => _settings.Workspace.VerticalSplit; set => SaveSplit(value, false); }
    public bool IsPreviewOnly => ImagesSidebarCollapsed && RightSidebarCollapsed;
    public string PreviewOnlyLabel => Text[IsPreviewOnly ? "ShowSidebars" : "PreviewOnly"];
    public string ImagesRailLabel => string.Format(Text["ExpandImages"], Images.Count);
    public string RightRailLabel => IsLaserMode ? string.Format(Text["ExpandPages"], Laser.Pages.Count)
        : string.Format(Text["ExpandColors"], ExportColorCount, Groups.Count);
    public bool ImagesHasNotice => ImagesSidebarCollapsed && _imagesNotice;
    public bool RightHasNotice => RightSidebarCollapsed && (IsLaserMode ? _pagesNotice : _colorsNotice);

    private void InitializeWorkspacePreferences()
    {
        PreviewOnlyCommand = new(() =>
        {
            bool collapse = !IsPreviewOnly;
            ImagesSidebarCollapsed = collapse; RightSidebarCollapsed = collapse;
            return Task.CompletedTask;
        }, () => true);
        OriginalViewport.PropertyChanged += PreviewViewportChanged; ExportViewport.PropertyChanged += PreviewViewportChanged;
        Images.CollectionChanged += ImagesChanged; Groups.CollectionChanged += ColorsChanged; Laser.Pages.CollectionChanged += LaserPagesChanged;
    }
    private void SidebarChanged(string property)
    {
        Changed(property); Changed(nameof(IsPreviewOnly)); RefreshWorkspaceLabels(); SaveSettings();
    }
    private void SaveSplit(double value, bool horizontal)
    {
        if (!double.IsFinite(value)) return; value = Math.Clamp(value, .15, .85);
        if (horizontal) _settings.Workspace.HorizontalSplit = value; else _settings.Workspace.VerticalSplit = value;
        SaveSettings();
    }
    private void PreviewViewportChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_synchronizingPreviews || e.PropertyName is not (nameof(PreviewViewportViewModel.Zoom) or nameof(PreviewViewportViewModel.CenterX) or nameof(PreviewViewportViewModel.CenterY))) return;
        _lastViewport = (PreviewViewportViewModel)sender!;
        if (SynchronizePreviews) SynchronizeFrom(_lastViewport);
    }
    private void SynchronizeFrom(PreviewViewportViewModel source)
    {
        _synchronizingPreviews = true;
        try { (ReferenceEquals(source, OriginalViewport) ? ExportViewport : OriginalViewport).SetViewport(source.Zoom, source.CenterX, source.CenterY); }
        finally { _synchronizingPreviews = false; }
    }
    private void ResetPreviewViewports()
    {
        _synchronizingPreviews = true;
        try { OriginalViewport.SetViewport(1, .5, .5); ExportViewport.SetViewport(1, .5, .5); _lastViewport = OriginalViewport; }
        finally { _synchronizingPreviews = false; }
    }
    private void ImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    { if (ImagesSidebarCollapsed) _imagesNotice = true; RefreshWorkspaceLabels(); }
    private void ColorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    { if (RightSidebarCollapsed || IsLaserMode) _colorsNotice = true; RefreshWorkspaceLabels(); }
    private void LaserPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    { if (RightSidebarCollapsed || !IsLaserMode) _pagesNotice = true; RefreshWorkspaceLabels(); }
    private void WorkspaceModeChanged()
    {
        if (!RightSidebarCollapsed) { if (IsLaserMode) _pagesNotice = false; else _colorsNotice = false; }
        RefreshWorkspaceLabels();
    }
    private void RefreshWorkspaceLabels()
    {
        Changed(nameof(ImagesRailLabel)); Changed(nameof(RightRailLabel)); Changed(nameof(ImagesHasNotice)); Changed(nameof(RightHasNotice)); Changed(nameof(PreviewOnlyLabel));
    }
    private void DisposeWorkspacePreferences()
    {
        OriginalViewport.PropertyChanged -= PreviewViewportChanged; ExportViewport.PropertyChanged -= PreviewViewportChanged;
        Images.CollectionChanged -= ImagesChanged; Groups.CollectionChanged -= ColorsChanged; Laser.Pages.CollectionChanged -= LaserPagesChanged;
    }
}
