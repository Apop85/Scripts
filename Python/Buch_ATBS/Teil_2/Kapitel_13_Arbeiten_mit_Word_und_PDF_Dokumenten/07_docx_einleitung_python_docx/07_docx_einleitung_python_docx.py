# 07_word_einleitung_python_docx.py
# In diesem Beispiel wird das Modul Python-Docx vorgestellt. Damit lässt sich in docx-Dokumenten
# lesen und schreiben. Das Modul Python-Docx muss jedoch erst installiert werden da es nicht
# standardmässig zu Python dazugehört.   

import  os, re, docx
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

target='.\\demo.docx'
output_file='.\\edited.docx'

output('Struktur von docx-Dateien', 'docx-Dateien sind vielschichtiger aufgebaut als reine Text-Dateien. Sie bestehen im wesentlichen aus drei Ebenen, die oberste Ebene ist befindet sich ein "Document"-Objekt für das gesammte Dokument welches welches eine Liste aus Paragraphen mit den einzelnen Absätzen beinhaltet. Mit jedem Zeilenumbruch im Dokument wird ein neuer Paragraph erstellt. Diese Absätze sind wiederum in run-Objekte unterteilt welche die Formatierung des Textes definieren. Ein run-Objekt ist ein String mit den selben Formatierungseigenschaften. Sobald die Formatierung sich ändert entsteht ein neues run-Objekt.')

output('docx-Datei öffnen', 'Um eine docx-Datei mittels dem Modul docx aus der installation python-docx muss man den Befehl docx.Document(filename) verwenden.')
doc_file=docx.Document(target)

output('Anzahl Paragraphen', 'Um die Anzahl an Paragraphen herauszufinden kann man das mit len(doc_file.paragraphs) ermitteln. In diesem Beispiel ist das: '+str(len(doc_file.paragraphs)))

output('Paragraphen Anzeigen', 'Da die Paragraphen als Liste gespeichert sind können diese Bequem mittels doc_file.paragraphs[1].text abgerufen werden. Beispiel: '+doc_file.paragraphs[1].text)

output('.paragraphs[].run[]', 'Mit der Funktion doc_file.paragraphs[1].runs[1] lässt sich der erste run des ersten Paragraphs aufrufen. In diesem Beispiel wäre das: '+doc_file.paragraphs[1].runs[1].text)

output('Länge eines runs', 'Um die Länge eines runs auszulesen kann man folgende Funktion verwenden: len(doc_file.paragraphs[1].runs) verwenden. In diesem Beispiel wäre das:'+str(len(doc_file.paragraphs[1].runs)))

output('Kompletten Text einer Datei abrufen', 'Um den kompletten Text eines Dokuments auszulesen muss man mittels einem Loop alle Paragraphen zusammenhängen damit man einen String mit dem Inhalt erhalten kann.')
full_text=[]
for i in range(len(doc_file.paragraphs)):
    full_text+=[doc_file.paragraphs[i].text]
full_text='\n'.join(full_text)
print(full_text)
input()

output('Textformatierung', 'Die Textformatierung besteht bei Word-Dokumenten aus drei Formaten. Eine Absatzformatierung für die Paragraphen-Objekte, eine Zeichenformatierung für die Formatierung der runs und verknüpfte Formate für beide Elemente.')
output('Textformatierung', 'Um Paragraphen und Run-Objekten Formatierungen hinzuzufügen muss man ein Style-Attribut zuweisen mit dem Namen des gewünschten Formats.')

output('Formatbeispiele', "Die Standardformate von Word sind die folgenden: 'Normal', 'BodyText1-3', 'Caption', 'Heading1-9', 'IntenseQuote', 'List', 'List2/3', 'ListBullet', 'ListBullet2/3', 'ListContinue', 'ListContinue2/3', 'ListParagraph', 'MacroText', 'Quote', 'Subtitle', 'TOCHeading' und 'Title'")

output('Verknüpfte Formate', 'Um eine Verknüpfte formatierung anzuwenden muss am ende des Formatnamens "Char" stehen. Beispielsweise: Um den einzelnen Paragraphen paragraph_object zu ändern muss man paragraphen_objekt.style = "Quote" verwenden. Möchte man eine verknüpfte Formatierung anwenden muss man dazu das run Objekt verwenden und Char am ende des Formatnamens anhängen. Beispiel: run_objekt.style="QuoteChar"')
doc_file.paragraphs[1].style="Quote"
doc_file.paragraphs[3].runs[0].style='Quote Char'

output('Manuelle Formatvorlagen', 'Man kann auch eigene Formatvorlagen verwenden. Sofern sie beim erstellen des Files bereits vorhanden waren können sie unter ihrem eigenen Namen aufgerufen werden.')

output('Formatierung von paragraph-Objekten', 'Um paragraph-Objekte zu formatieren muss man folgenden Syntax verwenden: doc_file.paragraphs[1].style = "Normal"')
doc_file.paragraphs[3].style='Normal'

output('Formatierung von run-Objekten', 'Um run-Objekte zu formatieren verwendet man folgenden Syntax: doc_file.paragraphs[1].runs[1].style = "QuoteChar" oder doc_file.paragraphs[1].runs[1].underline = True')
doc_file.paragraphs[4].runs[0].underline=True

output('Formatierung abfragen', 'Mit der Funktion doc_file.paragraphs[1].style kann man auslesen welchen Style der Paragraph hinterlegt hat. In diesem Beispiel wäre das: '+str(doc_file.paragraphs[1].style))

output('File speichern', 'Um die vorgenommenen Änderungen zu speichern kann man die Funktion doc_file.save(safe_file) verwenden.')
doc_file.save(output_file)
