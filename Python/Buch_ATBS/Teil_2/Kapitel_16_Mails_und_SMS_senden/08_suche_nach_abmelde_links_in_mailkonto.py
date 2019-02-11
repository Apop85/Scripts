# 08_suche_nach_abmelde_links_in_mailkonto.py

import imapclient, bs4

print('Mail Server angeben:')
server_url=input()
print('Benutzernamen/Mail angeben:')
user_name=input()
print('Passwort eingeben')
password=input()

imap_object=imapclient.IMAPClient(server_url, ssl=True)
imap_object.login(user_name, password)

folder_names=imap_object.list_folders()
def check_for_subfolder():
    subfolders=[]
    for possible_folder_name in folder_names:
        if '[' not in possible_folder_name[-1].split('/')[-1]:
            subfolders+=[possible_folder_name[-1].split('/')[-1]]
    print(subfolders)

check_for_subfolder()

