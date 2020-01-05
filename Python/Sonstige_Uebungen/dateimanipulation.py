#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: create_folder_by_season.py
# Project: Desktop
#-----
# Created Date: Sunday 29.12.2019, 20:45
# Author: Raffael Baldinger
#-----
# Last Modified: Sunday 05.01.2020, 21:10
#-----
# Copyright (c) 2020 Raffael Baldinger
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Durchsuche Ordner und wende unterschiedliche Regeln auf die enthaltenen Dateien an
####

import os
import re
import shutil
from time import sleep


def init():
    pass

def menu():
    menu_items = {1 : "Dateien nach Suchmuster in entsprechenden Ordner verschieben", 
                  2 : "Fehler in Dateinamen korrigieren", 
                  3 : "Dateien mit Präfix versehen", 
                  4 : "Dateien aus Ordner in Elternordner verschieben und nach Ordner benennen", 
                  5 : "Wörter und Buchstabenfolgen aus Dateinamen entfernen",
                  0 : "Exit"}

    choosed = print_menu(menu_items)

    if choosed != 0:
        root_path = choose_path()
    if choosed in [1, 2, 4, 5]:
        regex_pattern = get_regex()

    if choosed == 1:
        pass
    elif choosed == 2:
        pass
    elif choosed == 3:
        pass
    elif choosed == 4:
        pass
    elif choosed == 5:
        pass
    elif choosed == 0:
        print("Programm wird beendet")
        sleep(5)
        exit()

def print_menu(items):
    while True:
        print("█"*100)
        # Generiere Menü	
        for item in items.keys():
            string = str(item) + ". "
            string = string.rjust(15)
            string = string+items[item].center(96-len(string))
            if item != 0:                                       # Null wird als Exit-Item verwendet, wenn nicht 0 normal ausgeben
                print("█ "+string.center(96)+" █")              
            else:
                zero_item = "█ "+string.center(96)+" █"         # Ist das Item 0 wird dies gesondert ausgegeben
        print("█"*100)
        print(zero_item)
        print("█"*100)
        choosed = input("Bitte Option auswählen: ")
        check = check_option(choosed, items)                    # Prüfe Eingabe anhand des vorgegebenen Menüs
        if check:
            return int(choosed)                                 # Ist die Eingabe valide wird der Wert zurückgegeben

def choose_path():
    # Entsprechenden Pfad auswählen
    checked = False
    while not checked:
        print("Bitte entsprechenden Grundpfad angeben:")
        path = input()
        path = path.strip('"')              # Entferne eventuelle Anführungszeichen aus Pfadangabe
        if os.path.exists(path):            # Prüfe Pfad auf richtigkeit
            os.chdir(path)                  # Wechsle Pfad zu Arbeitspfad
            return path
        else:
            print("Der angegebene Pfad konnte nicht gefunden werden")

def check_option(option, items):
    # Ist die Option eine Dezimalzahl und grösser 0 sowie kleiner der Anzahl Menüeinträge minus 1?
    if option.isdecimal() and int(option) >= 0 and int(option) <= len(items)-1:
        return True
    else: 
        return False

def get_regex():
    checked = False
    abstand = 45
    while not checked:
        print("Regexsuchmuster angeben: (--h für Cheat-Sheet)")
        regex = input()
        if "--h" in regex[:3] or len(regex) == 0:                       # Ausgabe Cheatsheet 
            helplines = [   r"\w = Wort", r"\W = kein Wort", 
                            r"\d = Zahl", r"\D = keine Zahl", 
                            r"\s = Leerschlag", r"\S = kein Leerschlag", 
                            r"[abc] = a,b und c müssen vorkommen", r"[^abc] = dürfen nicht vorkommen",
                            r"[a-g] = a-g können vorkommen", r"[a-zA-Z] = Alle Buchstaben von a-Z", 
                            r"^abc = Beginnt mit 'abc'", r"abc$ = Endet mit 'abc'", 
                            r"\\ = Maskieren eines Spezialcharakters", r"abc|bcd = abc oder bcd", 
                            r"\. = Jedes Zeichen ausser 'newline'", r"\t = Tabulator", 
                            r"\n = 'newline'", r"\r = Return", 
                            r"(abc) = als Gruppe erfassen", r"\1 = Gruppe 1", 
                            r"\d{1,5} = Zahlen zwischen ein- und fünfstellig", r"\d{2} = Zahlen, genau zweistellig", 
                            r"\d{2,} = Zahlen mindestens zweistellig", r"\d{2,}? = Mindestens zweistellig, so klein wie möglich", 
                            r"\d+ = 1 oder mehr", r"\d* = 0 oder mehr", 
                            r"\d? = 0 oder 1", r"\d* = 0 oder mehr"]

            linesplits = 2
            abstand = 50
            count, counter = 0, 0
            print("█"*100)
            for line in helplines:
                if len(regex.split(" ")) > 1:
                    if regex.split(" ")[1].lower() in line.lower():
                        if count < linesplits-1:
                            print(line.ljust(abstand), end="")
                            count += 1
                            counter += 1
                        elif count == linesplits-1:
                            print(line)
                            count = 0
                            counter += 1
                elif len(regex.split(" ")) == 1:
                    if count < linesplits-1:
                        print(line.ljust(abstand), end="")
                        count += 1
                        counter += 1
                    elif count == linesplits-1:
                        print(line)
                        count = 0
                        counter += 1
            print()
            print("█"*100)
            input("Enter zum fortsetzen")
            continue

        # Prüfe Regexangabe
        try:
            search_pattern = re.compile(regex)
            checked = True
        except:
            print("Angegebenes Suchmuster fehlerhaft!")

    return search_pattern


menu()
exit()

# Dateien mit bestimmtem Muster in neu angelegten Ordner verschieben
for i in range(0,22):
    newpath = ".\\Staffel "+str(i)
    for filename in os.listdir():
        pattern = re.compile(r'S(\d\d)E')
        if i < 10:
                current="0"+str(i)
        else:
                current=str(i)
        result = pattern.findall(filename)
        if current in result:
            if not os.path.exists(newpath):
                os.mkdir(newpath)
            shutil.move(filename, newpath)


# Präfix an Dateinamen hängen
os.chdir(root_path)
musthave = "Fringe "

for filename in os.listdir():
    if not musthave in filename:
        os.rename(filename, musthave+filename)


# Fehlerkorrektur in Dateinamen     
os.chdir(root_path)      
pattern = re.compile(r'S\d\d(S\d\d)E(\d{1,2})')
for filename in os.listdir():
    result = pattern.findall(filename)
    if len(result) > 0:
        staffel = result[0][0]
        episode = result[0][1]
        new_name = re.sub(pattern, staffel+"E"+episode, filename)
        # os.rename(filename, new_name)
        print(new_name)

# Bennene Dateien nach ihrem Ordner und verschiebe sie in den Elternordner
folder_list = os.listdir()
for item in folder_list:
    os.chdir(root_path)
    new_item = " ".join(item.split("."))
    pattern = re.compile(r'Subbed|HDTVR|HDT|SOF|XviD|HDTVRiP|SOF|iNTENTi|GERMAN|DUBBED|iP|-|Xv|Dubbed|SO|iNTERNAL|M4xd0me|Spon|Dubbed|Custom| WS| V|SSL')
    new_item = re.sub(pattern, "", new_item)
    new_item = "".join(new_item.split("  "))
    new_item = "".join(new_item.split("V"))
    new_item = new_item.rstrip(" ")
    os.chdir(root_path+"\\"+item)
    for filename in os.listdir():
        file_pattern = re.compile(r'\.avi')
        result = file_pattern.findall(filename)[0]
        new_filename = new_item+result
        os.rename(filename, new_filename)
        shutil.move(new_filename, root_path)

