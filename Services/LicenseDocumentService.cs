using HappyColorSplit.Models;

namespace HappyColorSplit.Services;

public interface ILicenseDocumentService
{
    IReadOnlyList<LicenseDocument> GetDocuments(Texts texts);
    Task<string> ReadAsync(LicenseDocument document, CancellationToken token);
}

public sealed class LicenseDocumentService(string? directory = null) : ILicenseDocumentService
{
    private readonly string _directory = Path.GetFullPath(directory ?? AppContext.BaseDirectory);

    public IReadOnlyList<LicenseDocument> GetDocuments(Texts texts)
    {
        LicenseDocument Entry(string name, string type, string path) => new($"{name} · {texts[type]}", path);
        return [
            new(texts["ProjectLicense"], "LICENSE"),
            new(texts["UsageNotice"], texts.Language == "de" ? "docs/USAGE_NOTICE_DE.md" : "docs/USAGE_NOTICE.md"),
            new(texts["ComponentOverview"], texts.Language == "de" ? "docs/THIRD_PARTY_COMPONENTS_DE.md" : "docs/THIRD_PARTY_COMPONENTS.md"),
            new(texts["ThirdPartyNotices"], "licenses/THIRD_PARTY_NOTICES.md"),
            Entry("Avalonia", "LicenseText", "licenses/Avalonia/LICENSE.md"),
            Entry("Avalonia", "Notices", "licenses/Avalonia/NOTICE.md"),
            Entry("Inter", "FontLicense", "licenses/Inter/LICENSE.txt"),
            Entry("SkiaSharp / HarfBuzzSharp", "LicenseText", "licenses/SkiaSharp-HarfBuzzSharp/LICENSE.txt"),
            Entry("SkiaSharp / HarfBuzzSharp", "Notices", "licenses/SkiaSharp-HarfBuzzSharp/THIRD-PARTY-NOTICES.txt"),
            Entry("ANGLE", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/LICENSE"),
            Entry("ANGLE / Chromium", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/third-party/Chromium-LICENSE"),
            Entry("ANGLE / Abseil", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/third-party/Abseil-LICENSE"),
            Entry("ANGLE / ASTC Encoder", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/third-party/ASTC-Encoder-LICENSE.txt"),
            Entry("ANGLE / xxHash", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/third-party/xxHash-LICENSE"),
            Entry("ANGLE / zlib", "LicenseText", "licenses/Avalonia.Angle.Windows.Natives/third-party/zlib-LICENSE"),
            Entry("MicroCom.Runtime", "LicenseText", "licenses/MicroCom.Runtime/LICENSE"),
            Entry("Tmds.DBus.Protocol", "LicenseText", "licenses/Tmds.DBus.Protocol/COPYING"),
            Entry(".NET AppHost", "LicenseText", "licenses/DotNet-AppHost/LICENSE.TXT"),
            Entry(".NET AppHost", "Notices", "licenses/DotNet-AppHost/THIRD-PARTY-NOTICES.TXT"),
            Entry(".NET Runtime", "LicenseText", "licenses/DotNet-Runtime/LICENSE.TXT"),
            Entry(".NET Runtime", "Notices", "licenses/DotNet-Runtime/THIRD-PARTY-NOTICES.TXT")
        ];
    }

    public Task<string> ReadAsync(LicenseDocument document, CancellationToken token)
    {
        string path = Path.GetFullPath(Path.Combine(_directory, document.RelativePath));
        if (!path.StartsWith(_directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("License document is outside the application directory.");
        return File.ReadAllTextAsync(path, token);
    }
}
