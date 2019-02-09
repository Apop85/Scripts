# 01_csv_grundlagen.py

import csv, os, re
os.chdir(os.path.dirname(__file__))

max_text_length=70
max_text_delta=20

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

output('CSV-Dateien', 'CSV ist die Abkürzung für "comma seperated values" also mit Komma voneinander getrennte Werte')

output('Inhalte einer CSV-Datei', 'Im gegensatz zu Python kennt CSV keine unterschiedlichen Werte. Alle EInträge werden daher als String ausgelesen.')

output('Aufbau', 'Eine CSV kann wie folgt aussehen: "2/5/2015 13:12,Apples,73" wobei Hier die drei Informationen Datum/Uhrzeit, Beschreibung und Anzahl als einzelne Einträge betrachtet werden. Man kann sich CSV also auch als vereinfachte Exceldarstellung betrachten.')

output('Formatierung', 'CSV-Dateien lassen keine Formatierung zu, weder zu Schriftgrösse noch -Farbe noch sonst was.')

output('Typ', 'Eine CSV-Datei ist eigentlich nichts anderes als eine reine Textdatei. Sie beinhaltet ausser den Daten keine weiteren Informationen und können daher im normalen Lesemodus ausgelesen werden.')

output('CSV in Python', 'Auch wenn Python auch roh mit CSV umgehen könnte ist es zu empfehlen dass man das csv-Modul verwendet da innerhalb von CSV auch Maskierungszeichen vorkommen können welche ohne Modul nur umständlich auszulesen wären.')

output('CSV auslesen', 'Um CSV-Daten auszulesen muss man erst einen reader einrichten mittels: csv_file=open("example.csv"), dann csv_reader=csv.reader(csv_file) und um anschliessend die Zeilen einzeln auszulesen list(csv_reader)')
csv_file=open('example.csv')
csv_reader=csv.reader(csv_file)
csv_list=list(csv_reader)
output('Beispiel', 'Die erste Zeile aus example.csv lautet:'+str(csv_list[0]))

output('Zugriff auf Daten', 'Da die Daten nun als Liste in Liste gespeichert sind kann man auf diese nun wie in einer Excel-Tabelle mit einem 2-Dimensionalen Koordinatensystem zugreiffen. Beispiel: csv_list[reihe][spalte] bei [1][1] wäre das:'+csv_list[1][1])

output('Auslesen aller Daten', 'Um alle Daten auszulesen Verwendet man einen Loop um an die Liste der Reihen zu kommen oder einen verschachtelten Loop um an die Inhalte dieser Listen zu kommen')
output('Zeilen Nummer auslesen', 'Um die Zeilennummer auszulesen kann man die Funktion .line_num verwenden.')
for i in range(len(csv_list)):
    print('Reihe: '+str(i+1)+' - '+str(csv_list[i]))
input()

output('CSV-Schreiben', 'Um in einer CSV-Datei schreiben zu können muss man erst einen Writer einrichten. csv_file=open(csv_file, "w", newline="") dann csv_writer=csv.writer(csv_file). Auch sollte man die Angabe newline="" einfügen da man sonst einen Doppelten Zeilenabstand erhält.')
csv_file_new=open('example_output.csv', 'w', newline='')
csv_file_writer=csv.writer(csv_file_new)
output('Zeile hinzufügen', 'Um eine Zeile in einer CSV-Datei hinzuzufügen verwendet man csv_file_writer.writerow(["a","b","c","d"]))')
csv_file_writer.writerow(["a","b","c","d"])
csv_file_writer.writerow(["e","f","g","h"])
csv_file_writer.writerow(["i","j","k","l"])

output('Datei speichern', 'Die Datei kann man sanschliessend speichern indem man sie an .close() übergibt. Bsp: csv_file_writer.close()')
csv_file_new.close()

output('delimiter und lineterminator', 'Man kann beim Öffnen der Datei auch noch zusätzlich die Formatierung der Tabelle selber übergeben. Dazu kann man delimiter="\\t" welches Beispielsweise die Daten mit einem Tabulator trennt statt mit einem Komma oder lineterminator="\\n\\n" um am Ende jedes Eintrags eine Zeile leer zu lassen.')
