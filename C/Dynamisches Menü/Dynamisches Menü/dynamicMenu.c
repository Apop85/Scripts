/*
####
# File: dynamicMenu.c
# Project: dynamic Menu
#-----
# Created Date: Tuesday 24.12.2020, 16:40
# Author: Apop85
#-----
# Last Modified: Tuesday 24.12.2020, 17:21
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Funktion f�r die Dynamische Ausgabe eines Men�s und die Auswertung der Eingabe
####
*/

// Konstantendeklaration
#define _CRT_SECURE_NO_WARNINGS

// Modulimport
#include <stdio.h>
#include <stdbool.h>
#include <string.h>


int printMenu(char menuArray[][55], int amountOfItems) {
	// Funktion zur Ausgabe des Men�s und Auswertung der Auswahl
	// Variabeldeklaration
	int i, itemNo, choice, itemLength;
	bool firstRun = true;
	
	// Wiederhole solange keine g�ltige Antwort gegeben wurde
	do {
		// Ist das nicht das erste mal, dass das Men� angezeigt wird, zeige Fehlermeldung an
		if (!firstRun) {
			printf("Eingabe \"%i\" ist ungueltig!\n", choice);
		}
		printf("###################################################\n");
		// Ausgabe der Men�-Items
		for (itemNo = 0; itemNo < amountOfItems; itemNo++) {
			printf("%20d. ", itemNo + 1);
			itemLength = sizeof(menuArray[itemNo]);
			for (i = 0; i < itemLength; i++) {
				printf("%c", menuArray[itemNo][i]);
			};
			printf("\n");
		};
		printf("###################################################\n");
		printf("Auswahl: ");
		scanf("%i", &choice);
		while (getchar() != '\n');
		firstRun = false;
	} while (choice <= 0 || choice > amountOfItems);

	// Gebe Auswahl zur�ck
	return choice;
}

int main(void) {
	// Variabeldeklaration
	char menu[5][55] = { "Oeffnen", "Speichern", "Rueckgaengig", "Loeschen", "Beenden" };
	int amountOfItems = sizeof(menu) / sizeof(menu[0]);
	// Aufrufen der printMenu-Funktion und speichern des R�ckgabewerts
	int choice = printMenu(menu, amountOfItems);
		
	printf("Die Auswahl \"%i\" ist valid: %s", choice, menu[choice-1]);
	
	return 0;
}