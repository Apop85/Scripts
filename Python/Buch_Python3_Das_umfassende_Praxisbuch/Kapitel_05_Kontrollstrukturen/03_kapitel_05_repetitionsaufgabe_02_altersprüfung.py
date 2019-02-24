#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_kapitel_05_repetitionsaufgabe_02_altersprüfung.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 22:20
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 10:56
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 2. Page 144. This script will check the user input and determine 
# if the user is a youth or mature.
###

while True:
    user_age=input('Please input your age: ')
    if user_age.isdecimal() and int(user_age) > 0:
        user_age=int(user_age)
        break

if 14 < user_age < 18:
    print('You\'re a youth.')
elif 18 <= user_age:
    print('You\'re mature.')
else:
    print('You\'re a child.')