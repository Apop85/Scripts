import json
import os
import shutil
import subprocess
import unicodedata
from datetime import datetime

from sflix_update import DownloadError, download_file, enter_stefflix_dir, install_file, load_checksums

try:
    from PIL import Image, ImageOps
except ImportError:
    Image = None

print("".center(50, "="))
print("Update STEFFLIX-Daten".center(50))
print("".center(50, "="))

# Immer im eigenen Ordner arbeiten: updateData benennt Dateien um und darf nie
# versehentlich einen fremden Ordner (z.B. C:\Windows) durchsuchen
homeDir = enter_stefflix_dir()

# Muss mit den Listen in sflix_sys/js/core.js übereinstimmen (mediaTypes, musicTypes, imageTypes)
allowedFileTypes = [
    "mp4", "m4v", "webm", "ogv",                                  # Video
    "mp3", "m4a", "ogg", "oga", "opus", "aac", "flac", "wav",     # Musik
    "jpg", "jpeg", "png", "gif", "webp", "avif", "bmp",           # Bilder
]
disallowedItems = ["System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys"]
animationArray = ["Suchen", "sUchen", "suChen", "sucHen", "suchEn", "sucheN", "suchEn", "sucHen", "suChen", "sUchen"]

# Zu grosse Vorschaubilder werden direkt im Preview-Ordner auf diese Grösse zugeschnitten (16:9).
# Das Original kommt mit gleichem Namen nach Preview\__original (wie im Updater ab 3.0.4).
PREVIEW_FOLDER = "Preview"
PREVIEW_SIZE = (480, 270)
PREVIEW_QUALITY = 80
ORIGINAL_FOLDER = "__original"
# Bis Updater 3.0.3 lagen Originale als "<Dateiname>.original" im Preview-Ordner. Sie ziehen um.
ORIGINAL_SUFFIX = ".original"
migratedOriginals = 0
# Speicherformat je Endung. Andere Formate (z.B. avif, gif) bleiben unverändert.
PREVIEW_FORMATS = {"jpg": "JPEG", "jpeg": "JPEG", "png": "PNG", "webp": "WEBP", "bmp": "BMP"}
shrinkCounter = 0

# Bis Version 2.01 wurden verkleinerte Kopien in diesem Ordner abgelegt. Er wird entfernt.
OLD_THUMB_DIR = os.path.join(homeDir, "sflix_sys", "thumbs")

# Zeichenfolgen, die STEFFLIX intern als Trennzeichen nutzt oder die in URLs Probleme machen
forbiddenSymbols = {"$=$": "-", "|": "-", "#": "", "%": ""}

# Häufige Zeichen ausserhalb von Latin-1 mit sinnvollem Ersatz
replacements = {
    "–": "-", "—": "-", "‐": "-", "‑": "-", "‒": "-", "−": "-",
    "‘": "'", "’": "'", "‚": ",", "‛": "'",
    "“": "'", "”": "'", "„": "'", "«": "'", "»": "'",
    "…": "...", "€": "EUR", "•": "-", "·": "-",
    " ": " ", "™": "", "®": "", "©": "",
    "Ł": "L", "ł": "l", "Đ": "D", "đ": "d", "Œ": "OE", "œ": "oe",
}

renamedItems = []
renamedPairs = []
errorList = []
RENAMED_FILE = os.path.join(homeDir, "sflix_sys", "renamed.js")
RENAMED_PREFIX = "var renamedPaths = "


def sanitizeName(name):
    """Ersetzt oder entfernt alle Zeichen, die btoa() im Browser nicht verarbeiten kann
    (alles ausserhalb von Latin-1) sowie die Trennzeichen von STEFFLIX."""
    for old, new in replacements.items():
        name = name.replace(old, new)
    for old, new in forbiddenSymbols.items():
        name = name.replace(old, new)

    result = ""
    for char in name:
        code = ord(char)
        if code < 0x20 or 0x7F <= code <= 0x9F:
            continue
        if code <= 0xFF:
            result += char
            continue
        # z.B. "ő" -> "o", "ł" wird entfernt
        for part in unicodedata.normalize("NFKD", char):
            if 0x20 <= ord(part) <= 0x7E or 0xA0 <= ord(part) <= 0xFF:
                result += part

    # Mehrfache Leerzeichen zusammenfassen, Windows mag keine Leerzeichen/Punkte am Ende
    result = " ".join(result.split()).rstrip(" .")
    return result or "unbenannt"


def sanitizeFileName(fileName):
    stem, extension = os.path.splitext(fileName)
    return sanitizeName(stem) + extension.lower()


def renameIfNeeded(path, item, isFile):
    """Benennt Dateien/Ordner mit ungültigen Zeichen um. Gibt den neuen Namen zurück
    oder None, wenn das Umbenennen nicht möglich war."""
    newName = sanitizeFileName(item) if isFile else sanitizeName(item)
    if newName == item:
        return item

    stem, extension = os.path.splitext(newName) if isFile else (newName, "")
    candidate = newName
    counter = 2
    while os.path.exists(os.path.join(path, candidate)) and candidate.lower() != item.lower():
        candidate = f"{stem} ({counter}){extension}"
        counter += 1

    try:
        os.rename(os.path.join(path, item), os.path.join(path, candidate))
        renamedItems.append((os.path.join(path, item), candidate))
        renamedPairs.append([relativeUrl(os.path.join(path, item)), relativeUrl(os.path.join(path, candidate))])
        return candidate
    except OSError:
        errorList.append(os.path.join(path, item))
        return None


def relativeUrl(fullPath):
    return "./" + os.path.relpath(fullPath, homeDir).replace("\\", "/")


def isToolStorage(name):
    """Ordner mit "__" im Namen sind Ablagen der Werkzeuge (z.B. __teile, __konvertiert,
    __gelöscht, __original) und werden nicht eingelesen (wie im Updater)."""
    return "__" in name


def uniquePath(folder, name):
    """Freier Dateiname im Ordner: "Name.jpg", sonst "Name (2).jpg", "Name (3).jpg" ..."""
    stem, extension = os.path.splitext(name)
    candidate = os.path.join(folder, name)
    counter = 2
    while os.path.exists(candidate):
        candidate = os.path.join(folder, f"{stem} ({counter}){extension}")
        counter += 1
    return candidate


def migrateOldOriginal(previewDir, item):
    """Verschiebt "<Dateiname>.original" (Ablage bis Updater 3.0.3) nach Preview\\__original.
    Gibt es dort schon eine Datei gleichen Namens, bleiben beide erhalten."""
    global migratedOriginals
    name = item[:-len(ORIGINAL_SUFFIX)]
    if name == "":
        return
    try:
        folder = os.path.join(previewDir, ORIGINAL_FOLDER)
        os.makedirs(folder, exist_ok=True)
        os.rename(os.path.join(previewDir, item), uniquePath(folder, name))
        migratedOriginals += 1
    except OSError:
        errorList.append(os.path.join(previewDir, item))


def shrinkPreview(fullPath):
    """Schneidet ein zu grosses Vorschaubild auf PREVIEW_SIZE zu. Das Original kommt mit
    gleichem Namen nach Preview\\__original, die zugeschnittene Version erhält den bisherigen
    Namen. Pfad und Name in data.js bleiben dadurch gleich. Ohne Pillow oder bei Fehlern
    bleibt das Bild unverändert."""
    global shrinkCounter
    saveFormat = PREVIEW_FORMATS.get(fullPath.split(".")[-1].lower())
    if Image is None or saveFormat is None:
        return

    tempPath = fullPath + ".tmp"
    originalPath = os.path.join(os.path.dirname(fullPath), ORIGINAL_FOLDER, os.path.basename(fullPath))
    try:
        with Image.open(fullPath) as image:
            # Animierte Bilder nicht anfassen
            if getattr(image, "is_animated", False):
                return
            image = ImageOps.exif_transpose(image)
            if image.width <= PREVIEW_SIZE[0] and image.height <= PREVIEW_SIZE[1]:
                return
            if saveFormat == "JPEG" or image.mode not in ("RGB", "RGBA"):
                image = image.convert("RGB")
            image = ImageOps.fit(image, PREVIEW_SIZE, Image.LANCZOS)
            options = {"quality": PREVIEW_QUALITY, "optimize": True} if saveFormat in ("JPEG", "WEBP") else {}
            image.save(tempPath, saveFormat, **options)
        # Erst wenn die neue Version fertig ist: Original nach Preview\__original sichern, neue
        # Version an seine Stelle. Ein dort schon vorhandenes Original stammt von einem früher
        # ersetzten Bild und wird durch das aktuelle, neuere Original ersetzt.
        os.makedirs(os.path.dirname(originalPath), exist_ok=True)
        os.replace(fullPath, originalPath)
        os.replace(tempPath, fullPath)
        shrinkCounter += 1
    except Exception:
        if os.path.exists(tempPath):
            try:
                os.remove(tempPath)
            except OSError:
                pass
        # Falls das Original schon verschoben wurde, zurückholen
        if not os.path.exists(fullPath) and os.path.exists(originalPath):
            try:
                os.replace(originalPath, fullPath)
            except OSError:
                pass


def removeOldThumbnails():
    """Entfernt den Ordner sflix_sys/thumbs mit den verkleinerten Vorschaubildern aus Version 2.00/2.01."""
    if os.path.isdir(OLD_THUMB_DIR):
        shutil.rmtree(OLD_THUMB_DIR, ignore_errors=True)
        print("Alte verkleinerte Vorschaubilder entfernt (sflix_sys/thumbs)")


def writeRenamedPaths():
    """Speichert die Umbenennungen in sflix_sys/renamed.js. STEFFLIX passt damit beim
    nächsten Start Favoriten, Zeitstempel und "Weiterschauen" an. Frühere Einträge
    bleiben erhalten, falls updateData mehrmals ohne Öffnen von STEFFLIX läuft."""
    previous = {"id": "", "pairs": []}
    if os.path.exists(RENAMED_FILE):
        try:
            with open(RENAMED_FILE, encoding="utf-8") as fileReader:
                content = fileReader.read().strip()
            previous = json.loads(content[len(RENAMED_PREFIX):].rstrip(";"))
        except (OSError, ValueError):
            pass

    pairs = (previous.get("pairs") or []) + renamedPairs
    renameId = previous.get("id") or ""
    if renamedPairs or not renameId:
        renameId = datetime.now().strftime("%Y%m%d%H%M%S")

    with open(RENAMED_FILE, "w", encoding="utf-8") as fileWriter:
        fileWriter.write(RENAMED_PREFIX + json.dumps({"id": renameId, "pairs": pairs[-5000:]}, ensure_ascii=False) + ";")


def loadingAnimation(counter):
    index = (counter // 10) % len(animationArray)
    print("\rDurchsuche Ordner...".ljust(40) + animationArray[index], end="")


def recCrawler(path, counter=0):
    data = {}
    isPreviewFolder = os.path.basename(path) == PREVIEW_FOLDER

    for item in os.listdir(path):
        fullPath = os.path.join(path, item)
        isFile = os.path.isfile(fullPath)
        fileExtension = item.split(".")[-1].lower()

        if isFile and isPreviewFolder and item.lower().endswith(ORIGINAL_SUFFIX):
            migrateOldOriginal(path, item)
        elif isFile and fileExtension in allowedFileTypes:
            item = renameIfNeeded(path, item, True)
            if item is None:
                continue
            fullPath = os.path.join(path, item)
            if isPreviewFolder:
                shrinkPreview(fullPath)
            data[relativeUrl(fullPath)] = None
            counter += 1
        elif os.path.isdir(fullPath) and item not in disallowedItems and not isToolStorage(item):
            item = renameIfNeeded(path, item, False)
            if item is None:
                continue
            data[item], counter = recCrawler(os.path.join(path, item), counter)

        if counter % 10 == 0:
            loadingAnimation(counter)
    return data, counter


if Image is None:
    print("[HINWEIS] Pillow nicht installiert - Vorschaubilder werden nicht verkleinert.")

print("Durchsuche Ordner...".ljust(40), end="")
try:
    filelist, counter = recCrawler(homeDir)
    print("\rDurchsuche Ordner...".ljust(40) + "OK     ")
except Exception:
    filelist = {}
    counter = 0
    print("\rDurchsuche Ordner...".ljust(40) + "Fehler")

dataFile = os.path.join(homeDir, "sflix_sys", "data.js")

if len(filelist) > 0:
    try:
        print("Erstelle Backup...".ljust(40), end="")
        if os.path.exists(dataFile):
            if os.path.exists(dataFile + ".bak"):
                os.remove(dataFile + ".bak")
            os.replace(dataFile, dataFile + ".bak")
        print("OK")
    except OSError:
        print("Fehler")

    try:
        print("Speichere neue Version...".ljust(40), end="")
        with open(dataFile, "w", encoding="utf-8") as fileWriter:
            fileWriter.write("var data = " + json.dumps(filelist, ensure_ascii=False) + ";")
        print("OK")
    except OSError:
        print("Fehler")

    try:
        writeRenamedPaths()
    except OSError:
        print("Umbenennungen konnten nicht gespeichert werden".ljust(40))

    # Erst nach dem Schreiben von data.js, damit keine Einträge mehr auf den Ordner zeigen
    removeOldThumbnails()
else:
    print("Im STEFFLIX-Verzeichnis wurden keine Medien gefunden.")

if shrinkCounter > 0:
    print(f"{shrinkCounter} Vorschaubilder verkleinert (Original in Preview\\{ORIGINAL_FOLDER} gesichert)")
if migratedOriginals > 0:
    print(f"{migratedOriginals} ältere .original-Dateien nach Preview\\{ORIGINAL_FOLDER} verschoben")

if len(renamedItems) > 0:
    print("".center(50, "-"))
    print("Folgende Dateien/Ordner wurden umbenannt,")
    print("weil sie ungültige Zeichen enthielten:")
    for index, (oldPath, newName) in enumerate(renamedItems, start=1):
        print(f"{index}. {os.path.relpath(oldPath, homeDir)}  ->  {newName}")
    print("".center(50, "-"))

if len(errorList) > 0:
    print("".center(50, "!"))
    print("Folgende Dateien enthalten ungültige Zeichen und")
    print("konnten nicht umbenannt werden (werden ignoriert):")
    for index, errorFile in enumerate(errorList, start=1):
        print(f"{index}. {os.path.relpath(errorFile, homeDir)}")
    print("".center(50, "!"))
    print()

print("".center(50, "="))
print("Update abgeschlossen".center(50))
print(f"{counter} Medien gefunden".center(50))
print("".center(50, "="))

updaterPath = os.path.join(homeDir, "updater.exe")
if not os.path.exists(updaterPath):
    print("Lade Autoupdater herunter...".ljust(40), end="")
    try:
        install_file(updaterPath, download_file("updater.exe", load_checksums()))
        print("OK")
        print("Updater wird ausgeführt...".ljust(40))
        # Direkt starten, nicht über die Kommandozeile (diese kann keine Netzwerkpfade)
        subprocess.call([updaterPath], cwd=homeDir)
    except (DownloadError, OSError) as error:
        print(f"FEHLER: {error}")
        print()
        input("Enter zum Beenden")
else:
    print()
    input("Enter zum Beenden")
