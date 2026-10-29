// Texte des Reiters "Hilfe" (seit 3.3). Bei Änderungen am Updater hier nachführen.
// Format je Zeile: "# " Zwischenüberschrift, "- " Aufzählung, "> " Befehl/Beispiel (Festbreitenschrift),
// sonst normaler Absatz. Leere Zeile = Abstand.

using System.Collections.Generic;

namespace Sflix
{
    public class HelpTopic
    {
        public string Title;
        public string Text;
        public HelpTopic(string title, string text) { Title = title; Text = text; }
    }

    public static class HelpContent
    {
        public static List<HelpTopic> Topics()
        {
            List<HelpTopic> t = new List<HelpTopic>();

            t.Add(new HelpTopic("Überblick",
                "Der STEFFLIX Updater hält STEFFLIX aktuell, liest die Medien ein und bringt Werkzeuge rund um die Mediathek mit. " +
                "Er liegt im STEFFLIX-Ordner (updateStefflix.exe) und arbeitet nur in diesem Ordner.\n\n" +
                "# Registerkarten\n" +
                "- Updates: STEFFLIX und den Updater aktualisieren, Medien einlesen, Versionen.\n" +
                "- Importer: Videos, Musik und Bilder in die Mediathek übernehmen.\n" +
                "- Tools: Vorschaubilder, Folgen umbenennen, Duplikate finden, Zusammenführen, Konvertieren, Medien prüfen, Ablagen leeren.\n" +
                "- Lizenz bzw. Feedback: Lizenzschlüssel eingeben, danach Nachrichten an den Entwickler schicken.\n" +
                "- Hilfe: diese Seite (auch mit F1).\n\n" +
                "# Kacheln und Start\n" +
                "Auf Updates und Tools wählst du mit den Kacheln, was ausgeführt werden soll. Darunter erscheint je gewählter Kachel ein Reiter mit ihren Optionen. " +
                "„Start“ führt nur aus, was auf der sichtbaren Karte gewählt ist. Nach Importer und Tools werden die betroffenen Ordner automatisch neu eingelesen.\n\n" +
                "# Protokoll\n" +
                "„Details“ blendet rechts das Protokoll ein. Es zeigt jede Aufgabe mit Uhrzeit, Fehler rot, Hinweise gelb. " +
                "Bei einem Fehler öffnet es sich von selbst. Mit der Suche und „Nur Fehler und Warnungen“ findest du schnell die wichtigen Zeilen.\n" +
                "- In Datei speichern: schreibt das Protokoll nach sflix_sys\\updater.log, auch alles seit dem Start. Der Schalter ist bei jedem Start aus.\n" +
                "- Speichern unter …: speichert das Protokoll dieser Sitzung in eine selbst gewählte Datei (wenn die Datei aus ist).\n" +
                "Die Datei ist begrenzt: Wird sie grösser als 1 MB, bleiben die letzten 256 KB."));

            t.Add(new HelpTopic("Updates und Versionen",
                "Beim Start prüft der Updater im Hintergrund, ob es Neues gibt: STEFFLIX-Dateien, den Updater selbst und ein installiertes ffmpeg. " +
                "Falls ja, erscheint ein Fenster mit „Jetzt aktualisieren“ oder „Später“. Nach „Später“ bleibt ein gelber Balken stehen.\n\n" +
                "# Update\n" +
                "Lädt geänderte Dateien von GitHub, prüft jede über ihre Prüfsumme und ersetzt sie erst danach. Schlägt etwas fehl, bleibt der alte Stand. " +
                "Ist ein neuer Updater verfügbar, ersetzt er sich selbst, startet neu und macht mit den restlichen Aufgaben weiter.\n" +
                "- ffmpeg ebenfalls aktualisieren: bringt ein installiertes ffmpeg auf die vorgegebene Version.\n\n" +
                "# Versionen\n" +
                "Rechts stehen die installierten und verfügbaren Versionen von STEFFLIX, Updater und ffmpeg. „Patchnotes“ zeigt die Neuerungen, " +
                "„Neu prüfen“ fragt erneut nach. STEFFLIX-Versionen haben seit 2.5.0 das Format x.y.z, ältere werden umgerechnet (2.04 = 2.4.0)."));

            t.Add(new HelpTopic("Daten einlesen",
                "Durchsucht die Ordner von STEFFLIX und schreibt das Medienverzeichnis sflix_sys\\data.js, das STEFFLIX anzeigt. " +
                "Dabei werden ungültige Zeichen in Namen korrigiert und zu grosse Vorschaubilder verkleinert (Original in Preview\\__original).\n" +
                "- Alle Ordner einlesen: ganze Mediathek.\n" +
                "- Sonst nur die gewählten Ordner, der Rest von data.js bleibt unverändert. Das ist bei grossen Mediatheken deutlich schneller.\n" +
                "Ablagen (Ordner mit „__“), System- und versteckte Ordner werden übersprungen. Umbenannte Dateien werden in sflix_sys\\renamed.js vermerkt, " +
                "damit Favoriten, Fortschritt und „Weiterschauen“ in STEFFLIX erhalten bleiben."));

            t.Add(new HelpTopic("Importer",
                "Übernimmt Videos, Musik und Bilder in die Mediathek. Dateien und Ordner wählst du mit „Dateien …“ und „Ordner …“ oder ziehst sie irgendwo ins Fenster.\n" +
                "- Kategorie und Unterordner: Ziel innerhalb von STEFFLIX, z.B. Serien \\ Meine Serie \\ Staffel 01.\n" +
                "- Ordnerstruktur übernehmen: Unterordner der Quelle bleiben erhalten.\n" +
                "- Konvertieren: Video MP4, Musik MP3, Bild JPG. Dateien, die der Browser nicht abspielen kann, werden immer konvertiert.\n" +
                "- Verschieben statt kopieren: Originale konvertierter Dateien kommen nach „__konvertiert“.\n" +
                "- Danach fehlende Vorschaubilder erstellen.\n\n" +
                "# Datei vorhanden\n" +
                "Gibt es eine Datei im Ziel schon, zeigt ein Fenster beide Dateien mit Grösse, Auflösung, Dauer, Video/Ton und Datum. " +
                "Die besseren Werte sind hervorgehoben. Du wählst Ersetzen oder Überspringen, auf Wunsch für alle weiteren Fälle."));

            t.Add(new HelpTopic("Vorschaubilder",
                "Erstellt fehlende Vorschaubilder (480×270) im Ordner Preview neben den Medien: für Videos ein Bild aus der Mitte, für Bilder eine verkleinerte Kopie. " +
                "Vorschaubilder ohne zugehöriges Medium kommen nach Preview\\__gelöscht.\n" +
                "- Ordner: „Alles“ oder eine Liste von Ordnern (auch ausserhalb von STEFFLIX)."));

            t.Add(new HelpTopic("Folgen umbenennen",
                "Holt die Folgenliste einer Serie von fernsehserien.de (deutsche Titel, sonst TVmaze) und benennt die Dateien einheitlich um, " +
                "z.B. „Serie - S01E02 - Titel.mp4“ im Ordner „Staffel 01“. Erkannt werden S01E02, 1x02 und Doppelfolgen (S01E02E03).\n" +
                "- Vorher zeigt eine Vorschau alle Änderungen, fehlende Folgen und nicht zugeordnete Dateien. Nur angehakte Zeilen werden ausgeführt.\n" +
                "- Gibt es mehrere Dateien für eine Folge oder das Ziel schon, zeigt die Vorschau beim Anklicken einen Vergleich mit Grösse, Auflösung und Dauer.\n" +
                "- Vorschaubilder ziehen mit, Favoriten und Fortschritt bleiben erhalten.\n" +
                "- „Letztes Umbenennen rückgängig“ stellt den vorherigen Zustand wieder her.\n" +
                "Geladene Folgenlisten werden gespeichert (sflix_sys\\folgen_cache.json) und beim nächsten Mal angeboten."));

            t.Add(new HelpTopic("Duplikate finden",
                "Sucht doppelte Medien in den gewählten Ordnern:\n" +
                "- Identische Dateien: gleiche Grösse und gleicher Inhalt (Prüfsumme), auch mit anderem Namen.\n" +
                "- Gleiche Folge: dieselbe Staffel und Folge innerhalb einer Serie, egal ob S01E01, 1x01, S1E1 oder S01E1. Meist verschiedene Fassungen, z.B. SD und HD.\n" +
                "- Gleicher Name: gleicher Dateiname ohne Endung, z.B. in zwei Ordnern oder „Film.mp4“ neben „Film.mkv“. Preview- und Ablage-Ordner zählen nicht.\n" +
                "Ein Fenster zeigt jede Gruppe mit Grösse, Auflösung, Dauer, Video/Ton und Datum. Vorausgewählt bleibt die Datei mit der höheren Auflösung, " +
                "bei gleicher Auflösung die grössere (bei identischen Dateien die mit dem kürzesten Pfad). Überschneiden sich Doppelfolgen, ist nichts vorausgewählt. " +
                "Die übrigen Dateien kommen samt Vorschaubild nach „__duplikate“; „Ablagen leeren“ löscht sie später endgültig."));

            t.Add(new HelpTopic("Zusammenführen",
                "Fügt gesplittete Videos wie „Titel pt1“, „Titel pt2“ zu einer Datei zusammen. Vorher wählst du aus, welche.\n" +
                "- Teile danach nach „__teile“ verschieben."));

            t.Add(new HelpTopic("Konvertieren",
                "Wandelt Videos in browserkompatible MP4-Dateien (H.264/AAC) um, bei mehreren Tonspuren je eine Datei. " +
                "Der Updater nutzt die Grafikkarte (NVIDIA, Intel, AMD), sonst den Prozessor. Originale kommen nach „__konvertiert“.\n" +
                "- Gleichzeitig: wie viele Videos parallel umgewandelt werden (0 = automatisch).\n" +
                "- Originale löschen statt verschieben."));

            t.Add(new HelpTopic("Medien prüfen",
                "Prüft die Medien in den gewählten Ordnern und listet Probleme auf:\n" +
                "- Nicht abspielbar: Format oder Codec, den der Browser nicht abspielen kann (z.B. MKV, HEVC, AC3/DTS-Ton).\n" +
                "- Defekt: ffprobe kann die Datei nicht lesen.\n" +
                "- Leer: Datei mit 0 Byte.\n" +
                "- Ohne Vorschaubild.\n" +
                "Im Ergebnis hakst du Einträge an und wählst „Ausgewählte konvertieren“ oder „Vorschaubilder erstellen“. Danach wird neu eingelesen."));

            t.Add(new HelpTopic("Ablagen leeren",
                "Die Tools legen Originale und Teile in Ablagen ab, statt sie zu löschen: „__teile“, „__konvertiert“, „__gelöscht“, „__original“, „__ersetzt“ und „__duplikate“. " +
                "„Ablagen leeren“ zeigt sie mit Grösse an und löscht nach Bestätigung endgültig. Das geht nur innerhalb des Installationsordners, " +
                "Verknüpfungen werden nie verfolgt oder gelöscht."));

            t.Add(new HelpTopic("ffmpeg",
                "ffmpeg wird für Vorschaubilder, Konvertieren, den Importer, „Medien prüfen“, „Duplikate finden“ und die Vergleichstabellen gebraucht. " +
                "Es wird im Setup oder beim ersten Bedarf geladen (ca. 100 MB) und über eine Prüfsumme kontrolliert.\n" +
                "- Standardordner: %LOCALAPPDATA%\\STEFFLIX\\ffmpeg (je Benutzer und PC, auch wenn STEFFLIX auf einem NAS liegt).\n" +
                "- Unter Tools › Einstellungen stehen Ordner und Version, mit „Ändern …“ und „Installieren“.\n" +
                "- Gibt es den gewählten Ordner auf einem PC nicht, wird der Standardordner verwendet.\n" +
                "- Ein ffmpeg aus älteren Versionen (sflix_sys\\ffmpeg) wird beim Start in den Standardordner verschoben."));

            t.Add(new HelpTopic("Lizenz und Feedback",
                "Der Lizenzschlüssel (beginnt mit SFX2) schaltet das Feedback im Updater und in STEFFLIX frei. Er wird im Setup, im Reiter „Lizenz“ oder mit --license eingegeben " +
                "und in sflix_sys\\license.js gespeichert. Er gilt dann für diese Installation in allen Browsern.\n" +
                "- Beim Speichern wird eine Bestätigung mit der Kennung dieser Installation verschickt. Erst danach heisst der Reiter „Feedback“.\n" +
                "- Ohne Internet wird der Schlüssel gespeichert und die Bestätigung beim nächsten Start nachgeholt.\n" +
                "- Feedback: Name und Nachricht, auf Wunsch mit Diagnose und dem Protokoll dieser Sitzung.\n" +
                "- „Lizenzschlüssel entfernen“ meldet die Lizenz ab und löscht sie.\n" +
                "Schlüssel aus STEFFLIX bis 2.04 sind nicht mehr gültig."));

            t.Add(new HelpTopic("Kommandozeile",
                "Der Updater lässt sich auch ohne Oberfläche starten, z.B. für die Windows-Aufgabenplanung. Aufgaben: update, cleanup, merge, convert, preview, data, renameundo " +
                "(„Folgen umbenennen“ braucht die Oberfläche). Mehrere mit Komma trennen.\n\n" +
                "# Oberfläche mit sofortigem Start\n" +
                "> updateStefflix.exe --run update,data\n" +
                "> updateStefflix.exe --log\n" +
                "--log schreibt das Protokoll von Anfang an in die Datei.\n\n" +
                "# Ohne Oberfläche\n" +
                "> updateStefflix.exe --headless data\n" +
                "> updateStefflix.exe --headless data --data-folder \"Serien\\Meine Serie\"\n" +
                "--data-folder liest nur diesen Ordner ein (mehrfach möglich, relativ zum STEFFLIX-Ordner oder absolut). " +
                "Ohne Oberfläche schreibt der Updater immer ins Protokoll (sflix_sys\\updater.log, begrenzt). Exit-Code 0 = alles in Ordnung, 1 = Fehler, " +
                "2 = kein STEFFLIX-Ordner, 3 = der Updater läuft bereits.\n\n" +
                "# Installation\n" +
                "> updateStefflix.exe --install \"D:\\Medien\"\n" +
                "> updateStefflix.exe --install \"D:\\Medien\" --ffmpeg --license SFX2-…\n" +
                "--ffmpeg installiert ffmpeg in den Standardordner, --ffmpeg \"Ordner\" in einen eigenen.\n\n" +
                "# Lizenz\n" +
                "> updateStefflix.exe --license SFX2-…\n" +
                "> updateStefflix.exe --license-remove\n\n" +
                "# Aufgabenplanung (Beispiel)\n" +
                "Jede Nacht um 3 Uhr Medien einlesen:\n" +
                "> schtasks /create /tn \"STEFFLIX einlesen\" /sc daily /st 03:00 /tr \"\\\"D:\\Medien\\updateStefflix.exe\\\" --headless data\""));

            t.Add(new HelpTopic("Ablagen und Dateien",
                "# Ablagen der Tools (Ordner mit „__“)\n" +
                "- __teile: Teile nach dem Zusammenführen\n" +
                "- __konvertiert: Originale nach dem Konvertieren oder Importieren\n" +
                "- __gelöscht: Vorschaubilder ohne Medium\n" +
                "- __original: Originale verkleinerter Vorschaubilder\n" +
                "- __ersetzt: beim Umbenennen ersetzte Dateien\n" +
                "- __duplikate: aussortierte Duplikate\n\n" +
                "# Dateien in sflix_sys\n" +
                "- data.js: Medienverzeichnis (Daten einlesen)\n" +
                "- renamed.js: umbenannte Pfade\n" +
                "- install.js: Kennung dieser Installation\n" +
                "- license.js: Lizenzschlüssel\n" +
                "- updater_info.js: installierte Versionen für STEFFLIX\n" +
                "- updater_settings.json: Einstellungen des Updaters\n" +
                "- updater.log: Protokoll (nur auf Wunsch bzw. ohne Oberfläche)\n" +
                "- folgen_cache.json, folgen_rueckgaengig.json: Folgen umbenennen"));

            return t;
        }
    }
}
