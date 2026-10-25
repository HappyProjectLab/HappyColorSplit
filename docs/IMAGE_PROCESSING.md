# Bildverarbeitung

HappyColorSplit verwendet SkiaSharp 3.119.4 für PNG-/JPEG-Decodierung, Vorschauen und PNG-Export. Die Anwendung arbeitet lokal und verändert Quelldateien nicht absichtlich.

## Eingaben und Farbgruppen

- Alle Ladewege (Dateidialog, Drag-and-drop und Explorer-Startargumente) akzeptieren nur `.png`, `.jpg` und `.jpeg`. Zusätzlich wird das tatsächliche codierte Format auf PNG/JPEG geprüft; umbenannte andere Bildformate werden zurückgewiesen. Nach der Größenprüfung werden maximal 16 Millionen Pixel geladen.
- Die interne Pixelablage verwendet RGBA mit nicht vormultiplizierter Deckkraft. Gespeicherte RGB-Werte bleiben auch bei geringer Deckkraft für die Erkennung erhalten.
- Alle acht EXIF-Ausrichtungen einschließlich Spiegelungen werden berücksichtigt. Bei animierten PNG-Dateien wird nur das erste Bild verarbeitet.
- Pixel unterhalb der Mindestdeckkraft werden ignoriert. Übrige Pixel werden nach ihrem RGB-Abstand zu festen Referenzfarben gruppiert; maximal 256 Gruppen sind möglich.
- Die Farbgruppierung arbeitet mit den Originalpixeln nach Anwendung der EXIF-Ausrichtung. Eine individuelle Skalierung wird anschließend auf die Ausgabefläche angewendet, ohne die Farbgruppen neu zu bilden.

## Individuelle Skalierung

Jeder Eintrag der Importliste enthält eine eigene unveränderliche Skalierung aus Prozentwert und DPI. Breite und Höhe werden immer mit demselben Faktor berechnet und auf ganze Pixel gerundet; frei voneinander veränderbare Seiten gibt es nicht. Zentimeter beziehen sich auf die gesamte Bildfläche. Standard für neue Bilder sind 100 % und zunächst 300 DPI; die DPI-Vorgabe ist einstellbar und wird gespeichert. Die Quelldatei-DPI wird nicht automatisch ausgewertet.

Die Pixelmaße folgen `Pixel = cm / 2,54 × DPI`. In Zentimeter-Eingabe bleiben die physischen Maße bei einer DPI-Änderung erhalten; in Prozent-Eingabe bleiben die Pixelmaße erhalten. Ausgabegrößen oberhalb von 16 Millionen Pixeln werden vor der Allokation abgewiesen. Alle Farbauszüge desselben Bildes erhalten dieselbe Pixelgröße und denselben Maßstab. Im Stapel-Export wird die Skalierung pro Quelldatei als unveränderliche Momentaufnahme übernommen.

Die bereits klassifizierten Pixel werden mittels nächstem Nachbarn auf das Zielraster abgebildet; dadurch entstehen keine Zwischenfarben oder weichen Maskenränder. Sehr kleine Details können beim Verkleinern verloren gehen. Verkleinerte Vorschauen werden vom endgültigen Zielraster abgetastet. Originalbild, Maske und Überlagerung im Detailfenster haben dieselbe Zielgröße.

Die PNG-Datei enthält einen `pHYs`-Chunk mit gleicher horizontaler und vertikaler Auflösung in Pixeln pro Meter, Einheit Meter und gültiger CRC. Die Umrechnung aus DPI wird auf ganze Pixel pro Meter gerundet. Transparenz und 8-Bit-Graustufenmasken bleiben erhalten. Programme, die diese Metadaten ignorieren, benötigen beim Import eine passende DPI- oder Maßangabe.

## Export

Ausgewählte Gruppen werden standardmäßig als deckende 8-Bit-Graustufen-PNGs mit schwarzen und weißen Pixeln ausgegeben. Die gesamte Fläche kann schwarz, weiß, transparent oder in der RGB-Referenzfarbe ihrer Gruppe ausgegeben werden. Unabhängig davon ist der Hintergrund wählbar: Schwarz (`#000000`), Grau (`#808080`), Weiß (`#FFFFFF`) oder Transparent. Alle nicht zur ausgewählten Gruppe gehörenden Pixel verwenden diesen Hintergrund, einschließlich ignorierter und abgewählter Pixel.

Schwarze oder weiße Flächen auf deckendem Hintergrund verwenden weiterhin 8-Bit-Graustufen-PNGs. Gruppenfarbe oder Transparenz in einem der beiden Bereiche verwendet RGBA-Pixel. Schwarze, weiße und farbige Flächen werden mit Alpha 255 ausgegeben; die Flächeneinstellung Transparent erzeugt Alpha 0 und RGB 0. Dasselbe gilt unabhängig davon für transparenten Hintergrund. Es gibt keine Kantenglättung oder Zwischenwerte an den Flächengrenzen. Gleiche Flächen- und Hintergrundwerte können unsichtbare oder vollständig transparente Ergebnisse erzeugen; die gewählten Werte werden unverändert verwendet.

Vorschau und Export verwenden denselben Renderer. Das Raster in der Oberfläche kennzeichnet transparente Bereiche und wird nicht in das PNG gezeichnet. Neue Detailfenster übernehmen die aktuellen Ausgabeoptionen als Momentaufnahme. Gewählte Zielgröße und relative Positionen der Flächen bleiben erhalten. Dateinamen und neue Unterordner trennen die Ausgaben; Abbruch und Fehler werden berücksichtigt.

Die Anwendung führt keine automatische Konturreparatur oder Entfernung kleiner Flächen durch. Hinweise zu möglichen Fehlern und Sicherungskopien stehen in [USAGE_NOTICE_DE.md](USAGE_NOTICE_DE.md).

## Prüfung und Abhängigkeiten

Die synthetischen Funktionsprüfungen kontrollieren tatsächliche Exportpixel, Deckkraft, Gruppierung, Ausrichtung, animierte PNGs, Stapelverarbeitung, Abbruch und den Ablauf der Oberfläche. Optionale lokale Bildvergleiche werden außerhalb des Repositorys gehalten.

Die Komponenten- und Lizenzzuordnung steht in [THIRD_PARTY_COMPONENTS_DE.md](THIRD_PARTY_COMPONENTS_DE.md). Original-Lizenztexte werden unverändert mitgeliefert.
