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
       [--zeige-bereinigt]       bereinigte Stellen unter .claude/ und docs/ vorher/nachher zeigen
       [--erzwingen [PFAD ...]]  vorhandene Dateien im Ziel auch bei gleichem Inhalt ersetzen (ohne PFAD alle,
                                 sonst nur darunter, z. B. --erzwingen docs/ .claude/)

Regeln:
  - Übertragen wird jede Datei, die die .gitignore des Projekts nicht ausschliesst (eigene Auswertung:
    Kommentare, !-Ausnahmen, Ordnerregeln mit /, verankerte Pfade, *, ?, [..], **). .gitignore selbst wird übertragen.
  - Kopiert wird nur bei anderem Inhalt (Grösse, dann SHA-256), damit GitHub Desktop nichts Unnötiges zeigt;
    --erzwingen ersetzt vorhandene Dateien trotzdem (alle oder nur unter den genannten Pfaden).
  - Im Ziel werden Dateien und leere Ordner gelöscht, die es im Projekt nicht (mehr) gibt.
  - Nie angefasst: .git (auch nicht oberhalb), im Ziel laut .gitignore ignorierte Dateien, alles ausserhalb des
    Zielordners. Verknüpfungen (Junction, symbolischer Link) werden weder verfolgt noch übertragen.
  - Sperrliste unabhängig von .gitignore (geheime und persönliche Dateien): würde eine davon übertragen oder
    enthält eine Textdatei ein persönliches Suchmuster (SFLIX_Tools/Doku/private_patterns.txt), bricht das
    Skript ab, bevor etwas geändert wird.
  - Bereinigung (DEC-394): Dateien unter .claude/ und docs/ werden nur in der Kopie im Ziel bereinigt, die
    Originale im Projekt bleiben unverändert. Regeln (Zeile entfernen, Begriff ersetzen, verbotene Begriffe) in
    SFLIX_Tools/Doku/public_redactions.txt (nie übertragen); fehlt sie, werden diese Ordner nicht gespiegelt.
    Zusätzlich: Benutzername aus %USERNAME% (Pfad C:\\Users\\<Name> -> %USERPROFILE%, sonst <Benutzer>) und
    IP-Adressen ausser 127.0.0.1 und 0.0.0.0 (-> <IP>; vierteilige Versionsnummern nach "Version" bleiben).
    Danach wird der bereinigte Inhalt erneut geprüft; bleibt etwas übrig, bricht das Skript vor jeder Änderung
    ab. Regeln, die in keiner Datei greifen, ergeben eine Warnung. Vergleich und Hash gelten für den bereinigten
    Inhalt. Der Probelauf zeigt die Anzahl Stellen je Datei, --zeige-bereinigt jede Stelle vorher/nachher
    (nur lokal in Ausgabe und Protokoll).
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
    "sflix_sys/damaged.js", "sflix_sys/player.log",
    "SFLIX_Tools/Android/signing/", "SFLIX_Tools/Release/signing/", "*.jks", "*.keystore", "keystore.properties",
    "update_key*.xml", "*.dpapi", "*.dpapi.neu", "coordination/", "docs/intern/", ".claude/testenv/", ".claude/settings.local.json",
    ".claude/old/", ".claude/archiv/", ".claude/*.gif", ".claude/*.mp4", "SFLIX_Tools/Doku/private_patterns.txt", "tmp/",
    "public_redactions*.txt", "private_patterns*.txt", "DECISIONS-intern*.md",
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


# Private Schlüssel im Inhalt (immer geprüft, auch mit --ohne-suchmuster; SFLIX-20261123-079): PEM-Block und
# RSA-Schlüssel als XML (.NET ToXmlString(true), privater Exponent <D>). So geschrieben, dass diese Datei selbst
# nicht passt.
KEY_PATTERNS = [
    re.compile(r"-{5}BEGIN (?:RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-{5}"),
    re.compile(r"<D>[A-Za-z0-9+/=\s]{40,}</D>"),
]


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


# ---------------------------------------------------------------- Bereinigung (DEC-394)
# Nur die Kopien unter diesen Ordnern werden bereinigt; die Originale im Projekt bleiben unverändert,
# alle übrigen Dateien werden 1:1 übertragen.
REDACT_PREFIXES = (".claude/", "docs/")
REDACT_RULES_FILE = os.path.join("SFLIX_Tools", "Doku", "public_redactions.txt")
IP_KEEP = ("127.0.0.1", "0.0.0.0")
_OCTET = r"(?:25[0-5]|2[0-4]\d|[01]?\d?\d)"   # auch mit führender Null (192.168.001.005)
IPV4 = re.compile(r"(?<![\w.])" + _OCTET + r"(?:\." + _OCTET + r"){3}(?![\w]|\.\d)")
IPV6_FULL = re.compile(r"(?<![\w:])(?:[0-9A-Fa-f]{1,4}:){7}[0-9A-Fa-f]{1,4}(?![\w:])")
IPV6_SHORT = re.compile(r"(?<![\w:])(?:[0-9A-Fa-f]{1,4}(?::[0-9A-Fa-f]{1,4})*)?::(?:[0-9A-Fa-f]{1,4}(?::[0-9A-Fa-f]{1,4})*)?(?![\w:])")
# Versionsnummern mit vier Teilen in AssemblyVersion("4.0.7.0") bzw. AssemblyFileVersion("…") sind keine IP-Adressen
VERSION_BEFORE = re.compile(r"Version\(\s*\"$")


def needs_redaction(rel):
    low = rel.replace("\\", "/").lower()
    return any(low.startswith(p) for p in REDACT_PREFIXES)


class Redactions:
    """Regeln aus public_redactions.txt plus Benutzername (%USERNAME%) und IP-Adressen."""

    def __init__(self, source):
        self.remove, self.replace, self.forbidden = [], [], []
        self.loaded = False
        path = os.path.join(source, REDACT_RULES_FILE)
        try:
            with open(path, encoding="utf-8-sig") as r:
                for no, line in enumerate(r, 1):
                    line = line.rstrip("\r\n")
                    if not line.strip() or line.lstrip().startswith("#"):
                        continue
                    kind, body = line[:1], line[2:] if line[1:2] == " " else None
                    if body is None or kind not in "-=!":
                        raise SystemExit("FEHLER: %s Zeile %d: unbekanntes Format" % (REDACT_RULES_FILE, no))
                    if kind == "=":
                        m = re.match(r"^(.*?)\s->(?:\s(.*))?$", body)
                        if not m or not m.group(1):
                            raise SystemExit("FEHLER: %s Zeile %d: Ersetzen braucht 'Muster -> Ersatz'" % (REDACT_RULES_FILE, no))
                        self.replace.append([re.compile(m.group(1), re.IGNORECASE), m.group(2) or "", no, 0])
                    elif kind == "-":
                        self.remove.append([re.compile(body, re.IGNORECASE), None, no, 0])
                    else:
                        self.forbidden.append(re.compile(body, re.IGNORECASE))
            self.loaded = True
        except OSError:
            pass
        except re.error as e:
            raise SystemExit("FEHLER: ungültiges Muster in %s: %s" % (REDACT_RULES_FILE, e))
        # Benutzername und Name des Profilordners (können abweichen, z. B. Microsoft-Konto)
        names = [(os.environ.get("USERNAME") or "").strip(),
                 os.path.basename((os.environ.get("USERPROFILE") or "").rstrip("\\/")).strip()]
        names = sorted({n for n in names if n}, key=lambda n: -len(n))
        long_names = [n for n in names if len(n) >= 3]
        self.user_short = len(long_names) < len(names)
        self.user_path = self.user_any = None
        if names:
            # C:\Users\<Name> (auch mit / oder \\ wie in JSON) -> %USERPROFILE%, auch bei kurzen Namen
            self.user_path = re.compile(r"[A-Za-z]:(\\\\|\\|/)Users\1(?:" + "|".join(re.escape(n) for n in names) +
                                        r")(?=\\|/|\W|$)", re.IGNORECASE)
        if long_names:
            self.user_any = re.compile("|".join(re.escape(n) for n in long_names), re.IGNORECASE)

    @staticmethod
    def _ip_hits(line):
        hits = []
        for m in IPV4.finditer(line):
            if m.group(0) in IP_KEEP or VERSION_BEFORE.search(line[max(0, m.start() - 12):m.start()]):
                continue
            hits.append(m.span())
        for rx in (IPV6_FULL, IPV6_SHORT):
            for m in rx.finditer(line):
                groups = [g for g in m.group(0).split(":") if g]
                if rx is IPV6_SHORT and (len(groups) < 2 or not re.search(r"\d", m.group(0))):
                    continue   # ::, ::1 und Ähnliches sowie Bezeichner wie Bad::Add bleiben
                hits.append(m.span())
        return sorted(set(hits))

    def _replace_ips(self, line):
        hits = self._ip_hits(line)
        for start, end in reversed(hits):
            line = line[:start] + "<IP>" + line[end:]
        return line, len(hits)

    def clean(self, text):
        """(bereinigter Text, Änderungen [(Zeilennummer, vorher, nachher oder None)])."""
        result, changes = [], []
        for no, full_line in enumerate(text.splitlines(True), 1):
            # Zeilenende abtrennen, damit $ und .* bei CRLF wie bei LF wirken
            line = full_line.rstrip("\r\n")
            ending = full_line[len(line):]
            removed = False
            for rule in self.remove:
                if rule[0].search(line):
                    rule[3] += 1
                    removed = True
            if removed:
                changes.append((no, line, None))
                continue
            new = line
            for rule in self.replace:
                new, n = rule[0].subn(rule[1].replace("\\", "\\\\"), new)
                rule[3] += n
            if self.user_path is not None:
                new = self.user_path.sub(lambda m: "%USERPROFILE%", new)
            if self.user_any is not None:
                new = self.user_any.sub("<Benutzer>", new)
            new, _ = self._replace_ips(new)
            if new != line:
                changes.append((no, line, new))
            result.append(new + ending)
        return "".join(result), changes

    def leftovers(self, text):
        """Was nach der Bereinigung nicht mehr vorkommen darf: Liste von Beschreibungen. Auch die Muster der
        Regeln 'Zeile entfernen' und 'Begriff ersetzen' dürfen nicht mehr greifen."""
        found = []
        if any(rx.search(text) for rx in self.forbidden):
            found.append("verbotener Begriff (Regeldatei)")
        lines = text.splitlines()
        if any(rule[0].search(line) for rule in self.remove + self.replace for line in lines):
            found.append("Regel greift nach der Bereinigung noch (Regeldatei)")
        if (self.user_any is not None and self.user_any.search(text)) or \
                (self.user_path is not None and self.user_path.search(text)):
            found.append("Benutzername")
        if any(self._ip_hits(line) for line in lines):
            found.append("IP-Adresse")
        return found

    def unused(self):
        return [rule[2] for rule in self.remove + self.replace if rule[3] == 0]


def redact_bytes(data, red):
    """Bereinigte Bytes und Änderungen. Nur UTF-8-Text bis MAX_SCAN lässt sich bereinigen; Binärdateien
    (NUL, z. B. UTF-16, Bilder, PDF) und grössere Dateien unter den bereinigten Ordnern -> RuntimeError (Abbruch)."""
    if len(data) > MAX_SCAN:
        raise RuntimeError("grösser als %d MB, wird nicht bereinigt" % (MAX_SCAN // (1024 * 1024)))
    if b"\0" in data:
        raise RuntimeError("Binärdatei oder UTF-16, wird nicht bereinigt")
    bom = data.startswith(b"\xef\xbb\xbf")
    try:
        text = data[3:].decode("utf-8") if bom else data.decode("utf-8")
    except UnicodeDecodeError:
        raise RuntimeError("nicht als UTF-8 lesbar, kann nicht bereinigt werden")
    cleaned, changes = red.clean(text)
    out_data = cleaned.encode("utf-8")
    return (b"\xef\xbb\xbf" + out_data if bom else out_data), changes


# ---------------------------------------------------------------- Ablauf
def forced(force, rel):
    """--erzwingen: None = aus, [] = alle Dateien, sonst Pfade (Ordner oder Datei, ohne Gross/Klein)."""
    if force is None:
        return False
    low = rel.lower()
    return not force or any(low == f or low.startswith(f + "/") for f in force)


def plan(source, target, pats, red, force=None):
    with open(os.path.join(source, ".gitignore"), encoding="utf-8-sig") as r:
        rules = parse_rules(r.readlines())
    src_files, src_dirs, src_links = walk(source, rules, skip_git=True)
    dst_files, dst_dirs, dst_links = walk(target, rules, skip_git=True)

    sperre = parse_rules(SPERRLISTE)
    blocked = [rel for rel in src_files.values() if ignored_with_parents(sperre, rel, False)]
    personal = []
    nested = [rel for rel in src_files.values() if rel.lower().endswith("/.gitignore")]
    hashes = {}
    # Bereinigte Kopien (DEC-394): rel -> (Bytes, Änderungen); geprüft wird der bereinigte Inhalt
    redacted, leftover = {}, []
    for rel in src_files.values():
        full = os.path.join(source, rel)
        scan = None
        if needs_redaction(rel):
            with open(full, "rb") as f:
                data = f.read()
            try:
                cleaned, changes = redact_bytes(data, red)
            except RuntimeError as e:
                leftover.append((rel, str(e)))
                continue
            redacted[rel] = (cleaned, changes)
            scan = cleaned.decode("utf-8", errors="replace")
            for what in red.leftovers(scan):
                leftover.append((rel, what))
        else:
            scan = file_text(full)
        if pats and (any(p.search(rel) for p in pats) or any(p.search(scan) for p in pats)):
            personal.append(rel)

    def source_hash(rel):
        if rel in redacted:
            return hashlib.sha256(redacted[rel][0]).hexdigest()
        return sha256(os.path.join(source, rel))

    def unchanged(rel, dst):
        if rel not in redacted:
            return same_content(os.path.join(source, rel), dst)
        data = redacted[rel][0]
        try:
            if os.path.getsize(dst) != len(data):
                return False
        except OSError:
            return False
        return sha256(dst) == hashlib.sha256(data).hexdigest()

    copy, replace, delete, case_fix = [], [], [], []
    for key, rel in sorted(src_files.items()):
        if key not in dst_files:
            copy.append(rel)
            hashes[rel] = source_hash(rel)
            continue
        drel = dst_files[key]
        if drel != rel:
            case_fix.append((drel, rel))
            hashes[rel] = source_hash(rel)
        elif forced(force, rel) or not unchanged(rel, os.path.join(target, drel)):
            replace.append(rel)
            hashes[rel] = source_hash(rel)
    for key, rel in sorted(dst_files.items()):
        if key not in src_files:
            delete.append(rel)
    # Ordner im Ziel, die es im Projekt nicht gibt (nach dem Löschen leer): tiefste zuerst
    empty_dirs = sorted((rel for key, rel in dst_dirs.items() if key not in src_dirs), key=lambda r: -r.count("/"))
    return {
        "copy": copy, "replace": replace, "delete": delete, "case_fix": case_fix, "empty_dirs": empty_dirs,
        "blocked": blocked, "personal": personal, "src_links": src_links, "dst_links": dst_links,
        "dst_count": len(dst_files), "nested": nested, "hashes": hashes,
        "redacted": redacted, "leftover": leftover,
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


def write_checked(src, dst, expected, target, remove_first=None, red=None):
    """Liest die Quelle einmal, prüft den Hash aus dem Probelauf (Datei inzwischen geändert -> Abbruch) und
    schreibt genau diese Bytes über eine Zwischendatei. remove_first: alte Datei (andere Schreibweise),
    erst nach erfolgreicher Zwischendatei entfernt. red: Datei vorher bereinigen (DEC-394); der Hash
    gilt dann für den bereinigten Inhalt."""
    with open(src, "rb") as f:
        data = f.read()
    if red is not None:
        data = redact_bytes(data, red)[0]
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


def execute(p, source, target, red):
    for rel in p["copy"] + p["replace"]:
        write_checked(os.path.join(source, rel), safe_target_path(target, rel), p["hashes"][rel], target,
                      red=red if rel in p["redacted"] else None)
        out("  %s  %s" % ("NEU     " if rel in p["copy"] else "ERSETZT ", rel))
    for old, new in p["case_fix"]:
        old_full, new_full = safe_target_path(target, old), safe_target_path(target, new)
        if os.path.dirname(old).lower() == os.path.dirname(new).lower() and os.path.dirname(old) != os.path.dirname(new):
            out("  Hinweis: Ordner im Ziel in anderer Schreibweise, bitte von Hand angleichen: %s -> %s" % (old, new))
        write_checked(os.path.join(source, new), new_full, p["hashes"][new], target, remove_first=old_full,
                      red=red if new in p["redacted"] else None)
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
    ap.add_argument("--erzwingen", nargs="*", default=None, metavar="PFAD",
                    help="vorhandene Dateien im Ziel auch bei gleichem Inhalt ersetzen; ohne PFAD alle, "
                         "sonst nur unter diesen Pfaden (z. B. docs/ .claude/)")
    ap.add_argument("--zeige-bereinigt", dest="zeige_bereinigt", action="store_true",
                    help="bereinigte Stellen unter .claude/ und docs/ mit vorher/nachher zeigen (nur lokal)")
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
    pats = (pats or []) + KEY_PATTERNS
    out("Quelle: " + source)
    out("Ziel:   " + target + "  (Klon: " + git_root + ")")

    red = Redactions(source)
    if red.user_short:
        out("WARNUNG: Benutzer- bzw. Profilname kürzer als 3 Zeichen: dafür werden nur Pfade C:\\Users\\<Name> "
            "ersetzt und geprüft, nicht jedes Vorkommen.")
    # Originalzeilen (--zeige-bereinigt) nur im Probelauf und das Protokoll nie im Ziel (sonst käme es in den Klon)
    if a.zeige_bereinigt and a.ausfuehren:
        out("FEHLER: --zeige-bereinigt nur im Probelauf (ohne --ausfuehren).")
        return 1
    if a.protokoll and (inside(a.protokoll, git_root) or
                        inside(os.path.realpath(os.path.dirname(os.path.abspath(a.protokoll))), git_root)):
        out("FEHLER: Protokoll darf nicht im GitHub-Klon liegen: " + a.protokoll)
        return 1
    force = None
    if a.erzwingen is not None:
        force = [x.replace("\\", "/").strip("/").lower() for x in a.erzwingen if x.strip("\\/ ")]
        out("Erzwingen: vorhandene Dateien werden ersetzt (%s)" % (", ".join(force) if force else "alle"))
    p = plan(source, target, pats, red, force)
    if p["nested"]:
        out("ABBRUCH: weitere .gitignore unterhalb der Wurzel werden nicht ausgewertet: " + ", ".join(p["nested"]))
        write_log(source, a.protokoll)
        return 1
    if p["redacted"] and not red.loaded:
        out("ABBRUCH: %s fehlt: Dateien unter %s werden ohne Regeln nicht gespiegelt (nichts geändert)."
            % (REDACT_RULES_FILE.replace("\\", "/"), " und ".join(REDACT_PREFIXES)))
        write_log(source, a.protokoll)
        return 1
    if p["leftover"]:
        out("ABBRUCH: nach der Bereinigung ist noch Sensibles vorhanden (nichts geändert):")
        for rel, what in p["leftover"]:
            out("  %s: %s" % (rel, what))
        out("Regeln in %s ergänzen oder die Datei anpassen." % REDACT_RULES_FILE.replace("\\", "/"))
        write_log(source, a.protokoll)
        return 1
    changed_files = [(rel, ch) for rel, (data, ch) in sorted(p["redacted"].items()) if ch]
    if changed_files:
        out("Bereinigt (nur die Kopie, %s): %d Dateien" % (" und ".join(REDACT_PREFIXES), len(changed_files)))
        for rel, ch in changed_files:
            out("  %-60s %d Stellen" % (rel, len(ch)))
            if a.zeige_bereinigt:
                for no, before, after in ch:
                    out("    Zeile %d vorher:  %s" % (no, before.rstrip("\r\n")))
                    out("    Zeile %d nachher: %s" % (no, "(entfernt)" if after is None else after.rstrip("\r\n")))
    for no in red.unused():
        out("WARNUNG: Regel in Zeile %d von %s greift in keiner Datei mehr." % (no, REDACT_RULES_FILE.replace("\\", "/")))
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
        execute(p, source, target, red)
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
