#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_datentypen.py
# Project: Kapitel_04_Standarddatentypen
# Created Date: Friday 22.02.2019, 20:25
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
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-9)+r'}[ |.|,|\n|>|\W|\)|-]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Datentypen','Python kennt vier grundliegende Datentypen. Dazu gehören Zahlen, Kollektionen, Boolean-Werte und NoneType.')

print(' Zahlen '.upper().center(max_text_length+9,'█'))
output('Zahlen','Zu den Zahlen gehören Integer, also Ganzzahlen (1,2,3...), Float, also Fliesskommazahlen (1.123, 4.45...) und complex (1+2j).')
output('Integertypen','Es gibt noch einige Subtypen von Integern wie folgende: decimalinteger, nonzerodigit, digit, octinteger, hexinteger, bininteger, octdigit, hexdigit und bindigit')
output('Dezimalzahlen','Dezimalzahlen können ohne prefix als Wert übergeben werden. Z.b. a=10 oder print(15)')
output('Oktalzahlen','Oktalzahlen bestehen aus den Zahlen 0-7 und werden mit 0o als Prefix geschrieben. Beispielsweise oktal=0o2325 oder print(0o2325)')
output('Hexadezimalzahlen','Hexadezimalzahlen bestehen aus den Zahlen 0-9 und den Buchstaben A-F und werden mit dem Prefix 0x übergeben. Beispielsweise hex=0xABCD oder print(0xABCD).')
output('Binärzahlen','Binärzahlen bestehen aus 0 und 1 und werden mit dem Prefix 0b definiert. Beispielsweise bin=0b11010 oder print(0b11010)')
output('Fliesskommazahlen','Fliesskommazahlen sind Zahlen mit Kommastellen also rationale Zahlen. Für Rationale zahlen die sehr nahe bei Null oder sehr Lang sind verwendet man die exponentielle Schreibweise. Beispielsweise 5e-10 oder 5e10')
output('Komplexe Zahlen','Komplexe Zahlen bestehen einem realen Part (Ganzzahl oder Fliesskommazahl) und einem immaginären Part (j oder J)')

print(' Kollektionen '.upper().center(max_text_length+10,'█'))
output('Kollektionen','Kollektionen werden in drei Unterkategorien unterteilt. Sequenzen, Mengen und Abbildungen.')
output('Sequenzen','Sequenzen werden wiederum in veränderbar und unveränderbar unterteilt. Zu den Unveränderbaren gehören: String, Bytestring und Tuple. Zu den Veränderbaren gehören: Listen und Bytearrays.')
output('Eigenschaften von Sequenzen','Was jedoch alle Sequenzen gemeinsam haben ist dass sie einen Index für die enthaltenen Werte besitzt. Wobei das erste Listenelement immer dem Index 0 entspricht. Somit ist das Letzte Item mit n-1 aufrufbar.')
output('Sequenzen im Alltag','Sequenzen kennt man auch aus dem Alltag. Beispielsweise kann man ein Haus aus Sequenz aus Stockwerken betrachten. Auch das Periodensystem kann man als Sequenz erachten und die DNA des Menschen ist auch eine aus Basenpaaren bestehende Sequenz.')
output('Unicode Zeichen','Um Unicode-Zeichen darzustellen verwendet man \\N{UNICODE_NAME}. Beispiel: \N{CYRILLIC CAPITAL LETTER ZHE}. Auch möglich ist dies mit \\u + 4 stellige Unicodeidentifikationsnummer. Beispiel: \u0416')
output('Bytestrings','Bytestrings sind Folgen aus Zahlen zwischen 0 und 255. Sie können mit bytes([]) definiert werden. Beispiel bytes([104]) ergibt: '+str(bytes([104])))
output('Tupel','Während man Listen zum aufzählen mehrerer Objekte verwendet, verwendet man Tupels um ein einzelnes komplexes Objekt zu beschreiben. Typische Beispiele für Tupels sind (Name,Geburtstag), Beschreibung eines Punktes in einem 3D Koordinatensystem (x,y,z) als Tripel, Anschriften (Strasse,Hausnummer,PLZ,Stadt,Land) oder die Beschreibung einer Farbe als RGB-Wert (150,150,150)')
output('Tupel Inhalte','Die Inhalte eines Tupels können unterschiedliche Datentypen aufweisen. Beispiel: tupel=(12,"hallo",(9.81, 1.01)). Die Inhalte eines Tupels können im nachhinein nicht mehr geändert werden.')
output('Listen','Im gegensatz zu Tupeln besitzen Listen die Eigenschaft dass man bei diesen Inhalte ändern kann. Auch Listen können unterschiedliche Datentypen beinhalten. Typische Listen sind: Bestsellerliste, Top-10-Charts, Abonnentenliste einer Zeitung')
output('Bytearray','Ein Bytearray ist eine Liste aus Bytes. Beispiel: bytearray([104, 97, 108, 108, 111]) ergibt: '+str(bytearray([104, 97, 108, 108, 111])))
output('Konkatenation','Objekte des selben Datentyps können konkatiniert bzw. zusammengefügt werden.')
output('Mengen','Als Mengen bezeichnet man eine untergeordnete Kollektion von Objekten, wobei jedes Objekt nur einmal vorkommen darf. Um eine Menge zu erzeugen verwendet man set() und frozenset().')
output('set()','An set() kann man beispielsweise eine Liste oder ein String übergeben. Set() entfernt dann alle doppelten Einträge und erstellt daraus eine Menge. Diese Funktion wird verwendet um in der Mathematik übliche Operationen auszuführen wie Vereinigung, Durchschnitt und Differenz. Beispiel: set("abba") ergibt: '+str(set('abba'))+'.')
output('set() und frozenset()','Mengen haben keine Indexe können jedoch trozdem iteriert werden. frozenset() erzeugt das selbe Ergebnis wie set(), der Inhalt kann jedoch im nachhinein nicht mehr verändert werden.')
output('Mengendurchschnitt','Mit "menge1 & menge2" lässt sich der Durchschnitt der beiden Mengen anzeigen. Beispiel menge1="Einstein" und menge2="Relativitaet" ergibt den Durchschnitt: '+str(set('Einstein')&set('Relativitaet')))
output('Mengenvereinigung','Mit "menge1 | menge2" lassen sich zwe Mengen zu einer Vereinigen: Beispiel wieder mit Einstein und Relativitaet: '+str(set('Einstein')|set('Relativitaet')))
output('Mengendifferenz','Mit "menge1 - menge2" lässt sich die Differenz der beiden Mengen anzeigen. Beispiel wieder mit Einstein und Relativitaet: '+str(set('Einstein')-set('Relativitaet')))
output('Abbildungen','Die einzige Datentyp von Python der als Abbildung bezeichnet wird ist das Dictionary.')

print(' Boolean '.upper().center(max_text_length+10,'█'))
output('Boolean-Werte','Ein Boolean-Wert kann nur entweder True oder False beinhalten. Leere Dictionarys, Listen, die Zahl Null und unwahre logische Aussagen besitzen den Wahrheitswert False. Alle nicht leeren Sequenzen oder Dictionarys, wahre logische Aussagen und alle Zahlen ungleich Null besitzen den Wahrheitswert True. Man kann mit bool() auch ein beliebiges Objekt auf seinen Wahrheitswert prüfen.')

print(' NoneType '.upper().center(max_text_length+10,'█'))
output('NoneType','Der NoneType besitzt im Gegensatz zu den Anderen Datentypen keinen Wert. Er ist und bleibt None und gehört zu den literalen Datentypten.')

print(' '. center(max_text_length+10,'█'))
output('Unterschied: Typ - Klasse','Der Datentyp bezeichnet nur wie das Objekt verarbeitet werden soll. Die Klasse ist ein Bauplan für gleichartige Objekte. In der Klassendefinition wird festgelegt welche Attribute und Methoden die Objekte der Klasse besitzen.')
