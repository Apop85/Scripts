# STEFFLIX – Regelwerk für alle Rollen

Lokale Mediathek (Weboberfläche über `file://`), Desktop-Programm `STEFFLIX.exe` (C#, Player mit WebView2 und Tools) und Android-App. Dieser Ordner ist der **Release-Ordner** (GitHub `Apop85/Scripts/js/SFLIX`). Sprache des Projekts und des Benutzers: Deutsch, Schweizer Schreibweise („ss“ statt „ß“). Gilt für alle fünf Chat-Rollen und ihre Agents. Neu aufgebaut am 20.11.2026 (alte Fassung: `.claude\archiv\`, nie lesen).

## Stand
| Komponente | bereitgestellt | umgesetzt, noch nicht gebaut | nächste Version |
|---|---|---|---|
| STEFFLIX | 2.9.3 (24.11.2026) | 2.9.9 | 2.9.10 |
| Desktop (`STEFFLIX.exe`) | 4.2.8 (24.11.2026) | 4.2.9 | 4.2.10 |
| Android-App | 1.3.3 (23.11.2026) | 1.3.6 | 1.3.7 |

Nach jeder Umsetzung bzw. jedem Release diese Tabelle nachführen. Einzelheiten stehen im Changelog (`sflix_sys\news.txt`, über `news.py`).

## 0. Rollen und Start
Jeder Chat hat genau eine Rolle. Nach Chatstart oder `/clear` ruft der Benutzer den Rollen-Skill auf; ohne Rolle zuerst danach fragen.

| Rolle | Skill | Rollenprompt | Modell (stellt der Benutzer ein) | Auftrag |
|---|---|---|---|---|
| Planung | `/rolle-planung` | `.claude\roles\PLANUNG.md` | Sonnet / Medium | Ansprechpartner des Benutzers: Ideen sammeln bis zum GO, Anforderungen klären, zerlegen, priorisieren, Aufträge verteilen, Freigaben einholen |
| Architekt | `/rolle-architekt` | `.claude\roles\ARCHITEKT.md` | Opus / High | Analysen, Architektur, Schnittstellen, Datenformate, Risiken, Architektur-Dokumente |
| Entwicklung | `/rolle-entwicklung` | `.claude\roles\ENTWICKLUNG.md` | Sonnet / Medium (Opus bei schwierigen Fehlern) | Umsetzung in allen Komponenten, Fehleranalyse, gezielte Tests, Changelog, Versionen, Hilfe |
| Prüfung | `/rolle-pruefung` | `.claude\roles\PRUEFUNG.md` | Sonnet / Medium | Unabhängige Prüfung (Funktion, Regression, Sicherheit, Performance), Test-Builds, Emulator |
| Release | `/rolle-release` | `.claude\roles\RELEASE.md` | Sonnet / Low | Release-Ablauf, Versionen, Doku-Abgleich, Freigabeempfehlung |

**Startmeldung:** Rolle bestätigen, eigenen Posteingang mit `liste -Rolle <R> -Gruppen` abrufen und kurz ausgeben (ID, Prio, Status, Titel; bei mehreren Aufträgen die Gruppen nach gemeinsamen Dateien). **Nur den eigenen Posteingang.** Keine Übernahme, keine Projektanalyse: Der Benutzer nennt die Prioritäten, erst danach beginnt die Arbeit.

**Regelhierarchie:** (1) diese Datei → (2) `coordination\README.md` → (3) Rollenprompt → (4) Task-Datei. Bei Widerspruch gilt die höhere Stufe; nicht lösbare Widersprüche dem Benutzer vorlegen.

## 1. Verbindliche Regeln des Benutzers (alle Rollen und Agents)
- ⚠ **PowerShell-Alias-Falle (Datenverlust):** Nie eine Funktion (`function`/`filter`) mit einem Namen definieren, den PowerShell als Alias oder Cmdlet kennt (Aliase haben Vorrang: `function rd {…}` und danach `rd $p` ruft `Remove-Item` auf). Gefährlich u. a. `rd`, `ri`, `del`, `rm`, `erase`, `rmdir`, `r`, `sc`, `ac`, `gc`, `cp`, `mv`, `ni`, `sl`, `cat`, `echo`, `type`, `copy`, `move`. Stattdessen ohne eigene Funktionen arbeiten, eindeutige Verb-Nomen-Namen (`Read-FileText`) oder Skriptblöcke (`$teil = { … }; & $teil`). Technisch gesperrt (Guard); nie umgehen.
- ⚠ **PowerShell-Array-Abflachung:** `@(@('a','b'))` mit nur einem Paar ergibt zwei Zeichenketten; Ersetzen über die Liste beschädigt Dateien. Ersetzungen mit dem Edit-Werkzeug; sonst `@(,@('a','b'))`. Technisch gesperrt.
- **Kodierung:** Textdateien UTF-8 ohne BOM. In PowerShell 5.1 nie `Set-Content`/`Out-File`/`Add-Content` ohne Kodierung (`-Encoding utf8` schreibt ein BOM); `[IO.File]::WriteAllText(pfad, text, (New-Object Text.UTF8Encoding($false)))`. Lesen mit `Get-Content -Encoding UTF8`. **`.ps1`-Dateien reines ASCII** (PowerShell 5.1 liest sie ohne BOM als ANSI): Umlaute zur Laufzeit erzeugen (`[char]0xFC`); das Edit-Werkzeug wandelt `ü` in einem Text in ein echtes „ü“ um. Python-Ausgaben mit `sys.stdout.reconfigure(encoding="utf-8")`.
- **Zugriff nur im Projekt:** Lesen und Schreiben nur im Projektordner, `%TEMP%` (Scratchpad), `%USERPROFILE%\.claude`, `%LOCALAPPDATA%\STEFFLIX`, `…\STEFFLIX-Android`, `…\STEFFLIX-TestShare`, `…\Android\Sdk`, `%USERPROFILE%\.android`. Der Guard sperrt andere Orte (bei PowerShell Rückfrage). Bei einer Rückfrage nicht auf Zulassen drängen, sondern einen erlaubten Ort wählen. **Ausnahme GitHub-Klon** (`%USERPROFILE%\Documents\GitHub\Scripts\js\SFLIX`): nur die Rolle Release, nur über `python SFLIX_Tools\Release\sync_github.py`, nur nach erfolgreichem Release und ausdrücklicher Freigabe des Benutzers (DEC-362, DEC-363); sonst ist `Documents\GitHub` für alle gesperrt (Guard).
- **Testumgebung immer `.claude\testenv`:** nie auf produktive STEFFLIX-Ordner zugreifen. `STEFFLIX.exe` nur mit `$env:SFLIX_DIR` auf die Testumgebung starten (Guard). SMB-Tests nur über den lokalen Testserver (Freigabe `.claude\testenv`).
- **Netzlaufwerke (NAS, z. B. `M:`, `Y:`, `Z:`):** gesperrt. Nur wenn der Benutzer einen Ordner ausdrücklich nennt, und nur dort; nie Freigaben durchsuchen, auflisten, stichprobenartig lesen oder Rechte abfragen. Für die Fehlersuche nach dem genauen Pfad oder der Protokollzeile fragen.
- **Kein Webserver für STEFFLIX** (nur `file://`; der Vorschau-Server dient nur Tests). Dark Mode, keine Emojis in der Oberfläche.
- **Persönliche und geheime Dateien nie pushen oder weitergeben** (`install.js`, `license.js`, `lizenzen.json`, `Android\signing\`, `downloads.json` …); `.gitignore` sofort nachführen. Quellcode und Build-Werkzeuge sind Open Source und werden nie ignoriert. `docs\` wird gepusht und darf keine sensiblen Daten enthalten (`check_docs.py`).
- **Nur erklären, wenn erklären verlangt ist** („erst erläutern“): dann nichts ändern.
- **Ideen sammeln:** Beschreibt der Benutzer Ideen, nichts umsetzen, sondern Fragen, Widersprüche und Vorschläge klären (Skill `/ideen`). Erst auf sein Signal („GO“, „Release erstellen“) umsetzen, vorher die Liste bestätigen lassen; nicht nach jeder Runde die ganze Liste ausgeben. „Ideen für später“ werden Aufträge mit Priorität LOW an die Planung.
- **Rückfragen immer nummerieren** (1., 2., 3. …, fortlaufend über die Antwort), jede für sich verständlich, mit Empfehlung.
- **Zusammenfassungen nach Komponente:** zuerst STEFFLIX, dann Desktop, dann Android-App; ohne Änderung „keine Änderungen“.
- **Versionen** (`docs\RELEASE.md`): Major nur bei vollständiger Überarbeitung, Minor bei neuen Features, Patch bei kleinen Änderungen. Jedes GO und jede Korrektur nach einem Test erhöht die Version jeder geänderten Komponente (auch nur `start.html` → neue STEFFLIX-Version mit eigenem Block). **Schlägt ein Build fehl: Fehler beheben, Version bleibt.** Die Startversion nennt der Benutzer am Anfang der Unterhaltung. Skill `/version-erhoehen`.
- **Versions-String am Schluss jeder Umsetzung:** geänderte Komponenten in der Reihenfolge STEFFLIX, Desktop, App, z. B. „Stefflix 2.8.9 / Desktop 4.0.7“; unveränderte weglassen.
- **Changelog:** jede Änderung (Weboberfläche, Desktop, App) im obersten Block von `sflix_sys\news.txt`, nur über `python SFLIX_Tools\Release\news.py eintrag` (Skill `/changelog-eintrag`); bestehende Einträge zum selben Thema anpassen statt doppeln. `news.json` und `checksums.json` nie von Hand.
- **Builds:** Test-Builds (`SFLIX_Tools\SflixTools\build.ps1`, `SFLIX_Tools\Android\build_test_apk.ps1`) dürfen Prüfung, Release und Entwicklung; Release-Builds (`release.ps1 -Auto`) nur die Rolle Release (oder der Benutzer). Keine Debug-APK auf dem Gerät des Benutzers ohne Rückfrage (normale App muss deinstalliert werden, Daten gehen verloren); im Emulator erlaubt.
- **Bitdefender:** Frisch kompilierte EXE im Temp-Ordner werden als „Gen:Variant.Genie“ gemeldet → Code-Prüfungen als `/target:library`. Eine geladene Test-DLL, die ffmpeg startet oder Dateien verschiebt, hat die Shell gesperrt → nur nach Rückfrage (`docs\TESTING.md`).
- **Maus des Benutzers** nur nach ausdrücklicher Freigabe; GUI-Tests über Fenster-Nachrichten.
- **Webhook-Adressen** in PowerShell-Befehlen blockiert der Virenscanner (AMSI): Tests als Python-Skript im Scratchpad.
- **Android-App mitdenken:** bei jeder Änderung prüfen, ob sie die App betrifft (Touch, Player, Laden, neue Dateien, Namen in `inject.js`).
- **Hilfe nachführen:** Änderungen am Desktop in `src\HelpContent.cs`, in der Hilfe von `start.html` und in `README.md`.
- **Nie committen oder pushen** (es gibt kein Git in der Shell; der Benutzer pusht mit GitHub Desktop).
- **Benutzerfreigabe** (nur der Benutzer) vor: grösseren Architekturänderungen, wesentlichen Verhaltensänderungen, sicherheits- oder datenverlustkritischen Abläufen, destruktiven Aktionen ausserhalb der Testumgebung, Release-Builds ohne Auftrag und Änderungen am Regelwerk (diese Datei, `coordination\README.md`, `.claude\roles|skills|agents\`, `.claude\settings.json`, `.claude\settings.local.json`, Guard).
- **Nicht lesen:** `.claude\old\` und `.claude\archiv\` (alte Fassungen, gesperrt).

## 2. Schreibrechte je Rolle
Lesen ist allen erlaubt (Grenzen aus 1). Aufträge in `coordination\` nur über `SFLIX_Tools\Auftrag.ps1`, Entscheidungen nur über `decisions.py`, temporäre Dateien im Scratchpad.

| Rolle | Darf bearbeiten | Darf nicht bearbeiten |
|---|---|---|
| Planung | Aufträge; Regelwerk nur auf ausdrücklichen Auftrag des Benutzers | Code, Doku |
| Architekt | `docs\ARCHITECTURE.md`, `docs\INTERFACES.md`, Entscheidungen; Code nur auf ausdrücklichen Umsetzungsauftrag | übrige Doku, Release-Dateien |
| Entwicklung | Code aller Komponenten (`start.html`, `sflix_sys\`, `SFLIX_Tools\SflixTools\`, `SFLIX_Tools\Android\`, Werkzeuge in `SFLIX_Tools\`), `news.txt` (news.py), Versionsdateien, `README.md`, `HelpContent.cs`, `docs\WEB.md`, `docs\DESKTOP.md`, `docs\ANDROID.md` (eigene Änderungen nachführen), `.gitignore` | Release-Builds, `news.json`, `checksums.json`, `versions.json` |
| Prüfung | Testumgebung `.claude\testenv`, Build-Ausgaben (`dist\`, `obj\`, `app\build\`), Emulator/AVDs, SMB-Testserver, `docs\TESTING.md` (Erkenntnisse) | Produktions- und Testcode, übrige Doku, Versionen |
| Release | Release-Ablauf (`release.ps1 -Auto` mit seinen Ausgaben: EXE-Kopien, `STEFFLIX.apk`, `news.json`, `checksums.json`, `versions.json`, Versionsdateien), Spiegeln in den GitHub-Klon (`sync_github.py`, nach Freigabe), `docs\RELEASE.md`, `.gitignore`, diese Datei (Abschnitt Stand; sonst nur auf Auftrag) | Code |

Jede Rolle führt die Doku-Dateien ihres Bereichs im selben Schritt nach, wenn sie Dateien hinzufügt, löscht, umbenennt oder ihren Zweck ändert. `python SFLIX_Tools\Release\check_docs.py` prüft es.

## 3. Tickets (`coordination\`, Richtlinie `coordination\README.md`)
**Jede Aufgabe bekommt ein Ticket**, auch kleine (Nachvollziehbarkeit): eigene Arbeit mit `erledigt` in einem Aufruf, Aufträge an andere Rollen mit `erstellen`. Schreiben nur über das Skript (Guard), lesen über `liste`; nur die Task-Datei des bearbeiteten Auftrags direkt lesen. Folgeaufträge vollständig formulieren, Funde gesammelt in **einem** Auftrag. Jede Zeile beginnt mit `powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1`; `-Rolle` = eigene Rolle (`Planung`, `Architekt`, `Entwicklung`, `Pruefung`, `Release`):
```
liste -Rolle Entwicklung -Gruppen                                   # Posteingang mit Gruppen (Rollenstart)
erledigt -Rolle Entwicklung -Titel "…" -Aufgabe "…" -Dateien "start.html" -Arbeiten "…" -Tests "…" -Entscheide keine
erstellen -Rolle Entwicklung -An Pruefung -Prio MEDIUM -Titel "…" -Aufgabe "…" -Ergebnis "…" -Dateien "…" [-Kontext "…"]
status -Rolle Pruefung -Id 12 -Neu IN_PROGRESS                       # Übernahme erst nach der Priorisierung durch den Benutzer
abschliessen -Rolle Pruefung -Id 12 -Arbeiten "…" -Tests "…" -Entscheide "DEC-329" [-Risiken "…"] [-Folgeschritt "…"]
```
Mehrzeiliges über `-Eingabe <Datei im Scratchpad>`. Exit 0: nichts nachlesen; 1 korrigieren; 2 nicht umgehen; 3 Stand prüfen; 4 wiederholen; 5/6 `check -Fix`. Skill `/ticket`.

## 4. Entscheidungen (`docs\DECISIONS.md`)
Getroffene Entscheide (Verhalten, Architektur, Schnittstellen, Datenformate, Standardwerte, verworfene Alternativen, Arbeitsweise) hält die Rolle, die sie mit dem Benutzer trifft, **im selben Schritt** fest und nennt die DEC-IDs beim Ticketabschluss (`-Entscheide`, Pflicht). Nie die ganze Datei lesen (rund 100k Tokens):
```
python SFLIX_Tools\Doku\decisions.py suchen <Wort> | zeigen DEC-123 | baum DEC-123 [--rueckwaerts]
python SFLIX_Tools\Doku\decisions.py neu --titel "…" --entscheidung "…" --begruendung "…" --kategorie UI --ticket SFLIX-… --block "STEFFLIX 2.8.9" [--abhaengigkeiten "DEC-…"] [--intern]
python SFLIX_Tools\Doku\decisions.py ersetzen DEC-alt --durch DEC-neu | offen|aenderung|doku|risiko --text "…" | check
```
Gewöhnliche Bugfixes sind keine Entscheide. Interne Entscheide nur mit `--intern`.

## 5. Dokumentation (gezielt laden, nur nötige Abschnitte)
| Datei | Inhalt | Wer liest |
|---|---|---|
| `docs\WEB.md` | Weboberfläche: `start.html`, `sflix_sys\` | Entwicklung (Web), Architekt, Prüfung |
| `docs\DESKTOP.md` | `STEFFLIX.exe`: Quellcode, MainForm, lokale Dateien | Entwicklung (Desktop), Architekt, Prüfung |
| `docs\ANDROID.md` | Android-App | Entwicklung (App), Architekt, Prüfung |
| `docs\RELEASE.md` | Release-Ordner, Versionen, Release-Skripte, übrige Werkzeuge in `SFLIX_Tools\` | Release, Entwicklung (Versionen), Planung |
| `docs\TESTING.md` | Bauen, Testumgebung, Vorschau-Server, Emulator, SMB-Testserver, Bitdefender | Prüfung, Entwicklung, Release |
| `docs\DECISIONS.md` | Entscheidungen (nur über `decisions.py`) | alle |
| `docs\ARCHITECTURE.md`, `docs\INTERFACES.md` | Architektur (Komponenten, Datenflüsse, Risiken) und Schnittstellen (Brücken, Namen, Dateiformate, gespeicherte Schlüssel) | Architekt, Entwicklung, Prüfung (Schnittstellen) |
| `docs\intern\DECISIONS-intern.md` | interne Entscheide, nicht gepusht | nur über `decisions.py --intern` |
| `coordination\README.md` | Koordinationsrichtlinie (Regelwerk) | bei Bedarf gezielt |
| `README.md`, `SFLIX_Tools\SflixTools\README.md` | Nutzer- bzw. Entwickler-Doku (gepusht) | Entwicklung |

## 6. Agents (`.claude\agents\`)
Werkzeuge einer Rolle, keine Rollen; höchstens die Rechte der aufrufenden Rolle; alle Regeln aus 1 gelten (Alias-Falle in jeden Auftrag mit Dateiänderungen). Nur bei einem Auslöser einsetzen, nicht für Kleinigkeiten; Kontext knapp mitgeben; Ergebnis kritisch prüfen.

| Agent | Modell | Auslöser | Schreibt? |
|---|---|---|---|
| `sflix-explorer` | Haiku | breite Suche nach Code und Aufrufern quer über JS, C#, Java | nein |
| `schnittstellen-pruefer` | Sonnet / High | Änderung an Brücke WebView2 ↔ `ui.js`, `inject.js`, Datenformaten (`news.json`, `checksums.json`, `versions.json`, `data.js`, `install.js`, `license.js`) | nein |
| `csharp-pruefer` | Sonnet | Änderung am Desktop: Probe-Kompilieren als Library, C#-5-Regeln | nein (nur Scratchpad) |
| `web-tester` | Sonnet | Prüfung der Weboberfläche im Vorschau-Server | nein (nur Testumgebung) |
| `android-tester` | Sonnet | Prüfung der App im Emulator mit SMB-Testserver | nein (nur Emulator) |
| `security-auditor` | Opus / High | Lizenz, Webhook, Netzwerk, Download, Löschen/Verschieben von Dateien, Pfadprüfungen | nein |
| `perf-messer` | Sonnet | gemessenes Performance-Problem, Vergleich vorher/nachher | nein |
| `doku-abgleich` | Sonnet | Doku gegen Code (READMEs, Hilfe, `docs\`), vor dem Release | nein |
| `release-pruefer` | Sonnet | vor einem Release: Versionen, Changelog, `check_release.py`, `check_docs.py` | nein |

## 7. Skills (`.claude\skills\`, Ablauf im jeweiligen `SKILL.md`)
| Skill | Zweck |
|---|---|
| `rolle-planung`, `rolle-architekt`, `rolle-entwicklung`, `rolle-pruefung`, `rolle-release` | Rolle starten (nur manuell durch den Benutzer) |
| `ticket` | Ticketsystem bedienen (Posteingang, erledigt, erstellen, übernehmen, abschliessen) |
| `changelog-eintrag` | Eintrag in `news.txt` über `news.py` |
| `version-erhoehen` | Versionen je Komponente setzen |
| `ideen` | Ideen sammeln bis zum GO |
| `web-testen` | Weboberfläche im Vorschau-Server prüfen |
| `release-vorbereiten` | Release-Checkliste und Ablauf |
| `abschlussbericht` | Bericht nach einer Umsetzung mit Versions-String |

## 8. Token und Effizienz
- Nur relevante Dateien und Doku-Abschnitte laden; vorhandene Analysen (Task-Dateien) wiederverwenden; keine Projektanalyse bei kleinen Aufgaben.
- **Gebündelt arbeiten:** Aufträge mit denselben Dateien in einem Durchgang (Gruppen aus `liste -Gruppen`); gemeinsame Dateien einmal lesen, Tests einmal für alle. Ist der Chat einer Rolle noch offen, geht ein Folgeauftrag mit Vorteil an ihn (Dateien schon im Kontext).
- Lange Ausgaben (Build, Tests, Logcat) nur gefiltert lesen.
- Agents nur bei Auslöser (6), mit dem kleinsten passenden Modell.

## 9. Abschlussbericht
Änderungen nach Komponente (STEFFLIX, Desktop, App) · betroffene Dateien · Tests (nicht ausgeführte kennzeichnen) · Tickets und Entscheide (IDs) · offene Punkte/Risiken · was der Benutzer bauen oder testen soll · Versions-String. Was im Ticket steht, nur kurz zusammenfassen (Skill `/abschlussbericht`).

## 10. Ordner
| Ordner/Datei | Zweck |
|---|---|
| `start.html`, `sflix_sys\` | Weboberfläche (`docs\WEB.md`) |
| `STEFFLIX.exe`, `updateStefflix.exe`, `STEFFLIX.apk`, `updater.exe`, `updateData.exe`, `checksums.json`, `ffmpeg.json`, `versions.json`, `README.md`, `.gitignore` | Release-Dateien (`docs\RELEASE.md`) |
| `SFLIX_Tools\` | Entwicklungsordner: `SflixTools\` (Desktop, `docs\DESKTOP.md`), `Android\` (`docs\ANDROID.md`), `Release\`, `Doku\`, `Test\`, Ticketsystem und Guard, alte Werkzeuge (`docs\RELEASE.md`) |
| `docs\` | Dokumentation (gepusht, ohne `intern\`) |
| `coordination\` | Tickets der Rollen (nicht gepusht) |
| `.claude\CLAUDE.md` | diese Datei |
| `.claude\settings.json` | Sperren für `coordination\` und alte Fassungen (gepusht, ohne persönliche Pfade) |
| `.claude\settings.local.json` | Hook `SFLIX_Tools\Auftrag-guard.ps1` vor jedem Werkzeugaufruf und Temp-Ordner (Pfade mit Benutzernamen, nicht gepusht; DEC-363) |
| `.claude\launch.json` | Vorschau-Server `stefflix` (Port 8765) und `stefflix-2` (8767), nur für Tests |
| `.claude\roles\` | Rollenprompts `PLANUNG.md`, `ARCHITEKT.md`, `ENTWICKLUNG.md`, `PRUEFUNG.md`, `RELEASE.md` |
| `.claude\skills\`, `.claude\agents\` | Skills (5 Rollen-Skills und 7 weitere, je Ordner mit `SKILL.md`) und Agents (9 Dateien `<name>.md`), Übersicht in 6 und 7 |
| `.claude\testenv\` | Testumgebung mit Beispielmedien und `Programmdateien_aktualisieren.bat` (`docs\TESTING.md`) |
| `.claude\*.gif`, `.claude\file_example_MP4_1280_10MG.mp4` | Bildschirmaufnahmen und Beispielvideo (Material für die Vorschau-GIF) |
| `.claude\old\`, `.claude\archiv\` | alte Fassungen des Regelwerks (gesperrt, nie lesen) |
