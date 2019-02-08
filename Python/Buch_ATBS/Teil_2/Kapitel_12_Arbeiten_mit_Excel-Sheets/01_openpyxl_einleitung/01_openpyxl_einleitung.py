# 01_openpyxl_einleitung.py
# In dieser Einleitung geht es um die Grundlagen des Moduls openpyxl. openpyxl ist
# standardmässig nicht installiert und muss mittels pip nachinstalliert werden. 

import os, openpyxl, re

os.chdir(os.path.dirname(__file__))

excel_sheet='.\\test_sheet.xlsx'

max_text_length=70
max_text_delta=18

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]')
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('load_workbook()', 'Mit openpyxl.load_workbook(\'<dateiname>\') kann die Exceltabelle eingelesen werden.')
work_sheet=openpyxl.load_workbook(excel_sheet)

output('Datentyp', 'Der Datentyp des mit load.worbook() geladenen Werts ist "workbook"')
print(type(work_sheet))

sheet_names=work_sheet.sheetnames
output('.sheetnames', 'mit .sheetnames lassen sich die Namen der Arbeitsblätter in dieser Arbeitsmappe abrufen die in diesem Beispiel wie folgt lauten '+str(sheet_names))

output('<excel_file>[\'Mappenname\']', 'Mit <excel_file>[\'Mappenname\'] lässt sich das dabei angegebene Arbeitsblatt einlesen.')
sheet_1=work_sheet['Tabelle1']

sheet_1=work_sheet.active
output('.active', 'Mit Dem Befehl .active kann die Arbeitsmappe ausgelesen werden die im Dokument zuletzt geöffnet war.')

sheet_A1=sheet_1['A1'].value
output('Auf Zellen zugreiffen', 'Mittels sheet_1[\'A1\'].value lässt sich beispielsweise auf die Zelle A1 des entsprechenden Excel-Sheets zugreiffen und auslesen. In diesem Beispiel ist der Eintrag der Zelle A1 folgender:  '+sheet_A1)

output('.row und .column', 'Mit den optionen .row und .colum lassen sich Spalte und Absatz des entsprechenden Eintrags ermitteln. In diesem beispiel sind das folgende:  Reihe:'+str(sheet_1['A1'].row)+' Spalte:'+str(sheet_1['A1'].column))

output('.coordinate', 'Die Option .coordinate gibt direkt die Koordinaten des Eintrags aus. Also A2 oder B5 ect. In diesem Beispiel wäre das wie folgt: '+str(sheet_1['A1'].coordinate))

sheet_B1=sheet_1.cell(row=1, column=2).value
output('celL()', 'Mit cell() hat man den Vorteil dass man die Koordinaten für die auszulesende oder zu bearbeitende Zelle numerisch angeben kann. In diesem Beispiel ist der Wert des Eintrags B1 folgender: '+str(sheet_B1))

output('Mehrere Zellen auslesen', 'Um eine Reihe komplett auszulesen verwendet man am besten einen For-Loop wie nachfolgend.')
for i in range(2, 8, 2):
    print(sheet_1.cell(row=i, column=2).value)
input()

sheet_1=work_sheet['Tabelle1']
maxrow=sheet_1.max_row
maxcolumn=sheet_1.max_column

output('max_row und max_column', 'Mit den Methoden max_column und max_row lassen sich die maximal verwendeten Reihen und Spalten ausgeben. In der Beispieltabelle wären das folgende Werte: Reihen:'+str(maxrow)+' Spalten:'+str(maxcolumn)+' Mit diesen Informationen kann man Scripte schreiben die ein Dokument vollständig durchsuchen können.')

letter=openpyxl.utils.cell.get_column_letter(199)
output('get_column_letter()', 'Mit get_column_letter() lässt sich Anzeigen welche Buchstabenkombination eine Spalte aufweist z.b. der Wert 188 ergibt: '+letter)

coulumn_int=openpyxl.utils.cell.column_index_from_string('AAB')
output('column_index_from_string()', 'Dies ist das Gegenteil von get_column_letter(). Hier gibt man einen String wie z.b. AAB ein und erhält dafür den Integer: '+str(coulumn_int))

output('Komplettes Auslesen', 'Mit den bisher erhaltenen Informationen ist es nun möglich die Komplette Tabelle auszulesen:')

for colums in sheet_1['A1':'C3']:
    for cell_values in colums:
        print(cell_values.coordinate, cell_values.value)
    print('Ende dieser Reihe')


sheet=work_sheet.active
columns=list(sheet)
output('work_sheet.active.colums', 'Man Kann auch direkt ganze Zeilen und Spalten auslesen durch .active_colums')
# for entry in colums:
#     print(entry.value)
for i in range(len(columns)):
    print(columns[i][1].value)
    
output('', '')
output('', '')
output('', '')