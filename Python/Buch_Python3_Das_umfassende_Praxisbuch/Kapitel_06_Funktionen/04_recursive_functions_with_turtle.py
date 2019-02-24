#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 04_recursive_functions_with_turtle.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 12:33
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 12:45
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 6. Page 176. This script uses the turtle module to draw a spiral
# with a recursive function.
###

from turtle import *

def spirale(length):
    forward(distance=length)
    right(90)
    if length > 1:
        spirale(0.9*length) # launch recursive function.
    return

clear()
spirale(200)