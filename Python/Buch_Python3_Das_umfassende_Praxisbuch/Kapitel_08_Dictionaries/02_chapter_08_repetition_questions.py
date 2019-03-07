#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_chapter_08_repetition_questions.py
# Project: Kapitel_08_Dictionaries
# Created Date: Thursday 07.03.2019, 12:23
# Author: Apop85
# -----
# Last Modified: Thursday 07.03.2019, 13:22
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 8. Page 244. Task 1 - 4
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

output('Aufgabe 1','Folgendes Dictionary ist vorgegeben: d={"E260":"Essigsäure", "E200":"Sorbinsäure", "E210":"Benzoesäure"}. Was ergeben folgende Ausdrücke:')
output('Ausdruck 1','d["E210"]')
output('Lösung Ausdruck 1','Gibt den Wert des Keys "E210" zurück: "Benzoesäure"')
output('Ausdruck 2','print(list(d.keys()))')
output('Lösung Ausdruck 2','Gibt eine Liste aller enthaltenenen Keys des Dictionaries auf den Bildschirm aus: ["E260", "E200", "E210"]')
output('Ausdruck 3','del d["E210"]: print(list(d.values()))')
output('Lösung Ausdruck 3','Entfernt das Schlüssel-Wert-Paar mit dem Schlüssel "E210" und gibt die Werte des restlichen Dictionary auf dem Bildschirm aus: ["Essigsäure", "Sorbinsäure"]')
output('Ausdruck 4','for k in d.keys(): print(k+": "+d[k])')
output('Lösung Ausdruck 4','Gibt die Schlüssel-Wert-Parre zurück: "E260: Essigsäure", "E200: Sorbinsäure"')
output('Ausdruck 5','d["E239"]="Kaliumnitrit": for k in d.keys(): print(k)')
output('Lösung Ausdruck 5','Erstellt einen neuen Eintrag mit "E239" und dem Wert "Kaliumnitrit" und gibt dann alle Keys auf den Bilschirm zurück: "E260", "E200", "E239"')

output('Aufgabe 2','Welche Pythonanweisung ist gesucht? Gegeben ist ein Dictionary mit den Buchstaben als Schlüssel und dem Nato-Alphabet als Werte.')
output('Anweisung 1','Erstellen sie ein Dictionary mit dem Namen d.')
output('Lösung Anweisung 1','d={}')
output('Anweisung 2','Das Dictionary soll zu einzelnen Buchstaben den Buchstabier-Code im Nato-Alphabet liefern. Fügen Sie drei entsprechende Schlüssel-Wert-Paare in d ein.')
output('Lösung Anweisung 2','d.update({"a":"alfa","b":"bravo","c":"charlie"})')
output('Anweisung 3','Der Nato-Code des Buchstabens a wird auf dem Bildschirm ausgegeben.')
output('Lösung Anweisung 3','print(d["a"])')
output('Anweisung 4','Sämtliche Werte des Dictionaries werden ausgegeben.')
output('Lösung Anweisung 4','print(list(d.values()))')
output('Anweisung 5','Falls "h" als Schlüssel vorhanden ist soll der entsprechende Wert zurückgegeben werden.')
output('Lösung Anweisung 5','d.get("h")')

output('Aufgabe 3','Gegeben ist folgendes Dicionary: vokabeln={"hat":["Hut"], "shirt":["Hemd","Bluse"], "sock":["Socke", "Strumpf"]}.  Was ergeben die nachfolgenden Anweisungen?')
output('Anweisung 1','print(vokabeln["shirt"])')
output('Lösung Anweisung 1','Gibt den Wert des Schlüssels "shirt" auf dem Bildschirm aus: ["Hemd","Bluse"]')
output('Anweisung 2','print(vokabeln["shirt"][0])')
output('Lösung Anweisung 2','Gibt den ersten Wert des Schlüssels "shirt" auf dem Bildschirm aus: "Hemd"')
output('Anweisung 3','for key in vokabeln.keys(): print(vokabeln[key])')
output('Lösung Anweisung 3','Gibt alle Werte der Schlüssel auf dem Bildschirm aus: ["Hut"], ["Hemd", "Bluse"], ["Socke", "Strumpf"]')
output('Anweisung 4','print(list(vokabeln.values()))')
output('Lösung Anweisung 4','Gibt alle Werte des Dictionaries als Liste auf dem Bildschirm aus: [["Hut"], ["Hemd", "Bluse"], ["Socke", "Strumpf"]]')
output('Anweisung 5','for key in vokabeln.keys(): print(key+": ", end=" ": for w in vokabeln[key]: print(w, end=" "): print()')
output('Lösung Anweisung 5','Gibt alle Schlüssel-Wert-Paare auf den Bildschirm aus. Pro Paar eine Zeile: "hat: Hut", "shirt: Hemd Bluse", "sock: Socke Strumpf"')
output('Anweisung 6','vokabeln["cap"]=["Mütze"]: print(vokabeln["cap"])')
output('Lösung Anweisung 6','Legt ein neues Schlüssel-Wert-Paar mit dem Inhalt "cap":["Mütze"] an und gibt dann den Wert auf dem Bildschirm aus: ["Mütze"]')
output('Anweisung 7','vokabeln["cap"]=vokabeln["cap"]+["Kappe"]: print(vokabeln["cap"])')
output('Lösung Anweisung 7','Fügt den Wert "Kappe" dem Schlüssel "cap" hinzu und gibt dann den Wert des Schlüssels "cap" auf dem Bildschirm aus: ["Mütze", "Kappe"]')

output('Aufgabe 4','Was machen folgende Pythonskripte:')
print('Script 1:')
print('liste=["Auto", "Apfel", "Cello", "Banane", "Berg"]')
print('d={}')
print('for key in "ABC":')
print('\td[key] = [w for w in liste if w[0] == key]')
print('print(d)')
input()
output('Lösung Script 1','Erstellt die Keys "A", "B" und "C" und fügt alle Listeneinträge mit dem entprechenden Anfangsbuchstaben den Keys hinzu: {"A":["Auto","Apfel"],"B":["Banane", "Berg"], "C":["Cello"]}')
print('Script 2:')
print('def menge(s):')
print('\td={}')
print('\tfor i in s:')
print('\t\td[i] = None')
print('\treturn list(d.keys())')
print()
print('s=[1,2,5,67,7,2,1,1,36,2]')
print('print(menge(s))')
input()
output('Lösung Script 2','Erstellt aus jedem Eintrag der Liste "s" einen Schlüssel mit dem Wert None. Da Schlüssel nur einmal vergeben werden können werden keine doppelten Einträge erstellt. Zurückgegeben wird dann die Liste der Schlüssel: [1,2,5,67,7,36]')

