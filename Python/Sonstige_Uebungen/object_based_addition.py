#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: object_based_addition.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Saturday 16.01.2021, 13:45
# Author: Apop85
#-----
# Last Modified: Saturday 16.01.2021, 16:07
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Creates a specific object structure from an integer where each digit references the next digit
#              Example: 1234  => 4 -> 3 -> 2 -> 1 
#              
#              This object structure is used afterwards to calculate the sum of both input numbers out of this structure
####

value1 = 324
value2 = 9676

class Node(object):
    # Class to create the initial structure

    def __init__(self, value):
        # Recursive constructur
        # Input:
        #   value = integer
        #   example: value = 1234
        # Output:
        #   example: 4 -> 3 -> 2 -> 1

        # Set current value to the last digit
        self.value = int(str(value)[-1])
        if len(str(value)) > 1:
            # Add recursive next-values
            self.next = Node(int(str(value)[:-1]))
        else:
            self.next = None




class Add:
    # Class to add structured values

    def add(self, node1, node2, exponent=0, switch=False):
        # Recursive function to calculate the result where the recursive depth indicates the exponent
        if not switch:
            # If both nodes still have values calculate both together
            result = node1.value * 10**exponent + node2.value * 10**exponent
        else:
            # Calculate only the first node
            result = node1.value * 10**exponent

        # Check if more recursion steps are needed
        if node1.next != None and node2.next != None:
            result += self.add(node1.next, node2.next, exponent=exponent+1)
        # If one node has no more values set the calculation switch to true and use the still useable node as node1
        elif node1.next != None:
            result += self.add(node1.next, node2, exponent=exponent+1, switch=True)
        elif node2.next != None:
            result += self.add(node2.next, node1, exponent=exponent+1, switch=True)

        return result



n1 = Node(value1)
n2 = Node(value2)

result = Add().add(n1, n2)
print(result)