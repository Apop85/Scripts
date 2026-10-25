from base64 import b64encode
from base64 import b64decode
import random

# Define random seed 
randSeed = "MySeeD"

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
random.seed(randSeed)

print(f"Seed:".ljust(25) + randSeed)
print(f"Secret:".ljust(25) + rawSecret)

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
print("2D Alphabet Array used for de- and encryption:")
print(newList)
conversionMatrix = newList

# Function to encode the secret message with the conversion matrix
def encryptData(secretMessage):
    encrypted = ""
    for letter in secretMessage:
        for primary in conversionMatrix.keys():
            for secondary in conversionMatrix[primary].keys():
                if conversionMatrix[primary][secondary] == letter:
                    encrypted += f"{primary}{secondary}"

    # Split string to create private and public Key
    secretKey = encrypted[:len(encrypted)//2]
    privateKey = encrypted[len(encrypted)//2:]

    # Check if decryption is possible
    decryptDataToText(encrypted)

    # Convert private and public key to bits
    secretKey = str(bin(int(secretKey))).replace("0b", "")
    privateKey = str(bin(int(privateKey))).replace("0b", "")

    # Fill missing bits to equalize length
    if len(secretKey) != len(privateKey):
        while len(secretKey) > len(privateKey):
            privateKey = "0" + privateKey
        while len(privateKey) > len(secretKey):
            secretKey = "0" + secretKey

    # XOR bitwise
    publicKey = ""
    for i in range(0, len(privateKey)):
        if privateKey[i] == secretKey[i]:
            publicKey += "0"
        else:
            publicKey += "1"

    # Reverse bits to integer
    secretKey = int(secretKey, 2)
    privateKey = int(privateKey, 2)
    publicKey = int(bin(int(publicKey, 2)), 2)

    # Encode keys with base64
    number_bytes = publicKey.to_bytes((publicKey.bit_length() + 7) // 8, byteorder="big")
    publicEncoded = b64encode(number_bytes)
    number_bytes = secretKey.to_bytes((secretKey.bit_length() + 7) // 8, byteorder="big")
    secretEncoded = b64encode(number_bytes)
    number_bytes = privateKey.to_bytes((privateKey.bit_length() + 7) // 8, byteorder="big")
    privateEncoded = b64encode(number_bytes)
    
    # Print needed Informations
    print(f"secretKey:".ljust(25) + str(secretEncoded)[2:-1])
    print(f"privateKey:".ljust(25) + str(privateEncoded)[2:-1])
    print(f"publicKey:".ljust(25) + str(publicEncoded)[2:-1])

    # Print human readable Serial key
    print("Serial:".ljust(25), end="")
    for i in range(0, len(str(privateEncoded)[2:-1]), 4):
        print(str(privateEncoded)[2:-1][i:i+4], end=" ")
    print()

    return publicEncoded, privateEncoded, secretEncoded

# Function to check if decryption is possible
def decryptData(publicEncoded, privateEncoded):
    publicEncoded = int.from_bytes(b64decode(publicEncoded), "big")
    privateEncoded = int.from_bytes(b64decode(privateEncoded), "big")
    # Translate key to bits
    privateEncoded = str(bin(privateEncoded)).replace("0b", "")
    publicEncoded = str(bin(publicEncoded)).replace("0b", "")

    # Fill missing bits to equalize publicEncoded length
    if len(publicEncoded) != len(privateEncoded):
        while len(publicEncoded) > len(privateEncoded):
            privateEncoded = "0" + privateEncoded
        while len(privateEncoded) > len(publicEncoded):
            publicEncoded = "0" + publicEncoded

    # XOR bitwise
    privateKey = ""
    for i in range(0, len(privateEncoded)):
        if privateEncoded[i] == publicEncoded[i]:
            privateKey += "0"
        else:
            privateKey += "1"

    # Convert bits to integer
    privateKey = int(privateKey, 2)

    number_bytes = privateKey.to_bytes((privateKey.bit_length() + 7) // 8, byteorder="big")
    privateKeyEncoded = b64encode(number_bytes)

    # Print decrypted key
    print(f"Decryption:".ljust(25) +  str(privateKeyEncoded)[2:-1], end="   ")

    return privateKeyEncoded

# Function to caluclate factors // NOT USED
def createSerial(secretNumber):
    maximum = 999999999
    minimum = 100000
    print("Serial".ljust(25) ,end="")
    secretNumber = int(secretNumber)
    swap = False
    abort = False
    serial = []
    # for i in range(2, 100):
    #     print(secretNumber**(1/i))
    while len(str(secretNumber)) > 9 and not abort:
        if swap:
            swap = False
            for i in range(minimum, maximum):
                if secretNumber % i == 0:
                    key = i
                    print(key, end="-")
                    serial += [str(key)]
                    secretNumber = secretNumber / key
                    break
                if i == maximum-1:
                    abort = True
        else:
            swap = True
            for i in range(maximum,minimum,-1):
                if secretNumber % i == 0:
                    key = i
                    print(key, end="-")
                    serial += [str(key)]
                    secretNumber = secretNumber / key
                    break
                if i == minimum+1:
                    abort = True
    serial += [str(int(secretNumber))]
    print(secretNumber)
    return serial

# Function to decrypt a given coordinate-array to text
def decryptDataToText(data):
    decrypted = ""
    for i in range(0, len(data), 2):
        firstLevel = data[i]
        secondLevel = data[i+1]
        decrypted += conversionMatrix[int(firstLevel)][int(secondLevel)]
    print(f"Reconstruction:".ljust(25) + decrypted, end="   ")
    if decrypted == rawSecret:
        print("Successfull")
    else:
        print("FAILED. DO NOT USE FOLLOWING KEYS!")


if __name__ == "__main__":
    publicEncoded, privateEncoded, secretEncoded = encryptData(rawSecret)
    if secretEncoded == decryptData(publicEncoded, privateEncoded):
        print("Successfull")
    else:
        print("Failed")

