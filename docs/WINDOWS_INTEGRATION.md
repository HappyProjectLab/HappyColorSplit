# Bilder aus dem Explorer hinzufügen / Import from Explorer

## Deutsch

Diese Funktionen sind ab `0.1.0-preview.4` enthalten.

- **Drag-and-drop:** Eine oder mehrere PNG-/JPG-/JPEG-Dateien aus dem Explorer in das Fenster ziehen. Die vorhandene Bildliste wird ergänzt. Dieselben normalisierten Dateipfade werden nicht doppelt aufgenommen. Ordner werden nicht durchsucht. Während Laden, Erkennung oder Export wird kein weiterer Drop angenommen.
- **Einrichten:** Das vollständige Programmpaket zuerst an seinen dauerhaften Speicherort entpacken. `HappyColorSplit.exe` starten und oben **Windows-Einbindung → Explorer-Einträge einrichten / aktualisieren** wählen. Die Einträge gelten nur für das aktuelle Benutzerkonto. Das Standardprogramm für Bilder bleibt unverändert.
- **Öffnen mit:** Bei PNG, JPG oder JPEG im Explorer **Öffnen mit → HappyColorSplit** auswählen; ggf. unter den weiteren Apps suchen. Windows kann die Darstellung und Reihenfolge der Vorschläge selbst bestimmen.
- **Senden an:** Eine oder mehrere Dateien markieren und **Senden an → HappyColorSplit** wählen. Unter Windows 11 ggf. zuerst **Weitere Optionen anzeigen** öffnen. Das Windows-Menü „Senden an“ lässt sich nicht auf bestimmte Dateiendungen begrenzen. HappyColorSplit lädt trotzdem ausschließlich PNG/JPG/JPEG und zeigt für andere Eingaben eine Fehlermeldung.
- **Mehrere Aufrufe:** Jeder Start aus dem Explorer öffnet ein neues Programmfenster. Zum Ergänzen einer bereits geöffneten Bildliste Drag-and-drop verwenden. Windows kann bei „Öffnen mit“ für mehrere Dateien einzelne Programmstarts auslösen; „Senden an“ übergibt eine gemeinsame Auswahl.
- **Verschieben / Entfernen:** Nach einem Umzug des Programmordners die Einträge aus der neuen EXE erneut einrichten. Vor dem Löschen des Programms **Explorer-Einträge entfernen** wählen. Dies entfernt die von HappyColorSplit angelegten Einträge und die eigene Senden-an-Verknüpfung. Von Windows selbst gespeicherte Auswahllisten und manuell gesetzte Standardprogramme werden nicht bearbeitet.

Die Einbindung erfolgt freiwillig. Beim normalen Programmstart und beim Laden von Bildern werden keine Explorer-Einträge eingerichtet. Alle Ladewege verwenden dieselbe Prüfung der Dateiendung und des tatsächlichen Bildformats. Die Quelldateien werden zum Laden nur gelesen.

Technisch werden ausschließlich eigene Einträge unter `HKEY_CURRENT_USER\Software\Classes` sowie `HappyColorSplit.lnk` im Senden-an-Ordner des Benutzers eingerichtet. Es gibt keine Zuordnung für andere Dateiendungen, keine Änderungen an `UserChoice` und keine Änderungen unter `HKEY_LOCAL_MACHINE`. Der Startbefehl setzt Programmpfad und Dateipfad jeweils in Anführungszeichen. Zusätzliche NuGet-Pakete sind nicht erforderlich.

## English

These features are included starting with `0.1.0-preview.4`.

- **Drag and drop:** Drop one or more PNG/JPG/JPEG files into the window to add them to the image list. Normalized duplicate paths are skipped. Folders are not scanned. Drops are unavailable while loading, processing or exporting.
- **Set up:** Extract the entire application package to its permanent location first. Run `HappyColorSplit.exe`, then choose **Windows integration → Set up / update Explorer entries**. This only affects your account, requires no administrator privileges and leaves your default image viewer unchanged.
- **Open with:** Right-click a PNG, JPG or JPEG and choose **Open with → HappyColorSplit**. You may need to expand the list of apps. Windows controls how suggestions appear.
- **Send to:** Select one or more files and choose **Send to → HappyColorSplit**. On Windows 11, first select **Show more options** if necessary. Windows displays Send to entries for all file types; HappyColorSplit rejects unsupported input instead of loading it.
- **Repeated launches:** Each Explorer launch opens a new window. To add to an existing window, use drag and drop. Open with may start one instance per selected file; Send to passes the selection together.
- **Moving / removing:** Run setup again from the new executable after moving the application. Before deleting it, choose **Remove Explorer entries**. This removes the application's own registrations and Send to shortcut. Windows-maintained selection histories and manually chosen defaults are left untouched.

Integration is optional and never enabled automatically. All import routes check both the extension and the encoded image format. Input images are opened for reading only. Registration uses the current user's `Software\Classes` keys and Send to directory; it does not modify `UserChoice` or machine-wide settings. No additional NuGet dependencies are needed.
