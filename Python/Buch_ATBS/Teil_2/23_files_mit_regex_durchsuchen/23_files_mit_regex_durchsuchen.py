# 23_files_mit_regex_durchsuchen.py
# Dieses Script durchsucht alle txt Dateien in einem vorgegebenen Ordner und sucht nach entsprechenden Strings

import os, re
counter=0

pfad=os.path.dirname(__file__)
ordnerinhalt=' '.join(os.listdir(pfad))
os.chdir(pfad)

def scan_4_dir():
	unterordner=[]
	suchmuster=re.compile(r'(\w+\.\w+)|(\w+\w+)')
	ergebnis=suchmuster.findall(ordnerinhalt)
	for eintrag in ergebnis:
		if eintrag[1] != '' and os.path.isdir(eintrag[1]):
			unterordner+=[eintrag[1]]
	check_dir(unterordner)

def check_dir(ordnerliste):
	for ordner in ordnerliste:
		os.chdir(ordner)
		files=os.listdir(os.getcwd())
		for file in files:
			if '.txt' in file:
				open_file=open(file, 'r')
				content=open_file.read()
				open_file.close()
				check_content(content)

def check_content(string):
	global counter
	suchmuster=re.compile(r'[\w\S]+')
	ergebnis=suchmuster.findall(string)
	counter+=len(ergebnis)

scan_4_dir()
print('Die totale Anzahl Wörter in allen txt-Dateien der Unterordner ist:', counter)