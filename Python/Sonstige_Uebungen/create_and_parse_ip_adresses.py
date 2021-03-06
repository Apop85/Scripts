#!/usr/bin/env python3
# -*- coding:utf-8 -*-

# 
####
# File: create_and_parse_ip_adresses.py
# Project: Sonstige_Uebungen
#-----
# Created Date: Saturday 16.01.2021, 16:54
# Author: Apop85
#-----
# Last Modified: Saturday 16.01.2021, 19:10
#-----
# Copyright (c) 2021 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description: Parsing string numbers to ip-addresses
####

def generateIp(startValue = 0):
    # Function to generate IPs out of integers
    for i in range(startValue, 255**4):
        o1 = i // 2 ** 24 % 256
        o2 = i // 2 ** 16 % 256
        o3 = i // 2 ** 8 % 256
        o4 = i % 256 % 256
        yield f"{o1}.{o2}.{o3}.{o4}"
    

def parseIp(ip, ips=[]):
    # Recursive function to generate valid IPs from strings with no pointations
    # Input:
    #   ip = string  | example: 1921681125
    # Output
    #   returnArray = Array | example: 192.168.112.5, 192.168.11.25, 192.168.1.125, 192.16.81.125, 19.216.81.125

    returnArray = []
    if len(ips) == 4:
        if ip != "":
            # Return empty array if there are numbers left
            return []
        # return IP-Address
        return [".".join(ips)]
    if not ip: 
        # return empty array if ip is not set
        return []

    # Check the first 3 values and check if the value is between 100 and 2^8
    if len(ip) > 2 and 99 < int(ip[:3]) < 256:
        # Analyze reduced ip
        returnArray += parseIp(ip[3:], ips + [ip[:3]])
    # Check the first 2 values and check if the value is between 9 and 100
    if len(ip) > 1 and int(ip[:2]) > 9:
        # Analyze reduced ip
        returnArray += parseIp(ip[2:], ips + [ip[:2]])

    # Analyze reduced ip
    returnArray += parseIp(ip[1:], ips + [ip[0]])
    return returnArray


startValue = 3232235776 # 3232235776 equals to 192.168.1.0
ipAmount = 5

ipAddrGenerator = generateIp(startValue)
for i in range(ipAmount):
    ip = next(ipAddrGenerator)
    stringToParse = ip.split(".")
    stringToParse = "".join(ip.split("."))
    parsed = parseIp(stringToParse)
    print("█" * (len(parsed[0]) + 4))
    for ip in parsed:
        print(f"█ {ip} █")
print("█" * (len(parsed[0]) + 4))
