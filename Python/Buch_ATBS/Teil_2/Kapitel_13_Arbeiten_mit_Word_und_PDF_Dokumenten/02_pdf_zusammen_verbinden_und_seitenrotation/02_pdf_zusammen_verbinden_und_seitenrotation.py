# 02_pdf_zusammen_verbinden.py
# In diesem Beispiel geht es Darum zwei PDF's miteinander zu verschmelzen

import os, PyPDF2
os.chdir(os.path.dirname(__file__))

source_file_1='.\\meetingminutes.pdf'
source_file_2='.\\meetingminutes2.pdf'
output_file='.\\meetingminutes_merged.pdf'
output_file_2='.\\meetingminutes_rotated.pdf'

if os.path.exists(output_file):
    os.remove(output_file)

# Bereite Files zum Lesen vor und lese Seitenzahlen aus. 
file_1_open=open(source_file_1, 'rb')
file_2_open=open(source_file_2, 'rb')
file_1_content=PyPDF2.PdfFileReader(file_1_open)
file_2_content=PyPDF2.PdfFileReader(file_2_open)
pages_file_1_length=file_1_content.numPages
pages_file_2_length=file_2_content.numPages

# Aktiviere PfdFileWriter()
pdf_writer=PyPDF2.PdfFileWriter()
pdf_writer_2=PyPDF2.PdfFileWriter()

# Schreibe Inhalt von PDF 1 in Zwischenspeicher
for page in range(pages_file_1_length):
    page_content=file_1_content.getPage(page)
    pdf_writer.addPage(page_content)
    # Drehe Seite um 90°
    page_content.rotateClockwise(180)
    pdf_writer_2.addPage(page_content)

# Schreibe Inhalt von PDF 2 in Zwischenspeicher
for page in range(pages_file_2_length):
    page_content=file_2_content.getPage(page)
    pdf_writer.addPage(page_content)

# Schreibe neues PDF
output_file_open=open(output_file, 'wb')
pdf_writer.write(output_file_open)
output_file_open.close()

output_file_2_open=open(output_file_2, 'wb')
pdf_writer_2.write(output_file_2_open)
output_file_2_open.close()