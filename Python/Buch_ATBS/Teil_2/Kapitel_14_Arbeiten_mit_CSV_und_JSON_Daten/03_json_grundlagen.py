# 03_json_grundlagen.py

import os, re, json
os.chdir(os.path.dirname(__file__))

max_text_length=70
max_text_delta=24

def output(title, string):
    print('╔'+''.center(max_text_length+8, '═')+'╗')
    print('║ '+title.center(max_text_length+7).upper()+'║')
    print('╠'+''.center(max_text_length+8, '═')+'╣')
    string=string+' '*max_text_length
    search_pattern=re.compile(r'\w+.{'+str(max_text_length-max_text_delta-7)+r','+str(max_text_length-7)+r'}[ |.|,|\n|>|\W]', re.DOTALL)
    results=search_pattern.findall(string)
    for line in results:
        print('║ '+line.strip()+'║'.rjust(max_text_length+8-len(line.strip())))
    print('╚'+''.center(max_text_length+8, '═')+'╝')
    input()

output('json Dateien', 'JSON oder auch "Java Script Object Notation" ist eine Häufig genutzte Möglichkeit um Daten in Form eines einzelnen, für Menschen lesbaren Strings darzustellen. Es handelt sich dabei um die Art und Weise wie Java-Programme Datenstrukturen schreiben. Es weist einen ähnlichen Aufbau auf wie die Ausgabe der Funktion pprint')

output('Beispiel', 'Beispiel json-Eintrag: ["name" : "Zophie", "is_cat" : true, "mice_cought" : 0, "naps_taken" : 37.5, "feline_iq" : null]')

output('Verwendung von json', 'JSON-Dateien sind weit verbreitet und es lohnt sich daher sich mit diesen auzukennen. Viele Webseiten nutzen json Dateien um Daten für die Webseite bereitzustellen die dann Serverseitig aufgenommen und verarbeitet werden. Das nennt sich eine json-API.')

output('json-API', 'Viele Webseiten bieten eine json-API an um programme mit ihrer Webseite kommunizieren zu lassen. Man muss in der Beschreibung des Anbieters nachschauen wie man die API aufruft und wie die zurückgesendete Datenstruktur aussieht. Somit lassen sich beispielsweise Posts aus sozialen Medien auslesen oder senden oder es lassen sich auch Wörter übersetzen auf deepl.com mittels einer API')

output('Beispielnutzung', 'Rohdaten aus einer Website auslesen ist mit der API meist deutlich einfacher als den Quellcode mit BeautifulSoup oder manuell mit requests zu durchforsten.')
output('Beispielnutzung', 'Neue Posts aus dem Social-Media-Konto automatisch downloaden und auf einem anderen Netzwerk teilen.')
output('Beispielnutzung', 'Eine Filmenzeklopedie über die eigene Filmsamlung aufstellen indem man die Daten von rottentomatoes.com o.ä. abfragt und sammelt.')

output('json Modul', 'Das Modul json nimmt ihnen mit dem befehl json.loads() und json.dumps() die Umwandlung von json-Einträgen in von Python verständlichen Code ab')

output('json.loads()', 'Um einen String mit json-Daten in einen von Python verständlichen String umzuwandeln verwendet man json.loads(file_name). Dies wandelt die json-Einträge in ein Dictionary um.')
output('Beispiel', 'Übergibt man den String vom Obigen Beispiel in json.loads() erhält man folgende Ausgabe:'+str(json.loads('{"name" : "Zophie", "is_cat" : true, "mice_cought" : 0, "naps_taken" : 37.5, "feline_iq" : null}')))

output('json.dumps()', 'Mit json.dumps() lassen sich Strings aus Python in jsongerechte Strings umwandeln.')
output('Beispiel', 'Nimmt man die vorherige Ausgabe und übergibt diese wiederum json.dumps() wird daraus folgende Ausgabe: '+str(json.dumps({'name' : 'Zophie', 'is_cat' : True, 'mice_cought' : 0, 'naps_taken' : 37.5, 'feline_iq' : None})))

output('Einträge', 'Nur folgende Werte sind als json-Eintrag zulässig: Strings, Integer, Zahle, Fliesskommazahlen, Boolean-Werte, Listen, Dictionarys und None')
