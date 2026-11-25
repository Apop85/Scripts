---
name: changelog-eintrag
description: Eintrag im Changelog sflix_sys\news.txt über SFLIX_Tools\Release\news.py erstellen oder anpassen (Typ, Komponente mit Version, eine Zeile, keine verbotenen Begriffe). Verwenden nach jeder Änderung an Weboberfläche, Desktop oder App oder per /changelog-eintrag.
argument-hint: "[Komponente und Kurzbeschreibung]"
---

# Changelog-Eintrag

Änderung: $ARGUMENTS

1. **Block prüfen:** `python SFLIX_Tools\Release\news.py zeigen` zeigt den obersten Block. Gehört die Änderung zu einer neuen STEFFLIX-Version (dieses GO noch ohne Block), zuerst `/version-erhoehen`.
2. **Gleiches Thema schon vorhanden?** `python SFLIX_Tools\Release\news.py suchen <Stichwort>`. Wenn ja: `news.py anpassen --alt "<Teiltext>" --text "<neuer Text ohne Präfix>"` (anpassen statt doppeln).
3. **Neu eintragen:** `python SFLIX_Tools\Release\news.py eintrag --typ FEATURE|INTERFACE|OPTIMIERUNG|BUGFIX --komponente stefflix|desktop|app --text "<Text>"`.
   - Präfix „Desktop x.y.z:“ bzw. „Android-App x.y.z:“ setzt das Skript (Version aus `Program.cs` bzw. `app\build.gradle`; vorher die Version erhöhen). Text ohne Präfix.
   - Text für Nutzer: was sich für sie ändert, Schweizer Schreibweise, keine Code-Namen, wenn es auch ohne geht.
   - Verboten: Netflix, Webhook-Adressen (das Skript lehnt sie ab).
   - Exit 3 = ähnlicher Eintrag vorhanden → anpassen oder bewusst `--trotzdem`.
4. `python SFLIX_Tools\Release\news.py check` am Ende einer Umsetzung.
5. `news.json` nie von Hand: Sie entsteht beim Release.
