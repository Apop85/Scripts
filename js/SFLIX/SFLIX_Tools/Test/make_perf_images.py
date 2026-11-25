# Erzeugt einen Testordner mit vielen grossen Bildern fuer Leistungsmessungen des Bildbetrachters
# (SFLIX-20261124-136). Standard: 2000 JPEG 4000x3000 (je ca. 2-3 MB) in
# .claude\testenv\Bilder\_Perftest. Es werden 20 verschiedene Bilder erzeugt und kopiert (die Kosten
# beim Laden und Dekodieren haengen von Groesse und Aufloesung ab, nicht vom Inhalt).
#   python SFLIX_Tools\Test\make_perf_images.py [Anzahl] [--loeschen]
# Danach in der Testumgebung "Daten einlesen" ausfuehren (STEFFLIX.exe mit SFLIX_DIR auf .claude\testenv).
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
TARGET = os.path.join(ROOT, ".claude", "testenv", "Bilder", "_Perftest")


def make_base(index, path):
    import numpy as np
    from PIL import Image
    rng = np.random.default_rng(index)
    w, h = 4000, 3000
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    r = (np.sin(x / (180 + index * 7)) * 90 + 120)
    g = (np.cos(y / (150 + index * 5)) * 80 + 110)
    b = ((x + y) / (w + h) * 200 + 30)
    img = np.stack([r, g, b], axis=-1)
    img += rng.normal(0, 9, size=img.shape).astype(np.float32)
    img = np.clip(img, 0, 255).astype(np.uint8)
    Image.fromarray(img, "RGB").save(path, "JPEG", quality=88)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if "--loeschen" in sys.argv:
        if os.path.isdir(TARGET):
            shutil.rmtree(TARGET)
        print("geloescht:", TARGET)
        return
    count = int(args[0]) if args else 2000
    os.makedirs(TARGET, exist_ok=True)
    bases = []
    for i in range(20):
        p = os.path.join(TARGET, "Bild %04d.jpg" % (i + 1))
        if not os.path.exists(p):
            make_base(i, p)
        bases.append(p)
    print("Groesse eines Bildes: %.1f MB" % (os.path.getsize(bases[0]) / 1e6))
    for i in range(20, count):
        p = os.path.join(TARGET, "Bild %04d.jpg" % (i + 1))
        if not os.path.exists(p):
            shutil.copyfile(bases[i % 20], p)
    print(count, "Bilder in", TARGET)


if __name__ == "__main__":
    main()
