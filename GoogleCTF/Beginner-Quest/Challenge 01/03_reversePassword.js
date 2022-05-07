password = [52037,52077,52077,52066,52046,52063,52081,52081,52085,52077,52080,52066]
clearPass = ""
password.forEach(letter => {
    value = letter - 0xCafe;
    clearPass += String.fromCharCode(value) 
});
console.log(clearPass)