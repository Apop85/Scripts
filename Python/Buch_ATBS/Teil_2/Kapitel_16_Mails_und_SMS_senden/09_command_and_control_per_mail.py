# 09_command_and_control_per_mail.py
# In dieser Übung geht es darum Mails nach gewissem Inhalt und von einem spezifischen
# Absender zu suchen um daraus Befehle auszulesen.

import imapclient, pyzmail, bs4, requests

# Logindaten
mail_user_name=''
mail_server=''
mail_password=''
# Telegramdaten
telegram_chat_id=''
telegram_bot_token=''
# Erwartete Absenderadresse und Subject
mail_from=''
identifier_subject=''

def get_mail():
    imap_object=imapclient.IMAPClient(mail_server, ssl=True)
    imap_object.login(mail_user_name, mail_password)
    # Such nach übereinstimmenden Mails in INBOX
    imap_object.select_folder('INBOX', readonly=True)
    uids=imap_object.search(['FROM', mail_from, 'SUBJECT', '"'+identifier_subject+'"', 'UNSEEN'])
    # Lese Mails
    for uid in uids:
        raw_mail_content=pyzmail.PyzMessage.factory(imap_object.fetch(uid, ['BODY[]'])[uid][b'BODY[]'])
        # Decodiere Mailinhalt
        if raw_mail_content.text_part != None and raw_mail_content.text_part.charset != None:
            mail_decoded=raw_mail_content.text_part.get_payload().decode(raw_mail_content.text_part.charset)
        elif raw_mail_content.html_part != None and raw_mail_content.html_part.charset != None:
            mail_decoded=raw_mail_content.html_part.get_payload().decode(raw_mail_content.html_part.charset)
        else:
            continue
        check_mail_content(mail_decoded)
        imap_object.select_folder('INBOX', readonly=False)
        imap_object.delete_messages(uid)
        imap_object.expunge()
        imap_object.logout()

def check_mail_content(mail_decoded):
    mail_no_html=bs4.BeautifulSoup(mail_decoded, 'lxml').text
    commands=mail_no_html.strip().split('\n')
    send_confirmation(commands)

def send_confirmation(commands):
    telegram_message='Folgende Befehle wurden empfangen: \n'+'\n'.join(commands)
    telegram_api='https://api.telegram.org/bot'+telegram_bot_token+'/sendMessage?chat_id='+telegram_chat_id+'&text='+telegram_message
    requests.get(telegram_api)

get_mail()