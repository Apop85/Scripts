"""Prüft vor dem Bauen eines Releases, ob alles zusammenpasst. Bricht mit Fehlercode 1 ab,
wenn etwas fehlt. Wird von build_release.bat aufgerufen."""
import os
import re
import sys

from sflix_update import FILES, OBSOLETE_FILES, RELEASE_DIR, SCRIPT_FILES

base_dir = RELEASE_DIR
errors = []


def read(path):
    with open(os.path.join(base_dir, path), encoding="utf-8") as reader:
        return reader.read()


# 1. Version in version.js muss die oberste Version im Changelog sein
version_match = re.search(r"version\s*=\s*([\d.]+)", read("sflix_sys/version.js"))
news_match = re.search(r"<b>Version ([\d.]+)</b>", read("sflix_sys/news.txt"))
if not version_match or not news_match:
    errors.append("Version in version.js oder news.txt nicht gefunden")
elif float(version_match.group(1)) != float(news_match.group(1)):
    errors.append(f"version.js ({version_match.group(1)}) passt nicht zum Changelog (oberste Version {news_match.group(1)})")
else:
    print(f"Version {version_match.group(1)}")

# 2. Alle Dateien vorhanden, auch die eingefrorenen Übergangsprogramme (updater.exe,
#    updateData.exe; werden nicht mehr gebaut, aber von alten Installationen geladen)
for path in FILES:
    if not os.path.exists(os.path.join(base_dir, path)):
        errors.append(f"Datei fehlt: {path}")

# 3. start.html lädt alle Programmdateien
start_html = read("start.html")
for path in SCRIPT_FILES:
    if f'src="{path}"' not in start_html:
        errors.append(f"start.html lädt {path} nicht")

# 4. Platzhalter für Updater vor 2.00 (laden sflix_sys/sflix.js fest herunter)
if not os.path.exists(os.path.join(base_dir, "sflix_sys/sflix.js")):
    errors.append("Platzhalter sflix_sys/sflix.js fehlt (wird von Updatern vor 2.00 benötigt)")
for path in OBSOLETE_FILES:
    if path != "sflix_sys/sflix.js" and os.path.exists(os.path.join(base_dir, path)):
        errors.append(f"Veraltete Datei sollte entfernt werden: {path}")

# 5. updateStefflix.exe muss der STEFFLIX Updater ab 3.0 sein (wird nicht mehr aus
#    updateStefflix.py gebaut, sondern aus "SFLIX Tools\SflixTools" hierher kopiert)
def file_version(path):
    """Dateiversion (major, minor, build) einer Windows-Programmdatei oder None."""
    import ctypes
    from ctypes import wintypes
    version_dll = ctypes.WinDLL("version")
    size = version_dll.GetFileVersionInfoSizeW(path, None)
    if not size:
        return None
    buffer = ctypes.create_string_buffer(size)
    if not version_dll.GetFileVersionInfoW(path, 0, size, buffer):
        return None
    pointer = ctypes.c_void_p()
    length = wintypes.UINT()
    if not version_dll.VerQueryValueW(buffer, "\\", ctypes.byref(pointer), ctypes.byref(length)):
        return None
    info = ctypes.cast(pointer, ctypes.POINTER(ctypes.c_uint32 * 13)).contents
    # VS_FIXEDFILEINFO: dwFileVersionMS = info[2], dwFileVersionLS = info[3]
    return (info[2] >> 16, info[2] & 0xFFFF, info[3] >> 16)


updater_exe = os.path.join(base_dir, "updateStefflix.exe")
if not os.path.exists(updater_exe):
    errors.append("updateStefflix.exe fehlt (build_release.bat baut sie aus SFLIX Tools\\SflixTools)")
else:
    version = file_version(updater_exe)
    if version is None or version < (3, 0, 0):
        errors.append("updateStefflix.exe ist nicht der STEFFLIX Updater ab 3.0 (alte Python-Version?)")
    else:
        print("updateStefflix.exe Version %d.%d.%d" % version)

# 6. ffmpeg.json muss gültig sein (sonst lädt der Updater die eingebaute ffmpeg-Version)
import json
try:
    ffmpeg = json.loads(read("ffmpeg.json"))
    if not (ffmpeg.get("version") and str(ffmpeg.get("url", "")).startswith("https://")
            and len(str(ffmpeg.get("sha256", ""))) == 64):
        errors.append("ffmpeg.json ist unvollständig (version, url, sha256)")
    else:
        print(f"ffmpeg {ffmpeg['version']}")
except (OSError, ValueError):
    errors.append("ffmpeg.json fehlt oder ist kein gültiges JSON")

if errors:
    print("FEHLER:")
    for error in errors:
        print(f"  - {error}")
    sys.exit(1)
print("Prüfung OK")
