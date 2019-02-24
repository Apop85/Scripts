#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 08_chapter_06_repetition_task_1_koncat.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 14:17
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 14:19
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 190. Task 1. This script wil concatinate as many strings as aviable.
###

def concat(*args):
    return ''.join(args)

print(concat("Test","Me"))