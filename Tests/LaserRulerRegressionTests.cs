using Avalonia;
using HappyColorSplit.Controls;

internal static class LaserRulerRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var ticks = LaserWorkspaceCanvas.Ticks(300, 254).ToArray();
        check(ticks.Select(t => t.Cm).SequenceEqual(new decimal[] { 0, .5m, 1, 1.5m, 2, 2.5m, 3 }),
            "Centimetre rulers include whole and half centimetres from the origin to the sheet edge");
        check(ticks[0].Fraction == 0 && ticks[^1].Fraction == 1 && Math.Abs(ticks[1].Fraction - 1.0 / 6) < 1e-10,
            "Ruler ticks align with physical pixel positions rather than stretching independently of the sheet");
        var partial = LaserWorkspaceCanvas.Ticks(299, 254).ToArray();
        check(partial[^1].Cm == 2.5m && Math.Abs(partial[1].Fraction - 50.0 / 299) < 1e-10,
            "Pixel-rounded sheet dimensions do not invent a final centimetre tick outside the actual sheet");
        foreach (var size in new[] { new Size(600, 400), new Size(300, 600), new Size(1120, 740) })
        {
            var sheet = LaserWorkspaceCanvas.SheetBounds(size, new(300, 200));
            check(sheet.Left >= 50 && sheet.Top >= 32 && sheet.Right <= size.Width && sheet.Bottom <= size.Height
                && Math.Abs(sheet.Width / sheet.Height - 1.5) < 1e-10,
                "Both rulers retain their own gutter while the sheet keeps its aspect ratio when resized");
        }
    }
}
