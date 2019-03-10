#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_example_ceasar_chiffre.py
# Project: Kapitel_09_Ein_und_Ausgabe
# Created Date: Sunday 10.03.2019, 14:17
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 14:41
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 9. Page 270. Example of using command line arguments.
###

import sys

def manual_input():
    while True:
        string=input('Enter string to encrypt: ')
        shift=input('Alphabet shift: ')
        if shift.lstrip('-').isdecimal():
            return string, int(shift)

def ceasar(string='', shift=0):
    encrypted=''
    if (string, shift) == ('', 0):
        string=sys.argv[1]
        if not sys.argv[2].isdecimal():
            raise Exception('Invalid Arguments. Example: filename.py <string> <integer>')
        shift=int(sys.argv[2])
    for letter in string:
        new_letter=ord(letter.upper())+shift
        if new_letter > ord('Z'):
            new_letter-=25
        encrypted+=chr(new_letter)
    return encrypted

if len(sys.argv) == 1:
    string,shift=manual_input()
    print(ceasar(string=string, shift=shift))
elif len(sys.argv) == 3:
    print(ceasar())
elif len(sys.argv) > 3:
    print('Too many arguments. Example: filename.py <string> <integer>')
else:
    print('Missing argument. Example: filename.py <string> <integer>')