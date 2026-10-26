# Arbeitsbereich und Vorschauen / Workspace and previews

Diese Funktionen gehören zum vorbereiteten Entwicklungsstand und sind noch nicht im veröffentlichten Download `0.1.0-preview.4` enthalten.

## Deutsch

- Über den Vorschauen mit dem einzelnen Button **Untereinander ⇄ Nebeneinander** umschalten. Beide Varianten bleiben sichtbar; die aktuelle ist kräftig hervorgehoben. Nebeneinander erhält zusätzlich einen helleren Buttonhintergrund. Das Original steht oben beziehungsweise links, die Exportvorschau unten beziehungsweise rechts. Die Fenstergröße ändert diese Auswahl nicht automatisch.
- Die Trennlinie zwischen den Vorschauen ziehen, um ihren Platzanteil zu ändern. Alternativ die Trennlinie mit Tab fokussieren und mit den Pfeiltasten verschieben. Beide Anordnungen behalten ihre eigene Trennposition.
- Die Pfeilgriffe an der inneren Kante der Bilder- und Farbleiste klappen die Bereiche einzeln ein. Eingeklappte Bereiche zeigen einen beschrifteten Griff mit Anzahl und einem langsamen, dezenten Lauflicht am inneren Rand. Ein schwächerer Lichtschimmer läuft synchron über die gesamte Grifffläche; beide Seitenleisten verwenden denselben Takt. Zum Öffnen klicken; bloßes Darüberfahren öffnet sie nicht.
- Ein gelber Punkt weist auf neue Inhalte im verborgenen Bereich hin, etwa neu geladene Bilder oder neu erkannte Farben. Er verschwindet beim Öffnen. Der Farbgriff nennt ausgewählte und erkannte Gruppen des aktuellen Bildes.
- **Leiste ausblenden** klappt beide Leisten gemeinsam ein. **Leiste einblenden** öffnet immer beide, auch wenn zuvor eine einzeln geschlossen war. Der Button ist hell markiert, solange beide Leisten ausgeblendet sind.
- Mausrad über einem Bild: zoomen. Links ziehen: den vergrößerten Bildausschnitt verschieben. **Einpassen** oder Doppelklick stellt die Gesamtansicht wieder her. Die kleinen Plus-/Minus-Knöpfe sind eine Alternative zum Mausrad. Bei fokussierter Vorschau funktionieren auch Plus/Minus sowie Home oder F zum Einpassen.
- **Zoom koppeln** koppelt Zoom und relative Bildposition beider Vorschauen. Beim Einschalten wird die zuletzt bediente Vorschau übernommen. Die unterschiedlichen Vorschaufenster können je nach Größe verschiedene Ausschnitte zeigen.
- **Farbauswahl vereinen** sitzt rechtsbündig in der Leiste über beiden Vorschauen. Hell: gemeinsame Vorschau aller angehakten Farbgruppen; dunkel: Vorschau der einzeln angeklickten Farbgruppe. Die Exportart wird dadurch nicht geändert.
- **Farbauszüge ⇄ Laser** und **Deutsch ⇄ English** funktionieren ebenfalls als einzelne Umschaltbuttons mit beiden sichtbaren Varianten. Die aktuelle Variante wird hervorgehoben; Laser beziehungsweise Englisch erhalten den helleren Hintergrund.
- **Windows-Einbindung** und **Lizenzen & Hinweise** verwenden dieselbe Gestaltung. Sie sind hell hervorgehoben, während ihr Menü beziehungsweise Fenster geöffnet ist. Die bisherigen Knöpfe **Ansicht** und **Detailansicht / Überlagerung** entfallen.

Der Vorschauzoom ist relativ zur eingepassten Ansicht: `1×` zeigt das gesamte Bild. Er verändert weder Pixelmaße noch Zentimetermaße oder DPI des Exports und rekonstruiert keine Bilddetails. Für Maße weiterhin **Bildgröße** verwenden. Beim Bildwechsel werden beide Vorschauen eingepasst.

Im **Laser-Modus** nutzt die Arbeitsfläche den gesamten mittleren Bereich; die rechte Leiste enthält die Laser-Seiten. Die eingestellte Farbvorschau-Anordnung bleibt für die Rückkehr erhalten. Die Leisten öffnen sich beim Moduswechsel nicht von selbst.

Anordnung, Leistenzustand, beide Trennpositionen, Synchronisierung und reduzierte Bewegung werden in `%LOCALAPPDATA%\HappyColorSplit\settings.json` gespeichert. Zoom, Ausschnitt und Hinweispunkte gelten für die aktuelle Sitzung; die Bildliste wird weiterhin nicht als Projekt gespeichert.

Die erweiterte Einstellung `Workspace.ReduceMotion` bleibt in dieser JSON-Datei verfügbar: `true` schaltet Klappanimation und Lauflicht ab, Beschriftungen und Hinweise bleiben erhalten. Dafür gibt es nach Entfernen von „Ansicht“ keinen Menüknopf mehr. Die Einstellung bei geschlossenem Programm ändern.

## English

These features belong to the prepared development version and are not included in the published `0.1.0-preview.4` download yet.

- Use the single **Stacked ⇄ Side by side** button above the previews. Both variants stay visible and the current one is emphasized. Side by side also uses a brighter button background. Original is above/left; export preview is below/right. Resizing the window never switches the layout automatically.
- Drag the divider to resize the previews, or focus it with Tab and use the arrow keys. Each orientation retains its own divider position.
- Use the inner-edge arrow handles to collapse the image and color sidebars independently. Collapsed panes have labelled handles with counts and a slow, subtle running light along their inner edge. A fainter glow travels across the entire handle surface in sync with the edge; both sidebars share the same phase. Click to open; hovering does not open a pane.
- An amber dot signals new content in a hidden pane, such as imported images or detected colors. Opening the pane clears the notification. The color handle shows checked/total groups for the current image.
- **Hide sidebars** closes both panes together. **Show sidebars** always opens both, even if one had previously been closed individually. The button is highlighted while both panes are hidden.
- Use the mouse wheel to zoom and left-drag to pan a zoomed image. **Fit** or double-click restores the fitted view. Plus/minus buttons offer an alternative to the wheel. With the preview focused, Plus/Minus zoom and Home or F fits the image.
- **Link zoom** links zoom and normalized image position, initially copying the most recently used preview. Different viewport dimensions may show different crops.
- **Merge selected colors** sits at the right of the toolbar above both previews. Highlighted: preview all checked color groups together; dark: preview the individually clicked group. This does not change the export mode.
- **Color layers ⇄ Laser** and **Deutsch ⇄ English** are also single buttons with both variants visible. The current variant is emphasized; Laser and English additionally use a brighter background.
- **Windows integration** and **Licenses & notices** use the same styling and stay highlighted while their menu or window is open. The previous **View** and **Detail view / overlay** buttons have been removed.

Preview zoom is relative to the fitted view: `1×` fits the entire image. It never changes export pixels, physical dimensions or DPI, and cannot recover missing image detail. Use **Image size** to set export dimensions. Selecting another source fits both previews again.

In **Laser** mode, the sheet uses the entire central area and the right sidebar contains laser pages. The color-preview layout is preserved for your return. Switching modes never opens sidebars automatically.

Layout, collapsed states, both divider positions, synchronization and reduced motion are saved in `%LOCALAPPDATA%\HappyColorSplit\settings.json`. Zoom, pan and notification dots apply to the current session; the image list is still not saved as a project.

The advanced `Workspace.ReduceMotion` setting remains available in this JSON file: `true` disables sidebar transitions and running lights while retaining labels and notifications. There is no menu button for it after removing View. Close the application before editing the setting.
