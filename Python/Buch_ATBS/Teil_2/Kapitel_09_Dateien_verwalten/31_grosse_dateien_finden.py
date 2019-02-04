# 31_grosse_dateien_finden.py
# Dieses Script durchsucht den angegebenen Pfad und merkt sich wie Gross die entsprechenden Ordner 
# waren und gibt danach die grössten Dateien und Ordner in einer Liste aus

import os, sys
from collections import OrderedDict
choosendir=''
biggest, folder_size_list, file_size_list={'Biggest file:': ['', 0], 'Biggest folder:' : ['', 0]}, {}, {}

def menu():
    while True:
        print(''.center(110, '█'))
        print(''.rjust(35, '█')+' 1. Top X Files anzeigen '.center(40)+''.rjust(35, '█'))
        print(''.rjust(35, '█')+' 2. Top X Verzeichnisse anzeigen '.center(40)+''.rjust(35, '█'))
        print(''.rjust(35, '█')+' 3. Erneut durchsuchen '.center(40)+''.rjust(35, '█'))
        print(''.rjust(35, '█')+' 4. Verzeichnis wechseln '.center(40)+''.rjust(35, '█'))
        print(''.rjust(35, '█')+' 5. Script Beenden '.center(40)+''.rjust(35, '█'))
        print(''.center(110, '█'))
        choose=input(''.center(50))
        if choose == '1':
            if choosendir != '':
                top_X_files()
            else:
                choose_dir()
                crawl_harddrive()
                top_X_files()
        if choose == '2':
            if choosendir != '':
                top_X_folders()
            else:
                choose_dir()
                crawl_harddrive()
                top_X_folders()
        elif choose == '3':
            if choosendir != '':
                crawl_harddrive()
            else:
                choose_dir()
                crawl_harddrive()
        elif choose == '4':
            choose_dir()
            crawl_harddrive()
        elif choose == '5':
            sys.exit(0)
        else:
            print(' Eingabe nicht verstanden '.center(110))

def choose_dir():
    while True:
        global choosendir
        print(''.center(110, '█'))
        print(''.center(40, '█')+' Verzeichnis Angeben: '.center(30)+''.center(40, '█'))
        print(''.center(110, '█'))
        choosendir=input('Verzeichnis: '.rjust(50))
        if os.path.exists(choosendir):
            print(''.center(110, '█'))
            print(' Bitte Warten, Verzeichnisse werden gescannt. '.center(110, '█'))
            print(''.center(110, '█'))
            os.chdir(choosendir)
            break
        else:
            print(('Verzeichnis ' + choosendir + ' existiert nicht!').center(110))

def crawl_harddrive():
    global biggest, folder_size_list, file_size_list
    for folder, subfolder, file_list in os.walk(os.getcwd()):
        folder_size=0
        if '$' and '~~amd64~~' in folder:
            continue
        for file_name in file_list:
            if 'Edge' in file_name or '__' in file_name or file_name.startswith('$'):
                continue
            file_size=(os.path.getsize(folder+'\\'+file_name))
            folder_name=folder
            if file_size > 10**6:
                file_size_list.setdefault(file_size, folder_name+'\\'+file_name)
            folder_size+=file_size
        folder_size_list.setdefault(folder_size, folder_name)

def top_X_files():
    global file_size_list
    amount=choose_amount('Files')
    print(''.center(110, '█'))
    print((' Top '+str(amount)+' Files ').center(110, '█'))
    print(''.center(110, '█'))
    file_size_list=OrderedDict(sorted(file_size_list.items(), reverse=True))
    print(''.center(110, '█'))
    for i in range(amount):
        f_size, f_name=list(file_size_list.keys())[i], list(file_size_list.values())[i]
        print(str(i)+':  '+(str(f_size//10**6)+' MB').center(8)+' ---> '+f_name[-80:])
    print(''.center(110, '█'))
    input()

def top_X_folders():
    amount=choose_amount('Verzeichnisse')
    print(''.center(110, '█'))
    print((' Top '+str(amount)+' Verzeichnisse ').center(110, '█'))
    print(''.center(110, '█'))
    global folder_size_list
    folder_size_list=OrderedDict(sorted(folder_size_list.items(), reverse=True))
    for i in range(amount):
        f_size, f_name=list(folder_size_list.keys())[i], list(folder_size_list.values())[i]
        print(str(i)+':  '+(str(f_size//10**6)+' MB').center(8)+' ---> '+f_name[-80:])
    print(''.center(110, '█'))
    input()

def choose_amount(name):
    while True:
        print(''.center(110, '█'))
        print(('Wieviele '+name+' sollen angezeigt werden?').center(110, '█'))
        print(''.center(110, '█'))
        amount=input('Anzahl:'.rjust(50))
        if amount.isalnum():
            return int(amount)


menu()
