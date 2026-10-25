# -*- coding: utf-8 -*-
"""
Lädt Plakate für Serien (auch pro Staffel) und Filme herunter und speichert sie als JPG.

Aufruf:
    python plakate.py                 -> fragt alles ab
    python plakate.py "Lilyhammer"    -> sucht direkt

Abfragen im Skript:
  - Film oder Serie
  - bei Serien die Staffel:  leer = Serienplakat,  3 = Staffel 3,
                             1,3 oder 2-4 = mehrere,  alle = alle Staffeln
  - aus der Trefferliste die gewünschten Bilder: 2  |  1,3  |  a = alle  |
    0 = nichts davon, nächste Quelle

Gespeichert wird in ZIEL_ORDNER:
  Serien:  <ZIEL_ORDNER>\\<Serie>\\<Serie>.jpg  und  ...\\Staffel 01.jpg
  Filme:   <ZIEL_ORDNER>\\<Film>.jpg
Vorhandene Dateien werden nicht überschrieben (dann "Name (2).jpg").
Bilder in anderen Formaten (PNG, WebP ...) werden mit Pillow in JPG umgewandelt.

Quellen, in dieser Reihenfolge:
  Serien: TMDB (optional, Key) -> TVmaze -> iTunes -> Wikipedia
  Filme:  TMDB (optional, Key) -> Wikipedia
"""

# ======================= Einstellungen =======================

# Wohin die Bilder gespeichert werden. Leer = Ordner "Plakate" neben diesem Skript
ZIEL_ORDNER = ""

# Optional: TMDB-Key (v3 "API-Schlüssel" oder v4 "Lesezugriffstoken"), kostenlos unter
# https://www.themoviedb.org/settings/api. Mit Key gibt es die meisten (auch deutsche) Plakate
TMDB_API_KEY = ""

# JPG-Qualität beim Umwandeln (1-100)
JPG_QUALITAET = 95

# =============================================================

import html
import io
import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

USER_AGENT = "plakate.py/1.0 (privates Skript fuer die eigene Mediensammlung)"


def hole(url, header=None):
    anfrage = urllib.request.Request(url, headers={"User-Agent": USER_AGENT, **(header or {})})
    for versuch in range(4):
        try:
            with urllib.request.urlopen(anfrage, timeout=30) as antwort:
                return antwort.read()
        except urllib.error.HTTPError as fehler:
            if fehler.code not in (429, 503) or versuch == 3:
                raise
            print(f"  (Server bremst, warte {2 ** (versuch + 1)} s ...)")
            time.sleep(2 ** (versuch + 1))


def hole_json(url, header=None):
    return json.loads(hole(url, {"Accept": "application/json", **(header or {})}))


class Kandidat:
    def __init__(self, beschreibung, url, dateiname):
        self.beschreibung = beschreibung
        self.url = url
        self.dateiname = dateiname  # ohne Endung, z.B. "Staffel 01"


def staffel_name(nr):
    return f"Staffel {nr:02d}"


def passt(staffeln, nr):
    return staffeln == "alle" or nr in staffeln


# ------------------------- TMDB -------------------------

class Tmdb:
    name = "TMDB"
    bild = "https://image.tmdb.org/t/p/original"

    def aktiv(self, typ):
        return bool(TMDB_API_KEY.strip())

    def _get(self, pfad, **parameter):
        header = {}
        if len(TMDB_API_KEY) > 40:
            header["Authorization"] = "Bearer " + TMDB_API_KEY
        else:
            parameter["api_key"] = TMDB_API_KEY
        return hole_json("https://api.themoviedb.org/3" + pfad + "?" + urllib.parse.urlencode(parameter), header)

    def kandidaten(self, name, typ, staffeln):
        if typ == "film":
            treffer = self._get("/search/movie", query=name, language="de-DE").get("results", [])[:8]
            return [Kandidat(f"{t.get('title')} ({(t.get('release_date') or '')[:4]})",
                             self.bild + t["poster_path"], name)
                    for t in treffer if t.get("poster_path")]
        treffer = self._get("/search/tv", query=name, language="de-DE").get("results", [])[:4]
        ergebnis = []
        for t in treffer:
            serie = f"{t.get('name')} ({(t.get('first_air_date') or '')[:4]})"
            if staffeln is None:
                if t.get("poster_path"):
                    ergebnis.append(Kandidat(serie, self.bild + t["poster_path"], name))
                continue
            for s in self._get(f"/tv/{t['id']}", language="de-DE").get("seasons", []):
                if passt(staffeln, s["season_number"]) and s.get("poster_path"):
                    ergebnis.append(Kandidat(f"{serie} - {staffel_name(s['season_number'])}",
                                             self.bild + s["poster_path"], staffel_name(s["season_number"])))
        return ergebnis


# ------------------------- TVmaze -------------------------

class Tvmaze:
    name = "TVmaze"

    def aktiv(self, typ):
        return typ == "serie"

    def kandidaten(self, name, typ, staffeln):
        treffer = hole_json("https://api.tvmaze.com/search/shows?" + urllib.parse.urlencode({"q": name}))[:4]
        ergebnis = []
        for t in treffer:
            s = t["show"]
            serie = f"{s['name']} ({(s.get('premiered') or '')[:4]})"
            if staffeln is None:
                bilder = hole_json(f"https://api.tvmaze.com/shows/{s['id']}/images")
                for b in bilder:
                    if b.get("type") == "poster":
                        o = b["resolutions"]["original"]
                        groesse = f", {o['width']}x{o['height']}" if o.get("width") else ""
                        ergebnis.append(Kandidat(f"{serie}{groesse}", o["url"], name))
                continue
            for st in hole_json(f"https://api.tvmaze.com/shows/{s['id']}/seasons"):
                url = (st.get("image") or {}).get("original")
                if url and st.get("number") is not None and passt(staffeln, st["number"]):
                    ergebnis.append(Kandidat(f"{serie} - {staffel_name(st['number'])}", url,
                                             staffel_name(st["number"])))
        return ergebnis


# ------------------------- iTunes -------------------------

class Itunes:
    name = "iTunes"

    def aktiv(self, typ):
        return typ == "serie"

    def kandidaten(self, name, typ, staffeln):
        if staffeln is None:
            return []  # iTunes hat nur Staffelcover
        url = "https://itunes.apple.com/search?" + urllib.parse.urlencode(
            {"term": name, "media": "tvShow", "entity": "tvSeason", "country": "de", "limit": 50})
        ergebnis = []
        for t in hole_json(url).get("results", []):
            titel = t.get("collectionName") or ""
            m = re.search(r"(?:Staffel|Season)\s*(\d+)", titel, re.I)
            nr = int(m.group(1)) if m else None
            if nr is None or not passt(staffeln, nr) or not t.get("artworkUrl100"):
                continue
            gross = re.sub(r"/\d+x\d+bb\.", "/3000x3000bb.", t["artworkUrl100"])
            ergebnis.append((nr, Kandidat(titel + " (quadratisches Cover)", gross, staffel_name(nr))))
        return [k for _, k in sorted(ergebnis, key=lambda x: x[0])]


# ------------------------- Wikipedia -------------------------

class Wikipedia:
    """Plakat aus der Infobox des englischen Wikipedia-Artikels."""
    name = "Wikipedia"

    def aktiv(self, typ):
        return True

    def _api(self, sprache, **parameter):
        parameter.update(format="json", formatversion=2)
        return hole_json(f"https://{sprache}.wikipedia.org/w/api.php?" + urllib.parse.urlencode(parameter))

    def _englische_titel(self, name, typ):
        titel = []
        zusatz_de = " Film" if typ == "film" else " Fernsehserie"
        zusatz_en = " film" if typ == "film" else " TV series"
        # Deutsche Artikel suchen und auf den englischen Artikel umsteigen
        de = [t["title"] for t in self._api("de", action="query", list="search", srsearch=name + zusatz_de,
                                            srlimit=4).get("query", {}).get("search", [])]
        if de:
            seiten = self._api("de", action="query", prop="langlinks", lllang="en",
                               titles="|".join(de)).get("query", {}).get("pages", [])
            reihenfolge = {t: i for i, t in enumerate(de)}
            for s in sorted(seiten, key=lambda s: reihenfolge.get(s["title"], 99)):
                for l in s.get("langlinks", []):
                    titel.append(l["title"])
        for t in self._api("en", action="query", list="search", srsearch=name + zusatz_en,
                           srlimit=4).get("query", {}).get("search", []):
            titel.append(t["title"])
        return list(dict.fromkeys(titel))[:6]

    def _infobox_bild(self, titel):
        try:
            text = self._api("en", action="parse", page=titel, prop="text", section=0,
                             redirects=1)["parse"]["text"]
        except Exception:
            return None
        box = re.search(r'<table class="infobox.*?</table>', text, re.S)
        if not box:
            return None
        m = re.search(r'<img [^>]*src="([^"]+)"', box.group(0))
        if not m:
            return None
        src = html.unescape(m.group(1)).split("?")[0]  # Tracking-Parameter weg
        if src.startswith("//"):
            src = "https:" + src
        # Vorschaubild -> Original: .../thumb/a/ab/Datei.jpg/220px-Datei.jpg -> .../a/ab/Datei.jpg
        o = re.match(r"https://(?:upload|thumb)\.wikimedia\.org/wikipedia/([^/]+)/thumb/(.+?)/[^/]+$", src)
        return f"https://upload.wikimedia.org/wikipedia/{o.group(1)}/{o.group(2)}" if o else src

    def kandidaten(self, name, typ, staffeln):
        ergebnis = []
        for titel in self._englische_titel(name, typ):
            if staffeln is None:
                bild = self._infobox_bild(titel)
                if bild:
                    ergebnis.append(Kandidat(f"{titel} (en.wikipedia)", bild, name))
                continue
            basis = re.sub(r"\s*\((?:TV series|\d{4} TV series)\)$", "", titel)
            nummern = staffeln if staffeln != "alle" else range(1, 31)
            fehlend = 0
            for nr in nummern:
                bild = self._infobox_bild(f"{basis} season {nr}")
                if bild:
                    ergebnis.append(Kandidat(f"{basis} season {nr} (en.wikipedia)", bild, staffel_name(nr)))
                    fehlend = 0
                elif staffeln == "alle":
                    fehlend += 1
                    if fehlend >= 2:
                        break
            if ergebnis:
                break  # passende Serie gefunden
        return ergebnis


QUELLEN = [Tmdb(), Tvmaze(), Itunes(), Wikipedia()]


# ------------------------- Speichern -------------------------

def sauberer_dateiname(name):
    name = re.sub(r'[\\/:*?"<>|]', "", name)
    return " ".join(name.split()).strip(" .") or "Plakat"


def freier_pfad(ordner, name):
    pfad = os.path.join(ordner, name + ".jpg")
    i = 2
    while os.path.exists(pfad):
        pfad = os.path.join(ordner, f"{name} ({i}).jpg")
        i += 1
    return pfad


def speichere(kandidat, ordner):
    daten = hole(kandidat.url)
    os.makedirs(ordner, exist_ok=True)
    pfad = freier_pfad(ordner, sauberer_dateiname(kandidat.dateiname))
    groesse = ""
    try:
        from PIL import Image
        bild = Image.open(io.BytesIO(daten))
        groesse = f" ({bild.width}x{bild.height})"
        if bild.format != "JPEG":
            if bild.mode in ("RGBA", "LA", "P"):
                bild = bild.convert("RGBA")
                hintergrund = Image.new("RGB", bild.size, (255, 255, 255))
                hintergrund.paste(bild, mask=bild.split()[-1])
                bild = hintergrund
            else:
                bild = bild.convert("RGB")
            bild.save(pfad, "JPEG", quality=JPG_QUALITAET)
            return pfad + groesse + " - umgewandelt"
    except ImportError:
        if not daten.startswith(b"\xff\xd8"):
            endung = os.path.splitext(urllib.parse.urlparse(kandidat.url).path)[1] or ".bild"
            pfad = pfad[:-4] + endung
            with open(pfad, "wb") as f:
                f.write(daten)
            return pfad + "  (kein JPG - zum Umwandeln: python -m pip install pillow)"
    with open(pfad, "wb") as f:
        f.write(daten)
    return pfad + groesse


# ------------------------- Abfragen -------------------------

def frage(text, standard=""):
    antwort = input(f"{text}" + (f" [{standard}]" if standard else "") + ": ")
    return antwort.replace("﻿", "").strip() or standard


def lies_staffeln(text):
    """'' -> None (Serienplakat), 'alle', '3', '1,3', '2-4'"""
    text = text.strip().lower()
    if not text:
        return None
    if text in ("alle", "a", "*"):
        return "alle"
    nummern = set()
    for teil in re.split(r"[,\s]+", text):
        if re.fullmatch(r"\d+-\d+", teil):
            a, b = map(int, teil.split("-"))
            nummern.update(range(min(a, b), max(a, b) + 1))
        elif teil.isdigit():
            nummern.add(int(teil))
        else:
            raise ValueError(teil)
    return sorted(nummern)


def lies_auswahl(text, anzahl):
    text = text.strip().lower()
    if text in ("a", "alle"):
        return list(range(anzahl))
    gewaehlt = []
    for teil in re.split(r"[,\s]+", text):
        if re.fullmatch(r"\d+-\d+", teil):
            a, b = map(int, teil.split("-"))
            gewaehlt += range(min(a, b) - 1, max(a, b))
        elif teil.isdigit():
            gewaehlt.append(int(teil) - 1)
        else:
            return None
    return gewaehlt if all(0 <= g < anzahl for g in gewaehlt) else None


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass

    name = " ".join(sys.argv[1:]).strip() or frage("Titel (Serie oder Film)")
    if not name:
        return
    typ = "film" if frage("Film oder Serie? (f/s)", "s").lower().startswith("f") else "serie"

    staffeln = None
    if typ == "serie":
        while True:
            try:
                staffeln = lies_staffeln(frage("Staffel (leer = Serienplakat, z.B. 3, 1,3, 2-4, alle)"))
                break
            except ValueError:
                print("Bitte eine Zahl, eine Liste (1,3), einen Bereich (2-4) oder 'alle' eingeben.")

    basis = ZIEL_ORDNER.strip() or os.path.join(os.path.dirname(os.path.abspath(__file__)), "Plakate")
    ordner = os.path.join(basis, sauberer_dateiname(name)) if typ == "serie" else basis

    quellen = [q for q in QUELLEN if q.aktiv(typ)]
    print("Quellen:", " -> ".join(q.name for q in quellen))
    if not TMDB_API_KEY.strip():
        print("(TMDB übersprungen - mit TMDB_API_KEY gibt es mehr und deutsche Plakate)")

    for i, quelle in enumerate(quellen):
        letzte = i == len(quellen) - 1
        print(f"\nSuche bei {quelle.name} ...")
        try:
            kandidaten = quelle.kandidaten(name, typ, staffeln)
        except Exception as fehler:
            print(f"{quelle.name}: Fehler ({fehler})")
            continue
        if not kandidaten:
            print(f"{quelle.name}: keine Bilder gefunden")
            continue

        kandidaten = kandidaten[:30]
        for nr, k in enumerate(kandidaten, 1):
            print(f"  {nr:>2}. {k.beschreibung}")
            print(f"      {k.url}")
        print("   0. nichts davon" + ("" if letzte else " - nächste Quelle"))

        while True:
            antwort = frage("Welche herunterladen? (z.B. 2 | 1,3 | 1-4 | a = alle)", "1")
            if antwort == "0":
                auswahl = []
                break
            auswahl = lies_auswahl(antwort, len(kandidaten))
            if auswahl is not None:
                break
            print("Ungültige Auswahl.")
        if not auswahl:
            continue

        for nr in auswahl:
            try:
                print("  gespeichert:", speichere(kandidaten[nr], ordner))
            except Exception as fehler:
                print(f"  Fehler bei {kandidaten[nr].beschreibung}: {fehler}")
        return

    print("\nKeine Plakate heruntergeladen.")


if __name__ == "__main__":
    main()
