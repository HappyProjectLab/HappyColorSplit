using System.Text.Json;
using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public sealed class SettingsService(string? directory = null, string? legacyDirectory = null)
{
    private readonly string _path = Path.Combine(directory ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HappyColorSplit"), "settings.json");
    // Read the previous application's settings only when no new settings exist.
    private readonly string? _legacyPath = legacyDirectory is not null ? Path.Combine(legacyDirectory, "settings.json")
        : directory is null ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HappyColors_to_PNG", "settings.json") : null;

    public AppSettings Load()
    {
        try
        {
            string source = !File.Exists(_path) && _legacyPath is not null ? _legacyPath : _path;
            string json = File.ReadAllText(source);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && !document.RootElement.TryGetProperty(nameof(AppSettings.ExportArea), out _)
                && document.RootElement.TryGetProperty("ExportInColor", out var legacyColor)
                && legacyColor.ValueKind == JsonValueKind.True)
                settings.ExportArea = ExportArea.GroupColor;
            if (!Enum.IsDefined(settings.ExportArea)) settings.ExportArea = ExportArea.White;
            settings.Language = settings.Language == "en" ? "en" : "de";
            settings.Tolerance = double.IsFinite(settings.Tolerance) ? Math.Clamp(settings.Tolerance, 0, 150) : 40;
            settings.MinimumAlpha = Math.Clamp(settings.MinimumAlpha, 1, 254);
            settings.LastExportFolder ??= "";
            if (!Enum.IsDefined(settings.ExportBackground)) settings.ExportBackground = ExportBackground.Black;
            settings.DefaultExportDpi = Math.Clamp(settings.DefaultExportDpi, 1, 2400);
            settings.ScaleUnit = settings.ScaleUnit == 1 ? 1 : 0;
            settings.Laser ??= new();
            try { settings.Laser.GetSize(); } catch (LaserSettingsException) { settings.Laser.WidthCm = 30; settings.Laser.HeightCm = 20; settings.Laser.Dpi = 300; }
            settings.Laser.Materials = (settings.Laser.Materials ?? []).Where(m => m is not null && LaserMaterial.TryRgb(m.Hex, out _))
                .Select(m => new LaserMaterial(string.IsNullOrWhiteSpace(m.Name) ? m.Hex.ToUpperInvariant() : m.Name.Trim()[..Math.Min(m.Name.Trim().Length, 80)], "#" + m.Hex.Trim().TrimStart('#').ToUpperInvariant()))
                .DistinctBy(m => m.Hex).Take(256).ToList();
            return settings;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(_path + ".tmp", _path, true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }
}
