#!/usr/bin/env python3
# -*- coding:utf-8 -*-
import sys
while True:
    msg = input("Nachricht eingeben: ")

    bits = bin(int.from_bytes(msg.encode('UTF16'), byteorder=sys.byteorder))
    bits_neu = ""

    for i in range (0, len(bits)):
        bits_neu += bits[len(bits)-1-i]
    bits_neu = str(bits)
    bits_neu = bits_neu.lstrip("b0")

    output = ""
    for i in range (0, len(bits_neu)-1):
        if (i)%8 == 0 and i != 0:
            output += " "
        else:
            output += bits_neu[i]

    print(bits)
    print(bits_neu.rstrip("b0"))
    print(output)
