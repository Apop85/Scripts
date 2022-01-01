from genericpath import exists
import requests, os
from shutil import move as moveFile

print("".center(60, "="))
print("STARTE STEFFLIXUPDATE")
print("".center(60, "="))


os.chdir(os.getcwd())

stefflixFiles = {
    "start.html": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/start.html",
    "updateData.exe": "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updateData.exe",
    "sflix_sys/sflix.css": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix.css",
    "sflix_sys/sflix.js": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix.js",
    "sflix_sys/version.js": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/version.js"
}

def writeToFile(data, fileLocation, writeMode="w"):
    try:
        if writeMode == "w":
            fileWriter = open(os.path.join(".", fileLocation), "w", encoding="utf-8")
        elif writeMode == "b":
            fileWriter = open(os.path.join(".", fileLocation), "wb")

        fileWriter.write(data)
        fileWriter.close()
        return True
    except:
        return False


def restoreFile(filePath):
    if os.path.exists(filePath + ".old"):
        if os.path.exists(filePath):
            os.remove(filePath)
        moveFile(filePath + ".old", filePath)


for fileLocation in stefflixFiles.keys():
    print(f"Lade {fileLocation} herunter...".ljust(50), end="")
    try:
        answer = requests.get(stefflixFiles[fileLocation])
        print("OK")
    except:
        print("FEHLER")

    if not fileLocation.endswith(".exe"):
        fileContent = answer.content.decode("UTF8")
        filePath = os.path.join(".", fileLocation)
        print("Erstelle Backup...".ljust(50), end="")
        try:
            if os.path.exists(filePath):
                if os.path.exists(filePath + ".old"):
                    os.remove(filePath + ".old")

            moveFile(filePath, filePath + ".old")

            print("OK")
        except:
            print("FEHLER")
        
        print("Update STEFFLIX-Daten...".ljust(50), end="")
        try:
            success = writeToFile(fileContent, filePath)
            print("OK")
        except:
            print("FEHLER")

    else:
        try:
            print("Erstelle Backup...".ljust(50), end="")
            filePath = os.path.join(".", fileLocation)
            if os.path.exists(filePath):
                if os.path.exists(filePath + ".old"):
                    os.remove(filePath + ".old")
            moveFile(filePath, filePath + ".old")
            
            print("OK")
        except:
            print("FEHLER")

        print("Update STEFFLIX-Daten...".ljust(50), end="")
        try:
            success = writeToFile(answer, filePath, "wb")
            print("OK")
        except:
            print("FEHLER")

print("".center(60, "="))
print("UPDATE BEENDET")
print("".center(60, "="))