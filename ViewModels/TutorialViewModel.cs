using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed class TutorialViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    private readonly AppSettings _settings;
    private readonly Action _save;
    private bool _visible;
    private int _step = -1;
    private (bool Laser, bool ImagesCollapsed, bool ColorsCollapsed)? _originalLayout;
    private string _hintKey = "";
    private static readonly (string Target, string Key)[] Steps =
    [
        ("OpenImagesButton", "Load"), ("LoadedImagesList", "Images"),
        ("TolerancePanel", "Tolerance"), ("AlphaPanel", "Opacity"), ("AnalyzeColorsButton", "Analyze"),
        ("OriginalPreviewPanel", "Original"), ("ColorsList", "Colors"),
        ("ExportOptionsPanel", "Appearance"), ("ImageSizeButton", "Size"), ("ExportPreviewPanel", "Preview"),
        ("ExportActionsPanel", "Export"), ("ColorPreviewToolbar", "Workspace"),
        ("WorkspaceSelector", "Laser"), ("AppVersionLabel", "Version")
    ];
    public TutorialViewModel(MainViewModel owner, AppSettings settings, Action save)
    {
        _owner = owner; _settings = settings; _save = save;
        StartCommand = new(() => { Begin(); return Task.CompletedTask; }, () => _owner.IsIdle);
        NextCommand = new(() => { Next(); return Task.CompletedTask; }, () => IsRunning && _owner.IsIdle);
        BackCommand = new(() => { Move(-1); return Task.CompletedTask; }, () => IsRunning && _step > 0 && _owner.IsIdle);
        SkipCommand = new(() => { Stop(); return Task.CompletedTask; }, () => IsVisible && _owner.IsIdle);
        DisableCommand = new(() => { _settings.TutorialTipsDisabled = true; Stop(); _save(); return Task.CompletedTask; }, () => IsVisible && _owner.IsIdle);
    }
    public Texts Text => _owner.Text;
    public bool IsVisible => _visible;
    public bool IsWelcome => IsVisible && _step < 0;
    public bool IsRunning => IsVisible && _step >= 0;
    public int StepIndex => _step;
    public int StepCount => Steps.Length;
    public string TargetName => IsRunning ? Steps[_step].Target : "";
    public string ProgressLabel => IsRunning ? string.Format(Text["GuideProgress"], _step + 1, StepCount) : Text["GuideWelcomeLabel"];
    public string Heading => Text[IsRunning ? "Guide" + Steps[_step].Key + "Title" : "GuideWelcomeTitle"];
    public string Description => Text[IsRunning ? "Guide" + Steps[_step].Key + "Body" : "GuideWelcomeBody"];
    public string NextLabel => Text[_step == StepCount - 1 ? "GuideFinish" : "GuideNext"];
    public string Hint => string.IsNullOrEmpty(_hintKey) ? "" : Text[_hintKey];
    public bool HasHint => !string.IsNullOrEmpty(_hintKey);
    public AsyncCommand StartCommand { get; }
    public AsyncCommand NextCommand { get; }
    public AsyncCommand BackCommand { get; }
    public AsyncCommand SkipCommand { get; }
    public AsyncCommand DisableCommand { get; }

    public void OfferOnStartup()
    {
        if (IsVisible || _settings.TutorialCompleted || _settings.TutorialTipsDisabled) return;
        _visible = true; _step = -1; Refresh();
    }
    public void Begin()
    {
        if (!_owner.IsIdle || IsRunning) return;
        _originalLayout = (_owner.IsLaserMode, _owner.ImagesSidebarCollapsed, _owner.RightSidebarCollapsed);
        _owner.IsLaserMode = false;
        _visible = true; _step = 0; PrepareStep(); Refresh();
    }
    private void Next()
    {
        if (!IsRunning || !_owner.IsIdle) return;
        if (_step == StepCount - 1) { _settings.TutorialCompleted = true; Stop(); _save(); }
        else Move(1);
    }
    private void Move(int delta)
    {
        if (!IsRunning || !_owner.IsIdle) return;
        _step = Math.Clamp(_step + delta, 0, StepCount - 1); PrepareStep(); Refresh();
    }
    private void PrepareStep()
    {
        SetHint("");
        if (TargetName is "LoadedImagesList" or "ImageSizeButton") _owner.ImagesSidebarCollapsed = false;
        if (TargetName == "ColorsList") _owner.RightSidebarCollapsed = false;
    }
    public void Stop()
    {
        _visible = false; _step = -1; SetHint("");
        if (_originalLayout is { } layout)
        {
            _owner.IsLaserMode = layout.Laser;
            _owner.ImagesSidebarCollapsed = layout.ImagesCollapsed;
            _owner.RightSidebarCollapsed = layout.ColorsCollapsed;
            _originalLayout = null;
        }
        Refresh();
    }
    public void SetHint(string key)
    { if (_hintKey == key) return; _hintKey = key; Changed(nameof(Hint)); Changed(nameof(HasHint)); }
    public void Refresh()
    {
        foreach (string property in new[] { nameof(Text), nameof(IsVisible), nameof(IsWelcome), nameof(IsRunning), nameof(StepIndex), nameof(TargetName), nameof(ProgressLabel), nameof(Heading), nameof(Description), nameof(NextLabel), nameof(Hint), nameof(HasHint) }) Changed(property);
        StartCommand.Refresh(); NextCommand.Refresh(); BackCommand.Refresh(); SkipCommand.Refresh(); DisableCommand.Refresh();
    }
}
