list1=['.']*6
list2=['.']+2*['@']+3*['.']
list3=4*['@']+2*['.']
list4=5*['@']+['.']
list5=[',']+5*['@']

grid=[list1, list2, list3,
      list4, list5, list4,
      list3, list2, list1]

counter=[0, 0]

for i in range(len(grid)*len(grid[1])):
    print(grid[counter[0]][counter[1]], end='')
    counter[0]+=1
    if counter[0] == len(grid):
        counter[1]+= 1
        counter[0] = 0
        print('')
        
