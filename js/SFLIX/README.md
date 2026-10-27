# STEFFLIX

STEFFLIX ist eine lokale Mediathek für Filme, Serien, Musik und Bilder im Stil eines Streamingdienstes. Sie läuft vollständig im Browser: `start.html` per Doppelklick öffnen, fertig. Es wird kein Server und keine Internetverbindung benötigt (ausser für Updates).

- Startseite mit "Weiterschauen" und Menübändern pro Kategorie
- Eigener Player für Videos und Musik, Bildbetrachter mit Vollbild und Diashow
- Autoplay, Zufallswiedergabe, Favoriten und Suche
- Markierung gesehener Videos, Fortsetzen an der letzten Stelle
- Vollständig per Tastatur bedienbar, optimiert für Chrome, Edge und Firefox

## Installation

**[STEFFLIX Setup herunterladen (updateStefflix.exe)](https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updateStefflix.exe)**

Alle Dateien liegen auch im [STEFFLIX-Ordner auf GitHub](https://github.com/Apop85/Scripts/tree/master/js/SFLIX).

### Voraussetzungen

- Windows 10 oder 11 (für den Updater; STEFFLIX selbst läuft in jedem aktuellen Browser)
- Google Chrome, Microsoft Edge oder Mozilla Firefox
- Internetverbindung für die Installation und für Updates

### Schritt für Schritt

1. **Setup herunterladen**: Über den Link oben `updateStefflix.exe` herunterladen, z.B. in den Ordner "Downloads".
2. **Setup starten**: `updateStefflix.exe` per Doppelklick starten. Zeigt Windows den Hinweis "Der Computer wurde durch Windows geschützt", auf "Weitere Informationen" und danach auf "Trotzdem ausführen" klicken.
3. **Ordner wählen**: Das Setup erkennt, dass es nicht in einem STEFFLIX-Ordner liegt, und öffnet das Fenster "STEFFLIX Setup". Bei "Installationsordner" über "Durchsuchen …" einen Ordner wählen, z.B. `D:\STEFFLIX` oder einen Ordner auf einer externen Festplatte. Ein noch nicht vorhandener Ordner wird auf Nachfrage erstellt.
4. **Installieren**: Auf "Installieren" klicken. Das Setup lädt alle STEFFLIX-Dateien herunter und kopiert sich selbst in den neuen Ordner. Die heruntergeladene Datei im Ordner "Downloads" wird danach nicht mehr gebraucht und kann gelöscht werden.
5. **Medien hinzufügen**: Im STEFFLIX-Ordner Kategorieordner anlegen, z.B. `Filme`, `Serien`, `Musik` und `Bilder`, und die Medien hineinkopieren (siehe [Neue Medien hinzufügen](#neue-medien-hinzufügen)).
6. **Daten einlesen**: Im Setup-Fenster auf "Updater starten" klicken (oder später `updateStefflix.exe` im STEFFLIX-Ordner starten), "Daten einlesen" auswählen und ausführen.
7. **STEFFLIX öffnen**: Im Setup-Fenster auf "STEFFLIX öffnen" klicken oder `start.html` im STEFFLIX-Ordner per Doppelklick öffnen. Tipp: Ein Lesezeichen setzen oder eine Verknüpfung auf den Desktop legen.
8. **Browser einrichten**: Damit Autoplay mit Ton funktioniert, die automatische Wiedergabe erlauben (siehe [Browsereinstellungen](#browsereinstellungen)).

Ein STEFFLIX-Ordner mit Medien sieht etwa so aus:

```
D:\STEFFLIX
├── Bilder             (Kategorieordner, selbst angelegt)
├── Filme
├── Musik
├── Serien
├── sflix_sys          (Programmdateien, nicht verändern)
├── start.html         (STEFFLIX öffnen)
└── updateStefflix.exe (Updater)
```

## Neue Medien hinzufügen

Neue Filme, Serien, Musik oder Bilder werden in den passenden Ordner im STEFFLIX-Verzeichnis kopiert. Danach liest `updateStefflix.exe` die Medien mit der Aufgabe "Daten einlesen" neu ein, und sie erscheinen in STEFFLIX.

1. Medien in den passenden Unterordner kopieren, z.B. `Serien\Meine Serie\Staffel 1`.
2. Optional ein Vorschaubild hinterlegen (siehe unten). Für Videos kann der Updater mit "Vorschaubilder erstellen" auch selbst eines erzeugen.
3. `updateStefflix.exe` starten, "Daten einlesen" auswählen und ausführen.

### Unterstützte Dateiformate

| Typ    | Endungen                                  |
| ------ | ----------------------------------------- |
| Video  | mp4, m4v, webm, ogv                       |
| Musik  | mp3, m4a, ogg, oga, opus, aac, flac, wav  |
| Bilder | jpg, jpeg, png, gif, webp, avif, bmp      |

Gross- und Kleinschreibung der Dateiendung spielt keine Rolle (JPG = jpg). Videos in anderen Formaten (z.B. mkv oder avi) wandelt der Updater mit "Videos für SFLIX konvertieren" in passende MP4-Dateien um.

### Vorschaubilder (Preview-Ordner)

Vorschaubilder liegen in einem Ordner namens `Preview`, und zwar im selben Verzeichnis wie das Medium oder der Ordner, zu dem sie gehören. Das Bild heisst gleich wie das Medium bzw. der Ordner (Endung jpg, png oder webp). Ohne Vorschaubild zeigt STEFFLIX eine farbige Kachel.

```
D:\sflix\Bilder
├── Buddy
├── Natur
├── Pilze
└── Preview
    ├── Buddy.jpg
    ├── Natur.jpg
    └── Pilze.jpg
```

Beim Einlesen der Daten schneidet `updateStefflix.exe` zu grosse Bilder auf 480x270 zu, damit sie schneller laden (webp-Bilder bleiben unverändert). Das Original wird mit gleichem Namen im Unterordner `Preview\__original` aufbewahrt.

## STEFFLIX aktualisieren

`updateStefflix.exe` im STEFFLIX-Ordner starten, die Aufgaben "Update" und "Daten einlesen" auswählen und ausführen. Der Updater lädt die neue Version herunter und liest danach die Medien neu ein. Er hält sich auch selbst aktuell und meldet beim Start, wenn eine neue Version bereitsteht.

### Aufgaben des Updaters

Die ausgewählten Aufgaben laufen immer in dieser Reihenfolge ab:

- **Update**: STEFFLIX-Dateien und den Updater selbst von GitHub aktualisieren. Installiert wird nur, wenn alle Dateien vollständig und gültig heruntergeladen wurden.
- **Ablagen leeren**: Die Ablageordner der Werkzeuge (Ordner, deren Name mit `__` beginnt, z.B. `__teile`, `__konvertiert`, `__gelöscht` und `__original`) endgültig löschen. Vorher wird angezeigt, was gelöscht wird.
- **Gesplittete Medien zusammenführen**: Videos wie "Titel pt1" und "Titel pt2" zu einer Datei zusammenfügen. Die Teile werden vorher in einer Liste ausgewählt, auf Wunsch wird neu kodiert. Die Teile können danach nach `__teile` verschoben werden.
- **Videos für SFLIX konvertieren**: Videos in browserkompatible MP4 (H.264/AAC) umwandeln. Die MP4 ersetzt das Original im selben Ordner, das Original kommt nach `__konvertiert` (oder wird auf Wunsch gelöscht). Die Grafikkarte wird automatisch genutzt.
- **Vorschaubilder erstellen**: Für jedes Video ohne Vorschaubild ein Bild aus der Mitte des Videos (480x270) im Preview-Ordner erstellen. Vorschaubilder ohne zugehöriges Video kommen nach `Preview\__gelöscht`.
- **Daten einlesen**: Medien neu einlesen, ungültige Zeichen in Datei- und Ordnernamen korrigieren und zu grosse Vorschaubilder verkleinern. Ordner, deren Name mit `__` beginnt, werden ignoriert. Mit dem Schalter "Alle Ordner einlesen" wird alles eingelesen. Ist er aus, lassen sich einzelne Ordner wählen, und nur diese werden aktualisiert. Das geht deutlich schneller bei grossen Mediatheken. Nach einem Import liest der Updater automatisch nur den Zielordner ein.

Zusammenführen, Konvertieren, Vorschaubilder und Ablagen leeren arbeiten im eingestellten Medienordner (Standard: der STEFFLIX-Ordner). Das dafür nötige Programm ffmpeg lädt der Updater beim ersten Bedarf selbst herunter.

## Autoplay

Mit Autoplay spielt STEFFLIX nach dem aktuellen Medium automatisch das nächste ab. Die Einstellungen befinden sich im Menü unter "Autoplay" und hinter dem Autoplay-Symbol in der Player-Leiste. Das Symbol zeigt, ob Autoplay aktiv ist (ausgegraut = aus). Kurz vor dem Wechsel erscheint im Player ein Countdown; ein Klick darauf startet das nächste Medium sofort.

- **Automatisch vorwärts**: schaltet Autoplay ein oder aus.
- **Unbegrenzt**: spielt ohne feste Anzahl weiter, bis das Ende der Playliste erreicht ist.
- **Anzahl abzuspielender Medien**: wie viele Medien nacheinander abgespielt werden. Die Zahl wird mit jedem Medium heruntergezählt, bei 0 hält Autoplay an. Bei "Unbegrenzt" wird sie nicht verwendet.
- **Dauer bis Übergang (Sekunden)**: Bei Videos und Musik wechselt STEFFLIX so viele Sekunden vor dem Ende (plus etwa eine Sekunde) zum nächsten Medium, bei Musik höchstens 11 Sekunden vorher. Bilder bleiben so viele Sekunden plus 11 Sekunden sichtbar.

## Player

Videos und Musik haben eine eigene Steuerleiste. Bei Videos blendet sie sich während der Wiedergabe nach kurzer Zeit aus und erscheint wieder, sobald die Maus bewegt oder das Video angetippt wird. Bei Musik bleibt sie sichtbar; darüber wird das Vorschaubild des Titels angezeigt (sonst das des Albums bzw. Ordners).

- **Leiste**: vorheriges / nächstes Medium, Abspielen / Pause, 10 Sekunden zurück / vor, Lautstärke, Zeitleiste, Autoplay-Menü, Zahnradmenü, Bild im Bild und Vollbild.
- **Maus**: Klick auf das Video startet oder pausiert, Doppelklick schaltet das Vollbild ein oder aus.
- **Touch**: Tippen blendet die Leiste ein oder aus. Doppeltipp links oder rechts spult 10 Sekunden zurück bzw. vor, Doppeltipp in der Mitte schaltet das Vollbild um.
- **Zahnradmenü**: Geschwindigkeit (0,5x bis 1,75x), Herunterladen und Browser-Steuerung.
- **Vollbild**: Das Video füllt den Bildschirm. Darunter kann weiterhin gescrollt und ein anderes Medium gewählt werden.
- **Browser-Steuerung**: Wer die Steuerung des Browsers bevorzugt, schaltet sie im Zahnradmenü oder im Menü unter "Autoplay" ein.

## Tastenkürzel

Die Tastenkürzel funktionieren auf Medienseiten, solange nicht in ein Eingabefeld geschrieben wird.

| Taste                 | Funktion                                                      |
| --------------------- | ------------------------------------------------------------- |
| Leertaste / K         | Abspielen / Pause                                             |
| Pfeil links / rechts  | 10 Sekunden zurück / vor (bei Bildern: vorheriges / nächstes Bild) |
| Pfeil hoch / runter   | Lautstärke (nur wenn der Player sichtbar ist)                 |
| F                     | Vollbild (bei Videos auch per Doppelklick)                    |
| M                     | Ton aus / an                                                  |
| N / P                 | Nächstes / vorheriges Medium                                  |
| /                     | Suche öffnen                                                  |
| Esc                   | Fenster schliessen / Vollbild beenden                         |
| Tab / Enter           | Mit der Tastatur durch Menüs und Buttons springen, mit Enter auswählen |

## Browsereinstellungen

Damit Videos und Musik von selbst starten (z.B. bei Autoplay), muss der Browser die automatische Wiedergabe mit Ton für STEFFLIX erlauben.

**Google Chrome**
1. Die Toneinstellungen unter `chrome://settings/content/sound` öffnen.
2. Bei den Websites, die Ton wiedergeben dürfen, auf "Hinzufügen" klicken.
3. Folgenden Eintrag hinzufügen: `file:///*`

**Microsoft Edge**
1. Die Einstellungen unter `edge://settings/content/mediaAutoplay` öffnen.
2. Neben "Zulassen" auf "Hinzufügen" klicken.
3. Folgenden Eintrag hinzufügen: `file:///*`

**Mozilla Firefox**
1. In STEFFLIX mit `Ctrl + I` die Seiteninformationen öffnen.
2. Den Tab "Berechtigungen" öffnen.
3. Bei "Automatische Wiedergabe" die Option "Audio und Video erlauben" wählen.

### Daten beim Schliessen des Browsers behalten

Ist im Browser eingestellt, dass er beim Schliessen die Chronik oder die Website-Daten löscht, gehen auch Favoriten, "Weiterschauen", Gesehen-Markierungen und die Sortierung der Startseite verloren. Dann STEFFLIX als Ausnahme eintragen. Die Einstellungsseiten lassen sich nicht über einen Link öffnen: Adresse in die Adresszeile kopieren und Enter drücken. In der Hilfe von STEFFLIX kopiert ein Klick auf die Adresse sie.

**Mozilla Firefox**
1. `about:preferences#history` öffnen.
2. Ist "Die Chronik löschen, wenn Firefox geschlossen wird" eingeschaltet, auf "Ausnahmen verwalten" klicken.
3. `file:///` eingeben, auf "Erlauben" und danach auf "Änderungen speichern" klicken.
4. Ist unter `about:preferences#privacy` bei "Cookies und Website-Daten" das Löschen beim Beenden eingeschaltet, dort unter "Ausnahmen verwalten" ebenfalls `file:///` erlauben.

**Google Chrome**
1. `chrome://settings/content/siteData` öffnen.
2. Ist das Löschen der Websitedaten beim Schliessen aller Fenster eingeschaltet, bei den Websites, die immer Daten speichern dürfen, auf "Hinzufügen" klicken.
3. Folgenden Eintrag hinzufügen: `file:///*`
**Microsoft Edge**
1. `edge://settings/clearBrowsingDataOnClose` öffnen.
2. Ist "Cookies und andere Websitedaten" eingeschaltet, bei "Nicht löschen" auf "Hinzufügen" klicken.
3. Folgenden Eintrag hinzufügen: `file:///*`
## Datensicherung

Favoriten, Fortschritt, Gesehen-Markierungen, "Weiterschauen", Einstellungen und die Sortierung der Startseite werden nur im verwendeten Browser gespeichert. In der Hilfe von STEFFLIX lassen sie sich unter "Datensicherung" als Datei exportieren und wieder importieren, z.B. um sie auf einen anderen Browser oder PC zu übertragen.

Gibt es mehrere STEFFLIX-Ordner auf einem PC, speichert jeder seine Daten getrennt. Der Updater legt dafür in jedem Ordner eine eigene Kennung an (`sflix_sys\install.js`). Diese Datei nicht in einen anderen STEFFLIX-Ordner kopieren. Wer einen ganzen Ordner kopiert, startet danach einmal den Updater in der Kopie, dann bekommt sie automatisch eine neue Kennung.

## Fehlerbehebung

Lädt STEFFLIX nicht richtig, stimmen die Vor- und Zurück-Buttons nicht oder setzen Videos an der falschen Stelle fort, können in der Hilfe von STEFFLIX unter "Fehlerbehebung" die gespeicherten Daten zurückgesetzt werden (Favoriten, Playlisten, Zeitstempel, "Weiterschauen" oder alles). Die Medien selbst werden dabei nicht verändert. Vorher am besten eine Sicherung erstellen.

Fehlen Programmdateien, z.B. nach einem abgebrochenen Update, `updateStefflix.exe` erneut mit der Aufgabe "Update" ausführen.

## Neuerungen

Die Änderungen jeder Version stehen in [sflix_sys/news.txt](sflix_sys/news.txt) und in STEFFLIX unter Hilfe › Changelog.
