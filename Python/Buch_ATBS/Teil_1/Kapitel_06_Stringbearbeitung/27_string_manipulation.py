# Manipulation von Strings

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

print('In diesem Beispiel geht es um join() und split(). Diese Funktionen\nDienen dazu mehrere Strings miteinander zu verbinden\noder einen String mit mehreren Wörter zu einzelnen Strings zu splitten.')
print('Gebe einen Satz mit mehreren Wörtern ein:')
sent=input()
spl=sent.split(' ')
print('Beispiel - Original:\t[', sent, ']')
print('Beispiel - Splitted:\t[', spl, ']')
print('Beispiel - Join mit "-":[', '-'.join(spl), ']')
print()
input()

msg='''Mit der split() und join()
Methode lassen sich auch
mehrzeilige Strings zu einer
Zeile zusammenfassen wie in
diesem Beispiel.'''
msgsplit=msg.split('\n')
print(' '.join(msgsplit))
print()
input()

print('Man Kann auch die Ausrichtung einer Stringausgabe mit \nrjust(), ljust und center() angeben')
print('Beispiel ljust():',teststring.ljust(50, '#'))
print('Beispiel rjust():',teststring.rjust(50, '#'))
print('Beispiel center():',teststring.center(49, '#'))
print()
input()

print('Die Optionen strip(), lstrip() und rstrip() ermöglichen es \nbestimmte Zeichen vom Anfang und dem Ende des Strings zu entfernen.')
teststring='           ==Hallo Welt==         '
print('Teststring:\t\t['+teststring+']')
print('Beispiel lstrip():\t['+teststring.lstrip()+']')
print('Beispiel rstrip():\t['+teststring.rstrip()+']')
print('Beispiel strip():\t['+teststring.strip()+']')
print('Beispiel strip(\' =\'):\t['+teststring.strip(' =')+']')
