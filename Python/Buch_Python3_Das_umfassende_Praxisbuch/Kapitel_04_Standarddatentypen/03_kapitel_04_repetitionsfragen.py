#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_kapitel_04_repetitionsfragen.py
# Project: Kapitel_04_Standarddatentypen
# Created Date: Saturday 23.02.2019, 11:41
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 14:51
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

output('Aufgabe 1','Weise folgenden Objekten den richtigen Datentyp zu:')
output('Beispiel 1','Durchmesser von Atomen')
output('Lösung Beispiel 1','Datentyp: float Beispielliteral: 0.000000000012')
output('Beispiel 2','Pflanzenname')
output('Lösung Beispiel 2','Datentyp: string Beispielliteral: "Sonnenblume"')
output('Beispiel 3','Namen der Teilnehmer eines Wettlaufs')
output('Lösung Beispiel 3','Datentyp: liste Beispielliteral: ["john","michael","jenny",...]')
output('Beispiel 4','Name, Vorname und Alter einer Person')
output('Lösung Beispiel 4','Datentyp: tupel Beispielliteral: ("john","doe",25)')
output('Beispiel 5','Name, Vorname und Alter von Teilnehmern eines Wettlaufs')
output('Lösung Beispiel 5','Datentyp: tupelliste Beispielliteral:[("john","doe",25),("jenny","doe",20)]')
output('Beispiel 6','Spielstand eines Fussballspiels. Beispiel: 0:1')
output('Lösung Beispiel 6','Datentyp: liste Beispielliteral: [0,1]')
output('Beispiel 7','Tabelle in der zu Elementensymbolen (z.b. H) die englischen und deutschen Namen der chemischen Elemente vermerkt sind.')
output('Lösung Beispiel 7','Datentyp: dictionary Beispielliteral:{"H":{"en":"hydrogen", "de":"wasserstoff"}}')

output('Aufgabe 2','Welche der folgenden Beispiele sind keine gültigen Pythoniterale?')
output('Beispiel 1','2e+1j')
output('Lösung Beispiel 1','Gültig')
output('Beispiel 2','1.000e-0.2')
output('Lösung Beispiel 2','Ungültig, da Exponent keine Ganzzahl ist')
output('Beispiel 3','0o237')
output('Lösung Beispiel 3','Gültige Oktalzahl')
output('Beispiel 4','0o238')
output('Lösung Beispiel 4','Ungültig, Oktalzahlen gehen von 0 bis 7')
output('Beispiel 5','0x123')
output('Lösung Beispiel 5','Gültige Hexadezimalzahl')
output('Beispiel 6','00x123')
output('Lösung Beispiel 6','Ungültige Hexadezimalzahl, diese müssen mit 0x beginnen.')
output('Beispiel 7','""zwei""')
output('Lösung Beispiel 7','Ungültig da "zwei" von zwei Leerstrings umgeben ist, selbst jedoch nicht Teil eines String ist.')
output('Beispiel 8','"Körpergrösse"')
output('Lösung Beispiel 8','Gültiger String')
output('Beispiel 9','b\'Körpergrösse\'')
output('Lösung Beispiel 9','Ungültig da nur ASCI-Zeichen in einem Bytestring verwendet werden dürfen, also keine Sonderzeichen wie ä,ö,ü,ect...')
output('Beispiel 10','00023e001')
output('Lösung Beispiel 10','Gültige wissenschaftliche Zahlenangabe, auch wenn man auf die Nullen verzichten könnte.')
output('Beispiel 11','(1;2;3)')
output('Lösung Beispiel 11','Ungültiger Ausdruck')

output('Aufgabe 3','Welche Ergebnisse liefern die Auswertung folgender Beispiele? Welche Ausdrücke sind Ungültig?')
output('Beispiel 1','10%7')
output('Lösung Beispiel 1','Die Modulo-Funktion liefert den Restwert einer Division. In diesem Beispiel ergibt dieser Ausdruck: 3')
output('Beispiel 2','1+1.0')
output('Lösung Beispiel 2','Da eine der Zahlen eine Fliesskommazahl ist wird auch das Resultat als float dargestellt: 2.0')
output('Beispiel 3','5//2')
output('Lösung Beispiel 3','Eine Ganzzahldivision gibt als Resultat immer ein Integer, also eine Ganzzahl aus: 2. Dabei wird nicht gerundet sondern einfach die Kommastellen entfernt.')
output('Beispiel 4','3*(1.0/2)')
output('Lösung Beispiel 4','Eine Division eribt automatisch eine Fliesskommazahl. Das Resultat lautet hier: 1.5')
output('Beispiel 5','int("10")*0o2')
output('Lösung Beispiel 5','Auch wenn kompliziert ausgedrückt, ist das eine valide Multiplikation. Resultat: 20')
output('Beispiel 6','int("10",2)*2')
output('Lösung Beispiel 6','Dies ist ein gültiger Ausdruck, die zweite Zahl beschreibt die Basis mit welcher die Zahl gewertet werden soll. 2 Bedeutet in diesem Fall Binär weswegen 10 als 2 ausgewertet wird weswegen das Endresultat 4 lautet.')
output('Beispiel 7','0x10+0x20')
output('Lösung Beispiel 7','Gültige addition von Hexadezimalwerten. Ausgewertet wird dies als: 16+32 Resultat: 48 bzw 0x30')
output('Beispiel 8','(1,2)+(3,4)')
output('Lösung Beispiel 8','Dies Konkateniert die beiden Tupel zu (1,2,3,4)')
output('Beispiel 9','[1,2]+(3,4)')
output('Lösung Beispiel 9','Ungültiger Ausdruck. Konkatenation kann nur zwischen identischen Datentypen stattfinden.')
output('Beispiel 10','[1,2]+[(3,4)]')
output('Lösung Beispiel 10','Diese Konkatenierung ist gültig da das zweite Objekt ebenfalls eine Liste ist welche einfach ein Tuple als Wert beinhaltet. Resultat: [1,2,(3,4)]')
output('Beispiel 11','"2"+"3"')
output('Lösung Beispiel 11','Stringkonkatenierung. Ergebnis: "23"')
output('Beispiel 12','(1,2)+(3)')
output('Lösung Beispiel 12','Dieser Ausdruck ist ungültig da die (3) als integer Interpretiert wird. Damit es als Tupel erkannt wird müsste man (3,) schreiben.')
output('Beispiel 13','float(1//2)')
output('Lösung Beispiel 13','Gültiger Ausdruck, das Resultat lautet: 0.0')
output('Beispiel 14','str(2+3.0)')
output('Lösung Beispiel 14','Gültiger Ausdruck, das Resultat lautet: "5.0"')
output('Beispiel 15','str(complex(1))')
output('Lösung Beispiel 15','Gültiger Ausdruck, das Resultat lautet: (1+0j)')
output('Beispiel 16','complex(2//3)')
output('Lösung Beispiel 16','Gültiger Ausdruck, das Resultat lautet: 0j')
output('Beispiel 17','int(complex(1))')
output('Lösung Beispiel 17','Ungültiger Ausdruck. Durch den nicht realen Anteil bei Komplexen ausdrücken kann der Wert nicht als Integer ausgegeben werden.')
output('Beispiel 18','list(str(123456))')
output('Lösung Beispiel 18','Gültiger Ausdruck. das Resultat lautet: ["1","2","3","4","5","6"]')
