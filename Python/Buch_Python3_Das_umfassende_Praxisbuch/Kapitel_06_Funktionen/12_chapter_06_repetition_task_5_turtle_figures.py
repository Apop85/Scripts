#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 12_chapter_06_repetition_task_5_turtle_figures.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 16:38
# Author: Apop85
# -----
# Last Modified: Tuesday 26.02.2019, 10:57
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 193. Task 5. This script will paint a asymmetric tree and a 90° tree
###

from turtle import *
from time import sleep

def move_to_start():
    up()
    left(90)
    back(200)
    down()

def asym_tree(x):
    if x < 3:
        return
    forward(x)
    left(30)
    asym_tree(x*0.6)
    right(90)
    asym_tree(x*0.4)
    left(60)
    back(x)
    return

def binary_tree(x):
    if x < 3:
        return
    forward(x)
    left(90)
    binary_tree(x/2)
    right(180)
    binary_tree(x/2)
    left(90)
    back(x)

clear()

move_to_start()
try:
    asym_tree(200)
    sleep(5)
    clear()
    binary_tree(200)
    sleep(5)
except:
    pass