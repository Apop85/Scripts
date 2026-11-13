"""Erzeugt checksums.json für alle Dateien, die setup und die Updater herunterladen.

Wird von build_release.bat aufgerufen.
checksums.json muss zusammen mit den Programmen ins Repository eingecheckt werden.
"""
import json
import os
import sys

from sflix_update import CHECKSUM_FILE, FILES, OBSOLETE_FILES, RELEASE_DIR, WEB_FILES, checksum

# Ab Updater 3.4.1: Listen der Programmdateien und der veralteten Dateien. Der Updater nimmt sie
# statt seiner eingebauten Listen (mit Schutzregeln, siehe Net.IsAllowedWebPath). Ältere Updater
# lesen nur die Prüfsummen und übergehen diese Einträge.
WEB_FILES_KEY = "_webFiles"
OBSOLETE_FILES_KEY = "_obsoleteFiles"

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

files_count = len(result)
result[WEB_FILES_KEY] = sorted(WEB_FILES)
result[OBSOLETE_FILES_KEY] = sorted(OBSOLETE_FILES)

with open(os.path.join(base_dir, CHECKSUM_FILE), "w", encoding="utf-8") as file_writer:
    json.dump(result, file_writer, indent=2, sort_keys=True)
    file_writer.write("\n")

print(f"\n{CHECKSUM_FILE} geschrieben ({files_count} Dateien, {len(WEB_FILES)} Programmdateien, "
      f"{len(OBSOLETE_FILES)} veraltete)")
if missing:
    print("WARNUNG - folgende Dateien fehlen und haben keine Prüfsumme:")
    for path in missing:
        print(f"  {path}")
    sys.exit(1)
