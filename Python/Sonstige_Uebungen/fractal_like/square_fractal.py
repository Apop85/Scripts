#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: square_fractal.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Thursday 28.02.2019, 13:11
# Author: Apop85
# -----
# Last Modified: Thursday 28.02.2019, 13:14
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try to paint a square fractal with turtle
###
from time import sleep
from turtle import *

def square_fractal(x):
    if x < 2:
        return
    for i in range(4):
        forward(x)
        right(90)
        square_fractal(x/2)


def move_2_start():
    up()
    back(100)
    down()

try:
    clear()
    speed(5000)
    move_2_start()
    square_fractal(200)
    pause(20)
except:
    print("Script aborted")