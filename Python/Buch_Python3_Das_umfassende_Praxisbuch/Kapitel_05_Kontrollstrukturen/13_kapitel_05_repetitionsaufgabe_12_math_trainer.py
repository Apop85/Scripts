#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 13_kapitel_05_repetitionsaufgabe_12_math_trainer.py
# Project: Kapitel_05_Kontrollstrukturen
# Created Date: Sunday 24.02.2019, 10:26
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 10:52
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Task 12. Page 147. This script will output a random calculation and measures
# the time the user needed to solve 6 tasks.
###

from time import time
from random import randint as rng

min_number=2
max_number=100

operators=["+","-","*"]
total_time=0

for i in range(6):
    random_operator=operators[rng(0,len(operators)-1)]
    random_calculation=str(rng(min_number,max_number))+random_operator+str(rng(min_number,max_number))
    if len(random_calculation.split("+")) > 1:
        result=int(random_calculation.split("+")[0]) + int(random_calculation.split("+")[1])
    elif len(random_calculation.split("-")) > 1:
        result=int(random_calculation.split("-")[0]) - int(random_calculation.split("-")[1])
    elif len(random_calculation.split("*")) > 1:
        result=int(random_calculation.split("*")[0]) * int(random_calculation.split("*")[1])
    no_answer=True
    time_start=time()
    while no_answer:
        try:
            guess=int(input(random_calculation+' = '))
            if guess == result:
                total_time+=time()-time_start
                no_answer=False
            else:
                print('This answer was not correct. Try again.')
        except:
            print('Input is no valid number!')

print('Time needed to complete all 6 tasks: '+str(round(total_time,2))+' seconds.')