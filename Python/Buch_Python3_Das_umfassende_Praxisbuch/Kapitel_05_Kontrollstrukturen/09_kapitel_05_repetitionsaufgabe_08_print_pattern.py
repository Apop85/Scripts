#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 09_kapitel_05_repetitionsaufgabe_08_print_pattern.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 23:44
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 23:58
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 8. Page 146. This script will print some pattern made of symbols
###

symbol="*"

for i in range(4):
    for j in range(4):
        print(symbol, end=' ')
    print()
print()

for i in range(1,6):
    print(symbol*i)
print()

for i in range(0,5):
    print(' '*(4-i)+symbol*(1+i+i))
    