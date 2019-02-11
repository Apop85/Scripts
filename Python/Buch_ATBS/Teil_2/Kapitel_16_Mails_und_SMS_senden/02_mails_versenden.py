# 02_mails_versenden.py
# In dieser Übung geht es darum sich über den SMTP-Server einzuloggen und 
# Eine testnachricht zu versenden.

import smtplib

print('SMTP-Server angeben:')
smtp_server=input()
print('Port angeben:')
smtp_port=input()

print('Benutzername angeben:')
user_name=input()
print('Passwort angeben:')
password=input()

# Verbindung mittels TLS oder SSL verschlüsselung
try:
    smtp_object=smtplib.SMTP(smtp_server, int(smtp_port))
    TLS=True
except:
    smtp_object=smtplib.SMTP_SSL(smtp_server, int(smtp_port))
    TLS=False

# Begrüssung bei Server
smtp_object.ehlo()

# Schalte TLS-Verschlüsselung ein falls die Verbindung mit TLS aufgebaut wurde
if TLS == True:
    smtp_object.starttls()

# Login beim Server
smtp_object.login(user_name, password)

# Versende Mail an sich selbst.
send_test=smtp_object.sendmail(user_name, user_name, 'Subject: Das ist eine Testnachricht \nDiese Nachricht wurde durch mein Pythonscript versendet.')

# Prüfe ob Mail versendet werden konnte.
if len(send_test) == 0:
    print('Mail erfolgreich versendet.')
else:
    print('Mail wurde nicht versendet.')

print('Verbindung zum Mailserver wird geschlossen')
smtp_object.quit()