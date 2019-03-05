#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_grundlagen.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 26.02.2019, 11:02
# Author: Apop85
# -----
# Last Modified: Tuesday 05.03.2019, 15:39
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Basic knowledge about sequences, quantities and generators. 
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

output('Definition Sequenzen','Sequenzen sind Objekte, die aus einer linearen Anordnung anderer Objekte bestehen, die man Items oder Elemente der Sequenz nennt.')
output('Sequenztypen','Python unterscheidet zwischen drei verschiedenen Sequenztypen: Liste, Tupel und String.')
output('Sequenzeigenschaften','Sequenzen besitzen besondere Eigenschaften welche sie von anderen Typen unterscheidet. Sie besitzen alle eine Länge, heisst alle Items sind durchnummeriert und in einer bestimmten Reihenfolge angeordnet.')
output('Sequenzoperationen','Mit Sequenzen lassen sich folgende Operationen durchführen:')
output('Sequenzprüfung','"x in sequenz", "x not in sequenz" um zu Prüfen ob das Objekt x in einer Sequenz vorkommt oder nicht.')
output('Sequenzkonkatination und Multiplikation','"sequenz+sequenz", "sequenz*integer" um Squenzen zu verbinden oder zu vervielfältigen.')
output('Zugriff auf Elemente in einer Sequenz','"sequenz[integer]" um auf ein bestimmtes Element einer Sequenz zuzugreiffen oder "sequenz[integer:]", "sequenz[:integer]", "sequenz[integer:integer+n]" um ein Slice einer Sequenz zu erhalten.')
output('Sequenzfunktionen','"len(sequenz)" um die Länge der Sequenz herauszufinden, "min(sequenz)" und "max(sequenz)" um den kleinsten bzw. grössten Eintrag in der Sequenz zu finden.')
output('Sequenzunpacking','Wenn man die Länge einer Sequenz kennt kann man sie "auspacken", heisst alle Elemente einer Sequenz werden in einer einzigen Zuweisung Namen zugeordnet. Beispiel: liste=["hallo","du"] | a,b=liste somit ist a="hallo" und b="du". Bei unbekannter Listenlänge kann man auch a,*b=liste verwenden. Somit wird das erste Element zu a zugewiesen und die restlichen Elemente zu b.')
output('Tupel','Tupel sind unveränderbare Sequenzen. Man verwendet Tupel dann wenn man ein zusammenhängendes Objekt, das aus mehreren Elementen besteht, repräsentieren möchte. Beispiel Vor- und Nachname einer Person oder X- und Y-Koordinate eines Objekts.')
output('Listen','Im Gegensatz zu Tupeln sind Listen veränderbar. Sie können Objekte beliebigen Typs, auch bunt gemischt, enthalten. Mithilfe von Listen kann man komplexe Strukturen datentechnisch modellieren, von einfachen linearen Anordnungen wie Telefonlisten bis zu virtuellen Welten, wie etwa einem Gebäude aus Gängen und Räumen die über Türen und Treppen miteinander verbunden sind.')
output('Listenfunktionen','Listen verfügen durch ihre veränderbarkeit zusätzliche Operationen bzw Methoden um diese zu verändern.')
output('Listen: Items ersetzen','Mittels den Operationen liste[n]=x wird dem n-ten Element der Liste der Wert x zugewiesen. Mittels liste[n-p]=liste2 werden den Elementen n bis p mit den Einträgen aus liste2 ersetzt.')
output('Listen: Items hinzufügen','Durch liste.append(x) wird die Liste um das Item x an letzter Stelle ergänzt. Mit liste.extend(liste2) kann die Liste um den Inhalt einer anderen Liste ergänzt werden. Mittels liste.insert(n, x) wird das Objekt x an n-ter Stelle in der Liste eingefügt.')
output('Listen: Items entfernen','Mittels del liste[n] wird das n-te Element der Liste entfernt. Durch del liste[n-p] werden die Elemente n bis p entfernt. Mit der Funktion liste.pop() wird das letzte Element der Liste entfernt und der Wert davon wird zurückgegeben. Um ein Item mit einem bestimmten Wert aus der Liste zu entfernen verwendet man liste.remove(x), in diesem Beispiel um das erste Element welches gleich x ist zu entfernen.')
output('Listen: Items suchen','Um herauszufenden an welcher Stelle der Liste das Item x sich befindet verwendet man liste.index(x). Um herauszufinden wie oft das Element x in der Liste vorkommt verwendet man liste.count(x).')
output('Listen: Reihenfolge ändern','Um die Reihenfolge einer Liste zu verändern kann man entweder liste.reverse() um die Liste umzudrehen oder liste.sort() um die Liste aufsteigend zu Sortieren verwenden. Man kann bei sort() auch einen Suchschlüssel wie z.b. liste.sort(key=len) übergeben, in diesem Beispiel um den Inhalt der Liste anhand der Länge der Items zu sortieren. Als zusätzliches Argument kann noch reverse=True übergeben werden um die Reihenfolge der Bedingung umzukehren.')
output('Listen: Kopieren tief/flach','Man kann Listen auch Kopieren wobei liste2=liste1 keine Kopie darstellt, dieser Ausdruck bedeutet dass der name liste2 auf die liste1 verweist, jedoch keine eigene Liste ist. Möchte man eine Liste kopieren in welcher sich Items noch ändern können erstellt man eine flache Kopie der Liste mittels liste2=liste1[:]. Somit wird eine neue Liste mit dem Namen liste2 erstellt mit den Verweisen auf die Inhalte von liste1. Möchte man eine eigentständige Kopie in welcher die Werte von liste1 enthalten sind verwendet man deepcopy() aus dem Modul copy.')
output('Listen per Ausdruck generieren','Listen können auch per Ausdruck generiert werden. Beispiel: liste=[i**2 for i in range(5)] oder liste=[i**2 for i in range(100) if i%7==0] um die Potenz aller durch Sieben teilbaren Zahlen bis 100 zu erhalten. Um zwei Listen zu vergleichen und die Gemeinsamen Inhalte auszugeben kann man liste=[i for i in liste1 if i in liste2. Um aus zwei Listen Tupel zu generieren mit kann man folgendes verwenden: liste=[(i,j) for i in liste1 for j in liste2]')


output('Definition Generatoren','Wärend in Sequenzen und Mengen Daten explizit gespeichert werden, sind Generatoren virtuelle Kollektionen, deren Inhalt erst bei Bedarf erzeugt werden.')
output('Generatoren-Formel','Um einen einfachen Generator zu verwenden verwendet man Formeln. Beispielsweise um alle quadratzahlen von 1 bis 10 aufzulisten n_square=(i*i for i in range(10))')
output('Generatoren Auslesen','Generatoren kann man nicht mittels print direkt ausgeben. Die einzelnen Items darin können jedoch mit einem For-Loop ausgelesen werden.')
output('Generatorfunktionen','Generatorfunktionen sind Funktionen in welchen das Statement yield vorkommt.')
output('yield','Mit yield <Rückgabewert> kann man einen Generator unterbrechen und den aktuellen Wert ausgeben.')
output('next()','Mit der Funktion next(generator_item) lässt sich der nächste Wert mit dem Generator generieren und ausgeben.')
output('Iteratoren','Iteratoren sind spezielle Generatoren die den Zugriff auf Elemente einer Kollektion kontrollieren.')
output('iter()','Die Funktion iter() nimmt eine Sequenz entgegen und gibt nach und nach das nächste Element der Sequenz aus.')
output('min() / max()','Wird die Funktion min() oder max() auf einen Generator angewendet so besitzt dieser im nachhinein keine Objekte mehr da alle abgearbeitet wurden.')
output('Verwendung Generatoren','Generatoren verwenden deutlich weniger Speicher als statische Listen. Oft werden sie auch verwendet um Mengen zu definieren.')


output('Definition Mengen','Mengen sind ungeordnete Datenkolletktionen mit einmalig vorkommenden Elementen.')
output('Mengentypen','Zur Darstellung von endlichen Mengen gibt es in Python die Typen "set" und "frozenset".')
output('set()','Objekte vom Typ set() sind wie Listen, die Inhalte sind also veränderbar. Alternativ zu set() kann man die Liste in geschweiften Klammern schreiben. Beispiel: a={1,2,3,4,5}')
output('frozenset()','Objekte vom Typ frozenset() sind wie Tupel oder Strings, also unveränderbar.')
output('Besonderheiten von Mengen','Wird eine Liste mit mehreren identischen Einträgen an set() oder frozenset() übergeben so werden die mehrfach vorkommenden Einträge entfernt. Auch dürfen nur unveränderbare Elemente in einer Menge vorkommen, heisst Tupel, Strings, frozenset-Objekte und Zahlen, keine Variabeln.')
output('menge_1 | menge_2','Mit dem Operator "|" können zwei Mengen vereinigt werden.')
output('menge_1 & menge_2','Mit dem Operator "&" wird der Durchschnitt der beiden Mengen ausgegeben.')
output('menge_1 - menge_2','Mit dem Operator "-" wird die Differenz der beiden Mengen ausgegeben.')
output('menge_1 <= menge_2','Mit dem Operator "<=" kann geprüft werden ob menge_1 eine Teilmenge von menge_2 ist.')
output('menge_1 >= menge_2','Mit dem Operator ">=" kann geprüfte werden ob menge_2 eine Teilmenge von menge_1 ist.')
output('Iterierbarkeit von Mengen','Mengen sind ebenfalls iterierbar weswegen sie ebenfalls mittels einem For-Loop ausgelesen werden können.')
