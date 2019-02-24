#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 12_chapter_06_repetition_task_5_turtle_figures.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 16:38
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 17:10
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 193. Task 5. This script will paint a asymmetric tree and a 90° tree
###

from turtle import *

def move_to_start():
    up()
    left(90)
    back(200)
    down()

def asym_tree(x):
    forward(x)
    left(30)
    if x > 3:
        asym_tree(x*0.6)
    right(30)
    back(x)
    right(70)
    forward(x)
    if x > 3:
        asym_tree(x*0.4)
    back(x)
    left(70)
    # back(x)
    return



clear()

move_to_start()
try:
    forward(100)
    left(30)
    asym_tree(100)
except:
    pass