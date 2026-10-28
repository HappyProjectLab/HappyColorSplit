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

internal static class ColorListDoubleClickTests
{
    public static void Run(string root, Action<bool, string> check, Action<Func<bool>> pump)
    {
        string input = Path.Combine(root, "list-doubleclick.png");
        using (var image = new RasterImage(120, 60, new RgbaPixel(20, 90, 200)))
        {
            for (int y = 15; y < 45; y++) for (int x = 20; x < 100; x++) image[x, y] = new(230, 50, 60);
            image.SaveAsPng(input);
        }
        using var vm = new MainViewModel(new Dialogs(), new SettingsService(Path.Combine(root, "list-doubleclick-settings")));
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            vm.ReduceMotion = true;
            var add = vm.AddImagesAsync([input]); pump(() => add.IsCompleted && !vm.IsBusy);
            vm.AnalyzeCommand.Execute(null); pump(() => !vm.IsBusy); Layout();
            var original = window.FindControl<ImageViewport>("OriginalViewportControl")!;
            var list = window.FindControl<ListBox>("ColorsList")!;
            var red = vm.Groups.Single(g => g.Group.R > 200);
            var blue = vm.Groups.Single(g => g.Group.B > 150);
            red.ExportEnabled = false;
            var exportChoices = vm.Groups.Select(group => group.ExportEnabled).ToArray();
            Point OriginalPoint(double u, double v)
            {
                var r = ImageViewport.ImageBounds(original.Bounds.Size, original.Source!.Size, original.Zoom, original.CenterX, original.CenterY);
                return original.TranslatePoint(new Point(r.X + r.Width * u, r.Y + r.Height * v), window)!.Value;
            }
            void Click(Point point, MouseButton button = MouseButton.Left)
            { window.MouseDown(point, button); window.MouseUp(point, button); Layout(); }
            Point Center(Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
            Click(OriginalPoint(.5, .5));
            check(original.HighlightedGroupIndex == red.Group.Index, "Baseline original-image left click highlights red");
            using var leftClick = Capture(original);
            byte[] mask = Pixels(vm.Mask!);
            Click(OriginalPoint(.05, .5));
            vm.ShowCombinedPreview = true; Layout();
            var redSwatch = list.GetVisualDescendants().OfType<ColorSwatch>().Single(control => ReferenceEquals(control.DataContext, red));
            Click(Center(redSwatch));
            check(vm.SelectedGroup == red && original.HighlightedGroupIndex == blue.Group.Index && vm.ShowCombinedPreview, "Single list click keeps its existing selection behavior without activating the original highlight");
            Click(Center(redSwatch));
            check(vm.SelectedGroup == red && original.HighlightedGroupIndex == red.Group.Index && red.IsHighlighted && !blue.IsHighlighted && !vm.ShowCombinedPreview,
                "Double-click on the list swatch activates the same group and individual preview as the original-image click");
            using var doubleClick = Capture(original);
            check(Pixels(doubleClick).SequenceEqual(Pixels(leftClick)) && Pixels(vm.Mask!).SequenceEqual(mask), "List double-click produces exactly the same original highlight and export preview pixels as original-image left click");
            check(vm.Groups.Select(group => group.ExportEnabled).SequenceEqual(exportChoices), "List double-click leaves export checkboxes unchanged, including the unchecked picked group");
            var blueHex = list.GetVisualDescendants().OfType<TextBlock>().Single(text => ReferenceEquals(text.DataContext, blue) && text.Text == blue.Hex);
            Click(Center(blueHex)); Click(Center(blueHex));
            check(vm.SelectedGroup == blue && original.HighlightedGroupIndex == blue.Group.Index, "Double-clicking the color text activates that color too");
            var redCheck = list.GetVisualDescendants().OfType<CheckBox>().Single(control => ReferenceEquals(control.DataContext, red));
            Click(Center(redCheck)); Click(Center(redCheck));
            check(original.HighlightedGroupIndex == blue.Group.Index && vm.Groups.Select(group => group.ExportEnabled).SequenceEqual(exportChoices), "Double-clicking a checkbox retains normal checkbox toggling and does not activate a color");
            Click(Center(redSwatch), MouseButton.Right); Click(Center(redSwatch), MouseButton.Right);
            check(original.HighlightedGroupIndex == blue.Group.Index && vm.Groups.Select(group => group.ExportEnabled).SequenceEqual(exportChoices), "Right clicks in the color list do not trigger the new left-double-click action");
            vm.OriginalViewport.Zoom = 3; Layout();
            Click(Center(redSwatch)); Click(Center(redSwatch));
            check(original.HighlightedGroupIndex == red.Group.Index && original.Zoom == 3 && vm.SelectedGroup == red, "List double-click preserves the original viewport zoom");
            vm.ShowCombinedPreview = true;
            Click(Center(redSwatch)); Click(Center(redSwatch));
            check(!vm.ShowCombinedPreview && original.HighlightedGroupIndex == red.Group.Index, "Double-clicking the already-selected group also restores its individual preview");
            vm.Tolerance += 1; Layout();
            original.ActivateColorGroup(red.Group.Index);
            check(original.HighlightedGroupIndex == -1 && !vm.HasMask, "Cleared detection cannot activate a stale group index");
        }
        finally { window.Close(); }
    }
    private static void Layout() => Dispatcher.UIThread.RunJobs();
    private static RenderTargetBitmap Capture(Control control)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(control.Bounds.Width), (int)Math.Ceiling(control.Bounds.Height)));
        bitmap.Render(control); return bitmap;
    }
    private static byte[] Pixels(Bitmap bitmap)
    {
        using var stream = new MemoryStream(); bitmap.Save(stream, PngBitmapEncoderOptions.Default);
        using var image = RasterImageCodec.Load(stream.ToArray());
        var pixels = new byte[image.Width * image.Height * 4];
        for (int y = 0; y < image.Height; y++) image.GetRowSpan(y).CopyTo(pixels.AsSpan(y * image.Width * 4));
        return pixels;
    }
    private sealed class Dialogs : IFileDialogService
    {
        public Task<IReadOnlyList<string>> OpenImagesAsync(Texts texts) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string?> SelectOutputAsync(Texts texts) => Task.FromResult<string?>(null);
    }
}
