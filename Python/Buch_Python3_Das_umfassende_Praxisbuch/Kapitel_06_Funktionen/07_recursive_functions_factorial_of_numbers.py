#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 07_recursive_functions_factorial_of_numbers.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 13:33
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 14:15
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Example chapter 6 page 182. This script will calculate the factorial of a
# user set integer.
###

def source_value():
    while True:
        src_value=input('Please enter a number: ')
        if src_value.isdecimal() and int(src_value) > 1:
            return int(src_value)

def factorial_src(value):
    if value == 1:
        return 1
    return value*factorial_src(value-1)

value=source_value()
factorial=factorial_src(value)
print(factorial)

# Alternative to recursive functions:
for i in range(value,1,-1):
    value*=(i-1)
print(value)