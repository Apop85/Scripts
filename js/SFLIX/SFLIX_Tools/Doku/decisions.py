#!/usr/bin/env python3
"""decisions.py - Entscheidungen von STEFFLIX lesen und pflegen (docs/DECISIONS.md, intern: docs/intern/DECISIONS-intern.md).

Warum: DECISIONS.md ist sehr gross (ganz gelesen rund 100k Tokens). Rollen lesen deshalb nur einzelne Einträge über
dieses Skript und schreiben neue Einträge nur hierüber (gleiches Format, fortlaufende Nummer, Prüfung).

Aufruf (Projektwurzel):  python SFLIX_Tools\\Doku\\decisions.py <Befehl> [Optionen]     Hilfe: ... <Befehl> -h

Lesen
  zeigen DEC-123 [DEC-124 ...]          einzelne Einträge vollständig
  suchen <Wort> [<Wort> ...]            Einträge, die alle Wörter enthalten (Titel, Felder); --status, --kategorie, --voll
  baum DEC-123 [--rueckwaerts]          Abhängigkeitsbaum (DEC-123 -> DEC-100 -> DEC-033 ...); rückwärts: wer baut darauf auf
  bloecke                               Blöcke (Sitzungen/Versionen) mit Nummernbereich
Schreiben
  neu --titel ... --entscheidung ... --begruendung ... [--kategorie] [--status] [--ticket] [--alternativen]
      [--konsequenzen] [--risiken] [--komponenten] [--abhaengigkeiten] [--block "STEFFLIX 2.8.8 / Desktop 4.0.6"]
      [--thema "Überschrift"] [--intern]
  ergaenzen DEC-123 --feld Konsequenzen --text "..." [--ersetzen]
  status DEC-123 Implemented
  ersetzen DEC-alt --durch DEC-neu       alter Eintrag Superseded + "Ersetzt durch", neuer nennt den alten unter Abhängigkeiten
  offen | aenderung | doku | risiko --text "..." [--intern]   Punkt in Abschnitt A-D des aktuellen Blocks
  intern DEC-123 [...]                  Einträge in die interne Datei verschieben (Eastereggs, nie öffentlich)
  index                                 Statistik oben in DECISIONS.md neu berechnen
Prüfen
  check                                 Pflichtfelder, Status, Verweise, doppelte Nummern, verbotene Begriffe (Exit 1 bei Fehlern)

Gemeinsame Nummerierung über beide Dateien. Ausgabe UTF-8. Schreibt atomar (Temp-Datei, dann ersetzen).
"""
import argparse
import datetime
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
# Test: SFLIX_DOCS_DIR=<Kopie von docs> arbeitet auf einer Kopie
DOCS = os.environ.get("SFLIX_DOCS_DIR") or os.path.join(ROOT, "docs")
PUBLIC = os.path.join(DOCS, "DECISIONS.md")
INTERN = os.path.join(DOCS, "intern", "DECISIONS-intern.md")

STATUS = ["Proposed", "Accepted", "Implemented", "Rejected", "Superseded"]
KATEGORIEN = ["Architektur", "UI", "Desktop", "Android", "Performance", "Sicherheit", "Datenintegrität",
              "Release", "Arbeitsweise", "Sonstiges"]
FELDER = ["Status", "Datum", "Kategorie", "Ticket", "Entscheidung", "Begründung", "Alternativen", "Konsequenzen",
          "Risiken", "Betroffene Komponenten", "Abhängigkeiten"]
PFLICHT = ["Status", "Datum", "Kategorie", "Entscheidung"]
ABSCHNITTE = {
    "offen": "## A. Offene Entscheidungen",
    "aenderung": "## B. Änderungsbedarf an bestehenden Entscheidungen",
    "doku": "## C. Dokumentationsbedarf",
    "risiko": "## D. Technische Risiken",
}
# In der öffentlichen Datei verboten (CLAUDE.md: Eastereggs und Discord nie erwähnen, keine persönlichen Daten)
VERBOTEN = [
    (re.compile(r"(?i)easter\s*-?egg"), "Easteregg"),
    (re.compile(r"(?i)widmung|dedication|getSplashDedication"), "Widmung (Easteregg)"),
    (re.compile(r"(?i)discord"), "Discord"),
    (re.compile(r"(?i)webhooks?/\d{6,}"), "Webhook-Adresse"),
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

ENTRY_RX = re.compile(r"^### (DEC-\d{3,}):\s*(.*?)\s*$")
FIELD_RX = re.compile(r"^([A-ZÄÖÜ][A-Za-zÄÖÜäöüß ()-]{1,40}?):\s?(.*)$")
DEC_RX = re.compile(r"\bDEC-\d{3,}\b")


def out(s=""):
    sys.stdout.write(s + "\n")


def fehler(msg, code=1):
    sys.stderr.write("FEHLER: " + msg + "\n")
    sys.exit(code)


def heute():
    return datetime.date.today().isoformat()


class Doc:
    def __init__(self, path, intern=False):
        self.path = path
        self.intern = intern
        self.nl = "\n"
        self.lines = []
        if os.path.exists(path):
            with open(path, "rb") as f:
                raw = f.read()
            if raw.startswith(b"\xef\xbb\xbf"):
                raw = raw[3:]
            text = raw.decode("utf-8")
            if "\r\n" in text:
                self.nl = "\r\n"
            self.lines = text.replace("\r\n", "\n").split("\n")
            if self.lines and self.lines[-1] == "":
                self.lines.pop()
        elif intern:
            self.lines = ["# STEFFLIX – interne Entscheidungen (nie pushen)", "",
                          "Nur Einträge, die nicht öffentlich sein dürfen (z. B. Eastereggs). Gleiches Format wie "
                          "`docs/DECISIONS.md`, gemeinsame Nummerierung. Pflege nur über `SFLIX_Tools\\Doku\\decisions.py --intern`.", ""]

    def save(self):
        os.makedirs(os.path.dirname(self.path), exist_ok=True)
        if not self.intern:
            update_stats(self)   # Kopf-Statistik immer aktuell halten
        text = self.nl.join(self.lines) + self.nl
        tmp = self.path + ".tmp-%d" % os.getpid()
        with open(tmp, "wb") as f:
            f.write(text.encode("utf-8"))
        os.replace(tmp, self.path)

    # ---------- Struktur
    def entries(self):
        res = []
        n = len(self.lines)
        i = 0
        while i < n:
            m = ENTRY_RX.match(self.lines[i])
            if not m:
                i += 1
                continue
            j = i + 1
            while j < n and not self.lines[j].startswith("#") and self.lines[j].strip() != "---":
                j += 1
            end = j
            while end > i + 1 and self.lines[end - 1].strip() == "":
                end -= 1
            res.append(Entry(self, m.group(1), m.group(2), i, end))
            i = j
        return res

    def blocks(self):
        """Level-1-Überschriften (# ...) = Blöcke; [(titel, start, ende)]."""
        idx = [i for i, l in enumerate(self.lines) if l.startswith("# ")]
        res = []
        for k, s in enumerate(idx):
            e = idx[k + 1] if k + 1 < len(idx) else len(self.lines)
            res.append((self.lines[s][2:].strip(), s, e))
        return res


class Entry:
    def __init__(self, doc, id_, titel, start, end):
        self.doc, self.id, self.titel, self.start, self.end = doc, id_, titel, start, end

    @property
    def num(self):
        return int(self.id.split("-")[1])

    def body(self):
        return self.doc.lines[self.start:self.end]

    def fields(self):
        """{Name: (zeile_start, zeile_ende, wert)} - Folgezeilen gehören zum vorigen Feld."""
        res = {}
        cur = None
        for i in range(self.start + 1, self.end):
            line = self.doc.lines[i]
            m = FIELD_RX.match(line)
            if m:
                cur = m.group(1).strip()
                res[cur] = [i, i, m.group(2).strip()]
            elif cur and line.strip():
                res[cur][1] = i
                res[cur][2] += " " + line.strip()
        return {k: tuple(v) for k, v in res.items()}

    def get(self, name):
        f = self.fields().get(name)
        return f[2] if f else ""

    def deps(self):
        return sorted(set(DEC_RX.findall(self.get("Abhängigkeiten"))), key=lambda x: int(x.split("-")[1]))

    def text(self):
        return "\n".join(self.body())


class Store:
    def __init__(self):
        self.pub = Doc(PUBLIC)
        self.int = Doc(INTERN, intern=True)

    def all(self):
        return [(e, False) for e in self.pub.entries()] + [(e, True) for e in self.int.entries()]

    def find(self, id_):
        id_ = norm_id(id_)
        hits = [(e, i) for e, i in self.all() if e.id == id_]
        if not hits:
            fehler("Eintrag nicht gefunden: " + id_)
        if len(hits) > 1:
            fehler("Eintrag doppelt vorhanden: " + id_ + " (check ausführen)")
        return hits[0]

    def next_id(self):
        nums = [e.num for e, _ in self.all()]
        return "DEC-%03d" % ((max(nums) if nums else 0) + 1)


def norm_id(s):
    s = s.strip().upper()
    if re.fullmatch(r"\d{1,5}", s):
        s = "DEC-%03d" % int(s)
    if not re.fullmatch(r"DEC-\d{3,}", s):
        fehler("Ungültige ID: " + s + " (z. B. DEC-123 oder 123)")
    return s


# ---------------------------------------------------------------- Blöcke und Abschnitte
def ensure_block(doc, block):
    """Liefert (start, ende) des aktuellen Blocks; legt mit --block einen neuen an, wenn der letzte anders heisst."""
    bl = doc.blocks()
    if block:
        # Gleichnamigen Block wiederverwenden (wechseln sich Rollen ab, entstanden sonst doppelte Überschriften)
        same = [b for b in bl if b[0].split(" (Arbeit vom")[0].strip() == block.strip()]
        if same:
            return same[-1][1], same[-1][2]
    if block and (not bl or not bl[-1][0].startswith(block)):
        datum = datetime.date.today().strftime("%d.%m.%Y")
        while doc.lines and doc.lines[-1].strip() == "":
            doc.lines.pop()
        neu = ["", "---", "", "# %s (Arbeit vom %s)" % (block, datum), "", "## Entscheidungen", ""]
        for k in ("offen", "aenderung", "doku", "risiko"):
            neu += [ABSCHNITTE[k], ""]
        if not doc.lines:
            neu = neu[3:]
        doc.lines += neu
        while doc.lines and doc.lines[-1] == "":
            doc.lines.pop()
        bl = doc.blocks()
    if not bl:
        fehler("Datei hat keinen Block (# Überschrift); --block angeben")
    return bl[-1][1], bl[-1][2]


def section_range(doc, bstart, bend, heading):
    """(start, ende) des Abschnitts mit Überschrift heading im Block; None wenn fehlt."""
    for i in range(bstart, bend):
        if doc.lines[i].strip() == heading:
            j = i + 1
            # Einträge (### DEC-...) gehören zum Abschnitt; er endet erst an der nächsten Überschrift der Ebene 1 oder 2
            while j < bend and not re.match(r"^#{1,2} ", doc.lines[j]) and doc.lines[j].strip() != "---":
                j += 1
            return i, j
    return None


def first_ad_heading(doc, bstart, bend):
    for i in range(bstart, bend):
        if re.match(r"^## [ABCD]\. ", doc.lines[i]):
            return i
    return None


def insert_lines(doc, pos, new):
    doc.lines[pos:pos] = new


def append_to_section(doc, bstart, bend, heading, new_lines, before_ad=False):
    rng = section_range(doc, bstart, bend, heading)
    if rng is None:
        pos = first_ad_heading(doc, bstart, bend) if before_ad else None
        if pos is None:
            pos = bend
            while pos > bstart and doc.lines[pos - 1].strip() in ("", "---"):
                pos -= 1
            insert_lines(doc, pos, ["", heading, ""] + new_lines)
        else:
            insert_lines(doc, pos, [heading, ""] + new_lines + [""])
        return
    s, e = rng
    end = e
    while end > s + 1 and doc.lines[end - 1].strip() == "":
        end -= 1
    block = [""] + new_lines   # Leerzeile nach der Überschrift bzw. zwischen zwei Einträgen
    if end > s + 1 and new_lines and new_lines[0].startswith("- ") and doc.lines[end - 1].startswith("- "):
        block = new_lines      # Aufzählung fortsetzen
    insert_lines(doc, end, block)
    # Leerzeile vor der nächsten Überschrift sicherstellen
    after = end + len(block)
    if after < len(doc.lines) and doc.lines[after].strip() != "":
        insert_lines(doc, after, [""])


# ---------------------------------------------------------------- Befehle: lesen
def cmd_zeigen(a):
    st = Store()
    for i, id_ in enumerate(a.ids):
        e, intern = st.find(id_)
        if i:
            out("")
        out(e.text() + ("\n[intern: docs/intern/DECISIONS-intern.md]" if intern else ""))


def cmd_suchen(a):
    st = Store()
    words = [w.lower() for w in a.woerter]
    n = 0
    for e, intern in st.all():
        t = e.text().lower()
        if not all(w in t for w in words):
            continue
        if a.status and e.get("Status").lower() != a.status.lower():
            continue
        if a.kategorie and not e.get("Kategorie").lower().startswith(a.kategorie.lower()):
            continue
        n += 1
        if a.voll:
            out(e.text() + ("\n[intern]" if intern else ""))
            out("")
        else:
            out("%s  %-11s  %-14s  %s%s" % (e.id, e.get("Status"), e.get("Kategorie")[:14], e.titel, "  [intern]" if intern else ""))
        if n >= a.max:
            out("... (mehr als %d Treffer, Suche eingrenzen oder --max)" % a.max)
            break
    if n == 0:
        out("Keine Treffer.")


def cmd_baum(a):
    st = Store()
    idx = {e.id: (e, i) for e, i in st.all()}
    start, _ = st.find(a.id)
    if a.rueckwaerts:
        rev = {}
        for e, _ in st.all():
            for d in e.deps():
                rev.setdefault(d, []).append(e.id)
        kinder = lambda x: sorted(set(rev.get(x, [])), key=lambda y: int(y.split("-")[1]))
        out("Rückwärts (wer baut auf %s auf):" % start.id)
    else:
        kinder = lambda x: idx[x][0].deps() if x in idx else []
        out("Abhängigkeiten von %s:" % start.id)
    gesehen = set()

    def zeile(x, tiefe, prefix):
        if x not in idx:
            out(prefix + x + "  (fehlt)")
            return
        e, intern = idx[x]
        mark = ""
        if x in gesehen:
            mark = "  (siehe oben)"
        out("%s%s  %-11s  %s%s%s" % (prefix, x, e.get("Status"), e.titel, "  [intern]" if intern else "", mark))
        if x in gesehen or tiefe >= a.tiefe:
            if tiefe >= a.tiefe and kinder(x) and x not in gesehen:
                out(prefix + "    ... (Tiefe %d erreicht)" % a.tiefe)
            return
        gesehen.add(x)
        for k in kinder(x):
            zeile(k, tiefe + 1, prefix + "    ")

    zeile(start.id, 0, "")


def cmd_bloecke(a):
    st = Store()
    for doc, name in ((st.pub, "DECISIONS.md"), (st.int, "intern")):
        ents = doc.entries()
        for titel, s, e in doc.blocks():
            ids = [x.num for x in ents if s <= x.start < e]
            rng = ("DEC-%03d bis DEC-%03d (%d)" % (min(ids), max(ids), len(ids))) if ids else "keine Einträge"
            out("%-12s Zeile %5d  %s  -  %s" % (name, s + 1, titel[:90], rng))


# ---------------------------------------------------------------- Befehle: schreiben
def check_kategorie(k):
    for c in KATEGORIEN:
        if c.lower() == k.lower():
            return c
    fehler("Kategorie ungültig: %s (erlaubt: %s)" % (k, ", ".join(KATEGORIEN)))


def check_status(s):
    for c in STATUS:
        if c.lower() == s.lower():
            return c
    fehler("Status ungültig: %s (erlaubt: %s)" % (s, ", ".join(STATUS)))


def einzeilig(s):
    return re.sub(r"\s*[\r\n]+\s*", " ", s or "").strip()


def verbotene_begriffe(text):
    return [name for rx, name in VERBOTEN if rx.search(text)]


def cmd_neu(a):
    st = Store()
    kat = check_kategorie(a.kategorie)
    status = check_status(a.status)
    deps = einzeilig(a.abhaengigkeiten) or "–"
    for d in DEC_RX.findall(deps):
        st.find(d)
    new_id = st.next_id()
    felder = [
        ("Status", status), ("Datum", heute()), ("Kategorie", kat), ("Ticket", einzeilig(a.ticket)),
        ("Entscheidung", einzeilig(a.entscheidung)), ("Begründung", einzeilig(a.begruendung) or "–"),
        ("Alternativen", einzeilig(a.alternativen) or "–"), ("Konsequenzen", einzeilig(a.konsequenzen) or "–"),
        ("Risiken", einzeilig(a.risiken)), ("Betroffene Komponenten", einzeilig(a.komponenten) or "–"),
        ("Abhängigkeiten", deps),
    ]
    lines = ["### %s: %s" % (new_id, einzeilig(a.titel))] + ["%s: %s" % (k, v) for k, v in felder if v]
    doc = st.int if a.intern else st.pub
    if not a.intern:
        bad = verbotene_begriffe("\n".join(lines))
        if bad:
            fehler("Verbotene Begriffe für die öffentliche Datei: %s. Nicht erwähnen oder mit --intern erfassen." % ", ".join(bad))
    bstart, bend = ensure_block(doc, a.block)
    append_to_section(doc, bstart, bend, "## " + (a.thema or "Entscheidungen"), lines, before_ad=True)
    doc.save()
    out("Erfasst: %s (%s, %s)%s" % (new_id, status, kat, " [intern]" if a.intern else ""))


def cmd_ergaenzen(a):
    st = Store()
    e, intern = st.find(a.id)
    doc = e.doc
    name = a.feld.strip()
    for f in FELDER:
        if f.lower() == name.lower() or f.lower().replace("ä", "ae").replace("ü", "ue") == name.lower():
            name = f
    text = einzeilig(a.text)
    if name.lower() in ("titel", "title", "ueberschrift", "überschrift"):
        if not intern:
            bad = verbotene_begriffe(text)
            if bad:
                fehler("Verbotene Begriffe für die öffentliche Datei: " + ", ".join(bad))
        doc.lines[e.start] = "### %s: %s" % (e.id, text)
        doc.save()
        out("%s: Titel geändert" % e.id)
        return
    if name not in FELDER:
        fehler("Unbekanntes Feld: %s (erlaubt: %s, Titel)" % (name, ", ".join(FELDER)))
    if name == "Status":
        text = check_status(text)
    if name == "Kategorie":
        text = check_kategorie(text)
    if not intern:
        bad = verbotene_begriffe(text)
        if bad:
            fehler("Verbotene Begriffe für die öffentliche Datei: " + ", ".join(bad))
    fs = e.fields()
    if name in fs:
        s, en, val = fs[name]
        if a.ersetzen or val in ("", "–", "-"):
            neu = text
        else:
            neu = val.rstrip() + (" " if val.rstrip().endswith((".", ";", ":")) else "; ") + text
        doc.lines[s:en + 1] = ["%s: %s" % (name, neu)]
    else:
        order = FELDER.index(name) if name in FELDER else len(FELDER)
        pos = e.end
        for f, (s, en, _) in sorted(fs.items(), key=lambda kv: kv[1][0]):
            if f in FELDER and FELDER.index(f) > order:
                pos = s
                break
        doc.lines[pos:pos] = ["%s: %s" % (name, text)]
    doc.save()
    out("%s: %s %s" % (e.id, name, "ersetzt" if a.ersetzen else "ergänzt"))


def cmd_status(a):
    a.feld, a.text, a.ersetzen = "Status", a.status, True
    cmd_ergaenzen(a)


def cmd_ersetzen(a):
    st = Store()
    alt, _ = st.find(a.id)
    neu, _ = st.find(a.durch)
    if alt.id == neu.id:
        fehler("alter und neuer Eintrag sind gleich")
    ns = argparse.Namespace(id=alt.id, feld="Status", text="Superseded", ersetzen=True)
    cmd_ergaenzen(ns)
    st = Store()
    alt, _ = st.find(a.id)
    if ("Ersetzt durch " + neu.id) not in alt.get("Konsequenzen"):
        cmd_ergaenzen(argparse.Namespace(id=alt.id, feld="Konsequenzen", text="Ersetzt durch %s." % neu.id, ersetzen=False))
    st = Store()
    neu, _ = st.find(a.durch)
    if alt.id not in neu.deps():
        cmd_ergaenzen(argparse.Namespace(id=neu.id, feld="Abhängigkeiten", text=alt.id, ersetzen=False))


def cmd_abschnitt(a):
    st = Store()
    doc = st.int if a.intern else st.pub
    text = einzeilig(a.text)
    if not a.intern:
        bad = verbotene_begriffe(text)
        if bad:
            fehler("Verbotene Begriffe für die öffentliche Datei: " + ", ".join(bad))
    bstart, bend = ensure_block(doc, a.block)
    append_to_section(doc, bstart, bend, ABSCHNITTE[a.befehl], ["- " + text])
    doc.save()
    out("Eingetragen in %s" % ABSCHNITTE[a.befehl][3:])


def cmd_intern(a):
    pub = Doc(PUBLIC)
    intd = Doc(INTERN, intern=True)
    moved = []
    for id_ in [norm_id(x) for x in a.ids]:
        hits = [e for e in pub.entries() if e.id == id_]
        if not hits:
            if any(e.id == id_ for e in intd.entries()):
                out("%s ist schon intern" % id_)
                continue
            fehler("Eintrag nicht gefunden: " + id_)
        e = hits[0]
        body = e.body()
        del pub.lines[e.start:e.end]
        while 0 < e.start < len(pub.lines) and pub.lines[e.start].strip() == "" and pub.lines[e.start - 1].strip() == "":
            del pub.lines[e.start]
        bstart, bend = ensure_block(intd, "Ausgelagert aus DECISIONS.md")
        append_to_section(intd, bstart, bend, "## Entscheidungen", body, before_ad=True)
        moved.append(id_)
    if moved:
        intd.save()
        pub.save()
        out("Verschoben (intern): " + ", ".join(moved))


def update_stats(doc):
    """Tabelle unter '## Statistik' neu berechnen; Anzahl Einträge oder None, wenn der Abschnitt fehlt."""
    ents = doc.entries()
    count = {s: 0 for s in STATUS}
    for e in ents:
        s = e.get("Status")
        count[s] = count.get(s, 0) + 1
    rows = ["| Status | Anzahl |", "|---|---|"] + ["| %s | %d |" % (s, c) for s, c in count.items() if c] + \
           ["| **Total** | **%d** |" % len(ents)]
    for i, l in enumerate(doc.lines):
        if l.strip() == "## Statistik":
            j = i + 1
            while j < len(doc.lines) and not doc.lines[j].startswith("|"):
                j += 1
            k = j
            while k < len(doc.lines) and doc.lines[k].startswith("|"):
                k += 1
            doc.lines[j:k] = rows
            return len(ents)
    return None


def cmd_index(a):
    doc = Doc(PUBLIC)
    n = update_stats(doc)
    if n is None:
        fehler("Abschnitt '## Statistik' nicht gefunden")
    doc.save()
    out("Statistik aktualisiert: %d Einträge" % n)


def cmd_check(a):
    st = Store()
    errors, hints = [], []
    seen = {}
    allids = set()
    for e, intern in st.all():
        where = "intern" if intern else "DECISIONS.md"
        if e.id in seen:
            errors.append("%s doppelt (%s und %s)" % (e.id, seen[e.id], where))
        seen[e.id] = where
        allids.add(e.id)
    for e, intern in st.all():
        fs = e.fields()
        for p in PFLICHT:
            if p not in fs or not fs[p][2]:
                errors.append("%s: Pflichtfeld fehlt: %s" % (e.id, p))
        s = e.get("Status")
        if s and s not in STATUS:
            errors.append("%s: Status ungültig: %s" % (e.id, s))
        k = e.get("Kategorie")
        if k and not any(k == c for c in KATEGORIEN):
            hints.append("%s: Kategorie ausserhalb der Liste (Altbestand): %s" % (e.id, k))
        for d in e.deps():
            if d not in allids:
                errors.append("%s: Abhängigkeit %s existiert nicht" % (e.id, d))
        if s == "Superseded" and not re.search(r"Ersetzt durch DEC-\d{3,}", e.text()):
            hints.append("%s: Superseded ohne 'Ersetzt durch DEC-...'" % e.id)
    for i, line in enumerate(st.pub.lines):
        for name in verbotene_begriffe(line):
            errors.append("DECISIONS.md Zeile %d: verbotener Begriff (%s) - auslagern mit 'intern' oder umformulieren" % (i + 1, name))
    for x in errors:
        out("FEHLER   " + x)
    if a.details:
        for x in hints:
            out("Hinweis  " + x)
    out("check: %d Einträge, %d Fehler, %d Hinweise%s" % (len(seen), len(errors), len(hints), "" if a.details or not hints else " (einzeln mit --details)"))
    sys.exit(1 if errors else 0)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser(description="Entscheidungen von STEFFLIX (docs/DECISIONS.md) lesen und pflegen.",
                                 formatter_class=argparse.RawDescriptionHelpFormatter, epilog=__doc__.split("\n\n", 2)[2])
    sub = ap.add_subparsers(dest="befehl")

    p = sub.add_parser("zeigen", help="Einträge vollständig zeigen"); p.add_argument("ids", nargs="+"); p.set_defaults(f=cmd_zeigen)
    p = sub.add_parser("suchen", help="Einträge suchen"); p.add_argument("woerter", nargs="+")
    p.add_argument("--status"); p.add_argument("--kategorie"); p.add_argument("--voll", action="store_true")
    p.add_argument("--max", type=int, default=60); p.set_defaults(f=cmd_suchen)
    p = sub.add_parser("baum", help="Abhängigkeitsbaum"); p.add_argument("id"); p.add_argument("--rueckwaerts", action="store_true")
    p.add_argument("--tiefe", type=int, default=12); p.set_defaults(f=cmd_baum)
    p = sub.add_parser("bloecke", help="Blöcke mit Nummernbereich"); p.set_defaults(f=cmd_bloecke)

    p = sub.add_parser("neu", help="neuen Eintrag erfassen")
    p.add_argument("--titel", required=True); p.add_argument("--entscheidung", required=True)
    p.add_argument("--begruendung", required=True); p.add_argument("--kategorie", required=True)
    p.add_argument("--status", default="Accepted"); p.add_argument("--ticket", default="")
    p.add_argument("--alternativen", default=""); p.add_argument("--konsequenzen", default="")
    p.add_argument("--risiken", default=""); p.add_argument("--komponenten", default="")
    p.add_argument("--abhaengigkeiten", default=""); p.add_argument("--block", default="")
    p.add_argument("--thema", default=""); p.add_argument("--intern", action="store_true"); p.set_defaults(f=cmd_neu)
    p = sub.add_parser("ergaenzen", help="Feld ergänzen oder ersetzen"); p.add_argument("id"); p.add_argument("--feld", required=True)
    p.add_argument("--text", required=True); p.add_argument("--ersetzen", action="store_true"); p.set_defaults(f=cmd_ergaenzen)
    p = sub.add_parser("status", help="Status setzen"); p.add_argument("id"); p.add_argument("status"); p.set_defaults(f=cmd_status)
    p = sub.add_parser("ersetzen", help="Eintrag durch neueren ersetzen"); p.add_argument("id"); p.add_argument("--durch", required=True)
    p.set_defaults(f=cmd_ersetzen)
    for k in ABSCHNITTE:
        p = sub.add_parser(k, help="Punkt in " + ABSCHNITTE[k][3:]); p.add_argument("--text", required=True)
        p.add_argument("--block", default=""); p.add_argument("--intern", action="store_true"); p.set_defaults(f=cmd_abschnitt)
    p = sub.add_parser("intern", help="Einträge in die interne Datei verschieben"); p.add_argument("ids", nargs="+")
    p.set_defaults(f=cmd_intern)
    p = sub.add_parser("index", help="Statistik neu berechnen"); p.set_defaults(f=cmd_index)
    p = sub.add_parser("check", help="Datei prüfen"); p.add_argument("--details", action="store_true"); p.set_defaults(f=cmd_check)

    a = ap.parse_args()
    if not a.befehl:
        ap.print_help()
        return 1
    a.f(a)
    return 0


if __name__ == "__main__":
    sys.exit(main())
