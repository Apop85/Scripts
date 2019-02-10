# 05_kapitel_14_repetitionsfragen.py

import re

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

output('Frage 1', 'Welche Merkmale von Excel-Arbeitsblättern weisen CSV-Dateien nicht auf?')
output('Antwort', 'Jegliche Arten von Formatierungen. Man kann weder Schriftart noch -Grösse noch Zellenhöhe und -Breite verändern.')

output('Frage 2', 'Was übergeben sie csv.reader() oder csv.writer() um ein Reader- bzw. Writer-Objekt zu erstellen?')
output('Antwort', 'Für den Reader übergibt man csv_writer=open(file_name, \'r\') und für einen Writer übergibt man csv_writer=open(file_name, \'w\')')

output('Frage 3', 'In welchem Modus müssen File-Objekte für Reader- und Writerobjekte geöffnet werden?')
output('Antwort', 'Im Lese- ("r") oder Schreibmodus ("w"), da CSV-Dateien eigentlich reine Textdateien sind ist es nicht nötig diese als Binary zu öffnen.')

output('Frage 4', 'Welche Methode nimmt ein Listenargument entgegen und schreibt diese in eine CSV-Datei?')
output('Antwort', 'csv.writerow(row_list) nimmt eine Liste als Argument entgegen und schreibt diese entsprechend Formatiert in eine CSV-Datei.')

output('Frage 5', 'Was bewirken die Schlüsselargumente delimiter= und lineterminator=?')
output('Antwort', 'Der delimiter bestimmt wie die Werte voneinander getrennt sein sollen. CSV macht das normalerweise mit Kommas, man kann aber auch alles andere Verwenden indem man das bei delimiter= hinterlegt. Das Argument lineterminator definiert wie das Ende einer Zeile aussehen soll. Normalerweise ist das "\\n", dies kann man jedoch mit der Angabe der lineterminator ändern.')

output('Frage 6', 'Welche Funktion nimmt ein JSON-Datenstring entgegen und gibt eine Pythondatenstruktur zurück?')
output('Antwort', 'Aus dem Modul json die Funktion json.loads(). Dies wandelt json-Strings in ein für Python verständliches Dictionary um.')

output('Frage 7', 'Welche Funktion nimmt eine Python-Datenstruktur entgegen und gibt diese als JSON-Datenstring zurück?')
output('Antwort', 'Mit der Funktion json.dumps() kann ein Dictionary in das JSON-Format umgewandelt werden.')