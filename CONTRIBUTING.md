# Mitwirken / Contributing

Danke für dein Interesse an HappyColorSplit. Diese Regeln gelten für Fehlerberichte, Vorschläge, Dokumentation und Änderungen am Programm. Das Projekt wird öffentlich als Open Source entwickelt. Der öffentlich sichtbare Maintainer-Name ist **HappyProjectLab**. Allgemeine Fragen, Fehlerberichte und Vorschläge bitte über [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues) einreichen; dafür ist ein GitHub-Konto erforderlich. Issues sind öffentlich einsehbar.

## Fehler und Vorschläge

Vor einer neuen Meldung bitte die [vorhandenen Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues) prüfen. Beschreibe das erwartete und tatsächliche Verhalten, die verwendete Programmversion, Windows-Version und die Schritte zur Reproduktion. Bei Bildproblemen gehören Farbähnlichkeit, Mindestdeckkraft und gewählte Farbgruppen dazu. Größere Funktionsänderungen bitte zunächst in einem Issue abstimmen.

**Sicherheitslücken folgen ausschließlich dem vertraulichen Verfahren in [SECURITY.md](SECURITY.md).** Keine Ausnutzungsdetails oder vertraulichen Beispieldateien in öffentliche Meldungen aufnehmen.

Für Beispiele und Screenshots nur Material verwenden, das du für diesen Zweck teilen darfst. Kleine synthetische Testbilder sind vorzuziehen. Entferne personenbezogene Daten, unnötige Metadaten und private Dateipfade. Keine Zugangsdaten, privaten Originalbilder, Benutzereinstellungen oder erzeugten Exportordner einchecken. Die öffentliche Git-Historie kann eingecheckte Daten dauerhaft enthalten, auch wenn sie später aus aktuellen Dateien entfernt werden.

## Lokal entwickeln

Voraussetzung sind Windows und das .NET SDK 10. Die Anwendung verwendet C#, Avalonia 12.1.1 mit AXAML und SkiaSharp. Nach dem Klonen im Projektordner ausführen:

```powershell
dotnet restore HappyColorSplit.slnx
dotnet build HappyColorSplit.slnx -c Release
dotnet run --project HappyColorSplit.csproj
```

Die Funktionsprüfungen sind ein eigenes ausführbares Projekt:

```powershell
dotnet run --project Tests/HappyColorSplit.Tests.csproj
```

Die regulären Prüfungen verwenden synthetische Bilder. Zusätzliche lokale Bildvergleiche sind optional; private Beispieldateien aus `../Import` und erzeugte Referenzdateien gehören nicht ins Repository. Referenzwerte nur nach fachlicher Prüfung einer beabsichtigten Verhaltensänderung aktualisieren.

## Änderungen am Programm

- Kleine, nachvollziehbare Änderungen mit einem klaren Zweck bevorzugen; unabhängige Aufräumarbeiten getrennt halten.
- Oberfläche in `Views` und `Controls`, Zustand und Befehle in `ViewModels`, Daten in `Models` und Fachlogik in `Services` pflegen. Aufwendige Bildverarbeitung darf die Oberfläche nicht blockieren.
- Oberflächentexte zentral in `Services/Texts.cs` für Deutsch und Englisch ergänzen. Beide Sprachen und lange Texte bei Änderungen an der Oberfläche prüfen.
- Originalgröße und Position der Exportmasken, weiße Flächen auf schwarzem Hintergrund, Deckkraftfilter und Farbauswahl erhalten, sofern die Änderung nicht ausdrücklich anderes vorsieht. Auswirkungen auf Stapelverarbeitung und Abbruch berücksichtigen.
- Keine unnötigen Netzwerkzugriffe oder zusätzlichen Bibliotheken einführen. Grafik- und PDF-Bibliotheken nur ergänzen, wenn eine vereinbarte Funktion sie benötigt.
- Passende Prüfungen für geändertes Verhalten oder behobene Fehler ergänzen. Bei reinen Textänderungen reicht eine angemessene Prüfung der Texte und Verweise.

## Abhängigkeiten, Lizenzen und Windows-Pakete

Beiträge müssen unter [GPL-3.0-or-later](LICENSE) verwendbar sein. Mit dem Einreichen bestätigst du, dass du die nötigen Rechte an deinem Beitrag hast und ihn unter dieser Projektlizenz bereitstellst. Eine Übertragung deines Urheberrechts ist damit nicht verbunden. Fremder Code und andere übernommene Inhalte benötigen eine dokumentierte Herkunft und mit dem Projekt verträgliche Lizenzbedingungen.

NuGet-Abhängigkeiten mit festen Versionen einbinden. Bei neuen oder aktualisierten Komponenten auch transitive und native Bestandteile prüfen und das Inventar unter `licenses/component-inventory.json`, die Komponentenübersichten sowie die Hinweise und Original-Lizenztexte aktualisieren. Originaltexte unverändert aus nachvollziehbaren Quellen übernehmen. Prüfsummen nur nach Prüfung der tatsächlichen Quelle und Paketänderung aktualisieren.

Bei Änderungen an Abhängigkeiten, Lizenzunterlagen oder Paketierung zusätzlich das Windows-Paket erstellen und prüfen:

```powershell
dotnet publish HappyColorSplit.csproj -p:PublishProfile=Windows -o artifacts/HappyColorSplit-Windows
pwsh -NoProfile -File tools/ValidateThirdPartyCompliance.ps1 -PublishDirectory artifacts/HappyColorSplit-Windows
```

Das bestehende Profil erzeugt ein Windows-x64-Paket mit integrierter .NET-Laufzeit 10.0.12. Änderungen an der Laufzeit oder Zielplattform erfordern eine erneute Komponenten- und Lizenzprüfung. Details stehen in der [Komponentenübersicht](docs/THIRD_PARTY_COMPONENTS_DE.md). `tools/New-WindowsRelease.ps1` erstellt aus einem sauberen Git-Stand zusätzlich Windows-ZIP, passenden Quellcode und Prüfsummen.

## Pull Requests und Zusammenarbeit

Änderungen in einem eigenen Branch vorbereiten. Der Pull Request soll das Problem, die resultierende Änderung, relevante Prüfungen und bekannte Einschränkungen beschreiben. Build und die für die Änderung relevanten Prüfungen müssen erfolgreich sein; nicht durchführbare Prüfungen offen benennen. Generierte Dateien aus `bin`, `obj` und `artifacts` nicht aufnehmen.

Bitte sachlich und respektvoll miteinander umgehen. Kritik soll sich auf die Sache beziehen. Keine Belästigungen oder Veröffentlichung personenbezogener Daten. Die Verantwortlichen prüfen Beiträge und entscheiden über ihre Aufnahme; eine Bearbeitungsfrist wird nicht zugesagt.

## English

HappyColorSplit is developed publicly as open source. The public maintainer name is **HappyProjectLab**. Use [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues) for general questions, bug reports and suggestions; submitting an issue requires a GitHub account. Issues are publicly visible. Report reproducible bugs with version, environment and processing settings. Discuss larger changes first. Follow [SECURITY.md](SECURITY.md) for vulnerabilities.

Use only samples you may share, preferably synthetic images, and remove personal data and secrets. Keep changes focused, respect the MVVM structure, update both German and English UI texts, and run the build and relevant functional checks above. Preserve mask export semantics unless a change is explicitly intended. Dependency and packaging changes also require the Windows publish and compliance checks, including updated component inventories and unmodified upstream license texts.

By submitting a contribution, you confirm that you have the necessary rights and provide it under GPL-3.0-or-later; copyright ownership is not transferred. Document the origin and compatible license terms of third-party material. Describe the problem, resulting behavior, validation and limitations in your pull request. Be respectful; maintainers decide whether to accept contributions and do not promise a review deadline.
