#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 11_kapitel_05_repetitionsaufgabe_10_dna_sequence.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Sunday 24.02.2019, 10:07
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 10:11
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 10. Page 147. This script will print out any possible combinations
# of DNS base pairs with the length of 4. Aviable combinations: TA AT GC CG
###

base_list=['TA','AT','GC','CG']

for base1 in base_list:
    for base2 in base_list:
        for base3 in base_list:
            for base4 in base_list:
                print(base1,base2,base3,base4)