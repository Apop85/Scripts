# 07_kapitel_16_repetitionsfragen.py

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

output('Frage 1', 'Welches Protokoll dient zum Senden von E-Mails? Welches zur Überprüfung auf neu eingetroffene Mails und zum Empfangen?')
output('Antwort', 'SMTP oder Simple Mail Transfer Protocoll dient zum Senden von Nachrichten während das Protokoll IMAP oder Internet Message Access Protocol ist zuständig für den Empfang der nachrichten.')

output('Frage 2', 'Welche Funktionen/Methoden müssen sie aufrufen um sich bei einem SMTP-Server anzumelden?')
output('Antwort', 'Erst muss man sich mit dem Server verbinden mit smtp_object=smtplib.SMTP(server_url, port). Falls die Verschlüsselung nicht TLS sondern SSL ist muss man .smtplib.SMTP_SSL() verwenden. Danach begrüsst man den Server mit smtp_object.ehlo() und danach startete man falls nötig die TLS verschlüsselung mit smtp_object.starttls(). Schlussendlich kann man sich dann mit smtp_object.login(username, passwort) anmelden.')

output('Frage 3', 'Welche beiden Funktionen/Methoden vom Modul imapclient müssen sie Aufrufen um sich bei einem IMAP-Server anzumelden?')
output('Antwort', 'imap_object=imapclient.IMAPClient(server_url, ssl=True) und imap_object.login(username, passwort)')

output('Frage 4', 'Was für ein Argument müssen sie an imap_object.search() übergeben?')
output('Antwort', 'Eine Liste mit dem Suchschlüssel (z.b. FROM) und dem gesuchten String. Beispiel: imap_object.search(["FROM example@example.com"])')

output('Frage 5', 'Was machen sie wenn sie die Fehlermeldung "got more than 10000 bytes" erhalten?')
output('Antwort', 'import imaplib und dann imaplib._MAXBYTES= erhöhen auf z.b. 10**7')

output('Frage 6', 'Das Modul imapclient kümmert sich darum, die Verbindung zu einem Server herzustellen und E-Mails zu finden. Mit welchem Modul lesen sie die Mails die imapclient abruft?')
output('Antwort', 'Die kann man mit dem Modul pyzmail machen welches den Inhalt des Mails von bytecode in von Menschen leserliche Strings umwandelt.')
