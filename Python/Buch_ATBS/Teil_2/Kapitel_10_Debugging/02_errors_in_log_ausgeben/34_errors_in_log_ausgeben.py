# 34_errors_in_log_ausgeben.py
# Um eine Fehlermeldung in ein log schreiben zu lassen kann man das Modul traceback importieren
import traceback, re, os
os.chdir(os.path.dirname(__file__))

maxl, dv= 50, 15
def output(string):
    print(''.center(maxl, '█'))
    string+=' '*(maxl)
    suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
    ergebnis=suchmuster.findall(string)
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
            choose=int(choose)
            print(''.center(choose, '█'))
            for i in range(choose//2):
                print('█'+''.center(choose-3), '█')
            print(''.center(choose, '█'))
    except Exception as error_message:
        error_file=open('error.log', 'w')
        error_file.write(traceback.format_exc())
        error_file.close()
        print('Fehler wurde in Logfile geschrieben.')
        continue
