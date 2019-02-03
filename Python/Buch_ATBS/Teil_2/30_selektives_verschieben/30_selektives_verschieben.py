# 30_selektives_verschieben.py
# Projekt aus dem Kapitel 9 (s. 243) für das selektive verschieben bzw. kopieren von Dateien
# In diesem Script werden alle Dateien anhand ihrer Endungen in einen entsprechend der Endung 
# benannten Unterordner kopiert.
# Um zu Verschieben .\\sorted mit in .\\sort_me umschreiben. 
# Auch muss man die If-Funktion bei Zeile 12-16 auskommentieren und aus shutil.copy() shutil.move() machen.

import os, re, shutil
os.chdir(os.path.dirname(__file__))
targetpath='.\\sort_me'

if os.path.exists('.\\sorted'):
    shutil.rmtree('.\\sorted')
    os.mkdir('.\\sorted')
else:
    os.mkdir('.\\sorted')

def crawl_folders():
    for folder, subfolder, files in os.walk(targetpath):
        searchpattern=re.compile(r'\.(.{3})')
        for file_name in files:
            result=searchpattern.findall(file_name)
            if not os.path.exists('.\\sorted\\'+result[0]):
                os.mkdir('.\\sorted\\'+result[0].upper())
            shutil.copy(folder+'\\'+file_name, '.\\sorted\\'+result[0].upper()+'\\'+file_name)

crawl_folders()