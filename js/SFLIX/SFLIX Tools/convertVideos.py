#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Browser Video Converter
=======================

Konvertiert Videos rekursiv in browserkompatible MP4-Dateien.

Eigenschaften
-------------
- Resume nach Abbruch (JSON)
- Automatische GPU-Erkennung
- CPU-Fallback
- Überspringt bereits kompatible Dateien
- Mehrere Videos parallel
- Validierung der Ausgabe
- Log-Datei
- Temporäre Ausgabedateien
- Übernimmt alle Audio-/Untertitel-/Metadaten
"""

from __future__ import annotations

import concurrent.futures
import json
import logging
import os
import subprocess
import sys
import threading
import time
import shutil
import socket

from pathlib import Path
from dataclasses import dataclass
from typing import Optional

###########################################################################
# KONFIGURATION
###########################################################################

# ffmpeg
FFMPEG = Path(r"C:\Users\rbald\Downloads\ffmpeg\bin\ffmpeg.exe")
FFPROBE = Path(r"C:\Users\rbald\Downloads\ffmpeg\bin\ffprobe.exe")

# Bei vorhandener Queue - Filesystem auf Änderungen / neue Dateien durchsuchen
RESCAN_QUEUE = False

# Ordner mit den Videodateien
SOURCE_FOLDER = Path(r"C:\Users\rbald\Videos")
# Zielordner
OUTPUT_FOLDER = SOURCE_FOLDER / "converted"

# Qualität
PRESET = "medium"
QUALITY_SD = 21
QUALITY_HD = 19
QUALITY_FULLHD = 18
QUALITY_4K = 17

# mehrere Videos gleichzeitig
MAX_PARALLEL_JOBS = max(1, os.cpu_count() // 2)

# Original löschen?
DELETE_ORIGINAL = False

# bereits kompatible Videos überspringen
SKIP_ALREADY_COMPATIBLE = True

# Dateiendungen
VIDEO_EXTENSIONS = {
    ".mp4",
    ".mkv",
    ".avi",
    ".mov",
    ".m4v",
    ".wmv",
    ".flv",
    ".webm",
    ".ts",
    ".mpeg",
    ".mpg",
}

# PROGRESS_FILE = TARGET_FOLDER / "conversion_progress.json"
STATE_FILE = OUTPUT_FOLDER / "conversion_state.json"
LOG_FILE = OUTPUT_FOLDER / "conversion.log"

###########################################################################
# LOGGING
###########################################################################

logger = logging.getLogger(__name__)


def setup_logging():
    # Erst nach create_folder() aufrufen: basicConfig schlägt fehl, wenn der Ordner fehlt
    logging.basicConfig(
        filename=LOG_FILE,
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(message)s",
    )

###########################################################################
# DATENKLASSEN
###########################################################################

@dataclass
class VideoInfo:
    path: Path
    duration: float
    video_codec: str
    audio_codec: str
    pixel_format: str
    container: str

###########################################################################
# QUEUE ERSTELLEN / AKTUALISIEREN
###########################################################################

def create_or_update_queue(state):
    existing = {
        (
            job["source"]
            +
            f"|audio:{job['audio_stream']}"
        ): job
        for job in state["jobs"]
    }

    print("Scanning files... ", end="")

    for video in find_videos():
        stat = video.stat()
        jobs = create_jobs(video)

        for job in jobs:
            job["size"] = stat.st_size
            job["mtime"] = stat.st_mtime

            key = (
                job["source"]
                +
                f"|audio:{job['audio_stream']}"
            )

            if key not in existing:
                existing[key] = job

            else:
                old_job = existing[key]

                # Datei geändert -> neu konvertieren
                if (
                    old_job["size"] != stat.st_size
                    or
                    old_job["mtime"] != stat.st_mtime
                ):
                    old_job["status"] = "pending"
                    old_job["size"] = stat.st_size
                    old_job["mtime"] = stat.st_mtime

    state["jobs"] = list(existing.values())
    save_state(state)
    print("Done")

    return state

###########################################################################
# PERSISTENTE QUEUE
###########################################################################

def load_state():
    print("Queue needs to be created...")
    if not STATE_FILE.exists():
        return {
            "jobs": []
        }

    print("Loading queue... ", end="")
    with open(
        STATE_FILE,
        "r",
        encoding="utf8"
    ) as f:
        print("Done")
        return json.load(f)
    

def save_state(state):

    tmp = STATE_FILE.with_suffix(".tmp")

    with open(
        tmp,
        "w",
        encoding="utf8"
    ) as f:

        json.dump(
            state,
            f,
            indent=4,
            ensure_ascii=False
        )

    tmp.replace(STATE_FILE)

###########################################################################
# DATEIEN SUCHEN
###########################################################################

def find_videos():
    videos = []

    for file in SOURCE_FOLDER.rglob("*"):
        if not file.is_file():
            continue

        if file.name.startswith("._"):
            continue

        # Outputordner ignorieren
        try:
            file.relative_to(
                OUTPUT_FOLDER
            )
            continue
        
        except ValueError:
            pass

        if file.suffix.lower() in VIDEO_EXTENSIONS:
            videos.append(file)

    videos.sort()

    return videos

###########################################################################
# FFPROBE
###########################################################################

_PROBE_CACHE = {}
_PROBE_LOCK = threading.Lock()


def ffprobe_json(file: Path):
    """ffprobe-Ergebnis einer Datei. Wird pro Datei (Pfad, Grösse, Änderungszeit) nur
    einmal ermittelt, das spart bei Netzlaufwerken viel Zeit."""
    try:
        stat = file.stat()
        key = (str(file), stat.st_size, stat.st_mtime)
    except OSError:
        key = None

    if key is not None:
        with _PROBE_LOCK:
            if key in _PROBE_CACHE:
                return _PROBE_CACHE[key]

    data = _ffprobe_json_uncached(file)

    if key is not None and data is not None:
        with _PROBE_LOCK:
            _PROBE_CACHE[key] = data

    return data


def _ffprobe_json_uncached(file: Path):
    cmd = [
        str(FFPROBE),
        "-v",
        "quiet",
        "-print_format",
        "json",
        "-show_format",
        "-show_streams",
        str(file)
    ]

    result = subprocess.run(
        cmd,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace"
    )

    if result.returncode != 0:
        logging.warning(
            "ffprobe fehlgeschlagen: %s\n%s",
            file,
            result.stderr.strip()
        )

        return None


    if not result.stdout.strip():
        logging.warning(
            "ffprobe liefert keine Daten: %s",
            file
        )

        return None
    try:
        return json.loads(
            result.stdout
        )
    except json.JSONDecodeError:
        logging.warning(
            "ffprobe JSON ungültig: %s",
            file
        )

        return None

###########################################################################
# VIDEO INFORMATIONEN
###########################################################################

def get_video_info(file: Path) -> Optional[VideoInfo]:
    try:
        data = ffprobe_json(file)

        if not data:
            return None

        format_data = data.get("format")
        streams = data.get("streams")

        if not format_data or streams is None:
            return None

        duration = float(
            format_data.get("duration", 0)
        )

        container = format_data.get(
            "format_name",
            ""
        )

        video_codec = ""
        audio_codec = ""
        pixel = ""

        for stream in streams:
            if stream.get("codec_type") == "video":
                video_codec = stream.get(
                    "codec_name",
                    ""
                )
                pixel = stream.get(
                    "pix_fmt",
                    ""
                )

            elif stream.get("codec_type") == "audio":
                audio_codec = stream.get(
                    "codec_name",
                    ""
                )

        return VideoInfo(
            path=file,
            duration=duration,
            video_codec=video_codec,
            audio_codec=audio_codec,
            pixel_format=pixel,
            container=container
        )

    except Exception as e:
        logger.exception(e)
        return None

###########################################################################
# GPU ERKENNUNG
###########################################################################

_GPU_CACHE = None


def detect_encoder():
    """
    Erkennt automatisch den besten verfügbaren H.264-Encoder.

    Priorität:
        1. NVIDIA (h264_nvenc)
        2. Intel   (h264_qsv)
        3. AMD     (h264_amf)
        4. CPU     (libx264)
    """

    global MAX_PARALLEL_JOBS
    global _GPU_CACHE

    if _GPU_CACHE is not None:
        return _GPU_CACHE

    # "ffmpeg -encoders" reicht nicht: Die üblichen Windows-Builds enthalten alle
    # GPU-Encoder, auch wenn keine passende Grafikkarte da ist. Deshalb wird jeder
    # Encoder mit einem kurzen Testbild ausprobiert.
    names = {
        "h264_nvenc": "NVIDIA NVENC",
        "h264_qsv": "Intel QuickSync",
        "h264_amf": "AMD AMF",
    }

    _GPU_CACHE = "libx264"

    for encoder, name in names.items():
        if encoder_works(encoder):
            logger.info("Nutze %s", name)
            _GPU_CACHE = encoder
            return _GPU_CACHE

    logger.info("Nutze libx264")
    return _GPU_CACHE


def encoder_works(encoder):
    cmd = [
        str(FFMPEG), "-hide_banner", "-v", "error",
        "-f", "lavfi", "-i", "color=black:s=256x256:d=0.2",
        "-frames:v", "1",
        "-pix_fmt", pixel_format_for(encoder),
        "-c:v", encoder,
        "-f", "null", "-",
    ]
    try:
        result = subprocess.run(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            timeout=30
        )
        return result.returncode == 0
    except (OSError, subprocess.TimeoutExpired):
        return False


def pixel_format_for(encoder):
    # QuickSync arbeitet mit nv12 (ebenfalls 4:2:0, wird von ffprobe als yuv420p erkannt)
    return "nv12" if encoder == "h264_qsv" else "yuv420p"


def quality_options(encoder, quality):
    """Qualitätsparameter je Encoder. -preset medium kennen nur libx264 und QuickSync."""
    if encoder == "h264_nvenc":
        return ["-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", str(quality), "-b:v", "0"]
    if encoder == "h264_qsv":
        return ["-preset", PRESET, "-global_quality", str(quality)]
    if encoder == "h264_amf":
        return ["-quality", "balanced", "-rc", "cqp",
                "-qp_i", str(quality), "-qp_p", str(quality), "-qp_b", str(quality)]
    return ["-crf", str(quality), "-preset", PRESET]


###########################################################################
# BROWSERKOMPATIBILITÄT
###########################################################################

def is_browser_compatible(info: VideoInfo):
    if info is None:
        return False
    container = info.container.lower()
    if "mp4" not in container:
        return False
    if info.video_codec != "h264":
        return False
    # Videos ohne Ton sind ebenfalls kompatibel
    if info.audio_codec not in ("aac", ""):
        return False
    if info.pixel_format != "yuv420p":
        return False

    return True


###########################################################################
# AUSGABEDATEI
###########################################################################

def output_file(job):
    source = Path(job["source"])

    relative = source.relative_to(SOURCE_FOLDER)

    output = OUTPUT_FOLDER / relative

    output = output.with_name(
        output.stem
        + "_"
        + job["suffix"]
        + ".mp4"
    )

    output.parent.mkdir(
        parents=True,
        exist_ok=True
    )

    return output


# def temp_output_file(source: Path):
#     output = output_file(source)
#     return output.with_name(
#         output.stem + ".tmp.mp4"
#     )


###########################################################################
# FFMPEG BEFEHL
###########################################################################

def build_ffmpeg_command(
        source: Path,
        output: Path,
        audio_stream: int | None,
        copy_streams: bool = False,
        encoder: str | None = None):

    if encoder is None:
        encoder = detect_encoder()

    quality = get_quality_value(
        get_video_info(source),
        encoder
    )

    cmd = [
        str(FFMPEG),
        "-hide_banner",
        "-y",
        "-i",
        str(source),

        "-map",
        "0:v:0",
    ]

    if audio_stream is not None:
        cmd.extend([
            "-map",
            f"0:{audio_stream}",
        ])
    else:
        cmd.extend([
            "-an"
        ])

    if copy_streams:

        cmd.extend([
            "-c:v",
            "copy",
        ])

        if audio_stream is not None:
            cmd.extend([
                "-c:a",
                "copy",
            ])

    else:

        cmd.extend([
            "-c:v",
            encoder,
            "-pix_fmt",
            pixel_format_for(encoder),
        ])

        if audio_stream is not None:
            cmd.extend([
                "-c:a",
                "aac",
                "-b:a",
                "160k",
            ])

        cmd.extend(quality_options(encoder, quality))

    cmd.extend([
        "-map_metadata",
        "0",

        "-map_chapters",
        "0",

        "-movflags",
        "+faststart",

        "-fps_mode",
        "passthrough",

        str(output)
    ])

    return cmd


###########################################################################
# VALIDIERUNG
###########################################################################

def validate_output(source: Path, target: Path):

    if not target.exists():
        return False

    src = get_video_info(source)
    dst = get_video_info(target)

    if src is None or dst is None:
        return False

    if dst.video_codec != "h264":
        return False

    # Ohne Tonspur in der Quelle hat auch das Ergebnis keine
    if dst.audio_codec != ("aac" if src.audio_codec else ""):
        return False

    if dst.pixel_format != "yuv420p":
        return False

    # Dauer max. 1 Sekunde (bei langen Videos 0,5 %) Unterschied
    if abs(src.duration - dst.duration) > max(1.0, src.duration * 0.005):
        logger.warning(
            "Dauer unterschiedlich: %s (%s) -> %s (%s)",
            source,
            src.duration,
            target,
            dst.duration
        )
        return False

    return True


###########################################################################
# STATUSVERWALTUNG
###########################################################################

progress_lock = threading.Lock()

completed_counter = 0
failed_counter = 0
start_time = 0


def mark_done(state, job):
    global completed_counter

    with progress_lock:
        completed_counter += 1

        for item in state["jobs"]:
            if (
                item["source"] == job["source"]
                and item["audio_stream"] == job["audio_stream"]
            ):
                item["status"] = "done"
                item["output"] = str(output_file(job))
                item["finished"] = time.time()
                break

        save_state(state)


def mark_failed(state, job, error):
    global failed_counter

    with progress_lock:
        failed_counter += 1
        for item in state["jobs"]:
            if (
                item["source"] == job["source"]
                and item["audio_stream"] == job["audio_stream"]
            ):
                item["status"] = "failed"
                item["error"] = error
                break

        save_state(state)


###########################################################################
# FORTSCHRITT
###########################################################################

def format_seconds(seconds):

    seconds = int(seconds)

    h = seconds // 3600
    m = (seconds % 3600) // 60
    s = seconds % 60

    if h > 0:
        return f"{h:02}:{m:02}:{s:02}"

    return f"{m:02}:{s:02}"


def progress_bar(done, total, width=40):
    if total == 0:
        return "[" + ("-" * width) + "]"
    percent = done / total
    filled = int(width * percent)

    return "[" + "#" * filled + "-" * (width - filled) + "]"


def print_progress(done, total):
    global failed_counter
    global completed_counter
    elapsed = time.time() - start_time
    if done == 0:
        eta = 0
    else:
        eta = (elapsed / done) * (total - done)
    percent = (done / total * 100) if total else 100

    print(
        "\r"
        f"{progress_bar(done,total)} "
        f"{done}/{total} "
        f"{percent:5.1f}% "
        f"ETA {format_seconds(eta)}  ",
        f"OK: {completed_counter} FAILED: {failed_counter}",
        end="",
        flush=True
    )


###########################################################################
# EINZELNES VIDEO
###########################################################################

def worker(job, state):
    source = Path(job["source"])

    try:
        info = get_video_info(source)

        if info is None:
            mark_failed(
                state,
                job,
                "ffprobe fehlgeschlagen"
            )
            return

        out = output_file(job)

        if SKIP_ALREADY_COMPATIBLE and is_browser_compatible(info):

            streams = get_audio_streams(source)

            if len(streams) <= 1:

                if not out.exists():
                    out.parent.mkdir(
                        parents=True,
                        exist_ok=True
                    )

                    shutil.copy2(
                        source,
                        out
                    )

                logger.info(
                    "Bereits browserkompatibel (kopiert): %s",
                    source
                )

                mark_done(
                    state,
                    job
                )

                return

            logger.info(
                "Bereits browserkompatibel (%d Audiospuren, remux): %s",
                len(streams),
                source
            )

        # Ausgabe bereits vorhanden?
        if out.exists():
            if validate_output(source, out):
                logger.info(
                    "Ausgabe existiert bereits: %s",
                    out
                )

                mark_done(
                    state,
                    job
                )

                return

        ok = convert_job(job)

        logger.info(
            "convert_job Ergebnis %s: %s",
            job["source"],
            ok
        )

        if ok:
            mark_done(
                state,
                job
            )
        else:
            mark_failed(
                state,
                job,
                "Konvertierung fehlgeschlagen"
            )

    except Exception as e:
        logger.exception(e)

        mark_failed(
            state,
            job,
            str(e)
        )


###########################################################################
# THREADPOOL
###########################################################################

def process_all(jobs, progress):
    global completed_counter
    global failed_counter
    global start_time

    pending_jobs = [
        job
        for job in jobs
        if job["status"] == "pending"
    ]

    total = len(pending_jobs)

    completed_counter = 0
    failed_counter = 0
    start_time = time.time()

    if total == 0:
        print("Keine Videos zu konvertieren.")
        return

    print(f"{total} Jobs gefunden.")
    print()

    with concurrent.futures.ThreadPoolExecutor(
            max_workers=MAX_PARALLEL_JOBS) as executor:

        futures = [
            executor.submit(worker, job, progress)
            for job in pending_jobs
        ]

        while True:
            done = sum(
                future.done()
                for future in futures
            )

            print_progress(done, total)

            if done == total:
                break

            time.sleep(1)

        print()
        concurrent.futures.wait(futures)

    print()
    print("=" * 60)
    print("Konvertierung abgeschlossen")
    print("=" * 60)
    print(f"Erfolgreich : {completed_counter}")
    print(f"Fehler      : {failed_counter}")
    print(f"Dauer       : {format_seconds(time.time()-start_time)}")


###########################################################################
# SYSTEMPRÜFUNGEN
###########################################################################

def check_programs():
    missing = []

    if not FFMPEG.exists():
        missing.append(str(FFMPEG))

    if not FFPROBE.exists():
        missing.append(str(FFPROBE))

    if missing:
        print("Folgende Programme wurden nicht gefunden:")
        for item in missing:
            print(" -", item)

        print()
        print(
            "Bitte die Pfade am Anfang des Skripts anpassen."
        )

        sys.exit(1)


def create_folder():
    try:
        OUTPUT_FOLDER.mkdir(
            parents=True,
            exist_ok=True
        )
    except OSError as e:
        print(
            f"Zielordner kann nicht angelegt werden: {OUTPUT_FOLDER} ({e})"
        )
        sys.exit(1)


def get_quality_value(info: VideoInfo, encoder):
    # Auflösung aus ffprobe holen
    data = ffprobe_json(info.path)
    width = 0
    height = 0

    for stream in data["streams"]:
        if stream["codec_type"] == "video":
            width = stream.get("width", 0)
            height = stream.get("height", 0)
            break

    pixels = width * height

    if pixels >= 3840 * 2160:
        quality = QUALITY_4K
    elif pixels >= 1920 * 1080:
        quality = QUALITY_FULLHD
    elif pixels >= 1280 * 720:
        quality = QUALITY_HD
    else:
        quality = QUALITY_SD

    return quality


def create_jobs(video):
    streams = get_audio_streams(video)

    if not streams:
        return [{
            "source": str(video.resolve()),
            "audio_stream": None,
            "audio_number": 0,
            "language": None,
            "suffix": "default",
            "status": "pending"
        }]

    jobs = []
    used_suffixes = set()

    for number, stream in enumerate(streams, start=1):
        language = (
            stream.get("tags", {})
            .get("language")
        )

        suffix = language if language and language != "und" else f"track{number}"

        # Zwei Spuren mit derselben Sprache dürfen nicht in dieselbe Datei schreiben
        base_suffix = suffix
        counter = 2
        while suffix in used_suffixes:
            suffix = f"{base_suffix}{counter}"
            counter += 1
        used_suffixes.add(suffix)

        jobs.append({
            "source": str(video.resolve()),
            "audio_stream": stream["index"],
            "audio_number": number,
            "language": language,
            "suffix": suffix,
            "status": "pending"
        })

    return jobs

def convert_job(job):
    source = Path(job["source"])
    output = output_file(job)
    temp = output.with_suffix(".tmp.mp4")

    info = get_video_info(source)

    copy_streams = (
        SKIP_ALREADY_COMPATIBLE
        and info is not None
        and is_browser_compatible(info)
        and len(get_audio_streams(source)) > 1
    )

    cmd = build_ffmpeg_command(
        source,
        temp,
        job["audio_stream"],
        copy_streams
    )

    try:
        subprocess.run(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=True
        )
    except subprocess.CalledProcessError:
        # GPU-Encoder können an einzelnen Videos scheitern (z.B. Auflösung, Profil):
        # dann einmal mit libx264 (CPU) wiederholen
        if copy_streams or detect_encoder() == "libx264":
            raise
        logger.warning("GPU-Encoder fehlgeschlagen, wiederhole mit libx264: %s", source)
        cmd = build_ffmpeg_command(
            source,
            temp,
            job["audio_stream"],
            copy_streams,
            encoder="libx264"
        )
        subprocess.run(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            check=True
        )

    for attempt in range(20):
        try:
            os.replace(temp, output)
            break
        except PermissionError:
            if attempt == 19:
                raise
            time.sleep(0.25*attempt)

    return True

def is_smb_reachable(host, port=445, timeout=3):
    # Nur Netzwerkpfade (\\server\freigabe) prüfen; lokale Pfade wie C:\... sind immer "erreichbar"
    if not str(host).startswith("\\\\"):
        return Path(host).exists()
    host = str(host).lstrip("\\").split("\\")[0]
    try:
        with socket.create_connection((host, port), timeout):
            return True
    except (socket.timeout, OSError):
        return False


def get_audio_streams(file: Path):
    data = ffprobe_json(file)

    if data is None:
        return []

    streams = []

    for stream in data.get("streams", []):
        if stream.get("codec_type") == "audio":
            streams.append(stream)

    return streams

###########################################################################
# HAUPTPROGRAMM
###########################################################################

def main():
    print()
    if not is_smb_reachable(SOURCE_FOLDER):
        print(f"Share {SOURCE_FOLDER} nicht erreichbar")
        return
    print("=" * 60)
    print(" Browser Video Converter")
    print("=" * 60)
    print()

    check_programs()
    create_folder()
    setup_logging()

    state = load_state()

    if RESCAN_QUEUE or not state["jobs"]:
        state = create_or_update_queue(state)
    else:
        print("Bestehende Queue geladen.")

    jobs = [
        j for j in state["jobs"]
        if j["status"] == "pending"
    ]

    if not jobs:
        print("Alle Videos wurden bereits verarbeitet.")
        return

    print(f"Noch zu verarbeiten: {len(jobs)}")
    print(f"Encoder: {detect_encoder()}")
    print(f"Parallele Jobs: {MAX_PARALLEL_JOBS}")
    print()

    try:
        process_all(
            jobs,
            state
        )

    except KeyboardInterrupt:
        print()
        print("Abbruch durch Benutzer.")
        print("Der Fortschritt wurde gespeichert.")
        save_state(state)
        sys.exit(0)

###########################################################################
# START
###########################################################################

if __name__ == "__main__":
    main()