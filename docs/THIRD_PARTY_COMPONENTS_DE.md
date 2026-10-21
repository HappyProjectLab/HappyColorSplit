# Drittanbieter-Komponenten von HappyColorSplit

Prüfstand: 20. Oktober 2026. Diese Übersicht beschreibt das konkret geprüfte Windows-Paket. Die vollständigen Original-Lizenztexte und Copyright-Hinweise stehen unter `licenses/`; die technische Zuordnung steht in [THIRD_PARTY_NOTICES.md](../licenses/THIRD_PARTY_NOTICES.md).

## Geprüfte Ausgabe

- .NET 10, Zielplattform `win-x64`
- Eigenständig startfähig (self-contained): .NET-Laufzeit 10.0.12 für Windows x64 ist enthalten; keine separate .NET-Installation erforderlich.
- Mehrere Dateien, ohne Single-File-Paket, Trimming oder ReadyToRun
- Publish-Profil: `Properties/PublishProfiles/Windows.pubxml`
- 19 Anwendungs-NuGet-Pakete mit 32 Bibliotheks-/Symboldateien sowie das .NET-Laufzeitpaket mit 187 zusätzlichen Dateien, zugeordnet in `licenses/component-inventory.json`

Die nativen Bibliotheken sind Windows-x64-Dateien. Avalonia liefert zusätzlich plattformübergreifende verwaltete Backend-Assemblies mit; deren Vorhandensein erklärt noch keine Unterstützung anderer Betriebssysteme. Plattformen und Architekturen außerhalb dieses Profils benötigen eine eigene Bestandsaufnahme.

## Zuordnung

| Komponente | Version | Lizenztexte |
|---|---|---|
| Avalonia und zugehörige Desktop-, Backend- und Fluent-Pakete | 12.1.1 | MIT; `licenses/Avalonia/LICENSE.md` und `NOTICE.md` |
| Eingebettete Inter-Schrift | 3.019, Revision `0a5106e0b` gemäß der bisherigen Font-Inventur desselben Avalonia-Pakets | SIL Open Font License 1.1; `licenses/Inter/LICENSE.txt` |
| SkiaSharp und Windows-Nativpaket | 3.119.4 | MIT und gebündelte Drittanbieterhinweise; `licenses/SkiaSharp-HarfBuzzSharp/` |
| HarfBuzzSharp und Windows-Nativpaket | 8.3.1.3 | MIT und gebündelte Drittanbieterhinweise; `licenses/SkiaSharp-HarfBuzzSharp/` |
| Avalonia ANGLE Windows Natives | 2.1.27548.20260419 | ANGLE-Lizenz und Texte für Chromium-Code, Abseil, ASTC Encoder, xxHash und zlib unter `licenses/Avalonia.Angle.Windows.Natives/` |
| MicroCom.Runtime | 0.11.6 | MIT; `licenses/MicroCom.Runtime/LICENSE` |
| Tmds.DBus.Protocol | 0.94.1 | MIT; `licenses/Tmds.DBus.Protocol/COPYING` |
| .NET-AppHost-Startvorlage aus dem SDK | 10.0.12, SDK 10.0.401 | MIT und paketweite Drittanbieterhinweise; `licenses/DotNet-AppHost/` |
| Microsoft.NETCore.App.Runtime.win-x64 | 10.0.12 | MIT und vollständige paketweite Drittanbieterhinweise; `licenses/DotNet-Runtime/` |

SkiaSharp und HarfBuzzSharp verwenden bytegleiche Paket-Lizenztexte und Drittanbieterhinweise. Deshalb liegt ein gemeinsamer Satz vor, der ausdrücklich beiden Paketfamilien zugeordnet ist. Native Debug-Symbole aus diesen Paketen sind ebenfalls in der technischen Inventur erfasst.

Avalonia.BuildServices 11.3.2 ist ein Build-Werkzeug und liegt nicht im geprüften Programmpaket. Weitere Plattformpakete im NuGet-Cache oder Restore-Graphen werden dadurch nicht automatisch zu ausgelieferten Dateien. Der Validator prüft deshalb zusätzlich das tatsächliche Publish-Verzeichnis.

## Herkunft und Abgleich

Die unveränderten Lizenztexte und die detaillierte ANGLE-Zuordnung stammen aus der bestehenden HappyPattern-Inventur. Für HappyColorSplit wurden Paketversionen, Repository-Revisionen, Paketarchive, enthaltene Dateien und deren SHA-256-Prüfsummen abgeglichen. Die Originaltexte werden über `.gitattributes` vor einer Änderung der Zeilenenden durch Git geschützt.

`licenses/component-inventory.json` enthält die Paket-/Dateizuordnung, offiziellen Repository-URLs und Revisionen sowie SHA-256-Prüfsummen der Paketarchive, 32 Anwendungs-Drittanbieterdateien, 187 Laufzeitdateien und 18 unveränderten Lizenz-/Notice-Texte einschließlich der Projektlizenz. Die darin erfassten Dateien wurden gegen den lokalen NuGet-Paketinhalt geprüft. Es wurde kein unabhängiger Neuaufbau der nativen Bibliotheken durchgeführt.

Zusätzlich enthält `HappyColorSplit.exe` die native .NET-AppHost-Startvorlage. Die installierte Vorlage wurde mit dem offiziellen NuGet-Paket der Version 10.0.12 bytegenau abgeglichen. Dessen MIT-Lizenz und paketweite Drittanbieterhinweise werden unverändert mitgeliefert. Die SDK-Version und der Hash der unveränderten Vorlage sind gesondert erfasst; die fertige Startdatei enthält zusätzlich vom SDK eingesetzte Anwendungsnamen und Ressourcen. Ein SDK-Wechsel erfordert daher ebenfalls eine neue Prüfung. Zusätzlich wird das offizielle NuGet-Laufzeitpaket `Microsoft.NETCore.App.Runtime.win-x64` 10.0.12 aus `api.nuget.org` mitgeliefert. Alle 187 verwendeten Laufzeitdateien wurden bytegenau mit dem Paketarchiv abgeglichen; Archiv- und Dateiprüfsummen stehen im Abschnitt `runtimePack` der Inventur. Dessen vollständige MIT-Lizenz und paketweite Drittanbieterhinweise bleiben unter `licenses/DotNet-Runtime/` erhalten. Laufzeitupdates erfordern einen neuen Paketabgleich und ein neues HappyColorSplit-ZIP.

## Abgrenzung

HappyColorSplit verwendet kein Poppler und enthält keine PDF-Verarbeitung. Die Poppler-spezifischen Unterlagen und Lizenztexte aus HappyPattern gehören daher nicht zu diesem Paket.

Die Projektlizenz `GPL-3.0-or-later` ändert die Lizenzen der Drittanbieter-Komponenten nicht. Bei einer Weitergabe bleiben außerdem die für HappyColorSplit geltenden GPL-Pflichten, einschließlich der anwendbaren Quellcodebereitstellung, zu beachten. Der passende Quellcode ist im öffentlichen Repository und als Quellcode-ZIP beim jeweiligen Release verfügbar. Bei einer Weitergabe muss der zur Programmversion gehörende Quellcode nach den anwendbaren Lizenzbedingungen zugänglich bleiben.

## Auslieferung und Prüfung

Die Datei `LICENSE`, der gesamte Ordner `licenses` und beide Komponentenübersichten werden automatisch in Build- und Publish-Ausgaben kopiert. Ein späterer Installer muss diese Dateien in zugänglicher Form erhalten.

Über **Lizenzen & Hinweise** in der Hauptansicht sind die Projektlizenz, diese Übersicht sowie alle Original-Lizenz- und Notice-Texte offline im Programm lesbar und kopierbar. Die Bedienung und die Übersicht sind auf Deutsch und Englisch verfügbar; die Originaltexte werden nicht übersetzt oder gekürzt.

```powershell
dotnet publish HappyColorSplit.csproj -p:PublishProfile=Windows -o artifacts/HappyColorSplit-Windows
pwsh -NoProfile -File tools/ValidateThirdPartyCompliance.ps1 -PublishDirectory artifacts/HappyColorSplit-Windows
```

Änderungen an Paketen, Plattform, Architektur, Bereitstellungsart, Trimming, Single-File-Einstellungen oder Installer-Inhalten erfordern eine erneute Bestandsaufnahme. Der Validator erkennt ausgewählte technische Abweichungen und fehlende Dateien; er bescheinigt keine rechtliche Vollständigkeit.
