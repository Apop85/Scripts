import os

from sflix_update import INSTALL_FILES, update_files

# Alle Dateien ausser denen, die nur für checksums.json da sind (z.B. ffmpeg.json)
stefflixFiles = list(INSTALL_FILES)

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

print("==============================================================")
print("        Willkommen beim Setup von STEFFLIX")
print("==============================================================")
print("STEFFLIX ist eine Browserapplikation angelehnt an Netflix, um")
print("lokal oder im Netzwerk gespeicherte Mediendateien abzuspielen.")
print("")
print("Ausser einem installierten Browser hat STEFFLIX keine weiteren")
print("Voraussetzungen.")
print("==============================================================")

check = False
while not check:
    print("In welchem Ordner soll STEFFLIX installiert werden? (z.B. C:\\Medien)")
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

failed = update_files(stefflixFiles, base_dir=installPath, label="[SETUP] Lade", skip_existing=True)

# Leere Liste der Umbenennungen (wird von updateData.exe gefüllt)
renamedFile = os.path.join(installPath, "sflix_sys", "renamed.js")
if not os.path.exists(renamedFile):
    try:
        with open(renamedFile, "w", encoding="utf-8") as fileWriter:
            fileWriter.write('var renamedPaths = {"id": "", "pairs": []};')
    except OSError:
        pass
if failed:
    print(f"\n[WARNUNG] {failed} Datei(en) konnten nicht installiert werden. Bitte Setup erneut ausführen.")


print("==============================================================")
print("                Installation abgeschlossen")
print("==============================================================")
print("Eine Anleitung zum Hinzufügen von Medien findest du unter folgender Adresse im Browser:")
url = installPath.replace("\\", "/")
print(f"file:///{url}/start.html?=SEVMUA==")
print()