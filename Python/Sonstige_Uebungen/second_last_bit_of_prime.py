#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: second_last_bit_of_prime.py
# Project: Python
#-----
# Created Date: Monday 11.01.2021, 20:46
# Author: Apop85
#-----
# Last Modified: Monday 11.01.2021, 21:52
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Show amount of 1 and 0 in the last bit of the first million prime numbers
####

from math import sqrt
from time import time

# Initiate counter values
counter, timer_counter, pps, seconds_left, max_pps = 0, 0, 0, 0, 0
min_pps = 10000000
# Define Start value
current_number = 2
# Set Bit to detect
#   1 = 1001001[0]
#   2 = 100100[1]0
#   3 = 10010[0]10
detectedBit = 2
# Set Start timestamp
timestamp = time()
starttime = timestamp
# Set amount of calculated prime numbers
amount_of_primes = 1000000
# Define bit counter
values = {"0": 0, "1": 0}

print("Task         : Calculate prime numbers")
print("Start value  : {}".format(current_number))
print("Target amount: {}".format(amount_of_primes))
print("Bit to detect: {}".format(detectedBit))
print("-------------------------------------")

def isPrime(number):
    # Function to check if number is prime or not

    if number == 2:
        return True

    if str(bin(number)).endswith("0"):
        return False

    # The max value of a divisor can't be greater than the square root of the number
    max_value = sqrt(number)

    # Iterate trough each number up to max_value
    for i in range(2, int(max_value)+1):
        if number % i == 0:
            # If the modulo of number and counter is 0 it can't be a prime
            return False
    return True

# Print header
# print("AMOUNT OF PRIMES".center(30) + "PRIMES PER SECOND".center(30) + "MINUTES LEFT".center(30) + "DONE".center(30))
amount_before = 0

# Repeat until found 100 prime numbers
while counter < amount_of_primes:
    # If this is the 1000st iteration calculate pps and left time
    if timer_counter == 1000 and time() - timestamp != 0:
        amount_delta = counter - amount_before
        amount_before = counter
        pps = amount_delta / (time() - timestamp)
        if min_pps > pps:
            min_pps = pps
        if max_pps < pps:
            max_pps = pps
        seconds_left = (amount_of_primes - counter)  / pps / 60
    # Check if current number is a prime
    if isPrime(current_number):

        # Calculate percentage done
        done = 100 / amount_of_primes * counter
        # Count second last bit
        values[bin(current_number)[-detectedBit]] += 1
        # Iterate prime counter 
        counter += 1
        # Ouput current numbers
        # output = str(counter).center(30) + str(int(pps)).center(30) + str(int(seconds_left)).center(30) + (str(round(done, 2)) + "%").center(30)
        # print(output, end="\r"*len(output)) 

    # Reset timer_counter after 1000 iterations
    if timer_counter == 1000:
        timestamp = time()
        timer_counter = 0
    else:
        timer_counter += 1
    # Iterate current number
    current_number += 1

current_number -= 1
# print()
print("Amount of 0  : {}".format(values["0"]))
print("Amount of 1  : {}".format(values["1"]))
print("Min PPS      : {}".format(int(min_pps)))
print("Max PPS      : {}".format(int(max_pps)))
print("Last prime   : {}".format(current_number))
print("Needed time  : {} sec".format(int((time() - starttime))))