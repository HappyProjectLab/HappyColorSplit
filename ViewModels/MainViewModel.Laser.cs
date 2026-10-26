using HappyColorSplit.Models;

namespace HappyColorSplit.ViewModels;

public sealed partial class MainViewModel
{
    private int _workspaceIndex;
    public LaserWorkspaceViewModel Laser { get; }
    public int WorkspaceIndex
    {
        get => _workspaceIndex;
        set
        {
            if (IsBusy || value is not (0 or 1) || !Set(ref _workspaceIndex, value)) return;
            Changed(nameof(IsLaserMode)); WorkspaceModeChanged(); if (IsLaserMode) Laser.RefreshAppearance();
        }
    }
    public bool IsLaserMode { get => WorkspaceIndex == 1; set => WorkspaceIndex = value ? 1 : 0; }
    internal Task RunLaserAsync(Func<CancellationToken, Task> work) => RunAsync(work);
    internal Task<string?> ChooseLaserOutputAsync() => _dialogs.SelectOutputAsync(Text);
    internal void RememberLaserFolder(string path) => RememberExportFolder(path);
    internal void SaveLaserSettings() => SaveSettings();
    internal void SetLaserStatus(string key, params object[] args) => StatusMessage(key, args);
    internal void SetLaserProgress(int value) => Progress = value;
    internal ExportOptions LaserAppearance => CurrentExportOptions;
    internal IReadOnlyList<LaserSource> GetLaserSources() => Images.Select((image, index) => (image, index))
        .Where(pair => pair.image.ColorsDetected && pair.image.ExportColors is { Count: > 0 })
        .Select(pair => new LaserSource(pair.index + 1, pair.image.FilePath, pair.image.Scale, pair.image.ExportColors!.ToHashSet(), _settings.SmoothContours)).ToArray();
}
