# Berechne molare Masse anhand chemischer Summenformel

periodensystem={ 'H' : 1.0079, 'He' : 4.0026,
                 'Li' : 6.941, 'Be' : 9.0122, 'B' : 10.811, 'C' : 12.011, 'N' : 14.007, 'O' : 15.999, 'F' : 18.988, 'Ne' : 20.180,
                 'Na' : 22.990, 'Mg' : 24.305, 'Al' : 26.982, 'Si' : 28.086, 'P' : 30.974, 'S' : 32.065, 'Cl' : 35.453, 'Ar' : 39.948,
                 'K' : 39.098, 'Ca' : 40.078, 'Sc' : 44.956, 'Ti' : 47.867, 'V' : 50.942, 'Cr' : 51.996, 'Mn' : 54.938, 'Fe' : 55.845, 'Co' : 58.933, 'Ni' : 58.693, 'Cu' : 63.546, 'Zn' : 65.38, 'Ga' : 69.723, 'Ge' : 72.64, 'As' : 74.922, 'Se' : 78.96, 'Br' : 79.904, 'Kr' : 83.798,
                 'Rb' : 85.468, 'Sr' : 87.62, 'Y' : 88.906, 'Zr' : 91.224, 'Nb' : 92.906, 'Mo' : 95.96, 'Tc' : 98.91, 'Ru' : 101.07, 'Rh' : 102.91, 'Pd' : 106.42, 'Ag' : 107.87, 'Cd' : 112.41, 'In' : 114.82, 'Sn' : 118.71, 'Sb' : 121.76, 'Te' : 127.60, 'I' : 126.90, 'Xe' : 131.29,
                 'Cs' : 132.91, 'Ba' : 137.33, 'Hf' : 178.49, 'Ta' : 180.95, 'W' : 183.84, 'Re' : 186.21, 'Os' : 190.23, 'Ir' : 192.22, 'Pt' : 195.08, 'Au' : 196.97, 'Hg' : 200.59, 'Tl' : 204.38, 'Pb' : 207.2, 'Bi' : 208.98, 'Po' : 209.98, 'At' : 210, 'Rn' : 222,
                 'Fr' : 223, 'Ra' : 226.03, 'Rf' : 261, 'Db' : 262, 'Sg' : 263, 'Bh' : 262, 'Hs' : 265, 'Mt' : 266, 'Ds' : 296, 'Rg' : 272, 'Cn' : 277, 'Nh' : 287, 'Fl' : 289, 'Mc' : 288, 'Lv' : 289, 'Ts' : 293, 'Og' : 294,
                 'La' : 138.91, 'Ce' : 140.12, 'Pr' : 140.91, 'Nd' : 144.24, 'Pm' : 146.90, 'Sm' : 146.90, 'Eu' : 151.96, 'Gd' : 157.25, 'Tb' : 158.93, 'Dy' : 162.50, 'Ho' : 164.93, 'Er' : 167.26, 'Tm' : 168.93, 'Yb' : 173.05, 'Lu' : 174.97,
                 'Ac' : 227, 'Th' : 232.04, 'Pa' : 231.04, 'U' : 238.03, 'Np' : 237.05, 'Pu' : 244.10, 'Am' : 243.10, 'Cm' : 247.10, 'Bk' : 247.10, 'Cf' : 251.10, 'Es' : 254.10, 'Fm' : 257.10, 'Md' : 258, 'No' : 259, 'Lr' : 260}

# Prüfe wie lange die entdeckte Zahl ist
def checknum(i, nummer):
    zahl=str(nummer)
    global z
    z=0
    # Prüfe auf Folgezahlen.
    for n in range(i+1, len(sformel)):
        # Falls Zahl gefunden, hänge an vorherige an
        if sformel[n].isdecimal(): 
            zahl+=str(sformel[n])
            z+=1
        else:
        # Beende Loop wenn keine Zahl folgt
            if not sformel[n].isdecimal(): 
                break
    return zahl

# Prüfe ob das Element vollständig ist
def checkelement(i, element, gew):
    # Wenn letzter Buchstabe in Liste
    if i == len(sformel)-1:
        gew+=float(periodensystem[element])
    # Wenn nächster Buchstabe gross
    elif i < len(sformel)-1 and sformel[i+1].isupper():
        gew+=float(periodensystem[element])
    return gew

print('Summenformel eingeben:')
sformel=input('Summenformel: ')
sformel=list(sformel)
i=-1
gew=0
try:
    while i != len(sformel)-1:
        i+=1
        # Wenn Grossbuchstabe
        if sformel[i].isupper():
            element=sformel[i]
            gew=checkelement(i, element, gew)
        # Wenn Buchstabe klein, hänge an vorherigen an
        elif sformel[i].islower():
            element+=sformel[i]
            gew=checkelement(i, element, gew)
        # Wenn Element eine Zahl
        elif sformel[i].isdecimal(): 
            num=str(checknum(i, sformel[i]))
            i+=z
            gew+=float(periodensystem[element])*float(num)
        else:
            print('Error')
            break

    print('Die molare Masse von', ''.join(sformel), 'ist', str(round(gew, 4)) + 'g/mol')
except NameError:
    print('Der Eintrag konnte nicht gefunden werden.')
