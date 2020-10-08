#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: 07_Standardklassen_als_Basisklassen.py
# Project: Kapitel_10_Definition_eigener_Klassen
#-----
# Created Date: Wednesday 07.10.2020, 16:03
# Author: Apop85
#-----
# Last Modified: Wednesday 07.10.2020, 16:03
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Chapter 10. Page 304. Example with standard classes as base classes
####

# Neben den Standardmethoden lassen sich auch Methoden anderer Objekte überlagern bzw erweitern wie 
# beispielsweise "gititem" welches zu der Klasse "list" gehört

class DefaultList(list):
    '''Überschreibung: getitem() gibt None zurück anstatt IndexError bei fehlendem Eintrag'''

    def __init__(self, liste=[], default=None):
        self.default = default
        list.__init__(self, liste)

    def __getitem__(self, index):
        try:
            return list.__getitem__(self, index)
        except:
            return self.default

    # Damit bei einer Listenkonkatenierung kein Fehler auftritt weil versucht 
    # wird "list" und "DefaultList" miteinander zu vermischen muss die __add__ Methode überschrieben werden.
    def __add__(self, item):
        result = list.__add__(self, item)
        return DefaultList(result, self.default)


liste = DefaultList([1,2,3,4,5])

for i in range(0, len(liste) + 1):
    # Hier wird der Standard-Defaultwert "None" verwendet
    print(liste[i])
input('Enter zum fortfahren...')


planeten = ["Merkur", "Venus", "Erde", "Mars", "Jupiter", "Saturn", "Uranus", "Neptun"]
# In diesem Beispiel wird der Standard-Defaultwert überschrieben
planeten_default_liste = DefaultList(planeten, "Unbekannter Planet")
planeten_default_liste += ["Pluto"]

for i in range(0, len(planeten_default_liste) + 1):
    print(planeten_default_liste[i])
