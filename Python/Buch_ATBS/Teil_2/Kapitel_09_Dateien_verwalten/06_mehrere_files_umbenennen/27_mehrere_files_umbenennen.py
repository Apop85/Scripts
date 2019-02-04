# 27_mehrere_files_umbenennen.py
# In diesem Script geht es darum Files mit amerikanischer Datumsangabe (mm/dd/yyyy) in ihrem Namen zu finden und sie entsprechend der Europäischen Datumsangabe (dd/mm/yyyy) einheitlich anzupassen.

import os, re, shutil
os.chdir(os.path.dirname(__file__))
zielordner=os.path.dirname(__file__)+'\\umbenannt'

def check_4_files():
    for folder, subfolder, files in os.walk('.\\'):
        suchmuster=re.compile(r'([0|1]?\d)(-?)([0|1|2|3]\d)(-?)(20\d{2})')
        ergebnis=suchmuster.findall(' '.join(files))
        i=0
        if ergebnis != []:
            for file in files:
                if ergebnis[i][0] and ergebnis[i][2] and ergebnis[i][4] in file:
                    filename=suchmuster.sub(r'\3\2\1\4\5', file)
                    shutil.copy(folder+'\\'+file, zielordner+'\\'+filename)
                    i+=1

check_4_files()