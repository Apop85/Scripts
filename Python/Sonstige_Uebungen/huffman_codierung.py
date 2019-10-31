#!/usr/bin/env python3
# -*- coding:utf-8 -*-

####
# File: huffman_codierung.py
# Project: Sonstige_Uebungen
# -----
# Created Date: Wednesday 30.10.2019, 16:49
# Author: Apop85
# -----
# Last Modified: Thu Oct 31 2019
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Encode with huffman encoding
####

from copy import deepcopy as copy


def init():
    menu_items = {1: "Satz codieren", 0: "Beenden"}
    choice = create_menu(menu_items)
    if choice == 0:
        exit()
    elif choice == 1:
        print("\n"*5)
        data = get_data()
        encoded_data = encode_data(data)


def create_menu(menu_items):
    while True:
        print("█"*80)
        for key in menu_items.keys():
            item = str(key) + ". "+menu_items[key]
            item_lenght = len(item)
            if key != 0:
                print("█ "+" "*int(round((76-item_lenght)/2, 0)) +
                      item+" "*int((76-item_lenght)/2)+" █")
            else:
                print("█"*80)
                print("█ "+" "*int(round((76-item_lenght)/2, 0)) +
                      item+" "*int((76-item_lenght)/2)+" █")
                print("█"*80)
        choice = input(" "*30+"Auswahl: ")
        if choice.isdecimal() and int(choice) in menu_items.keys():
            return int(choice)
        else:
            print("░0"*40)
            print("Eingabe ungültig")
            print("░0"*40)


def get_data():
    print("█"*80)
    print("█"+"Zu codierenden Satz eingeben".center(78, " ")+"█")
    print("█"*80)
    data = input("Eingabe: ")
    return process_data(data)


def process_data(data):
    data_tree = {}
    for i in range(0, len(data)):
        data_tree.setdefault(data[i], 0)
        data_tree[data[i]] += 1
    processed_tree = {}
    for key in data_tree.keys():
        new_tree = copy(data_tree)
        del new_tree[key]
        value = data_tree[key]
        processed_tree.setdefault(value, [key])
        for new_key in new_tree.keys():
            if new_tree[new_key] == value and not new_key in processed_tree[value]:
                processed_tree[value] += [new_key]
    sorted_data = {}
    for key in sorted(processed_tree.keys()):
        sorted_data.setdefault(key, processed_tree[key])
    return sorted_data


def encode_data(data):
    characters = {}
    for key in data.keys():
        for character in data[key]:
            characters.setdefault(character, "")
    huffman_tree, characters = process_tree(data, characters)
    for key in sorted(list(huffman_tree.keys())):
        print(str(key)+str(huffman_tree[key]).center(100))
    for char in characters.keys():
        print(char, characters[char])
    # print(characters)


def process_tree(data, characters, rest_data=[], original={}, depth=-1, data_pyramid={}):
    depth += 1
    if depth == 0:
        original = copy(data)
    data_tree = {}
    current_key = list(data.keys())[0]
    if len(data[current_key]) != 0:
        data_pyramid.setdefault(current_key, copy(data[current_key]))

    if len(rest_data) > 0:
        last_sub_key = data[current_key][len(data[current_key])-1]
        new_data = rest_data[1]+last_sub_key
        data.setdefault(current_key+rest_data[0], [])
        # !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        # 0 oder 1 noch anfügen
        for key in characters.keys():
            # print(key, rest_data[1])
            if key in rest_data[1]:
                characters[key] += "1"
            elif key in last_sub_key:
                characters[key] += "0"
        # !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        data[current_key+rest_data[0]] += [new_data]
        del data[current_key][-1]
        rest_data = []

    for i in range(0, len(data[current_key]), 2):
        try:
            for character in characters.keys():
                if character in data[current_key][i]:
                    characters[character] += "1"
                elif character in data[current_key][i+1]:
                    characters[character] += "0"
            new_data = [data[current_key][i]+data[current_key][i+1]]
            if len(new_data) != 0:
                data_tree.setdefault(current_key*2, [])
                data_tree[current_key*2] += new_data
        except:
            rest_data = [current_key, data[current_key][i]]

    try:
        new_data = data_tree[current_key*2]
        data.setdefault(current_key*2, [])
        data[current_key*2] += new_data
    except:
        if len(data[current_key]) != 0:
            rest_data = [current_key, data[current_key][0]]
    del data[current_key]

    if len(data.keys()) != 0:
        data_pyramid, characters = process_tree(
            data, characters, rest_data, original, depth, data_pyramid)
    return data_pyramid, characters


init()
