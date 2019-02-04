# Ein kleines Script dass anscheinend gerne bei Vorstellungsgesprächen abgefragt wird.
# 1. Es sollen alle Zahlen von 0 bis 100 ausgegeben werden
# 2. Ist die ausgegebene Zahl ein vielfaches von 3, schreibe Fizz
# 3. Ist die ausgegebene Zahl ein vielfaches von 5, schreibe Buzz
# 4. Trifft 2. und 3. zu, schreibe FizzBuzz

for i in range(1, 101):
    if i % 3 == 0 and i % 5 == 0:
        print('FizzBuzz', end=' ')
    elif i % 3 == 0:
        print('Fizz', end=' ')
    elif i % 5 == 0:
        print('Buzz', end=' ')
    else:
        print(i, end=' ')
