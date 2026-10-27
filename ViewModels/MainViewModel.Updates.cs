using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed partial class MainViewModel
{
    private IUpdateService _updates = null!;
    private IUpdatePromptService? _updatePrompt;
    private IReleaseBrowser _releaseBrowser = null!;
    private readonly CancellationTokenSource _updateLifetime = new();
    private AvailableUpdate? _availableUpdate;
    private bool _checkingUpdate, _openingUpdate;
    private string _updateHintKey = "UpdateNotChecked";
    public bool HasAvailableUpdate => _availableUpdate is not null;
    public string VersionLabel => string.Format(Text["AppVersion"], AppVersion.Current);
    public string UpdateButtonLabel => _availableUpdate is { } update
        ? string.Format(Text["UpdateAvailableButton"], update.Tag, UpdateDate.Format(update.PublishedAt, Text.Language)) : "";
    public string UpdateCheckHint => Text[_updateHintKey];
    public AsyncCommand OpenUpdateCommand { get; private set; } = null!;

    private void InitializeUpdates(IUpdateService? updates, IUpdatePromptService? prompt, IReleaseBrowser? browser)
    {
        _updates = updates ?? new GitHubUpdateService(); _updatePrompt = prompt; _releaseBrowser = browser ?? new ReleaseBrowser();
        OpenUpdateCommand = new(OpenUpdateAsync, () => !_openingUpdate && HasAvailableUpdate && _updatePrompt is not null);
    }
    public async Task CheckForUpdatesAsync()
    {
        if (_checkingUpdate || _updateLifetime.IsCancellationRequested) return;
        _checkingUpdate = true; _updateHintKey = "UpdateChecking"; Changed(nameof(UpdateCheckHint));
        try
        {
            var result = await _updates.CheckAsync(AppVersion.Current, _updateLifetime.Token);
            if (!_updateLifetime.IsCancellationRequested) ApplyUpdateResult(result);
        }
        catch (Exception error) when (error is System.Net.Http.HttpRequestException or IOException or OperationCanceledException)
        { if (!_updateLifetime.IsCancellationRequested) ApplyUpdateResult(new(UpdateCheckState.Unavailable)); }
        finally { _checkingUpdate = false; }
    }
    private void ApplyUpdateResult(UpdateCheckResult result)
    {
        _availableUpdate = result.State == UpdateCheckState.Available ? result.Update : null;
        _updateHintKey = result.State switch
        {
            UpdateCheckState.Available => "UpdateAvailableHelp", UpdateCheckState.UpToDate => "UpdateUpToDate",
            UpdateCheckState.Untrusted => "UpdateUntrusted", _ => "UpdateUnavailable"
        };
        RefreshUpdateLabels(); OpenUpdateCommand.Refresh();
    }
    private void RefreshUpdateLabels()
    { Changed(nameof(VersionLabel)); Changed(nameof(UpdateButtonLabel)); Changed(nameof(HasAvailableUpdate)); Changed(nameof(UpdateCheckHint)); }

    private async Task OpenUpdateAsync()
    {
        if (_openingUpdate || _availableUpdate is not { } offered || _updatePrompt is null) return;
        _openingUpdate = true;
        try
        {
            if (!await _updatePrompt.ConfirmAsync(offered, Text) || _updateLifetime.IsCancellationRequested) return;
            // Revalidate the pinned identities after confirmation, just before leaving the app.
            var result = await _updates.CheckAsync(AppVersion.Current, _updateLifetime.Token);
            if (_updateLifetime.IsCancellationRequested) return;
            ApplyUpdateResult(result);
            if (result.Update != offered || result.State != UpdateCheckState.Available)
            { _updateHintKey = result.State == UpdateCheckState.Untrusted ? "UpdateUntrusted" : "UpdateChanged"; Changed(nameof(UpdateCheckHint)); return; }
            _releaseBrowser.Open(offered.Page);
        }
        catch (Exception error) when (error is System.Net.Http.HttpRequestException or IOException or OperationCanceledException or
            System.ComponentModel.Win32Exception or InvalidOperationException)
        { _updateHintKey = "UpdateOpenFailed"; Changed(nameof(UpdateCheckHint)); }
        finally { _openingUpdate = false; OpenUpdateCommand.Refresh(); }
    }
}
