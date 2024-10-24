# Brüche addieren
# Author: Apop85

def check_input(f1,f2):
  if (len(f1), len(f2)) == (2,2):
    for i in range(2):
      if not (f1[i].isdecimal() and f2[i].isdecimal()):
        return False
      return True
  else:
    return False

def add_fractions(f1,f2):
  if f1[1] == f2[1]:
    result=[str(int(f1[0])+int(f2[0])),f2[1]]
  else:
    new_f1=[int(f1[0])*int(f2[1]), int(f1[1])*int(f2[1])]
    new_f2=[int(f2[0])*int(f1[1]), int(f2[1])*int(f1[1])]
    result=[str(int(new_f1[0])+int(new_f2[0])),str(new_f1[1])]
  print('/'.join(result), end=' ')
  reduced_result=reduce_fraction(result)
  if reduced_result != result and reduced_result != 0:
    print('or '+'/'.join(reduced_result))
  else:
    print()

def reduce_fraction(result):
  for i in range(int(result[0]),0,-1):
    if (int(result[1])%i,int(result[0])%i) == (0,0):
      new_result=[str(int(result[0])//i),str(int(result[1])//i)]
      return new_result
  return 0

while True:
  fraction_1=input('Enter first fraction (1/2, 1/3): ')
  fraction_1=fraction_1.split('/')
  fraction_2=input('Enter second fraction (1/2, 1/3): ')
  fraction_2=fraction_2.split('/')
  check=check_input(fraction_1,fraction_2)
  if check:
    add_fractions(fraction_1,fraction_2)
  else:
    print('Input is invalid')

