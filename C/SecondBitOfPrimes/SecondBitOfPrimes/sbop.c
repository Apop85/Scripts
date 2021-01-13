#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <string.h>
#include <stdbool.h>
#include <stdlib.h>
#include <math.h>
#include <time.h>


int JumpBack(int amount) {
	// Output \r for a specific amount of time
	int i;
	for (i = 0; i < amount; i++) {
		printf("\r");
	}
	return 0;
};


bool IsPrime(long long int number) {
	// Function to determine if a number is a prime or not

	// Variabel declaration
	long long int maxValue;
	int i;
	char binary[255];

	// Convert number to bin value
	_itoa(number, binary, 2);
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


int PrintSpaces(int amount) {
	// Function to print empty spaces
	int i;
	for (i = 0; i < amount; i++) {
		printf(" ");
	}
};


int main(void) {
	// Variabel deklaration
	long long int value, targetAmount, currentAmount, timeStamp, timeDelta, timeStart, timeEnd, valueBefore, valueDelta, ones, zeroes, targetBit;
	double minLeft, done;
	int columnWidth, i, sizeOfArray, leftSide, rightSide, pps, counter, counterMax, binaryLength;
	char strAmount[255], strPPS[255], strMinLeft[255], strBinary[255];

	// Variabel definitions

	// Outputarray definition
	char header[4][30] = {"AMOUNT OF PRIMES", "PRIMES PER SECOND", "MINUTES LEFT", "DONE"};
	sizeOfArray = sizeof(header) / sizeof(header[0]);

	// Get Timestamp of programm start
	timeStart = time(NULL);

	// Set max column width
	columnWidth = 25;

	// Set measurement interval
	counterMax = 100;

	// Set target amount of prime numbers
	targetAmount = 1000000;

	// Set start value
	value = 3;

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

	// Output header
	for (i = 0; i < sizeOfArray; i++) {
		// Calculate empty spaces
		leftSide = (columnWidth - strlen(header[i])) / 2;
		rightSide = columnWidth - leftSide - strlen(header[i]);

		// Output header item
		PrintSpaces(leftSide);
		printf("%s", header[i]);
		PrintSpaces(rightSide);
	}

	printf("\n");
	// Set timestamp for live measurement
	timeStamp = time(NULL);
	
	// Repeat until target amount of primes is reached
	do {
		// Check if current number is a prime
		if (IsPrime(value)) {
			// Iterate counter
			currentAmount++;
			
			// Integer to string conversion - decimal
			_itoa(currentAmount, strAmount, 10);
			// Calculate empty space
			leftSide = (columnWidth - strlen(strAmount)) / 2;
			rightSide = columnWidth - leftSide - strlen(strAmount);

			// Output current amount of primes
			PrintSpaces(leftSide);
			printf("%s", strAmount);
			PrintSpaces(rightSide);

			// Integer to string conversion -> decimal
			_itoa(pps, strPPS, 10);
			// Calculate empty space
			leftSide = (columnWidth - strlen(strPPS)) / 2;
			rightSide = columnWidth - leftSide - strlen(strPPS);

			// Ouput primes per second
			PrintSpaces(leftSide);
			printf("%s", strPPS);
			PrintSpaces(rightSide);

			// Integer to string conversion -> decimal
			_itoa(minLeft, strMinLeft, 10);
			// Calculate empty space
			leftSide = (columnWidth - strlen(strMinLeft)) / 2;
			rightSide = columnWidth - leftSide - strlen(strMinLeft);

			// Output minutes left
			PrintSpaces(leftSide);
			printf("%s", strMinLeft);
			PrintSpaces(rightSide);

			// Calculate percentage done
			done = 100 / (double)targetAmount * (double)currentAmount;
			// Calculate empty space
			leftSide = (columnWidth - 7) / 2;
			rightSide = columnWidth - leftSide - 7;

			// Output percentage done
			PrintSpaces(leftSide);
			printf("%6.2lf%%", done);
			PrintSpaces(rightSide);
			
			// Integer to string conversion -> dual
			_itoa(value, strBinary, 2);
			binaryLength = strlen(strBinary);
			// Iterate amount of 0 or 1
			if (strBinary[binaryLength - (targetBit + 1)] == 1) {
				ones++;
			} else {
				zeroes++;
			}

			// Return cursor to start of the line
			JumpBack(sizeOfArray * columnWidth);
		}
		// Iterate value
		value++;

		if (counter <= counterMax) {
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
				// Calculate minutes left
				minLeft = (targetAmount - currentAmount) / pps / 60;
			}
		}
	} while (currentAmount <= targetAmount);
	
	// Get finished timestamp
	timeEnd = time(NULL);
	
	// Output statistics
	printf("Amount of 0: %lli\n", zeroes);
	printf("Amount of 1: %lli\n", ones);
	printf("Needed Time: %lli min\n", (timeStart - timeEnd) / 60);
	return 0;
}

