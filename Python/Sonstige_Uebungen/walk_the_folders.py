#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: walk_the_folders.py
# Project: sonstige_uebungen
#-----
# Created Date: Monday 20.04.2020, 12:17
# Author: Apop85
#-----
# Last Modified: Monday 20.04.2020, 21:50
#-----
# Copyright (c) 2020 Raffael Baldinger
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: This script analyzes every Folder and Subfolder and lists Folder, Subfolder and Files as well as filesize for each extension.
####
import os

def loading():
    # Setze globale Variablen
    global current_direction
    global current_position
    # Defniere Aussehen des Ladebalkens
    loading_bar = "[                                ]"
    # Definiere das zu bewegende Objekt
    loading_symbol_fw = ">++('>"
    loading_symbol_bw = "<')++<"

    # Ist die Variabel current_direction noch nicht gesetzt wird ein Error ausgelöst
    try:
        test = current_direction*5
    except:
        # Der Error wird abgefangen und die Variablen gesetzt
        current_direction = 0
        current_position = 1

    # Ist die Bewegung vorwärts erhöhe Position um 1
    if current_direction == 0:
        current_position += 1
        symbol = loading_symbol_fw
    # Ist die Bewegung rückwärts vermindere Position um 1
    else:
        current_position -= 1
        symbol = loading_symbol_bw

    # Prüfe ob der Rand des Ladebalkens erreicht wurde und wechsle Laufrichtung
    if current_position >= len(loading_bar)-1:
        current_direction = 1
    elif current_position == 1:
        current_direction = 0

    # Gebe Ladebalken aus
    print("\r"*(len(loading_bar)+len(symbol)) + loading_bar[0:current_position] + symbol + loading_bar[current_position:], end="")


# Fordere Zielpfad an
while True:
    target_dir = input("Bitte Zielpfad angeben: ")
    # Prüfe ob Pfad ein Ordner ist und existiert
    if os.path.exists(target_dir) and os.path.isdir(target_dir):
        break
    else:
        print("Pfad konnte nicht gefunden werden")

# Fordere Pfad für die Zieldatei an
while True:
    target_file = input("Bitte Pfad für Ausgabedatei angeben: ")
    # Splitte Pfad auf
    rel_path = target_file.split("\\")
    del rel_path[-1]
    # Füge absoluter Pfad zusammen
    rel_path = "\\".join(rel_path)
    if os.path.exists(rel_path):
        break
    else:
        print("Pfad " + rel_path + " konnte nicht gefunden werden.")

# Frage ob nach jedem Eintrag eine Pause eingelegt werden soll
pause = "na"
while not pause in ["0","1"]:
    pause = input("Nach jedem Ordner eine Pause? (0 = nein, 1 = ja): ")


# Füllelemente
filler = "█"
filler2 = "░"
# Leere Resultattabelle erstellen
result_table = {target_dir : {}}

# Erstelle Walk-Generator
folder_table = os.walk(target_dir)

# Animationsfunktion
def animation(key=0):
    if (key+1 <= len(ani_row)-1):
        return key+1
    else:
        return 0

anim_now = 0
ani_row = ["\\", "\\", "|","|", "/", "/", "–", "–"]

# Prüfe alle Einträge
for folder, subfolder, filename in folder_table:
    anim_now = animation(anim_now)
    # print("Verarbeite... " + ani_row[anim_now] + " "*30 + "\r"*50, end="")
    loading()
    # Existiert noch kein Eintrag für den aktuellen Ordner dann erstellen
    if not folder in result_table.keys():
        # Lese übergeordneter Ordner aus
        last_folder = folder.split("\\")
        del last_folder[-1]
        last_folder = "\\".join(last_folder)
        # Erstelle EIntrag in Resultattabelle
        result_table.setdefault(folder, {})

    # Wenn Dateien im Ordner existieren
    if filename != []:
        # Lege Eintrag unter dem Schlüssel "FILE" an mit den Dateien
        result_table[folder].setdefault("FILE", filename)
        for file in filename:
            # Lese Dateiendung aus
            file_extension = (file.split("."))[-1]
            # Erstelle Eintrag für Dateiendung mit Bytecounter
            result_table[folder].setdefault(file_extension, 0)
            try:
                # Versuche Dateigrösse auszulesen
                file_size = (os.stat(folder + "\\" + file)).st_size
                # Füge bytes dem Bytecounter hinzu
                result_table[folder][file_extension] += file_size
            except:
                pass
    # Sind Unterordner vorhanden  
    if subfolder != []:
        # Lege EIntrag mit dem Schlüssel "SUB" an mit den Unterordnern
        result_table[folder].setdefault("SUB", subfolder)

def print_n_save(content):
    print(content)
    file_writer.write(content + "\n")

# Bereite Filewriter vor
file_writer = open(target_file, "w", encoding="utf-8")
# Laufe alle Ordner durch
for key in result_table.keys():
    print_n_save(filler*100)
    print_n_save("Ordner: " + key)
    # Laufe alle Einträge im Ordner durch
    for subkey in result_table[key].keys():
        # Lautet der Key "SUB"?
        if subkey == "SUB":
            print_n_save(filler2*100)
            print_n_save("Unterordner:")
            # Gebe alle unterordner aus
            for foldername in result_table[key][subkey]:
                print_n_save("--> " + foldername)
            print_n_save(filler2*100)
        # Lautet der Key "FILE"?
        elif subkey == "FILE":
            print_n_save(filler2*100)
            print_n_save("Dateien:")
            # Gebe alle Dateien aus
            for filename in result_table[key][subkey]:
                print_n_save("--> " + filename)
            print_n_save(filler2*100)
        else:
            # Ist es weder FILE noch SUB, ist es ein Dateityp 
            print_n_save("Dateityp: " + subkey + " - Totalgrösse: " + str(int(result_table[key][subkey]/1000)) + " kb")
    if pause == "1":
        input("Enter zum Fortfahren")

# Speichere Output-Datei
file_writer.close()
