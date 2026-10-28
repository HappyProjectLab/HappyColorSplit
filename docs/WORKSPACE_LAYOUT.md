# Arbeitsbereich und Vorschauen / Workspace and previews

Diese Funktionen sind ab `0.1.0-preview.5` im Windows-Download enthalten.

## Deutsch

**Neu in 1.0.0:** Der Button **Anleitung** öffnet eine geführte Einführung mit 14 Schritten und sichtbaren Funktionsrahmen. Beim ersten Start wird sie angeboten. **Überspringen** beendet sie für die Sitzung, **Keine Tipps notwendig** speichert die Ablehnung und **Fertig** den Abschluss. Sie bleibt jederzeit erneut aufrufbar. [Bedienung und Verhalten](GUIDED_TOUR.md).

**Neu in 0.1.0-preview.7:** Doppelklick auf Farbfeld oder Farbbezeichnung in der Farbliste aktiviert dieselbe Hervorhebung im Originalbild und dieselbe Einzelvorschau wie ein Linksklick im Originalbild. Die kombinierte Vorschau wird ausgeschaltet; Export-Häkchen und Vorschauzoom bleiben erhalten. Die Häkchen selbst behalten ihre normale Bedienung.

**Rechtsklick im Originalbild:** Den Export-Haken der passenden Farbgruppe umkehren. Die betroffene Farbzeile und der Haken leuchten etwa eine halbe Sekunde auf und blenden weich aus. Bei eingeklappter Farbleiste wird sie geöffnet und die betroffene Zeile sichtbar gemacht. Wie beim Linksklick wird die angeklickte Farbe ausgewählt und hervorgehoben und ihre Einzelvorschau angezeigt; eine kombinierte Vorschau wird ausgeschaltet. Rechtsklicks auf transparente oder ausgefilterte Stellen entfernen wie Linksklicks die Hervorhebung, ohne Haken zu ändern. Ziehen mit rechts ändert keine Haken. Bei reduzierter Bewegung bleibt die Rückmeldung kurz statisch sichtbar.

**Neu in 0.1.0-preview.6:** Ein **Linksklick im Originalbild** hebt alle Flächen der erkannten Farbgruppe optisch zum Betrachter hin hervor. Kräftigere Lichtkanten und weiche Kontaktschatten erzeugen Tiefe; die Bildflächen werden nicht verschoben. Der Effekt blendet in etwa 120 ms weich ein, aus und zwischen Farben über. Bloßes Darüberfahren oder Verlassen des Bildes ändert die Hervorhebung nicht. Gleichzeitig wird die Gruppe in der Farbliste markiert und ihre Einzelvorschau angezeigt; **Farbauswahl vereinen** wird ausgeschaltet, ohne Export-Häkchen zu ändern. Ein Linksklick auf transparente oder ausgefilterte Stellen blendet den Effekt aus. Ziehen und Zoom erhalten die Hervorhebung. Reduzierte Bewegung überspringt die Übergänge. Es gibt keine Pipette und keinen zusätzlichen Modus.

Die Ansichtsbuttons sind einheitlich 32 Pixel hoch; Text und Symbole sind vertikal zentriert. Unter **Farbähnlichkeit** und **Mindestdeckkraft** erklären links- und rechtsbündige Texte die niedrige beziehungsweise hohe Einstellung. Die Zusammenfassung steht mit Abstand eine Zeile tiefer, zentriert und fett; sie bezeichnet keine mittlere Reglerposition.

Bei **Bilder hinzufügen** läuft die Aktivitätsanzeige dezent über den gesamten Fensterhintergrund, bis Dateiauswahl und Import beendet sind. Der bisherige zusätzliche Abbrechen-Knopf im Hauptfenster entfällt. Im Dateiauswahldialog lässt sich weiterhin abbrechen. Farberkennung und Export behalten ihre Fortschrittsanzeige.

- Über den Vorschauen mit dem einzelnen Button **Untereinander ⇄ Nebeneinander** umschalten. Beide Varianten bleiben sichtbar; die aktuelle ist kräftig hervorgehoben. Nebeneinander erhält zusätzlich einen helleren Buttonhintergrund. Das Original steht oben beziehungsweise links, die Exportvorschau unten beziehungsweise rechts. Die Fenstergröße ändert diese Auswahl nicht automatisch.
- Die Trennlinie zwischen den Vorschauen ziehen, um ihren Platzanteil zu ändern. Alternativ die Trennlinie mit Tab fokussieren und mit den Pfeiltasten verschieben. Beide Anordnungen behalten ihre eigene Trennposition.
- Die Pfeilgriffe an der inneren Kante der Bilder- und Farbleiste klappen die Bereiche einzeln ein. Eingeklappte Bereiche zeigen einen beschrifteten Griff mit Anzahl und einem langsamen, dezenten Lauflicht am inneren Rand. Ein schwächerer Lichtschimmer läuft synchron über die gesamte Grifffläche; beide Seitenleisten verwenden denselben Takt. Zum Öffnen klicken; bloßes Darüberfahren öffnet sie nicht.
- Ein gelber Punkt weist auf neue Inhalte im verborgenen Bereich hin, etwa neu geladene Bilder oder neu erkannte Farben. Er verschwindet beim Öffnen. Der Farbgriff nennt ausgewählte und erkannte Gruppen des aktuellen Bildes.
- **Leiste ausblenden** klappt beide Leisten gemeinsam ein. **Leiste einblenden** öffnet immer beide, auch wenn zuvor eine einzeln geschlossen war. Der Button ist hell markiert, solange beide Leisten ausgeblendet sind.
- Mausrad über einem Bild: zoomen. Links ziehen: den vergrößerten Bildausschnitt verschieben. **Einpassen** oder Doppelklick stellt die Gesamtansicht wieder her. Die kleinen Plus-/Minus-Knöpfe sind eine Alternative zum Mausrad. Bei fokussierter Vorschau funktionieren auch Plus/Minus sowie Home oder F zum Einpassen.
- **Zoom koppeln** koppelt Zoom und relative Bildposition beider Vorschauen. Beim Einschalten wird die zuletzt bediente Vorschau übernommen. Die unterschiedlichen Vorschaufenster können je nach Größe verschiedene Ausschnitte zeigen.
- **Farbauswahl vereinen** sitzt rechtsbündig in der Leiste über beiden Vorschauen. Hell: gemeinsame Vorschau aller angehakten Farbgruppen; dunkel: Vorschau der einzeln angeklickten Farbgruppe. Die Exportart wird dadurch nicht geändert.
- **Farbauszüge ⇄ Laser (experimentell)** und **Deutsch ⇄ English** funktionieren ebenfalls als einzelne Umschaltbuttons mit beiden sichtbaren Varianten. Die aktuelle Variante wird hervorgehoben; Laser beziehungsweise Englisch erhalten den helleren Hintergrund.
- **Windows-Einbindung** und **Lizenzen & Hinweise** öffnen jeweils eigene Fenster und verwenden dieselbe Gestaltung. Sie sind hell hervorgehoben, während ihr Fenster geöffnet ist. Die bisherigen Knöpfe **Ansicht** und **Detailansicht / Überlagerung** entfallen.

Der Vorschauzoom ist relativ zur eingepassten Ansicht: `1×` zeigt das gesamte Bild. Er verändert weder Pixelmaße noch Zentimetermaße oder DPI des Exports und rekonstruiert keine Bilddetails. Für Maße weiterhin **Bildgröße** verwenden. Beim Bildwechsel werden beide Vorschauen eingepasst.

Im **experimentellen Laser-Modus** nutzt die Arbeitsfläche den gesamten mittleren Bereich; die rechte Leiste enthält die Laser-Seiten. Die Kennzeichnung **Experimentell** ist im Arbeitsbereich dauerhaft sichtbar. Die eingestellte Farbvorschau-Anordnung bleibt für die Rückkehr erhalten. Die Leisten öffnen sich beim Moduswechsel nicht von selbst.

Die Laser-Aktionen stehen ebenfalls in der kompakten Leiste **über** dem Arbeitsbereich: Leisten ein-/ausblenden, Laser-Einstellungen, Anordnen, Bearbeiten, Rückgängig und Drehen. Sie verwenden dieselbe Höhe und Gestaltung wie die Farbauszüge; bei wenig Platz bricht die Leiste um. Rechts schaltet **Gruppenrahmen** die Rahmen in der Vorschau ein oder aus. Ein heller Button zeigt die eingeschaltete Option beziehungsweise das geöffnete Menü. Unter der Materialseite gibt es dafür keine zusätzliche Checkbox mehr. Die Rahmen werden weiterhin nicht exportiert.

Anordnung, Leistenzustand, beide Trennpositionen, Synchronisierung und reduzierte Bewegung werden in `%LOCALAPPDATA%\HappyColorSplit\settings.json` gespeichert. Zoom, Ausschnitt und Hinweispunkte gelten für die aktuelle Sitzung; die Bildliste wird weiterhin nicht als Projekt gespeichert.

Die erweiterte Einstellung `Workspace.ReduceMotion` bleibt in dieser JSON-Datei verfügbar: `true` schaltet Klappanimation und Lauflicht ab, Beschriftungen und Hinweise bleiben erhalten. Dafür gibt es nach Entfernen von „Ansicht“ keinen Menüknopf mehr. Die Einstellung bei geschlossenem Programm ändern.

## English

**New in 1.0.0:** **Guide** opens a 14-step introduction with visible feature outlines. It is offered on first launch. **Skip** dismisses it for this session, **No tips needed** saves the opt-out and **Finish** records completion. You can always open it again. [Behavior and instructions](GUIDED_TOUR.md).

**New in 0.1.0-preview.7:** Double-click a swatch or color label in the color list to activate the same original-image highlight and individual export preview as left-clicking the original. Combined preview turns off; export checkboxes and viewport zoom stay unchanged. The checkboxes themselves keep their usual behavior.

**Right-click the original:** Toggle the matching color group's export checkbox. Its row and checkbox light up for about half a second and fade out. A collapsed color sidebar opens and scrolls to the affected row. Just like left-clicking, this selects and highlights the clicked color and displays its individual layer, turning the combined preview off. Right-clicking transparent or filtered pixels clears the highlight just like left-clicking, without changing checkboxes. Right dragging changes no checkboxes. Reduced motion shows a brief static confirmation.

**New in 0.1.0-preview.6:** **Left-click the original** to bring all areas of the detected color group visually towards the viewer. Stronger bevel lighting and soft contact shadows add depth without moving image areas. The effect fades in, out and between groups over about 120 ms. Hovering and leaving the image do not change the highlight. The color list highlights the matching group and its individual layer is shown; **Merge selected colors** turns off without changing export checkboxes. Left-click transparent or opacity-filtered pixels to fade the effect out. Pan and zoom preserve the highlight. Reduced motion skips transitions. No eyedropper or separate mode is needed.

These features are included in the Windows download starting with `0.1.0-preview.5`.

View buttons share a compact 32-pixel height with vertically centered text and symbols. Under **Color similarity** and **Minimum opacity**, left- and right-aligned labels describe the low and high settings. The bold, centered summary sits on a separate line below with extra spacing; it does not label the slider's midpoint.

During **Add images**, subtle activity travels across the entire window background until the picker and import finish. The redundant main-window Cancel button has been removed; cancelling the native picker still works. Color analysis and export retain their progress indication.

- Use the single **Stacked ⇄ Side by side** button above the previews. Both variants stay visible and the current one is emphasized. Side by side also uses a brighter button background. Original is above/left; export preview is below/right. Resizing the window never switches the layout automatically.
- Drag the divider to resize the previews, or focus it with Tab and use the arrow keys. Each orientation retains its own divider position.
- Use the inner-edge arrow handles to collapse the image and color sidebars independently. Collapsed panes have labelled handles with counts and a slow, subtle running light along their inner edge. A fainter glow travels across the entire handle surface in sync with the edge; both sidebars share the same phase. Click to open; hovering does not open a pane.
- An amber dot signals new content in a hidden pane, such as imported images or detected colors. Opening the pane clears the notification. The color handle shows checked/total groups for the current image.
- **Hide sidebars** closes both panes together. **Show sidebars** always opens both, even if one had previously been closed individually. The button is highlighted while both panes are hidden.
- Use the mouse wheel to zoom and left-drag to pan a zoomed image. **Fit** or double-click restores the fitted view. Plus/minus buttons offer an alternative to the wheel. With the preview focused, Plus/Minus zoom and Home or F fits the image.
- **Link zoom** links zoom and normalized image position, initially copying the most recently used preview. Different viewport dimensions may show different crops.
- **Merge selected colors** sits at the right of the toolbar above both previews. Highlighted: preview all checked color groups together; dark: preview the individually clicked group. This does not change the export mode.
- **Color layers ⇄ Laser (experimental)** and **Deutsch ⇄ English** are also single buttons with both variants visible. The current variant is emphasized; Laser and English additionally use a brighter background.
- **Windows integration** and **Licenses & notices** each open a separate window, use the same styling and stay highlighted while their window is open. The previous **View** and **Detail view / overlay** buttons have been removed.

Preview zoom is relative to the fitted view: `1×` fits the entire image. It never changes export pixels, physical dimensions or DPI, and cannot recover missing image detail. Use **Image size** to set export dimensions. Selecting another source fits both previews again.

In **experimental Laser** mode, the sheet uses the entire central area and the right sidebar contains laser pages. An **Experimental** badge stays visible in the workspace. The color-preview layout is preserved for your return. Switching modes never opens sidebars automatically.

Laser actions also live in the compact toolbar **above** the workspace: show/hide sidebars, settings, arrange, edit, undo and rotate. They use the same height and styling as the color-layer controls and wrap when space is limited. The right-aligned **Group outlines** button switches preview outlines on or off; highlighted buttons indicate enabled options or open menus. The former checkbox below the material sheet has been removed. Group outlines are still excluded from exports.

Layout, collapsed states, both divider positions, synchronization and reduced motion are saved in `%LOCALAPPDATA%\HappyColorSplit\settings.json`. Zoom, pan and notification dots apply to the current session; the image list is still not saved as a project.

The advanced `Workspace.ReduceMotion` setting remains available in this JSON file: `true` disables sidebar transitions and running lights while retaining labels and notifications. There is no menu button for it after removing View. Close the application before editing the setting.
