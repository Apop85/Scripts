liste=[ 'Rasenmäher', 'Spachtelmasse', 'Zwieback',
        'Hundehalsband', 'Teeblätter', 'Schrauben',
        'Rollator', 'Ersatzreifen', 'Discman' ]

maxl, string = 0, ''

for wort in liste:
    if maxl < len(wort):
        maxl=len(wort)
maxl+=3

for wort in liste:
    string+=wort.rjust(maxl)
    if len(string) == maxl*3:
        print(string)
        string=''
