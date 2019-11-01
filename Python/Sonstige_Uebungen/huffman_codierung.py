#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: huffman_codierung_v2.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Friday 01.11.2019, 12:25
# Author: Apop85
#-----
# Last Modified: Friday 01.11.2019, 14:50
#-----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
####

from copy import deepcopy as copy

def init():
    menu_items = {1: "Satz codieren", 0: "Beenden" }
    choice = create_menu(menu_items)
    if choice == 0:
        exit()
    elif choice == 1:
        print("\n"*5)
        data = get_data()
        encoded_data = encode_data(data)
        saved_space = 100-(100/(8*len(data)))*len(encoded_data)
        print(encoded_data, "\n\tSaved space: "+str(round(saved_space, 1))+"%")

def create_menu(menu_items):
    # Erstelle Menü anhand der übergebenen Menü-Liste
    while True:
        print("█"*80)
        for key in menu_items.keys():
            item = str(key) + ". "+menu_items[key]
            item_lenght = len(item)
            if key != 0:
                print ("█ "+" "*int(round((76-item_lenght)/2, 0))+item+" "*int((76-item_lenght)/2)+" █")
            else:
                # 0 für exit soll immer am Schluss kommen
                zero_item = (copy(item), item_lenght)
        # Ausgabe der exit-option am Schluss
        print("█"*80)
        print ("█ "+" "*int(round((76-zero_item[1])/2, 0))+zero_item[0]+" "*int((76-zero_item[1])/2)+" █")
        print("█"*80)
        choice = input(" "*30+"Auswahl: ")

        # Prüfe Eingabe ob Zahl und vorhanden in der Menüliste
        if choice.isdecimal() and int(choice) in menu_items.keys():
            return int(choice)
        else:
            print("░0"*40)
            print("Eingabe ungültig")
            print("░0"*40)

def get_data():
    print("█"*80)
    print("█"+"Zu codierenden Satz eingeben".center(78, " ")+"█")
    print("█"*80)
    data = input("Eingabe: ")
    return data


def encode_data(data):
    characters, character_path = get_character_list(data)
    tree, path = create_huffman_tree(characters, character_path)
    encoded_data = ""
    for character in data:
        encoded_data += path[character]
    return encoded_data

def get_character_list(data):
    # Erstelle Dictionary mit den Buchstaben und deren Anzahl
    char_list = {}
    character_path = {}
    for character in data:
        char_list.setdefault(character, 0)
        character_path.setdefault(character, "")
        char_list[character] += 1
    
    character_values = {}
    key_list = []

    # Lese Zahlen aus 
    for key in char_list.keys():
        key_list += [char_list[key]]
    key_list = sorted(key_list)

    # Dictionary umdrehen damit die Zahlen die Keys sind
    for value in key_list:
        for key in char_list.keys():
            if value == char_list[key]:
                character_values.setdefault(value, [])
                if key not in character_values[value]:
                    character_values[value] += [key]
                    break
    return character_values, character_path
    
def create_huffman_tree(data, char_path, depth=-1, original={}, huf_tree={}, rest_data=[]):
    depth += 1
    # Erstelle Kopie der Originaldaten bei erstem durchlauf
    if not depth:
        original = copy(data)

    # Lese aktuellen Key von data aus
    key_list = list(data.keys()) 
    current_key = key_list[0]
    huf_tree.setdefault(current_key, [])
    huf_tree[current_key] += data[current_key]

    # Verrechne den Wert der Buchstaben in 2er Paaren
    last_insert = 0
    for i in range(0, len(data[current_key]), 2):
        key_1 = data[current_key][i]
        try:
            # Wenn noch zwei Werte vorhanden sind verrechne miteinander
            key_2 = data[current_key][i+1]
            for key in char_path.keys():
                # Setze den Pfad des ersten Werts auf 1 und beim zweiten auf 0
                if key in key_1:
                    char_path[key] = "1"+char_path[key]
                elif key in key_2:
                    char_path[key] = "0"+char_path[key]
            new_key = current_key*2
            data.setdefault(new_key, [])
            data[new_key].insert(i, key_1+key_2)
            # last_insert = i
        except:
            # Ist bereits ein Restwert vorhanden, verrechne mit Restwert sonst erstelle Restwert 
            if len(rest_data) != 0:
                new_key = current_key + rest_data[0]
                data.setdefault(new_key, [])
                data[new_key].insert(last_insert, rest_data[1]+key_1)
                last_insert += 2
                for key in char_path.keys():
                    # Setze den wert aus dem vorherigen rest auf 0 den neuen auf 1
                    if key in rest_data[1]:
                        char_path[key] = "1"+char_path[key]
                    elif key in key_1:
                        char_path[key] = "0"+char_path[key]
                rest_data = []
            else:
                rest_data = [current_key, key_1]

    # Lösche die verwendeten Daten 
    del data[current_key]
    
    if len(list(data.keys())) > 0:
        # Sind noch Daten vorhanden führe Funktion mit neuem Datenset erneut durch.
        huf_tree, char_path = create_huffman_tree(data, char_path, depth, original, huf_tree, rest_data)
    # Bleiben keine Daten mehr übrig ist der Prozess abgeschlossen
    return huf_tree, char_path
    

init()