"""Erzeugt das Hintergrundbild des Ladebildschirms (STEFFLIX ab 2.6.1, App ab 1.1.2).

Eine schräge Plakatwand aus erfundenen Film-, Serien- und Musikplakaten (keine echten Motive,
keine lesbaren Titel), stark unscharf und leicht abgedunkelt. Weil das Bild unscharf ist, reicht
eine kleine Auflösung: Es wird als JPG (etwa 20 KB) direkt in start.html eingebettet und als Datei
in die Assets der Android-App kopiert.

Aufruf:  python ladehintergrund.py            (schreibt start.html, App-Asset und Vorschau)
         python ladehintergrund.py --vorschau (nur die Vorschaubilder hier im Ordner)
Braucht Pillow (pip install pillow). Mit fester Zufallszahl: gleiches Ergebnis bei jedem Lauf.
"""
import base64
import io
import math
import os
import random
import re
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
RELEASE_DIR = os.path.abspath(os.path.join(HERE, "..", ".."))
START_HTML = os.path.join(RELEASE_DIR, "start.html")
APP_ASSET = os.path.join(RELEASE_DIR, "SFLIX Tools", "Android", "app", "src", "main", "assets", "loading_backdrop.jpg")
PREVIEW_SHARP = os.path.join(HERE, "ladehintergrund_scharf.jpg")
PREVIEW = os.path.join(HERE, "ladehintergrund.jpg")

SEED = 2601
WALL_W, WALL_H = 2400, 1500        # Arbeitsfläche, wird schräg gestellt und beschnitten
OUT_W, OUT_H = 560, 350            # Grösse des eingebetteten Bildes
BLUR = 26                          # Unschärfe auf der Arbeitsfläche (Variante B: nur noch Farben und Formen)
DARKEN = 0.72                      # leicht abdunkeln, den Rest macht die Abdeckung im CSS
JPEG_QUALITY = 70

# Farbstimmungen wie auf Kinoplakaten: (oben, unten, Akzent)
PALETTES = [
    ((14, 30, 58), (6, 10, 22), (255, 140, 60)),     # Nacht mit orangem Licht
    ((60, 12, 40), (12, 4, 14), (255, 70, 120)),     # Neon pink
    ((10, 54, 66), (4, 16, 22), (110, 230, 210)),    # Petrol
    ((70, 18, 12), (16, 4, 4), (255, 190, 80)),      # Rot und Gold
    ((20, 46, 28), (6, 14, 8), (200, 230, 120)),     # Wald
    ((34, 26, 72), (8, 6, 22), (160, 140, 255)),     # Violett
    ((90, 60, 20), (24, 14, 6), (255, 220, 150)),    # Wüste
    ((22, 32, 40), (6, 8, 10), (230, 235, 240)),     # Kühles Grau
    ((8, 40, 80), (2, 10, 26), (90, 190, 255)),      # Tiefsee
    ((80, 30, 60), (20, 6, 20), (255, 160, 90)),     # Sonnenuntergang
]


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def gradient(w, h, top, bottom):
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        d.line([(0, y), (w, y)], fill=lerp(top, bottom, y / max(1, h - 1)))
    return img


def glow(img, cx, cy, radius, color, strength=0.8):
    """Weiches Licht (Kreis, stark unscharf, aufaddiert)."""
    layer = Image.new("RGB", img.size, (0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=tuple(int(c * strength) for c in color))
    layer = layer.filter(ImageFilter.GaussianBlur(radius * 0.6))
    return ImageChops.add(img, layer)


def silhouette_color(pal):
    return tuple(max(0, c // 4) for c in pal[1])


def motif_landscape(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * rnd.uniform(0.3, 0.7), h * 0.42, w * 0.22, pal[2], 0.9)
    d = ImageDraw.Draw(img)
    for layer in range(3):
        base = h * (0.55 + layer * 0.09)
        pts = [(0, h)]
        x = 0
        while x <= w:
            pts.append((x, base - rnd.uniform(0, h * 0.14)))
            x += w / rnd.randint(3, 6)
        pts += [(w, base), (w, h)]
        shade = lerp(pal[1], (0, 0, 0), 0.3 + layer * 0.25)
        d.polygon(pts, fill=shade)
    return img


def motif_city(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * 0.5, h * 0.35, w * 0.35, pal[2], 0.5)
    d = ImageDraw.Draw(img)
    x = 0
    while x < w:
        bw = rnd.uniform(w * 0.08, w * 0.18)
        top = h * rnd.uniform(0.3, 0.62)
        d.rectangle([x, top, x + bw, h], fill=silhouette_color(pal))
        for wy in range(int(top + 8), int(h * 0.92), 14):
            for wx in range(int(x + 5), int(x + bw - 5), 11):
                if rnd.random() < 0.35:
                    d.rectangle([wx, wy, wx + 4, wy + 6], fill=lerp(pal[2], (255, 255, 255), 0.3))
        x += bw + rnd.uniform(1, 6)
    return img


def motif_person(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * 0.5, h * 0.4, w * 0.4, pal[2], 0.7)
    d = ImageDraw.Draw(img)
    cx = w * rnd.uniform(0.4, 0.6)
    head = w * 0.13
    d.ellipse([cx - head, h * 0.3, cx + head, h * 0.3 + head * 2.3], fill=silhouette_color(pal))
    d.polygon([(cx - w * 0.42, h), (cx - w * 0.3, h * 0.62), (cx, h * 0.55), (cx + w * 0.3, h * 0.62), (cx + w * 0.42, h)],
              fill=silhouette_color(pal))
    return img


def motif_space(img, pal, rnd):
    w, h = img.size
    d = ImageDraw.Draw(img)
    for _ in range(90):
        x, y, r = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(0.6, 2.0)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(235, 235, 245))
    img = glow(img, w * 0.6, h * 0.5, w * 0.3, pal[2], 0.6)
    d = ImageDraw.Draw(img)
    r = w * rnd.uniform(0.22, 0.3)
    cx, cy = w * 0.55, h * 0.52
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=lerp(pal[0], pal[2], 0.45))
    d.ellipse([cx - r * 1.7, cy - r * 0.25, cx + r * 1.7, cy + r * 0.25], outline=lerp(pal[2], (255, 255, 255), 0.4), width=4)
    return img


def motif_sea(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * 0.5, h * 0.45, w * 0.18, (255, 230, 200), 0.8)
    d = ImageDraw.Draw(img)
    for i in range(7):
        y = h * (0.52 + i * 0.07)
        pts = [(0, h)] + [(x, y + math.sin(x / w * math.pi * 4 + i) * 8) for x in range(0, w + 10, 10)] + [(w, h)]
        d.polygon(pts, fill=lerp(pal[0], (0, 0, 0), 0.2 + i * 0.1))
    return img


def motif_road(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * 0.5, h * 0.48, w * 0.25, pal[2], 0.9)
    d = ImageDraw.Draw(img)
    d.rectangle([0, h * 0.5, w, h], fill=silhouette_color(pal))
    d.polygon([(w * 0.47, h * 0.5), (w * 0.53, h * 0.5), (w * 0.9, h), (w * 0.1, h)], fill=(30, 30, 34))
    for i in range(6):
        t = (i + 1) / 7
        y = h * 0.5 + (h * 0.5) * t * t
        d.rectangle([w * 0.5 - 2 - t * 4, y, w * 0.5 + 2 + t * 4, y + 6 + t * 18], fill=(230, 220, 160))
    return img


def motif_forest(img, pal, rnd):
    w, h = img.size
    img = glow(img, w * 0.5, h * 0.3, w * 0.3, pal[2], 0.5)
    d = ImageDraw.Draw(img)
    for i in range(14):
        x = rnd.uniform(-w * 0.1, w * 1.1)
        th = h * rnd.uniform(0.35, 0.7)
        tw = th * 0.35
        base = h * rnd.uniform(0.85, 1.05)
        d.polygon([(x, base - th), (x - tw, base), (x + tw, base)], fill=lerp(silhouette_color(pal), pal[1], rnd.uniform(0, 0.6)))
    return img


def motif_vinyl(img, pal, rnd):
    w, h = img.size
    d = ImageDraw.Draw(img)
    cx, cy, r = w * rnd.uniform(0.45, 0.6), h * 0.5, w * 0.38
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(14, 14, 16))
    for k in range(6):
        rr = r * (0.9 - k * 0.09)
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], outline=(34, 34, 38), width=2)
    rl = r * 0.32
    d.ellipse([cx - rl, cy - rl, cx + rl, cy + rl], fill=pal[2])
    return img


def motif_shapes(img, pal, rnd):
    w, h = img.size
    d = ImageDraw.Draw(img)
    for _ in range(5):
        x, y, s = rnd.uniform(0, w), rnd.uniform(0, h), rnd.uniform(w * 0.15, w * 0.45)
        col = rnd.choice([pal[2], lerp(pal[2], (255, 255, 255), 0.4), lerp(pal[0], pal[2], 0.5)])
        if rnd.random() < 0.5:
            d.ellipse([x - s / 2, y - s / 2, x + s / 2, y + s / 2], fill=col)
        else:
            d.rectangle([x - s / 2, y - s / 6, x + s / 2, y + s / 6], fill=col)
    return glow(img, w * 0.5, h * 0.5, w * 0.3, pal[2], 0.3)


POSTER_MOTIFS = [motif_landscape, motif_city, motif_person, motif_space, motif_sea, motif_road, motif_forest]
ALBUM_MOTIFS = [motif_vinyl, motif_shapes, motif_person, motif_space]


def title_block(img, pal, rnd, album):
    """Unleserliche Titel- und Textzeilen wie auf einem Plakat."""
    w, h = img.size
    d = ImageDraw.Draw(img)
    light = lerp(pal[2], (255, 255, 255), 0.6)
    if album:
        y = h * rnd.choice([0.07, 0.82])
        d.rectangle([w * 0.08, y, w * rnd.uniform(0.45, 0.8), y + h * 0.06], fill=light)
        return
    top = rnd.random() < 0.4
    y = h * (0.08 if top else 0.72)
    d.rectangle([w * 0.12, y, w * rnd.uniform(0.6, 0.88), y + h * 0.055], fill=light)
    d.rectangle([w * 0.12, y + h * 0.075, w * rnd.uniform(0.35, 0.6), y + h * 0.1], fill=lerp(light, pal[1], 0.4))
    for i in range(3):
        yy = h * 0.9 + i * h * 0.022
        d.rectangle([w * 0.15, yy, w * 0.85, yy + h * 0.008], fill=(120, 120, 130))


def make_cover(rnd, album):
    w, h = (220, 220) if album else (200, 300)
    pal = rnd.choice(PALETTES)
    img = gradient(w, h, pal[0], pal[1])
    img = rnd.choice(ALBUM_MOTIFS if album else POSTER_MOTIFS)(img, pal, rnd)
    title_block(img, pal, rnd, album)
    return img


def make_wall(rnd):
    wall = Image.new("RGB", (WALL_W, WALL_H), (12, 12, 14))
    gap = 18
    y = -120
    while y < WALL_H + 120:
        x = -rnd.randint(0, 160)
        row_h = 300
        while x < WALL_W + 40:
            album = rnd.random() < 0.3
            cover = make_cover(rnd, album)
            oy = y + (row_h - cover.height) // 2
            wall.paste(cover, (x, oy))
            x += cover.width + gap
        y += row_h + gap
    return wall


def build(seed=SEED):
    rnd = random.Random(seed)
    wall = make_wall(rnd)
    tilted = wall.rotate(-10, resample=Image.BICUBIC, expand=False, fillcolor=(12, 12, 14))
    # Mitte ausschneiden (ohne die Ecken der Drehung), Seitenverhältnis 16:10
    cw, ch = int(WALL_W * 0.78), int(WALL_W * 0.78 * OUT_H / OUT_W)
    left, top = (WALL_W - cw) // 2, (WALL_H - ch) // 2
    sharp = tilted.crop((left, top, left + cw, top + ch))
    blurred = sharp.filter(ImageFilter.GaussianBlur(BLUR))
    blurred = Image.eval(blurred, lambda v: int(v * DARKEN))
    small = blurred.resize((OUT_W, OUT_H), Image.LANCZOS)
    return sharp, small


def jpeg_bytes(img):
    buf = io.BytesIO()
    img.save(buf, "JPEG", quality=JPEG_QUALITY, optimize=True, progressive=True)
    return buf.getvalue()


def embed(data):
    """Ersetzt das Bild zwischen den Markierungen in start.html."""
    with open(START_HTML, "r", encoding="utf-8") as f:
        html = f.read()
    pattern = re.compile(r"(/\* ladehintergrund:start \*/).*?(/\* ladehintergrund:ende \*/)", re.S)
    if not pattern.search(html):
        sys.exit("Markierungen /* ladehintergrund:start */ … /* ladehintergrund:ende */ fehlen in start.html")
    url = 'url("data:image/jpeg;base64,' + base64.b64encode(data).decode("ascii") + '")'
    html = pattern.sub(lambda m: m.group(1) + url + m.group(2), html, count=1)
    with open(START_HTML, "w", encoding="utf-8", newline="") as f:
        f.write(html)


def main():
    sharp, small = build()
    data = jpeg_bytes(small)
    sharp.resize((sharp.width // 2, sharp.height // 2), Image.LANCZOS).save(PREVIEW_SHARP, "JPEG", quality=82)
    with open(PREVIEW, "wb") as f:
        f.write(data)
    print(f"Vorschau: {PREVIEW_SHARP}")
    print(f"Ergebnis: {PREVIEW} ({len(data) // 1024} KB, {OUT_W}x{OUT_H})")
    if "--vorschau" in sys.argv:
        return
    embed(data)
    os.makedirs(os.path.dirname(APP_ASSET), exist_ok=True)
    with open(APP_ASSET, "wb") as f:
        f.write(data)
    print(f"Eingebettet in {START_HTML}")
    print(f"App-Asset: {APP_ASSET}")


if __name__ == "__main__":
    main()
