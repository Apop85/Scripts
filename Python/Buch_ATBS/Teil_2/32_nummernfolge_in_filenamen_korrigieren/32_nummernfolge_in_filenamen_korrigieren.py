# 32_nummernfolge_in_filenamen_korrigieren.py
# In dieser Übung geht es darum nummerierte Dateien auf ihre Sequenz zu überprüfen. Sollte eine Sequenz
# unterbrochen sein sollen die Dateinamen korrigiert werden. 

import os, shutil, re

os.chdir(os.path.dirname(__file__))
target='.\\correct_me'

def categorize_files():
    file_dict={}
    # Durchsuche Ordner und Unterordner nach files mit dem Muster name+dreistellige zahl+endung 
    # aufgesplittet wird es nach Name und Endung und dann wird gezählt wie oft die jeweiligen 
    # Endungen vorkommen
    for folder, subfolder, files in os.walk(target):
        for file_name in files:
            results=re.split(r'([a-z]+)([0-9]+)(\.[a-z]{2,3})', file_name)
            file_dict.setdefault(results[1]+results[3], 0)
            file_dict[results[1]+results[3]]+=1
        get_new_filename(file_dict.keys(), folder, file_dict, files)

def get_new_filename(keys, folder, file_dict, files):
    global counter
    print(keys)
    for key in keys:
        counter=1
        for i in range(file_dict[key]):
            # Hier wird nochmals in dateiname und endung aufgeteilt um daraus dann wieder 
            # den neuen Dateinamen zu generieren 
            file_name=re.split(r'([a-z]+)(\.[a-z]{2,3})', key)
            rename_files(file_name, folder, files, i+1)

def rename_files(name, folder, files, range):
    # Hier wird geprüft ob die vorhandenen dateien dem vorgegebenen Muster folgen und falls nicht 
    # werden sie umbenannt. 
    global counter
    file_name='\\n\\n'
    while file_name not in files:
        file_name=generate_filename(counter, file_name, name)
        counter+=1
    else:
        new_name=generate_filename(range, file_name, name)
        if new_name != file_name:
            shutil.move(folder+'\\'+file_name, folder+'\\'+new_name)

def generate_filename(range, file_name, name):
    if range == 1000:
        print('End of possible numbers')
        return
    elif range >= 100:
        file_name=name[1]+str(range)+name[2]
    elif range < 10:
        file_name=name[1]+'00'+str(range)+name[2]
    elif range < 100:
        file_name=name[1]+'0'+str(range)+name[2]            
    else:
        return
    return file_name

            
categorize_files()
