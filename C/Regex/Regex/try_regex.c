#include <regex.h>
#include <stdio.h>

int main(void) {
    int returnCode;
    char testString[] = "Hallo Welt";
    // char regex = [];
    // Kompilierung des Suchmusters
    returnCode = regexcomp(&regex, "[a-zA-Z]+", 0);
    if (returnCode == 0) {
        printf("Muster erfolgreich kompiliert\n");
        // Variabel auf Suchmuster prüfen
        returnCode = regexec(&regex, testString, 0, NULL, 0);
        if (returnCode == 0) {
            printf("Muster gefunden");
        }
        else if (returnCode == REG_NO_MATCH) {
            printf("Muster nicht gefunden");
        }
        else {
            printf("Fehler bei der Suche");
        }
    }
    else {
        printf("Fehler bei der Kompilierung");
    }
    return 0;
}
