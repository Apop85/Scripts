"""Dateilisten und Prüffunktionen für das Release (create_checksums.py, check_release.py, release.ps1).

Seit 4.0 ist STEFFLIX.exe das Programm für Player, Tools, Installation und Updates (C#, Quellcode in
"SFLIX_Tools\\SflixTools"); es lädt selbst herunter und prüft. Dieselbe Datei liegt vorerst auch als
updateStefflix.exe im Repository (Updater bis 3.x laden nur diesen Namen). updater.exe und updateData.exe
sind eingefroren und werden nur noch für den Umstieg älterer Installationen bereitgestellt.

Seit 4.1.0 nur noch Listen und Prüffunktionen (ohne Download, ohne das Paket requests).
Der Release-Ordner (start.html, sflix_sys, die Programme, checksums.json) liegt zwei Ebenen über
diesem Skript: RELEASE_DIR.
"""
import hashlib
import os

# Release-Ordner (dieses Skript liegt in <Release-Ordner>\SFLIX_Tools\Release)
RELEASE_DIR = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))

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
    # Updater 3.x (laden nur diesen Namen); ab 4.0 dieselbe Datei wie STEFFLIX.exe
    "updateStefflix.exe": BIN_URL + "updateStefflix.exe",
    # Ab 4.0: Player und Tools (Selbst-Update über diesen Namen)
    "STEFFLIX.exe": BIN_URL + "STEFFLIX.exe",
    # Vorgabe für ffmpeg (Version, Download-Adresse, SHA256), gelesen vom Programm ab 3.0
    "ffmpeg.json": RAW_URL + "ffmpeg.json",
}

# Dateien, die nur eine Prüfsumme in checksums.json brauchen und nicht installiert werden
RELEASE_ONLY_FILES = ["ffmpeg.json"]

# Dateien, die setup in einen neuen STEFFLIX-Ordner lädt
INSTALL_FILES = [name for name in FILES if name not in RELEASE_ONLY_FILES]

# Programmdateien der Weboberfläche (ab 2.00 aufgeteilt, vorher sflix_sys/sflix.js)
SCRIPT_FILES = [name for name in FILES if name.startswith("sflix_sys/js/")]

# Dateien der Weboberfläche, die in jeder Installation vorhanden sein müssen
# (data.js erzeugt "Daten einlesen" bzw. updateData.exe, die Programme aktualisieren sich selbst)
WEB_FILES = [name for name in INSTALL_FILES if not name.endswith(".exe") and name != "sflix_sys/data.js"]

# Dateien früherer Versionen, die nach einem erfolgreichen Update gelöscht werden.
# Seit Updater 3.4.1 stehen WEB_FILES und OBSOLETE_FILES auch in checksums.json: Eine neue oder
# entfallene Datei hier eintragen genügt, ein neuer Updater ist dafür nicht nötig.
# Erlaubt sind nur start.html und Dateien in sflix_sys (siehe Net.IsAllowedWebPath).
OBSOLETE_FILES = ["sflix_sys/sflix.js", "sflix_sys/sflix_bg.jpg"]


TEXT_EXTENSIONS = (".js", ".css", ".html", ".txt", ".json", ".svg")

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
