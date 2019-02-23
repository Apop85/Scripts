#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 08_kapitel_05_repetitionsaufgabe_07_bacteria_reproduction.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 23:35
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 23:41
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 7. Page 146. This script will show the bacteria 
# reproduction for the next 48 hours doubling the amount every 30 minutes
###

bacteria=1
print('Bacteria amount hour 0: '+str(bacteria))
for i in range(48):
    bacteria*=4
    print('Bacteria amount hour '+str(i+1)+': '+str(bacteria))