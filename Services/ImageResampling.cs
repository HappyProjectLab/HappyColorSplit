namespace HappyColorSplit.Services;

internal static class ImageResampling
{
    // Sample the final export grid first, so a reduced preview matches the exported mask.
    public static int Source(int position, int previewLength, int exportLength, int sourceLength) =>
        (int)((position * (long)exportLength / previewLength) * sourceLength / exportLength);
}
