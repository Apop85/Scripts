#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_chapter_09_repetition_task_1.py
# Project: Kapitel_09_Ein_und_Ausgabe
# Created Date: Sunday 10.03.2019, 14:45
# Author: Apop85
# -----
# Last Modified: Sunday 10.03.2019, 15:02
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 9. Page 272. Task 1. Task: Race example. There are 10 runners. 
# As soon a runner crosses the finish line enter his start number and press enter. The 
# script will save the start number and the local time in a output file. Script will 
# exit if the input is empty
###

import os
from time import asctime
os.chdir(os.path.dirname(__file__))

target_file='.\\output.txt'
file_writer=open(target_file, 'w')
file_writer.close()

while True:
    number=input('Enter number or empty to exit: ')
    if number=='':
        break
    elif not number.isdecimal():
        continue
    write_string=number+'\t'+asctime()+'\n'
    with open(target_file, 'a') as file_writer:
        file_writer.write(write_string)
    print('Saved: '+write_string)
    

