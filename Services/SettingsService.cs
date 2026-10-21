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
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(source)) ?? new();
            settings.Language = settings.Language == "en" ? "en" : "de";
            settings.Tolerance = double.IsFinite(settings.Tolerance) ? Math.Clamp(settings.Tolerance, 0, 150) : 40;
            settings.MinimumAlpha = Math.Clamp(settings.MinimumAlpha, 1, 254);
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
