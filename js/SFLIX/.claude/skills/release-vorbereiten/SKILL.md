---
name: release-vorbereiten
description: Release von STEFFLIX vorbereiten und ausführen (Rolle Release) - Umfang, Versionen, Prüfskripte, offene Fehler, release.ps1 -Auto, Push-Liste und Testhinweise für den Benutzer. Verwenden, wenn der Benutzer ein Release will, oder per /release-vorbereiten.
argument-hint: "[Komponenten oder Versionen, optional]"
---

# Release vorbereiten

Vorgabe: $ARGUMENTS

**Nur die Rolle Release** (oder auf ausdrücklichen Auftrag des Benutzers). Gepusht wird nur vom Benutzer (GitHub Desktop).

1. **Umfang:** `python SFLIX_Tools\Release\news.py zeigen` (oberster Block, ggf. weitere ungebaute Blöcke laut Stand in `.claude\CLAUDE.md`); welche Komponenten haben sich seit dem letzten Release geändert (`release_state.json` von Desktop und App)?
2. **Offene Aufträge:** `liste -Status OPEN,IN_PROGRESS,BLOCKED` (ohne `-Rolle`, nur Überblick): offene CRITICAL/HIGH oder Fehler der Prüfung → nicht freigeben, dem Benutzer melden.
3. **Prüfskripte** (Agent `release-pruefer` oder selbst, Ausgaben gefiltert): `python SFLIX_Tools\Release\versioning.py`, `python SFLIX_Tools\Release\news.py check`, `python SFLIX_Tools\Doku\decisions.py check`, `python SFLIX_Tools\Release\check_docs.py`, `python SFLIX_Tools\Release\check_release.py`. Fehler beheben lassen (Entwicklung) oder Doku selbst korrigieren.
4. **Freigaberegeln:** keine Freigabe bei offenen kritischen Fehlern, nicht ausgeführten nötigen Tests, Build-Problemen, ungeklärten Sicherheitsfragen, fehlenden Versions- oder Migrationsangaben. Unbekannte Testergebnisse gelten nicht als bestanden.
5. **Ausführen:** `powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\release.ps1 -Auto` (im Hintergrund, Ausgabe gefiltert). Build-Fehler → Auftrag an die Entwicklung; die Version bleibt, danach erneut ausführen.
6. **Dem Benutzer melden:** Versions-String, Push-Liste aus der Ausgabe (inkl. `checksums.json` zusammen mit den EXEs, App-Quellcode und `STEFFLIX.apk` falls neu gebaut, `docs\` ohne `intern\`), was er auf dem Gerät testen soll, Freigabeempfehlung. Ausdrücklich fragen, ob der Projektordner in den GitHub-Klon gespiegelt werden soll.
7. **In den GitHub-Klon spiegeln** (DEC-362, DEC-363), nur wenn das Release erfolgreich war und der Benutzer im Chat ausdrücklich zugestimmt hat:
   - Probelauf: `python SFLIX_Tools\Release\sync_github.py` (ändert nichts). Dem Benutzer die Zusammenfassung zeigen (kopieren, ersetzen, löschen) und die Löschungen nennen. Bricht der Probelauf wegen der Sperrliste oder eines persönlichen Suchmusters ab: nichts umgehen, Fund melden (Auftrag an die Entwicklung).
   - Nach seiner Bestätigung genau dieses Ergebnisses: `python SFLIX_Tools\Release\sync_github.py --ausfuehren --ja`. `--viele-loeschen-ok` nur, wenn der Benutzer die gemeldeten vielen Löschungen ausdrücklich bestätigt hat. `--ohne-suchmuster` nie.
   - Der Guard lässt den Klon nur über dieses Skript zu (kein Lesen, Kopieren, Löschen von Hand, keine weiteren Befehle in derselben Zeile). Danach committet und pusht der Benutzer im Klon mit GitHub Desktop.
8. **Nach dem Push** (auf Ansage): `release.ps1 -MarkReleased`, Tabelle „Stand“ in `.claude\CLAUDE.md` nachführen, Ticket abschliessen (`-Entscheide`).
