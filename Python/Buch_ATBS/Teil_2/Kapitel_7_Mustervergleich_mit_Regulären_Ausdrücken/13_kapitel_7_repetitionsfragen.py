# Wiederholungsfragen für das Kapitel 8, Reguläre Ausdrücke
maxl=53
dv=13
import re
def ouput(string):
    delta=len(string)%maxl
    if delta != 0:
        string+=' '*(maxl-delta)
    pattern=re.compile(r'[a-zA-Z0-9.,()\-_:; ÄÖÜäöü\W\?"]{'+str(maxl-dv)+r','+str(maxl)+r'}[\s\W.,\-_:;]')
    output=pattern.findall(string)
    for i in range(len(output)):
        print(output[i])


string='1. Welche Funktion erstellt Regex Objekte?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Die Funktion re.compile() erstellt die zur Suche benötigten Regexobjekte'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='2. Warum werden beim Erstellen von Regex-Objekten häufig Rohstrings verwendet?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Da innerhalb der Rohstrings alle Zeichen erfasst werden inklusive \\n, \\t ect somit muss man nicht mit doppeltem Backslash arbeiten.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='3. Was gibt die Methode search() zurück?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Die Methode search() gibt immer den ersten übereinstimmenden String aus. Auch Match-Objekt genannt.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='4. Wie können sie die Strings der übereinstimmenden Texte aus dem Match-Objekt gewinnen?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Die Methode group() gibt Match-Objekte zurück.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'5. Wofür steht die Gruppe 0 bei dem regulären Ausdruck r"(\d\d\d)-(\d\d\d-\d\d\d\d)". Wofür steht Gruppe 1 und 2?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Gruppe 0: Gesammte übereinstimmung, Gruppe 1: Erster Satz, Gruppe 2: Zweiter Satz.
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'6. In regulären Ausdrücken haben Punkte und Klammern eine besondere Bedeutung. Wie suchen sie mit einem regulären Ausdruck nach Klammern und Punkten im Text?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Mit \. bzw. \), \(. Innerhalb von Eckklammern kann man den Backslash weglassen.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='7. Wann gibt die Methode findall() eine Liste zurück und wann Tuples?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Wird im regulären Ausdruck mehr als ein Ziel gesetzt so entsteht ein Tuple. Sucht man nur nach einem einzigen Ausschnitt entsteht eine Liste. Beispiel Tuple: r"(\w)(\d)" - Liste: r"(\w\d)"'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'8. Was bedeutet das Zeichen | in einem regulären Ausdruck?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Das bedeutet OR. So kann man meherere mögliche Bedingungen für einen Ausschnitt prüfen.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'9. Welche zwei Bedeutungen kann das Zeichen ? in einem regulären Ausdruck haben?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Man kann es benutzen um eine Bedingung auf nicht gierig zu stellen um so die anderen Bedingungen zu bevorzugen. Man kann es jedoch auch hinter eine Bedingung stellen ob der gesuchte ausschnitt null oder ein mal vorhanden sein soll.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='10. Was ist der Unterschied zwischen dem Zeichen + und * in einem regulären Ausdruck?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Hängt man an eine Bedingung ein * kann man sagen dass die Bedingung wahr ist oder gar nicht verwendet werden soll falls sie unwahr ist. Hängt man ein + hinter eine Bedingung heisst das dass sie sich wiederholen darf. Z.b. r"\w(ma)+" Was bedeutet dass sich das ma mehrmals wiederholen darf am ende des Textes.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string='11. Was ist der Unterschied zwischen der angabe {3} und {3,5}'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string='Hängen geschwungene Klammern hinter einer Bedingung heisst das dass die Bedingung so oft wie die Zahl in den Klammern vorkommen soll. Steht da {3,5} heisst das dass die Bedingung zwischen 3 und 6 mal zutreffen muss.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'12. Wofür stehen die Zeichenklassen \d \w und \s?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Die Zeichenklasse \d steht für alle Zahlen, \w für alle Buchstaben und \s für whitespaces.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'13. Wofür stehen die Zeichenklassen \D, \W und \S?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Sie sind das komplette gegenteil von \d, \w und \s. Wird ein \D geschrieben heisst das es dürfen Keine Zahlen vorkommen, bei \W keine Buchstaben und bei \S keine Whitespaces.'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'14. Wie sorgen sie dafür dass ein regulärer Ausdruck nicht zwischen Gross- und Kleinschreibung unterscheidet?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Am ende des Regulären ausdrucks setzt man nach einem Komma noch re.I. Z.B. (r"\w", re.I). Mann kann jedoch auch \w verwenden oder [a-zA-Z]'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'15. Wofür steht das Zeichen . normalerweise? Wofür steht es wenn sie re.DOTALL als zweites Argument and re.compile() übergeben?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Wird re.DOTALL nicht verwendet Zählt . für alle Zeichen ausser den Zeilenumbruch. Mit DOTALL wird auch der Zeilenumbruch dazugezählt'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'16. Was ist der Unterschied zwischen (.*) und (.*?)'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Setzt man das Fragezeichen so wird die jeweils kleinste mögliche Übereinstimmung gesucht (nicht gierig). Ohne Fragezeichen die grösste (gierig).'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'17. Welche Zeichenklasse geben sie an um eine Übereinstimmung mit allen Ziffern und kleinbuchstaben zu suchen?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Ich verwende [0-9a-z] oder [\da-z]'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'18. Gegeben ist der folgende Reguläre Ausdruck: r"\d+" was gibt regex.sub("X", "12 Drummers, 11 Pipers, five rings, 3 hens") zurück?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Mit der Methode sub() lassen sich Abschnitte im Text ersetzen. Im gegebenen Beispeil wäre dies dann "X drummer, X pipers, five rings, X hens"'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'19. Was können sie tun wenn re.VERBOSE als zweites Argument an re.compile() übergeben wird?'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'Ich kann die Regulären Ausdrücke Mehrzeilig schreiben'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'20. Wie schreiben sie einen regulären Ausdruck, um nach Zahlen zu suchen, die wie im englischen üblich Kommas als Tausendertrennzeichen verwenden? Der Ausdruck muss folgende Zahlen finden können: 42, 1,234, 6,368,745'
print(''.center(maxl, '█'))
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'[,\d{1,3}]+[^,\s]'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'21. Wie schreiben sie einen Regulären Ausdruck, die die Vollständigen Namen aller Personen mit dem Namen Nakamoto findet? Sie können voraussetzen dass der Vorname vor dem Nachnamen steht immer nur ein Wort beinhaltet und diese Jeweils mit Grossbuchstaben beginnen. '
print(''.center(maxl, '█'))
ouput(string)
string=r'Folgende Namen müssen gefunden werden: Satoshi Nakamoto, Alice Nakamoto, RoboCop Nakamoto'
ouput(string)
string=r'Folgende Namen müssen Ignoriert werden: satoshi Nakamoto, Mr. Nakamoto, Satoshi nakamoto'
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'([A-Z]\w+)\s[N]\w{7} oder ([A-Z]\w+) Nakamoto'
ouput(string)
input()
print(''.center(maxl, '█'))
print()

string=r'22. Wie schreiben sie einen regulären Ausdruck, der Sätze mit folgendem Aufbau findet: 1. Wort: Alice Bob oder Carol, 2. Wort: eats, pets, throws und 3. Wort: apples, cats oder baseball. Die sätze müssen mit einem Punkt enden und es wird nicht auf die Gross- und Kleinschreibung geachtet. Der Ausdruck muss also folgende Sätze finden können:'
print(''.center(maxl, '█'))
ouput(string)
string='Positivbeispiele: Alice eats apples. Bob pets cats. Carol throws baseball. Alice throws Apples. BOB EATS CATS.'
ouput(string)
string='Negativbeispiele: RoboCop eats apples. ALICE THROWS FOOTBALLS, Carl eats 7 cats.'
ouput(string)
print()
input()
print(''.center(maxl, '█'))
string=r'"(Alice|Bob|Carol) (eats|pets|throws) (apples|cats|baseball)\.", re.I'
ouput(string)
input()
print(''.center(maxl, '█'))
print()
