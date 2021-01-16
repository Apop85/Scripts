#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: rearrange_string.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Saturday 16.01.2021, 11:52
# Author: Apop85
#-----
# Last Modified: Saturday 16.01.2021, 13:19
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Get a random string and rearrange it so no identical character i written next to each other
####

from random import choice as randomChoice

# Set default values
charAmount = 100
useableChars = "abcdefghijklmnopqrstuvwxyz"


def createRandomString(amount, chars, string=""):
    # Function to create a random string
    # Inputs:
    #   amount = integer | Defines the target length of the random string
    #   chars = string   | Defines the useable characters for the random string

    for i in range(charAmount):
        string += randomChoice(useableChars)

    return string

def solutionPossible(dictionary, string):
    # Function to check if the string is rearrangeable
    # Inputs:
    #   dictionary = dictionary  | Dictionary with the characters and values

    # Set counters
    zeroCounts = 0
    nonZeroCounts = 0
    character = ""
    for key in dictionary.keys():
        if dictionary[key] == 0:
            zeroCounts += 1
        else:
            nonZeroCounts += 1
            character = key
    
    if zeroCounts + 1 == len(dictionary) and string[-1] == character:
        # If the amount of zeroes + 1 equals the length of the dictionary 
        # and the last character is the same as the one left there is no solution
        return False
    else:
        return True


def countChars(string, returnDict={}):
    # Function to count the characters from input string
    # Inputs:
    #   string = string    | String to count characters

    for char in string:
        if not char in returnDict.keys():
            returnDict.setdefault(char, 0)
        returnDict[char] += 1
    
    return returnDict

def sortDict(inputDict):
    # Function to sort a Dictionary by values
    # Input:
    #   inputDict = Dictionary   | Unsorted Dictionary to be sorted

    outputDict={}
    # Get reverse sort of inputDict values
    sortedValues = sorted(inputDict.values(), reverse=True)
    # Create return dictionary sorted from highest value to lowest
    for value in sortedValues:
        for key in inputDict.keys():
            if inputDict[key] == value and not key in outputDict.keys():
                outputDict.setdefault(key, value)
                break

    return outputDict

def rearrangeString(sortedDict, outputString=""):
    # Function to create the rearranged string
    # Inputs: 
    #   sortedDict = Dictionary | Dictionary sorted by the highest values


    while len(outputString) < charAmount:
        for key in sortedDict.keys():
            if sortedDict[key] > 0:
                if len(outputString) == 0:
                    # Insert first character into string
                    outputString += key
                    sortedDict[key] -= 1
                    # Sort again by values
                    sortedDict = sortDict(sortedDict)
                    break
                elif outputString[-1] != key:
                    # If last char not equal current key, insert key into string
                    outputString += key
                    sortedDict[key] -= 1
                    sortedDict = sortDict(sortedDict)
                    break

        if not solutionPossible(sortedDict, outputString):
            print("Solution not possible!")
            break

    return outputString


randomString = createRandomString(charAmount, useableChars)
print("Random string    : {}".format(randomString))
charDict = countChars(randomString)
sortedDict = sortDict(charDict)
print("Rearranged String: {}".format(rearrangeString(sortedDict)))