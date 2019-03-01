#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: pentagon_fractal.py
# Project: Sonstige_Uebungen
# Created Date: Thursday 28.02.2019, 20:29
# Author: Apop85
# -----
# Last Modified: Friday 01.03.2019, 13:00
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try to draw a pentagon_fractal with turtle
###
import turtle

def pent_fractal(x=25):
    if x < 1:
        return
    for i in range(5):
        turtle.fd(x)
        turtle.right(72)
        pent_fractal(x/2.62)

def move_2_start():
    turtle.up()
    turtle.setx(-150)
    turtle.sety(200)
    turtle.down()

try:
    turtle.speed(0)
    turtle.clear()
    move_2_start()
    pent_fractal(300)
    input()
except:
    print("Script aborted")