# 37_kapitel_10_repetitionsfragen.py
import re
maxl, dv= 50, 19
def output(string):
    print(''.center(maxl, '█'))
    delta=len(string) % maxl
    string+=' '*(maxl)
    suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
    ergebnis=suchmuster.findall(string)
    for abschnitt in ergebnis:
        print(abschnitt)
    return input()

frage='1. Schreiben sie eine assert Anweisung die einen AssertError auslöst wenn die Variabel spam klainer als 10 ist.'
output(frage)
antwort='assert spam >= 10, \'Spam sollte über 10 sein!\''
output(antwort)
print('\n')

frage='2. Schreiben sie eine assert Anweisung die einen AssertError auslöst wenn in der Variabel spam und bacon die gleichen Strings enthalten sind und welche die Grosschreibung ignoriert.'
output(frage)
antwort='assert bacon.lower() != spam.lower(), \'Bacon sollte nicht identisch mit Spam sein\''
output(antwort)
print('\n')

frage='3. Schreiben sie eine assert Anweisung die immer einen AssertError auslöst.'
output(frage)
antwort='assert False, \'Fehler weil... fehler?\''
output(antwort)
print('\n')

frage='4. Welche beiden Zeilen müssen enthalten sein damit logging.debug() aufgerufen werden kann?'
output(frage)
antwort='import logging und                               logging.basicConfig(logging=logging.DEBUG, format=\' %(asctime)s - %(levelname)s - %(message)s\')'
output(antwort)
print('\n')

frage='5. Welche beiden zeilen müssen enthalten sein damit logging.debug() in ein Logfile schreibt?'
output(frage)
antwort='import logging und                               logging.basicConfig(logging=logging.DEBUG, filename=logfile.log, format=\' %(asctime)s - %(levelname)s - %(message)s\''
output(antwort)
print('\n')

frage='6. Welche 5 Protokolliergrade gibt es?'
output(frage)
antwort='DEBUG, INFO, WARNING, ERROR, CRITICAL'
output(antwort)
print('\n')

frage='7. Mit welcher Codezeile können sie sämtliche logging-Aktivitäten deaktivieren?'
output(frage)
antwort='logging.disable(logging.CRITICAL)'
output(antwort)
print('\n')

frage='8. Warum ist es besser Protokollmeldungen zu verwenden anstatt die print()-Funktion?'
output(frage)
antwort='Protokollmeldungen können mit einer Zeile deaktiviert werden und die möglichkeit so einen Printaufruf unbeabsichtigt zu löschen oder ändern ist ausgeschlossen. Auch besitzen Protokolle Zeitstempel und können in unterschiedlichen Schweregraden an oder abgeschalten werden.'
output(antwort)
print('\n')

frage='9. Welche unterschiede gibt es beim IDLE-Debugger zwischen Step, Over und Out?'
output(frage)
antwort='Step=Nächste Zeile aufrufen, Over=Codezeile oder Aufruf überspringen, Out=Erst nach abschluss der Unterfunktion wieder anhalten.'
output(antwort)
print('\n')

frage='10. Wann hält der Debugger an wenn sie im Kontrollfenster auf Go geklickt haben?'
output(frage)
antwort='Falls ein Breakpoint gesetzt wurde an dieser stelle ansonsten läuft das Script zum Ende durch.'
output(antwort)
print('\n')

frage='11. Was ist ein Haltepunkt?'
output(frage)
antwort='Ein Haltepunkt markiert eine Stelle im code an welchem der Debugger halten soll um von dort an manuell fortzufahren.'
output(antwort)
print('\n')

frage='12. Wie setzen sie in IDLE einen Haltepunkt in die Codezeile?'
output(frage)
antwort='Rechtsklick auf die Zeile und dann set breakpoint'
output(antwort)