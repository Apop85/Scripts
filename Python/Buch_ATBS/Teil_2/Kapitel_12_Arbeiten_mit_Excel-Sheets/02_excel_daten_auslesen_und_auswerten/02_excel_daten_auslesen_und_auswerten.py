# 02_excel_daten_auslesen_und_auswerten.py
# In diesem Beispiel geht es Darum eine Sehr lange Excel-Tabelle auszulesen
# und die Daten auszuwerten

import os, openpyxl
os.chdir(os.path.dirname(__file__))

search_file='.\\censuspopdata.xlsx'
work_sheet=openpyxl.load_workbook(search_file)
active_sheet=work_sheet.active
counter_dictionary={}

max_rows=active_sheet.max_row+1
max_colums=active_sheet.max_column+1

# Durchkämme komplette Excel-Tabelle und nehme Werte in ein Dictionary auf
for row_number in range(2,max_rows):
    for colum_number in range(2, max_colums):
        if colum_number == 2:
            counter_dictionary.setdefault(active_sheet.cell(row=row_number, column=colum_number).value, {})
        elif colum_number == 3:
            counter_dictionary[active_sheet.cell(row=row_number, column=colum_number-1).value].setdefault(active_sheet.cell(row=row_number, column=colum_number).value, 0)
        elif colum_number == 4:
            counter_dictionary[active_sheet.cell(row=row_number, column=colum_number-2).value][active_sheet.cell(row=row_number, column=colum_number-1).value]+=active_sheet.cell(row=row_number, column=colum_number).value

total=0
# Auswertung des Dictionarys.
save_file='.\\auswertung.txt'
write_save=open(save_file, 'w', encoding='UTF-8')
for bundesland in counter_dictionary:
    write_save.write('\n╔═>>> '+bundesland+'\n')
    print('\n╔═>>> '+bundesland)
    amount_of_countys, bundesland_counter=0, 0
    for county in counter_dictionary[bundesland]:
        write_save.write('╠════ '+county+': '+str(counter_dictionary[bundesland][county])+'\n')
        print('╠════ '+county+': '+str(counter_dictionary[bundesland][county]))
        bundesland_counter+=int(counter_dictionary[bundesland][county])
        total+=int(counter_dictionary[bundesland][county])
        amount_of_countys+=1
    print('╠════════════════ Anzahl Countys: '+str(amount_of_countys))
    print('╚═>>> Total für den Bundesstaat '+str(bundesland)+': '+str(bundesland_counter))
    write_save.write('╠════════════════ Anzahl Countys: '+str(amount_of_countys)+'\n')
    write_save.write('╚═>>> Total für den Bundesstaat '+str(bundesland)+': '+str(bundesland_counter)+'\n')
print((' Total: '+str(total)+' ').center(50, '▒'))
write_save.write((' Total: '+str(total)+' ').center(50, '▒')+'\n')
write_save.close()