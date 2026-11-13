# -*- coding: utf-8 -*-
"""
Holt die Folgenliste einer Serie aus dem Internet und gibt sie im Format des
SERIEN-Arrays von serien_umbenennen.py aus, zum Kopieren und Einfügen.

Aufruf:
    python folgenliste.py                 -> fragt nach dem Seriennamen
    python folgenliste.py "Family Guy"    -> sucht direkt

Titel: zuerst Deutsch, wenn es keinen deutschen Titel gibt, Englisch.

Datenquellen, in dieser Reihenfolge (liefert eine keinen Treffer, kommt die nächste):
  1. TMDB (themoviedb.org)      - optional, deutsche Titel, braucht TMDB_API_KEY
  2. fernsehserien.de           - ohne Key, deutsche Titel
  3. Wikipedia (de.wikipedia.org) - ohne Key, deutsche Titel aus den Episodenlisten
  4. TheTVDB (thetvdb.com)      - optional, deutsche Titel, braucht TVDB_API_KEY
  5. TVmaze (tvmaze.com)        - ohne Key, nur englische Titel
Quellen ohne Key werden übersprungen, ohne Keys läuft also
fernsehserien.de -> Wikipedia -> TVmaze.
IMDb ist nicht dabei: IMDb verbietet automatische Abfragen (robots.txt) und sperrt sie.
Bei der Auswahl kann man mit 0 in der nächsten Quelle weitersuchen bzw. nach der
letzten Quelle einen anderen Suchbegriff eingeben.
"""

# ======================= Einstellungen =======================

# Optional: TMDB-Key (v3 "API-Schlüssel" oder v4 "Lesezugriffstoken"), kostenlos unter
# https://www.themoviedb.org/settings/api. Leer = TMDB überspringen
TMDB_API_KEY = ""

# Optional: TheTVDB-Key (v4), https://thetvdb.com/api-information. Leer = überspringen
TVDB_API_KEY = ""

# Specials (Staffel 0) mit ausgeben?
SPECIALS = False

# Dateiendung der Zielnamen
ENDUNG = ".mp4"

# Ausgabe zusätzlich in die Zwischenablage kopieren (nur Windows)
ZWISCHENABLAGE = True

# =============================================================

import html
import json
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from html.parser import HTMLParser

USER_AGENT = "folgenliste.py/1.0 (privates Skript zum Umbenennen von Serien)"


def hole_json(url, header=None, daten=None):
    header = {"Accept": "application/json", "User-Agent": USER_AGENT, **(header or {})}
    if daten is not None:
        daten = json.dumps(daten).encode("utf-8")
        header["Content-Type"] = "application/json"
    anfrage = urllib.request.Request(url, data=daten, headers=header)
    for versuch in range(4):
        try:
            with urllib.request.urlopen(anfrage, timeout=20) as antwort:
                return json.load(antwort)
        except urllib.error.HTTPError as fehler:
            # Server bremst (zu viele Anfragen) oder ist kurz überlastet: warten, nochmal
            if fehler.code not in (429, 503) or versuch == 3:
                raise
            warten = fehler.headers.get("Retry-After", "")
            warten = min(int(warten), 30) if warten.isdigit() else 2 ** (versuch + 1)
            print(f"  (Server bremst, warte {warten} s ...)")
            time.sleep(warten)


def platzhalter(titel):
    """Kein echter Titel, z.B. leer, "Folge 5", "Episode 5" oder "TBA"."""
    t = (titel or "").strip()
    return not t or t.upper() == "TBA" or re.fullmatch(r"(Folge|Episode)\s*\d+", t, re.I) is not None


def info(*teile):
    """Zusatzinfos für die Trefferliste, z.B. "US, Fox"."""
    return ", ".join(t for t in teile if t)


def mit_fallback(de, en):
    """Deutschen Titel nehmen, sonst englischen, sonst was da ist."""
    if not platzhalter(de):
        return de
    if not platzhalter(en):
        return en
    return de or en or ""


# ------------------------- TMDB -------------------------

class Tmdb:
    name = "TMDB"
    sprache = "deutsche Titel"

    def aktiv(self):
        return bool(TMDB_API_KEY.strip())

    def _get(self, pfad, **parameter):
        header = {}
        if len(TMDB_API_KEY) > 40:  # v4-Lesezugriffstoken
            header["Authorization"] = "Bearer " + TMDB_API_KEY
        else:
            parameter["api_key"] = TMDB_API_KEY
        return hole_json("https://api.themoviedb.org/3" + pfad + "?" + urllib.parse.urlencode(parameter), header)

    def suche(self, name):
        treffer = self._get("/search/tv", query=name, language="de-DE").get("results", [])
        return [(t["id"], t.get("name") or t.get("original_name"), (t.get("first_air_date") or "")[:4],
                 info(", ".join(t.get("origin_country") or []),
                      t.get("original_name") if t.get("original_name") != t.get("name") else ""))
                for t in treffer]

    def folgen(self, serien_id):
        info = self._get(f"/tv/{serien_id}", language="de-DE")
        folgen = {}
        for staffel in info.get("seasons", []):
            nr = staffel["season_number"]
            if nr == 0 and not SPECIALS:
                continue
            de = self._get(f"/tv/{serien_id}/season/{nr}", language="de-DE").get("episodes", [])
            en = None
            liste = []
            for folge in de:
                titel = folge.get("name") or ""
                if platzhalter(titel):
                    if en is None:  # englische Titel nur bei Bedarf laden
                        en = {f["episode_number"]: f.get("name") or "" for f in
                              self._get(f"/tv/{serien_id}/season/{nr}", language="en-US").get("episodes", [])}
                    titel = mit_fallback(titel, en.get(folge["episode_number"], ""))
                liste.append((folge["episode_number"], titel))
            if liste:
                folgen[nr] = liste
        return folgen


# ------------------------- fernsehserien.de -------------------------

def hole_html(url):
    """Lädt eine Webseite. Liefert (endgültige URL nach Weiterleitung, HTML)."""
    anfrage = urllib.request.Request(url, headers={
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " + USER_AGENT,
        "Accept-Language": "de-DE,de;q=0.9"})
    for versuch in range(4):
        try:
            with urllib.request.urlopen(anfrage, timeout=20) as antwort:
                return antwort.geturl(), antwort.read().decode("utf-8", "replace")
        except urllib.error.HTTPError as fehler:
            if fehler.code not in (429, 503) or versuch == 3:
                raise
            print(f"  (Server bremst, warte {2 ** (versuch + 1)} s ...)")
            time.sleep(2 ** (versuch + 1))


def ohne_tags(text):
    return " ".join(html.unescape(re.sub(r"<[^>]+>", " ", text)).split())


class Fernsehserien:
    name = "fernsehserien.de"
    sprache = "deutsche Titel"
    basis = "https://www.fernsehserien.de"

    def aktiv(self):
        return True

    def suche(self, name):
        url, seite = hole_html(self.basis + "/suche/" + urllib.parse.quote(name))
        pfad = urllib.parse.urlparse(url).path
        if not pfad.startswith("/suche"):
            # Eindeutiger Treffer: direkt auf die Serienseite weitergeleitet
            m = re.search(r'<h1 class="serien-titel"[^>]*>(.*?)</h1>', seite, re.S)
            return [(pfad.strip("/").split("/")[0], ohne_tags(m.group(1)) if m else pfad.strip("/"), "", "")]
        treffer = []
        for m in re.finditer(r'<li class="[^"]*suchergebnisse-sendung[^"]*">\s*<a [^>]*href="/([^"/]+)"[^>]*>'
                             r'.*?<dl>(.*?)</dl>', seite, re.S):
            teile = [ohne_tags(t) for t in re.findall(r"<d[td][^>]*>(.*?)</d[td]>", m.group(2), re.S)]
            titel = " – ".join(teile[:-1]) if len(teile) > 1 else (teile[0] if teile else m.group(1))
            zusatz = teile[-1] if len(teile) > 1 else ""
            jahr = re.search(r"\b(19|20)\d\d\b", zusatz)
            treffer.append((m.group(1), titel, jahr.group(0) if jahr else "", zusatz))
        return treffer

    def folgen(self, slug):
        _, seite = hole_html(f"{self.basis}/{slug}/episodenguide")
        folgen = {}
        # Jede Folge ist ein Link mit title="<Staffel>.<Folge> <Titel>", z.B. "1.01 Kulturelle Unterschiede"
        for m in re.finditer(r'<a role="row"[^>]*\btitle="(\d+)\.(\d+)\s+([^"]*)"', seite):
            staffel, nummer, titel = int(m.group(1)), int(m.group(2)), html.unescape(m.group(3)).strip()
            folgen.setdefault(staffel, {}).setdefault(nummer, titel)
        if list(folgen) == [0]:
            folgen = {1: folgen[0]}  # Serie ohne Staffeln: alles als Staffel 1
        elif 0 in folgen and not SPECIALS:
            del folgen[0]
        return {s: sorted(f.items()) for s, f in folgen.items()}


# ------------------------- Wikipedia -------------------------

class _TabellenLeser(HTMLParser):
    """Liest aus Wikipedia-HTML die Überschriften und Tabellen (class="wikitable") in
    Reihenfolge. Fußnoten, Bearbeiten-Links und versteckte Sortierschlüssel fallen weg."""

    LEER = {"br", "img", "hr", "meta", "link", "input", "wbr", "col", "source", "area"}

    def __init__(self):
        super().__init__(convert_charrefs=True)
        self.elemente = []      # ("ueberschrift", text) oder ("tabelle", zeilen)
        self.tiefe = 0
        self.ueberspringen = None
        self.ueberschrift = None
        self.tabelle = None     # Liste von Zeilen; Zeile = Liste von [text, rowspan, colspan, kopf]
        self.tabellen_tiefe = 0
        self.zelle = None

    def handle_starttag(self, tag, attrs):
        if tag in self.LEER:
            if tag == "br" and self.zelle is not None and self.ueberspringen is None:
                self.zelle[0] += " "
            return
        self.tiefe += 1
        a = dict(attrs)
        klasse = a.get("class") or ""
        if self.ueberspringen is None and (
                tag in ("style", "script", "sup") or "mw-editsection" in klasse or "reference" in klasse
                or "sortkey" in klasse or "display:none" in (a.get("style") or "").replace(" ", "")):
            self.ueberspringen = self.tiefe
        if tag == "table":
            self.tabellen_tiefe += 1
            if self.tabellen_tiefe == 1 and "wikitable" in klasse:
                self.tabelle = []
        elif self.tabelle is not None and self.tabellen_tiefe == 1:
            if tag == "tr":
                self.tabelle.append([])
            elif tag in ("td", "th") and self.tabelle:
                zahl = lambda w: int(w) if (w or "").isdigit() else 1
                self.zelle = ["", zahl(a.get("rowspan")), zahl(a.get("colspan")), tag == "th"]
        if tag in ("h2", "h3", "h4") and self.tabelle is None:
            self.ueberschrift = ""

    def handle_endtag(self, tag):
        if tag in self.LEER:
            return
        if self.ueberspringen == self.tiefe:
            self.ueberspringen = None
        self.tiefe -= 1
        if tag in ("td", "th") and self.zelle is not None and self.tabellen_tiefe == 1:
            self.zelle[0] = " ".join(self.zelle[0].split())
            self.tabelle[-1].append(self.zelle)
            self.zelle = None
        elif tag == "table":
            if self.tabellen_tiefe == 1 and self.tabelle is not None:
                self.elemente.append(("tabelle", self.tabelle))
                self.tabelle = None
            self.tabellen_tiefe -= 1
        elif tag in ("h2", "h3", "h4") and self.ueberschrift is not None:
            self.elemente.append(("ueberschrift", " ".join(self.ueberschrift.split())))
            self.ueberschrift = None

    def handle_data(self, data):
        if self.ueberspringen is not None:
            return
        if self.zelle is not None:
            self.zelle[0] += data
        elif self.ueberschrift is not None:
            self.ueberschrift += data


def _als_raster(zeilen):
    """Tabellenzeilen mit rowspan/colspan in ein gleichmäßiges Raster umwandeln."""
    raster, offen = [], {}  # offen: spalte -> [rest_zeilen, text, kopf]
    for zeile in zeilen:
        neu, spalte, zellen = [], 0, list(zeile)
        while zellen or any(s >= spalte for s in offen):
            if spalte in offen:
                rest, text, kopf = offen[spalte]
                neu.append((text, kopf))
                offen[spalte][0] -= 1
                if offen[spalte][0] <= 0:
                    del offen[spalte]
                spalte += 1
                continue
            if not zellen:
                if not any(s > spalte for s in offen):
                    break
                neu.append(("", False))
                spalte += 1
                continue
            text, rowspan, colspan, kopf = zellen.pop(0)
            for _ in range(colspan):
                neu.append((text, kopf))
                if rowspan > 1:
                    offen[spalte] = [rowspan - 1, text, kopf]
                spalte += 1
        raster.append(neu)
    return raster


def _zahl(text):
    m = re.search(r"\d+", text or "")
    return int(m.group(0)) if m else None


def _titel_sauber(text):
    text = (text or "").strip().strip("\"„“”‚‘’«»").strip()
    return "" if text in ("—", "–", "-", "?") else text


class Wikipedia:
    name = "Wikipedia"
    sprache = "deutsche Titel"
    api = "https://de.wikipedia.org/w/api.php"

    def aktiv(self):
        return True

    def _get(self, **parameter):
        parameter.update(format="json", formatversion=2)
        return hole_json(self.api + "?" + urllib.parse.urlencode(parameter))

    def suche(self, name):
        seiten = []
        for begriff, anzahl in ((f"{name} Episoden", 10), (name, 5)):
            for t in self._get(action="query", list="search", srsearch=begriff,
                               srlimit=anzahl, srnamespace=0).get("query", {}).get("search", []):
                if t["title"] not in seiten:
                    seiten.append(t["title"])
        # Episodenlisten zuerst
        seiten.sort(key=lambda s: 0 if "episoden" in s.lower() else 1)
        return [(s, s, "", "Wikipedia-Artikel") for s in seiten]

    def folgen(self, seite):
        html = self._get(action="parse", page=seite, prop="text", redirects=1)["parse"]["text"]
        leser = _TabellenLeser()
        leser.feed(html)

        folgen = {}
        staffel_aus_ueberschrift = None
        for art, inhalt in leser.elemente:
            if art == "ueberschrift":
                m = re.search(r"(?:Staffel|Season)\s*(\d+)", inhalt, re.I)
                staffel_aus_ueberschrift = int(m.group(1)) if m else None
                continue
            self._lies_tabelle(_als_raster(inhalt), staffel_aus_ueberschrift, folgen)

        if 0 in folgen and not SPECIALS:
            del folgen[0]
        return {s: sorted(f.items()) for s, f in folgen.items() if f}

    def _lies_tabelle(self, raster, staffel_standard, folgen):
        # Kopfzeile mit Titel-Spalte suchen
        for kopf_nr, zeile in enumerate(raster[:3]):
            namen = [t.lower() for t, _ in zeile]
            de = next((i for i, n in enumerate(namen) if "deutscher titel" in n), None)
            en = next((i for i, n in enumerate(namen) if "originaltitel" in n), None)
            if de is None:
                de = next((i for i, n in enumerate(namen) if "titel" in n and "original" not in n), None)
            if de is not None or en is not None:
                break
        else:
            return

        def spalte(bedingung):
            return next((i for i, n in enumerate(namen) if bedingung(n.replace(" ", ""))), None)

        nr_staffel = spalte(lambda n: "(st" in n or "nr.st" in n or "(staffel" in n
                            or n in ("folge", "nr.(st.)", "#(st.)"))
        nr_gesamt = spalte(lambda n: "ges" in n)
        staffel_spalte = spalte(lambda n: n in ("staffel", "st.", "st"))
        if nr_staffel is None and nr_gesamt is None:
            nr_staffel = spalte(lambda n: n in ("nr.", "nr", "#", "episode"))

        laufend = {}
        for zeile in raster[kopf_nr + 1:]:
            if all(kopf for _, kopf in zeile):
                continue  # weitere Kopfzeile
            wert = lambda i: zeile[i][0] if i is not None and i < len(zeile) else ""
            staffel = _zahl(wert(staffel_spalte)) if staffel_spalte is not None else staffel_standard
            if staffel is None:
                staffel = 1
            if nr_staffel is not None:
                nummer = _zahl(wert(nr_staffel))
            elif nr_gesamt is not None and _zahl(wert(nr_gesamt)) is not None:
                nummer = laufend.get(staffel, 0) + 1
            else:
                nummer = None
            if nummer is None:
                continue
            laufend[staffel] = nummer
            titel = mit_fallback(_titel_sauber(wert(de)), _titel_sauber(wert(en)))
            folgen.setdefault(staffel, {}).setdefault(nummer, titel)


# ------------------------- TheTVDB -------------------------

class Tvdb:
    name = "TheTVDB"
    sprache = "deutsche Titel"
    basis = "https://api4.thetvdb.com/v4"

    def __init__(self):
        self.token = None

    def aktiv(self):
        return bool(TVDB_API_KEY.strip())

    def _get(self, pfad, **parameter):
        if self.token is None:
            self.token = hole_json(self.basis + "/login", daten={"apikey": TVDB_API_KEY})["data"]["token"]
        url = self.basis + pfad + ("?" + urllib.parse.urlencode(parameter) if parameter else "")
        return hole_json(url, {"Authorization": "Bearer " + self.token})

    def suche(self, name):
        treffer = self._get("/search", query=name, type="series").get("data", [])
        ergebnis = []
        for t in treffer:
            titel = (t.get("translations") or {}).get("deu") or t.get("name")
            ergebnis.append((t.get("tvdb_id") or t.get("id", "").replace("series-", ""), titel,
                             t.get("year") or "",
                             info((t.get("country") or "").upper(), t.get("network") or "",
                                  t.get("name") if t.get("name") != titel else "")))
        return ergebnis

    def _episoden(self, serien_id, sprache):
        folgen, seite = [], 0
        while True:
            antwort = self._get(f"/series/{serien_id}/episodes/default/{sprache}", page=seite)
            folgen += (antwort.get("data") or {}).get("episodes", [])
            if not (antwort.get("links") or {}).get("next"):
                return folgen
            seite += 1

    def folgen(self, serien_id):
        de = self._episoden(serien_id, "deu")
        en = None
        folgen = {}
        for f in de:
            staffel, nummer = f.get("seasonNumber"), f.get("number")
            if staffel is None or nummer is None or (staffel == 0 and not SPECIALS):
                continue
            titel = f.get("name") or ""
            if platzhalter(titel):
                if en is None:
                    en = {(e.get("seasonNumber"), e.get("number")): e.get("name") or ""
                          for e in self._episoden(serien_id, "eng")}
                titel = mit_fallback(titel, en.get((staffel, nummer), ""))
            folgen.setdefault(staffel, []).append((nummer, titel))
        return folgen


# ------------------------- TVmaze -------------------------

class Tvmaze:
    name = "TVmaze"
    sprache = "englische Titel"

    def aktiv(self):
        return True

    def suche(self, name):
        url = "https://api.tvmaze.com/search/shows?" + urllib.parse.urlencode({"q": name})
        ergebnis = []
        for t in hole_json(url):
            s = t["show"]
            sender = s.get("network") or s.get("webChannel") or {}
            land = (sender.get("country") or {}).get("code", "")
            ergebnis.append((s["id"], s["name"], (s.get("premiered") or "")[:4],
                             info(land, sender.get("name", ""), s.get("language") or "")))
        return ergebnis

    def folgen(self, serien_id):
        url = f"https://api.tvmaze.com/shows/{serien_id}/episodes" + ("?specials=1" if SPECIALS else "")
        folgen = {}
        for f in hole_json(url):
            if f.get("number") is None:  # Specials ohne Nummer
                if not SPECIALS:
                    continue
                staffel, nummer = 0, len(folgen.get(0, [])) + 1
            else:
                staffel, nummer = f["season"], f["number"]
            folgen.setdefault(staffel, []).append((nummer, f.get("name") or ""))
        return folgen


QUELLEN = [Tmdb(), Fernsehserien(), Wikipedia(), Tvdb(), Tvmaze()]


# ------------------------- Ausgabe -------------------------

def als_array(serie, folgen):
    zeilen = [f"    {serie!r}: {{"]
    for staffel in sorted(folgen):
        zeilen.append(f"        'Staffel {staffel:02d}': [")
        for nummer, titel in sorted(folgen[staffel]):
            titel = " ".join((titel or "").split())
            name = f"{serie} - S{staffel:02d}E{nummer:02d}" + (f" - {titel}" if titel else "") + ENDUNG
            zeilen.append(f"            {name!r},")
        zeilen.append("        ],")
    zeilen.append("    },")
    return "\n".join(zeilen)


def in_zwischenablage(text):
    if sys.platform != "win32":
        return False
    try:
        subprocess.run("clip", input=text.encode("utf-16-le"), check=True, shell=True)
        return True
    except (OSError, subprocess.CalledProcessError):
        return False


def frage(text, standard=""):
    antwort = input(f"{text}" + (f" [{standard}]" if standard else "") + ": ")
    antwort = antwort.replace("﻿", "").strip()  # unsichtbares BOM (z.B. aus PowerShell) entfernen
    return antwort or standard


def waehle(treffer, quelle, gibt_naechste):
    """Lässt den Benutzer einen Treffer wählen. None = in der nächsten Quelle suchen."""
    treffer = treffer[:10]
    print(f"\n{quelle}: {len(treffer)} Treffer")
    for i, (_, titel, jahr, zusatz) in enumerate(treffer, 1):
        print(f"  {i:>2}. {titel}" + (f" ({jahr})" if jahr else "") + (f"  [{zusatz}]" if zusatz else ""))
    print("   0. nicht dabei - " + ("in der nächsten Quelle suchen" if gibt_naechste
                                     else "anderen Suchbegriff eingeben"))
    while True:
        nr = frage(f"\nWelche Serie ({quelle})? Nummer", "1")
        if nr == "0":
            return None
        if nr.isdigit() and 1 <= int(nr) <= len(treffer):
            return treffer[int(nr) - 1]
        print("Ungültige Auswahl.")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass

    quellen = [q for q in QUELLEN if q.aktiv()]
    print("Quellen:", " -> ".join(f"{q.name} ({q.sprache})" for q in quellen))
    fehlend = [q.name for q in QUELLEN if not q.aktiv()]
    if fehlend:
        print("Übersprungen (kein Key gesetzt):", ", ".join(fehlend))

    name = " ".join(sys.argv[1:]).strip() or frage("Serie")
    while name:
        if suche_in_quellen(name, quellen):
            return
        print("\nKeine passende Serie gefunden. Tipp: Originaltitel oder kürzeren Begriff versuchen.")
        name = frage("Neuer Suchbegriff (leer = beenden)")
    sys.exit(1)


def suche_in_quellen(name, quellen):
    """Sucht nacheinander in den Quellen. True, wenn eine Liste ausgegeben wurde."""
    for i, quelle in enumerate(quellen):
        gibt_naechste = i < len(quellen) - 1
        try:
            treffer = quelle.suche(name)
        except Exception as fehler:
            print(f"{quelle.name}: Fehler bei der Suche ({fehler})")
            continue
        if not treffer:
            print(f"{quelle.name}: keine Treffer")
            continue

        auswahl = waehle(treffer, quelle.name, gibt_naechste)
        if auswahl is None:
            continue
        serien_id, titel, _, _ = auswahl

        try:
            folgen = quelle.folgen(serien_id)
        except Exception as fehler:
            print(f"{quelle.name}: Fehler beim Laden der Folgen ({fehler})")
            continue
        if not folgen:
            print(f"{quelle.name}: keine Folgen gefunden")
            continue

        ordner = frage("Name des Serienordners", titel)
        text = als_array(ordner, folgen)
        print("\n" + text + "\n")
        anzahl = sum(len(f) for f in folgen.values())
        print(f"Quelle: {quelle.name} - {len(folgen)} Staffeln, {anzahl} Folgen.")
        if ZWISCHENABLAGE and in_zwischenablage(text):
            print("In die Zwischenablage kopiert.")
        return True
    return False


if __name__ == "__main__":
    main()
