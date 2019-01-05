#!/bin/bash
#Dieses simple Script dient zur aktualisierung der PiHole-Blocklisten und wird regelmässig ausgeführt

readonly RUNLOG=$HOME/scriptexec.log
touch $RUNLOG

pihole updateGravity

echo "$(date +"%d.%m.%Y - %H:%M:%S")" updatepiholelist.sh ausgeführt >> $RUNLOG

exit 0
