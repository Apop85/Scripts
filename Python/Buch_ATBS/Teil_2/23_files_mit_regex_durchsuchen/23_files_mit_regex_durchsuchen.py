# 23_files_mit_regex_durchsuchen.py
# Dieses Script durchsucht alle txt Dateien in einem vorgegebenen Ordner und sucht nach entsprechenden Strings


import os, re
total, counter=0, 0

pfad=os.path.dirname(__file__)
ordnerinhalt=' '.join(os.listdir(pfad))
os.chdir(pfad)

def scan_4_dir():
	unterordner=[]
	suchmuster=re.compile(r'[\w]+\.?[\w]+')
	ergebnis=suchmuster.findall(ordnerinhalt)
	for eintrag in ergebnis:
		if os.path.isdir(eintrag):
			unterordner+=[eintrag]
	check_dir(unterordner)

def check_dir(ordnerliste):
	global counter, total
	for ordner in ordnerliste:
		os.chdir(pfad+'\\'+ordner)
		files=os.listdir(os.getcwd())
		for file in files:
			if '.txt' in file:
				open_file=open(file, 'r')
				content=open_file.read()
				open_file.close()
				check_content(content)
		print((('Die txt-Dateien im Ordner '+ordner+' beinhalten:').ljust(75)+(str(counter))+' Wörter').rjust(20))
		total+=counter
		counter=0

def check_content(string):
	global counter
	suchmuster=re.compile(r'[\w\S]+')
	ergebnis=suchmuster.findall(string)
	counter+=len(ergebnis)

scan_4_dir()
print(('Die totale Anzahl Wörter in allen txt-Dateien aller Unterordner ist:'.ljust(75)+(str(total))+' Wörter').rjust(20))