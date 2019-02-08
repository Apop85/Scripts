# 03_excel_sheet_beschreiben.py
# Dieses Script dient als Beispiel wie man Daten in Excel-Dateien schreibt.

import os, openpyxl
os.chdir(os.path.dirname(__file__))

excel_file='.\\test_file.xlsx'
if os.path.exists(excel_file):
    os.remove(excel_file)

excel_sheet=openpyxl.Workbook()
sheet_names=excel_sheet.sheetnames
print(sheet_names)
active_sheet=excel_sheet[sheet_names[0]]

# Mit ".title =" kann man den Titel der aktuellen Arbeitsmappe festlegen.
titel='ATBS with Python'
active_sheet.title = titel
print(excel_sheet.sheetnames)

# Mit ".create_sheet(sheetname)" und .remove(sheetname) oder del excel_sheet[sheetname] lassen sich Arbeitsmappen hinzufügen und entfernen.
excel_sheet.create_sheet('Temp Sheet')
excel_sheet.save(excel_file)
print(excel_sheet.sheetnames)

# Man kann auch ein neues Sheet zwischen existierende Sheets erzeugen indem man die Positions des Sheets mitangibt.
excel_sheet.create_sheet('Example_Sheet', index=1)
print(excel_sheet.sheetnames)
del excel_sheet['Temp Sheet']
print(excel_sheet.sheetnames)

# Um Werte in Zellen zu schreiben kann man identisch vorgehen wie bei Dictionarys mit active_sheet[zellenkoordinate]=eintrag
active_sheet['A1']=titel
print(active_sheet['A1'].value)

# Am Ende jedes Schreibprouzesses wird das File mit .save(savename) gespeichert. 
excel_sheet.save(excel_file)

