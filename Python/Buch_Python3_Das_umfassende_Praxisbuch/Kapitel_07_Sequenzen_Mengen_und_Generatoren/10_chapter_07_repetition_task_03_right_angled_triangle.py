#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 10_chapter_07_repetition_task_03_right_angled_triangle.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Wednesday 06.03.2019, 11:45
# Author: Apop85
# -----
# Last Modified: Wednesday 06.03.2019, 11:50
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Find all right-angled triangles between a side length of 1 and 20
###

triangles=[(a,b,c) for a in range(1,21) for b in range(1,21) for c in range(1,21) if (a**2+b**2) == (c**2)]
for right_angled_triangle in triangles:
    print(right_angled_triangle)
