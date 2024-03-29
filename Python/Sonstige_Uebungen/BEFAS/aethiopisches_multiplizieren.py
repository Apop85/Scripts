# Aethiopisches Multiplizieren von Integern

def input_function():
	try:
		source_num=int(input('Bitte Ausgangszahl angeben: '))
		multiplicate_with=int(input('Multiplikator angeben: '))
		return source_num,multiplicate_with
	except:
		print('Eingabe Ungültig')

def step_1(num1, num2):
	result_list=[(num1,num2)]
	while num1 != 1:
		print(num1,num2)
		num1//=2
		num2*=2
		result_list+=[(num1,num2)]
	return result_list

def step_2(result_list):
	new_list=[]
	for entry in result_list:
		if not entry[0]%2 == 0:
			new_list+=[entry]
	for entry in new_list:
		print(entry[0],entry[1])
	return new_list
	
def step_3(result_list):
	result=0
	for entry in result_list:
		result+=entry[1]
	return result
			
source_num,multiplicate_with=input_function()
print(' Step 1 '.center(50,'█'))
result_list=step_1(source_num,multiplicate_with)
print(' Step 2 '.center(50,'█'))
result_list=step_2(result_list)
print(' Step 3 '.center(50,'█'))
print(step_3(result_list))