/*
 * Filename: windowManager.js
 * Path: Scripts\js\WindowManager\js
 * Created Date: Sunday, June 16th 2024, 5:15:39 pm
 * Author: Apop85
 * 
 * Copyright under MIT License by Apop85
 */

currentId = "";
isMoving = false;
minimizedWindow = []

// Funktion, um den Schatten eines Fensters ein- oder auszuschalten
function toggleShadow(id, turnOn=true) {
    if (id == "") return;
    element = document.getElementById(id);
    header = element.getElementsByClassName("windowHeader")[0]
    reorderWindows(id);

    if (window.getSelection().type != "Range") {
        if (turnOn || element.style.boxShadow == "") {
            element.style.boxShadow = "2px 2px 2px rgba(25,25,25,.5)";
            element.style.opacity = ".92";
            header.style.backgroundColor = "rgb(57, 57, 57)"
            isMoving = true;
        } else {
            element.style.boxShadow = "";
            element.style.opacity = "";
            currentId = "";
            header.style.backgroundColor = "";
            isMoving = false;
        }
    }
}


// Funktion, um die Reihenfolge der Fenster neu zu sortieren, damit immer dasd zuletzt angeklickte Fenster im Vordergrund ist. 
function reorderWindows(id) {
    elements = document.getElementsByClassName("window");
    windowElement = document.getElementById(id)
    orderArray = new Object();

    if (windowElement.style.zIndex != 10000) {
        for (let index = 0; index < elements.length; index++) {
            const element = elements[index];
            if (element.id != id) {
                orderArray[element.style.zIndex] = element;
            }
        }
        windowElement.style.zIndex = 10000;
        oaLength = objectLength(orderArray);
        // console.log(orderArray);

        counter = 0;
        for (key in orderArray) {
            currentElement = orderArray[key];
            newZIndex = 10000 - oaLength + counter;
            currentElement.style.zIndex = newZIndex;
            counter += 1;
        }
    }
}


// Funktion, um die Eventlistener auf die Fenster anzuwenden. 
// Quelle: @adeneo https://stackoverflow.com/questions/24050738/javascript-how-to-dynamically-move-div-by-clicking-and-dragging
function applyWindowManager(element) {
    var mousePosition;
    var offset = [0,0];
    var div;
    var isDown = false;

    header = element.getElementsByClassName("windowHeader");

    // Event bei Klick
    element.addEventListener('mousedown', function(e) {
        if (e.target.classList[0] != "windowContent") {
            document.getElementsByTagName("body")[0].style.userSelect = "none";
        }
        currentId = element.id;

        isDown = true;
        offset = [
            element.offsetLeft - e.clientX,
            element.offsetTop - e.clientY
        ];
    }, true);

    // Event bei Ende des Klicks
    document.addEventListener('mouseup', function() {
        isDown = false;
        document.getElementsByTagName("body")[0].style.userSelect = "";
        toggleShadow(currentId, false)
    }, true);

    // Event, während die Maus bewegt wird
    document.addEventListener('mousemove', function(event) {
        child = element.getElementsByClassName("windowContent")[0];
        selectionStyle = document.getElementsByTagName("body")[0].style.userSelect;

        // Verhindern Bewegung, wenn Text markiert wird
        selection = window.getSelection();
        if (selectionStyle != "none") {
            if (selection.type == "Range") return;
            if (child == event.target) return;
        } else {
            toggleShadow(currentId);
        }

        // Verschieben Element zu aktueller Mausposition
        event.preventDefault();
        if (isDown) {
            mousePosition = {

                x : event.clientX,
                y : event.clientY

            };
            element.style.left = (mousePosition.x + offset[0]) + 'px';
            element.style.top  = (mousePosition.y + offset[1]) + 'px';
        }
    }, true);
}

// Auslesen der Anzahl keys in einem Objekt
function objectLength(obj) {
    counter = 0
    for (key in obj) {
        counter += 1;
    }
    return counter;
}


function minimizeWindow(id) {
    console.log("minimize: " + id)
    var currentWindow = document.getElementById(id);

    windowHeaderHeight = currentWindow.getElementsByClassName("windowHeader")[0].offsetHeight;
    currentWindow.style.maxHeight = windowHeaderHeight + "px";
    currentWindow.style.maxWidth = "200px";

    symbol = currentWindow.getElementsByClassName("minimizeButton")[0];
    symbol.innerHTML = "🔲";
    symbol.onclick = function () {maximizeWindow(id)};
}

function maximizeWindow(id) {
    var currentWindow = document.getElementById(id);
    currentWindow.style.maxHeight = "";
    currentWindow.style.maxWidth = "";

    symbol = currentWindow.getElementsByClassName("minimizeButton")[0];
    symbol.innerHTML = "➖";
    symbol.onclick = function () {minimizeWindow(id)};
}

function closeWindow(id) {
    if (isMoving == false) {
        var currentWindow = document.getElementById(id);
        currentWindow.remove();
    }
}

function createNewWindow() {
    windowWidth = window.innerWidth;
    windowHeight = window.innerHeight;
    
    newWindow = document.createElement("div");

    counter = 1;
    while (true) {
        checkValue = JSON.stringify(document.getElementById("window" + counter));
        if (counter >= 10000 || checkValue == "null") {
            break;
        }
        counter += 1
    }

    randTop = Math.floor(Math.random() * (windowHeight - 150));
    randLeft = Math.floor(Math.random() * (windowWidth - 400));

    newWindow.classList.add("window");
    newWindow.style.top = randTop + "px";
    newWindow.style.left = randLeft + "px";
    // newWindow.style.top = "50px";
    // newWindow.style.left = "50px";
    newWindow.style.zIndex = "1";
    newWindow.id = "window" + counter;

    windowHeader = document.createElement("div");
    windowHeader.classList.add("windowHeader");
    windowHeader.innerHTML = "Fenstertitel (Window " + counter + ")";
    newWindow.appendChild(windowHeader);

    minimizeButton = document.createElement("span");
    minimizeButton.innerHTML = "➖";
    minimizeButton.classList.add("minimizeButton");
    minimizeButton.classList.add("windowButton");
    // minimizeButton.onclick = function() {minimizeWindow("window" + (counter + 1))};
    minimizeButton.setAttribute("onclick", "minimizeWindow('window" + counter + "')")
    windowHeader.appendChild(minimizeButton);

    closeButton = document.createElement("span");
    closeButton.innerHTML = "❌";
    closeButton.classList.add("closeButton");
    closeButton.classList.add("windowButton");
    // closeButton.onclick = function() {closeWindow("window" + (counter + 1))};
    closeButton.setAttribute("onclick", "closeWindow('window" + counter + "')")
    windowHeader.appendChild(closeButton);

    windowContent = document.createElement("div");
    windowContent.classList.add("windowContent");
    windowContent.innerHTML = "Lorem ipsum dolor sit amet consectetur adipisicing elit. Consequatur quis doloribus natus eos odio sequi culpa laborum praesentium? Expedita iusto sit velit amet, explicabo accusamus molestias cum dignissimos repudiandae et.";
    newWindow.appendChild(windowContent);
    
    document.getElementsByTagName("body")[0].appendChild(newWindow);

    // Anpassen der Elementgrösse des Textinhalts
    headerHeight = windowHeader.offsetHeight;
    elementHeight = newWindow.offsetHeight;
    delta = elementHeight - headerHeight - 12
    windowContent.style.height = delta + "px";

    reorderWindows("window" + counter);
    applyWindowManager(newWindow);
}

// Initialisierung
elements = document.getElementsByClassName("window");
// Iteriere durch alle Elemente mit der Klasse "window"
for (let index = 0; index < elements.length; index++) {
    const element = elements[index];
    const header = element.getElementsByClassName("windowHeader")[0]
    
    // Anpassen der Elementgrösse des Textinhalts
    headerHeight = header.offsetHeight;
    elementHeight = element.offsetHeight;
    delta = elementHeight - headerHeight - 12
    content = element.getElementsByClassName("windowContent")[0];
    content.style.height = delta + "px";

    // Events auf alle Elemente mit der Klasse "window" anwenden
    applyWindowManager(element);
}