using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using HappyColorSplit.Models;

namespace HappyColorSplit.Controls;

public sealed partial class LaserWorkspaceCanvas : Control
{
    public static readonly StyledProperty<IImage?> PreviewProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, IImage?>(nameof(Preview));
    public static readonly StyledProperty<LaserPage?> PageProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, LaserPage?>(nameof(Page));
    public static readonly StyledProperty<IBrush?> SheetBackgroundProperty = AvaloniaProperty.Register<LaserWorkspaceCanvas, IBrush?>(nameof(SheetBackground));
    static LaserWorkspaceCanvas() => AffectsRender<LaserWorkspaceCanvas>(PreviewProperty, PageProperty, SheetBackgroundProperty);
    public IImage? Preview { get => GetValue(PreviewProperty); set => SetValue(PreviewProperty, value); }
    public LaserPage? Page { get => GetValue(PageProperty); set => SetValue(PageProperty, value); }
    public IBrush? SheetBackground { get => GetValue(SheetBackgroundProperty); set => SetValue(SheetBackgroundProperty, value); }

    public static Rect SheetBounds(Size available, ImageDimensions pixels)
    {
        double width = Math.Max(0, available.Width - 62), height = Math.Max(0, available.Height - 44);
        double scale = Math.Min(width / pixels.Width, height / pixels.Height);
        double w = pixels.Width * scale, h = pixels.Height * scale;
        return new(50 + (width - w) / 2, 32 + (height - h) / 2, w, h);
    }

    // Use actual PNG pixels and DPI, rather than stretching the nominal centimetre setting.
    public static IEnumerable<(decimal Cm, double Fraction)> Ticks(int pixels, int dpi)
    {
        decimal cm = pixels * 2.54m / dpi;
        for (decimal tick = 0; tick <= cm; tick += .5m) yield return (tick, (double)(tick / cm));
    }

    public static Rect UsableBounds(Rect sheet, ImageDimensions pixels, int margin)
        => new(sheet.X + sheet.Width * margin / pixels.Width, sheet.Y + sheet.Height * margin / pixels.Height,
            sheet.Width * (pixels.Width - 2L * margin) / pixels.Width, sheet.Height * (pixels.Height - 2L * margin) / pixels.Height);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Page is not { } page) return;
        Rect sheet = SheetBounds(Bounds.Size, page.Size);
        if (sheet.Width <= 0 || sheet.Height <= 0) return;
        context.FillRectangle(SheetBackground ?? Brushes.Black, sheet);
        if (Preview is { } image) context.DrawImage(image, new Rect(image.Size), sheet);
        var ink = new SolidColorBrush(Color.Parse("#B7CBE0"));
        var ruler = new SolidColorBrush(Color.Parse("#1E2836"));
        var pen = new Pen(ink, 1);
        context.FillRectangle(ruler, new Rect(sheet.X, sheet.Y - 27, sheet.Width, 27));
        context.FillRectangle(ruler, new Rect(sheet.X - 43, sheet.Y, 43, sheet.Height));
        if (page.Settings.MarginPixels > 0)
        {
            var usable = UsableBounds(sheet, page.Size, page.Settings.MarginPixels);
            var disabled = new SolidColorBrush(Color.Parse("#4A4E56"));
            // Extend the same reserved bands into both rulers; the origin stays at the physical sheet edge.
            context.FillRectangle(disabled, new Rect(sheet.X, sheet.Y - 27, usable.X - sheet.X, sheet.Height + 27));
            context.FillRectangle(disabled, new Rect(usable.Right, sheet.Y - 27, sheet.Right - usable.Right, sheet.Height + 27));
            context.FillRectangle(disabled, new Rect(sheet.X - 43, sheet.Y, sheet.Width + 43, usable.Y - sheet.Y));
            context.FillRectangle(disabled, new Rect(sheet.X - 43, usable.Bottom, sheet.Width + 43, sheet.Bottom - usable.Bottom));
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#92969E")), 1), usable);
        }
        context.DrawRectangle(null, pen, sheet);
        Label(context, "cm", sheet.X - 32, sheet.Y - 23, ink);
        DrawTicks(context, page.Size.Width, page.Settings.Dpi, sheet, true, pen, ink);
        DrawTicks(context, page.Size.Height, page.Settings.Dpi, sheet, false, pen, ink);
    }

    private static void DrawTicks(DrawingContext context, int pixels, int dpi, Rect sheet, bool horizontal, Pen pen, IBrush ink)
    {
        double length = horizontal ? sheet.Width : sheet.Height;
        double halfCm = dpi / 2.54 * .5 * length / pixels;
        double lastLabelEnd = double.NegativeInfinity;
        foreach (var (cm, fraction) in Ticks(pixels, dpi))
        {
            bool whole = cm == decimal.Truncate(cm);
            double position = fraction * length, mark = whole ? 10 : 5;
            if (horizontal) context.DrawLine(pen, new(sheet.X + position, sheet.Y), new(sheet.X + position, sheet.Y - mark));
            else context.DrawLine(pen, new(sheet.X, sheet.Y + position), new(sheet.X - mark, sheet.Y + position));
            if (!whole && halfCm < (horizontal ? 20 : 15)) continue;
            var text = Text(cm.ToString("0.#", CultureInfo.CurrentCulture), ink);
            double span = horizontal ? text.Width : text.Height;
            double start = position - span / 2;
            if (start < lastLabelEnd + 3) continue;
            if (horizontal) context.DrawText(text, new(sheet.X + start, sheet.Y - 26));
            else context.DrawText(text, new(sheet.X - 15 - text.Width, sheet.Y + start));
            lastLabelEnd = start + span;
        }
    }
    private static FormattedText Text(string text, IBrush ink) => new(text, CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight, new Typeface(FontFamily.Default), 10, ink);
    private static void Label(DrawingContext context, string text, double x, double y, IBrush ink)
        => context.DrawText(Text(text, ink), new(x, y));
}
