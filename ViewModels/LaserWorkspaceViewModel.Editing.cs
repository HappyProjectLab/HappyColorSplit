using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed partial class LaserWorkspaceViewModel
{
    private readonly List<(LaserPageViewModel Target, LaserPage Before)> _moves = [];
    public bool CanEdit => IsIdle && IsCurrent && HasPages;
    public AsyncCommand UndoMoveCommand { get; }

    public Task MoveAsync(LaserMoveRequest request)
    {
        if (!CanEdit || SelectedPage is null || !ReferenceEquals(SelectedPage.Page, request.Page)) return Task.CompletedTask;
        return _owner.RunLaserAsync(async token =>
        {
            var target = SelectedPage;
            var result = await Task.Run(() => new LaserManualPlacementService().Move(request, token), token);
            if (!result.Success)
            {
                _owner.SetLaserStatus(result.Failure switch
                {
                    LaserMoveFailure.Edge => "LaserMoveEdge", LaserMoveFailure.Complexity => "LaserMoveComplexity",
                    _ => "LaserMoveClearance"
                });
                return;
            }
            if (ReferenceEquals(result.Page, request.Page)) return;
            await ApplyEditedPageAsync(target, result.Page, token);
            _moves.Add((target, request.Page)); if (_moves.Count > 20) _moves.RemoveAt(0);
            _owner.SetLaserStatus("LaserMoveApplied");
        });
    }

    private async Task UndoMoveAsync(CancellationToken token)
    {
        if (_moves.Count == 0) return;
        var previous = _moves[^1];
        await ApplyEditedPageAsync(previous.Target, previous.Before, token);
        _moves.RemoveAt(_moves.Count - 1); _owner.SetLaserStatus("LaserMoveUndone");
    }

    private async Task ApplyEditedPageAsync(LaserPageViewModel target, LaserPage page, CancellationToken token)
    {
        var options = _owner.LaserAppearance; bool groups = ShowGroups;
        var images = await Task.Run(() =>
        {
            var renderer = new LaserPageRenderer();
            return (Thumb: renderer.Render(page, new(ExportArea.GroupColor, ExportBackground.Transparent), token, 190, 130),
                Preview: renderer.Render(page, options, token, 1200, 900, groups));
        }, token);
        Bitmap? thumbnail = null, preview = null;
        try
        {
            using var thumbStream = new MemoryStream(images.Thumb); thumbnail = new Bitmap(thumbStream);
            using var previewStream = new MemoryStream(images.Preview); preview = new Bitmap(previewStream);
            token.ThrowIfCancellationRequested();
            target.Update(page, thumbnail); thumbnail = null;
            Set(ref _selectedPage, target, nameof(SelectedPage)); Preview = preview; preview = null;
            Changed(nameof(PageInfo));
        }
        finally { thumbnail?.Dispose(); preview?.Dispose(); }
    }
}
