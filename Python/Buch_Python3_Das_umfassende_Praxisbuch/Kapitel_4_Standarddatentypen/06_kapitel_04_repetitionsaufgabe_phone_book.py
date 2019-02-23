#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 06_kapitel_04_repetitionsaufgabe_phone_book.py
# Project: Kapitel_4_Standarddatentypen
# Created Date: Saturday 23.02.2019, 13:17
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 13:24
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 6 Page 115. This script will take names and phone numbers and output them
# together at the end.
###

print('Enter nothing to continue')
phone_book=[]
while True:
    new_name=input('Enter name: ')
    if new_name == '':
        break
    elif not new_name.isalpha():
        continue
    new_phone=input('Enter phone number :')
    if not new_phone.isdecimal():
        continue
    phone_book+=[(new_name,new_phone)]

print('\n\n\nPhone list:')
for entry in phone_book:
    print(entry[0]+'s phone number is: '+entry[1])