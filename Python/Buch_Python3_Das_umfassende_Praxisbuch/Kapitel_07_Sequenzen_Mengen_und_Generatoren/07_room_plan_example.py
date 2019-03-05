#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 07_room_plan_example.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Tuesday 05.03.2019, 15:40
# Author: Apop85
# -----
# Last Modified: Tuesday 05.03.2019, 16:15
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Chapter 7. Page 228.. Example of comparing and concatinate two '''mengen'''  
###


def find_next_room(plan, room):
    doors=plan[1]
    # Output of any tuple including the room number.
    doors_of_room=set(door for door in doors if room in door)
    neighbor_rooms=set()
    for door in doors_of_room:
        # Remove any room with no connection to the searched one.
        neighbor_rooms=neighbor_rooms|door
    # remove searched room from list and return the rest
    return neighbor_rooms-set([room])


def concat_plans(plan_1, plan_2):
    # concat plans into 1 plan removing duplicates
    concat_rooms=plan_1[0] | plan_2[0]
    concat_doors=plan_1[1] | plan_2[1]
    return (concat_rooms, concat_doors)

# Definition of rooms
plan_1_rooms={1,2,3,4,5}
plan_1_doors=set(frozenset(t) 
                for t in [(1,2),(2,3),(2,4),(3,4),(4,5)])
plan_1=(plan_1_rooms, plan_1_doors)

plan_2_rooms={4,5,6,7}
plan_2_doors=set(frozenset(t) 
                for t in [(5,4),(5,6),(5,7),(6,7)])
plan_2=(plan_2_rooms, plan_2_doors)

# Get results
one_plan=concat_plans(plan_1, plan_2)
neighbor=find_next_room(one_plan,2)

# Output
print()
print('Rooms:')
for item in one_plan[0]:
    print(item, end=' ')
print()
print('Door locations:')
for item in one_plan[1]:
    print(tuple(item), end=' ')
print()
print('Neighbors of room 2:')
for item in neighbor:
    print(item, end=' ')