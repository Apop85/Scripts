"""Erzeugt checksums.json für alle Dateien, die setup und die Updater herunterladen.

Wird von build_release.bat aufgerufen.
checksums.json muss zusammen mit den Programmen ins Repository eingecheckt werden.
"""
import json
import os
import sys

from sflix_update import CHECKSUM_FILE, FILES, checksum

base_dir = os.path.dirname(os.path.abspath(__file__))
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

with open(os.path.join(base_dir, CHECKSUM_FILE), "w", encoding="utf-8") as file_writer:
    json.dump(result, file_writer, indent=2, sort_keys=True)
    file_writer.write("\n")

print(f"\n{CHECKSUM_FILE} geschrieben ({len(result)} Dateien)")
if missing:
    print("WARNUNG - folgende Dateien fehlen und haben keine Prüfsumme:")
    for path in missing:
        print(f"  {path}")
    sys.exit(1)
