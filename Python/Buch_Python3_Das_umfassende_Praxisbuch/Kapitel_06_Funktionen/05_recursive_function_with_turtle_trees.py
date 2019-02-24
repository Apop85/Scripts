#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 05_recursive_function_with_turtle_trees.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 12:57
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 13:08
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Example at chapter 6 page 178. Draw a recursive Tree with turtle.
###

from turtle import *

def turtle_tree(x):
    if x < 3:
        return
    else:
        forward(x)
        left(45)
        turtle_tree(x/1.65)
        right(90)
        turtle_tree(x/1.65)
        left(45)
        back(x)
    return

clear()
left(90)
try:
    turtle_tree(100)
    hideturtle()
    input('Enter to end script')
except:
    print('Script aborted')
