() => {
  const password = document.getElementById("password").value;
  const passwordArray = Array.from(password).map(
      character => 0xCafe + character.charCodeAt(0)
  );

  if (passwordArray[0] === 52037 &&
    passwordArray[1] === 52077 &&
    passwordArray[2] === 52077 &&
    passwordArray[3] === 52066 &&
    passwordArray[4] === 52046 &&
    passwordArray[5] === 52063 &&
    passwordArray[6] === 52081 &&
    passwordArray[7] === 52081 &&
    passwordArray[8] === 52085 &&
    passwordArray[9] === 52077 &&
    passwordArray[10] === 52080 &&
    passwordArray[11] === 52066) {
    window.location.replace(v + ".html");
  } else {
    alert("Wrong password!");
  }
}