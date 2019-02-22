#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_datentypen.py
# Project: Kapitel_4_Standarddatentypen
# Created Date: Friday 22.02.2019, 20:25
# Author: Apop85
# -----
# Last Modified: Friday 22.02.2019, 22:06
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

output('Zahlen','Zu den Zahlen gehören Integer, also Ganzzahlen (1,2,3...), Float, also Fliesskommazahlen (1.123, 4.45...) und complex (1+2j).')
output('Integertypen','Es gibt noch einige Subtypen von Integern wie folgende: decimalinteger, nonzerodigit, digit, octinteger, hexinteger, bininteger, octdigit, hexdigit und bindigit')
output('Dezimalzahlen','Dezimalzahlen können ohne prefix als Wert übergeben werden. Z.b. a=10 oder print(15)')
output('Oktalzahlen','Oktalzahlen bestehen aus den Zahlen 0-7 und werden mit 0o als Prefix geschrieben. Beispielsweise oktal=0o2325 oder print(0o2325)')
output('Hexadezimalzahlen','Hexadezimalzahlen bestehen aus den Zahlen 0-9 und den Buchstaben A-F und werden mit dem Prefix 0x übergeben. Beispielsweise hex=0xABCD oder print(0xABCD).')
output('Binärzahlen','Binärzahlen bestehen aus 0 und 1 und werden mit dem Prefix 0b definiert. Beispielsweise bin=0b11010 oder print(0b11010)')
output('Fliesskommazahlen','Fliesskommazahlen sind Zahlen mit Kommastellen also rationale Zahlen. Für Rationale zahlen die sehr nahe bei Null oder sehr Lang sind verwendet man die exponentielle Schreibweise. Beispielsweise 5e-10 oder 5e10')
output('Komplexe Zahlen','Komplexe Zahlen bestehen einem realen Part (Ganzzahl oder Fliesskommazahl) und einem immaginären Part (j oder J)')

output('Kollektionen','Kollektionen werden in drei Unterkategorien unterteilt. Sequenzen, Mengen und Abbildungen.')

output('Sequenzen','Sequenzen werden wiederum in veränderbar und unveränderbar unterteilt. Zu den Unveränderbaren gehören: String, Bytestring und Tuple. Zu den Veränderbaren gehören: Listen und Bytearrays.')
output('Eigenschaften von Sequenzen','Was jedoch alle Sequenzen gemeinsam haben ist dass sie einen Index für die enthaltenen Werte besitzt. Wobei das erste Listenelement immer dem Index 0 entspricht. Somit ist das Letzte Item mit n-1 aufrufbar.')

output('Mengen','Als Mengen bezeichnet man eine untergeordnete Kollektion von Objekten, wobei jedes Objekt nur einmal vorkommen darf. Dazu gehören set und frozenset')
output('Abbildungen','Die einzige Datentyp von Python der als Abbildung bezeichnet wird ist das Dictionary.')

output('Boolean-Werte','Ein Boolean-Wert kann nur entweder True oder False beinhalten. Leere Dictionarys, Listen, die Zahl Null und unwahre logische Aussagen besitzen den Wahrheitswert False. Alle nicht leeren Sequenzen oder Dictionarys, wahre logische Aussagen und alle Zahlen ungleich Null besitzen den Wahrheitswert True.')

output('NoneType','Der NoneType besitzt im Gegensatz zu den Anderen Datentypen keinen Wert. Er ist und bleibt None und gehört zu den literalen Datentypten.')

output('Unterschied Typ - Klasse','Der Datentyp bezeichnet nur wie das Objekt verarbeitet werden soll. Die Klasse ist ein Bauplan für gleichartige Objekte. In der Klassendefinition wird festgelegt welche Attribute und Methoden die Objekte der Klasse besitzen.')
