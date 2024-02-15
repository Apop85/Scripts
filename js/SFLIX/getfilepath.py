# from operator import truediv
import os, requests
from shutil import move as moveFile

os.chdir(os.getcwd())

print("".center(50, "="))
print("Update STEFFLIX-Daten".center(50))
print("".center(50, "="))

homeDir = os.getcwd()
# print(homeDir)

allowedFileTypes = ["jpg", "jpeg", "mp4", "mp3", "png", "ogg", "gif", "m4a"]
diallowedItems = ["System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys"]
forbiddenSymbols = ["$=$"]
animationArray = ["Suchen", "sUchen", "suChen", "sucHen", "suchEn", "sucheN", "suchEn", "sucHen", "suChen", "sUchen"]


def containsForbiddenSymbol(text):
    for symbol in forbiddenSymbols:
        if symbol in text:
            return True
    return False

def loadingAnimation(animationArray, counter):
    index = (counter // 10) % (len(animationArray))
    print("\rDurchsuche Ordner...".ljust(40) + animationArray[index], end="")

def recCrawler(path, depth=0, counter=0):
    data={}
    folderContent = os.listdir(path)

    for item in folderContent:
        fileExtension = item.split(".")[-1]
        if os.path.isfile(os.path.join(path, item)) and fileExtension in allowedFileTypes:
            data.setdefault(os.path.join(path.replace(homeDir, ".\\"), item).replace("\\\\", "\\"), None)
            counter += 1
        elif os.path.isdir(os.path.join(path, item)) and not item in diallowedItems:
            data.setdefault(item, {})
            data[item], counter = recCrawler(os.path.join(path, item), depth+1, counter)
        if counter % 10 == 0:
            loadingAnimation(animationArray, counter)
    return data, counter

print("Durchsuche Ordner...".ljust(40), end="")
try:  
    filelist, counter = recCrawler(homeDir)
    print("\rDurchsuche Ordner...".ljust(40) + "OK     ")
    errorList = []

except:
    errorList = []
    filelist = []
    counter = 0
    print("\rDurchsuche Ordner...".ljust(40) + "Fehler")


if len(filelist) > 0:
    try:
        print("Erstelle Backup...".ljust(40), end="")
        if os.path.exists(os.path.join(homeDir, "sflix_sys", "data.js.bak")):
            os.remove(os.path.join(homeDir, "sflix_sys", "data.js.bak"))

        moveFile(os.path.join(homeDir, "sflix_sys", "data.js"), os.path.join(homeDir, "sflix_sys", "data.js.bak"))
        print("OK")
    except:
        print("Fehler")

    try:
        print("Speichere neue Version...".ljust(40), end="")
        fileWriter = open(os.path.join(homeDir, "sflix_sys", "data.js"), "w", encoding="utf-8")
        fileWriter.write("var data = " + str(filelist).replace("\\\\", "/").replace("None", "null") + ";")
        fileWriter.close()
        print("OK")
    except:
        print("Fehler")
else:
    print("Durchsuchen des Verzeichnis hat keine Resultate ergeben!")

if len(errorList) > 0:
    print("".center(50, "!"))
    print("Folgende Dateien enthalten ungültige Zeichen:")
    counter = 0
    for errorFile in errorList:
        counter += 1
        print(f"{counter}. {errorFile}")
    print("".center(50, "!"))
    print("Ungültige Zeichenfolgen: " + ", ".join(forbiddenSymbols))
    print("".center(50, "!"))
    print()
print("".center(50, "="))
print("Update abgeschlossen".center(50))
print(f"{counter} Medien gefunden".center(50))
print("".center(50, "="))

updaterPath = os.path.join(os.getcwd(), "updater.exe")
if not os.path.exists(updaterPath):
    url = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updater.exe"
    print("Lade Autoupdater herunter...".ljust(40), end="")
    try:
        answer = requests.get(url)
        fileWriter = open(updaterPath, "wb")
        fileWriter.write(answer.content)
        fileWriter.close()
        print("OK")

        print("Updater wird ausgeführt...".ljust(40))
        os.system(os.path.join(os.getcwd(), "updater.exe"))
    except:
        print("FEHLER")
        print()
        input("Enter zum Beenden")
else:
    print()
    input("Enter zum Beenden")