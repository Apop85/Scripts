# Mit diesem Script sollen die Antworten der Schüler überprüft werden und die Note berechnet werden
import os, re, pprint, shelve
filepath=os.path.dirname(__file__)+r'\Fragebogen'

testfiles=(os.listdir(filepath))

load=shelve.open('data')
content=load['content']
load.close()
points=0
maxpoints=len(content)



def fragen():
	for filename in testfiles:
		global points
		points=0
		file=open(filepath+'\\'+filename)
		print(filename[:-4], end=' ')
		content=file.read()
		file.close()
		suchmuster=re.compile(r'  [^\#]+', re.DOTALL)
		gefunden=suchmuster.findall(content)
		checkboxinquest(gefunden)
		score=6/maxpoints*points
		print('Note:', round(score, 2))


		

def checkboxinquest(gefunden):
	for i in range(len(gefunden)):
		suchmuster=re.compile(r'(Was bedeutet[^\n]+)|(\[.*\][^\n]+)')
		fragebox=suchmuster.findall(gefunden[i])
		checkforX(fragebox)


def checkforX(fragebox):
	for i in range(len(fragebox)):
		if 'Was bedeutet' in fragebox[i][0]:
			frage=fragebox[i][0]
		suchmuster=re.compile(r'\[.\]( .+)')
		antworten=suchmuster.findall(fragebox[i][1])
		if len(antworten) != 0: 
			checkanswer(antworten)

def checkanswer(antwort):
	antwort=antwort[0].strip()
	if antwort in content.values():
		global points
		points+=1

fragen()



	

