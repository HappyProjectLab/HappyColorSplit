using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HappyColorSplit.Controls;
using HappyColorSplit.Models;
using HappyColorSplit.Services;
using HappyColorSplit.ViewModels;
using HappyColorSplit.Views;

internal static class OriginalColorHoverTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string input = Path.Combine(root, "hover.png");
        using (var image = new RasterImage(200, 100, new RgbaPixel(30, 100, 190)))
        {
            for (int y = 20; y < 80; y++) for (int x = 20; x < 180; x++)
                if (x < 70 || x >= 130) image[x, y] = new((byte)(x < 70 ? 230 : 233), 50, 60);
            for (int y = 40; y < 60; y++) for (int x = 90; x < 110; x++) image[x, y] = new(0, 0, 0, 0);
            image[100, 20] = new(0, 255, 0, 20);
            image.SaveAsPng(input);
        }
        using var vm = new MainViewModel(new Dialogs(), new SettingsService(Path.Combine(root, "hover-settings")));
        var window = new MainWindow { DataContext = vm };
        window.Show();
        try
        {
            var add = vm.AddImagesAsync([input]); pump(() => add.IsCompleted && !vm.IsBusy);
            vm.Tolerance = 10; vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy); Layout();
            var control = window.FindControl<ImageViewport>("OriginalViewportControl")!;
            Point Local(double x, double y)
            {
                var r = ImageViewport.ImageBounds(control.Bounds.Size, control.Source!.Size, control.Zoom, control.CenterX, control.CenterY);
                return new(r.X + r.Width * x, r.Y + r.Height * y);
            }
            Point Global(double x, double y) => control.TranslatePoint(Local(x, y), window)!.Value;
            var red = vm.Groups.Single(g => g.Group.R > 200);
            var blue = vm.Groups.Single(g => g.Group.B > 150);
            red.ExportEnabled = false;
            vm.SelectedGroup = blue; vm.ShowCombinedPreview = true;
            var before = vm.Groups.Select(g => g.ExportEnabled).ToArray();
            window.MouseMove(Global(.2, .5)); Layout();
            check(control.HighlightedGroupIndex == -1 && !red.IsHighlighted && !blue.IsHighlighted, "Hover does not highlight any group in the original or color list");
            check(vm.SelectedGroup == blue && vm.ShowCombinedPreview && vm.Groups.Select(g => g.ExportEnabled).SequenceEqual(before), "Hover leaves selection, combined preview and export choices unchanged");
            check(control.ColorGroupAt(Local(.8, .5)) == red.Group.Index, "Disconnected areas and similar reds resolve to the same detected group");
            using var plain = Capture(control);
            RenderTargetBitmap? firstFrame = null, intermediateFrame = null;
            // The headless input helpers pump frames before returning; sample within the animation instead.
            void SampleTransition(object? sender, int index)
            {
                if (index != red.Group.Index) return;
                firstFrame = Capture(control);
                DispatcherTimer.RunOnce(() => intermediateFrame = Capture(control), TimeSpan.FromMilliseconds(45));
            }
            control.ColorGroupHighlighted += SampleTransition;
            window.MouseDown(Global(.2, .5), MouseButton.Left); window.MouseUp(Global(.2, .5), MouseButton.Left); Layout();
            control.ColorGroupHighlighted -= SampleTransition;
            check(control.HighlightedGroupIndex == red.Group.Index && red.IsHighlighted && !blue.IsHighlighted, "Only a left click activates the group highlight");
            Animate(280);
            using var start = firstFrame!;
            using var middle = intermediateFrame!;
            using var settled = Capture(control);
            check(Difference(start, plain) < Difference(settled, plain), "Click starts with a weaker effect than the completed highlight");
            check(!Pixels(middle).SequenceEqual(Pixels(plain)) && !Pixels(middle).SequenceEqual(Pixels(settled)), "Highlight passes through an intermediate rendered state");
            // The red surface remains at its original top and bottom edges: no translated copy above it.
            var edge = Local(.2, .2);
            var surface = Local(.2, .5);
            var originalSurface = Pixel(plain, surface);
            var selectedSurface = Pixel(settled, surface);
            var above = Pixel(settled, new Point(edge.X, edge.Y - 2));
            check(above.B > above.R && selectedSurface.R >= originalSurface.R, "Depth effect preserves the surface position with no red pixels shifted above its original edge");
            using (var capture = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height)))
            {
                capture.Render(window);
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "artifacts"); Directory.CreateDirectory(folder);
                using var output = File.Create(Path.Combine(folder, "color-hover-preview.png"));
                capture.Save(output, PngBitmapEncoderOptions.Default);
            }
            check(vm.SelectedGroup == red && !vm.ShowCombinedPreview && vm.HasMask, "Click selects the hovered color and switches to its individual layer");
            check(vm.Groups.Select(g => g.ExportEnabled).SequenceEqual(before), "Click preserves all export checkboxes including the unchecked picked group");
            vm.ReduceMotion = true; vm.RightSidebarCollapsed = true;
            vm.SelectedGroup = blue; vm.ShowCombinedPreview = true; Layout();
            window.MouseDown(Global(.8, .5), MouseButton.Right); window.MouseUp(Global(.8, .5), MouseButton.Right); Layout();
            check(red.ExportEnabled && blue.ExportEnabled && vm.SelectedImage!.ExportColors!.Contains(red.Hex), "Right click enables the unchecked matching color and persists it for export");
            check(!vm.RightSidebarCollapsed && red.ExportChangeStrength > 0 && blue.ExportChangeStrength == 0, "Right click reveals the matching row and flashes only that color");
            var redCheck = window.GetVisualDescendants().OfType<CheckBox>().Single(box => ReferenceEquals(box.DataContext, red));
            check(redCheck.IsChecked == true && redCheck.Parent is Border { BorderBrush: Avalonia.Media.SolidColorBrush haloBrush } && haloBrush.Color.A > 0, "Checkbox updates and receives a visible feedback border");
            using (var feedback = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height)))
            {
                feedback.Render(window);
                using var output = File.Create(Path.Combine(Directory.GetCurrentDirectory(), "artifacts", "export-toggle-feedback.png"));
                feedback.Save(output, PngBitmapEncoderOptions.Default);
            }
            check(vm.SelectedGroup == red && control.HighlightedGroupIndex == red.Group.Index && !vm.ShowCombinedPreview, "Right click selects the clicked color, highlights it and shows its individual layer just like left click");
            window.MouseDown(Global(.2, .5), MouseButton.Right); window.MouseUp(Global(.2, .5), MouseButton.Right); Layout();
            check(!red.ExportEnabled && blue.ExportEnabled && redCheck.IsChecked == false && !vm.SelectedImage!.ExportColors!.Contains(red.Hex), "Another area of the same color toggles its export checkbox back off");
            check(red.ExportChangeStrength > 0, "Repeated toggles restart the visual feedback");
            Animate(700);
            check(red.ExportChangeStrength == 0, "Export feedback clears after the short pulse");
            vm.ShowCombinedPreview = true;
            window.MouseDown(Global(.5, .1), MouseButton.Right); window.MouseUp(Global(.5, .1), MouseButton.Right); Layout();
            check(vm.ExportColorCount == 0 && vm.HasMask && !vm.ExportCommand.CanExecute(null) && !vm.ShowCombinedPreview && vm.SelectedGroup == blue && control.HighlightedGroupIndex == blue.Group.Index, "Right click exits combined preview, highlights the clicked color and disables export when its last checkbox is unchecked");
            window.MouseDown(Global(.5, .1), MouseButton.Right); window.MouseUp(Global(.5, .1), MouseButton.Right); Layout();
            check(blue.ExportEnabled && vm.HasMask && vm.ExportCommand.CanExecute(null) && vm.SelectedGroup == blue, "Rechecking a color retains its individual preview and restores export availability");
            vm.ShowCombinedPreview = false;
            vm.ReduceMotion = false;
            window.MouseMove(Global(.5, .5)); Layout();
            check(control.HighlightedGroupIndex == blue.Group.Index && blue.IsHighlighted, "Moving across transparency leaves the clicked highlight active");
            window.MouseMove(new Point(5, 5)); Layout();
            check(control.HighlightedGroupIndex == blue.Group.Index, "Leaving the image preserves the clicked highlight");
            window.MouseDown(Global(.5, .5), MouseButton.Right); window.MouseUp(Global(.5, .5), MouseButton.Right); Layout();
            check(control.HighlightedGroupIndex == -1 && vm.Groups.All(g => !g.IsHighlighted) && vm.Groups.Select(g => g.ExportEnabled).SequenceEqual(before), "Right click on transparency clears the highlight like left click and leaves export choices unchanged");
            window.MouseDown(Global(.5, .5), MouseButton.Left); window.MouseUp(Global(.5, .5), MouseButton.Left); Layout();
            check(control.HighlightedGroupIndex == -1 && vm.Groups.All(g => !g.IsHighlighted), "Left click on transparency clears the highlight");
            Animate(280);
            using var cleared = Capture(control);
            check(Pixels(cleared).SequenceEqual(Pixels(plain)), "Clearing fades back to the unchanged original");
            check(control.ColorGroupAt(Local(.5025, .205)) == -1, "Pixels excluded by minimum opacity cannot be picked");
            vm.SelectedGroup = blue;
            window.MouseDown(Global(.2, .5), MouseButton.Left); window.MouseMove(Global(.25, .55)); window.MouseUp(Global(.25, .55), MouseButton.Left); Layout();
            check(vm.SelectedGroup == blue && control.HighlightedGroupIndex == -1, "Dragging the original never selects or highlights a color");
            window.MouseDown(Global(.2, .5), MouseButton.Right); window.MouseMove(Global(.25, .55)); window.MouseUp(Global(.25, .55), MouseButton.Right); Layout();
            check(vm.Groups.Select(g => g.ExportEnabled).SequenceEqual(before), "Right dragging does not toggle export choices");
            vm.OriginalViewport.SetViewport(4, .65, .5); Layout();
            check(control.ColorGroupAt(Local(.8, .5)) == red.Group.Index, "Hit testing follows zoom and pan");
            window.MouseDown(Global(.8, .5), MouseButton.Right); window.MouseUp(Global(.8, .5), MouseButton.Right); Layout();
            check(red.ExportEnabled && vm.SelectedGroup == red && control.HighlightedGroupIndex == red.Group.Index, "Right click selects, highlights and toggles the matching group under zoom and pan");
            window.MouseDown(Global(.8, .5), MouseButton.Right); window.MouseUp(Global(.8, .5), MouseButton.Right); Layout();
            check(!red.ExportEnabled, "Repeated right clicks toggle even inside the double-click interval");
            vm.OriginalViewport.SetViewport(1, .5, .5); Layout();
            vm.ReduceMotion = true; Layout();
            window.MouseDown(Global(.8, .5), MouseButton.Left); window.MouseUp(Global(.8, .5), MouseButton.Left); Layout();
            using var immediate = Capture(control);
            check(control.HighlightedGroupIndex == red.Group.Index && Pixels(immediate).SequenceEqual(Pixels(settled)), "Reduced motion applies the same relief immediately");
            foreach (var viewport in new[] { (1d, .5, .5), (2d, .5, .5), (4d, .65, .5), (8d, .3, .5) })
            {
                vm.OriginalViewport.SetViewport(viewport.Item1, viewport.Item2, viewport.Item3); Layout();
                using var aligned = Capture(control);
                var bounds = ImageViewport.ImageBounds(control.Bounds.Size, control.Source!.Size, control.Zoom, control.CenterX, control.CenterY);
                int selectedSamples = 0, otherSamples = 0;
                for (int y = 8; y < aligned.PixelSize.Height - 8; y += 9)
                for (int x = 8; x < aligned.PixelSize.Width - 8; x += 9)
                {
                    double u = (x - bounds.X) / bounds.Width, v = (y - bounds.Y) / bounds.Height;
                    bool redInterior = v > .3 && v < .7 && ((u > .14 && u < .31) || (u > .69 && u < .86));
                    bool blueInterior = u > .02 && u < .98 && ((v > .03 && v < .13) || (v > .87 && v < .97));
                    var pixel = Pixel(aligned, new Point(x, y));
                    if (redInterior) { check(pixel.R >= 225 && pixel.B < 80, $"Selected surface stays aligned at zoom {viewport.Item1}, ({x},{y}): {pixel}"); selectedSamples++; }
                    if (blueInterior) { check(pixel.B >= 139 && pixel.B <= 150 && pixel.R <= 25, $"Dimming stays on unselected areas at zoom {viewport.Item1}, ({x},{y}): {pixel}"); otherSamples++; }
                }
                check(selectedSamples > 0, "Alignment fixture includes visible selected surface samples");
                if (viewport.Item1 == 1) check(otherSamples > 0, "Alignment fixture includes visible unselected surface samples");
            }
            vm.OriginalViewport.SetViewport(1, .5, .5); Layout();
            vm.ReduceMotion = false;
            window.MouseDown(Global(.5, .1), MouseButton.Left); window.MouseUp(Global(.5, .1), MouseButton.Left); Layout();
            Animate(90);
            window.MouseDown(Global(.8, .5), MouseButton.Left); window.MouseUp(Global(.8, .5), MouseButton.Left); Layout();
            Animate(280);
            check(control.HighlightedGroupIndex == red.Group.Index && red.IsHighlighted && !blue.IsHighlighted, "Rapid changes blend to the most recently clicked color");
            vm.OriginalViewport.Zoom = 2; Layout();
            check(control.HighlightedGroupIndex == red.Group.Index, "Zoom preserves the clicked highlight");
            vm.Tolerance = 11; Layout();
            check(control.Analysis is null && control.HighlightedGroupIndex == -1, "Changing detection settings clears stale hit testing and overlays");
        }
        finally { window.Close(); }
    }
    private static void Layout() { Dispatcher.UIThread.RunJobs(); }
    private static void Animate(int milliseconds)
    {
        using var finish = new CancellationTokenSource(TimeSpan.FromMilliseconds(milliseconds));
        Dispatcher.UIThread.MainLoop(finish.Token); Layout();
    }
    private static RenderTargetBitmap Capture(Control control)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)));
        bitmap.Render(control); return bitmap;
    }
    private static byte[] Pixels(Bitmap bitmap)
    {
        var pixels = new byte[bitmap.PixelSize.Width * bitmap.PixelSize.Height * 4];
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(pixels, System.Runtime.InteropServices.GCHandleType.Pinned);
        try { bitmap.CopyPixels(new PixelRect(bitmap.PixelSize), handle.AddrOfPinnedObject(), pixels.Length, bitmap.PixelSize.Width * 4); }
        finally { handle.Free(); }
        return pixels;
    }
    private static (byte B, byte G, byte R) Pixel(Bitmap bitmap, Point point)
    {
        var pixels = Pixels(bitmap); int p = ((int)point.Y * bitmap.PixelSize.Width + (int)point.X) * 4;
        return (pixels[p], pixels[p + 1], pixels[p + 2]);
    }
    private static long Difference(Bitmap a, Bitmap b) => Pixels(a).Zip(Pixels(b), (left, right) => (long)Math.Abs(left - right)).Sum();
    private sealed class Dialogs : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
}
