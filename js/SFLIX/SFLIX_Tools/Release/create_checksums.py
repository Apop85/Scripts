"""Erzeugt checksums.json für alle Dateien, die setup und die Updater herunterladen.

Wird von release.ps1 mit --schreiben aufgerufen. Ohne --schreiben werden die Prüfsummen nur
angezeigt (checksums.json bleibt unverändert, sonst passt die Signatur checksums.sig nicht mehr).
checksums.json muss zusammen mit den Programmen ins Repository eingecheckt werden.
"""
import argparse
import json
import os
import sys
from datetime import datetime, timezone

from sflix_update import CHECKSUM_FILE, FILES, OBSOLETE_FILES, RELEASE_DIR, WEB_FILES, checksum

sys.stdout.reconfigure(encoding="utf-8")
# Keine Abkürzungen: ein Tippfehler wie --schreib darf nicht wie --schreiben wirken (SFLIX-20261123-090)
parser = argparse.ArgumentParser(description="Prüfsummen der Release-Dateien berechnen (checksums.json).", allow_abbrev=False)
parser.add_argument("--schreiben", action="store_true",
                    help="checksums.json im Release-Ordner schreiben (nur im Release-Ablauf, danach neu signieren)")
args = parser.parse_args()

# Ab Updater 3.4.1: Listen der Programmdateien und der veralteten Dateien. Der Updater nimmt sie
# statt seiner eingebauten Listen (mit Schutzregeln, siehe Net.IsAllowedWebPath). Ältere Updater
# lesen nur die Prüfsummen und übergehen diese Einträge.
WEB_FILES_KEY = "_webFiles"
OBSOLETE_FILES_KEY = "_obsoleteFiles"
# DEC-372: Release-Nummer (14 Ziffern, UTC JJJJMMTThhmmss), streng steigend und mitsigniert. Der Desktop
# installiert keinen Stand mit kleinerer Nummer als der schon angenommenen.
SERIAL_KEY = "_serial"


def next_serial(previous_file, content):
    """Zeitpunkt jetzt (UTC); ist die bisherige Nummer gleich hoch oder höher, deren Wert plus 1.
    Ist der Inhalt ohne Nummer unverändert, bleibt die bisherige Nummer (gleicher Stand, keine neue
    Signatur und nichts zu pushen)."""
    serial = int(datetime.now(timezone.utc).strftime("%Y%m%d%H%M%S"))
    try:
        with open(previous_file, encoding="utf-8") as reader:
            previous = json.load(reader)
        old = previous.pop(SERIAL_KEY, None)
    except (OSError, ValueError, AttributeError, TypeError):
        previous, old = None, None
    if isinstance(old, str) and len(old) == 14 and old.isdigit():
        if previous == content:
            return old
        if int(old) >= serial:
            serial = int(old) + 1
    return f"{serial:014d}"

# checksums.json liegt im Release-Ordner, nicht neben diesem Skript
base_dir = RELEASE_DIR
result = {}
missing = []

for path in sorted(FILES):
    full_path = os.path.join(base_dir, path)
    if not os.path.exists(full_path):
        missing.append(path)
        continue
    with open(full_path, "rb") as file_reader:
        result[path] = checksum(path, file_reader.read())
    print(f"{path:<30} {result[path]}")

# Erst prüfen, dann schreiben: fehlt eine Datei, bleibt die bisherige checksums.json unverändert
if missing:
    print("FEHLER - folgende Dateien fehlen, checksums.json wurde nicht geschrieben:")
    for path in missing:
        print(f"  {path}")
    sys.exit(1)

files_count = len(result)
result[WEB_FILES_KEY] = sorted(WEB_FILES)
result[OBSOLETE_FILES_KEY] = sorted(OBSOLETE_FILES)
result[SERIAL_KEY] = next_serial(os.path.join(base_dir, CHECKSUM_FILE), dict(result))
print(f"{SERIAL_KEY:<30} {result[SERIAL_KEY]}")

if not args.schreiben:
    print(f"\nNur angezeigt ({files_count} Dateien): {CHECKSUM_FILE} wurde nicht geschrieben (dazu --schreiben).")
    sys.exit(0)

# LF wie auf GitHub (Signatur und Prüfsummen vereinheitlichen die Zeilenenden ohnehin)
with open(os.path.join(base_dir, CHECKSUM_FILE), "w", encoding="utf-8", newline="\n") as file_writer:
    json.dump(result, file_writer, indent=2, sort_keys=True)
    file_writer.write("\n")

print(f"\n{CHECKSUM_FILE} geschrieben ({files_count} Dateien, {len(WEB_FILES)} Programmdateien, "
      f"{len(OBSOLETE_FILES)} veraltete)")
