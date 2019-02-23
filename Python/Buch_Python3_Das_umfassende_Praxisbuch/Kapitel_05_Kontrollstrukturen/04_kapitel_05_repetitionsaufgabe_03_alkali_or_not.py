#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_kapitel_05_repetitionsaufgabe_03_alkali_or_not.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 22:27
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 22:32
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script is able to tell if a entered chemical element is an alkaline
# metal or not. 
###

alkaline_elements=["na","li","k","rb","cs"]

print('Enter a chemical element to check if it\'s a alkaline metal or not.')
check_element=input('Enter chemical element: ')
if check_element.lower() in alkaline_elements:
    print(check_element.title()+' is a alkaline metal.')
else:
    print(check_element.title()+' is not a alkaline metal.')

