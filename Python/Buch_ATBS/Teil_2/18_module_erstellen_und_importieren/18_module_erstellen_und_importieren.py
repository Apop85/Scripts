# Hier werden wir mit pprint.format() eine neue Pythondatei erstellen und diese dann als Modul wieder importieren
import pprint, os, re
maxl=50
dv=15

def output(string):
    string=str(string)
    delta=len(string)%maxl
    print(''.center(maxl+2, '█'))
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[\S|\\n].{'+str(maxl-dv)+r','+str(maxl)+r'}[ |{|}|:|;]', re.DOTALL)
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i].strip(' '))
    print(''.center(maxl+2, '█'))
    # input()

dictionary=[{"Ratten": 5, r"Maus": 12}, {"Tiger": 1, "Katze": 4}, { "Hund": 2, 'Wolf': 1 }]

string='Das Modul pprint.format() erstellt eine Ausgabe welche von Python interpretiert werden kann, daher ist dieses Modul optimal um damit beispielsweise ein eigenes Modul zu erstelen oder automatisch andere Pythonscripts zu erzeugen.'
output(string)
# input()
if os.path.exists('meinmodul.py'):
    os.remove('meinmodul.py')

string='Folgendes Dictionary wird nun in die Datei meinmodul.py geschrieben: '+ str(dictionary)
output(string)

myfile=open('meinmodul.py', 'w')
myfile.write('export = ' + pprint.pformat(dictionary) + '\n')
myfile.close()

string='Nun kann das Pythonfile wiederum als eigenes Modul aufgerufen werden um die entsprechenden Inhalte daraus auszulesen.'
output(string)

import meinmodul

ausmodul=meinmodul.export

string='Folgendes wurde nun aus dem Modul meinmodul ausgelesen:' + str(ausmodul)
output(string)

del meinmodul
os.remove('meinmodul.py')


