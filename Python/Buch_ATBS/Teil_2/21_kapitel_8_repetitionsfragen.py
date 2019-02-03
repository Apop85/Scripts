# 21_kapitel_8_repetitionsfragen.py
# Hier die Antworten zu den Wiederholeungsfragen aus Kapitel 8
import re
maxl, dv= 50, 13
def output():
	global string
	print(''.center(maxl, '█'))
	delta=len(string) % maxl
	string+=' '*(maxl-delta)
	suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +'}[,|\.|\n| ]', re.DOTALL)
	ergebnis=suchmuster.findall(string)
	for abschnitt in ergebnis:
		print(abschnitt)


string='Wozu ist ein relativer Pfad relativ?'
output()
string='Ein relativer Pfad zeigt an wo sich das Ziel relativ zum Arbeitsverzeichnis befindet (os.path.getcwd())'
output()
print('\n\n')

string='Womit beginnt ein Absoluter Pfad?'
output()
string='Ein Absoluter Pfad beginnt immer mit dem Laufwerk unter Windows zB C:\\ oder unter Linux und Mac mit /, diese nennt man auch Wurzel- oder Stammordner.'
output()
print('\n\n')

string='Was machen die Funktionen os.path.getcwd() und os.path.chdir()?'
output()
string='Mit os.path.getcwd() wird das aktuelle Arbeitsverzeichnis ausgegeben. Mit os.path.chdir() kann das Verzeichnis gewechselt werden.'
output()
print('\n\n')

string='Worum handelt es sich bei den Ordnern . und ..?'
output()
string='Bei . handelt es sich um den relativen Pfad zum aktuellen Verzeichnis. Bei .. handelt es sich um den relativen Pfad zum Elternverzeichnis.'
output()
print('\n\n')

string=r'Welcher Teil von C:\bacon\cheese\spam.txt ist der Verzeichnisname (os.path.dirname()) und welches ist der Grundname (os.path.basename())?' 
output()
string=r'C:\bacon\cheese ist der Verzeichnisname und spam.txt ist der Grundname.'
output()
print('\n\n')

string='Welche drei Modusargumente können sie der Funktion open() übergeben?'
output()
string='open(filename, \'r\') wenn man es nur lesen möchte, open(filename, \'w\') wenn man das File überschreiben möchte und open(filename, \'a\') wenn man etwas ans Ende einer Datei einfügen möchte.'
output()
print('\n\n')

string='Was geschieht wenn sie eine bereits vorhandene Datei im Schreibmodusöffnen?'
output()
string='Im Schreibmodus wird der Inhalt eines Files komplett überschrieben.'
output()
print('\n\n')

string='Was ist der Unterschied zwischen den Methoden read() und readlines()?'
output()
string='read() liest den kompletten Inhalt eines Files während readlines(), wie es der Name schon sagt nach Zeilenumbrüchen sucht und dann die Inhalte getrennt durch den Zeilenumbruch in einer Liste speichert'
output()
print('\n\n')

string='Welche Datenstruktur stellt der Shelvewert dar?'
output()
string='Der Shelvewert ähnelt einem Dictionary-Wert: Er berfügt über Schlüssel und Werte und die Methoden keys() und values(), die ähnlich wie die gleichnamigen Dictionary-Methoden funktionieren. Gespeichert wird der Inhalt in binärer Form.'
output()
print('\n\n')