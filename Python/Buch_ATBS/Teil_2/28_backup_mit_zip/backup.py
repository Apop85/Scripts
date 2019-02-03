# 28_backup_mit_zip.py
# Ordnerinhalt in Zipfile speichern mit forlaufender nummerierung

import os, shutil, zipfile

os.chdir(os.path.dirname(__file__))
backuppath='.\\backup_me'
targetpath='.\\backups'
backupcounter=len(os.listdir(targetpath))
zipname=zipfile.ZipFile(targetpath+'\\backup'+str(backupcounter+1)+'.zip', 'w')

for folder, subfolder, files in os.walk(backuppath):
    for file in files:
        zipname.write(backuppath+'\\'+file, compress_type=zipfile.ZIP_DEFLATED)

zipname.close()