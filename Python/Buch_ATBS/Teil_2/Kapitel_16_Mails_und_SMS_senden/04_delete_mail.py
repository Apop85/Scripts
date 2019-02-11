# 04_delete_mail.py
# In diesem Beispiel geht es darum die Mail aus 02_mail_versenden.py wieder zu löschen.

import imapclient

server='imap.gmail.com'

print('Benutzername angeben:')
user_name=input()
print('Passwort angeben:')
password=input()

# Verbinde zu Server
imap_object=imapclient.IMAPClient(server, ssl=True)
imap_object.login(user_name, password)
# Öffne Inbox-Ordner
imap_object.select_folder('INBOX', readonly=True)
# Suche mail von 02_mails_versenden.py
uids=imap_object.search('SUBJECT "Das ist eine Testnachricht"')
imap_object.select_folder('INBOX', readonly=False)
# Lösche mail
imap_object.delete_messages(uids)
imap_object.expunge()

# Serverlogout
imap_object.logout()
