---
name: ideen
description: Ideen des Benutzers für STEFFLIX sammeln bis zum GO - nichts umsetzen, nummerierte Rückfragen, Widersprüche und Vorschläge klären, vor dem GO die Liste nach Komponenten bestätigen lassen, danach in Aufträge zerlegen. Verwenden, wenn der Benutzer neue Ideen beschreibt, oder per /ideen.
argument-hint: "[Idee]"
---

# Ideen sammeln

Idee: $ARGUMENTS

1. **Nichts umsetzen**, keine Dateien ändern, bis der Benutzer „GO“ oder „Release erstellen“ sagt.
2. Je Idee kurz prüfen (nur Nötiges lesen): betroffene Komponenten (STEFFLIX, Desktop, Android-App), bestehende Entscheide (`python SFLIX_Tools\Doku\decisions.py suchen <Stichwort>`), Widersprüche zu Regeln aus `.claude\CLAUDE.md`, Folgen für die App.
3. Antwort: Einschätzung in wenigen Sätzen, dann **nummerierte** Rückfragen und Vorschläge (fortlaufend über die ganze Unterhaltung, jede für sich verständlich, mit Empfehlung). Nicht nach jeder Runde die ganze Liste ausgeben.
4. Ideen „für später“: Auftrag mit Priorität LOW an die Planung (`/ticket`, `erstellen -An Planung -Prio LOW`), nicht umsetzen.
5. **Vor dem GO:** auf Wunsch oder wenn der Benutzer GO sagt, die vollständige Liste der Änderungen gegliedert nach STEFFLIX, Desktop, Android-App (ohne Änderung „keine Änderungen“) zur Bestätigung vorlegen.
6. **Nach dem bestätigten GO:** Entscheide festhalten (`decisions.py neu`, mit Ticket), Aufträge an die zuständigen Rollen erstellen (zusammenhängende Kleinigkeiten in einem Auftrag) bzw. als Rolle Entwicklung selbst umsetzen. Danach wieder Ideen sammeln bis zum nächsten GO.
