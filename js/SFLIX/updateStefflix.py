import os
import subprocess

from sflix_update import SCRIPT_FILES, remove_obsolete_files, update_files

os.chdir(os.getcwd())

print("".center(60, "="))
print("STARTE STEFFLIXUPDATE")
print("".center(60, "="))

stefflixFiles = [
    "sflix_sys/sflix.css",
    *SCRIPT_FILES,
    "sflix_sys/splash.svg",
    "sflix_sys/sflixLogo.png",
    "sflix_sys/sflixLogo.svg",
    "sflix_sys/version.js",
    "start.html",
    "updateData.exe",
    "updater.exe",
]


def runProgram(name, *arguments):
    """Startet ein STEFFLIX-Programm im selben Fenster und wartet auf das Ende."""
    programPath = os.path.join(os.getcwd(), name)
    if not os.path.exists(programPath):
        print(f"{name} nicht gefunden.")
        return False
    try:
        subprocess.call([programPath, *arguments], cwd=os.getcwd())
        return True
    except OSError as error:
        print(f"{name} konnte nicht gestartet werden: {error.strerror}")
        return False


failed = update_files(stefflixFiles, label="Lade")

if failed:
    print()
    print("Das Update wurde nicht installiert. Bitte später erneut versuchen.")
    print()
    input("Enter zum Beenden")
else:
    remove_obsolete_files()

    # 1. updater.exe aktualisiert updateStefflix.exe (eine laufende Datei kann sich nicht
    #    selbst ersetzen). --no-pause: keine Enter-Abfrage mitten in der Kette.
    print()
    runProgram("updater.exe", "--no-pause")

    # 2. updateData.exe liest die Medien neu ein (Umbenennungen, Vorschaubilder)
    print()
    if not runProgram("updateData.exe"):
        print()
        input("Enter zum Beenden")
