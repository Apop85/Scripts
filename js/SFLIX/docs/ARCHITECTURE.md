# STEFFLIX – Architektur

Überblick über die drei Komponenten, ihre Laufzeit und die Datenflüsse zwischen ihnen. Gelesen von Architekt und Entwicklung (vor komponentenübergreifenden Änderungen), bei Bedarf Prüfung. Die Dateien selbst beschreiben `docs/WEB.md`, `docs/DESKTOP.md`, `docs/ANDROID.md` und `docs/RELEASE.md`; Nachrichten, Dateiformate und gespeicherte Schlüssel stehen in `docs/INTERFACES.md`. Hier stehen nur die Zusammenhänge. Pflege: Architekt (fachlich), Entwicklung meldet Änderungen an Abläufen.

## 1. Komponenten und Laufzeit

```
                          GitHub (Repository, nur lesend)
     checksums.json + checksums.sig · versions.json · news.json · version.js · EXE · APK · ffmpeg.json
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
                           updater_info.js, damaged.js, lokale Dateien des Desktops
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
| Persönliche Dateien werden nie verteilt | `install.js`, `license.js`, `data.js`, `renamed.js`, `damaged.js` und die lokalen Dateien des Desktops (darunter `player.log`) werden weder gepusht noch vom Update ersetzt oder gelöscht (`Net.ProtectedFiles`). |
| Release-Ordner ist gesperrt | Im Release-Ordner (es gibt `SFLIX_Tools\Release\release.ps1`) sind Update, Daten einlesen, Importer, Download, Tools und Lizenz gesperrt (DEC-271); der Desktop legt dort keine Dateien an. |

## 3. Datenflüsse

### 3.1 Medien einlesen → `data.js`
1. „Daten einlesen“ (`DataTask`) durchsucht den Installationsordner. Ablagen (Ordnername beginnt mit `__`, z. B. `__gelöscht`, `__konvertiert`, `Preview\__original`; `Folders.IsToolFolder`, für alle Aufgaben gleich, auch „Ablagen leeren“), System- und versteckte Ordner werden übersprungen (`Folders.Ignore`/`IgnoreAny`). Ordner mit `__` mitten im Namen werden eingelesen und im Protokoll genannt. Verknüpfungen auf Ordner (Junction, symbolischer Link) durchsucht kein Werkzeug (`Folders.IsLink`); „Daten einlesen“ nennt sie im Protokoll, „Medien prüfen“ meldet sie nie als leer (DEC-361).
2. Namen mit Zeichen, die `btoa` nicht verarbeiten kann, werden umbenannt; zu grosse Vorschaubilder werden auf 480×270 verkleinert (Original nach `Preview\__original`).
3. Ergebnis: `sflix_sys\data.js` (Medienbaum, geschrieben über `data.js.tmp`, vorheriger Stand als `data.js.bak`) und bei Umbenennungen `sflix_sys\renamed.js` (Paare alt → neu). Findet ein vollständiges Einlesen bei mindestens 50 bisherigen Medien weniger als die Hälfte, sind Ordner nicht lesbar oder würde ein nicht erreichbarer Ordner mit Medien entfernt, fragt der Desktop vor dem Speichern nach; ohne Oberfläche wird dann nicht gespeichert (DEC-361). Teil-Einlesen ersetzt nur die Zweige der gewählten Ordner (byte-gleich für den Rest). Importer, Download und Tools hängen das Einlesen ihrer Zielordner automatisch an (`TaskRunner.DataFolders`).
4. Der Desktop meldet dem Player `dataChanged`; STEFFLIX lädt neu (DEC-288), bei laufender Wiedergabe erst auf Wunsch.
5. Beim Start wendet STEFFLIX `renamed.js` einmal auf alle gespeicherten Pfade an (Favoriten, Zeitstempel, Werte je Ordner …; gemerkt über `renamedPathsApplied`) und erneuert Playlisten, wenn sich der Medienbestand geändert hat (`dataSignature`).

Auch „Folgen umbenennen“ (inklusive Rückgängig) und „Duplikate finden“ schreiben Paare in `renamed.js`, damit Favoriten und Fortschritt mit der Datei mitwandern.

### 3.2 Speicher je Installation
`install.js` (vom Desktop beim Start und im Setup geschrieben) enthält eine zufällige `installId`. STEFFLIX speichert damit in der IndexedDB `STEFFLIX-<installId>`; ohne Datei in der gemeinsamen `STEFFLIX`. Beim ersten Start mit eigener Datenbank werden die gemeinsamen Daten einmal übernommen (`sharedDataImported`). Ein kopierter Ordner bekommt eine neue Kennung (der alte Ordner ist noch erreichbar), ein verschobener behält sie (DEC-090). Dieselbe Trennung gilt für die App: Alle Quellen teilen eine Herkunft, getrennt wird über `install.js` der jeweiligen Quelle. Das Player-Fenster hat zusätzlich einen eigenen WebView2-Datenordner je Installation.

### 3.3 Update (Desktop)
1. Beim ersten `ready` des Players (bzw. beim Start der Tools ohne Player) prüft `UpdateCheck` gegen GitHub: `checksums.json` (Prüfsummen, `_webFiles`, `_obsoleteFiles`; ab Desktop 4.1.0 nur mit gültiger Signatur `checksums.sig`, siehe 3.7), `version.js` (STEFFLIX-Version) und `ffmpeg.json`. `versions.json` liest der Desktop nicht; die Version der neuen EXE kommt aus ihren Bytes.
2. Web-Dateien: Die Liste kommt aus `checksums.json` (DEC-226), gefiltert mit `Net.IsAllowedWebPath` (nur `start.html` und `sflix_sys/…` mit Endungen der Weboberfläche, nie geschützte Dateien). Jede Datei wird gegen ihre SHA256 geprüft und mit `.old`-Sicherung installiert; schlägt etwas fehl, wird zurückgerollt. Danach werden veraltete Dateien gelöscht (nicht im Release-Ordner).
3. Desktop selbst: neue EXE laden, Prüfsumme prüfen, Version direkt aus den Bytes lesen (DEC-312), über `.new`/`.old` ersetzen, Neustart mit `--updated`.
4. ffmpeg: nach `ffmpeg.json` (Version, URL, SHA256) nach `%LOCALAPPDATA%\STEFFLIX\ffmpeg`. Fehlt ffmpeg (erste Einrichtung), gilt seit 4.2.0 (DEC-375): Prüfsummen erreichbar, aber abgelehnt (Signatur, beschädigt, älterer oder fehlender Stand nach DEC-372) → kein ffmpeg, auch nicht die eingebaute Version, die Aufgabe meldet den Grund; Server nicht erreichbar → die in der EXE eingebaute Version (Adresse und SHA256 stecken in der EXE).
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

### 3.7 Signierte Updates (DEC-356, seit Desktop 4.1.0; Release-Nummer DEC-372, seit 4.1.4)
Ein Update wird nur installiert, wenn `checksums.json` vom Herausgeber signiert ist. Prüfsummen und Dateien kommen aus demselben Repository; ohne Signatur schützten sie nur gegen Übertragungsfehler, nicht gegen ein manipuliertes Repository. Werkzeuge: `update_key.ps1` (erzeugt die Schlüsselpaare und schreibt die öffentlichen Teile nach `SflixTools\src\UpdateKeys.cs`), `sign_checksums.ps1` (Signieren im Release), `update_signature.py` (Prüfung im Release, ohne Zusatzpakete); im Desktop `Net.VerifyChecksums`. Format: `docs/INTERFACES.md` Abschnitt 5.

```
release.ps1                                         STEFFLIX.exe (Net.LoadChecksums)
  create_checksums.py -> checksums.json + _serial     checksums.json + checksums.sig laden
  Bytes (CRLF -> LF) signieren, RSA-3072/SHA-256      Signatur mit Haupt- ODER Reserveschlüssel prüfen
  privater Schlüssel: signing\update_key.dpapi        ungültig/fehlend -> nichts installieren
    (DPAPI, nur im Speicher entschlüsselt)            _serial kleiner als update.serial -> nichts installieren
  -> checksums.sig (Base64)                           gültig -> update.serial nachführen, Prüfsummen wie bisher
```

- **Eine Stelle:** `Net.LoadChecksums` ist der einzige Weg, auf dem Update (`UpdateTask`), Setup (`InstallTask`) und ffmpeg (`Ffmpeg.LoadManifest`, über `ffmpeg.json` auch das ffmpeg-Archiv) ihre Prüfsummen bekommen. Die Signatur dort deckt alle Installationswege ab. Abgelehnte Prüfsummen (`ChecksumsRejectedException`) unterscheidet der Desktop von nicht erreichbaren (`DownloadException`): Nur bei nicht erreichbarem Server weicht ffmpeg auf die eingebaute Version aus (DEC-375, seit 4.2.0).
- **Format:** `checksums.sig` enthält die Base64-kodierte Signatur (PKCS#1 v1.5, SHA-256) über die Bytes von `checksums.json` mit vereinheitlichten Zeilenenden (CRLF → LF, dieselbe Regel wie bei den Prüfsummen der Textdateien). Signiert und geprüft wird mit .NET-Bordmitteln (`RSACryptoServiceProvider`), ohne neue Abhängigkeiten.
- **Schlüssel:** Zwei Schlüsselpaare. Der Hauptschlüssel liegt als `update_key.dpapi` im ignorierten Ordner `SFLIX_Tools\Release\signing\`, mit Windows-DPAPI für das Konto des Herausgebers verschlüsselt; `sign_checksums.ps1` entschlüsselt ihn nur im Speicher, `release.ps1 -Auto` braucht keine Eingabe (DEC-371). Auf einem anderen Konto oder Rechner ist die Datei nutzlos; dort wird die unverschlüsselte Offline-Sicherung mit `update_key.ps1 -Schuetzen` neu abgelegt. Unverschlüsselte Schlüssel (Haupt- und Reserveschlüssel) liegen nur offline beim Benutzer, nie im Projekt. Beide öffentlichen Schlüssel stehen in der EXE. Ein Schlüsselwechsel kommt mit einer neuen EXE, die noch mit dem alten Schlüssel signiert ist.
- **Release-Nummer (DEC-372, seit 4.1.4):** `checksums.json` trägt `_serial` (14 Ziffern, UTC-Zeitpunkt, streng steigend, mitsigniert). `Net.CheckSerial` vergleicht nach der Signatur mit der höchsten angenommenen Nummer der Installation (`update.serial` in `updater_settings.json`): kleiner oder fehlend, obwohl eine gespeichert ist, heisst nichts installieren (Meldung als vorübergehend, beide Nummern im Protokoll); höher wird gespeichert. Ohne gespeicherte Nummer (neue Installation) gilt jeder gültig signierte Stand. Das Setup in einen anderen Ordner führt die Nummer der Zielinstallation nach. So lässt sich kein älterer, früher korrekt signierter Stand (Weboberfläche, ffmpeg) wieder einspielen; die EXE schützt zusätzlich der Versionsvergleich (`SelfUpdate.FindNewer`). Gewollte Rücknahmen eines Inhalts gehen über ein neues Release. `create_checksums.py` behält die Nummer, wenn sich der Inhalt nicht geändert hat (keine neue Signatur, nichts zu pushen); `check_release.py` prüft das Format und meldet ein fehlendes `_serial` nur als Hinweis, weil es vor `create_checksums.py` läuft.
- **Ohne Übergangsphase:** Die erste EXE mit Prüfung und der erste signierte Release erscheinen zusammen. Eine fehlende Signatur gilt deshalb immer als Fehler; sonst könnte ein Angreifer sie einfach weglassen.
- **Rückwärtskompatibel:** Ältere Desktops und Updater 3.x laden `checksums.sig` nicht und arbeiten unverändert weiter.
- **Release:** `checksums.sig` gehört in denselben Push wie `checksums.json` und die EXE; `check_release.py` prüft die Signatur. Passt die Signatur kurz nach einem Push nicht (GitHub liefert teils verzögert aus), meldet der Desktop das als vorübergehend.
- **Nicht abgedeckt:** die Setup-EXE, die Nutzer im Browser herunterladen (nur ein Authenticode-Zertifikat hilft, ergänzend später möglich), und die App (bereits durch die Android-Signatur geschützt: Updates nur mit demselben Schlüssel).

### 3.8 Lückenhafte Videos (DEC-373, DEC-374, DEC-380; seit Desktop 4.2.0, STEFFLIX 2.9.0)
Fehlen in einem heruntergeladenen Video Teilstücke, zeigt der Player trotzdem die volle Länge (die Zeitstempel bleiben erhalten), bleibt aber an der Lücke hängen und springt lautlos zurück. Erkannt wird an drei Stellen, gesammelt in einer Liste:

```
Downloader (DownloadTask)              Medien prüfen (MediaCheckTask)        Player (player.js, auch App)
  Teilstück prüfen (Hls.SegmentProblem)   Option „Lücken suchen“ (check.gaps)   3x Rücksprung an derselben Stelle
  zusammenfügen -> ScanGaps               FfmpegTools.Gaps (gemerkt in          oder 2x Verbindung/Decodierfehler
  Lücke -> Teilstücke neu (2 Runden)        medien_cache.json › gaps)           -> hinter die Lücke, Hinweis
  bleibt -> „Lücke behalten/Datei löschen“ Art „lückenhaft“                      Desktop: Brücke damaged
        |                                      |                                 sonst: damagedLocal (Gerät)
        +------------------> DamagedList -> sflix_sys\damaged.js <----------------+   (+ player.log)
                                               |
                             STEFFLIX: Einstellungen › Beschädigte Dateien (damaged.js + damagedLocal)
```

- **Lückensuche (eine Funktion für Downloader und Medien prüfen):** `FfmpegTools.ScanGaps` liest mit ffprobe nur die Zeitstempel (DTS) der Pakete der ersten Videospur (`-select_streams`, ohne Dekodieren). Eine Lücke ist ein Abstand zweier Pakete, der den üblichen Bildabstand (Median) um mindestens 0,5 s übersteigt; die Paketdauer taugt nicht, weil der MP4-Muxer das letzte Bild vor einer Lücke verlängert (kalibriert mit `SFLIX_Tools\Test\make_gap_video.py`). Abweichung von DEC-373: Format und Spuren kommen aus dem bestehenden Kopf-Aufruf von ffprobe, die Pakete aus einem zweiten Aufruf; die Datei wird trotzdem nur einmal ganz gelesen.
- **Downloader:** Jedes Teilstück wird beim Laden und beim Fortsetzen geprüft (nicht leer, Content-Length, keine Textantwort, `.ts` Sync-Byte im 188-Byte-Raster mit erlaubtem ID3-Kopf, `.m4s` lückenlose Box-Kette, verschlüsselt nur Länge; Init-Segment und Schlüssel ebenso) und sonst neu geladen. Nach dem Zusammenfügen: Lückensuche, betroffene Teilstücke über die Dauern der Playlist bestimmen, löschen, neu laden, neu zusammenfügen (höchstens zwei Runden). Bleibt eine Lücke, wird die Datei normal abgelegt und nachbearbeitet; der Download bleibt mit „Lücke: <Länge> bei <Stelle>“ und den Knöpfen „Lücke behalten“ / „Datei löschen“ in der Liste (Zustand in `downloads.json`, übersteht einen Neustart). „Datei löschen“ verschiebt in die Ablage `__gelöscht` wie „Medien prüfen“ und entfernt den Eintrag aus `damaged.js`.
- **Medien prüfen:** Option „Lücken suchen“ (Standard aus, liest jedes Video ganz). Das Ergebnis steht im Abschnitt `gaps` von `medien_cache.json` und überlebt einen Wechsel der ffmpeg-Version; eine Sperre je Datei verhindert, dass gleichzeitige Aufgaben dieselbe Datei zweimal lesen. Funde der Art „lückenhaft“ sind nicht zum Löschen vorausgewählt (Abweichung von „Aktionen wie defekt“: lückenhafte Dateien sind meist noch brauchbar); gelöscht wird ohne erneute Prüfung.
- **Player:** `initDamagedDetection`/`noteDamagedEvent` zählen Rücksprünge ohne Eingabe des Benutzers (Zeiger, Tasten, Mausrad, Berühren) innerhalb von 60 s an derselben Stelle (±3 s) sowie Fehlschläge dort (Verbindungserkennung oder Decodierfehler). Seit STEFFLIX 2.9.2 (DEC-380) gilt ein Decodierfehler (MediaError 3) einer Datei, die schon lief, nicht als Verbindungsabbruch: `handleDecodeError` lädt die Datei beim ersten Fehler an einer Stelle still neu (ohne Hinweis „Verbindung unterbrochen“), der zweite gilt als beschädigte Stelle; Netzwerkfehler und Hänger bleiben bei der Verbindungserkennung. Nach drei Rücksprüngen bzw. zwei Fehlschlägen springt `skipDamagedSpot` hinter die Stelle: Steht die Lücke mit Länge in der gemeinsamen Liste (`damaged.js` oder `damagedLocal`, Funde von Medien prüfen und Downloader, `findKnownDamagedGap`), direkt dahinter plus 1 s; sonst 5 s und, scheitert die Wiedergabe innerhalb von 60 s kurz hinter dem letzten Ziel erneut (dieselbe Lücke), 10 bzw. 20 s ab der ursprünglichen Stelle (höchstens bis kurz vor das Ende). Steht das Element im Fehlerzustand, lädt der Sprung die Datei neu und setzt die Position über `sflixResume`. Der Hinweis nennt die tatsächlich übersprungenen Sekunden; gemeldet wird der Fund einmal je Datei und Stelle (`reportDamagedSpot`, mit den Sekunden des ersten Sprungs). Nach drei Sprüngen an derselben Lücke hält der Player an; im Fehlerzustand lädt er dabei angehalten 2 s vor der Stelle neu, damit Spulen hinter die Stelle wieder geht. Spulen, Bild-im-Bild und ein Wechsel der Datei setzen die Zählung zurück. Im Chromium des Vorschau-Servers liefen die Testdateien ohne Rücksprung durch; das echte Verhalten in WebView2 und in der App ist nur über Funde des Benutzers nachweisbar.
- **Liste:** `DamagedList` (Desktop) ist die einzige Stelle, die `damaged.js` schreibt (Format `docs/INTERFACES.md` Abschnitt 4). Ohne Desktop bleiben Funde des Players im Speicher des Geräts (`damagedLocal`), ausgeblendete Einträge in `damagedDismissed`; beide kommen nicht in Sicherungen. Die App liest `damaged.js` über `FileCache` wie die übrigen Programmdateien und schreibt nichts auf die Freigabe.

### 3.9 Bild-im-Bild im Desktop und WebView2-Argumente (seit Desktop 4.2.0)
Beim eigenen Bild-im-Bild (DEC-341, `docs/INTERFACES.md` Abschnitt 1) wandert das Video-Element in ein eigenes Fenster derselben WebView2-Umgebung, und das Hauptfenster wird minimiert (seit 4.0.7). Chromium hält die minimierte Seite für verborgen und schaltete mit neueren WebView2-Laufzeiten die Videospur ab (stehendes Bild, nur Ton; SFLIX-20261123-095). Seither startet `PlayerForm.InitWebView` die Umgebung mit `--disable-features=BackgroundVideoTrackOptimization,BackgroundVideoPauseOptimization` zusätzlich zu `--autoplay-policy=no-user-gesture-required`.
- Die Argumente gelten für die ganze Umgebung (Datenordner je Installation, `WebViewLibs.UserDataFolder`). Läuft sie bereits mit anderen Argumenten (`ArgumentException` oder `COMException` 0x8007139F), startet der Desktop mit den bisherigen Argumenten; dann kann das Bild im Bild-im-Bild wieder stehen bleiben (R-47).
- Folge: Auch ohne Bild-im-Bild dekodiert ein minimiertes Player-Fenster das Video weiter (mehr Last als vorher, Ton unverändert). Ein Wechsel der Evergreen-Laufzeit kann Namen von Chromium-Funktionen ändern (R-38).

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
Lebende Liste der technischen Risiken, Erstbestand aus der Gesamtanalyse SFLIX-20261122-049 (22.11.2026, gegen den Code geprüft), nachgeführt am 23.11.2026 (SFLIX-20261122-059). Abschnitt D je Block in `docs/DECISIONS.md` bleibt das historische Protokoll und wird nicht geändert. **Pflege:** Architekt. Andere Rollen nennen neue oder erledigte Risiken beim Ticketabschluss (`-Risiken`) oder per Ticket an den Architekten. **Status:** offen · in Arbeit (Ticket) · umgesetzt (Version; Prüfung ausstehend, Prüfauftrag genannt) · akzeptiert (bewusst getragen) · behoben (nach bestandener Prüfung in 6.4 verschieben).

Prüfaufträge: SFLIX-20261122-061 (Desktop 4.1.0, STEFFLIX 2.8.15, App 1.3.2: Datenintegrität, signierte Updates, Aufräumen), SFLIX-20261122-064 (App 1.3.2 im Emulator, Release-Werkzeuge), SFLIX-20261123-075 (STEFFLIX 2.8.16, Desktop 4.1.1), SFLIX-20261123-081 (Nachprüfung Funde 079: Desktop 4.1.3, STEFFLIX 2.8.17, App 1.3.3), SFLIX-20261123-086 (Desktop 4.1.4: Release-Nummer, DPAPI-Schlüssel), SFLIX-20261123-097 (STEFFLIX 2.9.0, Desktop 4.2.0: Lücken, beschädigte Dateien, Funde 090, Bild-im-Bild).

### 6.1 Datenintegrität
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-01 | Desktop | Konvertieren mit „Original löschen“ löschte das Original, bevor das Ergebnis an seinem Platz war | umgesetzt 4.1.0 (Prüfung 061) | Ergebnis zuerst, Original zuletzt (050) |
| R-02 | Desktop | War `downloads.json` beim Start nicht lesbar, wurden die Teilstücke pausierter Downloads gelöscht | umgesetzt 4.1.0 (061) | Lesefehler sperrt Speichern und Aufräumen (050) |
| R-03 | Desktop | Kopieren über Laufwerksgrenzen schrieb direkt unter dem Endnamen; ein Abbruch hinterliess abgeschnittene Dateien | umgesetzt 4.1.0 (061) | Kopie über `<Ziel>.__temp__`, Downloads in Nachbearbeitung pausierbar (050) |
| R-04 | Desktop | `data.js` wurde nicht atomar geschrieben; ein fehlendes Laufwerk entfernte seinen Zweig ohne Rückfrage | umgesetzt 4.1.0 (061) | `data.js.tmp`, Plausibilitätsprüfung mit Rückfrage (DEC-361). Rest: Teil-Einlesen übersieht Änderungen ausserhalb der gewählten Ordner (akzeptiert) |
| R-05 | Desktop | Beim Start wurden alle `*.old` im Installationsordner gelöscht, auch fremde | umgesetzt 4.1.0 (061) | nur eigene Sicherungen (`Net.IsOwnBackup`) |
| R-06 | Desktop | Vorübergehende Netzwerkfehler wurden dauerhaft als „defekt“ gemerkt und zum Löschen vorausgewählt | umgesetzt 4.1.0 (061) | mehr vorübergehende Fehler erkannt, Neuprüfung vor dem Löschen (DEC-361) |
| R-07 | Desktop | Abbrechen einer Tool-Aufgabe beendete ffmpeg laufender Downloads; ffmpeg lief nach dem Programmende weiter | umgesetzt 4.1.0 (061) | Prozesse je Aufgabe, Job-Objekt |
| R-08 | Desktop | Download-Nachbearbeitung und „Konvertieren“ konnten dieselbe Datei gleichzeitig bearbeiten | umgesetzt 4.1.0 (061) | Sperre je Datei |
| R-09 | Desktop | Uneinheitliche „__“-Regel zwischen Einlesen und „Ablagen leeren“ | umgesetzt 4.1.0 (061) | eine Regel: Name beginnt mit `__` (DEC-361, 3.1). Folge: Ordner mit `__` mitten im Namen erscheinen jetzt in der Mediathek |
| R-10 | Desktop | Ordnerdurchläufe folgten Junctions; Schreibzugriffe konnten ausserhalb des Installationsordners wirken, Zyklen liefen lange | umgesetzt 4.1.0 (061), Rest in Arbeit | Verknüpfungen werden nie durchsucht (DEC-361 Punkt 4, `Folders.Ignore`/`IsLink`). Rest: `ImportTask.RemoveIfEmpty` und `EpisodeTask.MergeFolder` (SFLIX-20261123-076). Gemeinsamer Walker `Folders.Walk` seit 4.1.1 (060); `DataTask.Crawl`, `MediaCheckTask.ScanFolder` und die Walker in Aufräumen, Folgen umbenennen und Import bleiben eigenständig |
| R-11 | STEFFLIX | „Alle Daten löschen“ entfernte die Migrationsmarken; beim nächsten Start liefen alte Migrationen erneut | umgesetzt 2.8.16 (075) | Marken bleiben, alte Migrationen nur bei Altbestand (DEC-367) |
| R-12 | STEFFLIX | „Verwaiste Einträge entfernen“ löschte ohne Sicherung | umgesetzt 2.8.16 (075) | Bestätigung mit Sicherung (DEC-366) |
| R-13 | alle | Favoriten, Fortschritt und Werte je Ordner hängen am Pfad; Umbenennen ausserhalb der Tools mit `renamed.js` (Explorer, Konvertieren, Wiederherstellen) erzeugt verwaiste Einträge; `renamed.js` hält höchstens 5000 Paare | akzeptiert | Zusammenfügen schreibt seit 4.1.0 Paare; übrige bewusst getragen |
| R-14 | Desktop | Wiederholtes Wiederherstellen oder Importieren mit „Als Kopie speichern“ erzeugt Dubletten (`_1`, `_2`), die „Duplikate finden“ und „Folgen umbenennen“ wieder melden | offen | Namensschema vereinheitlichen |
| R-15 | Desktop | Gemerkte Prüfergebnisse erkennen Änderungen nur über Grösse und Änderungszeit | akzeptiert | – |
| R-16 | Desktop | Heuristiken: „Folgen umbenennen“ (Titel mit kleiner Zahl als Doppelfolge, Staffelordner mit Zusatz, Kategorie als Serienordner), „Duplikate finden“ wählt Sprachfassungen und gleichnamige Dateien zum Aussortieren vor | offen | Vorschläge im Plan sichtbar machen; Vorauswahl nur bei identischen Dateien |
| R-17 | Desktop | Folgenlisten aus HTML-Seiten (reguläre Ausdrücke) fallen bei einem Umbau der Seite still aus | akzeptiert | Rückfall auf die nächste Quelle |
| R-46 | Desktop | Die Lückensuche erkennt nur Sprünge in den Zeitstempeln: Bitfehler ohne Lücke, Lücken unter 0,5 s und Lücken nur in der Tonspur bleiben unerkannt; ein gekürztes `.m4s` verdirbt alles danach | akzeptiert (Prüfung 097) | Teilstück-Prüfung im Downloader fängt gekürzte Teilstücke ab; volles Dekodieren nur über „Ganze Datei prüfen“ (DEC-373) |

### 6.2 Update, Release und Repository
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-18 | Desktop | Prüfsummen und Dateien kamen aus demselben Repository; ein kompromittiertes Konto hätte EXE, Web-Dateien oder ffmpeg ersetzen können | umgesetzt 4.1.0 (061) | Signierte Updates (3.7, DEC-356). Gilt für Installationen ab 4.1.0; ältere Desktops prüfen nicht |
| R-19 | Release | EXE, `checksums.json` und `checksums.sig` müssen im selben Push liegen; GitHub liefert kurz nach einem Push teils verzögert aus | akzeptiert | Push-Liste von `release.ps1`; der Desktop meldet eine unpassende Signatur als vorübergehend |
| R-20 | Release | Übergangsdateien für alte Installationen (`sflix.js`, `updater.exe`, `updateData.exe`, `updateStefflix.exe`): wann sie entfallen dürfen, ist nicht festgelegt; zu frühes Entfernen bricht alte Updater | offen | Kriterium festlegen (Architekt mit Benutzer) |
| R-21 | Release | Die Komponente eines Changelog-Eintrags ergab sich still aus dem Textanfang; Text ohne HTML-Escape | umgesetzt (Prüfung 064) | `news.py check` meldet ungültige Präfixe, `html.escape` (053) |
| R-22 | Repo | Lokale Testdaten unter `.claude\` waren nicht in `.gitignore` | umgesetzt (064) | `.gitignore` ergänzt, persönliche Pfade nur in nicht gepushten Dateien (DEC-363). Rest beim Benutzer: bereits gepushte Dateien der Testumgebung aus dem Repository entfernen |
| R-23 | Release | `versionCode` der App (x·10000 + y·100 + z) setzt y und z unter 100 voraus | umgesetzt (064) | Prüfung in `release.ps1` (052) |
| R-24 | Release | Der Android-Build lädt JDK, SDK und Gradle ohne Prüfsumme und bestätigt die SDK-Lizenzen automatisch | akzeptiert | nur auf dem Entwicklungs-PC |
| R-25 | Desktop | Ein Ordner mit `SFLIX_Tools\Release\release.ps1` gilt als Release-Ordner; auch eine Kopie des Repositorys verliert dann bei jedem Start ihre lokalen Dateien | offen (niedrig) | Eigene, nicht gepushte Markierung prüfen |
| R-26 | alle | Der Lizenzschlüssel enthält den Zugang zum Feedback-Kanal; die Verschleierung schützt ihn nicht (DEC-010) | akzeptiert | Schlüssel je Lizenz, Erwähnungen ausgeschaltet |
| R-27 | Desktop, App | Die Diagnose im Feedback enthält Installations- und ffmpeg-Pfade, Protokollzeilen bzw. Server, Freigabe und Benutzer der Quelle | offen | Pfade kürzen bzw. maskieren, Vorschau vor dem Senden |
| R-28 | Desktop | Download: Adressen aus Playlisten werden nicht nach Schema und Ziel geprüft; keine Grössengrenzen für Playlist, Teilstücke und Schlüssel | offen | nur http/https, Grenzen setzen |
| R-43 | Release | Gehen beide privaten Signaturschlüssel verloren, sind Desktop-Updates nur noch durch Neuinstallation möglich; ein gestohlener Schlüssel erlaubt signierte Fremd-Updates | akzeptiert | Hauptschlüssel nur als `update_key.dpapi` (DPAPI, nur für das Konto des Herausgebers lesbar, DEC-371) in `SFLIX_Tools\Release\signing\` (ignoriert), unverschlüsselte Sicherungen und Reserveschlüssel nur offline; Schlüsselwechsel über eine neue EXE (`docs/RELEASE.md`) |
| R-44 | Desktop | Jeder früher signierte Stand bleibt gültig: Mit dem GitHub-Konto liesse sich eine ältere Weboberfläche oder ein älteres ffmpeg (bekannte Lücken) wieder einspielen. Die EXE schützt der Versionsvergleich (`SelfUpdate.FindNewer`) | umgesetzt 4.1.4 (Prüfung 086 ausstehend) | Streng steigende, mitsignierte Release-Nummer `_serial` in `checksums.json`; der Desktop speichert die höchste angenommene (`update.serial`) und lehnt kleinere oder fehlende ab (DEC-372) |
| R-45 | Desktop | Einfrieren: Wer das Konto hat, kann dauerhaft denselben gültigen Stand ausliefern; es kommen keine Updates mehr an | akzeptiert | Ein Ablaufdatum der Signatur passt nicht zu unregelmässigen Releases (DEC-372); Löschen der Dateien wäre ohnehin möglich |

### 6.3 Schnittstellen und Laufzeit
| ID | Komp. | Risiko und Wirkung | Status | Massnahme |
|---|---|---|---|---|
| R-29 | App, Desktop | Namen der Weboberfläche ändern sich: `inject.js`, Prototyp-Eingriffe der Medienumleitung, Einfügemarke vor `main.js`, `isDesktopApp`/Brücke brechen unbemerkt | akzeptiert | Liste in `docs/INTERFACES.md` Abschnitt 2 und in `inject.js` vollständig halten; Einfügen mit Rückfall vor `</body>`, geprüft von `check_release.py` (1.3.2); Agent `schnittstellen-pruefer` |
| R-30 | STEFFLIX | Seitenwechsel ohne Neuladen: Timer und Listener der vorherigen Seite blieben übrig | umgesetzt 2.8.16 (075) | Regel in 4.2; Aufräumen in `resetPage`/`stopCurrentMedia` (051) |
| R-31 | STEFFLIX | Der Autoplay-Countdown wurde an seiner Anzeige erkannt | umgesetzt 2.8.16 (075) | eigenes Flag (051) |
| R-32 | STEFFLIX | Der Hinweis auf fehlende Programmdateien lag unter dem Ladescreen; geprüft wurde nur je ein Name pro Datei | umgesetzt 2.8.16 (075) | Endmarkierungen je Datei, `typeof data` (051) |
| R-33 | App | Der UI-Thread wartete während eines Verbindungsaufbaus auf die SMB-Verbindung | umgesetzt 1.3.2 (064) | Zustand ohne Sperre (052) |
| R-34 | App | Ein Fehler eines Threads schloss die gemeinsame Verbindung eines anderen; Vorausladen konnte nach dem Beenden neu verbinden | umgesetzt 1.3.2 (064) | nur eigene Verbindung schliessen, Prefetches beim Beenden schliessen (052) |
| R-35 | App | Bei einem Fehler des Android-Keystores wurden leere Passwörter gespeichert | umgesetzt 1.3.2 (064) | Passwort bleibt, Anzeige „Passwort neu eingeben“ (052) |
| R-36 | App | Bei pausierter Wiedergabe blieb die Seite im Hintergrund aktiv; Fehler beim Start des Vordergrunddienstes wurden verschluckt | umgesetzt 1.3.2 (064) | Freigabe nach 15 s Pause, Fehler protokolliert (052) |
| R-37 | App | `MediaServer` auf 127.0.0.1 ist nur durch zufälligen Port und Schlüssel im Pfad geschützt; `MIXED_CONTENT_ALWAYS_ALLOW` | umgesetzt 1.3.2 (064), Rest offen | Pfadfilter und begrenzter Thread-Pool (052); ob Mixed Content eingeschränkt werden kann, klärt die Prüfung im Emulator |
| R-38 | Desktop, App | Neue WebView2- und WebView-Versionen ändern Verhalten (Hintergrundwiedergabe, still ignorierte Einstellungen) | akzeptiert | nach Runtime-Updates gezielt testen (`docs/TESTING.md`) |
| R-39 | alle | Alte Versionszahl in vier Implementierungen (`versioning.py`, `core.js`, `UpdateTask.cs`, `release.ps1`) | akzeptiert | am 22.11.2026 gleich; bei Änderungen alle vier anpassen |
| R-40 | App | Mischstand nach einem Update (neue `start.html`, alte Skripte), bis alle Ordner geprüft sind | akzeptiert | „STEFFLIX-Dateien neu laden“ (DEC-220) |
| R-41 | Desktop | Selbst gezeichnete Listen mit Häkchen: drei Listen (Folgen umbenennen, Albumbilder, Zusammenfügen) sind beim Auf- und Abbau nicht gegen leere Einträge geschützt | offen | Schutz wie in `CompareView` |
| R-42 | Doku | `decisions.py` legte doppelte Block-Überschriften an, sobald sich Rollen abwechselten | umgesetzt (064) | gleichnamiger Block wird wiederverwendet (053) |
| R-47 | Desktop | Bild-im-Bild zeigte nur ein stehendes Bild, weil Chromium die Videospur der minimierten Seite abschaltete; läuft die WebView2-Umgebung schon mit anderen Argumenten, gilt der Rückfall ohne die Abhilfe. Ein minimiertes Player-Fenster dekodiert jetzt auch ohne Bild-im-Bild weiter | umgesetzt 4.2.0 (Prüfung 097 ausstehend) | Chromium-Argumente in `PlayerForm.InitWebView` (3.9); hilft das nicht, das Hauptfenster beim Bild-im-Bild nicht mehr minimieren (nur nach Freigabe des Benutzers) |
| R-48 | STEFFLIX | Die Erkennung beschädigter Stellen im Player stützt sich auf Rücksprünge, Verbindungserkennung und Decodierfehler (DEC-380), nicht auf die Datei: echte Verbindungsprobleme an derselben Stelle können als Fund gelten, das echte Zurückspringen war im Test nicht nachstellbar | akzeptiert (Prüfung 097) | nur Sprung, Hinweis und Eintrag in der Liste, nie eine Änderung an der Datei; Eingaben des Benutzers setzen die Zählung zurück (DEC-373) |

### 6.4 Behoben (geprüft am 22.11.2026)
- Laden der WebView2-Bibliotheken vor dem ersten WebView2-Typ (`WebViewLibs.Register` zuerst in `Program.Main`).
- Signaturprüfung des WebView2-Bootstrappers vor dem Start (`Signature.CheckMicrosoft`).
- Version der neuen EXE direkt aus den Bytes, ohne Temp-Datei (DEC-312).
- Raster der Kacheln beim Seitenwechsel (`closeCardGrid` in `resetPage`).
- Laufwerkswurzeln bei der Installationskennung (voller Pfad für Dateizugriffe); Name der Zweitverknüpfung bei einer Wurzel seit 4.1.0.
- Endungslisten von Weboberfläche und Einlesen sind gleich; seit 4.1.0 zentral in `MediaTypes`, auch für „Konvertieren“.

## 7. Verweise
- Dateien: `docs/WEB.md`, `docs/DESKTOP.md`, `docs/ANDROID.md`, `docs/RELEASE.md`; Tests: `docs/TESTING.md`.
- Schnittstellen und Formate: `docs/INTERFACES.md`.
- Entscheide: `python SFLIX_Tools\Doku\decisions.py suchen <Wort>`; zentrale Entscheide: DEC-090 (Speicher je Installation), DEC-177/DEC-180 (App lädt über abgefangene Anfragen und `MediaServer`), DEC-219/DEC-220 (Zwischenspeicher der App), DEC-226 (Dateilisten aus `checksums.json`), DEC-271 (Release-Ordner gesperrt), DEC-275/DEC-276/DEC-281 (Desktop mit WebView2), DEC-288 (Neuladen nach Änderungen), DEC-306 (Seitenwechsel ohne Neuladen), DEC-312 (Version aus den Bytes), DEC-356 (signierte Updates), DEC-357 (Risikoregister), DEC-372 (Release-Nummer), DEC-373/DEC-374 (lückenhafte Videos, `damaged.js`), DEC-375 (ffmpeg bei abgelehnten Prüfsummen).
