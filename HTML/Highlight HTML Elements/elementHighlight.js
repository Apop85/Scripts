// Ersetzen "__JAHR__" im Footer mit aktuellem Jahr
element = document.getElementById("copyright");
jahr = new Date().getFullYear();
element.innerHTML = element.innerHTML.replace("__JAHR__", jahr, )

// Funktion, um Listenelemente zu highlighten
function highlightLists(menuClass) {
    highlightMenuElements(menuClass);
    highlightElement('li', 'greenyellow');
    highlightElement('ul', 'green'); 
}

// Funktion, um Titel zu highlighten
function highlightTitles(menuClass) {
    highlightMenuElements(menuClass);
    highlightElement('h1', 'red');
    highlightElement('h2', 'orange'); 
    highlightElement('h3', 'yellow'); 
}

// Funktion, um Tabellen zu highlighten
function highlightTable(menuClass) {
    highlightMenuElements(menuClass);
    highlightElement('tr', 'yellow');
    highlightElement('td', 'greenyellow'); 
    highlightElement('th', 'lime'); 
}

// Funktion, um die Menüelemente zu highlighten
function highlightMenuElements(className) {
    elements = document.getElementsByClassName(className);
    for (let index = 0; index < elements.length; index++) {
        currentElement = elements[index];
                            
        if (currentElement.classList.contains('highlighted')) {
            currentElement.classList.remove("highlighted");
        } else {
            currentElement.classList.add("highlighted");
        }
    }
}

// Funktion um HTML Elemente hervorzuheben
function highlightElement(tagName, color, menuClass) {
    highlightMenuElements(menuClass);
    elements = document.getElementsByTagName(tagName);
    for(let i = 0;i < elements.length; i++) {
        toggleHighlight(elements[i], color);
        if (tagName == 'img') {
            toggleImage(elements[i]);
        }
    }
}

// Funktion, um Bilder ein- und auszublenden
function toggleImage(image) {
    imageName = image.src.split("/")[image.src.split("/").length-1]
    if (imageName == "bild.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinbild.png";
    } else if (imageName == "bild2.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinbild2.png";
    } else if (imageName == "bild3.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinbild2.png";
    } else if (imageName == "bild4.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinbild2.png";
    } else if (imageName == "bild5.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinbild2.png";
    } else if (imageName == "logo.png") {
        image.style.width = image.offsetWidth + "px";
        image.style.height = image.offsetHeight + "px";
        image.src = "keinlogo.png";
    } else if (imageName == "keinbild.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/bild.png";
    } else if (imageName == "keinbild2.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/bild2.png";
    } else if (imageName == "keinbild3.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/bild3.png";
    } else if (imageName == "keinbild4.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/bild4.png";
    } else if (imageName == "keinbild5.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/bild5.png";
    } else if (imageName == "keinlogo.png") {
        image.style.width = "";
        image.style.height = "";
        image.src = "bilder/logo.png";
    }
}

// Funktion, um die Hervorhebung ein- und auszuschalten
function toggleHighlight(siteElement,color) {
    if (siteElement.style.backgroundColor == "") {
        siteElement.style.backgroundColor = color;
    } else {
        siteElement.style.backgroundColor = "";
    }
    
    if (siteElement.style.border == "") {
        siteElement.style.border = "3px solid black";
        siteElement.style.marginLeft = "-3px";
        siteElement.style.marginRight = "-3px";
    } else {
        siteElement.style.border = "";
        siteElement.style.marginLeft = "";
        siteElement.style.marginRight = "";
    }
}

// Funktion um das CSS-Stylesheet ein- und auszuschalten
function toggleCSS() {
    for ( i=0; i<document.styleSheets.length; i++) {
        if (document.styleSheets.item(i).disabled == true) {
            void(document.styleSheets.item(i).disabled=false);
        } else {
            void(document.styleSheets.item(i).disabled=true);
        }
    }
}

// Randomisiere Bilder
possibleImages = [
    "bilder/bild.png",
    "bilder/bild2.png",
    "bilder/bild3.png",
    "bilder/bild4.png",
    "bilder/bild5.png",
];

imageElements = document.getElementsByClassName("artikelBild");
for (let index = 0; index < imageElements.length; index++) {
    const element = imageElements[index];

    backgroundImage = possibleImages[Math.floor(Math.random()*possibleImages.length)];
    element.src = backgroundImage;
}