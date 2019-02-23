#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_fibonacci.py
# Project: Kapitel_5_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 14:00
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 14:08
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Prints out the Fibonacci sequence.
###

a1=1
a2=1

amount=int(input('How many Fibonacci numbers to print? '))
print(a1,a2, end=' ')
for i in range(amount):
    a=a1+a2
    print(a, end=' ')
    a1,a2=a2,a
