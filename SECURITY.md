# Sicherheitsregeln / Security policy

## Projektstatus und unterstützte Versionen

HappyColorSplit ist ein öffentliches Open-Source-Projekt in der frühen Entwicklung. Der öffentlich sichtbare Maintainer-Name ist **HappyProjectLab**. Allgemeine Fragen werden über [GitHub Issues](https://github.com/HappyProjectLab/HappyColorSplit/issues) entgegengenommen; Sicherheitsmeldungen folgen dem getrennten Verfahren unten.

Sicherheitskorrekturen werden zunächst im aktuellen Entwicklungsstand auf `main` gepflegt. Für ältere Stände gibt es keine gesonderte Wartungszusage. Es bestehen keine garantierten Reaktions- oder Behebungsfristen. Das Windows-ZIP enthält seine eigene .NET-Laufzeit. Sicherheitsupdates dafür müssen über ein neues HappyColorSplit-Paket ausgeliefert werden; eine Aktualisierung der separat installierten .NET-Laufzeit ersetzt die gebündelten Dateien nicht.

## Sicherheitslücken vertraulich melden

**Vertrauliche Sicherheitsmeldungen über GitHub sind aktiviert.** Bitte den Bereich **Security → Advisories → Report a vulnerability** verwenden: [Sicherheitslücke vertraulich melden](https://github.com/HappyProjectLab/HappyColorSplit/security/advisories/new). Für die Meldung ist ein GitHub-Konto erforderlich. Meldungen über dieses Formular werden nicht als öffentliche Issues angelegt.

Eine separate Sicherheits-E-Mail-Adresse ist nicht eingerichtet. Keine vertraulichen Details, Ausnutzungsanleitungen oder privaten Dateien in öffentliche Issues, Diskussionen oder Pull Requests schreiben. Falls das Formular nicht erreichbar ist, kann öffentlich nach einem Kontaktweg gefragt werden, jedoch ohne vertrauliche oder ausnutzbare Details.

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

**GitHub Private Vulnerability Reporting is enabled.** Use **Security → Advisories → Report a vulnerability**, or [submit a confidential security report](https://github.com/HappyProjectLab/HappyColorSplit/security/advisories/new). Reporting requires a GitHub account. Reports submitted through this form are not created as public issues.

There is no separate security email address. Do not disclose exploit details or private files in public issues, discussions or pull requests. If the reporting form is unavailable, you may publicly request a contact channel without including sensitive or exploitable details.

Include the affected version, environment, impact and minimal reproduction steps. Prefer synthetic samples and remove personal data and secrets. Coordinate disclosure with maintainers. Reporting a vulnerability does not authorize testing third-party systems.
