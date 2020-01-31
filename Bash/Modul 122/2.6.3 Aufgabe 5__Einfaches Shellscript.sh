#!/bin/bash
# -*- coding:utf-8 -*-
###
 # ###
 #  File: 2.6.3 Aufgabe 5__Einfaches Shellscript.sh
 #  Project: scripts
 # -----
 #  Created Date: Thursday 16.01.2020, 18:02
 #  Author: Apop85
 # -----
 #  Last Modified: Thursday 16.01.2020, 18:16
 # -----
 #  Copyright (c) 2020 Apop85
 #  This software is published under the MIT license.
 #  Check http://www.opensource.org/licenses/MIT for further informations
 # -----
 #  Description:
 # ###
###

# Defniere Konstanten
readonly USERHOME="/mnt/c/Users/rbald"
readonly TARGET_DIR="/Test"

# Wechsel zu Userhome und erstelle Ordner "Test"
cd $USERHOME
mkdir $TARGET_DIR

# Wechsle zu neuem Verzeichnis und kopiere /etc/hosts ins aktuelle Verzeichnis
cd $TARGET_DIR
cp /etc/hosts ${USERHOME}${TARGET_DIR}

# Zeige Ordnerinhalt an
ls -la
