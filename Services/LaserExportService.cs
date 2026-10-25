using System.Text.Json;
using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed class LaserExportService
{
    public string Export(IReadOnlyList<LaserPage> pages, string parent, ExportOptions options,
        IProgress<int>? progress, CancellationToken token, bool merged = false)
    {
        token.ThrowIfCancellationRequested();
        if (pages.Count == 0) throw new LaserNoPartsException();
        string suffix = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
        string destination = Path.Combine(parent, "HappyColorSplit_Laser_" + suffix);
        string staging = Path.Combine(parent, $".HappyColorSplit_Laser_{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(staging);
        try
        {
            var renderer = new LaserPageRenderer();
            for (int i = 0; i < pages.Count; i++)
            {
                token.ThrowIfCancellationRequested(); var page = pages[i];
                var color = page.Material.Color;
                string file = $"Laser_{page.Index:D3}_{color.R:X2}{color.G:X2}{color.B:X2}{(merged ? "_merged" : "")}.png";
                File.WriteAllBytes(Path.Combine(staging, file), renderer.Render(page, options, token));
                progress?.Report((i + 1) * 100 / pages.Count);
            }
            var manifest = pages.Select(page => new
            {
                Page = page.Index, page.MaterialPage, Material = page.Material.Name, page.Material.Hex,
                page.Settings.WidthCm, page.Settings.HeightCm, page.Settings.Dpi, MinimumGapCm = LaserSettings.GapCm,
                PixelWidth = page.Size.Width, PixelHeight = page.Size.Height,
                Elements = page.Placements.Select(p => new
                {
                    File = p.Part.SourceName, Source = p.Part.SourceIndex, p.SourceChunk, p.Part.Region, Part = p.Part.Id,
                    XCm = p.X * 2.54m / page.Settings.Dpi, YCm = p.Y * 2.54m / page.Settings.Dpi,
                    WidthCm = p.Width * 2.54m / page.Settings.Dpi, HeightCm = p.Height * 2.54m / page.Settings.Dpi,
                    RotationDegrees = p.Turns * 90
                })
            });
            File.WriteAllText(Path.Combine(staging, "Laser_Layout.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
            token.ThrowIfCancellationRequested(); Directory.Move(staging, destination); return destination;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }
}
