#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_subfunctions_german_or_not.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 12:10
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 12:19
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will calculate the percentual amount of specific letters and check
# if this amount is matching the german language.
###

def deutsch(text):
    def letter_amount(text, letter):
        amount=text.count(letter)
        percent=100/len(text)*amount
        return percent
    if (4 < letter_amount(text, "a") < 8) and (15 < letter_amount(text, "e") < 20) and (letter_amount(text, "y") < 1):
        return True
    else: 
        return False

check_text=input('Enter text to check: ')
if deutsch(check_text.lower()):
    print('Probably german.')
else:
    print('Probably not german.')