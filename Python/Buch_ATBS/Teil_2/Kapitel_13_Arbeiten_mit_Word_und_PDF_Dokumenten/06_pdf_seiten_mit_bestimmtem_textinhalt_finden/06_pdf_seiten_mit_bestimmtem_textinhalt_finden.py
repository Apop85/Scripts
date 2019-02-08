# 06_pdf_seiten_mit_bestimmtem_textinhalt_finden.py
# Dieses Übungsscript soll alle PDF-Dateien eines Unterordners durchsuchen, diese Auslesen 
# und wenn eine Seite den gesuchten String enthält in ein neues File speichern.

import os, PyPDF2, re
os.chdir(os.path.dirname(__file__))

target_path='.\\search_me'
target_file='.\\results.pdf'

if os.path.exists(target_file):
    os.remove(target_file)

print('Bitte Suchbegriff eingeben:')
search_string=input()

# Finde alle PDF's im Ordner search_me und in dessen Unterordner
file_list=[]
for path in os.walk(target_path):
    for i in range(len(path)):
        if len(path[i]) > 0 and type(path[i]) == list:
            for file_name in path[i]:
                possible_pdf=(path[0]+'\\'+file_name)
                if os.path.isfile(possible_pdf):
                    file_list+=[possible_pdf]

# Öffne alle PDF's im lesemodus        
write_pdf=PyPDF2.PdfFileWriter()
counter=0
for file_name in file_list:
    print('öffne File: '+file_name)
    pdf_file_open=open(file_name, 'rb')
    pdf_content=PyPDF2.PdfFileReader(pdf_file_open)
    for page in range(pdf_content.numPages):
        current_page=pdf_content.getPage(page)
        extracted=current_page.extractText()
        search_pattern=re.compile(r'.?'+search_string.lower()+r'.?')
        search_results=search_pattern.findall(extracted.lower())
        if len(search_results) > 0:
            write_pdf.addPage(current_page)
            counter+=1

target_file_open=open(target_file, 'wb')
write_pdf.write(target_file_open)
target_file_open.close()

print('Gefundene Einträge: '+str(counter))