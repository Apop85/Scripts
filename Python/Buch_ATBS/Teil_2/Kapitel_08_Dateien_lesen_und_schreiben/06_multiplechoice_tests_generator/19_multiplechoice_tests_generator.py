# Übung: Erstelle ein Script welches X Multiplechoice tests als Textfile erstellt. Die Reihenfolge der Fragen und Antworten sollen sich in jedem Dokument unterscheiden.

import os, pprint, shelve
from copy import deepcopy as dcopy
from random import randint as rng
from random import shuffle

fragenpfad=os.path.dirname(__file__)+r'\Fragebogen'

if not os.path.exists(fragenpfad):
	os.mkdir(fragenpfad)

content={'pprint.pformat()' : 'Formatierte, Pythongerechte ausgabe', 
'try: except:' : 'Errorhandling', 
'string.sort(reverse=true)' : 'Entgegen dem Alphabet sortieren', 
'string.sort(key=str.lower)' : 'Sortiere ungeachtet Grossschreibung', 
'random.randint(9,10)' : 'Zufallszahl zwischen 9 und 10', 
'string[2][3]' : 'Ausgabe einer Liste in einer Liste', 
'for namen in liste:' : 'Loop welcher die Inhalte einer Liste durchgeht', 
'string[:7]' : 'String von anfang bis zu einem bestimmten Punkt ausgeben',
'(a, b, c, d)' : 'Tuple erstellen',
'string.append(\'text\')' : 'Text am ende eines Strings oder einer Liste anfügen',
'string=copy(variable)' : 'String in eine neue Variable kopieren',
'string=dcopy(variable)' : 'Liste oder Dictionary in eine neue Variable kopieren',
'string[2]=\'Blah\'' : 'Wert in einer Liste ändern',
'string[\'Name:\']=Michael' : 'Eintrag in einem Dictionary ändern',
'string.setdefault(\'Hund\', \'Schwarz\')' : 'Falls Eintrag nicht vorhanden Wert setzen',
'string.isupper()' : 'Prüft auf Grossbuchstaben',
'string.upper()' : 'Ausgabe in Grossbuchstaben',
'string.center()' : 'Zentrierte Ausrichtung',
'string.strip()' : 'Leerzeichen am anfang und Ende entfernen' }

teilnehmer=['Ronny', 'Sabri', 'Stefan', 'Peter', 'Samuel', 'Tino', 'Sarah', 'Steffanie', 'Sandra', 'Raffi']


intro=['Hey', 'REPLACE', ',ich hoffe du hast ausgiebig gelernt und hast dich auf den Test\nvorbereitet. Es gibt keine zweite chance also nutze sie gut. Wenn du den\nTest vermasselst gibt es Prügel!\n\n']
fragen=list(content.keys())
antworten=list(content.values())


def gentests():
	global intro, fragefile, file
	for namen in teilnehmer:
		fragefile=fragenpfad+'\\'+namen+'.txt'
		intro[1]=namen
		file=open(fragefile, 'w')
		file.write(' '.join(intro))
		file.close()
		shuffle(fragen)
		shuffle(antworten)
		file=open(fragefile, 'a')
		genquest()
		file.close()
	return
		

def genquest():
	global fragebogen, testfrage, fragenummer, file
	counter=0
	while counter != len(content):
		for frage in fragen:
			fragebogen=[]
			testfrage='Was bedeutet: ' + fragen[counter] +'?\n\n'
			fragebogen+=[content[frage]]
			counter+=1
			fragenummer=counter
			frageblock()
	return

def frageblock():
	global fragebogen
	counter=0
	while len(fragebogen) != 5:
		r=rng(0,len(antworten)-1)
		if antworten[r] not in fragebogen:
			fragebogen+=[antworten[r]]
			counter +=1
	shuffle(fragebogen)
	output()
	return

def output():
	r2=rng(0,4)
	file.write(((' Frage ' + str(fragenummer) + ' ').center(100, '#'))+'\n')
	file.write((testfrage).center(100)+'\n')
	for i in range(len(fragebogen)):
		if i != r2:
			file.write(('[]'.rjust(20))+'  '+fragebogen[i]+'\n')
		else:
			file.write(('[x]'.rjust(20))+'  '+fragebogen[i]+'\n')
	file.write('\n\n\n')
	return

def save():
    save=shelve.open('data')
    save['content'] = content
    save.close()

gentests()
save()
