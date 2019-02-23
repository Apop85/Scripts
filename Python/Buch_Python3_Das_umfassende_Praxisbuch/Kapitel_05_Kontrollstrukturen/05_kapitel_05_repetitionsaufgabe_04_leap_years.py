#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 05_kapitel_05_repetitionsaufgabe_04_leap_years.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 22:34
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 22:42
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 4. Page 144. This script will show you the next leap year from the year 
# set by the user. A leap year have to be divided by 400 without rest or if the year is 
# divideable by 4 but not by 100.
###

while True:
    check_year=input('Enter year to check: ')
    if check_year.isdecimal() and int(check_year) > 0:
        check_year=int(check_year)
        break

while True:
    if check_year%400 == 0 or (check_year%4 == 0 and check_year%100 != 0):
        print('The year '+str(check_year)+' is a leap year.')
        break
    check_year+=1
