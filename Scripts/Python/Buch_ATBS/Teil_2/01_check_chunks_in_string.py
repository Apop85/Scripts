# Dieses Script soll nach 12-Stelligen Telefonnummern mit dem Muster 123-456-7890

teststring='''Thomas: Hey Michael, How are you? Please call me later at 001-234-1246 ok?
Michael: Sure. Do you know my new home number? It's 921-234-4513. See you.'''

for i in range((len(teststring))-12):
    chunk = teststring[i:i+12]
    if chunk[3]+chunk[7] == '--' and (chunk[:2]+chunk[4:6]+chunk[8:]).isalnum():
        print('Nummer entdeckt!', chunk)

    
teststring='C4H12O3'
zahl=''
teststring=teststring+'  '
for i in range((len(teststring))-2):
    chunk=teststring[i:i+3]
    # Wenn Element mit einem Buchstaben ohne Zahl
    if chunk[0].isupper() and chunk[1].isupper():
        print('Element Gefunden:', chunk[0])
    # Wenn Element mit zwei Buchstaben ohne Zahl
    elif (chunk[0] + chunk[2]).isupper() and chunk[1].islower():
        if not chunk[2].isdecimal():
            print('Element Gefunden:', chunk[:2])
        else:
            element=chunk[:2]
    # Wenn Zahl zu Grossbuchstabe
    elif chunk[0].isupper() and chunk[1].isdecimal():
        element=chunk[0]+chunk[1]
    # Wenn Zahl nach Kleinbuchstabe
    elif chunk[0].islower() and chunk[1].isdecimal():
        element+=chunk[1]
    # Wenn Zahl grösser als 9
    elif chunk[0].isdecimal() and chunk[1].isdecimal():
        element+=chunk[1]
    # Wenn Zahl grösser 9 ausgelesen
    elif chunk[0].isdecimal() and chunk[1].isupper():
        print('Element gefunden:', element)
    # Wenn letzter Eintrag Grossbuchstabe
    elif chunk[1].isspace() and chunk[0].isupper():
        print('Element gefunden:', chunk[0])
    # Wenn letzter Eintrag Zahl
    elif chunk[0].isdecimal() and chunk[1].isspace():
        print('Element gefunden:', element)
