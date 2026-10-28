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

Neue Filme, Serien, Musik oder Bilder kommen am einfachsten mit dem Importer von `updateStefflix.exe` in STEFFLIX. Er kopiert oder verschiebt die Medien in den richtigen Ordner, wandelt nicht unterstützte Formate auf Wunsch um und liest danach nur den Zielordner neu ein. Das geht auch bei grossen Mediatheken schnell, der Rest bleibt unverändert.

1. `updateStefflix.exe` starten und die Registerkarte "Importer" öffnen. Dateien oder Ordner lassen sich auch einfach ins Fenster ziehen.
2. Unter "Was importieren?" mit "Dateien …" oder "Ordner …" die Medien wählen.
3. Unter "Ziel in STEFFLIX" die Kategorie wählen (z.B. Serien, mit "Neu …" auch eine eigene) und den Unterordner angeben, z.B. `Meine Serie\Staffel 1`.
4. Bei Bedarf die Optionen einschalten: Ordnerstruktur übernehmen, in ein browserkompatibles Format konvertieren, verschieben statt kopieren oder fehlende Vorschaubilder erstellen.
5. Auf "Start" klicken. Danach STEFFLIX neu laden, die Medien sind da.

### Alternative: Medien von Hand kopieren

Die Medien lassen sich auch selbst in die Ordner kopieren. Danach müssen sie eingelesen werden, damit sie in STEFFLIX erscheinen.

1. Medien in den passenden Unterordner kopieren, z.B. `Serien\Meine Serie\Staffel 1`.
2. Optional ein Vorschaubild hinterlegen (siehe unten). Für Videos kann der Updater unter "Tools" mit "Vorschaubilder" auch selbst eines erzeugen.
3. `updateStefflix.exe` starten und unter "Updates" die Aufgabe "Daten einlesen" ausführen. Ist "Alle Ordner einlesen" ausgeschaltet, werden nur die gewählten Ordner eingelesen. Das ist schneller, wenn nur an wenigen Stellen etwas dazugekommen ist.

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

`updateStefflix.exe` im STEFFLIX-Ordner starten, die Aufgaben "Update" und "Daten einlesen" auswählen und ausführen. Der Updater lädt die neue Version herunter und liest danach die Medien neu ein. Er hält sich auch selbst aktuell und meldet beim Start, wenn eine neue Version bereitsteht. Die Hilfe in STEFFLIX zeigt die installierten und neuesten Versionen von STEFFLIX, Updater und ffmpeg, und auf der Startseite erscheint ein Hinweis, wenn ein Update bereitsteht (abschaltbar unter Einstellungen › Startseite).

### Aufgaben des Updaters

Die ausgewählten Aufgaben laufen immer in dieser Reihenfolge ab:

- **Update**: STEFFLIX-Dateien und den Updater selbst von GitHub aktualisieren. Installiert wird nur, wenn alle Dateien vollständig und gültig heruntergeladen wurden.
- **Ablagen leeren**: Die Ablageordner der Werkzeuge (Ordner, deren Name mit `__` beginnt, z.B. `__teile`, `__konvertiert`, `__gelöscht` und `__original`) endgültig löschen. Vorher wird angezeigt, was gelöscht wird. Nur innerhalb des STEFFLIX-Ordners möglich.
- **Folgen umbenennen**: Die Folgen einer Serie anhand der Folgenliste von fernsehserien.de (sonst TVmaze) umbenennen, z.B. `Serie - S01E02 - Titel.mp4` im Ordner `Staffel 01`. Erkannt werden Namen mit `S01E02` oder `1x02`. Doppelfolgen behalten alle Nummern, z.B. `Serie - S01E02E03 - Titel.mp4`. Vorher werden alle Änderungen angezeigt, danach lässt sich der letzte Lauf rückgängig machen. Geladene Folgenlisten werden gespeichert, beim nächsten Lauf lässt sich die gespeicherte Liste verwenden oder neu laden.
- **Gesplittete Medien zusammenführen**: Videos wie "Titel pt1" und "Titel pt2" zu einer Datei zusammenfügen. Die Teile werden vorher in einer Liste ausgewählt, auf Wunsch wird neu kodiert. Die Teile können danach nach `__teile` verschoben werden.
- **Videos für SFLIX konvertieren**: Videos in browserkompatible MP4 (H.264/AAC) umwandeln. Die MP4 ersetzt das Original im selben Ordner, das Original kommt nach `__konvertiert` (oder wird auf Wunsch gelöscht). Die Grafikkarte wird automatisch genutzt.
- **Vorschaubilder erstellen**: Für jedes Video ohne Vorschaubild ein Bild aus der Mitte des Videos (480x270) im Preview-Ordner erstellen. Vorschaubilder ohne zugehöriges Video kommen nach `Preview\__gelöscht`.
- **Daten einlesen**: Medien neu einlesen, ungültige Zeichen in Datei- und Ordnernamen korrigieren und zu grosse Vorschaubilder verkleinern. Ordner, deren Name mit `__` beginnt, sowie System- und versteckte Ordner werden ignoriert. Mit dem Schalter "Alle Ordner einlesen" wird alles eingelesen. Ist er aus, lassen sich einzelne Ordner wählen, und nur diese werden aktualisiert. Das geht deutlich schneller bei grossen Mediatheken. Nach einem Import liest der Updater automatisch nur den Zielordner ein.

Zusammenführen, Konvertieren, Vorschaubilder und Ablagen leeren durchsuchen mit "Alles" den ganzen STEFFLIX-Ordner, sonst nur die gewählten Ordner. Danach werden nur die betroffenen Ordner neu eingelesen. Das dafür nötige Programm ffmpeg lädt der Updater beim ersten Bedarf selbst herunter.

## Autoplay

Mit Autoplay spielt STEFFLIX nach dem aktuellen Medium automatisch das nächste ab. Die Einstellungen befinden sich hinter dem Autoplay-Symbol in der Player-Leiste, bei Bildern hinter dem Knopf "Autoplay" unter dem Bild. Das Symbol zeigt, ob Autoplay aktiv ist (ausgegraut = aus), und daneben, wie viele Medien noch folgen (bei "Unbegrenzt" ∞). Kurz vor dem Wechsel erscheint im Player ein Countdown; ein Klick darauf startet das nächste Medium sofort.

- **Automatisch vorwärts**: schaltet Autoplay ein oder aus.
- **Unbegrenzt**: spielt ohne feste Anzahl weiter, bis das Ende der Playliste erreicht ist.
- **Anzahl abzuspielender Medien**: wie viele Medien nacheinander abgespielt werden. Die Zahl wird mit jedem Medium heruntergezählt, bei 0 hält Autoplay an. Bei "Unbegrenzt" wird sie nicht verwendet.
- **Dauer bis Übergang (Sekunden)**: Bei Videos und Musik wechselt STEFFLIX so viele Sekunden vor dem Ende (plus etwa eine Sekunde) zum nächsten Medium, bei Musik höchstens 11 Sekunden vorher. Bilder bleiben so viele Sekunden plus 11 Sekunden sichtbar.
- **Gilt für**: Die Dauer wird pro Ordner gespeichert und gilt auch für alle Unterordner ohne eigenen Wert, z.B. für eine ganze Serie und abweichend für eine Staffel. Ohne Eintrag gilt 0. Alle Einträge stehen unter Einstellungen › Wiedergabe.
- **Übergang hier** (Stoppuhr in der Player-Leiste): übernimmt die verbleibende Zeit des Mediums als Dauer bis Übergang, z.B. beim Beginn des Abspanns.

## Player

Videos und Musik haben eine eigene Steuerleiste. Bei Videos blendet sie sich während der Wiedergabe nach kurzer Zeit aus und erscheint wieder, sobald die Maus bewegt oder das Video angetippt wird. Bei Musik bleibt sie sichtbar; darüber wird das Vorschaubild des Titels angezeigt (sonst das des Albums bzw. Ordners).

- **Leiste**: vorheriges / nächstes Medium, Abspielen / Pause, 10 Sekunden zurück / vor, Lautstärke, Zeitleiste, Übergang hier, Autoplay-Menü, Zahnradmenü, Bild im Bild und Vollbild.
- **Musik**: Neben dem Player steht die Playliste mit allen Titeln und ihrer Dauer, der laufende Titel ist hervorgehoben. Ein Klick spielt einen Titel ab. Auf dem Handy steht die Liste unter dem Player.
- **Maus**: Klick auf das Video startet oder pausiert, Doppelklick schaltet das Vollbild ein oder aus.
- **Touch**: Tippen blendet die Leiste ein oder aus. Doppeltipp links oder rechts spult 10 Sekunden zurück bzw. vor, Doppeltipp in der Mitte schaltet das Vollbild um.
- **Zahnradmenü**: Geschwindigkeit (0,5x bis 1,75x), Herunterladen und Browser-Steuerung.
- **Vollbild**: Das Video füllt den Bildschirm. Darunter kann weiterhin gescrollt und ein anderes Medium gewählt werden.
- **Browser-Steuerung**: Wer die Steuerung des Browsers bevorzugt, schaltet sie im Zahnradmenü oder im Menü unter "Einstellungen" ein.

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
## Einstellungen

Im Menü unter "Einstellungen" lassen sich festlegen: Kachelgrösse (Klein, Mittel, Gross), Kacheln pro Zeile (fest oder automatisch nach Fensterbreite), Markierungen auf den Kacheln ("Gesehen", "Letzte Folge"), Animationen reduzieren, Startbildschirm überspringen, alphabetische Gruppen auf Ordnerseiten (A–D, E–H … mit Sprungleiste, Bilder und Musik ausnehmbar), Artikel beim Sortieren ignorieren ("Der Pate" unter P), Ordner getrennt von Medien, die Anzahl Einträge bei "Weiterschauen", Update-Hinweise, die Browser-Steuerung und die Dauer bis Übergang je Ordner. Änderungen gelten sofort und nur im verwendeten Browser.

Den Lizenzschlüssel für das Feedbackformular unter Einstellungen › Lizenz eingeben. Mit gültigem Schlüssel erscheint im Menü der Eintrag "Feedback".

## Datensicherung

Favoriten, Fortschritt, Gesehen-Markierungen, "Weiterschauen", Einstellungen, die Dauer bis Übergang je Ordner und die Sortierung der Startseite werden nur im verwendeten Browser gespeichert. Unter Einstellungen › Datensicherung lassen sie sich als Datei exportieren und wieder importieren, z.B. um sie auf einen anderen Browser oder PC zu übertragen.

Gibt es mehrere STEFFLIX-Ordner auf einem PC, speichert jeder seine Daten getrennt. Der Updater legt dafür in jedem Ordner eine eigene Kennung an (`sflix_sys\install.js`). Diese Datei nicht in einen anderen STEFFLIX-Ordner kopieren. Wer einen ganzen Ordner kopiert, startet danach einmal den Updater in der Kopie, dann bekommt sie automatisch eine neue Kennung.

## Fehlerbehebung

Lädt STEFFLIX nicht richtig, stimmen die Vor- und Zurück-Buttons nicht oder setzen Videos an der falschen Stelle fort, hilft Einstellungen › Fehlerbehebung: "Gespeicherte Daten prüfen" zeigt verwaiste Einträge (z.B. nach dem Umbenennen im Explorer) und entfernt nur diese. Eine Diagnose mit Versionen und Browser lässt sich kopieren. Unter den weiteren Optionen lassen sich Favoriten, Playlisten, Zeitstempel, "Weiterschauen" oder alles zurücksetzen. Die Medien selbst werden dabei nicht verändert. Vor "Alle Daten löschen" wird eine Sicherung angeboten.

Fehlen Programmdateien, z.B. nach einem abgebrochenen Update, `updateStefflix.exe` erneut mit der Aufgabe "Update" ausführen.

## Neuerungen

Die Änderungen jeder Version stehen in [sflix_sys/news.txt](sflix_sys/news.txt) und in STEFFLIX unter Hilfe › Changelog.
