#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 08_chapter_07_repetition_questions.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 05.03.2019, 16:17
# Author: Apop85
# -----
# Last Modified: Tuesday 05.03.2019, 16:43
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

output('Aufgabe 1','Die Ausgangsliste lautet ["mond","stoff","treib","raum","schiff"]. Wie lautet die Ausgabe von folgenden Anweisungen?')
output('Anweisung 1','print(liste[0])')
output('Lösung Anweisung 1:','Das erste Item der Liste wird ausgegeben: "mond"')
output('Anweisung 2','print(liste[2]+liste[1])')
output('Lösung Anweisung 2:','Das dritte und zweite Item der liste wird konkatiniert: "treibstoff"')
output('Anweisung 3','print(liste[-2]+liste[-1])')
output('Lösung Anweisung 3:','Das zweitletzte und letzte Item der Liste wird konkatiniert: "raumschiff"')
output('Anweisung 4','for wort in liste: if wort[0] == "s": print(wort)')
output('Lösung Anweisung 4:','Alle Items der Liste die mit einem "s" beginnen werden ausgegeben: "stoff", "schiff"')
output('Anweisung 5','for wort in liste: print(wort[1])')
output('Lösung Anweisung 5:','Von jedem Item der Liste wird der 2. Buchstabe ausgegeben: o,t,r,a,c')
output('Anweisung 6','liste=liste+["gestein"]')
output('Lösung Anweisung 6:','Fügt der Liste ein weiteres Item mit dem Inhalt "gestein" hinzu: ["mond","stoff","treib","raum","schiff", "gestein"]')
output('Anweisung 7','print(liste[0]+liste[-1])')
output('Lösung Anweisung 7:','Das erste und letzte Item der Liste wird konkatiniert: "mondgestein"')

output('Aufgabe 2','Welchen Wert haben die Listenobjekte s1,s2 und s3 jeweils nach folgenden Anweisungen:')
output('Anweisung 1','s1 = [1]: s1=[1,s1]: s1=[1,s1]')
output('Lösung Anweisung 1','s1=[1,[1,[1]]]')
output('Anweisung 2','A=["Haus","Garten"]: B=["bau","tier","pflanze"]: s2=[i+j for i in A for j in B]')
output('Lösung Anweisung 2','"Hausbau", "Haustier", "Hauspflanze", "Gartenbau", "Gartentier", "Gartenpflanze"')
output('Anweisung 3','A=[1,2,3,4]: B=[2,3,4,5]: s3=[i for i in A+B if (i not in A) or (i not in B)')
output('Lösung Anweisung 3','Es werden alle Zahlen ausgegeben welche nicht in beiden Listen vorkommen: 1,5')
