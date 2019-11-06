#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: write_bits_to_file.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Friday 01.11.2019, 21:22
# Author: Apop85
#-----
# Last Modified: Saturday 02.11.2019, 22:47
#-----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Write binary data to file
####

import struct
import re
import os
# import shelve
from array import array
import json

os.chdir(os.path.dirname(__file__))
tree = {1: ['i', 'l', 'a', 'u', 'n', 'e', 'r'], 2: ['il', 'c', 'au', 'h', 'ne', ' ', 't'], 3: ['rt', 'm'], 4: ['ilc', 'auh', 'ne '], 6: ['rtm'], 8: ['ilcauh'], 10: ['ne rtm'], 18: ['ilcauhne rtm']}
string = json.dumps(tree)
binary = ' '.join(format(ord(letter), 'b') for letter in string)

test_bits = "000111111101101000100001011110100001001000010100111001001100011"
pattern = re.compile(r'\d{1,8}')
octects = re.findall(pattern, test_bits)

bin_array = array("B")
for octect in re.findall(pattern, test_bits):
    bin_array.append(int(octect[::-1], 2))

binary = binary.split(" ")
for b in binary:
    bin_array.append(int(b[::-1], 2))
with open("test.bin", "wb") as filename:
    filename.write(bytes(bin_array))