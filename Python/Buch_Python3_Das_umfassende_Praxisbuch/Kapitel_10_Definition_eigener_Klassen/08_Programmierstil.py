#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: 08_Programmierstil.py
# Project: Kapitel_10_Definition_eigener_Klassen
#-----
# Created Date: Wednesday 07.10.2020, 16:30
# Author: Apop85
#-----
# Last Modified: Wednesday 07.10.2020, 16:30
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Chapter 10. Page 301. Clean coding example
####

class Behaelter(object):
    # 1. Zeile: Was macht die Klasse
    # 2. Zeile: Leer
    # 3. Zeile: Öffentliche Methoden und Attribute auflisten und kurzbeschreib 
    '''Setzt ein Volumen für einen Behälter fest

    Methoden: 
    setVolume() - Setzt das Volumen fest
    getVolume() - Gibt das Volumen zurück'''

    def __init__(self, volumen):
        # Attribute die ausserhalb der Klasse nicht modifiziert werden sollen, 
        # sollen als private Attribute definiert werden
        self.__volumen = float(volumen) # Volumen in Liter

    def setVolume(self, volumen):
        self.__volumen = float(volumen)

    def getVolume(self):
        return self.__volumen


becher = Behaelter(0.33)
karaffe = Behaelter(1.5)
weinglas = Behaelter(0.25)

try:
    print(becher.__volumen)
except:
    print("becher.__volumen kann nicht aufgerufen werden es es eine private Methode ist")

print(becher.getVolume())
karaffe.setVolume(1)
print(karaffe.getVolume())
print(weinglas.getVolume())