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
1. „Daten einlesen“ (`DataTask`) durchsucht den Installationsordner. Ordner mit `__` im Namen (Ablagen wie `__gelöscht`, `__konvertiert`, `Preview\__original`), System- und versteckte Ordner werden übersprungen (`Folders.Ignore`/`IgnoreAny`). Ausnahme heute: „Ablagen leeren“ erkennt nur Ordner, die mit `__` beginnen (R-09, Angleichung in SFLIX-20261122-050).
2. Namen mit Zeichen, die `btoa` nicht verarbeiten kann, werden umbenannt; zu grosse Vorschaubilder werden auf 480×270 verkleinert (Original nach `Preview\__original`).
3. Ergebnis: `sflix_sys\data.js` (Medienbaum) und bei Umbenennungen `sflix_sys\renamed.js` (Paare alt → neu). Teil-Einlesen ersetzt nur die Zweige der gewählten Ordner (byte-gleich für den Rest). Importer, Download und Tools hängen das Einlesen ihrer Zielordner automatisch an (`TaskRunner.DataFolders`).
4. Der Desktop meldet dem Player `dataChanged`; STEFFLIX lädt neu (DEC-288), bei laufender Wiedergabe erst auf Wunsch.
5. Beim Start wendet STEFFLIX `renamed.js` einmal auf alle gespeicherten Pfade an (Favoriten, Zeitstempel, Werte je Ordner …; gemerkt über `renamedPathsApplied`) und erneuert Playlisten, wenn sich der Medienbestand geändert hat (`dataSignature`).

Auch „Folgen umbenennen“ (inklusive Rückgängig) und „Duplikate finden“ schreiben Paare in `renamed.js`, damit Favoriten und Fortschritt mit der Datei mitwandern.

### 3.2 Speicher je Installation
`install.js` (vom Desktop beim Start und im Setup geschrieben) enthält eine zufällige `installId`. STEFFLIX speichert damit in der IndexedDB `STEFFLIX-<installId>`; ohne Datei in der gemeinsamen `STEFFLIX`. Beim ersten Start mit eigener Datenbank werden die gemeinsamen Daten einmal übernommen (`sharedDataImported`). Ein kopierter Ordner bekommt eine neue Kennung (der alte Ordner ist noch erreichbar), ein verschobener behält sie (DEC-090). Dieselbe Trennung gilt für die App: Alle Quellen teilen eine Herkunft, getrennt wird über `install.js` der jeweiligen Quelle. Das Player-Fenster hat zusätzlich einen eigenen WebView2-Datenordner je Installation.

### 3.3 Update (Desktop)
1. Beim ersten `ready` des Players (bzw. beim Start der Tools ohne Player) prüft `UpdateCheck` gegen GitHub: `checksums.json` (Prüfsummen, `_webFiles`, `_obsoleteFiles`; künftig mit Signatur `checksums.sig`, siehe 3.7), `version.js` (STEFFLIX-Version), `versions.json` (Desktop, ffmpeg), `ffmpeg.json`.
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

### 3.7 Signierte Updates (DEC-356, Umsetzung SFLIX-20261122-055)
Ziel: Ein Update wird nur installiert, wenn `checksums.json` vom Herausgeber signiert ist. Heute kommen Prüfsummen und Dateien aus demselben Repository; sie schützen nur gegen Übertragungsfehler, nicht gegen ein manipuliertes Repository.

```
release.ps1                                         STEFFLIX.exe (Net.LoadChecksums)
  create_checksums.py -> checksums.json               checksums.json + checksums.sig laden
  Bytes (CRLF -> LF) signieren, RSA-3072/SHA-256      Signatur mit Haupt- ODER Reserveschlüssel prüfen
  privater Schlüssel: SFLIX_Tools\Release\signing\    ungültig/fehlend -> nichts installieren
  -> checksums.sig (Base64)                           gültig -> Prüfsummen wie bisher (EXE, Web-Dateien, ffmpeg.json)
```

- **Eine Stelle:** `Net.LoadChecksums` ist der einzige Weg, auf dem Update (`UpdateTask`), Setup (`InstallTask`) und ffmpeg (`Ffmpeg.LoadManifest`, über `ffmpeg.json` auch das ffmpeg-Archiv) ihre Prüfsummen bekommen. Die Signatur dort deckt alle Installationswege ab.
- **Format:** `checksums.sig` enthält die Base64-kodierte Signatur (PKCS#1 v1.5, SHA-256) über die Bytes von `checksums.json` mit vereinheitlichten Zeilenenden (CRLF → LF, dieselbe Regel wie bei den Prüfsummen der Textdateien). Signiert und geprüft wird mit .NET-Bordmitteln (`RSACryptoServiceProvider`), ohne neue Abhängigkeiten.
- **Schlüssel:** Zwei Schlüsselpaare. Der Hauptschlüssel liegt im ignorierten Ordner `SFLIX_Tools\Release\signing\` (gesichert wie `Android\signing\`), der Reserveschlüssel nur offline. Beide öffentlichen Schlüssel stehen in der EXE. Ein Schlüsselwechsel kommt mit einer neuen EXE, die noch mit dem alten Schlüssel signiert ist.
- **Ohne Übergangsphase:** Die erste EXE mit Prüfung und der erste signierte Release erscheinen zusammen. Eine fehlende Signatur gilt deshalb immer als Fehler; sonst könnte ein Angreifer sie einfach weglassen.
- **Rückwärtskompatibel:** Ältere Desktops und Updater 3.x laden `checksums.sig` nicht und arbeiten unverändert weiter.
- **Release:** `checksums.sig` gehört in denselben Push wie `checksums.json` und die EXE; `check_release.py` prüft die Signatur. Passt die Signatur kurz nach einem Push nicht (GitHub liefert teils verzögert aus), meldet der Desktop das als vorübergehend.
- **Nicht abgedeckt:** die Setup-EXE, die Nutzer im Browser herunterladen (nur ein Authenticode-Zertifikat hilft, ergänzend später möglich), und die App (bereits durch die Android-Signatur geschützt: Updates nur mit demselben Schlüssel).

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

## 6. Risikoregister (DEC-357)
Lebende Liste der technischen Risiken, Erstbestand aus der Gesamtanalyse SFLIX-20261122-049 (22.11.2026, gegen den Code geprüft). Abschnitt D je Block in `docs/DECISIONS.md` bleibt das historische Protokoll und wird nicht geändert. **Pflege:** Architekt. Andere Rollen nennen neue oder erledigte Risiken beim Ticketabschluss (`-Risiken`) oder per Ticket an den Architekten. **Status:** offen · in Arbeit (Ticket) · akzeptiert (bewusst getragen) · behoben (mit Datum, nach Prüfung in den Abschnitt „Behoben“ verschieben).

### 6.1 Datenintegrität
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-01 | Desktop | Konvertieren mit „Original löschen“ löscht das Original, bevor das Ergebnis an seinem Platz ist; scheitert das Umbenennen, sind beide weg | in Arbeit | SFLIX-20261122-050 |
| R-02 | Desktop | Ist `downloads.json` beim Start nicht lesbar, gelten alle Zwischenordner als verwaist; Teilstücke pausierter Downloads werden gelöscht | in Arbeit | 050 |
| R-03 | Desktop | Kopieren über Laufwerksgrenzen (Download, Import) schreibt direkt unter dem Endnamen; ein Abbruch hinterlässt abgeschnittene Dateien in der Mediathek | in Arbeit | 050 |
| R-04 | Desktop | `data.js` wird nicht atomar geschrieben; ein vorübergehend fehlendes Laufwerk entfernt seinen Zweig ohne Rückfrage. Teil-Einlesen übersieht Änderungen ausserhalb der gewählten Ordner | in Arbeit | 050 |
| R-05 | Desktop | Beim Start werden alle `*.old` im Installationsordner, `sflix_sys` und `sflix_sys\js` gelöscht, auch fremde Dateien | in Arbeit | 050 |
| R-06 | Desktop | Vorübergehende Netzwerkfehler werden dauerhaft als „defekt“ gemerkt; „Medien prüfen“ wählt solche Dateien zum Löschen vor | in Arbeit | 050 |
| R-07 | Desktop | Abbrechen einer Tool-Aufgabe beendet auch ffmpeg laufender Downloads; ffmpeg läuft nach dem Programmende weiter | in Arbeit | 050 |
| R-08 | Desktop | Download-Nachbearbeitung und „Konvertieren“ können dieselbe Datei gleichzeitig bearbeiten (gleiche Zwischendatei) | in Arbeit | 050 |
| R-09 | Desktop | Uneinheitliche „__“-Regel: Einlesen und Tools überspringen Ordner mit `__` im Namen, „Ablagen leeren“ nur solche, die damit beginnen | in Arbeit | 050 |
| R-10 | Desktop | Ordnerdurchläufe folgen Junctions; Schreibzugriffe können ausserhalb des Installationsordners wirken, Zyklen laufen lange | in Arbeit | 050 |
| R-11 | STEFFLIX | „Alle Daten löschen“ entfernt die Migrationsmarken; beim nächsten Start laufen alte Migrationen erneut (Einstellungen und Gesehen gehen verloren, alte gemeinsame Daten kommen zurück) | in Arbeit | SFLIX-20261122-051 |
| R-12 | STEFFLIX | „Verwaiste Einträge entfernen“ löscht ohne Sicherung, auch wenn ein Laufwerk nur vorübergehend fehlt | in Arbeit | 051 |
| R-13 | alle | Favoriten, Fortschritt und Werte je Ordner hängen am Pfad; Umbenennen ausserhalb der Tools mit `renamed.js` (Explorer, Zusammenfügen, Konvertieren, Wiederherstellen) erzeugt verwaiste Einträge; `renamed.js` hält höchstens 5000 Paare | akzeptiert | Paare für Zusammenfügen in 050; übrige bewusst getragen |
| R-14 | Desktop | Wiederholtes Wiederherstellen oder Importieren mit „Als Kopie speichern“ erzeugt Dubletten (`_1`, `_2`), die „Duplikate finden“ und „Folgen umbenennen“ wieder melden | offen | Namensschema vereinheitlichen (SFLIX-20261122-054) |
| R-15 | Desktop | Gemerkte Prüfergebnisse erkennen Änderungen nur über Grösse und Änderungszeit | akzeptiert | – |
| R-16 | Desktop | Heuristiken: „Folgen umbenennen“ (Titel mit kleiner Zahl als Doppelfolge, Staffelordner mit Zusatz, Kategorie als Serienordner), „Duplikate finden“ wählt Sprachfassungen und gleichnamige Dateien zum Aussortieren vor | offen | Vorschläge im Plan sichtbar machen; Vorauswahl nur bei identischen Dateien |
| R-17 | Desktop | Folgenlisten aus HTML-Seiten (reguläre Ausdrücke) fallen bei einem Umbau der Seite still aus | akzeptiert | Rückfall auf die nächste Quelle |

### 6.2 Update, Release und Repository
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-18 | Desktop | Prüfsummen und Dateien kommen aus demselben Repository; ein kompromittiertes Konto könnte EXE, Web-Dateien oder ffmpeg ersetzen | in Arbeit | Signierte Updates (3.7, DEC-356, SFLIX-20261122-055) |
| R-19 | Release | EXE, `checksums.json` (künftig auch `checksums.sig`) müssen im selben Push liegen; GitHub liefert kurz nach einem Push teils verzögert aus | akzeptiert | Push-Liste von `release.ps1` |
| R-20 | Release | Übergangsdateien für alte Installationen (`sflix.js`, `updater.exe`, `updateData.exe`, `updateStefflix.exe`): wann sie entfallen dürfen, ist nicht festgelegt; zu frühes Entfernen bricht alte Updater | offen | Kriterium festlegen (Architekt mit Benutzer) |
| R-21 | Release | Die Komponente eines Changelog-Eintrags ergibt sich aus dem Textanfang; Tippfehler landen still bei STEFFLIX. Text wird ohne HTML-Escape übernommen | in Arbeit | SFLIX-20261122-053 |
| R-22 | Repo | Lokale Testdaten unter `.claude\` sind nicht in `.gitignore`; nur die Auswahl beim Push schützt sie | in Arbeit | 053 |
| R-23 | Release | `versionCode` der App (x·10000 + y·100 + z) setzt y und z unter 100 voraus, ohne Prüfung | in Arbeit | SFLIX-20261122-052 |
| R-24 | Release | Der Android-Build lädt JDK, SDK und Gradle ohne Prüfsumme und bestätigt die SDK-Lizenzen automatisch | akzeptiert | nur auf dem Entwicklungs-PC |
| R-25 | Desktop | Ein Ordner mit `SFLIX_Tools\Release\release.ps1` gilt als Release-Ordner; auch eine Kopie des Repositorys verliert dann bei jedem Start ihre lokalen Dateien | offen (niedrig) | Eigene, nicht gepushte Markierung prüfen |
| R-26 | alle | Der Lizenzschlüssel enthält den Zugang zum Feedback-Kanal; die Verschleierung schützt ihn nicht (DEC-010) | akzeptiert | Schlüssel je Lizenz, Erwähnungen ausgeschaltet |
| R-27 | Desktop, App | Die Diagnose im Feedback enthält Installations- und ffmpeg-Pfade, Protokollzeilen bzw. Server, Freigabe und Benutzer der Quelle | offen | Pfade kürzen bzw. maskieren, Vorschau vor dem Senden |
| R-28 | Desktop | Download: Adressen aus Playlisten werden nicht nach Schema und Ziel geprüft; keine Grössengrenzen für Playlist, Teilstücke und Schlüssel | offen | nur http/https, Grenzen setzen |

### 6.3 Schnittstellen und Laufzeit
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-29 | App, Desktop | Namen der Weboberfläche ändern sich: `inject.js`, Prototyp-Eingriffe der Medienumleitung, Einfügemarke vor `main.js`, `isDesktopApp`/Brücke brechen unbemerkt | akzeptiert | Liste in `docs/INTERFACES.md` Abschnitt 2 und in `inject.js` vollständig halten; Rückfall für die Einfügemarke in 052; Agent `schnittstellen-pruefer` |
| R-30 | STEFFLIX | Seitenwechsel ohne Neuladen: Timer und Listener der vorherigen Seite bleiben übrig (Bänder, Player-Menüs, Medien-Rückfall) | in Arbeit | Regel in 4.2; 051 |
| R-31 | STEFFLIX | Der Autoplay-Countdown wird an seiner Anzeige erkannt; Reste führen zu falschen „Gesehen“-Markierungen | in Arbeit | 051 |
| R-32 | STEFFLIX | Der Hinweis auf fehlende Programmdateien liegt unter dem Ladescreen; geprüft wird nur je ein Name pro Datei | in Arbeit | 051 |
| R-33 | App | Der UI-Thread wartet während eines Verbindungsaufbaus auf die SMB-Verbindung (App reagiert nicht) | in Arbeit | 052 |
| R-34 | App | Ein Fehler eines Threads schliesst die gemeinsame Verbindung eines anderen; Vorausladen kann nach dem Beenden neu verbinden | in Arbeit | 052 |
| R-35 | App | Bei einem Fehler des Android-Keystores werden beim nächsten Speichern leere Passwörter für alle Quellen geschrieben | in Arbeit | 052 |
| R-36 | App | Bei pausierter Wiedergabe bleibt die Seite im Hintergrund aktiv (Akku); Fehler beim Start des Vordergrunddienstes werden verschluckt | in Arbeit | 052 |
| R-37 | App | `MediaServer` auf 127.0.0.1 ist nur durch zufälligen Port und Schlüssel im Pfad geschützt; Pfadteile werden nicht gefiltert | in Arbeit | Pfadfilter in 052, sonst akzeptiert |
| R-38 | Desktop, App | Neue WebView2- und WebView-Versionen ändern Verhalten (Hintergrundwiedergabe, still ignorierte Einstellungen) | akzeptiert | nach Runtime-Updates gezielt testen (`docs/TESTING.md`) |
| R-39 | alle | Alte Versionszahl in vier Implementierungen (`versioning.py`, `core.js`, `UpdateTask.cs`, `release.ps1`) | akzeptiert | am 22.11.2026 gleich; bei Änderungen alle vier anpassen |
| R-40 | App | Mischstand nach einem Update (neue `start.html`, alte Skripte), bis alle Ordner geprüft sind | akzeptiert | „STEFFLIX-Dateien neu laden“ (DEC-220) |
| R-41 | Desktop | Selbst gezeichnete Listen mit Häkchen: drei Listen (Folgen umbenennen, Albumbilder, Zusammenfügen) sind beim Auf- und Abbau nicht gegen leere Einträge geschützt | offen | Schutz wie in `CompareView` |
| R-42 | Doku | `decisions.py` legt doppelte Block-Überschriften an, sobald sich Rollen abwechseln | in Arbeit | 053 |

### 6.4 Behoben (geprüft am 22.11.2026)
- Laden der WebView2-Bibliotheken vor dem ersten WebView2-Typ (`WebViewLibs.Register` zuerst in `Program.Main`).
- Signaturprüfung des WebView2-Bootstrappers vor dem Start (`Signature.CheckMicrosoft`).
- Version der neuen EXE direkt aus den Bytes, ohne Temp-Datei (DEC-312).
- Raster der Kacheln beim Seitenwechsel (`closeCardGrid` in `resetPage`).
- Laufwerkswurzeln bei der Installationskennung (voller Pfad für Dateizugriffe; Rest: Name der Zweitverknüpfung bei einer Wurzel, 050).
- Endungslisten von Weboberfläche und Einlesen sind gleich (Rest: „Konvertieren“ weicht ab, 050).

## 7. Verweise
- Dateien: `docs/WEB.md`, `docs/DESKTOP.md`, `docs/ANDROID.md`, `docs/RELEASE.md`; Tests: `docs/TESTING.md`.
- Schnittstellen und Formate: `docs/INTERFACES.md`.
- Entscheide: `python SFLIX_Tools\Doku\decisions.py suchen <Wort>`; zentrale Entscheide: DEC-090 (Speicher je Installation), DEC-177/DEC-180 (App lädt über abgefangene Anfragen und `MediaServer`), DEC-219/DEC-220 (Zwischenspeicher der App), DEC-226 (Dateilisten aus `checksums.json`), DEC-271 (Release-Ordner gesperrt), DEC-275/DEC-276/DEC-281 (Desktop mit WebView2), DEC-288 (Neuladen nach Änderungen), DEC-306 (Seitenwechsel ohne Neuladen), DEC-312 (Version aus den Bytes), DEC-356 (signierte Updates), DEC-357 (Risikoregister).
