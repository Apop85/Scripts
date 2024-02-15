# from genericpath import exists
import requests, os
from shutil import move as moveFile
from time import sleep

os.chdir(os.getcwd())

# Herunterzuladende Files (NUR BINARY FILES!)
stefflixFiles = {
    "updateStefflix.exe": "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updateStefflix.exe",
}

# Schreibe Filecontent in Datei
def writeToFile(data, fileLocation, writeMode="w"):
    try:
        fileWriter = open(os.path.join(".", fileLocation), "wb")

        fileWriter.write(data)
        fileWriter.close()
        return True
    except:
        return False

def restoreFile(filePath):
    print(f"Widerherstellung {filePath}...".ljust(50), end="")
    try:
        if os.path.exists(filePath + ".old"):
            if os.path.exists(filePath):
                os.remove(filePath)
            moveFile(filePath + ".old", filePath)
        print("OK")
    except:
        print("FEHLER")

for fileLocation in stefflixFiles.keys():
    print(f"Lade {fileLocation} herunter...".ljust(50), end="")
    try:
        answer = requests.get(stefflixFiles[fileLocation])
        print("OK")
    except:
        print("FEHLER")
    
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
        success = writeToFile(answer.content, filePath, "wb")
        if success:
            print("OK")
        else:
            print("FEHLER")
            restoreFile(filePath)
    except:
        print("FEHLER")


print("".center(60, "="))
print("UPDATE BEENDET")
print("".center(60, "="))
print()
input("Enter zum Beenden")