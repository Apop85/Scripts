#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_chapter_09_repetition_task_2.py
# Project: Kapitel_09_Ein_und_Ausgabe
# Created Date: Sunday 10.03.2019, 15:04
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 15:12
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 9. Page 273. Task 2. Print a list of tuples in different 
# formats to the screen.
###

tuple_list=[('Gold',0.1234), ('Silver',23.45), ('Platin',0.0678)]

# Method 1
for item in tuple_list:
    print(item, end=' // ' if item != tuple_list[-1] else '\n\n')

# Method 2
for item in tuple_list:
    print(item[0]+'\t'+str(item[1]))
print()

# Method 3
for item in tuple_list:
    print(format(item[0]+':','8s')+format(item[1],'8.2f'))