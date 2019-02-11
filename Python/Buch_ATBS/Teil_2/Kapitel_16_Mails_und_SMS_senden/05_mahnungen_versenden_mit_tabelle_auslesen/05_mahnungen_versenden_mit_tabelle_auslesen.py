# 04_mahnungen_versenden_mit_tabelle_auslesen.py
# In dieser Übung geht es darum eine Tabelle mit Zahlungen auszulesen und für die
# offenstehenden Rechnungen eine Mahnung per Mail zu versenden. 

import os, smtplib, openpyxl
os.chdir(os.path.dirname(__file__))

source_file='.\\mahnungen.xlsx'
server_url='smtp.gmail.com'
server_port=587
print('Benutzername angeben:')
user_name=input()
print('Passwort für '+user_name+' angeben:')
password=input()

excel_sheet=openpyxl.load_workbook(source_file)
active_sheet=excel_sheet.active

max_rows=active_sheet.max_row
max_columns=active_sheet.max_column

# Auslesen wer die Rechungen noch nicht bezahlt hat.
mahnungsliste={}
for y_axis in range(2,max_rows+1):
    name=active_sheet['A'+str(y_axis)].value
    mail=active_sheet['B'+str(y_axis)].value
    for x_axis in range(3,max_columns+1):
        column_name=openpyxl.utils.get_column_letter(x_axis)
        if active_sheet[column_name+str(y_axis)].value != 'paid':
            mahnungsliste.setdefault(mail, {'name' : name})
            mahnungsliste[mail].setdefault('mahnung', [])
            mahnungsliste[mail]['mahnung']+=[active_sheet[column_name+'1'].value]

# Generiere Mailnachrichten:
mail_message=[]
subject='Subject: Mahnung. Überfällige Rechnung wurde noch nicht bezahlt. \n'
for mail in mahnungsliste:
    monate_unbezahlt=''
    for monat in mahnungsliste[mail]['mahnung']:
        monate_unbezahlt+='-- '+monat+'\n'
    message=subject+'Sehr geehrte/r '+mahnungsliste[mail]['name']+'.\nLeider ist uns aufgefallen, dass sie bei uns noch offene Rechungen haben. Wir bitten sie die Rechnung für folgende Monat/e raschmöglichst zu bezahlen:\n'+monate_unbezahlt+'\n\nFreundliche Grüsse\nIhr Immobilienheini'
    mail_message+=[message]

# Verbinde mit Server
smtp_object=smtplib.SMTP(server_url, server_port)
smtp_object.ehlo()
smtp_object.starttls()
smtp_object.login(user_name, password)
counter=0
# Sende Mails
for mail in mahnungsliste:
    message_now=mail_message[counter].encode("utf-8")
    test_send=smtp_object.sendmail(user_name, mail, message_now)
    # Prüfe ob Mail mit errorcode zurückgegeben wurde.
    if len(test_send) == 0:
        print('Mail an '+mail+' erfolgreich versandt.')
    else:
        print('Mail an '+mail+' konnte nicht versandt werden.')
    counter+=1

# Logout aus Server
smtp_object.quit()