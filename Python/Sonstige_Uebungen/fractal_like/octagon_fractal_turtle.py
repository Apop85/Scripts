#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: octagon_fractal_turtle.py
# Project: Sonstige_Uebungen
# Created Date: Thursday 28.02.2019, 12:40
# Author: Apop85
# -----
# Last Modified: Friday 01.03.2019, 12:34
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try to paint a octagon fractal with turtle
###
from turtle import *

def octa_fract(x):
    if x < 0.8:
        return
    for i in range(8):
        forward(x)
        right(45)
        octa_fract(x/3.4)

def move_to_start():
    up()
    back(100)
    left(90)
    forward(250)
    right(90)
    down()

try:
    speed(500)
    move_to_start()
    octa_fract(200)
    input()
except:
    print("Script aborted")
