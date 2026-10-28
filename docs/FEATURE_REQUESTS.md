# Feature Requests

Sammlung gewünschter Erweiterungen. Offene Einträge sind keinem festen Release zugeordnet; umgesetzte Einträge nennen die zugehörige Version.

## 1. Farbwahl direkt im Originalbild

- [x] Ergänzung in 0.1.0-preview.7: Doppelklick auf eine Farbe in der Farbliste aktiviert denselben Ablauf wie ein Linksklick im Originalbild, einschließlich Hervorhebung, Einzelvorschau und Ausschalten der kombinierten Vorschau. Export-Häkchen und Zoom bleiben erhalten.

- [x] Umgesetzt in 0.1.0-preview.6: Erst beim Linksklick alle Flächen derselben erkannten Farbgruppe mit feiner Lichtkante und weichem Kontaktschatten räumlich zum Betrachter hin hervorheben. Die Flächen bleiben an ihrer ursprünglichen Bildposition; keine Verschiebung nach oben, keine Pipette und kein zusätzlicher Modus.
- Kräftigere Lichtkante und Kontrast, weiches Ein-, Aus- und Überblenden in etwa 120 ms. Die Einstellung für reduzierte Bewegung überspringt den Übergang.
- Die zugehörige Farbgruppe in der Farbliste gleichzeitig hervorheben. Die Hervorhebung bleibt beim Darüberfahren und Verlassen des Bildes erhalten, bis eine andere Farbe oder eine freie Stelle angeklickt wird.
- Ein Klick wählt die Gruppe aus und zeigt ihre Einzelvorschau; **Farbauswahl vereinen** wird dabei ausgeschaltet. Export-Häkchen bleiben unverändert.
- Ein Rechtsklick auf eine erkannte Farbfläche kehrt den Export-Haken der passenden Gruppe um. Die Farbzeile und eine Umrandung am Haken leuchten kurz auf und blenden anschließend aus. Bei eingeklappter Farbleiste diese öffnen und die betroffene Zeile sichtbar scrollen. Wie beim Linksklick wird die angeklickte Farbe ausgewählt und hervorgehoben und ihre Einzelvorschau angezeigt; eine kombinierte Vorschau wird ausgeschaltet. Ein Rechtsklick auf transparente oder ausgefilterte Stellen entfernt wie ein Linksklick die Hervorhebung, ohne Haken zu ändern. Ziehen mit rechts ändert keine Haken.
- Ein Linksklick auf transparente oder durch die Mindestdeckkraft ausgefilterte Pixel blendet die Hervorhebung aus und wählt keine neue Gruppe. Zoom, Verschieben und Doppelklick zum Einpassen bleiben erhalten; Zoom und Verschieben erhalten die Hervorhebung.

## 2. Update-Funktion

- [x] Umgesetzt in 0.1.0-preview.6: Beim Programmstart die GitHub-Releases auf eine neuere passende Version prüfen. Der Update-Button rechts neben der Überschrift erscheint nur bei einem bestätigten neueren Release und zeigt Version und Veröffentlichungsdatum.
- Button im vorhandenen Toolbar-Stil mit dezentem Lauflicht; reduzierte Bewegung zeigt eine statische Umrandung. Bei kleinen Fenstern kann er innerhalb des Titelbereichs umbrechen.
- Nach Bestätigung und erneuter Herkunftsprüfung die konkrete Release-Seite im externen Standardbrowser öffnen. Download und Installation erfolgen manuell.
- Feste GitHub-IDs von Repository, Eigentümerorganisation und Release-Autor prüfen, um gelöschte und unter gleichem Namen neu angelegte Accounts/Repositorys abzulehnen. Nur feste HTTPS-Endpunkte und geprüfte Release-Seiten zulassen. Details und Grenzen: [UPDATES.md](UPDATES.md).

## 3. Schritt-für-Schritt-Anleitung

- [x] Umgesetzt in 1.0.0: Eine geführte Anleitung mit 14 Schritten durch die Hauptfunktionen anbieten.
- Die jeweils erklärte Funktion mit einer gut sichtbaren Umrandung hervorheben und den zugehörigen Schritt kurz erklären.
- Die Steuerung rechts unten anzeigen: **Weiter**, **Zurück**, **Überspringen** und **Keine Tipps notwendig**.
- Beim ersten Start als Einführung anbieten. Überspringen beendet sie für diese Sitzung; Keine Tipps notwendig speichert eine dauerhafte Ablehnung. Fertig speichert den Abschluss. Über Anleitung jederzeit erneut aufrufbar. Vorherigen Leisten- und Moduszustand beim Beenden wiederherstellen; Export-Häkchen, Bilddaten und Vorschauzoom bleiben erhalten.

## 4. Versionshinweis

- [x] Umgesetzt in 0.1.0-preview.6: Die aktuelle Programmversion dauerhaft unter der Überschrift anzeigen, aus der Versionsangabe der Anwendung auslesen. Der Tooltip nennt das Ergebnis der Update-Prüfung. Neue Version und Veröffentlichungsdatum stehen zusätzlich im Update-Button und in der Browser-Bestätigung.
