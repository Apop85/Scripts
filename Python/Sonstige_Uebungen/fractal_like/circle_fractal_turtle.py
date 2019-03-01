#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: circle_fractal_turtle.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Thursday 28.02.2019, 13:15
# Author: Apop85
# -----
# Last Modified: Friday 01.03.2019, 11:09
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try to paint a circle fractal with turtle
###
from turtle import *


def spiral_fractal(x):
    if x < 0.05:
        return
    for i in range(1,181):
        right(2)
        forward(x)
        if i % 60 == 0:
            spiral_fractal(x/2.15)

def move_2_start():
    up()
    left(90)
    forward(250)
    right(90)
    down()

try:
    clear()
    speed(500)
    move_2_start()
    spiral_fractal(8)
    input()
except:
    print("Script aborted")