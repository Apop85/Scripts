#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 05_quicksort_algorithm.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Sunday 03.03.2019, 20:10
# Author: Apop85
# -----
# Last Modified: Sunday 03.03.2019, 20:26
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 215. Quicksort-algorithm using recursive function.
###

def quick_sort(item_list):
    if len(item_list) > 0:
        print('Sorting...', item_list)
    if len(item_list) <= 1:
        return item_list
    else:
        return quick_sort([x for x in item_list[1:] if x < item_list[0]]) + [item_list[0]] + quick_sort([y for y in item_list[1:] if y > item_list[0]])

unsorted_list=['m','g','w','h','l','z','b','c','y']
sorted_list=quick_sort(unsorted_list)
print(sorted_list)