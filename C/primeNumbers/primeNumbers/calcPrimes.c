/*

####
# File: calcPrimes.c
# Project: C
#-----
# Created Date: Monday 02.11.2020, 17:00
# Author: Apop85
#-----
# Last Modified: Monday 02.11.2020, 18:32
#-----
# Copyright (c) 2020 Apop85
# This software is published under the MIT license.
# Check http://www.opensource.org/licenses/MIT for further informations
#-----
# Description:
- Calculate prime numbers
####
*/
#define _CRT_SECURE_NO_WARNINGS

#include <math.h>
#include <stdio.h>
#include <stdbool.h>

int main(void) {
	// Initialize 64bit variables
	unsigned long long int currentNumber = 3;
	unsigned long long int targetDivisor, i;
	// Initialize prime switch
	bool prime;

	unsigned long long int max_value = 1 * (pow(2, 64) - 1);
	printf("%lld", max_value);

	// Iterate trough the full 64bit number range
	while (currentNumber <= max_value - 1) {
		// Set prime switch to true
		prime = true;

		// Get max divisor by the square root of the current number
		targetDivisor = sqrt(currentNumber);

		// Iterate trough all possible divisors 
		for (i = 2; i < targetDivisor; i++) {
			if (currentNumber % i == 0) {
				// If modulo of current number and current iteration is 0 it can't be prime
				prime = false;
				// Cancel loop
				break;
			}
		}

		if (prime == true) {
			// If no divisor was found output new prime number
			printf("%lld  ", currentNumber);
		}
		// Increase currentNumber by 1
		currentNumber++;
	}
	


	return 0;
}