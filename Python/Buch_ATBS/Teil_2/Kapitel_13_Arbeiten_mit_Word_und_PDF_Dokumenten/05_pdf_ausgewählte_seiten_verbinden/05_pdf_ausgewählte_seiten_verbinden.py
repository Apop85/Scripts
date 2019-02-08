# 05_pdf_ausgewählte_seiten_verbinden.py
# In diesem Übungsscript geht es darum Dass der Benutzer Seiten von zwei PDF's auswählen 
# kann und diese dann zu einem Neuen PDF kombiniert werden.

import os, PyPDF2
os.chdir(os.path.dirname(__file__))

pdf_file_1='.\\pdf_file_1.pdf'
pdf_file_2='.\\pdf_file_2.pdf'
target_file='.\\output.pdf'

# Auslesen der PDF-Files
pdf_file_1_open=open(pdf_file_1, 'rb')
pdf_file_2_open=open(pdf_file_2, 'rb')
pdf_1_content=PyPDF2.PdfFileReader(pdf_file_1_open)
pdf_2_content=PyPDF2.PdfFileReader(pdf_file_2_open)
pdf_1_max_pages=pdf_1_content.numPages
pdf_2_max_pages=pdf_2_content.numPages

# Auswahl der Seitenzahlen
while True:
    print('Bitte Seiten von PDF 1 auswählen, mehere Seiten mit Komma trennen:')
    pdf_1_choose_page=input('Nummer von 1 bis '+str(pdf_1_max_pages)+': ')
    print('Bitte Seiten von PDF 2 auswählen, mehere Seiten mit Komma trennen:')
    pdf_2_choose_page=input('Nummer von 1 bis '+str(pdf_2_max_pages)+': ')
    pdf_1_choose_list=pdf_1_choose_page.split(',')
    pdf_2_choose_list=pdf_2_choose_page.split(',')
    check=True
    for choice in pdf_1_choose_list:
        if int(choice) > pdf_1_max_pages:
            check=False
    for choice in pdf_2_choose_list:
        if int(choice) > pdf_2_max_pages:
            check=False
    if ''.join(pdf_1_choose_list).isdecimal() and ''.join(pdf_2_choose_list).isdecimal() and check:
        break
    else:
        print('\nEingabe entweder nicht Numerisch oder grösser als maximale Seitenzahl. Beispiel: 1,2,3,4\n')

# Dieser Loops sind so aufgebaut dass immer erst die Erste seite des ersten PDF-Files genommen wird, dann die erste Auswahl des zweiten PDF-Files und dann von vorne.
pdf_writer=PyPDF2.PdfFileWriter()
counter=[0, 0]
while True:
    # Prüfe ob i+1 der Zahl in der Liste der gewählten Seiten entspricht, falls ja kopiere Content in Zwischenspeicher
    for i in range(pdf_1_max_pages):
        if counter[0] < len(pdf_1_choose_list):
            if str(i+1) == pdf_1_choose_list[counter[0]]:
                page_content=pdf_1_content.getPage(i)
                pdf_writer.addPage(page_content)
                counter[0]+=1
                break
    for i in range(pdf_2_max_pages):
        if counter[1] < len(pdf_2_choose_list):
            if str(i+1) == pdf_2_choose_list[counter[1]]:
                page_content=pdf_2_content.getPage(i)
                pdf_writer.addPage(page_content)
                counter[1]+=1
                break
    if counter == [len(pdf_1_choose_list), len(pdf_2_choose_list)]:
        break

target_file_write=open(target_file, 'wb')
pdf_writer.write(target_file_write)
target_file_write.close()
