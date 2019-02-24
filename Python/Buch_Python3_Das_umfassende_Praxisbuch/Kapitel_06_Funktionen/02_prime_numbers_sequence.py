#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 02_prime_numbers_sequence.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 11:29
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 11:58
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: This script will output all prime numbers between two user set values
###

from math import sqrt

def user_input():
    while True:
        min_value=input('Enter the start value: ')
        max_value=input('Enter the end value: ')
        if (min_value.isdecimal() and max_value.isdecimal()) and (0 < int(min_value) < int(max_value)):
            return (int(min_value),int(max_value))

def get_prime_numbers(sequence, prime_list=[]):
    for i in range(sequence[0],sequence[1]):
        if i < 10:
            target_range=i
        else:
            target_range=int(sqrt(i))+1
        for j in range(2,target_range):
            if i % j == 0:
                break
            elif j == target_range-1:
                prime_list+=[i]
    return prime_list
            
def output_results(result_list,sequence):
    print('~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~')
    print('Prime numbers between '+str(sequence[0])+' and '+str(sequence[1]))
    print('~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~')
    for prime_number in result_list:
        print(prime_number, end=' ')

sequence=user_input()
results=get_prime_numbers(sequence)
output_results(results,sequence)