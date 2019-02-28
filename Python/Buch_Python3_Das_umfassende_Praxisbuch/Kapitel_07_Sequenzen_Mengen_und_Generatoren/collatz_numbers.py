#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: collatz_numbers.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Thursday 28.02.2019, 21:06
# Author: Apop85
# -----
# Last Modified: Thursday 28.02.2019, 21:18
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Calculating the Collatz sequence for the range of 10**n to 10**n+1
###


def collatz(n):
    highscore=[0,0]
    for i in range(10**n,10**(n+1)):
        counter=0
        n=i
        while n != 1:
            if n % 2 == 0:
                n//=2
            else:
                n=n*3+1
            counter+=1
        if counter > highscore[1]:
            highscore=[i,counter]
        # print(str(i)+': '+str(counter))
    print('Highscore: '+str(highscore[0])+' mit '+str(highscore[1])+' Berechnungen')

def input_number():
    factorial=input('Choose the factor of 10: ')
    if factorial.isdecimal():
        return int(factorial)

while True:
    n=input_number()
    collatz(n)