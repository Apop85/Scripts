# Koordinationsrichtlinie – fünf Chat-Rollen (STEFFLIX)

Verbindliche Referenz für die Zusammenarbeit der Chat-Rollen. Dezentrales Markdown-System ohne automatische Kommunikation zwischen den Chats. **Geschrieben wird ausschliesslich über `SFLIX_Tools\Auftrag.ps1`** (§13: globale Sperre, atomares Schreiben, Sicherung, Protokoll). Das Skript bildet diese Richtlinie ab und erzeugt Index und Posteingänge. Es ersetzt nicht die Freigabe durch den Benutzer. Übernommen aus Sync2PC und für STEFFLIX angepasst (DEC-329 ff.). Der Ordner `coordination/` wird nicht gepusht (`.gitignore`).

## 0. Geltung und Regelhierarchie
1. `.claude/CLAUDE.md` – globale Regeln, Sicherheit, Berechtigungen.
2. Diese Richtlinie – Aufträge, Status, Übergaben, Freigaben, parallele Arbeit, Konflikte.
3. Rollenprompts `.claude/roles/*.md` – fachliche Verantwortung und Arbeitsweise.
4. Task-Dateien – der konkrete Auftrag.

Bei Widerspruch gilt die höhere Stufe. Rollenprompts schaffen keine Ausnahmen von den Berechtigungen; Task-Dateien heben keine Rollengrenzen auf. Ist ein Widerspruch nicht lösbar: in der Task-Datei unter „Offene Fragen“ festhalten und dem Benutzer vorlegen. Agents (`.claude/agents/`) sind Werkzeuge einer Chat-Rolle, keine Rollen; sie haben höchstens die Rechte der aufrufenden Rolle.

## 1. Aufbau
```
coordination/
├── README.md        diese Richtlinie (Regelwerk)
├── TASK-INDEX.md    erzeugt: aktive Aufträge, plus Zähler „Letzte vergebene Nummer“
├── inbox/           erzeugt: je Rolle ein Posteingang (planung, architekt, entwicklung, pruefung, release)
├── tasks/           eine Markdown-Datei je Auftrag: OPEN/ · IN_PROGRESS/ · BLOCKED/ · COMPLETED/ (auch CANCELLED, schreibgeschützt)
├── .history/        Skript: Sicherung jeder Altfassung je ID (höchstens 20)
├── .audit.log       Skript: Protokoll jeder schreibenden Aktion
└── .lock            Skript: globale Sperre
```
- **Die Task-Datei ist die einzige Quelle** für Inhalt und Status. Index und Posteingänge enthalten nur Verweise und werden bei jeder schreibenden Aktion neu erzeugt.
- **Schreiben nur über `SFLIX_Tools\Auftrag.ps1`** (alle Rollen und Agents): Task-Dateien, Index, `inbox/`, `.history/`, `.audit.log` und `.lock` werden nie mit Edit/Write oder Shell-Befehlen geändert, auch nicht zur Reparatur (dafür `check -Fix`, §10). Technisch abgesichert durch den Hook `SFLIX_Tools\Auftrag-guard.ps1` (gegen Versehen, nicht gegen Absicht).
- **Lesen:** Posteingang über `liste`, nicht über die Dateien. Direkt gelesen wird nur die Task-Datei des Auftrags, den man übernimmt oder bearbeitet, einmal. Index, `inbox/`, `tasks/COMPLETED/`, `.history/`, `.audit.log` nur mit konkretem Grund (z. B. Vorgänger aus „Abhängigkeiten“, Fehlersuche nach Exit 3, 5, 6).

## 2. Rollen und Zuständigkeiten
Jeder Auftrag hat **genau eine verantwortliche Rolle: den Empfänger**. Andere Rollen wirken nur über eigene Aufträge mit.

| Rolle (Posteingang) | Entscheidet | Setzt um | Prüft |
|---|---|---|---|
| Planung (`planung`) | Zerlegung, Rollenwahl, Reihenfolge, Priorität; Ideen sammeln bis zum GO | Aufträge für Benutzeraufträge, Ideen als LOW-Aufträge | – |
| Architekt (`architekt`) | technische Architektur, Schnittstellen, Datenformate, Risiken | Analysen, Konzepte, Architektur-Dokumente in `docs/`; Code nur auf ausdrücklichen Auftrag des Benutzers | technische Umsetzbarkeit |
| Entwicklung (`entwicklung`) | Implementierungsdetails innerhalb der Vorgaben | Code aller Komponenten (Web, Desktop, App), `news.txt`, Versionen, Hilfe/README, Fehleranalyse | eigene gezielte Tests |
| Prüfung (`pruefung`) | Prüfurteil (funktional, Sicherheit, Performance) | Prüfberichte, Test-Builds, Tests im Emulator und in der Testumgebung | Qualität, Regressionen, Sicherheit, Performance |
| Release (`release`) | Freigabeempfehlung, Doku-Konsistenz | Release-Ablauf (`release.ps1 -Auto`), `check_docs.py`, Doku-Abgleich | Versionen, Changelog, Doku gegen Code |

**Planung** ist Ansprechpartner und Koordinator des Benutzers, kein fachlicher Vorgesetzter und keine Durchlaufstation. Sie bezieht nur die nötigen Rollen ein.

## 3. Übergabematrix
| Absender | Zulässige Empfänger |
|---|---|
| Benutzer | jede Rolle (direkt im Chat der Rolle; Absender „Benutzer“) |
| Planung | Architekt, Entwicklung, Prüfung, Release |
| Architekt, Entwicklung, Prüfung, Release | jede andere Rolle; Planung **nur** bei Koordinationsbedarf oder für Ideen |

**Jede Aufgabe bekommt ein Ticket** (Benutzerauftrag 2026-11-20, auch kleine): Eigene Arbeit ohne andere Rolle wird mit `erledigt` in einem Aufruf erfasst (anlegen, übernehmen, abschliessen). Für das Ergebnis einer anderen Rolle gibt es einen Auftrag mit `erstellen`. Kleine Rückfragen im Chat erzeugen kein Ticket. Interne Agents ersetzen keine Übergabe.

## 4. Chatstart und `/clear`
`CLAUDE.md` wird immer geladen, die Rolle nicht. Nach Start oder `/clear` den Rollen-Skill aufrufen: `/rolle-planung`, `/rolle-architekt`, `/rolle-entwicklung`, `/rolle-pruefung`, `/rolle-release`. Der Skill lädt den Rollenprompt, ruft den eigenen Posteingang mit `liste -Rolle <R> -Gruppen` ab und meldet die offenen Aufträge mit Gruppenvorschlag. **Keine Übernahme beim Start**: Der Benutzer nennt die Prioritäten, erst danach beginnt die Arbeit.

## 5. Neuen Auftrag erstellen (Absender)
1. Empfänger nach §2/§3 prüfen. Gibt es schon einen aktiven Auftrag mit gleichem Ziel oder einen eigenen OPEN-Auftrag an dieselbe Rolle zum selben Stand, diesen ergänzen (`aendern -Feld Aufgabe -Anhaengen`).
2. `Auftrag.ps1 erstellen` (§13). Das Skript vergibt die ID (`SFLIX-YYYYMMDD-NNN`, fortlaufend), legt die Datei nach §12 an, erzeugt Index und Posteingänge und prüft Dubletten (Exit 3: bestehenden ergänzen oder `-Trotzdem`).
3. Inhalt vollständig, aber kompakt; keine Chatverläufe, keine Quellcodekopien, keine erfundenen Pfade. **Betroffene Dateien mit Pfad ab der Projektwurzel** (z. B. `sflix_sys/js/player.js`), damit `liste -Gruppen` gruppieren kann. Längere Berichte als Abschnitt (`aendern -Abschnitt`).
4. Im eigenen Chat ID und Empfänger melden (der Benutzer überbringt sie).

**Priorität:** CRITICAL = akute Gefahr für Daten oder Grundfunktion · HIGH = wichtige Funktion, kritischer Fehler · MEDIUM = reguläre Funktion/Verbesserung · LOW = optional, Komfort, Ideen für später. HIGH/CRITICAL begründen (`-Begruendung`).

## 6. Statusmodell, Übernahme, Abschluss
| Status | Bedeutung | Wechsel durch |
|---|---|---|
| OPEN | erstellt, nicht übernommen | Absender |
| IN_PROGRESS | Empfänger hat übernommen | Empfänger |
| BLOCKED | Information, Freigabe oder Vorarbeit fehlt; Frage unter „Offene Fragen“ | Empfänger |
| COMPLETED | erledigt und dokumentiert | Empfänger |
| CANCELLED | abgebrochen, mit Begründung | Absender oder Benutzer |

Zulässig: OPEN → IN_PROGRESS · OPEN → BLOCKED · IN_PROGRESS ↔ BLOCKED · IN_PROGRESS → COMPLETED · OPEN/IN_PROGRESS/BLOCKED → CANCELLED. Kein Rücksprung aus COMPLETED/CANCELLED: Folgeauftrag. Abgeschlossene Aufträge sind gesperrt: `status`, `abschliessen` und `aendern` brechen mit Exit 3 („bereits abgeschlossen“) ab. Nur ausnahmsweise (z. B. vergessene Testangabe, Benutzer verlangt Nachtrag) `aendern … -Erzwingen -Notiz "<Grund>"`: nur Anhängen bzw. `-Abschnitt`, nie Ersetzen; der Verlauf vermerkt „[erzwungen: Grund]“.

**In Bearbeitung einer anderen Rolle (DEC-358):** Ist ein Auftrag IN_PROGRESS, hat der Empfänger ihn bereits gelesen und bemerkt spätere Änderungen eventuell nicht. `aendern` einer anderen Rolle bricht darum mit Exit 3 ab. Neue Anforderungen als eigenen Auftrag an den Empfänger erfassen (`erstellen -An <Empfänger> -Abhaengigkeiten <ID>`) und dem Benutzer melden. Nur ausnahmsweise (in Absprache mit dem Benutzer) `aendern … -Erzwingen -Notiz "<Grund>"`: Ersetzen und Anhängen erlaubt, Verlauf „[erzwungen: Grund]“, danach dem Benutzer melden, damit der Empfänger den Auftrag neu liest. OPEN und BLOCKED (Antworten auf „Offene Fragen“) sind nicht betroffen.

**Übernahme:** nur an die eigene Rolle adressierte Aufträge, erst nach der Priorisierung durch den Benutzer → Task-Datei einmal lesen, Dateistand prüfen (§9) → `status -Neu IN_PROGRESS`. Fehlt etwas: `status -Neu BLOCKED -Frage "…"`.

**Abschlussbedingungen:** Durchgeführte Arbeiten, betroffene Dateien, **tatsächlich** ausgeführte Tests mit Ergebnis, nicht ausgeführte mit Grund, offene Punkte, Restrisiken und **Entscheidungen** stehen in der Task-Datei. Nichts erfinden. Nötige Freigaben sind vermerkt (§8), Folgeaufträge angelegt. Eine abgeschlossene Implementierung gilt nicht als geprüft.

**Abschluss:** zuerst Folgeaufträge erstellen, dann `abschliessen` mit `-Arbeiten`, `-Tests`, **`-Entscheide`** (DEC-IDs der festgehaltenen Entscheide oder `keine`, Pflicht), bei Bedarf `-Risiken`, `-Folgeschritt`.

## 7. Übergaben und Folgeaufträge
Ergebnisse gehen **direkt** an die Rolle des nächsten Schritts, als Folgeauftrag mit „Abhängigkeiten: <Vorgänger-ID>“. Der Folgeauftrag ist in sich vollständig (Erkenntnisse im „Technischen Kontext“), damit der Empfänger die Vorgänger-Datei nicht lesen muss.

| Von | An | Wann |
|---|---|---|
| Planung | Architekt / Entwicklung | nach dem GO: Konzept nötig bzw. direkt umsetzbar |
| Architekt | Entwicklung | freigegebenes technisches Konzept |
| Entwicklung | Prüfung | Umsetzung zur Prüfung (vor einem Build oder Release, auf Wunsch) |
| Prüfung | Entwicklung | Fehler, Lücken, Korrekturen (gesammelt in **einem** Auftrag) |
| Entwicklung, Prüfung | Release | Stand ist bereit für das Release |
| Release | Entwicklung / Architekt | Doku weicht vom Code ab, Release-Prüfung schlägt fehl |
| jede Rolle | Planung | neue Idee (LOW), Koordinationsbedarf |

## 8. Benutzerfreigaben und Entscheidungen
Freigeben kann nur der Benutzer. Freigabepflichtig: grössere Architekturänderungen, Technologieentscheidungen, wesentliche Änderungen am Verhalten, sicherheits- oder datenverlustkritische Abläufe, destruktive Aktionen ausserhalb der Testumgebung, Veröffentlichungen (Push macht der Benutzer selbst) und Änderungen am Regelwerk (`CLAUDE.md`, diese Richtlinie, Rollenprompts, Skills, Agents, Guard). Die zuständige Rolle legt die Entscheidung dem Benutzer **direkt** vor (nummeriert, mit Empfehlung). Vermerk: `aendern -Feld Benutzerfreigabe -Wert "erteilt <Datum>, <was>, Chat <Rolle>"`. Bis zur Freigabe BLOCKED.

**Entscheidungen festhalten (jede Rolle):** Getroffene Entscheide (Verhalten, Architektur, Schnittstellen, Datenformate, Standardwerte, verworfene Alternativen, Regeln der Arbeitsweise) trägt die Rolle, die sie mit dem Benutzer getroffen hat, **im selben Schritt** mit `SFLIX_Tools\Doku\decisions.py neu` in `docs/DECISIONS.md` ein (mit Ticket-ID) und nennt die DEC-IDs beim Abschluss unter `-Entscheide`. Nur getroffene Entscheide, keine offenen Fragen. Gewöhnliche Bugfixes sind keine Entscheide. Interne Entscheide nur mit `--intern` (interne Datei, nicht gepusht).

## 9. Parallele Sitzungen und Wiederaufnahme
- Vor Übernahme und vor jeder Änderung an Projektdateien den Dateistand prüfen (Git gibt es nicht). Gemeinsame Dateien unmittelbar vorher neu lesen und nur gezielt ändern. Für `coordination/` übernimmt das Skript Sperre und Konflikterkennung.
- Betroffene Dateien eines IN_PROGRESS-Auftrags fasst keine andere Rolle an. Prüfen mit `liste -Text <Datei>`; `erstellen` warnt bei gleichen Dateien.
- Konkurrierende Änderung: stoppen, nichts überschreiben, neu lesen; sonst den Benutzer fragen.
- Abgebrochene Sitzung: dieselbe Rolle nimmt den IN_PROGRESS-Auftrag nach `/clear` über den Posteingang wieder auf (Bearbeitungsverlauf lesen, `aendern -Feld Bearbeitungsverlauf -Wert "wieder aufgenommen"`). Erledigtes nicht wiederholen, fehlende Belege nicht erfinden.

## 10. Wiederherstellung
Reihenfolge des Skripts: Task-Datei atomar ersetzen (vorher Sicherung) → verschieben → Schreibschutz → Index → Posteingänge → Audit. Bei Abbruch gilt die Status-Zeile der Task-Datei. Reparatur nur mit `check` und `check -Fix -Rolle <Rolle>`, nie von Hand. Meldet ein Aufruf „ist geschrieben, aber Übersicht/Audit fehlgeschlagen“: nicht wiederholen, sondern `check -Fix`. Doppelte Nummern, leere oder mehrdeutige Dateien, fehlende Pflichtfelder, fremde Dateien dem Benutzer vorlegen.

**BLOCKED:** `status -Neu BLOCKED -Frage "<konkrete Frage>"`. Muss eine andere Rolle antworten, eigenen Auftrag an sie und im blockierten Auftrag unter „Abhängigkeiten“ nennen (`-Anhaengen`). Antwort mit `aendern -Feld "Offene Fragen" -Wert "<Datum> Antwort: …"`, danach `status -Neu IN_PROGRESS`.

## 11. Konfliktbehandlung
Fachliche Konflikte klären die Rollen direkt. Innerhalb ihres Bereichs entscheidet die zuständige Rolle: Architektur → Architekt, Implementierungsdetail → Entwicklung, Prüfurteil → Prüfung, Doku-Konsistenz → Release. Keine Einigung oder Benutzerentscheidung nötig → beide Positionen kurz dem Benutzer vorlegen.

## 12. Vorlage Task-Datei
Kopfzeile `# <ID> – <Titel>`, danach die Felder (leere entfallen). Pflicht: ID/Titel, Erstellt am, Absender, Empfänger, Priorität, Status, Aufgabenbeschreibung, Erwartetes Ergebnis, Bearbeitungsverlauf, Übernahmebestätigung; beim Abschluss zusätzlich Durchgeführte Arbeiten, Tests und Ergebnisse, Entscheidungen. Weitere Felder: Übergabegrund, Technischer Kontext, Betroffene Dateien, Dokumentationsverweise, Benutzerfreigabe, Abhängigkeiten, Folgeschritt, Offene Fragen, Verbleibende Risiken, Abgeschlossen am.

## 13. Bedienung `SFLIX_Tools\Auftrag.ps1`
Das Skript muss nicht gelesen werden: `Auftrag.ps1 -Help` bzw. `Auftrag.ps1 <Befehl> -Help`.

**Aufruf** (PowerShell, Projektwurzel): `powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1 <Befehl> -Rolle <eigene Rolle> …`. In den Beispielen steht `Auftrag.ps1` für diesen Aufruf.
- `-Rolle`: `Planung`, `Architekt`, `Entwicklung`, `Pruefung`, `Release`, `Benutzer` (Altnamen wie `QA` oder `JavaScript Developer` werden zugeordnet). Agents verwenden die Rolle des aufrufenden Chats.
- `-Id`: volle ID oder nur die Nummer. `-WhatIf`: zeigt die Änderung, schreibt nichts. `-Json`: nur für maschinelle Auswertung.
- **Nach Exit 0 nichts nachlesen**; `WARNUNG:` beachten. Mehrzeiliges oder Text mit `"`, `` ` ``, `$` über `-Eingabe <Datei im Scratchpad>`.

| Befehl | Zweck | Parameter (Pflicht **fett**) | Wer |
|---|---|---|---|
| `erstellen` | neuer Auftrag | **`-An`**, **`-Titel`**, **`-Prio`**, **`-Aufgabe`**, **`-Ergebnis`**; **`-Begruendung`** bei HIGH/CRITICAL; optional `-Kontext`, `-Dateien`, `-Abhaengigkeiten`, `-Freigabe`, `-Folgeschritt`, `-Trotzdem` | Absender |
| `erledigt` | eigene Arbeit in einem Aufruf | **`-Titel`**, **`-Aufgabe`**, **`-Arbeiten`**, **`-Tests`**, **`-Entscheide`**; optional `-Prio` (Standard LOW), `-Dateien`, `-Kontext`, `-Risiken`, `-Folgeschritt` | eigene Rolle |
| `status` | Statuswechsel ausser Abschluss | **`-Id`**, **`-Neu`** `IN_PROGRESS` · `BLOCKED` mit **`-Frage`** · `CANCELLED` mit **`-Notiz`** | Empfänger; CANCELLED: Absender oder Benutzer |
| `abschliessen` | IN_PROGRESS → COMPLETED | **`-Id`**, **`-Arbeiten`**, **`-Tests`**, **`-Entscheide`**; optional `-Risiken`, `-Folgeschritt`, `-Notiz` | Empfänger |
| `aendern` | Feld setzen/ergänzen, Abschnitt anhängen | **`-Id`** und **`-Feld`** mit `-Wert`/`-Eingabe` (`-Anhaengen`) oder **`-Abschnitt`** mit **`-Eingabe`**; abgeschlossene und IN_PROGRESS einer anderen Rolle nur mit `-Erzwingen -Notiz` | Empfänger; andere nur für Ergänzungen nach dieser Richtlinie (nicht während IN_PROGRESS) |
| `liste` | Überblick, nur lesend | `-Rolle` = eigener Posteingang; Filter `-An`, `-Von`, `-Status` (`Alle`), `-Prio`, `-Text`, `-IdVon`/`-IdBis`, `-Seit`/`-Bis`; `-MitDateien`; **`-Gruppen`** (Gruppen nach gemeinsamen Dateien, „passt dazu“ im gleichen Ordner, BLOCKED mit offener Frage) | alle |
| `check` | Konsistenzprüfung; `-Fix` repariert | `-Fix` braucht `-Rolle`; `-Details` | alle |

**Exit-Codes:** 0 ok · 1 Eingabe/Pflichtfeld: korrigieren · 2 Berechtigung: nicht umgehen · 3 Zustandskonflikt oder mögliche Dublette: Stand prüfen · 4 Sperre: wiederholen · 5 `check` fand Fehler: `check -Fix` · 6 E/A-Fehler: bei „ist geschrieben, aber …“ nicht wiederholen, sondern `check -Fix`.

**Beispiele:**
```
Auftrag.ps1 liste -Rolle Entwicklung -Gruppen                                  # Rollenstart
Auftrag.ps1 erledigt -Rolle Entwicklung -Titel "Tippfehler Hilfe" -Aufgabe "Hilfe korrigieren" -Dateien "start.html" -Arbeiten "start.html korrigiert" -Tests "Vorschau-Server geprüft" -Entscheide keine
Auftrag.ps1 erstellen -Rolle Entwicklung -An Pruefung -Prio MEDIUM -Titel "Prüfung Download" -Aufgabe "…" -Ergebnis "Prüfbericht" -Dateien "SFLIX_Tools/SflixTools/src/DownloadTask.cs"
Auftrag.ps1 status -Rolle Pruefung -Id 12 -Neu IN_PROGRESS
Auftrag.ps1 abschliessen -Rolle Pruefung -Id 12 -Arbeiten "…" -Tests "…" -Entscheide keine -Folgeschritt "SFLIX-20261120-013 an Entwicklung"
Auftrag.ps1 check
```
Selbsttest (schreibt nur in den Temp-Ordner): `SFLIX_Tools\Auftrag-Selbsttest.ps1`.
