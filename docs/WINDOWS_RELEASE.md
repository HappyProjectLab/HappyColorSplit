# Windows-Download mit integrierter Laufzeit

HappyColorSplit **0.1.0-preview.3** wird als Windows-x64-ZIP mit integrierter .NET-Laufzeit **10.0.12** bereitgestellt. Der öffentlich sichtbare Maintainer-Name ist **HappyProjectLab**; allgemeiner Kontakt erfolgt über [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues).

## Für Anwender

Das Windows-ZIP vollständig entpacken und `HappyColorSplit/HappyColorSplit.exe` starten. Eine separate .NET-Installation ist nicht erforderlich. Alle Dateien und Unterordner zusammen behalten. Das Paket enthält keine privaten Bilder oder Benutzereinstellungen.

Der direkte Download-Link steht oben in der [README](../README.md). Er verweist auf die konkrete Vorabversion, nicht auf GitHubs neuestes stabiles Release.

## Datensicherheit und Haftung

Die Hinweise in [USAGE_NOTICE_DE.md](USAGE_NOTICE_DE.md) und [USAGE_NOTICE.md](USAGE_NOTICE.md) werden unverändert in das Paket kopiert. Das Programm zeigt im Fenster **Lizenzen & Hinweise** die zur Oberflächensprache passende Fassung. `START_HERE.txt` und README enthalten beide Fassungen. Die GPL-Originaltexte bleiben unverändert.

## Release erstellen

Voraussetzungen: Windows x64, Git, .NET SDK 10.0.401 und PowerShell 7. Laufzeit und Komponenten müssen zur [Inventur](../licenses/component-inventory.json) passen.

Im Projektordner ausführen:

```powershell
dotnet build HappyColorSplit.slnx -c Release
dotnet run --project Tests/HappyColorSplit.Tests.csproj
pwsh -NoProfile -File tools/New-WindowsRelease.ps1
```

Das Release-Skript akzeptiert einen sauberen Git-Stand und ein Ausgabeverzeichnis innerhalb des Projekts. Es baut mit dem Profil `Windows`, kontrolliert die binäre Commit-Kennung und führt die Komponentenprüfung aus. Unter `artifacts/releases` entstehen:

- `HappyColorSplit-0.1.0-preview.3-Windows-x64.zip`: Programm, Laufzeit, Lizenzunterlagen, Startanleitung und Quellcodeverweis;
- `HappyColorSplit-0.1.0-preview.3-Source.zip`: versionierte Projektdateien desselben Commits, einschließlich Build- und Release-Anleitung;
- `HappyColorSplit-0.1.0-preview.3-SHA256SUMS.txt`: SHA-256-Prüfsummen beider ZIP-Dateien.

Die Inhalte des fertigen Windows-ZIPs werden bytegenau mit dem geprüften Verzeichnis verglichen. Vorhandene Release-Dateien werden nicht überschrieben. Für einen weiteren Build ein anderes Verzeichnis über `-OutputDirectory artifacts/releases-test` angeben. `-NoRestore` setzt passend wiederhergestellte Pakete voraus.

Alle drei Dateien gemeinsam an ein GitHub-Release mit Tag `v0.1.0-preview.3` hängen. Der Tag muss denselben Commit bezeichnen. Diese Ausgabe als **Pre-release** kennzeichnen. Weitere Versionen erhalten neue Versionsangaben, Dateien und README-Links. Das Skript lädt selbst nichts auf GitHub hoch und ändert keine Repository-Sichtbarkeit.

## Komponentenprüfung und Updates

Das Profil ist self-contained, ohne Trimming, ReadyToRun oder Single-File. Die 187 ausgelieferten Dateien des offiziellen .NET-Laufzeitpakets sind zusätzlich zu den 32 Anwendungs-Drittanbieterdateien inventarisiert. Originaltexte, Archiv- und Dateiprüfsummen sowie Paket-/Quellrevisionen werden geprüft. Die vollständigen Laufzeithinweise liegen unter `licenses/DotNet-Runtime/`.

Bei Änderungen an Laufzeit, Abhängigkeiten oder Zielplattform erneut abgleichen. Eine systemweit aktualisierte .NET-Laufzeit ersetzt die gebündelte Laufzeit nicht; Sicherheitskorrekturen benötigen ein neues HappyColorSplit-Paket. Technische Prüfungen bescheinigen keine rechtliche Vollständigkeit oder Fehlerfreiheit auf jedem Windows-System.

## Öffentliche Freigabe und Sicherheitsmeldungen

Die Klärung gegebenenfalls erforderlicher Anbieterangaben steht noch aus. Der sichtbare Maintainer-Name allein ersetzt diese Angaben nicht.

GitHubs Private Vulnerability Reporting ist noch nicht aktiviert und steht erst bei öffentlichen Repositorys zur Verfügung. Nach ausdrücklich freigegebener Umstellung aktivieren und prüfen:

```powershell
gh api --method PUT repos/HappyProjectLab/HappyColorSplit/private-vulnerability-reporting
gh api repos/HappyProjectLab/HappyColorSplit/private-vulnerability-reporting --jq .enabled
```

Die Abfrage muss `true` liefern. Zusätzlich den Meldeweg **Security → Advisories → Report a vulnerability** mit einem geeigneten Konto ohne Maintainer-Rechte prüfen, ohne eine erfundene Meldung abzusenden. Danach den Status in [SECURITY.md](../SECURITY.md) aktualisieren. Normale Fragen und Fehler gehören in GitHub Issues; vertrauliche Details gehören nicht dorthin.
