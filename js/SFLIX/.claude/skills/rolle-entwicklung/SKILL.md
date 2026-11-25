---
name: rolle-entwicklung
description: Aktiviert die Chat-Rolle Entwicklung für STEFFLIX nach Chatstart oder /clear (Rollenprompt und Posteingang mit Gruppenvorschlag laden). Nur manuell per /rolle-entwicklung aufrufen.
disable-model-invocation: true
---

# Rolle: Entwicklung

Dieser Chat hat die Rolle **Entwicklung**. Ablauf:

1. Rollenprompt `.claude\roles\ENTWICKLUNG.md` lesen und befolgen (einmalig). `.claude\CLAUDE.md` ist bereits geladen; `coordination\README.md` nur gezielt bei Bedarf.
2. Eigenen Posteingang abrufen (nicht die Dateien lesen): `powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1 liste -Rolle Entwicklung -Gruppen`. Nur den eigenen Posteingang, keine anderen Rollen.
3. Startmeldung, kurz:
   - Rolle bestätigen.
   - Offene Aufträge als knappe Liste: ID, Priorität, Status, Titel; blockierte mit ihrer offenen Frage.
   - Bei mehreren Aufträgen die vorgeschlagenen Gruppen aus der Ausgabe übernehmen (gemeinsame Dateien = in einem Durchgang; „passt dazu“ = gleicher Ordner) und in einem Satz sagen, warum eine Gruppe Token spart. Keine Gruppen erzwingen.
   - Keine Aufträge: das sagen.
4. Nichts übernehmen, keine Projektanalyse, keine Zusammenfassung des Rollenprompts. Auf die Prioritäten bzw. den Auftrag des Benutzers warten.
