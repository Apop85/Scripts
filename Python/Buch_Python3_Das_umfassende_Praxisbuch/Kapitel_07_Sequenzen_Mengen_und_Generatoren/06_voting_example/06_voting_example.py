#!/usr/bin/env python3
# -*- coding:utf-8 -*-
###
# File: 06_voting_example.py
# Project: Kapitel_07_Sequenzen_Mengen_und_Generatoren
# Created Date: Sunday 03.03.2019, 20:34
# Author: Apop85
# -----
# Last Modified: Monday 04.03.2019, 12:20
# -----
# Copyright (c) 2019 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
# -----
# Description: Example Chapter 7. Page 217. Create a list of parties and use the List to vote.
###
import os
os.chdir(os.path.dirname(__file__))

def create_chart_file(item_list):
    file_writer=open('.\\charts.txt', 'w')
    for i in range(len(item_list)):
        file_writer.write(str(i)+',0,'+item_list[i]+'\n')
    file_writer.close()

def get_candidates():
    file_reader=open('.\\charts.txt', 'r')
    lines=file_reader.readlines()
    file_reader.close()
    overall_list=[]
    for line in lines:
        overall_list.append([])
        for item in line.split(','):
            if item.isdecimal():
                overall_list[-1]+=[int(item)]
            else:
                overall_list[-1]+=[item.strip('\n')]
    return overall_list

def do_vote(chart_list):
    while True:
        print('Index'.center(15)+'|'+'Votes'.center(15)+'|'+'Partie'.center(15))
        print(''.center(47, '-'))
        for item in chart_list:
            for value in item:
                print(str(value).center(15), end='')
                if item.index(value) != len(item)-1:
                    print('|', end='')
            print()
        vote=input('Vote for your partie: ')
        if vote.isdecimal() and 0 <= int(vote) < len(chart_list):
            chart_list[int(vote)][1]+=1
        elif vote == '':
            return print_winner(chart_list)
        else:
            print('Invalid choice.')

def print_winner(chart_list):
    most_votes=[0,0]
    # Find the partie with the highes vote
    for i in range(len(chart_list)):
        if chart_list[i][1] > most_votes[1]:
            most_votes=[i,chart_list[i][1]]
    winner=(chart_list[most_votes[0]][-1],most_votes[1])
    # Check if there are other parties with the same amount of votes
    for i in range(len(chart_list)):
        if chart_list[i][1] == most_votes[1] and i != most_votes[0]:
            if type(winner[0]) == str:
                winner=[winner[0],chart_list[i][-1]],winner[1]
            else:
                winner[0]+=[chart_list[i][-1]]
    if winner[1] == 0:
        winner=('No winner',0)
    return winner

parties=['CVP','SVP','SP','FDP','Gruene','BDP','EVP']
create_chart_file(parties)
chart_list=get_candidates()
winner=do_vote(chart_list)
print('Winner is: '+str(winner[0])+' with '+str(winner[1])+' votes!')
