# Bildverarbeitung

HappyColorSplit verwendet SkiaSharp 3.119.4 für PNG-/JPEG-Decodierung, Vorschauen und PNG-Export. Die Anwendung arbeitet lokal und verändert Quelldateien nicht absichtlich.

## Eingaben und Farbgruppen

- Alle Ladewege (Dateidialog, Drag-and-drop und Explorer-Startargumente) akzeptieren nur `.png`, `.jpg` und `.jpeg`. Zusätzlich wird das tatsächliche codierte Format auf PNG/JPEG geprüft; umbenannte andere Bildformate werden zurückgewiesen. Nach der Größenprüfung werden maximal 16 Millionen Pixel geladen.
- Die interne Pixelablage verwendet RGBA mit nicht vormultiplizierter Deckkraft. Gespeicherte RGB-Werte bleiben auch bei geringer Deckkraft für die Erkennung erhalten.
- Alle acht EXIF-Ausrichtungen einschließlich Spiegelungen werden berücksichtigt. Bei animierten PNG-Dateien wird nur das erste Bild verarbeitet.
- Pixel unterhalb der Mindestdeckkraft werden ignoriert. Übrige Pixel werden nach ihrem RGB-Abstand zu festen Referenzfarben gruppiert; maximal 256 Gruppen sind möglich.
- Die Farbgruppierung arbeitet mit den Originalpixeln nach Anwendung der EXIF-Ausrichtung. Eine individuelle Skalierung wird anschließend auf die Ausgabefläche angewendet, ohne die Farbgruppen neu zu bilden.

„Farben erkennen“ verarbeitet alle geladenen Bilder nacheinander. Pro Bild bleiben Erkennungsstatus und angehakte RGB-Referenzfarben erhalten; volle Pixel- und Zuordnungspuffer werden nicht für die gesamte Liste gespeichert. Beim Bildwechsel wird die aktive Zuordnung für die eigene Palette wiederhergestellt. Eine erneute Erkennung behält vorhandene Häkchen bei unveränderten Erkennungseinstellungen bei. Fehlerhafte Bilder werden gemeldet; andere Bilder werden weiter verarbeitet. Ein Abbruch behält vollständig erkannte Bilder bei.

Der Stapel-Export übernimmt explizite Farbauswahlen pro Bild als Momentaufnahme. Bilder ohne Erkennung oder mit leerer Farbauswahl werden gezählt übersprungen. Es gibt im Anwendungsablauf keinen automatischen Rückfall auf alle Farben. Änderungen der Farbähnlichkeit oder Mindestdeckkraft verwerfen alle Erkennungsstände und Auswahlen.

## Individuelle Skalierung

Jeder Eintrag der Importliste enthält eine eigene unveränderliche Skalierung aus Prozentwert und DPI. Breite und Höhe werden immer mit demselben Faktor berechnet und auf ganze Pixel gerundet; frei voneinander veränderbare Seiten gibt es nicht. Zentimeter beziehen sich auf die gesamte Bildfläche. Standard für neue Bilder sind 100 % und zunächst 300 DPI; die DPI-Vorgabe ist einstellbar und wird gespeichert. Die Quelldatei-DPI wird nicht automatisch ausgewertet.

Die Pixelmaße folgen `Pixel = cm / 2,54 × DPI`. In Zentimeter-Eingabe bleiben die physischen Maße bei einer DPI-Änderung erhalten; in Prozent-Eingabe bleiben die Pixelmaße erhalten. Ausgabegrößen oberhalb von 16 Millionen Pixeln werden vor der Allokation abgewiesen. Alle Farbauszüge desselben Bildes erhalten dieselbe Pixelgröße und denselben Maßstab. Im Stapel-Export wird die Skalierung pro Quelldatei als unveränderliche Momentaufnahme übernommen.

Ohne Konturglättung werden bereits klassifizierte Pixel mittels nächstem Nachbarn auf das Zielraster abgebildet; dadurch entstehen keine Zwischenfarben oder weichen Maskenränder. Die optionale Konturrekonstruktion wird unten beschrieben. Sehr kleine Details können beim Verkleinern verloren gehen. Verkleinerte Vorschauen werden vom endgültigen Zielraster abgetastet. Originalbild, Maske und Überlagerung im Detailfenster haben dieselbe Zielgröße.

Die PNG-Datei enthält einen `pHYs`-Chunk mit gleicher horizontaler und vertikaler Auflösung in Pixeln pro Meter, Einheit Meter und gültiger CRC. Die Umrechnung aus DPI wird auf ganze Pixel pro Meter gerundet. Transparenz und 8-Bit-Graustufenmasken bleiben erhalten. Programme, die diese Metadaten ignorieren, benötigen beim Import eine passende DPI- oder Maßangabe.

## Export

Optional steht **Harte Konturen glätten** zur Verfügung. Dabei rekonstruiert ein kubischer 4×4-Kern die ausgewählte Maske zwischen den Originalpixeln und ein 0,5-Schwellwert erzeugt wieder eine harte Vorder-/Hintergrundentscheidung. Kombinierte Farben werden vorher vereinigt. Die Option betrifft auch Laser-Masken vor ihrer Teilerkennung; es erfolgt keine nachträgliche Änderung gepackter Konturen. Ohne aktivierte Option bleibt die bisherige Rasterabtastung erhalten. Bedienung, Erhalt der cm-Größe und Grenzen: [Konturglättung](CONTOUR_SMOOTHING.md).

Ausgewählte Gruppen werden standardmäßig als deckende 8-Bit-Graustufen-PNGs mit schwarzen und weißen Pixeln ausgegeben. Die gesamte Fläche kann schwarz, weiß, transparent oder in der RGB-Referenzfarbe ihrer Gruppe ausgegeben werden. Unabhängig davon ist der Hintergrund wählbar: Schwarz (`#000000`), Grau (`#808080`), Weiß (`#FFFFFF`) oder Transparent. Alle nicht zur ausgewählten Gruppe gehörenden Pixel verwenden diesen Hintergrund, einschließlich ignorierter und abgewählter Pixel.

Schwarze oder weiße Flächen auf deckendem Hintergrund verwenden weiterhin 8-Bit-Graustufen-PNGs. Gruppenfarbe oder Transparenz in einem der beiden Bereiche verwendet RGBA-Pixel. Schwarze, weiße und farbige Flächen werden mit Alpha 255 ausgegeben; die Flächeneinstellung Transparent erzeugt Alpha 0 und RGB 0. Dasselbe gilt unabhängig davon für transparenten Hintergrund. Auch bei optionaler harter Konturglättung gibt es keine weichen Zwischenfarben oder Alpha-Zwischenwerte an den Flächengrenzen. Gleiche Flächen- und Hintergrundwerte können unsichtbare oder vollständig transparente Ergebnisse erzeugen; die gewählten Werte werden unverändert verwendet.

Vorschau und Export verwenden denselben Renderer. Das Raster in der Oberfläche kennzeichnet transparente Bereiche und wird nicht in das PNG gezeichnet. Neue Detailfenster übernehmen die aktuellen Ausgabeoptionen als Momentaufnahme. Gewählte Zielgröße und relative Positionen der Flächen bleiben erhalten. Dateinamen und neue Unterordner trennen die Ausgaben; Abbruch und Fehler werden berücksichtigt.

Beim kombinierten Export ist die gemeinsame Maske die Vereinigung aller Pixel, deren Gruppenindex zu den angehakten Gruppen gehört. Dies entspricht dem Zusammenfügen schwarzer, deckender Flächen auf transparentem Hintergrund; deckende Einzelmasken-Hintergründe werden nicht übereinander gezeichnet. Erst die gemeinsame Zugehörigkeitsmaske wird auf das individuelle Zielraster skaliert und mit der gewählten Flächen- und Hintergrunddarstellung ausgegeben. Bei Gruppenfarbe wird an jedem ausgewählten Pixel die ursprüngliche Gruppenreferenzfarbe verwendet. Das Ergebnis ist genau eine `_merged.png` pro exportiertem Bild. Leere Auswahlen erzeugen keine versehentlichen Vollbild- oder Alle-Farben-Exporte.

Die umschaltbare gemeinsame Vorschau verwendet denselben Renderer. Detailmaske und grüne Überlagerung berücksichtigen dann dieselbe Vereinigung. Die Exporte bleiben atomar pro Bild: Bei Abbruch werden unvollständige Dateien entfernt; bereits abgeschlossene Bildexporte bleiben erhalten.

Die Anwendung führt keine automatische Konturreparatur oder Entfernung kleiner Flächen durch. Hinweise zu möglichen Fehlern und Sicherungskopien stehen in [USAGE_NOTICE_DE.md](USAGE_NOTICE_DE.md).

## Prüfung und Abhängigkeiten

Der lokale Laser-Modus verwendet die skalierten Materialmasken für eine achtfach zusammenhängende Teilerkennung. Masken werden als Zeilenläufe gespeichert. Das mehrstufige Rechteckpacken wird durch eine konservative Suche nach freien Aussparungen für kleinere Teile derselben Quelldatei und Materialfarbe ergänzt. Datei- und Bereichsnummern bleiben erhalten. Die Packgeometrie arbeitet vollständig auf dem Laser-Ausgaberaster. Rand, Abstand zum umgebenden Teil einer Aussparung und Abstand zu anderen Teilen sind unabhängig einstellbar und werden jeweils auf ganze Ausgabepixel aufgerundet. Vierteldrehungen werden vorwärts und invers ohne Spiegelung abgebildet. Vorschau-Rahmen werden getrennt von den exportierten Masken gezeichnet. Details und Grenzen: [Laser-Arbeitsbereich](LASER_WORKSPACE.md).

Die synthetischen Funktionsprüfungen kontrollieren tatsächliche Exportpixel, Deckkraft, Gruppierung, Ausrichtung, animierte PNGs, Stapelverarbeitung, Abbruch und den Ablauf der Oberfläche. Optionale lokale Bildvergleiche werden außerhalb des Repositorys gehalten.

Die Komponenten- und Lizenzzuordnung steht in [THIRD_PARTY_COMPONENTS_DE.md](THIRD_PARTY_COMPONENTS_DE.md). Original-Lizenztexte werden unverändert mitgeliefert.
