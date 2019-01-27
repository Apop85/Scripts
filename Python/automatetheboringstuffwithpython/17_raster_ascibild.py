grid=[['.']*6,
      ['.']+2*['@']+3*['.'],
      []+4*['@']+2*['.'],
      []+5*['@']+['.'],
      [',']+5*['@'],
      []+5*['@']+['.'],
      []+4*['@']+2*['.'],
      ['.']+2*['@']+3*['.'],
      ['.']*6]

counter=[0, 0]

for i in range(len(grid)*len(grid[1])):
    print(grid[counter[0]][counter[1]], end='')
    counter[0]+=1
    if counter[0] == len(grid):
        counter[1]+= 1
        counter[0] = 0
        print('')
        
