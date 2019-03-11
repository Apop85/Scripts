#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_klassenbeispiel_geld_ueberlagerungen.py
# Project: Kapitel_10_Definition_eigener_Klassen
# Created Date: Monday 11.03.2019, 16:45
# Author: Apop85
# -----
# Last Modified: Monday 11.03.2019, 17:18
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 10. Page 296. Example with overlaping functions
###

class Geld(object):
    __wechselkurs={'USD':0.84998, 
                'GBP':1.3948, 
                'EUR':1.0, 
                'JPY':0.007168}
    
    def __init__(self, waehrung, betrag):
        self.waehrung = waehrung
        self.betrag = round(float(betrag),2)
    
    def getEuro(self):
        return self.betrag * self.__wechselkurs[self.waehrung]

    def __add__(self, geld):
        # Überladung des Plusoperators
        a = self.getEuro()
        b = geld.getEuro()
        faktor = 1.0/self.__wechselkurs[self.waehrung]
        summe = Geld(self.waehrung, (a+b)*faktor)
        return summe
        
    def __neg__(self):
        # Überladen des Minusoperators
        a = self.getEuro()
        faktor = 1.0/self.__wechselkurs[self.waehrung]
        summe = Geld(self.waehrung, (-a)*faktor)
        return summe

    def __lt__(self, other):
        # Überladen des "<"-Operators
        a = self.getEuro()
        b = other.getEuro()
        return a < b
    
    def __le__(self, other):
        # Überladen des "<="-Operators
        a = self.getEuro()
        b = other.getEuro()
        return a <= b
    
    def __eq__(self, other):
        # Überladen des "=="-Operators
        a = self.getEuro()
        b = other.getEuro()
        return a == b
    
    def __str__(self):
        # Ausgabe bei print()-Aufruf
        return format(self.betrag, ".2f")+' '+self.waehrung


# Beispielaufrufe
budget = Geld('USD',500)
preis = Geld('EUR',100)

print(budget+-preis)
budget=budget+-preis

if budget > preis:
    print('Price is affordable')


