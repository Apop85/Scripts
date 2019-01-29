# Die Theorie der Strings, wie sie verwendet und verarbeitet werden können

st='stri'
stri1=('Gehen wir durch die Theorie von Strings.')
stri2=('Strings können zusammenhängend wie dieser Satz sein.')
stri3=('''Sie können
jedoch auch
mehrere Zeilen
beinhalten wie
dieser Satz.''')
stri4=('''Dabei wird wird auch
            der Tabulator berücksichtig.''')
stri5='Test String'

varlist=[stri1, stri2, stri3, stri4, stri5]

for strings in varlist:
    print(strings)
input()
print()
print('Man Kann Strings jedoch auch Zerstückeln')
print('Beispiel:', stri2[15:28], stri2[8:14], stri4[16:20], stri2[23:29])
print('Beispiel:', stri5[5:], stri5[:5], '\n\n')
input()

print('Man kann Strings auch Teilen')
print('Beispiel:', stri1[int(len(stri1)/2):])
print('Beispiel:', stri1[:int(len(stri1)/2)])
input()

print()
print('Man kann auch die Operatoren "in" und "not" auf Strings anwenden')
print('Beispiel: \'wir\' in stri1:')
if 'wir' in stri1:
    print('True')

print()
print('Beispiel: \'wir\' not in stri1:')
if 'wir' not in stri1:
    print('True')
input()
