#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 09_chapter_06_repetition_task_2_encrypt.py
# Project: Kapitel_06_Funktionen
# Created Date: Sunday 24.02.2019, 14:40
# Author: Apop85
# -----
# Last Modified: Sunday 24.02.2019, 14:45
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Page 190. Task 2. This script will encrypt a message by adding a random amount
# of characters to each character.
###
from random import randint as rng

def encrypt_message(message, delta=rng(1,5)):
    alphabet, encrypted='qwertzuiopasdfghjklyxcvbnm', ''
    for letter in message.upper():
        encrypted+=letter
        for i in range(delta):
            encrypted+=alphabet[rng(0,len(alphabet)-1)].upper()
    return encrypted

message=input('Enter your message to encrypt: ')
encrypted_message=encrypt_message(message)

print(encrypted_message)
