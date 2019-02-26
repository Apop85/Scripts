#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_recursive_sum_of_sequences.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 26.02.2019, 11:51
# Author: Apop85
# -----
# Last Modified: Tuesday 26.02.2019, 11:56
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 201. This script will calculate the sum of a list content 
# with a recursive function.
###

num_list=[15,18,20,12,-5,-10]

def sum_of(num_list):
    if len(num_list) == 1:
        return num_list[0]
    return num_list[0]+sum_of(num_list[1:])

sum_list=sum_of(num_list)
print(sum_list)