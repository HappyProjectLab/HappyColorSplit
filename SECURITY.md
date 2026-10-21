# Sicherheitsregeln / Security policy

## Projektstatus und unterstützte Versionen

HappyColorSplit ist ein öffentliches Open-Source-Projekt in der frühen Entwicklung. Der öffentlich sichtbare Maintainer-Name ist **HappyProjectLab**. Allgemeine Fragen werden über [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues) entgegengenommen; Sicherheitsmeldungen folgen dem getrennten Verfahren unten.

Sicherheitskorrekturen werden zunächst im aktuellen Entwicklungsstand auf `main` gepflegt. Für ältere Stände gibt es keine gesonderte Wartungszusage. Es bestehen keine garantierten Reaktions- oder Behebungsfristen. Das Windows-ZIP enthält seine eigene .NET-Laufzeit. Sicherheitsupdates dafür müssen über ein neues HappyColorSplit-Paket ausgeliefert werden; eine Aktualisierung der separat installierten .NET-Laufzeit ersetzt die gebündelten Dateien nicht.

## Sicherheitslücken vertraulich melden

**Derzeit ist kein öffentlich nutzbarer vertraulicher Meldeweg bestätigt.** Eine Sicherheits-E-Mail-Adresse ist nicht eingerichtet. Bei der öffentlichen Freigabe müssen die Verantwortlichen GitHubs **Private Vulnerability Reporting** aktivieren und den Zugang für Meldende prüfen. Bis zur Bestätigung gilt der nachfolgende Hinweis zur noch fehlenden Verfügbarkeit.

GitHub bietet diese Funktion für öffentliche Repositorys an. Sie kann daher erst nach dem Wechsel der Repository-Sichtbarkeit aktiviert werden. Siehe [GitHub-Dokumentation](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository).

Sobald diese Funktion verfügbar ist, bitte über den Bereich **Security → Report a vulnerability** des [Repositorys](https://github.com/HappyProjectLab/HappyColorSplit) melden: [vertrauliche Sicherheitsmeldung](https://github.com/HappyProjectLab/HappyColorSplit/security/advisories/new). Der Link ist erst nach entsprechender Freischaltung als Meldeweg nutzbar.

Solange dieser Weg fehlt, keine vertraulichen Details in öffentliche Issues, Diskussionen oder Pull Requests schreiben. Wer bereits einen direkten Kontakt zu den Projektverantwortlichen hat, kann darüber zunächst einen geeigneten vertraulichen Austausch vereinbaren. Andernfalls bitte mit technischen Details warten, bis ein vertraulicher Meldeweg bereitsteht. Eine öffentliche Anfrage nach einem Meldeweg darf keine ausnutzbaren Details enthalten.

Eine hilfreiche vertrauliche Meldung enthält:

- betroffene Version oder Commit, Windows-Version und .NET-Version;
- kurze Beschreibung der möglichen Auswirkung und Voraussetzungen;
- minimale Schritte zur Reproduktion und gegebenenfalls relevante, bereinigte Fehlermeldungen;
- möglichst eine kleine synthetische Beispieldatei statt privater Originalbilder.

Keine Zugangsdaten, personenbezogenen Daten oder fremden vertraulichen Bilder beifügen. Mit einer Meldung ist keine Erlaubnis verbunden, fremde Systeme zu testen. Bitte eine Veröffentlichung von Ausnutzungsdetails mit den Verantwortlichen abstimmen.

## Was als Sicherheitsproblem relevant ist

Relevant sind insbesondere Schwachstellen beim Decodieren von PNG/JPG, der Behandlung manipulierter Dateien, Ressourcenbegrenzungen, Dateipfaden und Schreibzugriffen sowie in mitgelieferten Abhängigkeiten. Auch unbeabsichtigtes Überschreiben oder Löschen fremder Dateien und unbeabsichtigte Offenlegung privater Daten gehören dazu.

Normale Abweichungen bei Farbgruppen, Maskenkanten, Übersetzungen oder Bedienung können als gewöhnliche Fehler gemeldet werden, sofern keine Sicherheitsauswirkung vorliegt. Die Anwendung verarbeitet Bilder lokal; die Größe eines Bildes allein erlaubt keine Aussage darüber, ob dessen Verarbeitung sicher ist. Vorhandene Verarbeitungsgrenzen ersetzen keine Prüfung der Decoder und Abhängigkeiten.

## Umgang mit bestätigten Problemen

Die Verantwortlichen prüfen Meldungen nach Möglichkeit, klären Auswirkungen und betroffene Stände und stellen eine Korrektur bereit. Sicherheitsrelevante Änderungen sollen passende Regressionstests erhalten. Abhängigkeiten und deren Lizenzinventar werden bei Updates gemeinsam geprüft. Hinweise zu einer Korrektur sollen betroffene Versionen und verfügbare Abhilfe nennen, ohne unnötig private Daten offenzulegen.

## English

HappyColorSplit is a public open-source project in early development. The public maintainer name is **HappyProjectLab**. General questions belong in [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues); security reports follow the separate process below. Fixes target the current `main` branch; there is no separate maintenance commitment for older versions or guaranteed response time.

**No publicly accessible confidential reporting channel has been confirmed yet.** At public launch, maintainers must enable and verify GitHub Private Vulnerability Reporting. Until availability is confirmed, the instructions below apply. Once enabled, use [Report a vulnerability](https://github.com/HappyProjectLab/HappyColorSplit/security/advisories/new). Until then, do not disclose exploit details or private files in public issues or pull requests. If you already have a direct maintainer contact, arrange a confidential channel first; otherwise wait for one to become available.

GitHub provides this feature for public repositories. Enable it after changing repository visibility; see [GitHub's documentation](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/configure-vulnerability-reporting/configure-for-a-repository).

Include the affected version, environment, impact and minimal reproduction steps. Prefer synthetic samples and remove personal data and secrets. Coordinate disclosure with maintainers. Reporting a vulnerability does not authorize testing third-party systems.
