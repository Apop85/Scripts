# 01_grundlagen_modul_time_und_datetime.py

import os, re, time, datetime

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

start_time=time.time()
output('Time Modul', 'Mit dem Modul time lassen sich unterschiedliche Zeitbezogene Aufgaben erfüllen. Man kann eine Pause im Code hinterlegen oder die Zeit auslesen wie lange ein Codeabschnitt zum verarbeiten gebraucht hat.')
read_time=round(time.time()-start_time, 2)
output('time.time()', 'Um zu berechnen wie Lange ein Codeabschnitt benötigt hat um abgearbeitet zu werden kann man time.time() eingeben. Dies gibt die Absolute Zeit in Sekunden bis zum mehrstelligen Kommabereich seit Beginn der UNIX-Epoche zurück. Wenn man also Die Startzeit nimmt und diese dann von der aktuellen Zeit abzieht kriegt man raus wiviele Sekunden es benötigt hat. Beispielsweise hast du '+str(read_time)+' Sekunden benötigt den ersten Text zu lesen.')

output('time.sleep()', 'Mit der Sleep-Funktion von time können sie das Programm für eine angegebene Zeit pausieren.')
time.sleep(1)
print('Tick')
time.sleep(1)
print('Tock')
time.sleep(1)

output('datetime', 'Das Modul datetime kann man nutzen wenn man das aktuelle Datum mit Uhrzeit auslesen möchte anstatt die UNIX-Zeit.')

output('.now()', 'Die Funktion datetime.now() gibt das Aktuelle Datum sowie die Systemzeit bis auf die Mikrosekunde zurück. Beispiel: '+str(datetime.datetime.now()))

output('.year .month .day', 'Wird datetime.now() beispielsweise in die Variabel now gespeichert so kann man mit now.year, now.month und now.day das Datum auslesen. Beispiel: '+str(datetime.datetime.now().day)+'.'+str(datetime.datetime.now().month)+'.'+str(datetime.datetime.now().year))
output('.hour .minute', 'Selbiges gillt auch für die Stunde und die Minute. Beispiel:'+str(datetime.datetime.now().hour)+':'+str(datetime.datetime.now().minute))

output('Vergleich', 'datetime-Objekte kann man auch mit >, >=, <, <= und == miteinander vergleichen um herauszufinden ob ein Datum älter oder jünger als das andere ist.')

output('.timedelta()', 'Das Modul datetime verfügt auch über die Funktion .timedelta(). Damit kann man eine Zeitangabe in das time.time() format umrechnen. Die einträge lassen sich ebenfalls mit den Selben Attributen aufrufen wie beim Auslesen des Monats/Yahrs. Beispielsweise dauert ein Tag '+str(datetime.timedelta(days=0, hours=24, minutes=0, seconds=0).total_seconds())+' Sekunden')

output('Ab bestimmtem Zeitpunkt ausführen', 'Man kann datetime auch nutzen um bestimmte Programmabschnitte nur an einem bestimmten Datum zu nutzen. man kann ein datetime objekt erstellen mit z.b. (2020, 5, 27, 12, 0, 0) und dann mit einer If abfrage prüfen ob .datetime.now() gleich dem Daytimeobjekt ist und falls ja etwas bestimmtes ausführen.')

output('.strftime()', 'Mit der Funktion .strftime() kann man die eher unleserliche Ausgabe von datetime in ein leserliches Format umwandeln. Beispielsweise wird die Ausgabe '+str(datetime.datetime.now())+' in '+datetime.datetime.now().strftime('%d.%m.%Y %H:%M:%S')+' umgewandelt.')
output('.strftime() syntax', 'Um die gewünschte Fomatierung zu erhalten verwendet man folgende Direktiven: %Y für Jahr mit Jahrhundert angabe, %y für Jahr ohne Jahrhundertangabe, %m Monat als Dezimalzahl, %B Ausgeschriebener Name, %b abgekürzter Monatsname, %d Tag im Monat als Dezimalzahl, %j Tag im Jahr (x/366), %w Tag in der Woche (0-6), %A ausgeschriebener Wochentag, %a abgekürzter Name des Wochentags, %H Stunde (24h System), %I Stunde (12h System), %M Minute, %S Sekunde, %p pm oder am und %% um das %-Zeichen zu maskieren.')

output('.strptime()', 'Man kann formatierte Datumsangaben auch auslesen indem man diese mit der Funktion .strptime() ausliest. Beispiel: .strptime(\'25.11.2015 12:45\', \'%d.%m.%Y %H:%M\') ergibt: '+str(datetime.datetime.strptime('25.11.2015 12:45', '%d.%m.%Y %H:%M')))
