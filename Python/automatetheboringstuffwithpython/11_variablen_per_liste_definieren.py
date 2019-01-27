liste=['dick', 'rot', 'böbbi']
woliste=[]

size,color,name=liste

print('Meine Katze namens', name,'ist',size,'und das Fell ist',color)

for i in range(2):
    print('Ein Nomen bitte:')
    woliste=woliste + [input()]
    
for i in range(2):
    print('Ein Adjektiv bitte:')
    woliste=woliste + [input()]

print('Ein', woliste[0], 'besuchte das Meer und er fand es', woliste[2], 'Das', woliste[1], 'war jedoch', woliste[3])
