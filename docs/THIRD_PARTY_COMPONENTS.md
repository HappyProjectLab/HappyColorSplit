# Third-party components used by HappyColorSplit

Audited on 20 October 2026. [German documentation](THIRD_PARTY_COMPONENTS_DE.md). Full upstream texts are retained under `licenses/`; detailed attribution and source revisions appear in [THIRD_PARTY_NOTICES.md](../licenses/THIRD_PARTY_NOTICES.md).

## Distribution baseline

- .NET 10.0.12, `win-x64`, self-contained
- Multi-file, not trimmed, not ReadyToRun, not single-file
- Publish profile: `Properties/PublishProfiles/Windows.pubxml`
- .NET 10.0.12 Windows x64 runtime included; no separately installed .NET runtime required
- 19 application NuGet packages with 32 library/symbol assets, plus a .NET runtime pack with 187 additional files, mapped in `licenses/component-inventory.json`

Only Windows x64 native assets are shipped. Avalonia also ships portable managed platform backends, which does not establish support for other operating systems. Other platforms or deployment modes require a separate inventory.

## Included components

| Component | Version | Retained legal texts |
|---|---|---|
| Avalonia desktop, rendering, backend and Fluent packages | 12.1.1 | MIT; `Avalonia/LICENSE.md`, `Avalonia/NOTICE.md` |
| Embedded Inter font | 3.019, revision `0a5106e0b`, as identified in the previous font inventory of the same Avalonia package | SIL Open Font License 1.1; `Inter/LICENSE.txt` |
| SkiaSharp, SkiaSharp.NativeAssets.Win32 | 3.119.4 | MIT and bundled upstream notices; `SkiaSharp-HarfBuzzSharp/` |
| HarfBuzzSharp, HarfBuzzSharp.NativeAssets.Win32 | 8.3.1.3 | MIT and bundled upstream notices; `SkiaSharp-HarfBuzzSharp/` |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | ANGLE and component licenses; `Avalonia.Angle.Windows.Natives/` |
| MicroCom.Runtime | 0.11.6 | MIT; `MicroCom.Runtime/LICENSE` |
| Tmds.DBus.Protocol | 0.94.1 | MIT; `Tmds.DBus.Protocol/COPYING` |
| SDK .NET apphost launcher template | 10.0.12, SDK 10.0.401 | MIT and package-wide notices; `DotNet-AppHost/` |
| Microsoft.NETCore.App.Runtime.win-x64 | 10.0.12 | MIT and full package-wide notices; `DotNet-Runtime/` |

SkiaSharp and HarfBuzzSharp native packages provide byte-identical license and third-party-notice files. One unchanged copy is retained and explicitly assigned to both families. Native symbol files provided by these packages are also inventoried.

Avalonia.BuildServices 11.3.2 is build tooling, absent from the audited output. Packages for other platforms may exist in the restore graph or cache without being shipped. The validator checks the actual publish output as well as the declared package versions.

## Provenance and verification

The retained license texts and component-level ANGLE investigation originated in HappyPattern. HappyColorSplit reuses that mapping after checking package versions, upstream repository revisions, package archive hashes and the bytes of the shipped assets. This is not a claim of an independent native rebuild.

`licenses/component-inventory.json` maps all shipped packages and assets to the appropriate legal files. It includes official repository URLs and revisions and SHA-256 hashes of package archives, 32 application third-party assets, 187 runtime files and 18 unchanged upstream license/notice texts, including the application license. `.gitattributes` preserves the exact bytes of retained legal texts across checkouts.

`HappyColorSplit.exe` additionally includes the native .NET apphost template. The installed template was matched byte-for-byte against official NuGet package version 10.0.12. Its MIT license and package-wide notices are retained unchanged. The SDK version and unpatched template hash are tracked separately; the final executable also contains the application name and Windows resources patched in by the SDK. Changing the SDK therefore requires a fresh review. The official `Microsoft.NETCore.App.Runtime.win-x64` 10.0.12 NuGet package from `api.nuget.org` supplies the bundled runtime. All 187 shipped runtime files were matched byte-for-byte against its archive. Package and file hashes appear in the inventory's `runtimePack` section. Its complete MIT license and aggregate third-party notices are retained under `licenses/DotNet-Runtime/`. Runtime updates require a new audit and a new HappyColorSplit ZIP.

## Scope

HappyColorSplit has no Poppler or PDF functionality. Poppler documents and binaries are not included.

HappyColorSplit itself is licensed under `GPL-3.0-or-later`. Third-party components retain their own licenses. Distribution must also satisfy applicable GPL obligations for HappyColorSplit, including corresponding-source requirements. Matching source is available in the public repository and as a source ZIP alongside each release. Redistribution must keep the source corresponding to the binary version available under the applicable license terms.

## Distribution and future changes

Keep the root `LICENSE`, the entire `licenses` directory and both component documents with the application. They are copied automatically to build and publish output; a later installer must preserve them in an accessible location.

The **Licenses & notices** button in the main window opens an offline viewer for the project license, this overview and every retained original license/notice text. Text can be selected and copied. The interface and overview follow the selected German/English UI language; upstream legal texts are displayed without translation or truncation.

```powershell
dotnet publish HappyColorSplit.csproj -p:PublishProfile=Windows -o artifacts/HappyColorSplit-Windows
pwsh -NoProfile -File tools/ValidateThirdPartyCompliance.ps1 -PublishDirectory artifacts/HappyColorSplit-Windows
```

Re-inventory any change to dependencies, RID, deployment mode, trimming, single-file or installer contents. The validator detects selected technical drift and missing files; it does not certify legal completeness.
