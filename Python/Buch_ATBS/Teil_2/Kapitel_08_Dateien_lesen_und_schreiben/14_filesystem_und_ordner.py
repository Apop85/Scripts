# Einführung in Ordner- und Dateimanagement
import os
maxl=50
dv=12
import re
def output(string):
    delta=len(string)%maxl
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[\S].{'+str(maxl-dv)+r','+str(maxl)+r'}[ ]')
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i])

print(''.center(maxl, '█'))
string='Um in Python das aktuelle Verzeichnis anzuzeigen kann man os.getcwd() verwenden welcher dann Folgendes Anzeigt:'
output(string)
print('\n'+(os.getcwd()).center(maxl))
print(''.center(maxl, '█'))
path=os.getcwd()
input()


print(''.center(maxl, '█'))
string='Mit os.chdir(PFAD) kann man das aktuelle Verzeichnis wechseln.'
output(string)
print('\n'+(os.getcwd()).center(maxl))
os.chdir(r'C:\Users\Apop85\Desktop')
print('\n'+(os.getcwd()).center(maxl))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.mkdir(PFAD) kann man Ordner erstellen.'
output(string)
os.mkdir(path + r'\testordner')
os.chdir(path + r'\testordner')
print('\n'+(os.getcwd()).center(maxl))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='mit os.mkdirs(PFAD) kann man direkt mehrere Unterverzeichnisse erstellen.'
output(string)
os.makedirs(path+r'\sub1\sub2\sub3')
os.chdir(path+r'\sub1\sub2\sub3')
print('\n'+(os.getcwd()).center(maxl))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.path.abspath(PFAD) kann man einen Relativen Pfad wie ..\\..\\ in einen absoluten Pfad umwandeln.'
output(string)
print('\n'+os.path.abspath('..\\..\\'))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string=r'Mit os.path.relpath(PFAD) kann man schauen wo sich der Zielordner relativ zu einem befindet und kriegt entsprechend einen relativen Pfad zurück. Beispiel C:\Users'
output(string)
print(os.path.relpath('C:\\Users'))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.path.basename(ZIELFILE) kann man sich den Dateinamen ohne den Pfad zu einer Zieldatei auslesen. Mit os.path.dirname(PFAD) lässt sich der Pfad dieser Datei auslesen'
pathexe=r'C:\Windows\System32\calc.exe'
output(string)
print('\n'+os.path.basename(pathexe))
print('\n'+os.path.dirname(pathexe))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.path.split() lässt sich der Pfad in ein Tuple ausgeben mit Pfad und Dateiname.'
output(string)
print('\n',os.path.split(pathexe))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mittels os.path.getsize(PFAD) kann die Grösse einer Datei in Byte ausgegeben werden.' 
output(string)
print('\n'+(os.path.basename(pathexe))+' : '+str(os.path.getsize(pathexe))+' Bytes')
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.listdir(PFAD) werden alle in dem Ordner vorhandenen Dateien und Unterordner angezeigt.'
output(string)
output(('\n\n ►'+' ►'.join(os.listdir('..\\..\\..\\..\\'))))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit einer for schleife kann man die Grösse des Ordnerinhalts prüfen indem man alle Dateigrössen zusammenrechnet.'
output(string)
size=0
for files in os.listdir('..\\..\\..\\..\\..\\'):
    size+=os.path.getsize('..\\..\\..\\..\\..\\'+files)
output(('Total für '+os.path.abspath('..\\..\\..\\..\\..\\')+' : '+str(size)+' Bytes'))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mittels os.path.exists(PFAD) kann man Prüfen ob die angegebene Datei oder das Verzeichnis vorhanden ist und gibt entsprechend True oder False zurück.'
output(string)
output(('\nIst Pfad '+path+' vorhanden: '+str(os.path.exists(path))))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.path.isfile(PFAD) kann man prüfen ob das angegebene Ziel eine Datei oder ein Ordner ist.'
output(string)
output(('\n\nIst '+pathexe+' ein File? Antwort: '+str(os.path.isfile(pathexe))))
print(''.center(maxl, '█'))
path=os.getcwd()
input()

print(''.center(maxl, '█'))
string='Mit os.path.isdir(PFAD) kann man prüfen ob das angegebene Ziel ein Ordner ist oder nicht.'
output(string)
output(('\n\n Ist '+os.path.abspath('..\\..\\..\\')+' ein Verzeichnis? Antwort: '+str(os.path.isdir('..\\..\\..\\'))))
print(''.center(maxl, '█'))
input()

import shutil
path='C:\\Users\\Apop85\\Desktop'
os.chdir(path)
shutil.rmtree(path+'\\testordner')

