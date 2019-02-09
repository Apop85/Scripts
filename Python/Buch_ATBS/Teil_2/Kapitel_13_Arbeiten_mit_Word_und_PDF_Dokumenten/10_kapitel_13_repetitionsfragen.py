# 10_kapitel_13_repetitionsfragen.py

import re

max_text_length=70
max_text_delta=20

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Frage 01', 'Der Funktion PyPDF2.PdfFileReader() übergeben sie nicht den Stringwert mit dem Namen der PDF-Datei. Was übergeben sie statdessen?')
output('Antwort', 'Man übergibt die Variabel welche die geöffnete Datei im binary Modus beinhaltet. Bsp: pdf_file_content = PyPDF2.PdfFileReader(open(pdf_file.pdf, "rb"))')

output('Frage 02', 'In welchen Modi müssen File-Objekte für PdfFileReader() und PdfFileWriter() geöffnet sein?')
output('Antwort', 'Read Binary bzw "rb" Beispiel: open(pdf_file.pdf, "rb") oder im Modus Write-Binary für .PdfFileWriter()')

output('Frage 03', 'Wie rufen sie dsa Page-Objekt für die Seite 5 von einem PdfFileReader-Objekt ab?')
output('Antwort', 'Man nutzt page_5=pdf_file_content.getPage(4) wobei ein PDF-Dokument immer mit der Seitenzahl 0 beginnt und daher getPage(4) für die 5. Seite verwendet werden muss.')

output('Frage 04', 'In welcher PdfFileReader-Variable ist die Anzahl der Seiten in einem PDF-Dokument gespeichert?')
output('Antwort', 'pdf_file_content.numPages liest die Anzahl Seiten des PDF\'s aus')

output('Frage 05', 'Was müssen sie tun, bevor sie die Page-Objekte von einem PdfFileReader-Objekt abrufen können, dessen PDF mit dem Passwort "swordfish" geschützt ist?')
output('Antwort', 'Mit pdf_file_content.decrypt("swordfish") lässt sich die Datei entschlüsseln. Mit .encrypt(passwort) wieder verschlüsseln')

output('Frage 06', 'Welche Methoden verwenden sie, um eine Seite zu drehen?')
output('Antwort', 'Die Seite lässt sich mit page_5.rotateClockwise(90) drehen.')

output('Frage 07', 'Welche Methode gibt ein Document-Objekt für die Datei demo.docx zurück?')
output('Antwort', 'Mit doc_file=docx.Document("demo.docx") lässt sich demo.docx auslesen und in einer Variabel speichern.')

output('Frage 08', 'Was ist der Unterschied zwischen einem Paragraphen- und einem Run-Objekt?')
output('Antwort', 'Die Paragraphen beinhalten den Kompletten Text bis zum nächsten Zeilenumbruch und ist selber wiederum in Runs unterteilt welche das Aussehen der Textabschnitte bestimmt. Jedes mal wen sich die Formatierung ändert entsteht ein neuer Run.')

output('Frage 09', 'Wie rufen sie die Liste der Paragraphen-Objekte für ein Dokument ab das in der Variabel "doc" gespeichert ist?')
output('Antwort', 'Die Funktion doc.paragraphs gibt alle Paragraphen-Objekte als Liste aus')

output('Frage 10', 'Was für Objekte verfügen über die Variablen bold, underline, italic, strike und outline?')
output('Antwort', 'Diese Variablen gehören zum Run-Objekt und definieren ob der Text fett, unterstrichen, schräg, durchgestrichen oder outlined ist. Beispiel: run_objekt.italic=True')

output('Frage 11', 'Was ist der Unterschied zwischen den Werten True, False und None für die Variable bold?')
output('Antwort', 'bold=True heisst dass der Text fett geschrieben wird, bold=False heisst er wird nicht Fett dargestellt und bold=None verwendet die Standardwerte des Run-Objekts')

output('Frage 12', 'Wie erstellen sie ein Document-Objekt für ein neues Word_Dokument?')
output('Antwort', 'Ein neues Dokument kann man mit docx.Document() erstellen')

output('Frage 13', 'Wie fügen sie einem Document-Objekt in der Variablen doc einen Absatz mit dem Text "Hello there" hinzu?')
output('Antwort', 'Mit doc.add_paragraph("Hello there") lässt sich ein neuer Abschnitt mit dem entsprechenden Textinhalt erstellen.')

output('Frage 14', 'Welche Integerwerte können sie verwenden, um in einem Word-Dokument Überschriften-Ebenen anzugeben?')
output('Antwort', 'Mit doc_file.add_heading("Header Text", 0-4) lassen sich Header einfügen.')
