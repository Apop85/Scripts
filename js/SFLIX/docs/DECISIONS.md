# STEFFLIX – Entscheidungen

Getroffene Entscheidungen zu STEFFLIX, Desktop und Android-App mit Begründung, verworfenen Alternativen und Folgen. Ein Block je Sitzung bzw. Umsetzung (Überschrift `# …`), am Ende jedes Blocks die Abschnitte A (offene Entscheidungen), B (Änderungsbedarf an bestehenden Entscheidungen), C (Dokumentationsbedarf) und D (technische Risiken).

**Pflege nur über `SFLIX_Tools\Doku\decisions.py`** (nie die ganze Datei lesen): `zeigen`, `suchen`, `baum` (Abhängigkeiten, auch `--rueckwaerts`), `neu`, `ergaenzen`, `status`, `ersetzen`, `offen`/`aenderung`/`doku`/`risiko`, `check`. Jede Rolle hält die Entscheide, die sie mit dem Benutzer trifft, im selben Schritt fest und nennt die DEC-IDs beim Abschluss des Tickets. Felder: Status (Proposed, Accepted, Implemented, Rejected, Superseded), Datum, Kategorie (Architektur, UI, Desktop, Android, Performance, Sicherheit, Datenintegrität, Release, Arbeitsweise, Sonstiges; ältere Einträge auch andere), Ticket, Entscheidung, Begründung, Alternativen, Konsequenzen, Risiken, Betroffene Komponenten, Abhängigkeiten. Einträge, die nicht öffentlich sein dürfen, stehen in `docs/intern/DECISIONS-intern.md` (nicht gepusht, gemeinsame Nummerierung; Lücken in den Nummern hier sind deshalb gewollt).

## Statistik

| Status | Anzahl |
|---|---|
| Proposed | 4 |
| Accepted | 38 |
| Implemented | 268 |
| Rejected | 29 |
| Superseded | 32 |
| **Total** | **371** |

## Block 1: Session STEFFLIX 2.00 (Oktober 2026)

Auswertung einer einzelnen Claude-Code-Session (Arbeit an STEFFLIX 2.00, Oktober 2026), erfasst am 2026-11-13. Grundlage ist **nur der Verlauf dieser Session**; der Status beschreibt den Stand am Ende der Session. Der erste Teil der Session ist nur als Zusammenfassung erhalten (DEC-001 bis DEC-012 knapper beschrieben).

---

## Architektur

### DEC-001: STEFFLIX läuft ausschliesslich über file://, ohne Webserver
Status: Accepted
Datum: 2026-11-13
Kategorie: Architektur
Entscheidung: Die Weboberfläche muss per Doppelklick auf `start.html` (file://) funktionieren. Für Endnutzer gibt es keinen lokalen Webserver.
Begründung: Vorgabe des Nutzers; STEFFLIX soll ohne Installation von Zusatzsoftware laufen.
Alternativen: Lokaler Webserver – vom Nutzer ausgeschlossen. Ein Python-Server wird nur für Tests verwendet (`.claude\launch.json`).
Konsequenzen: Keine ES-Module, kein fetch auf lokale Dateien; Daten kommen über Skriptdateien (`data.js`, `renamed.js`, `version.js`). Externe Daten (Changelog, neueste Version) nur über GitHub. Ausnahme: Die App betreibt für Medien einen Server nur auf 127.0.0.1 (DEC-180, Videopuffer DEC-210).
Betroffene Komponenten: `start.html`, `sflix_sys\js\*.js`
Abhängigkeiten: DEC-002, DEC-014

### DEC-002: Speicher in IndexedDB mit synchronem Cache
Status: Implemented
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: Gespeicherte Daten liegen in der IndexedDB „STEFFLIX“. Ein Cache im Speicher (`storageCache`) erlaubt synchronen Zugriff über `getData`, `setData`, `removeData`, `getAllDataKeys`. Daten aus localStorage werden migriert.
Begründung: Laut Zusammenfassung umgesetzt; ermöglicht synchrone Nutzung im bestehenden Code bei dauerhaftem Speicher. Genauere Begründung nicht erhalten.
Alternativen: Nicht überliefert.
Konsequenzen: Schreibzugriffe laufen asynchron im Hintergrund; der Start wartet auf das Laden des Caches.
Betroffene Komponenten: `storage.js` (`initDatabase`, `loadDatabaseCache`, `migrateLocalStorageToIndexedDB`)
Abhängigkeiten: DEC-003, DEC-013

### DEC-003: Datenmigrationen mit Versionsmarkierungen und Umbenennungsliste
Status: Implemented
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: `updateData()` führt Migrationen einmalig aus und setzt danach Markierungen (`v1.28`, `v1.33`, `v1.38`, `v2.00`). Von updateData.exe umbenannte Pfade werden über `sflix_sys\renamed.js` in Favoriten, Zeitstempeln und Gesehen-Markierungen nachgezogen (Markierung `renamedPathsApplied`).
Begründung: Gespeicherte Daten älterer Versionen sollen erhalten bleiben, wenn sich Format oder Dateinamen ändern.
Alternativen: Nicht überliefert.
Konsequenzen: Die Migration 1.28 ist nicht idempotent (benennt Schlüssel um); Markierungen dürfen deshalb nicht leichtfertig zurückgesetzt werden (siehe DEC-013, DEC-041).
Betroffene Komponenten: `storage.js` (`updateData`, `applyRenamedPaths`, `migrateLegacyTimestamps`, `removePlaylists`, `normalizeLastPlayedUrls`), `getfilepath.py`
Abhängigkeiten: DEC-013, DEC-037

### DEC-004: Updater installiert alles oder nichts, mit Prüfsummen und Rückabwicklung
Status: Implemented
Datum: 2026-11-13
Kategorie: Sicherheit
Entscheidung: Heruntergeladene Dateien werden geprüft (HTTP-Status, nicht leer, gültiges UTF-8 bei Text, MZ-/PE-Kennung bei .exe, SHA256 gegen `checksums.json`; Text mit vereinheitlichten Zeilenenden). Installiert wird erst, wenn alle Dateien gültig sind, mit `.old`-Sicherung und Rückabwicklung bei Fehlern. `SFLIX_UPDATE_URL` erlaubt einen lokalen Testserver.
Begründung: Ein halb installiertes oder manipuliertes Update soll STEFFLIX nicht beschädigen.
Alternativen: Dateiweise Installation ohne Prüfung (Verhalten der alten Updater vor 2.00).
Konsequenzen: `checksums.json` und die .exe-Dateien müssen immer zusammen veröffentlicht werden, sonst verweigern die Updater die Installation. Die Rückabwicklung hat sich im Feldtest bewährt (DEC-038).
Betroffene Komponenten: `sflix_update.py`, `create_checksums.py`, `checksums.json`
Abhängigkeiten: DEC-005, DEC-020, DEC-025

### DEC-005: Updater lädt nur geänderte Dateien
Status: Implemented
Datum: 2026-11-13
Kategorie: Performance
Entscheidung: Dateien, deren lokale Prüfsumme bereits dem Stand in `checksums.json` entspricht, werden übersprungen (`is_up_to_date`).
Begründung: Wunsch des Nutzers; spart Download und Zeit.
Alternativen: Immer alle Dateien laden.
Konsequenzen: Setzt korrekte Prüfsummen voraus.
Betroffene Komponenten: `sflix_update.py`
Abhängigkeiten: DEC-004

### DEC-006: Update-Kette updateStefflix → updater → updateData
Status: Implemented
Datum: 2026-11-13
Kategorie: Architektur
Entscheidung: `updateStefflix.exe` aktualisiert die Web-Dateien, startet `updater.exe --no-pause` (ersetzt `updateStefflix.exe`, das sich nicht selbst ersetzen kann) und danach `updateData.exe`.
Begründung: Eine laufende Datei kann sich nicht selbst ersetzen; Medien sollen nach dem Update automatisch neu eingelesen werden.
Alternativen: Nicht überliefert.
Konsequenzen: Die Programme rufen sich gegenseitig auf; der Arbeitsordner muss dabei stimmen (siehe DEC-038). Im Feldtest zuverlässig nur zusammen mit DEC-038; seit DEC-055 nur noch für den Umstieg alter Installationen.
Betroffene Komponenten: `updateStefflix.py`, `updater.py`, `getfilepath.py`
Abhängigkeiten: DEC-017, DEC-038

## Wiedergabe und Bedienung

### DEC-007: Regeln für „Gesehen“ und „Weiterschauen“
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Ein Video gilt als gesehen ab 95 %, beim Ende oder wenn Autoplay vor dem Ende weiterspringt. „Weiterschauen“ zeigt nach einer gesehenen Folge die nächste Folge an, sonst bleibt der Eintrag mit Markierung „Letzte Folge“/„Letzter Titel“. Die nächste Folge wird nur innerhalb derselben Serie bzw. desselben Ordners gesucht.
Begründung: Wunsch des Nutzers; frühere Fehler führten in fremde Serien oder Kategorien.
Alternativen: Suche über Staffel- und Seriengrenzen hinweg (`swapSeason`) – führte zu falschen Treffern.
Konsequenzen: „Zeitstempel löschen“ entfernt auch die Gesehen-Markierungen.
Betroffene Komponenten: `player.js` (`isSeen`, `markSeen`, `getNextMediaPath`), `pages.js` (`showContinueWatching`), `cards.js` (`addCardLabel`)
Abhängigkeiten: –

### DEC-008: Zusätzliche Dateiformate, Gross-/Kleinschreibung egal
Status: Implemented
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Unterstützt werden Video mp4, m4v, webm, ogv; Musik mp3, m4a, ogg, oga, opus, aac, flac, wav; Bilder jpg, jpeg, png, gif, webp, avif, bmp. Endungen werden ohne Rücksicht auf Gross-/Kleinschreibung erkannt; updateData.exe schreibt Endungen klein.
Begründung: Wunsch des Nutzers (JPG = jpg = JPEG = jpeg).
Alternativen: –
Konsequenzen: Die Listen in `core.js` und `getfilepath.py` müssen übereinstimmen.
Betroffene Komponenten: `core.js`, `getfilepath.py`
Abhängigkeiten: DEC-039

### DEC-009: Grundsätze des Designs
Status: Accepted
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Dark Mode; keine Emojis, stattdessen SVG-Symbole; Eingabefelder hell-anthrazit; runde Schliessen-Knöpfe; Hamburger-Menü statt Zahnrad.
Begründung: Vorgaben des Nutzers für das 2.00-Design.
Alternativen: –
Konsequenzen: Neue Oberflächenelemente folgen diesen Regeln.
Betroffene Komponenten: `sflix.css`, `start.html`, `core.js` (`iconShapes`, `createIcon`)
Abhängigkeiten: DEC-019, DEC-032

### DEC-010: Vorerst nicht umgesetzte Vorschläge
Status: Rejected
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Nicht umgesetzt werden: Härtung des Bot-Tokens im Lizenzschlüssel („aktuell keine Prio“), Untertitel („aktuell nicht“), Benutzerprofile („kein Bedarf“), stabiler Update-Kanal (lokal nicht machbar).
Begründung: Entscheidung des Nutzers; Fokus auf lokal machbare Arbeiten.
Alternativen: –
Konsequenzen: Der Lizenzschlüssel enthält weiterhin den Zugang zum Bot (Risiko D1).
Betroffene Komponenten: –
Abhängigkeiten: DEC-030

### DEC-011: Keine Tests mit Ton-Testdateien
Status: Accepted
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Browsertests nur mit Bildern, dem Beispielvideo (stumm) oder Seiten ohne Wiedergabe; keine erzeugten WAV-Testtöne.
Begründung: Der Piepton stört den Nutzer.
Alternativen: –
Konsequenzen: Audio-Wiedergabe wird in Tests kaum geprüft.
Betroffene Komponenten: Testablauf
Abhängigkeiten: –

### DEC-012: Der Agent baut keine .exe-Dateien
Status: Superseded
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Die .exe-Dateien baut der Nutzer selbst; der Agent nur auf ausdrücklichen Auftrag.
Begründung: Vorgabe des Nutzers.
Alternativen: –
Konsequenzen: Änderungen an Python-Quellen wirken erst nach einem Build durch den Nutzer; der Agent weist darauf hin. Ersetzt durch DEC-082.
Betroffene Komponenten: `build_release.bat`
Abhängigkeiten: DEC-025, DEC-026

## Datensicherung

### DEC-013: Sicherungen enthalten die Version und werden beim Import angepasst
Status: Implemented
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: Beim Import wird die Version der Sicherung angezeigt. Sicherungen aus einer neueren STEFFLIX-Version werden abgelehnt. Für ältere Sicherungen gibt es eine Liste schrittweiser Anpassungen (`backupMigrations`, zurzeit leer, da Sicherungen erst ab 2.00 existieren). Migrationsmarkierungen (`vX.XX`) und `renamedPathsApplied` werden nicht importiert; `renamedPathsApplied` wird nach dem Import entfernt, damit die aktuellen Umbenennungen auch auf die wiederhergestellten Pfade angewendet werden.
Begründung: Wunsch des Nutzers, ältere Exporte an neuere Versionen angleichen zu können. Die Markierungen gehören zur installierten Version.
Alternativen: Migrationsmarkierungen zurücksetzen und die Migrationen in `updateData()` erneut laufen lassen – verworfen (DEC-041).
Konsequenzen: Künftige Formatänderungen brauchen einen Eintrag in `backupMigrations`.
Betroffene Komponenten: `storage.js` (`readBackupFile`, `restoreUserData`, `migrateBackupData`, `isMigrationFlag`)
Abhängigkeiten: DEC-003, DEC-041

### DEC-041: Migrationen beim Import nicht erneut ausführen
Status: Rejected
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: Der zunächst angekündigte Plan, Markierungen neuer als die Sicherung zurückzusetzen, damit `updateData()` die Migrationen erneut ausführt, wurde verworfen.
Begründung: Die Migration 1.28 ist nicht idempotent; sie würde auf heutigen Daten Schlüssel wie `settings` oder `seen-…` in `playlist-…` umbenennen.
Alternativen: Eigene Anpassungsliste nur für Sicherungen (DEC-013).
Konsequenzen: Sicherungs-Anpassungen und Datenmigrationen sind getrennt.
Betroffene Komponenten: `storage.js`
Abhängigkeiten: DEC-003, DEC-013

## Aufbau der Weboberfläche und Updates

### DEC-014: sflix.js wird in sieben Dateien aufgeteilt
Status: Implemented
Datum: 2026-11-13
Kategorie: Architektur
Entscheidung: Der Code liegt in `sflix_sys\js\` als core, storage, cards, pages, player, ui, main (Ladereihenfolge in `start.html`). Alle Funktionen bleiben global; beim Laden führt nur `main.js` Code aus. Die Aufteilung wurde per Skript erzeugt und zeilenweise gegen das Original geprüft (keine Zeile verloren oder verändert).
Begründung: Auftrag des Nutzers; bessere Übersicht.
Alternativen: Ein File behalten; ES-Module (wegen file:// nicht möglich, DEC-001).
Konsequenzen: `start.html`, Updater-, Setup- und Prüfsummenlisten mussten angepasst werden; neue Dateien müssen in `sflix_update.py` eingetragen werden.
Betroffene Komponenten: `sflix_sys\js\*.js`, `start.html`, `sflix_update.py` (`FILES`, `SCRIPT_FILES`), `updateStefflix.py`
Abhängigkeiten: DEC-015, DEC-016, DEC-017

### DEC-015: Platzhalter sflix.js bleibt im Repository
Status: Implemented
Datum: 2026-11-13
Kategorie: Architektur
Entscheidung: `sflix_sys\sflix.js` bleibt als kurzer Platzhalter auf GitHub. Der neue Updater löscht die Datei bei Nutzern (`OBSOLETE_FILES`); `check_release.py` prüft, dass der Platzhalter vorhanden ist.
Begründung: Das alte updateStefflix.exe lädt eine feste Liste inklusive `sflix.js` und prüft nicht, ob die Datei existiert.
Alternativen: Datei löschen – würde alte Updater stören.
Konsequenzen: Der Platzhalter darf nicht gelöscht werden, solange alte Installationen im Umlauf sind. Bestätigt durch DEC-055; sflix.js steht heute in _obsoleteFiles (bei Nutzern gelöscht) und bleibt für alte Updater im Repository.
Betroffene Komponenten: `sflix_sys\sflix.js`, `sflix_update.py`, `check_release.py`
Abhängigkeiten: DEC-017

### DEC-016: Hinweis bei fehlenden Programmdateien
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: `start.html` prüft nach dem Laden, ob zentrale Funktionen aller sieben Dateien vorhanden sind, und zeigt sonst den Hinweis, STEFFLIX sei unvollständig installiert und updateStefflix.exe sei erneut auszuführen.
Begründung: Ein unvollständiges Update soll keine leere Seite hinterlassen.
Alternativen: –
Konsequenzen: Die geprüften Funktionsnamen müssen bei Umbenennungen nachgeführt werden.
Betroffene Komponenten: `start.html`
Abhängigkeiten: DEC-014

### DEC-017: Umstieg auf 2.00 mit einem einzigen Lauf von updateStefflix.exe
Status: Implemented
Datum: 2026-11-13
Kategorie: Architektur
Entscheidung: Das alte updateStefflix.exe lädt den neuen `updater.exe` und startet ihn. Der neue Updater lädt alle fehlenden Web-Dateien nach (`WEB_FILES`, z.B. `sflix_sys\js\*.js`, `sflixLogo.png`), entfernt veraltete Dateien und startet bei einem solchen Umstieg danach `updateData.exe`.
Begründung: Nutzer sollen updateStefflix.exe nicht zweimal ausführen müssen. Die ursprüngliche Fassung des alten Updaters (aus dem Session-Verlauf) zeigte, dass er `updater.exe` herunterlädt und startet.
Alternativen: Zweimaliges Ausführen – verworfen.
Konsequenzen: Verhalten hängt vom alten updateStefflix.exe ab; mit Netzwerkpfaden trat ein Fehler auf (DEC-038). Gilt seit DEC-055 nur noch für den Umstieg alter Installationen.
Betroffene Komponenten: `updater.py`, `sflix_update.py` (`WEB_FILES`, `OBSOLETE_FILES`, `remove_obsolete_files`)
Abhängigkeiten: DEC-006, DEC-015, DEC-038

### DEC-018: Zufallswiedergabe ohne Wiederholungen, mit gespeichertem Verlauf
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Jedes Medium kommt pro Durchgang nur einmal; sind alle gespielt, ist die Playliste zu Ende (Autoplay hält an). „Zurück“/„Weiter“ bewegen sich im Verlauf, neue Medien kommen erst am Ende des Verlaufs. Der Verlauf wird gespeichert (`shuffleState`) und übersteht ein Neuladen. Ein neuer Durchgang beginnt mit „Zufällig abspielen“ oder beim Einschalten des Schalters; ein Wechsel der Playliste setzt den Verlauf zurück.
Begründung: Wunsch des Nutzers („wenn alle Medien durch sind, ist die Playliste zu Ende“).
Alternativen: Bisheriges Verhalten (DEC-042).
Konsequenzen: Bei aktiver Zufallswiedergabe kein Rückfall auf die normale Reihenfolge oder Nachbarstaffeln.
Betroffene Komponenten: `player.js` (`getShuffleState`, `recordShuffleHistory`, `getShuffleNeighbour`, `resetShuffleHistory`, `getNeighbourData`)
Abhängigkeiten: DEC-042

### DEC-042: Zufallswiedergabe mit Verlauf nur im Speicher
Status: Superseded
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Frühere Umsetzung: Verlauf nur im Arbeitsspeicher, nach allen Medien Neubeginn, „Zurück“ kürzt den Verlauf.
Begründung: –
Alternativen: –
Konsequenzen: Wiederholungen nach Neuladen oder Zurückgehen möglich. Ersetzt durch DEC-018.
Betroffene Komponenten: `player.js` (`shuffleHistory`, `shuffleNavigatingBack`, entfernt)
Abhängigkeiten: DEC-018

## Logo und Startbildschirm

### DEC-019: Neues Logo im Band-Stil mit fliessendem Farbverlauf
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Logo-Variante C3: Band-S mit kantigen Enden auf dunkler Kachel, Farbverlauf von Hell- zu Dunkelrot über die Mitte des S. Als `sflixLogo.svg` für Kopfzeile und Browser-Tab, `sflixLogo.png` (256 px, aus dem SVG gerendert) als Ersatz-Symbol. In der Kopfzeile zusätzlich der Schriftzug „STEFFLIX“, zwischen 601 und 1100 px Breite ausgeblendet.
Begründung: Wahl des Nutzers; Verlauf auf seinen Wunsch „fliessender“. Schriftzug bei mittlerer Breite aus, weil sonst das Kategorien-Menü zu eng wird.
Alternativen: Varianten A (rotes S, dunkel), B (weisses S, rot), C1/C2/C4/C5 (Band-Varianten) – nicht gewählt.
Konsequenzen: Beide Logodateien in die Download-Listen aufgenommen, damit bestehende Installationen das Logo erhalten.
Betroffene Komponenten: `sflix_sys\sflixLogo.svg`, `sflix_sys\sflixLogo.png`, `start.html`, `sflix.css` (`#logoText`), `sflix_update.py`, `updateStefflix.py`
Abhängigkeiten: DEC-020

### DEC-020: SVG gilt bei den Prüfsummen als Textdatei
Status: Implemented
Datum: 2026-11-13
Kategorie: Sicherheit
Entscheidung: `.svg` wurde in `TEXT_EXTENSIONS` aufgenommen; Zeilenenden werden vor der Prüfsumme vereinheitlicht.
Begründung: Unterschiedliche Zeilenenden (Windows/Git) hätten sonst zu falschen Prüfsummen geführt.
Alternativen: –
Konsequenzen: Gilt für alle künftigen SVG-Dateien.
Betroffene Komponenten: `sflix_update.py`
Abhängigkeiten: DEC-004

### DEC-027: Animierter Startbildschirm als SVG
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Neuer Splashscreen `sflix_sys\splash.svg` (etwa 3 KB): Leuchten, das Band-S zeichnet sich, Schriftzug, Linie, Untertitel. Quadratischer Bildausschnitt, Anzeige mit `object-fit: contain` vor gleichfarbigem Hintergrund, Standarddauer 3,6 s; bei „Animationen reduzieren“ direkt das fertige Bild. Ersetzt das Foto `sflix.png`.
Begründung: Auftrag des Nutzers, passend zum neuen Design. Das quadratische Format macht das Logo im Hochformat fast doppelt so gross; contain verhindert Abschneiden.
Alternativen: Zunächst Seitenverhältnis 16:9 – auf dem Handy zu klein, verworfen. `object-fit: cover` – hätte Inhalte abgeschnitten.
Konsequenzen: `splash.svg` statt `sflix.png` in den Download-Listen.
Betroffene Komponenten: `sflix_sys\splash.svg`, `pages.js` (`showSplashscreen`), `sflix.css` (`#splashscreen`, `splash-out`), `sflix_update.py`, `updateStefflix.py`
Abhängigkeiten: DEC-022, DEC-028

## Aufräumen von Dateien

### DEC-021: sflix_bg.jpg entfernt
Status: Implemented
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Das ungenutzte Hintergrundbild `sflix_sys\sflix_bg.jpg` (1,3 MB) wurde gelöscht, aus den Download-Listen genommen und in `OBSOLETE_FILES` eingetragen.
Begründung: Wird nirgends mehr verwendet; Freigabe durch den Nutzer.
Alternativen: –
Konsequenzen: Bestehende Installationen löschen die Datei beim Update.
Betroffene Komponenten: `sflix_update.py`, `check_release.py`
Abhängigkeiten: –

### DEC-022: Altes Splash-Foto sflix.png nicht mehr verwendet
Status: Implemented
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: `sflix.png` wurde aus den Download-Listen genommen; die Datei selbst wurde nicht gelöscht. Empfehlung: im Repository lassen (siehe DEC-040).
Begründung: Ersetzt durch `splash.svg`; das alte updateStefflix.exe lädt `sflix.png` fest herunter.
Alternativen: Löschen – vom Nutzer nicht beantwortet (Offene Entscheidung A1).
Konsequenzen: Die Datei wurde später gelöscht (DEC-085).
Betroffene Komponenten: `sflix_update.py`, `updateStefflix.py`
Abhängigkeiten: DEC-027, DEC-040

### DEC-023: Versionsvorschau (sflix.gif) ersetzt der Nutzer selbst
Status: Accepted
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Das GIF der Versionsvorschau zeigt die alte Oberfläche; der Nutzer ersetzt es selbst. `sflix.gif` bleibt im Repository, weil die Hilfe es von GitHub lädt.
Begründung: Aussage des Nutzers („mache ich dann noch“).
Alternativen: Abschnitt entfernen – nicht gewählt.
Konsequenzen: Bis dahin zeigt die Hilfe das alte Design.
Betroffene Komponenten: `sflix_sys\sflix.gif`, `pages.js` (`previewGifUrl`)
Abhängigkeiten: –

## Release-Ablauf

### DEC-024: create_news.py im Projekt
Status: Implemented
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Das Skript, das `news.json` aus `news.txt` erzeugt, liegt als `create_news.py` im Projekt (vorher nur im temporären Ordner der Session). `news.txt` bleibt die Quelle, weil Versionen vor 2.00 sie direkt lesen.
Begründung: `news.json` liess sich sonst nicht ohne den Agenten erzeugen.
Alternativen: –
Konsequenzen: Ausgabe war byte-gleich mit dem bisherigen Skript.
Betroffene Komponenten: `create_news.py`, `sflix_sys\news.txt`, `sflix_sys\news.json`
Abhängigkeiten: DEC-025

### DEC-025: build_release.bat ersetzt py_2_exe.bat
Status: Implemented
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Ein Doppelklick führt aus: 1. Prüfung (`check_release.py`: Version in `version.js` = oberste Version im Changelog, Dateien vorhanden, `start.html` lädt alle Programmdateien, Platzhalter vorhanden), 2. `news.json`, 3. Programme bauen (eine .exe wird erst nach erfolgreichem Build ersetzt), 4. `checksums.json` (nach den Programmen), 5. Aufräumen. Abbruch mit Meldung bei Fehlern; Modus `ohne-exe` für Tests. `create_checksums.py` bricht bei fehlenden Dateien ab.
Begründung: Wunsch des Nutzers, nur einmal doppelklicken zu müssen, bevor gepusht wird.
Alternativen: `py_2_exe.bat` (DEC-044).
Konsequenzen: Version in `version.js` und oberster Changelog-Block müssen zusammen geändert werden. Der Schritt „Programme bauen“ und der Modus ohne-exe entfielen (DEC-056); heute baut release.ps1 Desktop und App (docs\RELEASE.md).
Betroffene Komponenten: `build_release.bat`, `check_release.py`, `create_checksums.py`, `create_news.py`
Abhängigkeiten: DEC-004, DEC-024, DEC-026; DEC-044

### DEC-044: py_2_exe.bat als Build-Skript
Status: Superseded
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Früheres Skript, das nur die Programme baute und bestehende .exe vor dem Build löschte.
Begründung: –
Alternativen: –
Konsequenzen: Ersetzt durch DEC-025; Datei gelöscht.
Betroffene Komponenten: `py_2_exe.bat`
Abhängigkeiten: DEC-025

### DEC-026: news.json und checksums.json erzeugt nur noch build_release
Status: Accepted
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Der Agent erzeugt `news.json` und `checksums.json` nicht mehr selbst. Änderungen werden weiterhin in `news.txt` nachgetragen.
Begründung: Der Nutzer führt vor jedem Release ohnehin `build_release.bat` aus.
Alternativen: Agent erzeugt beide Dateien nach jeder Änderung (DEC-045).
Konsequenzen: Zwischen Änderung und Build sind `news.json` und `checksums.json` veraltet.
Betroffene Komponenten: `sflix_sys\news.txt`, `news.json`, `checksums.json`
Abhängigkeiten: DEC-025, DEC-045

### DEC-045: Agent erzeugt news.json und Prüfsummen nach Änderungen
Status: Superseded
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Frühere Praxis in der Session.
Begründung: –
Alternativen: –
Konsequenzen: Ersetzt durch DEC-026.
Betroffene Komponenten: `create_news.py`, `create_checksums.py`
Abhängigkeiten: DEC-026

### DEC-040: Regeln für den Push von 2.00
Status: Proposed
Datum: 2026-11-13
Kategorie: Sonstiges
Entscheidung: Vorschlag: .exe-Dateien, `checksums.json`, `start.html` und `sflix_sys\` (inkl. `js\`, `news.json`) zusammen pushen; `.claude\` nicht pushen; `sflix.js`, `sflix.png` und `sflix.gif` im Repository lassen; nach dem Push eine Kopie einer alten Installation aktualisieren, Firefox und den Feedback-Versand prüfen.
Begründung: Passen .exe und Prüfsummen nicht zusammen, verweigern die Updater die Installation; alte Updater laden feste Dateilisten.
Alternativen: –
Konsequenzen: sflix.png wurde gelöscht (DEC-085); die Liste der zu pushenden Dateien erstellt heute release.ps1.
Betroffene Komponenten: Release-Ordner
Abhängigkeiten: DEC-004, DEC-015, DEC-022

## Feedback und Lizenz

### DEC-030: Feedback-Nachricht als Formulardaten senden
Status: Implemented
Datum: 2026-11-13
Kategorie: Sicherheit
Entscheidung: Die Nachricht wird per `fetch` mit `URLSearchParams` im Body gesendet statt als ungeschützter Text in der Adresse. Meldungen erscheinen als reiner Text mit Farbklassen (error/success/info) statt als HTML mit festen Farben.
Begründung: Nachrichten mit `&`, `#` oder `+` wurden abgeschnitten oder verfälscht.
Alternativen: Bisheriger XMLHttpRequest mit Text in der Adresse.
Konsequenzen: Bis zum Absenden getestet; der echte Versand ist nicht getestet (siehe DEC-031).
Betroffene Komponenten: `ui.js` (`sendMessage`, `showFeedbackStatus`), `start.html` (`#feedbackStatus`), `sflix.css`
Abhängigkeiten: DEC-031

### DEC-031: Agent gibt keine echten Zugangsdaten ein
Status: Rejected
Datum: 2026-11-13
Kategorie: Sicherheit
Entscheidung: Der Vorschlag, dass der Agent den echten Lizenzschlüssel eingibt und eine Testnachricht schickt, wurde nicht umgesetzt. Stattdessen Test mit einem erfundenen Bot und abgefangener Anfrage; den echten Versand macht der Nutzer.
Begründung: Der Schlüssel enthält Zugangsdaten eines externen Dienstes; der Agent tippt echte Zugangsdaten grundsätzlich nicht in Formulare.
Alternativen: Testschlüssel mit `create_serial.py` erzeugen – zu aufwendig, weil er zum festen öffentlichen Schlüssel passen müsste.
Konsequenzen: Der echte Versand bleibt ein manueller Test des Nutzers.
Betroffene Komponenten: `ui.js` (`decodeSerial`, `sendMessage`)
Abhängigkeiten: DEC-030

## Menü, Hilfe und Einstellungen

### DEC-032: Menü-Symbole als SVG mit PNG-Fallback, ohne Tabellen
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Autoplay, Feedback, Lizenz und Hilfe erhalten Linien-Symbole im Stil von Suche und Favoriten. Im CSS steht zuerst das bisherige PNG, darüber das SVG (Browser ohne SVG behalten das PNG). Die Menüeinträge sind mit Flexbox statt Tabellen aufgebaut.
Begründung: Wunsch des Nutzers („SVG-Symbole mit Fallback zu PNG, wie beim Logo“); einheitliches Erscheinungsbild.
Alternativen: PNG-Symbole unverändert lassen (früher erlaubt).
Konsequenzen: Eine ältere Regel für das Handy (`display: block`) musste auf `flex` angepasst werden.
Betroffene Komponenten: `start.html` (`#settingsDD`), `sflix.css` (`.menuIcon`, `.menuLabel`, `#autoplayIcon`, `#supportIcon`, `#lizenzIcon`, `#helpIcon`)
Abhängigkeiten: DEC-009, DEC-033

### DEC-033: Reihenfolge im Menü
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Suche (nur Handy), Autoplay, Favoriten, Lizenz, Feedback, Hilfe. Feedback (nicht in der Vorgabe des Nutzers genannt, nur mit gültigem Lizenzschlüssel sichtbar) steht hinter Lizenz.
Begründung: Vorgabe des Nutzers; Feedback neben Lizenz, weil es davon abhängt.
Alternativen: –
Konsequenzen: Kein Code hängt von der Reihenfolge ab.
Betroffene Komponenten: `start.html`
Abhängigkeiten: DEC-032

### DEC-034: Hilfe inhaltlich überarbeitet
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Hilfetext neu geschrieben: aufklappbare Abschnitte (`<details>`) für Dateiformate und Vorschaubilder statt Links mit `onclick`, Ordnerstruktur als Beispiel, gleiche Begriffe wie im Autoplay-Fenster, Versionen als Liste. Schreibfehler in Hilfe, Setup und updateData korrigiert. Der Changelog-Fehler erscheint als Hinweis statt grosser Überschrift.
Begründung: Auftrag des Nutzers; der Inhalt stammte aus älteren Versionen.
Alternativen: –
Konsequenzen: IDs, die JavaScript verwendet (Browser-Hilfe, Versionen, Sprungmarken), blieben erhalten.
Betroffene Komponenten: `start.html` (`#help`), `sflix.css` (`.helpDetails`, `.folderTree`, `.versionInfo`, `.changelogNotice`), `pages.js`, `setup.py`, `getfilepath.py`
Abhängigkeiten: –

### DEC-035: Autoplay mit Schalter „Unbegrenzt“
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Neuer Schalter „Unbegrenzt“: Autoplay läuft bis zum Ende der Playliste, die Anzahl wird nicht heruntergezählt und ausgegraut. Gespeichert als vierter Wert in `settings`; ältere Einstellungen ohne diesen Wert bleiben gültig. Die Dauer bis Übergang bleibt standardmässig 0.
Begründung: Wunsch des Nutzers; bisher brauchte Autoplay Schalter und Anzahl.
Alternativen: Standarddauer 30 Sekunden – vom Nutzer abgelehnt („muss standardmässig auf 0 bleiben“).
Konsequenzen: Dauer wirkt so: Videos und Musik wechseln „Dauer + 1 s“ vor dem Ende (Musik höchstens 11 s vorher), Bilder bleiben „Dauer + 11 s“ stehen (`minTime` = 11).
Betroffene Komponenten: `ui.js` (`isAutoplayInfinite`, `saveAutoplaySettings`, `updateAutoplayAmountState`, `checkAutoplayAmount`, `updateOptions`, `loadSettings`), `start.html` (`#autoplayInfinite`), `sflix.css`
Abhängigkeiten: –

### DEC-036: Alte Anzeigen ersetzt: roter Rahmen und Protokoll der Fehlerbehebung
Status: Implemented
Datum: 2026-11-13
Kategorie: UI
Entscheidung: Der rote Rahmen um fast fertige Videos über 10 Minuten entfällt (ersetzt durch die Markierung „Gesehen“). Die Fehlerbehebung zeigt eine kurze Statusmeldung (z.B. „Playlisten gelöscht (5 Einträge).“) statt eines Protokoll-Textfelds.
Begründung: Freigabe des Nutzers; der Rahmen wirkte im neuen Design wie ein Fehler, das Protokoll war veraltet.
Alternativen: –
Konsequenzen: `#debugOutput` entfällt, neu `#repairStatus`.
Betroffene Komponenten: `player.js`, `storage.js` (`modifyLocalData`, `repairLabels`), `start.html`, `sflix.css`
Abhängigkeiten: –

### DEC-037: Hinweisfenster der Migrationen 1.33 und 1.38 entfernt
Status: Implemented
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: Die `alert()`-Hinweise der Migrationen 1.33 und 1.38 wurden entfernt; die Migrationen und Markierungen bleiben.
Begründung: Der Hinweis zu 1.38 konnte wegen `version == 1.38` nie mehr erscheinen; beide Hinweise sind überflüssig, weil der Updater ab 2.00 updateData.exe selbst startet und updateData.exe einen fehlenden Updater nachlädt.
Alternativen: Bedingung auf `>= 1.38` ändern – verworfen (DEC-046).
Konsequenzen: Keine Browser-Popups mehr beim Start.
Betroffene Komponenten: `storage.js` (`updateData`)
Abhängigkeiten: DEC-003, DEC-046

### DEC-046: Bedingung „>= 1.38“ für den alten Update-Hinweis
Status: Rejected
Datum: 2026-11-13
Kategorie: Datenbank
Entscheidung: Die Frage des Nutzers, ob die Bedingung `>= 1.38` lauten müsste, wurde mit Nein beantwortet.
Begründung: Die Markierung `v1.38` wird ausserhalb der Bedingung immer gesetzt; mit `>=` hätte auch jede Neuinstallation den Hinweis gesehen. Richtig wäre „Markierung fehlt und alte Daten vorhanden“ gewesen; der Hinweis wurde stattdessen entfernt.
Alternativen: DEC-037.
Konsequenzen: –
Betroffene Komponenten: `storage.js`
Abhängigkeiten: DEC-037

## Programme auf Netzwerkpfaden

### DEC-038: Programme arbeiten immer in ihrem eigenen Ordner
Status: Implemented
Datum: 2026-11-13
Kategorie: Sicherheit
Entscheidung: `updater.exe`, `updateStefflix.exe` und `updateData.exe` bestimmen ihren Ordner über den Pfad der eigenen .exe (`program_dir`, bei Python-Skripten weiterhin das Arbeitsverzeichnis). `enter_stefflix_dir` bricht ab, wenn dort `start.html` oder `sflix_sys` fehlt. updateData.exe startet den Updater mit `subprocess` statt `os.system`.
Begründung: Feldtest des Nutzers mit Installation auf `\\192.168.3.5\...`: Das alte updateStefflix.exe startet den Updater über die Kommandozeile, die Netzwerkpfade nicht unterstützt und nach `C:\Windows` ausweicht. Der Updater versuchte dort zu installieren (von Windows verweigert, Rückabwicklung griff). Danach hätte updateData.exe dort Dateien umbenennen können.
Alternativen: Weiter dem Arbeitsverzeichnis vertrauen (`os.chdir(os.getcwd())`) – verworfen.
Konsequenzen: Wirkt erst nach neuem Build und Push. Mit simuliertem Programm und falschem Arbeitsverzeichnis getestet, nicht mit gebauter .exe auf einem echten Netzwerkpfad.
Betroffene Komponenten: `sflix_update.py` (`program_dir`, `enter_stefflix_dir`), `updater.py`, `updateStefflix.py`, `getfilepath.py`
Abhängigkeiten: DEC-006, DEC-017

## Vorschaubilder

### DEC-039: Grösse der Vorschaubilder unverändert lassen
Status: Implemented
Datum: 2026-11-13
Kategorie: Performance
Entscheidung: Vorschlag, die bestehende Grösse von 480 × 270 (JPEG Qualität 80, mittig auf 16:9 beschnitten) beizubehalten.
Begründung: Rechnung aus dem CSS: Kacheln sind auf den meisten Bildschirmen 143 bis 340 tatsächliche Pixel breit; nur 4K mit 150 % Skalierung (etwa 595 px) wird leicht hochgerechnet.
Alternativen: 640 × 360 – deckt auch 4K ab, Dateien etwa 1,7-mal so gross; nicht beschlossen.
Konsequenzen: Die Konstanten THUMB_SIZE/THUMB_QUALITY entfielen; 480 × 270 und Qualität 80 gelten weiter, jetzt im Preview-Ordner statt in sflix_sys\thumbs (DEC-049, DEC-053), heute in DataTask.cs.
Betroffene Komponenten: `getfilepath.py` (`THUMB_SIZE`, `THUMB_QUALITY`)
Abhängigkeiten: –

---

## A. Offene Entscheidungen

1. **sflix.png löschen?** Die Frage blieb unbeantwortet. Empfehlung war, die Datei wegen alter Updater im Repository zu lassen (DEC-022, DEC-040).
2. **Grösse der Vorschaubilder** 480 × 270 oder 640 × 360 (DEC-039).
3. **Versionsvorschau**: Inhalt des neuen GIF und Zeitpunkt (DEC-023).

## B. Änderungsbedarf an bestehenden Entscheidungen

- DEC-042 → DEC-018 (Zufallswiedergabe), DEC-044 → DEC-025 (Build-Skript), DEC-045 → DEC-026 (news.json und Prüfsummen).
- Der angekündigte Plan für den Import alter Sicherungen wurde vor der Umsetzung geändert (DEC-041 statt Zurücksetzen der Markierungen).
- DEC-006 (Update-Kette) wurde durch den Feldtest relativiert: Sie funktioniert nur zuverlässig mit DEC-038.

## C. Dokumentationsbedarf

- **Architektur**: Ladereihenfolge und Zuständigkeit der sieben JS-Dateien; nur `main.js` führt beim Laden Code aus; neue Dateien gehören in die Listen von `sflix_update.py`.
- **Update-Ablauf**: Kette der Programme, Umstieg von Versionen vor 2.00 (feste Dateilisten alter Updater, Platzhalter `sflix.js`, `sflix.png`), alles-oder-nichts-Installation, Arbeitsordner auf Netzwerkpfaden.
- **Datenmodell**: Speicherschlüssel (`favorites`, `playlast`, `timestamp-…`, `timestampDuration-…`, `seen-…`, `playlist-…`, `settings` mit vier Werten, `shuffle`, `shuffleState`, `dashboardOrder`, Migrationsmarkierungen, `renamedPathsApplied`), Format der Sicherung (`app`, `format`, `version`, `exportedAt`, `data`) und `backupMigrations`.
- **Tests**: Nur über einen Testserver mit eigener Testumgebung, keine WAV-Töne; Updater-Tests mit `SFLIX_UPDATE_URL`, einer Serverkopie mit eigenen Prüfsummen und simuliertem `sys.frozen`; Feedback-Test mit abgefangener Anfrage.
- **Autoplay-Zeiten**: Wirkung der Dauer bis Übergang für Videos, Musik und Bilder (DEC-035).

## D. Technische Risiken

1. Der Lizenzschlüssel enthält den Zugang zum Bot; wer den Schlüssel hat, kann Nachrichten senden. Die Härtung wurde bewusst zurückgestellt (DEC-010). Der Schlüssel wurde in dieser Session im Chat geteilt.
2. Ältere Updater laden feste Dateilisten ohne Fehlerprüfung. Werden `sflix.js`, `sflix.png` oder andere Dateien aus dem Repository entfernt, schreiben sie Fehlerseiten in die Dateien.
3. `.exe` und `checksums.json` müssen immer zusammen veröffentlicht werden; GitHub liefert neue Dateien teils verzögert aus, was kurz nach einem Push zu abgelehnten Updates führen kann.
4. Die Migration 1.28 ist nicht idempotent; Markierungen dürfen nicht zurückgesetzt werden.
5. Nicht getestet: Umstieg mit dem echten alten updateStefflix.exe auf einem lokalen Laufwerk und nach der Korrektur auf einem Netzwerkpfad, Firefox, Start direkt per `file://` (Tests liefen meist über einen Server), echter Feedback-Versand.
6. `start.html` prüft feste Funktionsnamen auf Vollständigkeit; Umbenennungen müssen dort nachgeführt werden.
7. Die Listen der Dateiformate in `core.js` und `getfilepath.py` müssen manuell synchron gehalten werden.

---
---

# Session STEFFLIX 2.02 (Arbeit vom 25.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (STEFFLIX 2.02, Updater 3.0.3/3.0.4), erfasst am 2026-11-13. Die Nummern schliessen an die Session oben an (ab DEC-047).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt, z.B. die Umbenennung von updateStefflix.exe in STEFFLIX.exe und der Umzug der Release-Werkzeuge nach `SFLIX Tools\Release\`. Teile der Entscheidungen zum Updater beruhen auf dem Übergabedokument des Nutzers und dem Quellcode in `SFLIX Tools\SflixTools\src` (in dieser Session nur gelesen, nicht geändert).

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 1 |
| Implemented | 12 |
| Proposed | 0 |
| Rejected | 1 |
| Superseded | 3 |

## Wiedergabe und Performance

### DEC-047: Nächstes und vorheriges Bild im Hintergrund vorladen
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Performance
Entscheidung: Sobald das laufende Medium angezeigt wird und das Band darunter steht (Callback von `whenMainMediaReady` in `renderMediaContext`), lädt STEFFLIX das nächste und das vorherige Medium vor, sofern es Bilder sind, die in `data.js` stehen und nicht gerade angezeigt werden. Das nächste kommt zuerst dran, es gibt höchstens zwei Vorladungen. Geladen wird über `new Image()` mit `decoding = "async"` und `fetchPriority = "low"`, nach `load` folgt `decode()`. Nicht mehr benötigte Vorladungen werden über `removeAttribute("src")` abgebrochen, ausser das Bild wird inzwischen angezeigt. Der Pfad hat dieselbe Schreibweise wie `node.src` in `loadDecodedMedia`. Das gilt auch in der normalen Reihenfolge, nicht nur in der Zufallswiedergabe.
Begründung: Wunsch des Nutzers. In der Zufallswiedergabe steht das nächste Medium schon fest, wenn das laufende angezeigt wird (Link im Weiter-Knopf); für den Zurück-Knopf kam das Vorladen auf Wunsch während der Umsetzung hinzu. Der Start nach `whenMainMediaReady` folgt der Leitlinie, dass das Hauptbild Vorrang hat.
Alternativen: Keine anderen besprochen. Videos und Musik werden bewusst nicht vorgeladen.
Konsequenzen: Im Test über den lokalen Testserver war das Bild beim Klick auf „Weiter“ sofort fertig, und die Datei wurde nur einmal angefragt. Unter `file://` und mit grossen Ordnern ist das nicht gemessen. Beim Umschalten der Zufallswiedergabe wird die Vorladung neu berechnet.
Betroffene Komponenten: `player.js` (`preloadNeighbourImages`, `startImagePreload`, `preloadedImages`, `updatePlaylistNavigation` mit `lastNavigation.nextPath`/`prevPath`, `initShuffleButton`), `pages.js` (`renderMediaContext`)
Abhängigkeiten: DEC-018

### DEC-048: Preview-Ordner gehören nicht zu den Medien eines Ordners
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: `collectMediaPaths` überspringt Schlüssel `Preview`.
Begründung: Beim Test fiel auf, dass „Zufällig abspielen“ die Ordner-Vorschaubilder als Fotos in die Playliste aufnahm (in der Testumgebung 23 statt 20 Einträge). Der Nutzer wollte das zuerst ignorieren und liess es dann doch beheben.
Alternativen: –
Konsequenzen: Wirkt auch auf die Anzeige des Knopfs „Zufällig abspielen“ (mindestens zwei Medien) und auf `migrateLegacyTimestamps`. Changelog-Eintrag als BUGFIX.
Betroffene Komponenten: `core.js` (`collectMediaPaths`), indirekt `player.js` (`startShuffleForFolder`), `pages.js`, `storage.js` (`migrateLegacyTimestamps`)
Abhängigkeiten: –

## Vorschaubilder

### DEC-049: Kein Ordner sflix_sys\thumbs, Vorschaubilder kommen aus dem Preview-Ordner
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Architektur
Entscheidung: Vorschaubilder liegen ausschliesslich im Ordner `Preview` neben dem Medium bzw. dem Ordner, und `data.js` zeigt direkt auf diese Dateien. updateData (`getfilepath.py`) legt keine Kopien in `sflix_sys\thumbs` mehr an und löscht einen vorhandenen Ordner `sflix_sys\thumbs` nach dem Schreiben von `data.js`. Das Nachladen des grossen Originals im Musik-Player (`loadOriginalArtwork`) fällt weg.
Begründung: Der Nutzer will den Ordner nicht und hatte ihn gelöscht. Danach fehlten bei Videos und Musik die Vorschaubilder, weil `data.js` nur auf die Kopien zeigte. Bilder fielen auf das Foto selbst zurück.
Alternativen: Kopien in `sflix_sys\thumbs` (Verhalten seit 2.00) – vom Nutzer abgelehnt.
Konsequenzen: Hilfe und Changelog angepasst. Fehlerhafte Rückmeldung des Agenten in der Session: Er hatte `thumbs` zuerst als „ignorieren“ übernommen, statt auf die Abhängigkeit hinzuweisen.
Betroffene Komponenten: `getfilepath.py` (`removeOldThumbnails`, `OLD_THUMB_DIR`), `player.js` (`updateAudioArtwork`), `start.html` (Hilfe)
Abhängigkeiten: DEC-050, DEC-053, DEC-039

### DEC-050: Alte thumbs-Pfade in data.js auf das Original umrechnen
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges (Rückwärtskompatibilität)
Entscheidung: `findPreviewInFolder` rechnet Einträge der Form `./sflix_sys/thumbs/<Pfad>` auf `./<Pfad>` um (`getOriginalPreviewPath`).
Begründung: Vorschaubilder sollten sofort wieder erscheinen, auch mit einer data.js aus updateData 2.00/2.01 und ohne neuen Durchlauf.
Alternativen: Warten, bis updateData neu läuft – hätte die Vorschaubilder bis dahin fehlen lassen.
Konsequenzen: Die alten Kopien hiessen immer `.jpg`. Ein Original mit anderer Endung (z.B. `.png`) wird erst nach einem neuen Einlesen gefunden. Getestet in der Testumgebung mit umbenanntem `thumbs`-Ordner: Alle 59 Videos und Musiktitel fanden ihr Bild.
Betroffene Komponenten: `core.js` (`getOriginalPreviewPath`, `findPreviewInFolder`)
Abhängigkeiten: DEC-049

### DEC-051: Originale ohne Verkleinerung verwenden
Status: Superseded
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Performance
Entscheidung: Erste Umsetzung von DEC-049: updateData verkleinert nichts mehr, Pillow wird nicht mehr importiert, die Kacheln laden die Originale.
Begründung: Kein Ordner `thumbs` mehr.
Alternativen: –
Konsequenzen: Grosse Originale hätten die Kacheln verlangsamt. Ersetzt durch DEC-052 auf Wunsch des Nutzers.
Betroffene Komponenten: `getfilepath.py`
Abhängigkeiten: DEC-049, DEC-052

### DEC-052: Zu grosse Vorschaubilder im Preview-Ordner zuschneiden, Original als <Dateiname>.original
Status: Superseded
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Performance
Entscheidung: Ist ein Vorschaubild im Preview-Ordner breiter als 480 oder höher als 270 Pixel, wird es (unter Berücksichtigung der EXIF-Drehung) auf 480 × 270 zugeschnitten und unter demselben Namen gespeichert. Das Original wird in `<Dateiname>.original` umbenannt. Reihenfolge: zuerst die neue Version als `.tmp` speichern, dann das Original umbenennen, dann die neue Version einsetzen; bei Fehlern wird das Original zurückgeholt. jpg, jpeg, png, webp und bmp bleiben in ihrem Format, animierte GIFs und avif bleiben unverändert. Ohne Pillow wird nichts verkleinert.
Begründung: Vorgabe des Nutzers. Pfade in `data.js` bleiben gleich, weil der Dateiname gleich bleibt.
Alternativen: DEC-051.
Konsequenzen: Ersetzt durch DEC-053, nachdem der Updater 3.0.4 die Ablage geändert hatte.
Betroffene Komponenten: `getfilepath.py` (`shrinkPreview`, `PREVIEW_SIZE`, `PREVIEW_QUALITY`, `PREVIEW_FORMATS`)
Abhängigkeiten: DEC-051, DEC-053

### DEC-053: Originale verkleinerter Vorschaubilder kommen nach Preview\__original
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Architektur
Entscheidung: Wie DEC-052, aber das Original wird mit gleichem Namen nach `Preview\__original\<Dateiname>` verschoben; ein dort vorhandenes Original wird durch das neuere ersetzt. Alte Dateien `<Dateiname>.original` im Preview-Ordner werden dorthin verschoben; bei gleichem Namen entsteht „Name (2).jpg“, damit beide erhalten bleiben. Das Verhalten folgt dem Updater 3.0.4 (`DataTask.cs`), dessen Quellcode in dieser Session nur gelesen wurde.
Begründung: Änderung im Updater 3.0.4 laut Nutzer und Übergabedokument; das Übergangsprogramm updateData.exe sollte sich gleich verhalten. „Ablagen leeren“ im Updater kann `__original` löschen.
Alternativen: DEC-052 (`.original` neben dem Bild).
Konsequenzen: Getestet an Testbildern: verkleinern, umziehen, kleines Bild unverändert, zweiter Lauf ohne Änderung. Hilfe, README und Changelog nennen `Preview\__original`. Laut Übergabedokument verkleinert der Updater WebP-Bilder nicht (System.Drawing), `getfilepath.py` schon.
Betroffene Komponenten: `getfilepath.py` (`shrinkPreview`, `migrateOldOriginal`, `uniquePath`, `ORIGINAL_FOLDER`), `start.html`, `README.md`, `sflix_sys\news.txt`; im Updater (nicht in dieser Session geändert) `DataTask.cs`, `CleanupTask.cs`
Abhängigkeiten: DEC-052, DEC-054

### DEC-054: Ordner mit „__“ im Namen beim Einlesen ignorieren
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: `recCrawler` in `getfilepath.py` überspringt Ordner, deren Name „__“ enthält (`isToolStorage`), wie der Updater (`Core.cs`: `name.Contains("__")`).
Begründung: Ohne die Regel hätte updateData die Originale in `Preview\__original` als zusätzliche Vorschaubilder in `data.js` eingetragen. Laut Übergabedokument fehlte die Regel bis dahin in der Fassung im Release-Ordner.
Alternativen: –
Konsequenzen: Ablagen wie `__konvertiert`, `__teile`, `__gelöscht` und `__original` erscheinen nicht in `data.js` (getestet). „Ablagen leeren“ im Updater erkennt Ablagen dagegen nur, wenn der Name mit „__“ beginnt (`CleanupTask.cs`: `StartsWith("__")`). Erweitert durch DEC-100 auf System- und versteckte Ordner.
Betroffene Komponenten: `getfilepath.py` (`isToolStorage`, `recCrawler`)
Abhängigkeiten: DEC-053

## Updater, Release und Repository

### DEC-055: Neuer Updater ersetzt die Python-Programme, Übergangsdateien bleiben für alte Installationen
Status: Accepted
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Architektur
Entscheidung: Das C#-Programm `updateStefflix.exe` (3.x) ersetzt updateStefflix.py, updater.exe, updateData.exe und setup.exe (Update, Selbst-Update, Daten einlesen, Vorschaubilder, Zusammenführen, Konvertieren, Ablagen leeren, Installation). Im Repository bleiben trotzdem `updater.exe` und `updateData.exe` mit Prüfsumme, `sflix_sys\sflix.js` und `setup.exe`.
Begründung: Vorgabe des Nutzers: Nutzer älterer Versionen brauchen diese Dateien, damit ihr Updater nicht abbricht. Laut Übergabedokument lädt die alte Kette updater.exe und updateData.exe mit Prüfsumme; fehlt eine, bricht das ganze Update ab. Updater vor 2.00 laden `sflix.js` fest herunter. `setup.exe` bleibt für alte Download-Links.
Alternativen: Sofortiges Entfernen – verworfen.
Konsequenzen: Der neue Updater entfernt updater.exe und updateData.exe bei den Nutzern. `data.js` wird nur bei einer Neuinstallation geladen, bei Updates nie (`WEB_FILES`, `Net.WebFiles`).
Betroffene Komponenten: `updater.exe`, `updateData.exe`, `setup.exe`, `sflix_sys\sflix.js`, `sflix_update.py` (`FILES`, `WEB_FILES`), `checksums.json`; im Updater `Net.cs`, `InstallTask.cs`
Abhängigkeiten: DEC-015, DEC-017, DEC-056

### DEC-056: Übergangsprogramme einfrieren, build_release.bat baut keine Programme mehr
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges (Release)
Entscheidung: updater.exe, updateData.exe und setup.exe werden nicht mehr gebaut. `build_release.bat` führt nur noch aus: Prüfung (`check_release.py`), `news.json`, `checksums.json`, Aufräumen. Aus dem Release-Ordner entfernt (in den Scratchpad verschoben, nicht gelöscht): `updater.py`, `setup.py`, `getfilepath.py`, `build_check.py`, `build_hashes.json`. `check_release.py` prüft jetzt auch, ob alle Dateien aus `FILES` und `setup.exe` vorhanden sind. `sflix_update.py` bleibt wegen der Dateiliste für Prüfsummen und Release-Prüfung.
Begründung: Der neue Updater braucht die Skripte nicht. Der Nutzer bestätigte, dass Kopien der Skripte in einem anderen Ordner liegen. `build_check.py` und `build_hashes.json` dienten nur dazu, unveränderte Programme nicht neu zu bauen.
Alternativen: Weiter bauen, solange Skripte im Ordner liegen (bisheriger Ablauf).
Konsequenzen: PyInstaller und Pillow werden für das Release nicht mehr gebraucht, `requests` schon (Import in `sflix_update.py`). Fehler in den Übergangsprogrammen lassen sich nur noch beheben, wenn die Skripte zurückgeholt werden. Getestet: `check_release.py` läuft ohne Fehler durch, und die Prüfsummen stimmen weiterhin (Vergleich nur lesend). Die Optionen `ohne-exe` und `force` entfallen. „Baut keine Programme mehr“ gilt nur für die Python-Programme; release.ps1 baut den C#-Desktop und die App.
Betroffene Komponenten: `build_release.bat`, `check_release.py`, `sflix_update.py`, `create_checksums.py`
Abhängigkeiten: DEC-025, DEC-055, DEC-057

### DEC-057: updateStefflix.py entfernen; build_hashes.json nur für Python-Builds
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges (Release)
Entscheidung: `updateStefflix.py` kam aus dem Release-Ordner (verschoben). Der Eintrag `updateStefflix.exe` in `build_hashes.json` wurde gelöscht.
Begründung: updateStefflix.exe wird seit 3.0 im C#-Projekt gebaut. `build_hashes.json` enthielt nur Fingerabdrücke der Python-Quellen für Programme, die `build_release.bat` mit PyInstaller baut; der Eintrag wurde nicht mehr gelesen. Die Prüfsumme für Updates steht in `checksums.json` und wurde gegen die aktuelle Datei geprüft. Rückfrage des Nutzers („müsste es nicht umgekehrt sein?“) so beantwortet.
Alternativen: –
Konsequenzen: `build_hashes.json` selbst wurde kurz darauf mit DEC-056 ganz entfernt. build_hashes.json wurde später entfernt (DEC-056).
Betroffene Komponenten: `updateStefflix.py`, `build_hashes.json`, `checksums.json`
Abhängigkeiten: DEC-056

### DEC-058: data.js im Release enthält Demoinhalt
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: UI
Entscheidung: `sflix_sys\data.js` im Release-Ordner enthält statt des Platzhalters `{'TEST1': null, 'TEST2': null}` erfundene Demomedien (148 Einträge: Bilder, Filme mit Unterordner, Musik, Serien mit Staffeln). Format wie „Daten einlesen“: eine Zeile, ohne Kommentar, **ohne Preview-Einträge**.
Begründung: Wunsch des Nutzers („ersetze data.js mit deinem Beispiel“). Preview-Einträge fehlen, weil die Bilddateien bei einer Neuinstallation nicht existieren und Kacheln für fehlende Bilder keinen Ersatz haben (kein `onerror` in `cards.js`); ohne Eintrag zeigt STEFFLIX farbige Kacheln.
Alternativen: DEC-059 (eigene Beispieldatei), DEC-060 (leere data.js beim Setup).
Konsequenzen: Neuinstallationen zeigen zuerst den Demoinhalt; ein Klick auf einen Demo-Titel meldet „nicht gefunden“. Bestehende Installationen sind nicht betroffen (Updates laden `data.js` nicht). Darstellung in der Testumgebung geprüft.
Betroffene Komponenten: `sflix_sys\data.js`, `checksums.json` (über `build_release`)
Abhängigkeiten: DEC-055, DEC-059, DEC-060

### DEC-059: Eigene Beispieldatei data_beispiel.js
Status: Superseded
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges
Entscheidung: Eine lesbare, eingerückte `data_beispiel.js` mit erklärendem Kommentar und einigen Preview-Einträgen im Release-Ordner, die nirgends heruntergeladen wird.
Begründung: Erste Aussage des Nutzers: „die data.js soll nie heruntergeladen werden, soll einfach als Beispiel dienen.“ Die zuerst überschriebene Platzhalter-data.js wurde dafür byte-gleich wiederhergestellt.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-058; die Datei kam aus dem Release-Ordner, weil sie doppelt gewesen wäre und ihr Kommentar nicht mehr gestimmt hätte.
Betroffene Komponenten: `data_beispiel.js` (entfernt)
Abhängigkeiten: DEC-058

### DEC-060: Setup soll eine leere data.js anlegen statt sie herunterzuladen
Status: Rejected
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Architektur
Entscheidung: Der Vorschlag des Agenten, dass das Setup `data.js` selbst leer anlegt (wie `renamed.js`), statt sie herunterzuladen, wurde nicht umgesetzt.
Begründung: Antwort des Nutzers: „ja beim setup ist ok.“
Alternativen: –
Konsequenzen: Das Setup lädt `sflix_sys/data.js` weiterhin bei jeder Neuinstallation herunter (`InstallTask.cs`, `INSTALL_FILES` in `sflix_update.py`).
Betroffene Komponenten: `InstallTask.cs` (nicht geändert), `sflix_update.py`
Abhängigkeiten: DEC-058

### DEC-061: README.md für GitHub, Hilfe auf den neuen Updater umgestellt
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: UI
Entscheidung: Neue `README.md` im Release-Ordner, inhaltlich aus der Hilfe übernommen, dazu eine Installationsanleitung in Schritten. Der Downloadlink des Setups zeigt auf `updateStefflix.exe` (raw-Adresse im Repository), nicht auf `setup.exe`. Die Hilfe in `start.html` beschreibt den neuen Updater: Aufgaben „Daten einlesen“, „Vorschaubilder erstellen“, „Videos für SFLIX konvertieren“ (statt des VLC-Links), aufklappbar „Aufgaben des Updaters“ und „STEFFLIX neu installieren“.
Begründung: Auftrag des Nutzers. Laut Übergabedokument installiert der neue Updater selbst, wenn er ausserhalb eines STEFFLIX-Ordners gestartet wird. Die Knopfnamen stammen aus `InstallForm.cs`.
Alternativen: Link auf `setup.exe` – nicht gewählt, die Datei bleibt nur für alte Links.
Konsequenzen: Zwei Angaben in der README sind Annahmen des Agenten und als solche genannt: der Hinweis auf die Windows-Warnung bei unsignierten Programmen und die Voraussetzung Windows 10/11 (.NET Framework 4.x). Die Darstellung auf GitHub wurde nicht geprüft.
Betroffene Komponenten: `README.md`, `start.html` (`#help`, Kommentar zu `renamed.js`)
Abhängigkeiten: DEC-055

### DEC-062: create_serial.py gehört nicht in dieses Repository
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges
Entscheidung: Der Nutzer hat `create_serial.py` (Werkzeug für Lizenzschlüssel) aus dem Release-Ordner verschoben.
Begründung: Aussage des Nutzers: „das brauchts in diesem git nicht“. Keine andere Datei verweist darauf (geprüft).
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: `create_serial.py`
Abhängigkeiten: –

### DEC-063: Version 2.02 und Changelog mit den Funktionen des Updaters
Status: Implemented
Datum: 2026-11-13 (Session: 2026-10-25)
Kategorie: Sonstiges
Entscheidung: `version.js` auf 2.02, neuer Block `Version 2.02` in `news.txt`. Die Funktionen des neuen Updaters stehen in diesem STEFFLIX-Block mit dem Präfix „Updater:“, dazu der vom Nutzer vorgegebene Satz zur deutlich verbesserten Performance.
Begründung: Auftrag des Nutzers.
Alternativen: –
Konsequenzen: `news.json` entsteht erst durch `build_release.bat` (DEC-026); in der Session fehlten die Einträge deshalb zuerst in der Anzeige.
Betroffene Komponenten: `sflix_sys\version.js`, `sflix_sys\news.txt`
Abhängigkeiten: DEC-026

---

## A. Offene Entscheidungen (diese Session)

1. **Wann die Übergangsdateien entfernt werden** (`updater.exe`, `updateData.exe`, `setup.exe`, `sflix_sys\sflix.js`): „wenn keine alten Installationen mehr im Umlauf sind“. Woran man das erkennt, ist nicht festgelegt. Gleichzeitig müssten `sflix_update.py`, `build_release.bat` und `check_release.py` angepasst werden.
2. **`sflix_sys\sflix.png`**: Der aktuelle Code verwendet die Datei nicht; ob ältere Versionen sie noch laden, liess sich nicht prüfen. Empfehlung: behalten (wie A1 der Session oben).
3. **Rückfall für alte thumbs-Pfade** (DEC-050): Ob und wann er entfällt, ist offen.
4. **WebP-Vorschaubilder**: `getfilepath.py` verkleinert sie, der Updater nicht. Ob beide gleich arbeiten sollen, ist nicht entschieden.
5. **Ersatz für fehlende Kachelbilder**: Kacheln zeigen bei einem fehlenden Vorschaubild ein kaputtes Bild; ein Ersatz (z.B. farbige Kachel) wurde festgestellt, aber nicht besprochen.

## B. Änderungsbedarf an bestehenden Entscheidungen

- **DEC-039** (Grösse der Vorschaubilder): Die Konstanten `THUMB_SIZE`/`THUMB_QUALITY` gibt es nicht mehr; Grösse 480 × 270 und Qualität 80 gelten weiter als `PREVIEW_SIZE`/`PREVIEW_QUALITY`, jetzt im Preview-Ordner statt in `sflix_sys\thumbs` (DEC-049, DEC-053).
- **DEC-025** (build_release.bat): Schritt „Programme bauen“ und der Modus `ohne-exe` entfallen (DEC-056).
- **DEC-006 / DEC-017** (Update-Kette der Python-Programme): gilt nur noch für den Umstieg alter Installationen (DEC-055).
- **DEC-015** (Platzhalter sflix.js) und **DEC-022** (sflix.png) wurden bestätigt (DEC-055, A2).
- Innerhalb dieser Session: DEC-051 → DEC-052 → DEC-053, DEC-059 → DEC-058, Eintrag in `build_hashes.json` (DEC-057) → Datei entfernt (DEC-056).

## C. Dokumentationsbedarf

- **architecture.md**: Ablauf beim Medienwechsel (`loadDecodedMedia` → `renderMediaContext` → `whenMainMediaReady` → Bänder, dann `preloadNeighbourImages`); welche Dateien bei Installation bzw. Update geladen werden (`data.js` nur beim Setup).
- **data-model.md**: Aufbau von `data.js` (Ordner als Objekte, Dateien als Pfad-Schlüssel mit `null`, `Preview`-Objekte), Ablagen mit „__“ sind ausgeschlossen, `Preview\__original`, Rückfall für `./sflix_sys/thumbs/…`.
- **Release** (sync-engine.md o.ä.): eingefrorene Übergangsprogramme, verkürzter Ablauf von `build_release.bat`, Prüfsummen gelten auch für die eingefrorenen EXEs.
- **testing.md**: Weboberfläche über den Testserver in `.claude\testenv` testen; nach dem Kopieren neuer Programmdateien hält der Browser alte Skripte im Cache (neu laden mit `fetch(…, {cache: "reload"})`); `requestAnimationFrame` und Lazy Loading pausieren in einem Tab im Hintergrund; keine Wiedergabe von Testtönen; Prüfsummen nur lesend vergleichen; Python-Funktionen per `ast` aus `getfilepath.py` herauslösen und an Testbildern prüfen.

## D. Technische Risiken

1. Kacheln haben keinen Ersatz für fehlende Bilder: Preview-Einträge, deren Datei fehlt, erscheinen als kaputtes Bild (Grund für DEC-058 ohne Preview-Einträge).
2. Der Rückfall für thumbs-Pfade nimmt `.jpg` an; Originale mit anderer Endung fehlen bis zum nächsten Einlesen.
3. Uneinheitliche „__“-Regel im Updater: Einlesen ignoriert Ordner mit „__“ irgendwo im Namen, „Ablagen leeren“ nur Ordner, die mit „__“ beginnen.
4. Werden Übergangsdateien zu früh entfernt, brechen alte Updater ab, weil eine Prüfsumme fehlt.
5. `getfilepath.py` schreibt `data.js` nur, wenn Medien gefunden wurden. Läuft das Übergangsprogramm updateData.exe in einer Installation ohne Medien, bleibt der Demoinhalt stehen. Für den Updater nicht geprüft.
6. Das Vorladen lädt bis zu zwei Originalfotos im Hintergrund, auf Netzlaufwerken mit mehreren MB je Bild; unter `file://` und mit grossen Ordnern nicht gemessen.
7. Dass die eingefrorene updateData.exe die Logik zu `__original` enthält, beruht nur auf den Dateizeiten (EXE 22:50, letzte Änderung an `getfilepath.py` 22:45); geprüft wurde das nicht.
8. Die Release-Skripte brauchen weiterhin das Paket `requests`, weil `sflix_update.py` es beim Import lädt.

---
---

# Session Updater 3.1.0/3.1.1 (Arbeit vom 26.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (STEFFLIX 2.02 unverändert, Updater 3.1.0 → 3.1.1), erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-064).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt, z.B. die Umbenennung in STEFFLIX.exe, die weiteren Tools, die Android-App und spätere Regeln zur Versionsvergabe.

Einschränkung: Der erste Teil der Session (Importer, Vorschaubilder für Bilder, zweispaltiges Layout) ist nur als Zusammenfassung erhalten. DEC-064 bis DEC-068 stützen sich auf diese Zusammenfassung und sind knapper begründet.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 3 |
| Implemented | 16 |
| Proposed | 0 |
| Rejected | 1 |
| Superseded | 2 |

## Importer und Vorschaubilder (erster Teil der Session, aus der Zusammenfassung)

### DEC-064: Importer übernimmt Medien in Kategorie und Unterordner
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Architektur
Entscheidung: Neue Aufgabe „Medien importieren“: Dateien und Ordner werden über Knöpfe (Dateien …, Ordner …, Entfernen) oder per Drag & Drop gewählt und in eine Kategorie und einen optionalen Unterordner von STEFFLIX übernommen. Die Ordnerstruktur kann übernommen werden. Standard ist Kopieren, Verschieben auf Wunsch. Nach einem Verschieben werden leere Quellordner entfernt. Die Kategorie lässt sich mit „Neu …“ anlegen, der Unterordner muss innerhalb der Kategorie liegen. Nach erfolgreichem Import wird die Liste geleert. Import wird nie vorausgewählt.
Begründung: Wunsch des Nutzers; ersetzt das manuelle Kopieren in die STEFFLIX-Ordner. Beides (Knöpfe und Drag & Drop) ausdrücklich vom Nutzer gewünscht.
Alternativen: Nicht überliefert.
Konsequenzen: Ohne Oberfläche ist der Import nicht möglich (`TaskRunner` bricht mit Fehler ab). Ungültige Unterordner (`..`, Laufwerk, Sonderzeichen) werden abgelehnt.
Betroffene Komponenten: `ImportTask.cs` (`ImportOptions`, `ImportItem`, `ImportTask.Plan`, `RemoveEmptySourceFolders`), `MainForm.cs` (`BuildImportPage`, `AddImportSources`, `LoadCategories`, `AddCategory`, `ChooseSubFolder`, `EnableDrop`), `Dialogs.cs` (`InputDialog`)
Abhängigkeiten: DEC-065, DEC-066, DEC-069

### DEC-065: Nicht kompatible Medien werden beim Import zwingend konvertiert
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Video wird zu MP4, Musik zu MP3 (libmp3lame), Bilder zu JPG. Enthält die Auswahl nicht browserkompatible Dateien, wird der Schalter „Konvertieren“ automatisch gesetzt und gesperrt, mit Hinweis (Anzahl und Beispiele). Wird die Liste leer, wird nur das erzwungene Häkchen zurückgenommen, die eigene Wahl bleibt. Beim Kopieren kommt das Original nicht in eine `__`-Ablage; beim Verschieben landen konvertierte Originale in `<Ziel>\__konvertiert`.
Begründung: Vorgabe des Nutzers. Nicht kompatible Dateien könnte STEFFLIX sonst nicht abspielen. Gespeichert wird nur die eigene Wahl, nicht das erzwungene Häkchen.
Alternativen: Nicht überliefert.
Konsequenzen: Der Import nutzt den Konverter (`ConvertTask.ForImport`, `ConvertInto`). Die Listen der Endungen stehen in `MediaTypes`.
Betroffene Komponenten: `ImportTask.cs` (`MediaTypes`, `ConvertSingle`, `FinishConverted`), `ConvertTask.cs` (`IsBrowserReady`, `ForImport`, `ConvertInto`, `TrackFileNames`), `MainForm.cs` (`UpdateImportHint`, `importConvertWasOff`)
Abhängigkeiten: DEC-064

### DEC-066: Vorhandene Zieldateien: Rückfrage ersetzen oder überspringen, merkbar
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges (Konfliktbehandlung)
Entscheidung: Gibt es eine Zieldatei schon, fragt ein Dialog „Ersetzen“ oder „Überspringen“, mit „Für alle weiteren Dateien merken“. Esc bedeutet Überspringen, es wird nichts überschrieben.
Begründung: Vorgabe des Nutzers (Rückfrage mit merkbarer Antwort).
Alternativen: Nicht überliefert.
Konsequenzen: Die Rückfrage läuft aus dem Arbeits-Thread über `Invoke` im UI-Thread (`TaskContext.AskConflict`). Ergänzt um die Vergleichstabelle (DEC-156).
Betroffene Komponenten: `Dialogs.cs` (`ConflictDialog`), `ImportTask.cs` (`ConflictAnswer`, `AskReplace`), `Core.cs` (`TaskContext.AskConflict`), `MainForm.cs` (`StartTasks`)
Abhängigkeiten: DEC-064

### DEC-067: Vorschaubilder wahlweise für Videos und/oder Bilder
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges
Entscheidung: „Vorschaubilder erstellen“ hat die Optionen „für Videos (Bild aus der Mitte)“ und „für Bilder“ (Bilder werden ohne Suchen zugeschnitten). Die Prüfung auf verwaiste Vorschaubilder vergleicht mit allen Medienendungen. Mindestens eine Option muss gewählt sein.
Begründung: Wunsch des Nutzers.
Alternativen: Nicht überliefert.
Konsequenzen: Ohne Oberfläche gelten die gespeicherten Einstellungen `preview.videos` und `preview.images`.
Betroffene Komponenten: `PreviewTask.cs` (`Run`, `VideoExtensions`, `ImageExtensions`, `MediaExtensions`), `MainForm.cs`, `Program.cs`
Abhängigkeiten: DEC-070

### DEC-068: Zweispaltiges Fenster mit Kernaufgaben links
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Frühere Fassung: links die Kernaufgaben „Update“ und „Daten einlesen“, rechts alle anderen Aufgaben und Einstellungen als eingeklappte Karten mit Pfeil; Import hakte „Vorschaubilder“ und „Daten einlesen“ automatisch an.
Begründung: Vorgabe des Nutzers im ersten Teil der Session.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-069 (Registerkarten) und DEC-070 (Start je Karte). Ersetzt durch DEC-070.
Betroffene Komponenten: `MainForm.cs` (`TaskCard`, `CollapsibleCard`, entfernt)
Abhängigkeiten: DEC-069, DEC-070

## Aufbau des Fensters

### DEC-069: Registerkarten „Updates“, „Importer“, „Tools“, nur eine sichtbar
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Oben im Fenster gibt es Registerkarten im Stil von Aktenreitern mit gezeichneten Symbolen (Pfeil nach oben, Pfeil in eine Ablage, Schraubenschlüssel). Sichtbar ist nur der Bereich der gewählten Karte. Der Untertitel im Kopfbild wechselt zu UPDATES, IMPORTER bzw. TOOLS. Updates enthält die Kernkomponenten, Importer den Import, Tools alle übrigen Aufgaben und die Einstellungen (Medienordner). Der Importer ist in die Bereiche „Was importieren?“, „Ziel in STEFFLIX“ und „Optionen“ gegliedert, die Knöpfe stehen neben der Liste. Hineingezogene Dateien wechseln automatisch zum Importer.
Begründung: Vorschlag des Nutzers; der Importer brauchte im alten Layout zu viel Platz, und das Aufklappen über den Pfeil war mühsam.
Alternativen: Zweispaltiges Layout (DEC-068).
Konsequenzen: Die Fensterhöhe passt sich beim Wechsel an. Dabei auftretende Fehler wurden behoben: Eine stehengebliebene Scrollleiste wird nach jeder Anpassung neu berechnet, und ein Fenster, das unten über den Bildschirm ragt, wird nach oben geschoben.
Betroffene Komponenten: `Controls.cs` (`TabStrip`, `LogToggle.FitWindow`), `MainForm.cs` (`ShowPage`, `PageSubtitles`, `BuildImportPage`, `SectionCard`), `Branding.cs` (Untertitel)
Abhängigkeiten: DEC-068, DEC-070, DEC-073

### DEC-070: „Start“ führt nur die sichtbare Karte aus, danach automatisch „Daten einlesen“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Architektur
Entscheidung: Updates: die gewählten Aufgaben „Update“/„Daten einlesen“. Importer: Import, auf Wunsch fehlende Vorschaubilder (Schalter, gespeichert als `import.preview`), danach immer „Daten einlesen“. Tools: die gewählten Tools, danach immer „Daten einlesen“. Der Import hat kein eigenes Häkchen mehr. Die Ausführungsreihenfolge bleibt `TaskIds`. Nach einem Selbst-Update laufen die übrigen Aufgaben aus der Kommandozeile (`preset`) unabhängig von der Karte. Die Vorschaubilder nach einem Import nutzen die Einstellung aus Tools, mindestens für Videos. Der Start-Knopf beschreibt, was passiert („Importieren“, „2 Tools starten“, „Aktualisieren“).
Begründung: Vorgabe des Nutzers für Tools; für den Importer vom Agenten übernommen, weil Import bisher schon „Daten einlesen“ anhakte.
Alternativen: Aufgaben aller Karten gemeinsam ausführen – nicht gewählt.
Konsequenzen: Der Medienordner gilt nur für Tools; fehlt er, öffnet sich Tools › Einstellungen.
Betroffene Komponenten: `MainForm.cs` (`StartTasks`, `ToolIds`, `UpdateStartText`, `lastRunImported`), `TaskRunner`
Abhängigkeiten: DEC-069, DEC-073; DEC-068

### DEC-071: Klick auf die Karte klappt Tools auf, Häkchen wählt aus
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Zwischenstand: Ein Klick irgendwo auf eine Tool-Karte (ausser auf Häkchen und Eingaben) klappt sie auf oder zu; bei immer offenen Karten schaltete ein Klick die Aufgabe um.
Begründung: Wunsch des Nutzers („das Klicken auf die Pfeile ist mühsam“).
Alternativen: –
Konsequenzen: Ersetzt durch DEC-073 (Kacheln mit Options-Reitern).
Betroffene Komponenten: `MainForm.cs` (`CollapsibleCard`, `ToggleExpanded`, entfernt)
Abhängigkeiten: DEC-073

### DEC-072: In allen Texten „STEFFLIX“ statt „SFLIX“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Texte im Updater, in der README des Updaters und im Changelog nennen „STEFFLIX“ (z.B. „Videos für STEFFLIX konvertieren“). Datei- und Ordnernamen, GitHub-Pfade (`js/SFLIX`) und Umgebungsvariablen (`SFLIX_DIR`, `SFLIX_UPDATE_URL`) bleiben unverändert.
Begründung: Wunsch des Nutzers. Technische Namen zu ändern hätte Pfade, Downloads und bestehende Installationen gebrochen.
Alternativen: –
Konsequenzen: Im Code und in Dateinamen kommt „SFLIX“ weiterhin vor.
Betroffene Komponenten: `MainForm.cs` (`TaskTitles`, `TaskRunner.Title`), `ConvertTask.cs`, `ImportTask.cs` (Kommentare), `SFLIX Tools\SflixTools\README.md`, `sflix_sys\news.txt`
Abhängigkeiten: –

### DEC-073: Kacheln mit einem Options-Reiter je gewählter Kachel (Variante B)
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Updates und Tools zeigen Kacheln (Symbol, Titel, kurze Zeile). Ein Klick wählt eine Kachel aus (roter Rand, Haken oben rechts). Unter den Kacheln gibt es für jede gewählte Kachel einen Reiter mit Beschreibung und Optionen; eine neu gewählte Kachel bringt ihren Reiter nach vorne. In Tools ist der Reiter „Einstellungen“ immer dabei. Der Optionsbereich ist so hoch wie sein höchster Reiter. Beim Start ist der Reiter des ersten gewählten Tools vorne.
Begründung: Der Nutzer wählte Variante B, weil sie ihm optisch am besten gefiel. Seine Skepsis, alle Optionen untereinander aufzulisten, führte zu den Reitern („nicht zu unübersichtlich oder zu überfüllt“). Die feste Höhe verhindert, dass das Fenster beim Wechsel springt.
Alternativen: Siehe DEC-075; Optionen aller gewählten Kacheln untereinander (vom Nutzer abgelehnt).
Konsequenzen: `checks` enthält seither `TileButton` statt Checkboxen. Gezeichnete Symbole für Daten, Bild, Zusammenführen, Konvertieren, Papierkorb, Zahnrad.
Betroffene Komponenten: `Controls.cs` (`TileButton`, `Glyph`, `Glyphs`, `TabStrip` mit `SetTabs` und Stilfeldern), `MainForm.cs` (`TileSection`, `TileSpec`, `RefreshOptionTabs`, `ShowOptionPanel`, `ShowOption`, `EqualizeOptionHosts`)
Abhängigkeiten: DEC-071, DEC-074, DEC-075

### DEC-074: Kleine Optionen als Schiebeschalter
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Optionen innerhalb der Reiter und im Importer (z.B. „ffmpeg ebenfalls aktualisieren“, „Ordnerstruktur übernehmen“) sind Schiebeschalter statt Checkboxen.
Begründung: Der Nutzer fand die Checkboxen altbacken und die Schalter besser als Checkboxen.
Alternativen: Checkboxen (bisher).
Konsequenzen: `DarkCheckBox` kann beides (`Switch`); Dialoge wie der Konfliktdialog behalten die Checkbox.
Betroffene Komponenten: `Controls.cs` (`DarkCheckBox.Switch`), `MainForm.cs` (`SmallCheck`)
Abhängigkeiten: DEC-073

### DEC-075: Schalter, Plus-zu-Haken-Knopf oder Knopf je Tool als Auswahl der Aufgaben
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Für die Auswahl der Aufgaben wurden nicht umgesetzt: Schiebeschalter auf jeder Karte (erster Vorschlag), Variante A (runder Knopf, Plus wird zu rotem Haken; vom Agenten empfohlen) und Variante C (eigener „Starten“-Knopf je Tool).
Begründung: Schalter gefielen dem Nutzer „noch nicht so ganz“; aus den drei Alternativen wählte er B (DEC-073). C hätte zudem mehrere Tools in einem Durchgang verhindert.
Alternativen: DEC-073.
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-073

## Updates und Versionen

### DEC-076: Hinweis beim Start, wenn ein Update verfügbar ist, mit Versionsnummern
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Architektur
Entscheidung: Beim Start prüft der Updater im Hintergrund STEFFLIX (Web-Dateien gegen `checksums.json`), sich selbst und ein bereits installiertes ffmpeg. Ist etwas neu, erscheint ein Fenster mit installierter und verfügbarer Version, auch für STEFFLIX (neue Nummer aus `version.js` auf GitHub; gleiche Nummer mit geänderten Dateien → „(neue Dateien)“). „Jetzt aktualisieren“ führt die Aufgabe „Update“ aus (inkl. ffmpeg, wenn dort neuer); nach „Später“ bleibt ein Balken mit demselben Knopf. Ohne Verbindung gibt es nur eine Zeile im Protokoll. Ein noch nicht installiertes ffmpeg löst keinen Hinweis aus.
Begründung: Wunsch des Nutzers, beim Start direkt auf Updates hingewiesen zu werden und sie ausführen zu können; Versionsnummer für STEFFLIX ausdrücklich gewünscht. Bisher prüfte das Programm nur die eigene Version und installierte bei „Jetzt aktualisieren“ nur die EXE.
Alternativen: Bisheriger Balken nur für den Updater (`InstallSelfUpdate`, entfernt).
Konsequenzen: Der Hinweis erscheint bei jedem Start, solange das Update fehlt. Das hatte der Agent empfohlen, der Nutzer hat diese Frage nicht ausdrücklich entschieden (siehe A1). Für die STEFFLIX-Version wird `version.js` mit Prüfsumme geladen.
Betroffene Komponenten: `UpdateTask.cs` (`UpdateInfo`, `UpdateCheck`, `LocalWebVersion`, `ParseWebVersion`), `Ffmpeg.cs` (`InstalledVersionName`), `Dialogs.cs` (`UpdateDialog`, `GlyphBox`), `MainForm.cs` (`CheckForUpdateInBackground`, `RunUpdateNow`, `forceFfmpeg`)
Abhängigkeiten: DEC-077, DEC-078, DEC-079

### DEC-077: Versionsübersicht rechts auf der Seite „Updates“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: Eine Karte „Versionen“ zeigt für STEFFLIX, Updater und ffmpeg die installierte und die verfügbare Version (Neueres rot). Installierte Werte stehen sofort fest, verfügbare kommen aus der Prüfung; Status mit Uhrzeit, „Neu prüfen“. Nach einem Update wird automatisch neu geprüft, und der Balken verschwindet, wenn alles aktuell ist.
Begründung: Wunsch des Nutzers.
Alternativen: –
Konsequenzen: Die Prüfung liefert dafür auch die verfügbaren Versionen, wenn nichts neu ist (`SelfAvailable`, `FfmpegRemote` immer).
Betroffene Komponenten: `MainForm.cs` (`BuildVersionsCard`, `ShowInstalledVersions`, `ShowAvailableVersions`, `SetAvailable`), `UpdateTask.cs` (`UpdateInfo`)
Abhängigkeiten: DEC-076

### DEC-078: Patchnotes aus news.json, nur die Versionen neuer als die installierte
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: UI
Entscheidung: „Patchnotes“ im Update-Fenster und in der Versionsübersicht lädt `sflix_sys/news.json` von GitHub und zeigt alle Versionen, die neuer als die installierte STEFFLIX-Version sind; gibt es keine, die neueste. Darstellung: Versionsüberschrift, farbiger Typ, eingerückter Text, dunkle Scrollleiste.
Begründung: Wunsch des Nutzers. Updater-Einträge stehen im Block der STEFFLIX-Version, deshalb zählt die STEFFLIX-Version.
Alternativen: –
Konsequenzen: `news.json` steht nicht in `checksums.json` und wird ohne Prüfsumme geladen (nur angezeigt, HTML-Reste werden entfernt). Die RichTextBox muss erst in `OnLoad` befüllt werden, weil sie beim Einsetzen ins Fenster die Schrift erbt und alle Formatierungen verliert.
Betroffene Komponenten: `UpdateTask.cs` (`PatchVersion`, `PatchNotes.Load`, `PatchNotes.Select`), `Dialogs.cs` (`PatchNotesDialog`), `MainForm.cs` (`ShowPatchNotes`)
Abhängigkeiten: DEC-076, DEC-077

### DEC-079: Neustart nach einem Selbst-Update
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Architektur
Entscheidung: Hat sich der Updater beim Update selbst erneuert, beendet er sich, startet die neue Version (`--updated`, mit den restlichen Aufgaben) und macht weiter. Das Verhalten bestand schon; in dieser Session wurde es auf Nachfrage des Nutzers geprüft und nicht geändert.
Begründung: Wunsch des Nutzers („gleich neu starten“).
Alternativen: –
Konsequenzen: Getestet mit einer Testversion 3.1.1 auf einem lokalen Testserver: alte Instanz beendet, 3.1.1 lief mit „Alles aktuell“.
Betroffene Komponenten: `UpdateTask.cs` (`SelfUpdate.Restart`, `UpdateTask.Run`), `MainForm.cs` (`Finished`)
Abhängigkeiten: DEC-076

### DEC-080: Sicherungsdateien *.old automatisch entfernen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges
Entscheidung: Alle `*.old` im STEFFLIX-Ordner, in `sflix_sys` und `sflix_sys\js` werden beim Start und nach jedem erfolgreichen Update gelöscht. Medienordner bleiben unberührt; gesperrte Dateien werden beim nächsten Start erneut versucht. Nur echte Endung `.old`.
Begründung: Auftrag des Nutzers (Reste der alten Updater). Der neue Updater legt beim Ersetzen von Web-Dateien selbst `.old`-Sicherungen an und entfernte bisher nur die eigene `updateStefflix.exe.old`; `sflix_sys\js` wurde deshalb über den Auftrag hinaus einbezogen.
Alternativen: –
Konsequenzen: Nur kompiliert, das Löschen selbst wurde nicht getestet (Build durch den Nutzer).
Betroffene Komponenten: `Net.cs` (`RemoveOldBackups`), `UpdateTask.cs` (`SelfUpdate.CleanupOld`, `UpdateTask.Run`)
Abhängigkeiten: –

### DEC-081: Version 3.1.0 bleibt über mehrere Builds; 3.1.1 setzt build_release
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges (Release)
Entscheidung: Die Änderungen dieser Session wurden mehrfach als 3.1.0 gebaut („nicht höher“ in `build_release` bestätigt), solange nicht gepusht. Für die letzte Änderung legte der Nutzer 3.1.1 fest. Der Agent setzt die Nummer nicht im Quellcode, weil `release.ps1` höhere von Quellcode und Release + 1 vorschlägt (= 3.1.1); eine vorab gesetzte 3.1.1 würde zu 3.1.2 führen.
Begründung: Vorgabe des Nutzers; Ablauf von `release.ps1`.
Alternativen: Version vorab in `Program.cs` setzen – bewusst nicht gemacht.
Konsequenzen: Das Selbst-Update installiert nur höhere Versionen; mehrere 3.1.0-Builds sind deshalb nur vor dem Push unproblematisch.
Betroffene Komponenten: `src\Program.cs`, `SFLIX Tools\Release\release.ps1`, `release_state.json`
Abhängigkeiten: DEC-082

### DEC-082: Release baut der Nutzer; der Agent prüft nur, ob der Code kompiliert, als DLL
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sicherheit
Entscheidung: Build und Release führt der Nutzer selbst aus („das kannst du dir sparen“). Der Agent prüft nur die Kompilierbarkeit. Nachdem Bitdefender eine frisch kompilierte Prüf-EXE im Temp-Ordner als „Gen:Variant.Genie“ blockierte, werden solche Prüfungen als Bibliothek (`/target:library`) kompiliert.
Begründung: Vorgabe des Nutzers; der Fehlalarm beunruhigte ihn.
Alternativen: Prüf-EXE im Temp-Ordner – verworfen.
Konsequenzen: Als Gedächtnis-Eintrag gespeichert. Ob die fertige `updateStefflix.exe` bei Nutzern gemeldet wird, ist offen (D1). Gilt auch für die APK; Test-Builds erstellen seit DEC-336 Prüfung, Release und Entwicklung, Release-Builds nur die Rolle Release oder der Benutzer.
Betroffene Komponenten: Testablauf, `build.ps1`, `build_release.bat`
Abhängigkeiten: DEC-012, DEC-026

### DEC-083: GUI-Tests ohne die Maus des Nutzers
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges (Tests)
Entscheidung: Nachdem der Nutzer die Mausfreigabe entzogen hatte, laufen GUI-Tests über Fenster-Nachrichten an das Steuerelement (`BM_CLICK`, `WM_LBUTTONDOWN`/`UP` über `RealChildWindowFromPoint`), Screenshots über `PrintWindow`. Echte Mausklicks nur mit Freigabe.
Begründung: Vorgabe des Nutzers.
Alternativen: Echte Mausbewegungen (`mouse_event`) – nur während der Freigabe genutzt.
Konsequenzen: Echte Maus- und Tastaturbedienung wird dadurch nicht geprüft.
Betroffene Komponenten: Testwerkzeug im Scratchpad (`ui.exe`)
Abhängigkeiten: –

## Dokumentation und Aufräumen

### DEC-084: Projektübersicht für Agenten in .claude\CLAUDE.md, stets aktuell halten
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges (Dokumentation)
Entscheidung: `.claude\CLAUDE.md` beschreibt jede Datei im Release-Ordner nach Ordner (inkl. `SFLIX Tools` als Baum), die Regeln des Nutzers, den Aufbau von MainForm sowie Bauen und Testen. Agenten müssen die Datei bei jeder Änderung an Dateien, Regeln, Abläufen oder Versionen im selben Schritt anpassen. Ausdrücklich aufgenommen: Jede Änderung im obersten Block von `news.txt` nachtragen, `news.json` nie von Hand ändern.
Begründung: Auftrag des Nutzers, damit der nächste Agent sofort Bescheid weiss. Der Ort `.claude\` wurde gewählt, weil Claude Code die Datei dort automatisch lädt und der Ordner nicht zum Release gehört.
Alternativen: Artifact-Seite – vom Agenten nicht gewählt, weil ein Agent sie nicht automatisch liest.
Konsequenzen: Die Beschreibungen der JS-Dateien stammen aus deren Dateiköpfen; die Medien der Testumgebung sind nach Muster zusammengefasst.
Betroffene Komponenten: `.claude\CLAUDE.md`, `sflix_sys\news.txt`
Abhängigkeiten: DEC-026

### DEC-085: sflix_sys\sflix.png gelöscht
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-26)
Kategorie: Sonstiges
Entscheidung: `sflix_sys\sflix.png` wurde im Release-Ordner und in der Testumgebung gelöscht.
Begründung: Entscheidung des Nutzers („kann weg“). Die Datei wird im Code nicht verwendet und steht in keiner Release-Liste (`FILES`), `check_release` vermisst sie also nicht.
Alternativen: Behalten wegen alter Updater (Empfehlung aus DEC-022/DEC-040).
Konsequenzen: Auf GitHub verschwindet die Datei erst mit dem Push der Löschung. Ob alte Updater (vor 2.00) sie fest herunterladen und dann eine Fehlerseite schreiben, wurde in dieser Session nicht geprüft (Risiko D3).
Betroffene Komponenten: `sflix_sys\sflix.png`, `.claude\testenv\sflix_sys\sflix.png`
Abhängigkeiten: DEC-022, DEC-040

---

## A. Offene Entscheidungen (diese Session)

1. **„Später“ für eine bestimmte Version merken?** Der Agent fragte und empfahl, den Hinweis bei jedem Start zu zeigen. Umgesetzt ist „jedes Mal“, eine ausdrückliche Antwort des Nutzers gibt es nicht.
2. **Fehlalarm von Bitdefender**: Ob die fertige `updateStefflix.exe` gemeldet wird und ob ein Fehlalarm bei Bitdefender eingereicht wird, ist offen.
3. **„SFLIX Tools“ als Import-Kategorie**: Im Entwicklungsordner listet der Importer alle Ordner der Wurzel; bei Nutzern gibt es den Ordner nicht. Nicht behoben, nur dokumentiert.

## B. Änderungsbedarf an bestehenden Entscheidungen

- **DEC-022 / DEC-040 / A1 und A2 der früheren Sessions** (sflix.png behalten): durch DEC-085 überholt, die Datei ist gelöscht.
- **DEC-025 / DEC-056** (build_release): Zu dieser Zeit baute `release.ps1` den C#-Updater wieder, zählte die Version hoch und listete die zu pushenden Dateien (inkl. `news.txt`/`news.json`, wenn sich `news.json` geändert hat). Die Beschreibung in DEC-056 („baut keine Programme mehr“) gilt nur für die Python-Programme.
- **DEC-012** (Agent baut keine .exe): In dieser Session baute der Agent Test-Builds mit `build.ps1` und führte `build_release` mehrfach aus, bis der Nutzer das Release übernahm (DEC-082).
- Innerhalb dieser Session: DEC-068 → DEC-069/DEC-070, DEC-071 → DEC-073.

## C. Dokumentationsbedarf

- **architecture.md**: Aufbau von MainForm (Registerkarten, `TileSection` mit Options-Reitern, Start je Karte, automatisches „Daten einlesen“); Ablauf der Update-Prüfung beim Start (`UpdateCheck` → `UpdateDialog`/Balken → `RunUpdateNow` → Selbst-Update → `--updated`).
- **data-model.md**: Einstellungen in `updater_settings.json` (`task.*`, `import.*` inkl. `import.preview`, `preview.videos`/`preview.images`, `media.folder`); Aufbau von `news.json` (`versions[].version`, `items[].type`/`text`).
- **testing.md**: Lokaler Update-Server mit eigener `checksums.json` (Testversion mit höherer FileVersion für das Selbst-Update); GUI-Tests über Fenster-Nachrichten und `PrintWindow`; Kompilierprüfung als DLL; RichTextBox erst in `OnLoad` befüllen; WinForms-Scrollleisten nach Grössenänderung neu berechnen.

## D. Technische Risiken

1. Frisch gebaute, unsignierte EXE mit Selbst-Update können von Virenscannern heuristisch gemeldet werden.
2. `news.json` wird für die Patchnotes ohne Prüfsumme geladen; manipulierte Inhalte erscheinen nur als Text, aber ungeprüft.
3. Das Löschen von `sflix.png` könnte alte Updater (vor 2.00) mit fester Dateiliste stören; nicht geprüft.
4. `RemoveOldBackups` löscht jede `*.old` in drei Ordnern, auch Dateien, die nicht vom Updater stammen.
5. Der Update-Hinweis bei jedem Start kann Nutzer stören, die bewusst nicht aktualisieren wollen (A1).
6. Mehrere Builds mit derselben Versionsnummer: Wird ein solcher Stand gepusht und danach noch einmal geändert, ohne die Nummer zu erhöhen, übernehmen bestehende Installationen das Update nicht.

---

# Session STEFFLIX 2.03 (Arbeit vom 27.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (STEFFLIX 2.03, Updater 3.1.1 → 3.1.2 im Quellcode), erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-086).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt (z.B. die heutige Gestaltung der Ordnerseiten mit Sprungleiste und automatischer Zeilenlänge, siehe B).

Einschränkung: Die Session wurde zweimal zusammengefasst. Die Entscheidungen stützen sich auf beide Zusammenfassungen und auf Auszüge der Antworten aus dem Protokoll der Session. Nichts wurde in dieser Session gebaut oder veröffentlicht: Änderungen am Updater wirken erst nach `build_release.bat` durch den Nutzer.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 2 |
| Implemented | 20 |
| Proposed | 1 |
| Rejected | 2 |
| Superseded | 2 |

## Gespeicherte Daten und Installationen

### DEC-086: Sortierung der Startseite bleibt erhalten, auch für gerade fehlende Bänder
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Beim Speichern der Reihenfolge (`saveRowOrder`) werden die gespeicherten Schlüssel von Bändern, die gerade nicht angezeigt werden, hinter ihrem bisherigen gespeicherten Nachbarn wieder eingefügt, statt verloren zu gehen.
Begründung: Der Nutzer musste die Sortierung immer wieder neu einstellen und verlangte, dass die Konfiguration dauerhaft bleibt.
Alternativen: Keine überliefert.
Konsequenzen: Die gespeicherte Reihenfolge kann Schlüssel enthalten, die zurzeit kein Band haben.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`saveRowOrder`)
Abhängigkeiten: DEC-089, DEC-090

### DEC-087: „Weiterschauen“-Einträge nicht löschen, wenn ein Medium nicht gefunden wird
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: `removeFromLastPlayed` und ihr Aufruf beim Nichtfinden eines Mediums wurden entfernt.
Begründung: Einträge unter „Weiterschauen“ verschwanden; ein vorübergehend fehlendes Medium (z.B. anderer Stand von `data.js`) soll keine Daten löschen.
Alternativen: Keine überliefert.
Konsequenzen: Verwaiste Einträge bleiben stehen; sie räumt die Fehlerbehebung auf.
Betroffene Komponenten: `sflix_sys\js\player.js`
Abhängigkeiten: DEC-086

### DEC-088: Dauerhaften Speicher beim Browser anfordern
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: STEFFLIX ruft `navigator.storage.persist()` auf (`requestPersistentStorage`).
Begründung: Die Konfiguration soll dauerhaft bleiben; der Browser soll die Daten nicht bei Speicherknappheit räumen.
Alternativen: –
Konsequenzen: Schützt nicht vor dem Löschen beim Schliessen des Browsers (siehe DEC-096).
Betroffene Komponenten: `sflix_sys\js\storage.js` (`requestPersistentStorage`)
Abhängigkeiten: DEC-096

### DEC-089: „Weiterschauen“ erscheint unter „Startseite anpassen“ auch ohne Einträge
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: `showContinueWatching` legt das Band immer an. Ohne Einträge hat es die Klasse `emptyRow` und den Hinweis „zurzeit leer“; ausserhalb des Bearbeitungsmodus ist es ausgeblendet.
Begründung: Ausdrücklicher Wunsch des Nutzers; so lässt sich das Band auch leer einsortieren.
Alternativen: –
Konsequenzen: Die Sortierung kennt das Band immer.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`showContinueWatching`), `sflix_sys\sflix.css` (`.emptyRow`, `.emptyRowHint`)
Abhängigkeiten: DEC-086

### DEC-090: Eigene zufällige Kennung und eigener Speicher je Installation
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: Der Updater schreibt `sflix_sys\install.js` mit `installId` (32 Hex-Zeichen) und `installFolder`, beim Start und im Setup. Ein kopierter Ordner (der alte hat noch dieselbe Kennung) bekommt eine neue Kennung, ein verschobener oder umbenannter behält sie. STEFFLIX nutzt damit die IndexedDB `STEFFLIX-<installId>`, ohne gültige Datei weiterhin die gemeinsame `STEFFLIX`. Beim ersten Start mit Kennung werden die gemeinsamen Daten einmalig übernommen (Markierung `sharedDataImported`, gilt als Migrationsmarkierung).
Begründung: Edge und Chrome teilen unter `file://` einen Speicher für alle Ordner; mehrere Installationen auf einem PC beeinflussten sich (Sortierung, Favoriten, Weiterschauen). Der Nutzer verlangte eine zufällige Kennung je Installation.
Alternativen: Trennung nach Ordnerpfad (DEC-091, verworfen).
Konsequenzen: Wirksam erst mit neuem Updater. `install.js` darf nie gepusht oder in andere Installationen kopiert werden; die Testumgebung hat eine eigene und `Programmdateien_aktualisieren.bat` überschreibt sie nicht. „Alle Daten löschen“ holt den alten gemeinsamen Speicher nicht zurück. Auf das Fenster von STEFFLIX.exe übertragen durch DEC-281.
Betroffene Komponenten: `Core.cs` (`InstallId`: `Read`, `Ensure`, `FilePath`), `Program.cs`, `InstallTask.cs`, `start.html`, `sflix_sys\js\storage.js` (`getDatabaseName`, `importSharedDatabase`, `modifyLocalData`, `isMigrationFlag`), `.claude\testenv\Programmdateien_aktualisieren.bat`
Abhängigkeiten: DEC-091, DEC-099, DEC-112

### DEC-091: Speicher nach Ordnerpfad trennen
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: Den Speicher anhand des Ordnerpfads zu trennen, wurde nicht umgesetzt.
Begründung: Nach jedem Verschieben oder mit anderem Laufwerksbuchstaben (externe Festplatte) wären alle Daten weg. Vorteil wäre gewesen, dass es ohne Änderung am Updater geht.
Alternativen: Kennung in einer Datei im Ordner (DEC-090).
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-090

## Daten einlesen (Updater)

### DEC-092: Teil-Einlesen einzelner Ordner, Schalter „Alle Ordner einlesen“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: `DataTask.Run(ctx, folders)` liest nur die gewählten Ordner und ersetzt deren Zweige in der bestehenden `data.js` (inkl. Ordnerbild im `Preview`-Ordner eine Ebene darüber, Namenskorrektur, Vorschaubilder, Umbenennungen). Neue Einträge werden in Ordnerreihenfolge einsortiert (`OrderLikeFolder`), sodass das Ergebnis byte-gleich wie ein vollständiges Einlesen ist. Fehlt `data.js`, ist sie beschädigt oder ist die Wurzel gewählt, wird alles eingelesen. In „Daten einlesen“ gibt es den Schalter „Alle Ordner einlesen“ (Standard an), ausgeschaltet eine gespeicherte Ordnerliste (`data.all`, `data.folders`, relativ). Importer lesen danach nur ihren Zielordner, Tools nur ihren Medienordner (wenn er in STEFFLIX liegt). Ohne Oberfläche: `--data-folder` (mehrfach).
Begründung: Wunsch des Nutzers; ein Einlesen des Zielordners erfasst alle Änderungen des Imports sicher und dauert nur einen Bruchteil.
Alternativen: Einträge des Imports direkt in `data.js` einspeisen (DEC-093, verworfen).
Konsequenzen: Ein Teil-Einlesen bemerkt keine Änderungen ausserhalb der gewählten Ordner (Hinweis unter der Liste). Einige Seiten zeigen Einträge in der Reihenfolge von `data.js`, darum ist die gleiche Reihenfolge nötig.
Betroffene Komponenten: `DataTask.cs` (`Run`, `ExecutePartial`, `UpdateBranch`, `RefreshPreview`, `OrderLikeFolder`, `LoadExistingData`, `SaveData`), `Util.cs` (`JsonObject.Remove`, `SortKeys`), `MainForm.cs` (`optDataAll`, `BuildDataFoldersRow`, `TaskRunner.DataFolders`), `Program.cs` (`--data-folder`)
Abhängigkeiten: DEC-093, DEC-099, DEC-100

### DEC-093: Importierte Dateien direkt in data.js eintragen
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: Der vom Nutzer angefragte Ansatz, neue Einträge beim Import einzeln in `data.js` einzuspeisen, wurde zugunsten des Teil-Einlesens nicht umgesetzt (vom Nutzer mit „wie vorgeschlagen“ bestätigt).
Begründung: Beim Import ändern sich Dateinamen (Korrektur), Endungen (Konvertieren), Originale wandern nach `__konvertiert`, Vorschaubilder kommen dazu. Diese Logik nachzubauen hätte Abweichungen zu „Daten einlesen“ erzeugen können.
Alternativen: DEC-092.
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-092

### DEC-099: An einer Laufwerkswurzel volle Pfade für Dateioperationen verwenden
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Pfade werden nur noch für Vergleiche am Ende gekürzt (`TrimEnd('\\')`); für Dateioperationen bleibt der volle Pfad (`D:\`). Betroffen: `UpdateBranch`, `InstallId.Ensure`, Quellordner des Importers.
Begründung: Liegt STEFFLIX an einer Laufwerkswurzel, wurde aus `D:\` durch Kürzen `D:` und mit `Path.Combine` ein laufwerksrelativer Pfad. Nach einem Import standen in `data.js` falsche Pfade (`./ilme/…` statt `./Filme/…`), und Vorschaubilder fehlten auch nach vollständigem Einlesen. Mit `subst` nachgestellt und behoben.
Alternativen: –
Konsequenzen: Ein betroffenes `data.js` repariert ein vollständiges Einlesen mit dem neuen Updater.
Betroffene Komponenten: `DataTask.cs` (`UpdateBranch`), `Core.cs` (`InstallId.Ensure`), `ImportTask.cs`
Abhängigkeiten: DEC-090, DEC-092

### DEC-100: System- und versteckte Ordner werden überall ignoriert
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: `Folders.SystemDirs` enthält Papierkorb- und Systemordner (u.a. von NAS). Neu `Folders.IsHidden` (Name beginnt mit „.“ oder Attribut Versteckt/System, Laufwerkswurzeln ausgenommen), `Folders.Ignore(path)` und `Folders.IgnoreAny(root, rel)`. Alle Aufgaben nutzen es: Daten einlesen (auch Teil-Einlesen und Ordnerliste), Importer, Vorschaubilder, Zusammenführen, Ablagen leeren, Import-Kategorien. Die eigene Liste `DisallowedItems` in `DataTask.cs` entfällt.
Begründung: Wunsch des Nutzers, zuerst nur für einen NAS-Papierkorbordner, dann allgemein für versteckte Ordner.
Alternativen: Nur einzelne Ordnernamen ausschliessen (erste Stufe, erweitert).
Konsequenzen: Versteckte Ordner mit Medien erscheinen nicht mehr in STEFFLIX.
Betroffene Komponenten: `Core.cs` (`Folders`), `DataTask.cs`, `ImportTask.cs`, `PreviewTask.cs`, `MergeTask.cs`, `CleanupTask.cs`, `MainForm.cs` (`LoadCategories`)
Abhängigkeiten: DEC-101

### DEC-101: Texte für Nutzer nennen keine einzelnen Systemordner
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: Changelog, Hilfe, README und Meldungen sagen nur „System- und versteckte Ordner werden ignoriert“, ohne Namen bestimmter NAS-Ordner.
Begründung: Ausdrückliche Vorgabe des Nutzers („überall entfernen“).
Alternativen: Ordnernamen je NAS-Hersteller nennen (zuerst so geschrieben, entfernt).
Konsequenzen: Gilt als Regel in `.claude\CLAUDE.md`.
Betroffene Komponenten: `sflix_sys\news.txt`, `start.html`, `README.md`, `MainForm.cs`, `DataTask.cs`
Abhängigkeiten: DEC-100

## Ordner und Vorschaubilder in der Weboberfläche

### DEC-094: „Gesehen“ für Ordner wird bei jedem Seitenaufbau berechnet
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Eine Ordnerkachel trägt „Gesehen“, wenn der Ordner mindestens ein Video enthält und alle Videos darin (laut `data.js`) gesehen sind. Für Ordner wird nichts gespeichert.
Begründung: Wunsch des Nutzers. Weil nur einzelne Medien gespeichert sind, nimmt eine neue, ungesehene Folge dem Ordner das Abzeichen automatisch wieder weg (vom Nutzer gefragt und bestätigt).
Alternativen: –
Konsequenzen: Neue Dateien zählen erst nach „Daten einlesen“. Ordner nur mit Bildern oder Musik bekommen kein Abzeichen.
Betroffene Komponenten: `sflix_sys\js\cards.js` (`isFolderSeen`, `createFolderCard`)
Abhängigkeiten: –

### DEC-095: Bildordner ohne Vorschaubild zeigen ihr erstes Bild
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Fehlt das Vorschaubild, nimmt `getFirstImageInFolder` das erste Bild im Ordner (natürlich sortiert, ohne `Preview`), sonst rekursiv aus den sortierten Unterordnern.
Begründung: Wunsch des Nutzers („erstes Bild, alphanumerisch sortiert“).
Alternativen: –
Konsequenzen: Das Bild wird in voller Grösse geladen, nicht als verkleinertes Vorschaubild.
Betroffene Komponenten: `sflix_sys\js\cards.js` (`getFirstImageInFolder`, `createFolderCard`)
Abhängigkeiten: –

### DEC-102: Ordner und Medien gemeinsam alphabetisch auf Ordnerseiten
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Auf Seiten wie „Filme“ stehen Ordner und Medien in einer gemeinsamen, natürlich sortierten Liste (`compareNatural`); das eigene Band „Alle Filme“ für Ordner entfällt. Staffeln und Alben (`shouldShowFolderContents`) behalten ein Band je Unterordner.
Begründung: Wunsch des Nutzers; Ordner sollten in die alphabetische Sortierung aufgenommen werden (Vorschlag 1 des Agenten).
Alternativen: Ordner weiterhin getrennt.
Konsequenzen: Ordnerkacheln öffnen den Ordner, Medienkacheln spielen ab.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderFolderPage`)
Abhängigkeiten: DEC-103, DEC-104, DEC-105

### DEC-103: Bänder nach Anfangsbuchstaben, Gruppen nach Inhalt
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Erste Umsetzung (Vorschlag 2): Benachbarte Anfangsbuchstaben werden zu Bändern mit etwa 10 bis 30 Einträgen zusammengelegt, grosse Buchstaben nach dem zweiten Buchstaben geteilt, Titel wie „R – Sj“; Umlaute und Akzente zählen zum Grundbuchstaben (Hinweis des Nutzers), Sonderzeichen zu „Sonstige“. Werte zuerst 20/10/20/30, auf Wunsch des Nutzers geändert auf 15/–/15/25.
Begründung: Die festen Gruppen („Q – T“ usw.) passten selten zu den Titeln.
Alternativen: Feste Buchstabengruppen (vorher).
Konsequenzen: Ersetzt durch DEC-104 und dann DEC-105.
Betroffene Komponenten: `sflix_sys\js\pages.js`
Abhängigkeiten: DEC-104, DEC-105

### DEC-104: Gleichmässige Verteilung auf Bänder mit Buchstabentiteln
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Zweite Umsetzung: bis 9 Einträge ein Band, sonst gleichmässig auf mindestens 3 Bänder mit höchstens 15 Einträgen; Titel zeigen den Anfang bis zur ersten Abweichung (z.B. „Av – Car“).
Begründung: Vorgabe des Nutzers für die Verteilung.
Alternativen: –
Konsequenzen: Vom Nutzer verworfen („das ist quatsch … das gefällt mir nicht“) und rückgängig gemacht; ersetzt durch DEC-105. Ersetzt durch DEC-105.
Betroffene Komponenten: `sflix_sys\js\pages.js`
Abhängigkeiten: DEC-103, DEC-105

### DEC-105: Ein Band ohne Titel, das in Zeilen umbricht
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Ordnerseiten zeigen ein Band ohne Titel (`untitledRow`, rechts nur „Alle anzeigen“). Zeilen (`splitIntoRows`): bis 9 Einträge eine Zeile, bis 36 gleichmässig auf 3 Zeilen (vordere Zeilen bekommen den Rest), danach Zeilen zu je 12. Beispiele: 14 → 5+5+4, 37 → 12+12+12+1.
Begründung: Vorgabe des Nutzers („alphabetisch, nach 12 Einträgen umbrechen, Regel >9 und ≤36 gilt weiter“); später ausdrücklich: „die Bänder sollen keinen Titel tragen“.
Alternativen: DEC-103, DEC-104; Titel = gewählte Kategorie (zuerst so beantwortet, dann vom Nutzer zurückgenommen).
Konsequenzen: Zusatzzeilen entstehen über `addRowTrack`; die Länge ist fest (12), nicht abhängig von der Fensterbreite. Später ergänzt um alphabetische Gruppen mit Sprungleiste (DEC-117), getrennte Ordner und automatische Zeilenlänge (rowAuto).
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderGroupedBands`, `splitIntoRows`, `splitEvenly`, `rowSize`), `sflix_sys\js\cards.js` (`addRowTrack`), `sflix_sys\sflix.css` (`.untitledRow`)
Abhängigkeiten: DEC-102, DEC-106; DEC-104

### DEC-106: „Alle anzeigen“ bei jedem Band, Rasteransicht als Overlay
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Jedes Band hat „Alle anzeigen“. Hat es eine eigene Seite (Kategorien der Startseite, Staffelbänder), führt der Link dorthin; sonst öffnet ein Knopf die Rasteransicht `#cardGrid` (Titel, Anzahl, Schliessen-Knopf, Esc). Die Rasteransicht verschiebt die echten Kacheln hinein und beim Schliessen zurück; ein Klick auf eine Kachel schliesst sie.
Begründung: Wunsch des Nutzers; Wahl „Rasteransicht (Empfohlen)“. Das Verschieben der echten Kacheln erhält ihre Ereignisse ohne neuen Aufbau.
Alternativen: Weitere im Dialog angebotene Varianten sind nicht überliefert.
Konsequenzen: `closeOverlays` schliesst zuerst die Rasteransicht; im Bearbeitungsmodus ist der Link ausgeblendet.
Betroffene Komponenten: `sflix_sys\js\cards.js` (`createCardRow`, `openCardGrid`, `closeCardGrid`), `sflix_sys\js\ui.js` (`closeOverlays`), `sflix_sys\sflix.css` (`#cardGrid`, `.rowLink`)
Abhängigkeiten: DEC-105

### DEC-107: Buchstabenleiste zum Springen zwischen Bändern
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Vorschlag 3 des Agenten: eine Leiste mit den Gruppen unter dem Seitentitel, Klick springt zum Band. Der Nutzer wählte „1+2 zusammen“, also nicht diesen Vorschlag; in dieser Session nicht umgesetzt.
Begründung: Lange Kategorien ohne Scrollen erreichbar.
Alternativen: –
Konsequenzen: – (Hinweis: Laut heutiger CLAUDE.md gibt es inzwischen eine Sprungleiste, ausserhalb dieser Session.); Ersetzt durch DEC-117.
Betroffene Komponenten: –
Abhängigkeiten: DEC-103

### DEC-108: Geringerer Abstand zwischen Bändern
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: `.cardRow` hat unten 20 statt 34 px Abstand, auch auf der Startseite.
Begründung: Wunsch des Nutzers, den Platz besser zu nutzen („eine kleine Korrektur“).
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: `sflix_sys\sflix.css` (`.cardRow`)
Abhängigkeiten: –

## Wiedergabe

### DEC-109: Pfadleiste bei der Wiedergabe unter den Knöpfen Zurück/Weiter
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Während der Wiedergabe steht die Pfadleiste hinter `#mediaNav`; auf Ordnerseiten bleibt sie oben.
Begründung: Wunsch des Nutzers: Das Video soll ganz oben sein, damit es beim Hochscrollen sauber zentriert ist.
Alternativen: –
Konsequenzen: Die Leiste wird je nach Seite verschoben (`renderMediaContext`, `renderFolderPage`).
Betroffene Komponenten: `sflix_sys\js\pages.js`, `sflix_sys\sflix.css` (`.folderHeader.playerHeader`)
Abhängigkeiten: –

### DEC-110: Autoplay-Knopf unter Bildern
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Neuer Knopf `#autoplayNav` zwischen „Merken“ und „Zufall“, nur bei Bildern; öffnet das Autoplay-Fenster, rot hervorgehoben wenn Autoplay an ist. Bis 480 px Breite zeigen „Autoplay“ und „Zufall“ nur das Symbol.
Begründung: Wunsch des Nutzers nach schnellem Zugang. Videos und Musik haben Autoplay schon in der Player-Leiste; fünf beschriftete Knöpfe passen auf dem Handy nicht nebeneinander.
Alternativen: –
Konsequenzen: `updatePlayerAutoplayState` aktualisiert auch diesen Knopf.
Betroffene Komponenten: `start.html`, `sflix_sys\js\player.js` (`initAutoplayNavButton`, `updateAutoplayNavButton`, `loadDecodedMedia`), `sflix_sys\js\main.js`, `sflix_sys\sflix.css`
Abhängigkeiten: –

## Hilfe und Dokumentation

### DEC-096: Hilfe erklärt, wie Daten beim Schliessen des Browsers erhalten bleiben
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Hilfe und README beschreiben je Browser die Ausnahme für „Daten beim Schliessen löschen“: Firefox an zwei Stellen (`about:preferences#privacy` und Chronik unter `about:preferences#history`, Eintrag `file:///`), Chrome (`chrome://settings/content/siteData`) und Edge (`edge://settings/clearBrowsingDataOnClose`) mit `file:///*`. Die Adressen sind Knöpfe, die die Adresse kopieren (`copyAddress`); die Zeilen für den Fall, dass die Ausnahme nicht greift, wurden entfernt.
Begründung: Der Nutzer verlor in Firefox die Daten bei jedem Schliessen; die Ausnahme half erst zusammen mit der Chronik-Einstellung. Für Edge bestätigte der Nutzer, dass `file:///*` wirkt. Browser lassen Links von `file://` zu internen Einstellungsseiten nicht zu, darum Kopieren statt Link.
Alternativen: Links auf die Einstellungsseiten (vom Browser blockiert); anderen Browser verwenden oder Datensicherung als Notlösung (vom Agenten vorgeschlagen, nicht in die Hilfe übernommen).
Konsequenzen: Kopieren über `navigator.clipboard` mit Rückfall auf `execCommand`.
Betroffene Komponenten: `start.html` (Hilfe „Browsereinstellungen“), `README.md`, `sflix_sys\js\ui.js` (`copyAddress`), `sflix_sys\sflix.css` (`.copyAddress`)
Abhängigkeiten: DEC-088

### DEC-097: Hilfe empfiehlt den Importer zum Hinzufügen von Medien
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: „Neue Medien hinzufügen“ beschreibt zuerst den Importer; Kopieren von Hand mit „Daten einlesen“ steht als Alternative in einem `<details>`-Abschnitt (auch im README).
Begründung: Wunsch des Nutzers; der Updater aktualisiert `data.js` schneller und gezielter.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: `start.html`, `README.md`
Abhängigkeiten: DEC-092

## Release und Projektordner

### DEC-098: release.ps1 hängt Einträge an einen vorhandenen Versionsblock an
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: Gibt es in `news.txt` schon einen Block `<b>Version X</b>`, fügt `release.ps1` neue Einträge vor dessen erstem `</ul>` ein, statt einen zweiten Block anzulegen; ohne Einträge bleibt die Datei unverändert. Ein leerer doppelter 2.03-Block wurde entfernt.
Begründung: Beim Release entstand ein doppelter Block für dieselbe Version; vom Nutzer so beauftragt.
Alternativen: –
Konsequenzen: An einer kopierten `news.txt` getestet, nicht in einem echten Release.
Betroffene Komponenten: `SFLIX Tools\Release\release.ps1`, `sflix_sys\news.txt`
Abhängigkeiten: DEC-026, DEC-082

### DEC-111: Alte Python-Skripte unter SFLIX Tools\Updater
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: `setup.py`, `teile_zusammenfuegen.py` und `convertVideos.py` liegen bei den alten Python-Updatern in `SFLIX Tools\Updater\`.
Begründung: Auftrag des Nutzers; alle drei sind durch Funktionen des Updaters ersetzt.
Alternativen: –
Konsequenzen: Kein Skript verweist auf die alten Pfade; nur Kommentare im C#-Code und die Entwickler-Doku nennen die Dateinamen.
Betroffene Komponenten: `SFLIX Tools\Updater\`, `.claude\CLAUDE.md`
Abhängigkeiten: –

### DEC-112: Weboberfläche nur über den Vorschau-Server testen
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Testen)
Entscheidung: Die Testumgebung wird nie per `file://` geöffnet, sondern über `http://localhost:8765/.claude/testenv/start.html`.
Begründung: Die per `file://` geöffnete Testumgebung teilte den Speicher mit der echten Installation; zusammen mit den Fehlern aus DEC-086/DEC-087 war das die Ursache für die verlorene Sortierung und die fehlenden „Weiterschauen“-Einträge.
Alternativen: –
Konsequenzen: Als Regel in `.claude\CLAUDE.md` eingetragen. Der einfache Server kann keine Range-Anfragen.
Betroffene Komponenten: `.claude\launch.json`, `.claude\testenv\`
Abhängigkeiten: DEC-090

---

## A. Offene Entscheidungen (diese Session)

1. **Buchstabenleiste** (DEC-107): vorgeschlagen, vom Nutzer nicht gewählt und nicht ausdrücklich abgelehnt.
2. **Teil-Einlesen in der Oberfläche**: Der Dialog „Hinzufügen …“ und der Import wurden nicht über die Oberfläche getestet, nur die Logik als DLL. Die gekürzte Hinweiszeile wurde nach dem Neukompilieren nicht mehr im Fenster angesehen.

## B. Änderungsbedarf an bestehenden Entscheidungen

- **DEC-082 / DEC-026** bestätigt: Der Nutzer bat, 2.03 neu zu bauen; der Agent lehnte ab, weil der Nutzer `build_release.bat` selbst ausführt.
- **DEC-054** („__“-Ordner ignorieren) wurde durch DEC-100 auf System- und versteckte Ordner erweitert.
- Innerhalb dieser Session: DEC-103 → DEC-104 → DEC-105.
- **DEC-105 und DEC-107** sind nach der heutigen CLAUDE.md (in dieser Session beim Start eingelesen) überholt: Ordnerseiten haben inzwischen alphabetische Gruppen mit Sprungleiste, Ordner getrennt und automatische Zeilenlänge (`rowAuto`). Das geschah ausserhalb dieser Session und ist hier nicht bewertet.
- Eine Aussage des Agenten in dieser Session, Firefox beachte die Ausnahme `file:///` grundsätzlich nicht, erwies sich als unvollständig: Mit der Chronik-Einstellung wirkt sie (DEC-096).

## C. Dokumentationsbedarf

- **architecture.md**: Speicher je Installation (`install.js`, `STEFFLIX-<id>`, einmalige Übernahme, Kopie/Verschiebung); Teil-Einlesen und welche Aufgabe welche Ordner einliest; `Folders.Ignore`/`IgnoreAny` als gemeinsame Regel aller Aufgaben.
- **data-model.md**: `install.js` (`installId`, `installFolder`); Markierung `sharedDataImported`; Einstellungen `data.all`/`data.folders`; Reihenfolge in `data.js` muss der Ordnerreihenfolge entsprechen.
- **testing.md**: Laufwerkswurzel mit `subst S: <Pfad>` nachstellen und mit `subst S: /D` entfernen; Browser-Cache mit `fetch(url, {cache: 'reload'})` umgehen; csc-Prüfung braucht `System.IO.Compression(.FileSystem).dll`; Testumgebung nie per `file://`.

## D. Technische Risiken

1. **Laufwerkswurzeln**: Jedes `TrimEnd('\\')` auf einem Pfad, der danach in `Path.Combine` oder einer Dateioperation landet, macht aus `D:\` einen laufwerksrelativen Pfad.
2. **Teil-Einlesen** übersieht Änderungen ausserhalb der gewählten Ordner; eine veraltete `data.js` fällt erst beim vollständigen Einlesen auf.
3. **Kopierte Installationen**: Die Unterscheidung Kopie/Verschiebung hängt davon ab, dass der alte Ordner noch erreichbar ist; wird `install.js` von Hand in eine andere Installation kopiert, teilen sich beide den Speicher.
4. **Versteckte Ordner**: Nutzer, die Medien bewusst in versteckten Ordnern ablegen, sehen sie nicht mehr.
5. **Rasteransicht** verschiebt die echten Kacheln; ein Seitenwechsel oder Fehler bei offenem Raster könnte Kacheln am falschen Ort lassen, wenn `closeCardGrid` nicht läuft.
6. **Browsereinstellungen** zum Löschen beim Schliessen liegen ausserhalb von STEFFLIX; die Adressen in der Hilfe können sich mit neuen Browserversionen ändern.

# Session STEFFLIX 2.04 / Updater 3.2 (Arbeit vom 27. und 28.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (STEFFLIX 2.04, Updater 3.2.0 bzw. 3.2.1 im Quellcode), erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-113).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt, sie stehen unter B.

Einschränkung: Die Session wurde einmal zusammengefasst. Die Entscheidungen bis zur Umsetzung von Updater 3.2 (DEC-114 bis DEC-131) stützen sich auf diese Zusammenfassung, die übrigen auf den wörtlichen Verlauf. Gebaut wurden nur Testbuilds nach `dist`. Das Release führt der Nutzer mit `build_release.bat` aus.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 2 |
| Implemented | 28 |
| Proposed | 0 |
| Rejected | 1 |
| Superseded | 1 |

## Arbeitsweise

### DEC-113: Ideen sammeln, umsetzen erst nach ausdrücklichem Signal
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Arbeitsweise)
Entscheidung: Der Nutzer sammelt Ideen, der Agent ändert dabei nichts, meldet aber Widersprüche, Rückfragen und Verbesserungsvorschläge. Umgesetzt wird erst auf ein ausdrückliches Signal („Release erstellen“, „loslegen“, „GO“). Vorher zeigt der Agent die Liste der Änderungen und fragt nochmals nach. Sagt der Nutzer ausdrücklich „direkt implementieren“, gilt das als Signal.
Begründung: Wunsch des Nutzers („Führe die Changes erst aus wenn ich dir das GO gebe. Ich sammle erstmal Ideen.“).
Alternativen: Keine überliefert.
Konsequenzen: Vorschläge stehen bis zum Signal nur im Gespräch. Als Memory gespeichert (`idea-collection-workflow.md`). Ergänzt um die Versionsregeln je GO (DEC-196, DEC-334).
Betroffene Komponenten: keine (Arbeitsweise)
Abhängigkeiten: DEC-131

## STEFFLIX 2.04: Einstellungen und Ordnerseiten

### DEC-114: Eigene Seite „Einstellungen“, Werte sofort wirksam und Teil der Datensicherung
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Neue Seite „Einstellungen“ (URL-Seite `SETTINGS`, Menüpunkt mit Zahnrad). Alle Einstellungen der Oberfläche liegen als JSON unter `displaySettings`, gelten sofort und gehören zur Datensicherung. Datensicherung, Fehlerbehebung und Browser-Steuerung ziehen von der Hilfe bzw. dem Autoplay-Fenster in die Einstellungen, die Hilfe verweist nur noch darauf. Der Menüpunkt „Autoplay“ entfällt (Autoplay über das Symbol im Player bzw. den Knopf unter Bildern). „Zurücksetzen“ setzt nur die Einstellungen der Seite zurück.
Begründung: Ideen des Nutzers (Menüpunkt „Einstellungen“, Autoplay aus dem Menü). „Sofort übernehmen“ und „Zurücksetzen“ hat der Nutzer bestätigt.
Alternativen: Keine überliefert.
Konsequenzen: Hilfe und Einstellungen nutzen denselben Aufbau (`buildSectionPage`). Gespeicherte Daten wie Favoriten, Fortschritt und Autoplay bleiben beim Zurücksetzen erhalten.
Betroffene Komponenten: `start.html` (`#settingsPage`), `sflix_sys\js\core.js` (`getDisplaySettings`, `getSetting`, `setSetting`, `resetDisplaySettings`, `applyDisplaySettings`), `sflix_sys\js\ui.js` (`initSettingsPage`), `sflix_sys\js\pages.js` (`showSettingsPage`, `openSettings`, `buildSectionPage`), `sflix_sys\js\main.js`
Abhängigkeiten: DEC-115 bis DEC-120, DEC-135, DEC-136

### DEC-115: Kachelgrösse wählbar statt Anzahl Kacheln begrenzen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Die Kacheln sind auf dem Computer grösser (Standard 5 statt 6 nebeneinander). In den Einstellungen lässt sich die Kachelgrösse auf Klein, Mittel oder Gross stellen (Klassen `cardsSmall`/`cardsLarge` auf `<html>`, CSS-Variable `--cards-per-view` je Breakpoint). Auf dem Handy bleibt sie gleich.
Begründung: Der Nutzer wollte grössere Kacheln, aber die Anzahl nicht beschränken, und wünschte eine Einstellung „klein, mittel, gross“.
Alternativen: Anzahl Kacheln begrenzen (vom Nutzer verworfen).
Konsequenzen: Die Kachelbreite hängt von `--cards-per-view` ab, das später auch die automatische Zeilenlänge bestimmt (DEC-137). Die Ausnahme für das Handy entfiel: Die Kachelgrösse wirkt auf allen Breiten (DEC-195).
Betroffene Komponenten: `sflix_sys\sflix.css`, `sflix_sys\js\core.js` (`applyDisplaySettings`)
Abhängigkeiten: DEC-116, DEC-137

### DEC-116: „Kacheln pro Zeile“ als Umbruch eines Bandes
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: „Kacheln pro Zeile“ (4 bis 30, Standard 12) legt fest, nach wie vielen Kacheln ein Band auf eine neue Zeile umbricht. Bis drei Viertel davon bleibt es eine Zeile, bis dreimal so viele werden gleichmässig auf höchstens drei Zeilen verteilt.
Begründung: Der Nutzer wählte die Umbruch-Variante (b).
Alternativen: Andere Bedeutung von „Kacheln pro Zeile“ (Variante a, nicht gewählt; Inhalt nicht überliefert).
Konsequenzen: `splitIntoRows` verteilt die Einträge. Ergänzt durch die automatische Variante (DEC-137).
Betroffene Komponenten: `sflix_sys\js\pages.js` (`splitIntoRows`, `splitEvenly`, `renderGroupedBands`)
Abhängigkeiten: DEC-137

### DEC-117: Alphabetische Gruppen für alle Ordner, mit Ausnahmen und Sprungleiste
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Schalter „Alphabetische Gruppen“ (A–D, E–H, I–L, M–P, Q–T, U–Z, 0–9 / #) für alle Ordner, mehrzeilig. Gruppiert wird nur, wenn es mehr als eine Gruppe gibt. Serienfolgen (S01E02) bleiben in ihrer Reihenfolge. Bilder lassen sich ausnehmen (Standard ein), Musik ebenfalls (Standard aus). Eine Sprungleiste erscheint mit eigenem Schalter, der nur bei eingeschalteter Gruppierung sichtbar ist.
Begründung: Wunsch des Nutzers („ja nimm das für alle Ordner“, Musik nur gruppieren, wenn es mehr als eine Gruppe geben kann, Musik ausnehmbar).
Alternativen: Keine überliefert.
Konsequenzen: Unteroptionen hängen über `data-depends` an der Hauptoption.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`groupAlphabetically`, `shouldGroupAlphabetically`, `renderFolderEntries`, `createJumpBar`), `start.html`
Abhängigkeiten: DEC-118, DEC-138; DEC-107

### DEC-118: Artikel beim Sortieren ignorieren, auch in Playlisten
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Führende Artikel (Der, Die, Das, Ein, Eine, The, A, An) werden beim Sortieren und Gruppieren übergangen („Der Pate“ unter P), abschaltbar. Die Regel gilt über `sortNatural` auch für Playlisten, damit Vor/Zurück und Autoplay der angezeigten Reihenfolge folgen.
Begründung: Vom Nutzer angenommener Vorschlag.
Alternativen: Keine überliefert.
Konsequenzen: Ändert sich die Einstellung, ändert sich die Reihenfolge aller Ordner-Playlisten.
Betroffene Komponenten: `sflix_sys\js\core.js` (`leadingArticles`, `getSortName`, `compareSortNames`, `sortNatural`)
Abhängigkeiten: DEC-117

### DEC-119: „Gesehen“ wird immer gespeichert, nur die Markierungen lassen sich ausblenden
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Schalter „Markierungen auf Kacheln anzeigen“ blendet alle Markierungen aus („Gesehen“, „Letzte Folge“, „Letzter Titel“). Gesehen wird trotzdem gespeichert.
Begründung: Der Nutzer wollte „Gesehen“ im Speicher behalten und nur die Anzeige ein- und ausschalten; ausgeblendet heisst alle ausgeblendet.
Alternativen: Option „Gesehen nicht speichern“ (vom Nutzer gestrichen).
Konsequenzen: Die Daten bleiben vollständig, nur `cards.js` prüft `showMarkers`.
Betroffene Komponenten: `sflix_sys\js\cards.js`, `sflix_sys\js\core.js`
Abhängigkeiten: keine

### DEC-120: Band „Favoriten“ immer verfügbar
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Das Band „Meine Favoriten“ heisst „Favoriten“ und erscheint unter „Startseite anpassen“ auch ohne Einträge, damit sich seine Position festlegen lässt. „Weiterschauen“ zeigt höchstens `continueMax` Einträge (5 bis 50, Standard 20), gespeichert werden bis 50.
Begründung: Idee des Nutzers; Anzahl bei „Weiterschauen“ als angenommener Vorschlag.
Alternativen: Keine überliefert.
Konsequenzen: Leere Bänder werden über `markRowEmpty` dargestellt.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`showFavoritesRow`, `markRowEmpty`, `showContinueWatching`), `sflix_sys\js\player.js` (`setLastPlayed`)
Abhängigkeiten: keine

## Updater 3.2: Ordner, Sicherheit und Protokoll

### DEC-121: Installationsordner nur lesbar, eigene Ordnerfelder je Tool
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: Der frühere „Medienordner“ heisst „Installationsordner“, wird nur angezeigt und bei der Installation festgelegt. Vorschaubilder, Zusammenführen, Konvertieren und Ablagen leeren haben je ein Ordnerfeld mit dem Schalter „Alles“ (gleich für alle Tools) oder einer Liste mehrerer Ordner, auch ausserhalb des Installationsordners (ausser Ablagen leeren, DEC-122). Die Auswahl wird nicht gespeichert.
Begründung: Vorgaben des Nutzers („Installationsordner soll nur bei der Installation angegeben werden können“, „Ordner ausserhalb kannst du zulassen“, Auswahl muss nicht gespeichert werden).
Alternativen: Medienordner im Updater änderbar (vom Nutzer verworfen).
Konsequenzen: Ein früher eingestellter Medienordner wird nicht mehr verwendet. `media.folder` und die alten Ordner-Schlüssel werden beim Speichern geleert. Ohne Oberfläche arbeiten alle Tools im Installationsordner.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\MainForm.cs` (`FolderPick`, `installFolderBox`, `StartTasks`), `src\Core.cs` (`Folders.WithoutNested`), `src\Program.cs`
Abhängigkeiten: DEC-122, DEC-123

### DEC-122: „Ablagen leeren“ nur innerhalb des Installationsordners
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sicherheit
Entscheidung: „Ablagen leeren“ löscht nur innerhalb des Installationsordners. Geprüft wird beim Wählen des Ordners und nochmals direkt vor dem Löschen. Verknüpfungen (Reparse-Points) werden nie verfolgt und nie als Ablage gelöscht.
Begründung: Der Nutzer: „die user sollen nicht versehentlich zeugs von ihrer festplatte löschen“.
Alternativen: Löschen in beliebigen Ordnern wie bei den anderen Tools (verworfen).
Konsequenzen: Ordner ausserhalb werden im Ordnerfeld von Ablagen leeren abgelehnt. Getestet: eine Verknüpfung nach aussen blieb unberührt.
Betroffene Komponenten: `src\CleanupTask.cs` (`Run(ctx, roots)`), `src\Core.cs` (`Folders.IsInsideRoot`), `src\MainForm.cs` (`AddPickFolder`)
Abhängigkeiten: DEC-121

### DEC-123: Nach Tools nur die betroffenen Ordner einlesen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Performance
Entscheidung: Nach Vorschaubildern, Zusammenführen und Konvertieren liest der Updater nur die betroffenen Ordner im Installationsordner neu ein, bei „Alles“ alles. Nach „Ablagen leeren“ wird nichts eingelesen.
Begründung: Idee des Nutzers (gezieltes Update von `data.js` statt der ganzen Mediathek).
Alternativen: Immer alles einlesen (bisheriges Verhalten).
Konsequenzen: Nutzt das Teil-Einlesen `DataTask.Run(ctx, folders)`.
Betroffene Komponenten: `src\MainForm.cs` (`StartTasks`, `TaskRunner.DataFolders`), `src\DataTask.cs`
Abhängigkeiten: DEC-121

### DEC-124: Protokoll rechts mit Splitter, Suche und Filter
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Das Protokoll („Details“) steht rechts neben dem Inhalt, die Versionsanzeige bleibt. Es hat einen verschiebbaren Splitter (Breite in `log.width`), lässt sich ein- und ausklappen, färbt Zeilen nach Art mit Uhrzeit und hat Suche, Filter „Nur Fehler und Warnungen“, Kopieren, Leeren und „Datei öffnen“. Bei einem Fehler klappt es von selbst auf.
Begründung: Idee des Nutzers (Detailausgabe erneuern, eventuell rechts), Splitter, Ein-/Ausklappen sowie Filter und Suche ausdrücklich gewünscht.
Alternativen: Protokoll unter dem Inhalt (bisher).
Konsequenzen: Beim Aufklappen wird das Fenster breiter. Das Ziehen am Splitter liest die echte Maus und wurde nicht getestet.
Betroffene Komponenten: `src\LogView.cs` (`LogPanel`, `SplitBar`), `src\MainForm.cs` (`SetLogExpanded`, `SetLogWidth`, `FitHeight`)
Abhängigkeiten: keine

### DEC-125: Zusammenfassung je Aufgabe und Übersicht nach „Daten einlesen“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: UI
Entscheidung: Am Ende nennt der Updater je Aufgabe Ergebnis und Dauer. Nach „Daten einlesen“ folgt eine Übersicht je Kategorie (Videos, Titel, Bilder, Ordner, fehlende Vorschaubilder).
Begründung: Vom Nutzer gewünscht („Anzahl Einträge nach dem Einlesen anzeigen“).
Alternativen: Keine überliefert.
Konsequenzen: Jede Aufgabe setzt `TaskContext.Summary`.
Betroffene Komponenten: `src\Core.cs` (`TaskContext.Summary`), `src\DataTask.cs` (`LogOverview`), `src\MainForm.cs` (`TaskRunner`)
Abhängigkeiten: keine

## Updater 3.2: Folgen umbenennen

### DEC-126: Neues Tool „Folgen umbenennen“ mit Quellen ohne API-Schlüssel
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Architektur
Entscheidung: Das Python-Skript `folgenliste.py` wird als Tool in den Updater übernommen. Die Folgenliste kommt zuerst von fernsehserien.de (deutsche Titel, HTML ausgelesen), ohne Treffer von TVmaze (englische Titel, JSON). Keine API-Schlüssel, kein Wikipedia. Die Suche läuft im Dialog im Hintergrund.
Begründung: Vorgaben des Nutzers (keine API-Keys, Wikipedia weg).
Alternativen: Wikipedia als Quelle und Quellen mit API-Schlüssel (vom Nutzer verworfen).
Konsequenzen: Ändert fernsehserien.de den Aufbau der Seite, findet das Tool dort nichts mehr und weicht auf TVmaze aus (englische Titel). Später kam serienjunkies.de als erste Quelle hinzu (DEC-240).
Betroffene Komponenten: `src\EpisodeTask.cs` (`EpisodeSource`, `FernsehserienSource`, `TvmazeSource`, `EpisodeHttp`), `src\EpisodeDialogs.cs` (`EpisodeSearchDialog`)
Abhängigkeiten: DEC-127, DEC-128, DEC-134

### DEC-127: Namensschema und btoa-kompatible Dateinamen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Zielname „Serie - S01E02 - Titel.ext“ im Staffelordner „Staffel 01“ (zweistellig, wie in der Folgenliste). ß wird zu ss, Zeichen, die Windows oder `btoa()` nicht verarbeiten, werden wie beim Einlesen ersetzt.
Begründung: Vorgaben des Nutzers („nur ss verwenden“, keine Zeichen, die nicht `btoa()`-kompatibel sind, „nicht Staffel 1 sondern Staffel 01“).
Alternativen: Keine überliefert.
Konsequenzen: Umbenannte Namen sind in URLs von STEFFLIX immer kodierbar.
Betroffene Komponenten: `src\EpisodeTask.cs` (`BuildName`, `CleanPart`, `SeasonFolderName`), `src\DataTask.cs` (`SanitizeFileName`)
Abhängigkeiten: DEC-145

### DEC-128: Staffelordner angleichen, Wahl bei mehreren Dateien, Vorschau und Rückgängig
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Konflikterkennung)
Entscheidung: Staffelordner werden umbenannt („Staffel 1“ → „Staffel 01“), gibt es beide, zusammengeführt. Folgen in der falschen Staffel oder direkt im Serienordner kommen in den richtigen, bei Bedarf neu angelegten Staffelordner. Gibt es mehrere Dateien für dieselbe Folge, wählt der Nutzer in der Vorschau eine (sonst nicht angehakt). Die Vorschau zeigt auch fehlende Folgen und nicht zugeordnete Dateien. Ersetzte Dateien kommen nach `__ersetzt`. Jeder Lauf wird protokolliert und lässt sich rückgängig machen. Vorschaubilder ziehen mit, `renamed.js` wird ergänzt.
Begründung: Antworten des Nutzers („zusammenführen“, „verschieben“, „Folgen direkt in korrekten ggf. neu erstellten Ordner“, bei mehreren Dateien entscheidet der Nutzer); Rückgängig und fehlende Folgen als angenommene Vorschläge.
Alternativen: Keine überliefert.
Konsequenzen: `sflix_sys\folgen_rueckgaengig.json` darf nicht gepusht werden. Favoriten, Fortschritt und „Weiterschauen“ bleiben über `renamed.js` erhalten.
Betroffene Komponenten: `src\EpisodeTask.cs` (`Plan`, `Execute`, `Undo`, `MergeFolder`, `MoveFolderPreview`, `SetAside`), `src\EpisodeDialogs.cs` (`EpisodePlanDialog`), `src\DataTask.cs` (`AppendRenamedPairs`)
Abhängigkeiten: DEC-145

### DEC-129: Doppelfolgen nach der kleinsten Folgennummer benennen
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Doppelfolgen (S01E01E02, S01E0102) zählen als eine Folge und werden nach der kleinsten Nummer benannt („Serie - S01E01 - Titel“).
Begründung: Vorgabe des Nutzers („immer nach der kleinsten Folgennummer benennen“).
Alternativen: Der Vorschlag, Doppelfolgen als alle Teile zu zählen (damit der zweite Teil nicht als fehlend erscheint), wurde zuerst vom Nutzer zurückgestellt („musst du nicht durchführen“).
Konsequenzen: Der zweite Teil erschien fälschlich als fehlende Folge. Ersetzt durch DEC-133.
Betroffene Komponenten: `src\EpisodeTask.cs` (`ParseEpisode`)
Abhängigkeiten: DEC-133

### DEC-130: folgenliste.py unverändert archivieren
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: `folgenliste.py` wird unverändert nach `SFLIX Tools\Updater` verschoben (nur Referenz). `serien_umbenennen.zip` bleibt, wo es ist, und wird nicht geöffnet (passwortgeschützt).
Begründung: Vorgabe des Nutzers („Das Pythonskript unverändert lassen“, ZIP bleibt).
Alternativen: Keine überliefert.
Konsequenzen: Keine.
Betroffene Komponenten: `SFLIX Tools\Updater\folgenliste.py`, `SFLIX Tools\serien_umbenennen.zip`
Abhängigkeiten: DEC-126

### DEC-131: GUI-Tests des Updaters im Hintergrund
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Sonstiges (Testen)
Entscheidung: Der Updater wird für Tests minimiert ohne Fokus gestartet (`CreateProcess`, `SW_SHOWMINNOACTIVE`), ausserhalb des Bildschirms wiederhergestellt (x = -6000, `SW_SHOWNOACTIVATE`) und nur über Fenster-Nachrichten bedient, Aufnahmen über `PrintWindow`. `SFLIX_UPDATE_URL=http://127.0.0.1:9/` verhindert den Update-Dialog.
Begründung: Der Nutzer: „versuche das öffnen des updaters so kurz wie möglich zu halten, damit ich nicht gestört werde. wenn möglich im hintergrund“.
Alternativen: Normales Öffnen des Fensters (stört den Nutzer).
Konsequenzen: Das Ziehen am Splitter lässt sich so nicht testen. In CLAUDE.md unter „Bauen und Testen“ festgehalten.
Betroffene Komponenten: keine (Testverfahren)
Abhängigkeiten: DEC-113

## Updater 3.2: Folgenlisten speichern und Doppelfolgen

### DEC-132: Folgenlisten speichern und beim nächsten Lauf anbieten
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Performance
Entscheidung: Geladene Folgenlisten werden in `sflix_sys\folgen_cache.json` gespeichert, gefunden über den Seriennamen (Ordnername oder gefundener Titel, ohne Gross-/Kleinschreibung, Jahr und Satzzeichen), nicht über den Pfad. Bei einem Treffer fragt ein Dialog: „Gespeicherte verwenden“ (ohne Seriensuche), „Neu laden“ (dieselbe Serie ohne Suche), „Andere Serie suchen“ oder „Abbrechen“. Kein Ablaufdatum, „Neu laden“ überschreibt. Gespeichert wird nur eine Liste mit mindestens einer Folge. Schlägt das Neuladen fehl, bleibt die alte Liste und der Dialog bietet sie erneut an.
Begründung: Wunsch des Nutzers, nicht immer dieselbe Abfrage abzusetzen. Antworten: Suche überspringen, Knopf „Andere Serie suchen“, kein Ablaufdatum, keine leeren Einträge, unbedingt nach Serienname statt Pfad (Ordner werden umbenannt).
Alternativen: Speicher nach Ordnerpfad (verworfen: findet nach Umbenennen nichts mehr), Ablaufdatum (vom Nutzer verworfen).
Konsequenzen: Neue persönliche Datei `folgen_cache.json` (nicht pushen). `EpisodeList` kennt jetzt `SeriesId` und `Loaded`. Die Knöpfe heissen seit Updater 3.4.4 „Liste aktualisieren“ und „Neu suchen“ (DEC-241).
Betroffene Komponenten: `src\EpisodeTask.cs` (`EpisodeCache`, `ChooseList`, `EpisodeSource.ByName`), `src\EpisodeDialogs.cs` (`EpisodeCacheDialog`), `src\Core.cs` (`TaskContext.ChooseCachedEpisodeList`), `src\MainForm.cs`
Abhängigkeiten: DEC-126

### DEC-133: Doppelfolgen behalten alle Nummern im Namen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Doppelfolgen werden vollständig bezeichnet, z.B. „Serie - S01E02E03 - Titel“. Erkannt werden S01E02E03, S01E02-E03, S01E02-03, S01E0203, 1x02-03 und 1x02-1x03, sortiert und ohne Doppelte. Eine Nummer ohne „E“ zählt nur, wenn sie höchstens 3 über der letzten liegt (sonst z.B. ein Jahr). Titel werden zusammengefasst („Pilot (1)“ + „Pilot (2)“ → „Pilot (1+2)“, gleiche Titel einmal, sonst „A + B“). Alle Teile gelten als vorhanden. Steckt eine Folge zusätzlich in einer anderen Datei, bilden beide eine Wahlgruppe. Nicht fortlaufende Nummern und Teile ohne Eintrag in der Folgenliste ergeben einen Hinweis in der Vorschau.
Begründung: Der Nutzer wollte, dass klar ersichtlich ist, dass es sich um eine Doppelfolge handelt, und gab das Signal „direkt implementieren“. Titel, Hinweis bei Lücken und Konfliktgruppe folgen den Vorschlägen des Agenten.
Alternativen: Nur der Titel des ersten Teils (Alternative zum Titelvorschlag, nicht gewählt); Benennung nach der kleinsten Nummer (DEC-129, ersetzt).
Konsequenzen: Bekannte Ungenauigkeit: Beginnt der Titel direkt nach der Nummer mit einer kleinen Zahl (`S01E05 - 6 Leben`), wird sie als zweiter Teil gelesen; sichtbar in der Vorschau.
Betroffene Komponenten: `src\EpisodeTask.cs` (`ParseEpisodes`, `EpisodeLabel`, `CombineTitles`, `BuildName`, `Plan`, `EpisodeOp.Episodes`)
Abhängigkeiten: ersetzt DEC-129, DEC-127, DEC-128

## STEFFLIX 2.04: Ergänzungen nach dem ersten GO

### DEC-134: Lizenz in den Einstellungen, Schlüssel nur als Sternchen
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Sicherheit
Entscheidung: Die Eingabe des Lizenzschlüssels zieht vom Hamburger-Menü in die Einstellungen (eigener Abschnitt vor „Zurücksetzen“). „Feedback“ bleibt im Menü, nur mit gültigem Schlüssel. Ist ein Schlüssel gespeichert, zeigen die Felder `****`. Ein Klick leert alle Felder, „Speichern“ mit unveränderten Sternchen behält den alten Schlüssel. „Zurücksetzen“ behält die Lizenz. Das frühere Lizenzfenster entfällt.
Begründung: Wunsch des Nutzers; Sternchen und Lizenz behalten beim Zurücksetzen hat er so festgelegt. Der Schlüssel enthält verschlüsselt Zugangsdaten und soll nicht lesbar in den Feldern stehen.
Alternativen: Schlüssel im Klartext in den Feldern anzeigen (verworfen).
Konsequenzen: Sternchen dürfen nie als Schlüssel gespeichert werden (`isLicenseMasked`). Ersetzt durch DEC-152.
Betroffene Komponenten: `start.html` (`#lizenz`, `#licenseInputs`), `sflix_sys\js\ui.js` (`initLicenseSection`, `fillLicenseFields`, `saveLicenseKey`), `sflix_sys\js\core.js` (`modalIds`), `sflix_sys\sflix.css`
Abhängigkeiten: DEC-114, DEC-142

### DEC-135: Neue Gliederung der Einstellungen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: UI
Entscheidung: Abschnitte Darstellung, Sortierung (vorher „Ordnerseiten“), Startseite, Wiedergabe, Datensicherung, Fehlerbehebung, Lizenz, Zurücksetzen. „Startseite und Wiedergabe“ wird aufgeteilt, damit „Update-Hinweise anzeigen“ unter Startseite passt und die Übersicht der Übergänge unter Wiedergabe.
Begründung: Vorgabe „Sortierung reicht“ und Zustimmung zum Vorschlag, den Abschnitt aufzuteilen.
Alternativen: „Sortierung und Gruppen“ (vom Nutzer nicht gewählt).
Konsequenzen: Sprungmarken `#wiedergabe`, `#lizenz` für Verweise aus der Hilfe.
Betroffene Komponenten: `start.html`, `sflix_sys\js\pages.js` (`showSettingsPage`)
Abhängigkeiten: DEC-114, DEC-141

### DEC-136: Kacheln pro Zeile automatisch nach verfügbarer Breite
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: UI
Entscheidung: Schalter „Kacheln pro Zeile automatisch“: Jede Zeile enthält genau so viele Kacheln, wie nebeneinander Platz haben (ganzzahliger Teil von `--cards-per-view`), auch auf dem Handy. Ist er an, wird „Kacheln pro Zeile“ ausgeblendet. Ändert sich die Fenstergrösse so, dass mehr oder weniger Kacheln passen, wird die Ordnerseite mit gleicher Scrollposition neu aufgebaut.
Begründung: Wunsch des Nutzers, Handy ausdrücklich eingeschlossen.
Alternativen: Keine überliefert.
Konsequenzen: Neues Attribut `data-hides` in den Einstellungen. Die Browser-Emulation im Test löste kein `resize` aus; die Logik wurde mit einem ausgelösten Ereignis geprüft.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`getCardsPerRow`, `splitIntoRows`, `initAutoRowResize`, `renderFolderPage`), `sflix_sys\js\ui.js` (`fillSettingsFields`), `sflix_sys\js\core.js` (`rowAuto`)
Abhängigkeiten: DEC-115, DEC-116

### DEC-137: Musik als Playliste neben dem Player
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: UI
Entscheidung: Beim Abspielen von Musik stehen der Player links und die Titelliste rechts (Entwurf B), auf schmalen Bildschirmen untereinander. Die Liste zeigt Nummer, Titel, Dauer und Gesamtdauer, hebt den laufenden Titel hervor, ein Klick spielt ab. Nur Musik: Startseite, Favoriten und Suche bleiben Kacheln, Alben (Unterordner) bleiben Kacheln, alphabetische Gruppen werden in der Liste zu Zwischentiteln. Das Band „Mehr aus …“ entfällt bei Musik. Die Dauer wird per JavaScript aus den Metadaten gelesen, nacheinander nur für sichtbare Zeilen, und unter `timestampDuration-<Pfad>` gespeichert. Ohne Vorschaubild steht die Steuerleiste unten, direkt über den Knöpfen Vor/Zurück/Merken/Zufall.
Begründung: Wunsch des Nutzers (Darstellung wie bei Musikplayern), Wahl von Entwurf B. Zur Dauer der Hinweis des Nutzers, dass sie in JavaScript aus den Metadaten lesbar ist. Ausrichtung unten nach seinem Screenshot.
Alternativen: Entwurf A (Albumseite als Titelliste, nicht gewählt); Dauer über ffprobe beim „Daten einlesen“ (verworfen zugunsten JavaScript); Abspielleiste unten, die beim Seitenwechsel weiterspielt (verworfen, weil unter `file://` jeder Klick die Seite neu lädt und die Musik abbricht).
Konsequenzen: `#media` bekommt in der Musikansicht `display: grid` per Skript (`loadDecodedMedia` setzt sonst `block`). Bei sehr grossen Ordnern füllen sich die Zeiten nach und nach.
Betroffene Komponenten: `sflix_sys\js\player.js` (`renderMusicPlaylist`, `buildMusicPlaylist`, `loadNextDuration`, `markCurrentPlaylistRow`), `sflix_sys\js\pages.js` (`renderMediaContext`, `renderMediaBands`), `sflix_sys\sflix.css` (`#media.musicLayout`, `#musicPlaylist`)
Abhängigkeiten: DEC-117

### DEC-138: Anzahl abzuspielender Medien am Autoplay-Symbol
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: UI
Entscheidung: Bei aktivem Autoplay zeigt das Autoplay-Symbol in der Steuerleiste die verbleibende Anzahl, bei „Unbegrenzt“ ∞, bei ausgeschaltetem Autoplay nichts.
Begründung: Wunsch des Nutzers.
Alternativen: Keine überliefert.
Konsequenzen: Die Zahl zählt mit, weil die Anzahl mit jedem Medium heruntergezählt wird.
Betroffene Komponenten: `sflix_sys\js\player.js` (`ctrlBadge`, `updatePlayerAutoplayState`), `sflix_sys\sflix.css`
Abhängigkeiten: keine

### DEC-139: Dauer bis Übergang je Ordner mit Vererbung, Standard 0
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Architektur
Entscheidung: „Dauer bis Übergang“ wird je Ordner gespeichert (`transitionByFolder` als JSON, Schlüssel „Serien/Show“, „“ = alle Ordner). Es gilt der Wert des nächsten Ordners darüber mit eigenem Wert, sonst 0. Das Autoplay-Menü und das Autoplay-Fenster haben die Auswahl „Gilt für“; vorausgewählt ist der Ordner, von dem der Wert kommt, sonst die Ebene unter der Kategorie (Serie). Ein Knopf in der Steuerleiste (nur Video und Musik) übernimmt die verbleibende Zeit für diese Ebene. Unter Einstellungen › Wiedergabe steht eine einklappbare, bearbeitbare Übersicht; Ordner, die es nicht mehr gibt, sind markiert. Umbenennungen über `renamed.js` ziehen die Werte mit (bei zusammengeführten Ordnern bleibt der vorhandene Wert). Der Wert gehört zur Datensicherung, alte Sicherungen bleiben importierbar.
Begründung: Wunsch des Nutzers (ordnerspezifisch, einfache Vererbung, Übersicht einklappbar, Export/Import rückwärtskompatibel); Vorauswahl nach Vorschlag des Agenten, vom Nutzer bestätigt.
Alternativen: Den bisherigen Wert für alles als Standard übernehmen (Vorschlag des Agenten, vom Nutzer verworfen: „nimm Standard 0“).
Konsequenzen: Der dritte Wert in `settings` wird nicht mehr gelesen, ein früher eingestellter Wert geht verloren. Bei Musik wird weiterhin höchstens 11 s vor dem Ende gewechselt. Erweitert durch DEC-150 (Intro) und DEC-151 (Lautstärke); „Gilt für“ hat zuerst „Automatisch“, der Knopf heisst je nach Hälfte „Intro bis hier“ bzw. „Abspann ab hier“.
Betroffene Komponenten: `sflix_sys\js\core.js` (`getTransitionMap`, `setTransition`, `getTransitionFor`, `getTransitionScopes`, `getDefaultTransitionScope`), `sflix_sys\js\ui.js` (`fillTransitionFields`, `saveTransitionFromFields`, `refreshTransitionForMedia`, `renderTransitionOverview`), `sflix_sys\js\player.js` (`setTransitionFromPlayer`), `sflix_sys\js\storage.js` (`applyRenamedPaths`), `start.html`
Abhängigkeiten: DEC-135, DEC-142

### DEC-140: Symbole der Player-Leiste in der Hilfe
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: UI
Entscheidung: Die Hilfe zeigt bei Autoplay und Player die echten Symbole neben den Erklärungen. Platzhalter `<span class="helpIcon" data-icon="…">` werden beim Öffnen mit `createIcon` gefüllt.
Begründung: Wunsch des Nutzers (visuelle Hilfe); dieselben Formen wie im Player, damit Hilfe und Player übereinstimmen.
Alternativen: Keine überliefert.
Konsequenzen: Neue Symbole in `iconShapes` erscheinen automatisch, wenn die Hilfe sie nennt.
Betroffene Komponenten: `start.html`, `sflix_sys\js\pages.js` (`fillHelpIcons`), `sflix_sys\js\core.js` (`iconShapes`)
Abhängigkeiten: keine

### DEC-141: Versionen von Updater und ffmpeg, Hinweis auf neuen Updater
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Architektur
Entscheidung: Der Updater schreibt bei jedem Start und nach einer ffmpeg-Installation `sflix_sys\updater_info.js` (eigene Version, installierte ffmpeg-Version; nur bei Änderungen, nicht pushen). `release.ps1` erzeugt `versions.json` mit den neuesten Versionen von Updater und ffmpeg; STEFFLIX liest sie nur von GitHub. Die Hilfe zeigt eine Tabelle installiert/neueste für STEFFLIX, Updater und ffmpeg. Auf der Startseite erscheint der Hinweis auf einen neuen Updater nur, wenn STEFFLIX selbst aktuell ist (das STEFFLIX-Update bringt den Updater mit). Alle Update-Hinweise lassen sich unter Einstellungen › Startseite ausschalten.
Begründung: Wunsch des Nutzers (Versionen anzeigen, Hinweis auf neuen Updater, Hinweise ausblendbar). Der Browser kann die EXE nicht lesen, darum die Datei vom Updater.
Alternativen: Updater-Version in `version.js` (verworfen: der Updater würde bei jedem Updater-Release ein STEFFLIX-Update melden); Version aus der EXE im Browser lesen (nicht möglich).
Konsequenzen: Bis `versions.json` gepusht ist, zeigt die Hilfe „unbekannt“. Mit Updater 3.1.4 und älter steht „unbekannt (Updater starten)“. `versions.json` steht nicht in `checksums.json`. STEFFLIX zählt seit DEC-145 im Format x.y.z; der Updater heisst seit 4.0.0 STEFFLIX.exe (Desktop, DEC-275); versions.json enthält auch die App (DEC-206).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\Core.cs` (`UpdaterInfo`), `src\Program.cs`, `src\Ffmpeg.cs`, `SFLIX Tools\Release\release.ps1`, `start.html` (`updater_info.js`, `table.versionInfo`), `sflix_sys\js\pages.js` (`checkForUpdate`, `checkForUpdaterUpdate`, `fetchVersions`, `fillToolVersions`), `sflix_sys\js\core.js` (`compareVersions`, `getUpdaterInfo`, `versionsUrl`)
Abhängigkeiten: DEC-135

### DEC-142: Fehlerbehebung prüft statt pauschal zu löschen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Sonstiges (Lösch- und Wiederherstellungslogik)
Entscheidung: „Fehlerbehebung“ bleibt und wird ausgebaut: „Gespeicherte Daten prüfen“ zählt je Art (Favoriten, „Weiterschauen“, Abspielpositionen, Gesehen, Spieldauer, Übergänge, Playlisten) die Einträge und die verwaisten und entfernt auf Wunsch nur diese, mit Warnung, wenn alle Einträge betroffen wären. Eine Diagnose (Versionen, Browser, Speicher, Anzahl Medien, Einstellungen) lässt sich kopieren und im Feedback mitsenden (Schalter, Standard ein). Vor „Alle Daten löschen“ wird eine Sicherung angeboten. Die bisherigen Löschknöpfe stehen unter „Weitere Optionen“.
Begründung: Auf die Frage des Nutzers, ob es die Fehlerbehebung noch braucht, nannte der Agent verwaiste Einträge (Umbenennen im Explorer) als wahrscheinlichste Fehlerquelle; der Nutzer stimmte dem Ausbau zu.
Alternativen: Fehlerbehebung entfernen (nicht gewählt); nur pauschales Löschen (bisher).
Konsequenzen: Fehlt ein Laufwerk vorübergehend, wirken Einträge verwaist; der Hinweis warnt davor.
Betroffene Komponenten: `sflix_sys\js\storage.js` (`analyzeStoredData`, `removeOrphanedData`, `showDataCheck`, `confirmDeleteAllData`), `sflix_sys\js\ui.js` (`buildDiagnosisText`, `initDiagnosis`, `sendMessage`), `start.html`
Abhängigkeiten: DEC-143, DEC-139

### DEC-143: Playlisten automatisch neu erstellen, wenn sich die Medien ändern
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-28)
Kategorie: Sonstiges (Datenintegrität)
Entscheidung: Beim Start bildet STEFFLIX einen Fingerabdruck der eingelesenen Medien (Anzahl und Pfade). Ändert er sich, werden die gespeicherten Playlisten gelöscht und beim nächsten Abspielen neu erstellt. Beim ersten Start wird er nur gemerkt. Der Fingerabdruck wird beim Import einer Sicherung nicht übernommen.
Begründung: Teil des Ausbaus der Fehlerbehebung (DEC-142): Veraltete Playlisten führten Vor/Zurück zum falschen Titel.
Alternativen: Nur Löschen von Hand (bisher, bleibt als Option).
Konsequenzen: Nach jedem Einlesen mit Änderungen beginnt auch eine laufende Zufallswiedergabe neu.
Betroffene Komponenten: `sflix_sys\js\storage.js` (`getDataSignature`, `refreshPlaylistsOnDataChange`, `isMigrationFlag`)
Abhängigkeiten: DEC-142

## Verworfene Ansätze

### DEC-144: Plakate laden wird nicht übernommen
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-27)
Kategorie: Sonstiges
Entscheidung: Der Vorschlag, das Laden von Plakaten (`plakate.py`) in diese Version zu übernehmen, wurde nicht umgesetzt.
Begründung: Der Nutzer: „hat nicht immer zuverlässig funktioniert“.
Alternativen: Keine.
Konsequenzen: `plakate.py` bleibt ein eigenständiges Skript.
Betroffene Komponenten: `SFLIX Tools\plakate.py`
Abhängigkeiten: keine
Unsicherheit: Der Wortlaut des Vorschlags ist nur aus der Zusammenfassung bekannt.

## A. Offene Entscheidungen (diese Session)

- Keine fachlich offenen Entscheidungen. Alle Rückfragen wurden beantwortet.
- Nicht geprüft und damit offen, ob die Umsetzung so bleibt: Ziehen am Splitter des Protokolls, das Fenster für gespeicherte Folgenlisten in der laufenden Oberfläche, das Feedback mit Diagnose (kein gültiger Schlüssel zum Testen).

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-129 (kleinste Nummer) wurde durch DEC-133 ersetzt.

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis für die zentrale Datei) sind später überholt oder geändert:
- DEC-134: Lizenzen nutzen inzwischen ein neues Schlüsselformat (SFX2) und werden in STEFFLIX.exe eingegeben; der Abschnitt „Lizenz“ in den Einstellungen zeigt nur noch den Stand.
- DEC-139: Aus „Dauer bis Übergang“ wurden „Intro“ und „Abspann“ je Ordner; „Gilt für“ hat zuerst „Automatisch“, der Knopf heisst je nach Hälfte „Intro bis hier“ bzw. „Abspann ab hier“.
- DEC-141: Der Updater heisst heute STEFFLIX.exe (Desktop); STEFFLIX hat Versionen im Format x.y.z; `versions.json` enthält auch die Android-App.
- DEC-126: Als erste Quelle kam später serienjunkies.de hinzu.
- DEC-132: Die Knöpfe heissen seit Updater 3.4.4 „Liste aktualisieren“ und „Neu suchen“.

## C. Dokumentationsbedarf

- data-model.md: neue Schlüssel im Browser-Speicher (`displaySettings`, `transitionByFolder` mit Ordnerschlüsseln, `dataSignature`, `updaterBannerDismissed`, `timestampDuration-<Pfad>` als Spieldauer auch für die Musikliste) und neue lokale Dateien (`updater_info.js`, `folgen_cache.json`, `folgen_rueckgaengig.json`, `versions.json` nur auf GitHub).
- architecture.md: Aufgabenteilung bei Versionen (Updater schreibt `updater_info.js`, Release schreibt `versions.json`, STEFFLIX vergleicht); Vererbung der Werte je Ordner.
- testing.md: Der Vorschau-Server bzw. Browser hält das Stylesheet zwischengespeichert (neu laden mit Ctrl + F5 oder neuer `href`); die Browser-Emulation einer Fenstergrösse löst kein `resize` aus; `node` ist auf dem Rechner nicht vorhanden; Tests der Folgenlogik über eine als Bibliothek kompilierte DLL.

## D. Technische Risiken

- Die Erkennung weiterer Teile ohne „E“ (`-03`) ist eine Heuristik: Ein Titel, der mit einer kleinen Zahl beginnt, wird als Doppelfolge gelesen.
- fernsehserien.de wird als HTML ausgelesen; Änderungen am Aufbau der Seite brechen die Quelle ohne Fehlermeldung (Ausweichen auf TVmaze mit englischen Titeln).
- „Verwaiste Einträge entfernen“ löscht dauerhaft, wenn ein Laufwerk nur vorübergehend fehlt.
- Werte je Ordner hängen am Pfad; Umbenennungen ausserhalb des Updaters (Explorer) erzeugen verwaiste Einträge.
- Die Musikliste liest die Dauer jeder sichtbaren Datei einzeln; über langsame Netzwerkfreigaben kann das viele Anfragen erzeugen.
- Die Musikansicht setzt `display: grid` per Skript gegen das `display: block` aus `loadDecodedMedia`; eine Änderung dort kann die Ansicht brechen.

---

# Session STEFFLIX 2.5.0 / Updater 3.3 (Arbeit vom 29.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (STEFFLIX 2.05 → 2.5.0 → 2.5.1, Updater 3.3.0 bis 3.3.3), erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-145).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt, Hinweise darauf stehen unter B.

Einschränkung: Die Session wurde einmal zusammengefasst. Die Entscheidungen bis einschliesslich der kompakten Kacheln (DEC-145 bis DEC-160) stützen sich auf diese Zusammenfassung und sind knapper begründet, die übrigen auf den wörtlichen Verlauf. Releases baute der Nutzer selbst mit `build_release.bat` (2.5.0 / Updater 3.3.3 während der Session). Neue Dialoge wurden als Bibliothek ausserhalb des Bildschirms getestet, nicht in der gebauten EXE.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 2 |
| Implemented | 25 |
| Proposed | 1 |
| Rejected | 2 |
| Superseded | 1 |

## Versionen, Release und Changelog

### DEC-145: STEFFLIX-Versionen im Format x.y.z, alte Zahl bleibt für alte Installationen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Architektur
Entscheidung: STEFFLIX zählt wie der Updater im Format x.y.z (2.05 wird 2.5.0). `version.js` enthält zuerst die alte Zahl (x + y/100 + z/10000, z.B. 2.05 bzw. 2.0501), danach `versionName`. Die Überschriften im Changelog wurden auf x.y.z umgeschrieben. `news.json` erhält je Version `version` (alte Zahl) und `name` (x.y.z).
Begründung: Einheitliches Format mit dem Updater; Installationen bis 2.04 und Updater bis 3.2 lesen nur eine Zahl und müssen Updates weiterhin erkennen (Vorgabe des Nutzers).
Alternativen: Weiter mit Dezimalzahlen zählen; nur noch x.y.z schreiben (hätte alte Installationen gebrochen).
Konsequenzen: Die Umrechnung muss an vier Stellen gleich sein; `check_release.py` prüft, dass `versionName` der obersten Changelog-Version entspricht und die alte Zahl dazu passt.
Betroffene Komponenten: `SFLIX Tools\Release\versioning.py` (neu), `release.ps1`, `check_release.py`, `create_news.py`, `sflix_sys\version.js`, `core.js` (`toVersionName`, `getVersionName`, `parseVersionJs`), `pages.js` (Update-Hinweis), `UpdateTask.cs` (`WebVersion`, `ParseWebVersion`, `PatchNotes`)
Abhängigkeiten: DEC-141, DEC-174

### DEC-174: Changelog je Version nach Typ gruppiert, automatisch beim Release
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: `create_news.py` sortiert die Einträge jeder Version stabil nach Typ (FEATURE, INTERFACE, OPTIMIERUNG, BUGFIX, HOTFIX, BUG) und schreibt die Reihenfolge auch in `news.txt` zurück; `news.json` erhält dieselbe Reihenfolge. Der auskommentierte Block „Bekannte Fehler“ bleibt unverändert. Ein Testlauf mit anderem Ziel (`python create_news.py <news.txt> <anderes Ziel.json>`) ändert `news.json` nicht.
Begründung: Wunsch des Nutzers, gleiche Typen beieinander zu sehen. Die Gruppierung im Skript statt einmalig von Hand sorgt dafür, dass von `release.ps1` angehängte Einträge zu ihrem Typ wandern.
Alternativen: Einmaliges Umsortieren von `news.txt` ohne Skriptänderung (würde bei neuen Einträgen wieder durcheinandergeraten).
Konsequenzen: Das Skript verändert seine Quelldatei. Die Patchnotes im Updater (auch bereits installierte Versionen) und STEFFLIX übernehmen die Reihenfolge ohne neue Programmversion, sobald die neue `news.json` veröffentlicht ist. Einträge mit „Updater:“ stehen innerhalb eines Typs weiterhin gemischt mit denen von STEFFLIX.
Betroffene Komponenten: `SFLIX Tools\Release\create_news.py` (`TYPE_ORDER`, `group_by_type`), `sflix_sys\news.txt`
Abhängigkeiten: DEC-026, DEC-175

### DEC-175: Änderungen nach einem gebauten Release kommen in eine neue Version (2.5.1)
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Auf Anweisung des Nutzers wurde die Gruppierung des Changelogs als STEFFLIX 2.5.1 eingetragen: neuer Block in `news.txt`, `version.js` mit `2.0501` / `"2.5.1"` (erzeugt über `versioning.version_js_text`). Vorher wurde anhand von `news.json` geprüft, welche Einträge schon im Build von 2.5.0 enthalten waren (alle 25).
Begründung: 2.5.0 war bereits gebaut; neue Änderungen sollen nicht nachträglich in einer veröffentlichten Version stehen.
Alternativen: Weiter im Block 2.5.0 nachtragen (bisherige Regel bis zum Release).
Konsequenzen: `build_release` fragt nicht erneut nach der STEFFLIX-Version.
Betroffene Komponenten: `sflix_sys\version.js`, `sflix_sys\news.txt`
Abhängigkeiten: DEC-145, DEC-174

## Weboberfläche (STEFFLIX)

### DEC-147: Wärmerer Rotton, kein Bezug zu einem bekannten Streamingdienst
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Rot `#e8261a`, dunkel `#a81a0f` (Variante A) in Weboberfläche, Logo, Animation und Updater. Texte für Nutzer sprechen von „bekannten Streamingdiensten“ statt einen Dienst zu nennen. Der Grund der Farbänderung erscheint nicht im Changelog.
Begründung: Rechtliche Frage des Nutzers zur Ähnlichkeit mit einem bekannten Dienst; Wahl des Nutzers.
Alternativen: Weitere Farbvarianten (nicht gewählt).
Konsequenzen: Farben sind als CSS-Variable `--sflix-accent` und als `Branding.Red`/`DarkRed` festgelegt.
Betroffene Komponenten: `sflix.css`, `sflixLogo.svg`/`.png`, `splash.svg`, `Branding.cs`, `InstallForm.cs`, `news.txt`
Abhängigkeiten: DEC-019

### DEC-148: Startbildschirm mit Lichtkegel, einmal nach jedem Versionsupdate, lokales Datum
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Der Startbildschirm zeigt das Logo im Lichtkegel (Variante 2: das S zeichnet sich zuerst, der Kegel setzt zur Hälfte mit Flackern ein). Nach einem Update auf eine neue Version erscheint er beim ersten Start einmal, auch wenn „Startbildschirm überspringen“ an ist oder er heute schon kam (Merker `lastSplashVersion`, nicht Teil der Datensicherung). Tagesbezogene Prüfungen nutzen das lokale Datum.
Begründung: Wahl des Nutzers. Vorher galt kurz nach Mitternacht noch der Vortag (Fehler).
Alternativen: Andere Varianten der Animation (nicht gewählt).
Konsequenzen: `getLocalDateKey` ersetzt den bisherigen Datumsvergleich. Im Fenster von STEFFLIX.exe ersetzt durch DEC-293; seit DEC-306 nicht mehr beim Wechsel zurück zur Startseite.
Betroffene Komponenten: `splash.svg`, `pages.js` (`showSplashscreen`, `getLocalDateKey`), `storage.js` (`isExcludedFromBackup`), `sflix.css`
Abhängigkeiten: DEC-027, DEC-028, DEC-149

### DEC-150: Intro überspringen je Ordner, im selben Knopf wie der Abspann
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Videos, die von vorne beginnen, starten je Ordner einige Sekunden später (nur Videos). Der Knopf der Player-Leiste setzt in der ersten Hälfte das Intro („Intro bis hier“), in der zweiten wie bisher den Abspann („Abspann ab hier“). Beides steht auch im Autoplay-Menü und unter Einstellungen › Wiedergabe. Wird an einer gespeicherten Stelle fortgesetzt, gilt diese. „Gesehen“ bleibt bei 95 %.
Begründung: Wunsch des Nutzers; ein Knopf genügt, weil Intro und Abspann in verschiedenen Hälften liegen.
Alternativen: Eigener Knopf für das Intro; „Gesehen“ beim Abspann statt bei 95 % (vom Nutzer auf später verschoben).
Konsequenzen: Allgemeines Modell „Werte je Ordner“ (`folderValueKinds`) für Übergang, Intro und Lautstärke; Migration, Fehlerbehebung und Datensicherung decken alle Arten ab.
Betroffene Komponenten: `core.js` (`folderValueKinds`, `getFolderValueFor`, `getIntroFor`), `player.js` (`isIntroPosition`, `setTransitionFromPlayer`, `updateTransitionButton`), `ui.js` (`renderTransitionOverview`), `storage.js`
Abhängigkeiten: DEC-139, DEC-151

### DEC-151: Lautstärke je Ordner
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Die Lautstärke wird je Ordner gemerkt (Album, Staffel). Ohne eigenen Wert gilt die des Ordners darüber, sonst die zuletzt eingestellte. Teil der Datensicherung; umbenannte Ordner ziehen mit.
Begründung: Wunsch des Nutzers.
Alternativen: Eine Lautstärke für alles (bisher).
Konsequenzen: `volumechange` speichert beim Ordner.
Betroffene Komponenten: `core.js` (`getVolumeFor`, `saveVolumeFor`), `player.js`
Abhängigkeiten: DEC-150

### DEC-173: Hinweise auf updateData.exe verweisen auf den Updater
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Meldungen der Weboberfläche nennen statt `updateData.exe` den Updater (`updateStefflix.exe`) mit Reiter „Updates“ und Aufgabe „Daten einlesen“; bei nicht ladbaren Medien zusätzlich das Tool „Medien prüfen“. Alte Changelog-Einträge, Release-Skripte und Updater-Code (Umstieg alter Installationen) bleiben unverändert.
Begründung: Auftrag des Nutzers; `updateData.exe` gibt es bei aktuellen Installationen nicht mehr.
Alternativen: Keine.
Konsequenzen: Meldung „Ordner nicht gefunden“ im Vorschau-Server geprüft.
Betroffene Komponenten: `pages.js`, `player.js`, `core.js`, `news.txt`
Abhängigkeiten: DEC-055

## Lizenz, Feedback und Hilfe

### DEC-152: Neue Lizenz: ein Webhook je Lizenz, Schlüssel SFX2, Eingabe nur im Updater
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Jede Lizenz entspricht einem eigenen Webhook eines Nachrichtendienstes. Der Schlüssel (Kennung `SFX2`, 82 Zeichen in Vierergruppen) enthält Webhook-ID, Token und CRC-16, verschleiert per XOR mit einem festen xorshift32-Schlüsselstrom; kein Name, kein Ablaufdatum. Eingabe nur im Updater (Setup, Reiter „Lizenz“, `--license`), gespeichert in `sflix_sys\license.js`; STEFFLIX zeigt nur den Stand. Nachrichten „Lizenz eingelöst“ (einmal je STEFFLIX-ID) und „Lizenz entfernt“. Alte Schlüssel sind ungültig, ihre Besitzer sehen einen Hinweis. Der Dienst wird in Texten für Nutzer nicht genannt („Lizenz“, „Feedback“).
Begründung: Der bisherige Bot-Dienst kam nicht mehr in Frage (DEC-153); Senden aus `file://` wurde getestet und funktioniert. Je Lizenz ein Webhook erlaubt einzelne Sperren; nur 2–3 Lizenzen geplant. Ein Ort für die Eingabe gilt für alle Browser einer Installation.
Alternativen: Längere oder kürzere Formate, Ablaufdatum, Name im Schlüssel, öffentlicher/privater Schlüssel (alle verworfen); bisheriger Bot-Dienst (DEC-153).
Konsequenzen: Gleiches Format in `License.cs`, `ui.js` und `create_license.py`; `license.js` darf nie gepusht werden. Die Verschleierung schützt den Token nicht wirklich (siehe D).
Betroffene Komponenten: `License.cs` (`LicenseKey`, `LicenseStore`), `ui.js` (`decodeLicenseKey`, `getLicenseState`, `sendMessage`), `SFLIX Tools\Lizenz\create_license.py`, `start.html`, `InstallForm.cs`/`InstallTask.cs`, `Program.cs`
Abhängigkeiten: DEC-134, DEC-153, DEC-154

### DEC-153: Bisheriger Bot-Dienst für Lizenz und Feedback verworfen
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Lizenz und Feedback laufen nicht mehr über den bisherigen Bot-Dienst.
Begründung: Laut Zusammenfassung lieferte der Dienst einen Fehler 401 und wäre kostenpflichtig geworden.
Alternativen: DEC-152.
Konsequenzen: `create_serial.py` bleibt nur als Referenz.
Betroffene Komponenten: `SFLIX Tools\create_serial.py`
Abhängigkeiten: DEC-152
Unsicherheit: Die Gründe sind nur aus der Zusammenfassung bekannt.

### DEC-154: Updater mit Reitern „Lizenz“/„Feedback“ und „Hilfe“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Neuer Reiter „Lizenz“ (Schlüssel eingeben), nach Bestätigung „Feedback“ (Nachricht mit Diagnose und Protokoll als Anhang, Schlüssel entfernen). Reiter „Hilfe“ (auch F1) mit Themen, Suche und Befehlen der Kommandozeile. Neue Befehle `--license`, `--license-remove`, `--ffmpeg`, `--log`.
Begründung: Wunsch des Nutzers; Lizenz nur noch im Updater.
Alternativen: Keine überliefert.
Konsequenzen: Hilfe-Texte in `HelpContent.cs` sind bei jeder Änderung am Updater nachzuführen.
Betroffene Komponenten: `FeedbackPage.cs`, `HelpPage.cs`, `HelpContent.cs`, `MainForm.cs`, `Program.cs`
Abhängigkeiten: DEC-152

## Updater: Grundlagen

### DEC-146: Protokolldatei des Updaters nur auf Wunsch
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Schalter „In Datei speichern“ im Protokoll, beim Start aus und nicht gespeichert; Datei über 1 MB wird auf 256 KB gekürzt; die bisherige `updater.log` wird einmalig gelöscht; ohne Oberfläche wird immer protokolliert; nach einem Selbst-Update bleibt der Schalter über `--log` erhalten.
Begründung: Wunsch des Nutzers; die Datei wuchs unbegrenzt.
Alternativen: Immer schreiben (bisher).
Konsequenzen: „Speichern unter …“ sichert das Protokoll der Sitzung.
Betroffene Komponenten: `LogFile.cs`, `LogView.cs`, `Program.cs`
Abhängigkeiten: keine

### DEC-155: ffmpeg je Benutzer in AppData, Ordner wählbar, Setup bietet die Installation an
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Architektur
Entscheidung: ffmpeg liegt standardmässig in `%LOCALAPPDATA%\STEFFLIX\ffmpeg`, ein anderer Ordner ist unter Tools › Einstellungen wählbar (`ffmpeg.folder`). Ein bisheriges ffmpeg in `sflix_sys\ffmpeg` gilt als installiert und wird verschoben. Das Setup bietet die Installation an (Standard ein). Ist der gewählte Ordner auf einem anderen PC nicht vorhanden, gilt der Standard.
Begründung: STEFFLIX liegt oft auf einem NAS; ffmpeg gehört lokal zum Rechner.
Alternativen: ffmpeg im Installationsordner (bisher).
Konsequenzen: `FfmpegLocation.MigrateLegacy` beim Start.
Betroffene Komponenten: `Ffmpeg.cs` (`FfmpegLocation`, `InstallNow`), `InstallForm.cs`, `InstallTask.cs`, `MainForm.cs`, `Program.cs`
Abhängigkeiten: keine

### DEC-169: Dateien immer über FileOps verschieben und löschen, robust für Netzlaufwerke
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: `FileOps.Rename`/`Replace`/`MoveAcross`/`Delete`/`DeleteEmptyDirectory` entfernen bei „Zugriff verweigert“ den Schreibschutz und setzen ihn am Ziel wieder, wiederholen bei gesperrten Dateien bis zu dreimal (zusammen etwa 2,4 s) und liefern Fehlertexte mit der wahrscheinlichen Ursache. Direkte Aufrufe von `File.Delete`, `File.Move`, `Directory.Delete` in den Tools wurden ersetzt.
Begründung: Der Nutzer erhielt im Updater auf dem NAS „Zugriff verweigert“. Im Test unter `Y:\Claude` liessen sich schreibgeschützte Dateien verschieben, aber nicht löschen.
Alternativen: Fehler unverändert melden.
Konsequenzen: Ein echter Rechtefehler verzögert jede betroffene Datei um die Wiederholungen. Regel für neuen Code in CLAUDE.md und Entwickler-README.
Betroffene Komponenten: `Util.cs` (`FileOps`), `ConvertTask.cs`, `CleanupTask.cs`, `PreviewTask.cs`, `ImportTask.cs`, `EpisodeTask.cs`
Abhängigkeiten: DEC-170, DEC-171

### DEC-170: Fehlendes Recht „Löschen“ automatisch ergänzen, mit Nachprüfung
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Auf Wunsch des Nutzers („Updater soll es setzen“) ergänzt `TryGrantDelete` bei „Zugriff verweigert“ das Recht „Löschen“ für alle Regeln mit Schreibrecht (per SID, ohne Namensübersetzung), prüft danach, ob es gespeichert wurde, und meldet es über `FileOps.Notice` ins Protokoll.
Begründung: Auf `M:` fehlte in den Rechten der Dateien das Recht „Löschen“; Dateien von STEFFLIX-Nutzern liessen sich trotzdem löschen, solche, die dem NAS-Administrator gehören, vermutlich nicht.
Alternativen: Rechte im NAS korrigieren (DEC-172).
Konsequenzen: Auf dem NAS des Nutzers (openmediavault mit Linux-Rechten) meldet das Setzen Erfolg, speichert aber nichts; dank Nachprüfung gibt der Updater dort auf statt eine Änderung vorzutäuschen. Wirksam nur auf NAS, die Windows-Rechte speichern (nicht getestet).
Betroffene Komponenten: `Util.cs` (`TryGrantDelete`, `HasDeleteRight`, `Notice`), `MainForm.cs`, `Program.cs`, `HelpContent.cs`
Abhängigkeiten: DEC-169, DEC-172
Unsicherheit: Die Ursache auf `M:` (Besitzer root) ist eine Vermutung; Dateien mit Besitzer root liessen sich unter Windows nicht nachstellen.

### DEC-172: Rechte auf dem NAS serverseitig korrigieren
Status: Proposed
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Vorschlag: Im openmediavault-Webinterface unter Speicher › Freigegebene Ordner › ACL die Rechte des Freigabeordners rekursiv setzen (Gruppe `users` Lesen/Schreiben/Ausführen); der Nutzer muss Mitglied von `users` sein. „Replace“ entfernt bestehende Zusatzrechte. Alternativ per SSH `chown -R`.
Begründung: Ob man löschen oder umbenennen darf, entscheidet unter Linux das Schreibrecht auf dem Ordner; der Updater kann es auf diesem NAS nicht ändern (DEC-170).
Alternativen: DEC-170.
Konsequenzen: Wirkt auch für den Explorer. Dateien, die Dienste des NAS neu anlegen, können wieder root gehören.
Betroffene Komponenten: NAS des Nutzers (keine Dateien im Projekt)
Abhängigkeiten: DEC-170
Unsicherheit: Ergebnis nicht zurückgemeldet.

### DEC-171: Netzlaufwerke nur in ausdrücklich genannten Testordnern
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Auf `M:`, `Y:`, `Z:` arbeitet der Agent nur in Ordnern, die der Nutzer nennt (z.B. `M:\Claude`), und darunter. Kein Durchsuchen, Auflisten, Stichproben oder Abfragen von Rechten anderswo, auch nicht lesend. Für die Diagnose nach Pfad oder Protokollzeile fragen.
Begründung: Der Agent hatte bei der Fehlersuche Freigaben durchsucht; der Nutzer widersprach.
Alternativen: Keine.
Konsequenzen: Als Memory gespeichert; Testordner wurden nach den Tests entfernt. Präzisiert durch DEC-305.
Betroffene Komponenten: keine (Arbeitsweise)
Abhängigkeiten: DEC-169

### DEC-159: Keine Version für macOS
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Eine macOS-Variante wird nicht weiter verfolgt („vergiss das mal mit macOS“).
Begründung: Entscheidung des Nutzers.
Alternativen: Keine überliefert.
Konsequenzen: Der Updater bleibt ein Windows-Programm.
Betroffene Komponenten: keine
Abhängigkeiten: keine
Unsicherheit: Der Inhalt der Frage ist nur aus der Zusammenfassung bekannt.

## Updater: Oberfläche und Tabellen

### DEC-160: Kompakte Kacheln bei mehr als einer Reihe
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Höchstens vier Kacheln je Reihe; bei mehreren Reihen steht das Symbol neben dem Text (`TileButton.Compact`), Häkchen auf dem Symbol, kürzere Untertitel.
Begründung: Wunsch des Nutzers, Platz zu sparen.
Alternativen: Grosse Kacheln (bisher).
Konsequenzen: Untertitel müssen kurz bleiben, sonst werden sie abgeschnitten.
Betroffene Komponenten: `Controls.cs` (`TileButton`), `MainForm.cs`
Abhängigkeiten: keine

### DEC-161: Reiterwechsel ohne Flackern, Warte-Mauszeiger statt Spinner
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: `ShowPage` und `ShowOptionPanel` laufen in `FreezePainting` (WM_SETREDRAW, danach RedrawWindow); dabei erscheint der Warte-Mauszeiger von Windows.
Begründung: Der Wechsel blockiert die Oberfläche (Tools etwa 0,5 s), ein selbst gezeichneter Spinner würde sich nicht drehen; den Mauszeiger animiert Windows selbst.
Alternativen: Eigener Spinner (vom Nutzer gewünscht, aus diesem Grund nicht umgesetzt); Layout beschleunigen (nicht verfolgt).
Konsequenzen: Bei kurzen Wechseln ist der Mauszeiger kaum sichtbar.
Betroffene Komponenten: `MainForm.cs` (`FreezePainting`, `ShowPage`, `ShowOptionPanel`)
Abhängigkeiten: keine

### DEC-162: Grosse Ergebnislisten: Sammelaufbau, Zählung aus dem Modell, Protokoll begrenzt
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Tabellen werden mit `BeginUpdate`/`AddRange` gefüllt; Häkchen-Meldungen bei Sammelaktionen und vor `Shown` werden ignoriert; Zählungen kommen aus dem Datenmodell statt aus der ListView; eine gemeinsame fette Schrift statt einer je Zelle; Zeilen derselben Datei über ein Verzeichnis (`rowsByPath`). Mit Oberfläche protokollieren „Medien prüfen“ nur die ersten 100 Einträge und „Duplikate finden“ die ersten 50 Gruppen (ohne Oberfläche alles); mehr als 20 neue Protokollzeilen werden als ein RTF-Block angehängt. Vor grossen Dialogen erscheint „Ergebnis wird aufbereitet …“.
Begründung: Mit 3400 Einträgen dauerten Aufbau und „Alle nicht abspielbaren“ mehrere Minuten (O(n²)-Zählung, Einzelaufbau, zeilenweises Protokoll); die Duplikate brauchten beim Anzeigen 24 s.
Alternativen: Virtueller Modus der ListView (nicht nötig).
Konsequenzen: Gemessen: Aufbau etwa 1 s, Sammelauswahl 0,05 s, 3400 Protokollzeilen 0,2 s, Duplikate 1,6 s. Viele Schriften pro Zeile hätten zusätzlich die GDI-Handles erschöpfen können.
Betroffene Komponenten: `MediaCheckTask.cs`, `DuplicateTask.cs`, `CompareView.cs`, `LogView.cs`, `MainForm.cs` (`ShowPreparing`)
Abhängigkeiten: DEC-163, DEC-165

### DEC-163: Alle Tabellen per Klick auf den Spaltenkopf sortierbar
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Gemeinsame Klasse `ListSort` (Klick sortiert, nochmals umgekehrt, Dreieck im Spaltenkopf). Schlüssel kommt aus dem `Tag` der Zelle (Bytes, Sekunden, Datum), sonst natürlicher Textvergleich („Folge 2“ vor „Folge 10“). `CompareView` sortiert innerhalb der Gruppen und ordnet die Gruppen nach ihrem ersten Eintrag. `DarkList` wurde dafür in die neue Datei `Lists.cs` verschoben; die eigenen Spaltenkopf-Zeichner der Dialoge entfielen.
Begründung: Wunsch des Nutzers.
Alternativen: Keine.
Konsequenzen: Der Sorter wird nur zum Sortieren gesetzt (sonst sortiert jedes `Items.Add` neu). Neue Tabellen sollen `DarkList.Apply` und `ListSort.Add` nutzen.
Betroffene Komponenten: `Lists.cs` (neu), `CompareView.cs`, `MediaCheckTask.cs`, `EpisodeDialogs.cs`, `MergeDialog.cs`, `CleanupDialog.cs`
Abhängigkeiten: DEC-162

## Updater: Tools

### DEC-156: Vergleichstabelle bei Konflikten, Duplikaten und Folgen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: `CompareView` zeigt Dateien nebeneinander (Grösse, Auflösung, Dauer, Video/Ton, Datum), bessere Werte grün, nicht abspielbare markiert. Genutzt bei „Duplikate finden“, „Folgen umbenennen“ und der Rückfrage bei vorhandenen Dateien (auch im Importer). Fehlt ffmpeg, kann es dort installiert werden; nicht abspielbare Dateien lassen sich konvertieren.
Begründung: Wunsch des Nutzers, Grösse und Auflösung vor der Entscheidung zu sehen.
Alternativen: Nur Dateinamen anzeigen (bisher).
Konsequenzen: ffprobe läuft im Hintergrund, die Tabelle wird danach neu aufgebaut.
Betroffene Komponenten: `CompareView.cs` (`MediaFacts`, `CompareView`), `Dialogs.cs` (`ConflictDialog`), `EpisodeDialogs.cs`, `DuplicateTask.cs`, `ImportTask.cs`, `Core.cs` (`AskConflict`)
Abhängigkeiten: DEC-066, DEC-157

### DEC-157: Tool „Duplikate finden“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Findet identische Dateien (Grösse und SHA256), gleiche Folgen einer Serie (S01E01 = 1x01 = S1E1 = S01E1) und gleiche Dateinamen ohne Endung (ohne Preview- und Ablage-Ordner). Als Serie gilt der Ordner über dem Staffelordner, so bleiben Serien in Unterordnern getrennt. Spalte „Grund“ (Datei, Folge, Dateiname). Vorausgewählt bleibt die bessere Fassung; ein Häkchen gilt für alle Zeilen derselben Datei, „behalten“ hat Vorrang. Aussortiertes kommt nach `__duplikate`, Favoriten und Fortschritt gehen über `renamed.js` auf die behaltene Datei über.
Begründung: Wunsch des Nutzers; Serien in Unterordnern erschienen anfangs fälschlich als Duplikate.
Alternativen: Serie = Ordner der Datei (führte zu Fehltreffern, ersetzt).
Konsequenzen: Ein Absturz beim Schliessen (Häkchen-Meldungen während des Abbaus) wurde behoben.
Betroffene Komponenten: `DuplicateTask.cs` (`SeriesKey`, `FindSameName`, `Preselect`, `DuplicateDialog`)
Abhängigkeiten: DEC-156, DEC-158

### DEC-158: Staffelordner breiter erkennen und vereinheitlichen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: „Folgen umbenennen“ erkennt auch „staffel1“, „Season 1 HD“, „Staffel 01 (Text)“ und benennt sie in „Staffel 01“ um; Ordner mit mehreren Staffeln („Staffel 1-3“) bleiben unverändert. `IsSeasonFolder` wird auch von „Duplikate finden“ genutzt.
Begründung: Wunsch des Nutzers.
Alternativen: Strenger Ausdruck (bisher).
Konsequenzen: Erkennung per regulärem Ausdruck (siehe D).
Betroffene Komponenten: `EpisodeTask.cs` (`IsSeasonFolder`)
Abhängigkeiten: DEC-128, DEC-157

### DEC-164: Erster Umbau von „Medien prüfen“: Häkchen je Auffälligkeit, Filter, getrennte Aktionen
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Eine Zeile je Auffälligkeit mit Häkchen (gemerkt in `MediaCheckItem.Selected`), Filter nach Art mit Anzahl, Suche, Knöpfe „Alle nicht abspielbaren“, „Alle ohne Vorschaubild“, „Keine“, „Ausgewählte konvertieren“ bzw. „Vorschaubilder erstellen“ (jeweils eine Aktion).
Begründung: Vorschläge 1–4 des Agenten, vom Nutzer freigegeben.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-165; Filter und Suche blieben erhalten.
Betroffene Komponenten: `MediaCheckTask.cs`
Abhängigkeiten: DEC-165

### DEC-165: „Medien prüfen“ mit Aktionsspalten und einem gemeinsamen Ausführen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Eine Zeile je Datei (`MediaCheckFile`); die ersten drei Spalten sind Kästchen für Löschen, Konvertieren und Vorschaubild, mit Symbolen (Papierkorb, Pfeile, Bild) im Spaltenkopf. Kästchen nur, wo die Aktion möglich ist. Vorschlag: defekte und leere Dateien löschen, sonst konvertieren und/oder Vorschaubild. Löschen schliesst die anderen aus; Konvertieren und Vorschaubild gehen zusammen. „Ausführen“ erledigt alles in der Reihenfolge Löschen, Konvertieren, Vorschaubilder; Vorschaubilder entstehen für die konvertierten Dateien (`ConvertOptions.Results`). Entf/K/V schalten für markierte Zeilen, „Vorschlag“/„Keine“ für die angezeigten.
Begründung: Vorgabe des Nutzers.
Alternativen: DEC-164; Kontextmenü für Sammelaktionen (wegen fehlendem dunklem Menü nicht umgesetzt).
Konsequenzen: Zellen werden selbst gezeichnet; dafür muss `DrawItem` `DrawDefault = false` setzen und Farben je Zelle gesetzt werden. Bei nicht konvertierbaren Formaten (z.B. Bilder) bleibt nur Löschen.
Betroffene Komponenten: `MediaCheckTask.cs` (`MediaCheckFile`, `MediaCheckChoice`, `MediaCheckDialog`, `Execute`), `ConvertTask.cs` (`Results`)
Abhängigkeiten: DEC-164, DEC-166, DEC-167

### DEC-166: Löschen in „Medien prüfen“ verschiebt nach „__gelöscht“
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Gelöschte Dateien kommen nach `<Ordner>\__gelöscht`, ihr Vorschaubild nach `Preview\__gelöscht`, ausser ein anderes Medium hat denselben Namen. Vor dem Ausführen fragt der Updater einmal nach. Endgültig löscht erst „Ablagen leeren“.
Begründung: Vom Agenten gewählt, weil umkehrbar und im Einklang mit den übrigen Ablagen; der Nutzer hat nicht widersprochen.
Alternativen: Endgültig löschen.
Konsequenzen: Die Ablagen wachsen, bis sie geleert werden (siehe DEC-168).
Betroffene Komponenten: `MediaCheckTask.cs` (`DeletedFolderName`, `MoveToDeleted`)
Abhängigkeiten: DEC-122, DEC-165, DEC-168

### DEC-167: Nicht abspielbare Musik in MP3 umwandeln
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: „Konvertieren“ wandelt auf Wunsch (Schalter, Einstellung `convert.audio`, Standard ein) Musik in MP3 um (`libmp3lame -q:a 2`, Tags übernommen, Codec und Länge geprüft): Endungen aus `MediaTypes.ConvertAudio` (WMA, AIFF, APE …) sowie M4A mit anderem Codec als AAC/MP3 (z.B. ALAC). „Medien prüfen“ erkennt solche Musik und wandelt gewählte immer um; der Importer wandelt ALAC ebenfalls um.
Begründung: Auftrag des Nutzers; bisher konnte das nur der Importer.
Alternativen: Musik nur über den Importer.
Konsequenzen: M4A-Dateien werden mit ffprobe untersucht. Originale nach `__konvertiert`.
Betroffene Komponenten: `ConvertTask.cs` (`ProcessAudio`, `IsAudioReady`, `PlaceResults`), `MediaCheckTask.cs` (`IsPlayable`), `ImportTask.cs`, `MainForm.cs`, `Program.cs`
Abhängigkeiten: DEC-165

### DEC-168: „Ablagen leeren“ zeigt den belegten Speicher, berechnet im Hintergrund
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Im Reiter des Tools steht, wie viel Speicher die Ablagen belegen (Dateien, Ordner). `CleanupTask.Scan` läuft im Hintergrund beim Anzeigen des Reiters, bei geänderter Ordnerwahl und nach jedem Lauf; eine ältere Berechnung wird abgebrochen.
Begründung: Vorgabe des Nutzers; die Abfrage darf die Oberfläche nicht blockieren.
Alternativen: Anzeige nur im Bestätigungsdialog (bisher).
Konsequenzen: Auf langsamen Freigaben laufen bei häufigem Wechsel wiederholte Scans (abgebrochen, aber gestartet).
Betroffene Komponenten: `CleanupTask.cs` (`Scan`), `MainForm.cs` (`RefreshCleanupInfo`, `FolderPick.Changed`)
Abhängigkeiten: DEC-122, DEC-166

## A. Offene Entscheidungen (diese Session)

- Ob die Rechte auf `M:` nach der Korrektur im NAS (DEC-172) ausreichen, ist nicht zurückgemeldet. Der Agent bat um eine betroffene Datei in `M:\Claude` bzw. die Protokollzeile.
- Ob `TryGrantDelete` (DEC-170) bleiben soll, nachdem es auf dem NAS des Nutzers wirkungslos ist, wurde nicht erneut besprochen.
- Ob Einträge mit „Updater:“ innerhalb eines Typs getrennt von STEFFLIX stehen sollen: nur erwähnt, nicht entschieden.
- Die neuen Dialoge (Aktionsspalten, Sortierung, Speicheranzeige, Warte-Mauszeiger) sind in der gebauten EXE nicht getestet.

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-164 wurde durch DEC-165 ersetzt; die Seriendefinition in DEC-157 wurde während der Session korrigiert.

Gegenüber früheren Sessions:
- DEC-134 (Lizenz in den Einstellungen mit Eingabe): ersetzt durch DEC-152; die Einstellungen zeigen nur noch den Stand.
- DEC-139 (Dauer bis Übergang je Ordner): erweitert durch DEC-150/DEC-151 auf Intro und Lautstärke.
- DEC-066 (Rückfrage bei vorhandenen Zieldateien): ergänzt um die Vergleichstabelle (DEC-156).
- DEC-141: Versionen von STEFFLIX jetzt im Format x.y.z (DEC-145).

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis): Der Updater heisst inzwischen STEFFLIX.exe (Desktop); „Medien prüfen“ findet auch leere Ordner; „Ablagen leeren“ kann wiederherstellen; Changelog-Einträge des Programms beginnen mit „Desktop x.y.z:“.

## C. Dokumentationsbedarf

- data-model.md: `sflix_sys\license.js` (`key`, `confirmedFor`), `lastSplashVersion`, `introByFolder`, `volumeByFolder`, Einstellungen `convert.audio`, `ffmpeg.folder`; `news.json` mit `version` und `name`; Format der alten Versionszahl.
- architecture.md: Regel „Dateioperationen nur über FileOps“; `ListSort`/`DarkList` als Standard für Tabellen; Ablauf von `MediaCheckTask.Execute` (Löschen → Konvertieren → Vorschaubilder über `Results`); Gruppierung im Release (`create_news.py` schreibt `news.txt` zurück).
- testing.md: Owner-gezeichnete ListView ruft `DrawSubItem` nur, wenn `DrawItem` `DrawDefault = false` setzt; PowerShell-Skriptblöcke als Delegate aus Arbeitsthreads brechen ab (C#-Hilfsklasse als Ziel verwenden); `Select-Object -First` beendet Testskripte vorzeitig (Bild nicht aktualisiert); Konstruktor mit Liste in PowerShell braucht `New-Object Typ(,$liste)`; NAS-Tests nur in genannten Testordnern; `check_release.py` nicht mit `python -I` starten (findet Nachbarmodule nicht).

## D. Technische Risiken

- Der Lizenzschlüssel enthält den vollständigen Webhook; die XOR-Verschleierung mit festem Startwert schützt ihn nicht. Wer einen Schlüssel hat, kann Nachrichten senden.
- NAS mit Linux-Rechten speichern kein Löschrecht pro Datei; `TryGrantDelete` wirkt dort nicht. Dateien, die Dienste des NAS als root anlegen, können wieder „Zugriff verweigert“ auslösen.
- `FileOps` wiederholt bei „Zugriff verweigert“ mit Pausen; bei vielen betroffenen Dateien verlängert das Läufe deutlich.
- Die Erkennung von Staffelordnern und Folgen (S1E1, 1x01, „Staffel 01 (Text)“) ist eine Heuristik; ungewöhnliche Ordnernamen können falsch zugeordnet werden.
- `create_news.py` verändert `news.txt`; unerwartete Formate (Eintrag über mehrere Zeilen, `<li>` vor `</ul>` auf derselben Zeile) würden übersehen bzw. nicht gruppiert.
- Die alte Versionszahl (z.B. 2.0501) muss in vier Implementierungen gleich umgerechnet werden; Abweichungen führen zu falschen Update-Hinweisen bei alten Installationen.
- Owner-gezeichnete Tabellen hängen von Details der WinForms-ListView ab (Zeichnen je Zelle, Häkchen-Meldungen beim Aufbau und Abbau); Änderungen dort führten in dieser Session zu einem Absturz und zu unsichtbaren Kästchen.

---

# Session Android-App 1.0.0 / STEFFLIX 2.5.1 (Arbeit vom 29.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session (Bau der Android-App 1.0.0, Arbeiten an STEFFLIX 2.5.1, Erweiterung von `build_release`), erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-176).

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session. Spätere Änderungen sind nicht berücksichtigt, Hinweise darauf stehen unter B.

Einschränkung: Die Session ist vollständig erhalten. Der Agent konnte die App nur bauen, nicht auf einem Gerät ausführen. Was „auf dem Gerät getestet“ ist, stützt sich auf Rückmeldungen des Nutzers (z.B. „jetzt läufts“ nach dem Medienserver). Die APK für die Veröffentlichung baute der Nutzer selbst; der Agent baute nur Debug-APKs zur Prüfung, ob der Code kompiliert.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 1 |
| Implemented | 17 |
| Proposed | 2 |
| Rejected | 2 |
| Superseded | 2 |

## Android-App: Aufbau

### DEC-176: Android-App als abgespeckter Browser für STEFFLIX auf einer SMB-Freigabe
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Android
Entscheidung: Die App zeigt die unveränderte Weboberfläche von STEFFLIX in einer WebView im Vollbild, ohne Adresszeile und ohne Linkvorschau (langer Druck abgeschaltet). Sie liest alle Dateien direkt aus einer SMB-Freigabe (SMB2/3 über die Bibliothek `smbj` 0.13 mit `BCSecurityProvider` für NTLM). Fremde Links öffnen sich im System-Browser, Videos können ins Vollbild, die Zurück-Taste blättert in STEFFLIX zurück. Umgesetzt in Java mit Gradle, ohne AndroidX, minSdk 26, targetSdk 34, Paket `ch.stefflix.app`, Version 1.0.0.
Begründung: Vorgabe des Nutzers („nichts anderes als ein abgespeckter Browser“, Anmeldung bei einem SMB-Share). Die Weboberfläche bleibt die einzige Oberfläche, die App ist nur eine Hülle. Java ohne AndroidX hält die Abhängigkeiten klein (Wahl des Agenten, nicht mit dem Nutzer besprochen).
Alternativen: Android-TV-Unterstützung (abgelehnt, DEC-197).
Konsequenzen: Die App funktioniert nur im Heimnetz oder über VPN. Änderungen an der Weboberfläche wirken in der App ohne neuen Build, sobald die Dateien auf der Freigabe liegen. Die App greift über eingefügte Skripte in die Weboberfläche ein (DEC-180, DEC-182) und hängt deshalb an Namen aus STEFFLIX.
Betroffene Komponenten: `SFLIX Tools\Android\` (`settings.gradle`, `build.gradle`, `gradle.properties`, `app\build.gradle`, `AndroidManifest.xml`, `BrowserActivity.java`, `SmbSource.java`, `res\values\styles.xml`, `res\drawable-nodpi\ic_launcher.png`)
Abhängigkeiten: DEC-177, DEC-180, DEC-181, DEC-186

### DEC-177: Seite, Skripte und Bilder über abgefangene Anfragen unter einer festen Herkunft
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Architektur
Entscheidung: Die WebView lädt `https://appassets.androidplatform.net/<Freigabe-ID>/start.html`. `shouldInterceptRequest` beantwortet jede Anfrage an diese Adresse direkt aus der Freigabe, ohne Server. Alle Freigaben teilen sich diese eine Herkunft; den Speicher trennt STEFFLIX selbst über `install.js` (eigene IndexedDB je Installation). Fehlt `start.html` oder ist die Freigabe nicht erreichbar, zeigt die App eine eigene Fehlerseite mit „Erneut versuchen“.
Begründung: Eine feste Herkunft hält IndexedDB (Favoriten, Fortschritt, „Weiterschauen“) über Neustarts erhalten. Der Nutzer wollte ursprünglich keinen Webserver für STEFFLIX.
Alternativen: Alles über einen lokalen Server auf 127.0.0.1 ausliefern (verworfen, weil sich mit dem Port die Herkunft und damit der Speicher ändern würde; in DEC-180 deshalb nur für Medien).
Konsequenzen: Abgefangene Antworten speichert die WebView nicht zwischen, darum braucht die App einen eigenen Zwischenspeicher (DEC-185). Für Videos reichte der Weg nicht (DEC-178).
Betroffene Komponenten: `BrowserActivity.java` (`Client.shouldInterceptRequest`, `serve`, `errorPage`, `mimeType`)
Abhängigkeiten: DEC-090, DEC-176, DEC-178, DEC-180, DEC-185

### DEC-178: Videos und Musik über abgefangene Anfragen mit Range/206
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Android
Entscheidung: Ursprünglich wurden auch Medien über `shouldInterceptRequest` ausgeliefert, mit Auswertung des Range-Headers und Antwort 206 samt `Content-Range`.
Begründung: Kein Server nötig, ein einziger Weg für alle Dateien.
Alternativen: Interner Server (vom Nutzer früh erlaubt: „interner Speicher ist i.o.“, vom Agenten als interner Server verstanden).
Konsequenzen: MP4-Dateien mit dem Inhaltsverzeichnis (`moov`) am Dateiende starteten in der App nicht (grauer Bildschirm, nach „Play“ Fehler „verschoben“), nachgewiesen mit ffprobe an `AbdB.mp4` (`mdat` vor `moov`). Ersetzt durch DEC-180. Die genaue Ursache in der WebView blieb unbestätigt.
Betroffene Komponenten: `BrowserActivity.java` (`serve`, `parseRange`)
Abhängigkeiten: DEC-179, DEC-180

### DEC-179: Annahme „doppelter Sprung“: Strom ab Dateianfang und schnelles skip
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Android
Entscheidung: Verworfen wurde die Annahme, die WebView überspringe bei Range-Anfragen selbst bis zum Startpunkt (`InputStream.skip`), weshalb der Strom immer am Dateianfang beginnen müsse. Der Versuch (Strom ab 0, `RangeStream.skip` verschiebt nur die Position) brachte keine Besserung, „noch immer dasselbe Verhalten“.
Begründung: Test des Nutzers auf dem Gerät negativ.
Alternativen: Interner Medienserver (DEC-180, umgesetzt).
Konsequenzen: Der Strom beginnt wieder an der angefragten Stelle. Die schnelle `skip`-Methode in `RangeStream` blieb im Code, ist aber für die Lösung nicht nötig.
Betroffene Komponenten: `BrowserActivity.java` (`serve`), `SmbSource.java` (`RangeStream.skip`)
Abhängigkeiten: DEC-178, DEC-180

### DEC-180: Interner Medienserver nur auf 127.0.0.1, Umleitung der Medienadressen per Skript
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Android
Entscheidung: Videos und Musik laufen über einen kleinen HTTP-Server in der App (`MediaServer`: GET/HEAD, Range/206, `Connection: close`, ein Thread je Verbindung), gebunden nur an 127.0.0.1, zufälliger Port, zufälliger 32-stelliger Schlüssel im Pfad, Start und Ende mit `BrowserActivity`. Ein vor `main.js` in `start.html` eingefügtes Skript (`mediaHook`) setzt für `audio`/`video` die Adresse auf diesen Server; `src`, `currentSrc` und `getAttribute("src")` liefern für STEFFLIX weiterhin die ursprüngliche Adresse. Dafür HTTP nur für 127.0.0.1 erlaubt (`network_security_config.xml`) und `MIXED_CONTENT_ALWAYS_ALLOW` gesetzt. Seite und Bilder bleiben bei DEC-177.
Begründung: Ein echter HTTP-Server mit Teilanfragen verhält sich wie ein normaler Webserver; damit starten auch MP4 mit `moov` am Ende und Spulen funktioniert. Vom Nutzer bestätigt: „jetzt läufts. perfekt.“ Die Umleitung nur der Medien hält die Herkunft der Seite (DEC-177) und damit die gespeicherten Daten stabil. Die Rückgabe der ursprünglichen Adresse ist nötig, weil STEFFLIX `player.src` für die Zeitstempel-Schlüssel und `getAttribute("src")` für Vergleiche in `loadMedia` nutzt.
Alternativen: Abgefangene Anfragen (DEC-178), Strom ab Dateianfang (DEC-179), ganze Seite vom lokalen Server (Herkunft ändert sich mit dem Port).
Konsequenzen: Ausnahme von „kein Webserver“ (DEC-001), nur innerhalb der App und nur auf dem Gerät. Der Schlüssel im Pfad schützt die Freigabe vor anderen Apps auf dem Gerät. Die Umleitung greift in `HTMLMediaElement.prototype` und `Element.prototype.getAttribute`/`setAttribute` ein; setzt STEFFLIX Medienadressen künftig anders (z.B. über `<source>`), greift sie nicht. Das Skript wurde im Vorschau-Browser geprüft, der Server nur durch den Nutzer auf dem Gerät.
Betroffene Komponenten: `MediaServer.java` (neu), `BrowserActivity.java` (`mediaHook`, `injectIntoStart`, `parseRange`, `mimeType`), `res\xml\network_security_config.xml` (neu), `AndroidManifest.xml`
Abhängigkeiten: DEC-001, DEC-177, DEC-178, DEC-179

### DEC-181: Mehrere Freigaben, Eingabe als ein Pfad, Passwort im Android-Keystore
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: Die App verwaltet mehrere Freigaben (Wunsch des Nutzers: „Es dürfen mehrere wählbar sein“). Eingabe: Name, ein Feld `\\Server\Freigabe\Ordner` (auch `//…` und `smb://…`), Benutzer (leer = Gast), Passwort, Domäne. Beim Speichern prüft die App die Verbindung und ob `start.html` vorhanden ist; bei Fehlern „Ändern“, „Trotzdem speichern“ oder „Abbrechen“. Gespeichert in SharedPreferences, das Passwort mit AES-GCM und einem Schlüssel im Android-Keystore, Sicherung durch Android abgeschaltet (`allowBackup="false"`). Beim Start öffnet die App die zuletzt genutzte Freigabe; die Liste erreicht man über die Zurück-Taste, langer Druck bearbeitet oder löscht. Das Passwortfeld verdeckt die Eingabe (nach einem Fehler: `setSingleLine` nach dem Eingabetyp hob die Verdeckung auf).
Begründung: Vorgaben des Nutzers; Passwörter nicht im Klartext auf dem Gerät.
Alternativen: Getrennte Felder für Server, Freigabe und Pfad (verworfen zugunsten eines Felds, weniger Eingaben).
Konsequenzen: Wird die App neu installiert, sind die Passwörter weg (Schlüssel im Keystore). Die Liste der Freigaben ist schwer zu finden (siehe DEC-198).
Betroffene Komponenten: `ShareStore.java` (neu), `ShareListActivity.java` (neu), `SmbSource.java` (`test`), `AndroidManifest.xml`
Abhängigkeiten: DEC-176, DEC-198

## Android-App: Anzeige und Laden

### DEC-182: Startbildschirm in der App, als Ladeanimation und nur beim Öffnen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: `splash.svg` ist in der App enthalten (Kopie aus `sflix_sys` durch `build.ps1`) und wird beim Öffnen sofort in einer eigenen WebView über der Seite gezeigt, während `start.html` im Hintergrund lädt. Er verschwindet, wenn die Animation (2,9 s, Tippen überspringt) und die Seite fertig sind. Nur beim Öffnen, nicht beim Zurückkehren zur Startseite. Den Startbildschirm der Weboberfläche schaltet ein in `start.html` eingefügtes Skript ab, ausser an Tagen, an denen STEFFLIX selbst einen besonderen Startbildschirm zeigt (dann zeigt ihn die Seite und meldet sich über `StefflixApp.webSplash()`). Die App setzt dazu `window.stefflixApp = true`.
Begründung: Wunsch des Nutzers: keine Ladezeit für die Animation, Startbildschirm als Ladeanzeige, kein erneutes Abspielen beim Zurückkehren.
Alternativen: Startbildschirm aus der Freigabe laden (verursacht Ladezeit).
Konsequenzen: Die App hängt an `showSplashscreen` und dem Skript-Tag von `main.js` in `start.html`. Hintergrund des Fensters `#0b0b0d` wie der Startbildschirm.
Betroffene Komponenten: `BrowserActivity.java` (`showSplash`, `hideSplash`, `INJECT`, `injectIntoStart`, `Bridge`), `app\src\main\assets\splash.html` (neu), `assets\splash.svg`, `build.ps1`, `res\values\styles.xml`
Abhängigkeiten: DEC-027, DEC-028, DEC-149

### DEC-183: Systemschriftgrösse von Android in der App nicht übernehmen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Die WebView zeigt STEFFLIX immer mit 100 % Schriftgrösse (`setTextZoom(100)`).
Begründung: Mit vergrösserter Systemschrift schnitt die WebView die zweite Titelzeile der Kacheln mitten in den Buchstaben ab (Screenshot des Nutzers): Die Schrift wurde grösser, die festen Zeilenhöhen nicht.
Alternativen: Titel auf drei Zeilen erlauben (nur als Ausweichmöglichkeit genannt, nicht nötig).
Konsequenzen: Die Bedienungshilfe „Schriftgrösse“ wirkt in der App nicht; grössere Kacheln gibt es über Einstellungen › Darstellung (DEC-195).
Betroffene Komponenten: `BrowserActivity.java`
Abhängigkeiten: DEC-195

### DEC-184: Standard-Vorschaubild der WebView für Videos ausblenden
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: `getDefaultVideoPoster` liefert ein durchsichtiges Bild.
Begründung: Ohne eigenes Bild legt die WebView ein graues Bild mit einem nicht bedienbaren Play-Symbol über den Player (Screenshot des Nutzers).
Alternativen: keine besprochen.
Konsequenzen: keine weiteren.
Betroffene Komponenten: `BrowserActivity.java` (`Chrome.getDefaultVideoPoster`)
Abhängigkeiten: DEC-180

### DEC-185: Geladene Bilder im Arbeitsspeicher zwischenspeichern
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Bilder bis 2 MB bleiben im Arbeitsspeicher, solange die App läuft (`LruCache`, 48 MB). Der Nutzer bestätigte ausdrücklich: „ja bitte die bereits geladenen previewbilder cachen“.
Begründung: Die WebView speichert abgefangene Antworten nicht; jeder Wechsel der Rubrik las alle Vorschaubilder erneut über SMB.
Alternativen: Zwischenspeicher auf dem Gerät mit Prüfung über Änderungszeit (vom Agenten erwogen, wegen zusätzlicher SMB-Anfragen nicht umgesetzt).
Konsequenzen: Ein während der Laufzeit neu erstelltes Vorschaubild sieht die App erst nach einem Neustart. Ersetzt durch DEC-211.
Betroffene Komponenten: `BrowserActivity.java` (`imageCache`, `imageResponse`, `readAll`)
Abhängigkeiten: DEC-177, DEC-192

## Android-App: Build, Release und Repository

### DEC-186: APK mit einem Klick bauen, Werkzeuge lädt das Skript selbst
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: `SFLIX Tools\Android\build_apk.bat` startet `build.ps1`. Das Skript lädt fehlende Werkzeuge nach `%LOCALAPPDATA%\STEFFLIX-Android` (JDK 17 Temurin, Android-Cmdline-Tools mit `platforms;android-34` und `build-tools;34.0.0`, Gradle 8.7, eigenes `gradle-home`), bestätigt die SDK-Lizenzen über eine Datei mit „y“-Zeilen, kopiert `splash.svg` und baut. Beim ersten Release-Build erzeugt es einen Signaturschlüssel (`signing\stefflix.jks`, Passwort per Eingabe, in `signing\keystore.properties` im Klartext) und legt `STEFFLIX.apk` in den Release-Ordner. `-Debug` baut `dist\STEFFLIX-debug.apk`, `-SetupOnly` nur die Werkzeuge. Der Nutzer erlaubte die Installation des SDK „inkl. Abhängigkeiten“ und wünschte einen One-Click-Builder wie `build_release.bat`.
Begründung: Der Nutzer baut Releases selbst; ein Doppelklick ohne vorher installiertes Android Studio.
Alternativen: Android Studio installieren (verworfen); Lizenzen über eine Pipe in PowerShell bestätigen (funktionierte nicht, ersetzt durch die Datei).
Konsequenzen: Der Ordner `signing\` muss gesichert werden; ohne ihn lassen sich keine Updates über eine installierte App spielen. Werkzeuge (rund 1 GB) liegen ausserhalb des Projekts.
Betroffene Komponenten: `SFLIX Tools\Android\build_apk.bat` (neu), `build.ps1` (neu), `app\build.gradle` (Signatur aus `keystore.properties`)
Abhängigkeiten: DEC-082, DEC-187, DEC-188

### DEC-187: build_release baut die App als Schritt 2, mit Versionierung und Änderungserkennung
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: `release.ps1` hat einen neuen Schritt 2 „Android-App“ (die übrigen Schritte rücken auf 1–6). Ein Fingerabdruck über `app\src`, die Gradle-Dateien, `build.ps1` und `sflix_sys\splash.svg` (ohne Versionszeilen, ohne die Kopie in `assets`) wird mit `Android\release_state.json` verglichen. Die Version steht in `app\build.gradle`: `versionName` x.y.z und `versionCode` = x·10000 + y·100 + z. Vorschlag: Version im Quellcode, wenn sie höher als die bereitgestellte ist (nach einem GO erhöht), sonst bereitgestellte + 1 Patch; Enter übernimmt, eine andere Nummer ist eintippbar (ausdrücklicher Wunsch des Nutzers), Warnung bei nicht höherer Version. Bei Fehlern wird `build.gradle` zurückgesetzt. Eine vorhandene `STEFFLIX.apk` ohne gespeicherten Stand gilt als Version im Quellcode. `STEFFLIX.apk` und der Quellordner erscheinen in der Push-Liste. `-MarkReleased` merkt Updater und App. Derselbe Vorschlag aus dem Quellcode gilt seitdem auch für den Updater in Schritt 1.
Begründung: Wunsch des Nutzers („apk builder auch noch in build_release … mit versionsnummerierung und prüfung, ob sich was geändert hat“), gleiches Muster wie beim Updater. Der Vorschlag aus dem Quellcode verhindert eine doppelte Erhöhung, wenn der Agent die Version nach einem GO schon gesetzt hat (DEC-196).
Alternativen: Vorschlag immer höhere Version + 1 (hätte nach einem GO einen Patch zu hoch vorgeschlagen).
Konsequenzen: Der `versionCode` springt von 1 auf 10001 (bei 1.0.1); y und z müssen unter 100 bleiben. Die APK steht nicht in `checksums.json`. Geprüft wurden nur Syntax und die neuen Funktionen einzeln, kein ganzer Release-Lauf.
Betroffene Komponenten: `SFLIX Tools\Release\release.ps1` (`Get-AndroidFingerprint`, `Get-AndroidVersion`, `Set-AndroidVersion`, `Save-AndroidState`, Schritt 2, Übersicht), `SFLIX Tools\Android\release_state.json` (entsteht beim Lauf)
Abhängigkeiten: DEC-025, DEC-081, DEC-186, DEC-196

### DEC-188: Quellcode und APK ins Repository, Geheimnisse über .gitignore ausschliessen
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sicherheit
Entscheidung: `STEFFLIX.apk` (Release-Ordner) und der ganze Quellcode der App inklusive Build-Werkzeugen und `assets\splash.svg` gehören ins Repository. Neue `.gitignore` in der Wurzel: `sflix_sys/install.js`, `sflix_sys/license.js`, `SFLIX Tools/Lizenz/lizenzen.json`, `SFLIX Tools/Android/signing/`, die lokalen Dateien des Updaters (`renamed.js`, `updater_settings.json`, `updater_info.js`, `folgen_cache.json`, `folgen_rueckgaengig.json`, `updater.log`, `sflix_sys/ffmpeg/`), `*.old`, `*.new`, Build-Ergebnisse der App (`.gradle/`, `app/build/`, `build/`, `dist/`, `local.properties`) und `SFLIX Tools/SflixTools/obj/`. `.claude/` steht nicht darin, weil der Nutzer den Ordner nie ins Repository kopiert. `data.js` bleibt im Repository (Muster).
Begründung: Vorgaben des Nutzers („source und alles was für die app nötig ebenfalls pushen“, Quellcode und Build-Werkzeuge immer Open Source). `splash.svg` bleibt drin, damit sich die App auch ohne `build.ps1` aus dem Repository bauen lässt.
Alternativen: `.claude/` und die kopierte `splash.svg` ignorieren (erster Entwurf, auf Wunsch des Nutzers geändert).
Konsequenzen: Die `.gitignore` muss bei neuen persönlichen oder erzeugten Dateien nachgeführt werden (DEC-196). Sie wirkt nur, wenn sie im Klon unter `js/SFLIX` liegt.
Betroffene Komponenten: `.gitignore` (neu)
Abhängigkeiten: DEC-186, DEC-196

## Vorstellung der App in STEFFLIX

### DEC-189: Die App vorerst nicht im Changelog erwähnen
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Während der ersten Etappe sollte die App nicht in `news.txt` stehen („die app noch nicht in news.txt erwähnen“).
Begründung: Die App war noch nicht getestet.
Alternativen: sofort eintragen.
Konsequenzen: Ersetzt durch DEC-190 („du kannst nun übrigens auch die android app in news eintragen“).
Betroffene Komponenten: `sflix_sys\news.txt`
Abhängigkeiten: DEC-190

### DEC-190: Abschnitt „Android-App“ in den Einstellungen mit Download und Changelog-Eintrag
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Neuer Abschnitt `#android` in den Einstellungen vor „Lizenz“: kleine Vorstellung mit grünem Android-Symbol (SVG), vier Funktionspunkte, roter Knopf „App herunterladen (APK)“ mit Symbol, Link auf `STEFFLIX.apk` im GitHub-Repository (`raw/master/js/SFLIX`), kurze Installationsanleitung. In der App (`window.stefflixApp`) heisst der Knopf „Neueste Version herunterladen“ mit Text zum Aktualisieren (`initAndroidSection`). FEATURE-Eintrag im Changelog 2.5.1. README mit eigenem Abschnitt „Android-App“.
Begründung: Wunsch des Nutzers („neue rubrik in den einstellungen … android-symbol … kleine app vorstellung“).
Alternativen: einfacher Link ohne Symbol (vom Nutzer ausdrücklich nicht gewünscht).
Konsequenzen: Der Download funktioniert erst, wenn die APK gepusht ist. In der App ersetzt inject.js den Abschnitt (DEC-204).
Betroffene Komponenten: `start.html` (`#android`, `#androidDownloadButton`, `#androidDownloadText`, `#androidHint`), `sflix_sys\js\ui.js` (`initAndroidSection`), `sflix_sys\sflix.css` (`.appPromo`, `.appDownload`), `sflix_sys\news.txt`, `README.md`
Abhängigkeiten: DEC-189, DEC-191

### DEC-191: Einmalige Ankündigung der App auf der Startseite
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Beim ersten Start mit 2.5.1 zeigt die Startseite ein grünes Banner „Neu: STEFFLIX für Android“ mit „App ansehen“ (öffnet Einstellungen › Android-App) und „Ausblenden“. Die Markierung `androidAppAnnounced` wird schon beim Anzeigen gesetzt; in der App erscheint das Banner nicht.
Begründung: Wunsch des Nutzers („beim ersten starten der version 2.5.1 einmal an, damit user bescheid wissen“).
Alternativen: Banner, bis es ausgeblendet wird (vom Agenten erwogen, wegen „einmal“ nicht gewählt).
Konsequenzen: Wer die Startseite nur kurz sieht, verpasst den Hinweis. Neue Installationen sehen ihn ebenfalls einmal.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`showAndroidAppNotice`, `androidIconPath`), `sflix_sys\js\pages.js` (`showStartPage`), `sflix_sys\sflix.css` (`.updateBanner.appNotice`)
Abhängigkeiten: DEC-190

## Weboberfläche (wirkt im Browser und in der App)

### DEC-192: Ladescreen wartet auf den Aufbau der Seite und die sichtbaren Bilder
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Der Ladescreen verschwindet erst, wenn `body`-onload (`setHeight`) und der Aufbau der Seite (`markApplicationStarted` nach `startApplication`) beide eingetreten sind, in beliebiger Reihenfolge, zwei Frames später. Danach wartet er, bis alle Bilder im sichtbaren Bereich geladen sind (`getVisibleLoadingImages`, sichtbare `lazy`-Bilder werden auf `eager` gesetzt), höchstens 10 s. `#loadingScreen` ist ein fixes Overlay (z-index 9999, unter dem Startbildschirm), damit die Bilder darunter an ihrem Platz laden; Leiste und Kreise passen auf schmale Bildschirme.
Begründung: Wunsch des Nutzers (Ladescreen bis alle sichtbaren Bilder geladen sind). Die erste Fassung (nur onload, 8 s) scheiterte in der App: IndexedDB öffnet asynchron, onload kam vor dem Aufbau, es gab noch keine Bilder zu messen. Vorschaubilder laden `lazy` und zählen nicht zum onload.
Alternativen: Warten nur auf onload (erste Fassung, ersetzt); `position: sticky` im Fluss (verschob die Inhalte unter den Bildschirm).
Konsequenzen: Bei langsamer Freigabe dauert der Ladescreen bis zu 10 s. Inhalte, die nach dem Aufbau entstehen, werden nicht abgewartet. Seit DEC-306 erscheint der Ladescreen nur beim Öffnen; präzisiert durch DEC-307 (Hinweise erst danach), die Grenze von 10 s bleibt (DEC-323).
Betroffene Komponenten: `start.html` (`#loadingScreen`, `#loadingBar`), `sflix_sys\js\ui.js` (`setHeight`, `markApplicationStarted`, `hideLoadingScreenWhenReady`, `waitForVisibleImages`, `getVisibleLoadingImages`), `sflix_sys\js\main.js`
Abhängigkeiten: DEC-002, DEC-185, DEC-193

### DEC-193: Animation des Ladescreens nur mit transform und opacity
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Die Kreise pulsieren über `transform: scale` und eine Abdeckung (`::before`) mit `opacity`, statt Farbe und Schriftgrösse zu ändern.
Begründung: Animationen von Farbe und Schriftgrösse laufen nur, wenn das Skript nicht beschäftigt ist; beim Aufbau der Seite stand die Animation still (Rückmeldung des Nutzers: „keine animation“).
Alternativen: keine besprochen.
Konsequenzen: Das Aussehen bleibt fast gleich (Kreise leuchten nacheinander, Buchstaben S-T-E-F-F-L-I-X).
Betroffene Komponenten: `start.html` (`.loadingSquare`, `@keyframes loadingPulse`, `loadingCover`)
Abhängigkeiten: DEC-192

### DEC-194: Touch: Tipp auf das Video spielt ab oder pausiert
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Auf Touch-Geräten spielt ein einfacher Tipp auf das Video ab bzw. pausiert und blendet dabei die Leiste ein. Doppeltipp links/rechts spult 10 s, Doppeltipp in der Mitte schaltet das Vollbild um. Hilfe, README und Changelog angepasst.
Begründung: Rückmeldung des Nutzers („wenn man auf das videobild klickt kann man nicht pausieren/abspielen“).
Alternativen: Bisheriges Verhalten „Tipp = Leiste ein/aus“.
Konsequenzen: Gilt auch im Handy-Browser, nicht nur in der App. Ersetzt durch DEC-299.
Betroffene Komponenten: `sflix_sys\js\player.js` (`bindSurfaceEvents`), `start.html` (Hilfe), `README.md`, `sflix_sys\news.txt`
Abhängigkeiten: keine

### DEC-195: Kachelgrösse wirkt auch auf Tablet und Handy
Status: Implemented
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Klein, Mittel und Gross ergeben auf allen Breiten eine eigene Anzahl Kacheln: bis 1100 px 5/4/3, bis 800 px 4/3/2,3, bis 600 px 3,3/2,3/1,6 (Raster der Suche entsprechend). Die Regeln stehen am Ende von `sflix.css`. Text der Einstellung: „auf Computer, Tablet und Handy“. BUGFIX-Eintrag im Changelog 2.5.1. Am Computer ändert sich nichts.
Begründung: Rückmeldung des Nutzers: auf dem Telefon wirkte die Einstellung weder im Browser noch in der App, auf dem Tablet waren Klein und Mittel gleich.
Alternativen: nur in der App wirksam (`html.inApp`, nach dem Hinweis „betrifft nur die stefflix android app“ kurz umgesetzt, nach der genaueren Beschreibung des Nutzers verworfen); Option in der Mobilansicht ausblenden (vom Nutzer als zulässig genannt, nicht gewählt).
Konsequenzen: `getCardsPerRow` rundet Bruchzahlen ab (für „Kacheln pro Zeile automatisch“).
Betroffene Komponenten: `sflix_sys\sflix.css`, `start.html` (Text der Einstellung), `sflix_sys\news.txt`
Abhängigkeiten: DEC-115, DEC-183

## Arbeitsweise

### DEC-196: Regeln: Patch-Version je GO, .gitignore pflegen, Android-App mitdenken
Status: Accepted
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Sonstiges
Entscheidung: Vom Nutzer für die CLAUDE.md vorgegeben: (1) Nach dem GO die besprochenen Änderungen umsetzen, danach wieder Ideen sammeln; jede Umsetzung erhöht die Version jeder betroffenen Komponente um einen Patch (z.B. 1.1.0 → 1.1.1), Startversion nennt der Nutzer am Anfang der Unterhaltung. (2) `.gitignore` aktuell halten; Quellcode und Build-Werkzeuge sind immer Open Source. (3) Bei jeder Änderung Auswirkungen auf die Android-App prüfen und sie nachführen.
Begründung: Vorgaben des Nutzers.
Alternativen: keine.
Konsequenzen: `release.ps1` übernimmt deshalb eine bereits erhöhte Version aus dem Quellcode (DEC-187). Präzisiert durch DEC-201 (nur geänderte Komponenten), DEC-251 (auch je Build mit Korrekturen) und DEC-334 (Major/Minor/Patch, fehlgeschlagener Build behält die Version).
Betroffene Komponenten: `.claude\CLAUDE.md` (Abschnitt „Wichtige Regeln“)
Abhängigkeiten: DEC-113, DEC-187, DEC-188

## Verworfene und offene Vorschläge

### DEC-197: Keine Unterstützung für Android TV
Status: Rejected
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Android
Entscheidung: Die App wird nicht für Android TV (Bedienung mit der Fernbedienung) gebaut.
Begründung: Antwort des Nutzers: „nein androidtv ist nicht nötig“.
Alternativen: Bedienung per Fernbedienung zusätzlich umsetzen.
Konsequenzen: Die Oberfläche ist nur für Touch, Maus und Tastatur ausgelegt.
Betroffene Komponenten: keine
Abhängigkeiten: DEC-176

### DEC-198: Menüeintrag „Freigaben wechseln“ in STEFFLIX, nur in der App
Status: Superseded
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: UI
Entscheidung: Vorschlag des Agenten, im Hamburger-Menü von STEFFLIX einen nur in der App sichtbaren Eintrag zu ergänzen, der direkt zur Liste der Freigaben führt. Der Nutzer hat darauf nicht geantwortet.
Begründung: Die Liste ist nur über mehrfaches „Zurück“ erreichbar, weil die App sie beim Start überspringt.
Alternativen: Weg über die Zurück-Taste (Stand am Ende der Session).
Konsequenzen: keine (nicht umgesetzt). Ersetzt durch DEC-204.
Betroffene Komponenten: (vorgeschlagen) `BrowserActivity.java`, eingefügtes Skript
Abhängigkeiten: DEC-181

### DEC-199: „Medien prüfen“ verschiebt das MP4-Inhaltsverzeichnis an den Anfang
Status: Proposed
Datum: 2026-11-20 (Session: 2026-10-29)
Kategorie: Performance
Entscheidung: Vorschlag des Agenten: „Medien prüfen“ im Updater erkennt MP4 mit `moov` am Dateiende und verschiebt es ohne neues Kodieren an den Anfang (`-movflags +faststart`, eingebettetes Titelbild dabei entfernen). Der Nutzer hat nicht entschieden.
Begründung: Solche Dateien starten auch im Browser langsamer; „Konvertieren“ setzt `+faststart` bereits, „Medien prüfen“ hält solche Dateien aber für abspielbar.
Alternativen: nichts tun (Medienserver der App kommt damit zurecht, DEC-180).
Konsequenzen: keine (nicht umgesetzt).
Betroffene Komponenten: (vorgeschlagen) `MediaCheckTask.cs`
Abhängigkeiten: DEC-165, DEC-180

## A. Offene Entscheidungen (diese Session)

- Etappe 2 der App: weitere Anpassungen der Weboberfläche für Touch (Wischen in den Bändern, Ersatz für Hover) und das Ausblenden der Updater-Hinweise in der App. Besprochen, nicht beschlossen.
- DEC-198 (Menüeintrag „Freigaben wechseln“) und DEC-199 (faststart in „Medien prüfen“).
- Ursache des Versagens von Range über abgefangene Anfragen (DEC-178): nicht geklärt, nur umgangen.

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-178 ersetzt durch DEC-180; DEC-189 ersetzt durch DEC-190; die erste Fassung des Ladescreens (nur onload, 8 s) wurde in DEC-192 ersetzt; die nur in der App wirksame Kachelgrösse wurde in DEC-195 verworfen.

Gegenüber früheren Sessions:
- DEC-001 (kein Webserver): Ausnahme in der App für Medien, nur auf 127.0.0.1 (DEC-180).
- DEC-115 (Kachelgrösse, „Auf dem Handy bleibt sie gleich“): ersetzt durch DEC-195.
- DEC-113 (Ideen sammeln): ergänzt um Patch-Version je GO (DEC-196).
- DEC-082 (Release baut der Nutzer): gilt jetzt auch für die APK; der Agent baute in dieser Session nur Debug-APKs zur Prüfung.

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis): Der Bilder-Zwischenspeicher (DEC-185) ist durch `FileCache` (Arbeitsspeicher und Gerät) ersetzt; beim Touch-Player zeigt der erste Tipp bei ausgeblendeter Leiste seit 2.8.2 nur die Leiste (DEC-194); die Zurück-Taste schliesst die App auf der Startseite, Quellen wechselt man über „Quelle wechseln“ und die App-Einstellungen (DEC-181, DEC-198); `initAndroidSection` ist im Browser leer und wird von `inject.js` ersetzt (DEC-190); Debug-APKs baut der Agent nur noch nach Rückfrage, Test-APKs baut der Nutzer.

## C. Dokumentationsbedarf

- architecture.md: Aufbau der App (Herkunft `appassets.androidplatform.net`, abgefangene Anfragen für Seite und Bilder, `MediaServer` für Medien, eingefügte Skripte vor `main.js`); Liste der Namen aus STEFFLIX, an denen die App hängt (`showSplashscreen`, Skript-Tag von `main.js`, `#loadingScreen`, `HTMLMediaElement.src`, `getAttribute("src")`, `initAndroidSection`); Ablauf des Ladescreens (onload und Seitenaufbau, dann sichtbare Bilder).
- data-model.md: SharedPreferences `shares` (Liste, Passwort `pw` als IV:Chiffre, `last`), Markierung `androidAppAnnounced`, `versionCode`-Schema, `Android\release_state.json`.
- testing.md: WebView-Verhalten nur auf dem Gerät prüfbar; Umleitung der Medien im Vorschau-Browser mit anderer Adresse (127.0.0.1 statt localhost) testbar; der einfache Python-Server kann keine Range-Anfragen (Spulen nicht testbar); simulierter Touch über `PointerEvent` mit `pointerType: "touch"`; Browser-Zwischenspeicher beim Testen mit `fetch(…, {cache: "reload"})` umgehen; SDK-Lizenzen nur über eine Eingabedatei bestätigen.

## D. Technische Risiken

- Die App greift in Prototypen der Seite ein (`HTMLMediaElement.prototype.src`, `Element.prototype.getAttribute`/`setAttribute`) und in Funktionsnamen von STEFFLIX; Umbenennungen in der Weboberfläche können die App unbemerkt brechen.
- `MIXED_CONTENT_ALWAYS_ALLOW` und HTTP auf 127.0.0.1 sind nur durch den zufälligen Schlüssel im Pfad geschützt; jede App auf dem Gerät kann den Port erreichen.
- Das Passwort des Signaturschlüssels steht im Klartext in `signing\keystore.properties`; geht `signing\` verloren, sind keine Updates über bestehende Installationen mehr möglich.
- Passwörter der Freigaben gehen bei Neuinstallation verloren (Schlüssel im Keystore, `allowBackup="false"`).
- Der Ladescreen kann bei langsamer Freigabe bis zu 10 s stehen; Bilder ausserhalb des sichtbaren Bereichs laden weiterhin nach.
- Der Zwischenspeicher der Bilder (DEC-185) liefert bis zum Neustart veraltete Vorschaubilder.
- `versionCode` nach x·10000 + y·100 + z setzt y und z unter 100 voraus.
- `build.ps1` bestätigt die SDK-Lizenzen automatisch und lädt Werkzeuge aus festen Adressen (Temurin, Google, Gradle) ohne Prüfsumme.

---

# Session STEFFLIX 2.6.0–2.6.2 / Updater 3.4.0–3.4.4 / Android-App 1.1.0–1.1.3 (Arbeit vom 29.10. bis 31.10.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session, erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-200). Die Session umfasste fünf Umsetzungen (GO), die der Nutzer jeweils selbst mit `build_release.bat` veröffentlicht hat:

| Runde | Versionen | Inhalt |
|---|---|---|
| 1 | STEFFLIX 2.6.0, Updater 3.4.0, App 1.1.0 (veröffentlicht als 1.1.1) | App-Einstellungen, Zwischenspeicher, Videopuffer, Updates in der App; Kacheln mittig; leere Ordner in „Medien prüfen“ |
| 2 | STEFFLIX 2.6.1, Updater 3.4.1, App 1.1.2 | neuer Ladebildschirm, Dateilisten aus dem Release, Ladeansicht der App |
| 3 | Updater 3.4.2 | Tools parallel, gemerkte Prüfergebnisse |
| 4 | STEFFLIX 2.6.2, Updater 3.4.3, App 1.1.3 | Albumbilder, Ablagen wiederherstellen, „Als Kopie speichern“, serienjunkies.de, Intro-Korrektur, Galerien, Ordnerbilder, schnellere App nach Pause |
| 5 | Updater 3.4.4 | Absturz in „Ablagen leeren“, Knöpfe bei „Folgen umbenennen“ |

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session (31.10.2026). Spätere Änderungen sind nicht berücksichtigt, Hinweise darauf stehen unter B.

Einschränkungen: Ein Teil der Session lag dem Agenten nur noch als Zusammenfassung vor; die Nachrichten von Nutzer und Agent wurden für diese Auswertung aus dem gespeicherten Verlauf nachgelesen. „Implemented“ heisst: im Code umgesetzt und vom Nutzer veröffentlicht. Getestet hat der Agent die Weboberfläche im Vorschau-Browser und die Updater-Logik als Bibliothek; die App konnte er nur bauen (Runden 1 und 2) bzw. ab Runde 4 nur noch schreiben, nicht auf einem Gerät ausführen. Was auf dem Gerät funktioniert, stützt sich auf Rückmeldungen des Nutzers (Diagnosedaten, „Sieht gut aus“). Die Fenster „Albumbilder“ und „Ablagen leeren“ wurden in der Session nicht über die Oberfläche getestet.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 5 |
| Implemented | 35 |
| Proposed | 0 |
| Rejected | 5 |
| Superseded | 3 |

## Arbeitsweise

### DEC-200: Rückfragen nummeriert, Zusammenfassungen nach Komponente, vorgemerkte Ideen nennen
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Sonstiges
Entscheidung: Rückfragen sind immer fortlaufend nummeriert. Zusammenfassungen sind nach STEFFLIX, Updater und Android-App gegliedert, Komponenten ohne Änderung stehen mit „keine Änderungen“ dabei. Sagt der Nutzer in einem neuen Chat, der Agent solle die CLAUDE.md einlesen, nennt der Agent alle Punkte aus „Ideen für später“. Beim Ideensammeln gibt der Agent nicht nach jeder Runde die ganze Liste aus, sondern erst auf Wunsch oder vor dem GO. Änderungen an Release-Werkzeugen (`release.ps1`) werden unter „Updater“ eingeordnet.
Begründung: Wunsch des Nutzers: Er kann mit der Nummer antworten, ohne die Frage zu wiederholen, und braucht die Liste nur vor dem Build.
Alternativen: Eigene Rubrik „Release“ in Zusammenfassungen (vom Nutzer verneint: „das passt“ unter Updater).
Konsequenzen: Als Regeln in `.claude\CLAUDE.md` („Wichtige Regeln“) festgehalten. Der Teil „vorgemerkte Ideen nennen“ entfällt; Ideen für später werden LOW-Tickets an die Planung (DEC-338).
Betroffene Komponenten: `.claude\CLAUDE.md`
Abhängigkeiten: DEC-113 (Ideen sammeln, frühere Session), DEC-196

### DEC-201: Version nur für Komponenten mit Änderungen; Changelog im Block der aktuellen STEFFLIX-Version
Status: Implemented
Datum: 2026-11-20 (Session: 30.10. und 31.10.2026)
Kategorie: Sonstiges
Entscheidung: Ein GO erhöht die Version nur bei Komponenten, die sich wirklich ändern („solange keine Änderungen auf dem Updater stattgefunden haben, Version nicht erhöhen“). Ändert sich nur der Updater, bekommt STEFFLIX keine neue Version; die Changelog-Einträge kommen in den Block der aktuellen, schon veröffentlichten STEFFLIX-Version (Updater 3.4.2 in Block 2.6.1, Updater 3.4.4 in Block 2.6.2).
Begründung: Vorgabe des Nutzers („da keine Änderung an STEFFLIX stattfindet, braucht es keine neue Version“).
Alternativen: Neuer STEFFLIX-Block 2.6.2 nur für einen Updater-Eintrag (vom Agenten empfohlen, damit die Patchnotes den Eintrag als neu zeigen; vom Nutzer abgelehnt).
Konsequenzen: Der Updater verteilt eine geänderte `start.html` (z.B. Hilfetext) trotzdem, weil sich ihre Prüfsumme ändert. Neue Einträge in einem schon veröffentlichten Block erscheinen in den Patchnotes nicht als „neu“. Die App wurde als 1.1.1 statt 1.1.0 veröffentlicht, weil `build_release` 1.1.0 ohne Release-Stand als vergeben ansah; der Changelog wurde danach von Hand auf 1.1.1 korrigiert. Ergänzt durch DEC-251 und DEC-334.
Betroffene Komponenten: `sflix_sys\news.txt`, `sflix_sys\version.js`, `SFLIX Tools\SflixTools\src\Program.cs`, `SFLIX Tools\Android\app\build.gradle`, `.claude\CLAUDE.md`
Abhängigkeiten: DEC-196

### DEC-202: Keine Debug-APK ohne Auftrag
Status: Accepted
Datum: 2026-11-20 (Session: 30.10. und 31.10.2026)
Kategorie: Android
Entscheidung: Der Agent baut keine Debug-APK mehr, ausser der Nutzer verlangt es oder er will gezielt etwas testen. Vor einem Test mit der Debug-APK fragt er und sagt dem Nutzer genau, was auf dem Gerät zu tun ist.
Begründung: Wunsch des Nutzers. Die Debug-APK ist anders signiert; wer sie installiert, muss die veröffentlichte App deinstallieren und verliert Quellen und Daten.
Alternativen: Debug-APK bei jeder Runde zur Kompilierprüfung (bis Runde 2 so gehandhabt).
Konsequenzen: Ab Runde 4 wurde der App-Code ohne Build abgegeben; Kompilierfehler zeigen sich erst beim Release-Build des Nutzers. Ergänzt durch DEC-248 und DEC-336 (Test-Builds durch Prüfung, Release und Entwicklung).
Betroffene Komponenten: `.claude\CLAUDE.md`, `SFLIX Tools\Android\build.ps1` (`-Debug`)
Abhängigkeiten: DEC-186

### DEC-203: Verhaltenstests mit Test-DLLs nur nach Rückfrage; keine breite Ausnahme im Virenschutz
Status: Accepted
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Sicherheit
Entscheidung: Tests, bei denen eine frisch kompilierte DLL in PowerShell geladen wird und dann ffmpeg startet sowie Dateien verschiebt oder löscht, macht der Agent nur nach Rückfrage. Reines Kompilieren als `/target:library` bleibt erlaubt. Dem Nutzer wurde empfohlen, `powershell.exe` oder `claude.exe` nicht von der Erweiterten Gefahrenabwehr auszunehmen.
Begründung: Bitdefender hat solche Tests zweimal als „SuspiciousBehavior“ blockiert und dabei die ganze Kette claude.exe → cmd.exe → powershell.exe gesperrt, auch nach einer Ordner-Ausnahme, weil das Verhalten von powershell.exe bewertet wird. Danach war die Shell des Agenten bis zum Neustart gesperrt. Eine Ausnahme für PowerShell würde den Schutz für jedes PowerShell-Skript aufheben.
Alternativen: Ausnahme für `powershell.exe`/`claude.exe` (abgeraten); Ausnahme für den Temp-Ordner bzw. Scratchpad (abgeraten); Test-DLLs im Projekt unter `SFLIX Tools\SflixTools\obj\tests` (ausprobiert, half nicht).
Konsequenzen: Die Logik von Albumbildern und Wiederherstellen ist mit einem Lauf je Fall geprüft; weitere Verhaltenstests brauchen eine Rückfrage. Regel und Erfahrung stehen in `.claude\CLAUDE.md`.
Betroffene Komponenten: `.claude\CLAUDE.md`
Abhängigkeiten: keine

## Android-App: Bedienung und Einstellungen (1.1.0)

### DEC-204: App-Einstellungen als eigener Bildschirm, Menüeinträge von der App eingefügt
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Android
Entscheidung: Im Hamburger-Menü stehen in der App immer „Quelle wechseln“ und „App-Einstellungen“. Die App fügt sie selbst über ihr eingefügtes Skript ein (nach `#settingsPageToggler`, sonst am Ende des Menüs), `start.html` bleibt unverändert. „App-Einstellungen“ ist ein eigener Bildschirm der App (keine Webseite) mit den Abschnitten Quellen, Wiedergabe und Anzeige („Display nicht ausschalten“, Vollbild quer), Speicher, Verbindung testen und Über die App (Version, Update, Hilfe, Diagnose kopieren). Der Abschnitt Einstellungen › Android-App zeigt in der App einen Verweis auf die App-Einstellungen; der App-Teil in `initAndroidSection` (ui.js) wurde entfernt.
Begründung: Der Nutzer will die Einträge direkt sichtbar, ohne Suchen. Die Lösung in der App wirkt mit jeder STEFFLIX-Version auf der Freigabe (auch 2.5.x), und die Desktop-Version bleibt unberührt („die Desktopversionen benötigen das ja nicht“). Ein nativer Bildschirm funktioniert auch, wenn die Freigabe nicht erreichbar ist, und gilt für alle Quellen.
Alternativen: Einträge in `start.html` (nur ab 2.6.0 sichtbar, verworfen); Zurück-Taste auf der Startseite führt zur Quellenauswahl (vom Nutzer abgelehnt); Quellen nur über langes Drücken im Startbildschirm (bisheriger Stand).
Konsequenzen: Das Skript hängt an Namen der Weboberfläche (`settingsPageToggler`, `#settingsDD`, `#android`, `initAndroidSection`); sie sind in `inject.js` oben aufgelistet und dürfen nicht umbenannt werden. Der Update-Knopf der App wanderte von Einstellungen › Android-App in die App-Einstellungen.
Betroffene Komponenten: `SFLIX Tools\Android\app\src\main\assets\inject.js`, `SettingsActivity.java`, `BrowserActivity.java` (Brücke `openSettings`, `switchSource`), `AppPrefs.java`, `sflix_sys\js\ui.js` (`initAndroidSection`)
Abhängigkeiten: DEC-180, DEC-190, DEC-198

### DEC-205: Beim Start immer die zuletzt geöffnete Quelle, Startanimation immer
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Android
Entscheidung: Die App öffnet beim Start immer die zuletzt genutzte Quelle, ohne Schalter. Die Startanimation läuft beim Öffnen der App immer, ohne Schalter.
Begründung: Vorgabe des Nutzers.
Alternativen: Einstellung „Zuletzt genutzte Quelle öffnen / Auswahl anzeigen“ und Schalter für die Startanimation (vom Agenten vorgeschlagen, vom Nutzer abgelehnt).
Konsequenzen: Ein Hinweis in den App-Einstellungen nennt das Verhalten. Ist die Quelle nicht erreichbar, greift DEC-209.
Betroffene Komponenten: `ShareListActivity.java`, `SettingsActivity.java`
Abhängigkeiten: DEC-182, DEC-209

### DEC-206: Updates direkt in der App mit Prüfsumme
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Sicherheit
Entscheidung: Die App prüft höchstens einmal am Tag `versions.json` auf GitHub (`android.version`, `android.sha256`). Gibt es eine neue Version, erscheint ein Hinweis mit „Später“, und „App-Einstellungen“ bekommt einen roten Punkt. Die APK wird in der App geladen, gegen die SHA256-Prüfsumme geprüft und über `PackageInstaller` installiert. `release.ps1` schreibt Version und Prüfsumme der APK in `versions.json`.
Begründung: Wunsch des Nutzers („gerne gleich in App-Updates zulassen“). Die Prüfsumme stellt sicher, dass nur eine unveränderte Datei installiert wird (vom Nutzer bestätigt).
Alternativen: Knopf öffnet nur die Download-Seite im Browser (verworfen, umständlicher).
Konsequenzen: Braucht das Recht `REQUEST_INSTALL_PACKAGES` und einmal „Unbekannte Apps installieren“. Updates über die installierte App klappen nur zwischen gleich signierten Versionen; der Wechsel von einer Debug-APK zur veröffentlichten App verlangt eine Deinstallation.
Betroffene Komponenten: `AppUpdate.java`, `AndroidManifest.xml`, `SFLIX Tools\Release\release.ps1`, `versions.json`, `inject.js` (roter Punkt)
Abhängigkeiten: DEC-187, DEC-202

### DEC-207: Verbindungstest mit genauem Grund
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Android
Entscheidung: Beim Einrichten einer Quelle und in App-Einstellungen › Verbindung testen prüft die App Schritt für Schritt und nennt den Grund: Server nicht erreichbar, Anmeldung abgelehnt, Freigabe nicht gefunden, Ordner nicht gefunden, kein STEFFLIX im Ordner. Bei Erfolg zeigt sie Antwortzeit und Lesegeschwindigkeit.
Begründung: Wunsch des Nutzers („Verbindungstest beim Einrichten einer Quelle wäre auch sinnvoll“); die bisherige Prüfung meldete nur „keine start.html“ oder einen allgemeinen Fehlertext.
Alternativen: keine besprochen.
Konsequenzen: Die Werte (z.B. 68 ms Antwortzeit über VPN) dienten in der Session zur Fehlersuche.
Betroffene Komponenten: `SmbSource.java` (`test`, `reason`), `ShareEditor.java`, `SettingsActivity.java`
Abhängigkeiten: DEC-181

### DEC-208: Grössere Menüleiste und Hamburger-Menü nur in der App, Updater-Hinweise in der App aus
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: UI
Entscheidung: In der App sind die obere Menüleiste (Filme, Serien …) und der Inhalt des Hamburger-Menüs etwa anderthalbmal so gross, Tippflächen 56 px. Hinweise auf ein neues STEFFLIX oder einen neuen Updater sind in der App ausgeblendet. Beides setzt das eingefügte Skript der App um.
Begründung: Der Nutzer fand die Texte in den Menüs der App zu klein und schwer zu treffen. Update-Hinweise für STEFFLIX und den Updater bringen auf dem Handy nichts, weil man dort nicht aktualisieren kann.
Alternativen: Grössere Tippflächen in der Weboberfläche für alle Touch-Geräte (vom Agenten empfohlen, vom Nutzer abgelehnt: „nur App“).
Konsequenzen: Im Handy-Browser bleiben die Menüs klein. Die Grössen hängen an Klassennamen der Weboberfläche.
Betroffene Komponenten: `inject.js` (`checkForUpdate`/`checkForUpdaterUpdate` leer, CSS für Menüleiste und Menü)
Abhängigkeiten: DEC-204

### DEC-209: Fehlerfälle führen zurück zur Quellenauswahl bzw. in einen Dialog
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Android
Entscheidung: Ist die Quelle beim Start nicht erreichbar, bleibt die App in der Quellenauswahl, oben mit Hinweis und „Erneut versuchen“; die Fehlerseite der WebView fällt weg. Bricht die Verbindung während der Nutzung ab (auch nach 3 Fehlern in 20 s oder wenn der Medienserver aufgibt), erscheint ein Dialog „Verbindung unterbrochen“ mit „Erneut versuchen“ und „Zur Quellenauswahl“.
Begründung: Wunsch des Nutzers („mit einem Hinweis zurück zur Auswahl der STEFFLIX-Instanzen“). Während der Nutzung nur ein Dialog, damit ein kurzer Aussetzer nicht gleich aus STEFFLIX wirft (vom Nutzer bestätigt).
Alternativen: Während der Nutzung automatisch zur Quellenauswahl wechseln (verworfen).
Konsequenzen: Fortschritte gehen nicht verloren, weil STEFFLIX laufend speichert.
Betroffene Komponenten: `ShareListActivity.java` (`EXTRA_ERROR`, `EXTRA_ERROR_ID`), `BrowserActivity.java`
Abhängigkeiten: DEC-210

## Android-App: Laden und Verbindung

### DEC-210: Videopuffer im Arbeitsspeicher mit automatischem Neuverbinden
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Performance
Entscheidung: Der Medienserver der App liest laufende Videos und Musik im Hintergrund voraus, im Arbeitsspeicher, Budget wählbar 32 / 64 / 128 MB (Standard 64) für alle Anfragen zusammen. Bricht das Lesen ab, verbindet er bis 30 Sekunden lang neu und liest an derselben Stelle weiter; erst danach meldet er den Abbruch.
Begründung: Wunsch des Nutzers, dass kurze Verbindungsunterbrüche ein Video nicht sofort anhalten. Arbeitsspeicher ist schneller und nutzt den Gerätespeicher nicht ab (vom Nutzer gewählt).
Alternativen: Puffer auf dem Gerätespeicher (grössere Puffer möglich, verworfen); feste Grösse ohne Einstellung (verworfen).
Konsequenzen: `largeHeap` im Manifest; die Einstellung gilt ab dem nächsten Öffnen einer Quelle.
Betroffene Komponenten: `MediaServer.java` (`Prefetch`), `AppPrefs.java`, `SettingsActivity.java`, `AndroidManifest.xml`
Abhängigkeiten: DEC-180, DEC-209, DEC-221

### DEC-211: Zwischenspeicher mit Prüfung über Ordnerlisten, im Arbeitsspeicher und auf dem Gerät
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: Die App liest Programmdateien, `data.js` und Vorschaubilder (bis 24 MB) über einen Zwischenspeicher. Ob eine Datei noch aktuell ist, prüft sie über eine Ordnerliste (Grösse und Änderungszeit aller Dateien) statt jede Datei einzeln zu öffnen: die drei Programmordner (Wurzel, `sflix_sys`, `sflix_sys\js`) höchstens alle 30 s, übrige Ordner (z.B. `Preview`) einmal je Sitzung. Gespeichert wird im Arbeitsspeicher (40 MB, Dateien bis 8 MB) und auf dem Gerät je Quelle (wählbar 100 / 250 / 500 MB, Standard 250, älteste zuerst gelöscht). Weniger SMB-Anfragen je Datei (Grösse aus der Liste, Schliessen ohne Warten). Je Seitenaufruf misst die App Dauer, Treffer und geladene Daten für die Diagnose.
Begründung: Über VPN dauerten Seitenwechsel bis zu 20–30 Sekunden, weil jede Datei über SMB mehrere Hin- und Rückwege (30–80 ms) kostet und jeder Klick die ganze Seite mit rund 15 Programmdateien und 4,2 MB `data.js` neu lädt. Nach der Umsetzung zeigten die Diagnosedaten des Nutzers 0,1–0,5 s je Seitenwechsel.
Alternativen: Weboberfläche ohne Neuladen bei jedem Klick umbauen (vom Agenten als zu grosser Umbau nicht empfohlen, nicht umgesetzt); Auswahl 32/48/96/128 MB für einen Bildzwischenspeicher im Arbeitsspeicher (ersetzt, DEC-212).
Konsequenzen: Dateien, die nicht in der Ordnerliste stehen (z.B. andere Unicode-Schreibweise), werden direkt gelesen. Änderungen auf dem NAS werden spätestens nach 30 s bzw. beim nächsten Öffnen einer Quelle erkannt.
Betroffene Komponenten: `FileCache.java`, `LoadStats.java`, `SmbSource.java` (`list`, `closeLater`), `BrowserActivity.java` (`shouldInterceptRequest`), `SettingsActivity.java`
Abhängigkeiten: DEC-185, DEC-212, DEC-213, DEC-219

### DEC-212: Wählbarer Bildzwischenspeicher im Arbeitsspeicher
Status: Superseded
Datum: 2026-11-20 (Session: 29.10. und 30.10.2026)
Kategorie: Performance
Entscheidung: Ursprünglich geplant: Auswahl 32 / 48 / 96 / 128 MB (Standard 48) für den Bildzwischenspeicher im Arbeitsspeicher, mit „Leeren“.
Begründung: Ersetzt durch den Zwischenspeicher auf dem Gerät (DEC-211); im Arbeitsspeicher bleibt ein fester Puffer. Vom Nutzer bestätigt („ok“).
Alternativen: –
Konsequenzen: In den App-Einstellungen gibt es nur „Zwischenspeicher auf dem Gerät“ und „Zwischenspeicher leeren“. Ersetzt durch DEC-211.
Betroffene Komponenten: `FileCache.java`, `SettingsActivity.java`
Abhängigkeiten: DEC-185, DEC-211

### DEC-213: Verbindung zum NAS warm halten
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: Beim Öffnen einer Quelle baut die App die Verbindung schon während der Startanimation auf und holt die drei Ordnerlisten der Programmdateien gleichzeitig (`warmUp`). Solange die App sichtbar ist, schickt sie alle 25 s eine kleine Anfrage ans NAS (`keepAlive`) und frischt die Listen auf. Im Hintergrund läuft nichts.
Begründung: Ein einzelner Seitenaufruf mit 5,1 s ohne geladene Dateien liess auf eine eingeschlafene Verbindung schliessen (Neuanmeldung über VPN). Der Nutzer wählte diesen Weg („ja, das bitte umsetzen“). Danach zeigten die Diagnosedaten nur noch 0,2 s je Seitenaufruf.
Alternativen: Ordnerlisten nach dem Anzeigen im Hintergrund prüfen (vom Agenten zuerst vorgeschlagen, dann als weniger wirksam eingeschätzt; später in 1.1.3 als DEC-219 doch umgesetzt).
Konsequenzen: Die Wiederaufnahme nach einer Pause (zuerst: alte Verbindung prüfen) wurde in 1.1.3 durch DEC-218 ersetzt. Die Wiederaufnahme nach einer Pause regelt DEC-218.
Betroffene Komponenten: `BrowserActivity.java` (`keepAlive`, `refreshConnection`, Thread-Pool `background`), `SmbSource.java` (`keepAlive`), `FileCache.java` (`warmUp`)
Abhängigkeiten: DEC-211, DEC-218

### DEC-214: Diagnose der App fliesst in die Diagnose von STEFFLIX und ins Feedback
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Sonstiges
Entscheidung: Läuft STEFFLIX in der App, hängt `buildDiagnosisText` die Angaben der App an (App-Version, Gerät, Android-Version, Quelle ohne Passwort, Zwischenspeicher, letzte Ladezeiten über `StefflixApp.diagnosis()`); damit landen sie im Feedback-Anhang. Zusätzlich gibt es „Diagnose kopieren“ in den App-Einstellungen. Die Hilfe von STEFFLIX bekommt einen Abschnitt „Android-App“.
Begründung: Vom Nutzer bestätigt; bei Problemen kommt alles in einer Nachricht. Am PC ändert sich nichts.
Alternativen: Diagnose nur über den Knopf in den App-Einstellungen.
Konsequenzen: Mit älteren STEFFLIX-Versionen auf der Freigabe gibt es die App-Angaben nur über den Knopf.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`buildDiagnosisText`), `SettingsActivity.java` (`diagnosis()`), `BrowserActivity.java` (Brücke), `start.html` (Hilfe `#androidHilfe`), `README.md`
Abhängigkeiten: DEC-211

### DEC-215: Ladeansicht im Startbildschirm der App beim ersten Laden
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: Lädt die App nach der Startanimation (2,9 s) noch Dateien vom NAS, wechselt der Startbildschirm zur Ladeansicht (Logo mit Ring, Plakatwand) mit einfachen Statustexten: „Verbinde mit …“, „Lade Programmdateien“, „Lade Medienverzeichnis n %“, „Lade Vorschaubilder n / m“. Der Startbildschirm bleibt, bis STEFFLIX seinen Ladebildschirm ausgeblendet hat (höchstens 25 s nach `onPageFinished`), es gibt also nur einen Ladebildschirm. Nach 15 s erscheint „Quelle wechseln“. Kommt alles aus dem Zwischenspeicher, sieht man nur die Animation.
Begründung: Wunsch des Nutzers, beim ersten Start über VPN zu sehen, was geladen wird. Anzeige nur, wenn Dateien vom NAS kommen und es länger als die Animation dauert (Vorgabe des Nutzers). Einfache Texte reichen; Details stehen in der Diagnose.
Alternativen: Statuszeile immer von Anfang an (verworfen); Angaben mit Dateinamen und MB (verworfen).
Konsequenzen: Funktioniert mit jeder STEFFLIX-Version, weil die App das Ausblenden von `#loadingScreen` beobachtet. Neue Brückenfunktion `loadingProgress`, `FileCache.Listener` für den Fortschritt von `data.js`.
Betroffene Komponenten: `BrowserActivity.java` (`updateSplashStatus`), `assets\splash.html`, `assets\loading_backdrop.jpg`, `inject.js` (`webReady`), `FileCache.java` (`Listener`), `sflix_sys\js\ui.js` (`showLoadingProgress` meldet `StefflixApp.loadingProgress`)
Abhängigkeiten: DEC-182, DEC-222, DEC-223

### DEC-216: Kein Puffern im Browser erzwingen
Status: Rejected
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Performance
Entscheidung: Ein eigener Videopuffer wie in der App wird für den Browser nicht gebaut.
Begründung: Unter `file://` darf eine Seite Dateien weder per fetch noch per XHR einlesen. Der Player setzt bereits `preload="auto"`; wie weit vorausgeladen wird, entscheidet der Browser.
Alternativen: Weiterspielen nach einem Abbruch (DEC-217, umgesetzt).
Konsequenzen: Im Browser stockt ein Video bei einem Abbruch kurz und läuft dann über DEC-217 weiter.
Betroffene Komponenten: –
Abhängigkeiten: DEC-001, DEC-217

## Weboberfläche: Wiedergabe

### DEC-217: Weiterspielen nach einem Verbindungsabbruch
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Synchronisation
Entscheidung: Lief eine Datei schon und meldet der Player einen Fehler oder hängt er länger als 15 s, lädt STEFFLIX die Datei neu, springt an die letzte Stelle (`sflixLastTime`) und spielt weiter. Versuche alle 4 s bis 30 s, danach ein Hinweis. Beim Neuladen werden Zeitstempel und Intro nicht erneut angewendet (`sflixResume`). Gilt im Browser und in der App.
Begründung: Vom Nutzer gewünscht („ja, kannst du machen“) als Ersatz für das im Browser unmögliche Puffern.
Alternativen: DEC-216.
Konsequenzen: Ob der Sprung an die Stelle klappt, liess sich im Testserver nicht prüfen (keine Range-Anfragen); unter `file://` und in der App erwartet.
Betroffene Komponenten: `sflix_sys\js\player.js` (`initConnectionRecovery`, `recoverConnection`), `start.html` (Hilfe), `README.md`
Abhängigkeiten: DEC-216, DEC-210

## Android-App: Nach einer Pause (1.1.3)

### DEC-218: Nach einer Pause die alte Verbindung sofort verwerfen
Status: Implemented
Datum: 2026-11-20 (Session: 30.10. und 31.10.2026)
Kategorie: Android
Entscheidung: Kommt die App nach einer Pause von mehr als 20 s zurück (Sperrbildschirm, andere App), verwirft sie die alte SMB-Verbindung sofort, ohne auf das NAS zu warten (`dropConnection`: Felder sofort leeren, Schliessen mit `close(true)` im Hintergrund), und baut im Hintergrund eine neue auf (`reconnectAfterPause`).
Begründung: Nach dem Sperrbildschirm tippte der Nutzer auf eine Kachel und es passierte lange nichts. Ursache: Die App prüfte die meist tote Verbindung mit einer Anfrage und wartete bis zum Zeitlimit von 20 s; Seitenaufrufe standen in derselben Warteschlange.
Alternativen: Alte Verbindung erst prüfen (bisher, DEC-213).
Konsequenzen: Ein Video, das vor der Pause im Puffer lief, verbindet über DEC-210 neu. Auf dem Gerät in der Session nicht getestet.
Betroffene Komponenten: `SmbSource.java` (`dropConnection`, `reconnect`), `BrowserActivity.java` (`onResume`, `reconnectAfterPause`)
Abhängigkeiten: DEC-213, DEC-219

### DEC-219: Programmdateien sofort aus dem Zwischenspeicher, Prüfung im Hintergrund
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Performance
Entscheidung: Gibt es für einen Programmordner schon eine Ordnerliste, nutzt die App sie sofort, auch wenn sie älter als 30 s ist, und holt die neue Liste im Hintergrund (stale-while-revalidate, höchstens eine Prüfung je Ordner gleichzeitig). Liegt eine Datei nicht im Zwischenspeicher und ist die Liste veraltet, liest die App sie direkt mit der echten Grösse (nicht mit der Grösse aus der alten Liste).
Begründung: Teil der Lösung für den Hänger nach dem Sperrbildschirm; der Ladebildschirm erscheint sofort, nur Vorschaubilder warten auf die neue Verbindung. Vom Nutzer zusammen mit DEC-218 gewählt („geht eine Kombination aus beiden?“ – „ja, beides umsetzen“).
Alternativen: Immer synchron prüfen, wenn die Liste älter als 30 s ist (bisher).
Konsequenzen: Eine Änderung auf dem NAS (z.B. nach „Daten einlesen“ oder einem Update) sieht man erst beim übernächsten Seitenwechsel; dafür gibt es DEC-220. Innerhalb eines Seitenaufrufs können Programmdateien aus unterschiedlichen Ständen kommen, wenn die Ordner getrennt neu geprüft werden.
Betroffene Komponenten: `FileCache.java` (`lookup`, `Found`, `revalidate`, `readDirect`)
Abhängigkeiten: DEC-211, DEC-218, DEC-220

### DEC-220: Knopf „STEFFLIX-Dateien neu laden“
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Android
Entscheidung: In App-Einstellungen › Speicher (nur aus einer geöffneten Quelle) vergisst „STEFFLIX-Dateien neu laden“ die Ordnerlisten sowie Arbeitsspeicher- und Gerätekopien der Programmdateien dieser Quelle, leert den Cache der WebView und lädt die Seite neu. Vorschaubilder bleiben.
Begründung: Wunsch des Nutzers, die Skripte manuell neu vom NAS zu holen; Ausgleich für die verzögerte Erkennung durch DEC-219.
Alternativen: „Zwischenspeicher leeren“ (löscht auch alle Vorschaubilder aller Quellen).
Konsequenzen: Neues Ergebnis `SettingsActivity.RESULT_RELOAD`.
Betroffene Komponenten: `SettingsActivity.java`, `BrowserActivity.java` (`onActivityResult`), `FileCache.java` (`forgetProgramFiles`)
Abhängigkeiten: DEC-219

## Weboberfläche: Darstellung

### DEC-221: Kacheln bei „automatisch“ zentrieren
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: Ist die Anzahl Kacheln pro Zeile auf „automatisch“ gestellt, stehen die Zeilen mittig mit gleichem Abstand links und rechts; die Kacheln behalten ihre Grösse (Klasse `autoRows`, `--auto-cards`).
Begründung: Auf Handy und Tablet blieb rechts der Platz für die angeschnittene Kachel des normalen Bandes leer (z.B. 16 px links, 62 px rechts). Der Nutzer wählte Zentrieren.
Alternativen: Kacheln strecken, bis die Zeile aufgeht (vom Agenten empfohlen, vom Nutzer abgelehnt).
Konsequenzen: Breitere Ränder auf dem Handy (39 px je Seite). Die Rasteransicht war schon symmetrisch und blieb unverändert. Wirkt auch in der App.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderGroupedBands`, `getCardsPerRow`), `sflix_sys\sflix.css` (`.cardRow.autoRows .rowTrack`)
Abhängigkeiten: DEC-195

### DEC-222: Ladebildschirm mit Ring um das STEFFLIX-Logo
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: Der Ladebildschirm zeigt das STEFFLIX-Logo mit einem roten Ring, darunter den Zähler der Vorschaubilder („5 / 8 Vorschaubilder“). Er erscheint erst nach 0,4 s, schnelle Seitenwechsel zeigen also nur den dunklen Grund. Mit „Animationen reduzieren“ (bzw. `prefers-reduced-motion`) stehen Ring und Hintergrund still, nur der Zähler läuft. Dieselbe Gestaltung nutzt die Ladeansicht der App (DEC-215).
Begründung: Der alte Ladebildschirm passte nicht mehr zum Design (Nutzer). Gewählt wurde Variante D, aber mit dem Logo statt eines Play-Symbols.
Alternativen: A (Logo mit durchlaufendem Balken, Empfehlung des Agenten), B (Schriftzug als Welle), C (Skelett der Seite).
Konsequenzen: Animation nur mit transform/opacity, damit sie bei beschäftigtem Skript läuft. Das Verhalten des Rings wurde in 2.6.2 geändert (DEC-224).
Betroffene Komponenten: `start.html` (`#loadingScreen`, `#loadingRing`, `#loadingCount`), `sflix_sys\js\ui.js` (`showLoadingProgress`), `SFLIX Tools\Android\app\src\main\assets\splash.html`
Abhängigkeiten: DEC-192, DEC-193, DEC-223, DEC-224, DEC-225

### DEC-223: Hintergrund des Ladebildschirms als festes Bild erfundener Plakate, eingebettet
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: Hinter dem Logo liegt eine stark unscharfe, schräge Plakatwand aus erfundenen Film-, Serien- und Musikplakaten (Unschärfe-Stufe B), etwa 9 KB, direkt in `start.html` eingebettet. Sie gleitet sehr langsam und steht bei reduzierten Animationen still. Das Bild erzeugt das Skript `SFLIX Tools\Bilder\ladehintergrund.py` (feste Zufallszahl), das es auch als `loading_backdrop.jpg` für die App schreibt.
Begründung: Der Nutzer wollte einen Hintergrund im Stil eines Rasters aus Vorschaubildern, aber ohne Bilder aus der Mediathek und mit festem Effekt. Echte Plakate kommen wegen des Urheberrechts nicht in Frage. Eingebettet steht das Bild sofort da und brauchte keine neue Datei und damit keinen neuen Updater.
Alternativen: roter Schein per CSS, Logo als Wasserzeichen, zufällige echte Vorschaubilder aus `data.js` (alle verworfen); Unschärfe A (verworfen).
Konsequenzen: Die scharfen Vorschaubilder des Skripts stehen in `.gitignore`. Neue Plakatwand nur über das Skript.
Betroffene Komponenten: `start.html` (`#loadingBackdrop`, Bereich `/* ladehintergrund:start */ … /* ladehintergrund:ende */`), `SFLIX Tools\Bilder\ladehintergrund.py`, `SFLIX Tools\Android\app\src\main\assets\loading_backdrop.jpg`, `.gitignore`
Abhängigkeiten: DEC-222, DEC-226

### DEC-224: Ring dreht immer, Bogen wächst mit dem Fortschritt
Status: Implemented
Datum: 2026-11-20 (Session: 30.10. und 31.10.2026)
Kategorie: UI
Entscheidung: Der Ring dreht sich, solange geladen wird. Sobald der Fortschritt bekannt ist, wird der rote Bogen mit dem Fortschritt länger, ist aber nie kürzer als ein Viertel. Gilt im Browser, in der App und in der Ladeansicht der App.
Begründung: Bei „Vorschaubilder 0 / 10“ stand ein winziger Bogen still und wirkte eingefroren; danach kamen alle Bilder fast gleichzeitig. Vom Nutzer als Kompromiss angenommen.
Alternativen: Immer nur ein drehendes Rädchen ohne Fortschritt (vom Nutzer als Option genannt); Fortschritt mit stillstehendem Ring (DEC-225, ersetzt).
Konsequenzen: CSS-Regel, die das Drehen bei `determinate` stoppte, entfernt; Mindestanteil 0,25 in `showLoadingProgress` und `splash.html`.
Betroffene Komponenten: `start.html`, `sflix_sys\js\ui.js` (`showLoadingProgress`), `SFLIX Tools\Android\app\src\main\assets\splash.html`
Abhängigkeiten: DEC-222, DEC-225

### DEC-225: Ring hört auf zu drehen, sobald der Fortschritt bekannt ist
Status: Superseded
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: In 2.6.1 drehte sich der Ring, bis die Zahl der Vorschaubilder bekannt war, und füllte sich dann still (Klasse `determinate`).
Begründung: Ursprünglich vom Agenten empfohlen und vom Nutzer angenommen, um den Fortschritt zu zeigen.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-224 (2.6.2).
Betroffene Komponenten: `start.html`, `sflix_sys\js\ui.js`, `splash.html`
Abhängigkeiten: DEC-224

## Updater: Verteilung der Web-Dateien

### DEC-226: Programmdateien und veraltete Dateien kommen aus dem Release (`checksums.json`)
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Sicherheit
Entscheidung: Ab Updater 3.4.1 stehen die Liste der Programmdateien (`_webFiles`) und die Liste veralteter, bei Nutzern zu löschender Dateien (`_obsoleteFiles`) in `checksums.json`. `create_checksums.py` schreibt sie aus `sflix_update.py` (`WEB_FILES`, `OBSOLETE_FILES`). Der Updater nimmt sie von dort, mit der eingebauten Liste als Rückfall. Schutzregeln (`IsAllowedWebPath`): nur `start.html` und Dateien in `sflix_sys` ohne `ffmpeg`, keine `..`, Laufwerke oder Backslashes, nur Web-Endungen, nie geschützte persönliche Dateien (`data.js`, `install.js`, `license.js`, `renamed.js`, `updater_info.js`, Einstellungen, Protokolle) und nie Medien; eine aktuelle Programmdatei wird nie als veraltet gelöscht.
Begründung: Wunsch des Nutzers, dass eine neue Datei der Weboberfläche keinen neuen Updater mehr braucht. Die Schutzregeln verhindern, dass ein manipuliertes oder fehlerhaftes `checksums.json` fremde oder persönliche Dateien überschreibt oder löscht. Auch die Löschliste kommt aus dem Release (vom Nutzer ausdrücklich gewünscht).
Alternativen: Feste Liste in `Net.WebFiles` (bisher; jede neue Datei braucht einen neuen Updater).
Konsequenzen: Ältere Updater lesen nur die Prüfsummen und übergehen die neuen Einträge. Neue oder entfallene Web-Dateien müssen in `sflix_update.py` eingetragen werden.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\Net.cs` (`WebFilesFor`, `ObsoleteFilesFor`, `IsAllowedWebPath`, `ProtectedFiles`), `SFLIX Tools\Release\create_checksums.py`, `SFLIX Tools\Release\sflix_update.py`, `checksums.json`
Abhängigkeiten: DEC-223

## Weboberfläche: Weiterschauen, Ordnerbilder, Intro

### DEC-227: Bildergalerien in „Weiterschauen“ ab dem 5. Bild
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Ein Bildordner erscheint in „Weiterschauen“, sobald man darin mindestens bis zum 5. Bild geblättert hat (höchste erreichte Bildnummer, `galleryProgress`). Die Kachel zeigt den Ordnernamen, das zuletzt angesehene Bild und „Bild x von y“ mit Balken; ein Klick öffnet dieses Bild. Nach dem letzten Bild verschwindet die Galerie wieder. „Weiterschauen leeren“ löscht auch `galleryProgress`.
Begründung: Wunsch des Nutzers, Galerien fortsetzen zu können. Fester Wert statt Prozent, weil man bei grossen Galerien sonst sehr weit blättern müsste. Höchste Bildnummer statt Zählen jedes Bildes, weil eindeutiger beim Springen und sparsamer.
Alternativen: Ab 20 % der Galerie (DEC-228, ersetzt); Zählen jedes angesehenen Bildes (verworfen); Anzeige schon ab dem ersten Bild (verworfen).
Konsequenzen: Galerien mit höchstens 5 Bildern erscheinen nie. Gruppe in „Weiterschauen“ ist bei Bildern der Bildordner, bei Videos weiterhin Kategorie/Serie.
Betroffene Komponenten: `sflix_sys\js\player.js` (`galleryMinImages`, `getGalleryPosition`, `updateGalleryProgress`, `setLastPlayed`, `removeLastPlayedGroup`), `sflix_sys\js\pages.js` (`showContinueWatching`), `sflix_sys\js\storage.js` (`modifyLocalData`)
Abhängigkeiten: DEC-228

### DEC-228: Galerie in „Weiterschauen“ ab 20 % angesehen
Status: Superseded
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Erste Vorgabe des Nutzers: Eine Galerie erscheint, wenn mindestens 20 % angesehen sind.
Begründung: Vom Nutzer wenige Minuten später korrigiert.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-227 (ab dem 5. Bild).
Betroffene Komponenten: –
Abhängigkeiten: DEC-227

### DEC-229: Ordner ohne Vorschaubild nehmen ein festes Bild aus tieferen Preview-Ordnern
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Hat ein Ordner kein eigenes Vorschaubild und kein Bild direkt darin, sucht STEFFLIX ohne Tiefenbegrenzung in den Unterordnern (Breitensuche, nächstgelegene zuerst) und nimmt ein Bild aus dem ersten `Preview`-Ordner mit Bildern. Welches Bild, bestimmt ein Hash des Ordnerpfads, also bei jedem Aufruf dasselbe.
Begründung: Ordner ohne Bild blieben leer, weil bisher nur nach oben gesucht wurde. Das Bild bleibt gleich, damit die Kacheln nicht bei jedem Seitenwechsel wechseln und der Zwischenspeicher der App wirkt (vom Nutzer bestätigt).
Alternativen: Bei jedem Aufruf ein anderes zufälliges Bild (verworfen).
Konsequenzen: Bei grossen Bäumen sucht die Kachel einmal durch den Datenbaum.
Betroffene Komponenten: `sflix_sys\js\cards.js` (`createFolderCard`, `getPreviewFromSubfolders`)
Abhängigkeiten: DEC-211

### DEC-230: Intro und Abspann: Menü zeigt den geltenden Wert und speichert dort, wo er herkommt
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Das Autoplay-Menü zeigt bei Intro und Dauer bis Übergang den Wert, der für das Video wirklich gilt, mit Herkunft (z.B. „Aus «Serien › Show › Staffel 1»“). Intro und Dauer haben je eine eigene Vorauswahl: Neu steht „Automatisch“ an erster Stelle von „Gilt für“ und speichert jeden Wert bei dem Ordner, von dem der geltende Wert stammt, sonst bei der Serienebene. „Intro bis hier“ und „Abspann ab hier“ speichern ebenso. Wählt man eine höhere Ebene, während darunter ein eigener Wert gilt, erscheint ein Hinweis mit „Für Unterordner übernehmen“, der die abweichenden Werte darunter entfernt.
Begründung: Fehler gemeldet vom Nutzer (Menü 6 s, gesprungen ~20 s). Ursache: Eine gemeinsame Auswahl „Gilt für“ richtete sich nach der Herkunft der Dauer bis Übergang; das Intro-Feld zeigte und speicherte deshalb auf der falschen Ebene, während eine Staffel mit eigenem Wert Vorrang hatte. Die Vererbung selbst war korrekt.
Alternativen: keine besprochen.
Konsequenzen: Neue Funktionen `getDefaultScopeFor`, `getOverridingValue`, `clearFolderValuesBelow`; `getDefaultTransitionScope` nutzt `getDefaultScopeFor`.
Betroffene Komponenten: `sflix_sys\js\core.js`, `sflix_sys\js\player.js` (`getMenuSaveScope`, `updateMenuValues`, `refreshPlayerMenu`, `setTransitionFromPlayer`), `sflix_sys\js\ui.js` (`renderScopeOverrideHint`, `updateSettingsBoxScopeHint`), `sflix_sys\js\main.js`, `start.html` (`#autoplayScopeHint`), `sflix_sys\sflix.css`
Abhängigkeiten: keine

## Updater

### DEC-231: „Medien prüfen“ findet leere Ordner
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Sonstiges
Entscheidung: Ab Updater 3.4.0 meldet „Medien prüfen“ Ordner ohne Medien, auch nicht in Unterordnern. Vorschaubilder, Ablagen (`__…`), `Thumbs.db`, System- und versteckte Ordner zählen nicht. Gemeldet wird nur der oberste solche Ordner, nie der gewählte Ordner selbst und nie eine Kategorie direkt im Installationsordner. Ordner lassen sich nur löschen: Sie kommen als Ganzes nach `__gelöscht` im Ordner darüber (mit `IsInsideRoot`). Vorgemerkt sind nur Ordner ohne andere Dateien als Vorschaubilder.
Begründung: Vom Nutzer gewünscht („ja, kannst du auch gleich umsetzen“). Ordner mit Dokumenten oder Untertiteln sollen nicht versehentlich gelöscht werden.
Alternativen: keine besprochen.
Konsequenzen: Erste Updater-Änderung dieser Session, darum Updater 3.4.0 (vorher 3.3.3, DEC-201).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\MediaCheckTask.cs` (`FindEmptyFolders`, `ScanFolder`, `MoveFolderToDeleted`, `MediaCheckDialog`)
Abhängigkeiten: DEC-201

### DEC-232: Tools arbeiten parallel, Anzahl einstellbar, volle Prüfsumme immer einzeln
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: Ab Updater 3.4.2 laufen „Medien prüfen“ (ffprobe, automatisch 4), der Schnelltest von „Duplikate finden“ (4), deren Videodaten (4) und „Vorschaubilder“ (2) parallel über eigene Threads (`Workers.ForEach`). Einstellung „Gleichzeitig“ unter Tools › Einstellungen: Automatisch / 1 / 2 / 4 / 8 (`tools.parallel`); eine feste Zahl gilt für alles ausser der vollen Prüfsumme, die immer einzeln läuft. Protokoll und Ergebnisliste bleiben in der Reihenfolge der Dateien, Abbrechen wirkt sofort, der erste Fehler bricht ab.
Begründung: Die Tools warten vor allem auf Programmstarts und Lesezugriffe. Der Nutzer arbeitet auf einem NAS, andere Nutzer lokal oder auf externen USB-Festplatten; bei HDDs würden viele gleichzeitige Lesezugriffe bremsen, darum moderate Standardwerte und „1“ für das alte Verhalten. Gemessen: 79 Dateien lokal 2,7 s → 0,75 s.
Alternativen: „Daten einlesen“ parallel (DEC-233, verworfen).
Konsequenzen: Testskripte mit PowerShell-Rückrufen aus Worker-Threads funktionieren nicht (Protokoll über kompilierte Hilfsklasse). Vorschaubilder brauchen je Thread eine eigene BMP-Zwischendatei.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\Util.cs` (`Workers`), `MediaCheckTask.cs`, `DuplicateTask.cs`, `PreviewTask.cs` (`MakeMany`), `MainForm.cs` (`BuildParallelRow`)
Abhängigkeiten: DEC-234, DEC-235

### DEC-233: „Daten einlesen“ nicht parallelisieren
Status: Rejected
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: „Daten einlesen“ und das Verkleinern zu grosser Vorschaubilder werden nicht parallelisiert.
Begründung: Der Nutzer erwartete kaum Gewinn. Auf dem NAS ist es vor allem Warten auf Ordnerlisten, die das NAS intern nacheinander abarbeitet; auf USB-HDDs würde es langsamer.
Alternativen: Mehrere Kategorien gleichzeitig durchsuchen (vom Agenten als kleineres Potenzial genannt).
Konsequenzen: keine.
Betroffene Komponenten: (nicht geändert) `DataTask.cs`
Abhängigkeiten: DEC-232

### DEC-234: Gemerkte Prüfergebnisse in `medien_cache.json`
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: Ab Updater 3.4.2 merkt sich der Updater die ffprobe-Ergebnisse je Datei (Schlüssel: relativer Pfad, Grösse, Änderungszeit) in `sflix_sys\medien_cache.json`: Dauer, Container, Video, Tonspuren oder den Fehler. Eine neue ffmpeg-Version (SHA256) verwirft alles, Einträge ohne Gebrauch fallen nach 180 Tagen weg, vorübergehende Lesefehler werden nicht gemerkt. Knopf „Gemerkte Prüfergebnisse löschen“ mit Grössenanzeige in Tools › Einstellungen. Die Datei ist persönlich: `.gitignore` und `Net.ProtectedFiles`.
Begründung: Ein zweiter Lauf soll nur neue oder geänderte Dateien prüfen (gemessen 0,02 s statt 0,75 s); auch „Vorschaubilder“ und „Duplikate finden“ nutzen die Angaben. Der Löschknopf deckt den Fall ab, dass ein Ergebnis nicht stimmt.
Alternativen: keine besprochen.
Konsequenzen: Eine Datei, deren Inhalt sich bei gleicher Grösse und Änderungszeit ändert, wird nicht neu geprüft.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\MediaCache.cs`, `Ffmpeg.cs` (`Probe`), `Net.cs` (`ProtectedFiles`), `MainForm.cs` (`BuildMediaCacheRow`, `ShowMediaCacheInfo`), `.gitignore`
Abhängigkeiten: DEC-232, DEC-226

### DEC-235: Duplikate: Schnelltest vor der vollen Prüfsumme
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Performance
Entscheidung: Bei gleich grossen Dateien vergleicht „Duplikate finden“ zuerst eine SHA256 über je 1 MB vom Anfang, aus der Mitte und vom Ende (`QuickFingerprint`, kleine Dateien ganz). Nur bei Übereinstimmung folgt die volle Prüfsumme, immer einzeln.
Begründung: Gleich grosse Dateien unterscheiden sich fast immer schon in diesen Stücken; das spart beim NAS das Lesen ganzer Filme. Getestet: Eine Datei mit einem einzigen geänderten Byte in der Mitte wird als verschieden erkannt.
Alternativen: Immer volle Prüfsumme (bisher).
Konsequenzen: Die volle Prüfsumme bleibt das entscheidende Kriterium für „identisch“.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\DuplicateTask.cs`
Abhängigkeiten: DEC-232

### DEC-236: Albumbilder für Musik in „Vorschaubilder“
Status: Implemented
Datum: 2026-11-20 (Session: 30.10. und 31.10.2026)
Kategorie: Sonstiges
Entscheidung: Ab Updater 3.4.3 gibt es bei „Vorschaubilder“ den Schalter „für Musik“ (`preview.music`).
- Album = Ordner mit mindestens 90 % Musik (Bilder nicht gezählt), nicht Wurzel oder Kategorie, ohne Vorschaubild im Preview-Ordner darüber.
- Kandidaten: alle Bilder im Album und eine Ebene darunter, mit beliebigem Namen, auch nicht kompatible Formate (Laden über ffmpeg); dazu das Cover aus der ersten Musikdatei (alle Musikformate), sonst aus zwei zufälligen anderen.
- Genau ein Kandidat ist vorgeschlagen; bei mehreren wählt der Nutzer im Fenster (Alben | Bilder | Vorschau).
- Das Original kommt nach `Preview\__original\<Album>.<ext>`, das Vorschaubild (480×270) zeigt das ganze Cover mittig mit unscharfem, vergrössertem Rand.
- „Übrige Bilder löschen“ verschiebt die anderen Bilder nach `<Album>\__gelöscht\<Unterordner>`; leere Bildordner werden entfernt.
- Ohne Oberfläche: nur Alben mit genau einem Kandidaten, nie löschen.
Begründung: Wunsch des Nutzers (Musikordner mit Bildunterordnern). Keine festen Dateinamen und Auswahl mit Vorschau auf Vorgabe des Nutzers. `__gelöscht` statt endgültig löschen, damit ein Versehen rückgängig zu machen ist. Rand statt Zuschnitt, weil beim 16:9-Zuschnitt eines quadratischen Covers fast die Hälfte wegfiele. Ablage im Album statt im Unterordner, damit der Unterordner leer werden und verschwinden kann (beim Test festgestellt und angepasst).
Alternativen: Vorschlag über feste Namen wie „front“, „cover“, „folder“ (vom Nutzer abgelehnt); Ablage `__albumbilder` (ersetzt durch `__gelöscht`); normales Zuschneiden (verworfen); je Titel eigenes Bild bei Samplern (verworfen: nur Album); Original verschieben und erst beim Einlesen zuschneiden (ersetzt durch direktes Erstellen); nur MP3 (erweitert auf alle Musikformate).
Konsequenzen: Braucht ffmpeg. Temporäre JPGs der eingebetteten Cover werden immer gelöscht. Logik getestet (eindeutige Alben ohne Oberfläche, Auswahl mit Löschen, quadratisches WEBP mit Rand); das Fenster selbst nicht über die Oberfläche getestet.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\AlbumCoverTask.cs` (`AlbumCandidate`, `AlbumCovers`, `AlbumCoverDialog`), `PreviewTask.cs`, `Core.cs` (`ChooseAlbumCovers`), `MainForm.cs` (Schalter, `TaskRunner.PreviewMusic`), `HelpContent.cs`
Abhängigkeiten: DEC-203

### DEC-237: Ablagen leeren: Art, Filter und Wiederherstellen
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Sonstiges
Entscheidung: Ab Updater 3.4.3 zeigt „Ablagen leeren“ jede Ablage mit Art (Gelöscht, Konvertiert, Original, Duplikate, Teile, Ersetzt …), sortierbar und mit Filterknöpfen. „Alle“ und „Keine“ rechnen die Summe nur einmal neu. Neben „Endgültig löschen“ gibt es „Wiederherstellen“: Der Inhalt kommt in den Ordner über der Ablage zurück, mit gleicher Unterordnerstruktur (z.B. `Filme\__gelöscht\X.mp4` → `Filme\X.mp4`). Wiederherstellen geht für ganze Ablagen oder einzelne Dateien (Doppelklick oder „Dateien wählen …“ mit Vergleich Ablage/Ziel). Gibt es das Ziel schon, fragt der Konfliktdialog (DEC-239). Danach werden die betroffenen Ordner neu eingelesen. Nur im Installationsordner (`IsInsideRoot`), ohne Oberfläche wird weiterhin alles gelöscht.
Begründung: Wünsche des Nutzers („Alle“/„Keine“ zu langsam, Wiederherstellen, Sortieren nach Art). Auswahl ganzer Ordner oder einzelner Dateien auf Vorgabe des Nutzers.
Alternativen: Nur ganze Ordner wiederherstellen (vom Agenten empfohlen, vom Nutzer erweitert); DEC-238.
Konsequenzen: `CleanupTask.Run` liefert die neu einzulesenden Ordner (bei `Preview\__…` der Ordner über Preview). Logik getestet, das Fenster erst nach einem Absturz beim Nutzer korrigiert (DEC-242).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\CleanupTask.cs` (`CleanupFolder.Kind`, `OnlyFiles`, `CleanupChoice`, `KindOf`, `Restore`), `CleanupDialog.cs` (`CleanupDialog`, `CleanupFilesDialog`), `Core.cs` (`ChooseCleanup`), `MainForm.cs` (`TaskRunner`, Neueinlesen)
Abhängigkeiten: DEC-238, DEC-239, DEC-242

### DEC-238: Beim Wiederherstellen aus `__konvertiert` die konvertierte Datei wegräumen
Status: Rejected
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Sonstiges
Entscheidung: Vorschlag des Agenten: Stellt man ein Original aus `__konvertiert` wieder her, kommt die konvertierte MP4 nach `__gelöscht`.
Begründung: Vom Nutzer abgelehnt: Das Original soll einfach wieder in seinen Ordner einsortiert werden.
Alternativen: –
Konsequenzen: Nach dem Wiederherstellen gibt es den Titel zweimal (z.B. `Film.mkv` und `Film.mp4`); STEFFLIX bzw. „Duplikate finden“ zeigen beide.
Betroffene Komponenten: –
Abhängigkeiten: DEC-237

### DEC-239: „Als Kopie speichern“ im Konfliktdialog, überall gleich
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Synchronisation
Entscheidung: Der Dialog „Datei vorhanden“ bietet neben Ersetzen und Überspringen „Als Kopie speichern“: Die Datei wird als `<Name>_<n>.<Endung>` gespeichert, mit dem ersten freien n (`Film_1.mp4`, `Film_2.mp4` …). Eine gemeinsame Auflösung (`ConflictAnswer.Resolve`, `CopyPath`) liefert Ziel, Kopie oder „überspringen“, inkl. „für alle merken“. Gilt beim Wiederherstellen, im Importer (auch beim Konvertieren) und bei „Folgen umbenennen“ (Umbenennen und Zusammenführen von Staffelordnern).
Begründung: Wunsch des Nutzers; überall gleiches Verhalten (vom Nutzer bestätigt).
Alternativen: Nur beim Wiederherstellen.
Konsequenzen: `__ersetzt` wird bei „Folgen umbenennen“ nur noch bei Ersetzen benutzt. Der Konverter bekommt eine Funktion zur Auflösung vorhandener Ziele (`ConvertInto(…, resolveExisting, …)`).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\ImportTask.cs` (`ConflictAnswer`), `Dialogs.cs` (`ConflictDialog`), `ConvertTask.cs` (`ConvertInto`), `EpisodeTask.cs` (`MergeFolder`), `CleanupTask.cs` (`Restore`)
Abhängigkeiten: DEC-237

### DEC-240: serienjunkies.de als erste Quelle für „Folgen umbenennen“
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: Sonstiges
Entscheidung: Die Folgenliste wird zuerst auf serienjunkies.de gesucht, danach auf fernsehserien.de, dann TVmaze. Die Suche probiert zuerst Adressen aus dem Seriennamen (`/<slug>/alle-serien-staffeln.html`, auch ohne führenden Artikel), danach die Stichwortsuche. Deutsche Titel, sonst die englischen.
Begründung: Wunsch des Nutzers („nimm es als primäre Quelle“). Die Stichwortsuche der Seite ist lückenhaft (z.B. „Dark“), darum das Raten der Adresse.
Alternativen: serienjunkies.de als zweite Quelle zwischen fernsehserien.de und TVmaze (Vorschlag des Agenten); Auswahl der Quelle im Suchfenster (verworfen).
Konsequenzen: Die Quelle liest HTML; ändert die Seite ihren Aufbau, findet sie nichts mehr und die nächste Quelle wird gefragt. Gespeicherte Folgenlisten behalten ihre Quelle (siehe DEC-241).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\EpisodeTask.cs` (`SerienjunkiesSource`, `EpisodeSource.All`), `HelpContent.cs`, `MainForm.cs` (Beschreibung)
Abhängigkeiten: DEC-241

### DEC-241: Knöpfe „Liste aktualisieren“ und „Neu suchen“ bei gespeicherten Folgenlisten
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Ab Updater 3.4.4 heissen die Knöpfe im Fenster der gespeicherten Folgenliste „Liste aktualisieren“ (lädt dieselbe Serie nochmals von derselben Quelle, vorher „Neu laden“) und „Neu suchen“ (Suche ab serienjunkies.de, vorher „Andere Serie suchen“). Die Frage im Fenster nennt die Quelle. Schlägt das Aktualisieren fehl, heisst der Knopf „Erneut aktualisieren“.
Begründung: Der Nutzer erwartete von „Neu laden“ eine neue Suche und meinte deshalb, serienjunkies.de werde nicht genutzt. Die Namen gab der Nutzer vor.
Alternativen: „Neu laden“ sucht zuerst auf serienjunkies.de und lädt sonst von der gespeicherten Quelle (vom Agenten vorgeschlagen, vom Nutzer stattdessen durch die Umbenennung ersetzt; nicht ausdrücklich abgelehnt).
Konsequenzen: „Erneut aktualisieren“ wählte der Agent selbst; die Rückfrage dazu blieb unbeantwortet (siehe A).
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\EpisodeDialogs.cs` (`EpisodeCacheDialog`), `HelpContent.cs`, `README.md`, `SFLIX Tools\SflixTools\README.md`
Abhängigkeiten: DEC-240

### DEC-242: Häkchen-Meldungen der Liste erst nach dem Anzeigen auswerten
Status: Implemented
Datum: 2026-11-20 (Session: 31.10.2026)
Kategorie: UI
Entscheidung: Im Fenster „Ablagen leeren“ wertet der Code `ItemChecked` erst aus, wenn das Fenster angezeigt ist (`Shown`), und berechnet dann einmal die Summe (Updater 3.4.4).
Begründung: Beim Öffnen stürzte der Updater mit einer NullReferenceException in `CleanupDialog.Checked()` ab: Beim Aufbau der Liste meldet Windows jedes Häkchen, und `Items` lässt sich in diesem Moment nicht lesen. Dasselbe Muster war in „Duplikate finden“ schon gelöst.
Alternativen: keine.
Konsequenzen: Korrektur nur kompiliert, nicht über die Oberfläche getestet. Gilt als Muster für alle Listen mit Häkchen.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\CleanupDialog.cs`
Abhängigkeiten: DEC-237

## Vorgemerkt und verworfen

### DEC-243: Desktop-Player als eine EXE `STEFFLIX.exe` (vorgemerkt)
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Architektur
Entscheidung: Player und Tools sollen später in einer einzigen EXE `STEFFLIX.exe` zusammenkommen: Doppelklick öffnet STEFFLIX in einem eigenen Fenster über WebView2, die Tools öffnen sich aus dem Hamburger-Menü. `updateStefflix.exe` wandelt sich beim Selbst-Update um. `start.html` im Browser bleibt möglich. Nicht in dieser Session umgesetzt, als eigene grosse Version vorgemerkt.
Begründung: Vorteile laut Diskussion: keine Browsereinstellungen für Autoplay und Vollbild, eigener Speicher je Installation, direkte Verbindung zu den Tools, Dateizugriff. Name vom Nutzer bestätigt.
Alternativen: Zwei EXEs für Player und Tools (vom Nutzer abgelehnt: „eine EXE reicht“); Umsetzung in dieser Runde (abgelehnt).
Konsequenzen: Offene Punkte laut Session: Übernahme der Browserdaten über die Datensicherung, Einbetten der WebView2-DLLs, Build und Selbst-Update, Warnungen des Virenschutzes bei unsignierter EXE. Umgesetzt durch DEC-275 bis DEC-292 (Desktop 4.0.0).
Betroffene Komponenten: (geplant) `SFLIX Tools\SflixTools\`
Abhängigkeiten: DEC-001

### DEC-244: Wiedergabe im Hintergrund in der App als primäres Ziel (vorgemerkt)
Status: Implemented
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: Android
Entscheidung: Musik und Videos (Ton) sollen in der App bei ausgeschaltetem Bildschirm oder in einer anderen App weiterlaufen. Vom Nutzer als primäres Ziel für spätere Updates vorgemerkt, in dieser Session nicht umgesetzt.
Begründung: Wunsch des Nutzers. Braucht einen Vordergrund-Dienst mit Benachrichtigung und Steuerung, darum eine eigene Etappe.
Alternativen: –
Konsequenzen: In `.claude\CLAUDE.md` unter „Ideen für später“ eingetragen. Umgesetzt durch DEC-252 bis DEC-262 (App 1.2.x).
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-245: Getrennte Patchnotes je Komponente (vorgemerkt)
Status: Implemented
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: UI
Entscheidung: Der Changelog soll später nach STEFFLIX, Updater und App getrennt angezeigt werden; jede Anwendung zeigt zuerst ihre eigenen Einträge, die anderen aufklappbar. In dieser Session nur vorgemerkt.
Begründung: Im Verlauf ist nur die Vormerkung belegt („Die getrennten Patchnotes stehen jetzt unter ‚Ideen für später‘“); die Begründung ist nicht erhalten (unsicher).
Alternativen: –
Konsequenzen: In `.claude\CLAUDE.md` unter „Ideen für später“. Umgesetzt durch DEC-272 und DEC-273 (ab STEFFLIX 2.7.2).
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-246: Keine Wischgesten im Player der App
Status: Rejected
Datum: 2026-11-20 (Session: 29.10.2026)
Kategorie: UI
Entscheidung: Wischen am rechten Rand für die Lautstärke und am linken für die Helligkeit wird nicht eingebaut. Doppeltippen zum Springen gibt es schon.
Begründung: Vom Nutzer abgelehnt („das mit dem Wischen mag ich nicht“).
Alternativen: –
Konsequenzen: keine.
Betroffene Komponenten: –
Abhängigkeiten: DEC-194

### DEC-247: Weboberfläche ohne Neuladen bei jedem Klick (für die App-Ladezeiten)
Status: Superseded
Datum: 2026-11-20 (Session: 30.10.2026)
Kategorie: Architektur
Entscheidung: Um die Ladezeiten der App zu senken, wird die Weboberfläche nicht auf Seitenwechsel ohne Neuladen umgebaut; stattdessen Zwischenspeicher und weniger SMB-Anfragen (DEC-211).
Begründung: Vom Agenten als grundlegender Umbau nicht empfohlen („die Punkte oben reichen“); der Nutzer nahm die übrigen Vorschläge an, ohne diesen aufzugreifen. Unsicher, ob das eine bewusste Ablehnung des Nutzers war.
Alternativen: DEC-211.
Konsequenzen: Jeder Klick lädt weiterhin `start.html` neu. Ersetzt durch DEC-306.
Betroffene Komponenten: –
Abhängigkeiten: DEC-211

## A. Offene Entscheidungen (diese Session)

- Beschriftung des Knopfs nach fehlgeschlagenem Aktualisieren in „Folgen umbenennen“: Der Agent fragte, ob er „Erneut aktualisieren“ heissen soll, und setzte es ohne Antwort so um (DEC-241).
- Ob „Liste aktualisieren“ zusätzlich zuerst auf serienjunkies.de suchen soll, wenn die gespeicherte Liste von einer anderen Quelle stammt (Vorschlag des Agenten, vom Nutzer nicht weiterverfolgt).
- Weitere Touch-Anpassungen der Weboberfläche für die App (Etappe 2) und „noch mehr Tools“ im Updater: angesprochen, ohne konkrete Beschlüsse.
- Ob der Sprung an die letzte Stelle nach einem Verbindungsabbruch (DEC-217) unter `file://` und in der App funktioniert: in der Session nicht nachgewiesen.

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-212 ersetzt durch DEC-211; DEC-225 ersetzt durch DEC-224; DEC-228 ersetzt durch DEC-227; die Wiederaufnahme nach einer Pause aus DEC-213 durch DEC-218; die Ablage `__albumbilder` und die festen Dateinamen bei Albumbildern durch DEC-236; der App-Teil von `initAndroidSection` aus DEC-190 durch DEC-204.

Gegenüber früheren Sessions:
- DEC-185 (Bilder im Arbeitsspeicher der App): ersetzt durch DEC-211 (Arbeitsspeicher und Gerät).
- DEC-190 (Abschnitt „Android-App“ in den Einstellungen): in der App ersetzt `inject.js` den Abschnitt (DEC-204).
- DEC-198 (Menüeintrag „Freigaben wechseln“, offen): umgesetzt als „Quelle wechseln“, von der App eingefügt (DEC-204).
- DEC-001 (kein Webserver): Die App betreibt weiterhin ihren Medienserver nur auf 127.0.0.1; mit Videopuffer erweitert (DEC-210).
- DEC-196 (Patch-Version je GO): präzisiert durch DEC-201 (nur geänderte Komponenten).

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis):
- DEC-243 und DEC-244 sind in späteren Sessions umgesetzt (Desktop 4.0.0 mit `STEFFLIX.exe`, App 1.2.x mit Wiedergabe im Hintergrund), ebenso DEC-245 (getrennte Patchnotes ab 2.7.2).
- DEC-247: Seit STEFFLIX 2.8.7 wechselt die Weboberfläche die Seiten doch ohne Neuladen.
- DEC-202: Die Regel wurde verschärft bzw. ergänzt: Test-APKs baut der Nutzer selbst über `build_apk.bat`.
- DEC-201: ergänzt um eine neue Version je Build mit Korrekturen.

## C. Dokumentationsbedarf

- architecture.md: Zwischenspeicher der App (Ordnerlisten statt Einzelprüfung, TTL 30 s für Programmordner, stale-while-revalidate, Gerätespeicher je Quelle, `forgetProgramFiles`); Verbindungsverwaltung (`warmUp`, `keepAlive` alle 25 s, `dropConnection` nach Pause > 20 s); Videopuffer mit Neuverbinden; Verteilung der Web-Dateien über `_webFiles`/`_obsoleteFiles` mit Schutzregeln; gemeinsame Konfliktauflösung `ConflictAnswer.Resolve`.
- data-model.md: `galleryProgress` (Ordner → höchste Bildnummer), Gruppierung in `playlast` bei Bildern; `@auto` in „Gilt für“ und die Herkunftslogik der Werte je Ordner; `sflix_sys\medien_cache.json` (Schlüssel, Gültigkeit, ffmpeg-SHA256, 180 Tage); `versions.json` › `android` (`version`, `sha256`); Namensschema `Name_<n>.ext`; Ablageorte der Albumbilder (`Preview\__original\<Album>.<ext>`, `<Album>\__gelöscht\<Unterordner>`).
- testing.md: Der Vorschau-Server kann keine Range-Anfragen (Sprung nach Verbindungsabbruch nicht testbar); neue Skripte lassen sich in die laufende Testseite nachladen, wenn die Testumgebung nicht aktualisiert werden kann; Updater-Logik als Bibliothek mit einer kompilierten Testklasse statt PowerShell-Rückrufen aus Worker-Threads; Bitdefender blockiert Verhaltenstests (DEC-203); WinForms-Listen mit Häkchen nicht vor `Shown` auswerten (DEC-242); Diagnosedaten der App (Seitenaufrufe, Ordnerlisten) als Messgrösse für Ladezeiten.

## D. Technische Risiken

- Durch stale-while-revalidate (DEC-219) kann ein Seitenaufruf Programmdateien aus unterschiedlichen Ständen mischen (z.B. neue `start.html`, alte Skripte), bis alle Ordner neu geprüft sind; nach einem Update von STEFFLIX kann das kurz zu Fehlern führen. Abhilfe ist DEC-220.
- `dropConnection` verwirft die Verbindung, während eventuell noch ein Medienstrom darauf liest; der Medienserver muss dann über sein Neuverbinden weitermachen.
- Der Konfliktdialog mit „Als Kopie speichern“ erzeugt bei wiederholtem Wiederherstellen oder Importieren Dubletten (`Name_1`, `Name_2`), die „Duplikate finden“ später wieder meldet.
- Die gemerkten Prüfergebnisse erkennen Änderungen nur über Grösse und Änderungszeit; Werkzeuge, die die Änderungszeit erhalten, umgehen die Neuprüfung.
- `SerienjunkiesSource` und `FernsehserienSource` lesen HTML per regulären Ausdrücken; Änderungen am Aufbau der Seiten lassen die Quelle still ausfallen.
- Die App hängt an Namen der Weboberfläche (Menü, Abschnitt `#android`, `#loadingScreen`, Klassen für die grösseren Menüs); Umbenennungen brechen sie unbemerkt.
- Parallele Tools (DEC-232) können bei langsamen Laufwerken oder NAS mit wenigen Platten langsamer sein als einzeln; der Standard „automatisch“ ist nicht nach Laufwerksart unterschieden.
- Dateien aus `checksums.json` werden zwar nach Pfad und Endung gefiltert, ein manipuliertes Repository könnte aber weiterhin beliebigen Inhalt in erlaubte Web-Dateien schreiben (Vertrauen in das GitHub-Repository bleibt Voraussetzung).
- Albumbilder und Wiederherstellen wurden nur je mit einem Testlauf an Beispieldaten geprüft; die Fenster nicht über die Oberfläche. Der Absturz in DEC-242 zeigt, dass solche Fehler erst beim Nutzer auffallen.
- Nach dem Wiederherstellen aus `__konvertiert` liegen Original und konvertierte Datei nebeneinander (DEC-238).

---

# Session STEFFLIX 2.7.0–2.7.3 / Updater 3.5.0 / Android-App 1.2.0–1.2.5 (Arbeit vom 01.11.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session, erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-248). Die Session umfasste ein GO und mehrere Test-Builds mit Korrekturen, die der Nutzer jeweils selbst mit `build_release.bat` gebaut hat:

| Build | Versionen | Inhalt |
|---|---|---|
| 1 | STEFFLIX 2.7.0, App 1.2.0–1.2.2 | Wiedergabe im Hintergrund, Bild-im-Bild; Korrekturen Audiofokus (1.2.1) und Aufgabe/Quellenliste (1.2.2) |
| 2 | STEFFLIX 2.7.1, App 1.2.3 | Knopf „Beenden“, Zurück-Taste, Symbol |
| 3 | STEFFLIX 2.7.2, Updater 3.5.0, App 1.2.4 | getrennte Patchnotes, gesperrter Release-Ordner |
| 4 | STEFFLIX 2.7.3, App 1.2.5 | App öffnet sich nach einem Update wieder (am Ende der Session umgesetzt, noch nicht gebaut) |

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session (01.11.2026). Spätere Änderungen sind nicht berücksichtigt, Hinweise darauf stehen unter B.

Einschränkungen: „Implemented“ heisst hier: im Code umgesetzt. Kompiliert hat der Agent den Java-Code mit `javac` gegen `android.jar` und den Updater als Bibliothek; die Weboberfläche bzw. `inject.js` testete er im Vorschau-Browser mit einer nachgebildeten App-Brücke. Auf dem Gerät testete nur der Nutzer; wo seine Bestätigung vorliegt, ist sie vermerkt. Nicht bestätigt sind der Knopf „Beenden“, die Zurück-Taste und das Symbol (1.2.3), die Sperre des Release-Ordners (3.5.0), die getrennten Patchnotes in Updater und App sowie das Öffnen nach einem Update (1.2.5).

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 0 |
| Implemented | 21 |
| Proposed | 1 |
| Rejected | 2 |
| Superseded | 3 |

## Arbeitsweise und Versionen

### DEC-248: Test-APKs baut der Nutzer selbst, Debug-APK nur bei sonst nicht auffindbaren Fehlern
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sonstiges
Entscheidung: Für Tests auf dem Gerät baut der Nutzer die APK selbst mit `SFLIX Tools\Android\build_apk.bat` (normaler Schlüssel). Der Agent sagt ihm, wann ein Build fällig ist und was zu testen ist. Eine Debug-APK gibt es nur auf Wunsch des Nutzers oder bei Fehlern, die sich anders nicht finden lassen, und immer erst nach Rückfrage.
Begründung: Die Debug-APK hat einen anderen Signaturschlüssel. Die installierte App müsste deinstalliert werden, wobei Quellen, Passwörter, Einstellungen und STEFFLIX-Daten der App verloren gehen. Sie ist zudem „debuggable“ (Zugriff auf App-Daten per USB-Debugging). Der Nutzer will Builds selbst auslösen („ich mach das jeweils selber“).
Alternativen: Debug-APK für normale Tests (verworfen, s.o.); eigene Build-Option `-Test` (DEC-249, verworfen); Agent führt `build_apk.bat` selbst aus (vom Nutzer abgelehnt).
Konsequenzen: Regel in `.claude\CLAUDE.md`. Der Test-Build überschreibt `STEFFLIX.apk` im Release-Ordner und darf nicht vor dem Release gepusht werden; `release.ps1` baut bei geändertem Quellcode ohnehin neu. Seit DEC-336 bauen Prüfung, Release und Entwicklung Test-APKs (build_test_apk.ps1).
Betroffene Komponenten: `.claude\CLAUDE.md`, `SFLIX Tools\Android\build_apk.bat`
Abhängigkeiten: DEC-202 (frühere Session), DEC-249

### DEC-249: Keine eigene Build-Option `-Test` für die App
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Die vom Agenten vorgeschlagene Option `-Test` in `build.ps1` (Build mit normalem Schlüssel nach `dist\STEFFLIX-test.apk`, ohne `STEFFLIX.apk` und Version zu berühren) wird nicht umgesetzt.
Begründung: Nutzer: Wenn sich die Test-APK nicht von der normalen unterscheidet, gibt es keinen Bedarf.
Alternativen: Normaler Build über `build_apk.bat` (gewählt, DEC-248).
Konsequenzen: Keine Änderung an `build.ps1`.
Betroffene Komponenten: –
Abhängigkeiten: DEC-248

### DEC-250: Versions-String am Schluss jeder Umsetzung
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sonstiges
Entscheidung: Am Ende jeder Umsetzung nennt der Agent die geänderten Komponenten mit Version, Reihenfolge STEFFLIX, Updater, App, z.B. „Stefflix 2.7.0 / App 1.2.0“. Komponenten ohne Änderung fehlen.
Begründung: Wunsch des Nutzers.
Alternativen: –
Konsequenzen: Regel in `.claude\CLAUDE.md`.
Betroffene Komponenten: `.claude\CLAUDE.md`
Abhängigkeiten: DEC-251

### DEC-251: Jeder Build mit Korrekturen bekommt eine neue Version, je geänderter Komponente
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sonstiges
Entscheidung: Startversionen der Session waren STEFFLIX 2.7.x, Updater 3.5.x und App 1.2.x; erhöht wird nur bei tatsächlichen Änderungen. Zusätzlich bekommt jeder Build mit Korrekturen nach einem Test eine neue Patch-Version, und zwar für jede Komponente mit Änderungen seit dem letzten Build. Das gilt auch für STEFFLIX, wenn nur `start.html` oder `news.txt` geändert wurde (neue Version mit eigenem Block in `news.txt`). Der Agent trägt die nächste Version nach einer Korrektur selbst ein (`version.js`, `Program.cs`, `app\build.gradle`) und nennt sie in den Changelog-Einträgen.
Begründung: `build_release.bat` erhöht die Version bei geändertem Quellcode ohnehin (die App ging so von 1.2.0 über 1.2.1 auf 1.2.2, ohne dass der Agent mitzählte). Der Nutzer wies darauf hin, dass eine Änderung an `start.html` nach dem Build von 2.7.0 eine 2.7.1 ergibt.
Alternativen: Version nur je GO erhöhen (frühere Regel DEC-201, ergänzt).
Konsequenzen: Der Block 2.7.0 in `news.txt` wurde auf den gebauten Stand (App 1.2.2) zurückgesetzt, die Korrekturen kamen in eigene Blöcke 2.7.1 bis 2.7.3. Auch reine App-Korrekturen erhöhen STEFFLIX, weil ihr Changelog-Eintrag in `news.txt` (eine Datei von STEFFLIX) landet. Regel in `.claude\CLAUDE.md`.
Betroffene Komponenten: `.claude\CLAUDE.md`, `sflix_sys\version.js`, `sflix_sys\news.txt`, `SFLIX Tools\Android\app\build.gradle`, `SFLIX Tools\SflixTools\src\Program.cs`
Abhängigkeiten: DEC-201 (frühere Session), DEC-250

## Android-App: Wiedergabe im Hintergrund (1.2.0–1.2.3)

### DEC-252: Wiedergabe im Hintergrund über Vordergrund-Dienst, MediaSession und Brücke in `inject.js`
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Musik und der Ton von Videos laufen weiter, wenn der Bildschirm aus ist oder eine andere App offen ist, auch mit Autoplay zum nächsten Medium (inkl. Playliste und Zufallswiedergabe). Umsetzung: ein Vordergrund-Dienst der Art `mediaPlayback` (`PlaybackService`), eine `MediaSession` (Sperrbildschirm, Bluetooth) und eine Benachrichtigung `MediaStyle` mit Titel, Ordner (Album bzw. Serie/Staffel) und Vorschaubild. STEFFLIX meldet den Zustand über `inject.js` (`StefflixApp.mediaState` bei Medienereignissen und alle 2 s bei Änderungen), die App schickt Befehle über `window.sflixAppMedia(cmd, arg)` zurück. Die Weboberfläche bleibt unverändert, ohne native Wiedergabe.
Begründung: Primäres Ziel des Nutzers (DEC-244). Die Brücke in `inject.js` funktioniert mit jeder STEFFLIX-Version auf der Freigabe.
Alternativen: Autoplay nur bis zum Ende der laufenden Datei (vom Nutzer verneint: Autoplay soll im Hintergrund weiterlaufen).
Konsequenzen: Neue Rechte (`FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_MEDIA_PLAYBACK`, `POST_NOTIFICATIONS`, `WAKE_LOCK`); `inject.js` hängt zusätzlich an `#video`, `#mediaTitle`, `#next a`/`#prev a`, `getInheritedPreview`/`getPreviewForMedia`. Vom Nutzer bestätigt: Musik bei ausgeschaltetem Bildschirm inkl. nächstem Titel, Ton von Videos in anderer App inkl. nächster Folge, Benachrichtigung, Sperrbildschirm und Bluetooth-Knöpfe, Öffnen an derselben Stelle.
Betroffene Komponenten: `Playback.java`, `PlaybackService.java`, `PlaybackReceiver.java`, `BrowserActivity.java` (Brücke `mediaState`, `Playback.Host`), `assets\inject.js`, `AndroidManifest.xml`
Abhängigkeiten: DEC-244 (frühere Session), DEC-253, DEC-254

### DEC-253: WebView meldet sich während der Wiedergabe im Hintergrund weiter als sichtbar
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: `PlayerWebView` gibt bei `setKeepVisible(true)` Fensterwechsel nicht an die Seite weiter (`onWindowVisibilityChanged` → VISIBLE). `BrowserActivity.onPause` setzt das, wenn „Wiedergabe im Hintergrund“ an ist und ein Medium geladen ist; dann entfallen `web.onPause()` und der Abbau der warm gehaltenen Verbindung, und beim Zurückkehren wird nicht neu verbunden. Sonst hält `onStop` die Wiedergabe an (ausser beim Öffnen der App-Einstellungen).
Begründung: Laut Agent hält Chromium Videos in einer unsichtbaren Seite an und drosselt die Skripte, von denen das Autoplay abhängt. Ein Neuverbinden nach der Pause hätte den laufenden Medienstrom unterbrochen. Der Nutzer bestätigte, dass Ton und Autoplay im Hintergrund laufen.
Alternativen: Absicherung des Autoplays aus der App, falls die Skripte im Hintergrund nicht laufen (als Rückfall erwogen, nicht nötig).
Konsequenzen: Die Seite läuft im Hintergrund mit vollem Takt weiter, solange ein Medium geladen ist.
Betroffene Komponenten: `PlayerWebView.java`, `BrowserActivity.java` (`onPause`, `onStop`, `onResume`, `backgroundAllowed`)
Abhängigkeiten: DEC-252

### DEC-254: Vordergrund-Dienst nur während der Wiedergabe starten, nach einer Pause verzögert lösen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: `Playback` startet `PlaybackService` erst, wenn etwas spielt. Nach einer Pause wird der Dienst 15 s später gelöst (`DETACH_DELAY_MS`), die Benachrichtigung bleibt. Wakelock und WLAN-Lock gelten nur während der Wiedergabe mit eingeschalteter Einstellung.
Begründung: Android verbietet den Start eines Vordergrund-Dienstes aus dem Hintergrund. Beim Wechsel zum nächsten Medium pausiert die Seite kurz; ohne Verzögerung ginge der Dienst dabei verloren und liesse sich im Hintergrund nicht neu starten. Wakelock und WLAN-Lock verhindern, dass bei ausgeschaltetem Bildschirm der Strom vom NAS abbricht.
Alternativen: Dienst dauerhaft im Vordergrund auch während einer Pause (erwogen, wegen nicht wegwischbarer Benachrichtigung nicht gewählt).
Konsequenzen: Fortsetzen nach längerer Pause aus dem Hintergrund (z.B. per Bluetooth) kann den Dienst nicht immer neu starten; Fehler beim Start werden abgefangen, die Wiedergabe läuft dann ohne Dienst weiter (siehe D).
Betroffene Komponenten: `Playback.java` (`updateNotification`, `detachForeground`, `updateLocks`), `PlaybackService.java`
Abhängigkeiten: DEC-252

### DEC-255: Eigener Audiofokus mit Pause und Wiederaufnahme
Status: Superseded
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: In 1.2.0 fragte `Playback` selbst nach dem Audiofokus (`AUDIOFOCUS_GAIN`) und pausierte bei Verlust (bei kurzem Verlust mit Wiederaufnahme).
Begründung: Gewünscht war: bei Anruf oder Ton einer anderen App pausieren und danach weiterspielen.
Alternativen: DEC-256.
Konsequenzen: Im Test pausierte jedes Musikstück und Video nach etwa einer Sekunde und liess sich nicht mehr starten: Die WebView hält selbst den Audiofokus, die eigene Anfrage nahm ihn ihr weg, und beim erneuten Abspielen hielt `Playback` den Fokusverlust für eine andere App. Ersetzt durch DEC-256 (1.2.1).
Betroffene Komponenten: `Playback.java`
Abhängigkeiten: DEC-256

### DEC-256: Kein eigener Audiofokus, die WebView regelt Unterbrechungen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Die App fragt nicht selbst nach dem Audiofokus. Pausieren bei Anruf oder Ton einer anderen App überlässt sie der WebView.
Begründung: Behebt den Fehler aus DEC-255. Vom Nutzer bestätigt: Die Wiedergabe läuft, und YouTube unterbricht STEFFLIX.
Alternativen: DEC-255 (ersetzt).
Konsequenzen: Ob die Wiedergabe nach einer kurzen Unterbrechung (Anruf, Sprachnachricht) von selbst weiterläuft, hängt nun von der WebView ab und ist nicht getestet (siehe A).
Betroffene Komponenten: `Playback.java`
Abhängigkeiten: DEC-255, DEC-257

### DEC-257: Pause beim Trennen der Kopfhörer
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Während der Wiedergabe hört `Playback` auf `ACTION_AUDIO_BECOMING_NOISY` und pausiert.
Begründung: Wunsch des Nutzers (nicht über den Lautsprecher weiterspielen). Vom Nutzer bestätigt.
Alternativen: –
Konsequenzen: Empfänger wird nur während der Wiedergabe registriert.
Betroffene Komponenten: `Playback.java` (`setNoisy`)
Abhängigkeiten: DEC-256

### DEC-258: Beenden durch Wegwischen der pausierten Benachrichtigung
Status: Superseded
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: Weil Android höchstens fünf Knöpfe zeigt, gab es in 1.2.0 statt eines Knopfs „Beenden“ das Wegwischen der Benachrichtigung nach einer Pause (`setDeleteIntent` → Pause, Benachrichtigung weg); Knöpfe waren Zurück, 10 s zurück, Abspielen/Pause, 10 s vor, Weiter.
Begründung: Der Nutzer wollte alle Knöpfe inkl. Beenden; die Plattform zeigt nicht mehr als fünf.
Alternativen: DEC-260.
Konsequenzen: Im Test (Android 13+) erschien die Steuerung als Mediaplayer auf Sperrbildschirm und in den Schnelleinstellungen; Wegwischen auf dem Sperrbildschirm blendete ihn nur aus und stoppte nichts. Ersetzt durch DEC-260.
Betroffene Komponenten: `Playback.java`
Abhängigkeiten: DEC-260

### DEC-259: Wegwischen der App stoppt die Wiedergabe
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Wird die App aus der Übersicht der letzten Apps gewischt, stoppt die Wiedergabe, die Benachrichtigung verschwindet und die Seite schliesst sich (`onTaskRemoved` → `CMD_QUIT`).
Begründung: Antwort des Nutzers „stoppen“. Vom Nutzer bestätigt.
Alternativen: Weiterlaufen bis „Beenden“ (verworfen).
Konsequenzen: `PlaybackService` mit `stopWithTask="false"`, damit `onTaskRemoved` ankommt.
Betroffene Komponenten: `PlaybackService.java`, `Playback.java`, `AndroidManifest.xml`
Abhängigkeiten: DEC-252

### DEC-260: Knöpfe Zurück, Beenden, Abspielen/Pause, 10 s vor, Weiter
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: „Beenden“ ersetzt „10 s zurück“, auch während der Wiedergabe. Reihenfolge in der Benachrichtigung: Zurück, Beenden, Abspielen/Pause, 10 s vor, Weiter (kompakt: Zurück, Abspielen/Pause, Weiter). Ab Android 13 baut das System den Mediaplayer aus `PlaybackState`: Zurück/Abspielen/Weiter plus die eigenen Aktionen Beenden und 10 s vor. Beenden pausiert, entfernt die Benachrichtigung und setzt die Session auf STOPPED und inaktiv, bis wieder abgespielt wird.
Begründung: Vorgabe des Nutzers („behalte 10s vor … ersetze 10s zurück mit dem Beenden Button“), nachdem Wegwischen auf dem Sperrbildschirm nicht beendete (DEC-258). Der Vorschlag des Agenten, „10 s vor“ nur während einer Pause durch Beenden zu ersetzen, wurde nicht gewählt.
Alternativen: DEC-258 (ersetzt); Beenden nur bei Pause anstelle von „10 s vor“ (nicht gewählt).
Konsequenzen: Kein Knopf „10 s zurück“ mehr in der Benachrichtigung (über `MediaSession.onRewind` weiterhin möglich). Auf dem Gerät nicht bestätigt (siehe A).
Betroffene Komponenten: `Playback.java` (`buildNotification`, `pushState`, `command`), `res\drawable\ic_close.xml`
Abhängigkeiten: DEC-258

### DEC-261: Schalter „Wiedergabe im Hintergrund“ und „Bild-im-Bild“, standardmässig ein
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: Zwei eigene Schalter in den App-Einstellungen unter „Wiedergabe und Anzeige“, beide standardmässig ein. Mit ausgeschalteter Wiedergabe im Hintergrund pausiert die Wiedergabe beim Verlassen der App wie bisher, und es gibt keine Benachrichtigung; mit ausgeschaltetem Bild-im-Bild fehlt der Knopf im Player.
Begründung: Antworten des Nutzers (je ein Schalter, beide ein). Vom Nutzer bestätigt.
Alternativen: Musik standardmässig ein, Videos optional (nicht gewählt).
Konsequenzen: `AppPrefs.backgroundPlayback`, `pictureInPicture`; Angaben auch in der Diagnose.
Betroffene Komponenten: `SettingsActivity.java`, `AppPrefs.java`, `BrowserActivity.java`, `assets\inject.js` (`sflixAppRefresh`)
Abhängigkeiten: DEC-252, DEC-263

### DEC-262: Frage nach Benachrichtigungen beim ersten Abspielen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Ab Android 13 fragt die App einmal beim ersten Abspielen (nicht beim Start) nach der Berechtigung für Benachrichtigungen (`notificationsAsked`).
Begründung: Vom Nutzer so bestätigt.
Alternativen: Frage beim Start der App (nicht gewählt).
Konsequenzen: Nach Erteilen wird die Benachrichtigung neu aufgebaut (`playback.refresh`).
Betroffene Komponenten: `BrowserActivity.java` (`askNotificationPermission`), `AppPrefs.java`
Abhängigkeiten: DEC-252

## Android-App: Bild-im-Bild und Navigation

### DEC-263: Bild-im-Bild über den vorhandenen Knopf von STEFFLIX
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Bild-im-Bild startet nur über einen Knopf in der Player-Leiste, nur bei Videos. Dafür nutzt die App den vorhandenen, in der WebView bisher unsichtbaren Knopf `.ctrlPip` von STEFFLIX: `inject.js` stellt `document.pictureInPictureEnabled`, `pictureInPictureElement`, `exitPictureInPicture` und `HTMLVideoElement.requestPictureInPicture` bereit und leitet an `StefflixApp.enterPip(breite, höhe)` weiter. Im kleinen Fenster zeigt die Klasse `appPip` nur das Video bildfüllend. Das Fenster hat das Seitenverhältnis des Videos (begrenzt auf 2,39:1 bzw. 1:2,39) und die Knöpfe Zurück, Abspielen/Pause, Weiter. Schliessen des Fensters bei eingeschaltetem Bildschirm hält das Video an; Bildschirm aus gilt nicht als Schliessen.
Begründung: Kein Eingriff in die Weboberfläche nötig; Antworten des Nutzers (Knopf statt automatisch, Schliessen = Pause). Vom Nutzer bestätigt (Knöpfe, Anhalten beim Schliessen, Rückkehr an dieselbe Stelle).
Alternativen: Automatischer Start beim Verlassen der App (DEC-264, verworfen); eigener neuer Knopf in der Weboberfläche (nicht nötig).
Konsequenzen: `BrowserActivity` mit `supportsPictureInPicture`; die Prüfung `pipAvailable` berücksichtigt Einstellung, Gerät und die Sperre in den Android-Einstellungen (AppOps). Die CSS-Regel setzt Transformationen der Eltern ausser Kraft.
Betroffene Komponenten: `assets\inject.js` (`sflixAppPip`), `BrowserActivity.java` (`enterPip`, `pipParams`, `onPictureInPictureModeChanged`, `onStop`), `AndroidManifest.xml`, `res\drawable\ic_*.xml`
Abhängigkeiten: DEC-261, DEC-265

### DEC-264: Kein automatisches Bild-im-Bild beim Verlassen der App
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: Bild-im-Bild startet nicht automatisch über Home-Taste oder App-Wechsel.
Begründung: Antwort des Nutzers: „nein, lieber mit einem button“.
Alternativen: DEC-263.
Konsequenzen: Verlässt man ein Video ohne Bild-im-Bild, läuft der Ton bei eingeschalteter Wiedergabe im Hintergrund weiter (DEC-253).
Betroffene Komponenten: –
Abhängigkeiten: DEC-263

### DEC-265: STEFFLIX allein in der Aufgabe, Quellenliste schliesst sich
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: `ShareListActivity` schliesst sich nach dem Öffnen einer Quelle (`open` → `finish`). `BrowserActivity` hat `launchMode="singleTask"`. `ShareListActivity.intent()` ersetzt die ganze Aufgabe (NEW_TASK, CLEAR_TASK); `onNewIntent` mit anderer Quelle baut die Seite frisch auf (`EXTRA_FRESH`).
Begründung: Beim Wechsel in Bild-im-Bild löste Android `BrowserActivity` aus der Aufgabe heraus, und die darunterliegende Quellenliste kam nach vorne (Rückmeldung des Nutzers). `singleTask` lässt das Antippen der Benachrichtigung die laufende Seite öffnen.
Alternativen: Eigene Aufgabe für `BrowserActivity` über `taskAffinity` (vom Agenten erwogen, nicht gewählt).
Konsequenzen: Die Zurück-Taste fand die Quellenliste nicht mehr unter STEFFLIX (siehe DEC-266, DEC-267). Vom Nutzer bestätigt („das ist nun auch korrekt“).
Betroffene Komponenten: `ShareListActivity.java`, `BrowserActivity.java`, `AndroidManifest.xml`
Abhängigkeiten: DEC-263, DEC-267

### DEC-266: Zurück-Taste öffnet die Quellenliste neu
Status: Superseded
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: In 1.2.2 öffnete die Zurück-Taste ganz am Anfang von STEFFLIX die Quellenliste neu (`EXTRA_CHOOSE`), um das bisherige Verhalten trotz DEC-265 zu erhalten.
Begründung: Vom Agenten zur Wahrung des alten Verhaltens gewählt.
Alternativen: DEC-267.
Konsequenzen: Vom Nutzer abgelehnt: Quellen wechselt man über „Quelle wechseln“ oder die App-Einstellungen. Ersetzt durch DEC-267.
Betroffene Komponenten: `BrowserActivity.java` (`onBackPressed`)
Abhängigkeiten: DEC-265, DEC-267

### DEC-267: Zurück höchstens bis zur Startseite, dann schliesst sich die App
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: Die Zurück-Taste geht durch den Verlauf von STEFFLIX bis zur Startseite; ein weiteres Zurück schliesst die App (`finish`), eine laufende Wiedergabe stoppt.
Begründung: Vorgabe des Nutzers.
Alternativen: App in den Hintergrund wie die Home-Taste (Vorschlag des Agenten, nicht gewählt); nichts tun (nicht gewählt); DEC-266 (ersetzt).
Konsequenzen: Beim nächsten Start öffnet die Quellenliste wieder die zuletzt genutzte Quelle. Auf dem Gerät nicht bestätigt.
Betroffene Komponenten: `BrowserActivity.java` (`onBackPressed`)
Abhängigkeiten: DEC-265, DEC-266

### DEC-268: STEFFLIX-„S“ als Symbol in Statusleiste und Always-on-Display
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: Das kleine Symbol der Benachrichtigung ist das „S“-Band aus `Bilder\sflixLogo.svg` als einfarbiger Vektor, ohne das Quadrat.
Begründung: Wunsch des Nutzers (statt Play-Kreis). Android zeigt kleine Symbole nur einfarbig; ein gefülltes Quadrat wäre nur eine Fläche.
Alternativen: Play-Symbol im Kreis (erste Fassung, ersetzt).
Konsequenzen: Auch im Hinweis nach einem Update verwendet (DEC-269). Auf dem Gerät nicht bestätigt.
Betroffene Komponenten: `res\drawable\ic_notification.xml`
Abhängigkeiten: DEC-252

### DEC-269: Nach einem Update aus der App wieder öffnen bzw. Hinweis zum Antippen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Android
Entscheidung: Vor dem Übergeben an den Installer merkt sich die App den Zeitpunkt (`AppPrefs.updateStarted`). Ein Empfänger für `MY_PACKAGE_REPLACED` (`AppUpdate.ReplacedReceiver`) zeigt, wenn das Update aus der App kam und höchstens 30 min zurückliegt, eine Benachrichtigung „STEFFLIX wurde aktualisiert“ (Antippen öffnet die App) und versucht zusätzlich, die App direkt zu öffnen. Beim Öffnen verschwindet der Hinweis (`clearUpdatedNotice`).
Begründung: Nach dem Update schloss sich die App und öffnete sich nicht wieder (Rückmeldung des Nutzers). Ab Android 10 darf eine App sich aus dem Hintergrund meist nicht selbst öffnen, daher die Benachrichtigung als Rückfall.
Alternativen: Nur direktes Öffnen (unter Android 10+ meist blockiert).
Konsequenzen: Nur für Updates aus der App, nicht für von Hand installierte APKs. Am Ende der Session umgesetzt und kompiliert, aber nicht gebaut; testbar erst beim Update von 1.2.5 auf eine spätere Version.
Betroffene Komponenten: `AppUpdate.java` (`ReplacedReceiver`, `install`, `InstallReceiver`), `AppPrefs.java`, `ShareListActivity.java`, `AndroidManifest.xml`
Abhängigkeiten: DEC-206 (frühere Session)

## Updater und Release

### DEC-270: Im Release-Ordner keine veralteten Dateien löschen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sicherheit
Entscheidung: `Net.RemoveObsoleteFiles` überspringt das Löschen, wenn der Updater im Release-Ordner läuft (`Net.IsReleaseFolder`: es gibt `SFLIX Tools\Release\release.ps1`). `updater.exe`, `updateData.exe` und `sflix_sys\sflix.js` bleiben dort erhalten.
Begründung: `check_release.py` brach ab, weil die drei Dateien im Release-Ordner fehlten. Wahrscheinliche Ursache (nicht belegt, kein Protokoll vorhanden): Der Updater hatte sie dort beim Update als veraltet gelöscht. Sie werden aber für alte Installationen weiter ausgeliefert. Vom Nutzer für diese Runde verlangt.
Alternativen: Nur als Idee für später vormerken (vom Nutzer verworfen: „direkt in diesem Run ausführen“).
Konsequenzen: Bei Nutzern ändert sich nichts. Die verlorenen Dateien stellte der Nutzer wieder her (EXEs mit Prüfsumme bestätigt), den Platzhalter `sflix.js` kopierte der Agent aus der Testumgebung zurück.
Betroffene Komponenten: `SFLIX Tools\SflixTools\src\Net.cs`, `SFLIX Tools\SflixTools\README.md`
Abhängigkeiten: DEC-271

### DEC-271: Release-Ordner gesperrt, der Updater legt dort keine Dateien an
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sicherheit
Entscheidung: Läuft der Updater im Release-Ordner (`App.IsReleaseFolder`), sind Update samt Selbst-Update, Daten einlesen, Importer, alle Tools und Lizenz einlösen bzw. entfernen gesperrt, mit Hinweisbalken oben. Erlaubt bleiben Versionsanzeige, Patchnotes, ffmpeg, Feedback, Hilfe und Setup in einen anderen Ordner. Keine Update-Abfrage und kein Update-Dialog beim Start. Der Updater legt keine Dateien an: Einstellungen nur im Speicher bis zum Schliessen, kein `install.js`, `updater_info.js`, `updater.log`. Beim Start löscht er die vom Updater angelegten Dateien in `sflix_sys` (`install.js`, `updater_info.js`, `updater_settings.json`, `updater.log`, `renamed.js`, `folgen_cache.json`, `medien_cache.json`, `folgen_rueckgaengig.json`), nie `data.js` und `license.js`. Ohne Oberfläche bricht er dort mit Code 4 ab.
Begründung: Vorgabe des Nutzers: Optionen, die Dateien der Komponenten ändern, sollen gesperrt sein, und vom Updater angelegte Dateien sollen gelöscht werden, damit sie nicht im Git-Ordner landen. Ein Update hätte zudem nicht gepushte Änderungen an `start.html` und `sflix_sys` mit dem Stand auf GitHub überschrieben.
Alternativen: Einstellungen unter `%LOCALAPPDATA%\STEFFLIX` speichern (vom Nutzer verworfen: nur bis zum Schliessen); vorhandene Dateien liegen lassen (vom Nutzer verworfen: löschen).
Konsequenzen: `license.js` hat der Agent eigenmächtig vom Löschen ausgenommen (persönlicher Schlüssel, ohnehin in `.gitignore`); die Rückfrage dazu blieb unbeantwortet (siehe A). Tests des Updaters brauchen `SFLIX_DIR` auf eine Testumgebung. In der Session nicht über die Oberfläche getestet. downloads.json gehört ebenfalls zu den Dateien, die im Release-Ordner nie geschrieben werden.
Betroffene Komponenten: `Core.cs` (`App.IsReleaseFolder`, `ReleaseFolder`, `Settings.Save`, `UpdaterInfo.Write`), `Program.cs`, `MainForm.cs` (`OnShown`, `CheckForUpdateInBackground`, `StartTasks`, `SetRunning`), `License.cs`, `LogFile.cs`, `SFLIX Tools\SflixTools\README.md`
Abhängigkeiten: DEC-270

### DEC-272: Getrennte Patchnotes: additive Felder in `news.json`, Komponente aus dem Textanfang
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Architektur
Entscheidung: Das Format von `news.txt` und `news.json` bleibt. `create_news.py` ergänzt jeden Eintrag um `component` (`stefflix`, `updater`, `app`), `componentVersion` und `short` (Text ohne Anfang). Die Komponente ergibt sich aus dem Textanfang „Updater x.y.z:“ bzw. „Android-App x.y.z:“, sonst STEFFLIX. Neue Leser (STEFFLIX, Updater, App) erkennen die Komponente bei einer älteren `news.json` selbst am Textanfang. Einträge des Updaters und der App beginnen künftig immer mit Komponente und Version.
Begründung: Der Nutzer fragte, ob sich das machen lässt, ohne dass alte Versionen Fehler zeigen. Alte Leser kennen nur `version`, `name`, `items` mit `type`/`text` und überlesen zusätzliche Felder.
Alternativen: Getrennte Dateien je Komponente (nicht gewählt).
Konsequenzen: Regel zu den Anfängen in `.claude\CLAUDE.md`. Jede `<li>`-Zeile muss auf einer eigenen Zeile stehen, sonst überliest `create_news.py` den Eintrag (in der Session aufgetreten). Ergänzt durch DEC-285 (Präfix „Desktop“) und DEC-301 („Alle Versionen“).
Betroffene Komponenten: `SFLIX Tools\Release\create_news.py`, `sflix_sys\news.txt`, `sflix_sys\js\pages.js` (`getNewsComponent`), `UpdateTask.cs` (`PatchNotes`, `ComponentOf`), `NewsDialog.java`, `.claude\CLAUDE.md`
Abhängigkeiten: DEC-245 (frühere Session), DEC-273, DEC-274

### DEC-273: Getrennte Patchnotes: eigene Einträge zuerst, die anderen aufklappbar
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: UI
Entscheidung: STEFFLIX (Hilfe › Changelog) zeigt die eigenen Einträge immer nach Versionen; Updater und Android-App stehen je in einem aufklappbaren Bereich, gegliedert nach „Updater x.y.z“ bzw. „Android-App x.y.z“, mit gleichen Überschriften zusammengefasst. Der Update-Hinweis von STEFFLIX zeigt zuerst die eigenen Einträge. Der Updater zeigt in „Patchnotes“ zuerst seine Einträge, Schalter „STEFFLIX“ und „Android-App“ blenden die anderen ein. Die App bekommt in den App-Einstellungen „Neuerungen“: zuerst die Einträge der App, Schalter für STEFFLIX und Updater. Der Anfang „Updater x.y.z:“ bzw. „Android-App x.y.z:“ ist in diesen Ansichten ausgeblendet.
Begründung: Vorgaben des Nutzers (App eigene Anzeige, in STEFFLIX alle Komponenten mit STEFFLIX immer sichtbar, Überschriften nach Komponentenversion, Anfang ausblenden).
Alternativen: Nur eine gemeinsame Liste wie bisher. Im Updater gibt es keine aufklappbaren Bereiche (Textfeld), daher Schalter.
Konsequenzen: In STEFFLIX im Vorschau-Browser geprüft, auch mit der alten `news.json` von GitHub; Updater-Dialog und App-Dialog nicht über die Oberfläche getestet. Ergänzt durch DEC-285 (Präfix „Desktop“) und DEC-301 („Alle Versionen“).
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderNews`, `fillUpdateNews`, `createNewsList`), `sflix_sys\sflix.css`, `start.html`, `Dialogs.cs` (`PatchNotesDialog`, `GroupsOf`), `HelpContent.cs`, `NewsDialog.java`, `SettingsActivity.java`, `README.md`
Abhängigkeiten: DEC-272

### DEC-274: Alte „Updater:“-Einträge ohne Version nicht nachtragen
Status: Proposed
Datum: 2026-11-20 (Session: 01.11.2026)
Kategorie: Sonstiges
Entscheidung: Ältere Einträge mit nur „Updater:“ (bzw. „Android-App:“) bekommen keine nachträgliche Versionsnummer. Sie erscheinen als „Updater (mit STEFFLIX x.y.z)“.
Begründung: Die Zuordnung zu Updater-Versionen ist nirgends festgehalten; der Agent wollte im für Nutzer sichtbaren Changelog nicht raten. Der Nutzer hatte die Vereinheitlichung erlaubt („Ja kannst du machen“), diese Einschränkung aber nicht bestätigt.
Alternativen: Nachtragen, falls der Nutzer die Zuordnung liefert.
Konsequenzen: Nur neue Einträge sind nach Komponentenversion gegliedert.
Betroffene Komponenten: `sflix_sys\news.txt`
Abhängigkeiten: DEC-272

## A. Offene Entscheidungen (diese Session)

- **Warnung von Google Play Protect** („potentiell schädlich“ seit einigen Releases): Erklärt (wahrscheinlich die Selbstinstallation über `REQUEST_INSTALL_PACKAGES` seit 1.1.1, unbekannter Entwickler). Optionen ohne Entscheidung: App bei Google zur Prüfung einreichen, Update nur über den Browser statt Selbstinstallation, Registrierung als Entwickler, genaue Meldung prüfen.
- **`license.js` im Release-Ordner**: Soll sie wie die übrigen Dateien gelöscht werden? Rückfrage unbeantwortet (DEC-271).
- **Zuordnung alter „Updater:“-Einträge** zu Updater-Versionen (DEC-274).
- **Nicht bestätigte Tests**: Wiederaufnahme nach kurzer Unterbrechung (Sprachnachricht, Anruf) unter DEC-256; Knopf „Beenden“, Zurück-Taste und Symbol (1.2.3); Sperre im Release-Ordner und Patchnotes-Dialog (Updater 3.5.0); „Neuerungen“ (App 1.2.4); Öffnen nach einem Update (1.2.5, noch nicht gebaut).

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-255 ersetzt durch DEC-256; DEC-258 ersetzt durch DEC-260; DEC-266 ersetzt durch DEC-267; Play-Symbol durch DEC-268.

Gegenüber früheren Sessions:
- DEC-244 (Wiedergabe im Hintergrund, vorgemerkt): umgesetzt durch DEC-252 bis DEC-262.
- DEC-245 (getrennte Patchnotes, vorgemerkt): umgesetzt durch DEC-272 und DEC-273.
- DEC-201 (Version nur je GO): ergänzt durch DEC-251 (auch je Build mit Korrekturen).
- DEC-202 (keine Debug-APK ohne Auftrag): ergänzt durch DEC-248 (Test-APKs baut der Nutzer).
- Navigation der App aus der Session 1.1.0 (Quellenliste unter STEFFLIX, Zurück führt dorthin): ersetzt durch DEC-265 und DEC-267.

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis):
- Der Updater heisst seit 4.0.0 „Desktop“ (`STEFFLIX.exe`); neue Einträge beginnen mit „Desktop x.y.z:“, „Updater x.y.z:“ wird weiter gelesen (betrifft DEC-272/DEC-273).
- „Alle Versionen“ als Schalter in Patchnotes und „Neuerungen“ (Desktop 4.0.3, App 1.3.1) ergänzt DEC-273.
- `downloads.json` gehört inzwischen ebenfalls zu den im Release-Ordner nie geschriebenen Dateien (ergänzt DEC-271).

## C. Dokumentationsbedarf

- architecture.md: Brücke App ↔ STEFFLIX für die Wiedergabe (`mediaState`, `sflixAppMedia`, `sflixAppPip`, Polyfill der Bild-im-Bild-Schnittstelle); Lebenszyklus der Wiedergabe im Hintergrund (`keepVisible`, kein `web.onPause`, Vordergrund-Dienst nur während der Wiedergabe, Lösen nach 15 s); Aufgabenstruktur der App (Quellenliste schliesst sich, `singleTask`, `CLEAR_TASK`); Sperrmodus des Updaters im Release-Ordner.
- data-model.md: Felder `component`, `componentVersion`, `short` in `news.json` und die Regel für Textanfänge; `AppPrefs`-Schlüssel `backgroundPlayback`, `pictureInPicture`, `notificationsAsked`, `updateStarted`; Liste der vom Updater angelegten Dateien (`ReleaseFolder.GeneratedFiles`).
- testing.md: Java-Prüfkompilierung mit `javac` gegen `android.jar` und die Jars aus `gradle-home` mit einer Stub-Klasse `R`; Test von `inject.js` im Vorschau-Browser mit nachgebildetem `window.StefflixApp` (vorübergehend in die `start.html` der Testumgebung eingefügt, danach über `Programmdateien_aktualisieren.bat` zurückgesetzt); `create_news.py` an einer Kopie im Scratchpad testen; Testablauf für das Öffnen nach einem Update (erst ab dem übernächsten Update testbar).

## D. Technische Risiken

- Die Wiedergabe im Hintergrund hängt am Verhalten der WebView (Audiofokus, Sichtbarkeit, Drosselung). Neue WebView-Versionen können das ändern; DEC-255 zeigte, wie schnell ein Konflikt die Wiedergabe ganz verhindert.
- Mit `keepVisible` läuft die Seite im Hintergrund mit vollem Takt weiter, solange ein Medium geladen ist (Akku), auch wenn die Wiedergabe pausiert ist.
- Nach dem Lösen des Dienstes (Pause über 15 s) kann Android den Neustart aus dem Hintergrund verweigern (z.B. Fortsetzen per Bluetooth); die Ausnahme wird verschluckt, und Android kann die App danach beenden.
- `inject.js` hängt an weiteren Namen der Weboberfläche (`#video`, `#mediaTitle`, `#next a`/`#prev a`, `.ctrlPip`, `getPlayerUI`/`getInheritedPreview`, Ereignisse `enterpictureinpicture`/`leavepictureinpicture`); Umbenennungen brechen Wiedergabe und Bild-im-Bild unbemerkt.
- Die Ansicht im Bild-im-Bild-Fenster setzt per CSS `visibility`, `transform`, `filter` usw. ausser Kraft; neue Layouts in STEFFLIX können das Video im kleinen Fenster verdecken oder verschieben.
- `singleTask` und `CLEAR_TASK` ändern das Navigationsverhalten der App grundlegend; Wege zur Quellenauswahl (Fehler, Quelle entfernt, Wechsel) beenden jetzt immer die laufende Seite.
- Die Erkennung des Release-Ordners hängt nur an der Datei `SFLIX Tools\Release\release.ps1`. Kopiert jemand `SFLIX Tools` in eine normale Installation, ist der Updater dort gesperrt und löscht beim Start dessen `install.js` und Einstellungen.
- Die getrennten Patchnotes hängen an der Schreibweise der Textanfänge in `news.txt`; ein Tippfehler ordnet einen Eintrag still STEFFLIX zu.
- Auch reine App-Korrekturen erhöhen die STEFFLIX-Version (DEC-251), weil ihr Eintrag in `news.txt` liegt; Nutzer sehen dadurch Updates von STEFFLIX ohne Änderung der Weboberfläche.
- Das Öffnen nach einem Update (DEC-269) ist unter Android 10+ in der Regel nur über die Benachrichtigung möglich; fehlt die Berechtigung für Benachrichtigungen, bleibt die App nach dem Update geschlossen.

---

# Session STEFFLIX 2.8.0–2.8.6 / Desktop 4.0.0–4.0.4 / Android-App 1.3.0–1.3.1 (Arbeit vom 01.11. bis 04.11.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session, erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-275). Die Session setzte den in DEC-243 vorgemerkten Desktop-Player um und brachte danach mehrere kleinere GOs. Gebaut und veröffentlicht hat jeweils der Nutzer mit `build_release.bat`; der Agent stellte den veröffentlichten Stand über die Dateien auf GitHub fest:

| Stand | Versionen | Inhalt |
|---|---|---|
| gebaut (13:00) | STEFFLIX 2.8.0, Desktop 4.0.0, App 1.3.0 | Desktop-Player, Umbenennung in `STEFFLIX.exe`, Präfix „Desktop“ |
| veröffentlicht | STEFFLIX 2.8.1, Desktop 4.0.1 | ein Menüeintrag „Tools“ |
| veröffentlicht | STEFFLIX 2.8.2, Desktop 4.0.2 | Startbildschirm im Fenster, Video im Querformat, Hintergrund, Touch, Laden der Tools |
| veröffentlicht | STEFFLIX 2.8.3, Desktop 4.0.3, App 1.3.1 | Schalter „Alle Versionen“ |
| veröffentlicht | STEFFLIX 2.8.4, Desktop 4.0.4 | lokales Symbol für Verknüpfungen |
| veröffentlicht | STEFFLIX 2.8.5 | feste Fläche für Videos und Bilder |
| umgesetzt, nicht gebaut | STEFFLIX 2.8.6 | Weiter/Zurück während des Autoplay-Countdowns |

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session (04.11.2026). Spätere Änderungen sind nicht berücksichtigt, Hinweise darauf stehen unter B.

Einschränkungen: „Implemented“ heisst hier: im Code umgesetzt. Den C#-Code prüfte der Agent als Bibliothek (`/target:library`) und mit einem Build über `build.ps1` (nur `dist\`, ohne Start der EXE wegen Bitdefender); die Weboberfläche testete er im Vorschau-Server, Funktionen des Desktop-Fensters mit einem nachgebildeten `window.chrome.webview`. Das echte Fenster (WebView2), die Datenübernahme, die Installation der Runtime, der Umstieg von `updateStefflix.exe`, die Verknüpfungen und das Tippen in der App testete nur der Nutzer. Eine ausdrückliche Bestätigung liegt im Verlauf nicht vor; die späteren Rückmeldungen des Nutzers (z.B. zum Symbol in der Taskleiste, zum gelben Rahmen um das Logo) zeigen aber, dass das Fenster grundsätzlich lief.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 0 |
| Implemented | 26 |
| Proposed | 0 |
| Rejected | 4 |
| Superseded | 1 |

## Desktop: Aufbau des Programms

### DEC-275: Desktop-Player und Tools in einer EXE `STEFFLIX.exe` mit WebView2
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: Doppelklick auf `STEFFLIX.exe` ohne Angaben öffnet STEFFLIX in einem eigenen Fenster (WinForms mit WebView2). Die bisherigen Tools (Hauptfenster des Updaters) öffnen sich als zweites Fenster im selben Prozess. Beide verwaltet ein `ApplicationContext` (`DesktopHost`); das Programm endet, wenn beide geschlossen sind. Neue Schalter `--tools` (nur die Tools) und `--player` (zusätzlich der Player); alle bisherigen Aufrufe (`--headless`, `--run`, `--install`, `--license` …) bleiben. `start.html` im Browser bleibt möglich. Das Programm heisst in der Oberfläche „STEFFLIX“, im Changelog „Desktop“; ab Version 4.0.0.
Begründung: Vorgemerkte Idee des Nutzers (DEC-243), eigene Version „z.B. Updater 4.0“. Ein Fenster ohne Browser, Tools im Menü.
Alternativen: Separate EXE für Player und Tools (nicht weiter diskutiert, die Idee sah von Anfang an eine EXE vor).
Konsequenzen: Gemeinsame `Settings`-Instanz für beide Fenster (sonst überschreiben sie sich gegenseitig `updater_settings.json`). Der Mutex heisst weiterhin `STEFFLIX-Updater-<Hash>`. Nach einem Selbst-Update startet das Programm mit `--updated --tools [--player]` neu. Die Hilfe, README, `start.html` und alle Texte nennen `STEFFLIX.exe` statt `updateStefflix.exe`.
Betroffene Komponenten: `src\Desktop.cs` (`DesktopHost`), `src\PlayerForm.cs`, `src\Program.cs` (`Main`/`Start`), `src\MainForm.cs`, `src\Core.cs` (`App.ExeName`, `App.Title`), `src\HelpContent.cs`, `README.md`, `start.html`
Abhängigkeiten: DEC-243 (frühere Session), DEC-276 bis DEC-289

### DEC-276: WebView2-SDK fest versioniert von NuGet, eingebettet in die EXE
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: `build.ps1` lädt das NuGet-Paket `Microsoft.Web.WebView2` in fester Version (1.0.4258.31) mit fester SHA256 nach `obj\webview2\`. `Microsoft.Web.WebView2.Core.dll` und `…WinForms.dll` sowie `WebView2Loader.dll` für x64, x86 und arm64 werden als Ressourcen in die EXE eingebettet. Beim Start lädt `WebViewLibs.Register` (erste Zeile in `Main`, der Rest in `Start` mit `NoInlining`) die verwalteten DLLs über `AssemblyResolve`; der native Loader wird nach `%LOCALAPPDATA%\STEFFLIX\webview2\<SDK>\<Architektur>` ausgepackt und über `SetLoaderDllFolderPath` gesetzt.
Begründung: Der Nutzer bevorzugt eine einzige EXE („single exe ist zu bevorzugen“). Die feste Prüfsumme schützt den Build.
Alternativen: DLLs neben der EXE (verworfen zugunsten einer Datei).
Konsequenzen: EXE etwa 1,7 MB statt 0,2 MB. WebView2-Typen dürfen nur in `PlayerForm` und `WebViewApi` vorkommen. Erfordert .NET Framework 4.6.2 oder neuer. Der native Loader liegt ausserhalb des Programmordners.
Betroffene Komponenten: `SFLIX Tools\SflixTools\build.ps1`, `src\Desktop.cs` (`WebViewLibs`, `WebViewApi`), `src\Program.cs`
Abhängigkeiten: DEC-275

### DEC-277: Seite über `file://` laden, wie im Browser
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: Das Fenster lädt `start.html` über `file://` aus dem Installationsordner (Variante A).
Begründung: Volle Sprungfähigkeit in Videos ohne Test; vom Nutzer bestätigt („von mir aus A“). Bleibt nah am Verhalten im Browser (DEC-001).
Alternativen: Virtuelle Adresse über `SetVirtualHostNameToFolderMapping` (DEC-278, verworfen).
Konsequenzen: Navigation und Nachrichten der Seite werden über den Pfad des Installationsordners geprüft (`IsInternal`).
Betroffene Komponenten: `src\PlayerForm.cs`
Abhängigkeiten: DEC-001 (frühere Session), DEC-278

### DEC-278: Virtuelle Adresse (`https://stefflix.local/`) für das Fenster
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: Keine virtuelle Adresse über `SetVirtualHostNameToFolderMapping`.
Begründung: Möglicherweise langsamer bei grossen Videos (nicht getestet); der Nutzer wählte Variante A.
Alternativen: –
Konsequenzen: Siehe DEC-277.
Betroffene Komponenten: –
Abhängigkeiten: DEC-277

### DEC-279: Fenster ohne Browser-Bedienelemente, fremde Links im Standardbrowser
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Wie in der Android-App: keine Linkvorschau (Statusleiste), kein Kontextmenü, keine DevTools, kein Zoom, keine Browser-Tastenkürzel, kein Wischen, keine Autofill-Funktionen; Links und Bilder lassen sich nicht aus dem Fenster ziehen. Kontextmenü und DevTools nur mit `"player.devtools": true` in `updater_settings.json` oder `SFLIX_DEVTOOLS=1`. Navigation ausserhalb des Installationsordners wird abgebrochen; http, https und mailto öffnen im Standardbrowser. Downloads (Datensicherung) mit „Speichern unter“. Autoplay ohne vorherigen Klick (`--autoplay-policy=no-user-gesture-required`). Dunkle Titelleiste, Grösse und Lage des Fensters werden gemerkt (`player.bounds`, `player.maximized`). Im Player kein Kopfbild bzw. Logo wie in den Tools.
Begründung: Vorgabe des Nutzers („Adresszeile und Linkvorschauen sollen nicht angezeigt werden“, „kein Logo oberhalb …, das würde den Videos Platz wegnehmen“).
Alternativen: Zoomen erlauben (vorgeschlagen, nicht gewählt).
Konsequenzen: F5 und F11 meldet STEFFLIX über die Brücke, weil die Browser-Kürzel aus sind.
Betroffene Komponenten: `src\PlayerForm.cs`, `sflix_sys\js\ui.js` (`initDesktopApp`)
Abhängigkeiten: DEC-275, DEC-280

### DEC-280: Vollbild mit F11 und beim Video-Vollbild
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: F11 schaltet das Fenster rahmenlos ins Vollbild und zurück. Wechselt ein Video ins Vollbild, geht auch das Fenster ins Vollbild; beim Verlassen kehrt es in den vorherigen Zustand zurück. War das Fenster schon per F11 im Vollbild, bleibt es dort.
Begründung: Wunsch des Nutzers („video vollbild soll auch F11 für vollbild auslösen“).
Alternativen: Medientasten, Knöpfe in der Taskleiste, Bild-im-Bild im Desktop (DEC-289, verworfen).
Konsequenzen: `ContainsFullScreenElementChanged` steuert das Fenster; F11 beendet auch ein laufendes Element-Vollbild.
Betroffene Komponenten: `src\PlayerForm.cs` (`SetFullscreen`, `ToggleFullscreen`, `OnFullScreenElementChanged`), `sflix_sys\js\ui.js`
Abhängigkeiten: DEC-279

### DEC-281: Daten des Fensters je Installation getrennt in AppData
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: Die WebView2-Daten (IndexedDB, Cache) liegen in `%LOCALAPPDATA%\STEFFLIX\webview\<installId>`; im Release-Ordner in `release`, ohne `install.js` in `shared`.
Begründung: Wie im Browser getrennt je Installation (DEC-090). Vom Nutzer bestätigt.
Alternativen: –
Konsequenzen: Fenster und Browser speichern getrennt (siehe DEC-282). Ein kopierter Ordner mit neuer Kennung beginnt im Fenster leer.
Betroffene Komponenten: `src\Desktop.cs` (`WebViewLibs.UserDataFolder`)
Abhängigkeiten: DEC-090 (frühere Session), DEC-282

### DEC-282: Datenübernahme aus dem Browser beim ersten Start, danach getrennt
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sonstiges
Entscheidung: Beim ersten Start des Fensters (nicht im Release-Ordner) fragt ein Assistent, ob Daten aus dem bisherigen Browser übernommen werden sollen. Er öffnet `start.html?export=app` mit dem gewählten Browser (direkt über dessen Programm), STEFFLIX speichert dort eine Datensicherung im Download-Ordner, der Assistent erkennt die neue Datei und übergibt sie dem Fenster, das sie ohne weitere Rückfrage übernimmt. Daneben „Sicherung wählen …“ und „Ohne Daten starten“. Browser und Fenster laufen danach getrennt weiter.
Begründung: Die WebView kann den Speicher von Edge oder Chrome nicht lesen. Vom Nutzer bestätigt (5. und 6.).
Alternativen: Nur ein Hinweis auf Export und Import von Hand (verworfen); den Browser nach der Übernahme „stilllegen“ (verworfen, „beides getrennt weiterlaufen“). Öffnen über die Shell (verworfen, weil die Angabe nach „?“ bei `file://` verloren geht).
Konsequenzen: Die Datensicherung in storage.js wurde aufgeteilt (`downloadBackup`, `parseBackupText`, `writeBackupData`, `importBackupFromDesktop`). Der Assistent erscheint nur einmal (`player.dataImportAsked`).
Betroffene Komponenten: `src\DesktopDialogs.cs` (`DataImportDialog`), `src\Desktop.cs` (`Browsers`), `src\PlayerForm.cs`, `sflix_sys\js\storage.js`, `sflix_sys\js\ui.js` (`showDesktopExportPage`), `sflix_sys\js\main.js`
Abhängigkeiten: DEC-013 (frühere Session), DEC-281

### DEC-283: Fehlende WebView2-Runtime installieren oder im Browser öffnen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sicherheit
Entscheidung: Fehlt die Runtime, erscheint ein Dialog mit „Jetzt installieren“, „Im Browser öffnen“ und „Abbrechen“. Installiert wird über den Evergreen-Bootstrapper von Microsoft (`/silent /install`); vor dem Start wird die Datei mit `WinVerifyTrust` und dem Herausgeber „Microsoft Corporation“ geprüft.
Begründung: Wunsch des Nutzers („falls möglich installiere direkt aus der App raus“). Der Bootstrapper ändert sich laufend, darum Signaturprüfung statt fester Prüfsumme.
Alternativen: Nur Hinweis mit Link zu Microsoft (ursprünglicher Vorschlag, durch Installation aus dem Programm ersetzt); feste SHA256 (nicht möglich).
Konsequenzen: Das Programm lädt und startet ein fremdes Setup; ohne Adminrechte installiert es nur für den Benutzer.
Betroffene Komponenten: `src\Desktop.cs` (`RuntimeInstaller`, `Signature`), `src\DesktopDialogs.cs` (`RuntimeDialog`)
Abhängigkeiten: DEC-276

### DEC-284: Umstieg von `updateStefflix.exe` auf `STEFFLIX.exe`
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Architektur
Entscheidung: Das Programm heisst `STEFFLIX.exe`; das Selbst-Update ab 4.0 lädt diesen Namen. Dieselbe Datei liegt vorerst auch als `updateStefflix.exe` im Repository und in `checksums.json`, damit Updater bis 3.x auf 4.x umsteigen. Startet die Datei als `updateStefflix.exe` mit Oberfläche, kopiert sie sich zu `STEFFLIX.exe` und startet diese mit `--migrated <alte Datei>`; diese biegt Verknüpfungen (Desktop, Startmenü, Taskleiste) um, legt fehlende auf Desktop und im Startmenü an und löscht die alte Datei (nur `updateStefflix.exe` im selben Ordner). Ohne Oberfläche, im Release-Ordner und mit `SFLIX_DIR` kein Umstieg. Das Setup legt auf Wunsch Verknüpfungen an (Standard ein).
Begründung: Vom Nutzer bestätigt („ok“, „zieh updateStefflix vorerst nach für ältere Versionen“). Ohne Oberfläche bleibt die alte Datei, damit Aufgaben der Windows-Aufgabenplanung weiterlaufen (Abweichung, dem Nutzer mitgeteilt).
Alternativen: `updateStefflix.exe` sofort entfernen (verworfen wegen alter Updater).
Konsequenzen: `release.ps1` kopiert `dist\STEFFLIX.exe` unter beiden Namen, `check_release.py` prüft, dass beide gleich sind; `sflix_update.py` führt beide in `FILES`.
Betroffene Komponenten: `src\Desktop.cs` (`ExeMigration`, `Shortcuts`), `src\Program.cs`, `src\InstallTask.cs`, `src\InstallForm.cs`, `SFLIX Tools\Release\release.ps1`, `check_release.py`, `sflix_update.py`
Abhängigkeiten: DEC-275, DEC-301

### DEC-285: Changelog-Präfix „Desktop“, Komponente bleibt `updater`
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sonstiges
Entscheidung: Einträge von `STEFFLIX.exe` beginnen mit „Desktop x.y.z:“; „Updater x.y.z:“ wird weiterhin gelesen. In `news.json` bleibt die Komponente `updater`; angezeigt wird überall „Desktop“ (Patchnotes, Versionskarte, Hilfe von STEFFLIX, „Neuerungen“ der App ab 1.3.0).
Begründung: Wunsch des Nutzers („Wechsle zu Desktop“). Die gleiche Komponente hält ältere Leser (Updater 3.5, STEFFLIX 2.7, App 1.2) funktionsfähig.
Alternativen: Neue Komponente „desktop“ (verworfen wegen älterer Leser).
Konsequenzen: Anpassungen in `create_news.py`, `UpdateTask.cs`, `Dialogs.cs`, `pages.js` und `NewsDialog.java`.
Betroffene Komponenten: `SFLIX Tools\Release\create_news.py`, `src\UpdateTask.cs`, `src\Dialogs.cs`, `src\MainForm.cs`, `sflix_sys\js\pages.js`, `SFLIX Tools\Android\…\NewsDialog.java`, `.claude\CLAUDE.md`
Abhängigkeiten: DEC-272 (frühere Session)

### DEC-286: Update-Prüfung beim Start durch das Programm, Hinweise der Weboberfläche im Fenster aus
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Ist der Player offen, prüft er beim Start auf Updates und zeigt den bekannten Update-Dialog; „Jetzt aktualisieren“ öffnet die Tools und startet das Update. Die Update-Banner der Weboberfläche erscheinen im Fenster nicht.
Begründung: Vom Nutzer bestätigt (9.).
Alternativen: –
Konsequenzen: Die Tools zeigen den Dialog nur, wenn kein Player offen ist (`ShowUpdateDialogOnStart`).
Betroffene Komponenten: `src\Desktop.cs` (`CheckForUpdates`), `src\MainForm.cs` (`StartUpdate`, `ShowPatchNotesFor`), `sflix_sys\js\pages.js` (`checkForUpdate`)
Abhängigkeiten: DEC-275

### DEC-287: Fensterverhalten von Player und Tools
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Schliessen des Players schliesst auch die Tools; läuft dort eine Aufgabe, fragt das Programm und bricht bei „Ja“ ab. Schliessen der Tools allein lässt den Player offen. Ein zweiter Start holt das offene Fenster nach vorne (registrierte Fensternachricht). Die Tools tragen den Reiter im Titel („STEFFLIX – Updates“).
Begründung: Vom Nutzer bestätigt (12.); zwei Fenster „STEFFLIX“ in der Taskleiste wären sonst nicht zu unterscheiden.
Alternativen: –
Konsequenzen: Der Neustart nach einem Selbst-Update schliesst den Player ohne Nachfrage (`CloseForRestart`).
Betroffene Komponenten: `src\Desktop.cs` (`DesktopHost`, `ConfirmPlayerClose`, `ActivationWindow`), `src\PlayerForm.cs`, `src\MainForm.cs`, `src\Program.cs`
Abhängigkeiten: DEC-275

### DEC-288: Nach „Daten einlesen“, Import oder Update lädt STEFFLIX neu
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Nach diesen Aufgaben meldet das Programm `dataChanged`; STEFFLIX lädt neu, während einer Wiedergabe erst über den Hinweis „Neu laden“.
Begründung: Im Fenster gibt es keinen Knopf zum Neuladen; eine laufende Wiedergabe soll nicht abbrechen.
Alternativen: –
Konsequenzen: Brücke `{event:"dataChanged"}`. Gilt auch nach DEC-306: dataChanged lädt start.html ganz neu.
Betroffene Komponenten: `src\Desktop.cs` (`OnTasksFinished`), `src\PlayerForm.cs` (`NotifyDataChanged`), `sflix_sys\js\ui.js` (`onDesktopDataChanged`, `showDesktopNotice`)
Abhängigkeiten: DEC-275

### DEC-289: Medientasten, Taskleisten-Knöpfe und Bild-im-Bild im Desktop
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Vorerst nicht umgesetzt.
Begründung: Antwort des Nutzers: „nein, ich glaube das braucht es vorerst nicht“.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-280

## Desktop: Menü, Laden und Symbol

### DEC-290: Fünf Menüeinträge für die Tools
Status: Superseded
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Im Hamburger-Menü des Fensters die Einträge Updates, Importer, Tools, Lizenz und „Hilfe zu den Tools“, je mit eigenem Reiter (umgesetzt in 2.8.0).
Begründung: Ursprünglicher Vorschlag.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-291.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`desktopMenuEntries`)
Abhängigkeiten: DEC-291

### DEC-291: Ein Menüeintrag „Tools“, öffnet immer beim Reiter „Updates“
Status: Superseded
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Die Tools stehen im Menü nur als ein Eintrag „Tools“ (Schraubenschlüssel). Er öffnet das Tools-Fenster immer beim Reiter „Updates“; ist es schon offen, kommt es nur nach vorne.
Begründung: Wunsch des Nutzers („fasse die menüpunkte für die Tools als Tools … zusammen“, „immer updates“).
Alternativen: Zuletzt benutzter Reiter oder immer „Tools“ (nicht gewählt).
Konsequenzen: Die nicht mehr gebrauchten Symbole wurden aus `iconShapes` entfernt. Version 2.8.1/4.0.1 (Hilfe in den Tools angepasst). Ersetzt durch DEC-311.
Betroffene Komponenten: `sflix_sys\js\ui.js`, `sflix_sys\js\core.js`, `src\HelpContent.cs`
Abhängigkeiten: DEC-290

### DEC-292: Tools-Fenster unsichtbar aufbauen, Ladehinweis im Player
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Das Tools-Fenster wird mit `Opacity = 0` aufgebaut und erst nach `Shown` sichtbar. Dauert das Öffnen länger als 0,3 s, zeigt STEFFLIX „Tools werden geöffnet …“ mit Lade-Ring, bis das Programm `toolsShown` meldet (höchstens 15 s).
Begründung: Rückmeldung des Nutzers: beim Öffnen zuerst eine „konsolenartige Ansicht“; Vorschlag vom Nutzer angenommen („ok“).
Alternativen: Eigenes Ladefenster mit Logo (nicht gewählt).
Konsequenzen: Gilt auch beim Start mit `--tools`. Der Fensterhintergrund des Players vor dem Laden entspricht der Seite (`#101013`).
Betroffene Komponenten: `src\Desktop.cs` (`ShowTools`), `src\PlayerForm.cs` (`NotifyToolsShown`), `sflix_sys\js\ui.js` (`openDesktopTools`), `sflix_sys\sflix.css`
Abhängigkeiten: DEC-291

### DEC-293: Startbildschirm im Fenster bei jedem Programmstart
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Im Fenster erscheint der Startbildschirm bei jedem Start von `STEFFLIX.exe` (Merker in `sessionStorage`), nicht beim Neuladen oder bei Seitenwechseln. Der Schalter „Startbildschirm überspringen“ ist im Fenster ausgeblendet. Im Browser gilt die bisherige Regel.
Begründung: Wunsch des Nutzers („wie in der App … immer angezeigt“, Schalter „ausschalten“).
Alternativen: Schalter auch im Fenster gelten lassen (nicht gewählt).
Konsequenzen: Weicht für das Fenster von DEC-148 ab. Seitenwechsel ohne Neuladen (DEC-306) zeigen ihn nicht erneut.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`showSplashscreen`), `sflix_sys\sflix.css`
Abhängigkeiten: DEC-148 (frühere Session)

### DEC-294: Kein Fokusrahmen am Logo beim Öffnen des Fensters
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Erhält ein Element der Kopfzeile den Fokus ohne vorherige Taste, Maus oder Berührung, entfernt STEFFLIX ihn wieder. Mit der Tab-Taste bleibt der Rahmen sichtbar.
Begründung: Rückmeldung des Nutzers (gelber Rahmen um „STEFFLIX“); Tastaturbedienung soll erhalten bleiben.
Alternativen: –
Konsequenzen: Nur im Fenster aktiv und nur in `.firstLevelWrap`.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`initDesktopApp`)
Abhängigkeiten: DEC-279

### DEC-295: Verknüpfungen verwenden ein lokal abgelegtes Symbol
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sonstiges
Entscheidung: Das Programm legt sein Symbol als `%LOCALAPPDATA%\STEFFLIX\stefflix.ico` ab. Neue und umgebogene Verknüpfungen verweisen darauf; bestehende Verknüpfungen auf die eigene EXE (Desktop, Startmenü, Taskleiste) passt es bei jedem Start im Hintergrund an (nicht im Release-Ordner).
Begründung: Liegt die EXE auf einem Netzlaufwerk (`M:\`), zeigt die Taskleiste beim Start über eine Verknüpfung nur das Standardsymbol; beim direkten Start stimmte es. Vom Nutzer bestätigt („ok“).
Alternativen: Feste App-Kennung (DEC-296, verworfen).
Konsequenzen: Bei bereits angehefteten Verknüpfungen kann das Symbol erst nach einer neuen Anmeldung stimmen. Version 4.0.4.
Betroffene Komponenten: `src\Desktop.cs` (`Shortcuts.IconLocation`, `RefreshIconsInBackground`), `src\Program.cs`
Abhängigkeiten: DEC-284, DEC-296

### DEC-296: Feste App-Kennung (AppUserModelID) für Fenster und Verknüpfungen
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sonstiges
Entscheidung: Keine eigene AppUserModelID.
Begründung: Vom Agenten nach der Diagnose selbst zurückgezogen: Sie würde angeheftete Verknüpfungen von den laufenden Fenstern trennen und war für das Symbolproblem nicht nötig.
Alternativen: –
Konsequenzen: Siehe DEC-295.
Betroffene Komponenten: –
Abhängigkeiten: DEC-295

## Weboberfläche (Browser, Fenster und App)

### DEC-297: Video im Querformat füllt die sichtbare Höhe
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Ist das Fenster breiter als hoch, wird ein Video so gross, dass es zusammen mit dem Titel die sichtbare Höhe füllt, auch breiter als 1280 px; Zurück/Weiter erscheinen erst beim Scrollen. Gilt nur für Videos; Musik, Bilder, Hochformat und Kinomodus bleiben unverändert, der Titel bleibt sichtbar.
Begründung: Wunsch des Nutzers; Antworten „nur videos“, Musik „so belassen“, Titel „lassen wie es ist“.
Alternativen: –
Konsequenzen: `fitVideoToViewport` setzt `--fit-width`/`--fit-height` und die Klasse `fitViewport`.
Betroffene Komponenten: `sflix_sys\js\player.js`, `sflix_sys\sflix.css`
Abhängigkeiten: DEC-300

### DEC-298: Hintergrund mit Lichtkegel (Variante C), hellere Fenster und Menüs
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Neutraler Hintergrund mit radialem Lichtkegel unter der Menüleiste (etwa `#25252c` → `#101013`), der beim Scrollen stehen bleibt; die Menüleiste bleibt dunkel. Fenster und Menüs werden heller (`--sflix-bg-elevated` `#26262c`). Gilt in Browser, Fenster und App.
Begründung: Rückmeldung des Nutzers (Menüleiste kaum vom Rest zu unterscheiden), Wunsch nach feinem Verlauf, „neutral halten“; der Nutzer wählte aus vier Vorschlägen Variante C.
Alternativen: A (dezent senkrecht), B (deutlicher senkrecht), D (diagonal); Hauch Rot (nicht gewählt).
Konsequenzen: `--sflix-bg-gradient` auch für die Rasteransicht `#cardGrid`.
Betroffene Komponenten: `sflix_sys\sflix.css`, `src\PlayerForm.cs` (`PageBack`)
Abhängigkeiten: –

### DEC-299: Touch: Erster Tipp bei ausgeblendeter Leiste zeigt nur die Leiste
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Bei Touch-Bedienung zeigt ein Tipp aufs Video bei ausgeblendeter Leiste nur die Leiste; erst ein weiterer Tipp pausiert bzw. spielt ab. Gilt überall bei Touch (Browser, App) und auch im Vollbild; Mausklicks reagieren wie bisher sofort; Doppeltipps unverändert.
Begründung: In der App pausierte der Tipp, wenn man Bild-im-Bild wählen wollte. Vom Nutzer so bestimmt („immer bei touch bedienung“, Vollbild „ja“).
Alternativen: Nur in der App (nicht gewählt).
Konsequenzen: Ändert DEC-194; keine eigene App-Version nötig.
Betroffene Komponenten: `sflix_sys\js\player.js` (`bindSurfaceEvents`, `controlsHiddenAtTap`)
Abhängigkeiten: DEC-194 (frühere Session)

### DEC-300: Feste Fläche für Videos (immer 16:9) und Bilder
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Die Fläche eines Videos steht schon vor dem Laden fest und hat immer das Format 16:9 (`aspect-ratio`, `object-fit: contain`); andere Formate bekommen schwarze Ränder; bis zum ersten Bild zeigt sie einen Lade-Kreis. Im Querformat wird ebenfalls mit 16:9 gerechnet. Bilder stehen in einer festen Fläche von 72 % der Fensterhöhe (nicht im Bild-Vollbild). Musik und Kinomodus unverändert.
Begründung: Rückmeldung des Nutzers: die Leiste Zurück/Merken/Zufall/Weiter sprang beim Laden nach oben und zurück. Vom Nutzer gewählt (Variante a, Bilder „ja“).
Alternativen: Fläche nach dem Laden an das echte Seitenverhältnis anpassen (DEC-302, verworfen).
Konsequenzen: Videos im Hoch- oder 4:3-Format nutzen die Fläche nicht ganz. Version 2.8.5.
Betroffene Komponenten: `sflix_sys\sflix.css`, `sflix_sys\js\player.js` (`fitVideoToViewport`, `isWaiting`)
Abhängigkeiten: DEC-297, DEC-302

### DEC-301: Patchnotes und Neuerungen: zuerst das Neue, Schalter „Alle Versionen“
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: In den Patchnotes von Desktop zeigt ein Schalter „Alle Versionen“ (nicht gemerkt) alle Versionen statt nur der des anstehenden Updates; Überschrift dann „Alle Neuerungen“. Die „Neuerungen“ der App zeigen zuerst nur die Neuerungen seit der installierten App-Version (ist die App aktuell, die der installierten Version), mit demselben Schalter.
Begründung: Rückfrage des Nutzers, warum nur eine Version erscheint; Entscheidung „durch einen Schalter“, „nicht merken“, App „gleich wie bei Punkt 1“.
Alternativen: Immer alle Versionen mit Markierung „Neu“, Knopf „Ältere anzeigen“ (nicht gewählt).
Konsequenzen: Version Desktop 4.0.3 und App 1.3.1; STEFFLIX 2.8.3 nur für den Changelog-Block.
Betroffene Komponenten: `src\Dialogs.cs` (`PatchNotesDialog`), `src\MainForm.cs`, `SFLIX Tools\Android\…\NewsDialog.java` (`recentItems`)
Abhängigkeiten: DEC-273 (frühere Session), DEC-285

### DEC-302: Videofläche passt sich nach dem Laden dem Seitenverhältnis an
Status: Rejected
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Nicht umgesetzt.
Begründung: Der Nutzer wählte die feste 16:9-Fläche („a) default soll 16:9 sein“); die Anpassung hätte bei anderen Formaten wieder einen Sprung verursacht.
Alternativen: –
Konsequenzen: Siehe DEC-300.
Betroffene Komponenten: –
Abhängigkeiten: DEC-300

### DEC-303: Weiter/Zurück während des Autoplay-Countdowns wie der automatische Wechsel
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: UI
Entscheidung: Ein Klick auf „Weiter“ während des Countdowns setzt Füllung und Countdown zurück, markiert ein Video als gesehen, löscht die gespeicherte Position und zählt die Autoplay-Anzahl herunter. „Zurück“ während des Countdowns setzt die Füllung zurück, markiert als gesehen und löscht die Position, die Anzahl bleibt. Ausserhalb des Countdowns unverändert. Gilt für alle Wege (Knöpfe unter dem Medium, Leiste, „Nächstes in … s“, Tasten N/P).
Begründung: Fehlerbericht des Nutzers (Animation blieb stehen, nicht als gesehen markiert). Die Regeln bestimmte der Nutzer einzeln (1. ja, 2. einverstanden, 3./4. für Zurück).
Alternativen: Zurück ohne Löschen der Position (zuerst so bestimmt, vom Nutzer vor der Umsetzung geändert: „position löschen“).
Konsequenzen: Ein Klick-Listener in der Capture-Phase auf `#next a`/`#prev a` erfasst alle Wege; der automatische Wechsel setzt den Countdown vorher zurück und zählt nicht doppelt. Version 2.8.6 (am Ende der Session nicht gebaut).
Betroffene Komponenten: `sflix_sys\js\player.js` (`finishCountdownOnNavigation`, `initPlayerControls`)
Abhängigkeiten: DEC-007 (frühere Session)

## Arbeitsweise und Versionen

### DEC-304: Veröffentlichte Versionsblöcke bleiben unverändert, neue Version auch für reine Desktop- oder App-Änderungen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sonstiges
Entscheidung: Vor jeder Umsetzung prüft der Agent auf GitHub, welche Version veröffentlicht ist. Ein bereits veröffentlichter Block in `news.txt` wird nicht umbenannt oder zusammengelegt; neue Änderungen kommen in einen neuen Block. Auch Änderungen nur an Desktop oder App erhalten einen eigenen STEFFLIX-Block (nur `version.js` und Changelog geändert, so 2.8.3 und 2.8.4).
Begründung: Der Agent hatte den Block 2.8.1 in 2.8.2 aufgehen lassen, obwohl 2.8.1 schon veröffentlicht war, und hat das korrigiert. Folgt DEC-251.
Alternativen: Nicht gebaute Versionen in die nächste aufgehen lassen (nur zulässig, wenn sie nicht veröffentlicht sind).
Konsequenzen: Nutzer sehen STEFFLIX-Updates ohne Änderung der Weboberfläche.
Betroffene Komponenten: `sflix_sys\news.txt`, `sflix_sys\version.js`
Abhängigkeiten: DEC-251 (frühere Session)

### DEC-305: Netzlaufwerke nur bei ausdrücklicher Angabe, sonst nachfragen
Status: Implemented
Datum: 2026-11-20 (Session: 01.11.–04.11.2026)
Kategorie: Sicherheit
Entscheidung: Auf Netzlaufwerken (z.B. `M:`, `Y:`, `Z:`) greift der Agent nur auf ausdrücklich genannte Ordner und darunter zu; sonst fragt er vorher. Nie durchsuchen, auflisten, Stichproben lesen oder Rechte abfragen. Die Regel steht seither in `.claude\CLAUDE.md` (vorher nur im Gedächtnis des Agenten).
Begründung: Wunsch des Nutzers („Zugriff nur erlaubt, wenn explizit angegeben. Ansonsten nachfragen“); die Regel soll mit `CLAUDE.md` gesichert sein.
Alternativen: –
Konsequenzen: Präzisiert DEC-171.
Betroffene Komponenten: `.claude\CLAUDE.md`, Gedächtnisdatei `nas-only-test-folders.md`
Abhängigkeiten: DEC-171 (frühere Session)

## A. Offene Entscheidungen (diese Session)

- **Aufgabenplanung nach dem Umstieg**: Aufgaben, die `updateStefflix.exe` aufrufen, laufen nur weiter, bis die Datei beim nächsten Start mit Oberfläche umbenannt wird. Ob das Programm diese Aufgaben anpassen soll, ist nicht entschieden (DEC-284).
- **Wie lange `updateStefflix.exe` nachgezogen wird**: „vorerst“, ohne Termin oder Version (DEC-284).
- **Nicht bestätigte Tests**: Fenster mit allen Funktionen (Vollbild, Links, Downloads, Neuladen, Schliessen mit laufender Aufgabe, zweiter Start, Fensterlage), Datenübernahme, Installation der Runtime, Umstieg und Verknüpfungen, Symbol in der Taskleiste nach 4.0.4, Tippen in der App, feste Flächen, Countdown-Klick (2.8.6, nicht gebaut).
- **Leistung von `file://` bei grossen Videos im Fenster** im Vergleich zur virtuellen Adresse wurde nie gemessen (DEC-277/DEC-278).
- **Eintrag des Ordners `docs\` in `.claude\CLAUDE.md`**: Die Datei beschreibt `docs\DECISIONS.md` nicht; laut deren Regel müsste sie das.

## B. Änderungsbedarf an bestehenden Entscheidungen

Innerhalb dieser Session: DEC-290 ersetzt durch DEC-291; bei DEC-303 die Regel für „Zurück“ (Position) vor der Umsetzung geändert.

Gegenüber früheren Sessions:
- DEC-243 (Desktop-Player, vorgemerkt): umgesetzt durch DEC-275 bis DEC-292.
- DEC-148 (Startbildschirm einmal nach Update bzw. am Tag): im Fenster ersetzt durch DEC-293.
- DEC-194 (Tipp aufs Video spielt ab oder pausiert): geändert durch DEC-299.
- DEC-272/DEC-273 (getrennte Patchnotes): ergänzt durch DEC-285 (Präfix „Desktop“) und DEC-301 („Alle Versionen“).
- DEC-171 (Netzlaufwerke nur in Testordnern): präzisiert durch DEC-305.
- DEC-090 (Speicher je Installation): auf das Fenster übertragen durch DEC-281.

Laut der heutigen CLAUDE.md (nicht Teil dieser Session, nur als Hinweis):
- Der Menüeintrag „Tools“ öffnet seit 2.8.7 den Reiter „Tools“ statt „Updates“ (betrifft DEC-291).
- Seit 2.8.7 Seitenwechsel ohne Neuladen; ob sich das auf `dataChanged` (DEC-288) oder den Startbildschirm im Fenster (DEC-293) auswirkt, ist hier nicht geprüft.

## C. Dokumentationsbedarf

- architecture.md: Aufbau von `STEFFLIX.exe` (DesktopHost mit zwei Fenstern, gemeinsame Settings, Mutex, Aktivierung per Fensternachricht, Neustart mit `--tools`/`--player`); Laden der eingebetteten WebView2-Bibliotheken (Reihenfolge `Register` vor jedem WebView2-Typ, Loader-Ordner, `SetLoaderDllFolderPath` nur einmal); Brücke `window.chrome.webview` mit allen Nachrichten (`ready`, `tools`, `fullscreen`, `reload`, `importResult`, `dataChanged`, `importBackup`, `toolsShown`); Umstieg `updateStefflix.exe` → `STEFFLIX.exe` (`--migrated`).
- data-model.md: Neue Einstellungen in `updater_settings.json` (`player.bounds`, `player.maximized`, `player.dataImportAsked`, `player.devtools`); Ablageorte in `%LOCALAPPDATA%\STEFFLIX` (`webview\<installId>`, `webview2\<SDK>\<Arch>`, `stefflix.ico`); Format der Übernahme-Sicherung `stefflix-sicherung-*.json`.
- testing.md: Prüfkompilierung mit Verweisen auf die WebView2-DLLs aus `obj\webview2\…`; Tests der Brücke im Vorschau-Server mit nachgebildetem `window.chrome.webview`; Messen von Layout-Sprüngen über die Position von `#mediaNav` vor und nach dem Laden; Nachstellen des Countdowns (`showAutoplayCountdown`, Hintergrund von `#next`); Testliste für das echte Fenster (nur durch den Nutzer).

## D. Technische Risiken

- WebView2 hängt an der Runtime von Microsoft (Evergreen); neue Runtime-Versionen können Verhalten ändern. Einige Einstellungen werden nur versucht (`Optional`), ältere Runtimes ignorieren sie still.
- Das Laden der verwalteten DLLs über `AssemblyResolve` hängt an der Reihenfolge in `Program.Main`; ein WebView2-Typ in `Program` oder `DesktopHost` könnte vor `Register` geladen werden und das Programm beim Start abbrechen lassen.
- Der Assistent der Datenübernahme erkennt die Sicherung am Dateinamen und an der Zeit im Download-Ordner; ein Browser, der nach dem Speicherort fragt oder Downloads blockiert, verhindert die automatische Übernahme.
- Der Bootstrapper wird aus dem Internet geladen und ausgeführt; die Sicherheit hängt an der Signaturprüfung.
- Der Umstieg löscht `updateStefflix.exe`; Aufgaben in der Aufgabenplanung und eigene Skripte mit diesem Namen laufen danach ins Leere.
- `RefreshIconsInBackground` ändert bei jedem Start Verknüpfungen des Benutzers (nur solche auf die eigene EXE).
- `isDesktopApp()` erkennt das Fenster an `window.chrome.webview`; andere WebView2-Programme, die `start.html` laden, würden ebenfalls als Desktop gelten.
- Die feste 16:9-Fläche verkleinert Videos in anderen Formaten; die feste Bildfläche (72 %) lässt bei breiten Bildern im Hochformat viel leeren Platz.
- `finishCountdownOnNavigation` erkennt den Countdown am sichtbaren Countdown bzw. am Hintergrund von `#next`; ändert sich die Anzeige des Countdowns, greift die Regel nicht mehr.
- Die Aktivierung eines zweiten Starts geht über eine Broadcast-Nachricht an alle Fenster; läuft noch ein Updater 3.x mit demselben Mutex, passiert beim zweiten Start nichts.

---

# Session STEFFLIX 2.8.7 / Desktop 4.0.5 (UX- und Code-Prüfung, Arbeit vom 04.11. bis 08.11.2026)

Auswertung einer weiteren, einzelnen Claude-Code-Session, erfasst am 2026-11-20. Die Nummern schliessen an (ab DEC-306). In der Session wurden zuerst Ideen gesammelt (u.a. der Reiter „Download“), danach prüfte der Agent die Ladesequenzen, die Oberfläche (Weboberfläche im Vorschau-Server, Desktop und App nur im Code) und heikle Stellen im Code. Die angenommenen Vorschläge setzte er nach dem GO des Nutzers um:

| Stand | Versionen | Inhalt |
|---|---|---|
| umgesetzt, am Ende der Session nicht gebaut | STEFFLIX 2.8.7, Desktop 4.0.5 | Seitenwechsel ohne Neuladen, Hinweise nach dem Ladescreen, Vorladen in Bändern, „Kacheln automatisch“ überall, Scrollbar im Vollbild, Menü „Tools“; Desktop: Reiter „Download“, Version beim Selbst-Update aus den Bytes |
| keine Änderungen | Android-App | – |

Grundlage ist **nur der Verlauf dieser Session**. Der Status beschreibt den Stand am Ende der Session (08.11.2026).

Einschränkungen: „Implemented“ heisst hier: im Code umgesetzt. Die Weboberfläche testete der Agent im Vorschau-Server (Seitenwechsel, Zurück, gespeicherte Scrollposition im Verlauf, „Kacheln automatisch“ bei Staffeln, CSS der Scrollbar, keine Fehler in der Konsole). Das Wiederherstellen der Scrollposition und die Sprungmarke in den Einstellungen liessen sich nicht prüfen, weil der Browser-Bereich verborgen war und dort `requestAnimationFrame` nicht lief. Den C#-Code prüfte der Agent nur als Bibliothek (`/target:library`, ohne Fehler und Warnungen), das Lesen der Version zusätzlich mit einem Python-Nachbau an der vorhandenen `STEFFLIX.exe` (Ergebnis 4.0.4). Der Download mit echten Adressen, das Fenster der EXE und alles auf dem Gerät sind ungetestet; der Nutzer baute am Ende der Session, Ergebnisse liegen nicht vor.

## Statistik (diese Session)

| Status | Anzahl |
|---|---|
| Accepted | 1 |
| Implemented | 16 |
| Proposed | 0 |
| Rejected | 8 |
| Superseded | 0 |

## Weboberfläche: Laden und Seitenwechsel

### DEC-306: Seitenwechsel ohne Neuladen von start.html
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Architektur
Entscheidung: Ordner, Suche, Favoriten, Hilfe, Einstellungen, Logo, Pfadleiste und die Zurück-Taste laden `start.html` nicht mehr neu. Beim Start wird der leere Aufbau der Bereiche `#navigation`, `#media`, `#mediaNav`, `#folderBands` gemerkt; bei jedem Wechsel (`history.pushState`) wird er wiederhergestellt und die Seite mit derselben Auswahl wie beim Start aufgebaut. Ein laufendes Medium wird dabei angehalten und seine Quelle entfernt. „Zurück“ stellt die gespeicherte Scrollposition wieder her (`history.scrollRestoration = "manual"`). Der Ladescreen erscheint nur noch beim Öffnen von STEFFLIX, der Startbildschirm nicht beim Wechsel zurück zur Startseite.
Begründung: Messungen und Code zeigten, dass jeder Seitenwechsel rund 15 Programmdateien (etwa 550 KB) und `data.js` neu vom NAS liest, die Datenbank neu öffnet und den Ladescreen zeigt; der Nutzer empfand vor allem die Seitenwechsel als langsam. Bringt in EXE, Browser und App am meisten. Vom Nutzer freigegeben („das kannst du so umsetzen“).
Alternativen: Programmdateien und `data.js` in der EXE aus dem Arbeitsspeicher liefern (siehe DEC-321, verworfen). Weiterhin Neuladen.
Konsequenzen: Zustand, der bisher mit dem Neuladen verschwand, muss beim Wechsel zurückgesetzt werden (Player, Timer, Kinomodus, Musikliste, Raster, Fenster, Suchfeld, Menümarkierung). Elemente, die `initEventListeners` beim Start in diese Bereiche einfügt (`initImageViewer`, `initShuffleButton`, `initAutoplayNavButton`), werden nach jedem Wechsel neu erzeugt. Listener auf `window` je Band entfernen sich selbst, wenn das Band weg ist. `dataChanged` aus STEFFLIX.exe lädt weiterhin ganz neu. Die App ist nicht betroffen, weil `inject.js` Elemente jedes Mal neu sucht.
Betroffene Komponenten: `sflix_sys\js\main.js` (`capturePageTemplate`, `restorePageTemplate`, `stopCurrentMedia`, `resetPage`, `navigateTo`, `showPage`, `renderCurrentPage`, `handlePageLinkClick`, `saveScrollPosition`, `softNavigation`), `sflix_sys\js\pages.js` (`startSearch`, `openHelp`, `openFavorites`, `openSettings`, `showStartPage`), `sflix_sys\js\player.js` (`loadDecodedMedia`), `sflix_sys\js\cards.js` (`initRowScrolling`)
Abhängigkeiten: DEC-307; DEC-247

### DEC-307: Später eingefügte Hinweise erst nach dem Ladescreen
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Die Update-Hinweise (neue STEFFLIX-Version, neue STEFFLIX.exe), die erst nach einer Abfrage bei GitHub feststehen, werden erst nach dem Ausblenden des Ladescreens eingefügt (`afterLoadingScreen`).
Begründung: Im Vorschau-Server nachgewiesen: Der Hinweis schob die Seite nach der Messung der sichtbaren Bilder nach unten; der Ladescreen wartete auf ein Band, das danach unterhalb des Fensters lag, und nicht auf die tatsächlich sichtbaren Bilder. Das erklärt beide vom Nutzer beobachteten Effekte (Ladescreen bleibt, obwohl alles da ist; leere Kacheln nach dem Ausblenden). Der Nutzer wählte „nach dem Ausblenden einfügen“.
Alternativen: Platz für die Hinweise im Voraus freihalten oder neu messen, wenn sich die Seite verschiebt (vorgeschlagen, nicht gewählt).
Konsequenzen: Der Hinweis erscheint kurz nach dem Ausblenden und schiebt dann die Seite. Die Hinweise auf geänderte Lizenzierung und auf die Android-App sind synchron und bleiben unverändert.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`loadingState`, `afterLoadingScreen`, `runAfterLoadingScreen`, `waitForVisibleImages`), `sflix_sys\js\pages.js` (`checkForUpdate`, `checkForUpdaterUpdate`)
Abhängigkeiten: DEC-313

### DEC-308: Vorschaubilder in Bändern im Voraus laden
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Performance
Entscheidung: Liegt ein Band nahe am sichtbaren Bereich (400 px), werden seine Vorschaubilder bis zwei Bandbreiten links und rechts vom sichtbaren Teil geladen (`IntersectionObserver` mit dem Band als Wurzel, `loading` von `lazy` auf `eager`). Zusätzlich wird `loading="lazy"` vor `src` gesetzt.
Begründung: Der Nutzer meldete, dass Bilder beim Scrollen nach rechts zu langsam nachladen. Bedingung des Nutzers: Es muss auch beim Ziehen funktionieren, nicht nur mit den Pfeilen. Weil die Lösung nur von der Scrollposition abhängt, gilt sie für Pfeile, Ziehen, Wischen und Tasten gleich.
Alternativen: Vorladen nur beim Klick auf einen Pfeil (vom Nutzer als nicht ausreichend bezeichnet).
Konsequenzen: Mehr Bilder werden geladen als bisher, aber nur in Bändern nahe am sichtbaren Bereich.
Betroffene Komponenten: `sflix_sys\js\cards.js` (`observeTrackForPreload`, `startTrackPreload`, `addRowTrack`, `createCard`)
Abhängigkeiten: –

### DEC-309: „Kacheln pro Zeile automatisch“ gilt auf allen Ordnerseiten
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Ist „Kacheln pro Zeile automatisch“ eingeschaltet, brechen auch die Bänder je Unterordner (Staffeln einer Serie, Alben) in Zeilen um und überschreiben die Voreinstellung für Ordner mit wenig Inhalt. Die Bänder behalten „Alle anzeigen“ zur Seite des Unterordners.
Begründung: Der Nutzer stellte fest, dass die Einstellung bei Ordnern mit mehreren Unterordnern nicht wirkte, und verlangte: „soll global wirken“. Ursache: `renderFolderPage` baute dort je Unterordner ein normales Band und beachtete `rowAuto` nicht.
Alternativen: Staffeln absichtlich als seitlich scrollende Bänder lassen (vom Nutzer verworfen).
Konsequenzen: Gilt im Browser, im Fenster und in der App gleich (gemeinsamer Code). **Unsicherheit**: Die Startseite und die Bänder unter dem Player („Mehr aus …“) bleiben seitlich scrollbar. Das hat der Agent so gewählt und gemeldet, der Nutzer hat es nicht ausdrücklich bestätigt.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderFolderPage`, `renderGroupedBands` mit neuem Parameter `linkHref`)
Abhängigkeiten: –

### DEC-310: Scrollbar im Vollbild unsichtbar, solange die Leiste ausgeblendet ist
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Im Video-Vollbild (Kinomodus) wird die Scrollbar der Seite unsichtbar (`scrollbar-color: transparent`), solange die Player-Leiste ausgeblendet ist. Bei Mausbewegung, Tasten und beim Scrollen erscheint sie wieder und blendet nach derselben Zeit wie die Leiste aus (`cinemaScrolling`). Im Kinomodus ist der Hintergrund der Seite schwarz.
Begründung: Fehlerbericht des Nutzers: Im Fenster von STEFFLIX.exe blieb die Scrollbar stehen, im Browser und in der App nicht. Ursache laut Agent: Chrome und Edge unter Windows 11 nutzen sich selbst ausblendende Scrollbars, WebView2 nicht. Der Nutzer fragte, ob sie beim Scrollen wieder erscheint; so wurde es umgesetzt.
Alternativen: WebView2 mit sich selbst ausblendenden Scrollbars starten (nicht gewählt, der Agent empfahl die CSS-Lösung, weil sie überall gleich wirkt). Scrollbar ganz entfernen (`scrollbar-width: none`) – verworfen, weil das Video dann beim Ein- und Ausblenden um die Breite der Scrollbar springt.
Konsequenzen: Die Breite der Scrollbar bleibt reserviert; am rechten Rand ist ein schwarzer Streifen statt einer Scrollbar.
Betroffene Komponenten: `sflix_sys\sflix.css` (`html.playerCinema`), `sflix_sys\js\player.js` (`initPlayerControls`)
Abhängigkeiten: –

### DEC-311: Menüeintrag „Tools“ öffnet den Reiter „Tools“
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Der Menüeintrag „Tools“ im Fenster von STEFFLIX.exe öffnet die Tools beim Reiter „Tools“ statt „Updates“.
Begründung: Ergebnis der UX-Prüfung: Name und Ziel passten nicht zusammen. Vom Nutzer angenommen.
Alternativen: Eintrag in „Updates und Tools“ umbenennen (vorgeschlagen, nicht gewählt).
Konsequenzen: Reine Änderung der Weboberfläche (`page: "tools"`); funktioniert auch mit bestehenden Desktop-Versionen, weil `OpenPage("tools")` schon existierte.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`desktopMenuEntries`), `src\HelpContent.cs`, `start.html`, `README.md`
Abhängigkeiten: DEC-291 (frühere Session, ersetzt in diesem Punkt)

## Desktop: Selbst-Update

### DEC-312: Version der neuen EXE direkt aus den heruntergeladenen Bytes lesen
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sicherheit
Entscheidung: `SelfUpdate.ReadVersion` liest die Dateiversion aus den Bytes der heruntergeladenen EXE (PE-Kopf → Ressource RT_VERSION → `VS_FIXEDFILEINFO`) und schreibt keine Datei mehr in den Temp-Ordner.
Begründung: Bisher wurde die EXE als `.exe` in `%TEMP%` geschrieben, um die Version zu lesen – genau das Muster, das Bitdefender als „Gen:Variant.Genie“ meldet. Sperrt der Scanner die Datei, liefert das Lesen keine Version, und das Update der EXE scheitert still („Server hat keine neuere Version (unbekannt)“). Vom Nutzer bestätigt („zu 19: ok“).
Alternativen: Version aus `versions.json` (Schlüssel `updater`) – verworfen, weil die Datei getrennt von der EXE liegt und einmal nicht dazu passen könnte. Einfache Suche nach der Kennung `0xFEEF04BD` in der ganzen Datei – verworfen, weil sie zuerst die Version der eingebetteten WebView2-DLL fand (1.0.4258).
Konsequenzen: Funktioniert nur mit gültigen PE-Dateien; sonst `null` (kein Update). Mit einem Python-Nachbau an der vorhandenen `STEFFLIX.exe` geprüft (4.0.4).
Betroffene Komponenten: `src\UpdateTask.cs` (`SelfUpdate.ReadVersion`, `FindVersionResource`, `FindResourceEntry`, `RvaToOffset`)
Abhängigkeiten: –

## Desktop: Reiter „Download“

### DEC-313: Eigener Reiter „Download“ für m3u8-Adressen (HLS)
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Architektur
Entscheidung: Neuer Reiter „Download“ in den Tools zwischen Importer und Tools. STEFFLIX.exe liest die m3u8-Playlist selbst, lädt die Teilstücke einzeln (je Download 4 Threads, Wiederholungen je Teilstück) und lässt ffmpeg am Schluss nur zusammenfügen (`-c copy`). Master-Playlists mit Qualitäten und getrennter Tonspur werden unterstützt, AES-128 mit abrufbarem Schlüssel ebenfalls. Der Reiter hat keinen Start-Knopf, sondern eine eigene Warteschlange.
Begründung: Idee des Nutzers (Download über `ffmpeg -i … -c copy`, mehrere gleichzeitig, Pausieren, Wiederholen, Abbrechen, URL bearbeiten). Mit `ffmpeg` allein gibt es kein echtes Pausieren, und bei abgelaufenen, signierten Adressen müsste der Download von vorne beginnen. Mit eigenem Laden der Teilstücke: echtes Pausieren und Fortsetzen auch nach Neustart, Wiederholen einzelner Teilstücke, genauer Fortschritt, Weiterladen mit neuer Adresse. Vom Nutzer gewählt („1. einverstanden“), Ort „Zwischen Importer und Tools“.
Alternativen: Nur ffmpeg (Pause = Prozess anhalten, Wiederholen = von vorne) – verworfen. Kachel unter „Tools“ statt eigener Reiter – nicht gewählt.
Konsequenzen: Die Reiter-Indizes in `MainForm` verschieben sich (`PageDownload` = 2). Teilstücke heissen immer `.ts` bzw. `.m4s`, weil manche Server sie als `.jpg` tarnen und neuere ffmpeg solche Endungen ablehnen. Die Medienfolge der Playlist bleibt erhalten (AES-128 ohne IV nutzt sie). Abgelehnt werden BYTERANGE und Live-Streams ohne ENDLIST. Neues Symbol `Glyph.Download`.
Betroffene Komponenten: `src\DownloadTask.cs` (`DownloadJob`, `Hls`, `Http`, `DownloadManager`), `src\DownloadPage.cs` (`DownloadPage`), `src\MainForm.cs` (`PageDownload`, `OpenPage("download")`), `src\Controls.cs` (`Glyph.Download`)
Abhängigkeiten: DEC-314 bis DEC-320

### DEC-314: Gleichzeitige Downloads einstellbar, Warteschlange
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Performance
Entscheidung: „Gleichzeitig“ 1 bis 4 Downloads, Standard 2 (Einstellung `download.parallel`), die übrigen warten. Die Verbindungsgrenze von .NET wird auf 32 angehoben.
Begründung: Vorschlag des Agenten, vom Nutzer angenommen. Die Grenze von .NET (2 Verbindungen je Server) hätte parallele Teilstücke ausgebremst.
Alternativen: –
Konsequenzen: Pro Download zusätzlich 4 Threads für Teilstücke.
Betroffene Komponenten: `src\DownloadTask.cs` (`DownloadManager.MaxParallel`, `Schedule`), `src\DownloadPage.cs`
Abhängigkeiten: DEC-313

### DEC-315: Liste der Downloads in `sflix_sys\downloads.json`, Zwischendateien lokal
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sicherheit
Entscheidung: Die Liste der Downloads überdauert einen Neustart in `sflix_sys\downloads.json` (bei Nutzern). Sie enthält Adressen mit Tokens und ist persönlich: Eintrag in `.gitignore`, `Net.ProtectedFiles` und `ReleaseFolder.GeneratedFiles`; im Release-Ordner wird nichts geschrieben und der Reiter ist gesperrt. Die Teilstücke liegen lokal in `%LOCALAPPDATA%\STEFFLIX\downloads\<Id>`; die fertige Datei kommt erst nach der Prüfung über `FileOps.MoveAcross` in den Zielordner (Name schon vorhanden: mit Zusatz über `FileOps.UniquePath`). Beim Laden wird ein Download, der noch lief, zu „Unterbrochen“ (pausiert).
Begründung: Vom Nutzer bestätigt („downloads.json auf Usersystemen für downloads anlegen“, lokale Zwischendateien „einverstanden“). Lokal ist schneller als auf dem NAS und hinterlässt keine halben Dateien in der Mediathek.
Alternativen: Zwischendateien im Zielordner auf dem NAS (nicht gewählt).
Konsequenzen: Gelöscht wird nur unterhalb des lokalen Zwischenordners (`DownloadStore.DeleteTemp`). Unterbrochene Downloads setzt der Nutzer selbst fort, sie starten nicht von allein.
Betroffene Komponenten: `src\DownloadTask.cs` (`DownloadStore`, `DownloadJob.FromJson`), `src\Net.cs` (`ProtectedFiles`), `src\Core.cs` (`ReleaseFolder.GeneratedFiles`), `.gitignore`
Abhängigkeiten: DEC-316

### DEC-316: Aufräumen nur im Reiter „Download“
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sonstiges
Entscheidung: Der Knopf „Aufräumen“ im Reiter „Download“ entfernt nach Rückfrage fertige und fehlgeschlagene Downloads aus der Liste samt ihren Zwischendateien und löscht verwaiste Zwischendateien. Verwaiste Zwischendateien (ohne Eintrag in der Liste) löscht das Programm auch beim Öffnen des Reiters. „Entfernen“ bricht einen Download ab und löscht seine Teilstücke; fertige Dateien im Zielordner bleiben immer.
Begründung: Wunsch des Nutzers („aufräumfunktion nicht vergessen für fehlerhafte oder abgebrochene downloadjobs“), Ort: „knopf in downloads reicht“.
Alternativen: Zwischendateien zusätzlich in „Ablagen leeren“ anzeigen – vom Nutzer abgelehnt.
Konsequenzen: „Ablagen leeren“ bleibt unverändert.
Betroffene Komponenten: `src\DownloadTask.cs` (`DownloadManager.Cleanup`, `Remove`, `DownloadStore.RemoveOrphans`), `src\DownloadPage.cs`
Abhängigkeiten: DEC-315

### DEC-317: Zielordner einlesen, sobald dort nichts mehr wartet oder läuft
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Architektur
Entscheidung: `data.js` wird je Zielordner aktualisiert, sobald für diesen Ordner kein Download mehr wartet oder läuft; pausierte und fehlgeschlagene Downloads halten das nicht auf. Wird ein solcher später fertig, wird der Ordner nochmals eingelesen. Läuft gerade eine andere Aufgabe in den Tools, folgt das Einlesen danach.
Begründung: Vorschlag des Agenten auf die Frage des Nutzers („Soll nach jedem download data.js updaten?“), vom Nutzer bestätigt („1. ok“). Nach jedem Download einzulesen hätte den Player jedes Mal neu laden lassen bzw. einen Hinweis gezeigt; zwei Einlesevorgänge können `data.js` nicht gleichzeitig schreiben.
Alternativen: Nach jedem einzelnen Download einlesen – verworfen.
Konsequenzen: Das Einlesen läuft über den normalen `TaskRunner` (`StartTasks(["data"], ordner)`), damit meldet STEFFLIX.exe dem Player wie gewohnt `dataChanged`.
Betroffene Komponenten: `src\DownloadTask.cs` (`DownloadManager.CheckFolderReady`, `FolderReady`), `src\MainForm.cs` (`RequestDataRead`, `RunPendingDataRead`, `StartTasks(preset, dataOverride)`, `Finished`)
Abhängigkeiten: DEC-313, DEC-318

### DEC-318: Nach dem Download prüfen, bei Bedarf konvertieren, Vorschaubild
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sonstiges
Entscheidung: Nach dem Zusammenfügen wird geprüft: alle Teilstücke vorhanden, Dauer passt zur Playlist (Toleranz 2 s bzw. 1 %), kurzer Dekodiertest an Anfang, Mitte und Ende; auf Wunsch die ganze Datei („Ganze Datei prüfen“). Danach wird sie bei Bedarf für den Browser konvertiert (`ConvertTask` mit `OnlyFiles`) und erhält ein Vorschaubild (`PreviewTask.MakeFor`), immer nur eine Nachbearbeitung auf einmal. Meldungen von ffmpeg und der Prüfung stehen je Download (Doppelklick), die Spalte „Ergebnis“ zeigt OK, Warnung oder Fehler.
Begründung: Wunsch des Nutzers (Prüfung „nicht korrupt, vollständig?“, Anzeige von ffmpeg-Fehlern und -Warnungen); die Stufen der Prüfung, die Statusspalte und die Nachbearbeitung schlug der Agent vor, der Nutzer stimmte zu.
Alternativen: Immer vollständiger Dekodiertest (dauert etwa so lange wie Abspielen im Schnelldurchlauf) – nur als Option.
Konsequenzen: Abweichungen ergeben eine Warnung, kein Abbruch; nicht lesbare Dateien ergeben einen Fehler.
Betroffene Komponenten: `src\DownloadTask.cs` (`Join`, `Verify`, `Decode`, `PostProcess`), `src\DownloadPage.cs` (`DownloadMessagesDialog`)
Abhängigkeiten: DEC-313, DEC-317

### DEC-319: Nur Streams ohne Kopierschutz
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sicherheit
Entscheidung: Der Download lädt nur Streams ohne Kopierschutz. Playlists mit SAMPLE-AES oder einem KEYFORMAT ausser `identity` (z.B. Widevine, FairPlay) werden mit einer Meldung abgelehnt. AES-128 mit frei abrufbarem Schlüssel gilt nicht als Kopierschutz. Hilfe, README und Changelog nennen die Einschränkung.
Begründung: Vom Agenten zu Beginn der Ideensammlung festgelegt (Umgehen eines Kopierschutzes wird nicht eingebaut); der Nutzer widersprach nicht.
Alternativen: –
Konsequenzen: Die Verantwortung für die geladenen Inhalte liegt beim Nutzer.
Betroffene Komponenten: `src\DownloadTask.cs` (`Hls.Parse`), `src\HelpContent.cs`, `start.html`, `README.md`
Abhängigkeiten: DEC-313

### DEC-320: Eingabe der Downloads: Zielordner, Dateiname, Qualität, Erweitert
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Im Dialog „Downloads hinzufügen“: mehrere Adressen (eine je Zeile); Zielordner aus den Ordnern von STEFFLIX (bis drei Ebenen) oder neu („Neuer Ordner …“ bzw. eintippen, wird angelegt; zuletzt gewählter wird gemerkt, `download.folder`); Dateiname mit Vorschlag der nächsten fehlenden Folge im Staffelordner (z.B. `S01E05.mp4`), bei mehreren Adressen fortlaufend nummeriert; Qualität „Beste“ als Standard, bei einer einzelnen Adresse über „Qualitäten abrufen“ wählbar; „Erweitert“ mit Referer und User-Agent. „URL bearbeiten“ ändert Adresse, Referer und User-Agent eines nicht laufenden Downloads und bietet das Fortsetzen an.
Begründung: Vorschläge des Agenten, vom Nutzer einzeln angenommen (6. einverstanden, 7. „kann man machen“, 8. „lass auswählen, default ist die beste qualität“, 9. ja).
Alternativen: –
Konsequenzen: Ziel nur innerhalb des Installationsordners (`Folders.IsInsideRoot`, keine `..`, keine Laufwerke). Nach „URL bearbeiten“ wird die Qualität über ihre Bezeichnung neu bestimmt.
Betroffene Komponenten: `src\DownloadPage.cs` (`DownloadAddDialog`, `DownloadUrlDialog`), `src\DownloadTask.cs` (`SuggestName`, `NumberedName`, `CleanFileName`, `ChangeUrl`)
Abhängigkeiten: DEC-313

### DEC-321: Laufende Downloads beim Schliessen pausieren
Status: Implemented
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Sonstiges
Entscheidung: Wird das Fenster der Tools geschlossen, während Downloads laufen, fragt es nach („Pausieren und schliessen?“) und pausiert sie; sie lassen sich beim nächsten Öffnen fortsetzen. `MainForm.IsRunning` zählt laufende Downloads mit, damit auch das Schliessen des Players nachfragt.
Begründung: Die Downloads gehören zum Fenster der Tools; ohne Nachfrage hätte das Schliessen sie unbemerkt angehalten. Vom Agenten so umgesetzt, nicht eigens mit dem Nutzer besprochen.
Alternativen: Downloads im Hintergrund weiterlaufen lassen, solange der Player offen ist (nicht umgesetzt).
Konsequenzen: Beim Schliessen über den Player erscheint die bestehende Nachfrage „In den Tools läuft noch eine Aufgabe“.
Betroffene Komponenten: `src\MainForm.cs` (`IsRunning`, `OnFormClosing`), `src\DownloadTask.cs` (`PauseAll`)
Abhängigkeiten: DEC-313

## Bewusst beibehalten und verworfen

### DEC-322: Autoplay „Unbegrenzt“ schaltet am Ende der Playliste aus
Status: Accepted
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Gibt es nach dem letzten Medium kein nächstes mehr, schaltet STEFFLIX Autoplay aus, auch bei „Unbegrenzt“. Das bleibt so.
Begründung: Vom Agenten in der Code-Prüfung als möglicher Fehler gemeldet; der Nutzer: „das ist so gewollt und kann bleiben“.
Alternativen: „Unbegrenzt“ am Ende eingeschaltet lassen – verworfen.
Konsequenzen: Keine Änderung.
Betroffene Komponenten: `sflix_sys\js\player.js` (Autoplay in der Zeitaktualisierung, `updateOptions`)
Abhängigkeiten: –

### DEC-323: Ladescreen nicht länger und nicht vollständiger warten lassen
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Performance
Entscheidung: Verworfen: Die feste Grenze von 10 s erst nach 10 s ohne neues fertiges Bild greifen lassen, vor dem Ausblenden `decode()` abwarten und Platzhalter hinter jedes Bild legen.
Begründung: Nutzer: Einige noch nicht geladene Bilder sind ihm lieber als minutenlanges Warten auf dem Ladescreen bei langsamer Verbindung.
Alternativen: Beibehaltung der Grenze von 10 s (gilt weiter).
Konsequenzen: Nach 10 s blendet der Ladescreen aus, auch wenn Bilder fehlen.
Betroffene Komponenten: `sflix_sys\js\ui.js` (`waitForVisibleImages`)
Abhängigkeiten: DEC-307

### DEC-324: Keine eigene Beschleunigung des Fensters der EXE (Punkt „EXE langsamer“ geschlossen)
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: Performance
Entscheidung: Verworfen: Programmdateien und `data.js` in STEFFLIX.exe aus dem Arbeitsspeicher liefern (z.B. über `WebResourceRequested`), wie es die App mit `FileCache` tut.
Begründung: Messung des Nutzers auf demselben Ordner auf dem NAS: EXE schneller als Firefox (Seite fertig 228 ms gegenüber 726 ms, grösstes Bild 400 ms gegenüber 1032 ms). Der empfundene Unterschied kommt vom Neuladen bei jedem Seitenwechsel, das DEC-306 entfernt. Die Speicherlösung hätte eine neue Herkunft und damit einen Umzug der Daten gebraucht. Nutzer: „punkt 7 schliessen“.
Alternativen: siehe Entscheidung.
Konsequenzen: Bleibt der Unterschied nach DEC-306 spürbar, soll er neu untersucht werden.
Betroffene Komponenten: `src\PlayerForm.cs` (unverändert)
Abhängigkeiten: DEC-306

### DEC-325: Kein „Weiterschauen“-Knopf auf Serienseiten
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Verworfen: Auf Serienseiten „Fortsetzen S01E03“ bzw. „Abspielen S01E01“ als Hauptknopf und „Zufällig abspielen“ als zweiten Knopf.
Begründung: Nutzer: „Weiterschauen“ gibt es bereits auf der Startseite, „Zufällig“ nur auf den Unterseiten.
Alternativen: –
Konsequenzen: Keine Änderung.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`renderFolderPage`, unverändert)
Abhängigkeiten: –

### DEC-326: Kein Serienname über dem Titel im Player
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Verworfen: Serie und Staffel klein über dem Titel des Players.
Begründung: Nutzer: Wer darauf geklickt hat, weiss, wo er ist; der Platz gehört dem Video.
Alternativen: –
Konsequenzen: Keine Änderung.
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-327: Untertitel „Video“ auf Kacheln bleibt
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Verworfen: Dauer statt „Video“ als Untertitel der Kacheln.
Begründung: Nutzer: Die Dauer ist nicht gespeichert, „so lassen“.
Alternativen: –
Konsequenzen: Keine Änderung.
Betroffene Komponenten: `sflix_sys\js\pages.js` (`createPlayableCard`, unverändert)
Abhängigkeiten: –

### DEC-328: Kleinere Anpassungen für das Handy und die App verworfen
Status: Rejected
Datum: 2026-11-20 (Session: 04.11.–08.11.2026)
Kategorie: UI
Entscheidung: Verworfen: (a) Suchsymbol auch auf dem Handy in der Kopfzeile, (b) Inhaltsverzeichnis der Einstellungen auf schmalen Bildschirmen einklappen, (c) Startanimation der App vorzeitig beenden, sobald STEFFLIX geladen ist.
Begründung: (a) Die Suche ist im Hamburgermenü untergebracht; (b) und (c) vom Nutzer ohne weitere Begründung abgelehnt („kannst du so belassen“, „abgelehnt“).
Alternativen: –
Konsequenzen: Keine Änderung.
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen (diese Session)

- **Startseite und Bänder unter dem Player bei „Kacheln automatisch“**: vom Agenten als seitlich scrollbar belassen, vom Nutzer nicht ausdrücklich bestätigt (DEC-309).
- **Ungetestet**: Download mit echten Adressen (Pausieren, Fortsetzen nach Neustart, URL bearbeiten, Prüfung, Nachbearbeitung, Einlesen), Seitenwechsel und Scrollposition im Fenster der EXE und in der App, Scrollbar im Vollbild der EXE, „Kacheln automatisch“ bei Staffeln im echten Ordner, Selbst-Update mit der neuen Versionsprüfung.
- **Optische Prüfung der Tools-Fenster** (Desktop) wurde nicht durchgeführt; der Agent bot an, sie nachzuholen.
- **Eintrag des Ordners `docs\` in `.claude\CLAUDE.md`** fehlt weiterhin (schon in der vorherigen Session offen).

## B. Änderungsbedarf an bestehenden Entscheidungen

- DEC-291 (Menüeintrag „Tools“ öffnet den Reiter „Updates“): ersetzt durch DEC-311.
- Seitenwechsel ohne Neuladen (DEC-306) betrifft frühere Entscheidungen zum Ladescreen (dieser erscheint nur noch beim Öffnen) und zum Startbildschirm (nicht mehr beim Wechsel zurück zur Startseite). `dataChanged` aus STEFFLIX.exe (DEC-288) lädt weiterhin ganz neu.
- DEC-307 und DEC-323 präzisieren das Verhalten des Ladescreens (Grenze 10 s bleibt).

## C. Dokumentationsbedarf

- architecture.md: Seitenwechsel ohne Neuladen (gemerkte Bereiche, Zurücksetzen des Zustands, Liste der Elemente, die beim Start in diese Bereiche eingefügt werden und nach jedem Wechsel neu entstehen müssen; Abfangen der Links; Scrollposition im Verlauf). Aufbau des Downloads (eigenes Laden der Teilstücke, ffmpeg nur zum Zusammenfügen, Nachbearbeitung, Einlesen je Zielordner über den `TaskRunner`).
- data-model.md: `sflix_sys\downloads.json` (Felder je Download, Zustände, Umwandlung von „läuft“ zu „unterbrochen“ beim Laden), Zwischenordner `%LOCALAPPDATA%\STEFFLIX\downloads\<Id>` mit `video.m3u8`/`audio.m3u8`, Teilstücken `v00000.ts`/`a00000.ts` bzw. `.m4s`, Schlüsseln und Init-Segmenten; Einstellungen `download.parallel` und `download.folder`.
- testing.md: Browser-Cache im Vorschau-Server umgehen (Dateien mit `fetch(…, {cache: "reload"})` neu holen); `requestAnimationFrame` läuft im verborgenen Browser-Bereich nicht; Messen der Ladezeiten in der EXE mit `SFLIX_DEVTOOLS=1` (Umgebungsvariable, nicht Argument; vorher alle Fenster schliessen) und dem Snippet für `largest-contentful-paint`; unter `file://` liefert der Browser keine Zeiten je Datei; Prüfung des Lesens der Version mit einem Python-Nachbau statt einer geladenen DLL (Bitdefender).

## D. Technische Risiken

- Seitenwechsel ohne Neuladen: Neuer Zustand in der Weboberfläche, der bisher mit dem Neuladen verschwand, muss in `resetPage`/`stopCurrentMedia` zurückgesetzt werden; neue Elemente, die beim Start in `#navigation`, `#media`, `#mediaNav` oder `#folderBands` eingefügt werden, müssen in `restorePageTemplate` neu erzeugt werden. Listener auf `document`/`window`, die je Seite angelegt werden, sammeln sich an.
- Ein Speicherleck bleibt möglich: Die Menüs des Players legen je Player einen Listener auf `document` an, der nicht entfernt wird (harmlos, aber wachsend).
- Download: Abhängigkeit von Servern, die Referer/User-Agent prüfen oder Tokens schnell ablaufen lassen; geänderte Anzahl Teilstücke nach neuer Adresse startet von vorne. `Proc.KillAll` beim Abbrechen einer Aufgabe in den Tools beendet auch ffmpeg-Prozesse der Downloads (der Download schlägt dann fehl und lässt sich wiederholen).
- Download: Nachbearbeitung (`ConvertTask`) kann gleichzeitig mit einem Tool desselben Ordners laufen.
- Download: Alle Teilstücke werden im Arbeitsspeicher gelesen, bevor sie geschrieben werden (je Teilstück, nicht je Datei).
- `ReadVersion` hängt an einer gültigen Versionsressource der EXE; fehlt sie, wird kein Update der EXE mehr installiert.
- Scrollbar im Vollbild: hängt an `:has()` und `scrollbar-color` (ältere WebView2-Runtimes oder Browser zeigen die Scrollbar weiterhin).

---

# STEFFLIX 2.8.8 / Desktop 4.0.6 (Rollen, Tickets und Regelwerk) (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-329: Fünf Chat-Rollen statt zwölf
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-001
Entscheidung: Für STEFFLIX gibt es fünf Chat-Rollen: Planung (Orchestrator und Product Owner), Architekt, Entwicklung (JavaScript, C#, Android und Fehleranalyse), Prüfung (QA, Security, Performance) und Release (Release Manager und Dokumentation). Start je Rolle mit /rolle-planung, /rolle-architekt, /rolle-entwicklung, /rolle-pruefung, /rolle-release; Rollenprompts in .claude\roles\.
Begründung: Benutzer: zwölf Rollen hätten bei kleinsten Anpassungen eine Flut an Tickets und Folgetickets ausgelöst; Ziel ist tokeneffizientes Arbeiten. Der Architekt wurde auf Wunsch des Benutzers als eigene Rolle ergänzt (Vorlage aus Sync2PC).
Alternativen: Zwölf Rollen nach den Vorlagen (verworfen: zu viele Übergaben, jeder Chat lädt Regeln und Dateien neu); sechs Rollen mit getrennten Entwicklern je Komponente (verworfen: die Komponenten hängen eng zusammen).
Konsequenzen: Auftrag.ps1 kennt nur noch diese fünf Rollen (Altnamen werden zugeordnet). Die zwölf Rollentexte liegen in .claude\archiv\roles\ und werden nicht gelesen.
Betroffene Komponenten: `.claude\roles\*.md`, `.claude\skills\rolle-*`, `SFLIX_Tools\Auftrag.ps1`, `.claude\CLAUDE.md`
Abhängigkeiten: –

### DEC-330: Jede Aufgabe bekommt ein Ticket; Rollenstart mit Gruppen, Arbeit erst nach Priorisierung
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-001
Entscheidung: Jede Aufgabe wird im Ticketsystem (coordination\, Auftrag.ps1) erfasst, auch wenn sie sofort erledigt wird: eigene Arbeit mit dem neuen Befehl erledigt (anlegen, übernehmen, abschliessen in einem Aufruf), Aufträge an andere Rollen mit erstellen. Beim Rollenstart zeigt die Rolle nur den eigenen Posteingang; bei mehreren Tickets schlägt liste -Gruppen Gruppen nach gemeinsamen Dateien vor (gleicher Ordner als passt dazu). Gearbeitet wird erst, wenn der Benutzer die Prioritäten nennt. Funde der Prüfung gehen gesammelt in einem Auftrag an die Entwicklung.
Begründung: Benutzer: Nachvollziehbarkeit und Dokumentation, auch für Tickets, die geöffnet und direkt geschlossen werden; Gruppen sparen Tokens, weil Dateien nur einmal gelesen werden. Folgeaufträge an einen offenen Entwickler-Chat nutzen dessen Kontext.
Alternativen: Tickets nur bei Arbeit über mehrere Sitzungen (verworfen vom Benutzer); Gruppieren durch das Modell (verworfen: mehr Tokens, weniger zuverlässig).
Konsequenzen: Neue Befehle erledigt und liste -Gruppen in Auftrag.ps1, Selbsttest Fall 18. coordination\ wird nicht gepusht.
Betroffene Komponenten: `SFLIX_Tools\Auftrag.ps1`, `SFLIX_Tools\Auftrag-Selbsttest.ps1`, `coordination\README.md`, `.claude\skills\ticket`
Abhängigkeiten: DEC-329

### DEC-331: Spezialisten als Agents nur bei Auslöser
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-001
Entscheidung: QA-, Security-, Performance- und Schnittstellenprüfungen laufen als Agents innerhalb der Rolle, die sie braucht, und nur bei einem Auslöser: sflix-explorer (Haiku), schnittstellen-pruefer, csharp-pruefer, web-tester, android-tester, perf-messer, doku-abgleich, release-pruefer (Sonnet), security-auditor (Opus). Agents sind rein lesend bzw. schreiben nur in Scratchpad, Testumgebung oder Emulator.
Begründung: Benutzer einverstanden; spart eigene Chats und Tokens; die Rollentexte des Benutzers wurden die Prompts der Agents.
Alternativen: Eigene Chat-Rollen je Spezialgebiet (verworfen, siehe DEC-329).
Konsequenzen: –
Betroffene Komponenten: `.claude\agents\*.md`, `.claude\CLAUDE.md` Abschnitt 6
Abhängigkeiten: DEC-329

### DEC-332: Entscheide festhalten ist Pflicht; DECISIONS.md nur über decisions.py
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-001
Entscheidung: Jede Rolle hält Entscheide, die sie mit dem Benutzer trifft, im selben Schritt mit SFLIX_Tools\Doku\decisions.py fest und nennt die DEC-IDs beim Abschluss des Tickets (-Entscheide, Pflicht, sonst keine). Das Skript kann zeigen, suchen, den Abhängigkeitsbaum ausgeben (auch rückwärts), neu erfassen, ergänzen, ersetzen, Punkte in A bis D eintragen, intern auslagern und prüfen. Die Blöcke mit A bis D bleiben. Einträge, die nicht öffentlich sein dürfen, stehen in docs\intern\DECISIONS-intern.md (nicht gepusht, gemeinsame Nummerierung).
Begründung: Benutzer: DECISIONS.md soll aktuell bleiben und alle Rollen sollen die wichtigen Entscheide festhalten; ein Skript erleichtert die Pflege der grossen Datei und erlaubt tiefere Analysen über den Abhängigkeitsbaum.
Alternativen: Pflege von Hand (verworfen: Datei rund 100k Tokens); Abschnitte A bis D streichen (verworfen vom Benutzer).
Konsequenzen: DEC-028, DEC-043 und DEC-149 sowie zwei Textstellen wurden in die interne Datei verschoben; check meldet verbotene Begriffe in der öffentlichen Datei.
Betroffene Komponenten: `SFLIX_Tools\Doku\decisions.py`, `docs\DECISIONS.md`, `docs\intern\DECISIONS-intern.md`, `SFLIX_Tools\Auftrag.ps1` (-Entscheide)
Abhängigkeiten: DEC-330

### DEC-333: Changelog nur über news.py
Status: Implemented
Datum: 2026-11-20
Kategorie: Release
Ticket: SFLIX-20261120-001
Entscheidung: Einträge in sflix_sys\news.txt entstehen über SFLIX_Tools\Release\news.py (eintrag, anpassen, version, suchen, zeigen, check): Typ, Präfix Desktop x.y.z bzw. Android-App x.y.z mit der Version aus Program.cs bzw. build.gradle, eine Zeile je Eintrag, Warnung bei ähnlichen Einträgen, verbotene Begriffe abgelehnt.
Begründung: Vorschlag des Benutzers: news.txt wird nicht kürzer, ein Skript verhindert Formatfehler, die create_news.py sonst überliest.
Alternativen: Weiter von Hand mit Edit.
Konsequenzen: –
Betroffene Komponenten: `SFLIX_Tools\Release\news.py`, `sflix_sys\news.txt`, `.claude\skills\changelog-eintrag`
Abhängigkeiten: DEC-174, DEC-285

### DEC-334: Versionsregeln: Major, Minor, Patch; fehlgeschlagener Build behält die Version
Status: Accepted
Datum: 2026-11-20
Kategorie: Release
Ticket: SFLIX-20261120-001
Entscheidung: Major nur bei vollständiger Überarbeitung, Minor bei neuen Features, Patch bei kleinen Änderungen, Anpassungen und Ergänzungen. Jedes GO und jede Korrektur nach einem Test erhöht die Version jeder geänderten Komponente. Schlägt ein Build fehl, wird zuerst der Fehler behoben; die Version bleibt gleich.
Begründung: Vorgabe des Benutzers vom 20.11.2026.
Alternativen: Jedes GO nur Patch (bisher, DEC-196).
Konsequenzen: Skill /version-erhoehen; Regel in CLAUDE.md und docs\RELEASE.md.
Betroffene Komponenten: `sflix_sys\version.js`, `SFLIX_Tools\SflixTools\src\Program.cs`, `SFLIX_Tools\Android\app\build.gradle`, `.claude\skills\version-erhoehen`
Abhängigkeiten: DEC-196, DEC-201, DEC-251

### DEC-335: Guard: Zugriff nur im Projekt, Netzlaufwerke gesperrt, Testumgebung immer .claude\testenv
Status: Implemented
Datum: 2026-11-20
Kategorie: Sicherheit
Ticket: SFLIX-20261120-001
Entscheidung: Ein Hook (SFLIX_Tools\Auftrag-guard.ps1, aus Sync2PC übernommen) prüft jeden Werkzeugaufruf: PowerShell-Funktionen mit Namen von Aliasen oder Cmdlets und die Array-Abflachung werden gesperrt; coordination\ nur über Auftrag.ps1; Lesen und Schreiben nur im Projekt und in freigegebenen Orten (Temp, .claude des Benutzers, STEFFLIX-, STEFFLIX-Android-, STEFFLIX-TestShare-Ordner, Android-SDK, AVDs), sonst gesperrt bzw. bei PowerShell Rückfrage; Netzlaufwerke und UNC-Pfade immer gesperrt; STEFFLIX.exe startet nur mit SFLIX_DIR auf .claude\testenv. Lesesperre für alte Fassungen des Regelwerks.
Begründung: Benutzer: Rollen dürfen nicht auf produktive STEFFLIX-Ablagen oder Ordner ausserhalb des Projekts zugreifen; Funktionen mit Alias-Namen haben schon mehrmals zu Datenverlust geführt; Meldungen nur, wenn nötig.
Alternativen: Nur Regeln ohne technische Sperre (verworfen); Lesen frei wie in Sync2PC (verworfen vom Benutzer).
Konsequenzen: Hält Versehen auf, keine absichtliche Umgehung; Pfade in PowerShell-Befehlen werden heuristisch erkannt. Selbsttest mit 60 Fällen. Der Ordner Sync2PC ist danach für alle Rollen gesperrt.
Betroffene Komponenten: `SFLIX_Tools\Auftrag-guard.ps1`, `SFLIX_Tools\Auftrag-guard-Selbsttest.ps1`, `.claude\settings.json`
Abhängigkeiten: –

### DEC-336: Prüfung und Release bauen und testen selbst; Emulator mit SMB-Testserver
Status: Implemented
Datum: 2026-11-20
Kategorie: Release
Ticket: SFLIX-20261120-001
Entscheidung: Prüfung und Release dürfen Test-Builds erstellen (Desktop build.ps1, App build_test_apk.ps1 nach dist\STEFFLIX-test.apk) und testen: Weboberfläche im Vorschau-Server, STEFFLIX.exe mit der Testumgebung (Player über Remote-Debugging von WebView2), App in eigenen Emulatoren STEFFLIX_API29/35 mit einem lokalen SMB-Testserver (impacket, Freigabe immer .claude\testenv, nur lesend). Die Rolle Release führt release.ps1 -Auto aus. Gepusht wird nur vom Benutzer über GitHub Desktop.
Begründung: Benutzer: Prüfung und Release sollen künftig kompilieren und die Anwendungen testen können; der SMB-Testserver aus Sync2PC wird für STEFFLIX angepasst.
Alternativen: Builds nur durch den Benutzer (bisherige Regel, ersetzt).
Konsequenzen: Der erste Start einer frisch gebauten EXE wird wegen Bitdefender einmal gemeinsam mit dem Benutzer ausprobiert. Fremde Emulatoren anderer Projekte werden nie angefasst (avd.ps1 -Stop beendet nur STEFFLIX_*). Debug-APKs nur im Emulator.
Betroffene Komponenten: `SFLIX_Tools\Android\build_test_apk.ps1`, `SFLIX_Tools\Test\avd.ps1`, `SFLIX_Tools\Test\smb-testshare.ps1`, `SFLIX_Tools\Test\smb-testshare.py`, `docs\TESTING.md`
Abhängigkeiten: DEC-335

### DEC-337: Entwicklungsordner heisst SFLIX_Tools
Status: Implemented
Datum: 2026-11-20
Kategorie: Desktop
Ticket: SFLIX-20261120-001
Entscheidung: Der Entwicklungsordner im Release-Ordner heisst SFLIX_Tools (vorher mit Leerzeichen). Alle Verweise wurden angepasst; Net.IsReleaseFolder erkennt beide Namen (DevFolderNames), damit STEFFLIX.exe den Release-Ordner weiter sperrt.
Begründung: Benutzer: Umbenennung war gewollt, alle Referenzen ändern, damit es zu keinen Problemen führt. Ohne Anpassung hätte STEFFLIX.exe im Release-Ordner Dateien angelegt und updater.exe, updateData.exe und sflix.js gelöscht; .gitignore schützte Signaturschlüssel und Lizenzliste nicht mehr.
Alternativen: Ordner zurück umbenennen.
Konsequenzen: Desktop 4.0.6, STEFFLIX 2.8.8 (nur Kommentare). Der erste Gradle-Build nach der Umbenennung baut neu.
Betroffene Komponenten: `SFLIX_Tools\SflixTools\src\Net.cs`, `DownloadPage.cs`, `SFLIX_Tools\Release\*`, `.gitignore`
Abhängigkeiten: DEC-271

### DEC-338: Regelwerk neu: kurze CLAUDE.md, Dokumentation in docs\
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-001
Entscheidung: Die CLAUDE.md wurde neu aufgebaut: nur Regeln für alle, Rollen, Tickets, Entscheidungen, Agents, Skills und Stand. Die Beschreibung der Dateien steht in docs\WEB.md, DESKTOP.md, ANDROID.md, RELEASE.md und TESTING.md (gepusht, ohne sensible Daten); jede Rolle liest nur das Nötige. Regeln einzelner Rollen stehen in deren Prompt. check_docs.py prüft, ob jede Datei dokumentiert ist und Dateien für Nutzer keine verbotenen Begriffe enthalten. Ideen für später werden LOW-Tickets an die Planung. Die Regel zu Bezügen auf andere Streamingdienste entfällt; ARCHITECTURE.md und INTERFACES.md erstellt der Architekt.
Begründung: Benutzer: CLAUDE.md überschreiben und neu aufbauen; tokeneffizient arbeiten (die alte Datei hatte rund 88 KB und wurde in jedem Chat geladen).
Alternativen: Referenzdateien unter .claude\docs (verworfen zugunsten von docs\); alle Regeln in CLAUDE.md (verworfen).
Konsequenzen: Alte Fassung in .claude\archiv\ (gesperrt). Die frühere Regel, vorgemerkte Ideen beim Einlesen zu nennen, entfällt (Posteingang der Planung).
Betroffene Komponenten: `.claude\CLAUDE.md`, `docs\*.md`, `SFLIX_Tools\Release\check_docs.py`
Abhängigkeiten: DEC-329, DEC-200

### DEC-339: Architektur- und Schnittstellendokumente: Aufteilung und Pflege
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-005
Entscheidung: docs\ARCHITECTURE.md beschreibt Komponenten, Laufzeit, Datenflüsse, Abläufe der Weboberfläche und Risiken; docs\INTERFACES.md die Verträge: Brücke WebView2 (cmd/event), Brücke StefflixApp, Namen der Weboberfläche, an denen Desktop und App hängen, Skriptdateien im Installationsordner, Release-Dateien, Adressen von start.html, Datensicherung und gespeicherte Schlüssel. Dateilisten bleiben in WEB/DESKTOP/ANDROID/RELEASE.md (nicht doppeln). Wer eine Schnittstelle ändert, führt INTERFACES.md im selben Schritt nach; Formatänderungen nur mit Entscheid und Migrationsplan. Inhalte, die frühere Blöcke für architecture.md und data-model.md vorgemerkt hatten, stehen jetzt in diesen beiden Dateien. Namen, die nur einem nicht öffentlichen Zweck dienen, stehen nur in inject.js, nicht in docs\.
Begründung: Auftrag SFLIX-20261120-005 (DEC-338): Zusammenhänge an einer Stelle, ohne die Dateiübersichten zu doppeln; data-model.md gibt es bewusst nicht.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: docs\ARCHITECTURE.md, docs\INTERFACES.md
Abhängigkeiten: DEC-338

### DEC-340: SMB-Testserver und Debug-Ports abseits gaengiger Werte
Status: Accepted
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-007
Entscheidung: Lokale Testdienste der Pruefung (SMB-Testserver, Remote-Debugging von WebView2) verwenden Ports abseits von 445/4445/4446 und 9222, Vorgabe des Benutzers: SMB-Testserver 47123, Remote-Debugging 47124. Standardwert in smb-testshare.ps1 und docs/TESTING.md entsprechend nachfuehren.
Begründung: Auf dem Rechner laufen Testdienste anderer Projekte auf aehnlichen Ports (z. B. 4446 fuer einen fremden Emulator); Verwechslungen und Portkonflikte vermeiden.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: Testwerkzeuge
Abhängigkeiten: DEC-336

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

- DEC-196 (Patch-Version je GO) ergänzt durch DEC-334: Minor bei neuen Features, Major nur bei vollständiger Überarbeitung, fehlgeschlagener Build behält die Version.
- DEC-200: Vorgemerkte Ideen werden nicht mehr beim Einlesen von CLAUDE.md genannt, sondern als LOW-Tickets an die Planung geführt (DEC-338).
- Regeln, nach denen nur der Benutzer EXE und APK baut, gelten nicht mehr für Test-Builds (DEC-336).
- Abschnitte B der früheren Blöcke nachgetragen (SFLIX-20261120-006): ersetzte Einträge mit „Ersetzt durch“ (u. a. DEC-134 → DEC-152, DEC-185 → DEC-211, DEC-198 → DEC-204, DEC-247 → DEC-306, DEC-291 → DEC-311, DEC-194 → DEC-299, DEC-107 → DEC-117, DEC-012 → DEC-082), vorgemerkte DEC-243/244/245 auf Implemented, Ergänzungen in den Konsequenzen der übrigen genannten Einträge; falscher Verweis von DEC-129 auf DEC-145 korrigiert (DEC-133).

## C. Dokumentationsbedarf

- docs\ARCHITECTURE.md und docs\INTERFACES.md erstellen (Architekt, Ticket offen); die Abschnitte C früherer Blöcke nennen Inhalte dafür.

## D. Technische Risiken

- Der Guard erkennt Pfade in PowerShell-Befehlen nur heuristisch; Textstellen mit UNC-Pfaden können fälschlich gesperrt werden.
- SMB-Testserver, Emulator-Start und Test-APK sind noch nicht im Ablauf ausprobiert (Ticket an die Prüfung).

---

# Desktop 4.0.7 (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-341: BiB im Desktop: eigenes Symbol, Hauptfenster minimieren und wiederherstellen
Status: Implemented
Datum: 2026-11-20
Kategorie: UI
Ticket: SFLIX-20261120-011
Entscheidung: Im Desktop-Player erhält das BiB-Fenster ein eigenes Taskleisten-Symbol (STEFFLIX-Logo mit kleinem Bild-in-Bild-Zeichen). Jeder BiB-Start (Schaltfläche, Tastenkürzel, Kontextmenü) minimiert das Hauptfenster. Beim Zurückführen öffnet sich das Hauptfenster im vorherigen Zustand (normal, maximiert, Vollbild) im Vordergrund; beim Schliessen mit X bleibt es minimiert. Browser und Android-App unverändert.
Begründung: Benutzerwunsch: nur das BiB-Bild soll sichtbar sein; X bedeutet Wiedergabe beenden und etwas anderes tun; erkennbarer Taskleisteneintrag statt leerem Symbol.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.9 (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-342: Next/Previous über Media Session überall
Status: Implemented
Datum: 2026-11-20
Kategorie: UI
Ticket: SFLIX-20261120-013
Entscheidung: Der Player setzt die Media Session API (previoustrack/nexttrack, play/pause, Metadaten) überall, wo er läuft (Desktop, Browser). Damit erscheinen Next/Previous im BiB, und Medientasten sowie die Windows-Mediensteuerung funktionieren.
Begründung: Benutzerwunsch Next/Previous im BiB; einheitliches Verhalten, Medientasten als erwünschter Nebeneffekt.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.0.7 (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-343: Lautstärke im BiB: eigenes BiB-Fenster wird zuerst bewertet
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-012
Entscheidung: Das native BiB bietet nur Stumm/Laut. Für eine stufenlose Lautstärke im BiB der Desktop-App bewertet der Architekt ein eigenes BiB-Fenster (eigenes Fenster in STEFFLIX.exe oder Document Picture-in-Picture); der Benutzer entscheidet danach. SFLIX-20261120-011 (DEC-341) bleibt bis dahin zurückgestellt. Der Browser behält das native BiB.
Begründung: Ein eigenes Fenster könnte Lautstärke, Symbol und Minimieren/Wiederherstellen gemeinsam lösen; Doppelarbeit vermeiden.
Alternativen: –
Konsequenzen: Bewertung in SFLIX-20261120-012 erledigt; der Benutzer behält das native BiB (DEC-344), SFLIX-20261120-011 ist nicht mehr zurückgestellt.
Betroffene Komponenten: –
Abhängigkeiten: DEC-341

### DEC-344: Desktop behält das native Bild-im-Bild, kein eigenes BiB-Fenster
Status: Superseded
Datum: 2026-11-20
Kategorie: UI
Ticket: SFLIX-20261120-012
Entscheidung: Im Fenster von STEFFLIX.exe bleibt Bild-im-Bild das native Fenster von Chromium (requestPictureInPicture). Es gibt kein eigenes BiB-Fenster; die Lautstärke ist im BiB nur stumm/laut, stufenlos weiterhin im Player. Vor/Zurück und Play/Pause im BiB kommen über die Media Session (DEC-342). SFLIX-20261120-011 (DEC-341: eigenes Symbol, Hauptfenster minimieren und im vorherigen Zustand wiederherstellen, X lässt minimiert) wird mit dem nativen BiB umgesetzt. Browser und Android-App unverändert.
Begründung: Benutzerentscheid nach der Bewertung in SFLIX-20261120-012 (Varianten mit Machbarkeit, Aufwand, Risiken).
Alternativen: A1 zweites Fenster mit eigener WebView2 (verworfen: Übergabe der Position mit Nachladen, Ton kurz doppelt, zweiter Seitenprozess); A3 Player-Fenster wird selbst zum kleinen obenliegenden Fenster mit schlanker Ansicht (empfohlen vom Architekten, vom Benutzer nicht gewählt); B Document Picture-in-Picture (verworfen: in WebView2 nach heutigem Wissen nicht verfügbar, nicht am Fenster geprüft; Fenster gehörte ohnehin dem WebView2-Prozess).
Konsequenzen: Das BiB-Fenster gehört dem Prozess msedgewebview2.exe: Symbol und Taskleisteneintrag aus DEC-341 lassen sich nur über das fremde Fenster setzen (Suche über CoreWebView2.BrowserProcessId, WM_SETICON) und können mit neuen Runtime-Versionen brechen. X und „Zurück zum Tab“ lösen beide leavepictureinpicture aus; unterschieden wird über den Pausenzustand des Videos (Chromium pausiert beim Schliessen). Test 21.11.2026 (SFLIX-20261120-024, Runtime 157): documentPictureInPicture ist in WebView2 vorhanden; requestWindow scheitert mit 'no window', weil PlayerForm.OnNewWindowRequested kein neues Fenster bereitstellt (die Anfrage navigiert den Player auf about:blank). Ein vom Programm bereitgestelltes Fenster (NewWindowRequested mit NewWindow) ist ungeprüft. Ersetzt durch DEC-351.
Risiken: Erkennung des BiB-Fensters und Unterscheidung X/Zurück sind Heuristiken über Verhalten von Chromium; nur am echten Fenster prüfbar.
Betroffene Komponenten: sflix_sys\js\player.js, SFLIX_Tools\SflixTools\src\PlayerForm.cs
Abhängigkeiten: DEC-341, DEC-342, DEC-343

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.8 / Desktop 4.0.6 (Rollen, Tickets und Regelwerk) (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-345: Abgeschlossene Tickets gesperrt, Nachfuehrung nur erzwungen
Status: Implemented
Datum: 2026-11-20
Kategorie: Arbeitsweise
Ticket: SFLIX-20261120-017
Entscheidung: Auftrag.ps1 lehnt bei COMPLETED/CANCELLED status, abschliessen und aendern mit Exit 3 'bereits abgeschlossen' ab. Ausnahme nur mit aendern -Erzwingen -Notiz <Grund>: nur Anhaengen bzw. -Abschnitt, nie Ersetzen; der Verlauf vermerkt [erzwungen: Grund]. -Erzwingen auf offenen Tickets ist ein Eingabefehler.
Begründung: Abgeschlossene Tickets sind Dokumentation und sollen nicht nebenbei veraendert werden; neue Arbeit gehoert in einen Folgeauftrag. Im Normalfall wird die Ausnahme nicht benoetigt (Wunsch des Benutzers).
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-330

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.0.8 (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-346: Downloader: Wiederholen beginnt von vorne
Status: Implemented
Datum: 2026-11-20
Kategorie: UI
Ticket: SFLIX-20261120-025
Entscheidung: Im Reiter Download verwirft 'Wiederholen' bei einem fehlgeschlagenen Download alle Zwischendateien (Teilstücke) und lädt komplett neu. 'Fortsetzen' bei pausierten Downloads setzt weiterhin an derselben Stelle fort.
Begründung: Beschädigte Teilstücke liessen sich mit dem bisherigen Wiederholen (nur Fortsetzen und erneutes Zusammenfügen) nicht beheben.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.10 (Arbeit vom 20.11.2026)

## Entscheidungen

### DEC-347: Kein eigenes Tastenkürzel für Bild-im-Bild
Status: Accepted
Datum: 2026-11-20
Kategorie: UI
Ticket: SFLIX-20261120-020
Entscheidung: STEFFLIX bekommt kein Tastenkürzel und kein eigenes Kontextmenü für Bild-im-Bild. BiB startet über den Knopf in der Player-Leiste; die Nennung von Tastenkürzel und Kontextmenü in DEC-341 ist kein Auftrag dafür.
Begründung: Benutzerentscheid nach Rückfrage aus SFLIX-20261120-020: P ist bereits Vorheriges, der Knopf genügt.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-341

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.0.7 (Arbeit vom 21.11.2026)

## Entscheidungen

### DEC-348: Performance der Tools: Regeln für Dateizugriffe und UI-Thread
Status: Accepted
Datum: 2026-11-21
Kategorie: Performance
Ticket: SFLIX-20261120-023
Entscheidung: Die Pakete P1 bis P5 aus SFLIX-20261120-023 werden umgesetzt. Regeln für den Desktop: (1) Ordner werden mit Metadaten aufgelistet (DirectoryInfo.EnumerateFileSystemInfos bzw. EnumerateFiles): Art, Attribute, Grösse und Änderungszeit kommen aus der Auflistung, keine zusätzliche Abfrage je Datei (File.Exists, new FileInfo, GetAttributes). (2) Im UI-Thread kein Dateizugriff im Installationsordner, kein Netzwerk und kein ffmpeg; Daten für Fenster und Dialoge werden im Hintergrund geladen oder vom Arbeitsthread mitgeliefert, die Oberfläche zeigt bis dahin einen Platzhalter. (3) Daten einlesen liest ein Vorschaubild nur, wenn es neu oder verändert ist (gemerkt mit Grösse und Änderungszeit) bzw. liest nur die Bildgrösse aus dem Dateikopf. (4) data.js bleibt byte-gleich. (5) Die Parallelität auf Laufwerken bleibt bei der Einstellung „Gleichzeitig“ (automatisch 2 bis 4). (6) Das Fenster der Tools wird beim Schliessen nur ausgeblendet und beim nächsten Öffnen wiederverwendet, sofern keine Aufgabe dagegen spricht.
Begründung: Benutzerentscheid 21.11.2026 nach der Analyse SFLIX-20261120-023: Installationen liegen oft auf einem NAS, jede Einzelabfrage kostet dort Zeit; Player und Tools teilen einen UI-Thread, Blockaden frieren beide Fenster ein.
Alternativen: Mehr Parallelität beim Durchsuchen (verworfen: NAS verträgt nur begrenzte Parallelität, Reihenfolge in data.js); eigener UI-Thread für die Tools (verworfen: grosser Umbau, WebView2 und WinForms an einen Thread gebunden).
Konsequenzen: Aufträge an die Entwicklung je Paketgruppe nach gemeinsamen Dateien; P4 (grosse Ergebnislisten) nach P1/P3. Aufträge: SFLIX-20261121-028 (P1+P3), SFLIX-20261121-029 (P2+P5), SFLIX-20261121-030 (P4, nach 028).
Risiken: Byte-Gleichheit von data.js und das Verhalten beim Teil-Einlesen müssen vorher/nachher an der Testumgebung verglichen werden; Gewinne auf dem NAS sind geschätzt.
Betroffene Komponenten: SFLIX_Tools\SflixTools\src (DataTask, Core, MainForm, Desktop, DownloadPage, EpisodeDialogs, CleanupDialog, CleanupTask, AlbumCoverTask, CompareView, MediaCache, MediaCheckTask, MergeTask, DuplicateTask, LogView)
Abhängigkeiten: –

### DEC-349: Prototyp: Document Picture-in-Picture in einem Fenster von STEFFLIX.exe
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261120-024
Entscheidung: Die Entwicklung baut und testet einen Machbarkeitsprototyp: Im Player-Fenster ruft STEFFLIX documentPictureInPicture.requestWindow auf; STEFFLIX.exe beantwortet die Anfrage (NewWindowRequested) mit einem eigenen, immer obenliegenden Fenster mit WebView2 (NewWindow), dessen Titel der Videotitel ist (DocumentTitleChanged) und das ein eigenes Symbol hat. Das Video-Element zieht ohne Neuladen ins Fenster und zurück; das Fenster enthält eigene Bedienelemente inklusive stufenloser Lautstärke. Erst nach dem Ergebnis entscheidet der Benutzer, ob das native BiB im Desktop ersetzt wird (DEC-344 gilt bis dahin). Browser und Android-App unverändert.
Begründung: Benutzerentscheid 21.11.2026 nach SFLIX-20261120-024: Titel statt „Datei auf Ihrem Gerät“; Test zeigte, dass documentPictureInPicture in WebView2 vorhanden ist, aber ein vom Programm gestelltes Fenster braucht.
Alternativen: Zweites Fenster mit eigener WebView2 und Nachladen der Datei (verworfen: Übergabe, Ton doppelt, Synchronisation); natives BiB mit Herkunft als Titel (bleibt, falls der Prototyp scheitert).
Konsequenzen: Neue Fensteranfragen mit about:blank dürfen den Player nicht mehr ersetzen (Nebenbefund aus SFLIX-20261120-024). Brücke im Prototyp: STEFFLIX kündigt das BiB-Fenster vor requestWindow an, damit STEFFLIX.exe die nächste Fensteranfrage zuordnen kann; endgültige Nachrichten legt der Architekt nach dem Prototyp in docs\INTERFACES.md fest. Auftrag: SFLIX-20261121-031. Prototyp erfolgreich (SFLIX-20261121-037); Fertigstellung nach DEC-351.
Risiken: In WebView2 nicht dokumentiert; Verhalten kann mit neuen Runtimes ändern. Zusammenspiel mit dem Player-Code (ui.media, Layout) und SFLIX-20261120-020/-027.
Betroffene Komponenten: sflix_sys\js\player.js, SFLIX_Tools\SflixTools\src\PlayerForm.cs
Abhängigkeiten: DEC-341, DEC-344

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.0.8 (Arbeit vom 21.11.2026)

## Entscheidungen

### DEC-350: Prototyp-BiB-Fenster ohne Titelleiste
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261121-031
Entscheidung: Das eigene BiB-Fenster (Document Picture-in-Picture, Prototyp) hat keine Titelleiste: Verschieben über das Videobild, Grösse über einen schmalen Rand, Schliessen (Video anhalten) und Zurück über Knöpfe in der Bedienleiste; kein Minimieren und Maximieren.
Begründung: Benutzerwunsch 21.11.2026: nur Bild und Bedienleiste sichtbar.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-349

### DEC-351: Desktop: eigenes Bild-im-Bild-Fenster (Document Picture-in-Picture), natives BiB als Rückfall
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261121-037
Entscheidung: Im Fenster von STEFFLIX.exe öffnet der Bild-im-Bild-Knopf standardmässig ein eigenes Fenster: STEFFLIX ruft documentPictureInPicture.requestWindow auf, STEFFLIX.exe stellt das Fenster (immer obenliegend, ohne Titelleiste, eigenes Symbol, Titel = Videotitel in Taskleiste und Alt+Tab), das Video-Element zieht ohne Neuladen hinein und zurück. Aussehen und Bedienung genau wie das native BiB: Bedienelemente als Symbole (Symbole des Players) über dem Bild, oben links „Zurück zum Player“, oben der Videotitel, oben rechts Schliessen, in der Mitte Vorheriges, 10 s zurück, Abspielen/Pause, 10 s vor, Nächstes, unten Fortschritt mit Zeit und Ton aus/an, zusätzlich stufenlose Lautstärke; die Bedienelemente erscheinen bei Mausbewegung und verschwinden nach etwa 2 s ohne Maus bzw. beim Verlassen des Fensters. Klick ins Bild spielt ab bzw. pausiert, Ziehen im Bild verschiebt das Fenster, ein schmaler Rand ändert die Grösse. Tastenkürzel des Players gelten auch im BiB-Fenster. Der Autoplay-Countdown erscheint auch im BiB-Fenster (Restzeit, Abbrechen). Lage und Grösse des Fensters werden gemerkt. „Zurück zum Player“: Video zurück, läuft weiter, Hauptfenster im vorherigen Zustand; Schliessen: Video zurück und angehalten, Hauptfenster bleibt minimiert (DEC-341). Wechsel zu Bild oder Musik schliesst das BiB-Fenster. Rückfall auf das native BiB, wenn documentPictureInPicture fehlt, das Fenster nicht geöffnet werden kann oder player.docpip auf false steht. Browser und Android-App behalten das native BiB.
Begründung: Benutzerentscheid 21.11.2026 nach dem Prototyp SFLIX-20261121-031 (Bericht SFLIX-20261121-037): Titel statt „Datei auf Ihrem Gerät“, stufenlose Lautstärke, X und Zurück ohne Heuristik; Benutzer: genau so wie das native BiB, mit Symbolen statt Texten.
Alternativen: Natives BiB behalten (DEC-344, ersetzt); Bedienleiste unter dem Video mit Text (Prototyp, verworfen); kein Rückfall (verworfen: ältere Runtimes, Fehler beim Öffnen).
Konsequenzen: Neue Brückennachrichten pipWindow/pipWindowReady, pipDrag, pipWindowClosed; pip mit eindeutigem paused (docs\INTERFACES.md); Einstellungen player.docpip (Standard true) und player.pipBounds. Die Heuristiken aus DEC-344 (Fenstersuche, X/Zurück über den Pausenzustand) gelten nur noch im Rückfall. SFLIX-20261120-027 wird damit für den Normalfall gegenstandslos.
Risiken: Document PiP über ein Fenster des Programms ist in WebView2 nicht dokumentiert und kann mit neuen Runtimes ändern (Rückfall fängt das ab); Ziehen und Grösse nur mit echter Maus prüfbar.
Betroffene Komponenten: sflix_sys\js\player.js, sflix_sys\js\ui.js, sflix_sys\sflix.css, SFLIX_Tools\SflixTools\src\PlayerForm.cs
Abhängigkeiten: DEC-341, DEC-342, DEC-349; DEC-344

### DEC-352: Duplikate finden bleibt ohne VirtualMode (CompareView mit nativen Häkchen)
Status: Implemented
Datum: 2026-11-21
Kategorie: Performance
Ticket: SFLIX-20261121-034
Entscheidung: Der Dialog von „Duplikate finden“ und die gemeinsame CompareView (auch in „Folgen umbenennen“, „Ablagen leeren“ und bei vorhandenen Zieldateien) bleiben normale Listen mit nativen Häkchen; kein VirtualMode, keine eigene Häkchenspalte. „Medien prüfen“ nutzt den VirtualMode (VirtualRows) bereits. Neu bewertet wird erst, wenn eine Messung mit echten Daten mehr als etwa 3 s für den Aufbau oder mehr als etwa 5'000 Zeilen zeigt.
Begründung: Regel aus DEC-348 (P4 nur bei messbarem Problem): Gemessen 1,6 s für 10'000 Zeilen einmalig beim Aufbau (mit Hinweis „Ergebnis wird aufbereitet“); Duplikat-Gruppen haben meist 2 bis 3 Dateien, realistisch Dutzende bis wenige Hundert Zeilen. VirtualMode löst kein ItemChecked aus: Häkchen, Klick, Leertaste, Sammelauswahl und die Gruppenlogik müssten nachgebaut werden, in einer Liste, die vier Dialoge nutzen; das Regressionsrisiko steht in keinem Verhältnis zum Gewinn.
Alternativen: (b) eigene Häkchenspalte und VirtualMode nur im Duplikat-Dialog (verworfen: Nachbau der Häkchen, eigene Variante der CompareView); (c) CompareView allgemein auf VirtualMode umbauen (verworfen: betrifft vier Dialoge, hohes Risiko).
Konsequenzen: Kein Umsetzungsauftrag; SFLIX-20261121-030 ist für Duplikate erledigt.
Betroffene Komponenten: SFLIX_Tools\SflixTools\src\CompareView.cs, DuplicateTask.cs, Lists.cs
Abhängigkeiten: DEC-348

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.12 (Arbeit vom 21.11.2026)

## Entscheidungen

### DEC-353: Eigenes BiB-Fenster: Rueckfall-Wartezeit, Fehlerfall, gemerkte Groesse
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261121-038
Entscheidung: Antwortet STEFFLIX.exe nicht innert 1 s auf pipWindow (statt 2 s), oder scheitert requestWindow, oeffnet STEFFLIX das native BiB und verwendet es bis zum Neuladen der Seite (docPipFailed). Gemerkt werden Lage und Breite (player.pipBounds); die Hoehe folgt beim Oeffnen dem Seitenverhaeltnis des Videos, das Fenster liegt immer ganz auf einem sichtbaren Bildschirm. Neu laden der Seite schliesst ein offenes BiB-Fenster.
Begründung: Rueckfall muss innerhalb der Nutzeraktivierung des Klicks liegen; ein verwaistes Fenster nach dem Neuladen blockierte jedes weitere BiB (Test SFLIX-20261121-038); Seitenverhaeltnis nach DEC-351.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-351

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.0.12 (Arbeit vom 21.11.2026)

## Entscheidungen

### DEC-354: Folgen umbenennen: Quellenwechsel jederzeit, gemerkte Listen nur auf Wunsch
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261121-045
Entscheidung: In Folgen umbenennen lässt sich die Quelle vor und nach dem Laden der Folgenliste in beide Richtungen wechseln (Quellenwahl in Suchdialog und Umbenennungsvorschau; ein Treffer lädt direkt, sonst Suchdialog). In der Sitzung geladene Listen bleiben gemerkt, werden beim Zurückwechseln aber nicht automatisch verwendet: der Benutzer wählt gemerkte Liste oder Neu laden. Gespeichert wird nur die Liste, mit der umbenannt wird.
Begründung: Benutzerwunsch: Quellen vergleichen, ohne abzubrechen und neu zu beginnen; Kontrolle, ob aktuelle Daten geladen werden.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.14 (Arbeit vom 21.11.2026)

## Entscheidungen

### DEC-355: Protokoll-Kopf: Zaehler bei wenig Platz gekuerzt mit Tooltip
Status: Implemented
Datum: 2026-11-21
Kategorie: UI
Ticket: SFLIX-20261121-047
Entscheidung: Die Anzahl Fehler und Hinweise neben 'Protokoll' bleibt einzeilig und wird bei wenig Platz mit '...' gekuerzt; der ganze Text erscheint als Tooltip (Label.AutoEllipsis). Die Werkzeugknoepfe rechts behalten ihren Platz.
Begründung: Pruefung 047: bei 1750 px Breite (250 %) gekuerzt. Platz schaffen hiesse Knoepfe verkleinern oder umbrechen; die Zahlen sind per Tooltip und im Protokoll selbst sichtbar.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Analyse SFLIX-20261122-049 (Architekt) (Arbeit vom 22.11.2026)

## Entscheidungen

## Sicherheit der Updates

### DEC-356: Desktop-Updates mit eigener Signatur über checksums.json absichern
Status: Accepted
Datum: 2026-11-22
Kategorie: Sicherheit
Ticket: SFLIX-20261122-049
Entscheidung: release.ps1 signiert checksums.json (Bytes mit vereinheitlichten Zeilenenden CRLF zu LF, wie bei den Prüfsummen der Textdateien) mit einem privaten RSA-3072-Schlüssel (SHA-256, PKCS#1 v1.5) und legt die Signatur Base64-kodiert als checksums.sig daneben. STEFFLIX.exe enthält zwei öffentliche Schlüssel (Haupt- und Reserveschlüssel) und prüft die Signatur in Net.LoadChecksums, der einzigen Stelle, über die Update, Setup (InstallTask) und ffmpeg (ffmpeg.json) ihre Prüfsummen beziehen. Ohne gültige Signatur wird nichts installiert, ab der ersten Version mit Prüfung (keine Übergangsphase). Der private Hauptschlüssel liegt in SFLIX_Tools/Release/signing/ (nie pushen, gesichert wie Android/signing/), der Reserveschlüssel nur offline.
Begründung: checksums.json kommt vom selben Repository wie die Dateien und schützt nur gegen Übertragungsfehler. Ein kompromittiertes Konto oder Token könnte allen Nutzern per Selbst-Update eine beliebige STEFFLIX.exe oder ein beliebiges ffmpeg ausliefern. Eine Signatur an der zentralen Stelle deckt EXE, Web-Dateien und ffmpeg.json (und damit das ffmpeg-Archiv) ab, ohne Kosten und ohne neue Abhängigkeiten (.NET-Bordmittel in C# 5 und PowerShell 5.1). Freigabe durch den Benutzer am 22.11.2026.
Alternativen: Authenticode-Zertifikat: jährliche Kosten, hilft zusätzlich bei SmartScreen und Virenscannern und schützt auch die erste Installation; später ergänzend möglich. Signatur je Datei: mehr Dateien ohne Mehrwert. Übergangsphase, in der eine fehlende Signatur akzeptiert wird: verworfen, weil Signierung und Prüfung mit demselben Release kommen und ein Angreifer die Signatur sonst einfach weglassen könnte. GPG oder Python-Paket cryptography: zusätzliche Werkzeuge.
Konsequenzen: Neue Datei checksums.sig im Repository; sie gehört in denselben Push wie checksums.json und die EXE (Push-Liste von release.ps1). check_release.py prüft die Signatur mit dem öffentlichen Schlüssel. Ältere Desktops und Updater 3.x ignorieren die Datei (rückwärtskompatibel). Schlüsselwechsel nur über eine neue EXE, die noch mit dem alten Schlüssel signiert ist; gehen beide privaten Schlüssel verloren, sind Desktop-Updates nur noch durch Neuinstallation möglich. Die App bleibt über die Android-Signatur abgesichert, versions.json bleibt unsigniert. Die Setup-EXE aus dem Browser bleibt ungeschützt. Neue Funktion: Minor-Version des Desktops. Umsetzung: SFLIX-20261122-055, Beschreibung docs/ARCHITECTURE.md Abschnitt 3.7.
Risiken: GitHub kann checksums.json und checksums.sig kurz nach einem Push uneinheitlich ausliefern: Meldung als vorübergehend (später erneut) statt als Manipulation. Signierung und Prüfung müssen die Zeilenenden gleich behandeln.
Betroffene Komponenten: Desktop (Net.cs, UpdateTask.cs, InstallTask.cs, Ffmpeg.cs), Release (release.ps1, check_release.py, create_checksums.py), .gitignore, docs/INTERFACES.md, docs/RELEASE.md, docs/ARCHITECTURE.md
Abhängigkeiten: DEC-004, DEC-226, DEC-010

## Risiken und Entscheide pflegen

### DEC-357: Lebendes Risikoregister in docs/ARCHITECTURE.md
Status: Implemented
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-049
Entscheidung: Offene technische Risiken stehen konsolidiert in docs/ARCHITECTURE.md Abschnitt 6 (ID R-nn, Komponente, Risiko und Wirkung, Status offen / in Arbeit / akzeptiert / behoben, Massnahme mit Ticket bzw. DEC). Pflege: Architekt. Andere Rollen nennen neue oder erledigte Risiken beim Ticketabschluss (-Risiken) oder per Ticket an den Architekten. Abschnitt D je Block in docs/DECISIONS.md bleibt das historische Protokoll und wird nicht nachträglich geändert.
Begründung: Die Analyse SFLIX-20261122-049 fand rund 140 Risiken in Abschnitt D über 18 Blöcke, ohne Status; viele bestehen weiter, einige sind behoben, ein Überblick fehlte. Freigabe durch den Benutzer am 22.11.2026.
Alternativen: Status je Risiko in decisions.py (risiko --status): braucht eine Formatänderung und den Umbau aller Blöcke. Eigene Datei RISKS.md: ARCHITECTURE.md ist bereits für Risiken zuständig.
Konsequenzen: Erstbestand aus der Analyse (verifizierte offene Risiken aus DECISIONS Abschnitt D und neue Funde). docs/ wird gepusht: keine persönlichen Daten, keine Angriffsdetails, keine verbotenen Begriffe im Register. Eine Pflichtregel für alle Rollen im Regelwerk wäre eine Änderung am Regelwerk und braucht die Freigabe des Benutzers.
Betroffene Komponenten: docs/ARCHITECTURE.md, docs/DECISIONS.md
Abhängigkeiten: –

## Ticketsystem

### DEC-358: Aufträge in Bearbeitung einer anderen Rolle gesperrt, Änderung nur erzwungen
Status: Implemented
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-049
Entscheidung: Auftrag.ps1 aendern lehnt Änderungen an einem Auftrag ab, der IN_PROGRESS beim Empfänger ist, wenn eine andere Rolle aufruft (Exit 3, nichts geändert). Die Meldung erklärt, dass der Empfänger die Änderung eventuell nicht mitbekommt, und verweist auf einen eigenen Auftrag (erstellen -An <Empfänger> -Abhaengigkeiten <ID>). Ausnahme mit -Erzwingen -Notiz <Grund>: Ersetzen und Anhängen erlaubt, Verlauf vermerkt [erzwungen: Grund], Warnung, dem Benutzer zu melden. Für OPEN und BLOCKED bleibt alles wie bisher (Antworten auf Offene Fragen); der Empfänger selbst und die Rolle Benutzer sind nicht betroffen.
Begründung: Eine Rolle liest die Task-Datei bei der Übernahme einmal; spätere Ergänzungen anderer Rollen gehen sonst unbemerkt verloren. Wunsch des Benutzers vom 22.11.2026.
Alternativen: Nur Warnung ohne Sperre: wird leicht übersehen. Vollständige Sperre ohne Erzwingen: verhindert nötige Korrekturen in Absprache mit dem Benutzer.
Konsequenzen: Selbsttest Fall 20; Fall 3 hängt Fragen jetzt als Empfänger an. Hilfe von aendern, coordination\README.md (Abschnitt Status und Befehlstabelle) und .claude\skills\ticket\SKILL.md nachgeführt (Freigabe des Benutzers am 22.11.2026).
Betroffene Komponenten: SFLIX_Tools\Auftrag.ps1, SFLIX_Tools\Auftrag-Selbsttest.ps1
Abhängigkeiten: DEC-345

## A. Offene Entscheidungen

- Noch als Proposed offen, Entscheid des Benutzers ausstehend: DEC-199 (faststart in „Medien prüfen“, im Code nur bei Konvertieren, Download und Zusammenfügen), DEC-172 (Rechte auf dem NAS, Aufgabe des Benutzers), DEC-274 (alte Updater-Einträge), DEC-040 (Push-Regeln 2.00, vermutlich überholt).
- Kriterium, ab wann die Übergangsdateien für alte Installationen entfallen dürfen (Risikoregister R-20).

## B. Änderungsbedarf an bestehenden Entscheidungen

- Status nachgeführt: DEC-341, DEC-342, DEC-346, DEC-350, DEC-351, DEC-352, DEC-353, DEC-354, DEC-355 sind im Code umgesetzt (Implemented). Statistik im Kopf neu berechnet.
- Doppelte Block-Überschriften (STEFFLIX 2.8.8 / Desktop 4.0.6 zweimal, Desktop 4.0.7 dreimal, Desktop 4.0.8 zweimal) und Blöcke, deren Version nicht zum Inhalt passt (z. B. Desktop 4.0.7 vom 21.11.2026 mit DEC-348/349): bleiben als Protokoll stehen; Ursache in decisions.py (neuer Block, sobald der letzte anders heisst) wird in SFLIX-20261122-053 behoben.

## C. Dokumentationsbedarf

- Offene Punkte (A) und Risiken (D) älterer Blöcke werden nicht mehr nachträglich geschlossen: der aktuelle Stand steht im Risikoregister docs/ARCHITECTURE.md Abschnitt 6 (DEC-357). Erledigt seit der Erfassung u. a.: Ordner docs\ in .claude\CLAUDE.md beschrieben.

## D. Technische Risiken

- Erstbestand des Risikoregisters R-01 bis R-42 (Datenintegrität, Update/Release, Schnittstellen/Laufzeit) aus der Analyse SFLIX-20261122-049, siehe docs/ARCHITECTURE.md Abschnitt 6. Höchste Risiken: Datenverlust beim Konvertieren, im Download, beim Schreiben von data.js und nach „Alle Daten löschen“; unsignierte Updates (DEC-356).

---

# Desktop 4.1.0 (Arbeit vom 22.11.2026)

## Entscheidungen

### DEC-359: Tools: Regeln gegen Datenverlust (Ablagen, data.js, defekte Dateien)
Status: Superseded
Datum: 2026-11-22
Kategorie: Datenintegrität
Ticket: SFLIX-20261122-050
Entscheidung: 1. Ablage ist nur ein Ordner, dessen Name mit __ beginnt (wie Ablagen leeren); Ordner mit __ mitten im Namen werden eingelesen und im Protokoll genannt. 2. Daten einlesen fragt vor dem Speichern nach, wenn bei mindestens 50 bisherigen Medien weniger als die Hälfte gefunden wird oder Ordner nicht lesbar sind (Lesefehler, nicht fehlende Rechte), und bevor ein nicht erreichbarer Ordner mit Medien aus data.js entfernt wird; ohne Oberfläche wird nicht gespeichert bzw. nicht entfernt. 3. Medien prüfen untersucht als defekt gemeldete Dateien vor dem Löschen ohne gemerktes Ergebnis neu; die Vorauswahl bleibt. 4. Verknüpfungen (Junctions) werden beim Einlesen weiter verfolgt (Mediathek kann Verknüpfungen enthalten), Entscheid zum Überspringen offen.
Begründung: Funde der Analyse SFLIX-20261122-049: Datenverlust durch NAS-Aussetzer und uneinheitliche Regeln; Vorauswahl beibehalten, damit der Ablauf für den Benutzer gleich bleibt.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-361.
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-360: Feedback-Dienst im öffentlichen Quellcode neutral benennen
Status: Accepted
Datum: 2026-11-22
Kategorie: Sonstiges
Ticket: SFLIX-20261122-054
Entscheidung: Desktop und Weboberfläche nennen den Dienst des Feedback-Kanals nicht mehr: Klasse FeedbackChannel (License.cs, FeedbackPage.cs), Kommentare neutral, die Webhook-Adresse steht als Base64 (FeedbackChannel.WebhookBase, ui.js atob). Ausgenommen bleiben die Prüfwerkzeuge (Suchmuster für verbotene Begriffe) und das Entwicklerwerkzeug Lizenz\\create_license.py.
Begründung: Benutzer am 22.11.2026 (SFLIX-20261122-054 Punkt 7): Der Dienst soll im gepushten Quellcode nicht erkennbar sein.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-361: Tools: Regeln gegen Datenverlust (Ablagen, data.js, defekte Dateien, Verknüpfungen)
Status: Accepted
Datum: 2026-11-22
Kategorie: Datenintegrität
Ticket: SFLIX-20261122-050
Entscheidung: 1. Ablage ist nur ein Ordner, dessen Name mit __ beginnt (wie Ablagen leeren); Ordner mit __ mitten im Namen werden eingelesen und im Protokoll genannt. 2. Daten einlesen fragt vor dem Speichern nach, wenn bei mindestens 50 bisherigen Medien weniger als die Hälfte gefunden wird oder Ordner nicht lesbar sind (Lesefehler, nicht fehlende Rechte), und bevor ein nicht erreichbarer Ordner mit Medien aus data.js entfernt wird; ohne Oberfläche wird nicht gespeichert bzw. nicht entfernt. 3. Medien prüfen untersucht als defekt gemeldete Dateien vor dem Löschen ohne gemerktes Ergebnis neu; die Vorauswahl bleibt. 4. Verknüpfungen auf Ordner (Junction, symbolischer Link) durchsucht kein Werkzeug (Folders.Ignore/IsLink); Daten einlesen nennt sie im Protokoll, Medien prüfen meldet sie und den Ordner darüber nie als leer.
Begründung: Funde der Analyse SFLIX-20261122-049; Benutzer am 22.11.2026: Verknüpfungen dürfen ausgelassen werden (keine bewusst angelegt). Ziel kann ausserhalb des Installationsordners liegen oder fehlen.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-359

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Werkzeuge (Arbeit vom 22.11.2026)

## Entscheidungen

### DEC-362: Release: Spiegeln in den GitHub-Klon mit sync_github.py
Status: Accepted
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-065
Entscheidung: Nach einem erfolgreichen Release und nur nach ausdrücklicher Freigabe des Benutzers spiegelt die Rolle Release den Projektordner mit SFLIX_Tools/Release/sync_github.py in den lokalen GitHub-Klon (%USERPROFILE%\Documents\GitHub\Scripts\js\SFLIX): übertragen wird, was .gitignore nicht ausschliesst; im Projekt gelöschte Dateien werden im Ziel entfernt; .git und im Ziel ignorierte Dateien bleiben unberührt. Zusätzliche Sperrliste für geheime Dateien bricht ab. Standard ist ein Probelauf; Ausführen nur nach Bestätigung, zusätzliche Warnung bei vielen Löschungen. Eigener Schritt, nicht in release.ps1.
Begründung: Benutzerwunsch: Release-Ordner und GitHub-Klon ohne Handarbeit gleich halten, ohne geheime Dateien zu veröffentlichen und ohne Leichen im Repository.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-363: Regelwerk: Zielordner GitHub-Klon nur für Release über sync_github.py
Status: Accepted
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-065
Entscheidung: Der Benutzer gibt frei, dass die Rolle Release ausschliesslich über sync_github.py in den lokalen GitHub-Klon (%USERPROFILE%\Documents\GitHub\Scripts\js\SFLIX) schreiben darf. Anzupassen durch die Planung nach Fertigstellung des Skripts: CLAUDE.md Abschnitt 1 (Zugriff), Guard Auftrag-guard.ps1, .claude/roles/RELEASE.md, Skill release-vorbereiten. Andere Rollen und andere Zugriffe auf den Ordner bleiben gesperrt.
Begründung: Ausdrückliche Benutzerfreigabe vom 22.11.2026 für die Regelwerksänderung.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.15 (Arbeit vom 22.11.2026)

## Entscheidungen

### DEC-364: .claude nicht veroeffentlichen
Status: Superseded
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-072
Entscheidung: Der Ordner .claude (Regelwerk, Rollen, Skills, Agents, Einstellungen, Aufnahmen, Testumgebung) steht als ganzer Ordner in .gitignore und wird weder gepusht noch von sync_github.py in den GitHub-Klon gespiegelt.
Begründung: Das Regelwerk nennt interne Punkte, die Nutzer nicht lesen sollen, und Pfade aus dem Benutzerordner; die Aufnahmen sind gross und nur Material. Entscheid des Benutzers beim Release Desktop 4.1.0.
Alternativen: –
Konsequenzen: Ersetzt durch DEC-365.
Betroffene Komponenten: –
Abhängigkeiten: DEC-362

### DEC-365: .claude vorlaeufig nicht veroeffentlichen
Status: Accepted
Datum: 2026-11-22
Kategorie: Arbeitsweise
Ticket: SFLIX-20261122-072
Entscheidung: Der Ordner .claude steht vorlaeufig als ganzer Ordner in .gitignore und wird weder gepusht noch von sync_github.py gespiegelt. Er soll spaeter veroeffentlicht werden: vorher Regelwerk, Rollen, Skills und Agents von internen Punkten und Pfaden aus dem Benutzerordner bereinigen und klaeren, ob die Aufnahmen mit sollen; dann die Zeile .claude/ in .gitignore entfernen (testenv, settings.local.json, old, archiv bleiben ausgeschlossen).
Begründung: Wunsch des Benutzers beim Release Desktop 4.1.0: .claude soll spaeter oeffentlich sein, aber nicht im aktuellen Zustand.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-362; DEC-364

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.16 (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-366: Verwaiste Einträge erst nach Bestätigung entfernen, Sicherung anbieten
Status: Accepted
Datum: 2026-11-23
Kategorie: UI
Ticket: SFLIX-20261122-051
Entscheidung: Einstellungen > Fehlerbehebung: Der Knopf 'Verwaiste Einträge entfernen' öffnet eine Rückfrage mit 'Sicherung exportieren', 'Endgültig entfernen' und 'Abbrechen' (wie 'Alle Daten löschen'). Erst 'Endgültig entfernen' entfernt.
Begründung: Fehlt ein Laufwerk nur vorübergehend, wären die Einträge sonst ohne Rückweg verloren (Risiko R-12).
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-367: Alle Daten löschen behält Migrationsmarken; alte Migrationen nur bei Altbestand
Status: Accepted
Datum: 2026-11-23
Kategorie: Datenintegrität
Ticket: SFLIX-20261122-051
Entscheidung: modifyLocalData('all') behält alle Schlüssel, für die isMigrationFlag gilt (v1.28, v1.33, v2.00, renamedPathsApplied, sharedDataImported, dataSignature). Die Migrationen 1.28 und 1.33 laufen nur bei Altbestand (keine Marke v2.00, Einträge vorhanden, keine Schlüssel ab 1.28/2.x wie playlist-, seen-, timestamp-./, displaySettings); sonst werden sie nur als erledigt markiert. 1.28 benennt Migrationsmarken nie um.
Begründung: Sonst benannte die Migration 1.28 nach 'Alle Daten löschen' Einstellungen und Gesehen in playlist-... um und die alte gemeinsame Datenbank wurde erneut übernommen.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.1.3 (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-368: Medien prüfen: nicht lesbare Dateien als eigene Art
Status: Accepted
Datum: 2026-11-23
Kategorie: Desktop
Ticket: SFLIX-20261123-079
Entscheidung: Scheitert ffprobe und lässt sich die Datei nicht öffnen (oder meldet ffprobe Permission denied), meldet Medien prüfen die Art 'locked' (Nicht lesbar, Hinweisfarbe, eigener Filter) statt 'defekt'. Nicht zum Löschen vorausgewählt; manuell wählbar.
Begründung: Eine vorübergehend gesperrte Datei wurde als defekt gemeldet und zum Löschen vorgeschlagen (Datenverlustgefahr).
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-370: downloads.json und ffmpeg.json: Fehler nie wie fehlende Daten behandeln
Status: Accepted
Datum: 2026-11-23
Kategorie: Datenintegrität
Ticket: SFLIX-20261123-079
Entscheidung: Ist sflix_sys nicht erreichbar, gilt downloads.json als nicht lesbar (Liste nicht speichern, keine Teilstücke löschen). Unlesbare Einzeleinträge bleiben unverändert in der Datei und ihre Teilstücke erhalten. Ist ffmpeg.json eingetragen, aber nicht lesbar, bleibt ein installiertes ffmpeg unverändert (kein Rückfall auf die eingebaute Version). Konvertieren sperrt je Ordner und Name ohne Endung.
Begründung: Sicherheitsbefunde L3, L4, L6 und M2 aus der Prüfung 079.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# STEFFLIX 2.8.17 (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-369: create_checksums.py schreibt nur mit --schreiben
Status: Accepted
Datum: 2026-11-23
Kategorie: Release
Ticket: SFLIX-20261123-079
Entscheidung: create_checksums.py wertet Argumente aus (argparse). Ohne --schreiben werden die Prüfsummen nur angezeigt, checksums.json bleibt unverändert. release.ps1 ruft es mit --schreiben auf.
Begründung: Ein Aufruf mit --help überschrieb checksums.json, danach passte die Signatur checksums.sig nicht mehr.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: –

### DEC-371: Signaturschluessel mit DPAPI ablegen
Status: Accepted
Datum: 2026-11-23
Kategorie: Sicherheit
Ticket: SFLIX-20261123-082
Entscheidung: Der private Signaturschluessel fuer checksums.sig (signing\update_key.xml) wird mit Windows-DPAPI (Bereich CurrentUser) verschluesselt abgelegt; sign_checksums.ps1 entschluesselt ihn nur im Speicher waehrend des Signierens, release.ps1 -Auto laeuft weiter ohne Eingabe. Eine unverschluesselte Sicherung liegt nur offline beim Benutzer (USB-Stick oder Passwortmanager), nie im Projekt.
Begründung: Pruefung 079 (M1): Schluessel lag unverschluesselt im Projektordner. DPAPI schuetzt gegen Kopieren der Datei auf andere Rechner oder Konten, ohne den automatischen Release zu brechen. Entscheid des Benutzers am 23.11.2026.
Alternativen: –
Konsequenzen: –
Betroffene Komponenten: –
Abhängigkeiten: DEC-356

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.1.4 (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-372: Zurückspielen älterer signierter Update-Stände verhindern (Release-Nummer in checksums.json)
Status: Accepted
Datum: 2026-11-23
Kategorie: Sicherheit
Ticket: SFLIX-20261123-083
Entscheidung: create_checksums.py schreibt bei jedem Release das Feld "_serial" in checksums.json: Zeichenkette aus 14 Ziffern, Zeitpunkt der Erzeugung in UTC (JJJJMMTThhmmss), mitsigniert über checksums.sig. Ist die bisherige checksums.json im Release-Ordner gleich hoch oder höher (falsche Uhr), nimmt das Skript deren Wert plus 1, damit die Nummer streng steigt. Der Desktop prüft in Net.LoadChecksums nach der Signatur die Nummer und merkt sich die höchste angenommene je Installation in sflix_sys\updater_settings.json (Schlüssel update.serial). Ist die Nummer kleiner als die gespeicherte, oder fehlt sie, obwohl eine gespeichert ist, wird nichts installiert (Update, Setup, ffmpeg); die Meldung nennt es als vorübergehend (später erneut versuchen), das Protokoll nennt beide Nummern. Ohne gespeicherte Nummer (neue Installation, Stände vor der Einführung) wird jeder gültig signierte Stand angenommen. check_release.py prüft, dass _serial vorhanden ist und das Format stimmt. Ein gewolltes Zurücknehmen eines Inhalts (z. B. ffmpeg auf eine ältere Version) bleibt über ein neues Release möglich.
Begründung: Seit DEC-356 sind die Prüfsummen signiert, aber jeder früher signierte Stand bleibt für immer gültig. Wer das GitHub-Konto übernimmt, könnte so eine ältere Weboberfläche oder ein älteres ffmpeg mit bekannten Lücken einspielen (ffmpeg verarbeitet auch Downloads aus dem Internet). Die EXE ist schon über den Versionsvergleich in SelfUpdate.FindNewer geschützt. Eine Nummer je signiertem Stand deckt alle Dateien einheitlich ab, auch künftige, ohne Versionsformate je Datei vergleichen zu müssen. Befund L1 aus Prüfung SFLIX-20261123-079. Freigabe durch den Benutzer am 23.11.2026.
Alternativen: Mindestversion je Datei (version.js, Version in ffmpeg.json): hängt vom Versionsformat ab, sperrt gewollte Rücknahmen und erkennt nicht, wenn sich zwischen zwei Ständen nur ffmpeg geändert hat. Ablaufdatum der Signatur (Schutz auch gegen Einfrieren): verworfen, weil Releases unregelmässig kommen und Updates sonst ohne neues Release ausfallen. Bewusst belassen: verworfen, weil das Risiko mit jedem Release wächst und die Lösung klein ist. Speicherort %LOCALAPPDATA%\STEFFLIX: verworfen, weil mehrere Installationen und die Testumgebung sich sonst gegenseitig beeinflussen.
Konsequenzen: Neues Feld _serial in checksums.json (ältere Desktops und Updater 3.x übergehen es wie _webFiles). Neuer Schlüssel update.serial in updater_settings.json. Wird updater_settings.json gelöscht, beginnt der Schutz neu (erster gültiger Stand wird angenommen). Nicht abgedeckt und bewusst belassen: Einfrieren (ein Angreifer liefert dauerhaft denselben Stand, es kommen keine Updates). Neue Funktion ohne sichtbare Änderung: Patch-Version des Desktops. Umsetzung: Auftrag an Entwicklung aus SFLIX-20261123-083. Umsetzung in Desktop 4.1.4 (SFLIX-20261123-085): create_checksums.py behält die bisherige Nummer, wenn sich der Inhalt nicht geändert hat (gleicher Stand, keine neue Signatur, nichts zu pushen). check_release.py meldet ein fehlendes _serial nur als Hinweis, weil die Prüfung vor create_checksums.py läuft; ein falsches Format bleibt ein Fehler. Nachgeführt in docs/ARCHITECTURE.md 3.7 und docs/INTERFACES.md 5 und 8.2 (SFLIX-20261123-088).
Risiken: GitHub kann kurz nach einem Push noch die alte checksums.json liefern: dann wird nichts installiert, Meldung als vorübergehend. Die Nummer darf erst gespeichert werden, wenn Signatur und Format gültig sind.
Betroffene Komponenten: Desktop (Net.cs, Core.cs Settings), Release (create_checksums.py, check_release.py), docs/INTERFACES.md, docs/ARCHITECTURE.md, docs/RELEASE.md
Abhängigkeiten: DEC-356

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Lückenhafte Videos (Architekt) (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-373: Lückenhafte Videos erkennen: Downloader, Medien prüfen und Player
Status: Accepted
Datum: 2026-11-23
Kategorie: Architektur
Ticket: SFLIX-20261123-094
Entscheidung: Videos mit Lücken (fehlende oder fehlerhafte Teilstücke eines Downloads) werden an drei Stellen erkannt. (1) Downloader: Jedes Teilstück wird beim Herunterladen und beim Fortsetzen geprüft: nicht leer, Länge gleich Content-Length (falls angegeben), keine Textantwort (HTML, JSON); unverschlüsselte .ts: Sync-Byte 0x47 im 188-Byte-Raster (ein ID3-Kopf am Anfang ist erlaubt); .m4s: lückenlose Box-Kette; verschlüsselte Teilstücke nur Länge (Vielfaches von 16). Sonst neu laden (bestehende Wiederholungen). Nach dem Zusammenfügen Lückensuche über die ganze Datei; die betroffenen Teilstücke werden über die Dauern der Playlist bestimmt, neu geladen und die Datei neu zusammengefügt (höchstens zwei Runden). Bleibt eine Lücke, wird die Datei normal abgelegt, und der Download bleibt mit „Lücke: <Länge> bei <Stelle>“ und den Knöpfen Behalten und Löschen in der Liste, auch bei 1 s und ohne Dialog. Löschen verschiebt die Datei in die Ablage wie „Medien prüfen“. (2) Medien prüfen: neue Option „Lücken suchen“ (Standard aus) und neue Art „lückenhaft“ mit Stelle und Länge, Aktionen wie bei defekten Dateien. Ein Lesevorgang je Datei: Ist die Option an, liefert derselbe ffprobe-Aufruf Format, Spuren und Pakete. Das Ergebnis steht in medien_cache.json; gelesen werden nur neue oder geänderte Dateien (Grösse, Änderungszeit), auch wenn mehrere Aufgaben gleichzeitig laufen. Das Lückenergebnis bleibt bei einem Wechsel der ffmpeg-Version erhalten. (3) Lückensuche: Zeitstempel (DTS) der Pakete der ersten Videospur ohne Dekodieren; eine Lücke ist ein Sprung zwischen zwei Paketen um mindestens 0,5 s (Schwelle mit der Testdatei kalibrieren). Downloader und Medien prüfen nutzen dieselbe Funktion. (4) Player (STEFFLIX, auch in der App): Springt die Wiedergabe ohne Spulen dreimal innerhalb von 60 s an dieselbe Stelle (±3 s) zurück, oder löst sie dort zweimal die Verbindungserkennung aus, gilt die Stelle als beschädigt: 5 s hinter die Stelle springen, kurzer Hinweis „Datei bei <Stelle> beschädigt: 5 s übersprungen“, Fund in die gemeinsame Liste und ins Protokoll. Höchstens drei Sprünge je Stelle, danach Hinweis und anhalten statt weiter zu versuchen. Liste und Protokoll: Schnittstellen-Entscheid zu damaged.js und player.log.
Begründung: Beobachtung des Benutzers: Heruntergeladene Videos zeigen die volle Länge, bleiben aber an einer Stelle hängen und springen lautlos in einer Endlosschleife zurück. Ursachen im Code: Der Downloader übernimmt jede Antwort des Servers ungeprüft, beim Fortsetzen gilt jede Teildatei mit mehr als 0 Byte als fertig, und nach dem Zusammenfügen gibt es nur drei Stichproben von je 5 s. Weil die Zeitstempel erhalten bleiben, stimmt die Gesamtdauer trotz Lücke. Freigabe durch den Benutzer am 23.11.2026.
Alternativen: Dauer der Folgen einer Staffel vergleichen (auch Vielfache für Doppelfolgen): vorerst verworfen, weil die Dauer trotz Lücke voll bleibt und Pilot, Finale oder Specials Fehlalarme ergeben. Vollständiges Dekodieren in Medien prüfen: zu langsam (Minuten je Film); bleibt im Downloader als Option „Ganze Datei prüfen“. Dialog im Downloader: verworfen, Downloads laufen im Hintergrund. Kurze Lücken (unter 2 s) ohne Frage behalten: verworfen, der Benutzer will immer entscheiden.
Konsequenzen: Neue Funktionen: Minor-Version von STEFFLIX und Desktop. Die Lückensuche liest jede Datei ganz (über das NAS etwa so lange wie Kopieren), deshalb Option und Cache. Bitfehler ohne Lücke erkennt nur volles Dekodieren. Tests mit einer künstlich beschädigten Testdatei aus dem Beispielvideo der Testumgebung. Umsetzung in Desktop 4.2.0 und STEFFLIX 2.9.0 (SFLIX-20261123-092/093) mit zwei Abweichungen: Format und Spuren kommen aus dem bestehenden Kopf-Aufruf von ffprobe, die Pakete aus einem zweiten Aufruf mit -select_streams (die Datei wird trotzdem nur einmal ganz gelesen); Funde der Art „lückenhaft“ sind in Medien prüfen nicht zum Löschen vorausgewählt (meist noch brauchbar), gelöscht wird ohne erneute Prüfung. Die Lücke gilt als Abstand zweier Pakete über dem Median-Bildabstand plus 0,5 s (Paketdauer untauglich). Nachgeführt in docs/ARCHITECTURE.md 3.8 und docs/INTERFACES.md (SFLIX-20261123-098).
Risiken: Das lautlose Zurückspringen kommt vom Browser; wie es sich in WebView2 und in der WebView der App genau zeigt, muss mit der Testdatei geprüft werden. Falsche Treffer beim Player (z. B. echte Verbindungsprobleme an derselben Stelle) sind möglich, deshalb nur Hinweis und Sprung, keine Änderung an der Datei.
Betroffene Komponenten: STEFFLIX (player.js, Einstellungen), Desktop (DownloadTask.cs, DownloadPage.cs, MediaCheckTask.cs, MediaCache.cs, Ffmpeg.cs), App (über den gemeinsamen Player)
Abhängigkeiten: –

### DEC-374: Gemeinsame Liste beschädigter Dateien (damaged.js) und player.log
Status: Accepted
Datum: 2026-11-23
Kategorie: Architektur
Ticket: SFLIX-20261123-094
Entscheidung: Liste: sflix_sys/damaged.js, vom Desktop geschrieben (atomar über .tmp, UTF-8 ohne BOM) und von start.html wie renamed.js geladen (fehlt sie, ist die Liste leer). Inhalt: var damagedMedia = {"version": 1, "items": [{"path": "./Serien/…/S01E01.mp4", "size": <Bytes>, "gaps": [{"at": <Sekunden>, "length": <Sekunden oder null>, "source": "download"|"check"|"player", "date": "<ISO-Zeit UTC>"}]}]}; Pfad wie in data.js und bei den Zeitstempeln (relativ, ./ und /). Ein Eintrag je Datei, Lücken an derselben Stelle (±3 s) zusammengefasst. Beim Schreiben entfernt der Desktop Einträge, deren Datei fehlt oder deren Grösse sich geändert hat; höchstens 2000 Einträge (die ältesten fallen weg). Brücke Weboberfläche → Desktop: {"cmd": "damaged", "path", "at", "skipped"} (Fund des Players: Liste ergänzen, Zeile in player.log) und {"cmd": "damagedRemove", "path"} (Eintrag entfernen). Ohne Desktop (App, Browser) speichert die Weboberfläche eigene Funde im Speicher-Schlüssel damagedLocal (Liste im Format von items) und ausgeblendete Einträge in damagedDismissed (Pfad → date des jüngsten ausgeblendeten Funds); beide gelten nur für das Gerät und kommen nicht in Sicherungen. Die Liste in den Einstellungen zeigt damaged.js und damagedLocal zusammen, ohne Ausgeblendete und ohne Pfade, die nicht in data.js stehen; je Eintrag Datei, Stelle, Länge, Datum, Herkunft und die Knöpfe „An der Stelle abspielen“ (5 s vor der Stelle) und „Aus Liste entfernen“. Protokoll: sflix_sys/player.log, Text UTF-8 ohne BOM, eine Zeile je Fund des Players im Desktop: „JJJJ-MM-TT hh:mm:ss  Beschädigt: <Pfad> bei <m:ss>, 5 s übersprungen“, vorerst nichts anderes. Über 256 KB fällt die ältere Hälfte weg. Die App schreibt kein player.log. Beide Dateien sind persönliche Dateien der Installation: nie herunterladen, ersetzen oder pushen (Net.ProtectedFiles, ReleaseFolder.GeneratedFiles, .gitignore, check_docs.py).
Begründung: Der Benutzer will an einer Stelle sehen, welche Dateien betroffen sind, und ein Protokoll mit möglichst wenig Rauschen. Eine Datei im Muster von renamed.js lässt sich über file:// und in der App lesen, ohne neuen Mechanismus. Die App schreibt nicht auf das NAS, deshalb bleiben ihre Funde auf dem Gerät. Freigabe durch den Benutzer am 23.11.2026 (Variante b).
Alternativen: Nur Liste im Speicher des Geräts (Variante a): verworfen, Funde von Downloader und Medien prüfen fehlten. JSON-Datei: über file:// nicht lesbar. Funde ins updater.log: verworfen, der Benutzer will ein eigenes player.log mit nur diesem Fehler.
Konsequenzen: Neue Dateien damaged.js und player.log in sflix_sys, neue Brückenbefehle damaged und damagedRemove, neue Speicher-Schlüssel damagedLocal und damagedDismissed. Umbenannte Dateien verlieren ihren Eintrag (Pfad fehlt), das ist hingenommen. Doku: docs/INTERFACES.md (Abschnitte Brücke, Dateiformate, gespeicherte Schlüssel) nach der Umsetzung. Umgesetzt in Desktop 4.2.0 (Damaged.cs) und STEFFLIX 2.9.0; Format, Brücke, Speicher-Schlüssel und player.log in docs/INTERFACES.md Abschnitte 1, 4, 7 und 8 (SFLIX-20261123-098).
Betroffene Komponenten: STEFFLIX (start.html, player.js, ui.js, Einstellungen), Desktop (PlayerForm.cs OnWebMessage, neue Klasse für die Liste, DownloadTask.cs, MediaCheckTask.cs, Core.cs, Net.cs), Release (.gitignore, check_docs.py)
Abhängigkeiten: –

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken

---

# Desktop 4.2.0 (Arbeit vom 23.11.2026)

## Entscheidungen

### DEC-375: ffmpeg: abgelehnte Prüfsummen verhindern auch die eingebaute Version
Status: Accepted
Datum: 2026-11-23
Kategorie: Sicherheit
Ticket: SFLIX-20261123-090
Entscheidung: Sind die Prüfsummen erreichbar, aber abgelehnt (Signatur ungültig, Datei beschädigt, älterer oder fehlender Stand nach DEC-372), installiert der Desktop kein ffmpeg, auch nicht die im Programm eingebaute Version; die Aufgabe meldet den Grund. Sind Prüfsummen oder Signatur nicht erreichbar (offline, Server-Fehler), wird wie bisher die eingebaute Version geladen: Adresse und SHA-256 stecken in der EXE und sind damit vertrauenswürdig.
Begründung: Fund 1 der Nachprüfung SFLIX-20261123-081/086: ffmpeg wurde trotz abgelehnter Release-Nummer installiert. Eine abgelehnte Antwort deutet auf einen manipulierten oder veralteten Server hin; ein Ausfall des Servers soll die erste Einrichtung dagegen nicht verhindern.
Alternativen: –
Konsequenzen: Umgesetzt in Desktop 4.2.0 (Ffmpeg.ManifestOrBuiltIn, ChecksumsRejectedException). Ohne erreichbare, gültige Prüfsummen bleibt eine neue Installation bis zum nächsten gültigen Stand ohne ffmpeg. Nachgeführt in docs/ARCHITECTURE.md 3.3 und 3.7 (SFLIX-20261123-098).
Betroffene Komponenten: Desktop (Ffmpeg.cs, Net.cs)
Abhängigkeiten: DEC-372

## A. Offene Entscheidungen

## B. Änderungsbedarf an bestehenden Entscheidungen

## C. Dokumentationsbedarf

## D. Technische Risiken
