#!/bin/bash

# Script zum vergleichen zweier Files
readonly GREEN=$'\e[92m'
readonly RED=$'\e[91m'
readonly YELLOW=$'\e[93m'
readonly NOCOLOR=$'\e[0m'
readonly BOLD=$'\e[1m'


function check_vars {
	if [ "$1" == "" ]; then
		echo "Enter /path/to/${RED}File1${NOCOLOR}:"
		read file1
		echo "Enter /path/to/${RED}File2${NOCOLOR}:"
		read file2
	elif [ "$1" != "" -a "$2" == "" ]; then
		echo "Found File 1: ${GREEN}$1${NOCOLOR}, Enter /path/to/${RED}File2${NOCOLOR}:"
		file1=$1
		read file2
	else
		echo "Files found: ${GREEN}$1 - $2${NOCOLOR}"
		file1=$1
		file2=$2
	fi
	echo ""
}

function find_difference {
	echo "${BOLD}DIFFERENCE ANALYSIS:${NOCOLOR}"
	echo "--------------------"
	diff_analysis=$(diff -q $file1 $file2)
	echo "${YELLOW}$diff_analysis${NOCOLOR}"
	echo ""
	if [[ $diff_analysis == *"differ"*  ]]; then
		echo "${RED}DIFFERENCES FOUND:${NOCOLOR}"
		echo "${RED}--------------------${NOCOLOR}"
		differences=$(diff $file1 $file2)
		echo "${YELLOW}$differences${NOCOLOR}"
	else
		echo "${GREEN}Files are identical${NOCOLOR}"
	fi
	 echo "=========================="
}

check_vars $1 $2
echo "Files: $file1 : $file2"
find_difference $file1 $file2
