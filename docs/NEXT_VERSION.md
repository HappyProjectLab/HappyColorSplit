# Umgesetzt für 1.0.0

Stand: 28.10.2026. Erste stabile Ausgabe der Farbauszüge; der Laser-Bereich bleibt ausdrücklich experimentell.

- [x] Geführte Einführung mit 14 Schritten, Funktionsrahmen und bedienbarem Arbeitsbereich. Deutsch und Englisch; Erststart-Angebot, gespeicherte Ablehnung und jederzeitiger Wiederaufruf.
- [x] Weiter, Zurück, Überspringen und Keine Tipps notwendig; Abschluss mit Fertig. Voraussetzungen und ausgeblendete Bereiche werden erklärt; vorheriger Modus und Leisten werden wiederhergestellt.
- [x] Stabiles Windows-ZIP mit festem Dateinamen für GitHubs Latest-Download. Versions- und Release-Unterlagen auf 1.0.0 aktualisiert.

## Umgesetzt für 0.1.0-preview.7

Stand: 28.10.2026.

- [x] Doppelklick auf eine Farbe in der Farbliste aktiviert dieselbe Hervorhebung im Originalbild und dieselbe Einzelvorschau wie ein Linksklick im Originalbild. Die kombinierte Vorschau wird ausgeschaltet; Export-Häkchen und Zoom bleiben erhalten. Doppelklicks direkt auf Häkchen behalten deren normale Bedienung.

## Umgesetzt für 0.1.0-preview.6

Stand: 27.10.2026. Diese Ausgabe enthält die Farbwahl direkt im Originalbild, die Versionsanzeige und die GitHub-Update-Prüfung. Offene Wünsche für spätere Versionen stehen in [FEATURE_REQUESTS.md](FEATURE_REQUESTS.md).

- [x] Linksklick auf eine erkannte Farbfläche wählt die Gruppe aus und zeigt ihre Einzelvorschau. Alle zugehörigen Flächen werden positionsgetreu mit kräftigerer Lichtkante und weichem Schatten hervorgehoben; Übergänge dauern etwa 120 ms.
- [x] Rechtsklick führt dieselbe Auswahl aus und kehrt zusätzlich den Export-Haken um. Farbzeile und Haken bestätigen die Änderung durch kurzes Aufleuchten; die Farbleiste öffnet sich bei Bedarf.
- [x] Aktuelle Version unter dem Titel anzeigen. Beim Start GitHub prüfen und einen Update-Button mit neuer Version, Veröffentlichungsdatum und Lauflicht ausschließlich bei einem passenden, herkunftsgeprüften Update anbieten.
- [x] Release-Seite erst nach Bestätigung und erneuter Herkunftsprüfung im Standardbrowser öffnen. Keine automatische Installation. Feste IDs von Repository, Organisation und Release-Autor schützen gegen gleichnamige Nachfolger.
- [x] README, deutsche und englische Bedienhinweise und Sicherheitsdokumentation aktualisieren; Bedienung, Herkunftsprüfung, Offline-Verhalten und Versionsvergleich durch Regressionstests absichern.

## Bereits in 0.1.0-preview.5 enthalten

Stand: 26.10.2026. Die abgehakten Punkte sind Bestandteil von `0.1.0-preview.5`. Offene Wünsche für spätere Versionen stehen in [FEATURE_REQUESTS.md](FEATURE_REQUESTS.md).

## Für diesen Release umgesetzt

- [x] Die **Laser-Funktion deutlich als experimentell kennzeichnen**: im Modus-Umschaltbutton **„Farbauszüge ⇄ Laser (experimentell)“** anzeigen und im geöffneten Laser-Arbeitsbereich einen gut sichtbaren Hinweis **„Experimentell“** platzieren. Die Kennzeichnung darf nicht nur in einem Tooltip stehen. Englische Entsprechung: **„Laser (experimental)“** beziehungsweise **„Experimental“**. Auch die Beschreibung der Laser-Funktion in README und Anleitung entsprechend kennzeichnen. Bei kleiner Fenstergröße sicherstellen, dass die Kennzeichnung lesbar bleibt.

- [x] Das bisherige **Lauflicht beim Aufruf von „Bilder hinzufügen“** entfernen und die Aktivitätsanzeige stattdessen dezent **über den gesamten Fensterhintergrund** laufen lassen. Gemeint ist die Hintergrundfläche des Programms, nicht nur eine Fortschrittslinie oder der Button. Text und Bedienelemente müssen gut lesbar bleiben; der Effekt darf keine Eingaben abfangen. Beim Schließen des Dateidialogs beziehungsweise Abschluss des Imports wieder beenden und die vorhandene Einstellung für reduzierte Bewegung berücksichtigen. Das separate Lauflicht der eingeklappten Seitenleisten bleibt erhalten.
- [x] Den zusätzlich eingeblendeten **Abbrechen-Button im Hauptfenster entfernen**. Beim geöffneten Dateidialog ist das Hauptfenster gesperrt, weshalb dieser Knopf dort nicht bedienbar ist. Das reguläre Abbrechen im Dateiauswahldialog bleibt möglich; die interne Abbruch- und Aufräumlogik nicht entfernen.

- [x] **Windows-Einbindung als eigenes Fenster** statt als aufklappendes Menü gestalten, passend zu **Lizenzen & Hinweise**. Der vorhandene Button öffnet das Fenster und bleibt währenddessen heller hervorgehoben; das Menüsymbol durch ein Symbol für das Öffnen eines Fensters ersetzen. Im Fenster die Erklärung sowie die bestehenden Aktionen zum Einrichten und Entfernen von „Öffnen mit“ und „Senden an“ übersichtlich darstellen und einen Schließen-Button anbieten. Die bisherigen Aktionen bleiben ausdrücklich vom Benutzer ausgelöst. Deutsch und Englisch berücksichtigen.

- [x] Texte und Symbole der neuen Buttons **vertikal zentrieren**, einschließlich der Umschalter mit zwei Varianten und Wechselpfeil sowie Windows-Einbindung, Lizenzen und Ansichtsbuttons. Zuerst die Inhaltsausrichtung korrigieren; danach prüfen, ob eine etwas kleinere gemeinsame Buttonhöhe unnötigen Leerraum reduziert. Alle betroffenen Buttons sollen weiterhin gleich hoch, gut lesbar und bequem anklickbar sein. Darstellung in beiden Sprachen und bei kleiner Fenstergröße kontrollieren.

- [x] Die Beschreibungen unter **Farbähnlichkeit** und **Mindestdeckkraft** neu anordnen. Direkt unter jedem Regler stehen die Beschreibungen seiner niedrigen und hohen Einstellung links- beziehungsweise rechtsbündig. Die Zusammenfassung der gesamten Funktion steht mit erkennbarem Abstand **eine Zeile darunter, zentriert und fett**. Sie darf optisch nicht wie die Beschriftung einer mittleren Reglerposition wirken. Name und aktueller Zahlenwert bleiben oberhalb des Reglers.
  - **Farbähnlichkeit:** links „Fein trennen“, rechts „Stärker zusammenfassen“; darunter mittig und fett „Ähnliche Farben gruppieren“.
  - **Mindestdeckkraft:** links „Alle sichtbaren Pixel“, rechts „Nur fast deckende Pixel“; darunter mittig und fett „Schwache Pixel ausfiltern“.
  - Bei der Umsetzung passende englische Texte zentral ergänzen und die Darstellung bei kleiner Fenstergröße prüfen.

## Weitere Änderungen dieses Releases

- [x] Den Menüknopf **Ansicht** aus der Oberfläche entfernen.
- [x] Den gemeinsamen Leistenknopf so ändern, dass er **beide** Seitenleisten gemeinsam ein- und ausblendet. Beim Einblenden beide öffnen, auch wenn zuvor nur eine geöffnet war. Die bisherige Wiederherstellung unterschiedlicher Leistenzustände soll hier entfallen. Gemeldetes Verhalten: „Leisten zeigen“ öffnet teilweise nur die rechte Leiste.
- [x] Die Beschriftung des gemeinsamen Knopfs auf **Leiste einblenden** beziehungsweise **Leiste ausblenden** ändern; die Aktion betrifft trotz der gewünschten Einzahl beide Seitenleisten. Englische Texte entsprechend anpassen.
- [x] **Auswahl vereinen** beibehalten, als Button **Farbauswahl vereinen** rechtsbündig in die Leiste oberhalb der beiden Vorschauen verschieben.
- [x] In der **Exportvorschau** den Knopf **Detailansicht / Überlagerung** entfernen.
- [x] Die Ansichtsoptionen als einheitliche Buttons gestalten: gleiche Höhe, Schrift, Rundung und Umrandung; Aufhellung beim Darüberfahren. Ausgeschaltete Optionen dunkel, eingeschaltete heller; nicht verfügbare Funktionen ausgegraut.
- [x] Genau einen Umschaltbutton **Untereinander ⇄ Nebeneinander** verwenden. Beide Varianten bleiben sichtbar; die aktuelle wird hervorgehoben. Bei Nebeneinander ist zusätzlich der Button heller. Kein automatischer Layoutwechsel.
- [x] Moduswechsel genauso gestalten: genau ein Button **Farbauszüge ⇄ Laser**, beide Varianten sichtbar und aktuelle Variante hervorgehoben. Laser erhält den helleren Hintergrund.
- [x] Sprachwahl als einen Button **Deutsch ⇄ English** mit Hervorhebung der aktuellen Sprache gestalten.
- [x] **Zoom koppeln** als Schaltbutton statt „Synchron“ verwenden.
- [x] **Windows-Einbindung** und **Lizenzen & Hinweise** optisch an diese Buttons angleichen. Sie öffnen jeweils ein eigenes Fenster; eine helle Hervorhebung zeigt deren geöffneten Zustand, keine dauerhafte Aktivierung.
- [x] Ein schwächeres Flächenlauflicht auf den gesamten eingeklappten Seitenleisten ergänzen. Randlicht und Flächenschimmer sowie beide Seitenleisten laufen synchron.
- [x] Zugehörige Bedienungsanleitung und Funktionsprüfungen bei der Umsetzung aktualisieren.

Die separate Exportfunktion **Kombiniert exportieren** bleibt erhalten. **Farbauswahl vereinen** schaltet weiterhin zwischen einer einzelnen Farbgruppe und der gemeinsamen Vorschau aller angehakten Gruppen um. Die Häkchen an den Farbgruppen bleiben zur Auswahl der Exportfarben erhalten.
