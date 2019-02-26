#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 03_binary_search_algorithm.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 26.02.2019, 12:51
# Author: Apop85
# -----
# Last Modified: Tuesday 26.02.2019, 13:24
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 212. This script will search for a specific item in a list
# using the binary search algorythm: Split the list and check if target item is in the first
# or second split and check this split. Repeat recursive until found.
###

search_list=['description', 'chapter', 'page', 'this', 'script', 'will', 'search', 'for', 'a', 'specific', 'item', 'in', 'a', 'list', 'using', 'the', 'binary', 'search', 'algorithm', 'split', 'the', 'list', 'and', 'check', 'if', 'target', 'item', 'is', 'in', 'the', 'first', 'or', 'second', 'split', 'and', 'check', 'this', 'split', 'repeat', 'recursive', 'until', 'found', 'nothing']
search_list.sort()

def find_it_binary(search_list, search_item):
    if len(search_list) == 1 and search_list[0] == search_item:
        return True
    elif len(search_list) == 1:
        return False
    if search_list[:len(search_list)//2][-1] >= search_item:
        return find_it_binary(search_list[:len(search_list)//2], search_item)
    else:
        return find_it_binary(search_list[len(search_list)//2:], search_item)

print(find_it_binary(search_list,'specific'))
