namespace HappyColorSplit.Models;

public sealed record ColorGroup(int Index, byte R, byte G, byte B, int PixelCount)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
}

public sealed record ColorAnalysis(int Width, int Height, int[] Labels, IReadOnlyList<ColorGroup> Groups);

public sealed class ImageLimitException : Exception;
public sealed class ColorLimitException : Exception;
public sealed class EmptyImageException : Exception;
public sealed class NoColorsSelectedException : Exception;
