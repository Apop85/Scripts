#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 11_chapter_07_repetition_task_04_dance_partner.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Thursday 07.03.2019, 11:29
# Author: Apop85
# -----
# Last Modified: Thursday 07.03.2019, 11:40
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 232. Task 4: Generate couples 
###

males=set('ABCD')
females=set('abcd')
teacher=set('X')

couples=set((i,k) for i in males|teacher for k in females|teacher if i != k)
for i in couples:
    print(i, end=' ')
