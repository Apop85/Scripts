/*

####
# File: FizzBuzz.c
# Project: Modul 403
#-----
# Created Date: Monday 09.11.2020, 11:45
# Author: Apop85
#-----
# Last Modified: Monday 09.11.2020, 12:52
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Kleine �bung die in Bewerbungsgespr�chen in den USA wohl sehr beliebt ist
- Ist Zahl durch 3 teilbar gebe Fizz aus
- Ist Zahl durch 5 teilbar gebe Buzz aus
- Ist Zahl sowohl durch 3 und durch 5 teilbar gebe FizzBuzz aus
####
*/

#include <stdio.h>

int main(void) {
	// Variabeldeklaration
	int i;
	for (i = 1; i <= 100; i++) {
		if (i % 3 == 0 && i % 5 == 0) {
			printf("FizzBuzz\n");
		} else if (i % 5 == 0) {
			printf("Buzz ");
		} else if (i % 3 == 0) {
			printf("Fizz ");
		} else {
			printf("%d ", i);
		}
	}
}