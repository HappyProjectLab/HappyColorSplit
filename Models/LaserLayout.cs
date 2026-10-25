namespace HappyColorSplit.Models;

public sealed record LaserMaterial(string Name, string Hex)
{
    public static bool TryRgb(string? text, out RgbaPixel color)
    {
        string value = (text ?? "").Trim().TrimStart('#');
        if (value.Length == 6 && int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out int rgb))
        { color = new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); return true; }
        color = default; return false;
    }
    public RgbaPixel Color => TryRgb(Hex, out var color) ? color : throw new ArgumentException("Invalid material color.");
}

public sealed class LaserSettings
{
    public decimal WidthCm { get; set; } = 30;
    public decimal HeightCm { get; set; } = 20;
    public int Dpi { get; set; } = 300;
    public List<LaserMaterial> Materials { get; set; } = [];
    public const decimal GapCm = 0.5m;

    public ImageDimensions GetSize()
    {
        if (WidthCm is <= 0 or > 500 || HeightCm is <= 0 or > 500 || Dpi is < 1 or > 2400) throw new LaserSettingsException();
        decimal w = decimal.Floor(WidthCm * Dpi / 2.54m), h = decimal.Floor(HeightCm * Dpi / 2.54m);
        if (w < 1 || h < 1 || w * h > ImageScale.MaxPixels) throw new LaserSettingsException();
        return new((int)w, (int)h);
    }
    public LaserSettings Snapshot() => new() { WidthCm = WidthCm, HeightCm = HeightCm, Dpi = Dpi, Materials = Materials.ToList() };
}

public sealed record LaserSource(int Index, string FilePath, ImageScale Scale, IReadOnlySet<string> Colors);
public readonly record struct LaserRun(int Y, int X, int Length);
public readonly record struct LaserRect(int X, int Y, int Width, int Height);

public sealed class LaserPart(int id, int sourceIndex, string sourceName, int materialIndex, int nativeDpi,
    LaserRect nativeBounds, IReadOnlyList<LaserRun> runs, int width, int height)
{
    public int Id { get; } = id;
    public int SourceIndex { get; } = sourceIndex;
    public string SourceName { get; } = sourceName;
    public int MaterialIndex { get; } = materialIndex;
    public int NativeDpi { get; } = nativeDpi;
    public LaserRect NativeBounds { get; } = nativeBounds;
    public IReadOnlyList<LaserRun> Runs { get; } = runs;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public int Region { get; set; }
}

public sealed record LaserPlacement(LaserPart Part, int X, int Y, int Turns, int SourceChunk = 0)
{
    public int Width => Turns % 2 == 0 ? Part.Width : Part.Height;
    public int Height => Turns % 2 == 0 ? Part.Height : Part.Width;
    public LaserRect Bounds => new(X, Y, Width, Height);
}

public sealed record LaserPage(int Index, int MaterialPage, LaserMaterial Material, LaserSettings Settings,
    ImageDimensions Size, IReadOnlyList<LaserPlacement> Placements);
public sealed record LaserLayout(IReadOnlyList<LaserPage> Pages, int PartCount);
public sealed class LaserSettingsException : Exception;
public sealed class LaserComplexityException : Exception;
public sealed class LaserNoPartsException : Exception;
public sealed class LaserPartTooLargeException(string source, decimal width, decimal height) : Exception
{ public string SourceName { get; } = source; public decimal WidthCm { get; } = width; public decimal HeightCm { get; } = height; }
