#include <stdio.h>

// Einfache Funktion die zwei Integer entgegennimmt und den grösseren davon zurückgibt
int maxWert(int a, int b) {
    if (a > b) {
        return a;
    }
    else {
        return b;
    }
}

int meaningOfLife(void) {
    // Wird die Funktion aufgerufen wird der Wert 42 zurückgegeben.
    return 42;
}

int faktorieren(int a, int b, int resultat, int tiefe) {
    // Initialisiere lokale Variable
    int wertNeu;
    // Prüfe ob Programm sich in der ersten Iteration der Funktion befindet
    if (tiefe == 0) {
        resultat = a;
    }
    // Ist nur noch Faktor 1 übrig, gebe Resultat zurück
    if (b < 2) {
        return resultat;
    }
    // Iteriere Variablen
    tiefe++;
    b--;
    // Berechne neuer Wert
    wertNeu = a * resultat;
    // Rufe funktion auf
    faktorieren(a, b, wertNeu, tiefe);
}




int main(void) {
    int value1 = 10;
    int value2 = 20;

    printf("%i\n", maxWert(value1, value2));
    printf("The meaning of life, death and all the rest is: %i\n", meaningOfLife());

    // Variabeldeklaration
    int wert = 10;
    int faktor = 4;

    // Aufruf der rekursiven Faktorierungsfunktion
    printf("%i^%i = %i\n",wert, faktor, faktorieren(wert, faktor, 0, 0));

    return 0;
}