#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: bin2csv.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Sunday 09.02.2020, 16:47
# Author: Raffael Baldinger
#-----
# Last Modified: Sunday 09.02.2020, 16:48
#-----
# Copyright (c) 2020 Raffael Baldinger
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
####

filename=r"C:\Users\rbald\Desktop\Values.csv"
file_writer=open(filename, "w", encoding="UTF-8")

for i in range(0,256):
    new_line=str(i)+";"+str(f'{i:08b}')+'\n'
    print(new_line)
    file_writer.write(new_line)

file_writer.close()

