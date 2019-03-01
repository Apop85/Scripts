#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: collatz_numbers.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Thursday 28.02.2019, 21:06
# Author: Apop85
# -----
# Last Modified: Thursday 28.02.2019, 23:24
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Calculating the Collatz sequence for the range of 10**n to 10**n+1
###


def collatz(n1=1,n2=10, step=10000, length=0):
    if step < 1:
        return
    highscore=[0,0]
    values=[]
    for i in range(n1,n2,step):
        counter=0
        n=i
        while n != 1:
            if n % 2 == 0:
                n//=2
            else:
                n=n*3+1
            counter+=1
        values+=[(counter,i)]
        if counter > highscore[1]:
            if length > 0:
                print('\r'*(length+10), end='')
            highscore=[i,counter]
            string='Current highscore: '+str(highscore[0])+' with '+str(highscore[1])+' calculations.'
            length=len(string)
            print(string, end='')
    values.sort()
    collatz(n1=values[len(values)//2][1]//2, n2=n2, step=step//10, length=length)
    print('\r'*(length+10), end='')

    print('Highscore: '+str(highscore[0])+' with '+str(highscore[1])+' calculations.'+' '*50)
    # values.sort()
    # print(values[-2:])

def input_number():
    while True:
        factorial=input('Choose the factor of 10**')
        if factorial.isdecimal():
            return int(factorial)

def prepare_collatz(n):
    return 10**n, 10**(n+1)

while True:
    n=input_number()
    n1,n2=prepare_collatz(n)
    collatz(n1,n2,step=10**(len(str(n1))-2))