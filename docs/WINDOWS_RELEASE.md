# Windows-Download mit integrierter Laufzeit

HappyColorSplit **0.1.0-preview.6** wird als Windows-x64-ZIP mit integrierter .NET-Laufzeit **10.0.12** bereitgestellt. Der öffentlich sichtbare Maintainer-Name ist **HappyProjectLab**; allgemeiner Kontakt erfolgt über [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues).

## Für Anwender

Das Windows-ZIP vollständig entpacken und `HappyColorSplit/HappyColorSplit.exe` starten. Eine separate .NET-Installation ist nicht erforderlich. Alle Dateien und Unterordner zusammen behalten. Das Paket enthält keine privaten Bilder oder Benutzereinstellungen.

Der direkte Download-Link steht oben in der [README](../README.md). Er verweist auf die konkrete Vorabversion, nicht auf GitHubs neuestes stabiles Release.

### Defender SmartScreen und Dateiblockierung

Nach Download und Entpacken muss `HappyColorSplit.exe` gegebenenfalls noch ausdrücklich zugelassen werden. Windows kann heruntergeladene Dateien kennzeichnen; Defender SmartScreen kann beim Start einer unbekannten App „Der Computer wurde durch Windows geschützt“ anzeigen.

Nur fortfahren, wenn das Paket aus dem offiziellen [GitHub-Release](https://github.com/HappyProjectLab/HappyColorSplit/releases/tag/v0.1.0-preview.6) stammt und du die Herkunft geprüft hast. Die dort angebotenen SHA-256-Prüfsummen ermöglichen zusätzlich einen Vergleich der heruntergeladenen Dateien.

1. Zeigt SmartScreen eine Warnung wegen einer unbekannten App, **Weitere Informationen** und anschließend **Trotzdem ausführen** wählen, sofern angeboten.
2. Bei einer blockierten Datei im Explorer mit der rechten Maustaste auf `HappyColorSplit.exe` klicken und **Eigenschaften → Allgemein** öffnen. Falls unten im Bereich **Sicherheit** die Option **Zulassen** erscheint, diese aktivieren und **Übernehmen** wählen. Danach erneut starten.
3. Alternativ bereits die heruntergeladene ZIP über **Eigenschaften → Allgemein → Sicherheit → Zulassen** freigeben und danach vollständig neu entpacken. Die EXE und alle mitgelieferten Dateien zusammen behalten.

Die Optionen erscheinen nicht auf jedem System. Bei administrativ verwalteten Geräten kann die Freigabe gesperrt sein. Eine konkrete Malware-Erkennung ist von einer SmartScreen-Warnung wegen fehlender Bekanntheit zu unterscheiden; in diesem Fall nicht auf diese Weise fortfahren. Den Virenschutz oder SmartScreen nicht generell abschalten. Das Zulassen einer Datei bestätigt nicht ihre Sicherheit.

**English:** After downloading and extracting the ZIP, Windows may block the executable or display “Windows protected your PC”. If you have verified that the package came from the official release, an unknown-app SmartScreen prompt may offer **More info → Run anyway**. For a downloaded-file block, right-click `HappyColorSplit.exe`, open **Properties → General**, select **Unblock** if available and click **Apply**. Alternatively, unblock the downloaded ZIP before extracting it again. These options may be unavailable on managed devices. Do not use these steps to bypass a malware detection or an administrator's policy, and do not disable antivirus protection or SmartScreen globally. Unblocking a file is not a safety guarantee.

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

- `HappyColorSplit-0.1.0-preview.6-Windows-x64.zip`: Programm, Laufzeit, Lizenzunterlagen, Startanleitung und Quellcodeverweis;
- `HappyColorSplit-0.1.0-preview.6-Source.zip`: versionierte Projektdateien desselben Commits, einschließlich Build- und Release-Anleitung;
- `HappyColorSplit-0.1.0-preview.6-SHA256SUMS.txt`: SHA-256-Prüfsummen beider ZIP-Dateien.

Die Inhalte des fertigen Windows-ZIPs werden bytegenau mit dem geprüften Verzeichnis verglichen. Vorhandene Release-Dateien werden nicht überschrieben. Für einen weiteren Build ein anderes Verzeichnis über `-OutputDirectory artifacts/releases-test` angeben. `-NoRestore` setzt passend wiederhergestellte Pakete voraus.

Alle drei Dateien gemeinsam an ein GitHub-Release mit Tag `v0.1.0-preview.6` hängen. Der Tag muss denselben Commit bezeichnen. Diese Ausgabe als **Pre-release** kennzeichnen. Weitere Versionen erhalten neue Versionsangaben, Dateien und README-Links. Das Skript lädt selbst nichts auf GitHub hoch und ändert keine Repository-Sichtbarkeit.

## Komponentenprüfung und Updates

Das Profil ist self-contained, ohne Trimming, ReadyToRun oder Single-File. Die 187 ausgelieferten Dateien des offiziellen .NET-Laufzeitpakets sind zusätzlich zu den 32 Anwendungs-Drittanbieterdateien inventarisiert. Originaltexte, Archiv- und Dateiprüfsummen sowie Paket-/Quellrevisionen werden geprüft. Die vollständigen Laufzeithinweise liegen unter `licenses/DotNet-Runtime/`.

Bei Änderungen an Laufzeit, Abhängigkeiten oder Zielplattform erneut abgleichen. Eine systemweit aktualisierte .NET-Laufzeit ersetzt die gebündelte Laufzeit nicht; Sicherheitskorrekturen benötigen ein neues HappyColorSplit-Paket. Technische Prüfungen bescheinigen keine rechtliche Vollständigkeit oder Fehlerfreiheit auf jedem Windows-System.

## Öffentliche Freigabe und Sicherheitsmeldungen

Die Klärung gegebenenfalls erforderlicher Anbieterangaben steht noch aus. Der sichtbare Maintainer-Name allein ersetzt diese Angaben nicht.

Das Repository wurde am 21. Oktober 2026 vom Projektverantwortlichen öffentlich geschaltet. GitHubs Private Vulnerability Reporting ist aktiviert; die API-Abfrage liefert `enabled: true`. Der Meldeweg ist in [SECURITY.md](../SECURITY.md) auf Deutsch und Englisch beschrieben. Ein GitHub-Konto ist zum Einreichen erforderlich. Den Zustand kann man erneut abfragen:

```powershell
gh api repos/HappyProjectLab/HappyColorSplit/private-vulnerability-reporting --jq .enabled
```

Die Abfrage muss `true` liefern. Bei Änderungen an dieser Einstellung auch `SECURITY.md` aktualisieren. Keine erfundenen Sicherheitsmeldungen zum Test absenden. Normale Fragen und Fehler gehören in GitHub Issues; vertrauliche Details gehören in das gesonderte Meldeformular.
