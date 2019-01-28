def cls():
    for i in range(30):
        print('\n')

print('1. Was bedeutet []?\n')
print('[] Bedeutet dass diese Variabel als Liste deklariert ist')
input('Enter zum fortfahren')

cls()
print('2. Wie weist man einen Wert dem 2. Element einer Liste zu?\n')
print('Durch variabelname[2]=\'Neuer Wert\'')
input('Enter zum fortfahren')

cls()
print('3. Zu was wird "spam[int(int(\'2\' * 3)/11)]" ausgewertet?\n')
print('Die Auswertung lautet spam[3] = d \nda 2*\'3\' 33 ergibt (Stringmultiplikation)')
input('Enter zum fortfahren')

cls()
print('4. wie wird spam[-1] ausgewertet?\n')
print('spam[-1] gibt den hintersten Eintrag in dieser Liste aus. = d')
input('Enter zum fortfahren')

cls()
print('5. Wie wird spam[:2] ausgewertet?\n')
print('Dieser aufruf gibt die Einträge mit der ID 0-1 aus. = ab \nwie bei range(4) wo die 4 nicht mehr gezählt wird.')
input('Enter zum fortfahren')

cls()
print('6. Wozu wird bacon.index(\'cat\') ausgewertet?\n')
print('Es wird der Listenindex ausgewertet, es wird geprüft ob in der Liste \nbacon einen Eintrag cat vorhanden ist und zeigt an wo dieser sich befindet.')
input('Enter zum fortfahren')

cls()
print('7. Wie sieht der Listenwert nach dem ausführen von \nbacon.append(99) aus?\n')
print('In der Liste bacon wird an der letzten Stelle der Integer 99 eingefügt')
input('Enter zum fortfahren')

cls()
print('8. Wie sieht der Listenwer nach dem ausführen von \nbacon.remove(\'cat\') aus?\n')
print('Es wird geprüft ob in der Liste bacon der Eintrag cat vorhanden ist\nund falls ja wird dieser entfernt.')
input('Enter zum fortfahren')

cls()
print('9. Wie sehen die Operatoren für die Listenverkettung und die Listenwiderholung aus?\n')
print('Listenverkettung funktioniert mit liste=[liste1 + liste2]\nListenwiderholung funktioniert mit liste=[0]*6')
input('Enter zum fortfahren')

cls()
print('10. Was ist der Unterschied zwischen den Listenmethoden \nappend() und insert()?\n')
print('append() fügt einen Eintrag am Ende der Liste ein, \ninsert() an einer gewünschten stelle')
input('Enter zum fortfahren')

cls()
print('11. Welche beiden Möglichkeiten gibt es Elemente einer Liste zu entfernen?\n')
print('del liste[0] oder liste.remove(\'eintrag\')')
input('Enter zum fortfahren')

cls()
print('12. Welche Ähnlichkeiten haben Listen und Strings?\n')
print('Strings und Listen können an len() übergeben werden haben Indices ([1]) und Slices ([:2])\n lassen sich in for-Schleifen verwenden, verändern und verketten\nund können mit den Operatoren in und not verwendet werden. ')
input('Enter zum fortfahren')

cls()
print('13. Worin unterscheiden sich Listen und Tupel?\n')
print('Listen sind Variabel und können verändert werden, Tuples sind Konstant und unveränderbar')
input('Enter zum fortfahren')

cls()
print('14. Wie geben sie einen Tuplewert an der nur einen Interger beinhaltet?\n')
print('durch var=(42,) Das Komma und die Rundklammern geben Python an \ndass es sich hier um ein Tuple handelt')
input('Enter zum fortfahren')

cls()
print('15. Wie können sie Listen in Touple umwandeln und umgekehrt?\n')
print('L2T: tupl=tuple(liste) | T2L: liste=list(tuple)')
input('Enter zum fortfahren')

cls()
print('16. In Variablen die Listenwerte enthalten sind \ngar nicht die Listen gespeichert. \nWas enthalten sie in Wirklichkeit?\n')
print('Den internen Verweis zu der Originalliste')
input('Enter zum fortfahren')

cls()
print('17. Was ist der Unterschied zwischen copy.copy() und copy.deepcopy()?\n')
print('mit copy Kann man eine Liste Kopieren die Werte beinhaltet.\nMit deepcopy() kann man Listen mit Listen als Inhalt kopieren.')
input('Enter zum fortfahren')

