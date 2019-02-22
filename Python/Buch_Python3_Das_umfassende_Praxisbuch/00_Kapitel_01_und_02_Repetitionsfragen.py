#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 00_Kapitel_01_bis_xx_Repetitionsfragen.py
# Project: Buch_Python3_Das_umfassende_Praxisbuch
# Created Date: Thursday 21.02.2019, 20:52
# Author: Apop85
# -----
# Last Modified: Friday 22.02.2019, 16:09
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
###

import re

max_text_length=70
max_text_delta=24

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Kapitel 1','Grundlagen')

output('Aufgabe 1','Welches der folgenden Beispiele sind Algorithmen? Liebesbrief, Formular zur beantragung eines Personalausweises, Märchen, Musterlösung bei Mathematikaufgabe, die christlichen 10 Gebote')
output('Antwort','Forumular zur beantragung eines Personalausweises sowie die Musterlösung einer Mathematikaufgabe sind beides Algorithman die mit einem definierten Ablauf zu einem bestimmten Ziel führen.')

output('Aufgabe 2','Ordnen sie folgende Beschreibungen von Problemlösungen den Programmierparadigmen imperativ, objektorientiert und deklarativ zu:')
output('1. Beschreibung','Um ein Zündholz zu entzünden, reiben sie den Kopf des Zündholzes über die Reibfläche.')
output('2. Beschreibung','Um eine Menge von Blumenvasen der grösse nach zu sortieren, sorgen sie davor, entweder an ganz am Anfang der Reihe steht oder die sie grösser als ihr linker Nachbar ist.')
output('3. Beschreibung','Der Betrieb in einem Restaurant funktioniert so: Es gibt einen Koch und einen Kellner. Der Kellner kümmert sich um die Gäste, säubert die Tische, bringt das Essen und Kassiert. Der Koch bereitet das Gericht zu, sobald der Kelner ihm einen Zettel mit der entsprechenden Aufgabe übergibt.')
output('Antworten','1. Imperativ | 2. Deklarativ | 3. Objektorientiert')

output('Kapitel 2','Der Einstieg')

output('Aufgabe 1','Welche Ausgaben liefern die folgenden Anweisungen?')
output('Anweisung 1','type(id("a"))')
output('Lösung Anweisung 1', 'Der Typ einer ID ist immer ein "Integer", also eine Ganzzahl.')
output('Anweisung 2','type(3/2)')
output('Lösung Anweisung 2', 'Da das Resultat der Rechnung 3/2 eine Fliesskommazahl ist ist der Typ dieses Objekts "Float"')
output('Anweisung 3','type(2.0/2)')
output('Lösung Anweisung 3', 'Da die erste Zahl in der Rechnung eine Fliesskommazahl ist wird auch das Resultat als "Float" ausgegeben.')
output('Anweisung 4','min("Abend", "Aal", "Ball")')
output('Lösung Anweisung 4', 'Die Funktion min() wird den kürzesten String in der Liste ausgeben, in diesem Beispiel wird das "Aal" sein. ')
output('Anweisung 5','-2 ** -3')
output('Lösung Anweisung 5', 'Dies wird als Rechnung "-2 hoch -3" betrachtet was 0.125 ergibt.')
output('Anweisung 6','not(2.0>2)')
output('Lösung Anweisung 6', 'Die Auswertung von (2.0>2) ist False, dies wird durch das not invertiert und daher zu "True"')
output('Anweisung 7','type(len("abc"))')
output('Lösung Anweisung 7', 'Die Funktion len gibt immer ein "Integer" als Resultat aus.')
output('Anweisung 8',' (1<2)+(1==1)')
output('Lösung Anweisung 8', 'Wenn man Boolean-Werte als Zahlen betrachtet so wird True zu 1 und False zu 0 daher wird in diesem Beispiel der Integer "2" ausgegeben.')
output('Anweisung 9','type(1+2<2)')
output('Lösung Anweisung 9', 'Überprüfungen geben immer einen "Boolean-Wert" aus, also True oder False.')

output('Aufgabe 2','Ergänzen sie die Variablen kontinuierlich anhand der vorgegebenen Eingaben: x=2, y=1, z=""')
output('Anweisung 1','z=x')
output('Lösung Anweisung 1','x=2, y=1, z=2')
output('Anweisung 2','z*=3')
output('Lösung Anweisung 2','x=2, y=1, z=6')
output('Anweisung 3','x,y=y,3')
output('Lösung Anweisung 3','x=1, y=3, z=6')
output('Anweisung 4','y=y/2')
output('Lösung Anweisung 4','x=1, y=1.5, z=6')
output('Anweisung 5','x,y,z=x,x,x')
output('Lösung Anweisung 5','x=1, y=1, z=1')
output('Anweisung 6','x = "y"')
output('Lösung Anweisung 6','x="y", y=1, z=1')
output('Anweisung 7','y=2>z')
output('Lösung Anweisung 7','x="y", y=True, z=1')
output('Anweisung 8','y=min(2,z,5)')
output('Lösung Anweisung 8','x="y", y=1, z=1')

output('Aufgabe 3','Nennen sie zu den folgenden umgangssprachlichen Beschreibungen passende Python-Anweisungen.')
output('Beschreibung 1','Das Objekt "Elena" erhält den namen person')
output('Lösung Beschreibung 1','person = "Elena"')
output('Beschreibung 2','Der Variablen "zahl" wird der Wert 10 zugewiesen')
output('Lösung Beschreibung 2','zahl=10')
output('Beschreibung 3','Der inhalt der Variabel zahl wird um 5 erhöht')
output('Lösung Beschreibung 3','zahl+=5')
output('Beschreibung 4','Der Inhalt der Variabel zahl wird auf den Bildschirm ausgegeben.')
output('Lösung Beschreibung 4','print(zahl)')
output('Beschreibung 5','Der Inhalt der Variabel x wird mit dem Inhalt der Variabel y multipliziert und das Resultat in der Variabel mit dem Namen produkt zugewiesen.')
output('Lösung Beschreibung 5','produkt=x*y')
output('Beschreibung 6','Dem Objekt mit dem Wert ["rot", "gelb", "grün"] wird der name s zugeordnet. An das objekt s wird die Botschaft geschickt, es möge die Reihenfolge der Listenelemente umkehren. Anschliessend soll der Wert ausgegeben werden.')
output('Lösung Beschreibung 6','print(s.reverse())')

