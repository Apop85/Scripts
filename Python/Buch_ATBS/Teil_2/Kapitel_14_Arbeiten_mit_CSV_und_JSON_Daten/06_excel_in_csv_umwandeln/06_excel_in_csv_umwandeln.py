# 06_excel_in_csv_umwandeln.py
# In dieser Übung geht es darum eine Excel-Tabelle einzulesen und als CSV-Datei wieder auszugeben

import os, csv, openpyxl
os.chdir(os.path.dirname(__file__))

source_file='.\\example.xlsx'
target_file='.\\example.csv'

excel_file=openpyxl.load_workbook(source_file)
excel_sheet=excel_file.active
max_rows=excel_sheet.max_row
max_colums=openpyxl.utils.get_column_letter(excel_sheet.max_column)

csv_file=open(target_file, 'w', newline='')
csv_writer=csv.writer(csv_file)
for i in range(max_rows):
    row=excel_sheet['A'+str(i+1)+':'+max_colums+str(i+1)]
    for cell_object in row:
        line=[]
        for entry in cell_object:
            line+=[entry.value]
        csv_writer.writerow(line)
csv_file.close()
