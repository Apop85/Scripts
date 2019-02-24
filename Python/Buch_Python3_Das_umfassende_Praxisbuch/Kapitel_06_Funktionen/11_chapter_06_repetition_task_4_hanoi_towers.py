#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 11_chapter_06_repetition_task_4_hanoi_towers.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 15:09
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 16:24
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 191. Task 4. This script will solve the game Hanoi Towers with recursive
# functions.
###

def create_tower(pieces_amount, start_tower):
    global towers, setup
    if not setup:
        towers={1:[], 2:[], 3:[]}
        for i in range(pieces_amount):
            towers[start_tower]+=[i+1]
        print('Start condition:')
        print(str(towers[1])+'\t'+str(towers[2])+'\t'+str(towers[3]))
        print()
        setup=True

def move_tower(pieces_amount=3, start_tower=1, target_tower=2, temp_tower=3):
    global towers
    create_tower(pieces_amount, start_tower)
    if pieces_amount == 1:
        print('Move from '+str(start_tower)+' to '+str(target_tower))
        towers[target_tower].insert(0, towers[start_tower][0])
        del towers[start_tower][0]
        print(str(towers[1])+'\t'+str(towers[2])+'\t'+str(towers[3]))
        print()
    else:
        move_tower(pieces_amount-1, start_tower, temp_tower, target_tower)
        move_tower(1, start_tower, target_tower, temp_tower)
        move_tower(pieces_amount-1, temp_tower, target_tower, start_tower)
     

setup=False
move_tower(3,1,3,2)
