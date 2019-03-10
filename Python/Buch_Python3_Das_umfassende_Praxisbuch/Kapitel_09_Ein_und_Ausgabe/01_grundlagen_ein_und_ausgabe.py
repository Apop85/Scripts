#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_grundlagen_ein_und_ausgabe.py
# Project: Kapitel_09_Ein_und_Ausgabe
# Created Date: Sunday 10.03.2019, 12:40
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 14:44
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Basics of i/o
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

output('Fileobjekt','Um ein Fileobjekt zu erstellen verwendet man die Funktion open(file_pfad, lesemodus, encoding) welche File-Objekt erstellt.')
output('Lese/Schreibmodus','Es gibt verschiedene modis welche der open()-Funktion übergeben werden können: "w" um zu (über-)schreiben, "wb" identisch mit "w" nimmt jedoch nur binäre Strings entgegen, "r" um ein File auszulesen, "rb" um das File im binären Modus auszulesen, "a" um Inhalte in einem bestehenden File anzuhängen.')
output('Speichern/Schliessen','Um die Änderungen an einem Fileobjekt zu speichern verwendet man die Methode .close().')
output('Zwischenspeichern','Möchte man den Inhalt einer Datei zwischenspeichern verwendet man die Methode .flush()')
output('Absolute und relative Pfade','Absolute Pfade beinhalten den kompletten Pfad zu einer Datei (z.B. c:\\users\\user\\Desktop\\file.txt) während relative Pfade nur eine Art Wegbeschreibung vom aktuellen Pfad zum Zielpfad darstellen (z.B. ..\\..\\bin\\file.txt um zwei Ordner zurück und da das File im Ordner bin zu öffnen).')

output('Fileattribute und -Methoden','Folgende Fileattribute und -Methoden sind verfügbar...')
output('file.close()','Speichert und schliesst das aktuelle File-Objekt in die Zieldatei.')
output('.closed','Gibt den Wert True zurück wenn das File geschlossen ist.')
output('.flush()','Zwischenspeichern des aktuellen File-Objekts ohne es zu schliessen.')
output('.mode','Gibt den Modus zurück mit welchem das File geöffnet wurde.')
output('.read()','Liest den Inhalt des Files aus.')
output('.readline()','Liest die nächste Zeile des Files aus.')
output('.readlines()','Liest das File unterteilt durch Zeilenumbrüche als Liste aus.')
output('.seek(integer)','Lässt den Cursor an eine bestimmte Stelle des Files springen.')
output('.tell()','Rückgabe der aktuellen Cursorposition.')
output('.write(string)','Der angegebene String wird in das Fileobjekt geschrieben.')

output('Try/Except-Anweisungen','Das Problem bei Dateien ist immer dass man nie genau weiss ob man auf die gewünschte Datei zugreiffen kann daher verwendet man bei Dateioperationen immer die Try/Except Anweisung um das Programm am Absturz durch einen fehlerhaften Dateizugriff zu hindern.')
output('Try/Finally-Anweisungen','Der Unterschied zwischen Except und Finally ist dass der Code im Block finally in jedem Fall ausgeführt wird, mit und ohne Fehler im Try-Block.')
output('With-Anweisungen','Die Idee einer With-Anweisung ist grob gesagt dass gewisse Operationen immer zu einem Ende kommen müssen, auch wenn ein Fehler auftritt, was die With-Funktion sicherstellen soll. Der Aufbau einer With-Anweisung sieht inetwa so aus: with object_name as var_name: <anweisungsblock>. Beispiel: with open(file_name, "r") as file_content: print(file_content.readline())')

output('Modul Pickle','Das Modul Pickle (aus dem englischen für Einmachen/Einlegen) kann verwendet werden um beliebige Datentypen auf der Festplatte zu speichern und wieder zu laden.')
output('pickle.dump(content, target)','Die Funktion .dump() nimmt den zu speichernden Inhalt sowie das entsprechende File-Objekt entgegen und speichert den Inhalt als binary string im File. Das File sollte demnach im Modus "wb" geöffnet sein.')
output('pickle.dumps(content)','Man kann den Bytestring mittels pickle.dumps() auch auslesen ohne diesen in eine Datei zu speichern.')
output('pickle.load(target)','Mittels der Funktion pickle.load() kann der Pickle-Inhalt aus einem File-Objekt ausgelesen sein. Das File muss jedoch im Modus "rb" geöffnet sein.')

output('Pseudofiles sys.stdin und sys.stdout','Wenn eine Eingabe über die Input-Methode abgefragt wird, schreibt Python den Inhalt in sogenannte Pseudofiles, sys.stdin für die Eingabe und sys.stdout für die Ausgabe.')
output('sys.stdin()','sys.stdin() besitzt keine Schreibrechte sondern verwendet nur die Funktion readline() um den Inhalt der Eingabe zu erfassen.')
output('sys.stdout()','sys.stdout() verhält sich wie ein File welches nur Schreib- aber keine Leserechte besitzt. Alles was in diese Datei geschrieben wird, wird von der Laufzeitumgebung automatisch auf den Bildschirm ausgegeben.')

output('print()','Der Funktion Print können alle möglichen Datentypen als Argument übergeben werden, diese Objekte besitzen eine Methode namens __str__() welche den Inhalt des Objekts als String zurück gibt.')
output('print() Schlüsselwortargumente','Der Funktion Print können auch noch Schlüsselwortargumente übergeben werden welche die Ausgabeform von print() bestimmen.')
output('print(x,y,z,sep=" ")','Das Schlüsselwortargument sep=" " bestimmt wie die unterschiedlichen Elemente auf dem Bildschirm voneinander getrennt sein sollen. Default ist sep=" ", also ein Leerzeichen zwischen den Elementen.')
output('print(x,end="\\n")','Das Schlüsselwortargument end="\\n" bestimmt wie die Print-Ausgabe beendet werden soll. Default ist end="\\n", also eine neue Zeile.')
output('Bedingte Printanweisung','Man kann innerhalb der Printfunktion auch Anweisungen verwenden um die Ausgabe den Bedingungen anzupassen. Beispiel: for item in liste: print(item, end=" + " if item != liste[-1] else "\\n") Diese Anweisung druckt alle Items einer Liste auf eine Line getrennt durch " + " ausser beim letzten Item wo ein "\\n" verwendet wird.')

output('format(string,darstellung)','Mit der Funktion format() kann man die entsprechende Formatierung der Ausgabe definieren. Beispiel: format(float_zahl,"8.3f") In diesem Beispil wird eine Fliesskommazahl entgegengenommen. Das Argument "8.3f" wird folgendermassen interpretiert: 8 Zeichen sollen für die Ausgabe reserviert werden, die Zahl soll 3 Kommastellen aufweisen und f definiert dieses als Fliesskommazahl.')

output('Kommandozeilenargumente','Kommandozeilenargumente sind Argumente die über die Kommandozeile an das Pythonscript übergeben werden. Um die Kommandozeilenargumente auszulesen muss man das Modul sys importieren. Das Erste Argument wird immer der Dateipfad des Scripts sein.')
