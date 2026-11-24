# STEFFLIX – Schnittstellen und Datenformate

Verträge zwischen Weboberfläche, Desktop (`STEFFLIX.exe`), Android-App und Release: Nachrichten der beiden Brücken, Namen, an denen andere Komponenten hängen, Dateiformate, Adressen und gespeicherte Schlüssel. Zusammenhänge und Abläufe: `docs/ARCHITECTURE.md`. Gelesen von Architekt und Entwicklung (vor Änderungen an diesen Stellen), Prüfung (Agent `schnittstellen-pruefer`). Pflege: wer eine Schnittstelle ändert, führt diese Datei im selben Schritt nach; Formatänderungen nur mit Entscheid und Migrationsplan.

**Grundregeln:** Keine stillen Schnittstellenänderungen. Felder nur hinzufügen, nicht umdeuten oder entfernen, solange ältere Installationen sie lesen. Unbekannte Felder und Nachrichten ignorieren alle Seiten.

## 1. Brücke Weboberfläche ↔ Desktop (WebView2)
Kanal: `window.chrome.webview` im Player-Fenster. JavaScript: `sendToDesktop`, `initDesktopApp`, `handleDesktopMessage` (ui.js). C#: `PlayerForm.OnWebMessage`, `PostEvent`, `ImportBackup` (PlayerForm.cs). Erkennung in STEFFLIX: `isDesktopApp()` (core.js) = `window.chrome.webview.postMessage` vorhanden. Im Browser und in der App tut die Brücke nichts.

**STEFFLIX → Desktop** (`postMessage` mit JSON-Text, Feld `cmd`). Der Desktop nimmt nur Nachrichten eigener Seiten an (Installationsordner, `about:`, `data:`, `blob:`; `IsInternal`).

| `cmd` | Felder | Absender | Wirkung im Desktop |
|---|---|---|---|
| `ready` | – | main.js nach dem Seitenaufbau (bei jedem Laden) | Beim ersten Mal: Datenübernahme anbieten (einmalig, `player.dataImportAsked`), dann Update-Prüfung |
| `tools` | `page` (heute immer `"tools"`) | Menüeintrag „Tools“ | Fenster der Tools öffnen bzw. nach vorne holen |
| `fullscreen` | – | Taste F11 | Vollbild des Fensters umschalten |
| `reload` | – | Taste F5 | Seite neu laden |
| `importResult` | `ok` (bool), `message` (Text) | `importBackupFromDesktop` (storage.js) | Ergebnis im Assistenten der Datenübernahme anzeigen |
| `pipWindow` | `width`, `height` (Pixel, gewünschte Grösse) | player.js unmittelbar vor `documentPictureInPicture.requestWindow` | Die nächste leere Fensteranfrage (`about:blank`, höchstens 5 s) als eigenes Bild-im-Bild-Fenster beantworten; Antwort `pipWindowReady`. Ohne Anmeldung wird jede leere Fensteranfrage abgelehnt und ersetzt nie den Player. |
| `pipDrag` | – | Ziehen im Videobild des eigenen Bild-im-Bild-Fensters | Fenster verschieben (`WM_NCLBUTTONDOWN` mit `HTCAPTION`) |
| `pip` | `on` (bool), `paused` (bool, nur bei `on:false`; fehlt = `true`) | Eigenes Fenster: `on:true` nach dem Umzug des Videos, `on:false` beim Zurückholen mit `paused:false` („Zurück zum Player“) bzw. `paused:true` (Schliessen, Wechsel zu Bild oder Musik). Rückfall natives BiB: `enterpictureinpicture` → `on:true`; `leavepictureinpicture` → 200 ms später `on:false` mit `paused` = Video pausiert oder nicht mehr im Dokument; `reportPipRemoved` → `on:false, paused:true` | `on:true`: Fensterzustand merken (normal, maximiert, Vollbild), minimieren; nur im Rückfall Symbol ins native Fenster setzen. `on:false`: bei `paused:false` vorherigen Zustand im Vordergrund wiederherstellen, bei `paused:true` minimiert lassen. Idempotent (`pipActive`): doppelte Meldungen schaden nicht. |
| `damaged` | `path` (Pfad wie in `data.js`, `./…`), `at` (Stelle in s, eine Nachkommastelle), `skipped` (tatsächlich übersprungene Sekunden des ersten Sprungs, ganze Zahl ab 1; seit STEFFLIX 2.9.2 bei bekannter Länge bis hinter die Lücke, sonst 5, DEC-380; fehlt der Wert, nimmt der Desktop 5) | `reportDamagedSpot` (player.js), einmal je Datei und Stelle, nachdem der Player eine beschädigte Stelle übersprungen hat (DEC-373, seit STEFFLIX 2.9.0) | Im Hintergrund `DamagedList.ReportFromPlayer`: nur Pfade innerhalb des Installationsordners, die existieren; Fund in `damaged.js` (`source: "player"`, `length: null`) und eine Zeile in `player.log` (DEC-374, seit Desktop 4.2.0). STEFFLIX trägt den Fund zugleich in die geladene Liste `damagedMedia` ein (sofort sichtbar, ohne Neuladen). |
| `damagedRemove` | `path` | Einstellungen › Beschädigte Dateien, „Aus Liste entfernen“ | Eintrag der Datei aus `damaged.js` entfernen (nur Pfade innerhalb des Installationsordners) |

**Desktop → STEFFLIX** (`PostWebMessageAsJson`, Feld `event`):

| `event` | Felder | Auslöser | Wirkung in STEFFLIX |
|---|---|---|---|
| `dataChanged` | – | Daten einlesen, Importer, Tools oder Update fertig | Neu laden; läuft ein Medium, erst auf Wunsch |
| `importBackup` | `text` (Inhalt einer Datensicherung) | Datenübernahme | Sicherung ohne Rückfrage übernehmen, Antwort `importResult` |
| `toolsShown` | – | Fenster der Tools sichtbar | Hinweis „Tools werden geöffnet …“ ausblenden |
| `pipWindowReady` | – | Antwort auf `pipWindow` | Jetzt `requestWindow` aufrufen; bleibt die Antwort aus (1 s, damit der Rückfall noch innerhalb der Nutzeraktivierung des Klicks liegt) oder scheitert `requestWindow`, Rückfall auf das native BiB und bis zum Neuladen der Seite nur noch dieses (`docPipFailed`, DEC-353) |
| `pipWindowClosed` | – | Eigenes Bild-im-Bild-Fenster geschlossen (auch durch Windows oder das Programmende) | Video zurückholen, falls `pagehide` im Bild-im-Bild-Dokument ausblieb |

Zusätzlich: Fremde Adressen öffnet der Desktop im Standardbrowser; beim Verlassen des Vollbilds ruft er `document.exitFullscreen` auf. Ist das eigene Bild-im-Bild eingeschaltet (`player.docpip`, Standard ein), setzt der Desktop vor jedem Dokument `window.sflixDocPip = true` (`AddScriptToExecuteOnDocumentCreated`).

**Bild-im-Bild im Desktop (DEC-341, DEC-351):** Der Knopf `.ctrlPip` öffnet im Fenster von `STEFFLIX.exe` ein eigenes Fenster über Document Picture-in-Picture: `pipWindow` → `pipWindowReady` → `documentPictureInPicture.requestWindow` → der Desktop beantwortet die Fensteranfrage (`NewWindowRequested` mit Deferral, `NewWindow` = WebView2 in derselben Umgebung) mit einem eigenen Fenster (immer obenliegend, ohne Titelleiste, eigenes Symbol, Titel aus `document.title` des Bild-im-Bild-Dokuments = Videotitel; gemerkt werden Lage und Breite in `player.pipBounds`, die Höhe folgt beim Öffnen dem Seitenverhältnis des Videos, das Fenster liegt immer ganz auf einem sichtbaren Bildschirm). Neu laden oder Verlassen der Seite im Player schliesst ein offenes Bild-im-Bild-Fenster (DEC-353). STEFFLIX verschiebt das Video-Element ohne Neuladen in dieses Dokument (gleicher Skriptkontext, darum keine Brücke für Bedienelemente, Tasten und Countdown) und holt es beim Schliessen zurück. **Rückfall** auf das native Bild-im-Bild von Chromium, wenn `documentPictureInPicture` fehlt, `pipWindowReady` ausbleibt, `requestWindow` scheitert oder `player.docpip` auf `false` steht. Nur im Rückfall gelten die Heuristiken: Symbol über die Fenstersuche (`CoreWebView2.BrowserProcessId`, `WM_SETICON`), X / „Zurück zum Tab“ über den Pausenzustand. Browser und Android-App nutzen immer das native Bild-im-Bild. Damit das Video im eigenen Fenster weiterläuft, während das Hauptfenster minimiert ist, startet der Desktop die WebView2-Umgebung seit 4.2.0 mit `--autoplay-policy=no-user-gesture-required --disable-features=BackgroundVideoTrackOptimization,BackgroundVideoPauseOptimization` (SFLIX-20261123-095, `docs/ARCHITECTURE.md` 3.9).

## 2. Namen der Weboberfläche, an denen andere Komponenten hängen
Umbenennungen in STEFFLIX sind Schnittstellenänderungen. Die verbindliche Liste für die App steht oben in `SFLIX_Tools\Android\app\src\main\assets\inject.js`; sie und diese Tabelle im selben Schritt nachführen.

| Name | Art | Genutzt von |
|---|---|---|
| `<script src="sflix_sys/js/main.js"` in `start.html` | Markup (genau diese Schreibweise) | App: fügt davor Medienumleitung und `inject.js` ein |
| `showSplashscreen`, `checkForUpdate`, `checkForUpdaterUpdate`, `initAndroidSection` | globale Funktionen (werden ersetzt) | App (`inject.js`) |
| `openHelp` | globale Funktion (wird aufgerufen) | App (Hilfe aus den App-Einstellungen) |
| `getInheritedPreview`, `getPreviewForMedia` | globale Funktionen | App (Bild für Benachrichtigung) |
| `#settings`, `#settingsDD`, `#settingsPageToggler`, Klassen `menuIcon`/`menuLabel` | Menü | App (Menüeinträge „Quelle wechseln“, „App-Einstellungen“); Desktop-Eintrag in ui.js |
| `#android`, `#androidDownloadButton`, `#androidHint` | Einstellungen › Android-App | App |
| `#loadingScreen` | Ladescreen (beim Ausblenden `display: none`) | App (`webReady` per MutationObserver) |
| `#video` (audio/video, `dataset.mediaPath`), `#mediaTitle`, `#next a`, `#prev a`, `.ctrlPip` | Player | App (Wiedergabe im Hintergrund, Bild-im-Bild) |
| `HTMLMediaElement.src`/`currentSrc`, `getAttribute("src")`/`setAttribute("src")` | Browser-Prototypen | App (Medienumleitung; STEFFLIX sieht weiterhin die ursprüngliche Adresse) |
| `StefflixApp.loadingProgress(geladen, alle)` | Aufruf aus ui.js (ab STEFFLIX 2.6.1) | App (Fortschritt im Startbildschirm) |
| `window.chrome.webview` | Erkennung des Player-Fensters | Desktop |
| Klassen `inApp` / `inDesktop` auf `<html>` | gesetzt von `inject.js` bzw. `initDesktopApp` | CSS der Weboberfläche |

## 3. Brücke Weboberfläche ↔ Android-App
Herkunft der Seite: `https://appassets.androidplatform.net/<Quellen-ID>/…` (`shouldInterceptRequest`; leerer Pfad = `start.html`). Medien: `http://127.0.0.1:<Port>/<Schlüssel>/…` (`MediaServer`, GET/HEAD mit Range). `window.stefflixApp = true` markiert die App.

**JavaScript → App** (`window.StefflixApp`, `@JavascriptInterface` in `BrowserActivity.Bridge`):

| Methode | Rückgabe | Zweck |
|---|---|---|
| `webSplash()` | – | Die Seite zeigt ihren eigenen Startbildschirm: App-Startbildschirm ausblenden |
| `webReady()` | – | Ladescreen der Seite ist weg |
| `loadingProgress(done, total)` | – | Sichtbare Bilder geladen / gesamt |
| `openSettings()` | – | App-Einstellungen öffnen |
| `switchSource()` | – | Quellenliste öffnen |
| `updateAvailable()` | bool | Neue App-Version vorhanden (roter Punkt im Menü) |
| `version()` | Text | Installierte App-Version |
| `setPlaying(playing)` | – | Wiedergabe läuft: Display bleibt an (Einstellung `keepScreenOn`) |
| `diagnosis()` | Text | Diagnose der App für die Diagnose von STEFFLIX |
| `mediaState(json)` | – | Zustand des Mediums (siehe unten) |
| `pipAvailable()` | bool | Bild-im-Bild möglich |
| `enterPip(width, height)` | bool | Bild-im-Bild starten |

`mediaState`: `{ active:false }` ohne Medium, sonst `{ active, playing, video, position, duration, rate, title, subtitle, artwork, hasNext, hasPrev, width, height }` (Zeiten in ms, `artwork` Pfad des Vorschaubilds). Gemeldet bei Medienereignissen und Änderungen.

**App → JavaScript** (`evaluateJavascript`, alle Funktionen von `inject.js` bereitgestellt, Aufruf immer mit Prüfung `window.x && x()`):

| Aufruf | Zweck |
|---|---|
| `sflixAppMedia(cmd, arg)` | `cmd`: `play`, `pause`, `toggle`, `next`, `prev`, `back` (−10 s), `forward` (+10 s), `seek` (`arg` in ms) |
| `sflixAppPip(aktiv)` | Bild-im-Bild begonnen/beendet (Ansicht anpassen) |
| `sflixAppRefresh()` | Nach Rückkehr aus den App-Einstellungen: Update-Markierung und Bild-im-Bild neu prüfen |
| `openHelp()` | Hilfe von STEFFLIX öffnen |

Startbildschirm der App (`assets\splash.html`): `showLoader()`, `setStatus(titel, text, anteil)`.

## 4. Dateien im Installationsordner (Skriptdateien für STEFFLIX)
Alle werden per `<script>` geladen (kein `fetch` unter `file://`), UTF-8 ohne BOM. Fehlt eine Datei, läuft STEFFLIX mit Standardwerten weiter (`typeof x === "undefined"` prüfen).

| Datei | Inhalt | Schreibt | Liest |
|---|---|---|---|
| `sflix_sys\data.js` | `var data = { … };` – Medienbaum, siehe 4.1 | Desktop (Daten einlesen); im Repository Muster | STEFFLIX |
| `sflix_sys\renamed.js` | `var renamedPaths = { "id": "<yyyyMMddHHmmss>", "pairs": [["./alt", "./neu"], …] };` höchstens 5000 Paare, neue Paare hinten | Desktop (Daten einlesen, Folgen umbenennen inkl. Rückgängig, Duplikate) | STEFFLIX: wendet die Paare der Reihe nach auf gespeicherte Pfade an (Präfix `alt/` → `neu/`), einmal je `id` (`renamedPathsApplied`) |
| `sflix_sys\version.js` | `var version = 2.0808;` (alte Zahl, x + y/100 + z/10000) und `var versionName = "2.8.8";` – die alte Zahl **zuerst** | Entwicklung (`/version-erhoehen`), `release.ps1` | STEFFLIX, Desktop (lokal und von GitHub), alte Updater |
| `sflix_sys\install.js` | `var installId = "<32 Hex>";` `var installFolder = "<Pfad>";` | Desktop (Start, Setup) | STEFFLIX (Name der IndexedDB), Desktop |
| `sflix_sys\license.js` | `var licenseFile = { "key": "SFX2-…", "confirmedFor": "<installId>" };` – Feedback ist erst aktiv, wenn `confirmedFor` der eigenen `installId` entspricht | Desktop (Lizenz) | STEFFLIX (`decodeLicenseKey`, `getLicenseState`), Desktop |
| `sflix_sys\updater_info.js` | `var updaterInfo = { "updater": "x.y.z", "ffmpeg": "<Version>" };` | Desktop (jeder Start) | STEFFLIX (Hilfe, Diagnose, Vergleich mit `versions.json`) |
| `sflix_sys\damaged.js` | `var damagedMedia = {"version": 1, "items": [{"path": "./…", "size": <Bytes>, "gaps": [{"at": <s>, "length": <s oder null>, "source": "download"\|"check"\|"player", "date": "<JJJJ-MM-TTThh:mm:ssZ>"}]}]};` (DEC-374, seit Desktop 4.2.0 / STEFFLIX 2.9.0). Pfad wie in `data.js`; ein Eintrag je Datei, Funde an derselben Stelle (±3 s) zusammengefasst (eine bekannte Länge bleibt, wenn ein späterer Fund sie nicht kennt); `at`/`length` mit einer Nachkommastelle. Beim Schreiben fallen Einträge weg, deren Datei fehlt, deren Grösse sich geändert hat oder die keine Lücke mehr haben; höchstens 2000 Einträge (die mit dem ältesten jüngsten Fund fallen weg). Leer: `{"version": 1, "items": []}` | Desktop (`DamagedList`, atomar über `.tmp`): Downloader (Lücke nach den Reparaturrunden), „Medien prüfen“ mit „Lücken suchen“, Brücke `damaged`/`damagedRemove`; entfernt bei „Datei löschen“ im Downloader und beim Verschieben in die Ablage; das Setup legt sie leer an | STEFFLIX (Einstellungen › Beschädigte Dateien, zusammen mit `damagedLocal`; Pfade, die nicht in `data.js` stehen, werden nicht gezeigt), App über `FileCache` |

`sflix_sys\player.log` (DEC-374, seit Desktop 4.2.0): Text, UTF-8 ohne BOM, LF, eine Zeile je Fund des Players im Desktop: `JJJJ-MM-TT hh:mm:ss  Beschädigt: ./<Pfad> bei <m:ss>, <n> s übersprungen` (Ortszeit, zwei Leerzeichen nach der Zeit; `<n>` = `skipped` der Brücke `damaged`, ganze Sekunden, seit STEFFLIX 2.9.2 die tatsächlich übersprungenen, DEC-380). Über 256 KB fällt vor dem nächsten Eintrag die ältere Hälfte der Zeilen weg. Sonst steht vorerst nichts darin; die App schreibt kein Protokoll.

Geschützt (nie per Update herunterladen, ersetzen oder löschen, `Net.ProtectedFiles`): `data.js`, `install.js`, `license.js`, `renamed.js`, `updater_info.js`, `damaged.js` sowie die lokalen Dateien des Desktops (`updater_settings.json`, `folgen_cache.json`, `folgen_rueckgaengig.json`, `updater.log`, `medien_cache.json`, `downloads.json`, `player.log`). Nie gepusht werden `install.js`, `license.js`, `renamed.js`, `updater_info.js`, `damaged.js`, `player.log` und die lokalen Dateien des Desktops (`.gitignore`, geprüft von `check_docs.py`); `data.js` liegt als Muster im Repository (Demoinhalt für das Setup).

### 4.1 `data.js`
- Wurzel: Kategorien (oberste Ordner) als Objekte; Unterordner als verschachtelte Objekte mit ihrem Namen als Schlüssel.
- Dateien: Schlüssel ist der relative Pfad mit `./` (`"./Serien/Show/Staffel 1/S01E01.mp4": null`), Wert `null`.
- `Preview`: Objekt mit den Vorschaubildern des Ordners (Dateien wie oben); STEFFLIX überspringt den Schlüssel beim Aufbau der Seiten.
- Ordner mit `__` im Namen, System- und versteckte Ordner fehlen. Die Reihenfolge entspricht der Ordnerreihenfolge (Teil-Einlesen ersetzt Zweige an derselben Stelle).
- Erlaubte Endungen: gleiche Liste in `DataTask.AllowedFileTypes` und `core.js` (Video, Musik, Bilder); beide synchron halten.
- Namen enthalten nur Zeichen, die `btoa` verarbeiten kann (Latin-1); `$=$`, `|`, `#`, `%` werden beim Einlesen ersetzt, weil STEFFLIX sie als Trennzeichen bzw. in Adressen nutzt.

## 5. Dateien aus dem Release (GitHub)
Gelesen von GitHub (`raw`-Adressen, im Test über `SFLIX_UPDATE_URL`); erzeugt nur von `release.ps1` und seinen Skripten, nie von Hand.

| Datei | Format | Erzeugt | Liest |
|---|---|---|---|
| `checksums.json` | Objekt Pfad → SHA256 (Hex) für Web-Dateien, EXEs und `ffmpeg.json`; dazu `_webFiles` (Liste der Programmdateien der Weboberfläche) und `_obsoleteFiles` (bei Nutzern zu löschen). Pfade relativ mit `/`. `_serial` (DEC-372): Release-Nummer als Zeichenkette aus 14 Ziffern (UTC `JJJJMMTThhmmss`), streng steigend; ist die bisherige gleich hoch oder höher, deren Wert plus 1; bei unverändertem Inhalt bleibt die bisherige Nummer. | `create_checksums.py` (aus `WEB_FILES`/`OBSOLETE_FILES` in `sflix_update.py`) | Desktop (`UpdateCheck`, `Net.WebFilesFor`/`ObsoleteFilesFor`: nur Pfade, die `IsAllowedWebPath` erlaubt und die eine Prüfsumme haben; ab 4.1.0 nur mit gültiger `checksums.sig`; ab 4.1.4 `Net.CheckSerial`: kein Stand mit kleinerer oder fehlender Nummer, sobald eine angenommen ist, Abschnitt 8.2 `update.serial`); `check_release.py` (Format; fehlt `_serial`, nur Hinweis, weil die Prüfung vor `create_checksums.py` läuft); alte Updater (übergehen Schlüssel mit `_`) |
| `checksums.sig` | Signatur von `checksums.json` (DEC-356, ab Desktop 4.1.0): eine Zeile Base64, UTF-8 ohne BOM, LF. Inhalt: RSA-Signatur PKCS#1 v1.5 mit SHA-256 über die Bytes von `checksums.json` mit CRLF → LF (gleiche Regel wie die Prüfsummen der Textdateien). Gültig, wenn einer der öffentlichen Schlüssel (RSA-XML in `SFLIX_Tools\SflixTools\src\UpdateKeys.cs`: Haupt- und Reserveschlüssel) passt. Gehört in denselben Push wie `checksums.json`. | `sign_checksums.ps1` (von `release.ps1` nach `create_checksums.py`; privater Schlüssel `SFLIX_Tools\Release\signing\update_key.dpapi`, mit Windows-DPAPI für das Konto des Herausgebers verschlüsselt und nur im Speicher entschlüsselt (DEC-371); Schlüsselpaare und Ablage von `update_key.ps1`, `-Schuetzen`) | Desktop (`Net.LoadChecksums` → `Net.VerifyChecksums`, fail-closed: fehlt oder passt sie nicht, wird nichts installiert); `update_signature.py` (Prüfung im Release); ältere Desktops und Updater 3.x lesen sie nicht |
| `versions.json` | `{ "updater": "x.y.z", "ffmpeg": "<Version>", "android": { "version": "x.y.z", "sha256": "<Hex>" } }` (Schlüssel `updater` = Desktop) | `release.ps1` | STEFFLIX (Hinweise), App (`AppUpdate`); der Desktop liest sie nicht (Versionen aus `checksums.json`, den Bytes der EXE und `ffmpeg.json`) |
| `ffmpeg.json` | `{ "version", "url", "sha256" }` (ZIP-Build von ffmpeg) | `release.ps1` | Desktop (`FfmpegManifest`) |
| `sflix_sys\news.json` | `{ "knownBugs": [], "versions": [ { "version": "<alte Zahl>", "name": "x.y.z", "items": [ { "type", "text", "component", "componentVersion", "short" } ] } ] }`; Einträge in `knownBugs` nur `{ "type", "text" }`; optional `"resolved": true` (durchgestrichener Eintrag, `<s>` in `news.txt`; nur STEFFLIX wertet es aus); `type`: FEATURE, OPTIMIERUNG, INTERFACE, BUGFIX, HOTFIX, BUG; `component`: `stefflix`, `updater` (Desktop), `app` | `create_news.py` aus `sflix_sys\news.txt` | STEFFLIX (Hilfe, Neuerungen), Desktop (`PatchNotes`), App (`NewsDialog`) |
| `sflix_sys\news.txt` | HTML-Quelle des Changelogs; Block `<b>Version x.y.z</b>`; Einträge des Desktops beginnen mit „Desktop x.y.z:“ (älter „Updater x.y.z:“), der App mit „Android-App x.y.z:“, STEFFLIX ohne Präfix | `news.py` | `create_news.py`; STEFFLIX vor 2.00 direkt |
| `sflix_sys\version.js` | siehe 4 | | Desktop (neueste STEFFLIX-Version) |

**Version x.y.z und alte Zahl:** `alt = x + y/100 + z/10000` (2.8.8 → 2.0808). Gleiche Regel in `SFLIX_Tools\Release\versioning.py`, `core.js` (`toVersionName`, `parseVersionJs`, `compareVersions`), `UpdateTask.cs` (`WebVersion`) und `release.ps1`. App: `versionCode = x·10000 + y·100 + z` (y, z < 100).

## 6. Adressen von `start.html`
`start.html?=<Base64>[#Anker]`. Der Base64-Text (`btoa`, darum nur Latin-1) wird mit `buildStartUrl` erzeugt und mit `decodeUrlData` gelesen; `buildStartUrl` hängt standardmässig `#mediaNav` an.

| Inhalt (dekodiert) | Seite |
|---|---|
| leer / ohne `?=` | Startseite |
| `LEVEL0:<Kategorie>$=$LEVEL1:<Ordner>…` | Ordnerseite (Ebenen fortlaufend nummeriert) |
| `…$=$MEDIA:./<Pfad>$=$PL:<Playliste>` | Player mit Medium (Ebenen davor, Playliste optional) |
| `SEARCH:<Begriff>` | Suche |
| `FAVORITES` | Favoriten |
| `HELP` | Hilfe (Anker z. B. `#stefflixUpdate`) |
| `SETTINGS` | Einstellungen (Anker je Abschnitt, z. B. `#lizenz`, `#android`) |

Trenner `$=$` (`listSeperator`). Sonderfall `start.html?export=app`: vom Desktop im Browser geöffnet, speichert eine Datensicherung für die Übernahme. Der Verlaufseintrag (`history.state`) enthält `sflixScroll` (Scrollposition für Zurück).

## 7. Datensicherung
Datei `stefflix-sicherung-<JJJJ-MM-TT>.json`: `{ "app": "STEFFLIX", "format": 1, "version": <alte Zahl>, "versionName": "x.y.z", "exportedAt": "<ISO>", "data": { Schlüssel: Text } }`. Enthält alle Schlüssel des Speichers ausser `serial`, `lastSplashVersion`, `damagedLocal` und `damagedDismissed` (`isExcludedFromBackup`); Migrationsmarkierungen, `sharedDataImported` und `dataSignature` werden beim Import nicht übernommen. Ältere Sicherungen passt `backupMigrations` (storage.js) an; neue Schritte hinten anfügen.

## 8. Gespeicherte Schlüssel

### 8.1 Browser-Speicher der Weboberfläche
IndexedDB `STEFFLIX-<installId>` (ohne `install.js`: `STEFFLIX`), Version 1, Objektspeicher `storage`, Werte als Text; im Programm über `getData`/`setData`/`removeData` mit synchronem Cache. Daten aus dem früheren localStorage werden einmal übernommen. Listen sind mit `$=$` getrennt.

| Schlüssel | Inhalt |
|---|---|
| `favorites` | Liste der Medienpfade |
| `playlast` | „Weiterschauen“ (bis 50 Einträge; Galerien als ein Eintrag je Ordner) |
| `galleryProgress` | JSON Ordner → höchste angesehene Bildnummer |
| `timestamp-<Pfad>` | Wiedergabeposition |
| `timestampDuration-<Pfad>` | Dauer des Mediums (auch für die Musikliste) |
| `seen-<Pfad>` | Zeitpunkt „gesehen“ |
| `playlist-<Name>`, `currentPrefix` | Playlisten (u. a. `shuffleList`, `searchResults`, `favoritesList`) und aktuelle Playliste |
| `shuffle`, `shuffleState` | Zufallswiedergabe an/aus und Verlauf (JSON) |
| `settings` | Autoplay: vier Werte (`$=$`-getrennt) |
| `displaySettings` | JSON der Darstellungseinstellungen (`cardSize`, `rowAuto`, `rowSize`, `showMarkers`, `reduceMotion`, `skipSplash`, `alphaGroups`, `groupExcludeImages`, `groupExcludeMusic`, `jumpBar`, `ignoreArticles`, `separateFolders`, `continueMax`, `showUpdateHints`) |
| `transitionByFolder`, `introByFolder`, `volumeByFolder` | JSON Ordnerpfad → Wert (`""` = alle Ordner) |
| `mediaVolume`, `nativeControls` | Lautstärke (Rückfall) und native Steuerelemente |
| `dashboardOrder` | Reihenfolge der Startseite |
| `lastSplashscreen`, `lastSplashVersion` | Startbildschirm zuletzt gezeigt (Datum, Version) |
| `updateBannerDismissed`, `updaterBannerDismissed`, `androidAppAnnounced` | Ausgeblendete Hinweise |
| `serial` | Alter Lizenzschlüssel bis 2.04 (nicht in Sicherungen) |
| `damagedLocal` | JSON-Liste im Format von `items` in `damaged.js` (`size` hier `null`): Funde des Players ohne Desktop (App, Browser), nur für dieses Gerät, nicht in Sicherungen (DEC-374, seit 2.9.0) |
| `damagedDismissed` | JSON Pfad → `date` des jüngsten ausgeblendeten Funds: ohne Desktop ausgeblendete Einträge aus `damaged.js`; ein neuerer Fund derselben Datei erscheint wieder. Nur für dieses Gerät, nicht in Sicherungen (DEC-374) |
| Migrationsmarkierungen | `v1.28`, `v1.33`, `v1.38`, `v2.00`, `renamedPathsApplied`, `sharedDataImported`, `dataSignature` – nie zurücksetzen (die Migration 1.28 ist nicht idempotent) |

`sessionStorage`: `sflixSplashShown` (Startbildschirm im Fenster des Desktops einmal je Programmstart).

### 8.2 Desktop: `sflix_sys\updater_settings.json`
Flaches JSON-Objekt mit Punkt-Schlüsseln (`Settings` in Core.cs). Gruppen: `task.<Aufgabe>` (gewählte Aufgaben), `update.*` (darunter `update.serial`: höchste angenommene Release-Nummer aus `checksums.json`, 14 Ziffern, DEC-372; fehlt sie, wird der erste gültig signierte Stand angenommen; das Setup schreibt sie in die Einstellungen der Zielinstallation), `import.*`, `data.all`/`data.folders` (Teil-Einlesen), `check.*` (Prüfungen von „Medien prüfen“, je ein Wahrheitswert, DEC-377, seit 4.2.1; Standard ohne Eintrag: `check.format` Nicht abspielbar und leer, an; `check.damaged` Defekt und nicht lesbar, aus; `check.gaps` Lücken suchen, aus, seit 4.2.0, DEC-373; `check.preview` Fehlende Vorschaubilder, an; `check.folders` Ordner ohne Medien, aus. ffprobe läuft nur bei `format` oder `damaged`; ohne `damaged` werden nicht lesbare Videos weder gemeldet noch auf Lücken untersucht), `preview.*`, `convert.*`, `merge.*`, `download.parallel`/`download.folder`, `tools.parallel`, `ffmpeg.folder`, `log.*`, `player.bounds`/`player.maximized`/`player.dataImportAsked`/`player.devtools`, `player.docpip` (eigenes Bild-im-Bild-Fenster, ohne Eintrag ein) und `player.pipBounds` (Lage und Breite des Bild-im-Bild-Fensters als `x,y,Breite,Höhe`; die Höhe wird beim Öffnen aus dem Seitenverhältnis des Videos neu berechnet). Weitere lokale Dateien in `sflix_sys\` (`folgen_cache.json`, `folgen_rueckgaengig.json`, `medien_cache.json`, `downloads.json`, `updater.log`) sind interne Formate des Desktops, keine Schnittstellen (`docs/DESKTOP.md`). Weil sie Versionen des Desktops überdauern, hier die Felder seit 4.2.0: `medien_cache.json` hat neben `files` (gilt nur für die ffmpeg-Version in `ffmpeg`) den Abschnitt `gaps` mit dem Ergebnis der Lückensuche je Datei (`{"s": Grösse, "m": Änderungszeit in Ticks als Text, "t": letzter Gebrauch in Tagen seit 2020-01-01, "g": [[Stelle, Länge], …]}`, Schlüssel = Pfad relativ zum Installationsordner in Kleinbuchstaben); er bleibt bei einer anderen ffmpeg-Version erhalten und fällt nach 180 Tagen ohne Gebrauch weg. `downloads.json` hat je Download optional `gaps` (`[[Stelle, Länge], …]`, Sekunden mit zwei Nachkommastellen) und `gapPending` (der Benutzer hat „Lücke behalten“ bzw. „Datei löschen“ noch nicht gewählt); fehlen sie, gilt der Download als ohne Lücke. Ablage ausserhalb des Installationsordners: `%LOCALAPPDATA%\STEFFLIX\` (ffmpeg, WebView2-Loader und -Daten je Installation, Zwischenordner der Downloads, Symbol). Der Zwischenordner `downloads\` ist für alle Installationen gemeinsam; jeder Ordner eines Downloads enthält seit 4.2.0 `stefflix_installation.txt` (voller Pfad der Installation, UTF-8 ohne BOM). Verwaiste Ordner löscht eine Installation nur, wenn die Markierung auf sie zeigt, Ordner ohne Markierung erst nach 30 Tagen ohne Änderung (SFLIX-20261123-090).

### 8.3 Android-App: SharedPreferences
| Datei | Schlüssel |
|---|---|
| `shares` | `list` (JSON-Liste der Quellen: `id`, `name`, `host`, `share`, `path`, `user`, `domain`, `pw` = AES-GCM als IV:Chiffre, Schlüssel im Android-Keystore), `last` (zuletzt geöffnete Quelle) |
| `app` | `keepScreenOn`, `landscapeFullscreen`, `backgroundPlayback`, `pictureInPicture`, `notificationsAsked`, `cacheMb`, `bufferMb`, `updateChecked`, `latestVersion`, `latestSha256`, `updateStarted`, `dismissedVersion` |

Passwörter gehen bei einer Neuinstallation verloren (`allowBackup="false"`, Schlüssel im Keystore). Der Browser-Speicher von STEFFLIX liegt in der WebView unter der gemeinsamen Herkunft `appassets.androidplatform.net`, getrennt je Quelle über `install.js` (8.1).
