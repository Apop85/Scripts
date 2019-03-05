#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 09_chapter_07_repetition_task_02_card_shuffle.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 05.03.2019, 16:46
# Author: Apop85
# -----
# Last Modified: Tuesday 05.03.2019, 17:15
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Task 3. The goal of this task is to simulate shuffling the cards
# of a scat card game. 
###
from random import randint as rng

def gen_card_deck():
    colors=['Cross','Pik','Heart','Caro']
    numbers=['7','8','9','10','Jack','Queen','King','Ace']
    card_deck=[(color,number) for color in colors for number in numbers]
    return card_deck

def shuffle_deck():
    card_deck=gen_card_deck()
    rng_range=rng(50,150)
    for i in range(rng_range):
        while True:
            random_1=rng(0,len(card_deck)-1)
            random_2=rng(0,len(card_deck)-1)
            if random_1 != random_2:
                break
        card_deck[random_1],card_deck[random_2]=card_deck[random_2],card_deck[random_1]
    return card_deck

deck=shuffle_deck()
print('Shuffled cards: ')
print(deck)

