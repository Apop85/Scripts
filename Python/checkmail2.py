#!/usr/bin/python2
#Script zur Benachrichtigung einer bald ablaufenden DynDNS-Adresse bei No-IP.com
#Übermittelt ebenfalls den Bestätigungslink

# -*- coding: utf-8 -*-
from __future__ import print_function
import email
import imaplib
import urllib
import requests
import time
from time import gmtime, strftime

logtime = time.strftime('%d.%m.%Y - %H:%M:%S')
logmessage = logtime + ' checkmail2.py ausgeführt' + "\n"
with open('/home/pi/scriptexec.log', 'a') as att_file:
    att_file.write(logmessage)

MAIL_SERVERS = {
                'gmail':
                    {
                        'Login': '',
                        'Password': '',
                        'Server': 'imap.gmail.com',
                        'Port': 993,
                    },
}

# react only on eMails with exact this subject
# subject = 'Confirm Your Hostname Now'

subject = 'ACTION REQUIRED: rbaldinger.ddns.net is Expiring Soon'

# BOT_TOKEN = ''
# TELEGRAM_CHAT_ID = ''
execfile("/home/pi/Autostart/telegram.inf")
telegram_notice_text = 'No-IP Adresse muss verifiziert werden! %confirmlink%'

URL = 'https://api.telegram.org/bot{}/'.format(BOT_ID)


DEBUG = False

def send_message(chat_id, text):
    print('Sending Telegram Notice...')
    text = urllib.quote_plus(text)
    url = URL + 'sendMessage?parse_mode=Markdown&chat_id={}&text={}'.format(chat_id, text)
    response = requests.get(url)

# check Mails
def check_mails():
    for SRV in MAIL_SERVERS:
        if MAIL_SERVERS[SRV]['Login'] == '':
            continue
        print('Checking: {}'.format(SRV))
        m = imaplib.IMAP4_SSL(MAIL_SERVERS[SRV]['Server'], MAIL_SERVERS[SRV].get('Port', 993))
        m.login(MAIL_SERVERS[SRV]['Login'], MAIL_SERVERS[SRV]['Password'])
        m.select('Inbox')
        status, unreadcount = m.status('INBOX', '(UNSEEN)')
        unreadcount = int(unreadcount[0].split()[2].strip(').,]'))
        if unreadcount > 0:
            print('processing {} unreaded emails ...'.format(unreadcount))
            items = m.search(None, 'UNSEEN')
            items = str(items[1]).strip('[\']').split(' ')
            for index, emailid in enumerate(items):
                resp, data = m.fetch(emailid, '(RFC822)')
                email_body = data[0][1]
                mail = email.message_from_string(email_body)
                
                if mail['Subject'] == subject:
                    timeNow = strftime('%d.%m.%Y %H:%M:%S', gmtime())
                    print('[{}]  New EMail with Subject "{}" received, checking.. '.format(timeNow, mail['Subject']))
                    countIt=False
                    nextmail=False
                    confirm_link=''
                    if mail.is_multipart():
                        for part in mail.walk():
                            if part.get_content_type() == 'text/html':
                                body = part.get_payload()
                                body = body.decode()
                                for line in body.split('\r\n'):
                                    if nextmail:
                                        continue
                                    if line == '':
                                        # skip empty lines
                                        continue
                                    if 'Click the button above to confirm your hostname' in line:
                                        countIt=True
                                        continue
                                    if countIt and 'www.noip.com' in line:
                                        if DEBUG: print(line)
                                        confirm_link = line.split('&')[0].strip()
                                        confirm_link = confirm_link.replace('?n=3D', '?n=')
                                        print('Confirm Link extracted: %s' % confirm_link)
                                        nextmail=True
                    # send notice to telegram
                    notice = telegram_notice_text.replace('%confirmlink%', confirm_link)
                    send_message(CHAT_ID, notice)
                else:
                    # mark other mails back to unread/unseen
                    m.store(emailid, '-FLAGS', '(\Seen)')
    try: m.logout()
    except: pass
    print('Done.')


if __name__ == '__main__':
    try:
        check_mails()
    except Exception, error:
        print('Error...: {}'.format(error))
    except (KeyboardInterrupt, SystemExit):
        print('Schliesse Programm.')

#EOF
