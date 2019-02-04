# 25_ordner_mit_allen_unterordnern_durchsuchen.py
# Dieses Script übernimmt die Aufgabe alle Unterordner von diesem Buch nach txt-Dateien zu durchsuchen um sie in ein Vorgegebenes Verzeichnis zu kopieren.

import os, shutil
os.chdir(os.path.dirname(__file__))
if not os.path.exists('.\\resultate'):
    os.mkdir('.\\resultate')

target=os.path.dirname(__file__)+'\\resultate'

for ordner, unterordner, dateien in os.walk('..\\'):
    if ordner.endswith('resultate'):
        continue
    for datei in dateien:
        if datei.endswith('.txt'):
            source, ziel=ordner+'\\'+datei, target+'\\'+datei
            shutil.copy(source, ziel)
            
        
