#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: penta_fractal_turtle.py
# Project: Sonstige_Uebungen
# Created Date: Thursday 28.02.2019, 12:07
# Author: Apop85
# -----
# Last Modified: Friday 01.03.2019, 12:50
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try of drawing a pentagram fractal
###

from turtle import *

def penta_cycle(x):
    if x < 10:
        return
    for i in range(5):
        forward(x)
        right(144)
        penta_cycle(x/2)

def move_to_start():
    up()
    back(250)
    right(90)
    back(50)
    left(90)
    down()
    speed()

clear()
try:
    speed(0)
    move_to_start()
    penta_cycle(500)
    input()
except:
    print('Script aborted')