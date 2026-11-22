#!/usr/bin/env python3
"""news.py - Changelog sflix_sys/news.txt lesen und pflegen (statt von Hand).

Warum: news.txt wird immer länger; Einträge brauchen ein festes Format (eine <li>-Zeile je Eintrag, Typ, Präfix
"Desktop x.y.z:" bzw. "Android-App x.y.z:" mit der Version der Komponente), sonst überliest create_news.py sie.

Aufruf (Projektwurzel):  python SFLIX_Tools\\Release\\news.py <Befehl> [Optionen]      Hilfe: ... <Befehl> -h

  zeigen [x.y.z]                         Einträge eines Blocks kurz (Standard: oberster Block)
  suchen <Wort> [<Wort> ...] [--alle]    Einträge mit allen Wörtern (Standard: oberster Block, --alle: ganze Datei)
  eintrag --typ FEATURE|INTERFACE|OPTIMIERUNG|BUGFIX --komponente stefflix|desktop|app --text "..."
          [--version x.y.z] [--trotzdem]  neuer Eintrag im obersten Block; Komponentenversion aus Program.cs bzw.
                                          app/build.gradle; ähnlicher Eintrag vorhanden -> Exit 3 (anpassen statt doppeln)
  anpassen --alt "Teiltext" --text "neuer Text"   bestehenden Eintrag im obersten Block ersetzen (Typ/Präfix bleiben)
  version x.y.z                          neuen Block <b>Version x.y.z</b> oben anlegen (version.js setzt /version-erhoehen)
  check                                  Format, Typen, Präfixe, oberste Version = version.js, verbotene Begriffe

Die Reihenfolge im Block ist egal (create_news.py gruppiert beim Release nach Typ).
"""
import argparse
import html
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
NEWS = os.environ.get("SFLIX_NEWS_FILE") or os.path.join(ROOT, "sflix_sys", "news.txt")
VERSION_JS = os.path.join(ROOT, "sflix_sys", "version.js")
PROGRAM_CS = os.path.join(ROOT, "SFLIX_Tools", "SflixTools", "src", "Program.cs")
BUILD_GRADLE = os.path.join(ROOT, "SFLIX_Tools", "Android", "app", "build.gradle")

TYPEN = {
    "FEATURE": '[<span style="color: red">FEATURE</span>]',
    "INTERFACE": '[<span style="color: blue">INTERFACE</span>]',
    "OPTIMIERUNG": "[OPTIMIERUNG]",
    "BUGFIX": '[<span style="color: darkgreen">BUGFIX</span>]',
}
ALLE_TYPEN = ["FEATURE", "INTERFACE", "OPTIMIERUNG", "BUGFIX", "HOTFIX", "stabil", "BUG"]
PRAEFIX = {"stefflix": "", "desktop": "Desktop", "app": "Android-App"}
BLOCK_RX = re.compile(r"^<b>Version (\d+\.\d+\.\d+)</b>")
LI_RX = re.compile(r"^\s*<li>(.*)</li>\s*$")
VERBOTEN = [
    (re.compile(r"(?i)discord"), "Discord"),
    (re.compile(r"(?i)easter\s*-?egg|widmung|dedication"), "Easteregg"),
    (re.compile(r"(?i)netflix"), "Netflix"),
    (re.compile(r"(?i)webhooks?/\d{6,}"), "Webhook-Adresse"),
]


def out(s=""):
    sys.stdout.write(s + "\n")


def fehler(msg, code=1):
    sys.stderr.write("FEHLER: " + msg + "\n")
    sys.exit(code)


class News:
    def __init__(self):
        with open(NEWS, "rb") as f:
            raw = f.read()
        self.bom = raw.startswith(b"\xef\xbb\xbf")
        text = raw[3:].decode("utf-8") if self.bom else raw.decode("utf-8")
        self.nl = "\r\n" if "\r\n" in text else "\n"
        self.lines = text.replace("\r\n", "\n").split("\n")

    def save(self):
        text = self.nl.join(self.lines)
        data = (b"\xef\xbb\xbf" if self.bom else b"") + text.encode("utf-8")
        tmp = NEWS + ".tmp-%d" % os.getpid()
        with open(tmp, "wb") as f:
            f.write(data)
        os.replace(tmp, NEWS)

    def blocks(self):
        """[(version, kopf_zeile, ul_start, ul_ende)] - ul_ende = Zeile mit </ul>."""
        res = []
        for i, l in enumerate(self.lines):
            m = BLOCK_RX.match(l.strip())
            if not m:
                continue
            ul = next((k for k in range(i + 1, min(i + 4, len(self.lines))) if self.lines[k].strip().startswith("<ul>")), None)
            if ul is None:
                continue
            end = next((k for k in range(ul + 1, len(self.lines)) if self.lines[k].strip().startswith("</ul>")), None)
            if end is None:
                continue
            res.append((m.group(1), i, ul, end))
        return res

    def block(self, version=None):
        bl = self.blocks()
        if not bl:
            fehler("Kein Block <b>Version x.y.z</b> in news.txt")
        if not version:
            return bl[0]
        for b in bl:
            if b[0] == version:
                return b
        fehler("Block nicht gefunden: " + version)

    def items(self, b):
        _, _, ul, end = b
        res = []
        for k in range(ul + 1, end):
            m = LI_RX.match(self.lines[k])
            if m:
                res.append((k, m.group(1)))
        return res


def plain(li):
    t = re.sub(r"<[^>]+>", "", li)
    return re.sub(r"\s+", " ", t).strip()


def words(s):
    return set(w for w in re.split(r"[^a-zäöüß0-9]+", s.lower()) if len(w) >= 4)


def komponentenversion(k):
    if k == "desktop":
        m = re.search(r'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)', open(PROGRAM_CS, encoding="utf-8-sig").read())
        return "%s.%s.%s" % m.groups() if m else None
    if k == "app":
        m = re.search(r'versionName\s+["\'](\d+\.\d+\.\d+)["\']', open(BUILD_GRADLE, encoding="utf-8-sig").read())
        return m.group(1) if m else None
    return None


def version_js():
    m = re.search(r'versionName\s*=\s*"(\d+\.\d+\.\d+)"', open(VERSION_JS, encoding="utf-8-sig").read())
    return m.group(1) if m else None


def cmd_zeigen(a):
    n = News()
    b = n.block(a.version)
    out("Version %s (%d Einträge):" % (b[0], len(n.items(b))))
    for _, li in n.items(b):
        out("  - " + plain(li)[:240])


def cmd_suchen(a):
    n = News()
    ws = [w.lower() for w in a.woerter]
    bl = n.blocks() if a.alle else [n.block()]
    hits = 0
    for b in bl:
        for _, li in n.items(b):
            p = plain(li)
            if all(w in p.lower() for w in ws):
                out("%s  %s" % (b[0], p[:240]))
                hits += 1
    if not hits:
        out("Keine Treffer.")


def cmd_eintrag(a):
    n = News()
    b = n.block()
    typ = a.typ.upper()
    if typ not in TYPEN:
        fehler("Typ ungültig: %s (FEATURE, INTERFACE, OPTIMIERUNG, BUGFIX)" % a.typ)
    k = a.komponente.lower()
    if k in ("android", "android-app"):
        k = "app"
    if k == "updater":
        k = "desktop"
    if k not in PRAEFIX:
        fehler("Komponente ungültig: %s (stefflix, desktop, app)" % a.komponente)
    # HTML-Sonderzeichen kodieren ("<Medien>" verschwände sonst in news.json, "&" bliebe roh)
    text = html.escape(re.sub(r"\s*[\r\n]+\s*", " ", a.text).strip(), quote=False)
    if re.match(r"^(Desktop|Updater|Android-App)\s+\d", text):
        fehler("Text ohne Präfix angeben; das Präfix setzt --komponente")
    for rx, name in VERBOTEN:
        if rx.search(text):
            fehler("Verbotener Begriff im Changelog: " + name)
    if k != "stefflix":
        v = a.version or komponentenversion(k)
        if not v:
            fehler("Komponentenversion nicht gefunden; --version x.y.z angeben")
        text = "%s %s: %s" % (PRAEFIX[k], v, text)
    neu_w = words(text)
    for _, li in n.items(b):
        alt_w = words(plain(li))
        if neu_w and alt_w and len(neu_w & alt_w) / len(neu_w | alt_w) >= 0.5 and not a.trotzdem:
            fehler("Ähnlicher Eintrag im Block %s (anpassen statt doppeln: news.py anpassen --alt ...):\n  %s" % (b[0], plain(li)[:300]), 3)
    line = "    <li>%s %s</li>" % (TYPEN[typ], text)
    n.lines.insert(b[3], line)
    n.save()
    out("Eingetragen in Version %s: %s" % (b[0], plain(line)[:200]))


def cmd_anpassen(a):
    n = News()
    b = n.block()
    hits = [(k, li) for k, li in n.items(b) if a.alt.lower() in plain(li).lower()]
    if not hits:
        fehler("Kein Eintrag im Block %s enthält: %s" % (b[0], a.alt))
    if len(hits) > 1:
        fehler("Mehrere Einträge passen, Teiltext genauer angeben:\n  " + "\n  ".join(plain(h[1])[:160] for h in hits))
    k, li = hits[0]
    m = re.match(r"^(\[[^\]]*?(?:</span>)?\]\s*)((?:Desktop|Updater|Android-App)\s+\d+\.\d+\.\d+:\s*)?", li)
    kopf = m.group(1) if m else ""
    pre = (m.group(2) or "") if m else ""
    text = html.escape(re.sub(r"\s*[\r\n]+\s*", " ", a.text).strip(), quote=False)
    for rx, name in VERBOTEN:
        if rx.search(text):
            fehler("Verbotener Begriff im Changelog: " + name)
    n.lines[k] = "    <li>%s%s%s</li>" % (kopf, pre, text)
    n.save()
    out("Angepasst: " + plain(n.lines[k])[:200])


def cmd_version(a):
    if not re.fullmatch(r"\d+\.\d+\.\d+", a.version):
        fehler("Version im Format x.y.z angeben")
    n = News()
    if any(b[0] == a.version for b in n.blocks()):
        out("Block %s gibt es schon." % a.version)
        return
    top = n.block()
    n.lines[top[1]:top[1]] = ["<b>Version %s</b>:<br>" % a.version, "<ul>", "</ul>"]
    n.save()
    out("Block Version %s angelegt (version.js: /version-erhoehen bzw. versioning.version_js_text)." % a.version)


def cmd_check(a):
    n = News()
    err, warn = [], []
    bl = n.blocks()
    if not bl:
        err.append("kein Block <b>Version x.y.z</b>")
    else:
        vj = version_js()
        if vj and bl[0][0] != vj:
            err.append("oberster Block %s ungleich version.js %s" % (bl[0][0], vj))
        seen = set()
        for b in bl:
            if b[0] in seen:
                err.append("Block %s doppelt" % b[0])
            seen.add(b[0])
        for b in bl[:3]:
            for k in range(b[2] + 1, b[3]):
                l = n.lines[k].strip()
                if not l:
                    continue
                if l.count("<li>") > 1 or (l.startswith("<li>") and not l.endswith("</li>")):
                    err.append("Zeile %d: jede <li>-Zeile einzeln und vollständig auf eigener Zeile" % (k + 1))
                    continue
                m = LI_RX.match(n.lines[k])
                if not m:
                    if not l.startswith("<!--"):
                        warn.append("Zeile %d: keine <li>-Zeile im Block %s" % (k + 1, b[0]))
                    continue
                p = plain(m.group(1))
                tm = re.match(r"^\[([A-Za-z]+)\]", p)
                if not tm or tm.group(1) not in ALLE_TYPEN:
                    err.append("Zeile %d: Typ fehlt oder unbekannt: %s" % (k + 1, p[:60]))
                if re.search(r"\]\s*(Desktop|Updater|Android-App)(?!\s+\d+\.\d+\.\d+:)", p):
                    err.append("Zeile %d: Präfix ohne Version x.y.z:" % (k + 1))
                rest = re.sub(r"^\[[A-Za-z]+\]\s*", "", p)
                gueltig = re.match(r"(Desktop|Updater|Android-App) (\d+\.\d+\.\d+):", rest)
                # Tippfehler im Präfix ("Desktp 4.0.1:", "desktop 4.0.1:", "Android App 1.3.2:") gingen still an STEFFLIX
                if not gueltig and re.match(r"(?i)\W*(desk|andr|app\b|upd)[\w-]*\s*\d+\.\d+(\.\d+)?\s*:", rest):
                    err.append("Zeile %d: ungültiges Komponenten-Präfix (Desktop x.y.z: bzw. Android-App x.y.z:): %s" % (k + 1, rest[:40]))
                if gueltig and b is bl[0]:
                    komp = "app" if gueltig.group(1) == "Android-App" else "desktop"
                    aktuell = komponentenversion(komp)
                    if aktuell and tuple(map(int, gueltig.group(2).split("."))) > tuple(map(int, aktuell.split("."))):
                        err.append("Zeile %d: %s %s ist höher als die Version im Quellcode (%s)" % (k + 1, gueltig.group(1), gueltig.group(2), aktuell))
            if "</ul>" in n.lines[b[3]] and n.lines[b[3]].strip() != "</ul>":
                err.append("Zeile %d: </ul> auf eigener Zeile" % (b[3] + 1))
    for i, l in enumerate(n.lines):
        for rx, name in VERBOTEN:
            if rx.search(l):
                err.append("Zeile %d: verbotener Begriff (%s)" % (i + 1, name))
    for x in err:
        out("FEHLER   " + x)
    for x in warn:
        out("Hinweis  " + x)
    out("check: %d Blöcke, %d Fehler, %d Hinweise (geprüft: Format der obersten 3 Blöcke, Begriffe überall)" % (len(bl), len(err), len(warn)))
    sys.exit(1 if err else 0)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser(description="Changelog sflix_sys/news.txt pflegen.",
                                 formatter_class=argparse.RawDescriptionHelpFormatter, epilog=__doc__.split("\n\n", 2)[2])
    sub = ap.add_subparsers(dest="befehl")
    p = sub.add_parser("zeigen"); p.add_argument("version", nargs="?"); p.set_defaults(f=cmd_zeigen)
    p = sub.add_parser("suchen"); p.add_argument("woerter", nargs="+"); p.add_argument("--alle", action="store_true"); p.set_defaults(f=cmd_suchen)
    p = sub.add_parser("eintrag"); p.add_argument("--typ", required=True); p.add_argument("--komponente", required=True)
    p.add_argument("--text", required=True); p.add_argument("--version"); p.add_argument("--trotzdem", action="store_true")
    p.set_defaults(f=cmd_eintrag)
    p = sub.add_parser("anpassen"); p.add_argument("--alt", required=True); p.add_argument("--text", required=True); p.set_defaults(f=cmd_anpassen)
    p = sub.add_parser("version"); p.add_argument("version"); p.set_defaults(f=cmd_version)
    p = sub.add_parser("check"); p.set_defaults(f=cmd_check)
    a = ap.parse_args()
    if not a.befehl:
        ap.print_help()
        return 1
    a.f(a)
    return 0


if __name__ == "__main__":
    sys.exit(main())
