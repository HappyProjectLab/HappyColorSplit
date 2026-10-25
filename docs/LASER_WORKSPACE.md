# Laser-Arbeitsbereich / Laser workspace

## Bedienung

Der Laser-Modus ist Teil des lokalen Entwicklungsstands und noch nicht im Download `0.1.0-preview.4` enthalten. Er erstellt PNG-Vorlagen mit platzierten Applikationen. Die Bilderkennung, Häkchen je Bild, individuellen Bildmaße und Ausgabefarben werden mit dem normalen Farbauszugs-Modus gemeinsam genutzt.

Nach dem Import gegebenenfalls die Bildgröße in cm oder % einstellen und **Farben erkennen** ausführen. Im Bereich **Farbauszüge** die auszuschneidenden Farben pro Bild anhaken. Nicht erkannte Bilder und Bilder ohne Häkchen gehören nicht zur Laser-Anordnung. Auch eine angehakte Hintergrundfarbe wird als Materialfläche behandelt; bei Bedarf abwählen.

Oben **Laser** wählen. Unter **Laser-Einstellungen** stehen:

- Arbeitsfläche: unabhängige Breite und Höhe in Zentimetern, zunächst 30 × 20 cm.
- Auflösung: zunächst 300 DPI, einstellbar von 1 bis 2400. Die gesamte Seite darf maximal 16 Millionen Pixel enthalten. Für große Arbeitsflächen gegebenenfalls die DPI zuerst reduzieren. Die Seitenmaße werden auf das enthaltene Pixelraster abgerundet, damit es die nominelle Arbeitsfläche nicht überschreitet.
- Materialliste: optionaler Materialname und Farbe als `#RRGGBB`. Es werden keine vorhandenen Materialien vorausgesetzt. Doppelte Farbwerte werden nicht hinzugefügt. Arbeitsfläche und Materialliste werden im Benutzerdatenverzeichnis als Einstellungen gespeichert.

**Anordnen** erstellt eine neue Anordnung für alle ausgewählten Bilder. Seiten stehen rechts mit Materialname, Seitennummer und Teileanzahl. Die Miniaturen zeigen die Materialfarbe; der große Arbeitsbereich zeigt die aktuellen Exportfarben. **Dateien und Bereiche** nennt die Quelle und Bereiche der gewählten Seite. Blaue Rahmen markieren Dateiblöcke, gestrichelte gelbe Rahmen Bereiche; diese Rahmen sind ausschließlich eine Vorschauhilfe.

## Materialzuordnung und Teilerkennung

Die feste Referenzfarbe jeder angehakten Bildgruppe wird in CIE Lab (D65) umgerechnet und dem kleinsten Delta-E-76-Abstand in der verfügbaren Materialliste zugeordnet. Bei gleichem Abstand gewinnt die zuerst eingetragene Materialfarbe. Das ist eine rechnerische Zuordnung der RGB-Referenzwerte, keine Messung realer Materialien.

Die Erkennung arbeitet auf derselben proportional skalierten Pixelversion wie der normale Export, einschließlich Deckkraftgrenze und ausgeschlossener Gruppen. Je Material werden achtfach zusammenhängende Pixel als ein Teil behandelt; berührende ausgewählte Farben, die demselben Material zugeordnet sind, können dadurch ein gemeinsames Teil bilden. Löcher und transparente Aussparungen bleiben in der Maske erhalten. Sehr kleine Details, die bereits bei der Bildskalierung verschwinden, werden nicht aus der Originalauflösung wieder hinzugefügt.

Teile werden zugeschnitten, ohne die Originaldatei zu verändern. Ihre physische Größe ergibt sich aus der Bildskalierung und der eigenen DPI-Zahl des Imports. Für die Laser-Seite wird auf deren DPI umgerechnet; Rundung auf ganze Pixel ist unvermeidlich. Ein Wechsel der Laser-DPI verändert deshalb die Auflösung, nicht absichtlich die Zentimetergröße der Teile.

## Gruppierung und Anordnung

Die erste Gruppierung ist die importierte Datei; auch gleichnamige Dateien aus verschiedenen Ordnern bleiben über ihre Importnummer unterscheidbar. Innerhalb einer Datei bilden nahe Teile Bereiche. Dafür werden die Abstände ihrer Begrenzungsrechtecke in der skalierten Ausgangsgrafik ausgewertet: bis 0,5 cm Abstand werden sie zusammengefasst, auch über eine Kette benachbarter Teile. Die Bereichsnummern werden vor der Aufteilung nach Material bestimmt.

Jede Materialfarbe erhält eine eigene Seitenfolge. Innerhalb eines Materials werden zunächst Teile pro Bereich kompakt angeordnet, anschließend Bereiche zu Dateiblöcken und die Dateiblöcke auf Seiten gepackt. Die Heuristik prüft 0° und 90° pro Packschritt; zusammengesetzte Drehungen ergeben 0°, 90°, 180° oder 270°. Es wird niemals gespiegelt. Zu große Gruppen werden als Fortsetzung auf weitere Blöcke beziehungsweise Seiten verteilt; ein einzelnes zu großes zusammenhängendes Teil wird dagegen als Fehler gemeldet.

Für den Abstand werden Begrenzungsrechtecke verwendet. Sie werden mit einem Abstandspuffer reserviert, der **mindestens 0,5 cm** entspricht und auf ganze Ausgabepixel aufgerundet wird. Dadurch bleiben auch die eigentlichen Teile mindestens so weit voneinander entfernt. Es ist kein zusätzlicher Randabstand zum Plattenrand vorgesehen. Die Teile bleiben vollständig innerhalb der Seite.

Die Rechteck-Packheuristik ist reproduzierbar, priorisiert die Gruppierung und nutzt freie Rechteckflächen. Sie verspricht kein global optimales oder beliebig gedrehtes Kontur-Nesting; transparente Löcher werden nicht mit anderen Teilen gefüllt. Ein Teil, das weder normal noch um 90° gedreht auf die Fläche passt, beendet die neue Anordnung mit einer Meldung. Teile werden nie automatisch verkleinert, zerschnitten oder stillschweigend gelöscht.

Technische Grenzen: höchstens 5.000 Teile, 1 Million gespeicherte Maskenläufe und 200 Seiten je Anordnung sowie 16 Millionen Pixel je Seite. Eine Überschreitung erzeugt eine Fehlermeldung. Bei Fehler oder Abbruch wird keine teilweise neue Anordnung als fertig freigegeben. Eine bisherige Anordnung bleibt gegebenenfalls sichtbar; nach Änderungen ist sie als veraltet markiert und nicht exportierbar.

## Export

**Seite exportieren** speichert die gewählte Seite, **Alle Seiten exportieren** alle Materialseiten. **Kombiniert exportieren** speichert ebenfalls die gemeinsame Maske jeder Seite mit dem Zusatz `_merged.png`; bei genau einer Materialfarbe pro Seite ist diese bereits die Vereinigung aller platzierten Teile.

Die üblichen Einstellungen gelten unverändert:

- Flächen: Schwarz, Weiß, Transparent oder Gruppenfarbe. Gruppenfarbe entspricht hier der zugeordneten Materialfarbe.
- Hintergrund: Schwarz, Grau, Weiß oder Transparent.
- PNG-Auflösungsmetadaten enthalten die Laser-DPI. Auswertende Programme müssen diese Angabe beim Import berücksichtigen, um die Zentimetermaße zu erhalten.

Der Export erzeugt einen neuen Ordner mit den Seiten-PNGs und `Laser_Layout.json`. Die JSON-Datei enthält Material, nominale Seitenmaße, Pixelgröße, DPI, Quelldateinamen, Importnummer, Dateiblock, Bereich, Teilnummer, Positionen in cm und Drehwinkel. Absolute Importpfade und Vorschau-Rahmen werden nicht exportiert. Der Button **Letzten Export öffnen** öffnet diesen Ordner.

Ein Laser-Exportjob wird als Ganzes in einem privaten temporären Ordner aufgebaut und erst nach vollständigem Abschluss sichtbar gemacht. Bei Abbruch wird dieser temporäre Job entfernt; bereits früher exportierte Ordner bleiben erhalten. Die PNG-Dateien sind Bildvorlagen; es werden keine Gerätebefehle, Schnittparameter oder Vektor-Schnittpfade erzeugt.

Bei Änderungen an Bildliste, Farbwahl, Erkennungseinstellungen, Bildskalierung, Materialliste oder Arbeitsfläche **erneut anordnen**. Eine Änderung der Ausgabefarben oder der nur angezeigten Gruppenrahmen erfordert keine neue Geometrieberechnung. Das Leeren der Importliste leert auch die Laser-Arbeitsfläche.

## English

Switch from **Color layers** to **Laser**. Find colors for all imported images and check the desired groups individually. Each image keeps its own scale and DPI. In **Laser settings**, choose the sheet width/height in cm, output DPI, and available material colors as `#RRGGBB`. Colors are assigned using nearest CIE Lab Delta E 76; each material gets separate pages.

**Arrange** extracts eight-connected material parts from the scaled images, retains holes, groups nearby parts into source regions, and packs regions inside source-file blocks. The deterministic rectangular heuristic uses quarter turns, never reflections. Bounding rectangles stay at least 0.5 cm apart after rounding to the output grid; parts do not overlap or extend outside the sheet. More pages are created as needed. Groups may continue across pages, but an oversized indivisible part produces an error instead of being shrunk or removed. This is not globally optimal contour nesting, and other parts are not placed inside transparent holes.

Choose pages in the right-hand list. Preview group outlines are not exported. Export one page, all pages, or one merged PNG per page using the usual foreground/background choices. Group color uses the assigned material color. Resolution metadata preserves print size when respected by the importing application. `Laser_Layout.json` records source filenames, grouping, positions and rotations without absolute input paths.

Changed selections, scales, materials or sheet dimensions invalidate the arrangement; arrange again before exporting. Limits are 5,000 parts, 1 million mask runs, 200 pages and 16 million pixels per page. Cancellation never publishes an incomplete layout or partial export job. No laser device commands or vector cutting paths are produced.
