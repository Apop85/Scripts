# 08_suche_nach_abmelde_links_in_mailkonto.py
# In dieser Übung geht es darum das komplette Mailkonto nach Newslettern zu durchsuchen
# und in diesen den Link um den Newsletter abzubestellen um diesen dann im Webrowser zu öffnen.

import imapclient, bs4, pyzmail, re, webbrowser

print('Mail Server angeben:')
server_url=input()
print('Benutzernamen/Mail angeben:')
user_name=input()
print('Passwort eingeben')
password=input()

imap_object=imapclient.IMAPClient(server_url, ssl=True)
imap_object.login(user_name, password)
folder_names=imap_object.list_folders()
already_found_links=[]

def check_for_subfolder():
    # Suche alle vorhandenen Unterordner des Mailkontos
    subfolders=[]
    for possible_folder_name in folder_names:
        if '[' not in possible_folder_name[-1].split('/')[-1]:
            subfolders+=[possible_folder_name[-1]]
    for folder in subfolders:
        imap_object.select_folder(folder, readonly=True)
        uids=imap_object.search('ALL')
        check_mails(uids)
    imap_object.logout()
                
def check_mails(uids):
    # Durchsuche alle Mails des aktuellen Unterordners
    for uid in uids:
        mail_content=imap_object.fetch(uid, ['BODY[]'])
        raw_mail=pyzmail.PyzMessage.factory(mail_content[uid][b'BODY[]'])
        # Prüfe ob HTML oder Text-Mail und decodiere Inhalt
        try:
            if raw_mail.html_part != None and raw_mail.html_part.charset != None:
                mail_decoded=raw_mail.html_part.get_payload().decode(raw_mail.html_part.charset)
            elif raw_mail.text_part != None and raw_mail.text_part.charset != None:
                mail_decoded=raw_mail.text_part.get_payload().decode(raw_mail.text_part.charset)
            else:
                continue
        except:
            continue
        # Suche nach abmelden oder unsubscribe
        if 'abmelden' in mail_decoded:
            search_pattern=re.compile(r'.{350}abmelden.{350}', re.DOTALL)
            results=search_pattern.findall(mail_decoded)
            get_link(results)
        elif 'unsubscribe' in mail_decoded:
            search_pattern=re.compile(r'.{,350}unsubscribe.{,350}', re.DOTALL)
            results=search_pattern.findall(mail_decoded)
            get_link(results)

def get_link(results):
    global already_found_links
    # Lese Link aus und öffne ihn im Browser
    for result in results:
        bs4_object=bs4.BeautifulSoup(result, features='lxml')
        bs4_result=bs4_object.find_all('a')
        if bs4_result != []:
            for entry in bs4_result:
                if entry.has_attr('href'):
                    if entry['href'] not in already_found_links:
                        webbrowser.open(entry['href'])
                        already_found_links+=[entry['href']]

check_for_subfolder()
