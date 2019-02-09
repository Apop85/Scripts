# 11_brute_force_pdf.py
# In diesem Übungsbeispiel geht es darum ein verschlüsseltes PDF zu knacken mittels der
# Dictionary-Attack- oder Brute-Force-Methode

import os, PyPDF2
os.chdir(os.path.dirname(__file__))

bruteforce_characters=[' ', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                    'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u',
                    'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F',
                    'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q',
                    'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '.', '-',
                    ',', '!', '?', '=', ')', '(', '/', '&', '%', '*', '0',
                    '1', '2', '3', '4', '5', '6', '7', '8', '9']

dictionary_file='.\\dictionary.txt'
counter=0
got_result=False

def brute_force_it(char_set):
    global counter, password, got_result, pdf_file
    while True:
        if got_result == True:
            return
        for letter in bruteforce_characters:
            if len(char_set) <= max_password_length:
                if counter == 0:
                    char_set = letter
                else:
                    char_set+=letter
                counter+=1
                check=pdf_file.decrypt(char_set.strip())
                if check == 1:
                    got_result=True
                    password=char_set.strip()
                    return
                if len(char_set) < max_password_length:
                    brute_force_it(char_set)
                # else:
                #     return
                char_set=char_set[:-1]
                counter-=1
            else:
                return
        break ### brauchts das?

def dictionary_attack():
    global pdf_file, password, got_result
    dict_file_open=open(dictionary_file)
    dict_content=dict_file_open.read()
    dict_content=dict_content.split('\n')
    dict_file_open.close()
    for possible_password in dict_content:
        check_1=pdf_file.decrypt(possible_password)
        check_2=pdf_file.decrypt(possible_password.lower())
        check_3=pdf_file.decrypt(possible_password.title())
        if check_1 == 1 or check_2 == 1 or check_3 == 1:
            if check_1 == 1:
                password=possible_password
            elif check_2 == 1:
                password=possible_password.lower()
            else:
                password=possible_password.title()
            got_result=True
            return

def choose_password_lenght():
    global max_password_length
    print(''.center(70, '█'))
    print(' Wie lange soll das Passwort höchstens sein? '.center(70, '█'))
    print(''.center(70, '█'))
    max_password_length=input(''.center(25))
    if max_password_length.isdecimal():
        max_password_length=int(max_password_length)
        brute_force_it('')

def choose_file():
    global pdf_file
    print(''.center(70, '█'))
    print(' Bitte Namen des PDF-Files angeben (demo.pdf): '.center(70, '█'))
    print(''.center(70, '█'))
    file_name=input(''.center(25))
    if os.path.isfile(file_name) and file_name.endswith('.pdf'):
        pdf_file=PyPDF2.PdfFileReader(file_name)
        print(''.center(70, '█'))
        print(' Passwort wird gesucht... '.center(70, '█'))
        print(''.center(70, '█'))
        if attack == 1:
            choose_password_lenght()
        else:
            dictionary_attack()


def choose_method():
    global attack
    while True:
        print(''.center(70, '█'))
        print(' Bitte Methode auswählen '.center(70, '█'))
        print(''.center(70, '█'))
        print(''.rjust(15, '█')+'1. Bruteforce-Attack'.center(40)+''.rjust(15, '█'))
        print(''.rjust(15, '█')+'2. Dictionary-Attack'.center(40)+''.rjust(15, '█'))
        print(''.center(70, '█'))
        attack=input(''.rjust(34))
        if attack.isdecimal():
            attack=int(attack)
            if attack < 3 and attack > 0:
                choose_file()
        else:
            continue
        if got_result == True:
            show_password()
        else:
            no_pass()

def show_password():
    print(''.center(70, '█'))
    print(' Passwort gefunden! '.center(70, '█'))
    print(''.center(70, '█'))
    print(''.rjust(15, '█')+password.center(40)+''.rjust(15, '█'))
    print(''.center(70, '█'))
    input()

def no_pass():
    print(''.center(70, '█'))
    print(' Passwort nicht gefunden '.center(70, '█'))
    print(''.center(70, '█'))
    input()

choose_method()