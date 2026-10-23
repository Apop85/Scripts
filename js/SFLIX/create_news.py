"""Erzeugt sflix_sys/news.json aus sflix_sys/news.txt.

news.txt bleibt die Quelle (wird von STEFFLIX vor 2.00 direkt gelesen und darum weiter
gepflegt). STEFFLIX ab 2.00 liest news.json. Nach jeder Änderung an news.txt ausführen:
    python create_news.py
"""
import html
import json
import os
import re
import sys

base_dir = os.path.dirname(os.path.abspath(__file__))
source = sys.argv[1] if len(sys.argv) > 1 else os.path.join(base_dir, "sflix_sys", "news.txt")
target = sys.argv[2] if len(sys.argv) > 2 else os.path.join(base_dir, "sflix_sys", "news.json")

result = {"knownBugs": [], "versions": []}
current = None

for line in open(source, encoding="utf-8"):
    line = line.strip()
    if line.startswith("<b>Bekannte Fehler"):
        current = result["knownBugs"]
        continue
    version = re.match(r"<b>Version ([\d.]+)</b>", line)
    if version:
        entry = {"version": version.group(1), "items": []}
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
