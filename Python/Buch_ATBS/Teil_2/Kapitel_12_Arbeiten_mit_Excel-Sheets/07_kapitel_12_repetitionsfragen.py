# 07_kapitel_12_repetitionsfragen.py

import os, re

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

output('Frage 1', 'Was gibt die Funktion openpycl.load_workbook() zurück?')
output('Antwort', 'load_workbook() erstellt ein Workbook-Objekt mit den entsprechenden Tabelleninhalten.')

output('Frage 2', 'Was gibt die Workbook-Methode .sheetnames?')
output('Antwort', 'Mit .sheetnames lassen sich die Namen der angelegten Arbeitsblätter ausgeben.')

output('Frage 3', 'Wie rufen sie das Worksheet-Objekt für das Arbeitsblatt "Sheet 1" ab?')
output('Antwort', 'mittels excel_sheet["Sheet 1"]')

output('Frage 4', 'Wie rufen sie das Worksheet-Objekt für das aktuelle Arbeitsblatt der Arbeismappe ab?')
output('Antwort', 'work_sheet.active')

output('Frage 5', 'Wie rufen sie den Wert der Zelle C5 ab?')
output('Antwort', 'work_sheet["C5"].value oder mit work_sheet.cell(row=5, column=3).value')

output('Frage 6', 'Wie setzen sie den Wert in der Zelle C5 auf "Hallo?')
output('Antwort', 'work_sheet["C5"]="Hallo" oder mit work_sheet.cell(row=5, column=3, value="Hallo")')

output('Frage 7', 'Wie rufen sie die Zeile und die Spalte einer Zelle als Integerwerte ab?')
output('Antwort', 'work_sheet["C5"].cell.row gibt die Reihe aus und work_sheet["C5"].cell.column die Spalte')

output('Frage 8', 'Was geben die Arbeisblattmethoden .max_row und .max_column zurück?')
output('Antwort', 'Diese Methoden geben einen Integerwert zurück für die Anzahl verwendeten Reihen und Spalten in einer Tabelle.')

output('Frage 9', 'Welche Funktion müssen sie aufrufen, um den Stringnamen der Spalte 14 zu ermittlen?')
output('Antwort', 'openpyxl.utils.get_column_letter(14)')

output('Frage 10', 'Welche Funktion müssen sie aufrufen um den Integerindex der Spalte "M" zu ermitteln?')
output('Antwort', 'openpyxl.utils.column_index_by_string("M")')

output('Frage 11', 'Wie können sie ein Tupel aller Cell-Objekte von A1 bis F1 abrufen?')
output('Antwort', 'Mittels tupel_objekt=work_sheet["A1:F1"]')

output('Frage 12', 'Wie speichern sie eine Arbeitsmappe unter dem Dateinamen example.xlsx?')
output('Antwort', 'work_sheet.save("example.xlsx")')

output('Frage 13', 'Wie fügen sie in einer Zelle eine Formel ein?')
output('Antwort', 'Wie normale Einträge mit work_sheet["A1"]="=A2+A3"')

output('Frage 14', 'Was müssen sie als erstes tun um das Ergebnis einer Formel in einer Zelle abzurufen statt in der Formel als solcher?')
output('Antwort', 'Man muss die Datei mit openpyxl.load_worksheet("exceldatei.xlsx", data_only=True) öffnen')

output('Frage 15', 'Wie setzen sie die Höhe der Zeile 5 auf 100?')
output('Antwort', 'Dazu muss man works_heet.row_dimensions[5].height = 100 verwenden.')

output('Frage 16', 'Wie können sie die Spalte C verstecken?')
output('Antwort', 'mittels work_sheet.column_dimensions["C"]=True')

output('Frage 17', 'Was Lädt OpenPyXl 2.1.4 nicht aus einer Arbeitsmappen-Datei?')
output('Antwort', 'Diagramme und Medien')

output('Frage 18', 'Was ist ein fixierter Bereich?')
output('Antwort', 'Ein Fixierter Bereich ist immer Sichtbar. Wenn man z.b. möchte dass die 1. Reihe immer sichtbar bleibt muss man folgenden Befehl verwenden: work_sheet.freeze_panes("A1")')

output('Frage 19', 'Welche 5 Funktionen und Methoden müssen sie aufrufen um ein Balkendiagramm zu erstellen?')
output('Antwort', 'Positionsangabe durch: diagram.openpyxl.Reference(work_sheet, min_row=1, min_column=1, max_row=15, max_column=2). Dann Definition der Daten mit diagram.openpyxl.chart.Series(title="Datenname"). Und schlussendlich noch den Diagramtyp definieren diagram.openpyxl.chart.LineChart()')

