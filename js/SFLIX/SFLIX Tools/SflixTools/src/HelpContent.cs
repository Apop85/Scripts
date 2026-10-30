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
                "Welche Dateien zu STEFFLIX gehören und welche alten Dateien entfernt werden, kommt mit dem Update. Persönliche Dateien wie Medienliste, Lizenz und Einstellungen bleiben immer unberührt. " +
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
                "- Ordner: „Alles“ oder eine Liste von Ordnern (auch ausserhalb von STEFFLIX).\n" +
                "- Mehrere Vorschaubilder entstehen gleichzeitig (siehe „Schneller prüfen“)."));

            t.Add(new HelpTopic("Schneller prüfen",
                "„Medien prüfen“, „Duplikate finden“ und „Vorschaubilder“ bearbeiten mehrere Dateien gleichzeitig und merken sich, was sie über eine Datei herausgefunden haben.\n" +
                "# Gleichzeitig\n" +
                "Einstellbar unter Tools › Einstellungen › Gleichzeitig. Automatisch: 4 Dateien beim Prüfen, 2 bei Vorschaubildern. " +
                "Bei einer langsamen externen USB-Festplatte 1 wählen, dann arbeiten die Tools wie früher eine Datei nach der anderen. " +
                "Die volle Prüfsumme bei „Duplikate finden“ liest immer eine Datei nach der anderen.\n" +
                "# Gemerkte Prüfergebnisse\n" +
                "Dauer, Auflösung, Video und Ton jeder untersuchten Datei stehen in sflix_sys\\medien_cache.json. Ist eine Datei unverändert (gleiche Grösse und Änderungszeit), " +
                "wird sie beim nächsten Mal nicht neu untersucht. Ein zweiter Lauf von „Medien prüfen“ ist dadurch fast sofort fertig. " +
                "Eine neue ffmpeg-Version verwirft die gemerkten Ergebnisse, ebenso der Knopf „Gemerkte Prüfergebnisse löschen“ in den Einstellungen.\n" +
                "# Duplikate: Schnelltest\n" +
                "Bei gleich grossen Dateien vergleicht „Duplikate finden“ zuerst je 1 MB vom Anfang, aus der Mitte und vom Ende. " +
                "Nur wenn das übereinstimmt, liest es die ganzen Dateien für die Prüfsumme. Das spart bei Filmen auf dem NAS viel Zeit."));

            t.Add(new HelpTopic("Folgen umbenennen",
                "Holt die Folgenliste einer Serie von fernsehserien.de (deutsche Titel, sonst TVmaze) und benennt die Dateien einheitlich um, " +
                "z.B. „Serie - S01E02 - Titel.mp4“ im Ordner „Staffel 01“. Erkannt werden S01E02, 1x02 und Doppelfolgen (S01E02E03).\n" +
                "- Staffelordner wie „Staffel 1“, „staffel1“, „Season 1“, „S01“ oder „Staffel 01 (Blu-ray)“ werden zu „Staffel 01“; gibt es beide, werden sie zusammengeführt. " +
                "Ordner mit mehreren Staffeln („Staffel 1-3“) bleiben unverändert.\n" +
                "- Vorher zeigt eine Vorschau alle Änderungen, fehlende Folgen und nicht zugeordnete Dateien. Nur angehakte Zeilen werden ausgeführt.\n" +
                "- Gibt es mehrere Dateien für eine Folge oder das Ziel schon, zeigt die Vorschau beim Anklicken einen Vergleich mit Grösse, Auflösung und Dauer.\n" +
                "- Vorschaubilder ziehen mit, Favoriten und Fortschritt bleiben erhalten.\n" +
                "- „Letztes Umbenennen rückgängig“ stellt den vorherigen Zustand wieder her.\n" +
                "Geladene Folgenlisten werden gespeichert (sflix_sys\\folgen_cache.json) und beim nächsten Mal angeboten."));

            t.Add(new HelpTopic("Duplikate finden",
                "Sucht doppelte Medien in den gewählten Ordnern:\n" +
                "- Identische Dateien: gleiche Grösse und gleicher Inhalt (Prüfsumme), auch mit anderem Namen.\n" +
                "- Gleiche Folge: dieselbe Staffel und Folge innerhalb einer Serie, egal ob S01E01, 1x01, S1E1 oder S01E1. Meist verschiedene Fassungen, z.B. SD und HD. " +
                "Als Serie gilt der Ordner über dem Staffelordner („Staffel 1“, „Season 01 HD“ …), sonst der Ordner der Folge. Serien in Unterordnern " +
                "(z.B. Serien\\Star Trek\\TOS und Serien\\Star Trek\\TNG) bleiben so getrennt.\n" +
                "- Gleicher Name: gleicher Dateiname ohne Endung, z.B. in zwei Ordnern oder „Film.mp4“ neben „Film.mkv“. Folgen werden dabei nur innerhalb derselben Serie verglichen. " +
                "Preview- und Ablage-Ordner zählen nicht.\n" +
                "Ein Fenster zeigt jede Gruppe mit dem Grund (Datei, Folge oder Dateiname), Grösse, Auflösung, Dauer, Video/Ton und Datum. " +
                "Steht eine Datei in mehreren Gruppen, gilt ihr Häkchen überall. Vorausgewählt bleibt die Datei mit der höheren Auflösung, " +
                "bei gleicher Auflösung die grössere (bei identischen Dateien die mit dem kürzesten Pfad). Überschneiden sich Doppelfolgen, ist nichts vorausgewählt. " +
                "Die übrigen Dateien kommen samt Vorschaubild nach „__duplikate“; „Ablagen leeren“ löscht sie später endgültig."));

            t.Add(new HelpTopic("Zusammenführen",
                "Fügt gesplittete Videos wie „Titel pt1“, „Titel pt2“ zu einer Datei zusammen. Vorher wählst du aus, welche.\n" +
                "- Teile danach nach „__teile“ verschieben."));

            t.Add(new HelpTopic("Konvertieren",
                "Wandelt Videos in browserkompatible MP4-Dateien (H.264/AAC) um, bei mehreren Tonspuren je eine Datei. " +
                "Der Updater nutzt die Grafikkarte (NVIDIA, Intel, AMD), sonst den Prozessor. Originale kommen nach „__konvertiert“.\n" +
                "- Gleichzeitig: wie viele Dateien parallel umgewandelt werden (0 = automatisch).\n" +
                "- Auch Musik in MP3 umwandeln: Musik, die der Browser nicht abspielt (z.B. WMA, AIFF, APE, ALAC in M4A), wird zu MP3 mit hoher Qualität, " +
                "Titel und Interpret bleiben erhalten. Abspielbare Musik (MP3, AAC, FLAC, OGG …) bleibt unverändert.\n" +
                "- Originale löschen statt verschieben."));

            t.Add(new HelpTopic("Medien prüfen",
                "Prüft die Medien in den gewählten Ordnern und listet Probleme auf:\n" +
                "- Nicht abspielbar: Format oder Codec, den der Browser nicht abspielen kann (z.B. MKV, HEVC, AC3/DTS-Ton, WMA, ALAC).\n" +
                "- Defekt: ffprobe kann die Datei nicht lesen.\n" +
                "- Leer: Datei mit 0 Byte.\n" +
                "- Ohne Vorschaubild.\n" +
                "- Leere Ordner: Ordner ohne Medien, auch nicht in Unterordnern (Vorschaubilder und Ablagen zählen nicht). Gemeldet wird nur der oberste solche Ordner, nie der gewählte Ordner selbst und nie eine Kategorie wie „Filme“.\n" +
                "Im Ergebnis steht jede Datei bzw. jeder Ordner einmal. In den ersten drei Spalten wählst du die Aktionen:\n" +
                "- Papierkorb: löschen. Die Datei kommt samt Vorschaubild nach „__gelöscht“, ein Ordner als Ganzes nach „__gelöscht“ im Ordner darüber. Endgültig löscht sie „Ablagen leeren“. Schliesst die anderen Aktionen aus.\n" +
                "- Pfeile: konvertieren (Videos zu MP4, Musik zu MP3).\n" +
                "- Bild: Vorschaubild erstellen (nach dem Konvertieren für die neue Datei). Geht zusammen mit Konvertieren.\n" +
                "Vorausgewählt: defekte und leere Dateien sowie leere Ordner löschen (Ordner mit anderen Dateien wie Dokumenten nicht), sonst konvertieren und Vorschaubilder erstellen. " +
                "Ein Klick auf ein Kästchen schaltet um; für markierte Zeilen geht das auch mit Entf, K und V. Du kannst nach Art filtern und nach " +
                "Datei oder Ordner suchen, die Auswahl bleibt erhalten. „Vorschlag“ und „Keine“ gelten für die angezeigten Zeilen. " +
                "„Ausführen“ startet alle gewählten Dateioperationen, danach wird neu eingelesen."));

            t.Add(new HelpTopic("Tabellen",
                "Alle Tabellen im Updater (Ergebnisse, Vorschau, Vergleich) lassen sich sortieren: Klick auf den Spaltenkopf sortiert aufsteigend, " +
                "ein zweiter Klick absteigend. Ein kleines Dreieck zeigt die Spalte. Grösse und Dauer werden als Zahl sortiert, Namen mit Zahlen natürlich " +
                "(„Folge 2“ vor „Folge 10“). In Tabellen mit Gruppen (z.B. Duplikate) wird innerhalb jeder Gruppe sortiert."));

            t.Add(new HelpTopic("NAS und Netzlaufwerke",
                "Die Tools arbeiten auch auf einem NAS. Schreibgeschützte Dateien verschieben, ersetzen und löschen sie trotzdem " +
                "(der Schreibschutz bleibt beim Verschieben erhalten). Ist eine Datei kurz geöffnet, wird es nach einer Pause erneut versucht.\n" +
                "Meldet das Protokoll trotzdem „Zugriff verweigert“:\n" +
                "- Die Datei ist geöffnet, z.B. in STEFFLIX im Browser, in der Vorschau des Explorers oder vom Indexdienst des NAS. Schliessen und erneut starten.\n" +
                "- Auf dem NAS fehlt das Recht „Löschen“. Wo das NAS Windows-Rechte speichert, ergänzt der Updater es selbst (steht dann im Protokoll). " +
                "NAS mit Linux-Rechten (z.B. QNAP) speichern es nicht pro Datei: Gehört die Datei einem anderen Benutzer, etwa dem Administrator " +
                "(Dateien aus der Download-Station oder dem Dateimanager des NAS), im NAS den Besitzer bzw. die Rechte des Ordners neu setzen und auf alle Unterordner anwenden.\n" +
                "- Der Updater läuft als Administrator: Dann sind verbundene Netzlaufwerke (z.B. M:) oft nicht oder mit anderen Zugangsdaten verbunden. " +
                "Den Updater normal starten."));

            t.Add(new HelpTopic("Ablagen leeren",
                "Die Tools legen Originale und Teile in Ablagen ab, statt sie zu löschen: „__teile“, „__konvertiert“, „__gelöscht“, „__original“, „__ersetzt“ und „__duplikate“. " +
                "Unter dem Tool steht, wie viel Speicher die Ablagen belegen (wird im Hintergrund berechnet). " +
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
                "- __gelöscht: Vorschaubilder ohne Medium und bei „Medien prüfen“ gelöschte Dateien\n" +
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
                "- folgen_cache.json, folgen_rueckgaengig.json: Folgen umbenennen\n" +
                "- medien_cache.json: gemerkte Prüfergebnisse der Tools (siehe „Schneller prüfen“)"));

            return t;
        }
    }
}
