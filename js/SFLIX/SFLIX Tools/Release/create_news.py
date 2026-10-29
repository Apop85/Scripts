"""Erzeugt sflix_sys/news.json aus sflix_sys/news.txt.

news.txt bleibt die Quelle (wird von STEFFLIX vor 2.00 direkt gelesen und darum weiter
gepflegt). STEFFLIX ab 2.00 liest news.json. Nach jeder Änderung an news.txt ausführen:
    python create_news.py

Jede Version erhält "name" (x.y.z, ab 2.5.0 das Format von STEFFLIX) und "version" (die alte
Zahl, z.B. "2.05"). STEFFLIX bis 2.04 und der Updater bis 3.2 lesen nur "version".

Die Einträge jeder Version werden nach Typ gruppiert (TYPE_ORDER, innerhalb eines Typs bleibt die
Reihenfolge). Das gilt für news.json und wird auch in news.txt zurückgeschrieben, damit neu angehängte
Einträge zu ihrem Typ wandern.
"""
import html
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from versioning import legacy_number, version_name  # noqa: E402

# Release-Ordner: dieses Skript liegt in <Release-Ordner>\SFLIX Tools\Release
base_dir = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
source =sys.argv[1] if len(sys.argv) > 1 else os.path.join(base_dir, "sflix_sys", "news.txt")
target = sys.argv[2] if len(sys.argv) > 2 else os.path.join(base_dir, "sflix_sys", "news.json")

TYPE_ORDER = ["FEATURE", "INTERFACE", "OPTIMIERUNG", "BUGFIX", "HOTFIX", "BUG"]


def item_type_of(line):
    """Typ eines Eintrags "<li>[...TYP...] Text</li>", sonst None."""
    match = re.match(r"\s*<li>\[(.*?)\]", line)
    return re.sub(r"<[^>]+>", "", match.group(1)).strip().upper() if match else None


def type_rank(item_type):
    return TYPE_ORDER.index(item_type) if item_type in TYPE_ORDER else len(TYPE_ORDER)


def group_by_type(text):
    """Sortiert in jedem Versionsblock die zusammenhängenden <li>-Zeilen nach Typ (stabil).
    Der auskommentierte Block "Bekannte Fehler" bleibt unverändert."""
    lines = text.splitlines(keepends=True)
    out, run, in_version = [], [], False

    def flush():
        run.sort(key=lambda l: type_rank(item_type_of(l)))   # sort ist stabil
        out.extend(run)
        run.clear()

    for line in lines:
        if re.match(r"\s*<b>Version ", line):
            in_version = True
        elif line.lstrip().startswith("<!--") or line.lstrip().startswith("<b>Bekannte Fehler"):
            in_version = False
        if in_version and item_type_of(line) is not None:
            run.append(line)
            continue
        flush()
        out.append(line)
    flush()
    return "".join(out)


# news.txt gruppieren (Zeilenenden bleiben erhalten)
with open(source, encoding="utf-8", newline="") as reader:
    original = reader.read()
grouped = group_by_type(original)
if grouped != original:
    with open(source, "w", encoding="utf-8", newline="") as writer:
        writer.write(grouped)
    print("news.txt nach Typ gruppiert")

result = {"knownBugs": [], "versions": []}
current = None

for line in grouped.splitlines():
    line = line.strip()
    if line.startswith("<b>Bekannte Fehler"):
        current = result["knownBugs"]
        continue
    version = re.match(r"<b>Version ([\d.]+)</b>", line)
    if version:
        name = version_name(version.group(1))
        entry = {"version": legacy_number(name), "name": name, "items": []}
        result["versions"].append(entry)
        current = entry["items"]
        continue
    item = re.match(r"<li>\[(.*?)\]\s*(.*)</li>$", line)
    if item and current is not None:
        item_type = re.sub(r"<[^>]+>", "", item.group(1)).strip().upper()
        raw_text = item.group(2)
        resolved = "<s>" in raw_text
        text = re.sub(r"<[^>]+>", "", raw_text)
        text = re.sub(r"\)?br>$", ")" if raw_text.rstrip().endswith(")br>") else "", text)
        text = html.unescape(text).strip()
        entry = {"type": item_type, "text": text}
        if resolved:
            entry["resolved"] = True
        current.append(entry)

with open(target, "w", encoding="utf-8") as writer:
    json.dump(result, writer, ensure_ascii=False, indent=2)
    writer.write("\n")

types = sorted({i["type"] for v in result["versions"] for i in v["items"]} | {i["type"] for i in result["knownBugs"]})
print(len(result["versions"]), "Versionen,", sum(len(v["items"]) for v in result["versions"]), "Einträge, Typen:", types)
