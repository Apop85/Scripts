# 01_einleitung_pypdf2.pypdf2.py
# Um PDF's zu lesen benötigt man das Modul PyPDF2 welches man mit pip gegebenenfalls nachinstallieren muss.

import PyPDF2, os, re
os.chdir(os.path.dirname(__file__))

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

target_file='.\\meetingminutes.pdf'

output('PyPDF2', 'PyPDF schafft es die meisten PDF\'s zu öffnen und zu interpretieren. Es kann jedoch keine Bilder, Grafiken oder ähnliche Elemente aus einem PDF extrahieren.')
output('Einlesen', 'Um ein PDF einzulesen kann man es normal mit der Funktion open() öffnen muss jedoch noch das Argument \'rb\' angeben um zu signalisieren dass die eingelesenen Daten binär sind.')
pdf_file=open(target_file, 'rb')

output('PyPDF2.PdfFileReader()', 'Um den Inhalt eines PDF\'s auszulesen verwendet man die Methode PyPDF2.PdfFileReader(pdf_file)')
pdf_content=PyPDF2.PdfFileReader(pdf_file)
output('PyPDF2.PdfFileReader()', 'Nach dem Auslesen erhält man folgendes Objekt zurück: '+str(pdf_content))

output('.numPages', 'Mit dem Attribut .numpages lässt sich die totale Seitenzahl auslesen. In diesem Neispiel wären das: '+str(pdf_content.numPages))

output('.getPage()', 'Mit der Funktion .getpage() lässt sich eine einzelne Seite definieren.')
pdf_page_1=pdf_content.getPage(0)

output('.extractText()', 'Mit der Funktion .extractText() lässt sich der Seiteninhalt auslesen.')
output('.extractText() Beispiel', pdf_page_1.extractText()[:44])

output('.decrypt()', 'Manche PDF\'s sind verschlüsselt und müssen zum lesen entschlüsselt werden, dazu verwendet man die Funktion <pdf_file>.decrypt(\'passwort\'). Um zu testen ob ein PDF verschlüsselt ist oder nicht kann man die Funktion <pdf_file>.isDecrypted() verwenden.')

output('PDF\'s erstellen', 'Im gegensatz zu Excelfiles lassen sich PDF-Files nicht mit willkürlichem Text erstellen. Dazu reichen die Fähigkeiten von PyPDF2 nicht aus. Was man jedoch machen kann ist PDF\'s zu verschmelzen, Seiten aus anderen PDF-Dateien einfügen, Seiten drehen, Seiten kopieren oder die Daten zu verschlüsseln.')
