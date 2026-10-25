using HappyColorSplit.Models;
using HappyColorSplit.Services;

internal static class LaserNestingRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var settings = new LaserSettings { WidthCm = 3.048m, HeightCm = 3.048m, Dpi = 25,
            Materials = [new("Red", "#FF0000")] };
        var ring = Shape(1, 1, 1, 30, 30, (x, y) => x == 0 || y == 0 || x == 29 || y == 29);
        var small = Shape(2, 1, 2, 8, 8, (_, _) => true);
        var service = new LaserLayoutService();
        var nested = service.Pack([ring, small], settings, null, CancellationToken.None);
        check(nested.Pages.Count == 1 && nested.Pages[0].Placements.Count == 2,
            "A small part in a different source region uses a ring's hole instead of creating another material page");
        CheckClearance(nested, 2, check);
        var host = nested.Pages[0].Placements.Single(p => p.Part.Id == 1);
        var insert = nested.Pages[0].Placements.Single(p => p.Part.Id == 2);
        check(insert.Part.Region == 2 && host.SourceChunk == insert.SourceChunk
            && insert.X > host.X && insert.Y > host.Y,
            "Nesting retains the source file and original region identifiers");
        using (var png = RasterImageCodec.Load(new LaserPageRenderer().Render(nested.Pages[0],
            new(ExportArea.White, ExportBackground.Black), CancellationToken.None)))
        {
            check(png[insert.X, insert.Y] == new RgbaPixel(255, 255, 255)
                && png[host.X + 1, host.Y + 1] == new RgbaPixel(0, 0, 0),
                "Export includes nested material without filling unused space in the host hole");
        }
        var sameRegion = service.Pack([ring, Shape(2, 1, 1, 8, 8, (_, _) => true)], settings, null, CancellationToken.None);
        check(sameRegion.Pages.Count == 1, "Parts from the same spatial region can also share an interior cavity");

        var concave = Shape(1, 1, 1, 30, 30, (x, y) => x == 0 || y == 0);
        var open = service.Pack([concave, small], settings, null, CancellationToken.None);
        check(open.Pages.Count == 1, "An open concavity is usable in addition to a fully enclosed hole");
        CheckClearance(open, 2, check);

        var incompatible = service.Pack([ring, Shape(2, 1, 1, 20, 20, (_, _) => true)], settings, null, CancellationToken.None);
        check(incompatible.Pages.Count == 2, "A part that fits the raw hole but violates the clearance stays on a separate page");
        CheckClearance(incompatible, 2, check);
        var otherSource = service.Pack([ring, Shape(2, 2, 1, 8, 8, (_, _) => true)], settings, null, CancellationToken.None);
        check(otherSource.Pages.Count == 2, "Nesting does not mix another imported file into a source-file block");

        // A wide, shallow hole can only accept this tall insert after a quarter turn.
        var slot = Shape(1, 1, 1, 30, 30, (x, y) => !(x >= 1 && x < 29 && y >= 1 && y < 20));
        var turn = service.Pack([slot, Shape(2, 1, 1, 8, 14, (_, _) => true)], settings, null, CancellationToken.None);
        check(turn.Pages.Count == 1 && turn.Pages[0].Placements.Single(p => p.Part.Id == 2).Turns % 2 == 1,
            "A small part is rotated to fit an interior cavity without mirroring");
        CheckClearance(turn, 2, check);

        var multiple = service.Pack([ring, Shape(2, 1, 1, 4, 4, (_, _) => true), Shape(3, 1, 1, 4, 4, (_, _) => true)],
            settings, null, CancellationToken.None);
        check(multiple.Pages.Count == 1, "Several small parts can share one hole when their mutual clearance permits it");
        CheckClearance(multiple, 3, check);

        // Non-integral native/output ratios exercise the exact same sampling as the PNG renderer.
        var scaledRing = new LaserPart(1, 1, "source.png", 0, 50, ring.NativeBounds, ring.Runs, 31, 29) { Region = 1 };
        var scaledSettings = settings.Snapshot(); scaledSettings.WidthCm = 3.1496m;
        var scaled = service.Pack([scaledRing, small], scaledSettings, null, CancellationToken.None);
        CheckClearance(scaled, 2, check);
        var repeat = service.Pack([ring, small], settings, null, CancellationToken.None);
        check(nested.Pages[0].Placements.SequenceEqual(repeat.Pages[0].Placements), "Cavity placement remains deterministic");

        var rotatedSettings = settings.Snapshot(); rotatedSettings.WidthCm = 2.3368m;
        var rotatedHost = Shape(1, 1, 1, 30, 23, (x, y) => x == 0 || x == 29 || y == 0 || y == 22);
        var insideRotated = service.Pack([rotatedHost, small], rotatedSettings, null, CancellationToken.None);
        check(insideRotated.Pages.Count == 1 && insideRotated.Pages[0].Placements.Single(p => p.Part.Id == 1).Turns % 2 == 1,
            "A cavity remains usable when its host was rotated by the rectangular packer");
        CheckClearance(insideRotated, 2, check);

        var largeSettings = settings.Snapshot(); largeSettings.WidthCm = 10.4648m; largeSettings.HeightCm = 6.1m; largeSettings.Dpi = 254;
        var largeHost = Shape(1, 1, 1, 1046, 610, (x, y) => x < 5 || x >= 1041 || y < 5 || y >= 605);
        var coarse = service.Pack([largeHost, Shape(2, 1, 1, 104, 61, (_, _) => true)], largeSettings, null, CancellationToken.None);
        check(coarse.Pages.Count == 1, "Large masks use a conservative search grid without losing the opportunity to nest small parts");
        CheckClearance(coarse, 2, check);
    }

    private static LaserPart Shape(int id, int source, int region, int width, int height, Func<int, int, bool> filled)
    {
        var runs = new List<LaserRun>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width;)
        {
            if (!filled(x, y)) { x++; continue; }
            int start = x; while (x < width && filled(x, y)) x++;
            runs.Add(new(y, start, x - start));
        }
        return new(id, source, "source.png", 0, 25, new(0, 0, width, height), runs, width, height) { Region = region };
    }

    private static void CheckClearance(LaserLayout layout, int count, Action<bool, string> check)
    {
        check(layout.Pages.Sum(p => p.Placements.Count) == count
            && layout.Pages.SelectMany(p => p.Placements).Select(p => p.Part.Id).Distinct().Count() == count,
            "Nesting neither drops nor duplicates parts");
        foreach (var page in layout.Pages)
        {
            int gap = Math.Min(page.Settings.CavityGapPixels, page.Settings.ElementGapPixels);
            var masks = new List<List<LaserRun>>();
            foreach (var placement in page.Placements)
            {
                check(placement.X >= 0 && placement.Y >= 0 && placement.X + placement.Width <= page.Size.Width
                    && placement.Y + placement.Height <= page.Size.Height, "Nested parts stay within the sheet");
                using var image = RasterImageCodec.Load(new LaserPageRenderer().Render(page with { Placements = [placement] },
                    new(ExportArea.White, ExportBackground.Black), CancellationToken.None));
                var filled = new List<LaserRun>();
                for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width;)
                {
                    if (image[x, y].R != 255) { x++; continue; }
                    int start = x; while (x < image.Width && image[x, y].R == 255) x++;
                    filled.Add(new(y, start, x - start));
                }
                masks.Add(filled);
            }
            long distance = long.MaxValue;
            for (int i = 0; i < masks.Count; i++) for (int j = 0; j < i; j++)
            foreach (var a in masks[i]) foreach (var b in masks[j])
            {
                // Pixels occupy unit squares, so centre distances alone would overstate the clearance.
                int dx = Math.Max(0, Math.Max(a.X - b.X - b.Length, b.X - a.X - a.Length));
                int dy = Math.Max(0, Math.Abs(a.Y - b.Y) - 1);
                distance = Math.Min(distance, (long)dx * dx + (long)dy * dy);
            }
            check(distance >= (long)gap * gap, "Actual exported filled areas retain at least 0.5 cm separation in every direction");
        }
    }
}
