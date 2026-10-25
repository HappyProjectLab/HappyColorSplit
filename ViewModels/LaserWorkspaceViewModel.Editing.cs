using Avalonia.Media.Imaging;
using HappyColorSplit.Models;
using HappyColorSplit.Services;

namespace HappyColorSplit.ViewModels;

public sealed partial class LaserWorkspaceViewModel
{
    private readonly List<(LaserPageViewModel Target, LaserPage Before)> _moves = [];
    private LaserSelection? _selection;
    public bool CanEdit => IsIdle && IsCurrent && HasPages;
    public LaserSelection? Selection
    {
        get => _selection;
        set
        {
            if (value is null) { SetSelection(null); return; }
            if (!CanEdit || !ReferenceEquals(value.Page, SelectedPage?.Page)) return;
            var parts = new LaserManualPlacementService().SelectMany(value.Page, value.Parts);
            SetSelection(parts.Count == 0 ? null : new(value.Page, Keys(parts), value.IsMultiSelection));
        }
    }
    public string SelectionInfo => string.Format(Text["LaserSelectionInfo"], _selection?.Parts.Count ?? 0);
    private static LaserPartKey[] Keys(IReadOnlyList<LaserPlacement> parts) => parts.Select(p => new LaserPartKey(p.Part.SourceIndex, p.Part.Id)).ToArray();
    private void SetSelection(LaserSelection? value)
    {
        if (ReferenceEquals(_selection?.Page, value?.Page) && (_selection is null
            || (_selection.IsMultiSelection == value!.IsMultiSelection && _selection.Parts.SequenceEqual(value.Parts)))) return;
        Set(ref _selection, value, nameof(Selection)); Changed(nameof(SelectionInfo));
        RotateClockwiseCommand.Refresh(); RotateCounterclockwiseCommand.Refresh();
    }
    public AsyncCommand UndoMoveCommand { get; }
    public AsyncCommand RotateClockwiseCommand { get; }
    public AsyncCommand RotateCounterclockwiseCommand { get; }
    private bool CanRotate => CanEdit && _selection is { Parts.Count: > 0 } && ReferenceEquals(_selection.Page, SelectedPage?.Page);
    public Task RotateSelectionAsync(int turns)
        => !CanRotate ? Task.CompletedTask : MoveSelectionAsync(new(_selection!.Page, _selection.Parts, 0, 0, turns));

    public Task MoveAsync(LaserMoveRequest request)
        => MoveSelectionAsync(new(request.Page, Keys(new LaserManualPlacementService().Select(request.Page, request.SourceIndex, request.PartId, request.WholeRegion)), request.DeltaX, request.DeltaY));

    public Task SelectPolygonAsync(LaserLassoRequest request)
    {
        if (!CanEdit || !ReferenceEquals(SelectedPage?.Page, request.Page)) return Task.CompletedTask;
        return _owner.RunLaserAsync(async token =>
        {
            var parts = await Task.Run(() => new LaserManualPlacementService().SelectPolygon(request, token), token);
            token.ThrowIfCancellationRequested();
            SetSelection(parts.Count == 0 ? null : new(request.Page, Keys(parts), true));
            _owner.SetLaserStatus("LaserSelectionInfo", parts.Count);
        });
    }

    public Task MoveSelectionAsync(LaserTransformRequest request)
    {
        if (!CanEdit || SelectedPage is null || !ReferenceEquals(SelectedPage.Page, request.Page)) return Task.CompletedTask;
        return _owner.RunLaserAsync(async token =>
        {
            var target = SelectedPage;
            var result = await Task.Run(() => new LaserManualPlacementService().Transform(request, token), token);
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
            _owner.SetLaserStatus(request.QuarterTurns % 4 == 0 ? "LaserMoveApplied" : "LaserRotationApplied");
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
        var selected = ReferenceEquals(_selection?.Page, target.Page) ? _selection.Parts : null;
        bool multi = _selection?.IsMultiSelection == true;
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
            SetSelection(selected is null ? null : new(page, selected, multi));
            Changed(nameof(PageInfo));
        }
        finally { thumbnail?.Dispose(); preview?.Dispose(); }
    }
}
