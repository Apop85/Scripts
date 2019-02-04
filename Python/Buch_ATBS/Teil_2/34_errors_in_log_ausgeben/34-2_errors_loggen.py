# 34-2_errors_loggen.py
# In dieser Variante wird das Modul logging verwendet um Fehler aufzuspüren
import logging, re, os
os.chdir(os.path.dirname(__file__))
####################################### Modul logging ######################################
logging.basicConfig(level=logging.DEBUG, format=' %(asctime)s - %(levelname)s - %(message)s')
# Um das Logging zu deaktivieren verwendet man logging.disable(logging.CRITICAL)
# Alternativ kann man auch ein Logfile angeben in welchem die Fehler ausgegeben werden sollen.
# Dies kann man mit dem weiteren Argument filename='logname.log' bei basicConfig ein/ausschalten.

logging.info('Programmstart')

maxl, dv= 50, 15
def output(string):
    print(''.center(maxl, '█'))
    string+=' '*(maxl)
    suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
    ergebnis=suchmuster.findall(string)
    logging.debug('Ergebnis von output:  '+' '.join(ergebnis))
    for abschnitt in ergebnis:
        print(abschnitt)
    return input()

text='Hier ein einfaches Script welches ein Quadrat zeichnet und bei einer fehlerhaften Eingabe eine eigene Errormeldung verursacht.'
output(text)
for u in range(5):
    try:
        text='Gebe eine Zahl von 3 bis 10 ein.'
        choose=output(text)
        if int(choose) < 3:
            raise Exception('Die eingegebene Zahl ist zu klein.')
        elif int(choose) > 10:
            raise Exception('Die eingegebene Zahl ist zu gross.')
        elif not choose.isdecimal():
            raise Exception('Nur Zahlen von 3 bis 10 sind gültig.')
        else:
            logging.debug('Zeichne Viereck mit der Grösse: '+str(choose))
            choose=int(choose)
            print(''.center(choose, '█'))
            for i in range(choose//2):
                print('█'+''.center(choose-3), '█')
            print(''.center(choose, '█'))
    except Exception as error_message:
        logging.error('Spieler wählt '+str(choose)+'  '+str(error_message))
        print(error_message)
        continue
