#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 10_kapitel_05_repetitionsaufgabe_09_capital_letter_combination.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Sunday 24.02.2019, 00:01
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 00:04
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 9. Page 147. This script woll print any possible combination 
# of capital letter with the length of 2.
###

possible_letter="ABCDEFGHIJKLMNOPQRSTUVWXYZ"

for i in range(len(possible_letter)):
    for j in range(len(possible_letter)):
        print(possible_letter[i]+possible_letter[j], end=' ')