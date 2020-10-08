#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: 05_Statische_Methoden.py
# Project: Kapitel_10_Definition_eigener_Klassen
#-----
# Created Date: Wednesday 07.10.2020, 13:43
# Author: Apop85
#-----
# Last Modified: Wednesday 07.10.2020, 13:44
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Chapter 10. Page 298. Example with static methods
####

class statistik(object):
    # Eine Statische Methode besitzt keine __init__ Funktion. Der "="-Operator wird also nicht überlagert und es können 
    # keine neuen Attribute erstellt werden

    def mittelwert(liste):
        # Auch wird bei der definition von statischen Methoden kein "self" aufgeführt
        if liste:
            return round(sum(liste) / len(liste), 2)
    
    def spannweite(liste):
        # Grösste minus kleinste Zahl der liste
        if liste:
            return max(liste) - min(liste)

    def median(liste):
        if liste:
            liste_sortiert = sorted(liste)
            if len(liste_sortiert) % 2 == 0:
                # Ist die Länge der Liste ungerade gebe mittelwert der mittleren beiden Zahlen aus
                return (liste_sortiert[int(len(liste)/2-1)] + liste_sortiert[int(len(liste)/2)])/2.0
            else:
                # Ist die Länge ungerade gebe den mittleren Wert zurück
                return liste_sortiert[int((len(liste)-1)/2)]

    # Anschliessend müssen die Methoden noch als statische Methoden definiert werden.
    mittelwert = staticmethod(mittelwert)
    spannweite = staticmethod(spannweite)
    median = staticmethod(median)

zahlenliste = [1,4,2,3,4,6,4,7,4,9,3,7]

# Der Aufruf der statischen Methode erfolgt über <Klasse>.<Methode>(<Parameter>)
print(statistik.mittelwert(zahlenliste))
print(statistik.spannweite(zahlenliste))
print(statistik.median(zahlenliste))