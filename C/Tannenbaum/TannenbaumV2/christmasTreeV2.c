/*

####
# File: christmasTree.c
# Project: Modul 403
#-----
# Created Date: Monday 09.11.2020, 15:15
# Author: Apop85
#-----
# Last Modified: Monday 09.11.2020, 15:30
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Das Programm soll einen hohlen Tannenbaum ausgeben dessen maximale Breite vom User angegeben wird
####
*/

// Ignoriere Warnungen von scanf
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>

int main(void) {
	// Variabelinitierungen
	int maxWidth, minWidth, userChoice, maxWindowsSize, delta, stemWidth, stemHeight, innerWidth, i, j;
	char gelesen;
	// Variabeldeklarationen
	char treeSymbol = '^';
	minWidth = 5;
	maxWidth = 50;
	maxWindowsSize = 71;

	do {
		// userChoice einlesen
		printf("Geben Sie eine Zahl zwischen %d und %d ein: ", minWidth, maxWidth);
		// gelesen = scanf("%d", &userChoice);
		// gelesen = getchar();
	} while (userChoice < minWidth || userChoice > maxWidth);

	// Berechne Stammbreite
	stemWidth = userChoice / 10;
	// Stammbreite auf mind. 1 setzen
	if (stemWidth % 2 == 0 || stemWidth < 1) {
		stemWidth++;
	}
	// Berechne Stammh�he
	stemHeight = stemWidth + 1;

	// Wenn userChoice 
	if (userChoice % 2 == 0) {
		userChoice++;
	}

	for (i = 1; i <= userChoice; i += 2) {
		// Berechne Differenz zur L�nge der ausgegebenen Zeile und der maximalen Ausgabebreite
		delta = maxWindowsSize / 2 - i / 2;
		// Berechne den leeren Teil des Baumes
		innerWidth = i - 2;
		
		// Ist die innerWidth kleiner 1 setze auf 0 
		if (innerWidth < 1) {
			innerWidth = 0;
		}

		// Gebe Leerzeichen vor Baumform aus
		for (j = 1; j <= delta; j++) {
			printf(" ");
		}
		// Gebe erstes treeSymbol aus
		printf("%c", treeSymbol);

		if (i != userChoice) {
			// Ist es nicht die letzte Zeile gebe Leerzeichen aus
			for (j = 1; j <= innerWidth; j++) {
				printf(" ");
			}
		} else {
			// Ist es die letzte Zeile gebe treeSymbol aus
			for (j = 1; j <= innerWidth; j++) {
				printf("%c", treeSymbol);
			}
		}

		// Gebe hinteres treeSymbol aus, ausser in der ersten Zeile
		if (i > 1) {
			printf("%c", treeSymbol);
		}
		printf("\n");
	}

	// Gebe Baumstamm aus
	for (i = 1; i <= stemHeight; i++) {
		// Berechne Differenz zwischen maximaler Bildbreite und Stammbreite
		delta = maxWindowsSize / 2 - stemWidth / 2;
		// Gebe Leerzeichen vor Baumstamm aus
		for (j = 1; j <= delta; j++) {
			printf(" ");
		}
		// Gebe treeSymbol aus 
		for (j = 1; j <= stemWidth; j++) {
			printf("%c", treeSymbol);
		}
		printf("\n");
	}

	return 0;
}