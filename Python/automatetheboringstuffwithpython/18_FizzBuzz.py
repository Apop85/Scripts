##write a program that prints every number from 1-100
##but if the number is a multiple of 3 it writes fizz,
##if its a multiple of 5, it writes buzz, and
##if its a multiple of 3 and 5 it writes FizzBuzz

for i in range(1, 101):
    if i % 3 == 0 and i % 5 == 0:
        print('FizzBuzz', end=' ')
    elif i % 3 == 0:
        print('Fizz', end=' ')
    elif i % 5 == 0:
        print('Buzz', end=' ')
    else:
        print(i, end=' ')
