# Versionsanzeige und GitHub-Updates

Diese Funktion ist ab `0.1.0-preview.6` enthalten. Frühere Downloads enthalten noch keine Update-Prüfung.

## Bedienung

Die aktuelle Programmversion steht dauerhaft unter der Überschrift **HappyColorSplit**. Sie stammt aus der beim Bauen hinterlegten Anwendungsversion; eine zusätzliche Kopie der Versionsnummer wird in der Oberfläche nicht gepflegt.

Beim Programmstart werden die öffentlichen GitHub-Metadaten des offiziellen Repositorys und bis zu 100 zuletzt veröffentlichten Releases abgerufen. Währenddessen bleibt die Bildbearbeitung bedienbar. Bilder, Dateipfade und Einstellungen werden nicht übertragen; GitHub erhält die üblichen Verbindungsdaten sowie den Programmnamen und die Version im User-Agent. Es werden keine Zugangsdaten verwendet.

Nur wenn eine neuere, passende und herkunftsgeprüfte Version vorliegt, erscheint rechts neben dem Programmtitel ein Button mit Versionsnummer, Veröffentlichungsdatum und einem dezenten Lauflicht. Er hat denselben Stil wie die benachbarten Buttons. Bei wenig Platz bricht er innerhalb des Titelbereichs um. Bei reduzierter Bewegung bleibt eine statische Umrandung. Datum und Beschriftung folgen der gewählten Sprache; das Datum wird in der lokalen Zeitzone angezeigt.

Ein Klick fragt, ob die angegebene GitHub-Release-Seite im externen Standardbrowser geöffnet werden soll. Nach Bestätigung wird das Angebot erneut geprüft. Hat es sich geändert oder kann seine Herkunft nicht mehr bestätigt werden, wird kein Browser geöffnet. Ein geändertes Angebot benötigt eine neue Bestätigung. Download und Installation erfolgen anschließend manuell im Browser; die Anwendung lädt und startet keine Update-Dateien.

Ohne neuere Version, ohne Netzwerk, bei GitHub-Limits oder bei fehlgeschlagener Herkunftsprüfung bleibt der Button verborgen. Der Tooltip der aktuellen Versionsanzeige zeigt den Prüfstatus. Der nächste Programmstart prüft erneut; es gibt keinen dauernden Hintergrundabruf und keinen zwischengespeicherten Update-Link.

## Versionsvergleich

Der Vergleich folgt Semantic Versioning, einschließlich numerischer Vorabversionsnummern: `preview.10` ist neuer als `preview.9`, die fertige `0.1.0` neuer als `0.1.0-preview.5`. Build-Metadaten hinter `+` lösen kein Update aus. Tags dürfen mit `v` beginnen. Entwürfe und ungültige Versionsangaben werden ausgeschlossen. Vorabinstallationen erhalten auch neuere Vorabversionen; stabile Installationen erhalten nur stabile Versionen. Unter den passenden Angeboten wird die höchste Version gewählt, unabhängig von der Reihenfolge der GitHub-Liste.

## Schutz vor wiederverwendeten Namen

Der Programmcode enthält diese am **27.10.2026** über die öffentliche GitHub-API abgeglichenen numerischen Identitäten:

| Identität | Fest hinterlegte ID |
| --- | --- |
| Repository `HappyProjectLab/HappyColorSplit` | `1426805595` |
| Eigentümerorganisation `HappyProjectLab` | `330600945` |
| Bisheriger Release-Autor | `8838505` |

Die IDs werden bei GitHub über HTTPS geprüft. Der Repository-Name allein reicht ausdrücklich nicht. Wenn ein Account oder Repository gelöscht und unter demselben Namen neu angelegt wird, stimmen seine neuen IDs nicht mit diesen Vorgaben überein. Ein solches Angebot wird verworfen. Auch ein Release eines anderen Autors wird nicht angeboten. Bei einem beabsichtigten Wechsel von Organisation, Repository oder veröffentlichendem Account müssen die Vorgaben bewusst im Quellcode angepasst und mit einer neuen vertrauenswürdigen Programmversion ausgeliefert werden. Die Anwendung übernimmt niemals automatisch eine neue Identität.

Die Anwendung nutzt nur feste GitHub-API-Endpunkte. Automatische HTTP-Weiterleitungen sind ausgeschaltet. Antworten haben Zeit- und Größenlimits und werden auf erwartete Felder geprüft. Browser-Links müssen auf die konkrete HTTPS-Release-Seite dieses Repositorys zeigen; sie werden aus dem geprüften Tag aufgebaut und unmittelbar vor dem Öffnen nochmals zugelassen. Fremde Hosts, andere Repositorys, lokale Dateien, HTTP statt HTTPS sowie zusätzliche URL-Parameter werden abgelehnt.

## Grenzen

Diese Prüfung schützt gegen einen späteren Namensnachfolger unter den hier hinterlegten GitHub-Identitäten. Sie vertraut GitHub, HTTPS und dem Betriebssystem-Zertifikatsspeicher. Sie ist **keine kryptografische Signatur der Download-Dateien** und schützt nicht vor einer Übernahme des bestehenden GitHub-Kontos, einer Kompromittierung von GitHub oder einem nach der Prüfung manipulierten Release. Auch der Browser kann nach dem Öffnen weitergeleitet werden; die Anwendung kontrolliert die Website nicht.

Ein SHA-256-Hash, der zusammen mit einer Datei vom selben möglicherweise fremden Account angeboten wird, würde dessen Urheberschaft nicht beweisen. Für einen späteren automatischen Download oder eine Installation wäre zusätzlich eine unabhängige Release-Signatur sinnvoll: Der private Signaturschlüssel bleibt beim Herausgeber, nur der öffentliche Prüfschlüssel wird mit der Anwendung ausgeliefert. Der aktuelle Schritt bietet bewusst ausschließlich den geprüften Link zum manuellen Öffnen an.

## Prüfung und Wartung

Die automatischen Prüfungen verwenden nachgebildete HTTP-Antworten und einen nachgebildeten Browser. Sie decken neue und alte Versionen, Preview-Vergleiche, andere Repository-/Organisations-/Autoren-IDs trotz gleicher Namen, manipulierte Links, Weiterleitungen, GitHub-Limits, ungültige und zu große Antworten, Bestätigung/Abbrechen sowie eine Herkunftsänderung zwischen Anzeige und Öffnen ab. Die Oberfläche wird auf Deutsch und Englisch sowie bei kleiner Fensterbreite geprüft. Ein gesonderter, ausdrücklich aufzurufender Live-Test überprüft die Herkunft mit der echten GitHub-API:

```powershell
dotnet run --project Tests/HappyColorSplit.Tests.csproj -- --updates-only
dotnet run --project Tests/HappyColorSplit.Tests.csproj -- --updates-live
```

Für ein neues Release die `Version` in der Projektdatei und die übrigen Release-Angaben wie in [WINDOWS_RELEASE.md](WINDOWS_RELEASE.md) aktualisieren. Ohne höhere Anwendungsversion wäre die neue Ausgabe gegenüber der alten Installation nicht als Update erkennbar.

## English

The current application version is always visible below the title. At startup, the app checks the official public GitHub repository and release metadata. A toolbar-style update button with the new version and publication date appears only for a newer matching release whose repository, organization and publisher IDs match the pinned values above. Preview installations may receive newer previews; stable installations receive stable versions only.

Clicking asks whether to open that release in the external default browser. After confirmation, the app checks the identities and offer again before opening the canonical HTTPS release page. It never downloads or installs update files. The running light respects reduced motion. Offline checks, rate limits and failed identity verification leave the button hidden; the current-version tooltip reports the check status.

Pinned GitHub IDs protect against deleted accounts or repositories recreated under the same names. They rely on GitHub and HTTPS; they are not signatures of the downloaded files and do not protect against compromise of the original account or GitHub. An eventual automatic installer should additionally verify independently signed release metadata with a public key embedded in the application.
