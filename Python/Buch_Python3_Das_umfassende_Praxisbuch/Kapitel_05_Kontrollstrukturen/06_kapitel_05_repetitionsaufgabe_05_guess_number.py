#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 06_kapitel_05_repetitionsaufgabe_05_guess_number.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Saturday 23.02.2019, 22:44
# Author: Apop85
# -----
# Last Modified: Saturday 23.02.2019, 22:52
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will generate a random number and the youser have to guess it.
###

from random import randint as rng
random_number=rng(1,100)

print('I think about a number between 1 and 100. Which might it be?')
input_number=-1
while input_number != random_number:
    input_number=input('Tell me your guess: ')
    if not input_number.isdecimal():
        print('Nah... that\'s not a valid number')
        continue
    input_number=int(input_number)
    if 0 < input_number > 100:
        print('Eh... you know the numbers between 1 and 100? '+str(input_number)+' is not part of it...') 
    elif input_number < random_number:
        print('Too low...')
    elif input_number > random_number:
        print('Too high...')

print('You got the correct answer '+str(random_number)+'!')