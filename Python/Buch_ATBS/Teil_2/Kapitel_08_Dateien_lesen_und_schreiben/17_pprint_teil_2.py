# Mit pprint lassen sich auch Dictionarys alphabetisch sortiert ausgeben
import pprint, os, re

maxl=50
dv=18

def output(string):
    delta=len(string)%maxl
    print(''.center(maxl+2, '█'))
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[\S|\\n].{'+str(maxl-dv)+r','+str(maxl)+r'}[ ]', re.DOTALL)
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i])
    print(''.center(maxl+2, '█'))
    input()

periodensystem={ 'H' : 1.0079, 'He' : 4.0026,
                 'Li' : 6.941, 'Be' : 9.0122, 'B' : 10.811, 'C' : 12.011, 'N' : 14.007, 'O' : 15.999, 'F' : 18.988, 'Ne' : 20.180,
                 'Na' : 22.990, 'Mg' : 24.305, 'Al' : 26.982, 'Si' : 28.086, 'P' : 30.974, 'S' : 32.065, 'Cl' : 35.453, 'Ar' : 39.948,
                 'K' : 39.098, 'Ca' : 40.078, 'Sc' : 44.956, 'Ti' : 47.867, 'V' : 50.942, 'Cr' : 51.996, 'Mn' : 54.938, 'Fe' : 55.845, 'Co' : 58.933, 'Ni' : 58.693, 'Cu' : 63.546, 'Zn' : 65.38, 'Ga' : 69.723, 'Ge' : 72.64, 'As' : 74.922}


string='Mit der Funktion pformat(STRING) des Moduls pprint lassen sich auch Dictionarys formatiert und alphabetisch sortiert ausgeben.'
output(string)
print(pprint.pformat(periodensystem))
input()

string='pformat(STRING) gibt den Inhalt in einem Gültigen Pythonformat aus, deshalb ist es auch möglich die ausgabe von pformat in eine extertne Pythondatei zu schreiben und als eigenes Modul später wieder zu importieren. \n\nDazu mehr in Projektdatei 18_*.py'
output(string)
