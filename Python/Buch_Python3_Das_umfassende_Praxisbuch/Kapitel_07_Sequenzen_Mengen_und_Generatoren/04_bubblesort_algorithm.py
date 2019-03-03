#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_bubblesort_algorithm.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Sunday 03.03.2019, 19:49
# Author: Apop85
# -----
# Last Modified: Sunday 03.03.2019, 20:02
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 214. Example of the bubblesort algorithm.
###


def bubblesort(item_list):
    for j in range(len(item_list)-1):
        for i in range(len(item_list)-1):
            a,b=item_list[i], item_list[i+1]
            if a > b:
                item_list[i+1], item_list[i] = a,b
        print(item_list)
    return item_list

unsorted=[8,6,7,3,15,1,65,19,24,5,9,0]
sorted_list=bubblesort(unsorted)
print(sorted_list)