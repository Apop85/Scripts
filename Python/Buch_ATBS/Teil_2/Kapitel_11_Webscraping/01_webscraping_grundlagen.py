# 01_webscraping_grundlagen.py
import re
maxl, dv=50, 15
def output(string):
    print(''.center(maxl, '█'))
    delta=len(string) % maxl
    string+=' '*(maxl)
    suchmuster=re.compile(r'.{'+str(maxl-dv) + r',' + str(maxl) +r'}[,|\.|\n| ]', re.DOTALL)
    ergebnis=suchmuster.findall(string)
    for abschnitt in ergebnis:
        print(abschnitt)
    return input()

text='Für das Webscraping verwendet man vorallem die Module webbrowser, request, BeautyfullSoup und Selenium'
output(text)
print('\n')

text='webbrowser: In python enthaltenes Modul welches eine bestimmte Seite im Browser öffnet'
output(text)
print('\n')

text='request: Lädt Inhalte herunter'
output(text)
print('\n')

text='BeautyfullSoup: Analysiert oder parst HTML'
output(text)
print('\n')

text='Selenium: Startet und steuert einen Webbrowser, damit können auch Klicks und Interaktionen simuliert werden.'
output(text)
print('\n')