"""Entscheidet für build_release.bat, ob ein Programm (.exe) neu gebaut werden muss.

PyInstaller erzeugt bei jedem Build eine leicht andere .exe, auch ohne Änderung am Code.
Damit sich die Prüfsummen (und damit die Downloads der Updater) nur bei echten Änderungen
ändern, wird ein Fingerabdruck aus allem gebildet, was in die .exe einfliesst:
  - das Skript und alle eigenen Module, die es (auch indirekt) importiert, z.B. sflix_update.py
  - die Build-Optionen
  - die Versionen von Python, PyInstaller, requests und Pillow
Die Fingerabdrücke des letzten erfolgreichen Builds stehen in build_hashes.json.

Aufruf:
  python build_check.py pruefen   <skript.py> <programm.exe> "<optionen>"
      Exit-Code 0 = neu bauen, 10 = unverändert (nicht bauen), 1 = Fehler
  python build_check.py speichern <skript.py> <programm.exe> "<optionen>"
      nach einem erfolgreichen Build: Fingerabdruck merken
"""
import ast
import hashlib
import json
import os
import sys
from importlib import metadata

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
HASH_FILE = os.path.join(BASE_DIR, "build_hashes.json")
PACKAGES = ["pyinstaller", "requests", "pillow"]


def local_modules(script):
    """Das Skript und alle eigenen Module im Projektordner, die es importiert (rekursiv)."""
    found = []
    pending = [os.path.join(BASE_DIR, script)]
    while pending:
        path = pending.pop()
        if path in found:
            continue
        found.append(path)
        with open(path, encoding="utf-8") as handle:
            tree = ast.parse(handle.read(), filename=path)
        for node in ast.walk(tree):
            names = []
            if isinstance(node, ast.Import):
                names = [alias.name for alias in node.names]
            elif isinstance(node, ast.ImportFrom) and node.module and node.level == 0:
                names = [node.module]
            for name in names:
                candidate = os.path.join(BASE_DIR, name.split(".")[0] + ".py")
                if os.path.isfile(candidate):
                    pending.append(candidate)
    return sorted(found)


def fingerprint(script, options):
    digest = hashlib.sha256()
    for path in local_modules(script):
        with open(path, "rb") as handle:
            # Zeilenenden vereinheitlichen (CRLF/LF ändert nichts am Programm)
            content = handle.read().replace(b"\r\n", b"\n")
        digest.update(os.path.basename(path).encode("utf-8") + b"\0" + content + b"\0")
    digest.update(("options:" + " ".join(options.split())).encode("utf-8"))
    digest.update(("python:%d.%d" % sys.version_info[:2]).encode("utf-8"))
    for package in PACKAGES:
        try:
            version = metadata.version(package)
        except metadata.PackageNotFoundError:
            version = "-"
        digest.update(("%s:%s" % (package, version)).encode("utf-8"))
    return digest.hexdigest()


def load_hashes():
    try:
        with open(HASH_FILE, encoding="utf-8") as handle:
            return json.load(handle)
    except (OSError, ValueError):
        return {}


def main():
    if len(sys.argv) != 5 or sys.argv[1] not in ("pruefen", "speichern"):
        print(__doc__)
        return 1
    action, script, exe, options = sys.argv[1:5]
    try:
        current = fingerprint(script, options)
    except (OSError, SyntaxError) as error:
        print("   Fingerabdruck fuer %s nicht moeglich: %s" % (exe, error))
        return 1

    hashes = load_hashes()
    if action == "pruefen":
        unchanged = hashes.get(exe) == current and os.path.isfile(os.path.join(BASE_DIR, exe))
        return 10 if unchanged else 0

    hashes[exe] = current
    with open(HASH_FILE, "w", encoding="utf-8") as handle:
        json.dump(hashes, handle, indent=2, sort_keys=True)
        handle.write("\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
