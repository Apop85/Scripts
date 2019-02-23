#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_kapitel_05_repetitionsfragen.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 14:48
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 22:19
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
###

import re

max_text_length=70
max_text_delta=14

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\(?"?\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-9)+r'}[ |.|,|\n|>|\W|\)|-]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Aufgabe 1','Welchen Wahrheitswert haben die folgenden Ausdrücke?')
output('Beispiel 1','"a" == "A"')
output('Lösung Beispiel 1','False, der Vergleich ist case sensitive.')
output('Beispiel 2','"a ">"a"')
output('Lösung Beispiel 2','True, der erste String besteht aus 2 Zeichen.')
output('Beispiel 3','"Baum" == 1')
output('Lösung Beispiel 3','False, der Vergleich zwei verschiedener Datentypen ergibt immer False')
output('Beispiel 4','123 > 2')
output('Lösung Beispiel 4','True, die Zahl 123 is grösser als 2')
output('Beispiel 5','"123" > "2"')
output('Lösung Beispiel 5','False weil "123" vor "2" kommt in lexikografischer Reihenfolge?')
output('Beispiel 6','"Sonne" != "Sonne"')
output('Lösung Beispiel 6','False, die Strings sind gleichwertig.')
output('Beispiel 7','12.2 > 12')
output('Lösung Beispiel 7','True, 12.2 ist grösser alsa 12')
output('Beispiel 8','12 + 4 > 4')
output('Lösung Beispiel 8','True, das Ergebnis aus 12+4 ist grösser als 4.')
output('Beispiel 9','12//4 <= 3')
output('Lösung Beispiel 9','True, 12 geteilt durch 4 ergibt 3 was wiederum kleiner-gleich 3 ist.')
output('Beispiel 10','"Kuh" not in ["Katze", "Hund", "Pferd", "Kuh"]')
output('Lösung Beispiel 10','False, da sich der Eintrag "Kuh" in der Liste befindet.')
output('Beispiel 11','3 in [2,3,5,7,11,13]')
output('Lösung Beispiel 11','True, der Wert 3 befindet sich in der Liste')

