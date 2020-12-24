/*

####
# File: workWithStrings.c
# Project: Modul 403
#-----
# Created Date: Monday 23.11.2020, 09:30
# Author: Apop85
#-----
# Last Modified: Monday 23.11.2020, 10:00
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Eingeben, Zusammenf�hren, Manipulieren und Auswerten von Strings
####
*/


#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <string.h>
#include <ctype.h>


int main(void) {
	// Variabeldeklaration
	int stringSize, i, matches;
	int totalMatches = 0;
	char stringVariable1[255], stringVariable2[255], stringVariable3[255], searchFor;
	int comparison;

	// Usereingabe
	printf("\nBitte ersten String eingeben: ");
	// String ben�tigt kein "&"-Zeichen f�r die Zuweisung des Zeichens
	scanf("%s", stringVariable1);
	while (getchar() != '\n');
	
	printf("Bitte zweiten String eingeben: ");
	scanf("%s", stringVariable2);
	while (getchar() != '\n');

	printf("Nach welchem Zeichen soll gesucht werden? ");
	scanf("%c", &searchFor);
	while (getchar() != '\n');

	// Kopiere Inhalt von erstem String in dritten String
	strcpy(stringVariable3, stringVariable1);

	// L�nge des Strings ermitteln
	stringSize = strlen(stringVariable1);
	printf("\nDer erste String ist %d Zeichen lang\n", stringSize);
	stringSize = strlen(stringVariable2);
	printf("Der zweite String ist %d Zeichen lang\n\n", stringSize);


	// Stringconcatination
	strcat(stringVariable1, stringVariable2);
	printf("Stringzusammensetzung:\n%s", stringVariable1);

	// Stringvergleiche
	printf("\n\nVergleich von Strings: \n");
	comparison = strcmp(stringVariable3, stringVariable2);
	printf("strcmp hat folgendes ergeben: %d\n", comparison);

	// strcmp == 0	--> Strings sind identisch
	// strcmp > 0	--> Erster String hat h�here Summe
	// strcmp < 0	--> Erster String hat geringere Summe
	if (strcmp(stringVariable3, stringVariable2) == 0) {
		printf("\"%s\" und \"%s\" sind identisch.\n", stringVariable3, stringVariable2);
	} else {
		printf("Die Strings \"%s\" und \"%s\" unterscheiden sich.\n", stringVariable3, stringVariable2);
	}


	// Invertiere Gross- Kleinschreibung
	char inverted[255] = "";
	stringSize = strlen(stringVariable1);
	for (i = 0; i < stringSize; i++) {
		// Ist aktueller Buchstabe ein Grossbuchstabe?
		if (stringVariable1[i] == toupper(stringVariable1[i])) {
			// Wandle in Kleinbuchstabe um
			inverted[i] = tolower(stringVariable1[i]);
		} else {
			// Wandle in Grossbuchstabe um
			inverted[i] = toupper(stringVariable1[i]);
		}
	}
	printf("\nInvertierte Ausgabe:\n%s\n", inverted);
	// Umwandeln in Gross- & Kleinbuchstaben
	printf("Alles Grossbuchstaben:\n%s\n", _strupr(stringVariable1));
	printf("Alles Kleinbuchstaben:\n%s\n", _strlwr(stringVariable1));



	// Nach bestimmtem Zeichen suchen
	stringSize = strlen(stringVariable3);
	matches = 0;
	for (i = 0; i < stringSize; i++) {
		if (stringVariable3[i] == searchFor) {
			matches++;
		}
	}
	printf("\nZaehle vorkommen von %c:\n", searchFor);
	printf("Im ersten String kommt %c %d mal vor.\n", searchFor, matches);

	totalMatches += matches;
	stringSize = strlen(stringVariable2);
	matches = 0;
	for (i = 0; i < stringSize; i++) {
		if (stringVariable2[i] == searchFor) {
			matches++;
		}
	}
	totalMatches += matches;
	printf("Im zweiten String kommt %c %d mal vor.\n", searchFor, matches);
	printf("Insgesamt kam %c %d mal vor.\n", searchFor, totalMatches);

}
