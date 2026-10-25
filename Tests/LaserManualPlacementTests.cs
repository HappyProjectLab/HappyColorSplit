using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;
using System.Text.Json;

internal static class LaserManualPlacementTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        var settings = new LaserSettings { WidthCm = 3.048m, HeightCm = 3.048m, Dpi = 25,
            MarginCm = .2032m, CavityGapCm = .1016m, ElementGapCm = .2032m };
        var editor = new LaserManualPlacementService();
        var a = Solid(1, 1, 1, 4, 4); var b = Solid(2, 1, 1, 4, 4);
        var page = Page(settings, [new(a, 2, 2, 0, 1), new(b, 12, 2, 0, 1)]);
        check(editor.Move(new(page, 1, 1, false, -1, 0), CancellationToken.None).Failure == LaserMoveFailure.Edge,
            "Manual movement cannot enter the configured grey margin");
        check(editor.Move(new(page, 1, 1, false, int.MaxValue, 0), CancellationToken.None).Failure == LaserMoveFailure.Edge,
            "Extreme pointer offsets cannot overflow into valid page coordinates");
        check(editor.Move(new(page, 1, 1, false, 5, 0), CancellationToken.None).Failure == LaserMoveFailure.Clearance,
            "A manual move that falls below the mutual spacing is rejected");
        var allowed = editor.Move(new(page, 1, 1, false, 4, 0), CancellationToken.None);
        check(allowed.Success && allowed.Page.Placements[0].X == 6 && page.Placements[0].X == 2,
            "Exact minimum clearance is allowed and the original page stays immutable");
        var region = editor.Move(new(page, 1, 1, true, 0, 10), CancellationToken.None);
        check(region.Success && region.Page.Placements.All(p => p.Y == 12) && region.Page.Placements[1].X == 12,
            "A source region translates as one rigid group without changing relative geometry");

        var runs = Enumerable.Range(0, 20).SelectMany(y => y is 0 or 19 ? new[] { new LaserRun(y, 0, 20) }
            : new[] { new LaserRun(y, 0, 1), new LaserRun(y, 19, 1) }).ToArray();
        var ring = new LaserPart(1, 1, "source.png", 0, 25, new(0, 0, 20, 20), runs, 20, 20) { Region = 1 };
        var small = Solid(2, 1, 2, 3, 3);
        var open = Page(settings, [new(ring, 2, 2, 0, 1), new(small, 24, 2, 0, 2)]);
        var nested = editor.Move(new(open, 1, 2, false, -20, 2), CancellationToken.None);
        check(nested.Success && nested.Page.Placements[1].NestedInPartId == 1,
            "Manual movement into a real hole assigns the surrounding part and uses cavity clearance");
        check(editor.HitTest(nested.Page, 4, 4)?.Part == small && editor.HitTest(nested.Page, 10, 10) is null,
            "Hit testing selects actual filled material and does not select the empty interior of a surrounding bounding box");
        check(editor.Select(nested.Page, 1, 1, false).Count == 2, "Moving a surrounding part carries its nested children across region boundaries");
        var carried = editor.Move(new(nested.Page, 1, 1, false, 0, 2), CancellationToken.None);
        check(carried.Success && carried.Page.Placements[0].Y == 4 && carried.Page.Placements[1].Y == 6,
            "Nested parts keep their relative positions when the surrounding part is moved");
        var detached = editor.Move(new(nested.Page, 1, 2, false, 20, -2), CancellationToken.None);
        check(detached.Success && detached.Page.Placements[1].NestedInPartId is null,
            "A part dragged back out of a cavity loses the stale surrounding-part relationship");
        check(editor.Move(new(open, 1, 2, false, -21, 1), CancellationToken.None).Failure == LaserMoveFailure.Clearance,
            "A manual cavity placement below the configured surrounding clearance is rejected");
        var zero = settings.Snapshot(); zero.CavityGapCm = zero.ElementGapCm = 0;
        var touch = open with { Settings = zero };
        check(editor.Move(new(touch, 1, 2, false, -21, 1), CancellationToken.None).Success,
            "Zero cavity clearance allows touching without overlap");
        check(editor.Move(new(touch, 1, 2, false, -22, 0), CancellationToken.None).Failure == LaserMoveFailure.Clearance,
            "Even a zero clearance never permits filled pixels to overlap");
        var otherSource = new LaserPart(1, 2, "other.png", 0, 25, ring.NativeBounds, ring.Runs, 20, 20) { Region = 1 };
        var separate = open with { Placements = [new(otherSource, 2, 2, 0, 1), new(small, 24, 2, 0, 2)] };
        check(editor.Move(new(separate, 1, 2, false, -20, 2), CancellationToken.None).Failure == LaserMoveFailure.Clearance,
            "A similarly numbered part from another file cannot accidentally become a cavity parent");
        check(editor.Move(new(page, 9, 9, false, 1, 1), CancellationToken.None).Failure == LaserMoveFailure.Selection,
            "An unknown selection cannot modify a page");
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            try { editor.Move(new(page, 1, 1, false, 0, 10), cancelled.Token); check(false, "Cancelled manual movement must not return a result"); }
            catch (OperationCanceledException) { check(true, "Cancelled manual movement leaves the input page intact"); }
        }
        CheckHitPixels(settings, editor, check);
        CheckMovedPixels(settings, editor, check);
        CheckMouseWorkflow(root, check, pump);
    }

    private static void CheckHitPixels(LaserSettings settings, LaserManualPlacementService editor, Action<bool, string> check)
    {
        var shape = new LaserPart(1, 1, "asymmetric.png", 0, 25, new(0, 0, 3, 2), [new(0, 0, 1), new(1, 0, 3)], 5, 4) { Region = 1 };
        for (int turn = 0; turn < 4; turn++)
        {
            var page = Page(settings, [new(shape, 5, 5, turn, 1)]);
            using var image = RasterImageCodec.Load(new LaserPageRenderer().Render(page, new(ExportArea.White, ExportBackground.Black), CancellationToken.None));
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                check((editor.HitTest(page, x, y) is not null) == (image[x, y].R == 255),
                    "Pointer hit testing agrees with actual exported pixels after scaling and every quarter turn");
        }
    }

    private static void CheckMouseWorkflow(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string folder = Path.Combine(root, "laser-mouse"); Directory.CreateDirectory(folder);
        string input = Path.Combine(folder, "three-parts.png");
        using (var image = new RasterImage(48, 20))
        {
            foreach (int left in new[] { 0, 11, 32 })
                for (int y = 0; y < 8; y++) for (int x = left; x < left + 8; x++) image[x, y] = new(255, 0, 0);
            File.WriteAllBytes(input, RasterImageCodec.EncodePng(image));
        }
        using var vm = new MainViewModel(new Dialogs(folder), new SettingsService(Path.Combine(folder, "settings")));
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            var loading = vm.AddImagesAsync([input]); pump(() => loading.IsCompleted);
            vm.ScaleDpi = 25; vm.Tolerance = 0; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy);
            var laser = vm.Laser; laser.Dpi = 25; laser.WidthCm = 8.128m; laser.HeightCm = 6.096m;
            laser.MarginCm = .1016m; laser.ElementGapCm = .2032m; laser.CavityGapCm = .1016m;
            laser.MaterialHex = "#FF0000"; laser.AddMaterialCommand.Execute(null); vm.WorkspaceIndex = 1;
            laser.ArrangeCommand.Execute(null); pump(() => !vm.IsBusy); Dispatcher.UIThread.RunJobs();
            var canvas = window.GetVisualDescendants().OfType<LaserWorkspaceCanvas>().Single();
            var original = laser.SelectedPage!.Page;
            check(laser.CanEdit && canvas.CanEdit && original.Placements.Count == 3, "A current arrangement enables the real mouse-editing surface");
            var part = original.Placements.First(p => p.Part.Id == 1);
            var oldThumbnail = laser.SelectedPage.Thumbnail; var oldPreview = laser.Preview;
            Drag(window, canvas, part, 30, 0); pump(() => !vm.IsBusy);
            var moved = laser.SelectedPage.Page;
            check(!ReferenceEquals(moved, original) && moved.Placements.Single(p => p.Part.Id == 1).X == part.X + 30
                && moved.Placements.Single(p => p.Part.Id == 2) == original.Placements.Single(p => p.Part.Id == 2),
                $"Real mouse dragging moves the chosen part without moving another part of the same region; status={vm.Status}, bounds={canvas.Bounds}, before={part.X},{part.Y}, after={string.Join(';', moved.Placements.Select(p => $"{p.Part.Id}:{p.X},{p.Y}"))}");
            check(laser.IsCurrent && laser.UndoMoveCommand.CanExecute(null), "A valid manual move stays exportable and enables undo");
            check(!ReferenceEquals(oldThumbnail, laser.SelectedPage.Thumbnail) && !ReferenceEquals(oldPreview, laser.Preview),
                "A valid mouse edit refreshes both the selected-page preview and its page-list thumbnail");
            laser.ExportAllCommand.Execute(null); pump(() => !vm.IsBusy);
            var pngPath = Directory.GetFiles(vm.LastExportFolder, "*.png").Single();
            using (var png = RasterImageCodec.Load(File.ReadAllBytes(pngPath)))
                check(png[part.X + 31, part.Y + 1].R == 255 && png[part.X + 1, part.Y + 1].R == 0,
                    "Export uses the manually moved geometry rather than the original automatic arrangement");
            using (var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(vm.LastExportFolder, "Laser_Layout.json"))))
            {
                var element = manifest.RootElement[0].GetProperty("Elements").EnumerateArray().Single(p => p.GetProperty("Part").GetInt32() == 1);
                check(element.GetProperty("XCm").GetDecimal() == (part.X + 30) * 2.54m / original.Settings.Dpi,
                    "The exported layout manifest records the manually edited physical position");
            }
            var undo = window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, vm.Text["LaserUndoMove"]));
            check(undo.Command == laser.UndoMoveCommand, "The visible undo button uses the editing command");
            undo.Command!.Execute(null); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.SelectedPage.Page, original) && !laser.UndoMoveCommand.CanExecute(null), "Undo restores the complete previous page and clears the last move");
            Dispatcher.UIThread.RunJobs();
            Drag(window, canvas, part, -part.X - 1, 0); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.SelectedPage.Page, original) && vm.Status == vm.Text["LaserMoveEdge"]
                && !laser.UndoMoveCommand.CanExecute(null), "An invalid mouse drop reports the blocked margin and keeps page and history intact");
            Drag(window, canvas, part, 0, 10); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.SelectedPage.Page, original) && vm.Status == vm.Text["LaserMoveClearance"],
                "A mouse drop onto another filled part is rejected without changing the arrangement");
            Drag(window, canvas, part, 30, 0, escape: true); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.SelectedPage.Page, original), "Escape cancels a captured mouse drag without changing the page");
            Drag(window, canvas, part, 30, 0, rightButton: true); pump(() => !vm.IsBusy);
            var group = laser.SelectedPage.Page;
            check(group.Placements.Where(p => p.Part.Region == part.Part.Region).All(p => p.X == original.Placements.Single(q => q.Part == p.Part).X + 30)
                && group.Placements.Where(p => p.Part.Region != part.Part.Region).All(p => p == original.Placements.Single(q => q.Part == p.Part)),
                "The right mouse button moves the source region on this page and leaves other regions in place");
            var selectedRegion = laser.Selection;
            Lasso(window, canvas, [new(.1, .1), new(70, .1), new(70, 50), new(.1, 50), new(.1, .1)], escape: true); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.Selection, selectedRegion) && ReferenceEquals(laser.SelectedPage.Page, group),
                "Escape during a lasso preserves the previous selection and geometry");
            Lasso(window, canvas, [new(.1, .1), new(70, .1), new(70, 50), new(.1, 50), new(.1, .1)]); pump(() => !vm.IsBusy);
            check(laser.Selection?.Parts.Count == 3 && ReferenceEquals(canvas.Selection, laser.Selection) && ReferenceEquals(laser.SelectedPage.Page, group),
                "A genuine freehand mouse outline selects multiple parts across original regions without changing geometry");
            var selectedPart = group.Placements.First(p => p.Part.Id == 1);
            Drag(window, canvas, selectedPart, 10, 0); pump(() => !vm.IsBusy);
            check(laser.SelectedPage.Page.Placements.All(p => p.X == group.Placements.Single(q => q.Part == p.Part).X + 10)
                && laser.Selection?.Parts.Count == 3 && ReferenceEquals(laser.Selection.Page, laser.SelectedPage.Page),
                "Dragging an already selected part moves the entire persistent multi-selection and keeps it selected");
            laser.UndoMoveCommand.Execute(null); pump(() => !vm.IsBusy);
            check(ReferenceEquals(laser.SelectedPage.Page, group), "Undo restores the page after a multi-selection move");
            window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Dispatcher.UIThread.RunJobs();
            check(laser.Selection is null && canvas.Selection is null, "Escape when idle clears the persistent selection");
            var stale = laser.MoveAsync(new(original, part.Part.SourceIndex, part.Part.Id, false, 0, 5)); pump(() => stale.IsCompleted);
            check(ReferenceEquals(laser.SelectedPage.Page, group), "A delayed mouse request for an obsolete page cannot overwrite a newer edit");
            laser.MarginCm = .2m; Dispatcher.UIThread.RunJobs();
            check(!canvas.CanEdit && !laser.UndoMoveCommand.CanExecute(null), "Geometry changes disable manual editing and discard obsolete undo history until rearranging");
        }
        finally { window.Close(); }
    }

    private static void CheckMovedPixels(LaserSettings settings, LaserManualPlacementService editor, Action<bool, string> check)
    {
        var shape = new LaserPart(1, 1, "scaled.png", 0, 25, new(0, 0, 3, 2), [new(0, 0, 1), new(1, 0, 3)], 5, 4) { Region = 1 };
        var obstacle = new LaserPlacement(Solid(1, 2, 1, 3, 3), 12, 8, 0, 2);
        for (int turn = 0; turn < 4; turn++)
        foreach (var (dx, dy) in new[] { (0, 1), (4, 0), (6, 0), (4, 4), (0, 5), (-5, 0), (20, 0) })
        {
            var placement = new LaserPlacement(shape, 5, 5, turn, 1);
            var page = Page(settings, [placement, obstacle]); var target = placement with { X = 5 + dx, Y = 5 + dy };
            int margin = settings.MarginPixels, gap = settings.ElementGapPixels;
            bool expected = target.X >= margin && target.Y >= margin && target.X + target.Width <= page.Size.Width - margin
                && target.Y + target.Height <= page.Size.Height - margin;
            if (expected)
            {
                var a = Filled(page with { Placements = [target] }); var b = Filled(page with { Placements = [obstacle] });
                foreach (var p in a) foreach (var q in b)
                {
                    int x = Math.Max(0, Math.Abs(p.X - q.X) - 1), y = Math.Max(0, Math.Abs(p.Y - q.Y) - 1);
                    if (p == q || x * x + y * y < gap * gap) expected = false;
                }
            }
            check(editor.Move(new(page, 1, 1, false, dx, dy), CancellationToken.None).Success == expected,
                "Manual collision validation matches independent PNG-pixel distances for scaled masks in every rotation");
        }
        static List<(int X, int Y)> Filled(LaserPage page)
        {
            using var image = RasterImageCodec.Load(new LaserPageRenderer().Render(page, new(ExportArea.White, ExportBackground.Black), CancellationToken.None));
            var result = new List<(int X, int Y)>();
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image[x, y].R == 255) result.Add((x, y));
            return result;
        }
    }

    private static void Drag(Window window, LaserWorkspaceCanvas canvas, LaserPlacement part, int dx, int dy, bool escape = false, bool rightButton = false)
    {
        Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame(); Dispatcher.UIThread.RunJobs(); var page = canvas.Page!;
        var sheet = LaserWorkspaceCanvas.SheetBounds(canvas.Bounds.Size, page.Size);
        var start = canvas.TranslatePoint(new Point(sheet.X + (part.X + part.Width / 2.0) * sheet.Width / page.Size.Width,
            sheet.Y + (part.Y + part.Height / 2.0) * sheet.Height / page.Size.Height), window)!.Value;
        var end = start + new Vector(dx * sheet.Width / page.Size.Width, dy * sheet.Height / page.Size.Height);
        var button = rightButton ? MouseButton.Right : MouseButton.Left;
        window.MouseDown(start, button); window.MouseMove(end);
        if (escape) window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.MouseUp(end, button); Dispatcher.UIThread.RunJobs();
    }

    private static void Lasso(Window window, LaserWorkspaceCanvas canvas, IReadOnlyList<LaserPoint> points, bool escape = false)
    {
        Dispatcher.UIThread.RunJobs(); using var frame = window.CaptureRenderedFrame();
        var page = canvas.Page!; var sheet = LaserWorkspaceCanvas.SheetBounds(canvas.Bounds.Size, page.Size);
        Point Map(LaserPoint p) => canvas.TranslatePoint(new Point(sheet.X + p.X * sheet.Width / page.Size.Width,
            sheet.Y + p.Y * sheet.Height / page.Size.Height), window)!.Value;
        window.MouseDown(Map(points[0]), MouseButton.Left);
        foreach (var p in points.Skip(1)) window.MouseMove(Map(p));
        if (escape) window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.MouseUp(Map(points[^1]), MouseButton.Left); Dispatcher.UIThread.RunJobs();
    }

    private static LaserPart Solid(int id, int source, int region, int w, int h) => new(id, source, "source.png", 0, 25,
        new(0, 0, w, h), Enumerable.Range(0, h).Select(y => new LaserRun(y, 0, w)).ToArray(), w, h) { Region = region };
    private static LaserPage Page(LaserSettings settings, IReadOnlyList<LaserPlacement> parts)
        => new(1, 1, new("Red", "#FF0000"), settings, settings.GetSize(), parts);
    private sealed class Dialogs(string folder) : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(folder);
    }
}
