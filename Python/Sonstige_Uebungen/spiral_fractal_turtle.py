#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: spiral_fractal_turtle.py
# Project: Sonstige_Uebungen
# Created Date: Thursday 28.02.2019, 13:39
# Author: Apop85
# -----
# Last Modified: Thursday 28.02.2019, 19:27
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Try to paint a spral fractal with turtle
###

from time import sleep
from turtle import *


def spiral_fractal(x=20, n=15, angle=1, w=10):
    cut_off=2
    decrease_rate=0.5
    pos,ang=position(), heading()
    width(w)
    set_color(w)
    if x < cut_off:
        return
    init_angle=angle
    for i in range(n):
        forward(round(x-(i/10),1))
        if round(x-(i/10),1) < cut_off:
            return
        angle+=1
        right(angle)
        if i%(n//2+1) == 0:
            spiral_fractal(x=x/1.5, n=int(n//1.5), angle=angle, w=w*decrease_rate)
            width(w)
            set_color(w)
        elif i == n-1:
            spiral_fractal(x=x/1.5, n=int(n//1.5), angle=angle, w=w*decrease_rate)
            width(w)
            set_color(w)
    angle=init_angle
    go_home(pos,ang,n)
    for i in range(n):
        forward(round(x-(i/10),1))
        if round(x-(i/10),1) < cut_off:
            return
        angle+=1
        left(angle)
        if i%(n//2+1) == 0:
            spiral_fractal(x=x/1.5, n=int(n//1.5), angle=angle, w=w*decrease_rate)
            width(w)
            set_color(w)
        elif i == n-1:
            spiral_fractal(x=x/1.5, n=int(n//1.5), angle=angle, w=w*decrease_rate)
            width(w)
            set_color(w)
    angle=init_angle
    go_home(pos,ang,n)
    
def go_home(pos,ang,n):
    up()
    setx(pos[0])
    sety(pos[1])
    setheading(ang)
    down()


def set_color(w):
    if w < 0.5:
        color('red')
    elif w < 2:
        color('blue')
    elif w < 5:
        color('#488A52')
    elif w == 10:
        color('black')
    else:
        color('brown')
    return            

def move_2_start():
    up()
    left(90)
    back(250)
    down()

try:
    clear()
    speed(0)
    move_2_start()
    spiral_fractal(angle=1)
    # spiral_fractal(angle=-1)
    input()
except:
    print("Script aborted")