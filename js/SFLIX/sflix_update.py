"""Gemeinsame Download- und Prüffunktionen für setup, updater, updateStefflix und updateData.

Ab Version 3.0 ist updateStefflix.exe der STEFFLIX Updater mit Oberfläche (C#, Quellcode in
"SFLIX Tools\\SflixTools"). Er wird nicht mehr aus updateStefflix.py gebaut. updater.exe und
updateData.exe werden nur noch für den Umstieg älterer Installationen bereitgestellt.

Jede heruntergeladene Datei wird geprüft:
  1. HTTP-Status 200 und nicht leer
  2. .exe-Dateien müssen echte Windows-Programme sein (MZ- und PE-Kennung)
  3. Textdateien müssen gültiges UTF-8 sein
  4. Die SHA256-Prüfsumme muss mit checksums.json übereinstimmen

Erst wenn alle Prüfungen bestanden sind, wird die vorhandene Datei ersetzt.
"""
import hashlib
import json
import os
import sys

import requests


def program_dir():
    """Ordner, in dem das laufende Programm liegt (bei den .exe-Dateien der STEFFLIX-Ordner).

    Das Arbeitsverzeichnis ist dafür nicht zuverlässig: Startet ein Programm ein anderes über
    die Kommandozeile (z.B. updateStefflix.exe vor 2.00 mit os.system) und liegt STEFFLIX auf
    einem Netzwerkpfad (\\\\server\\freigabe), setzt Windows das Arbeitsverzeichnis auf C:\\Windows.
    Beim Ausführen als Python-Skript (Entwicklung, Tests) gilt weiterhin das Arbeitsverzeichnis.
    """
    if getattr(sys, "frozen", False):
        return os.path.dirname(os.path.abspath(sys.executable))
    return os.getcwd()


def enter_stefflix_dir():
    """Wechselt in den Programmordner und bricht ab, wenn das kein STEFFLIX-Ordner ist
    (Schutz davor, in einem fremden Ordner Dateien zu installieren oder umzubenennen)."""
    folder = program_dir()
    os.chdir(folder)
    if not os.path.isfile(os.path.join(folder, "start.html")) or not os.path.isdir(os.path.join(folder, "sflix_sys")):
        print(f"Dieser Ordner ist kein STEFFLIX-Ordner (start.html oder sflix_sys fehlt):\n{folder}")
        print("Bitte das Programm im STEFFLIX-Ordner starten.")
        print()
        input("Enter zum Beenden")
        sys.exit(1)
    return folder

RAW_URL = "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/"
BIN_URL = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/"

# Zum Testen eines Updates vor dem Veröffentlichen, z.B.:
#   set SFLIX_UPDATE_URL=http://127.0.0.1:8000/
#   python -m http.server 8000   (im Entwicklungsordner)
if os.environ.get("SFLIX_UPDATE_URL"):
    RAW_URL = BIN_URL = os.environ["SFLIX_UPDATE_URL"].rstrip("/") + "/"

CHECKSUM_FILE = "checksums.json"

# Alle Dateien, die von setup oder den Updatern heruntergeladen werden können.
FILES = {
    "sflix_sys/data.js": RAW_URL + "sflix_sys/data.js",
    "sflix_sys/sflix.css": RAW_URL + "sflix_sys/sflix.css",
    "sflix_sys/js/core.js": RAW_URL + "sflix_sys/js/core.js",
    "sflix_sys/js/storage.js": RAW_URL + "sflix_sys/js/storage.js",
    "sflix_sys/js/cards.js": RAW_URL + "sflix_sys/js/cards.js",
    "sflix_sys/js/pages.js": RAW_URL + "sflix_sys/js/pages.js",
    "sflix_sys/js/player.js": RAW_URL + "sflix_sys/js/player.js",
    "sflix_sys/js/ui.js": RAW_URL + "sflix_sys/js/ui.js",
    "sflix_sys/js/main.js": RAW_URL + "sflix_sys/js/main.js",
    "sflix_sys/splash.svg": RAW_URL + "sflix_sys/splash.svg",
    "sflix_sys/sflixLogo.png": RAW_URL + "sflix_sys/sflixLogo.png",
    "sflix_sys/sflixLogo.svg": RAW_URL + "sflix_sys/sflixLogo.svg",
    "sflix_sys/version.js": RAW_URL + "sflix_sys/version.js",
    "start.html": RAW_URL + "start.html",
    "updateData.exe": BIN_URL + "updateData.exe",
    "updater.exe": BIN_URL + "updater.exe",
    "updateStefflix.exe": BIN_URL + "updateStefflix.exe",
    # Vorgabe für ffmpeg (Version, Download-Adresse, SHA256), gelesen von updateStefflix.exe ab 3.0
    "ffmpeg.json": RAW_URL + "ffmpeg.json",
}

# Dateien, die nur eine Prüfsumme in checksums.json brauchen und nicht installiert werden
RELEASE_ONLY_FILES = ["ffmpeg.json"]

# Dateien, die setup in einen neuen STEFFLIX-Ordner lädt
INSTALL_FILES = [name for name in FILES if name not in RELEASE_ONLY_FILES]

# Programmdateien der Weboberfläche (ab 2.00 aufgeteilt, vorher sflix_sys/sflix.js)
SCRIPT_FILES = [name for name in FILES if name.startswith("sflix_sys/js/")]

# Dateien der Weboberfläche, die in jeder Installation vorhanden sein müssen
# (data.js erzeugt updateData.exe, die Programme aktualisieren sich gegenseitig)
WEB_FILES = [name for name in INSTALL_FILES if not name.endswith(".exe") and name != "sflix_sys/data.js"]

# Dateien früherer Versionen, die nach einem erfolgreichen Update gelöscht werden
OBSOLETE_FILES = ["sflix_sys/sflix.js", "sflix_sys/sflix_bg.jpg"]


def remove_obsolete_files(base_dir="."):
    for name in OBSOLETE_FILES:
        path = os.path.join(base_dir, name)
        try:
            if os.path.isfile(path):
                os.remove(path)
                print(f"Veraltete Datei entfernt: {name}")
        except OSError:
            pass


TEXT_EXTENSIONS = (".js", ".css", ".html", ".txt", ".json", ".svg")
TIMEOUT = 120


class DownloadError(Exception):
    pass


def is_text_file(path):
    return path.lower().endswith(TEXT_EXTENSIONS)


def checksum(path, data):
    """SHA256 einer Datei. Bei Textdateien werden Zeilenenden vereinheitlicht,
    damit CRLF (Windows-Arbeitskopie) und LF (GitHub) dieselbe Summe ergeben."""
    if is_text_file(path):
        data = data.replace(b"\r\n", b"\n")
    return hashlib.sha256(data).hexdigest()


def is_windows_exe(data):
    """Prüft die MZ-Kennung und den PE-Header einer Windows-Programmdatei."""
    if len(data) < 0x40 or data[:2] != b"MZ":
        return False
    pe_offset = int.from_bytes(data[0x3C:0x40], "little")
    return data[pe_offset:pe_offset + 4] == b"PE\0\0"


def load_checksums():
    """Lädt checksums.json von GitHub. Ohne Prüfsummen wird nichts installiert."""
    try:
        answer = requests.get(RAW_URL + CHECKSUM_FILE, timeout=TIMEOUT)
    except requests.RequestException as error:
        raise DownloadError(f"Prüfsummen nicht erreichbar ({error.__class__.__name__})")
    if answer.status_code != 200:
        raise DownloadError(f"Prüfsummen nicht gefunden (HTTP {answer.status_code})")
    try:
        return json.loads(answer.content.decode("utf-8"))
    except ValueError:
        raise DownloadError("Prüfsummendatei ist beschädigt")


def download_file(path, checksums):
    """Lädt eine Datei herunter und gibt den geprüften Inhalt zurück."""
    url = FILES[path]
    try:
        answer = requests.get(url, timeout=TIMEOUT)
    except requests.RequestException as error:
        raise DownloadError(f"keine Verbindung ({error.__class__.__name__})")

    if answer.status_code != 200:
        raise DownloadError(f"HTTP {answer.status_code}")

    data = answer.content
    if not data:
        raise DownloadError("leere Datei")

    if path.lower().endswith(".exe") and not is_windows_exe(data):
        raise DownloadError("keine gültige Programmdatei")

    if is_text_file(path):
        try:
            data.decode("utf-8")
        except UnicodeDecodeError:
            raise DownloadError("ungültige Textdatei")

    expected = checksums.get(path)
    if expected is None:
        raise DownloadError("keine Prüfsumme vorhanden")
    if checksum(path, data) != expected:
        raise DownloadError("Prüfsumme stimmt nicht")

    return data


def install_file(target, data, backup=True):
    """Schreibt die Datei zuerst als .new und ersetzt dann das Original.
    Das Original wird zu .old umbenannt (geht auch bei laufenden Programmen).
    Gibt zurück, ob eine Sicherung (.old) angelegt wurde."""
    folder = os.path.dirname(target)
    if folder:
        os.makedirs(folder, exist_ok=True)

    temp_file = target + ".new"
    try:
        with open(temp_file, "wb") as file_writer:
            file_writer.write(data)
    except OSError:
        if os.path.exists(temp_file):
            os.remove(temp_file)
        raise

    old_file = target + ".old"
    has_backup = False
    if backup and os.path.exists(target):
        if os.path.exists(old_file):
            os.remove(old_file)
        os.replace(target, old_file)
        has_backup = True

    try:
        os.replace(temp_file, target)
    except OSError:
        # Original wiederherstellen
        if has_backup:
            os.replace(old_file, target)
        raise
    return has_backup


def is_up_to_date(path, target, checksums):
    """True, wenn die lokale Datei bereits dem Stand in checksums.json entspricht."""
    expected = checksums.get(path)
    if expected is None or not os.path.isfile(target):
        return False
    try:
        with open(target, "rb") as file_reader:
            return checksum(path, file_reader.read()) == expected
    except OSError:
        return False


def restore_file(target, has_backup):
    """Macht eine Installation rückgängig: .old zurück oder neue Datei entfernen."""
    old_file = target + ".old"
    if has_backup and os.path.exists(old_file):
        os.replace(old_file, target)
    elif not has_backup and os.path.exists(target):
        os.remove(target)


def update_files(paths, base_dir=".", label="Lade", skip_existing=False):
    """Lädt die angegebenen Dateien herunter und installiert sie nach dem
    Alles-oder-nichts-Prinzip:
      1. Alle Dateien herunterladen und prüfen. Schlägt eine fehl, wird nichts verändert.
      2. Alle Dateien installieren. Schlägt eine fehl, werden die bereits
         installierten wieder auf den alten Stand zurückgesetzt.
    Gibt die Anzahl fehlgeschlagener Dateien zurück (0 = Erfolg)."""
    print(f"{'Lade Prüfsummen...':<50}", end="")
    try:
        checksums = load_checksums()
        print("OK")
    except DownloadError as error:
        print(f"FEHLER: {error}")
        print("Es wurden keine Dateien verändert.")
        return len(paths)

    # Schritt 1: Herunterladen und prüfen
    downloads = []
    failed = 0
    for path in paths:
        target = os.path.join(base_dir, path)
        print(f"{label} {path}...".ljust(50), end="")

        if skip_existing and os.path.exists(target):
            print("VORHANDEN")
            continue

        # Unveränderte Dateien (gleiche Prüfsumme) nicht erneut herunterladen
        if is_up_to_date(path, target, checksums):
            print("AKTUELL")
            continue

        try:
            downloads.append((path, target, download_file(path, checksums)))
            print("OK")
        except DownloadError as error:
            failed += 1
            print(f"FEHLER: {error}")

    if failed:
        print(f"{failed} Datei(en) fehlerhaft. Es wurden keine Dateien verändert.")
        return failed

    # Schritt 2: Installieren, bei einem Fehler alles zurücksetzen
    installed = []
    for path, target, data in downloads:
        print(f"Installiere {path}...".ljust(50), end="")
        try:
            installed.append((target, install_file(target, data)))
            print("OK")
        except OSError as error:
            print(f"FEHLER: {error.strerror}")
            print("Installation wird rückgängig gemacht...".ljust(50), end="")
            restore_failed = 0
            for installed_target, has_backup in reversed(installed):
                try:
                    restore_file(installed_target, has_backup)
                except OSError:
                    restore_failed += 1
            print("OK" if not restore_failed else f"FEHLER bei {restore_failed} Datei(en)")
            return len(downloads)

    return 0
