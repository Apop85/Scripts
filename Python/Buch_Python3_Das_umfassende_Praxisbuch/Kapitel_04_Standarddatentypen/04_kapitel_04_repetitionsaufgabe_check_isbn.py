#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_kapitel_04_repetitionsaufgabe_check_isbn.py
# Project: Kapitel_04_Standarddatentypen
# Created Date: Saturday 23.02.2019, 12:45
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 14:51
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 4, Page 114. This Script will be able to tell if a entered ISBN 
# (International Standard Book Number) is valid or not. ISBN numbers are always 10 digits long.
# The checksum of a ISBN is calculated with: (z1+z2+...+z9)%11. If the rest of this division is
# 10 there will be a x or X instead. The checksum is always at the last position.
###

def check_isbn():
    while True:
        isbn_number=input('Enter ISBN: ')
        if len(isbn_number) == 10 and isbn_number[:-1].isdecimal():
            break

    result=0
    for i in range(len(isbn_number)-1):
        result+=int(isbn_number[i])*(i+1)

    check_sum=str(result%11)
    if check_sum == "10":
        check_sum = "x"

    if isbn_number[-1].lower() == check_sum:
        print('This ISBN is valid.')
    else:
        print('This ISBN is invalid.')

def create_checksum():
    while True:
        isbn_number=input('Enter 9 digits: ')
        if isbn_number.isdecimal() and len(isbn_number) == 9:
            break
    
    result=0
    for i in range(len(isbn_number)):
        result+=int(isbn_number[i])*(i+1)
    check_sum=str(result%11)
    if check_sum == "10":
        check_sum = "x"
    
    print('The checksum of your ISBN number '+isbn_number+' is '+check_sum)
    print('The correct ISBN number would be: '+isbn_number+check_sum)

while True:
    inp=''
    inp=input('Check (0) or generate ISBN (1)? ')
    if inp == "0":
        check_isbn()
    elif inp == "1":
        create_checksum()
    elif inp == '':
        break