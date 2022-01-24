from base64 import b64encode
import random

# Define random seed 
randSeed = "SEED"
random.seed(randSeed)

# Define secret message 
botToken = "123456789:RaNdOmChArAcTeRs"
chatId = "12345678"
rawSecret = botToken + "$=$" + chatId

# Define alphabet
alnumlist=[]
alphabet=r"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-=: ,./1234567890_%$?()*+!;[]&~{}üöäÖÄÜ}"
alnumlist += alphabet

newList = {}
index = 0

# Sort alphabet randomly into 2D array
while len(alnumlist) > 0:
    newList.setdefault(index, {})
    secondIndex = 0
    while secondIndex <= 9:
        if len(alnumlist) > 0:
            randomInt = random.randint(0, len(alnumlist)-1)
            newList[index].setdefault(secondIndex, alnumlist[randomInt])
            secondIndex += 1
            del alnumlist[randomInt]
        else:
            break
    index += 1

# Output array to decrypt data
print(newList)
conversionMatrix = newList

# Function to encode the secret message with the conversion matrix
def encodeData(secret):
    encrypted = ""
    for letter in secret:
        for primary in conversionMatrix.keys():
            for secondary in conversionMatrix[primary].keys():
                if conversionMatrix[primary][secondary] == letter:
                    encrypted += f"{primary}{secondary}"

    # Split string to create private and public Key
    publicKey = encrypted[:len(encrypted)//2]
    privateKey = encrypted[len(encrypted)//2:]

    # Check if decryption is possible
    decryptDataToText(encrypted)

    # Convert private and public key to bits
    publicKey = str(bin(int(publicKey))).replace("0b", "")
    privateKey = str(bin(int(privateKey))).replace("0b", "")

    # Fill missing bits to equalize length
    if len(publicKey) != len(privateKey):
        while len(publicKey) > len(privateKey):
            privateKey = "0" + privateKey
        while len(privateKey) > len(publicKey):
            publicKey = "0" + publicKey

    # XOR bitwise
    secret = ""
    for i in range(0, len(privateKey)):
        if privateKey[i] == publicKey[i]:
            secret += "0"
        else:
            secret += "1"

    # Reverse bits to integer
    publicKey = int(publicKey, 2)
    privateKey = int(privateKey, 2)
    secret = int(bin(int(secret, 2)), 2)

    # Encode keys with base64
    number_bytes = secret.to_bytes((secret.bit_length() + 7) // 8, byteorder="big")
    secretEncoded = b64encode(number_bytes)
    number_bytes = publicKey.to_bytes((publicKey.bit_length() + 7) // 8, byteorder="big")
    publicEncoded = b64encode(number_bytes)
    number_bytes = privateKey.to_bytes((privateKey.bit_length() + 7) // 8, byteorder="big")
    privateEncoded = b64encode(number_bytes)
    
    # Print needed Informations
    # print(f"publicKey:  {publicKey}")
    print(f"publicKey:  {publicEncoded}")
    # print(f"privateKey: {privateKey}")
    print(f"privateKey: {privateEncoded}")
    # print(f"secret:     {secret}")
    print(f"secret:     {secretEncoded}")

    # Print human readable Serial key
    print("Serial:     ", end="")
    for i in range(0, len(str(privateEncoded)[2:-1]), 4):
        if i == 0:
            print(str(privateEncoded)[2:-1][i:i+4], end="")
        else:
            print("-" + str(privateEncoded)[2:-1][i:i+4], end="")
    print()

    return publicKey, secret, privateKey

# Function to check if decryption is possible
def decryptData(key, secret):
    # Translate keys to bits
    secret = str(bin(secret)).replace("0b", "")
    key = str(bin(key)).replace("0b", "")

    # Fill missing bits to equalize key length
    if len(key) != len(secret):
        while len(key) > len(secret):
            secret = "0" + secret
        while len(secret) > len(key):
            key = "0" + key

    # XOR bitwise
    privateKey = ""
    for i in range(0, len(secret)):
        if secret[i] == key[i]:
            privateKey += "0"
        else:
            privateKey += "1"

    # Convert bits to integer
    privateKey = int(privateKey, 2)

    # Print decrypted key
    print(f"decrypted:  {privateKey}")

    return privateKey

# Function to caluclate factors // NOT USED
def createSerial(secret):
    maximum = 999999999
    minimum = 100000
    print("Serial:     ",end="")
    secret = int(secret)
    swap = False
    abort = False
    serial = []
    # for i in range(2, 100):
    #     print(secret**(1/i))
    while len(str(secret)) > 9 and not abort:
        if swap:
            swap = False
            for i in range(minimum, maximum):
                if secret % i == 0:
                    key = i
                    print(key, end="-")
                    serial += [str(key)]
                    secret = secret / key
                    break
                if i == maximum-1:
                    abort = True
        else:
            swap = True
            for i in range(maximum,minimum,-1):
                if secret % i == 0:
                    key = i
                    print(key, end="-")
                    serial += [str(key)]
                    secret = secret / key
                    break
                if i == minimum+1:
                    abort = True
    serial += [str(int(secret))]
    print(secret)
    return serial

# Function to decrypt a given coordinate-array to text
def decryptDataToText(data):
    decrypted = ""
    for i in range(0, len(data), 2):
        firstLevel = data[i]
        secondLevel = data[i+1]
        decrypted += conversionMatrix[int(firstLevel)][int(secondLevel)]
    print(decrypted)


if __name__ == "__main__":
    publicKey, secret, privateKey = encodeData(rawSecret)
    decryptData(publicKey, secret)
