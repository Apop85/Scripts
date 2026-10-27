# STEFFLIX Updater (`updateStefflix.exe`)

Ein Programm mit Oberfläche, das die bisherigen Werkzeuge ersetzt:

| Aufgabe | ersetzt |
|---|---|
| Update | `updateStefflix.py`, `updater.py`, `sflix_update.py` |
| Gesplittete Medien zusammenführen | `teile_zusammenfuegen.py` |
| Videos für STEFFLIX konvertieren | `convertVideos.py` |
| Vorschaubilder erstellen | `PreviewMaker` |
| Daten einlesen | `getfilepath.py` (`updateData.exe`) |

Dazu kommt „Ablagen leeren“: Es löscht `__teile`, `__konvertiert`, `__gelöscht` und `__original` (alle Ordner, deren Name mit `__` beginnt) im Medienordner endgültig, nachdem eine Liste zur Bestätigung angezeigt wurde.

„Daten einlesen“ verkleinert zu große Vorschaubilder auf 480×270. Das Original kommt nach `Preview\__original\<Dateiname>`. Bis Version 3.0.3 lag es als `<Dateiname>.original` direkt im Preview-Ordner; solche Dateien zieht „Daten einlesen“ automatisch um.

Das Fenster hat drei Registerkarten. Sichtbar ist nur der Bereich der gewählten Karte, der Untertitel im Kopfbild wechselt mit (UPDATES, IMPORTER, TOOLS). „Start“ führt aus, was auf der sichtbaren Karte gewählt ist:

| Karte | Inhalt | beim Start |
|---|---|---|
| Updates | Update, Daten einlesen | die angehakten |
| Importer | Medien importieren | Import, auf Wunsch Vorschaubilder (nur im Zielordner), danach Daten einlesen für den Zielordner |
| Tools | Vorschaubilder, Zusammenführen, Konvertieren, Ablagen leeren, Einstellungen | die angehakten, danach Daten einlesen (nur der Medienordner, wenn er in STEFFLIX liegt, sonst alles) |

**Teil-Einlesen:** Bei „Daten einlesen“ schaltet „Alle Ordner einlesen“ zwischen dem ganzen STEFFLIX-Ordner und einer Liste gewählter Ordner um. Die Liste wird gespeichert (`data.all`, `data.folders` relativ zum STEFFLIX-Ordner, getrennt mit `|`). Für jeden gewählten Ordner ersetzt `DataTask.UpdateBranch` dessen Zweig in der bestehenden `data.js`. Fehlende Zwischenordner werden angelegt, ein gelöschter Ordner wird entfernt, und der `Preview`-Ordner eine Ebene darüber wird mit eingelesen. Neue Einträge kommen in die Reihenfolge des Ordners, das Ergebnis entspricht also dem vollständigen Einlesen. Fehlt `data.js` oder ist sie nicht lesbar, wird alles eingelesen.

Updates und Tools werden als Kacheln gewählt (Klick wählt aus oder ab). Unter den Kacheln gibt es für jede gewählte Kachel einen Reiter mit Beschreibung und Optionen, bei Tools zusätzlich immer den Reiter „Einstellungen“. Optionen sind Schalter. Der Start-Knopf beschreibt, was passiert („2 Tools starten“, „Importieren“ …). Dateien und Ordner, die ins Fenster gezogen werden, landen immer im Importer.

Die Aufgaben laufen immer in dieser Reihenfolge: Update, Ablagen leeren, Import, Zusammenführen, Konvertieren, Vorschaubilder, Daten einlesen. „Ablagen leeren“ läuft vor Zusammenführen und Konvertieren, damit frisch gesicherte Originale nicht im selben Lauf gelöscht werden. „Daten einlesen“ kommt zuletzt, weil die anderen Aufgaben Dateien verändern.

Zusammenführen, Konvertieren, Vorschaubilder und Ablagen leeren arbeiten im **Medienordner**. Er wird unter Tools → Einstellungen einmal festgelegt und gespeichert. Am Ende jedes Laufs zeigt das Protokoll deutlich „VORGANG BEENDET“ mit Ergebnis und Dauer.

Der Updater arbeitet nur in einem STEFFLIX-Ordner, also dort, wo `start.html` und `sflix_sys` sind. **Liegt er woanders, bietet er stattdessen die Installation an** (wie `setup.exe`):
1. Ordner wählen.
2. Die Dateien der Weboberfläche und `data.js` werden geladen und geprüft.
3. `renamed.js` und `install.js` (eigene Kennung) werden angelegt.
4. Der Updater kopiert sich selbst in den neuen Ordner.

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
3. **STEFFLIX-Version:** Wurden Dateien der Weboberfläche geändert, kann `version.js` erhöht werden (z.B. 2.02 → 2.03). Die Changelog-Einträge werden dabei abgefragt und in `news.txt` eingefügt.
4. **Prüfen:** `check_release.py`, `create_news.py`, `create_checksums.py`.
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
- Wurde das Programm ersetzt, startet die neue Version automatisch und erledigt die restlichen ausgewählten Aufgaben.
- Ablauf beim Ersetzen:
  1. Die neue EXE wird geladen und geprüft (Windows-Programm, SHA256 aus `checksums.json`, höhere Versionsnummer).
  2. Die laufende EXE wird in `.old` umbenannt, die neue wird an ihre Stelle gesetzt.
  3. Die `.old` wird beim nächsten Start gelöscht.

## ffmpeg

- Liegt in `sflix_sys\ffmpeg\` und wird beim ersten Bedarf automatisch geladen. Quelle ist der offizielle Build von gyan.dev über GitHub-Releases, mit Prüfsumme.
- Welche Version geladen wird, bestimmt `ffmpeg.json` im Repository. Fehlt sie, gilt die eingebaute Version 9.0.2.
- Mit „ffmpeg ebenfalls aktualisieren“ holt die Aufgabe „Update“ die Version aus `ffmpeg.json`.

## Ordner mit `__`

Ordner, deren Name `__` enthält, sind Ablagen der Werkzeuge. Keine Aufgabe durchsucht sie, und sie erscheinen nicht in der Mediathek:
- `__teile`: Teile nach dem Zusammenführen
- `__konvertiert`: Originale nach dem Konvertieren
- `Preview\__gelöscht`: Vorschaubilder ohne zugehöriges Video

## Konvertieren

- Das Ergebnis (MP4, H.264, AAC) ersetzt das Original im selben Ordner. Das Original kommt nach `__konvertiert`, oder wird gelöscht, falls so eingestellt.
- Hat ein Video mehrere Tonspuren, entsteht pro Spur eine Datei `Name_deu.mp4`, `Name_eng.mp4` usw.
- Passende Spuren werden nur kopiert, zum Beispiel H.264 aus MKV. Das geht schnell und ist verlustfrei.
- Encoder: NVIDIA, Intel oder AMD werden mit einem Testbild geprüft, sonst wird die CPU verwendet. Scheitert die Grafikkarte an einem Video, wird es mit der CPU wiederholt.
- Bereits kompatible Videos merkt sich das Programm in `sflix_sys\konvertieren_kompatibel.json`. Sie werden beim nächsten Lauf nicht erneut untersucht.
- Qualität und Preset lassen sich in `sflix_sys\updater_settings.json` einstellen: `convert.qualitySD`, `convert.qualityHD`, `convert.qualityFullHD`, `convert.quality4K` und `convert.preset`.

## Ohne Oberfläche (z.B. Aufgabenplanung)

```bash
updateStefflix.exe --headless update,data
```

- Aufgaben: `update`, `cleanup`, `merge`, `convert`, `preview`, `data`.
- `data` liest ohne Oberfläche alles ein. Mit `--data-folder "Serien\Meine Serie"` (mehrfach möglich, relativ oder absolut) nur diese Ordner.
- `cleanup` löscht ohne Oberfläche **alle** Ablagen, ohne nachzufragen.
- Ordner und Optionen stammen aus `sflix_sys\updater_settings.json`.
- Beim Zusammenführen wird ohne Oberfläche nur verarbeitet, was ohne Neukodieren geht.
- Das Protokoll steht in `sflix_sys\updater.log`. Exit-Code 0 bedeutet, dass alles in Ordnung war.

Mit `--run update,data` startet die Oberfläche und beginnt sofort mit diesen Aufgaben.

## Testen

- `SFLIX_DIR=<Ordner>` verwendet einen anderen STEFFLIX-Ordner.
- `SFLIX_UPDATE_URL=http://127.0.0.1:8000/` verwendet einen lokalen Update-Server, zum Beispiel `python -m http.server 8000` im Repository-Ordner.
