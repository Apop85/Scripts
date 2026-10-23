import os
import subprocess
import sys

from sflix_update import WEB_FILES, remove_obsolete_files, update_files

os.chdir(os.getcwd())

# Der Updater aktualisiert updateStefflix.exe (sich selbst kann er nicht ersetzen)
failed = update_files(["updateStefflix.exe"], label="Lade")

# Umstieg von einer älteren Version: deren updateStefflix.exe lädt nur seine eigene,
# feste Dateiliste und startet danach diesen Updater. Fehlende Dateien der neuen
# Version (z.B. sflix_sys/js/*.js) werden deshalb hier nachgeladen.
missingFiles = [name for name in WEB_FILES if not os.path.exists(name)]
if missingFiles:
    failed += update_files(missingFiles, label="Lade")
    if not failed:
        remove_obsolete_files()

print("".center(60, "="))
print("UPDATE BEENDET" if not failed else "UPDATE FEHLGESCHLAGEN - BITTE updateStefflix.exe ERNEUT AUSFÜHREN")
print("".center(60, "="))

# Wird updater.exe von updateStefflix.exe (ab 2.00) gestartet, geht es ohne Pause weiter;
# updateStefflix.exe startet danach selbst updateData.exe
if "--no-pause" in sys.argv:
    sys.exit(0)

# Umstieg von einer älteren Version: Medien gleich neu einlesen, damit nur ein
# Programmaufruf nötig ist. updateData.exe fragt am Ende selbst nach Enter.
updateDataPath = os.path.join(os.getcwd(), "updateData.exe")
if missingFiles and not failed and os.path.exists(updateDataPath):
    print()
    subprocess.call([updateDataPath], cwd=os.getcwd())
else:
    print()
    input("Enter zum Beenden")
