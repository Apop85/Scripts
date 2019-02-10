# 06_kapitel_15_repetitionsfragen.py

import re

max_text_length=70
max_text_delta=24

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Frage 1', 'Was ist die UNIX-Zeitepoche?')
output('Antwort', 'Die UNIX-Zeitepoche begann am 1.1.1970 und zählt die seit dann vergangenen Sekunden.')

output('Frage 2', 'Welche Funktion gibt die Anzahl Sekunden zurück seit beginn der UNIX-Epoche?')
output('Antwort', 'time.time()')

output('Frage 3', 'Wie können sie ein Programm genau 5 Sekunden lang anhalten?')
output('Antwort', 'time.sleep(5)')

output('Frage 4', 'Was gibt die Funktion round() zurück?')
output('Antwort', 'Ein gerundeter Wert der übergebenen Float-Zahl. round() nimmt zwei Argumente entgegen. Als erstes den zu rundenden Wert und als zweites die Anzahl anzuzeigenden Kommastellen. Standardmässig gibt es eine Zahl ohne Kommastellen zurück')

output('Frage 5', 'Was ist der Unterschied zwischen einem datetime- und einem timedelta-Objekt?')
output('Antwort', 'datetime steht für einen bestimmten Zeitpunkt während timdelta eine Zeitdauer definiert.')

output('Frage 6', 'Wie können sie eine Funktion namens spam() aufrufen und dessen Code in einem eigenen Thread ausführen?')
output('Antwort', 'Dazu muss das Modul threading importiert sein und damit kann man spam() mittels threading.Thread(target=spam).start() als Thread starten')

output('Frage 7', 'Was sollten sie tun, um in Programmen mit mehreren Threads Nebenläufigkeitsprobleme zu vermeiden?')
output('Antwort', 'Man muss vermeiden dass der Thread die selben Variabeln versucht zu lesen und schreiben wie andere Threads.')

output('Frage 8', 'Wie sorgen sie dafür, dass ein Python-Programm das Programm calc.exe im Ordner C:\\Windows\\System32\\ ausführt?')
output('Antwort', 'Dazu muss das Modul subprocess importiert werden welches dann miitels subprocess.Popen("C:\\Windows\\System32\\calc.exe") die Anwendung Calc öffnet.')
