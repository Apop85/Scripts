---
name: version-erhoehen
description: Versionen von STEFFLIX, Desktop und Android-App nach den Versionsregeln setzen (version.js beide Zeilen und Block in news.txt, Program.cs, app\build.gradle versionName/versionCode). Verwenden bei jedem GO bzw. jeder Korrektur nach einem Test für jede geänderte Komponente oder per /version-erhoehen.
argument-hint: "[stefflix|desktop|app] [patch|minor|major|x.y.z]"
---

# Version erhöhen

Auftrag: $ARGUMENTS

**Regeln** (Benutzer, `docs\RELEASE.md`): Major nur bei vollständiger Überarbeitung · Minor bei neuen Features · Patch bei kleinen Änderungen, Anpassungen, Ergänzungen. Jedes GO und jede Korrektur nach einem Test erhöht jede Komponente mit Änderungen seit dem letzten Build (nur einmal je GO). **Schlug nur der Build fehl, bleibt die Version** (Fehler beheben, kein neuer Block). Ohne Änderung bleibt die Komponente unverändert. Die Startversion nennt der Benutzer am Anfang; sonst den Stand in `.claude\CLAUDE.md` und die Dateien lesen.

1. **Aktuelle Versionen lesen:** `sflix_sys\version.js` (`versionName`), `SFLIX_Tools\SflixTools\src\Program.cs` (`AssemblyVersion`), `SFLIX_Tools\Android\app\build.gradle` (`versionName`, `versionCode`). Wurde die Komponente in diesem GO schon erhöht? Dann nicht nochmals.
2. **STEFFLIX:** beide Zeilen in `version.js` setzen, z. B. `var version = 2.0809;` und `var versionName = "2.8.9";` (alte Zahl = x + y/100 + z/10000, Regeln in `SFLIX_Tools\Release\versioning.py`). Dann `python SFLIX_Tools\Release\news.py version 2.8.9` (neuer Block oben). Auch Änderungen nur an Desktop oder App brauchen einen Block für ihre Einträge: Ist der oberste Block bereits gebaut/bereitgestellt (siehe Stand), neue STEFFLIX-Version nur, wenn STEFFLIX selbst geändert wurde; sonst die Einträge in den obersten Block.
3. **Desktop:** in `Program.cs` `[assembly: AssemblyVersion("4.0.7.0")]` und `[assembly: AssemblyFileVersion("4.0.7.0")]`.
4. **App:** in `app\build.gradle` `versionName "1.3.2"` und `versionCode 10302` (x·10000 + y·100 + z).
5. Prüfen: `python SFLIX_Tools\Release\versioning.py` (Selbsttest) und `python SFLIX_Tools\Release\news.py check` (oberster Block = `version.js`).
6. Tabelle „Stand“ in `.claude\CLAUDE.md` (Spalte „umgesetzt, noch nicht gebaut“ und „nächste Version“) nachführen und den Versions-String im Abschlussbericht nennen (Reihenfolge STEFFLIX, Desktop, App; unveränderte weglassen).
