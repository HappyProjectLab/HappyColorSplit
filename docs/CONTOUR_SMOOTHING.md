# Harte Konturen glätten / Smooth hard contours

Die Funktion gehört zum lokalen Entwicklungsstand, noch nicht zum öffentlichen Download `0.1.0-preview.4`.

## Bedienung

Nach dem Laden und Erkennen der Farben neben **Flächen exportieren als** auf **Konturen** klicken. **Harte Konturen glätten** ist zunächst ausgeschaltet; die Einstellung wird für alle Bilder gespeichert. Sie betrifft Einzel-Layer, kombinierte Layer und die Erkennung von Laser-Teilen. Farberkennung und Farbauswahl bleiben erhalten.

Für ein niedrig aufgelöstes Original empfiehlt sich **3× Auflösung bei gleicher Größe**. Der Knopf aktiviert die Glättung und verdreifacht für das ausgewählte Bild Prozentmaßstab und DPI gemeinsam. Die beabsichtigten Zentimetermaße bleiben erhalten; Pixelrundung ist unvermeidlich. Andere geladene Bilder und die DPI-Vorgabe neuer Importe werden nicht geändert. Es entstehen etwa neunmal so viele Ausgabepixel. Der Knopf ist bei fehlendem Bild oder einer Überschreitung von 16 Millionen Pixeln beziehungsweise 2400 DPI gesperrt.

Alternativ unter **Bildgröße** zuerst **cm** wählen und anschließend die DPI erhöhen. In der cm-Ansicht hält das Programm die Zentimetermaße konstant und passt den Pixelmaßstab entsprechend an. Die Vorschau zeigt dieselben harten Maskenentscheidungen wie der spätere Export. **Detailansicht** zeigt das Ergebnis in voller Ausgabeauflösung einschließlich passender Überlagerung.

Die Glättung entfaltet ihre Wirkung zwischen den Originalpixeln beim Vergrößern. Bei 100 % bleibt die ursprüngliche Pixelmaske erhalten; ohne zusätzliche Ausgabepixel lässt sich eine harte Rasterkante nur begrenzt verbessern. Hochskalieren erfindet keine verlorenen Originaldetails. Enge Lücken, dünne Linien, winzige Inseln und spitze Enden nach der Änderung prüfen. Es gibt keine Garantie auf unveränderte Topologie bei jeder Vergrößerung oder Verkleinerung.

## Verarbeitung

Die Farberkennung bleibt unverändert. Der Renderer rekonstruiert Masken zwischen den Mittelpunkten der Originalpixel mit einem separablen kubischen Catmull-Rom-Kern über 4 × 4 Nachbarn. Ein Schwellwert von 0,5 trennt gültiges Material vom transparenten beziehungsweise ausgeschlossenen Bereich. Für einzelne Farbauszüge wird eine eindeutige rekonstruierte Farbgruppe gewählt und anschließend die angefragte Gruppe ausgegeben; dadurch überlappen sich die einzelnen Farbmasken nicht an Farbübergängen. Es werden keine geglätteten Grauwerte oder halbtransparenten Randpixel gespeichert. Schwarz, Weiß, Transparent und Gruppenfarbe sowie alle Hintergrundoptionen funktionieren weiterhin.

Bei kombinierten Layern werden die ausgewählten Farbgruppen vor der Glättung als gemeinsame Maske betrachtet. Innere Grenzen zwischen ausgewählten Farben bleiben deshalb nicht als künstliche Lücke bestehen. Das Glätten einer vorher vereinigten Maske kann an Mehrfarbübergängen von der nachträglichen Vereinigung separat geglätteter Einzelmasken abweichen. Bei Gruppenfarbe wird ein eindeutiger ausgewählter Referenzfarbwert anhand der rekonstruierten Beiträge gewählt; es werden keine Mischfarben ausgegeben. Bei Gleichstand wird zuerst die nächste Originalzelle, dann der kleinere Gruppenindex bevorzugt. Bildrandwerte werden fortgesetzt; außerhalb des Bildausschnitts wird kein Inhalt hinzugefügt.

Vorschauen fragen zuerst das spätere Ausgaberaster ab und verwenden anschließend denselben Konturentscheid wie der Export. Während des Exports und der Detailberechnung wird Abbruch berücksichtigt. Ohne aktivierte Option bleibt der bisherige Exportpfad erhalten. Es werden keine zusätzlichen Bibliotheken benötigt.

## Laser-Modus

Die Glättung wird vor der zusammenhängenden Teilerkennung auf die Materialmasken der skalierten Bilder angewendet. Mehrere ausgewählte Bildfarben, die demselben Material zugeordnet sind, bilden dabei eine gemeinsame Maske. Abstände, Seitenanordnung, manuelle Verschiebung und Drehung beruhen auf diesen geglätteten Masken. Nach dem Packen wird nicht nochmals geglättet, damit sich Materialkanten und Sicherheitsabstände nicht nachträglich verändern. Das Exportmanifest nennt die aktive Glättungsoption.

Nach Änderungen an Konturglättung oder Bildauflösung **erneut anordnen**, bevor Laser-Seiten exportiert werden. Dabei werden manuelle Platzierungen ersetzt. Die Ausgabeauflösung der Laser-Seite ebenfalls ausreichend hoch wählen: Eine niedrigere Seiten-DPI tastet die feinere Teilemaske erneut gröber ab. Auch die gesamte Laser-Seite darf höchstens 16 Millionen Pixel enthalten.

## English

Choose **Contours → Smooth hard contours**. It is off by default and saved globally. **3× resolution, same physical size** enables smoothing and triples the selected image's scale percentage and DPI together, preserving intended centimetre dimensions subject to pixel rounding. Other loaded images and the default DPI for future imports stay unchanged. Limits are 16 million output pixels and 2400 DPI. Alternatively, select cm under Image size before increasing DPI.

Smoothing reconstructs masks with a 4 × 4 Catmull-Rom kernel and a 0.5 coverage threshold. Individual layers use a unique reconstructed color assignment to prevent overlaps at color junctions. Output pixels remain exact foreground/background colors with alpha 0 or 255. Merged masks are united before reconstruction, which can differ from uniting individually smoothed layers at color junctions. Group-color export chooses an existing selected reference color rather than blending colors. Ties prefer the nearest source cell, then the smaller group index. Preview, detail overlay and file export use the same output grid. At native resolution the original mask is retained. Enlargement cannot recover missing source details; check narrow gaps, thin strokes and small islands, as topology is not guaranteed for every scale.

For laser layouts, smoothing precedes material-component extraction. Spacing and all subsequent layout/editing operations use the resulting hard masks, with no extra smoothing after packing. Rearrange after contour/scale changes; doing so replaces manual edits. A sufficiently high laser page DPI is also necessary to retain the finer contour resolution.
