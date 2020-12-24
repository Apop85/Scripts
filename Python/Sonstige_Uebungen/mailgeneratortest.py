import random, names


def randomizer(firstname, lastname, mail=""):
    provider_list = ["outlook.de", "outlook.com", "hotmail.de", "hotmail.com", "gmail.com", "gmx.de", "gmx.net", "gmx.ch", "yahoo.com", "1und1.de", "hotmail.co.uk"]
    while len(mail) < 5:
        fname = random.choice([firstname, firstname[:random.randint(1,len(firstname)-1)], firstname[0], ""])
        lname = random.choice([lastname, lastname[:random.randint(1,len(lastname)-1)], lastname[0], ""])
        delim = "" if fname == "" or lname == "" else random.choice(["_", "-", ".", ""])
    
        numbers = "".join(str(random.randint(0,9)) for i in range(random.randint(0,3)))
        capitalize_names = True if random.randint(1,10) % 3 == 0 else False
        
        # Capitalize in one third of the cases
        if capitalize_names:
            fname = fname.capitalize()
            lname = lname.capitalize()
        else:
            fname = fname.lower()
            lname = lname.lower()

        
        do_leet = True if random.randint(1,7) % 7 == 0 else False
        
        # Leet the names in 1 out of 7
        if do_leet:
            leet_translations = {"i":"1","z":"2","e":"3","a":"4","s":"5","g":"6","t":"7","b":"8","o":"0"}
            for key in leet_translations.keys():
                fname = fname.replace(key, leet_translations[key])
                lname = lname.replace(key, leet_translations[key])
        if random.choice([True,False]):
            mail = fname+delim+lname+numbers 
        else:
            mail = lname+delim+fname+numbers

    return mail + "@" + random.choice(provider_list)

while True:
    randomized = randomizer(names.get_first_name(), names.get_last_name())
    print(randomized)