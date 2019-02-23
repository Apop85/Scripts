#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_kapitel_04_repetitionsaufgabe_charts.py
# Project: Kapitel_04_Standarddatentypen
# Created Date: Saturday 23.02.2019, 13:12
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 14:51
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Input some song titles and this script will output them as chart list
###
chart_list=[]
print('Enter nothing to continue.')
while True:
    new_song=''
    new_song=input('Input song title: ')
    if new_song == '':
        break
    chart_list+=[new_song]

for i in range(len(chart_list)):
    print('Rank '+str(i+1)+': '+chart_list[i])
