# 26_files_zippen.py
# In diesem Script geht es darum files zu mittels Zip zu komprimieren und zu entkomprimieren.

import os, zipfile, re, shutil
maxl, dv= 50, 15

os.chdir(os.path.dirname(__file__))

def output(string):
	print(''.center(maxl, '█'))
	string+=' '*(maxl)
	suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
	ergebnis=suchmuster.findall(string)
	for abschnitt in ergebnis:
		print(abschnitt)

text='Um ein Zipfile zu öffnen benutzt man zipfile.ZipFile() wobei hier die Gross-/Kleinschreibung einzuhalten ist.'
output(text)
print('\n')
input()

text='Um sich den Inhalt eines Zipfiles anzeigen zu lassen nutzt man .namelist()'
output(text)
zipdatei=zipfile.ZipFile('unzipme.zip')
inhalt=zipdatei.namelist()
print(inhalt)
print('\n')
input()

text='Mittels .getinfo() lässt sich die Grösse der un-/komprimierten Daten auslesen'
output(text)
size=zipdatei.getinfo(inhalt[0])
print(inhalt[0]+'[un/ko]=', str(size.file_size)+'/'+str(size.compress_size))
print('\n')
input()

text='Mit extractall() lässt sich das komplette Zipfile entpacken.'
output(text)
zipdatei.extractall()
liste=' - '.join(os.listdir(".\\"))
output(liste)
print('\n')
input()

text='Mit extract() lassen sich gezielt Dateien entpacken.'
output(text)
zipdatei.extract('unzipme/unzipme.txt', '.\\unzipme\\')
print('\n')
input()

if os.path.exists('.\\unzipme.zip'):
    zipdatei.close()
    os.remove('.\\unzipme.zip')

text='Mit dem Befehl write() lassen sich Zipfiles erstellen wobei hier die Argumente <Zieldatei> und <Kompressionstyp> erwartet wird und das Zipwile mit ZipFile(\'example.zip\', \'w\') geöffnet wird und anschliessend mit close() wieder geschlossen wird.'
output(text)
zipdatei=zipfile.ZipFile('unzipme.zip', 'w')
zipdatei.write('.\\unzipme\\unzipme.txt', compress_type=zipfile.ZIP_DEFLATED)
shutil.rmtree('.\\unzipme')
zipdatei.close()