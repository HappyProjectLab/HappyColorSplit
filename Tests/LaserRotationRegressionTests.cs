using HappyColorSplit.Models;
using HappyColorSplit.Services;

internal static class LaserRotationRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var settings = new LaserSettings { WidthCm = 1, HeightCm = .8m, Dpi = 254,
            MarginCm = .02m, CavityGapCm = .01m, ElementGapCm = .02m };
        var shape = new LaserPart(1, 1, "asymmetric.png", 0, 254, new(0, 0, 3, 2), [new(0, 0, 1), new(1, 0, 3)], 3, 2) { Region = 1 };
        var page = Page(settings, [new(shape, 30, 30, 0, 1)]); var original = page;
        var editor = new LaserManualPlacementService(); var expected = Filled(page);
        for (int i = 0; i < 4; i++)
        {
            var result = editor.Transform(new(page, [new(1, 1)], 0, 0, 1), CancellationToken.None);
            expected = Clockwise(expected, 32, 31);
            check(result.Success && Filled(result.Page).SetEquals(expected), "An asymmetric part rotates around its grid-aligned center without mirroring any exported pixels");
            page = result.Page;
        }
        check(page.Placements.SequenceEqual(original.Placements), "Four quarter turns return odd-sized parts to the exact original position without half-pixel drift");
        var cw = editor.Transform(new(original, [new(1, 1)], 0, 0, 1), CancellationToken.None);
        var back = editor.Transform(new(cw.Page, [new(1, 1)], 0, 0, -1), CancellationToken.None);
        check(back.Success && back.Page.Placements.SequenceEqual(original.Placements), "Clockwise and counterclockwise rotations are exact inverses");

        var other = Solid(1, 2, 4, 3);
        var group = Page(settings, [new(shape, 30, 30, 0, 1), new(other, 40, 35, 0, 2)]);
        var rotatedGroup = editor.Transform(new(group, [new(1, 1), new(2, 1)], 0, 0, 1), CancellationToken.None);
        check(rotatedGroup.Success && Filled(rotatedGroup.Page).SetEquals(Clockwise(Filled(group), 37, 34)),
            "Several selected parts from different sources rotate as one rigid group around their common center");
        check(rotatedGroup.Page.Placements.All(p => p.Turns == 1) && rotatedGroup.Page.Placements.Select(p => p.Part).SequenceEqual(group.Placements.Select(p => p.Part)),
            "Group rotation preserves source identities, physical part sizes and relative arrangement");

        var ringRuns = Enumerable.Range(0, 10).SelectMany(y => y is 0 or 9 ? new[] { new LaserRun(y, 0, 20) }
            : new[] { new LaserRun(y, 0, 1), new LaserRun(y, 19, 1) }).ToArray();
        var ring = new LaserPart(1, 3, "ring.png", 0, 254, new(0, 0, 20, 10), ringRuns, 20, 10) { Region = 1 };
        var child = Solid(2, 3, 3, 2);
        var nested = Page(settings, [new(ring, 25, 25, 0, 1), new(child, 30, 28, 0, 1, 1)]);
        var carried = editor.Transform(new(nested, [new(3, 1)], 0, 0, 1), CancellationToken.None);
        check(carried.Success && carried.Page.Placements.All(p => p.Turns == 1) && carried.Page.Placements[1].NestedInPartId == 1
            && Filled(carried.Page).SetEquals(Clockwise(Filled(nested), 35, 30)), "A rotated surrounding part carries its inserted part and preserves the cavity mask and association");

        var edge = Page(settings, [new(Solid(1, 1, 18, 4), 3, 2, 0, 1)]);
        check(editor.Transform(new(edge, [new(1, 1)], 0, 0, 1), CancellationToken.None).Failure == LaserMoveFailure.Edge,
            "Rotation is rejected atomically if it would enter the reserved edge band");
        var collision = Page(settings, [new(Solid(1, 1, 8, 2), 30, 30, 0, 1), new(Solid(1, 2, 2, 2), 33, 25, 0, 2)]);
        check(editor.Transform(new(collision, [new(1, 1)], 0, 0, 1), CancellationToken.None).Failure == LaserMoveFailure.Clearance,
            "Rotation cannot violate the clearance of an unselected part");
        check(editor.Transform(new(original, [new(1, 1)], 0, 0, 4), CancellationToken.None).Page == original,
            "A full turn is a no-op and does not create an edit");
        for (int i = 0; i < 24; i++)
        {
            var varied = Page(settings, [new(Solid(1, 1, 3 + i % 4, 2 + i % 3), 30, 30, i % 4, 1),
                new(Solid(1, 2, 5, 2 + i % 2), 40, 36, (i + 1) % 4, 2)]);
            var current = varied;
            for (int turn = 0; turn < 4; turn++)
            {
                var next = editor.Transform(new(current, [new(1, 1), new(2, 1)], 0, 0, 1), CancellationToken.None);
                check(next.Success, "Mixed original orientations and odd/even group dimensions remain valid during repeated rigid rotations");
                current = next.Page;
            }
            check(current.Placements.SequenceEqual(varied.Placements), "Every odd/even multi-part combination returns exactly after four clockwise rotations");
        }
    }

    private static HashSet<(int X, int Y)> Clockwise(IEnumerable<(int X, int Y)> points, int pivotX, int pivotY)
        => points.Select(p => (pivotX + pivotY - p.Y - 1, pivotY + p.X - pivotX)).ToHashSet();
    private static HashSet<(int X, int Y)> Filled(LaserPage page)
    {
        using var image = RasterImageCodec.Load(new LaserPageRenderer().Render(page, new(ExportArea.White, ExportBackground.Black), CancellationToken.None));
        var result = new HashSet<(int X, int Y)>();
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image[x, y].R == 255) result.Add((x, y));
        return result;
    }
    private static LaserPart Solid(int id, int source, int width, int height) => new(id, source, "source.png", 0, 254,
        new(0, 0, width, height), Enumerable.Range(0, height).Select(y => new LaserRun(y, 0, width)).ToArray(), width, height) { Region = 1 };
    private static LaserPage Page(LaserSettings settings, IReadOnlyList<LaserPlacement> parts) => new(1, 1, new("Red", "#FF0000"), settings, settings.GetSize(), parts);
}
