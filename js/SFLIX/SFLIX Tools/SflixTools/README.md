# STEFFLIX Updater (`updateStefflix.exe`)

Ein Programm mit Oberfläche, das die bisherigen Werkzeuge ersetzt:

| Aufgabe | ersetzt |
|---|---|
| Update | `updateStefflix.py`, `updater.py`, `sflix_update.py` |
| Gesplittete Medien zusammenführen | `teile_zusammenfuegen.py` |
| Videos für STEFFLIX konvertieren | `convertVideos.py` |
| Vorschaubilder erstellen | `PreviewMaker` |
| Daten einlesen | `getfilepath.py` (`updateData.exe`) |
| Folgen umbenennen | `folgenliste.py`, `serien_umbenennen.py` |

Dazu kommen „Duplikate finden“, „Medien prüfen“ und „Ablagen leeren“. Letzteres löscht `__teile`, `__konvertiert`, `__gelöscht`, `__original`, `__ersetzt` und `__duplikate` (alle Ordner, deren Name mit `__` beginnt) endgültig, nachdem eine Liste zur Bestätigung angezeigt wurde. **Nur innerhalb des Installationsordners:** Ordner ausserhalb werden bei der Wahl abgelehnt und beim Start nochmals geprüft (`Folders.IsInsideRoot`), Verknüpfungen (Junctions, symbolische Links) werden nie verfolgt und nie gelöscht. Das gilt auch ohne Oberfläche.

**Folgen umbenennen** (`EpisodeTask.cs`, Dialoge in `EpisodeDialogs.cs`): Serienordner wählen, im Dialog die Serie suchen (fernsehserien.de mit deutschen Titeln, ohne Treffer oder mit „Nicht dabei“ TVmaze mit englischen Titeln), Folgen laden. Folgennummern werden aus `S01E02`, `S01 E02`, `1x02` erkannt; Doppelfolgen (`S01E01E02`, `S01E0102`, `S02E03-E04`, `S02E03-04`, `1x02-03`; eine Nummer ohne „E“ nur, wenn sie höchstens 3 über der letzten liegt) behalten alle Nummern: `Serie - S01E02E03 - Titel.ext`, Titel über `CombineTitles` („Pilot (1)“ + „Pilot (2)“ → „Pilot (1+2)“, sonst „A + B“). Sie gelten bei den fehlenden Folgen als alle Teile; steckt eine Folge zusätzlich in einer anderen Datei, bilden beide eine Wahlgruppe. Zielname sonst `Serie - S01E02 - Titel.ext` (ß → ss, Zeichen wie beim Einlesen bereinigt) im Ordner `Staffel 01`. Staffelordner werden angeglichen (`Staffel 1`, `Season 1`, `S01` → `Staffel 01`), gibt es beide, wird zusammengeführt. Folgen in der falschen Staffel oder direkt im Serienordner werden verschoben. Die Vorschau zeigt alle Änderungen, mehrere Dateien für dieselbe Folge sind nicht angehakt (nur eine wählbar), dazu fehlende Folgen und nicht zugeordnete Dateien. Vorschaubilder ziehen mit, ersetzte Dateien kommen nach `<Serie>\__ersetzt`, `renamed.js` wird ergänzt (`DataTask.AppendRenamedPairs`). Jeder Lauf steht in `sflix_sys\folgen_rueckgaengig.json`; „Letztes Umbenennen rückgängig“ (Aufgabe `renameundo`) spielt ihn rückwärts ab. Geladene Folgenlisten speichert `EpisodeCache` in `sflix_sys\folgen_cache.json` (nicht pushen), gefunden über den Seriennamen (Ordnername oder gefundener Titel, ohne Gross-/Kleinschreibung, Jahr und Satzzeichen). Gibt es eine, fragt `EpisodeCacheDialog`: „Gespeicherte verwenden“ (ohne Suche), „Neu laden“ (dieselbe Serie über `SeriesId`), „Andere Serie suchen“ oder „Abbrechen“. Gespeichert wird nur eine Liste mit Folgen; schlägt das Neuladen fehl, bleibt die alte Liste und der Dialog bietet sie erneut an.

„Daten einlesen“ verkleinert zu große Vorschaubilder auf 480×270. Das Original kommt nach `Preview\__original\<Dateiname>`. Bis Version 3.0.3 lag es als `<Dateiname>.original` direkt im Preview-Ordner; solche Dateien zieht „Daten einlesen“ automatisch um.

Das Fenster hat fünf Registerkarten. Sichtbar ist nur der Bereich der gewählten Karte, der Untertitel im Kopfbild wechselt mit (UPDATES, IMPORTER, TOOLS, LIZENZ bzw. FEEDBACK, HILFE). „Start“ führt aus, was auf der sichtbaren Karte gewählt ist:

| Karte | Inhalt | beim Start |
|---|---|---|
| Updates | Update, Daten einlesen | die angehakten |
| Importer | Medien importieren | Import, auf Wunsch Vorschaubilder (nur im Zielordner), danach Daten einlesen für den Zielordner |
| Tools | Vorschaubilder, Folgen umbenennen, Duplikate finden, Zusammenführen, Konvertieren, Medien prüfen, Ablagen leeren (höchstens vier Kacheln je Reihe), Einstellungen | die angehakten, danach Daten einlesen nur für die betroffenen Ordner im Installationsordner (mit „Alles“ alles, nach „Ablagen leeren“ allein nichts) |
| Lizenz / Feedback | `FeedbackPage.cs` | kein Start-Knopf |
| Hilfe | `HelpPage.cs`, Texte in `HelpContent.cs` (auch F1) | kein Start-Knopf |

**Lizenz und Feedback** (`License.cs`, `FeedbackPage.cs`): Der Schlüssel `SFX2-…` enthält einen Discord-Webhook (ID und Token, verschleiert, mit Prüfsumme; Format siehe `License.cs` und `SFLIX Tools\Lizenz\create_license.py`). Er steht in `sflix_sys\license.js` (`licenseFile = { key, confirmedFor }`, nie pushen) und gilt auch für STEFFLIX, das ihn nur liest. Beim Speichern schickt `LicenseStore.Save` „Lizenz eingelöst“ mit STEFFLIX-ID; erst danach (`confirmedFor` = `installId`) heisst der Reiter „Feedback“ und STEFFLIX zeigt das Feedback. Ohne Verbindung bleibt die Bestätigung ausstehend und wird beim Start nachgeholt (`ConfirmPending`, auch für eine Kopie des Ordners mit neuer ID). „Lizenzschlüssel entfernen“ schickt „Lizenz entfernt“ (ohne Verbindung mit Rückfrage). Feedback geht mit Diagnose (`MainForm.BuildDiagnosis`) und Protokoll der Sitzung als Anhang an den Webhook (`Discord.Send`, multipart, keine Erwähnungen). Lizenzen erzeugt `create_license.py`.

**Vergleich** (`CompareView.cs`): Tabelle mit Grösse, Auflösung, Dauer, Video/Ton, Datum und Hinweis (nicht abspielbar, defekt), bessere Werte grün. ffprobe läuft im Hintergrund; fehlt ffmpeg, bietet die Tabelle „ffmpeg installieren“ an. Genutzt von `ConflictDialog` (Importer, Folgen umbenennen; `TaskContext.AskConflict(quelle, ziel)`), der Vorschau von Folgen umbenennen (Konfliktzeile anklicken) und `DuplicateDialog`.

**Medien prüfen** (`MediaCheckTask.cs`): nicht abspielbar (`MediaCheck.IsPlayable`: MP4 nur H.264 8 Bit mit AAC/MP3, WebM VP8/VP9/AV1 mit Opus/Vorbis), defekt (ffprobe-Fehler), leer, ohne Vorschaubild (gemäss den Schaltern bei Vorschaubilder), leere Ordner (seit 3.4: `FindEmptyFolders`/`ScanFolder`, Ordner ohne Medien auch darunter, nur der oberste, nie der gewählte Ordner oder eine Kategorie; `SuggestDelete` nur ohne andere Dateien als Vorschaubilder; Löschen verschiebt den ganzen Ordner nach `__gelöscht` im Ordner darüber, `MoveFolderToDeleted` prüft `IsInsideRoot`). `MediaCheckDialog`: eine Zeile je Datei (`MediaCheckFile`), Aktionsspalten Löschen/Konvertieren/Vorschaubild (selbst gezeichnete Kästchen, Symbole im Spaltenkopf, Löschen schliesst die anderen aus, Vorschlag `Suggest`), Filter nach Art, Suche. `MediaCheckChoice` → `MediaCheckTask.Execute`: zuerst nach `__gelöscht` verschieben (Vorschaubild nach `Preview\__gelöscht`, ausser ein anderes Medium hat denselben Namen), dann `ConvertTask` mit `OnlyFiles` und `Results`, dann `PreviewTask.MakeFor` für die Ergebnisse.

**Ablagen leeren**: Der Reiter zeigt den belegten Speicher (`CleanupTask.Scan` im Hintergrund, `RefreshCleanupInfo` beim Anzeigen, bei geänderter Ordnerwahl und nach jedem Lauf; ältere Berechnungen werden abgebrochen). M4A wird untersucht (ALAC ist nicht abspielbar). Mit Oberfläche protokolliert es nur die ersten 100 Einträge, „Duplikate finden“ die ersten 50 Gruppen.

**Tabellen** (`Lists.cs`): `DarkList.Apply` zeichnet die Spaltenköpfe dunkel und schaltet `ListSort` ein (Klick auf den Kopf sortiert, nochmals = absteigend, Dreieck in der Spalte). Sortiert wird nach dem `Tag` einer Zelle (z.B. Bytes, Sekunden, mit `ListSort.Add`), sonst natürlich nach Text. `CompareView` sortiert selbst (`ListSort.Custom`): innerhalb der Gruppen, die Gruppen nach ihrem ersten Eintrag. Grosse Listen immer mit `BeginUpdate` und `AddRange` füllen und Häkchen-Meldungen bei Sammelaktionen unterdrücken.

**Duplikate finden** (`DuplicateTask.cs`): identische Dateien (Grösse, dann SHA256), gleiche Folgen je Serie (Kategorie\Serie, `EpisodeTask.ParseEpisodes`, Union-Find über gemeinsame Folgen, Überschneidungen ohne Vorauswahl) und gleicher Dateiname ohne Endung (`FindSameName`; keine Gruppe, die schon identisch ist oder dieselben Dateien hat). Eine Datei in zwei Gruppen wird nur einmal verschoben. Aussortiert wird nach `<Ordner>\__duplikate` (Vorschaubild nach `__duplikate\Preview`), `renamed.js` führt Favoriten und Fortschritt zur behaltenen Datei. Optional werden nicht abspielbare behaltene Videos konvertiert.

**Teil-Einlesen:** Bei „Daten einlesen“ schaltet „Alle Ordner einlesen“ zwischen dem ganzen STEFFLIX-Ordner und einer Liste gewählter Ordner um. Die Liste wird gespeichert (`data.all`, `data.folders` relativ zum STEFFLIX-Ordner, getrennt mit `|`). Für jeden gewählten Ordner ersetzt `DataTask.UpdateBranch` dessen Zweig in der bestehenden `data.js`. Fehlende Zwischenordner werden angelegt, ein gelöschter Ordner wird entfernt, und der `Preview`-Ordner eine Ebene darüber wird mit eingelesen. Neue Einträge kommen in die Reihenfolge des Ordners, das Ergebnis entspricht also dem vollständigen Einlesen. Fehlt `data.js` oder ist sie nicht lesbar, wird alles eingelesen.

Updates und Tools werden als Kacheln gewählt (Klick wählt aus oder ab). Unter den Kacheln gibt es für jede gewählte Kachel einen Reiter mit Beschreibung und Optionen, bei Tools zusätzlich immer den Reiter „Einstellungen“. Optionen sind Schalter. Der Start-Knopf beschreibt, was passiert („2 Tools starten“, „Importieren“ …). Dateien und Ordner, die ins Fenster gezogen werden, landen immer im Importer.

Die Aufgaben laufen immer in dieser Reihenfolge: Update, ffmpeg installieren, Ablagen leeren, Import, Folgen umbenennen (bzw. rückgängig), Duplikate finden, Zusammenführen, Konvertieren, Medien prüfen, Vorschaubilder, Daten einlesen. „Ablagen leeren“ läuft vor Zusammenführen und Konvertieren, damit frisch gesicherte Originale nicht im selben Lauf gelöscht werden. „Folgen umbenennen“ läuft vor den anderen Tools, die dann schon mit den neuen Namen arbeiten. „Daten einlesen“ kommt zuletzt, weil die anderen Aufgaben Dateien verändern.

**Ordner der Tools:** Vorschaubilder, Zusammenführen, Konvertieren und Ablagen leeren haben je ein eigenes Ordnerfeld mit dem Schalter „Alles“ (ganzer Installationsordner, Standard) oder einer Liste mehrerer Ordner (`FolderPick` in `MainForm`). Die Wahl wird nicht gespeichert. Ordner ausserhalb von STEFFLIX sind erlaubt (ausser bei Ablagen leeren), für sie wird `data.js` nicht angepasst. Mehrere Ordner laufen nacheinander (`TaskRunner.ForEachFolder`, doppelte und verschachtelte fallen weg). Der **Installationsordner** steht unter Tools → Einstellungen nur zur Ansicht; er wird bei der Installation festgelegt. Der frühere `media.folder` wird nicht mehr verwendet.

**Protokoll** (`LogView.cs`): „Details“ blendet es rechts neben dem Inhalt ein, das Fenster wird dabei breiter. Die Breite lässt sich am Splitter ziehen und wird gespeichert (`log.width`). Zeilen sind nach Art gefärbt (Fehler, Hinweis, Erfolg, Überschrift) und haben eine Uhrzeit. Oben: Kopieren, Leeren, Suche, „Nur Fehler und Warnungen“ und „In Datei speichern“. Die Datei `sflix_sys\updater.log` (`LogFile.cs`) wird nur mit diesem Schalter geschrieben; er wird nicht gespeichert und ist beim Start aus (`--log` schaltet ihn ein, nach einem Selbst-Update wird er so weitergegeben). Beim Einschalten kommt alles seit dem Start in die Datei. Ausgeschaltet heisst der Knopf „Speichern unter …“ und speichert das Protokoll der Sitzung. Die Datei wird über 1 MB auf die letzten 256 KB gekürzt; die Datei aus Versionen bis 3.2 wird einmal gelöscht (`log.legacyRemoved`). Bei einer Fehlerzeile klappt das Protokoll von selbst auf. Die Zusammenfassung am Ende nennt je Aufgabe das Ergebnis (`TaskContext.Summary`) und die Dauer; „Daten einlesen“ zeigt zusätzlich eine Übersicht je Kategorie (Videos, Musik, Bilder, Ordner, Videos ohne Vorschaubild). Am Ende jedes Laufs steht deutlich „VORGANG BEENDET“ mit Ergebnis und Dauer.

Der Updater arbeitet nur in einem STEFFLIX-Ordner, also dort, wo `start.html` und `sflix_sys` sind. **Liegt er woanders, bietet er stattdessen die Installation an** (wie `setup.exe`):
1. Ordner wählen.
2. Die Dateien der Weboberfläche und `data.js` werden geladen und geprüft.
3. `renamed.js` und `install.js` (eigene Kennung) werden angelegt.
4. Der Updater kopiert sich selbst in den neuen Ordner.
5. Auf Wunsch (Standard: ein) wird ffmpeg installiert, standardmässig nach `%LOCALAPPDATA%\STEFFLIX\ffmpeg`; ein anderer Ordner kommt als `ffmpeg.folder` in `updater_settings.json`. Ein optionaler Lizenzschlüssel wird gespeichert und bestätigt (`InstallOptions`).

**Kennung der Installation** (`sflix_sys\install.js`, Klasse `InstallId` in `Core.cs`): Der Browser speichert die Daten aller `file://`-Seiten gemeinsam. Mit der zufälligen Kennung nutzt jede Installation eine eigene IndexedDB (`STEFFLIX-<Kennung>`). Der Updater prüft die Datei bei jedem Start. Fehlt sie, legt er sie an. Ist der Ordner kopiert, enthält der alte Ordner also noch dieselbe Kennung, bekommt die Kopie eine neue. Ist der Ordner verschoben, bleibt die Kennung und nur der gespeicherte Ordner ändert sich.

Danach lässt sich STEFFLIX direkt öffnen oder der Updater im neuen Ordner starten. Ohne Oberfläche geht das mit `updateStefflix.exe --install <Ordner>`.

## Bauen

```bash
powershell -ExecutionPolicy Bypass -File build.ps1
```

Das Ergebnis ist `dist\updateStefflix.exe` (rund 0,2 MB). Benötigt wird nur .NET Framework 4.x, das auf jedem Windows 10/11 vorhanden ist. Ein Visual Studio ist nicht nötig. Icon und Kopfbild werden beim Bauen aus dem Logo erzeugt. Eine Vorschau des Kopfbilds liegt danach in `obj\banner.png`.

## Neue Version veröffentlichen

Ordnerstruktur:

```
SFLIX\                        Release-Ordner (wird gepusht)
├── README.md, start.html, updateStefflix.exe, updater.exe, updateData.exe
├── checksums.json, ffmpeg.json   (müssen hier liegen: die Updater laden sie von hier)
├── sflix_sys\
└── SFLIX Tools\
    ├── Release\              build_release.bat, release.ps1, check_release.py,
    │                         create_checksums.py, create_news.py, sflix_update.py
    └── SflixTools\           dieses Projekt (Quellcode des Updaters)
```

**`SFLIX Tools\Release\build_release.bat` doppelklicken** (startet `release.ps1`). Die Skripte arbeiten immer auf dem Release-Ordner zwei Ebenen darüber, egal von wo sie gestartet werden. Das Skript fragt alles Nötige ab, Enter übernimmt jeweils den Vorschlag:

1. **Updater:** Hat sich der Quellcode hier seit dem letzten Release geändert, schlägt es vor, ihn zu bauen. Die Versionsnummer wird automatisch um eins erhöht (z.B. 3.0.4 → 3.0.5) und kann vor dem Bauen geändert werden. Danach wird `updateStefflix.exe` gebaut und nach `SFLIX` kopiert. Schlägt der Build fehl, wird die Versionsnummer zurückgesetzt.
2. **ffmpeg:** Gibt es eine neuere stabile ffmpeg-Version, kann `ffmpeg.json` darauf gesetzt werden.
3. **STEFFLIX-Version:** Wurden Dateien der Weboberfläche geändert, kann `version.js` erhöht werden (z.B. 2.02 → 2.03). Die Changelog-Einträge werden dabei abgefragt und in `news.txt` eingefügt. Gibt es dort schon einen Block für die eingegebene Version, kommen sie an dessen Ende statt in einen neuen Block.
4. **Prüfen:** `check_release.py`, `create_news.py`, `create_checksums.py`. Danach schreibt das Skript `versions.json` (neueste Versionen von Updater und ffmpeg) in den Release-Ordner. STEFFLIX liest sie nur von GitHub (Hilfe, Hinweis auf einen neuen Updater), der Updater lädt sie nicht.
5. **Übersicht:** welche Dateien sich geändert haben und zusammen gepusht werden müssen.

Danach nur noch pushen. Optionen: `build_release.bat -Auto` übernimmt alle Vorschläge ohne Rückfrage, `-MarkReleased` merkt den aktuellen Quellcode als bereitgestellt, ohne zu bauen.

Welcher Quellcode-Stand zuletzt bereitgestellt wurde, steht in `release_state.json` (hier im Projekt). Das Selbst-Update installiert nur Versionen, die **höher** sind als die laufende. Das Skript warnt, wenn eine eingegebene Version nicht höher ist.

`tools\publish.ps1` wird für diesen Ablauf nicht mehr gebraucht. Es kann aber weiterhin eine einzelne EXE samt `ffmpeg.json` in einen anderen Ordner übertragen.

`setup.exe` gibt es nicht mehr: `updateStefflix.exe` installiert selbst, wenn sie außerhalb eines STEFFLIX-Ordners gestartet wird.

**Umstieg bestehender Installationen:** `updateData.exe` und `updater.exe` samt ihren Einträgen in `checksums.json` noch eine Weile im Repository lassen. Die alte Python-Version lädt diese Dateien beim Update, holt dann über `updater.exe` die neue `updateStefflix.exe` und ist damit umgestellt. Die neue Version löscht die beiden alten Programme danach selbst.

## Selbst-Update

- Beim Start prüft das Programm im Hintergrund, ob es Neues gibt: STEFFLIX-Dateien (Vergleich mit `checksums.json`, Versionsnummer aus `version.js` auf GitHub), dieses Programm und ein bereits installiertes ffmpeg. Falls ja, erscheint ein Fenster mit den Versionen und „Jetzt aktualisieren“ / „Später“. Nach „Später“ bleibt ein Hinweisbalken mit demselben Knopf stehen.
- Rechts auf der Seite „Updates“ steht eine Übersicht „Versionen“ (installiert und verfügbar, Neueres in Rot). „Neu prüfen“ wiederholt die Prüfung, nach einem Update geschieht das automatisch.
- „Patchnotes“ (im Hinweisfenster und in der Übersicht) lädt `sflix_sys/news.json` von GitHub und zeigt alle Versionen, die neuer sind als die installierte STEFFLIX-Version. Gibt es keine, wird die neueste Version gezeigt.
- Die Aufgabe „Update“ aktualisiert zuerst die STEFFLIX-Dateien, dann ffmpeg (optional) und zuletzt sich selbst.
- Bei jedem Start und nach einer ffmpeg-Installation schreibt `UpdaterInfo` (Core.cs) die eigene Version und die installierte ffmpeg-Version nach `sflix_sys\updater_info.js` (nur bei Änderungen, nicht pushen). STEFFLIX zeigt sie in der Hilfe und vergleicht mit `versions.json` auf GitHub.
- Wurde das Programm ersetzt, startet die neue Version automatisch und erledigt die restlichen ausgewählten Aufgaben.
- Ablauf beim Ersetzen:
  1. Die neue EXE wird geladen und geprüft (Windows-Programm, SHA256 aus `checksums.json`, höhere Versionsnummer).
  2. Die laufende EXE wird in `.old` umbenannt, die neue wird an ihre Stelle gesetzt.
  3. Die `.old` wird beim nächsten Start gelöscht.

## ffmpeg

- Liegt seit 3.3 je Benutzer in `%LOCALAPPDATA%\STEFFLIX\ffmpeg` (`FfmpegLocation`) oder im gewählten Ordner (`ffmpeg.folder`, Tools › Einstellungen „Ändern …“). Gesucht wird: gewählter Ordner, Standardordner, alter Ort `sflix_sys\ffmpeg`. Gibt es den gewählten Ordner auf einem PC nicht, gilt der Standardordner. Ein ffmpeg am alten Ort wird beim Start in den Standardordner verschoben (ohne eigenen Ordner; klappt es nicht, bleibt es dort in Gebrauch).
- Wird im Setup, mit „Installieren“ (Aufgabe `ffmpeg`) oder beim ersten Bedarf geladen. Quelle ist der offizielle Build von gyan.dev über GitHub-Releases, mit Prüfsumme. Ein selbst hineinkopiertes ffmpeg ohne `version.json` wird nicht aktualisiert.
- Welche Version geladen wird, bestimmt `ffmpeg.json` im Repository. Fehlt sie, gilt die eingebaute Version 9.0.2.
- Mit „ffmpeg ebenfalls aktualisieren“ holt die Aufgabe „Update“ die Version aus `ffmpeg.json`.

## Ordner mit `__`

Ordner, deren Name `__` enthält, sind Ablagen der Werkzeuge. Keine Aufgabe durchsucht sie, und sie erscheinen nicht in der Mediathek:
- `__teile`: Teile nach dem Zusammenführen
- `__konvertiert`: Originale nach dem Konvertieren
- `Preview\__gelöscht`: Vorschaubilder ohne zugehöriges Video
- `__ersetzt`: beim Umbenennen ersetzte Dateien
- `__duplikate`: aussortierte Duplikate

## Dateioperationen (NAS)

Verschieben, Umbenennen und Löschen laufen über `FileOps` in `Util.cs`. Bei „Zugriff verweigert“ entfernt es den Schreibschutz (oft per SMB gesetzt) und setzt ihn am Ziel wieder. Danach ergänzt `TryGrantDelete` das Recht „Löschen“ für alle Regeln mit Schreibrecht (per SID, nachgeprüft; NAS mit Linux-Rechten wie QNAP speichern es nicht, dann bleibt es beim Fehler). Meldungen gehen über `FileOps.Notice` ins Protokoll. Bei gesperrten Dateien wird bis zu dreimal mit Pause wiederholt. Die Fehlermeldung nennt die wahrscheinliche Ursache. Neue Codestellen nicht direkt `File.Delete`/`File.Move` verwenden.

## Konvertieren

- Das Ergebnis (MP4, H.264, AAC) ersetzt das Original im selben Ordner. Das Original kommt nach `__konvertiert`, oder wird gelöscht, falls so eingestellt.
- Hat ein Video mehrere Tonspuren, entsteht pro Spur eine Datei `Name_deu.mp4`, `Name_eng.mp4` usw.
- Passende Spuren werden nur kopiert, zum Beispiel H.264 aus MKV. Das geht schnell und ist verlustfrei.
- Encoder: NVIDIA, Intel oder AMD werden mit einem Testbild geprüft, sonst wird die CPU verwendet. Scheitert die Grafikkarte an einem Video, wird es mit der CPU wiederholt.
- Bereits kompatible Videos merkt sich das Programm in `sflix_sys\konvertieren_kompatibel.json`. Sie werden beim nächsten Lauf nicht erneut untersucht.
- Musik (`ConvertOptions.Audio`, Schalter „Auch Musik in MP3 umwandeln“, Einstellung `convert.audio`, Standard ein): Endungen aus `MediaTypes.ConvertAudio` (WMA, AIFF, APE …) und M4A mit anderem Codec als AAC/MP3 (z.B. ALAC, `ConvertTask.IsAudioReady`) werden mit `libmp3lame -q:a 2` zu MP3, Tags bleiben. Geprüft wird Codec und Länge. „Medien prüfen“ wandelt gewählte Musik immer um, der Importer ALAC ebenfalls.
- Qualität und Preset lassen sich in `sflix_sys\updater_settings.json` einstellen: `convert.qualitySD`, `convert.qualityHD`, `convert.qualityFullHD`, `convert.quality4K` und `convert.preset`.

## Ohne Oberfläche (z.B. Aufgabenplanung)

```bash
updateStefflix.exe --headless update,data
```

- Aufgaben: `update`, `ffmpeg`, `cleanup`, `dupes`, `merge`, `convert`, `check`, `preview`, `data`, `renameundo`. `rename` braucht die Oberfläche (Auswahl der Serie); `dupes` und `check` protokollieren ohne Oberfläche nur.
- Lizenz: `--license <Schlüssel>` speichert und bestätigt, `--license-remove` meldet ab und entfernt. Setup: `--install <Ordner> [--ffmpeg [Ordner]] [--license <Schlüssel>]`.
- `data` liest ohne Oberfläche alles ein. Mit `--data-folder "Serien\Meine Serie"` (mehrfach möglich, relativ oder absolut) nur diese Ordner.
- `cleanup` löscht ohne Oberfläche **alle** Ablagen im Installationsordner, ohne nachzufragen.
- Die Tools arbeiten ohne Oberfläche im ganzen Installationsordner, Optionen stammen aus `sflix_sys\updater_settings.json`.
- Beim Zusammenführen wird ohne Oberfläche nur verarbeitet, was ohne Neukodieren geht.
- Das Protokoll steht immer in `sflix_sys\updater.log` (begrenzt auf 1 MB). Exit-Code 0 bedeutet, dass alles in Ordnung war.

Mit `--run update,data` startet die Oberfläche und beginnt sofort mit diesen Aufgaben.

## Testen

- `SFLIX_DIR=<Ordner>` verwendet einen anderen STEFFLIX-Ordner.
- `SFLIX_UPDATE_URL=http://127.0.0.1:8000/` verwendet einen lokalen Update-Server, zum Beispiel `python -m http.server 8000` im Repository-Ordner.
