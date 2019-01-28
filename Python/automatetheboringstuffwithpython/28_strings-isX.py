print('Hier geht es um die isX Anweisungen für Strings, mit dieser lassen \nsich viele Eigenshaften eines Strings liecht prüfen')
print()
input()

print('Erstes Beispiel ist isalpha(). Diese Funktion prüft \nob der String nur aus Buchstaben besteht.')
print('Beispiel 1: \'abc123\'.isalpha()')
if not 'abc123'.isalpha():
    print('False')
print('Beispiel 2: \'abcdef\'.isalpha()')
if 'abcdef'.isalpha():
    print('True')
print()
input()

print('Zweites Beispiel ist isalnum(). Diese Funktion prüft \nob der String nur aus Buchstaben UND Zahlen besteht.')
print('Beispiel 1: \'1111\'.isalnum()')
if '1111'.isalnum():
    print('True')
print('Beispiel 2: \'1a11\'.isalnum()')
if '1a11'.isalnum():
    print('True')
print('Beispiel 3: \'1a&2\'.isalnum()')
if not '1a&2'.isalnum():
    print('False')
print()
input()

print('Das dritte Beispiel ist isdecimal(). Diese Funktion prüft\nob der String nur aus Zahlen besteht.')
print('Beispiel: \'1111\'.isdecimal()')
if '1111'.isdecimal():
    print('True')
print('Beispiel: \'1a11\'.isdecimal()')
if not '1a11'.isdecimal():
    print('False')
print()
input()

print('Viertes Beispiel ist isspace(). Diese Funktion prüft\nob der String nur aus Leerzeichen besteht.')
print('Beispiel: \'  \'.isspace():')
if '   '.isspace():
    print('True')
print()
input()

print('Das fünfte Besipiel ist istitle(). Diese Funktion prüft ob der \nAnfangsbuchstabe des Strings gross geschrieben ist.')
print('Beispiel: \'Hallo\'.istitle()')
if 'Hallo'.istitle():
    print('True')
print('Beispiel: \'hallo\'.istitle()')
if not 'hallo'.istitle():
    print('False')
print()
input()

print('Man Kann auch testen ob ein String in Gross- \noder Kleinbuchstaben geschrieben ist. mittels isupper() oder islower()')
print('Beispiel mit [' + low + '] low.islower():')
if low.islower():
    print('low.islower() = True')
print('Beispiel mit [' + upp + '] upp.isupper():')
if upp.isupper():
    print('upp.isupper() = True')
print()
input()




