
/*

####
# File: forLoop.c
# Project: Modul 403
#-----
# Created Date: Monday 26.10.2020, 10:32
# Author: Apop85
#-----
# Last Modified: Monday 26.10.2020, 10:32
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Loop-�bungen
####
*/

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>

int main(void) {
	int i, gelesen;
	// Aufgabe 1: For-Loop von 1 bis 10
	for (i = 1; i <= 10; i++) {
		printf("%d\n", i);
	}


	//Aufgabe 2: Manuelle Angabe der Start- und Endzahl
	int max, min;
	// Dateneingabe
	printf("Bitte Startzahl eingeben: ");
	gelesen = scanf("%d", &min);
	gelesen = getchar();
	printf("\nBitte Endzahl eingeben:   ");
	gelesen = scanf("%d", &max);
	gelesen = getchar();

	// Pr�fe ob die erste Zahl gr�sser als die zweite ist
	if (min <= max) {
		// Iteriere positiv
		for (i = min; i <= max; i++) {
			printf("%d\n", i);
		}
	}
	else {
		// Iteriere negativ
		for (i = min; i >= max; i--) {
			printf("%d\n", i);
		}
	}


	//Aufgabe 3: Manuelle Eingabe der Schrittgr�sse
	// Initialisierung
	int stepsize;

	// Dateneingabe
	printf("\nBitte Startzahl eingeben: ");
	gelesen = scanf("%d", &min);
	gelesen = getchar();
	gelesen = printf("\nBitte Endzahl eingeben:   ");
	gelesen = scanf("%d", &max);
	gelesen = printf("\nBitte Schrittgr�sse eingeben: ");
	gelesen = scanf("%d", &stepsize);

	// Pr�fe ob stepsize gr�sser oder kleiner 0 ist
	if (stepsize < 0) {
		// Wandle in positiven Wert um
		stepsize = stepsize * -1;
	}
	else if (stepsize == 0) {
		// Setze mindest
		stepsize = 1;
	}

	if (min <= max) {
		for (i = min; i <= max; i = i + stepsize) {
			printf("%d\n", i);
		}
	}
	else {
		for (i = min; i >= max; i = i - stepsize) {
			printf("%d\n", i);
		}
	}

	//Aufgabe 4: Endloser Loop
	for (i = 0; i < 1; i = i) {
		char yesno;
		printf("\nBitte Startzahl eingeben: ");
		gelesen = scanf("%d", &min);
		gelesen = getchar();

		gelesen = printf("\nBitte Endzahl eingeben:   ");
		gelesen = scanf("%d", &max);

		gelesen = printf("\nBitte Schrittgr�sse eingeben: ");
		gelesen = scanf("%d", &stepsize);

		if (stepsize < 0) {
			stepsize = stepsize * -1;
		}

		if (min <= max) {
			for (i = min; i <= max; i = i + stepsize) {
				printf("%d\n", i);
			}
		}
		else {
			for (i = min; i >= max; i = i - stepsize) {
				printf("%d\n", i);
			}
		}

		printf("\nErneuter durchgang? [Y/n] ");
		gelesen = scanf("%c", &yesno);
		gelesen = getchar();

		if (yesno == "n" || yesno == "N") {
			break;
		}
		else if (yesno != "y" || yesno != "Y") {
			printf("Antwort nicht erkannt");
		}
	}
	
	//Aufgabe 5: While Loop
	i = 65;
	while (i <= 122) {
		if (i < 91 || i > 96) {
			printf("%c\n",i);
		}
		i++;
	}
	


	// Aufgabe 6 Do-While Loop
	char c;
	i = 122;
	do {
		if (i < 91 || i > 96) {
			c = i;
			printf("%c\n", c);
		}
		i--;
	} while (i >= 65);

	return 0;
}