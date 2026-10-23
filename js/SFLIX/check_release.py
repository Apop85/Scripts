"""Prüft vor dem Bauen eines Releases, ob alles zusammenpasst. Bricht mit Fehlercode 1 ab,
wenn etwas fehlt. Wird von build_release.bat aufgerufen."""
import os
import re
import sys

from sflix_update import FILES, OBSOLETE_FILES, SCRIPT_FILES

base_dir = os.path.dirname(os.path.abspath(__file__))
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

# 2. Alle Dateien der Weboberfläche vorhanden (die Programme werden erst gebaut)
for path in FILES:
    if not path.endswith(".exe") and not os.path.exists(os.path.join(base_dir, path)):
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

if errors:
    print("FEHLER:")
    for error in errors:
        print(f"  - {error}")
    sys.exit(1)
print("Prüfung OK")
