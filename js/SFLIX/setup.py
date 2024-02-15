import os, requests

stefflixFiles = {
    "sflix_sys/data.js": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/data.js",
    "sflix_sys/sflix_bg.jpg": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix_bg.jpg",
    "sflix_sys/sflix.css": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix.css",
    "sflix_sys/sflix.js": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix.js",
    "sflix_sys/sflix.png": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflix.png",
    "sflix_sys/sflixLogo.png": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/sflixLogo.png",
    "sflix_sys/version.js": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/version.js",
    "start.html": "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/start.html",
    "updateData.exe": "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updateData.exe",
    "updater.exe": "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updater.exe",
    "updateStefflix.exe": "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/updateStefflix.exe",
}

def checkInput(type, text, output="default"):
    answer = None
    if output == "default":
        print(f"Ist die Eingabe {text} für {type} korrekt? [J/n]")
    else:
        print(text)
    while not answer in ["", "j", "J", "n", "N", "y", "Y"]:
        answer = input("Eingabe: ")
    if answer in ["", "j", "J", "y", "Y"]:
        return True
    else:
        return False

def createPath(installPath):
    try:
        os.makedirs(installPath, exist_ok=True)
        print("[INFO] Pfad erstellt")
        return True
    except:
        print(f"\n[WARNUNG] Der Pfad {installPath} konnte nicht erstellt werden\n")
        return False

def writeToFile(data, installPath, writeMode="w"):
    try:
        if not os.path.exists(installPath):
            if writeMode == "w":
                fileWriter = open(installPath, "w", encoding="utf-8")
            elif writeMode == "wb":
                fileWriter = open(installPath, "wb")

            fileWriter.write(data)
            fileWriter.close()
        return True
    except:
        return False

print("==============================================================")
print("     Willkommen beim Setup zum einrichten von Stefflix")
print("==============================================================")
print("Stefflix ist eine Browserapplikation angelehnt an Netflix um")
print("lokale oder im Netzerk gespeicherte Mediendateien wiederzugeben")
print("")
print("Ausser einem installierten Browser hat Stefflix keine weiteren")
print("Voraussetzungen.")
print("==============================================================")

check = False
while not check:
    print("In welchem Pfad soll Stefflix installiert werden? (z.B: C:\Medien)")
    installPath = input("Pfad:")
    installPath = installPath.replace('"', '')
    if not os.path.exists(installPath):
        print(f"\n[WARNUNG] Der Pfad {installPath} existiert nicht\n")
        if checkInput("", "Soll der Pfad erstellt werden?", output=False):
            check = createPath(installPath)
    else:
        check = checkInput("den Speicherort", installPath)


print("==============================================================")
print("                Installation wird gestartet")
print("==============================================================")

print("[SETUP] Erstelle Ordnerstruktur...".ljust(50), end="")
try:
    os.mkdir(os.path.join(installPath, "sflix_sys"))
    print("OK")
except:
    print("FEHLER")

for fileLocation in stefflixFiles.keys():
    print(f"[SETUP] Lade {fileLocation} herunter...".ljust(50), end="")
    try:
        answer = requests.get(stefflixFiles[fileLocation])
        filePath = os.path.join(installPath, fileLocation)

        if not fileLocation.endswith(".exe") and not fileLocation.endswith(".png") and not fileLocation.endswith(".jpg"):
            fileContent = answer.content.decode("UTF8")
                    
            try:
                success = writeToFile(fileContent, filePath)
                if success:
                    print("OK")
                else:
                    print("FEHLER")
            except:
                print("FEHLER")

        else:
            try:
                success = writeToFile(answer.content, filePath, "wb")
                if success:
                    print("OK")
                else:
                    print("FEHLER")
            except:
                print("FEHLER")
    except:
        print("FEHLER")

    

print("==============================================================")
print("                Installation abgeschlossen")
print("==============================================================")
print("Für Hilfe zum Hinzufügen von Mediendateien prüfe folgende URL in deinem Browser:")
url = installPath.replace("\\", "/")
print(f"file:///{url}/start.html?=SEVMUA==")
print()