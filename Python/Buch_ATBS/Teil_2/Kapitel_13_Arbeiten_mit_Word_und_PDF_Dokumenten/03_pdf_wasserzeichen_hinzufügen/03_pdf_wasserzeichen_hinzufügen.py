# 03_pdf_wasserzeichen_hinzufügen.py
# In diesem Beispiel geht es darum ein Wasserzeichen auf allen Seiten eines PDF's zu Hinterlegen.

import PyPDF2, os
os.chdir(os.path.dirname(__file__))

watermark_file='.\\watermark.pdf'
source_file='.\\watermark_me.pdf'
target_file='.\\watermarked.pdf'

watermark_file_open=open(watermark_file, 'rb')
source_file_open=open(source_file, 'rb')
watermark_content=PyPDF2.PdfFileReader(watermark_file_open)
source_file_content=PyPDF2.PdfFileReader(source_file_open)
max_source_pages=source_file_content.numPages

pdf_writer=PyPDF2.PdfFileWriter()
for i in range(max_source_pages):
    page_content=source_file_content.getPage(i)
    page_content.mergePage(watermark_content.getPage(0))
    pdf_writer.addPage(page_content)

target_file_open=open(target_file, 'wb')
pdf_writer.write(target_file_open)
target_file_open.close()