# STEFFLIX – Release, Versionen und Werkzeuge

Gelesen von der Rolle Release (immer), Entwicklung (Versionen, Changelog) und Planung. Nur den nötigen Abschnitt lesen.

## Release-Ordner
Der Projektordner ist der Release-Ordner. Er wird über GitHub Desktop (durch den Benutzer, es gibt kein Git in der Shell) nach GitHub `Apop85/Scripts/js/SFLIX` gepusht, und zwar nur die Dateien, die `build_release` am Ende auflistet, dazu Quellcode und Werkzeuge (Open Source). `.claude\`, `coordination\` und `docs\intern\` werden nie gepusht.

| Datei (Wurzel) | Zweck |
|---|---|
| `start.html` | Einstieg der Weboberfläche (`docs/WEB.md`). |
| `README.md` | Nutzer-Doku für GitHub: Installation, Medien, Autoplay, Player, Browsereinstellungen, Einstellungen, Datensicherung, Fehlerbehebung. |
| `.gitignore` | Dateien, die nie gepusht werden dürfen (persönliche Dateien, Schlüssel, Lizenzen, lokale Dateien von STEFFLIX.exe, Build-Zwischenstände, `coordination/`, `docs/intern/`, alte Fassungen des Regelwerks). Bei neuen solchen Dateien sofort ergänzen; Quellcode und Build-Werkzeuge nie ignorieren. |
| `STEFFLIX.exe` | Desktop ab 4.0.0 (Kopie von `SFLIX_Tools\SflixTools\dist\STEFFLIX.exe`, kopiert von `build_release`). |
| `updateStefflix.exe` | Dieselbe Datei wie `STEFFLIX.exe` (geprüft von `check_release.py`), vorerst nachziehen: Updater bis 3.x laden nur diesen Namen; 4.x benennt sich beim ersten Start mit Oberfläche um (nicht im Release-Ordner). |
| `STEFFLIX.apk` | Android-App (`docs/ANDROID.md`). |
| `updater.exe`, `updateData.exe` | Alte, eingefrorene Python-Programme für den Umstieg von Installationen vor 3.0. STEFFLIX.exe löscht sie bei Nutzern, nie im Release-Ordner. |
| `checksums.json` | SHA256 aller ladbaren Dateien (Web-Dateien, EXEs, `ffmpeg.json`) und die Listen `_webFiles`/`_obsoleteFiles`. Erzeugt `create_checksums.py`; immer zusammen mit den EXEs pushen. |
| `ffmpeg.json` | Vorgegebene ffmpeg-Version (gyan.dev-Build über GitHub-Release): Version, URL, SHA256. |
| `versions.json` | Neueste Versionen von Desktop (Schlüssel `updater`) und ffmpeg, dazu `android` (`version`, `sha256`). Schreibt `release.ps1`; STEFFLIX liest sie nur von GitHub. Nicht in `checksums.json`. |
| `docs\` | Dokumentation (dieser Ordner), gepusht; ohne `docs\intern\`. |

## Versionen
- **Format x.y.z** für STEFFLIX, Desktop und App. Installationen bis 2.04 lesen nur eine Zahl, darum steht in `version.js` zuerst die alte Zahl (x + y/100 + z/10000, z. B. 2.0808), dann `versionName`. Regeln in `SFLIX_Tools\Release\versioning.py` (gleich in core.js `toVersionName`, UpdateTask.cs `WebVersion`, release.ps1).
- **Erhöhen** (Benutzerregel 2026-11-20): Major nur bei vollständiger Überarbeitung; Minor bei neuen Features; Patch bei kleinen Änderungen, Anpassungen, Ergänzungen. Jede Umsetzung (jedes GO) und jede Korrektur nach einem Test erhält eine neue Version – nur für Komponenten mit Änderungen seit dem letzten Build. **Schlägt ein Build fehl, wird zuerst der Fehler behoben; die Version bleibt gleich.**
- **Wo:** STEFFLIX `sflix_sys\version.js` (beide Zeilen) plus Block `<b>Version x.y.z</b>` in `news.txt` (`news.py version`); Desktop `SFLIX_Tools\SflixTools\src\Program.cs` (AssemblyVersion und AssemblyFileVersion); App `SFLIX_Tools\Android\app\build.gradle` (`versionName`, `versionCode`). Skill `/version-erhoehen`.
- **Changelog:** jede Änderung über `SFLIX_Tools\Release\news.py eintrag` im obersten Block; Präfix „Desktop x.y.z:“ bzw. „Android-App x.y.z:“ (mit Version der Komponente; ältere Einträge „Updater x.y.z:“ werden auch gelesen), STEFFLIX ohne Präfix. `news.json` entsteht beim Release.

## Ablauf (`SFLIX_Tools\Release\`)
| Datei | Zweck |
|---|---|
| `build_release.bat` | Einstieg (Doppelklick), startet `release.ps1`. |
| `release.ps1` | 1. Desktop bauen (`dist\STEFFLIX.exe` → `STEFFLIX.exe` und `updateStefflix.exe`; Vorschlag: Version im Quellcode, wenn höher als die bereitgestellte, sonst +1 Patch) → 2. App (Fingerabdruck über `Android\app\src`, Gradle-Dateien, `build.ps1`, `sflix_sys\splash.svg`; Stand in `Android\release_state.json`) → 3. `ffmpeg.json` → 4. STEFFLIX-Version und Changelog → 5. `check_release.py`, `create_news.py`, `create_checksums.py`, `versions.json` → 6. Liste der zu pushenden Dateien. Optionen `-Auto` (alle Vorschläge übernehmen; so nutzt ihn die Rolle Release), `-MarkReleased`. |
| `check_release.py` | Prüft Vollständigkeit (u. a. `versionName` = oberste Version im Changelog, `STEFFLIX.exe` = `updateStefflix.exe`). |
| `create_news.py` | `news.txt` → `news.json` (je Version `version`/`name`, je Eintrag `component`, `componentVersion`, `short`); gruppiert die Einträge nach Typ (`TYPE_ORDER`) und schreibt das in `news.txt` zurück. Test: `python create_news.py <news.txt> <anderes Ziel.json>`. |
| `create_checksums.py` | Erzeugt `checksums.json` mit `_webFiles` (= `WEB_FILES`) und `_obsoleteFiles` (= `OBSOLETE_FILES`). |
| `sflix_update.py` | Dateilisten und Prüffunktionen (`FILES`, `WEB_FILES`, `OBSOLETE_FILES` …). Neue oder entfallene Web-Dateien hier eintragen. |
| `versioning.py` | `version_name`, `legacy_number`, `read_version_js`, `version_js_text`; Selbsttest `python versioning.py`. |
| `news.py` | Changelog lesen und pflegen (`zeigen`, `suchen`, `eintrag`, `anpassen`, `version`, `check`). |
| `check_docs.py` | Prüft, ob jede Projektdatei dokumentiert ist, genannte Dateien existieren und Dateien für Nutzer keine verbotenen Begriffe enthalten. Vor jedem Release. |

**Wer baut:** Release-Builds (`release.ps1`) führt die Rolle Release mit `-Auto` aus oder der Benutzer per Doppelklick; `news.json` und `checksums.json` entstehen nur dabei, nie von Hand. Gepusht wird nur vom Benutzer (GitHub Desktop). Test-Builds (`build.ps1` des Desktops, `build_test_apk.ps1`) machen Prüfung und Release. Ablauf und Checkliste: Skill `/release-vorbereiten`.

## Weitere Werkzeuge in `SFLIX_Tools\`
| Datei | Zweck |
|---|---|
| `Auftrag.ps1`, `Auftrag-Selbsttest.ps1` | Ticketsystem der Rollen (`coordination/README.md`) und sein Selbsttest (schreibt nur in den Temp-Ordner). |
| `Auftrag-guard.ps1`, `Auftrag-guard-Selbsttest.ps1` | Hook vor jedem Werkzeugaufruf (`.claude\settings.json`): PowerShell-Alias-Falle, Array-Abflachung, `coordination/`, Zugriff nur im Projekt und in freigegebenen Orten, Netzlaufwerke gesperrt, STEFFLIX.exe nur mit der Testumgebung; Selbsttest. |
| `Doku\decisions.py` | Entscheidungen lesen und pflegen (`docs/DECISIONS.md`, intern `docs/intern/DECISIONS-intern.md`): `zeigen`, `suchen`, `baum`, `neu`, `ergaenzen`, `status`, `ersetzen`, A–D, `intern`, `index`, `check`. |
| `Test\smb-testshare.ps1`, `Test\smb-testshare.py` | Lokaler SMB-Testserver für den Emulator, Freigabe immer `.claude\testenv` (`docs/TESTING.md`). |
| `Test\avd.ps1` | Eigene Emulatoren `STEFFLIX_API29`/`STEFFLIX_API35` anlegen und starten. |
| `Lizenz\create_license.py` | SFX2-Schlüssel aus einer Webhook-Adresse (`--name`, `--test`, `--decode`, `--liste`); `lizenzen.json` nie pushen. Webhook-Tests als Python-Skript im Scratchpad (PowerShell mit Webhook-Adressen blockiert der Virenscanner). |
| `Bilder\sflixLogo.png`, `Bilder\sflixLogo.svg` | Logo-Quellen (Rot `#e8261a`). |
| `Bilder\ladehintergrund.py` | Hintergrundbild des Ladescreens (erfundene Plakate, unscharf, 560×350): bettet es in `start.html` ein und schreibt `Android\app\src\main\assets\loading_backdrop.jpg`; `--vorschau`. Braucht Pillow. Keine echten Plakate (Urheberrecht). |
| `plakate.py` | Eigenständig: Plakate für Serien, Staffeln und Filme als JPG. |
| `create_serial.py` | Alter Lizenzgenerator bis STEFFLIX 2.4.0, nur Referenz. |
| `serien_umbenennen.zip` | Passwortgeschützt, alt (ersetzt durch „Folgen umbenennen“). Nicht entpacken. |
| `Updater\updateStefflix.py`, `updater.py`, `getfilepath.py`, `sflix_update.py`, `setup.py`, `convertVideos.py`, `teile_zusammenfuegen.py`, `folgenliste.py` | Alte Python-Programme (Quellen von updater.exe/updateData.exe und ersetzten Tools), nur Referenz. |
| `PreviewMaker\src\PreviewMaker.cs`, `build.ps1`, `make-previews.ps1`, `make-previews.log` | Alter Vorschaubild-Generator (ersetzt durch PreviewTask). |
