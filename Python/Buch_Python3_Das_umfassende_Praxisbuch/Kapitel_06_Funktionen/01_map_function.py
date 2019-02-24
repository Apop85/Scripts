#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 01_map_function.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 11:14
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 11:20
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description. Chapter 6. Page 155. Introduction into the map() function.
# The map function is used to apply function to any entry on a list.
###

word_list=['short','long','small','wide','tall','thin','thick']
word_list_length=list(map(len, word_list))   # gets the length of any word in the list.
for i in range(len(word_list)):
    print(word_list[i]+':\t'+str(word_list_length[i]))