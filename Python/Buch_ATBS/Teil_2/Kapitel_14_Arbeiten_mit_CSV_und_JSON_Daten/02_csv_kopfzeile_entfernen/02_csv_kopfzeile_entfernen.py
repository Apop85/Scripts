# 02_csv_kopfzeile_entfernen.py
# In diesem Anwendungsbeispiel geht es darum Alle CSV-Dateien in einem Ordner zu finden und aus all diesen Dateien die Kopfzeile zu entfernen.

import csv, os
os.chdir(os.path.dirname(__file__))

target_dir='.\\search_me'

file_list_raw=os.listdir(target_dir)

# Finde alle CSV-Dateien im Unterordner .\search_me
pure_file_list=[]
for possible_csv_file in file_list_raw:
    if possible_csv_file.endswith('.csv'):
        if os.path.isfile(target_dir+'\\'+possible_csv_file):
            pure_file_list+=[target_dir+'\\'+possible_csv_file]

for i in range(len(pure_file_list)):
    csv_file=open(pure_file_list[i], 'r')
    csv_reader=csv.reader(csv_file)
    csv_content=list(csv_reader)
    csv_file.close()
    os.remove(pure_file_list[i])
    csv_file=open(pure_file_list[i], 'w', newline='')
    csv_writer=csv.writer(csv_file)
    for z in range(len(csv_content)):
        if z == 0:
            continue
        csv_writer.writerow(csv_content[z])


