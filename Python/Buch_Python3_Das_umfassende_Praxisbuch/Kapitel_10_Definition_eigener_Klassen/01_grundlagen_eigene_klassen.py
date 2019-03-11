#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_grundlagen_eigene_klassen.py
# Project: Kapitel_10_Definition_eigener_Klassen
# Created Date: Sunday 10.03.2019, 16:20
# Author: Apop85
# -----
# Last Modified: Monday 11.03.2019, 16:44
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description:
###

import re

def output(title, string, max_length=80, max_delta=20):
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

output('Klassen','Eine Klasse ist ein Bauplan für Objekte. In ihr werden der prinzipielle Aufbau und das Verhalten einer Menge von Objekten beschrieben. Ein Objekt wiederum gehört immer zu einer bestimmten Klasse. Man sagt auch, ein Objekt ist eine Instanz oder Inkarnation einer Klasse.')
output('Klassenbeispiel','Ein Beispiel für eine Klasse die wir in der realen Welt täglich verwenden ist "Geld". Ein Geldobjekt kann somit ein Geldschein, eine Münze aber auch die Kosten für einen Mietwagen oder der Wert einer Aktie sein. All diese Objekte haben gewisse gemeinsame Merkmale, nämlich einen Betrag und eine Währungsangabe. Auf jedem Geldschein, Münze, Rechnung oder Aktie sind diese beiden Attribute vermerkt.')
output('Geld als Klasse','In einem objektorientierten Programm, in dem mit Geld unterschiedlicher Währungen operiert wird macht es Sinn, eine Klasse mit dem Namen Geld zu definieren und festzulegen dass jedes Objekt dieser Klasse die Attribute Betrag und Währung besitzen soll.')
output('Attribute','Ein Attribut besteht aus einem Attributnamen und einem Attributwert. Man sagt auch: Ein Attribut ist mit einem Wert belegt. Alle Objekte einer Klasse besitzen die gleichen Attributnamen, aber möglicherweise andere Attributwerte.')
output('Geldobjekt Beispiel','Bei einem Geld-Objekt, das einen 10-Euro-Schein repräsentiert, hat das Attribut mit dem Namen Betrag den Wert "10.0" und das Attribut Währung den Wert "Euro". Bei einem Objekt, dass eine 1-Pfund Briefmarke repräsentiert wären die Attribute "1.0" und "Pfund".')
output('Klassenattribute','Es gibt auch Attribute, deren Wertbelegung für alle Instanzen einer Klasse gleich ist. Man bezeichnet diese auch als Klassenattribute. Im Falle des Geldes kann man den aktuellen Wechselkurs als Klassenattribut betrachten. Der Wert eines Gelscheins hängt prinzipiell vom Wechselkurs ab. Der Wechselkurs ist also nicht ein Merkmal eines einzelnen Geld-Objektes, sondern eine Eigenschaft der gesammten Klasse Geld.')
output('Objektoperationen (Methoden)','Objekte können Operationen ausführen, die als Methoden bezeichnet werden. In der Klassendefinition werden neben den Attributen auch Methoden definiert, über die die Objekte der Klasse verfügen können. An ein Objekt kann eine Botschaft geschickt werden, die es veranlasst, eine bestimmte Methode auszuführen.')

output('Klassendefinition','Um eine Klasse zu definieren verwendet man folgenden Syntax: class MeineKlasse: oder class MeineKlasse(Oberklasse1, Oberklasse2). Unterteilt wird die Klasse in folgende Abschnitte: Kopf: Klassenname und Körper: Attribute und Operationsliste/Methoden.')
output('Aufbau einer Klasse','Der Körper einer Klassendefinition beinhaltet in der Praxis folgende Komponenten: 1. Definition der Klassenelemente welche für alle Objekte der Klasse gelten (Syntax: klassenattribut = wert). 2. Definition der Konstruktormethode __init__(). Sie wird aufgerufen, wenn ein Objekt der Klasse instanziert wird. In der Konstruktormethode werden Objektattribute mit einem Anfangswert belegt. 3. Definition weiterer Methoden.')

output('Zugriff auf Attribute','Attribute beschreiben die Merkmale eines Objektes. Objektattribute bezeicnen sich auf Eigenschaften individueller Objekte und werden bei Python durch Zuweisung der Form self.attribut = wert innerhalb des Konstruktors __init__() erzeugt. Klassenattribute - gelegentlich auch Klassenvariablen genannt - sind dagegen Merkmale, die alle Objekte einer Klasse besitzen.')
output('Zugriff auf Attribute','Sie sind von der Existenz eines Objektes der Klasse unabhängig und werden durch eine Zuweisung der Form attribut = wert innerhalb der Klassendeklaration (aber ausserhalb einer Methode) definiert. Objektattribute dagegen werden erst bei der Instanzierung eines Objektes mit (individuellen) Werten belegt. Der Zugriff auf Klassen- und Objektattribute kann eingeschränkt werden. Man spricht hier auch von Sichtbarkeit und unterscheidet zwischen öffentlichen und privaten Attributen.')
output('Öffentliche Attribute','Auf öffentliche Attribute kann man "von aussen" lesend und schreibend zugreifen, indem man hinter den Namen eines Objektes (Instanz) einen Punkt und dann den Namen des Attributs schreibt: objekt.attribut. Beispiel: summe.betrag oder summe.waehrung')
output('Öffentliche Attribute','Analog kann auf ein öffentliches Klassenattribut zugegriffen werden. Man schreibt hinter den Namen der Klasse einen Punkt und dann den Namen des Attributs: Klasse.attribut. Beispiel: Geld.wechselkurs.')
output('Private Attribute','Um Attribute vor öffentlichem Zugriff zu schützenm kann man ihnen einen Namen geben, der mit ein oder zwei Unterstrichen beginnt. Ein solcherart abgeschirmtes Attribut nennt man in der objektorientierten Programmierung "privat". Python unterscheidet zwischen schwacher und starker Privatheit.')
output('Starke Privatheit','Stark private Attribute haben Namen, die mit Zwei Unterstrichen beginnen und nicht mit Unterstrichen enden. Beispiel: __privat. Es ist nur möglich, innerhalb der Klassendefinition auf ein solches Attribut zuzugreifen.')
output('Schwache Privatheit','Schwach private Attribute beginnen mit einem einfachen Unterstrich. Beispiel: _privat. Diese Namen werden durch from ... import * nicht in den Namensraum importiert. Damit wird das Risiko von Namenskollisionen verringert. Man kann aber auch problemlos auf das Attribut zugreiffen, wenn man seinen Namen kennt.')
output('Eingeschränkter Schutz','Private Namen bieten keinen wirklichen Schutz vor einem Zugriff von aussen. Attribute, deren Namen mit doppeltem Unterstrich beginnen, werden eigentlich nur versteckt. Sie können ein streng privates Attribut sichtbar machen, in dem dem Attributnamen einen Unterstrich und den Namen der Klasse vorangeschrieben wird. Beispiel: print(objekt._classname__privat).')

output('Klasseneigenschaften (properties())','Python bietet eine Möglichkeit, Attribute so zu definieren, dass man auf sie von aussen scheinbar direkt zugreiffen kann, der Zugriff aber von speziellen, in der Klasse definierten Methoden kontrolliert wird. Dazu definiert man am Ende der Klassendefinition mithilfe der Funktion property() so genannte Properties. jede Property korrespondiert mit einem (privaten) Attribut, das nach aussen sichtbar sein soll.')
output('properties(lesemethode, schreibmethode)','Als Argumente werden die Namen der Zugriffsmethoden übergeben, zuerst die Methode zum Abfragen und dann die Methode zum ändern. Die Namen dieser Methoden sind beliebig aber üblicherweise beginnt die Lesemethode mit "get" und die Schreibmethode mit "set". Beispiel: betrag = property(get_betrag, set_betrag) oder waehrung = property(get_waehrung, set_waehrung). Wird der property-Funktion kein zweites Argument übergeben so wird keine Schreibmethode spezifiziert und der Versuch dieses Attribut zu beschreiben führt zu einem AttributError.')

output('Dynamische Erzeugung von Attributen','Python ermöglicht es dass während des Programmlaufs neue Attribute dynamisch erzeugt werden. Dieses Feature ist eine potenzielle Fehlerquelle für Pythonskripte. Wenn nämlich beim schreibenden Zugriff auf ein öffentliches Attribut eines Objektes der Attributname versehentlich falsch geschrieben wird, gibt es keine Fehlermeldung. Stattdessen wird einfach ein neues Attribut hinzugefügt.')

output('Klassenmethoden','Methoden werden im Prizip wie Funktionen definiert. Beispiel: def klassen_methode(self, arg1, arg2). Es gibt jedoch wesentliche Unterschiede zu Funktionen auf welche nachfolgend eingegangen wird.')
output('Unterschied 1: Methode vs Funktion','Methoden sind vom Wesen her keine selbstständigen Objekte, sondern integraler Bestandteil einer Klasse. Sie werden innerhalb einer Klassendeklaration definiert.')
output('Unterschied 2: Methode vs Funktion','In der Liste der formalen Parameter der Methodendefinition bezeichnet der erste Parameter immer die Instanz (self), also das aktuelle Objekt. Der Name des ersten Parameters ist beliebig aber üblicherweise verwendet man self.')
output('Unterschied 3: Methode vs Funktion','Eine Methode eines Objektes wird aufgerufen, indem man den Namen des Objektes angibt, gefolgt von einem Punkt (.), dem Namen der Methode und in Klammern der Parameterliste. Format: objekt.methode(arg1, arg2, ...). Dabei ist die Argumentliste des Aufrufs um eins kürzer als die Argumentliste der Methodendefinition. Das erste Argument self wird weggelassen.')
output('Private Methoden','Es gibt auch hier schwach- und starkprivate Methoden die identisch zu den Attributen definiert werden. Zwei Unterstriche für starke Privatheit und einen Unterstrich für schwache Privatheit. Beispiel: __stark_privat oder _schwach_privat. ')

output('Polymorphismus','Ein wichtiges Konzept der objektorientierten Programmierung ist der Polymorphismus (bzw. die Polymorphie). Dmit ist die Möglichkeit gemeint, den gleichen Namen für (mehr oder weniger) gleichartige Operationen zu verwenden, die auf Objekte unterschiedlicher Klassen angewendet werden. Man spricht auch vom Überladen (overloading) einer Operation.')
output('Beispiel "+"','Der Plusoperator kann auf Zahlen und auf Strings angewendet werden. Sind die Operanden Zahlen, bewirkt "+" eine Addition. Handelt es sich dagegen um Sequenzen werden die Operanden aneinandergehängt (konkatiniert). Der Plusoperator ist also überladen.')
output('Standardfunktionen überladen','In Python können Operatoren und Standardfunktionen überladen werden, indem man bei einer neuen Klasse bestimmte Methoden mit vorgegebenen Namen definiert. Diese reservierten Methodennamen beginnen und enden mit doppelten Unterstrichen.')
output('__add__','Wird die Methode __add__ in der Klasse definiert so wird der Plusoperator überladen. Beispiel: var1 + var2')
output('__contains__','Mit der Methode __contains__ kann der in-Operator überladen werden. Beispiel: variabel in objekt')
output('__del__','Mit der Methode __del__ wird definiert was bei einem Aufruf der del-Operation geschehen soll. Beispiel: del objekt')
output('__delitem__',' Mit der Methode __delitem__ wird das vorgehen bei der del-Operation bei Containerobjekten wie Dictionarys definiert. Beispiel: del objekt[key]')
output('__eq__','Mit der Definition von __eq__ kann der Gleichheitsoperator (==) definiert werden. Beispiel: objekt1 == objekt2')
output('__float__','Mit der Definition von __float__ kann die Standardfunktion float() überladen werden. Beispiel: float(objekt1)')
output('__ge__','Durch die Definition von __ge__ kann die Grösser-Gleich-Operation überladen werden. Beispiel: objekt1 >= objekt2')
output('__getitem__','Mit __getitem__ kann definiert werden was bei einem Containeraufruf geschehen soll. Beispiel: objekt[key]')
output('__gt__','Durch die Definition von __gt__ kann die Grösser-Als-Operation überladen werden. Beispiel: objekt1 > objekt2')
output('__iadd__','Mit der Definition von __iadd__ kann das Verhalten der "+="-Operation überladen werden. Beispiel: objekt+=1')
output('__idiv__','Überladung der "/="-Operation. Beispiel: objekt/=2')
output('__imul__','Überladung der "*="-Operation. Beispiel: objekt*=2')
output('__le__','Mit der Definition von __le__ kann die Kleiner-Gleich-Operation überladen werden. Beispiel: object1 <= object2')
output('__len__','Durch das hinzufügen der __len__-Definition kann geregelt werden was bei einem Aufruf der Funktion len() geschehen soll. Beispiel: len(objekt)')
output('__lt__','Die Methode __lt__ definiert was bei einer Kleiner-Als-Operation geschehen soll. Beispiel: object1 < object2')
output('__mod__','Überlagert die Modulo-Operation. Beispiel: objekt % 5')
output('__mul__','Überlagert die Multiplikationsoperation. Beispiel: objekt*5')
output('__ne__','Überladen des Ungleich-Operators. Beispiel: objekt1 != objekt2')
output('__neg__','Überladen des Negationsoperators. Beispiel: objekt1 - 5')
output('__nonzero__','Implementiert das Testen von Wahrheitswerten Beispiel: while testobject:')
output('__setitem__','Implementiert die Zuweisung in Containerobjekten. Beispiel: object[key] = 5')
output('__str__','Wird das Objekt mit der print-Funktion ausgegeben wird diese Methode für die Ausgabe aufgerufen.')
output('','')
output('','')
output('','')
output('','')
output('','')
output('','')
output('','')
output('','')
output('','')