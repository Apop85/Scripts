"""Prüft vor dem Bauen eines Releases, ob alles zusammenpasst. Bricht mit Fehlercode 1 ab,
wenn etwas fehlt. Wird von build_release.bat aufgerufen."""
import os
import re
import sys

from sflix_update import FILES, OBSOLETE_FILES, RELEASE_DIR, SCRIPT_FILES
from versioning import legacy_number, read_version_js, version_name

base_dir = RELEASE_DIR
errors = []


def read(path):
    with open(os.path.join(base_dir, path), encoding="utf-8") as reader:
        return reader.read()


# 1. Version in version.js muss die oberste Version im Changelog sein. Ab 2.5.0 steht dort
#    zusätzlich die alte Zahl für Installationen bis 2.04 (siehe versioning.py).
web_name, web_legacy = read_version_js(read("sflix_sys/version.js"))
news_match = re.search(r"<b>Version ([\d.]+)</b>", read("sflix_sys/news.txt"))
if not web_name or not news_match:
    errors.append("Version in version.js oder news.txt nicht gefunden")
elif web_name != version_name(news_match.group(1)):
    errors.append(f"version.js ({web_name}) passt nicht zum Changelog (oberste Version {news_match.group(1)})")
elif web_legacy is None or float(web_legacy) != float(legacy_number(web_name)):
    errors.append(f"version.js: alte Zahl {web_legacy} passt nicht zu {web_name} (erwartet {legacy_number(web_name)})")
else:
    print(f"Version {web_name} (alte Zahl {web_legacy})")

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
# Die Android-App setzt ihre Skripte vor genau diese Zeile ein (BrowserActivity.injectIntoStart; Rückfall vor </body>)
if '<script src="sflix_sys/js/main.js"' not in start_html:
    errors.append('start.html: <script src="sflix_sys/js/main.js" fehlt (Einstiegspunkt der Android-App)')

# 4. Platzhalter für Updater vor 2.00 (laden sflix_sys/sflix.js fest herunter)
if not os.path.exists(os.path.join(base_dir, "sflix_sys/sflix.js")):
    errors.append("Platzhalter sflix_sys/sflix.js fehlt (wird von Updatern vor 2.00 benötigt)")
for path in OBSOLETE_FILES:
    if path != "sflix_sys/sflix.js" and os.path.exists(os.path.join(base_dir, path)):
        errors.append(f"Veraltete Datei sollte entfernt werden: {path}")

# 5. STEFFLIX.exe muss das Programm ab 4.0 sein (aus "SFLIX_Tools\SflixTools" hierher kopiert),
#    updateStefflix.exe dieselbe Datei (für Updater bis 3.x)
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
desktop_exe = os.path.join(base_dir, "STEFFLIX.exe")
if not os.path.exists(desktop_exe):
    errors.append("STEFFLIX.exe fehlt (build_release.bat baut sie aus SFLIX_Tools\\SflixTools)")
else:
    version = file_version(desktop_exe)
    if version is None or version < (4, 0, 0):
        errors.append("STEFFLIX.exe ist nicht das Programm ab 4.0")
    else:
        print("STEFFLIX.exe Version %d.%d.%d" % version)
        # Version im Quellcode: weicht sie ab, ist der Quellcode nicht (oder anders) gebaut. Nur Hinweis, weil
        # release.ps1 diese Prüfung vor dem Bauen bzw. bei absichtlich nicht gebautem Desktop aufruft.
        program_cs = os.path.join(base_dir, "SFLIX_Tools", "SflixTools", "src", "Program.cs")
        if os.path.exists(program_cs):
            m = re.search(r'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)', read(os.path.relpath(program_cs, base_dir)))
            if m and tuple(int(x) for x in m.groups()) != version:
                print("Hinweis: Program.cs hat Version %s.%s.%s, STEFFLIX.exe %d.%d.%d" % (m.groups() + version))
# Android-App: versions.json gegen build.gradle und STEFFLIX.apk (nur Hinweise, release.ps1 schreibt
# versions.json erst nach dieser Prüfung neu)
apk = os.path.join(base_dir, "STEFFLIX.apk")
gradle = os.path.join(base_dir, "SFLIX_Tools", "Android", "app", "build.gradle")
if os.path.exists(apk) and os.path.exists(os.path.join(base_dir, "versions.json")):
    import hashlib
    import json as _json
    try:
        android = _json.loads(read("versions.json")).get("android") or {}
        with open(apk, "rb") as reader:
            apk_sha = hashlib.sha256(reader.read()).hexdigest()
        if android.get("sha256") and android["sha256"].lower() != apk_sha:
            print("Hinweis: versions.json nennt eine andere SHA256 als STEFFLIX.apk (wird beim Release neu geschrieben)")
        if os.path.exists(gradle):
            gm = re.search(r"versionName\s+['\"](\d+\.\d+\.\d+)['\"]", read(os.path.relpath(gradle, base_dir)))
            if gm and android.get("version") and android["version"] != gm.group(1):
                print("Hinweis: versions.json nennt App %s, build.gradle %s" % (android["version"], gm.group(1)))
    except (OSError, ValueError) as e:
        errors.append("versions.json nicht lesbar: %s" % e)
if not os.path.exists(updater_exe):
    errors.append("updateStefflix.exe fehlt (Kopie von STEFFLIX.exe für Updater bis 3.x)")
elif os.path.exists(desktop_exe):
    # Updater bis 3.x laden nur updateStefflix.exe: muss dieselbe Datei sein
    with open(updater_exe, "rb") as a, open(desktop_exe, "rb") as b:
        if a.read() != b.read():
            errors.append("updateStefflix.exe ist nicht dieselbe Datei wie STEFFLIX.exe (build_release.bat kopiert sie)")

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

# 7. Signatur der veröffentlichten checksums.json (DEC-356). Vor dem ersten signierten Release fehlt
#    checksums.sig noch: dann nur ein Hinweis; release.ps1 signiert und prüft danach streng.
from update_signature import check as check_signature
if os.path.exists(os.path.join(base_dir, "checksums.sig")):
    signature_error = check_signature(base_dir)
    if signature_error:
        errors.append(signature_error)
    else:
        print("Signatur von checksums.json gültig")
else:
    print("Hinweis: checksums.sig fehlt noch (wird beim Release erzeugt)")

# 8. Release-Nummer _serial (DEC-372): 14 Ziffern. Die Prüfung läuft vor create_checksums.py; vor dem
#    ersten Release mit Nummer fehlt sie noch: dann nur ein Hinweis (create_checksums.py setzt sie immer).
try:
    serial = json.loads(read("checksums.json")).get("_serial")
    if serial is None:
        print("Hinweis: _serial fehlt in checksums.json noch (wird beim Release erzeugt)")
    elif not (isinstance(serial, str) and len(serial) == 14 and serial.isdigit()):
        errors.append(f"checksums.json: _serial hat nicht genau 14 Ziffern ({serial!r})")
    else:
        print(f"Release-Nummer {serial}")
except (OSError, ValueError, AttributeError):
    errors.append("checksums.json fehlt oder ist kein gültiges JSON")

if errors:
    print("FEHLER:")
    for error in errors:
        print(f"  - {error}")
    sys.exit(1)
print("Prüfung OK")
