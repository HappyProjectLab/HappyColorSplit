using HappyColorSplit.Models;
using HappyColorSplit.Services;

internal static class LaserSelectionRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var settings = new LaserSettings { WidthCm = 1, HeightCm = .8m, Dpi = 254, ElementGapCm = 0, CavityGapCm = 0 };
        var editor = new LaserManualPlacementService();
        var a = Solid(1, 1, 10, 10); var b = Solid(2, 1, 10, 10); var c = Solid(1, 2, 10, 10); var d = Solid(2, 2, 10, 10);
        var page = new LaserPage(1, 1, new("Red", "#FF0000"), settings, settings.GetSize(),
            [new(a, 10, 10, 0, 1), new(b, 30, 10, 0, 1), new(c, 10, 40, 0, 2), new(d, 30, 40, 0, 2)]);
        var rectangle = editor.SelectPolygon(new(page, [new(5, 5), new(45, 5), new(45, 25), new(5, 25)]), CancellationToken.None);
        check(rectangle.Select(p => p.Part).ToHashSet().SetEquals([a, b]), "A closed lasso selects fully surrounded parts without selecting distant regions");
        var concave = editor.SelectPolygon(new(page, [new(5, 5), new(45, 5), new(45, 25), new(25, 25), new(25, 55), new(5, 55)]), CancellationToken.None);
        check(concave.Select(p => p.Part).ToHashSet().SetEquals([a, b, c]), "A nonrectangular lasso can select multiple source files while excluding a part in its notch");
        var move = editor.Transform(new(page, [new(1, 1), new(2, 1)], 5, 0), CancellationToken.None);
        check(move.Success && move.Page.Placements[0].X == 15 && move.Page.Placements[2].X == 15
            && move.Page.Placements[1] == page.Placements[1] && move.Page.Placements[3] == page.Placements[3],
            "A multi-part move distinguishes identical part numbers belonging to different source files");
        check(editor.Transform(new(page, [new(1, 1), new(9, 9)], 5, 0), CancellationToken.None).Failure == LaserMoveFailure.Selection,
            "A stale multi-selection is rejected atomically rather than moving its remaining valid parts");

        var block = Solid(3, 1, 20, 20);
        var blockPage = page with { Placements = [new(block, 60, 10, 0, 1)] };
        var cut = editor.SelectPolygon(new(blockPage, [new(55, 5), new(68, 5), new(68, 25), new(72, 25), new(72, 5), new(85, 5), new(85, 35), new(55, 35)]), CancellationToken.None);
        check(cut.Count == 0, "A lasso notch through filled material excludes the part even when all bounding-box corners are inside");
        var diamond = new LaserPart(3, 1, "diamond.png", 0, 254, new(0, 0, 5, 5),
            [new(0, 2, 1), new(1, 1, 3), new(2, 0, 5), new(3, 1, 3), new(4, 2, 1)], 5, 5) { Region = 1 };
        var diamondPage = page with { Placements = [new(diamond, 60, 50, 0, 1)] };
        var tight = editor.SelectPolygon(new(diamondPage, [new(62.5, 49.25), new(65.75, 52.5), new(62.5, 55.75), new(59.25, 52.5)]), CancellationToken.None);
        check(tight.Count == 1 && tight[0].Part == diamond, "A tight freehand outline follows actual material instead of requiring empty bounding-box corners");
        var runs = Enumerable.Range(0, 20).SelectMany(y => y is 0 or 19 ? new[] { new LaserRun(y, 0, 20) }
            : new[] { new LaserRun(y, 0, 1), new LaserRun(y, 19, 1) }).ToArray();
        var ring = new LaserPart(1, 3, "ring.png", 0, 254, new(0, 0, 20, 20), runs, 20, 20) { Region = 1 };
        var child = Solid(2, 3, 3, 3);
        var nested = page with { Placements = [new(ring, 60, 40, 0, 1), new(child, 68, 48, 0, 1, 1)] };
        var hole = editor.SelectPolygon(new(nested, [new(66, 46), new(74, 46), new(74, 54), new(66, 54)]), CancellationToken.None);
        check(hole.Count == 1 && hole[0].Part == child, "An outline inside a hole selects its inserted part without selecting the surrounding ring");
        check(editor.SelectMany(nested, [new(3, 1)]).Count == 2, "Multi-selection preserves the rule that surrounding parts carry nested descendants");
        check(editor.SelectPolygon(new(page, [new(double.NaN, 0), new(5, 5), new(9, 9)]), CancellationToken.None).Count == 0,
            "Invalid lasso coordinates cannot produce a selection");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        try { editor.SelectPolygon(new(page, [new(0, 0), new(100, 0), new(100, 80)]), cancellation.Token); check(false, "Cancelled lasso selection must stop"); }
        catch (OperationCanceledException) { check(true, "Lasso mask testing responds to cancellation"); }
    }
    private static LaserPart Solid(int id, int source, int width, int height) => new(id, source, "source.png", 0, 254,
        new(0, 0, width, height), Enumerable.Range(0, height).Select(y => new LaserRun(y, 0, width)).ToArray(), width, height) { Region = id };
}
