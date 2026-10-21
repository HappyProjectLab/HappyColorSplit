namespace HappyColorSplit.Services;

public static class SupportedImageFiles
{
    public static IReadOnlyList<string> Extensions { get; } = Array.AsReadOnly(new[] { ".png", ".jpg", ".jpeg" });
    public static bool IsSupported(string? path) => !string.IsNullOrWhiteSpace(path)
        && Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
}
