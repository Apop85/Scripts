teststring='das ist EIN TEsTsTRInG'
low='lower'
upp='UPPER'
print('Stirings wie dieser:['+teststring+'] Können auch manipuliert werden')
print()
input()

print('Man kann z.b. mit upper() den kompletten Inhalt des Strings\nin Grossbuchstaben schreiben.')
print('Beispiel:', teststring.upper())
print()
input()

print('Man kann auch alles in Kleinbuchstaben ausgeben:')
print('Beispiel:', teststring.lower())
print()
input()

print('Man Kann auch testen ob ein String in Gross- \noder Kleinbuchstaben geschrieben ist. mittels isupper() oder islower()')
print('Beispiel mit [' + low + '] low.islower():')
if low.islower():
    print('low.islower() = True')
print('Beispiel mit [' + upp + '] upp.isupper():')
if upp.isupper():
    print('upp.isupper() = True')
input()


