#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_kapitel_05_repetitionsfragen.py
# Project: Kapitel_5_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 14:48
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 14:50
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
###

import re

max_text_length=70
max_text_delta=14

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\(?"?\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-9)+r'}[ |.|,|\n|>|\W|\)|-]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()