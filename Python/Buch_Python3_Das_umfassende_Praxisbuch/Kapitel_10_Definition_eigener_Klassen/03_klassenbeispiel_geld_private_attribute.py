#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_klassenbeispiel_geld_private_attribute.py
# Project: Kapitel_10_Definition_eigener_Klassen
# Created Date: Monday 11.03.2019, 15:30
# Author: Apop85
# -----
# Last Modified: Monday 11.03.2019, 15:59
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 10. Page 290. Example class "Geld" using private attributes.
###

class Geld(object):
    __wechselkurs={'USD':0.84998, 
                'GBP':1.3948, 
                'EUR':1.0, 
                'JPY':0.007168}
    
    def __init__(self, waehrung, betrag):
        self.__waehrung = waehrung
        self.__betrag = float(betrag)
    
    def get_betrag(self):
        return round(self.__betrag,2)

    def get_waehrung(self):
        return self.__waehrung

    def set_betrag(self, neuer_betrag):
        self.__betrag = round(float(neuer_betrag),2)

    def set_waehrung(self, neue_waehrung):
        if neue_waehrung in self.__wechselkurs.keys():
            alt = self.__wechselkurs[self.__waehrung]
            neu = self.__wechselkurs[neue_waehrung]
            self.__betrag = alt/neu * self.__betrag
            self.__waehrung = neue_waehrung

    def getEuro(self):
        return self.__betrag * self.__wechselkurs[self.__waehrung]
    
    def add(self, geld):
        summe_in_euro = self.getEuro()+geld.getEuro() 
        summe=Geld(self.__waehrung, summe_in_euro/self.__wechselkurs[self.__waehrung])
        return summe

    betrag = property(get_betrag, set_betrag)
    waehrung = property(get_waehrung, set_waehrung)

# Beispielaufrufe
preis = Geld('USD', 100.45).add(Geld('JPY',15789))
preis.set_waehrung('EUR')
print(preis.get_betrag(), preis.get_waehrung())

# Durch die property-Funktion am Ende der Klasse kann noch immer, 
# scheinbar direkt, auf die Attribute zugegriffen werden.
preis = Geld('USD', 100.45)
preis.waehrung = 'EUR'
print(preis.betrag, preis.waehrung)