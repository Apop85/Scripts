# 36_assertion_ampelsimulation.py
# Dieses Script verwendet assertion um sicherzustellen dass der Code so läuft wie geplant. 

from time import sleep

ampel1={'nord_sued' : 'green', 'west_ost' : 'red'}
ampel2={'nord_sued' : 'red', 'west_ost' : 'green'}

def switch_lights():
    try:
        while True:  
            for keys in ampel1.keys():
                if ampel1[keys] == 'green':
                    ampel1[keys] = 'yellow' 
                    print_it(keys)
                    ampel1[keys] = 'red'
                    print_it(keys)
                    ampel2[keys] = 'yellow'
                    print_it(keys)
                    ampel2[keys] = 'green'
                    print_it(keys)
                else:
                    ampel2[keys] = 'yellow'
                    print_it(keys)
                    ampel2[keys] = 'red'
                    print_it(keys)
                    ampel1[keys] = 'yellow'
                    print_it(keys)
                    ampel1[keys] = 'green'
                print_it(keys)
                sleep(5)
    except Exception as error_message:
        print(error_message)


def print_it(key):
    try:
        assert ampel1[key] and ampel2[key] == 'red' or ampel1[key] != ampel2[key], 'Ampeln sind gleichgeschaltet!'
        sleep(0.5)
        print(''.center(36, '#'))
        print(''.center(36, '#'))
        # Ampel1
        if ampel1[key] == 'red':
            print('[ '+''.ljust(10)+'|'+''.ljust(10)+'|'+ampel1[key].center(10)+' ]')
        elif ampel1[key] == 'yellow':
            print('[ '+''.ljust(10)+'|'+ampel1[key].center(10)+'|'+''.ljust(10)+' ]')
        else:
            print('[ '+ampel1[key].center(10)+'|'+''.ljust(10)+'|'+''.ljust(10)+' ]')
        # Ampel1
        if ampel2[key] == 'red':
            print('[ '+''.ljust(10)+'|'+''.ljust(10)+'|'+ampel2[key].center(10)+' ]')
        elif ampel2[key] == 'yellow':
            print('[ '+''.ljust(10)+'|'+ampel2[key].center(10)+'|'+''.ljust(10)+' ]')
        else:
            print('[ '+ampel2[key].center(10)+'|'+''.ljust(10)+'|'+''.ljust(10)+' ]')
        print(''.center(36, '#'))
        print(''.center(36, '#'))
    except Exception as error_message:
        print(error_message)

switch_lights()