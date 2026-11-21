# STEFFLIX – Architektur

Überblick über die drei Komponenten, ihre Laufzeit und die Datenflüsse zwischen ihnen. Gelesen von Architekt und Entwicklung (vor komponentenübergreifenden Änderungen), bei Bedarf Prüfung. Die Dateien selbst beschreiben `docs/WEB.md`, `docs/DESKTOP.md`, `docs/ANDROID.md` und `docs/RELEASE.md`; Nachrichten, Dateiformate und gespeicherte Schlüssel stehen in `docs/INTERFACES.md`. Hier stehen nur die Zusammenhänge. Pflege: Architekt (fachlich), Entwicklung meldet Änderungen an Abläufen.

## 1. Komponenten und Laufzeit

```
                          GitHub (Repository, nur lesend)
             checksums.json · versions.json · news.json · version.js · EXE · APK · ffmpeg.json
                 |  Update (Desktop)          |  Hinweise (STEFFLIX)          |  Update (App)
                 v                            v                               v
 +--------------------------+     +---------------------------+     +---------------------------+
 | Desktop: STEFFLIX.exe    |     | STEFFLIX (Weboberfläche)  |     | Android-App               |
 | C# 5, WinForms, .NET 4.x |     | start.html + sflix_sys\   |     | Java ohne AndroidX        |
 |  Player-Fenster ---------+---->|  läuft über file://       |<----+  WebView unter            |
 |   (WebView2, file://)    |     |  im Browser, im Player-   |     |  appassets.android...     |
 |  Tools-Fenster (MainForm)|     |  Fenster oder in der App  |     |  SMB-Freigabe als Quelle  |
 +-----------+--------------+     +-------------+-------------+     +-------------+-------------+
             | schreibt                         | liest                           | liest über SMB
             v                                  v                                 v
      Installationsordner: Medienordner, sflix_sys\data.js, renamed.js, install.js, license.js,
                           updater_info.js, lokale Dateien des Desktops
```

- **STEFFLIX (Weboberfläche)** ist die eigentliche Mediathek. Sie läuft **nur über `file://`**, ohne Webserver und ohne ES-Module. Alle Funktionen sind global, die sieben Skripte werden in fester Reihenfolge geladen (core, storage, cards, pages, player, ui, main); nur `main.js` führt beim Laden Code aus. Daten kommen ausschliesslich über Skriptdateien (`var … = …;`), weil `fetch` auf lokale Dateien unter `file://` nicht geht. Gespeichert wird nur im Browser (IndexedDB mit synchronem Cache). Externe Daten (Changelog, neueste Versionen, Vorschau-Animation) kommen nur von GitHub.
- **Desktop (`STEFFLIX.exe`)** ist eine einzige EXE mit zwei Fenstern unter einem gemeinsamen `DesktopHost` (DEC-275): dem **Player** (WebView2 lädt `start.html` per `file://`) und den **Tools** (`MainForm`: Update, Daten einlesen, Importer, Download, Tools, Lizenz/Feedback, Hilfe). Die WebView2-Bibliotheken sind in der EXE eingebettet und fest versioniert (DEC-276); die Runtime ist die Evergreen-Runtime von Microsoft. Daten des Fensters liegen je Installation getrennt in `%LOCALAPPDATA%\STEFFLIX\webview\<installId>` (DEC-281). Der Desktop ist die **einzige Komponente, die im Installationsordner schreibt**.
- **Android-App** ist ein abgespeckter Browser für eine STEFFLIX-Installation auf einer SMB-Freigabe („Quelle“). Kein Webserver für die Seite: Die WebView fragt `https://appassets.androidplatform.net/<Quellen-ID>/…` an, die App beantwortet Seite, Skripte und Bilder aus der Freigabe (`FileCache`, DEC-177). Videos und Musik laufen über den internen `MediaServer` auf 127.0.0.1 (DEC-180), weil MP4 mit `moov` am Ende über abgefangene Anfragen nicht starteten. Vor `main.js` fügt die App zwei Skripte in `start.html` ein (Umleitung der Medienadressen und `inject.js`). Die App liest nur; sie ändert nichts auf der Freigabe ausser dem Browser-Speicher von STEFFLIX auf dem Gerät.

## 2. Unveränderliche Vorgaben
| Vorgabe | Grund / Folge |
|---|---|
| Nur `file://`, kein Webserver für STEFFLIX | Doppelklick auf `start.html` muss genügen. Keine ES-Module, kein `fetch` auf lokale Dateien; neue Daten als Skriptdatei. |
| Globale Funktionen, feste Ladereihenfolge | Desktop und App greifen auf Namen der Weboberfläche zu; Umbenennungen sind Schnittstellenänderungen (`docs/INTERFACES.md` Abschnitt 2). |
| C# 5, .NET Framework 4.x, Build mit `csc` | Läuft ohne Installation von Laufzeiten auf Windows; keine String-Interpolation, kein `?.`. |
| Java ohne AndroidX, minSdk 26 | Kleine App, Build ohne Abhängigkeiten ausser `smbj`. |
| Rückwärtskompatibilität | Updater bis 3.x laden nur `updateStefflix.exe`; STEFFLIX bis 2.04 liest nur die alte Versionszahl; ältere Updater laden feste Dateilisten. Dateien, die alte Installationen erwarten, bleiben im Repository, bis sie niemand mehr braucht. |
| Dateioperationen nur über `FileOps` | Schreibschutz, fehlendes Löschrecht, Wiederholen bei „Zugriff verweigert“, Hinweise für NAS an einer Stelle. |
| Persönliche Dateien werden nie verteilt | `install.js`, `license.js`, `data.js`, `renamed.js` und die lokalen Dateien des Desktops werden weder gepusht noch vom Update ersetzt oder gelöscht (`Net.ProtectedFiles`). |
| Release-Ordner ist gesperrt | Im Release-Ordner (es gibt `SFLIX_Tools\Release\release.ps1`) sind Update, Daten einlesen, Importer, Download, Tools und Lizenz gesperrt (DEC-271); der Desktop legt dort keine Dateien an. |

## 3. Datenflüsse

### 3.1 Medien einlesen → `data.js`
1. „Daten einlesen“ (`DataTask`) durchsucht den Installationsordner. Ordner mit `__` im Namen (Ablagen wie `__gelöscht`, `__konvertiert`, `Preview\__original`), System- und versteckte Ordner werden übersprungen (`Folders.Ignore`/`IgnoreAny`, für alle Aufgaben gleich).
2. Namen mit Zeichen, die `btoa` nicht verarbeiten kann, werden umbenannt; zu grosse Vorschaubilder werden auf 480×270 verkleinert (Original nach `Preview\__original`).
3. Ergebnis: `sflix_sys\data.js` (Medienbaum) und bei Umbenennungen `sflix_sys\renamed.js` (Paare alt → neu). Teil-Einlesen ersetzt nur die Zweige der gewählten Ordner (byte-gleich für den Rest). Importer, Download und Tools hängen das Einlesen ihrer Zielordner automatisch an (`TaskRunner.DataFolders`).
4. Der Desktop meldet dem Player `dataChanged`; STEFFLIX lädt neu (DEC-288), bei laufender Wiedergabe erst auf Wunsch.
5. Beim Start wendet STEFFLIX `renamed.js` einmal auf alle gespeicherten Pfade an (Favoriten, Zeitstempel, Werte je Ordner …; gemerkt über `renamedPathsApplied`) und erneuert Playlisten, wenn sich der Medienbestand geändert hat (`dataSignature`).

Auch „Folgen umbenennen“ (inklusive Rückgängig) und „Duplikate finden“ schreiben Paare in `renamed.js`, damit Favoriten und Fortschritt mit der Datei mitwandern.

### 3.2 Speicher je Installation
`install.js` (vom Desktop beim Start und im Setup geschrieben) enthält eine zufällige `installId`. STEFFLIX speichert damit in der IndexedDB `STEFFLIX-<installId>`; ohne Datei in der gemeinsamen `STEFFLIX`. Beim ersten Start mit eigener Datenbank werden die gemeinsamen Daten einmal übernommen (`sharedDataImported`). Ein kopierter Ordner bekommt eine neue Kennung (der alte Ordner ist noch erreichbar), ein verschobener behält sie (DEC-090). Dieselbe Trennung gilt für die App: Alle Quellen teilen eine Herkunft, getrennt wird über `install.js` der jeweiligen Quelle. Das Player-Fenster hat zusätzlich einen eigenen WebView2-Datenordner je Installation.

### 3.3 Update (Desktop)
1. Beim ersten `ready` des Players (bzw. beim Start der Tools ohne Player) prüft `UpdateCheck` gegen GitHub: `checksums.json` (Prüfsummen, `_webFiles`, `_obsoleteFiles`), `version.js` (STEFFLIX-Version), `versions.json` (Desktop, ffmpeg), `ffmpeg.json`.
2. Web-Dateien: Die Liste kommt aus `checksums.json` (DEC-226), gefiltert mit `Net.IsAllowedWebPath` (nur `start.html` und `sflix_sys/…` mit Endungen der Weboberfläche, nie geschützte Dateien). Jede Datei wird gegen ihre SHA256 geprüft und mit `.old`-Sicherung installiert; schlägt etwas fehl, wird zurückgerollt. Danach werden veraltete Dateien gelöscht (nicht im Release-Ordner).
3. Desktop selbst: neue EXE laden, Prüfsumme prüfen, Version direkt aus den Bytes lesen (DEC-312), über `.new`/`.old` ersetzen, Neustart mit `--updated`.
4. ffmpeg: nach `ffmpeg.json` (Version, URL, SHA256) nach `%LOCALAPPDATA%\STEFFLIX\ffmpeg`.
5. Danach meldet der Desktop `dataChanged`, STEFFLIX lädt neu.

So kommen neue oder entfallene Dateien der Weboberfläche **ohne neuen Desktop** zu den Nutzern; neue Dateien müssen dafür in `SFLIX_Tools\Release\sflix_update.py` (`WEB_FILES`, `OBSOLETE_FILES`) stehen.

### 3.4 Versionen und Hinweise
- Der Desktop schreibt bei jedem Start `updater_info.js` (eigene Version, ffmpeg); `release.ps1` schreibt `versions.json`; STEFFLIX vergleicht beides und zeigt im Browser Hinweise auf neue Versionen (im Player-Fenster und in der App aus, dort übernimmt der Desktop bzw. die App).
- Die App prüft höchstens einmal am Tag `versions.json` › `android`, lädt die APK, prüft die SHA256 und installiert über den `PackageInstaller` (DEC-206).
- Die Patchnotes aller drei Komponenten stehen in einer Datei (`news.txt` → `news.json`); getrennt wird über das Feld `component`.

### 3.5 Release
`release.ps1` baut nacheinander Desktop und App (nur wenn sich ihr Fingerabdruck geändert hat), aktualisiert `ffmpeg.json`, die STEFFLIX-Version und den Changelog, prüft mit `check_release.py` und erzeugt `news.json`, `checksums.json` und `versions.json`. Am Ende steht die Liste der zu pushenden Dateien; gepusht wird nur vom Benutzer. **EXE und `checksums.json` gehören immer in denselben Push**, sonst lehnen Installationen das Update wegen falscher Prüfsummen ab. Einzelheiten: `docs/RELEASE.md`.

### 3.6 Datenübernahme in das Player-Fenster
Beim ersten Start des Players bietet der Desktop an, die Daten aus einem Browser zu übernehmen: Er öffnet `start.html?export=app` im Browser, STEFFLIX speichert dort eine Datensicherung in den Download-Ordner, der Desktop findet die Datei (`stefflix-sicherung-*.json`) und übergibt ihren Text mit `importBackup` an den Player; STEFFLIX antwortet mit `importResult`.

## 4. Abläufe in der Weboberfläche

### 4.1 Start und Ladescreen
`main.js` öffnet die IndexedDB (asynchron), führt Migrationen aus, merkt sich den leeren Aufbau der Seite und baut die Seite zur aktuellen Adresse. Der Ladescreen erscheint nur beim Öffnen und verschwindet, wenn body-onload und der Seitenaufbau (`markApplicationStarted`) erfolgt sind, danach wartet er höchstens 10 s auf die sichtbaren Bilder. Später eingefügte Inhalte (Update-Hinweise) warten mit `afterLoadingScreen`. Die App blendet ihren eigenen Startbildschirm aus, wenn `#loadingScreen` verschwindet (`webReady`), und zeigt den Fortschritt aus `StefflixApp.loadingProgress`.

### 4.2 Seitenwechsel ohne Neuladen (DEC-306)
`start.html` wird nur beim Öffnen geladen. Ordner, Suche, Favoriten, Hilfe, Einstellungen, Logo, Pfadleiste und Zurück wechseln über `navigateTo(url)` (`history.pushState`) → `showPage` → `resetPage` → `renderCurrentPage`:
- `capturePageTemplate` merkt beim Start den leeren Aufbau der Bereiche `#navigation`, `#media`, `#mediaNav`, `#folderBands`; `restorePageTemplate` stellt ihn bei jedem Wechsel wieder her.
- `resetPage` beendet laufende Medien (`stopCurrentMedia`) und setzt Bildbetrachter, Zufalls- und Autoplay-Knopf neu auf.
- `handlePageLinkClick` fängt Links zu `start.html` ab; Zurück läuft über `popstate` mit gemerkter Scrollposition (`sflixScroll`, `history.scrollRestoration = "manual"`).

**Regel für neue Funktionen:** Zustand, der früher mit dem Neuladen verschwand, muss in `resetPage`/`stopCurrentMedia` zurückgesetzt werden; Elemente, die beim Start in eine der vier Bereiche eingefügt werden, müssen nach `restorePageTemplate` neu entstehen; Listener auf `document`/`window` nur einmal anlegen.

### 4.3 Medienwechsel im Player
`loadDecodedMedia` → `renderMediaContext` → `whenMainMediaReady` → Bänder mit dem Ordnerinhalt, danach `preloadNeighbourImages` (bis zu zwei Nachbarbilder). Zurück im Player lädt nur das Medium neu, nicht die Seite.

### 4.4 Werte je Ordner
Dauer bis Übergang, Intro und Lautstärke werden je Ordner gespeichert (`transitionByFolder`, `introByFolder`, `volumeByFolder`). `getFolderValueFor` sucht vom Medium aufwärts den nächsten Ordner mit Wert; der Schlüssel `""` gilt für alle Ordner. Im Autoplay-Menü wählt „Gilt für“ den Ordner, `@auto` nimmt den Ordner, aus dem der geltende Wert stammt.

## 5. Android-App im Detail
- **Laden:** `FileCache` prüft Dateien über Ordnerlisten (Programmordner höchstens alle 30 s), hält Kopien im Arbeitsspeicher und auf dem Gerät und liefert Programmdateien sofort aus dem Zwischenspeicher, während im Hintergrund geprüft wird (stale-while-revalidate, DEC-219). „STEFFLIX-Dateien neu laden“ verwirft diese Kopien (DEC-220).
- **Verbindung:** `warmUp` beim Öffnen, alle 25 s `keepAlive`, nach längerer Pause neu verbinden; der `MediaServer` verbindet bei Abbrüchen bis 30 s neu und puffert Videos im Arbeitsspeicher (DEC-210).
- **Wiedergabe im Hintergrund und Bild-im-Bild:** `inject.js` meldet den Zustand des Mediums (`mediaState`), Android steuert über `sflixAppMedia`; ein Vordergrund-Dienst läuft nur während der Wiedergabe und löst sich nach 15 s Pause (DEC-252). Bild-im-Bild nutzt den vorhandenen Knopf von STEFFLIX; `inject.js` stellt dafür die Bild-im-Bild-Schnittstelle des Browsers nach (DEC-263).
- **Aufgaben:** `BrowserActivity` ist `singleTask`; die Quellenliste schliesst sich nach dem Öffnen einer Quelle.

## 6. Risiken
Die vollständige Liste steht je Block in Abschnitt D von `docs/DECISIONS.md` (`decisions.py`). Die wichtigsten für Architekturentscheide:

| Risiko | Wirkung | Absicherung |
|---|---|---|
| Namen der Weboberfläche ändern sich | App (`inject.js`, Medienumleitung) oder Desktop (`isDesktopApp`, Brücke) brechen unbemerkt | Liste in `docs/INTERFACES.md` Abschnitt 2 und oben in `inject.js`; Agent `schnittstellen-pruefer` |
| EXE ohne passende `checksums.json` gepusht | Installationen lehnen das Update ab | `release.ps1` listet beides; immer zusammen pushen |
| Dateien entfernt, die alte Installationen erwarten | Ältere Updater brechen ab oder schreiben Fehlerseiten in Dateien | Übergangsdateien (`sflix.js`, `updater.exe`, `updateData.exe`, `updateStefflix.exe`) erst entfernen, wenn kein Umstieg mehr nötig ist |
| Alte Versionszahl falsch umgerechnet | Falsche Update-Hinweise bei alten Installationen | Gleiche Regel in `versioning.py`, `core.js` (`toVersionName`), `UpdateTask.cs` (`WebVersion`) und `release.ps1` |
| Seitenwechsel ohne Neuladen | Zustand oder Listener aus der vorherigen Seite bleiben übrig | Regel in 4.2 |
| Vertrauen in das GitHub-Repository | Ein manipuliertes Repository könnte erlaubte Web-Dateien mit beliebigem Inhalt ersetzen | Pfadfilter `IsAllowedWebPath`, geschützte Dateien, Prüfsummen (schützen gegen Übertragungsfehler, nicht gegen das Repository selbst) |
| Mischstand in der App nach einem Update | Neue `start.html` mit alten Skripten bis zur Prüfung aller Ordner | „STEFFLIX-Dateien neu laden“ (DEC-220) |
| `MediaServer` auf 127.0.0.1 | Jede App auf dem Gerät kann den Port erreichen | Zufälliger Port und Schlüssel im Pfad |
| WebView2- und WebView-Versionen | Neue Runtime-Versionen ändern Verhalten (Wiedergabe im Hintergrund, Einstellungen, die eine alte Runtime still ignoriert) | Nach Updates der Runtime gezielt testen (`docs/TESTING.md`) |
| Lizenzschlüssel | Wer einen Schlüssel hat, kann Feedback senden; Härtung bewusst zurückgestellt (DEC-010) | Schlüssel nie weitergeben, `license.js` nie pushen |

## 7. Verweise
- Dateien: `docs/WEB.md`, `docs/DESKTOP.md`, `docs/ANDROID.md`, `docs/RELEASE.md`; Tests: `docs/TESTING.md`.
- Schnittstellen und Formate: `docs/INTERFACES.md`.
- Entscheide: `python SFLIX_Tools\Doku\decisions.py suchen <Wort>`; zentrale Entscheide: DEC-090 (Speicher je Installation), DEC-177/DEC-180 (App lädt über abgefangene Anfragen und `MediaServer`), DEC-219/DEC-220 (Zwischenspeicher der App), DEC-226 (Dateilisten aus `checksums.json`), DEC-271 (Release-Ordner gesperrt), DEC-275/DEC-276/DEC-281 (Desktop mit WebView2), DEC-288 (Neuladen nach Änderungen), DEC-306 (Seitenwechsel ohne Neuladen), DEC-312 (Version aus den Bytes).
