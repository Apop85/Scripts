#!/bin/bash
# -*- coding:utf-8 -*-
###
 # ###
 #  File: 2.4.8 Zwischenaufgabe__Ausgaben_filtern.sh
 #  Project: scripts
 # -----
 #  Created Date: Thursday 16.01.2020, 19:11
 #  Author: Apop85
 # -----
 #  Last Modified: Thursday 16.01.2020, 19:19
 # -----
 #  Copyright (c) 2020 Apop85
 #  This software is published under the MIT license.
 #  Check http://www.opensource.org/licenses/MIT for further informations
 # -----
 #  Description:
 # ###
###

pfadname="$HOME/scripts/backup/backup1.sh"

clear
echo "Original                                      $pfadname"
echo "{pfadname%/*}                                ${pfadname%/*}"
echo "{pfadname%backup*}                                ${pfadname%backup*}"
echo "{pfadname%scripts*}                                ${pfadname%scripts*}"
echo "{pfadname%%backup*}                                ${pfadname%%backup*}"
echo "{pfadname%%scripts*}                                ${pfadname%%scripts*}"
echo ""
echo "{pfadname#*backup}                                ${pfadname#*backup}"
echo "{pfadname#*scripts}                                ${pfadname#*scripts}"
echo "{pfadname##*/}                                ${pfadname##*/}"
echo "{pfadname##*backup}                                ${pfadname##*backup}"
echo "{pfadname##*scripts}                                ${pfadname##*scripts}"
echo ""
