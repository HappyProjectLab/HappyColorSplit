# Bildverarbeitung

HappyColorSplit verwendet SkiaSharp 3.119.4 für PNG-/JPEG-Decodierung, Vorschauen und PNG-Export. Die Anwendung arbeitet lokal und verändert Quelldateien nicht absichtlich.

## Eingaben und Farbgruppen

- PNG und JPEG werden nach einer Größenprüfung geladen. Das Limit liegt bei 16 Millionen Pixeln.
- Die interne Pixelablage verwendet RGBA mit nicht vormultiplizierter Deckkraft. Gespeicherte RGB-Werte bleiben auch bei geringer Deckkraft für die Erkennung erhalten.
- Alle acht EXIF-Ausrichtungen einschließlich Spiegelungen werden berücksichtigt. Bei animierten PNG-Dateien wird nur das erste Bild verarbeitet.
- Pixel unterhalb der Mindestdeckkraft werden ignoriert. Übrige Pixel werden nach ihrem RGB-Abstand zu festen Referenzfarben gruppiert; maximal 256 Gruppen sind möglich.
- Eine Vorschau kann verkleinert werden. Farbgruppierung und Export arbeiten mit den Originalpixeln nach Anwendung der EXIF-Ausrichtung.

## Export

Ausgewählte Gruppen werden als deckende 8-Bit-Graustufen-PNGs mit schwarzen und weißen Pixeln ausgegeben. Bildgröße und Position der Flächen bleiben erhalten. Dateinamen und neue Unterordner trennen die Ausgaben; Abbruch und Fehler werden berücksichtigt.

Die Anwendung führt keine automatische Konturreparatur oder Entfernung kleiner Flächen durch. Hinweise zu möglichen Fehlern und Sicherungskopien stehen in [USAGE_NOTICE_DE.md](USAGE_NOTICE_DE.md).

## Prüfung und Abhängigkeiten

Die synthetischen Funktionsprüfungen kontrollieren tatsächliche Exportpixel, Deckkraft, Gruppierung, Ausrichtung, animierte PNGs, Stapelverarbeitung, Abbruch und den Ablauf der Oberfläche. Optionale lokale Bildvergleiche werden außerhalb des Repositorys gehalten.

Die Komponenten- und Lizenzzuordnung steht in [THIRD_PARTY_COMPONENTS_DE.md](THIRD_PARTY_COMPONENTS_DE.md). Original-Lizenztexte werden unverändert mitgeliefert.
