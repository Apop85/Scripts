#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 06_recursive_functions_with_turtle_sierpinsky_triangle.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 13:09
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 13:31
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Example of chapter 6 page 180. Drawing a Sierpinsky triangle with recursive 
# functions and turtle
###

from turtle import *

def goto_start():
    up()
    forward(250)
    right(90)
    forward(250)
    right(90)
    down()

def sierpinsky(x):
    if x < 3:
        return
    for i in range(3):
        forward(x)
        right(120)
        sierpinsky(x/2)

clear()
speed(500)

try:
    goto_start()
    sierpinsky(500)
    hideturtle()
    input('Enter to end script.')
except Exception as error_message:
    print('Script aborted! '+str(error_message))