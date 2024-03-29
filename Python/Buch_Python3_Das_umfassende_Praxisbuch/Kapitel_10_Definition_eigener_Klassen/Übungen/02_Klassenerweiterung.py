#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: 02_Klassenerweiterung.py
# Project: Übungen
#-----
# Created Date: Wednesday 07.10.2020, 18:18
# Author: Apop85
#-----
# Last Modified: Wednesday 07.10.2020, 18:18
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:Chapter 10. Page 311. Task to extend a existing class and add the class "Square" to "Ding"
####

class Ding(object):
    '''Takes an Object by Element and its volume to calculate density and mass of the object
    element: str
    volume: float

    getDensity(): Returns density of the object
    getMass(): Returns mass of the object
    getVolume(): Returns the objects volume
    setVolume(): Changes the volume of the object
    setElement(): Changes the objets element
    
    Overwrites: __str__'''


    __periodic_table={ 'H' : 1.0079, 'He' : 4.0026,
                'Li' : 6.941, 'Be' : 9.0122, 'B' : 10.811, 'C' : 12.011, 'N' : 14.007, 'O' : 15.999, 'F' : 18.988, 'Ne' : 20.180,
                'Na' : 22.990, 'Mg' : 24.305, 'Al' : 26.982, 'Si' : 28.086, 'P' : 30.974, 'S' : 32.065, 'Cl' : 35.453, 'Ar' : 39.948,
                'K' : 39.098, 'Ca' : 40.078, 'Sc' : 44.956, 'Ti' : 47.867, 'V' : 50.942, 'Cr' : 51.996, 'Mn' : 54.938, 'Fe' : 55.845, 'Co' : 58.933, 'Ni' : 58.693, 'Cu' : 63.546, 'Zn' : 65.38, 'Ga' : 69.723, 'Ge' : 72.64, 'As' : 74.922, 'Se' : 78.96, 'Br' : 79.904, 'Kr' : 83.798,
                'Rb' : 85.468, 'Sr' : 87.62, 'Y' : 88.906, 'Zr' : 91.224, 'Nb' : 92.906, 'Mo' : 95.96, 'Tc' : 98.91, 'Ru' : 101.07, 'Rh' : 102.91, 'Pd' : 106.42, 'Ag' : 107.87, 'Cd' : 112.41, 'In' : 114.82, 'Sn' : 118.71, 'Sb' : 121.76, 'Te' : 127.60, 'I' : 126.90, 'Xe' : 131.29,
                'Cs' : 132.91, 'Ba' : 137.33, 'Hf' : 178.49, 'Ta' : 180.95, 'W' : 183.84, 'Re' : 186.21, 'Os' : 190.23, 'Ir' : 192.22, 'Pt' : 195.08, 'Au' : 196.97, 'Hg' : 200.59, 'Tl' : 204.38, 'Pb' : 207.2, 'Bi' : 208.98, 'Po' : 209.98, 'At' : 210, 'Rn' : 222,
                'Fr' : 223, 'Ra' : 226.03, 'Rf' : 261, 'Db' : 262, 'Sg' : 263, 'Bh' : 262, 'Hs' : 265, 'Mt' : 266, 'Ds' : 296, 'Rg' : 272, 'Cn' : 277, 'Nh' : 287, 'Fl' : 289, 'Mc' : 288, 'Lv' : 289, 'Ts' : 293, 'Og' : 294,
                'La' : 138.91, 'Ce' : 140.12, 'Pr' : 140.91, 'Nd' : 144.24, 'Pm' : 146.90, 'Sm' : 146.90, 'Eu' : 151.96, 'Gd' : 157.25, 'Tb' : 158.93, 'Dy' : 162.50, 'Ho' : 164.93, 'Er' : 167.26, 'Tm' : 168.93, 'Yb' : 173.05, 'Lu' : 174.97,
                'Ac' : 227, 'Th' : 232.04, 'Pa' : 231.04, 'U' : 238.03, 'Np' : 237.05, 'Pu' : 244.10, 'Am' : 243.10, 'Cm' : 247.10, 'Bk' : 247.10, 'Cf' : 251.10, 'Es' : 254.10, 'Fm' : 257.10, 'Md' : 258, 'No' : 259, 'Lr' : 260}

    __element_names = { 'H' : "Wasserstoff", 'He' : "Helium",
                'Li' : "Lithium", 'Be' : "Beryllium", 'B' : "Bor", 'C' : "Kohlenstoff", 'N' : "Stickstoff", 'O' : "Sauerstoff", 'F' : "Fluor", 'Ne' : "Neon",
                'Na' : "Natrium", 'Mg' : "Magnesium", 'Al' : "Aluminium", 'Si' : "Silizium", 'P' : "Phosphor", 'S' : "Schwefel", 'Cl' : "Chlor", 'Ar' : "Argon",
                'K' : "Kalium", 'Ca' : "Kalzium", 'Sc' : "Scandium", 'Ti' : "Titan", 'V' : "Vanadium", 'Cr' : "Chrom", 'Mn' : "Mangan", 'Fe' : "Eisen", 'Co' : "Kobalt", 'Ni' : "Nickel", 'Cu' : "Kupfer", 'Zn' : "Zink", 'Ga' : "Gallium", 'Ge' : "Germanium", 'As' : "Arsen", 'Se' : "Selenium", 'Br' : "Brom", 'Kr' : "Kryptonium",
                'Rb' : "Rubinium", 'Sr' : "????", 'Y' : "???", 'Zr' : "Zirkonium", 'Nb' : "????", 'Mo' : "Molybdän", 'Tc' : "Technicium", 'Ru' : "????", 'Rh' : "????", 'Pd' : "????", 'Ag' : "Gold", 'Cd' : "Cadmium", 'In' : "Indium", 'Sn' : "???", 'Sb' : "????", 'Te' : "????", 'I' : "Indium", 'Xe' : "Xenon",
                'Cs' : 132.91, 'Ba' : 137.33, 'Hf' : 178.49, 'Ta' : 180.95, 'W' : 183.84, 'Re' : 186.21, 'Os' : 190.23, 'Ir' : 192.22, 'Pt' : 195.08, 'Au' : 196.97, 'Hg' : 200.59, 'Tl' : 204.38, 'Pb' : 207.2, 'Bi' : 208.98, 'Po' : 209.98, 'At' : 210, 'Rn' : 222,
                'Fr' : 223, 'Ra' : 226.03, 'Rf' : 261, 'Db' : 262, 'Sg' : 263, 'Bh' : 262, 'Hs' : 265, 'Mt' : 266, 'Ds' : 296, 'Rg' : 272, 'Cn' : 277, 'Nh' : 287, 'Fl' : 289, 'Mc' : 288, 'Lv' : 289, 'Ts' : 293, 'Og' : 294,
                'La' : 138.91, 'Ce' : 140.12, 'Pr' : 140.91, 'Nd' : 144.24, 'Pm' : 146.90, 'Sm' : 146.90, 'Eu' : 151.96, 'Gd' : 157.25, 'Tb' : 158.93, 'Dy' : 162.50, 'Ho' : 164.93, 'Er' : 167.26, 'Tm' : 168.93, 'Yb' : 173.05, 'Lu' : 174.97,
                'Ac' : 227, 'Th' : 232.04, 'Pa' : 231.04, 'U' : 238.03, 'Np' : 237.05, 'Pu' : 244.10, 'Am' : 243.10, 'Cm' : 247.10, 'Bk' : 247.10, 'Cf' : 251.10, 'Es' : 254.10, 'Fm' : 257.10, 'Md' : 258, 'No' : 259, 'Lr' : 260}


    def __init__(self, element, volume):
        # Weise private Eigenschaften zu
        self.__volume = volume
        self.__element = element.capitalize()
        self.__element_name = self.__element_names[self.__element]
        self.__mass = volume * self.__periodic_table[self.__element]
        self.__density = self.__periodic_table[self.__element]

    # Get Methoden
    def getDensity(self):
        return self.__density

    def getMass(self):
        return self.__mass

    def getElementName(self):
        return self.__element_name

    def getVolume(self):
        return self.__volume

    # Set Methoden
    def setVolume(self, new_volume):
        self.__volume = new_volume
        self.__mass = self.__volume * self.__periodic_table[self.__element]

    def setElement(self, new_element):
        self.__init__(new_element, self.__volume)

    # Überladungen
    def __str__(self):
        return "Stoffname: " + self.__element_name + "\nVolumen: " + str(round(self.__volume, 2)) + \
            "\nMasse: " + str(round(self.__mass, 2)) + "\nDichte " + str(round(self.__density, 2))


class Quader(Ding):
    '''Takes elemment, width, height and length of the object and turns it into a Ding object

    element: str
    width: float
    height: float
    length: float
    Overwrites: __eq__, __lt__, __gt__, __le__, __ge__, __ne__
    '''

    def __init__(self, element, width, height, length):
        self.__volume = width * height * length
        self.__length = length
        self.__width = width
        self.__height = height
        Ding.__init__(self, element, self.__volume)

    def __eq__(self, other):
        return self.getVolume() == other.getVolume()

    def __lt__(self, other):
        return self.getVolume() < other.getVolume()

    def __gt__(self, other):
        return self.getVolume() > other.getVolume()

    def __le__(self, other):
        return self.getVolume() <= other.getVolume()

    def __ge__(self, other):
        return self.getVolume() >= other.getVolume()

    def __ne__(self, other):
        return self.getVolume() != other.getVolume()

quader1 = Quader("fe", 1, 2, 3)
quader2 = Quader("si", 3, 4, 5)

print("{} <= {} = {}".format(quader1.getVolume(), quader2.getVolume(), quader1 <= quader2))
print("{} >= {} = {}".format(quader1.getVolume(), quader2.getVolume(), quader1 >= quader2))
print("{} > {} = {}".format(quader1.getVolume(), quader2.getVolume(), quader1 > quader2))
print("{} < {} = {}".format(quader1.getVolume(), quader2.getVolume(), quader1 < quader2))
print("{} != {} = {}".format(quader1.getVolume(), quader2.getVolume(), quader1 != quader2))
print(str(quader1))
print(str(quader2))