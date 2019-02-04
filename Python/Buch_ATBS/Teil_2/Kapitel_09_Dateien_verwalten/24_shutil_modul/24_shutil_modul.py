# 24_shutil_modul.py
# In dieses Script geht es um das Shutil-Modul welches in der Lage ist, Dateien sowie ganze Ordnerstrukturen auf einmal zu verschieben.

import os, shutil, re
os.chdir(os.path.dirname(__file__))
maxl, dv= 50, 15

def output(string):
	print(''.center(maxl, '█'))
	delta=len(string) % maxl
	string+=' '*(maxl)
	suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
	ergebnis=suchmuster.findall(string)
	for abschnitt in ergebnis:
		print(abschnitt)

string='Die Funktion shutil.copy() kopiert einzelne Dateien und kann diese falls gewünscht auch umbenennen.'
output(string)
shutil.copy(os.getcwd()+'\\copy_me\\copy_me.txt', 'copy_me2.txt')
print('\n')
input()

string='Hingegen kann shutil.copytree() komplette Ordnerstrukturen und dessen Inhalte kopieren'
output(string)
if not os.path.exists('copy_me2'):
    shutil.copytree('copy_me' , 'copy_me2')
print('\n')
input()

string='Mit shutil.move() kann man Ordner und Dateien verschieben und Umbenennen.'
output(string)
if os.path.exists('copy_me2.txt'):
    shutil.move('.\\copy_me2.txt', '.\\copy_me2\\new_copy_me2_txt')
print('\n')
input()

string='Mit os.unlink() kann man einzelne Dateien löschen.'
output(string)
if os.path.exists('.\\copy_me2\\copy_me.txt'):
    os.unlink('.\\copy_me2\\copy_me.txt')
print('\n')
input()

string='Mit os.rmdir() lassen sich einzelne LEERE Ordner löschen. Ist der Ordner nicht leer wird die Funktion mit einem Error abgebrochen.'
output(string)
input()
string='Mit shutil.rmtree() lassen sich ganze Verzeichnisstrukturen löschen.'
output(string)
if os.path.exists('.\\copy_me2'):
    shutil.rmtree('.\\copy_me2')
print('\n')
input()
