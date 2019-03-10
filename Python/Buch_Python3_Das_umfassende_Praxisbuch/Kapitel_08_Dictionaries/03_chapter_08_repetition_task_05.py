#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_chapter_08_repetition_task_05.py
# Project: Kapitel_08_Dictionaries
# Created Date: Sunday 10.03.2019, 12:30
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 12:39
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 8. Page 247. Task 5. Write a function which counts how often any
# letter of a string occurs inside the string.
###

def letter_frequency(string, result={}):
    for i in range(len(string)):
        result.setdefault(string[i], string.count(string[i]))
    print(result)

string=input('Enter string: ')
letter_frequency(string)