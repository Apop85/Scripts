
# 04_pdf_verschluesseln.py
# In diesem Beispiel geht es Darum PDF-Dateien zu verschlüsseln um 
# sie vor fremdem Zugriff zu schützen

import os, PyPDF2
os.chdir(os.path.dirname(__file__))

source_file='.\\decrypted.pdf'
target_file='.\\encrypted.pdf'
password='123445'

source_file_open=open(source_file, 'rb')
source_content=PyPDF2.PdfFileReader(source_file_open)
page_amount=source_content.numPages

write_pdf=PyPDF2.PdfFileWriter()
for i in range(page_amount):
    current_page=source_content.getPage(i)
    write_pdf.addPage(current_page)

write_pdf.encrypt(password)
target_file_open=open(target_file, 'wb')
write_pdf.write(target_file_open)
target_file_open.close()