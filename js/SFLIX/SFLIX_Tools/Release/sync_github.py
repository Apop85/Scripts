#!/usr/bin/env python3
"""sync_github.py - spiegelt den Release-Ordner in den lokalen GitHub-Klon (DEC-362, DEC-363).

Nur die Rolle Release, nach einem erfolgreichen Release und nur nach ausdrücklicher Freigabe des
Benutzers. Eigener Schritt, nicht Teil von release.ps1. Kein Git nötig.

Aufruf (Projektwurzel):
  python SFLIX_Tools\\Release\\sync_github.py                 Probelauf: zeigt, was kopiert, ersetzt, gelöscht würde
  python SFLIX_Tools\\Release\\sync_github.py --ausfuehren    ausführen, fragt vorher nach ("ja")
       [--ja]                    Bestätigung schon gegeben (Benutzer hat im Chat zugestimmt)
       [--viele-loeschen-ok]     nötig bei mehr als 20 Löschungen bzw. mehr als 10 % der Zieldateien
       [--ziel PFAD]             Zielordner (Standard: %USERPROFILE%\\Documents\\GitHub\\Scripts\\js\\SFLIX,
                                 oder Umgebungsvariable SFLIX_GITHUB_DIR)
       [--quelle PFAD]           Release-Ordner (Standard: zwei Ebenen über diesem Skript)

Regeln:
  - Übertragen wird jede Datei, die die .gitignore des Projekts nicht ausschliesst (eigene Auswertung:
    Kommentare, !-Ausnahmen, Ordnerregeln mit /, verankerte Pfade, *, ?, [..], **). .gitignore selbst wird übertragen.
  - Kopiert wird nur bei anderem Inhalt (Grösse, dann SHA-256), damit GitHub Desktop nichts Unnötiges zeigt.
  - Im Ziel werden Dateien und leere Ordner gelöscht, die es im Projekt nicht (mehr) gibt.
  - Nie angefasst: .git (auch nicht oberhalb), im Ziel laut .gitignore ignorierte Dateien, alles ausserhalb des
    Zielordners. Verknüpfungen (Junction, symbolischer Link) werden weder verfolgt noch übertragen.
  - Sperrliste unabhängig von .gitignore (geheime und persönliche Dateien): würde eine davon übertragen oder
    enthält eine Textdatei ein persönliches Suchmuster (SFLIX_Tools/Doku/private_patterns.txt), bricht das
    Skript ab, bevor etwas geändert wird.
  - Protokoll: tmp\\sync_github_<Zeit>.log im Projekt.
Exit: 0 = in Ordnung (bzw. nichts zu tun), 1 = Abbruch/Fehler, 2 = Bestätigung fehlt.
"""
import argparse
import datetime
import fnmatch
import hashlib
import os
import re
import shutil
import stat
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_SOURCE = os.path.normpath(os.path.join(HERE, "..", ".."))

# Geheime und persönliche Dateien (gitignore-Muster): nie übertragen, auch wenn .gitignore sie nicht ausschliesst
SPERRLISTE = [
    "sflix_sys/install.js", "sflix_sys/license.js", "sflix_sys/downloads.json", "lizenzen.json",
    "SFLIX_Tools/Android/signing/", "SFLIX_Tools/Release/signing/", "*.jks", "*.keystore", "keystore.properties",
    "update_key*.xml", "coordination/", "docs/intern/", ".claude/testenv/", ".claude/settings.local.json",
    ".claude/old/", ".claude/archiv/", "SFLIX_Tools/Doku/private_patterns.txt", "tmp/",
    # auch Kopien an anderen Orten und Varianten
    "install*.js", "license*.js", "downloads*.json", "lizenzen*.json", "local.properties",
    "*.pfx", "*.p12", "*.pem", "*.snk",
]
MAX_SCAN = 5 * 1024 * 1024   # Inhalt grösserer Dateien nicht nach persönlichen Mustern durchsuchen
FILE_ATTRIBUTE_REPARSE_POINT = 0x400
MANY_DELETES = 20
MANY_DELETES_SHARE = 0.10


def out(s=""):
    sys.stdout.write(s + "\n")
    LOG.append(s)


LOG = []


# ---------------------------------------------------------------- .gitignore
class Rule:
    def __init__(self, pattern, negate, dir_only, anchored, regex):
        self.pattern, self.negate, self.dir_only, self.anchored, self.regex = pattern, negate, dir_only, anchored, regex


def glob_to_regex(pat):
    """gitignore-Muster (ohne führenden / und ohne abschliessenden /) als regulärer Ausdruck über den Pfad."""
    i, n, res = 0, len(pat), ""
    while i < n:
        c = pat[i]
        if pat.startswith("**/", i):
            res += "(?:.*/)?"
            i += 3
        elif pat.startswith("/**", i) and i + 3 == n:
            res += "/.*"
            i += 3
        elif pat.startswith("**", i):
            res += ".*"
            i += 2
        elif c == "*":
            res += "[^/]*"
            i += 1
        elif c == "?":
            res += "[^/]"
            i += 1
        elif c == "[":
            j = pat.find("]", i + 1)
            if j < 0:
                res += re.escape(c)
                i += 1
            else:
                inner = pat[i + 1:j]
                if inner.startswith("!"):
                    inner = "^" + inner[1:]
                res += "[" + inner.replace("\\", "\\\\") + "]"
                i = j + 1
        elif c == "\\" and i + 1 < n:
            res += re.escape(pat[i + 1])
            i += 2
        else:
            res += re.escape(c)
            i += 1
    return res


def parse_rules(lines):
    rules = []
    for raw in lines:
        line = raw.rstrip("\n").rstrip("\r")
        # Abschliessende Leerzeichen entfernen, ausser mit \ geschützt
        while line.endswith(" ") and not line.endswith("\\ "):
            line = line[:-1]
        if not line or line.startswith("#"):
            continue
        negate = False
        if line.startswith("!"):
            negate, line = True, line[1:]
        elif line.startswith("\\!") or line.startswith("\\#"):
            line = line[1:]
        dir_only = line.endswith("/")
        line = line.rstrip("/")
        if not line:
            continue
        anchored = "/" in line
        line = line.lstrip("/")
        regex = re.compile("^" + glob_to_regex(line) + "$", re.IGNORECASE)   # Windows: ohne Gross/Klein
        rules.append(Rule(line, negate, dir_only, anchored, regex))
    return rules


def rule_matches(rule, rel, is_dir):
    if rule.dir_only and not is_dir:
        return False
    target = rel if rule.anchored else rel.rsplit("/", 1)[-1]
    return bool(rule.regex.match(target))


def ignored(rules, rel, is_dir):
    """Letzte passende Regel entscheidet (wie Git). rel: relativer Pfad mit /."""
    result = False
    for r in rules:
        if rule_matches(r, rel, is_dir):
            result = not r.negate
    return result


def ignored_with_parents(rules, rel, is_dir):
    """Ist ein Elternordner ignoriert, ist alles darunter ignoriert (Git schliesst es nie wieder ein)."""
    parts = rel.split("/")
    for k in range(1, len(parts)):
        if ignored(rules, "/".join(parts[:k]), True):
            return True
    return ignored(rules, rel, is_dir)


# ---------------------------------------------------------------- Dateisystem
def is_link(path):
    try:
        st = os.lstat(path)
    except OSError:
        return False
    if stat.S_ISLNK(st.st_mode):
        return True
    return bool(getattr(st, "st_file_attributes", 0) & FILE_ATTRIBUTE_REPARSE_POINT)


def walk(base, rules, skip_git):
    """{relativer Pfad (klein) -> relativer Pfad} aller nicht ignorierten Dateien; Ordner (klein) mit Namen;
    ignorierte und verknüpfte Einträge werden nicht betreten."""
    files, dirs, links = {}, {}, []
    stack = [""]
    while stack:
        rel_dir = stack.pop()
        full_dir = os.path.join(base, rel_dir) if rel_dir else base
        try:
            names = sorted(os.listdir(full_dir))
        except OSError as e:
            raise SystemExit("FEHLER: Ordner nicht lesbar: %s (%s)" % (full_dir, e))
        for name in names:
            rel = rel_dir + "/" + name if rel_dir else name
            full = os.path.join(full_dir, name)
            if skip_git and name.lower() == ".git":
                continue
            if is_link(full):
                links.append(rel)
                continue
            if os.path.isdir(full):
                if ignored_with_parents(rules, rel, True):
                    continue
                dirs[rel.lower()] = rel
                stack.append(rel)
            elif not ignored_with_parents(rules, rel, False):
                files[rel.lower()] = rel
    return files, dirs, links


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()


def same_content(a, b):
    try:
        if os.path.getsize(a) != os.path.getsize(b):
            return False
    except OSError:
        return False
    return sha256(a) == sha256(b)


def find_git(path):
    p = os.path.abspath(path)
    while True:
        if os.path.isdir(os.path.join(p, ".git")) or os.path.isfile(os.path.join(p, ".git")):
            return p
        parent = os.path.dirname(p)
        if parent == p:
            return None
        p = parent


def inside(child, parent):
    c = os.path.normcase(os.path.abspath(child)).rstrip("\\/")
    p = os.path.normcase(os.path.abspath(parent)).rstrip("\\/")
    return c == p or c.startswith(p + os.sep)


def private_patterns(source):
    """Liste der Muster; None, wenn die Datei fehlt oder keine Muster enthält."""
    path = os.path.join(source, "SFLIX_Tools", "Doku", "private_patterns.txt")
    res = []
    try:
        with open(path, encoding="utf-8") as r:
            for line in r:
                line = line.strip()
                if line and not line.startswith("#"):
                    res.append(re.compile(line, re.IGNORECASE))
    except OSError:
        return None
    return res or None


def file_text(path):
    """Inhalt als Text zum Durchsuchen (UTF-16 mit BOM erkannt, sonst UTF-8 bzw. Latin-1 für Binärdateien)."""
    try:
        if os.path.getsize(path) > MAX_SCAN:
            return ""
        with open(path, "rb") as f:
            data = f.read()
    except OSError:
        return ""
    if data[:2] in (b"\xff\xfe", b"\xfe\xff"):
        return data.decode("utf-16", errors="replace")
    text = data.decode("utf-8", errors="replace")
    if b"\0" in data[:8192]:
        # Binärdatei: zusätzlich als Latin-1 und UTF-16-LE (z.B. Pfade in EXE-Dateien)
        text += "\n" + data.decode("latin-1") + "\n" + data.decode("utf-16-le", errors="replace")
    return text


# ---------------------------------------------------------------- Ablauf
def plan(source, target, pats):
    with open(os.path.join(source, ".gitignore"), encoding="utf-8-sig") as r:
        rules = parse_rules(r.readlines())
    src_files, src_dirs, src_links = walk(source, rules, skip_git=True)
    dst_files, dst_dirs, dst_links = walk(target, rules, skip_git=True)

    sperre = parse_rules(SPERRLISTE)
    blocked = [rel for rel in src_files.values() if ignored_with_parents(sperre, rel, False)]
    personal = []
    nested = [rel for rel in src_files.values() if rel.lower().endswith("/.gitignore")]
    hashes = {}
    for rel in src_files.values():
        full = os.path.join(source, rel)
        if pats and (any(p.search(rel) for p in pats) or any(p.search(file_text(full)) for p in pats)):
            personal.append(rel)

    copy, replace, delete, case_fix = [], [], [], []
    for key, rel in sorted(src_files.items()):
        s = os.path.join(source, rel)
        if key not in dst_files:
            copy.append(rel)
            hashes[rel] = sha256(s)
            continue
        drel = dst_files[key]
        if drel != rel:
            case_fix.append((drel, rel))
            hashes[rel] = sha256(s)
        elif not same_content(s, os.path.join(target, drel)):
            replace.append(rel)
            hashes[rel] = sha256(s)
    for key, rel in sorted(dst_files.items()):
        if key not in src_files:
            delete.append(rel)
    # Ordner im Ziel, die es im Projekt nicht gibt (nach dem Löschen leer): tiefste zuerst
    empty_dirs = sorted((rel for key, rel in dst_dirs.items() if key not in src_dirs), key=lambda r: -r.count("/"))
    return {
        "copy": copy, "replace": replace, "delete": delete, "case_fix": case_fix, "empty_dirs": empty_dirs,
        "blocked": blocked, "personal": personal, "src_links": src_links, "dst_links": dst_links,
        "dst_count": len(dst_files), "nested": nested, "hashes": hashes,
    }


def safe_target_path(target, rel):
    """Voller Zielpfad; bricht ab, wenn er ausserhalb des Ziels, in .git oder über eine Verknüpfung führt."""
    full = os.path.abspath(os.path.join(target, rel))
    parts = rel.replace("\\", "/").split("/")
    if not inside(full, target) or any(x.strip(" .").lower() == ".git" for x in parts):
        raise RuntimeError("Pfad ausserhalb des Ziels oder in .git: " + rel)
    cur = target
    for part in parts[:-1]:
        cur = os.path.join(cur, part)
        if os.path.lexists(cur) and is_link(cur):
            raise RuntimeError("Pfad führt im Ziel über eine Verknüpfung: " + rel)
    if os.path.lexists(full) and is_link(full):
        raise RuntimeError("Ziel ist eine Verknüpfung: " + rel)
    parent = os.path.dirname(full)
    if os.path.exists(parent) and not inside(os.path.realpath(parent), os.path.realpath(target)):
        raise RuntimeError("Pfad zeigt aufgelöst ausserhalb des Ziels: " + rel)
    return full


def write_checked(src, dst, expected, target, remove_first=None):
    """Liest die Quelle einmal, prüft den Hash aus dem Probelauf (Datei inzwischen geändert -> Abbruch) und
    schreibt genau diese Bytes über eine Zwischendatei. remove_first: alte Datei (andere Schreibweise),
    erst nach erfolgreicher Zwischendatei entfernt."""
    with open(src, "rb") as f:
        data = f.read()
    if hashlib.sha256(data).hexdigest() != expected:
        raise RuntimeError("Datei hat sich seit der Prüfung geändert, bitte erneut starten: " + src)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if not inside(os.path.realpath(os.path.dirname(dst)), os.path.realpath(target)):
        raise RuntimeError("Zielordner zeigt aufgelöst ausserhalb des Ziels: " + dst)
    tmp = dst + ".sync-tmp"
    with open(tmp, "wb") as f:
        f.write(data)
    shutil.copystat(src, tmp)
    if remove_first:
        os.chmod(remove_first, stat.S_IWRITE)
        os.remove(remove_first)
    if os.path.exists(dst):
        os.chmod(dst, stat.S_IWRITE)
    os.replace(tmp, dst)


def execute(p, source, target):
    for rel in p["copy"] + p["replace"]:
        write_checked(os.path.join(source, rel), safe_target_path(target, rel), p["hashes"][rel], target)
        out("  %s  %s" % ("NEU     " if rel in p["copy"] else "ERSETZT ", rel))
    for old, new in p["case_fix"]:
        old_full, new_full = safe_target_path(target, old), safe_target_path(target, new)
        if os.path.dirname(old).lower() == os.path.dirname(new).lower() and os.path.dirname(old) != os.path.dirname(new):
            out("  Hinweis: Ordner im Ziel in anderer Schreibweise, bitte von Hand angleichen: %s -> %s" % (old, new))
        write_checked(os.path.join(source, new), new_full, p["hashes"][new], target, remove_first=old_full)
        out("  SCHREIBUNG  %s -> %s" % (old, new))
    for rel in p["delete"]:
        full = safe_target_path(target, rel)
        os.chmod(full, stat.S_IWRITE)
        os.remove(full)
        out("  GELÖSCHT  " + rel)
    for rel in p["empty_dirs"]:
        full = safe_target_path(target, rel)
        try:
            if not os.listdir(full):
                os.rmdir(full)
                out("  ORDNER GELÖSCHT  " + rel)
        except OSError:
            pass   # enthält noch ignorierte Dateien: bleibt


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser(description="Release-Ordner in den GitHub-Klon spiegeln (Probelauf ohne --ausfuehren)")
    default_target = os.environ.get("SFLIX_GITHUB_DIR") or os.path.join(
        os.environ.get("USERPROFILE", os.path.expanduser("~")), "Documents", "GitHub", "Scripts", "js", "SFLIX")
    ap.add_argument("--ziel", default=default_target)
    ap.add_argument("--quelle", default=DEFAULT_SOURCE)
    ap.add_argument("--ausfuehren", action="store_true")
    ap.add_argument("--ja", action="store_true")
    ap.add_argument("--viele-loeschen-ok", dest="viele", action="store_true")
    ap.add_argument("--protokoll", default=None, help="Protokolldatei (Standard: tmp im Projekt)")
    ap.add_argument("--ohne-suchmuster", dest="ohne_suchmuster", action="store_true",
                    help="ohne private_patterns.txt spiegeln (nur ausnahmsweise)")
    a = ap.parse_args()
    # Aufgelöst (Junctions, Symlinks, Kurznamen), damit die Prüfungen unten nicht umgangen werden
    if is_link(os.path.abspath(a.ziel)):
        out("FEHLER: Zielordner ist eine Verknüpfung: " + a.ziel)
        return 1
    source, target = os.path.realpath(a.quelle), os.path.realpath(a.ziel)

    # Prüfungen vor dem Start
    if not os.path.isfile(os.path.join(source, "start.html")) or not os.path.isfile(os.path.join(source, "versions.json")) \
            or not os.path.isfile(os.path.join(source, ".gitignore")):
        out("FEHLER: Quelle ist kein Release-Ordner (start.html, versions.json, .gitignore): " + source)
        return 1
    if not os.path.isdir(target):
        out("FEHLER: Zielordner fehlt: " + target)
        return 1
    if inside(target, source) or inside(source, target):
        out("FEHLER: Quelle und Ziel dürfen nicht ineinander liegen")
        return 1
    git_root = find_git(target)
    if not git_root:
        out("FEHLER: Ziel liegt in keinem Git-Klon (.git in keinem Elternordner): " + target)
        return 1
    if os.path.dirname(git_root.rstrip("\\/")) == git_root.rstrip("\\/") or os.path.splitdrive(git_root)[1] in ("", "\\", "/"):
        out("FEHLER: Git-Klon ist eine Laufwerkswurzel: " + git_root)
        return 1
    # Ziel plausibel: leer oder schon ein Stand von STEFFLIX
    try:
        target_entries = [x for x in os.listdir(target) if x.lower() != ".git"]
    except OSError as e:
        out("FEHLER: Zielordner nicht lesbar: %s" % e)
        return 1
    if target_entries and not (os.path.isfile(os.path.join(target, "start.html")) or os.path.isfile(os.path.join(target, ".gitignore"))):
        out("FEHLER: Zielordner ist weder leer noch ein STEFFLIX-Stand (start.html bzw. .gitignore fehlt): " + target)
        return 1
    # Weitere Ignore-Quellen des Klons werden nicht ausgewertet: dann lieber abbrechen
    exclude = os.path.join(git_root, ".git", "info", "exclude")
    try:
        with open(exclude, encoding="utf-8", errors="replace") as r:
            if any(l.strip() and not l.strip().startswith("#") for l in r):
                out("FEHLER: %s enthält Regeln, die sync_github.py nicht auswertet. Bitte in die .gitignore des Projekts übernehmen." % exclude)
                return 1
    except OSError:
        pass
    pats = private_patterns(source)
    if pats is None and not a.ohne_suchmuster:
        out("FEHLER: SFLIX_Tools/Doku/private_patterns.txt fehlt oder ist leer: ohne persönliche Suchmuster wird nicht gespiegelt "
            "(Ausnahme: --ohne-suchmuster).")
        return 1
    out("Quelle: " + source)
    out("Ziel:   " + target + "  (Klon: " + git_root + ")")

    p = plan(source, target, pats)
    if p["nested"]:
        out("ABBRUCH: weitere .gitignore unterhalb der Wurzel werden nicht ausgewertet: " + ", ".join(p["nested"]))
        write_log(source, a.protokoll)
        return 1
    if p["blocked"] or p["personal"]:
        out("ABBRUCH: geheime bzw. persönliche Dateien würden übertragen (nichts geändert):")
        for rel in p["blocked"]:
            out("  Sperrliste: " + rel)
        for rel in p["personal"]:
            out("  persönliches Suchmuster: " + rel)
        out("Diese Dateien in .gitignore eintragen bzw. bereinigen.")
        write_log(source, a.protokoll)
        return 1

    n_changes = len(p["copy"]) + len(p["replace"]) + len(p["delete"]) + len(p["case_fix"])
    out("Kopieren: %d   Ersetzen: %d   Löschen: %d   Schreibweise: %d   leere Ordner: %d"
        % (len(p["copy"]), len(p["replace"]), len(p["delete"]), len(p["case_fix"]), len(p["empty_dirs"])))
    for rel in p["copy"]:
        out("  neu       " + rel)
    for rel in p["replace"]:
        out("  ersetzen  " + rel)
    for old, new in p["case_fix"]:
        out("  Schreibweise  %s -> %s" % (old, new))
    for rel in p["delete"]:
        out("  löschen   " + rel)
    for rel in p["empty_dirs"]:
        out("  Ordner löschen (falls leer)  " + rel)
    for rel in p["src_links"]:
        out("  Hinweis: Verknüpfung im Projekt nicht übertragen: " + rel)
    for rel in p["dst_links"]:
        out("  Hinweis: Verknüpfung im Ziel unverändert: " + rel)

    many = len(p["delete"]) > MANY_DELETES or (p["dst_count"] and len(p["delete"]) > p["dst_count"] * MANY_DELETES_SHARE)
    if many:
        out("WARNUNG: %d Löschungen (%d Dateien im Ziel). Ist das wirklich gewollt?" % (len(p["delete"]), p["dst_count"]))

    if n_changes == 0 and not p["empty_dirs"]:
        out("Nichts zu tun: Ziel ist auf dem Stand des Projekts.")
        write_log(source, a.protokoll)
        return 0
    if not a.ausfuehren:
        out("Probelauf: nichts geändert. Ausführen mit --ausfuehren (nach Freigabe des Benutzers).")
        write_log(source, a.protokoll)
        return 0
    if many and not a.viele:
        out("Abgebrochen: bei so vielen Löschungen zusätzlich --viele-loeschen-ok angeben.")
        write_log(source, a.protokoll)
        return 2
    if not a.ja:
        try:
            answer = input("Ausführen? Mit 'ja' bestätigen: ").strip().lower()
        except EOFError:
            answer = ""
        if answer != "ja":
            out("Abgebrochen: keine Bestätigung.")
            write_log(source, a.protokoll)
            return 2
        if many:
            try:
                answer = input("Wirklich %d Dateien im Ziel löschen? 'ja': " % len(p["delete"])).strip().lower()
            except EOFError:
                answer = ""
            if answer != "ja":
                out("Abgebrochen: Löschungen nicht bestätigt.")
                write_log(source, a.protokoll)
                return 2
    try:
        execute(p, source, target)
    except Exception as e:
        out("ABBRUCH mitten im Lauf: %s (bereits Erledigtes steht oben; ein neuer Lauf setzt fort)" % e)
        write_log(source, a.protokoll)
        return 1
    out("Fertig: %d Änderungen im Ziel. Danach in GitHub Desktop prüfen, committen und pushen." % n_changes)
    write_log(source, a.protokoll)
    return 0


def write_log(source, path):
    if not path:
        folder = os.path.join(source, "tmp")
        os.makedirs(folder, exist_ok=True)
        path = os.path.join(folder, "sync_github_%s.log" % datetime.datetime.now().strftime("%Y%m%d-%H%M%S"))
    try:
        with open(path, "w", encoding="utf-8", newline="\n") as w:
            w.write("\n".join(LOG) + "\n")
        sys.stdout.write("Protokoll: " + path + "\n")
    except OSError:
        pass


if __name__ == "__main__":
    sys.exit(main())
