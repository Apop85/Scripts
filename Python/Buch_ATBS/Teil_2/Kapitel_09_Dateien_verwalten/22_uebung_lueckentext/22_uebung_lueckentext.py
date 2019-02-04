# 22_uebung_lueckentext.py
# In diesem Übungsbeispiel geht es darum Inhalte in Strings mit von dem Benutzer angegebenen Stringwerten zu ersetzen
# Der Text wird aus einem File extrahiert und beinhaltet einen lückenhaften Text in welchem zu ersetzende Werte mit NOMEN, ADJEKTIV, VERB gekennzeichnet sind. 


import os, re
filepfad=os.path.dirname(__file__)
os.chdir(filepfad)

def file_open():
	global content, replace
	replace=0
	filename='replaceme.txt'
	if os.path.exists(filename):
		file=open(filename, 'r')
		content=file.read()
		file.close()
		find_snippets()

def find_snippets():
	suchmuster=re.compile(r'([A-Z]{4,})|(\w?[^A-Z]{2,}[A-Z]?[^A-Z]+)')
	ergebnis=suchmuster.findall(content)
	choose_words(ergebnis)
	
def choose_words(liste):
	neuer_satz=''
	for eintrag in liste:
		if eintrag[0] == '':
			neuer_satz+=eintrag[1]
		else:
			auswahl=input('Bitte ein '+eintrag[0].title()+' eingeben: ')
			neuer_satz+=auswahl
	save_file(neuer_satz)

def save_file(text):
        print(text)
        file=open('output.txt', 'w')
        file.write(text)
        file.close()

file_open()


