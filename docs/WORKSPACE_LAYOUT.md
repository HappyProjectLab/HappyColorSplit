# Arbeitsbereich und Vorschauen / Workspace and previews

Diese Funktionen gehören zum vorbereiteten Entwicklungsstand und sind noch nicht im veröffentlichten Download `0.1.0-preview.4` enthalten.

## Deutsch

- Über den Vorschauen **Untereinander** oder **Nebeneinander** wählen. Das Original steht oben beziehungsweise links, die Exportvorschau unten beziehungsweise rechts. Die Fenstergröße ändert diese Auswahl nicht automatisch.
- Die Trennlinie zwischen den Vorschauen ziehen, um ihren Platzanteil zu ändern. Alternativ die Trennlinie mit Tab fokussieren und mit den Pfeiltasten verschieben. Beide Anordnungen behalten ihre eigene Trennposition.
- Die Pfeilgriffe an der inneren Kante der Bilder- und Farbleiste klappen die Bereiche einzeln ein. Eingeklappte Bereiche zeigen einen beschrifteten Griff mit Anzahl und einem langsamen, dezenten Lauflicht. Zum Öffnen klicken; bloßes Darüberfahren öffnet sie nicht.
- Ein gelber Punkt weist auf neue Inhalte im verborgenen Bereich hin, etwa neu geladene Bilder oder neu erkannte Farben. Er verschwindet beim Öffnen. Der Farbgriff nennt ausgewählte und erkannte Gruppen des aktuellen Bildes.
- **Nur Vorschauen** klappt beide Leisten ein. **Leisten zeigen** stellt ihren vorherigen Zustand wieder her. Nach einem Neustart mit zwei eingeklappten Leisten öffnet dieser Knopf beide.
- Mausrad über einem Bild: zoomen. Links ziehen: den vergrößerten Bildausschnitt verschieben. **Einpassen** oder Doppelklick stellt die Gesamtansicht wieder her. Die kleinen Plus-/Minus-Knöpfe sind eine Alternative zum Mausrad. Bei fokussierter Vorschau funktionieren auch Plus/Minus sowie Home oder F zum Einpassen.
- **Synchron** koppelt Zoom und relative Bildposition beider Vorschauen. Beim Einschalten wird die zuletzt bediente Vorschau übernommen. Die unterschiedlichen Vorschaufenster können je nach Größe verschiedene Ausschnitte zeigen.
- Unter **Ansicht → Bewegung reduzieren** lassen sich Klappanimation und Lauflicht abschalten. Beschriftungen, Pfeile und Hinweispunkte bleiben erhalten.

Der Vorschauzoom ist relativ zur eingepassten Ansicht: `1×` zeigt das gesamte Bild. Er verändert weder Pixelmaße noch Zentimetermaße oder DPI des Exports und rekonstruiert keine Bilddetails. Für Maße weiterhin **Bildgröße**, für die detaillierte Überlagerung **Detailansicht / Überlagerung** verwenden. Beim Bildwechsel werden beide Vorschauen eingepasst.

Im **Laser-Modus** nutzt die Arbeitsfläche den gesamten mittleren Bereich; die rechte Leiste enthält die Laser-Seiten. Die eingestellte Farbvorschau-Anordnung bleibt für die Rückkehr erhalten. Die Leisten öffnen sich beim Moduswechsel nicht von selbst.

Anordnung, Leistenzustand, beide Trennpositionen, Synchronisierung und reduzierte Bewegung werden in `%LOCALAPPDATA%\HappyColorSplit\settings.json` gespeichert. Zoom, Ausschnitt und Hinweispunkte gelten für die aktuelle Sitzung; die Bildliste wird weiterhin nicht als Projekt gespeichert.

## English

These features belong to the prepared development version and are not included in the published `0.1.0-preview.4` download yet.

- Choose **Stacked** or **Side by side** above the previews. Original is above/left; export preview is below/right. Resizing the window never switches the layout automatically.
- Drag the divider to resize the previews, or focus it with Tab and use the arrow keys. Each orientation retains its own divider position.
- Use the inner-edge arrow handles to collapse the image and color sidebars independently. Collapsed panes have labelled handles with counts and a slow, subtle running light. Click to open; hovering does not open a pane.
- An amber dot signals new content in a hidden pane, such as imported images or detected colors. Opening the pane clears the notification. The color handle shows checked/total groups for the current image.
- **Previews only** closes both sidebars. **Show sidebars** restores their previous individual states. After a restart with both panes closed, this button opens both.
- Use the mouse wheel to zoom and left-drag to pan a zoomed image. **Fit** or double-click restores the fitted view. Plus/minus buttons offer an alternative to the wheel. With the preview focused, Plus/Minus zoom and Home or F fits the image.
- **Sync** links zoom and normalized image position, initially copying the most recently used preview. Different viewport dimensions may show different crops.
- **View → Reduce motion** disables sidebar transitions and running lights. Labels, arrows and notification dots remain visible.

Preview zoom is relative to the fitted view: `1×` fits the entire image. It never changes export pixels, physical dimensions or DPI, and cannot recover missing image detail. Use **Image size** to set export dimensions and **Detail view / overlay** for a detailed comparison. Selecting another source fits both previews again.

In **Laser** mode, the sheet uses the entire central area and the right sidebar contains laser pages. The color-preview layout is preserved for your return. Switching modes never opens sidebars automatically.

Layout, collapsed states, both divider positions, synchronization and reduced motion are saved in `%LOCALAPPDATA%\HappyColorSplit\settings.json`. Zoom, pan and notification dots apply to the current session; the image list is still not saved as a project.
