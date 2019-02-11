# 03_mails_empfangen.py
# In dieser Übung geht es darum Mails von einem IMAP-Server auszulesen und den Inhalt auszugeben.

import imapclient, pyzmail

print('IMAP-Server angeben:')
imap_server=input()

print('Benutzername angeben:')
user_name=input()
print('Passwort angeben:')
password=input()

#Verbinde zu Server
imap_object=imapclient.IMAPClient(imap_server, ssl=True)

# Login bei Server
imap_object.login(user_name, password)

# Suche verfügbare Ordner und zeige sie an.
print(imap_object.list_folders)

# Öffne INBOX-Ordner
imap_object.select_folder('INBOX', readonly=True)

# Suche ungelesene Mails:
UIDs = imap_object.search(['ALL'])

# Lese Body des ersten Mail aus der UIDs-Liste aus
last_uid=UIDs[-1]
raw_mail_message=imap_object.fetch([last_uid], ['BODY[]'])

# Übergebe Rohnachricht an pyzmail
mail_message=pyzmail.PyzMessage.factory(raw_mail_message[last_uid][b'BODY[]'])

# Zeige Subject an:
print(mail_message.get_subject())

# Zeige Absender an:
print(mail_message.get_addresses('from'))

# Extrahiere Text und HTML-Elemente:
if mail_message.text_part != None:
    mail_text=mail_message.text_part.get_payload().decode(mail_message.text_part.charset)
    print(mail_text)
if mail_message.html_part != None:
    mail_html=mail_message.html_part.get_payload().decode(mail_message.html_part.charset)
    print(mail_html)

# Logout
imap_object.logout()