"""Testdateien für lückenhafte Videos (DEC-373, SFLIX-20261123-092/093).

Zerlegt ein Beispielvideo mit ffmpeg in HLS-Teilstücke (.ts und fMP4 .m4s), kürzt eines und lässt eines
weg, fügt die Teilstücke wie der Downloader von STEFFLIX.exe zusammen und legt die Ergebnisse in der
Testumgebung ab:

    Filme\\Lueckentest intakt.mp4   ohne Fehler (Gegenprobe: keine Lücke)
    Filme\\Lueckentest ts.mp4       .ts-Teilstücke, eines gekürzt, eines fehlt
    Filme\\Lueckentest m4s.mp4      .m4s-Teilstücke, eines gekürzt, eines fehlt

Danach sucht es die Lücken wie der Desktop (Zeitstempel der Pakete der Videospur, Sprung ab 0,5 s) und
gibt erwartete und gefundene Stellen aus.

Mit --server startet zusätzlich ein lokaler HLS-Server (nur 127.0.0.1, Testport 47123, DEC-340): Er
liefert die .ts-Teilstücke, ein Teilstück einmal als HTML-Fehlerseite und eines gekürzt (Test des
Downloaders: Prüfung jedes Teilstücks, neu laden). Beenden mit Strg+C.

    python SFLIX_Tools\\Test\\make_gap_video.py [--quelle <mp4>] [--ziel <Ordner>] [--server]
"""
import argparse
import http.server
import json
import os
import shutil
import subprocess
import sys
import threading

sys.stdout.reconfigure(encoding="utf-8")
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
TESTENV = os.path.join(ROOT, ".claude", "testenv")
FFMPEG_DIR = os.path.join(os.environ.get("LOCALAPPDATA", ""), "STEFFLIX", "ffmpeg")
GAP_THRESHOLD = 0.5   # Sekunden, wie FfmpegTools.ScanGaps
PORT = 47123


def run(args, cwd=None):
    r = subprocess.run(args, cwd=cwd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if r.returncode != 0:
        raise SystemExit("Fehler (" + str(r.returncode) + "): " + " ".join(args[:3]) + "\n" + r.stderr[-2000:])
    return r.stdout


def segments(playlist):
    """[(Name, Dauer)] aus einer Media-Playlist."""
    result, duration = [], 0.0
    for line in open(playlist, encoding="utf-8"):
        line = line.strip()
        if line.startswith("#EXTINF:"):
            duration = float(line[8:].split(",")[0])
        elif line and not line.startswith("#"):
            result.append((line, duration))
    return result


def split(ffmpeg, source, work, fmp4):
    if os.path.isdir(work):
        shutil.rmtree(work)
    os.makedirs(work)
    ext = "m4s" if fmp4 else "ts"
    args = [ffmpeg, "-hide_banner", "-v", "error", "-y", "-i", os.path.abspath(source), "-c", "copy", "-f", "hls", "-hls_time", "2",
            "-hls_playlist_type", "vod", "-hls_segment_filename", "v%05d." + ext]
    if fmp4:
        args += ["-hls_segment_type", "fmp4", "-hls_fmp4_init_filename", "vinit0.mp4"]
    run(args + ["video.m3u8"], cwd=work)
    return os.path.join(work, "video.m3u8")


def join(ffmpeg, playlist, output):
    """Wie DownloadManager.Join: ffmpeg fügt nur zusammen (-c copy)."""
    r = subprocess.run([ffmpeg, "-hide_banner", "-nostdin", "-y", "-v", "warning", "-allowed_extensions", "ALL",
                        "-protocol_whitelist", "file,crypto,data", "-i", playlist, "-map", "0:v?", "-map", "0:a?",
                        "-c", "copy", "-movflags", "+faststart", output], capture_output=True, text=True,
                       encoding="utf-8", errors="replace")
    if r.returncode != 0 or not os.path.exists(output):
        raise SystemExit("Zusammenfügen fehlgeschlagen:\n" + r.stderr[-2000:])


def damage(work, segs, fmp4):
    """Kürzt das Teilstück bei etwa 1/3 auf die Hälfte (.ts) bzw. löscht es (.m4s: ein gekürztes fMP4-
    Teilstück verdirbt alles danach, das fängt die Prüfung der Teilstücke ab) und löscht das bei etwa 2/3.
    Ergebnis: erwartete Stellen."""
    starts, t = [], 0.0
    for _, d in segs:
        starts.append(t)
        t += d
    cut, drop = len(segs) // 3, (2 * len(segs)) // 3
    cut_file = os.path.join(work, segs[cut][0])
    if fmp4:
        os.remove(cut_file)
    else:
        with open(cut_file, "r+b") as f:
            f.truncate(os.path.getsize(cut_file) // 2)
    os.remove(os.path.join(work, segs[drop][0]))
    return [("fehlt" if fmp4 else "gekürzt", starts[cut], segs[cut][1]), ("fehlt", starts[drop], segs[drop][1])]


def scan_gaps(ffprobe, path):
    """Wie FfmpegTools.ScanGaps: DTS der Pakete der ersten Videospur. Eine Lücke ist ein Abstand zweier
    Pakete, der den üblichen Bildabstand (Median) um mindestens GAP_THRESHOLD übersteigt. Die Paketdauer
    taugt nicht: Der MP4-Muxer verlängert das letzte Bild vor einer Lücke bis zum nächsten."""
    out = run([ffprobe, "-v", "error", "-select_streams", "v:0", "-show_entries", "packet=dts_time",
               "-of", "csv=p=0", path])
    times = []
    for line in out.splitlines():
        try:
            times.append(float(line.strip().split(",")[0]))
        except (ValueError, IndexError):
            continue
    diffs = [b - a for a, b in zip(times, times[1:]) if b > a]
    if not diffs:
        return []
    step = sorted(diffs)[len(diffs) // 2]
    return [(a + step, (b - a) - step) for a, b in zip(times, times[1:]) if (b - a) - step >= GAP_THRESHOLD]

def fmt(seconds):
    return "%d:%04.1f" % (int(seconds // 60), seconds % 60)


def serve(work, segs):
    """HLS-Server: ein Teilstück beim ersten Abruf als HTML-Fehlerseite, eines gekürzt."""
    bad_html, bad_short = segs[len(segs) // 4][0], segs[len(segs) // 2][0]
    served = set()

    class Handler(http.server.SimpleHTTPRequestHandler):
        def __init__(self, *a, **k):
            super().__init__(*a, directory=work, **k)

        def do_GET(self):
            name = self.path.lstrip("/").split("?")[0]
            if name == bad_html and name not in served:
                served.add(name)
                body = b"<html><body>Fehler</body></html>"
                self.send_response(200)
                self.send_header("Content-Type", "text/html")
                self.send_header("Content-Length", str(len(body)))
                self.end_headers()
                self.wfile.write(body)
                return
            if name == bad_short and name not in served:
                served.add(name)
                data = open(os.path.join(work, name), "rb").read()
                self.send_response(200)
                self.send_header("Content-Type", "video/mp2t")
                self.send_header("Content-Length", str(len(data)))
                self.end_headers()
                self.wfile.write(data[: len(data) // 2])   # Verbindung bricht ab: kürzer als Content-Length
                return
            super().do_GET()

        def log_message(self, *a):
            pass

    server = http.server.ThreadingHTTPServer(("127.0.0.1", PORT), Handler)
    print(f"\nHLS-Server: http://127.0.0.1:{PORT}/video.m3u8  (beim ersten Abruf: {bad_html} als HTML, "
          f"{bad_short} gekürzt). Beenden mit Strg+C.")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


def main():
    p = argparse.ArgumentParser(description=__doc__.split("\n")[0], allow_abbrev=False)
    p.add_argument("--quelle", default=os.path.join(TESTENV, "Filme", "Das letzte Signal.mp4"))
    p.add_argument("--ziel", default=os.path.join(TESTENV, "Filme"))
    p.add_argument("--arbeit", default=os.path.join(ROOT, "tmp", "lueckentest"))
    p.add_argument("--ffmpeg", default=FFMPEG_DIR)
    p.add_argument("--server", action="store_true", help="lokalen HLS-Server mit defekten Teilstücken starten")
    a = p.parse_args()
    ffmpeg, ffprobe = os.path.join(a.ffmpeg, "ffmpeg.exe"), os.path.join(a.ffmpeg, "ffprobe.exe")
    if not os.path.exists(ffmpeg):
        raise SystemExit("ffmpeg fehlt: " + ffmpeg)
    os.makedirs(a.ziel, exist_ok=True)
    ok = True

    work = os.path.join(a.arbeit, "intakt")
    playlist = split(ffmpeg, a.quelle, work, False)
    target = os.path.join(a.ziel, "Lueckentest intakt.mp4")
    join(ffmpeg, playlist, target)
    found = scan_gaps(ffprobe, target)
    print(f"intakt: {len(segments(playlist))} Teilstücke, Lücken gefunden: {len(found)} (erwartet 0)")
    ok &= not found

    for fmp4 in (False, True):
        name = "m4s" if fmp4 else "ts"
        work = os.path.join(a.arbeit, name)
        playlist = split(ffmpeg, a.quelle, work, fmp4)
        segs = segments(playlist)
        expected = damage(work, segs, fmp4)
        target = os.path.join(a.ziel, f"Lueckentest {name}.mp4")
        join(ffmpeg, playlist, target)
        found = scan_gaps(ffprobe, target)
        print(f"\n{name}: {len(segs)} Teilstücke")
        for what, start, dur in expected:
            print(f"  erwartet ({what}): Teilstück ab {fmt(start)}, Dauer {dur:.2f} s")
        for at, length in found:
            print(f"  gefunden: bei {fmt(at)}, Länge {length:.2f} s")
        ok &= len(found) == len(expected)

    print("\n" + ("OK: Lücken wie erwartet" if ok else "ABWEICHUNG: Schwelle bzw. Erkennung prüfen"))
    if a.server:
        work = os.path.join(a.arbeit, "server")
        playlist = split(ffmpeg, a.quelle, work, False)
        serve(work, segments(playlist))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
