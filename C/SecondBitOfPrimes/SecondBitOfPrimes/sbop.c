#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <string.h>
#include <stdbool.h>
#include <stdlib.h>
#include <math.h>
#include <time.h>

bool IsPrime(long long int number) {
	// Function to determine if a number is a prime or not

	// Variabel declaration
	long long int maxValue;
	int i;
	char binary[255];

	// Convert number to bin value
	_itoa(number, binary, 2);

	if (number == 2) {
		return true;
	}

	// If last bit is a 0, it s not a prime number
	if (binary[strlen(binary) - 1] == '0') {
		return false;
	}

	// Max value to check is never greater than the square root of the number + 1
	maxValue = sqrt(number) + 1;

	// Iterate trough values
	for (i = 2; i <= maxValue; i++) {
		// Check if modulo of number and i equals 0
		if (number % i == 0) {
			// If modulo equals 0 it's not a prime number
			return false;
		}
	}
	// If modulo never equals 0 it's a prime number
	return true;
};

int main(void) {
	// Variabel deklaration
	long long int value, targetAmount, currentAmount, timeStamp, timeDelta, timeStart, timeEnd, valueBefore, valueDelta, ones, zeroes;
	double minLeft, done;
	int columnWidth, i, sizeOfArray, pps, counter, iterationInterval, binaryLength, minPPS, maxPPS, targetBit;
	char strAmount[255], strPPS[255], strMinLeft[255], strBinary[255];

	// Variabel definitions
	// Get Timestamp of programm start
	timeStart = time(NULL);

	// Set max column width
	columnWidth = 25;

	// Set measurement interval
	iterationInterval = 100;

	// Set target amount of prime numbers
	targetAmount = 2000000;

	// Set start value
	value = 2;

	/* Set target bit to track
		1 = 1001010[1]
		2 = 100101[0]1
		3 = 10010[1]01
	*/
	targetBit = 2;

	// Set initial Values
	currentAmount = 0;
	counter = 0;
	pps = 0;
	valueBefore = 0;
	minLeft = 0;
	ones = 0;
	zeroes = 0;
	minPPS = 20000000;
	maxPPS = 0;

	// Set timestamp for live measurement
	timeStamp = time(NULL);
	
	printf("Task         : Calculate prime numbers\n");
	printf("Start value  : %lli\n", value);
	printf("Target amount: %lli\n", targetAmount);
	printf("Bit to detect: %i\n", targetBit);
	printf("--------------------------------------------\n");

	// Repeat until target amount of primes is reached
	do {
		// Check if current number is a prime
		if (IsPrime(value)) {
			// Iterate counter
			currentAmount++;
			
			// Integer to string conversion -> dual
			_itoa(value, strBinary, 2);
			binaryLength = strlen(strBinary);
			// Iterate amount of 0 or 1
			if (strBinary[binaryLength - targetBit] == '1') {
				ones++;
			} else {
				zeroes++;
			}
		}
		// Iterate value
		value++;

		if (counter <= iterationInterval) {
			// Iterate counter if smaller than max counter value
			counter++;
		} else {
			// Get delta time since last measurement
			timeDelta = time(NULL) - timeStamp;
			if (timeDelta == 0) {
				counter++;
			} else {
				// Reset Counter if greater than max counter value
				counter = 0;
				// Calculate delta values and reset measurement data
				valueDelta = currentAmount - valueBefore;
				valueBefore = currentAmount;
				timeStamp = time(NULL);
				// Calculate primes per second
				pps = valueDelta / timeDelta;

				// Detect min/max pps
				if (minPPS > pps) {
					minPPS = pps;
				}

				if (maxPPS < pps) {
					maxPPS = pps;
				}
				// Calculate minutes left
				minLeft = (targetAmount - currentAmount) / pps / 60;
			}
		}
	} while (currentAmount < targetAmount);
	
	value--;

	// Output statistics
	printf("Amount of 0  : %lli\n", zeroes);
	printf("Amount of 1  : %lli\n", ones);
	printf("min PPS      : %i\n", minPPS);
	printf("max PPS      : %i\n", maxPPS);
	printf("Last prime   : %lli\n", value);
	printf("Needed Time  : %lli sec\n", (time(NULL) - timeStart));
	return 0;
}

