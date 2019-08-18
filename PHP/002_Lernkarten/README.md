# Lernkarten
Diese Seite durchsucht alle Word-Dokumente nach darin hinterlegten Lernkarten-Einträgen.

# Installation
1. Repository herunterladen
2. Apache-Server oder Nginx installieren
3. PHP herunterladen
4. Apache/Nginx/PHP konfigurieren
5. Python 3 herunterladen und installieren
6. Python bei Systemvariabeln in PATH hinterlegen
7. Lernkarten-Dateien in HTML-Ordner von Apache oder Nginx verschieben
8. Nachfolgendes Setupprocedere durchführen

# Setup
Die Seite wird als erstes nach dem Installationspfad und dem Dokumentenpfad fragen.

- **Beispiel Installationspfad: C:\Apache\httpd\Lernkarten**
- **Beispiel Dokumentenpfad: C:\Users\Username\Dokumente**

Danach wird nach der Einleitung und dem Trennzeichen für die Fragen gefragt
Als standardeintrag für die Einleitung ist "//qa" hinterlegt und als Trennzeichen "<"

Dies wird folgendermassen in Worddateien verwendet:
//qa<Fach/Thema<Frage<Antwort<

### Beispiel:

**//qa<Mathematik/Addition<Was ergibt 1+1?<2<**

# Funktionen
- Extraktion/Erstellen von Lernkarten
  - Automatische extraktion aus allen auffindbaren docx-Dateien im hinterlegten Dokumentenordner
  - Extraktion aus allen docx-Dateien aus einem bestimmten Ordner
  - Extraktion aus einem bestimmten docx-File
  - Manuelles erstellen von Lernkarten
- Referenzangabe aus welchem File eine Karte generiert wurde
- Erstellen von randomisierten Prüfungen
  - Limitiert nach Auswahl Fächer
  - Limitiert nach erreichter Punktzahl
- Statistische Auswertung
  - Nach Auswertung aller Antworten
  - Nach Auswertung der Punktzahl
  - Nach Auswertung der Antworten pro Fach
- Einstellungen
  - Manuelle Angabe des Standardpfades für Dokumente
  - Überschreibung des Installationspfades möglich
  - Angabe der Einleitungssequenz einer Frage
  - Angabe des Trennzeichens einer Frage
  - Zurücksetzen der Statistik
  - Beispiele
