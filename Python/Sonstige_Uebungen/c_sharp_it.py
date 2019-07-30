#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: c_sharp_it.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Monday 29.07.2019, 22:11
# Author: Apop85
#-----
# Last Modified: Monday 29.07.2019, 22:28
#-----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Takes Ascii art and translates into c sharp console print code
####

from pyperclip import copy,paste
from time import sleep

def check_clipboard():
    copy("0")
    print("Waiting for Clipboard")
    while paste() == "0":
        sleep(0.5)
    print(paste())
    return paste()     

def print_csharp(ascii_art):
    ascii_art = ascii_art.split("\n")
    print('\n')
    for draw_line in ascii_art:
        print('    Console.WriteLine("', end="")
        print(draw_line, end='"'+')'+';')
        print('')

ascii_art = check_clipboard()
print_csharp(ascii_art)