# Umrechnen zwischen unterschiedlichen Einheiten im molaren und metrischen System
# ToDo: Was tun wenn auf wiki nichts gefunden wurde?, mol_hard_calculate

import re, requests, os, logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
os.chdir(os.path.dirname(__file__))

periodic_table={ 'H' : 1.0079, 'He' : 4.0026,
                 'Li' : 6.941, 'Be' : 9.0122, 'B' : 10.811, 'C' : 12.011, 'N' : 14.007, 'O' : 15.999, 'F' : 18.988, 'Ne' : 20.180,
                 'Na' : 22.990, 'Mg' : 24.305, 'Al' : 26.982, 'Si' : 28.086, 'P' : 30.974, 'S' : 32.065, 'Cl' : 35.453, 'Ar' : 39.948,
                 'K' : 39.098, 'Ca' : 40.078, 'Sc' : 44.956, 'Ti' : 47.867, 'V' : 50.942, 'Cr' : 51.996, 'Mn' : 54.938, 'Fe' : 55.845, 'Co' : 58.933, 'Ni' : 58.693, 'Cu' : 63.546, 'Zn' : 65.38, 'Ga' : 69.723, 'Ge' : 72.64, 'As' : 74.922, 'Se' : 78.96, 'Br' : 79.904, 'Kr' : 83.798,
                 'Rb' : 85.468, 'Sr' : 87.62, 'Y' : 88.906, 'Zr' : 91.224, 'Nb' : 92.906, 'Mo' : 95.96, 'Tc' : 98.91, 'Ru' : 101.07, 'Rh' : 102.91, 'Pd' : 106.42, 'Ag' : 107.87, 'Cd' : 112.41, 'In' : 114.82, 'Sn' : 118.71, 'Sb' : 121.76, 'Te' : 127.60, 'I' : 126.90, 'Xe' : 131.29,
                 'Cs' : 132.91, 'Ba' : 137.33, 'Hf' : 178.49, 'Ta' : 180.95, 'W' : 183.84, 'Re' : 186.21, 'Os' : 190.23, 'Ir' : 192.22, 'Pt' : 195.08, 'Au' : 196.97, 'Hg' : 200.59, 'Tl' : 204.38, 'Pb' : 207.2, 'Bi' : 208.98, 'Po' : 209.98, 'At' : 210, 'Rn' : 222,
                 'Fr' : 223, 'Ra' : 226.03, 'Rf' : 261, 'Db' : 262, 'Sg' : 263, 'Bh' : 262, 'Hs' : 265, 'Mt' : 266, 'Ds' : 296, 'Rg' : 272, 'Cn' : 277, 'Nh' : 287, 'Fl' : 289, 'Mc' : 288, 'Lv' : 289, 'Ts' : 293, 'Og' : 294,
                 'La' : 138.91, 'Ce' : 140.12, 'Pr' : 140.91, 'Nd' : 144.24, 'Pm' : 146.90, 'Sm' : 146.90, 'Eu' : 151.96, 'Gd' : 157.25, 'Tb' : 158.93, 'Dy' : 162.50, 'Ho' : 164.93, 'Er' : 167.26, 'Tm' : 168.93, 'Yb' : 173.05, 'Lu' : 174.97,
                 'Ac' : 227, 'Th' : 232.04, 'Pa' : 231.04, 'U' : 238.03, 'Np' : 237.05, 'Pu' : 244.10, 'Am' : 243.10, 'Cm' : 247.10, 'Bk' : 247.10, 'Cf' : 251.10, 'Es' : 254.10, 'Fm' : 257.10, 'Md' : 258, 'No' : 259, 'Lr' : 260}

conversion_factors={ 'fg' : 10**-15, 'pg' : 10**-12, 'ng' : 10**-9, 'mcg' : 10**-6, 'mg' : 10**-3, 'g' : 1, 'kg' : 10**3,
                    'fmol' : 10**-15, 'pmol' : 10**-12, 'nmol' : 10**-9, 'mcmol' : 10**-6, 'mmol' : 10**-3, 'mol' : 1,
                    'fl' : 10**-15, 'pl' : 10**-12, 'nl' : 10**-9, 'mcl' : 10**-3, 'ml' : 10**-3, 'dl' : 10**-1, 'l' : 1 }

def get_substance():
    print('Bitte Stoffnamen oder Summenformel angeben:')
    substance=input()
    check_pattern=re.compile(r'[A-Z0-9]')
    capital=check_pattern.findall(substance)
    check_pattern=re.compile(r'[a-z]')
    lower_case=check_pattern.findall(substance)
    if len(capital) > len(lower_case):
        logging.debug('Summenformel erkannt. A-Z0-9:'+str(len(capital))+' > a-z:'+str(len(lower_case)))
        molar_mass=calculate_molar_mass(substance)
        substance, substance_names=lookup_sum_formula(substance)
        return molar_mass, substance, substance_names
        ## GET CHEMICAL NAME @SUMFORMULA_LOOKUP_WEBSITE
    else:
        logging.debug('Stoffname erkannt. A-Z0-9:'+str(len(capital))+' < a-z:'+str(len(lower_case)))
        molar_mass=get_molar_mass_by_name(substance)
        return molar_mass, substance, ''

def lookup_sum_formula(substance):
    url_name='https://webbook.nist.gov/cgi/cbook.cgi?Formula=+'+substance+'&NoIon=on&Units=SI'
    url_content=requests.get(url_name)
    check=check_url(url_content)
    if check == 1:
        substance_name = 'UNKNOWN'
        return substance_name, ''
    tmpfile=open('.\\tmpfile.tmp', 'wb')
    for chunk in url_content.iter_content(10**6):
        tmpfile.write(chunk)
    tmpfile.close()
    tmpfile=open('.\\tmpfile.tmp', encoding='UTF-8')
    content=tmpfile.read()
    tmpfile.close()
    os.remove('.\\tmpfile.tmp')
    substance_names=search_substance_name(content)
    return substance, substance_names

def search_substance_name(content):
    possible_substance_names=re.compile(r'SI">([A-Z|a-z|,| |0-9|-]*?)</a>')
    substance_names=possible_substance_names.findall(content)
    input()
    return substance_names

def get_molar_mass_by_name(substance):
    url_name='https://de.wikipedia.org/wiki/'+substance.title()
    url_content=requests.get(url_name)
    check=check_url(url_content)
    if check == 1:
        url_name='https://de.wikipedia.org/wiki/'+substance.title()+'eigenschaften'
        url_content=requests.get(url_name)
        check=check_url(url_content)
    if check == 1:
        logging.error('Wikipediaeintrag nicht gefunden.')
        raise Exception('Wikipediaeintrag nicht gefunden!')
    tmpfile=open(r'.\tmpfile.tmp', 'wb')
    for chunk in url_content.iter_content(10**6):
        tmpfile.write(chunk)
    tmpfile.close()
    tmpfile=open(r'.\tmpfile.tmp', encoding='UTF-8')
    content=tmpfile.read()
    tmpfile.close()
    os.remove(r'.\tmpfile.tmp')
    molar_mass=search_molar_mass(content)
    return molar_mass

def search_molar_mass(content):
    cut_search_pattern=re.compile(r'Molare Masse<(.*?)</p>', re.DOTALL)
    content_cut=cut_search_pattern.findall(content)
    molar_mass_search_pattern=re.compile(r'>(\d+),(\d+) ')
    molar_mass=molar_mass_search_pattern.findall(str(content_cut))
    molar_mass='.'.join(molar_mass[0])
    logging.info('Molare Masse gefunden: '+str(molar_mass))
    return float(molar_mass)

def check_url(url_content):
    try:
        url_content.raise_for_status()
        return 0
    except Exception as error_message:
        logging.debug('URL nicht gefunden')
        return 1

def calculate_molar_mass(sum_formula):
    molar_mass=0
    pattern=re.compile(r'[A-Z][^A-Z]*')
    pattern2=re.compile(r'([a-zA-Z]+)(\d+)')
    elemente=re.findall(pattern, sum_formula)
    for element in elemente:
        zahl=re.findall(pattern2, element)
        if len(zahl) != 0:
            molar_mass+=periodic_table[zahl[0][0]]*int(zahl[0][1])
        else:
            molar_mass+=periodic_table[element]
    return molar_mass

def mol_easy_calculate(molar_mass, source_unit, target_unit, source_value):
    conversion_factor=(conversion_factors[source_unit[0]]/conversion_factors[target_unit[0]])/(conversion_factors[source_unit[1]]/conversion_factors[target_unit[1]])
    if 'mol' in source_unit[0]:
        logging.debug('Rechne von molarem System zu metrisch: '+'/'.join(source_unit)+' nach '+'/'.join(target_unit))
        # Stoffmenge(n) = konzentration(m) * molare_masse(M) * umrechnungsfaktor(u) --> n=m/M*u
        substance_concentration=((float(source_value)*molar_mass)*conversion_factor)
        logging.debug('Führe Berechnung durch: '+str(source_value)+'/'.join(source_unit)+' * '+str(molar_mass)+'g/mol * '+str(conversion_factor))
        return substance_concentration
    else:
        logging.debug('Rechne von metrischem System zu molarem: '+'/'.join(source_unit)+' nach '+'/'.join(target_unit))
        # Konzentraton(m) = stoffmenge(n) / molare_masse(M) * umrechnungsfaktor(u) == m=n*M*u
        substance_concentration=((float(source_value)/molar_mass)*conversion_factor)
        logging.debug('Führe Berechnung durch: '+str(source_value)+'/'.join(source_unit)+' / '+str(molar_mass)+'g/mol * '+str(conversion_factor))
        return substance_concentration

def mol_hard_calculate(molar_mass, source_unit, target_unit, source_value):
    print('No Idea Yet')

def nomol_calculate(source_unit, source_value, target_unit):
    conversion_factor=(conversion_factors[source_unit[0]]/conversion_factors[target_unit[0]])/(conversion_factors[source_unit[1]]/conversion_factors[target_unit[1]])
    result=float(source_value)/conversion_factor
    logging.debug('Umrechung im selben Bezugssystem. Umrechnungsfaktor: '+str(conversion_factor)+', Resultat: '+str(result))
    return result

def check_input(choose_value, target_unit):
    try:
        choose_value=choose_value.split(' ')
        target_unit=target_unit.split('/')
        choose_value[0], choose_value[1] = choose_value[0].lower(), choose_value[1].lower()
        source_unit, source_value=choose_value[1].split('/'), choose_value[0]
        logging.debug('Folgende unterteilungen gefunden: '+str(source_unit)+' '+str(target_unit)+' '+str(source_value))
        if source_unit[0] and source_unit[1] and target_unit[0] and target_unit[1] in conversion_factors.keys():
            logging.debug('Einheiten in Umrechnungstabelle vorhanden.')
            if 'mol' in target_unit[1] and 'mol' in source_unit[0] or 'mol' in source_unit[0] and 'mol' in source_unit[1]:
                raise Exception('Diese Angabe macht keinen Sinn.')
            elif 'l' in target_unit[1] and 'l' in source_unit[1] and 'mol' not in source_unit[0] and 'mol' not in target_unit[0]:
                logging.debug('Angabe des Stoffs unnötig da beide Einheiten im Metrischen-System (Liter) sind')
                molar_mass, substance='UNKNOWN', 'UNKNOWN'
                result=nomol_calculate(source_unit, source_value, target_unit)
                output_results(substance, molar_mass, result, source_value, source_unit, target_unit, '')
            elif 'mol' in target_unit[0] and 'mol' in source_unit[0] and target_unit[1][-1:] == source_unit[1][-1:]:
                logging.debug('Angabe des Stoffs unnötig da beide Einheiten im Mol-System')
                molar_mass, substance='UNKNOWN', 'UNKNOWN'
                result=nomol_calculate(source_unit, source_value, target_unit)
                output_results(substance, molar_mass, result, source_value, source_unit, target_unit, '')
            elif 'g' in target_unit[0][-1:] and 'g' in source_unit[0][-1:]:
                logging.debug('Angabe des Stoffs unnötig da beide Einheiten im Metrischen-System (Gramm) sind')
                molar_mass, substance='UNKNOWN', 'UNKNOWN'
                result=nomol_calculate(source_unit, source_value, target_unit)
                output_results(substance, molar_mass, result, source_value, source_unit, target_unit, '')
            elif source_unit[1][-1:] == target_unit[1][-1:] and 'mol' not in target_unit[1] or 'mol' in source_unit[1]:
                logging.debug('Umrechnung von Metrischem zu Molarem System notwendig. (Im selben Bezugssystem)'+str(source_unit[1][-1:])+'='+str(target_unit[1][-1:])) # z.b. nmol/l nach mg/ml und nicht nmol/l nach mg/kg
                molar_mass, substance, substance_names=get_substance()
                result=mol_easy_calculate(molar_mass, source_unit, target_unit, source_value)
                output_results(substance, molar_mass, result, source_value, source_unit, target_unit, substance_names)
            elif 'mol' not in target_unit[1] or 'mol' in source_unit[1]:
                logging.debug('Umrechnung von Metrischem zu Molarem System notwendig. (Nicht im selben Bezugssystem)'+str(source_unit[1][-1:])+'='+str(target_unit[1][-1:])) # z.b. nmol/l nach mg/ml und nicht nmol/l nach mg/kg
                molar_mass, substance, substance_names=get_substance()
                output_results(substance, molar_mass, result, source_value, source_unit, target_unit, substance_names)
                #### CALC MISSING
            elif 'mol' in target_unit[1] or source_unit[1]:
                raise Exception('Berechnung kann mit diesen Einheiten nicht durchgeführt werden.'+'/'.join(source_unit)+' zu '+'/'.join(target_unit))
            else:
                logging.critical('Da ging was ziemlich schief:'+str(source_unit)+' - '+str(target_unit))
                raise Exception('Etwas unerklärliches ist passiert... keine Ahnung was genau...')
        else:
            raise Exception('conversion_factors nicht gefunden.')
    except Exception as error_message:
        print('Fehler: %s' %(error_message))

def choose_menu():
    while True:
        print('Umzurechnender Wert (z.b. 123.45 mg/dl):')
        choose_value=input()
        print('In welche Einheit soll umgerechnet werden?')
        target_unit=input()
        target_unit=target_unit.lower()
        check_input(choose_value, target_unit)

def output_results(name, molar_mass, result, source_value, source_unit, target_unit, substance_names):
    breakpoint
    line1=' Umrechnung von '+str(source_value)+' '+'/'.join(source_unit)+' zu '+'/'.join(target_unit)+' '
    line2=' Stoffname: '+str(name)+' '
    line3=' Molare Masse: '+str(molar_mass)+' g/mol '
    line4=' Resultat: '+str(round(result, 4))+' '+'/'.join(target_unit)+' '
    print(''.center(80, '█'))
    print(''.center(15, '█')+line1.center(50)+''.center(15, '█'))
    print(''.center(80, '█'))
    if molar_mass != 'UNKNOWN':
        print(''.center(15, '█')+line2.center(50)+''.center(15, '█'))
        print(''.center(15, '█')+line3.center(50)+''.center(15, '█'))
        print(''.center(80, '█'))
    print(''.center(15, '█')+line4.center(50)+''.center(15, '█'))
    print(''.center(80, '█'))
    if substance_names != '':
        print(''.center(15, '█')+' Mögliche Stoffbezeichnungen: '.center(50)+''.center(15, '█'))
        for name in substance_names:
            if len(name) < 50:
                print(''.center(15, '█')+name.center(50)+''.center(15, '█'))
        print(''.center(80, '█'))
    input()

choose_menu()
