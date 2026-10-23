import json
import os
import unicodedata
from datetime import datetime

from sflix_update import DownloadError, download_file, install_file, load_checksums

try:
    from PIL import Image, ImageOps
except ImportError:
    Image = None

os.chdir(os.getcwd())

print("".center(50, "="))
print("Update STEFFLIX-Daten".center(50))
print("".center(50, "="))

homeDir = os.getcwd()

# Muss mit den Listen in sflix_sys/js/core.js übereinstimmen (mediaTypes, musicTypes, imageTypes)
allowedFileTypes = [
    "mp4", "m4v", "webm", "ogv",                                  # Video
    "mp3", "m4a", "ogg", "oga", "opus", "aac", "flac", "wav",     # Musik
    "jpg", "jpeg", "png", "gif", "webp", "avif", "bmp",           # Bilder
]
disallowedItems = ["System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys"]
animationArray = ["Suchen", "sUchen", "suChen", "sucHen", "suchEn", "sucheN", "suchEn", "sucHen", "suChen", "sUchen"]

# Vorschaubilder werden auf diese Grösse zugeschnitten (16:9) und hier abgelegt
PREVIEW_FOLDER = "Preview"
THUMB_DIR = os.path.join(homeDir, "sflix_sys", "thumbs")
THUMB_SIZE = (480, 270)
THUMB_QUALITY = 80

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
thumbsInUse = set()
thumbCounter = 0


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


def createThumbnail(sourcePath):
    """Erstellt ein verkleinertes Vorschaubild in sflix_sys/thumbs und gibt dessen Pfad zurück.
    Ohne Pillow oder bei Fehlern wird das Originalbild verwendet."""
    global thumbCounter
    if Image is None:
        return sourcePath

    relative = os.path.relpath(sourcePath, homeDir)
    thumbPath = os.path.join(THUMB_DIR, os.path.splitext(relative)[0] + ".jpg")
    thumbsInUse.add(os.path.normcase(thumbPath))

    try:
        if os.path.exists(thumbPath) and os.path.getmtime(thumbPath) >= os.path.getmtime(sourcePath):
            return thumbPath

        os.makedirs(os.path.dirname(thumbPath), exist_ok=True)
        with Image.open(sourcePath) as image:
            image = ImageOps.exif_transpose(image).convert("RGB")
            image = ImageOps.fit(image, THUMB_SIZE, Image.LANCZOS)
            image.save(thumbPath, "JPEG", quality=THUMB_QUALITY, optimize=True)
        thumbCounter += 1
        return thumbPath
    except Exception:
        thumbsInUse.discard(os.path.normcase(thumbPath))
        return sourcePath


def removeUnusedThumbnails():
    """Löscht Vorschaubilder, deren Original nicht mehr existiert."""
    if Image is None or not os.path.isdir(THUMB_DIR):
        return
    for root, dirs, files in os.walk(THUMB_DIR, topdown=False):
        for fileName in files:
            fullPath = os.path.join(root, fileName)
            if os.path.normcase(fullPath) not in thumbsInUse:
                os.remove(fullPath)
        if root != THUMB_DIR and not os.listdir(root):
            os.rmdir(root)


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

        if isFile and fileExtension in allowedFileTypes:
            item = renameIfNeeded(path, item, True)
            if item is None:
                continue
            fullPath = os.path.join(path, item)
            if isPreviewFolder:
                fullPath = createThumbnail(fullPath)
            data[relativeUrl(fullPath)] = None
            counter += 1
        elif os.path.isdir(fullPath) and item not in disallowedItems:
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

    try:
        removeUnusedThumbnails()
    except OSError:
        pass
else:
    print("Im STEFFLIX-Verzeichnis wurden keine Medien gefunden.")

if thumbCounter > 0:
    print(f"{thumbCounter} Vorschaubilder verkleinert")

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

updaterPath = os.path.join(os.getcwd(), "updater.exe")
if not os.path.exists(updaterPath):
    print("Lade Autoupdater herunter...".ljust(40), end="")
    try:
        install_file(updaterPath, download_file("updater.exe", load_checksums()))
        print("OK")
        print("Updater wird ausgeführt...".ljust(40))
        os.system(f'"{updaterPath}"')
    except (DownloadError, OSError) as error:
        print(f"FEHLER: {error}")
        print()
        input("Enter zum Beenden")
else:
    print()
    input("Enter zum Beenden")
