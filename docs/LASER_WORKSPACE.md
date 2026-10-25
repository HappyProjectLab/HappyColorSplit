# Laser-Arbeitsbereich / Laser workspace

## Bedienung

Der Laser-Modus ist Teil des lokalen Entwicklungsstands und noch nicht im Download `0.1.0-preview.4` enthalten. Er erstellt PNG-Vorlagen mit platzierten Applikationen. Die Bilderkennung, Häkchen je Bild, individuellen Bildmaße und Ausgabefarben werden mit dem normalen Farbauszugs-Modus gemeinsam genutzt.

Nach dem Import gegebenenfalls die Bildgröße in cm oder % einstellen und **Farben erkennen** ausführen. Im Bereich **Farbauszüge** die auszuschneidenden Farben pro Bild anhaken. Nicht erkannte Bilder und Bilder ohne Häkchen gehören nicht zur Laser-Anordnung. Auch eine angehakte Hintergrundfarbe wird als Materialfläche behandelt; bei Bedarf abwählen.

Oben **Laser** wählen. Unter **Laser-Einstellungen** stehen:

- Arbeitsfläche: unabhängige Breite und Höhe in Zentimetern, zunächst 30 × 20 cm.
- Auflösung: zunächst 300 DPI, einstellbar von 1 bis 2400. Die gesamte Seite darf maximal 16 Millionen Pixel enthalten. Für große Arbeitsflächen gegebenenfalls die DPI zuerst reduzieren. Die Seitenmaße werden auf das enthaltene Pixelraster abgerundet, damit es die nominelle Arbeitsfläche nicht überschreitet.
- Mindestabstände: **Zum Plattenrand** (zunächst 0 cm), **Beim Füllen von Aussparungen** (zunächst 0,5 cm zum umgebenden Teil) und **Zwischen anderen Teilen** (zunächst 0,5 cm). Alle drei Werte sind unabhängig von 0 bis 500 cm einstellbar und werden gespeichert. Ein Rand muss noch eine nutzbare Arbeitsfläche übrig lassen. 0 erlaubt Berührung, aber keine Überlappung. Ein bereits zu großes Einzelteil wird nicht durch einen kleineren Abstand automatisch verkleinert.
- Materialliste: optionaler Materialname und Farbe als `#RRGGBB`. Es werden keine vorhandenen Materialien vorausgesetzt. Doppelte Farbwerte werden nicht hinzugefügt. Arbeitsfläche und Materialliste werden im Benutzerdatenverzeichnis als Einstellungen gespeichert.

**Anordnen** erstellt eine neue Anordnung für alle ausgewählten Bilder. Seiten stehen rechts mit Materialname, Seitennummer und Teileanzahl. Die Miniaturen zeigen die Materialfarbe; der große Arbeitsbereich zeigt die aktuellen Exportfarben. **Dateien und Bereiche** nennt die Quelle und Bereiche der gewählten Seite. Blaue Rahmen markieren Dateiblöcke, gestrichelte gelbe Rahmen Bereiche; diese Rahmen sind ausschließlich eine Vorschauhilfe.

Links und oberhalb der Arbeitsfläche stehen Zentimetermaßstäbe mit Markierungen alle 0,5 cm. Ganze Zentimeter haben längere Striche. Bei genügend Platz werden auch halbe Zentimeter beschriftet; Zahlen werden bei kleinen Fenstern beziehungsweise großen Platten ausgedünnt, damit sie sich nicht überlagern. Der Nullpunkt liegt links oben. Maßstäbe und Arbeitsfläche werden beim Fensterwechsel gemeinsam eingepasst; die Beschriftung behält ihre lesbare Bildschirmgröße. Die Skala richtet sich nach den tatsächlichen Seitenpixeln und der Laser-DPI. Maßstäbe werden ausschließlich in der Vorschau angezeigt und nicht in die PNG-Dateien gezeichnet.

## Materialzuordnung und Teilerkennung

Die feste Referenzfarbe jeder angehakten Bildgruppe wird in CIE Lab (D65) umgerechnet und dem kleinsten Delta-E-76-Abstand in der verfügbaren Materialliste zugeordnet. Bei gleichem Abstand gewinnt die zuerst eingetragene Materialfarbe. Das ist eine rechnerische Zuordnung der RGB-Referenzwerte, keine Messung realer Materialien.

Die Erkennung arbeitet auf derselben proportional skalierten Pixelversion wie der normale Export, einschließlich Deckkraftgrenze und ausgeschlossener Gruppen. Je Material werden achtfach zusammenhängende Pixel als ein Teil behandelt; berührende ausgewählte Farben, die demselben Material zugeordnet sind, können dadurch ein gemeinsames Teil bilden. Löcher und transparente Aussparungen bleiben in der Maske erhalten. Sehr kleine Details, die bereits bei der Bildskalierung verschwinden, werden nicht aus der Originalauflösung wieder hinzugefügt.

Teile werden zugeschnitten, ohne die Originaldatei zu verändern. Ihre physische Größe ergibt sich aus der Bildskalierung und der eigenen DPI-Zahl des Imports. Für die Laser-Seite wird auf deren DPI umgerechnet; Rundung auf ganze Pixel ist unvermeidlich. Ein Wechsel der Laser-DPI verändert deshalb die Auflösung, nicht absichtlich die Zentimetergröße der Teile.

## Gruppierung und Anordnung

Die erste Gruppierung ist die importierte Datei; auch gleichnamige Dateien aus verschiedenen Ordnern bleiben über ihre Importnummer unterscheidbar. Innerhalb einer Datei bilden nahe Teile Bereiche. Dafür werden die Abstände ihrer Begrenzungsrechtecke in der skalierten Ausgangsgrafik ausgewertet: bis 0,5 cm Abstand werden sie zusammengefasst, auch über eine Kette benachbarter Teile. Die Bereichsnummern werden vor der Aufteilung nach Material bestimmt.

Jede Materialfarbe erhält eine eigene Seitenfolge. Innerhalb eines Materials werden zunächst Teile pro Bereich kompakt angeordnet, anschließend Bereiche zu Dateiblöcken und die Dateiblöcke auf Seiten gepackt. Die Heuristik prüft 0° und 90° pro Packschritt; zusammengesetzte Drehungen ergeben 0°, 90°, 180° oder 270°. Es wird niemals gespiegelt. Zu große Gruppen werden als Fortsetzung auf weitere Blöcke beziehungsweise Seiten verteilt; ein einzelnes zu großes zusammenhängendes Teil wird dagegen als Fehler gemeldet.

Das anfängliche Packen reserviert Begrenzungsrechtecke mit dem eingestellten Mindestabstand **zwischen anderen Teilen**. Zusätzlich werden transparente Löcher und offene Aussparungen größerer Teile nach Platz für kleinere Teile durchsucht. Dabei zählt die tatsächliche Materialmaske einschließlich Drehung und Abtastung auf dem Laser-Ausgaberaster. Zwischen einem eingesetzten Teil und seinem umgebenden Teil gilt der Abstand **beim Füllen von Aussparungen**; zwischen mehreren eingesetzten Teilen gilt weiterhin der Abstand **zwischen anderen Teilen**. Diese Werte dürfen unterschiedlich groß sein. Alle Abstände werden getrennt auf ganze Ausgabepixel aufgerundet.

Die Suche nach Aussparungen bleibt innerhalb derselben Quelldatei und Materialfarbe; Teile aus demselben ursprünglichen Bereich werden bevorzugt. Sie kann kleine Teile aus späteren Dateiblöcken beziehungsweise Seiten in frühere Aussparungen verlegen. Ihre Datei- und Bereichsnummern bleiben erhalten; Bereichsrahmen können sich deshalb in der Vorschau überlagern. Leere Blöcke entfallen. Zwischen verschiedenen Quelldateien bleibt die räumliche Gruppierung bestehen.

Für diese zusätzliche Suche wird ein konservatives Raster verwendet: Jede von Material berührte Zelle und ein nach oben gerundeter Abstandspuffer werden gesperrt. Das gesamte Begrenzungsrechteck eines einzufügenden Teils muss in freie Zellen passen. Der Puffer ist quadratisch, weshalb diagonal teilweise mehr Abstand bleibt als zwingend nötig. Bei großen Masken werden gröbere Zellen verwendet; enge Lücken können dadurch ungenutzt bleiben. Eine begrenzte Suche hält den Rechenaufwand überschaubar. Die reproduzierbare Heuristik verspricht kein global optimales oder beliebig gedrehtes Kontur-Nesting.

Der eingestellte Rand wird an allen vier Plattenseiten freigehalten. Die Teile bleiben vollständig innerhalb der verbleibenden nutzbaren Fläche. Ein Teil, das weder normal noch um 90° gedreht auf diese Fläche passt, beendet die neue Anordnung mit einer Meldung. Teile werden nie automatisch verkleinert, zerschnitten oder stillschweigend gelöscht.

Technische Grenzen: höchstens 5.000 Teile, 1 Million gespeicherte Maskenläufe und 200 Seiten je Anordnung sowie 16 Millionen Pixel je Seite. Eine Überschreitung erzeugt eine Fehlermeldung. Bei Fehler oder Abbruch wird keine teilweise neue Anordnung als fertig freigegeben. Eine bisherige Anordnung bleibt gegebenenfalls sichtbar; nach Änderungen ist sie als veraltet markiert und nicht exportierbar.

## Export

**Seite exportieren** speichert die gewählte Seite, **Alle Seiten exportieren** alle Materialseiten. **Kombiniert exportieren** speichert ebenfalls die gemeinsame Maske jeder Seite mit dem Zusatz `_merged.png`; bei genau einer Materialfarbe pro Seite ist diese bereits die Vereinigung aller platzierten Teile.

Die üblichen Einstellungen gelten unverändert:

- Flächen: Schwarz, Weiß, Transparent oder Gruppenfarbe. Gruppenfarbe entspricht hier der zugeordneten Materialfarbe.
- Hintergrund: Schwarz, Grau, Weiß oder Transparent.
- PNG-Auflösungsmetadaten enthalten die Laser-DPI. Auswertende Programme müssen diese Angabe beim Import berücksichtigen, um die Zentimetermaße zu erhalten.

Der Export erzeugt einen neuen Ordner mit den Seiten-PNGs und `Laser_Layout.json`. Die JSON-Datei enthält Material, nominale Seitenmaße, Pixelgröße, DPI, alle drei Abstandseinstellungen, Quelldateinamen, Importnummer, Dateiblock, Bereich, Teilnummer, gegebenenfalls das umgebende Teil (`NestedInPartId`), Positionen in cm und Drehwinkel. Absolute Importpfade und Vorschau-Rahmen werden nicht exportiert. Der Button **Letzten Export öffnen** öffnet diesen Ordner.

Ein Laser-Exportjob wird als Ganzes in einem privaten temporären Ordner aufgebaut und erst nach vollständigem Abschluss sichtbar gemacht. Bei Abbruch wird dieser temporäre Job entfernt; bereits früher exportierte Ordner bleiben erhalten. Die PNG-Dateien sind Bildvorlagen; es werden keine Gerätebefehle, Schnittparameter oder Vektor-Schnittpfade erzeugt.

Bei Änderungen an Bildliste, Farbwahl, Erkennungseinstellungen, Bildskalierung, Materialliste oder Arbeitsfläche **erneut anordnen**. Eine Änderung der Ausgabefarben oder der nur angezeigten Gruppenrahmen erfordert keine neue Geometrieberechnung. Das Leeren der Importliste leert auch die Laser-Arbeitsfläche.

## English

Switch from **Color layers** to **Laser**. Find colors for all imported images and check the desired groups individually. Each image keeps its own scale and DPI. In **Laser settings**, choose the sheet width/height in cm, output DPI, and available material colors as `#RRGGBB`. Colors are assigned using nearest CIE Lab Delta E 76; each material gets separate pages.

**Arrange** extracts eight-connected material parts from the scaled images, retains holes, groups nearby parts into source regions, and packs regions inside source-file blocks. The deterministic heuristic uses quarter turns, never reflections. An additional conservative raster search places smaller parts inside holes and open cavities of larger parts from the same source and material, preferring the same original region. It can move parts from later blocks into earlier ones while preserving source/region identifiers; preview region outlines may overlap. Configure three independent distances: **Sheet edge** (default 0 cm), **When filling cavities** (default 0.5 cm from the surrounding part), and **Between other parts** (default 0.5 cm, also between several inserted parts). All are saved, rounded up independently to output pixels, and may range from 0 to 500 cm. Zero permits touching but never overlap. The edge margin must leave usable space. The square clearance buffer and coarser cells for large masks may leave some usable gaps empty. The search is bounded and does not guarantee globally optimal contour nesting. Parts do not overlap or extend into the margin. More pages are created as needed. An oversized indivisible part produces an error instead of being shrunk or removed. The export manifest records all distances and the surrounding part identifier of nested elements.

Choose pages in the right-hand list. Preview group outlines are not exported. Export one page, all pages, or one merged PNG per page using the usual foreground/background choices. Group color uses the assigned material color. Resolution metadata preserves print size when respected by the importing application. `Laser_Layout.json` records source filenames, grouping, positions and rotations without absolute input paths.

Centimetre rulers above and to the left of the sheet mark every half centimetre, with longer ticks at whole centimetres. Numbers are thinned when space is limited. The origin is at the top left; ticks follow the actual pixel dimensions and output DPI. Rulers resize with the sheet while their labels keep a readable screen size. They are preview aids and are never included in exported PNGs.

Changed selections, scales, materials or sheet dimensions invalidate the arrangement; arrange again before exporting. Limits are 5,000 parts, 1 million mask runs, 200 pages and 16 million pixels per page. Cancellation never publishes an incomplete layout or partial export job. No laser device commands or vector cutting paths are produced.
