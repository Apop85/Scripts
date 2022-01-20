from operator import truediv
import os
from shutil import move as moveFile

os.chdir(os.getcwd())

print("".center(50, "="))
print("Update STEFFLIX-Daten".center(50))
print("".center(50, "="))

homeDir = os.getcwd()

allowedFileTypes = ["jpg", "jpeg", "mp4", "mp3", "png", "ogg", "gif", "m4a"]
diallowedItems = ["System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys"]
forbiddenSymbols = ["$=$"]

def containsForbiddenSymbol(text):
    for symbol in forbiddenSymbols:
        if symbol in text:
            return True
    return False

def recursiveCrawler(path, project="", serie="", staffel="", folge="", filelist={}, depth=0, errorList=[]):
    if depth == 0:
        pass
    elif depth == 1:
        project = path.split("\\")[-1]
        filelist.setdefault(project, {})
    elif depth == 2:
        serie = path.split("\\")[-1]
        filelist[project].setdefault(serie, {})
    elif depth == 3:
        staffel = path.split("\\")[-1]
        filelist[project][serie].setdefault(staffel, {})
    elif depth == 4:
        folge = path.split("\\")[-1]
        filelist[project][serie][staffel].setdefault(folge, {})
        

    # print(f"{project} {serie} {staffel}")

    folderContent = os.listdir(path)
    for item in folderContent:
        if not item in diallowedItems:
            if os.path.isfile(os.path.join(path, item)):
                extension = item.split(".")[-1]
                if extension in allowedFileTypes:
                    if depth == 1:
                        relPath = "." + os.path.join(path, item)[len(homeDir):]
                        if not containsForbiddenSymbol(relPath):
                            filelist[project].setdefault(relPath)
                        else:
                            errorList += [relPath]
                    elif depth == 2:
                        relPath = "." + os.path.join(path, item)[len(homeDir):]
                        if not containsForbiddenSymbol(relPath):
                            filelist[project][serie].setdefault(relPath)
                        else:
                            errorList += [relPath]
                    elif depth == 3:
                        relPath = "." + os.path.join(path, item)[len(homeDir):]
                        if not containsForbiddenSymbol(relPath):
                            filelist[project][serie][staffel].setdefault(relPath, None)
                        else:
                            errorList += [relPath]
                    elif depth > 3:
                        relPath = "." + os.path.join(path, item)[len(homeDir):]
                        if not containsForbiddenSymbol(relPath):
                            filelist[project][serie][staffel][folge].setdefault(relPath, None)
                        else:
                            errorList += [relPath]

            elif os.path.isdir(os.path.join(path, item)):
                filelist, errorList = recursiveCrawler(os.path.join(path, item), project, serie, staffel, folge, filelist, depth+1, errorList)
    return filelist, errorList

print("Durchsuche Ordner...".ljust(40), end="")
try:  
    filelist, errorList = recursiveCrawler(homeDir)
    print("OK")
except:
    errorList = []
    filelist = []
    print("Fehler")


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
print("".center(50, "="))
print()
input("Enter zum Beenden")