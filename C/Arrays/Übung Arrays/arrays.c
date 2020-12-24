/*

####
# File: arrays.c
# Project: Modul 403
# Version: 1
#-----
# Created Date: Monday 30.11.2020, 08:43
# Author: Apop85
#-----
# Last Modified: Monday 30.11.2020, 10:16
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Definieren eines Arrays und Berechnen der Anzahl Stunde der definierten Woche
####
*/

// Konstantendefinition
#define _CRT_SECURE_NO_WARNINGS 
#define WEEKSIZE 5
#define MENUWITH 50
// Modulinkludierung
#include <stdio.h>

int main(void) {
	// Variabelinitialisierung
	int i;
	float hoursPerWeekday[WEEKSIZE];
	int hours, minutes, choice;
	float sum;
	// Variabeldeklaration
	char answer = '0';
	char weekdays[5][11] = { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag" };
	char menu[4][50] = { "#", "0: Neue Woche berechnen", "1: Werte korrigieren", "x: Beenden" };

	do {
		sum = 0;
		if (answer != '1') {
			// Angabe der Stunden für jeden Arbeitstag
			for (i = 0; i < WEEKSIZE; i++) {
				printf("Geben sie die Anzahl Stunden fuer %s ein: ", weekdays[i]);
				scanf("%f", &hoursPerWeekday[i]); 
				while (getchar() != '\n');
			}
		} else {
			do {
				// Auswahl des zu korrigierenden Arbeitstages
				printf("\nWelchen Wochentag moechten Sie aendern?\n");
				// Ausgabe der möglichen Arbeitstage
				for (i = 0; i < WEEKSIZE; i++) {
					printf("%i. %s\n",i+1, weekdays[i]);
				}
				scanf("%i", &choice);
				while (getchar() != '\n');

			} while (choice < 1 && choice > 5);
			// Überschreiben des gewählten Arbeitstages
			printf("Geben sie die Anzahl Stunden fuer %s ein: ", weekdays[choice-1]);
			scanf("%f", &hoursPerWeekday[choice-1]);
			while (getchar() != '\n');

		}
		printf("\n");

		// Gebe zusammenfassung der Tage aus mit Stunden und Minuten
		for (i = 0; i < sizeof(hoursPerWeekday) / sizeof(hoursPerWeekday[1]); i++) {
			// Wandle float in int um 
			hours = (int)hoursPerWeekday[i];
			// Berechne Minuten aus Nachkommastellen
			minutes = (int)((hoursPerWeekday[i] - hours) * 60);
			printf("%-11s: %ih %imin\n", weekdays[i], hours, minutes);
			sum += hoursPerWeekday[i];
		}
		
		printf("\n\n");
		// Berechne Gesamtsarbeitszeit in Stunden und Minuten
		hours = (int)sum;
		minutes = (int)((sum - hours) * 60);
		// Ausgabe Trennstriche
		for (i = 0; i < MENUWITH; i++) {
			printf("=");
		}
		printf("\nArbeitsstunden in dieser Woche: %ih %imin\n", hours, minutes);
		// Ausgabe Trennstriche
		for (i = 0; i < MENUWITH; i++) {
			printf("=");
		}
		printf("\n\n");

		do {
			// Ausgabe Trennstriche
			for (i = 0; i < MENUWITH; i++) {
				printf("%s", menu[0]);
			}
			printf("\n");
			// Ausgabe der Menüpunkte
			for (i = 1; i < sizeof(menu) / sizeof(menu[1]); i++) {
				printf("%s %-46s %s\n",menu[0], menu[i], menu[0]);
			}
			// Ausgabe Trennstriche
			for (i = 0; i < MENUWITH; i++) {
				printf("%s", menu[0]);
			}
			printf("\n");

			scanf("%c", &answer);
			while (getchar() != '\n');
			printf("\n");

		} while (answer != '0' && answer != '1' && answer != 'x' && answer != 'X');

	} while (answer == '1' || answer == '0');
	printf("CYA");

	return 0;
}