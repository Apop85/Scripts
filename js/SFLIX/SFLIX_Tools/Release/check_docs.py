#!/usr/bin/env python3
"""check_docs.py - prüft, ob die Dokumentation zum Projekt passt (ersetzt das Nachführen von Hand).

1. Jede Projektdatei (Quellcode, Werkzeuge, Regelwerk) ist in der Dokumentation genannt
   (.claude/CLAUDE.md und docs/*.md ausser DECISIONS.md) -> sonst FEHLER.
2. In der Dokumentation genannte Dateien (`name.ext`) gibt es -> sonst Hinweis (erzeugte Dateien sind ausgenommen).
3. Dateien, die gepusht werden und Nutzer lesen (READMEs, docs/, news.txt/.json, start.html, HelpContent.cs),
   enthalten keine verbotenen Begriffe (Eastereggs, Discord, Netflix, Webhook-Adressen, Schlüssel, persönliche Daten)
   -> sonst FEHLER.

Aufruf (Projektwurzel):  python SFLIX_Tools\\Release\\check_docs.py [--details]      Exit 1 bei Fehlern.
Genutzt von der Rolle Release (vor jedem Release) und dem Skill /release-vorbereiten.
"""
import argparse
import fnmatch
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# Dokumentation, in der Dateien beschrieben sein müssen
DOC_GLOBS = [".claude/CLAUDE.md", "docs/*.md", "coordination/README.md", "SFLIX_Tools/SflixTools/README.md"]
DOC_EXCLUDE = ["docs/DECISIONS.md"]

# Ordner, die nicht einzeln dokumentiert werden (Build, Laufzeit, Testdaten, Tickets, Archiv)
SKIP_DIRS = [
    ".git", "__pycache__", ".claude/testenv", ".claude/old", ".claude/archiv", "coordination/tasks", "coordination/inbox",
    "coordination/.history", "docs/intern", "tmp",
    "SFLIX_Tools/SflixTools/obj", "SFLIX_Tools/SflixTools/dist", "SFLIX_Tools/PreviewMaker/bin",
    "SFLIX_Tools/Android/app/build", "SFLIX_Tools/Android/build", "SFLIX_Tools/Android/.gradle",
    "SFLIX_Tools/Android/dist", "SFLIX_Tools/Android/signing", "SFLIX_Tools/Android/app/src/main/res",
]
# Einzelne Dateien bzw. Muster ohne eigenen Eintrag
SKIP_FILES = [
    "*.old", "*.new", "*.tmp-*", "*.pyc", ".claude/*.gif", ".claude/*.mp4", ".claude/settings.local.json",
    "SFLIX_Tools/Doku/private_patterns.txt",
    "coordination/TASK-INDEX.md", "coordination/.audit.log", "coordination/.lock",
    "SFLIX_Tools/Bilder/ladehintergrund*.jpg",
]
# Erzeugte oder persönliche Dateien, die im Release-Ordner fehlen dürfen (Prüfung 2)
GENERATED = {
    "install.js", "license.js", "renamed.js", "updater_settings.json", "updater_info.js", "folgen_cache.json",
    "folgen_rueckgaengig.json", "medien_cache.json", "downloads.json", "updater.log", "lizenzen.json",
    "keystore.properties", "stefflix.jks", "STEFFLIX-debug.apk", "STEFFLIX-test.apk", "local.properties",
    "app.ico", "AssetGen.exe", "banner.png", "logo256.png", "stefflix.ico", "version.json", "news.json",
    "TASK-INDEX.md", "DECISIONS-intern.md", "settings.local.json", "make-previews.log", "data.js",
}
# Dateien, die Nutzer lesen und die gepusht werden (Prüfung 3)
USER_GLOBS = ["README.md", "SFLIX_Tools/*/README.md", "SFLIX_Tools/**/README.md", "docs/*.md", "sflix_sys/news.txt",
              "sflix_sys/news.json", "start.html", "SFLIX_Tools/SflixTools/src/HelpContent.cs"]
VERBOTEN = [
    (re.compile(r"(?i)easter\s*-?egg"), "Easteregg"),
    (re.compile(r"(?i)widmung|dedication"), "Widmung (Easteregg)"),
    (re.compile(r"(?i)discord"), "Discord"),
    (re.compile(r"(?i)netflix"), "Netflix"),
    (re.compile(r"(?i)webhooks?/\d{6,}"), "Webhook-Adresse"),
    # Schlüssel sind Base64 (auch Kleinbuchstaben, + und /), rund 80 Zeichen; "SFX2-Schlüssel" im Text ist keiner
    (re.compile(r"SFX2-[A-Za-z0-9+/]{16,}"), "Lizenzschlüssel"),
]

def private_patterns():
    """Persönliche Suchmuster (Benutzername, Teil der Mailadresse) aus der lokalen, nie gepushten Datei
    SFLIX_Tools/Doku/private_patterns.txt (ein regulärer Ausdruck je Zeile, # = Kommentar)."""
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Doku", "private_patterns.txt")
    res = []
    try:
        with open(path, encoding="utf-8") as reader:
            for line in reader:
                line = line.strip()
                if line and not line.startswith("#"):
                    res.append((re.compile("(?i)" + line), "persönliche Daten"))
    except OSError:
        pass
    return res

VERBOTEN += private_patterns()
FILE_RX = re.compile(r"`([^`\s]*?([\w.-]+\.(?:js|html|css|cs|java|py|ps1|bat|json|md|txt|xml|gradle|svg|png|jpg|apk|exe|ico)))`")


def rel(p):
    return os.path.relpath(p, ROOT).replace("\\", "/")


def match_any(path, patterns):
    return any(fnmatch.fnmatch(path, pat) for pat in patterns)


def walk():
    for d, dirs, files in os.walk(ROOT):
        rd = rel(d)
        dirs[:] = [x for x in dirs if not any((rd + "/" + x if rd != "." else x) == s or (rd + "/" + x).startswith(s + "/") for s in SKIP_DIRS)]
        for f in files:
            yield (f if rd == "." else rd + "/" + f)


def read(path):
    with open(os.path.join(ROOT, path), "rb") as fh:
        raw = fh.read()
    return raw.decode("utf-8", errors="replace")


def glob_files(globs, exclude=()):
    allf = list(walk_all())
    res = []
    for g in globs:
        for f in allf:
            if fnmatch.fnmatch(f, g) and f not in exclude and f not in res:
                res.append(f)
    return res


def walk_all():
    for d, dirs, files in os.walk(ROOT):
        rd = rel(d)
        dirs[:] = [x for x in dirs if x not in (".git", "__pycache__", "testenv", "old", "intern", "build", ".gradle", "obj", "tasks", "inbox", ".history")]
        for f in files:
            yield (f if rd == "." else rd + "/" + f)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("--details", action="store_true", help="Hinweise einzeln zeigen")
    a = ap.parse_args()

    docs = glob_files(DOC_GLOBS, DOC_EXCLUDE)
    doctext = "\n".join(read(d) for d in docs)
    low = doctext.lower()
    errors, hints = [], []

    # 1. Projektdateien dokumentiert
    files = [f for f in walk() if not match_any(f, SKIP_FILES)]
    for f in files:
        name = f.rsplit("/", 1)[-1]
        if name.lower() in low or f.lower() in low or f.replace("/", "\\").lower() in low:
            continue
        # Agents werden mit ihrem Namen ohne Endung genannt (z. B. `sflix-explorer`)
        if f.startswith(".claude/agents/") and ("`" + name.rsplit(".", 1)[0].lower() + "`") in low:
            continue
        errors.append("nicht dokumentiert: " + f)

    # 2. Genannte Dateien vorhanden
    names = set(f.rsplit("/", 1)[-1].lower() for f in walk_all())
    for d, dirs, fs in os.walk(ROOT):  # auch Build-Ordner (z. B. dist/STEFFLIX.exe)
        if ".git" in d:
            continue
        for x in fs:
            names.add(x.lower())
    for m in sorted(set(FILE_RX.findall(doctext))):
        nm = m[1]
        if "*" in m[0] or "<" in m[0] or nm in GENERATED or nm.lower() in names:
            continue
        hints.append("genannt, aber nicht vorhanden: " + m[0])

    # 3. Verbotene Begriffe in Dateien für Nutzer
    for f in glob_files(USER_GLOBS):
        if f.startswith("docs/intern/"):
            continue
        for i, line in enumerate(read(f).split("\n")):
            for rx, nm in VERBOTEN:
                if rx.search(line):
                    errors.append("%s Zeile %d: verbotener Begriff (%s)" % (f, i + 1, nm))

    for x in errors:
        print("FEHLER   " + x)
    if a.details:
        for x in hints:
            print("Hinweis  " + x)
    print("check_docs: %d Dateien geprüft, %d Dokumente, %d Fehler, %d Hinweise%s" % (
        len(files), len(docs), len(errors), len(hints), "" if a.details or not hints else " (einzeln mit --details)"))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
