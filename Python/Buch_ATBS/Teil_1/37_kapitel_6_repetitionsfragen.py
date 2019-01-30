print('1. Was sind Maskierungssequenzen?')
input()
print('Maskierungszeichen stehen für Zeichen die sonst nur\nschwer in einem Code wiedergeben lassen')
print(r'Beispiele: \n für newline \t für tabulator \\ für backslash ... ')
print('')
input()

print('2. Wofür stehen die Maskierungssequenzen \\t und \\n')
input()
print(r'\n steht für Newline und \t für Tabulator')
print('')
input()

print(r'3. Wie können sie einen Backslash (\) in einen String einfügen?')
input()
print('Es gibt zwei Möglichkeiten.')
print(r"Einmal mittels mittels '\\' und einmal mittels r'\'")
print('')
input()

print('4. "How\'s your day?" ist ein gültiger Stringwert. \nWarum führt das als Apostroph verwendete einfache\nAnführungszeichen in How\'s nicht zu einem Problem \nobwohl es nicht maskiert ist?')
input()
print('Da der String mit doppelten Anführungszeichen\ngeschrieben wurde werden die einfachen\nAnführungszeichen innerhalb des Strings nicht\nausgewertet.')
print('')
input()

print('5. Wie können sie einen String mit Zeilenumbruch schreiben ohne \\n zu verwenden?')
input()
print('Man kann die Zeilenumbrüche auch direkt in Strings verwenden ohne \\n nutzen zu müssen.')
print('')
input()

print('6. Wozu werden folgende Ausdrücke ausgewertet?\n\'Hello World\'[1]\n\'Hello World\'[0:5]\n\'Hello World\'[:5]\n\'Hello World\'[3:]')
input()
print('1. e - 2. Hello - 3. Hello - 4. lo World')
print('')
input()

print('7. Wozu werden folgende Ausdrücke ausgewertet?\n\'Hello\'.upper()\n\'Hello\'.upper().isupper()\n\'Hello\'.upper().lower()')
input()
print('1. HELLO - 2. True - 3. hello')
print('')
input()

print('8. Wozu werden folgende Ausdrücke ausgewertet?\n\'Remember, remember, the fifth of november.\'.split()\n\'-\'.join(\'There can be only one.\'.split())')
input()
print('1. [ \'Remember,\', \'remember,\', \'the\', \'fifth\', \'of\', \'november.\' ]')
print('2. There-can-be-only-one.')
input()

print('9. Mit welchen Stringmethoden können sie einen String\nRechtsbünding, Zentriert oder Linksbündig ausrichten?')
input()
print('Mit den Methoden string.center(n), string.rjust(n) und string.ljust(n)')
print('')
input()

print('10. Wie können Weissraumzeichen/Leerschläge am Ende\n und am Anfang eines Strings entfernt werden?')
input()
print('Mit string.strip(), string.lstrip() und string.rstrip()')
print('')
input()

