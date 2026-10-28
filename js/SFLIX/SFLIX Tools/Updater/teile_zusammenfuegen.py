# -*- coding: utf-8 -*-
"""
Findet gesplittete Videos (z.B. "Titel pt1.mp4", "Titel pt2.mp4") und fügt sie nach
Rückfrage mit ffmpeg zu einer Datei zusammen ("Titel.mp4").

Aufruf:
    python teile_zusammenfuegen.py

Erkannt werden Namen, die auf pt1, pt2, ... enden, auch mit Trennzeichen davor oder
als "part": "Titel pt1", "Titel - pt 1", "Titel_PT1", "Titel.part1". Alle Teile müssen
im selben Ordner liegen, dieselbe Endung haben und lückenlos ab 1 nummeriert sein.

Zuerst werden alle Funde nacheinander abgefragt, danach werden alle ausgewählten
Videos am Stück verarbeitet (man kann also nach den Fragen weggehen).
Pro Fund:  j = zusammenfügen,  n = überspringen,  a = alle weiteren ohne Rückfrage
zusammenfügen,  q = keine weiteren Fragen (bisher Ausgewähltes wird verarbeitet).

Ablauf beim Zusammenfügen:
  1. Haben alle Teile dasselbe Format (Codec, Auflösung, Ton), werden sie verlustfrei
     und schnell aneinandergehängt. Sonst wird gefragt, ob neu kodiert werden soll
     (dauert deutlich länger).
  2. Die Länge des Ergebnisses wird mit der Summe der Teile verglichen.
  3. Erst wenn alles passt, wird mit den Teilen gemacht, was bei TEILE eingestellt ist.
Vorhandene Dateien werden nie überschrieben.

Tipp: Vor serien_umbenennen.py ausführen. Sonst hält das Umbenennungsskript pt1 und
pt2 für zwei Kopien derselben Folge und hängt "_DUPLIKAT" an.

Benötigt ffmpeg (https://ffmpeg.org), z.B. installieren mit:
    winget install Gyan.FFmpeg
"""

# ======================= Einstellungen =======================

# Ordner, der durchsucht wird (mit allen Unterordnern), z.B. "M:/Serien"
ORDNER = "M:/Filmen"

# Was nach erfolgreichem Zusammenfügen mit den Teilen passiert:
#   "behalten"    = Teile bleiben liegen
#   "verschieben" = Teile kommen in den Unterordner TEILE_ORDNER_NAME
TEILE = "verschieben"
TEILE_ORDNER_NAME = "__teile"

# Nur diese Dateitypen werden beachtet
VIDEO_ENDUNGEN = (".mp4", ".m4v", ".webm", ".ogv")

# Pfad zu ffmpeg/ffprobe. Leer = im PATH suchen.
# Beispiel: r"C:\ffmpeg\bin"
FFMPEG_ORDNER = r"C:\Users\rbald\Downloads\ffmpeg\bin"

# Erlaubte Abweichung der Gesamtlänge in Sekunden
TOLERANZ_SEKUNDEN = 2.0

# =============================================================

import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import unicodedata

TEIL_RE = re.compile(r"^(?P<titel>.*?)[\s._-]*(?:pt|part)[\s._-]*(?P<nr>\d{1,2})$", re.I)


def norm(name):
    return unicodedata.normalize("NFC", name).casefold()


def werkzeug(name):
    if FFMPEG_ORDNER:
        pfad = os.path.join(FFMPEG_ORDNER, name + (".exe" if os.name == "nt" else ""))
        return pfad if os.path.isfile(pfad) else None
    return shutil.which(name)


def groesse_text(pfad):
    b = os.path.getsize(pfad)
    if b >= 1024 ** 3:
        return f"{b / 1024 ** 3:.2f} GB"
    if b >= 1024 ** 2:
        return f"{b / 1024 ** 2:.0f} MB"
    return f"{b / 1024:.0f} KB"


def zeit_text(sekunden):
    if sekunden is None:
        return "?"
    s = int(round(sekunden))
    return f"{s // 3600}:{s % 3600 // 60:02d}:{s % 60:02d}"


# ------------------------- Suchen -------------------------

def finde_gruppen(ordner):
    """Liefert Liste von (zieldatei, [teil1, teil2, ...])."""
    gruppen = []
    for wurzel, unterordner, dateien in os.walk(ordner):
        unterordner[:] = [o for o in unterordner if norm(o) != norm(TEILE_ORDNER_NAME)]
        kandidaten = {}  # (titel, endung) -> {nr: datei}
        for datei in dateien:
            stamm, endung = os.path.splitext(datei)
            if endung.lower() not in VIDEO_ENDUNGEN:
                continue
            m = TEIL_RE.match(stamm)
            if not m or not m.group("titel").strip():
                continue
            titel = m.group("titel").rstrip(" ._-")
            schluessel = (norm(titel), endung.lower())
            eintrag = kandidaten.setdefault(schluessel, {"titel": titel, "endung": endung, "teile": {}})
            eintrag["teile"].setdefault(int(m.group("nr")), []).append(datei)

        for eintrag in kandidaten.values():
            teile = eintrag["teile"]
            nummern = sorted(teile)
            if len(nummern) < 2 or nummern != list(range(1, len(nummern) + 1)):
                if len(nummern) >= 2 or nummern != [1]:
                    print(f"[UNVOLLSTÄNDIG] {os.path.join(wurzel, eintrag['titel'])}: "
                          f"Teile {nummern} - übersprungen")
                continue
            if any(len(teile[n]) > 1 for n in nummern):
                print(f"[MEHRDEUTIG] {os.path.join(wurzel, eintrag['titel'])}: "
                      f"ein Teil existiert mehrfach - übersprungen")
                continue
            ziel = os.path.join(wurzel, eintrag["titel"] + eintrag["endung"])
            gruppen.append((ziel, [os.path.join(wurzel, teile[n][0]) for n in nummern]))
    return sorted(gruppen)


# ------------------------- ffprobe / ffmpeg -------------------------

def untersuche(pfad):
    """Dauer und Stream-Eckdaten einer Datei."""
    ausgabe = subprocess.run(
        [werkzeug("ffprobe"), "-v", "error", "-show_entries",
         "format=duration:stream=codec_type,codec_name,width,height,sample_rate,channels",
         "-of", "json", pfad],
        capture_output=True, text=True, encoding="utf-8", errors="replace")
    if ausgabe.returncode != 0:
        raise RuntimeError(ausgabe.stderr.strip() or "ffprobe-Fehler")
    daten = json.loads(ausgabe.stdout or "{}")
    dauer = float(daten.get("format", {}).get("duration") or 0) or None
    streams = tuple(
        (s.get("codec_type"), s.get("codec_name"), s.get("width"), s.get("height"),
         s.get("sample_rate"), s.get("channels"))
        for s in daten.get("streams", []) if s.get("codec_type") in ("video", "audio"))
    hat_ton = any(s[0] == "audio" for s in streams)
    return dauer, streams, hat_ton


def concat_liste(teile, ordner):
    liste = os.path.join(ordner, "teile.txt")
    with open(liste, "w", encoding="utf-8") as f:
        for t in teile:
            f.write("file '" + os.path.abspath(t).replace("'", "'\\''") + "'\n")
    return liste


def fuege_zusammen(teile, ziel, neu_kodieren, hat_ton, breite, hoehe):
    with tempfile.TemporaryDirectory() as tmp:
        if not neu_kodieren:
            befehl = [werkzeug("ffmpeg"), "-hide_banner", "-loglevel", "error", "-y",
                      "-f", "concat", "-safe", "0", "-i", concat_liste(teile, tmp),
                      "-map", "0", "-c", "copy"]
        else:
            befehl = [werkzeug("ffmpeg"), "-hide_banner", "-loglevel", "error", "-y"]
            for t in teile:
                befehl += ["-i", t]
            filter_teile, eingaenge = [], ""
            for i in range(len(teile)):
                # alle Teile auf die Auflösung von Teil 1 bringen
                filter_teile.append(f"[{i}:v:0]scale={breite}:{hoehe}:force_original_aspect_ratio=decrease,"
                                    f"pad={breite}:{hoehe}:(ow-iw)/2:(oh-ih)/2,setsar=1[v{i}]")
                eingaenge += f"[v{i}]" + (f"[{i}:a:0]" if hat_ton else "")
            filter_teile.append(f"{eingaenge}concat=n={len(teile)}:v=1:a={1 if hat_ton else 0}[v]"
                                + ("[a]" if hat_ton else ""))
            befehl += ["-filter_complex", ";".join(filter_teile), "-map", "[v]"]
            if hat_ton:
                befehl += ["-map", "[a]", "-c:a", "aac", "-b:a", "192k"]
            befehl += ["-c:v", "libx264", "-crf", "20", "-preset", "medium"]
        if ziel.lower().endswith((".mp4", ".m4v")):
            befehl += ["-movflags", "+faststart"]
        befehl.append(ziel)
        ergebnis = subprocess.run(befehl, capture_output=True, text=True, encoding="utf-8", errors="replace")
        if ergebnis.returncode != 0:
            raise RuntimeError(ergebnis.stderr.strip()[-800:] or "ffmpeg-Fehler")


# ------------------------- Ablauf -------------------------

def frage(text, erlaubt):
    while True:
        antwort = input(text).replace("\ufeff", "").strip().lower()
        if antwort in erlaubt:
            return antwort
        print("Bitte " + " / ".join(erlaubt) + " eingeben.")


def frage_ab(ziel, teile, auto):
    """Phase 1: zeigt einen Fund und fragt nach. Liefert (auftrag oder None, status, auto).
    status: ja / uebersprungen / fehler / ende"""
    print("\n" + "=" * 70)
    print("Gefunden in", os.path.dirname(ziel))
    infos = []
    for t in teile:
        try:
            dauer, streams, hat_ton = untersuche(t)
        except Exception as fehler:
            print(f"  {os.path.basename(t)}: kann nicht gelesen werden ({fehler}) - übersprungen")
            return None, "fehler", auto
        infos.append((dauer, streams, hat_ton))
        print(f"  {os.path.basename(t)}   {zeit_text(dauer)}   {groesse_text(t)}")
    gesamt = sum(d or 0 for d, _, _ in infos)
    print(f"  -> {os.path.basename(ziel)}   {zeit_text(gesamt)}")

    if os.path.exists(ziel):
        print("  Zieldatei gibt es schon - übersprungen (nichts überschrieben).")
        return None, "uebersprungen", auto

    if auto:
        print("  -> wird zusammengefügt (a = alle)")
    else:
        antwort = frage("Zusammenfügen? (j = ja, n = nein, a = alle weiteren ohne Frage, q = keine weiteren Fragen): ",
                        ["j", "n", "a", "q"])
        if antwort == "q":
            return None, "ende", auto
        if antwort == "n":
            return None, "uebersprungen", auto
        if antwort == "a":
            auto = True

    neu_kodieren = False
    if not all(i[1] == infos[0][1] for i in infos):
        print("  Achtung: Die Teile haben unterschiedliche Formate:")
        for t, (_, streams, _) in zip(teile, infos):
            print(f"    {os.path.basename(t)}: {streams}")
        # Diese Frage kommt auch bei "a", weil Neukodieren lange dauert
        if frage("  Neu kodieren? Das dauert deutlich länger. (j/n): ", ["j", "n"]) == "n":
            return None, "uebersprungen", auto
        neu_kodieren = True

    video = next((s for s in infos[0][1] if s[0] == "video"), None)
    auftrag = {
        "ziel": ziel,
        "teile": teile,
        "gesamt": gesamt,
        "neu_kodieren": neu_kodieren,
        "hat_ton": all(i[2] for i in infos),
        "breite": video[2] if video else 1280,
        "hoehe": video[3] if video else 720,
    }
    return auftrag, "ja", auto


def verarbeite(auftrag, nr, anzahl):
    """Phase 2: fügt einen Auftrag zusammen. Liefert zusammengefuegt / uebersprungen / fehler."""
    ziel, teile, gesamt = auftrag["ziel"], auftrag["teile"], auftrag["gesamt"]
    print(f"\n[{nr}/{anzahl}] {ziel}")
    if os.path.exists(ziel):
        print("  Zieldatei gibt es inzwischen - übersprungen (nichts überschrieben).")
        return "uebersprungen"
    if not all(os.path.isfile(t) for t in teile):
        print("  Ein Teil fehlt inzwischen - übersprungen.")
        return "fehler"

    stamm, endung = os.path.splitext(ziel)
    temp = stamm + ".__temp__" + endung
    print("  füge zusammen" + (" (neu kodieren) ..." if auftrag["neu_kodieren"] else " ..."), flush=True)
    try:
        fuege_zusammen(teile, temp, auftrag["neu_kodieren"], auftrag["hat_ton"],
                       auftrag["breite"], auftrag["hoehe"])
        dauer_neu, _, _ = untersuche(temp)
        if dauer_neu is None or abs(dauer_neu - gesamt) > max(TOLERANZ_SEKUNDEN, gesamt * 0.005):
            raise RuntimeError(f"Länge passt nicht: Ergebnis {zeit_text(dauer_neu)}, "
                               f"erwartet {zeit_text(gesamt)}")
        os.rename(temp, ziel)
    except Exception as fehler:
        if os.path.exists(temp):
            os.remove(temp)  # nur die eigene, unfertige Zwischendatei
        print(f"  FEHLER: {fehler}")
        print("  Die Teile sind unverändert.")
        return "fehler"

    print(f"  fertig: {os.path.basename(ziel)} ({zeit_text(dauer_neu)}, {groesse_text(ziel)})")
    if TEILE == "verschieben":
        ablage = os.path.join(os.path.dirname(ziel), TEILE_ORDNER_NAME)
        os.makedirs(ablage, exist_ok=True)
        for t in teile:
            neu = os.path.join(ablage, os.path.basename(t))
            i = 2
            while os.path.exists(neu):
                s, e = os.path.splitext(os.path.basename(t))
                neu = os.path.join(ablage, f"{s} ({i}){e}")
                i += 1
            os.rename(t, neu)
        print(f"  Teile verschoben nach {TEILE_ORDNER_NAME}\\")
    return "zusammengefuegt"


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass

    if not werkzeug("ffmpeg") or not werkzeug("ffprobe"):
        print("ffmpeg/ffprobe nicht gefunden.")
        print("Installieren z.B. mit:  winget install Gyan.FFmpeg")
        print("(danach neues Terminal öffnen) oder FFMPEG_ORDNER im Skript setzen.")
        sys.exit(1)
    if not os.path.isdir(ORDNER):
        print(f"Ordner nicht gefunden: {ORDNER}")
        sys.exit(1)

    print(f"Suche gesplittete Videos in {ORDNER} ...")
    gruppen = finde_gruppen(ORDNER)
    print(f"{len(gruppen)} zusammengehörige Teil-Gruppen gefunden.")

    # Phase 1: alle Fragen stellen
    zaehler, auftraege, auto = {}, [], False
    for ziel, teile in gruppen:
        auftrag, status, auto = frage_ab(ziel, teile, auto)
        if status == "ende":
            break
        if auftrag:
            auftraege.append(auftrag)
        else:
            zaehler[status] = zaehler.get(status, 0) + 1

    # Phase 2: alles verarbeiten
    if auftraege:
        print("\n" + "#" * 70)
        print(f"Alle Fragen beantwortet. {len(auftraege)} Video(s) werden jetzt zusammengefügt:")
        for a in auftraege:
            print("  -", a["ziel"] + ("  (neu kodieren)" if a["neu_kodieren"] else ""))
        for nr, auftrag in enumerate(auftraege, 1):
            status = verarbeite(auftrag, nr, len(auftraege))
            zaehler[status] = zaehler.get(status, 0) + 1
    else:
        print("\nNichts zum Zusammenfügen ausgewählt.")

    print("\nZusammenfassung:")
    for status, text in (("zusammengefuegt", "zusammengefügt"), ("uebersprungen", "übersprungen"),
                         ("fehler", "Fehler")):
        print(f"  {text:<16} {zaehler.get(status, 0)}")


if __name__ == "__main__":
    main()
