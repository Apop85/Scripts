#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 12_kapitel_05_repetitionsaufgabe_11_ulam_sequence.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Sunday 24.02.2019, 10:14
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 10:19
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 11. Page 147. This script will take a number and generate the ulam sequence
# with this number as start value. Tasks: get a start value, if value equals 1 stop script,
# if value%2 equals 0 devide value by 2 else multiplicate the value with 3 and add 1
###

while True:
    start_value=input('Enter a start value: ')
    if start_value.isdecimal() and int(start_value) > 0:
        value=int(start_value)
        break

while value != 1:
    if value % 2 == 0:
        value/=2
    else:
        value=(value*3)+1
    print(int(value), end=' ')