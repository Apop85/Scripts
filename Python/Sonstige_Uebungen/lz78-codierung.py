#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: lz78-codierung.py
# Project: Sonstige_Uebungen
# -----
# Created Date: Wednesday 06.11.2019, 12:40
# Author: Apop85
# -----
# Last Modified: Wed Nov 06 2019
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Encode with lz78 encoding
####

from copy import deepcopy as copy
import re


def init(switch=False):
    if switch:
        base_dictionary = ["a","b","c","d","e","f","g","h","i","j", "k",
                           "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
                           "v", "w", "x", "y", "z"]
    else:
        base_dictionary = ["a","b","c","d"]

    return base_dictionary


def choose_option(options):
    menu_items = {0: "Beenden", 1: "Daten codieren", 2: "Mit eigenem Wörterbuch codieren", 3: "Ausgabeoptionen" }
    choice = print_menu(menu_items)
    if choice == 0:
        exit()

    if choice in [1, 2]:
        message = ("Information","Zu codierende Daten eingeben")
        data = get_data(message)

    if choice == 1:
        encoded_data, dictionary = encode_data(data)
        if options[0]:
            print_result(("Encodierte Daten", encoded_data))
        if options[1]:
            print_result(("Wörterbuch", str(dictionary).strip("[]}{")))
        if options[2]:
            original_data, compressed, dict_length = get_status_data(data, encoded_data, dictionary)            
            print_stats(original_data, compressed, dict_length)
            
    elif choice == 2:
        message = ("Eingabe", "Wörterbuch definieren (Beispiel: abcde oder dehkw)")
        dictionary = list(get_data(message))
        encoded_data, dictionary = encode_data(data, dictionary)
        if options[0]:
            print_result(("Encodierte Daten", encoded_data))
        if options[1]:
            print_result(("Wörterbuch", str(dictionary).strip("[]}{")))
        if options[2]:
            original_data, compressed, dict_length = get_status_data(data, encoded_data, dictionary)
            print_stats(original_data, compressed, dict_length)

    elif choice == 3:
        options = setup_options(options)
    
    return options

def get_status_data(data, encoded_data, dictionary):
    original_data = len(data)*8
    compressed = len(encoded_data.split(","))*8
    key_sum = 0
    # Rechne Schlüssel in Bytes um
    for key in dictionary.keys():
        if key/(2**8) < 1:
            key_sum += 1
        elif key/(2**8) < 2:
            key_sum += 2
        elif key/(2**8) < 3:
            key_sum += 3
        elif key/(2**8) < 4:
            key_sum += 4
        elif key/(2**8) < 5:
            key_sum += 5
        elif key/(2**8) < 6:
            key_sum += 6

    compressed_dict = str(list(dictionary.values())).strip("}{[]")
    compressed_dict = "".join(compressed_dict.split(" "))
    compressed_dict = "".join(compressed_dict.split('"'))
    compressed_dict = "".join(compressed_dict.split("'"))

    dict_length = (len(compressed_dict)*8)+key_sum
    return original_data, compressed, dict_length


def print_stats(original_data, compressed, dict_length):
    print()
    print("╔"+"═" * 78+"╗")
    line = "Originalgrösse: "+str(original_data)+" bytes"
    print("║"+line.center(78)+"║")
    print("╠"+"═" * 78+"╣")
    line = "Komprimiert: "+str(compressed)+" bytes"
    print("║"+line.center(78)+"║")
    line = "Wörterbuch: "+str(dict_length)+" bytes"
    print("║"+line.center(78)+"║")
    print("╠"+"═" * 78+"╣")
    line = "Total komprimiert: "+str(dict_length+compressed)+" bytes"
    print("║"+line.center(78)+"║")
    print("╚"+"═" * 78+"╝")
    input("Enter zum fortfahren")


def print_menu(menu_items):
    print("\n"*20)
    while True:
        print("█" * 80)
        for key in menu_items.keys():
            item = str(key)+". "+menu_items[key]
            if key != 0:
                print("█"+item.center(78)+"█")
            else:
                zero_item = copy(item)
        print("█" * 80)
        print("█"+zero_item.center(78)+"█")
        print("█" * 80)

        choice = input("Auswahl: ")

        # Prüfe Eingabe auf richtigkeit
        if choice.isdecimal() and int(choice) in menu_items.keys():
            return int(choice)
        else:
            message = ("Information","Ungültige Auswahl!")
            print_infobox(message)


def print_infobox(message):
    print()
    print("╔"+"═" * 78+"╗")
    print("║"+message[0].upper().center(78)+"║")
    print("╠"+"═" * 78+"╣")
    print("║"+message[1].center(78)+"║")
    print("╚"+"═" * 78+"╝")
    print()


def print_result(message):
    print()
    print("╔"+"═" * 78+"╗")
    print("║"+message[0].upper().center(78)+"║")
    print("╠"+"═" * 78+"╣")
    if len(message[1]) < 77: 
        print("║"+message[1].center(78)+"║")
    else:
        split_pattern = re.compile(r".{1,72},?")
        lines = re.findall(split_pattern, message[1])
        for line in lines:
            print("║"+line.center(78)+"║")
    print("╚"+"═" * 78+"╝")
    input("Enter zum Fortfahren")


def setup_options(current_options):
    while True: 
        options = {0:"Speichern und zurück", 1:"Encodierte Daten ausgeben: ", 2:"Wörterbuch ausgeben: ", 3:"Statistik ausgeben: "}
        if current_options[0]:
            options[1] += "EIN"
        else:
            options[1] += "AUS"
        if current_options[1]:
            options[2] += "EIN"
        else:
            options[2] += "AUS"
        if current_options[2]:
            options[3] += "EIN"
        else:
            options[3] += "AUS"
        
        choice = print_menu(options)
        if choice == 1:
            if current_options[0]:
                current_options[0] = False
            else:
                current_options[0] = True
        elif choice == 2:
            if current_options[1]:
                current_options[1] = False
            else:
                current_options[1] = True
        elif choice == 3:
            if current_options[2]:
                current_options[2] = False
            else:
                current_options[2] = True
        elif choice == 0:
            return current_options


def get_data(message):
    print_infobox(message)
    data = input("Daten: ")
    return data


def encode_data(data, dictionary=[None]):
    encoded = ""
    # Wenn die Daten mehr als 100 Zeichen beinhalten nehme vollständiges Dictionary, sonst ein minimales
    if len(data) > 100 and len(dictionary) > 0 and dictionary[0] == None:
        dictionary = init(True)
    elif dictionary == [] or dictionary[0] != None:
        pass
    else:
        dictionary = init()

    last_letter = ""
    data = list(data)
    # Füge End of File markierung ein
    data += ["EOF"]
    for letter in data:
        if (last_letter in dictionary and not last_letter + letter in dictionary) or letter == "EOF":
            # Ist die letzte Buchstabenkombination vorhanden trage Key als Encodierung ein
            encoded = encoded + "," + str(dictionary.index(last_letter))
        
        if not letter in dictionary and letter != "EOF":
            # Wenn der Buchstabe an sich nicht im Dictionary existiert einen entsprechenden Eintrag erstellen  
            dictionary += [letter]
            
        if not last_letter + letter in dictionary and letter != "EOF":
            # Ist die Buchstabenkombination noch nicht vorhanden wird ein Eintrag angelegt
            dictionary += [last_letter+letter]
            last_letter = letter
        else:
            # Ist die Buchstabenkombination vorhanden übernehme diese für die nächste Auswertung
            last_letter = last_letter + letter
            
    encoded = encoded.lstrip(",")
    dictionary = cleanup_dictionary(dictionary, encoded)
    return encoded, dictionary

def cleanup_dictionary(dictionary, encoded):
    # Entferne ungenutzte Einträge aus dem Wörterbuch 
    encoded = encoded.split(",")
    cleaned_dictionary = {}
    for key in encoded:
            cleaned_dictionary.setdefault(int(key), dictionary[int(key)])
    return cleaned_dictionary
 
    
default_options = [True, False, False]
while True:
    default_options = choose_option(default_options)
