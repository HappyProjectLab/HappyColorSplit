# Geführte Einführung / Guided introduction

Die Einführung ist ab **1.0.0** enthalten. Sie erklärt die Farbauszüge in 14 Schritten direkt im Hauptfenster. Der Laser-Bereich wird als experimentelle Zusatzfunktion vorgestellt.

## Deutsch

Beim ersten Start erscheint rechts unten ein Angebot zur Einführung. Erst **Einführung starten** beginnt die Schritte und hebt die erste Funktion hervor. Das Angebot ändert noch keinen Arbeitsbereich.

- **Weiter / Zurück:** Zwischen den Erklärungen wechseln. Im letzten Schritt schließt **Fertig** die Einführung ab; der Abschluss wird gespeichert und die Einführung danach nicht mehr automatisch angeboten.
- **Überspringen:** Die Einführung für die aktuelle Sitzung beenden. Beim nächsten Programmstart wird sie erneut angeboten, solange sie nicht abgeschlossen oder dauerhaft abgelehnt wurde.
- **Keine Tipps notwendig:** Die Ablehnung speichern und automatische Angebote unterdrücken. Das ist auch schon im Begrüßungsangebot möglich.
- **Anleitung:** Über diesen Button oben im Hauptfenster jederzeit erneut starten, auch nach Abschluss oder Ablehnung. Ein manueller Aufruf hebt eine gespeicherte Ablehnung nicht auf.

Ein heller Rahmen kennzeichnet die erklärte Funktion; die Umgebung wird leicht abgedunkelt. Der Rahmen fängt keine Eingaben ab. Du kannst Bilder laden, Farben erkennen und die beschriebenen Funktionen ausprobieren. Die Navigation pausiert während einer laufenden Verarbeitung und wird danach wieder bedienbar. Die Einführung lädt oder exportiert selbst keine Dateien.

Die Steuerung steht grundsätzlich rechts unten. Würde die Karte das hervorgehobene Ziel verdecken, rückt sie rechts nach oben, soweit dort genügend Platz ist. Die Markierung folgt der Fenstergröße, Trennlinien und dem aktuellen Layout. Sie arbeitet ohne Blink- oder Bewegungsanimationen.

Die Einführung beginnt in den Farbauszügen und öffnet benötigte Seitenleisten. Beim Beenden werden der vorherige Modus und die vorherigen Leistenzustände wiederhergestellt. Export-Häkchen, ausgewählte Farbgruppe, Vorschauzoom, Farbregler und Bilddaten werden durch das Weiter- oder Zurückgehen nicht verändert. Änderungen, die du selbst an den beschriebenen Funktionen vornimmst, bleiben bestehen.

Fehlt ein Bild für **Bildgröße**, zeigt die Einführung stattdessen **Bilder hinzufügen** und erklärt die Voraussetzung. Wenn du während der Einführung zu Laser wechselst oder eine benötigte Seitenleiste einklappst, weist sie auf den nötigen Rückweg hin. Du kannst den Schritt trotzdem überspringen, indem du weitergehst.

Die 14 Themen sind: Bilder hinzufügen, Bilderliste, Farbähnlichkeit, Mindestdeckkraft, Farberkennung, Originalvorschau, Farbgruppen und Export-Häkchen, Ausgabedarstellung, Bildgröße, Exportvorschau, Exportarten, Vorschau-Anordnung, experimenteller Laser-Bereich sowie Version und Updates.

Die Sprache kann während der Einführung zwischen Deutsch und Englisch gewechselt werden; der aktuelle Schritt bleibt erhalten. Abschluss und Ablehnung werden zusammen mit den übrigen Programmeinstellungen im Benutzerprofil gespeichert. Vorhandene Einstellungsdateien ohne diese Angaben erhalten das Erststart-Angebot.

## English

On first launch, a welcome card offers a 14-step introduction. **Start introduction** begins the steps; the welcome offer itself does not change the workspace.

**Next / Back** navigate the explanations. **Finish** records completion and stops future automatic offers. **Skip** closes the introduction for this session; it can be offered on the next launch. **No tips needed** permanently disables automatic offers. The **Guide** button at the top always lets you start again, including after declining or completing the introduction.

A bright outline points out each feature without intercepting input. You can use the app while following the guide. Guide navigation pauses during processing. The guide itself does not load or export files or change image data, export selections or zoom. It opens needed sidebars and starts in Color layers; closing it restores your previous sidebar states and workspace mode. Any changes you make yourself while trying features remain.

The guide normally sits at the lower right and moves upward when needed to avoid a highlighted target. Missing prerequisites, hidden sidebars and a switch to Laser are explained. You can switch the interface language without losing the current step. The Laser workspace remains experimental.

## Prüfungen

Die automatischen Prüfungen decken Erststart, Weiter/Zurück, Überspringen, Abschluss, dauerhafte Ablehnung bereits vor dem Start und manuellen Wiederaufruf ab. Sie prüfen alle Schritte auf Deutsch und Englisch bei kleiner Fenstergröße, Eingaben durch die Markierung hindurch, Verarbeitungspausen, fehlende Bilder, eingeklappte Leisten und echte erkannte Bildinhalte. Vorheriger Modus und Leisten sowie unveränderte Exportauswahl und Zoom werden kontrolliert.

```powershell
dotnet run --project Tests/HappyColorSplit.Tests.csproj -- --tutorial-only
```
