# 01_grundlagen_mailversand.py

import re

max_text_length=70
max_text_delta=24

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

####### MAILS SENDEN
output('Mails senden', 'Im ersten Teil geht es erst darum wie man Mails versendet, der Empfang von Mails kommt im 2. Teil')

output('SMTP', 'SMTP ist das Protokoll welches zum Versenden von Emails verwendet wird und bedeutet so viel wie: "Simple Mail Transfer Protocol". Dieses bestimmt wie E-Mail-Nachrichten formatiert, verschüsselt und von einem Mailserver zum anderen weitergeleitet werden müssen. ')

output('Modul smtplib', 'Das Modul smtplib bietet einige einfache Funktionen um Mails zu versenden.')

output('.SMTP()', 'Die Funktion smtplib.SMTP() definiert den SMTP-Server und den dazugehörigen Port. Beispiel: smtp_objekt=smtplib.SMTP("smtp.gmx.ch", 587)')
output('.SMTP_SSL()', 'Schlägt ein Aufruf von .SMTP() fehl so kann es daran liegen, dass der Server TLS (Verschlüsselungsverfahren) nicht unterstützt. In diesem Falle kann man mit .SMTP_SSL() versuchen sich mit dem Server zu verbinden. Meist ist dazu auch ein anderer Port von 587 (TLS) nach 465 (SSL) notwendig.')

output('.ehlo()', 'Das erste was man macht wenn man sich mit einem Mailserver verbindet ist diesem mit der Funktion smtplib.ehlo() zu kontaktieren. Ohne diesen Aufruf werden alle nachfolgenden Anfragen scheitern.')
output('ehlo() Antwort 250', 'Antwortet der Server mit dem Statuscode 250 heisst dies, dass alles korrekt lief.')

output('Verschlüsselung einleiten.', 'Je nach dem welche Verschlüsselung muss das Mail anders aufbereitet werden. Bei TLS ist das mittels smptlib.starttls(). Bei SSL ist dieser Schritt nicht notwendig da der Port 465 bereits über eine Verschlüsselung verfügt.')
output('TLS-Verschlüsselung', 'Gibt .starttls() den Statuscode 220 zurück so ist der Server bereit zur kommunikation über die TLS-Verschlüsselte Verbindung.')

output('Anmelden beim SMTP-Server', 'Waren die ersten Schritte erfolgreich geht es nun darum sich mit seinem Benutzerkonto beim Mail-Server anzumelden.')
output('.login()', 'Mit der Funktion smtplib.login(username, passwort) meldet man sich beim Mailserver mit seinem Account an.')
output('Anwendungsspezifische Passwörter', 'Mail-Dienste wie Gmail verfügen über einen Mechanismus dass anwendungen ein anwendungsspezifisches Passwort benötigen um sich einzuloggen um so die Sicherheit zu erhöhen falls das übermittelte Passwort abgefangen werden sollte.')
output('Warnung', 'Passwörter sollten nie im Script selber gespeichert sein, falls jemand das Script kopieren sollte hat diese Person sonst vollen Zugriff auf das Mailkonto.')

output('Mail senden', 'Um ein Mail zu versenden verwendet man smtplib.sendmail(absender, empfänger, subject)')
output('Mehrere Empfänger', 'Sollten mehrere Empfänger gewünscht sein kann man diese auch als Liste an .sendmail() übergeben.')

output('Formatierung von subject', 'Der String für den E-Mail rumpf muss mit "Subject: \\n" beginnen. Alles vor dem Zeilenumbruch wird im Betreff stehen, alles danach wird als Nachricht übermittelt.')
output('Rückgabewerte', 'Fehlgeschlagene Mails geben ein Dictionary mit Integerwerten zurück. Ist das Dictionary leer so bedeutet dies dass das Versenden der Nachricht erfolgreich war.')

output('Vom Server trennen', 'Um die Verbindung zum Server zu trennen verwendet man smtplib.quit(). Der Statuscode 221 bedeutet dass die Verbindung getrennt wurde.')

####### MAILS EMPFANGEN
output('Mails empfangen', 'Dies ist nun der 2. Teil in welchem es darum geht Mails zu empfangen, zu löschen, auszullesen ect.')

output('IMAP', 'IMAP oder auch "Internet Message Access Protocol" wird zur Kommunikation mit dem Server verwendet um die Mails auf dem entsprechenden Konto aufrufen zu können.')

output('Modul imapclient', 'Auch wenn Python ein eigenes Modul namens imaplib besitzt ist der Umgang mit imapclient() deutlich einfacher weswegen es in den folgenden Beispielen verwendet wird.')

output('Modul pyzmail', 'Das Modul pyzmail kann den Inhalt von Mails entgegennehmen und sie in Brauchbare Strings umwandeln. Auch dies ist ein Python-externes Tool welches erst nachinstalliert werden muss.')
output('Mails auslesen', 'Um Mails vom Server auszulesen sind deutlich mehr Schritte notwendig als um ein Mail zu senden. ')

output('.imapClient()', 'Mit der Funktion imap_object=imapclient.imapClient(server, verschlüsselung) definiert man zu welchem Server und mit welcher Verschlüsselung eine Verbindung hergestellt werden soll. Bei den Meisten Server ist eine SSL-Verschlüsselung notwendig wesegen man das Argument ssl=True übergeben muss. Es gibt jedoch auch Server bei welchen man auf das verzichten kann.')
output('.login()', 'Mit der Funktion imap_object.login(username, passwort) kann man sich anschliessend mit dem Server verbinden. Wenn der Server den Benutzernamen oder das Passwort ablehnt erhält man die Fehlermeldung imaplib.error')

output('.list_folders()', 'Mit der Funktion imap_object.list_folders() lassen sich alle Verfügbaren Ordner aus diesem Mailkonto anzeigen. Zurückgegeben werden die Ordner als Tuple mit Ordnername und einigen Informationen. Beispielsweise besteht das Tuple aus einem Weiteren Tuple welches die Flags des Ordners anzeigen, dann das Trennzeichen zwischen Eltern- und Kindordner und der Vollständige Ordnername. Bsp: [((\'\\\\HashNoChildern\',), \'/\', \'INBOX\')]')
output('.select_folder()', 'Mit der Funktion imap_object.select_folder(ordnername, berechtigung) kann man dem Server mitteilen welchen Ordner man durchsuchen möchte. Meistens verwedet man dazu readonly=True damit man den Inhalt der Mails nicht versehentlich ändert oder das Mail sogar löscht.')

output('.search()', 'Mit der imap_object.search(suchbedingung) Funktion lassen sich Mails suchen die den angegebenen Bedingungen erfüllt.')
output('Suchstrings', 'Es gibt diverse Strings die zur spezifizierung der Suche dienen.')
output('Suchstring ALL', 'Der Suchstring ALL gibt alle Mails im aktiven Ordner als resultat zurück.')
output('Suchstring BEFORE, ON und SINCE', 'Mit BEFORE, ON und SINCE kann man Mails vor, an oder seit einem bestimmten Datum suchen. Bsp: imap_object.search(["BEFORE 05-Jul-2015"]) für alle Mails vor dem 5.7.2015.')
output('Suchstring SUBJECT, BODY, TEXT', 'Mit den Suchstrings SUBJECT, BODY und TEXT kann man nach bestimmten Inhalten in der Betreffzeile, im Body und im Text des Mails finden. Bsp: imap_object.search([\'SUBJECT "Your ddns-Address is expiring soon"\']) findet alle Mails mit dem angegebenen Subjekt.')
output('Suchstring FROM, TO, CC und BCC', 'Mit den Suchstrings FROM, TO, CC und BCC kann man nach Mails mit entsprechendem Absender, Empfänger, Kopie an und Blindkopie an finden. Bsp: imap_object.search(["TO example@company.com"])')
output('Suchstring SEEN und UNSEEN', 'Mit SEEN und UNSEEN lassen sich die gelesenen bzw ungelesenen Mails im aktiven Ordner finden.')
output('Suchstring ANSWERED und UNANSWERED', 'Die Suchstrings ANSWERED und UNANSWERED geben die beantworteten bzw unbeantworteten Mails zurück.')
output('Suchstring DELETED und UNDELETED', 'DELETED und UNDELETED findet alle gelöschten und nicht gelöschten Mails zurück.')
output('Suchstring DRAFT und UNDRAFT', 'Die Suchstrings DRAFT und UNDRAFT finden alle mit oder ohne den Flag DRAFT (Entwurf)')
output('Suchstring FLAGGED und UNFLAGGED', 'FLAGGED und UNFLAGGED geben alle Nachrichten mit oder ohne Flag zurück. Gewöhnlicherweise kommen diese Flags vor bei Mails mit hoher Priorität oder Lesebestätigung.')
output('Suchstring LARGER n und SMALLER n', 'Diese Suchstrings geben alle Mails zurück die kleiner oder grösser N Bytes sind.')
output('Suchstring NOT', 'Mit NOT lassen sich alle Mails suchen die den angegebenen Suchstring nicht enthalten. Bsp: imap_object.search(["NOT BIGGER 150"]) gibt alle Mails kleiner 150bytes zurück.')
output('Suchstring OR', 'Mit OR lassen sich or-Bedingungen formulieren. Dies gibt alle Mails zurück die entweder dem einen Suchstring oder dem anderen Suchstring entsprechen. Bsp: imap_object.search(["OR FROM example@gmx.ch FROM example2@gmx.ch"]) gibt alle Mails zurück von example@gmx.ch oder example2@gmx.ch als Absender.')
output('Mehrere Suchstrings', 'Man kann auch Suchstrings kombinieren. Bsp: imap_object.search(["SINCE 01-Jan-2015", "NOT FROM alice@example.com"])')
output('Resultat der Suchanfrage', 'Als Resultat kriegt man die UID Liste der Mail(s) zurück die mit der Suchanfrage übereinstimmt.')
output('Fehlerhafte Suchanfrage', 'Gibt die Suchanfrage mehr als 10000 Bytes an Daten zurück gibt imaplib einen Error aus. Möchte man mit den Daten trozdem arbeiten sollte man das Modul imaplib importieren und imaplib._MAXLINE auf 10**7 erhöhen')
output('Vereinfachung bei GMAIL-Konten', 'Bei einem GMail-Konto kann man die vereinfachte Suchfunktion .gmail_search() verwenden welche das Suchfeld von GMail nachahmt.')

output('.fetch()', 'Um von den gesammelten UIDs zu den Mails selber zu kommen verwendet man die Funktion raw_mail_content=imap_object.fetch(UID, ["BODY[]"]). Die UID beschreibt um welches Mail es geht und das zweite Argument weist imapclient an den Body-Text auszulesen. Die Rohnachricht wird dann als Dictionary zurückgegeben mit den entsprechenden Informationen des Mails sowie dessen Inhalt. Damit die vom Script aufgerufene Mail als gelesen markiert wird muss man zuvor nochmals imap_object.select_folder() mit dem Argument readonly=False aufrufen.')

output('Mails löschen', 'Um ein Mail löschen zu können muss der Aufgerufene Ordner natürlich mit dem Argument readonly=False geöffnet sein. Dann kann man diese mittels imap_object.delete_message(UID-Liste) löschen. Die Mails werden jedoch erst endgültig entfernt wenn man anschliessend noch imap_object.expunge() ausführt.')

output('Verbindung zum Server trennen', 'Um die Verbindung zum Server zu trennen verwendet man die funktion imap_object.logout()')

####### pyzmail
output('Nachricht aus Rohformat gewinnen', 'Um nun die eigentliche Mailnachricht aus den Rohdaten zu gewinnen verwenden wir in diesem Beispiel pyzmail.')
output('Rohformat umwandeln', 'Mittels mail_message=pyzmail.PyzMessage.factory(raw_mail_content[UID][\'BODY[]\']) wird das Rohformats des Mails in verständliche Abschnitte aufgeteilt.')

output('pyzmail', 'Das Modul pyzmail bietet nach umwandlung des Rohformats diverse Möglichkeiten den Inhalt des Mails auszulesen.')
output('get_subject()', 'Mit der Funktion mail_message.get_subject() lässt sich der Betreff auslesen.')

output('get_adress("from")', 'Mittels mail_message.get_adress("from") kann man den Absender auslesen.')
output('get_adresses("to")', 'Mit dem Argument "to" kann man mit .get_adress() die Empfänger auslesen.')
output('get_adresses("cc")', 'Mit dem Argument "cc" kann man die Copy-Empfänger auslesen.')
output('get_adresses("bcc")', 'Mit dem Argument "bcc" lassen sich die Empfänger von Blindkopien auslesen.')

output('Rumpf aus Rohnachticht auslesen', 'Der Rumpf des Mails kann in reinem Textformat aber jedoch auch in HTML oder beidem geschrieben sein.')
output('Text/HTML', 'In reinen Textmails sind bei pyzmail alle html_part-Attribute auf None gesetzt und bei HTML-Mails alle text_part-Attribute.')
output('Prüfung ob HTML, Text oder beides', 'Mittels dem einfachen if-Statement "if mail_message.text_part != None" um zu Prüfen ob Textelemente vorhanden sind oder "if mail_message.html_part != None" um zu Prüfen ob ein HTML-Part vorhanden ist.')

output('.get_payload()', 'Mit der Funktion mail_message.text_part.get_payload() Kann man den Inhalt des Textparts und mit html_part.get_payload() den HTML-Part als bytecode auslesen.')
output('bytecode', 'Da bytecode für Menschen unleserlich ist muss dieser noch decodiert werden. Das auslesen und decodieren kann in einem Schritt erfolgen mittels: mail_message.html_part.get_payload().decode(mail_message.html_part.charset)')
output('', '')
output('', '')
output('', '')