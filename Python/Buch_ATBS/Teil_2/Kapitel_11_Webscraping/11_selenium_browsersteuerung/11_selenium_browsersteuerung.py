# 11_selenium_browsersteuerung.py
# In diesem Script geht es darum Selenium kennenzulernen
# Für dieses Script muss sowohl selenium nachinstalliert werden und der aktuellste
# Geckodriver gedownloaded werden. (https://github.com/mozilla/geckodriver/releases)

from time import sleep
from selenium import webdriver
from selenium.webdriver.common.keys import Keys
import os, re

max_text_length=70
max_text_delta=20

def output(string, title):
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

gecko = os.path.normpath(os.path.join(os.path.dirname(__file__), 'geckodriver'))
binary = r'C:\Program Files (x86)\Mozilla Firefox\firefox.exe'

title='Selenium nutzen'
text='Wenn man Selenium nutzen möchte muss man definieren wo sich Firefox und der Geckodriver befindet.'
output(text, title)

title='Der Befehl .get()'
text='Mit dem Befehl .get() lässt sich anschliessend ein von Selenium gesteuertes Firefoxfenster starten.'
output(text, title)

title='Browserklasse'
text='Das Browserelement weist folgenden Typ auf: <class \'selenium.webdriver.firefox.webdriver.WebDriver\'>'
output(text, title)

title='find_element(s)_*'
text='Mit der Methode find_element_* wird das nächste zutreffende Ergebnis angezeigt und der Rest wird ignoriert. Mit find_elements_* werden alle Übereinstimmungen rausgesucht.'
output(text, title)

title='Methoden für find_elements_'
text='Folgende Methoden sind für find_element(s)_ verfügbar: _by_class_name(), _by_css_selector(), _by_link_text(), _by_partial_link_text(), _by_name(), _by_tag_name()'
output(text, title)

title='_by_class_name()'
text='Mit find_elements_by_class_name() lassen sich Elemente der angegeben CSS-Klasse finden.'
output(text, title)

title='_by_css_selector()'
text='Mit find_elements_by_css_selector() lassen sich Elemente dem angegeben CSS-Selektor finden.'
output(text, title)

title='_by_id()'
text='Mit find_elements_by_id() lassen sich Elemente dem angegeben Wert mit dem Attribut id finden.'
output(text, title)

title='_by_link_text()'
text='Mit find_elements_by_link_text() lassen sich <a> Elemente mit genau dem angegebenen Linktext finden.'
output(text, title)

title='_by_partial_link_text()'
text='Mit find_elements_by_patial_link_text() lassen sich <a> Elemente deren Link den angegebenen Text enthält finden.'
output(text, title)

title='_by_name()'
text='Mit find_elements_by_name() lassen sich Elemente mit dem angegebenen Namen finden.'
output(text, title)

title='_by_tag_name()'
text='Mit find_elements_by_tag_name() lassen sich Elemente mit dem angegbenen Tag finden. z.B. <a>, <td> ect.. Es wird jedoch nicht zwischen Gross- und Kleinschreibung unterschieden. Wenn man nach <a> sucht wird auch <A> gefunden werden.'
output(text, title)

title='Attribute eines Elements auslesen'
text='Nach erfolgreicher Suche kann das Suchergebnis noch weiter ausgelesen werden mittels nachfolgenden Befehlen:'
output(text, title)

title='tag_name'
text='Mit tag_name findet man den Namen des Tags z.B. findet er "a" in einem <a> element.'
output(text, title)

title='get_attribute(name)'
text='Mit get_attribute(name) kann man den Wert des Attributs mit dem angegebenen Namen auslesen.'
output(text, title)

title='text'
text='Mit text lässt sich der Text eines Elements auslesen. Z.B. "Hallo" aus <span>Hallo</span>'
output(text, title)

title='clear()'
text='Mit clear kann man den Inhalt wieder löschen.'
output(text, title)

title='is_displayed()'
text='Mit is_displayed() zeigt an ob ein Objekt im Browser sichtbar (True) oder nicht (False) ist.'
output(text, title)

title='is_enabled()'
text='Mit is_enabled() kann man überprüfen ob ein Eingabeelement aktiviert (True) oder nicht (False) ist.'
output(text, title)

title='is_selected()'
text='Mit is_selected() lässt sich auslesen ob ein Kontrollkästchen oder Optionsfelder aktiviert (True) oder nicht (False) sind.'
output(text, title)

title='location'
text='Location ist ein Dictionary welches die X und Y Koordinaten eines Elements enthält. Diese Braucht man um Anschliessend Klicks im Browser zu erzeugen'
output(text, title)

title='Beispiel'
text='Nachfolgend wird Firefox gestartet und es wird geprüft ob sich die Klasse "card-img-top" auf inventwithpython.com befindet. Firefox öffnet und schliesst sich in den nachfolgenden Beispielen selbstständig.'
output(text, title)

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
browser.get('http://inventwithpython.com')
try:
    result=browser.find_element_by_class_name('cover-thumb')
    title='Gefunden'
    text='Selenium hat erfolgreich ein <%s> Element mit dem Namen "card-img-top" gefunden.' % (result.tag_name)
    sleep(2)
    browser.quit()
    output(text, title)
except:
    title='Nope. Das war nichts.'
    text='Selenium hat das angegebene Element auf der Seite nicht gefunden.'
    browser.quit()
    output(text, title)

title='click()'
text='Mit der Funktion click() lassen sich Elemente und Links gezielt anklicken. Nachfolgend wird Firefoxaufgerufen und der Link mit dem Text "Read It Online" wird gesucht und falls vorhanden angeklickt. Firefox kann nach ausführung wieder geschlossen werden.'
output(text, title)

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
browser.get('http://inventwithpython.com')
try:
    link_result=browser.find_element_by_link_text('Read Online for Free')
    link_result.click()
    title='Das hat Geklappt'
    text='Selenium hat den Link gefunden und erfolgreich darauf geklickt.'
    sleep(5)
    browser.quit()
    output(text, title)
except:
    title='Nope. Das war nichts.'
    text='Selenium konnte den angegebenen Link leider nicht finden. '
    sleep(2)
    browser.quit()
    output(text, title)

title='send_keys()'
text='Wenn man Textfelder ermittelt hat kann man diese mit send_keys() ausfüllen. Nachfolgend wird wieder Firefox geöffnet und auf mail.yahoo.com nach dem Feld für den Usernamen und das Passwort gesucht und diese dann Ausgefüllt. Danach kann Firefox wieder geschlossen werden. Mit submit() werden diese dann abgesendet.'
output(text, title)

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
browser.get('http://mail.yahoo.com')
try:
    username_text_field=browser.find_element_by_id('login-username')
    username_text_field.send_keys('username@yahoo.com')
    username_text_field.submit()
    title='Das hat Geklappt'
    text='Selenium hat das Textfeld gefunden und ausgefüllt.'
    sleep(5)
    browser.quit()
    output(text, title)
except:
    title='Nope. Das war nichts.'
    text='Selenium konnte das angegebene Textfeld leider nicht finden. '
    sleep(2)
    browser.quit()
    output(text, title)

from selenium.webdriver.common.keys import Keys
title='Betätigung von Sondertasten simulieren'
text='Manchmal ist es Notwendig innerhalb eines Formulars gewisse Sondertasten zu betätigen. Sei dies TAB oder die Pfeiltasten oder was auch immer. Dazu kann man jedoch erstmal die Funktion Keys aus selenium.webdriver.common.keys importieren. Damit lassen sich dann Sondertasten dem Browser übermitteln.'
output(text, title)

title='Beispiele für Sondertastenbefehle'
text='Keys.UP/DOWN/LEFT/RIGHT für die Pfeiltasten. Keys.ENTER/RETURN für die Tasten Enter und Return. Keys.HOME/END/PAGE_UP/PAGE_DOWN für die entsprechenden Tasten Home, End, PgUp und PgDwn. Keys.ESCAPE/BACK_SPACE/DELETE für ESC Backspace und Delete. Keys.F1/F2/../F12 für die Funktionstasten. Keys.TAB für den Tabulator.'
output(text, title)

title='Beispiel für Keys.'
text='Nachfolgend wird wieder Firefox geöffnet und durch die Übermittlung von Sondertasten wird im Browserfenster hoch und runter gescrollt.'
output(text, title)

browser=webdriver.Firefox(firefox_binary=binary, executable_path=gecko+'.exe')
browser.get('http://inventwithpython.com')
try:
    html_element=browser.find_element_by_tag_name('html')
    html_element.send_keys(Keys.PAGE_DOWN)
    sleep(0.5)
    for i in range(5):
        html_element.send_keys(Keys.PAGE_DOWN)
        sleep(0.5)
        html_element.send_keys(Keys.PAGE_UP)
        sleep(0.5)
    sleep(5)
    title='Das hat Geklappt'
    text='Selenium hat die Sondertasten erfolgreich übermittelt.'
    browser.quit()
    output(text, title)
except:
    sleep(2)
    browser.quit()
    title='Nope. Das war nichts.'
    text='Selenium konnte die Sonderzeichen leider nicht übermitteln.'
    output(text, title)
    
title='Browserkontrollschalter'
text='Mit den Befehlen .back(), .forward(), .quit() und .refresh() kann man den Browser entsprechend Steuern. Damit wurden zuvor auch schon Die Browserfenster automatisch geschlossen.'
output(text, title)