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
counter, timer_counter, pps, seconds_left = 0, 0, 0, 0
# Define Start value
current_number = 3
# Set Start timestamp
timestamp = time()
# Set amount of calculated prime numbers
amount_of_primes = 1000000
# Define bit counter
values = {"0": 0, "1": 0}


def isPrime(number):
    # Function to check if number is prime or not

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
print("AMOUNT OF PRIMES".center(30) + "PRIMES PER SECOND".center(30) + "MINUTES LEFT".center(30) + "DONE".center(30))

# Repeat until found 100 prime numbers
while counter < amount_of_primes:
    # Check if current number is a prime
    if isPrime(current_number):
        # If this is the 1000st iteration calculate pps and left time
        if timer_counter == 1000 and time() - timestamp != 0:
            pps = (1 / (time() - timestamp)) * 1000
            seconds_left = (amount_of_primes - counter)  / pps / 60

        # Calculate percentage done
        done = 100 / amount_of_primes * counter
        # Count second last bit
        values[bin(current_number)[-2]] += 1
        # Iterate prime counter 
        counter += 1
        # Ouput current numbers
        output = str(counter).center(30) + str(int(pps)).center(30) + str(int(seconds_left)).center(30) + (str(round(done, 2)) + "%").center(30)
        print(output, end="\r"*len(output)) 

        # Reset timer_counter after 1000 iterations
        if timer_counter == 1000:
            timestamp = time()
            timer_counter = 0
        else:
            timer_counter += 1
    # Iterate current number
    current_number += 1

print()
print(values)