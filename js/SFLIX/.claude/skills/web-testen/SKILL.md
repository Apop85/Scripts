---
name: web-testen
description: Weboberfläche von STEFFLIX im Vorschau-Server mit der Testumgebung .claude\testenv prüfen (Programmdateien aktualisieren, Seiten öffnen, Konsole, Regressionsliste, Screenshot als Beleg). Verwenden nach Änderungen an start.html oder sflix_sys oder per /web-testen.
argument-hint: "[Fokus, z. B. Player, Einstellungen, Seitenwechsel]"
---

# Weboberfläche testen

Fokus: $ARGUMENTS

1. **Testumgebung aktualisieren:** `.claude\testenv\Programmdateien_aktualisieren.bat` ausführen (Medien, `data.js`, `install.js`, `license.js` bleiben).
2. **Vorschau-Server starten** über das Browser-Werkzeug (`preview_start` mit `stefflix`, bei belegtem Port `stefflix-2`), Seite `http://localhost:8765/.claude/testenv/start.html`. Nie die Testumgebung per `file://` öffnen. Geänderte Dateien bei Bedarf mit `fetch(…, {cache: "reload"})` bzw. Neuladen holen.
3. **Konsole** auf Fehler prüfen (`read_console_messages`), Inhalt über `read_page`/`get_page_text` statt Screenshots.
4. **Gezielt** den Fokus prüfen, dann die **Kurz-Regression**: Startseite lädt (Ladescreen verschwindet), Ordnerseite und Zurück (Scrollposition), Suche, Favoriten, Player mit Video (Start, Weiter/Zurück, Autoplay-Menü), Musik mit Playliste, Bildbetrachter, Einstellungen (ein Schalter speichert), Hilfe mit Changelog. Bei Touch-Themen mit `resize_window` (mobile) prüfen; Fenstergrösse löst kein `resize` aus.
5. **Brücke zu STEFFLIX.exe** (falls betroffen): `window.chrome.webview` mit `postMessage`/`addEventListener` nachbilden, `initDesktopApp()` aufrufen, Nachrichten prüfen.
6. Grenzen nennen: kein Springen in Videos (Server ohne Range), keine App, kein Fenster der EXE. Was davon nötig ist, an die Prüfung (Emulator, Test-Build) bzw. als manuellen Test an den Benutzer.
7. Ergebnis: geprüfte Punkte mit Ergebnis, Fehler mit Reproduktion, Screenshot nur als Beleg für sichtbare Änderungen; danach `resize_window` zurück auf `desktop`.
