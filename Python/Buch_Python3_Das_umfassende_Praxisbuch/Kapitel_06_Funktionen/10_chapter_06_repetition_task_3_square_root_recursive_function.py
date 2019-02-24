#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 10_chapter_06_repetition_task_3_square_root_recursive_function.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 14:46
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 15:04
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 191. Task 3. This script will calculate the square root by a recursive
# function using the heronic algorithm. 
###

def square_root(value, approach=10):
    if approach == 1:
        return 1
    return 0.5*(square_root(value, approach-1)+value/square_root(value, approach-1))

def get_value():
    value=input('Enter value to calulate the square root: ')
    if value.isdecimal() and int(value) > 1:
        return int(value)

value=get_value()
sqrt=square_root(value, 15)
print(sqrt)