# 29_kapitel_9_repetitionsfragen.py

import re

maxl, dv= 50, 15
def output(string):
	print(''.center(maxl, '█'))
	delta=len(string) % maxl
	string+=' '*(maxl)
	suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
	ergebnis=suchmuster.findall(string)
	for abschnitt in ergebnis:
		print(abschnitt)

text='1. Was ist der Unterschied zwischen shutil.copy() und shutil.copytree()?'
output(text)
input()
text='shutil.copy() Kopiert ein File in ein Verzeichnis während shutil.copytree() das komplette Verzeichnis mit allen darin enthaltenen Dateien und Unterordner kopiert'
output(text)
print('\n')
input()

text='2.Mit welcher Funktion benennen sie Dateien um?'
output(text)
input()
text='Mit shutil.move()'
output(text)
print('\n')
input()

text='3. Was ist der Unterschied zwischen den Löschmodulen in send2trash und shutil?'
output(text)
input()
text='Wie der Name schon besagt werden Dateien mit send2trash in den Papierkorb verschoben während sie mit shutil unwiederruflich gelöscht werden.'
output(text)
print('\n')
input()

text='4. Ebenso wie File-Objekte verfügen auch ZipFile Objekte eine close() Methode. Welche ZipFile-Methode entspricht aber der File-Methode open()?'
output(text)
input()
text='Die Methode lautet zipfile.ZipFile(argument1, argument2). Das 1. Argument beinhaltet den Zielpfad zur Datei und das 1. Argument den Modus in welchem das File geöffnet werden soll (w/a/r)'
output(text)
print('\n')
input()