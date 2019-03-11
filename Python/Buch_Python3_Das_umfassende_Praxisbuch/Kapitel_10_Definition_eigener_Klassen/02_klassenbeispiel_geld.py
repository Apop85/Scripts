#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_klassenbeispiel_geld.py
# Project: Kapitel_10_Definition_eigener_Klassen
# Created Date: Monday 11.03.2019, 14:25
# Author: Apop85
# -----
# Last Modified: Monday 11.03.2019, 15:04
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 10. Page 284. Currency used as class example.
###

class Geld(object):
    # Festlegung der Klassenattribute
    wechselkurs={'USD':0.84998, 
                'GBP':1.3948, 
                'EUR':1.0, 
                'JPY':0.007168}
    
    def __init__(self, waehrung, betrag):
        # Die Konstruktormethode bewirkt, dass ein neues Objekt nach dem Bauplan der 
        # Klasse Geld generiert wird. Das erste Argument "self" darf bei keiner 
        # Methodendefinition fehlen. Es bezeichnet das konkrete Objekt (die Instanz). 
        # Die beiden anderen Argumente sind Anfangswerte für die Attribute waehrung und 
        # betrag. Es ist eine Hauptaufgabe der Konstruktormethode, die Objektattribute mit 
        # Anfangswerten zu belegen. Für Zuweisungen verwendet man das folgende Format: 
        # self.attribut = attribut
        self.waehrung = waehrung
        self.betrag = round(float(betrag),2)
        # Die __init__()-Methode darf keine return-Anweisung enthalten.
    
    def getEuro(self):
        # Beim Zugriff auf Attribute der Instanz wird zuerst der Instanzname self, 
        # dann ein Punkt und dann der name des Attributs aufgeschrieben. Mithilfe des 
        # Objektattributs self.betrag und des Klassenattributs self.wechselkurs wird 
        # der Betrag in Euro berechnet und zurückgegeben.
        return self.betrag * self.wechselkurs[self.waehrung]
    
    def add(self, geld):
        # Die Methode add() verwendet zwei Argumente: Das erste Argument self bezeichnet  
        # wiederum die Instanz, geld bezeichnet ein Objekt der Klasse Geld. 
        
        # Zunächst werden die Geldwerte in Euro des eigenen Objektes self und des 
        # übergebenen Objektes addiert.
        summe_in_euro = self.getEuro()+geld.getEuro()
        # In dieser Zeile wird ein neues Geld-Objekt namens summe instanziert. Es erhält als 
        # Wert für das Attribut betrag die Summe in Euro, umgerechnet in die Währung der 
        # Instanz self. Die Währung ist die geliche wie bei der Instanz self. 
        summe=Geld(self.waehrung, summe_in_euro/self.wechselkurs[self.waehrung])
        # Dieses neue Objekt wird in der return-Anweisung an den Sender der Botschaft 
        # zurückgegeben.
        return summe

# Beispielaufrufe
banknote = Geld('USD', 100)
print(banknote)
print(banknote.betrag, banknote.waehrung)

hotelrechnung=Geld('USD',123.45)
mietwagen=Geld('EUR',527.30)
summe=hotelrechnung.add(mietwagen)
# Da die Hotelrechnung in USD definiert wurde ist auch die Ausgabe in USD
print(summe.betrag, summe.waehrung)

summe = mietwagen.add(hotelrechnung)
# Da der Mietwagen in EUR definiert wurde ist auch die Ausgabe in EUR
print(summe.betrag, summe.waehrung)

# Verwendung anonymer Objekte
summe=(Geld('EUR',55)).add(Geld('USD',111))
print(summe.betrag, summe.waehrung)
