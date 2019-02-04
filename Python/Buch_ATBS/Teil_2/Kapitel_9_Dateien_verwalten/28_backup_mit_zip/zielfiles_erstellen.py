# Hilfsscript um die Zielfiles zu erstellen
import os
os.chdir(os.path.dirname(__file__))

while True:
    print('Filenamen eingeben:')
    name=input()
    file=open('.\\backup_me\\'+name+'.txt', 'w')
    file.write('DUMMYTEXT')
    file.close()