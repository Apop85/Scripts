#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: 06_Klassenvererbung_und_Erweiterung.py
# Project: Kapitel_10_Definition_eigener_Klassen
#-----
# Created Date: Wednesday 07.10.2020, 14:04
# Author: Apop85
#-----
# Last Modified: Wednesday 07.10.2020, 14:04
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Chapter 10. Page 301. Example with inherited and extending classes
####

import time
from random import randint
from random import choice

class Geld(object):
    __wechselkurs={'USD':0.84998, 
                'GBP':1.3948, 
                'EUR':1.0, 
                'JPY':0.007168,
                'CHF':0.95,
                'THB':0.033,
                'RPE':0.028}
    
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
    


# Durch die Angabe von "Geld" als basisklasse erweitert die Klasse "Konto" nun diese.
class Konto(Geld):
    """Spezialisierung der Klasse Geld für die Verwaltung eines Kontos
    geerbt: betrag, waehrung und wechselkurs
    öffentliche Methoden und Überladungen:
        geerbt: __add__, __lt__, __le__, __eq__, getEuro
        überschrieben: __str__
        erweiterungen: einzahlen, auszahlen, kontosauszug
    """

    def __init__(self, waehrung, inhaber):
        # Als erstes wird die __init__ Methode der Basisklasse aufgerufen. Bei der eröffnung eines Kontos soll der Betrag 0 sein
        Geld.__init__(self, waehrung, 0)
        self.__inhaber = inhaber
        self.__kontoauszug = [str(self)]

    def einzahlung(self, waehrung, betrag):
        # Der Betrag der eingezahlt werden soll wird zuerst an die Geld-Methode übergeben 
        einzahlung = Geld(waehrung, betrag)
        # Durch die überschreibung von __add__ in der Basismethode kann hier direkt eine Addition verwendet werden. Die Basismethode 
        # errechnet den endgültigen Betrag anhand des Umrechnungsfaktors
        self.betrag = (self + einzahlung).betrag
        log_eintrag = time.asctime() + ' ' + str(einzahlung) + ' neuer Kontostand: ' + self.waehrung + format(self.betrag, '2f')
        self.__kontoauszug += [log_eintrag]

    def auszahlen(self, waehrung, betrag):
        self.einzahlung(waehrung, -betrag)

    def kontosauszug(self):
        for eintrag in self.__kontoauszug:
            if eintrag != None:
                print(eintrag)
        self.__kontoauszug = [str(self)]

    def __str__(self):
        betrag = format(self.betrag, '2f')
        return "Konto von {} Kontostand am {} {} {}".format(self.__inhaber, time.asctime(), self.waehrung, betrag)


# Neues Konto erstellen
konto = Konto('USD', 'Apop85')

# Einzahlen
konto.einzahlung('EUR', 1000)

# Auszahlungen
auszahlungen = []
waehrungen = ['USD', 'JPY', 'EUR', 'GBP', 'CHF', 'RPE']
choices = [0,0,0,0,0,1]
for i in range(randint(4,12)):
    time.sleep(randint(1,10))
    konto.auszahlen(waehrungen[randint(0,len(waehrungen)-1)], randint(1000,25000)/100)
    print("Geld abgehoben")
    if choice(choices):
        konto.kontosauszug()

# Kontoauszug erstellen
konto.kontosauszug()
print(konto)

    