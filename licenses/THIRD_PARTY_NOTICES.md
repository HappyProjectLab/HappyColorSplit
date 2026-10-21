# Third-Party Notices for the HappyColorSplit Windows Application

This file records the third-party components found in the audited HappyColorSplit `win-x64` self-contained, multi-file publish. It is a technical inventory, not legal advice and not a claim of legal completeness. HappyColorSplit itself is licensed under `GPL-3.0-or-later`; third-party components remain under their own licenses.

Audit baseline: .NET 10, Avalonia 12.1.1, SkiaSharp 3.119.4, Windows RID `win-x64`, self-contained with .NET runtime 10.0.12, multi-file, not trimmed.

## Runtime components in the HappyColorSplit publish

| Component / package | Version | Role | License metadata | Included legal text |
|---|---:|---|---|---|
| Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, Avalonia.Fonts.Inter, Avalonia.Base, Avalonia.Controls, Avalonia.DesignerSupport, Avalonia.Dialogs, Avalonia.FreeDesktop, Avalonia.FreeDesktop.AtSpi, Avalonia.HarfBuzz, Avalonia.Markup, Avalonia.Markup.Xaml, Avalonia.Metal, Avalonia.Native, Avalonia.OpenGL, Avalonia.Remote.Protocol, Avalonia.Skia, Avalonia.Vulkan, Avalonia.Win32, Avalonia.Win32.Automation, Avalonia.X11 | 12.1.1 | UI framework and platform backends | MIT; Avalonia Team / AvaloniaUI OÜ | `Avalonia/LICENSE.md`, `Avalonia/NOTICE.md` |
| Inter font embedded by Avalonia.Fonts.Inter | 3.019, git 0a5106e0b | Application font | SIL Open Font License 1.1; The Inter Project Authors | `Inter/LICENSE.txt` |
| Avalonia.Angle.Windows.Natives | 2.1.27548.20260419 | `av_libglesv2.dll` | ANGLE BSD-3-Clause-style license plus the component licenses listed below | `Avalonia.Angle.Windows.Natives/LICENSE`, `Avalonia.Angle.Windows.Natives/third-party/*` |
| SkiaSharp, SkiaSharp.NativeAssets.Win32 | 3.119.4 | Image processing and `libSkiaSharp.dll` | MIT package license plus bundled notices; Microsoft/Mono project and upstream components | `SkiaSharp-HarfBuzzSharp/LICENSE.txt`, `SkiaSharp-HarfBuzzSharp/THIRD-PARTY-NOTICES.txt` |
| HarfBuzzSharp, HarfBuzzSharp.NativeAssets.Win32 | 8.3.1.3 | Text shaping and `libHarfBuzzSharp.dll` | MIT package license plus bundled notices; Microsoft/Mono project and upstream components | `SkiaSharp-HarfBuzzSharp/LICENSE.txt`, `SkiaSharp-HarfBuzzSharp/THIRD-PARTY-NOTICES.txt` |
| MicroCom.Runtime | 0.11.6 | Transitive COM interop runtime | MIT; Copyright 2021 Nikita Tsukanov | `MicroCom.Runtime/LICENSE` |
| Tmds.DBus.Protocol | 0.94.1 | Transitive D-Bus protocol runtime used by Avalonia desktop packages | MIT; Alp Toker, other contributors, Tom Deseyn | `Tmds.DBus.Protocol/COPYING` |
| Microsoft.NETCore.App.Host.win-x64 (SDK apphost template) | 10.0.12, SDK 10.0.401 | Native Windows launcher patched by the SDK to produce `HappyColorSplit.exe` | MIT and package-supplied third-party notices; .NET Foundation and contributors | `DotNet-AppHost/LICENSE.TXT`, `DotNet-AppHost/THIRD-PARTY-NOTICES.TXT` |

The SkiaSharp and HarfBuzzSharp native packages ship the same upstream `LICENSE.txt` and the same `THIRD-PARTY-NOTICES.txt`; one byte-identical copy is retained here and explicitly assigned to both package families. That notice contains the component-specific notices supplied with those native NuGet packages, including notices for Skia, HarfBuzz, ICU, libpng, ANGLE and other incorporated code.

`Avalonia.BuildServices` 11.3.2 is a build-only transitive package and is not present in the audited application publish. This self-contained publish redistributes the .NET 10.0.12 Windows x64 runtime from the official `Microsoft.NETCore.App.Runtime.win-x64` package. Its 187 shipped managed/native files, archive hash, repository revision and legal texts are recorded in the inventory's `runtimePack` section. Full, unchanged `LICENSE.TXT` and `THIRD-PARTY-NOTICES.TXT` from that package are retained under `DotNet-Runtime/`. These aggregate notices cover the upstream package without asserting that every described component is incorporated into every runtime file. The native apphost launcher is accounted for separately below. A runtime update, single-file, trimmed, installer, macOS, Linux or other-RID distribution requires a new inventory and review.

## .NET apphost provenance

The bundled runtime comes from official NuGet package [Microsoft.NETCore.App.Runtime.win-x64 10.0.12](https://api.nuget.org/v3-flatcontainer/microsoft.netcore.app.runtime.win-x64/10.0.12/microsoft.netcore.app.runtime.win-x64.10.0.12.nupkg), repository https://github.com/dotnet/dotnet at revision `95017c711e6afc1085133d440e42b4bd78155701`. Its archive SHA-256 is `0F63DEE7CA4383CB16848966F81E439399E2058E1671EBE27E5EAF4B200E8691`. The restored package source is the official NuGet v3 feed. All 187 redistributed runtime files were matched to archive entries; the inventory preserves each package path and hash. The package declares MIT and includes aggregate third-party notices, both retained unchanged under `DotNet-Runtime/`.

The installed `apphost.exe` template is byte-identical to the template in the official NuGet package `Microsoft.NETCore.App.Host.win-x64` 10.0.12. Its SHA-256 is `0649F1BEBACEF4F17B6694201ABC6DDE152C333468D2314B4F7104D34D70C7AA`. Package metadata declares MIT and records source repository https://github.com/dotnet/dotnet at revision `95017c711e6afc1085133d440e42b4bd78155701`. The package's full `LICENSE.TXT` and aggregate `THIRD-PARTY-NOTICES.TXT` are retained unchanged. Those aggregate notices cover the upstream package and are not a claim that every component described there is incorporated into this launcher.

The SDK patches the template with the application assembly name and Windows resources, so the final application executable is not expected to have the template's hash. The inventory records the official package archive hash, template hash and SDK version; the technical validator checks the selected SDK and installed template. Runtime framework DLLs are additionally included from the separately inventoried .NET runtime package.

The ANGLE source/build investigation and retained upstream texts were originally documented for HappyPattern. HappyColorSplit reuses that component mapping after matching the package revision, package archive hash, x64 binary hash, and retained license-file hashes. No claim of an independent native rebuild is made.

## Official provenance used for this audit

- Avalonia 12.1.1 repository revision recorded by NuGet: https://github.com/AvaloniaUI/Avalonia/tree/e33eaed9c106846b200680751022385d9cc5dc6f
- Avalonia ANGLE native package repository revision: https://github.com/AvaloniaUI/angle/tree/1c89805903c1482166356d3b950d474973180e61
- Inter font metadata in the shipped font identifies version 3.019, git 0a5106e0b: https://github.com/rsms/inter/tree/0a5106e0b
- SkiaSharp 3.119.4 repository revision recorded by NuGet: https://github.com/mono/SkiaSharp/tree/f568ac94dd768ef9a2f593537cfde2dd0d348ef5
- MicroCom.Runtime 0.11.6 repository revision recorded by NuGet: https://github.com/kekekeks/MicroCom/tree/76785efcafd91b5902fd19dd11145f6dd655b7b4
- Tmds.DBus.Protocol 0.94.1 repository revision recorded by NuGet: https://github.com/tmds/Tmds.DBus/tree/b4a7fed0b878f74cb54f7cca84d2889af4e596ba

## Avalonia ANGLE native binary audit

The audited Windows publish contains `av_libglesv2.dll` with SHA-256 `53191A77FE783CD757CA7767077C2A64A662E7043777A5B4AB74980D4A0B73E3` and a size of 5,394,096 bytes. It is byte-identical to `runtimes/win-x64/native/av_libglesv2.dll` in `Avalonia.Angle.Windows.Natives` 2.1.27548.20260419. The package archive has SHA-256 `76D67901097E9173D155EFC4C2B7D2C8E3122F2C2E0415028EB0C88478A386F9`; its NuGet repository metadata and the DLL version resource both identify Avalonia ANGLE commit `1c89805903c1482166356d3b950d474973180e61` (the version resource abbreviates it to `1c89805903c1`).

Avalonia's [successful GitHub Actions run 24629955434](https://github.com/AvaloniaUI/angle/actions/runs/24629955434) built the DLL from that fork revision. The workflow builds `libGLESv2.dll` and renames it to `av_libGLESv2.dll` before packing it; it does not copy an externally supplied official ANGLE binary. Its Windows x64 GN arguments disable Vulkan, OpenGL, null, D3D9, Metal, WebGPU and SwiftShader, leaving the D3D11 backend. Frame capture is not selected for the `libGLESv2` target. The exact build graph consequently establishes the following redistributed code and license texts:

| Incorporated code | Exact audited revision | License / notice file shipped here | Source and retained file SHA-256 |
|---|---|---|---|
| ANGLE and Avalonia fork changes | `1c89805903c1482166356d3b950d474973180e61` | `Avalonia.Angle.Windows.Natives/LICENSE` | [AvaloniaUI/angle LICENSE](https://github.com/AvaloniaUI/angle/blob/1c89805903c1482166356d3b950d474973180e61/LICENSE), `54AFF7276217DF9F6B5181613999D208C9E40D2B1D51BF55217837E6871A4A63` |
| Chromium `base::` helper code hard-forked in ANGLE | `28b5bbb227d331c01e6ff9b2f8729732135aadc7` as recorded by ANGLE's `src/common/base/README.chromium` | `Avalonia.Angle.Windows.Natives/third-party/Chromium-LICENSE` | [Chromium LICENSE](https://chromium.googlesource.com/chromium/src/+/28b5bbb227d331c01e6ff9b2f8729732135aadc7/LICENSE), `845022E0C1DB1ABB41A6BA4CD3C4B674EC290F3359D9D3C78AE558D4C0ED9308` |
| Abseil C++ | wrapper `677093525ed5a2f357482681a222274866088a2c`, upstream `b9536c952c6f6bbcb37a5edb4fa88ee62d27cd7e` | `Avalonia.Angle.Windows.Natives/third-party/Abseil-LICENSE` | [abseil-cpp LICENSE](https://github.com/abseil/abseil-cpp/blob/b9536c952c6f6bbcb37a5edb4fa88ee62d27cd7e/LICENSE), `C79A7FEA0E3CAC04CD43F20E7B648E5A0FF8FA5344E644B0EE09CA1162B62747` |
| Arm ASTC Encoder | `2319d9c4d4af53a7fc7c52985e264ce6e8a02a9b` | `Avalonia.Angle.Windows.Natives/third-party/ASTC-Encoder-LICENSE.txt` | [astc-encoder LICENSE.txt](https://github.com/ARM-software/astc-encoder/blob/2319d9c4d4af53a7fc7c52985e264ce6e8a02a9b/LICENSE.txt), `494ACCC32E50EB523A0E384D0AE6D4B702DB867A89D6971216760E92B240EE12` |
| xxHash 0.8.3 | `e626a72bc2321cd320e953a0ccf1584cad60f363` | `Avalonia.Angle.Windows.Natives/third-party/xxHash-LICENSE` | [vendored ANGLE license](https://github.com/AvaloniaUI/angle/blob/1c89805903c1482166356d3b950d474973180e61/src/common/third_party/xxhash/LICENSE), `6FFEDBC0F7878612D2B23589F1FF2AB15633E1DF7963A5D9FC750EC5500C7E7A` |
| Chromium zlib wrapper / zlib 1.3.2 | wrapper `b80f1d1e5256ac25f6aea3f31f13d458981cb1f9`, upstream `09a1572aa624e5ddb6c075dc013880de70b1b9b9` | `Avalonia.Angle.Windows.Natives/third-party/zlib-LICENSE` and the Chromium license above for Chromium-authored wrapper code | [Chromium zlib LICENSE](https://chromium.googlesource.com/chromium/src/third_party/zlib/+/b80f1d1e5256ac25f6aea3f31f13d458981cb1f9/LICENSE), `E32FF4E00D9D94930537635291DA39E7E612703334BF6FDE8C7F1686FE8A45A2` |

The exact ASTC Encoder and Abseil source revisions contain no applicable upstream `NOTICE` file in addition to their full license texts. The NuGet package itself contains only the ANGLE license and the three architecture-specific DLLs; it supplies no consolidated third-party notice file.

The following components present elsewhere in the ANGLE source checkout are not incorporated into this audited DLL: RapidJSON and ceval (frame-capture-only code), Vulkan/SPIR-V/glslang, SwiftShader, the Vulkan overlay, D3D9, OpenGL, Metal, WebGPU and the null backend. Windows D3D11 system interfaces are used, but no Windows SDK or Direct3D redistributable binary is included in the HappyColorSplit publish.

## Distribution note

Keep this entire `licenses` directory, the root `LICENSE`, and the `docs/THIRD_PARTY_*` files with binary distributions. An installer must install them in an accessible location and must not discard them. Re-run the technical compliance validator after every dependency, RID, publish-mode or deployment change.
