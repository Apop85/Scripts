#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_grundlagen_dictionaries.py
# Project: Kapitel_08_Dictionaries
# Created Date: Thursday 07.03.2019, 11:46
# Author: Apop85
# -----
# Last Modified: Thursday 07.03.2019, 12:22
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description:
###

import re

def output(title, string):
    max_length=80
    max_delta=20
    string+=' '*max_length
    print('╔'+'═'*max_length+'╗')
    print('║'+title.center(max_length).upper()+'║')
    print('╠'+'═'*max_length+'╣')
    search_pattern=re.compile(r'(.{'+str(max_length-max_delta-10)+r','+str(max_length-10)+r'}[^\w"])')
    reg_lines=search_pattern.findall(string)
    for line in reg_lines:
        print('║ '+line+' '*(max_length-len(line)-1)+'║')
    print('╚'+'═'*max_length+'╝')
    input()

output('Dictionary','Ein Dictionary besteht immer aus Schlüssel-Wert-Paaren. Der Schlüssel ermöglicht einen direkten Zugriff auf Daten.')
output('Typische Anwendungen','In Telefonbüchern findet man unter dem Namen (Schlüssel) die Anschrift und die Telefonnummer raus (Werte). Über das Autokennzeichen (Schlüssel) kann man den Halter (Wert) ausfindig machen. In einem De-En Wörterbuch findet man bei einem deutschen Wort (Schlüssel) die englische Übersetzung (Wert).')

output('Operationen','Nachfolgend werden alle Operationen aufgeführt welche auf ein Dictionary angewendet werden können.')
output('dictionary[key]','Mit der Operation dictionary[key] kann der Wert zu dem angegebenen Schlüssel aufgerufen werden.')
output('dictionary[key] = x','Mit der Operation dictionary[key] = x wird dem angegebenen Schlüssel der Wert x zugeteilt.')
output('dictionary.clear()','Die Funktion .clear() löscht den Inhalt des Dictionaries.')
output('dictionary.copy()','Die Funktion .copy() erstellt eine Flache kopie des Dictionaries. Heisst es wird einfach auf die Keys und Werte des originalen Dictionaries verwiesen.')
output('del dictionary[key]','Mit der Anweisung del dictionary[key] kann das Wertepaar mit dem angegebenen Key gelöscht werden.')
output('dictionary.get(key,x)','Mit der Funktion .get(key, x) lässt sich nach einem Schlüssel im Dictionary suchen. Ist dieser nicht vorhanden wird der Wert x zurückgegeben.')
output('key in dictionary','Diese Prüfung gibt True zurück falls der angegebene Key im Dictionary existiert.')
output('key not in dictionary','Diese Prüfung gibt True zurück falls der angegebene Key nicht im Dictionary existiert.')
output('dictionary.items()','Die Funktion items() gibt ein dict_keys-Objekt aus welches Tupel aus den Schlüssel-Wert-Paaren des Dictionaries enthält')
output('dictionary.keys()','Die Funktion keys() gibt alle Schlüsselwörter des Dictionaries als dict_keys-Objekt zurück.')
output('dictionary.setdefault(key,x)','Die Funktion .setdefault() prüft ob ein Schlüssel bereits vorhanden ist und falls nicht wird ein Schlüssel-Wert-Paar mit dem Schlüssel und dem angegebenen Wert erstellt.')
output('dictionary.update(dictionary_2)','Für alle Schlüssel-Wert-Paare in dictionary_2 wird ein Schlüssel-Wert-Paar in dictionary angelegt.')
output('dictionary.values()','Die Funktion .values() gibt alle Werte eines Dictionarys in Form eines dict_values-Objekts aus.')

