# 12_kapitel_11_repetitionsfragen.py

import os, re

max_text_length=70
max_text_delta=20

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]')
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('Frage 1', 'Beschreiben sie Kurz den Unterschied zwischen requests, BeautifulSoup, webbrowser und Selemium.')
output('Antwort', 'Mittels requests lassen sich Inhalte downloaden und den Inhalt der Seite im Rohformat verarbeiten. Mit BeautifulSoup kann man gezielt nach Elementen in einer Webseite suchen, mit Selenium kann man ein Browserfenster öffnen, analysieren und steuern, mit webbrowser lässt sich hingegen nur ein Browserfenster öffnen')

output('Frage 2', 'Welche art von Objekt gibt requests.get() zurück und wie können sie auf den heruntergeladenen Inhalt als String zugreiffen?')
output('Antwort', 'content=requests.get(url) gibt ein Response-Objekt zurück und Lädt den Inhalt der Seite direkt runter und Testet ob diese vorhanden ist. content.text Lässt dann auf den Inhalt zugreiffen.')

output('Frage 3', 'Welche requests-Methode prüft ob ein Download erfolgreich war?')
output('Antwort', 'die methode .raise_for_status() gibt einen Error zurück wenn die Seite nicht geladen werden konnte.')

output('Frage 4', 'Wie können sie den HTTP-Statuscode einer request-Antwort abrufen?')
output('Antwort', 'mit content.status_code(s) kann der HTTP-Statuscode abgerufen werden.')

output('Frage 5', 'Wie Speichern sie eine requests-Antwort in einer Datei?')
output('Antwort', 'Dazu kann man einen For-Loop verwenden mit for chunk in content.iter_content(10**6): save_file.write(chunk).')

output('Frage 6', 'Welches Tastenkürzel können sie nutzen um das Entwicklerfenster im Browser zu öffnen?')
output('Antwort', 'CTRL+SHIFT+C')

output('Frage 7', 'Wie können sie in den Entwicklertools den HTML-Code eines bestimmten Elements anschauen?')
output('Antwort', 'Rechtsklick auf das zu untersuchende Element und dann Q')

output('Frage 8', 'Welchen CSS-Selektorstring verwenden sie um ein Element mit dem id-Attribut main zu finden?')
output('Antwort', 'BeautifulSoup: soup.select(#main)  Selenium: browser.find_element_by_id(\'main\')')

output('Frage 9', 'Welchen CSS-Selektorstring verwenden sie, um Elemente mit der CSS-Klasse highlight zu finden?')
output('Antwort', 'BeautifulSoup: soup.select(.\'highlight\') Selenium: browser.find_element_by_class_name(\'highlight\')')

output('Frage 10', 'Welchen CSS-Selektorstring verwenden sie, um alle <div>-Elemente zu finden, welche in ein anderes <div>-Element verschachtelt sind?')
output('Antwort', 'BeautifulSoup: soup.select(div div) Selenium: browser.find_elements_by_??????')

output('Frage 11', 'Welchen CSS-Selektorstring verwenden sie, um das <button>-Element mit dem Attribut favorite zu finden?')
output('Antwort', 'BeautifulSoup: soup.select(\'button[value="favorite"]\')Selenium: browser.find_element_by_name(\'favorite\'')

output('Frage 12', 'Nehmen sie an in der Variabel spam ist das Tag-Objekt von BeautifulSoup für das Element <div>Hallo</div> gespeichert. Wie rufen sie den String "Hallo" aus diesem Objekt ab?')
output('Antwort', 'Mittels spam.getText()')

output('Frage 13', 'Wie speichern sie alle Attribute eines Tag-Objekts von BeautifulSoup in der Variabel linkElem?')
output('Antwort', 'linkElem.attrs')

output('Frage 14', 'Der Befehl import selenium funktioniert nicht. Wie importieren sie das Modul Selenium auf korrekte Weise?')
output('Antwort', 'from selenium import webdriver')

output('Frage 15', 'Was ist der Unterschied zwischen den find_element_* ind den find_elements_*-Methoden?')
output('Antwort', 'find_element_ findet das erstbeste Resultat während find_elements_ alle zutreffenden Elemente findet und in einer Liste speichert.')

output('Frage 16', 'Welche WebElement-Methoden bietet Selenium zum Simulieren von Mausklicks und Tastenbetätigungen?')
output('Antwort', 'das Modul element.click() oder from selenium.webdriver.common.keys import Keys und dann mit browser.send_key(Keys.UP/DOWN/LEFT/RIGHT/TAB/ENTER/RETURN/ect...)')

output('Frage 17', 'Welche einfachere Möglichkeit bietet Selenium zum Absenden eines Forumulars, anstatt send_keys(Keys.ENTER) für das WebElement-Objekt der Submit-Schaltfläche abzurufen?')
output('Antwort', 'browser_element.submit()')

output('Frage 18', 'Wie können sie mit Selenium einen Klick auf die Browserschaltflächen Weiter, Zurück und Aktualisieren simulieren?')
output('Antwort', 'Weiter: browser.forward() Zurück: browser.back() Aktualisieren: browser.refresh()')
