---
name: ticket
description: Ticketsystem von STEFFLIX bedienen (SFLIX_Tools\Auftrag.ps1) - Posteingang mit Gruppen, eigene Arbeit mit erledigt erfassen, Auftrag an eine andere Rolle erstellen, übernehmen, blockieren, abschliessen mit -Entscheide. Verwenden bei jeder Aufgabe (jede Aufgabe bekommt ein Ticket) oder per /ticket.
argument-hint: "[liste | erledigt | erstellen | uebernehmen | abschliessen | blockieren]"
---

# Ticket

Aktion (optional): $ARGUMENTS

Aufruf immer: `powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1 <Befehl> -Rolle <eigene Rolle> …` (Rollen: `Planung`, `Architekt`, `Entwicklung`, `Pruefung`, `Release`). Nie Dateien in `coordination\` direkt ändern (Guard). Nach Exit 0 nichts nachlesen. Parameter: `… <Befehl> -Help`.

1. **Welcher Fall?**
   - Eigene Arbeit, keine andere Rolle nötig (auch kleine Änderungen) → **`erledigt`** nach getaner Arbeit:
     `erledigt -Rolle <R> -Titel "…" -Aufgabe "…" -Dateien "<Pfade ab Projektwurzel>" -Arbeiten "…" -Tests "…" -Entscheide "DEC-…|keine"`
   - Ergebnis einer anderen Rolle nötig → **`erstellen`** (vorher `liste -Text <Datei>` auf laufende Arbeit prüfen; bestehenden eigenen OPEN-Auftrag an dieselbe Rolle lieber mit `aendern -Feld Aufgabe -Anhaengen` ergänzen):
     `erstellen -Rolle <R> -An <Rolle> -Prio LOW|MEDIUM|HIGH|CRITICAL -Titel "…" -Aufgabe "…" -Ergebnis "…" -Dateien "…" -Kontext "<alles, was der Empfänger wissen muss>" [-Abhaengigkeiten "<Vorgänger-ID>"] [-Begruendung "…" bei HIGH/CRITICAL]`
   - Auftrag im eigenen Posteingang → erst nach der Priorisierung durch den Benutzer: Task-Datei einmal lesen, `status -Rolle <R> -Id <n> -Neu IN_PROGRESS`.
   - Es fehlt etwas → `status -Rolle <R> -Id <n> -Neu BLOCKED -Frage "<konkrete Frage>"`.
   - Fertig → Folgeaufträge zuerst, dann `abschliessen -Rolle <R> -Id <n> -Arbeiten "<was, welche Dateien>" -Tests "<ausgeführt mit Ergebnis; nicht ausgeführt mit Grund>" -Entscheide "DEC-…|keine" [-Risiken "…"] [-Folgeschritt "<ID> an <Rolle>"]`.
2. **Entscheide:** Wurde mit dem Benutzer etwas entschieden, zuerst `python SFLIX_Tools\Doku\decisions.py neu …` (mit `--ticket <ID>`), dann die DEC-ID bei `-Entscheide` angeben.
3. **Mehrzeiliges** oder Text mit `"`, `` ` ``, `$`: Datei im Scratchpad im Format `- Schlüssel: Wert` (Folgezeilen zwei Leerzeichen eingerückt) und `-Eingabe <Datei>`.
4. **Exit-Codes:** 1 korrigieren · 2 nicht umgehen (falsche Rolle) · 3 Stand prüfen bzw. Dublette ergänzen oder bewusst `-Trotzdem` · 4 wiederholen · 5/6 `check -Fix -Rolle <R>`.
   - „bereits abgeschlossen“ (Exit 3): abgeschlossene Tickets nicht ändern, neue Arbeit als Folgeauftrag. Nur ausnahmsweise nachführen (z. B. vergessene Testangabe, Benutzer verlangt es): `aendern … -Anhaengen -Erzwingen -Notiz "<Grund>"`.
   - „IN_PROGRESS bei <Rolle>“ (Exit 3): Der Empfänger arbeitet schon daran und bemerkt Änderungen eventuell nicht. Eigenen Auftrag erfassen (`erstellen -An <Rolle> -Abhaengigkeiten <ID>`) und dem Benutzer melden. Nur in Absprache mit dem Benutzer: `aendern … -Erzwingen -Notiz "<Grund>"`, danach melden, damit der Empfänger neu liest.
5. Im Chat nur ID, Empfänger und Status nennen (der Benutzer überbringt Aufträge an andere Chats).
