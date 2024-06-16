/*
 * Filename: windowManager.js
 * Path: Scripts\js\WindowManager\js
 * Created Date: Sunday, June 16th 2024, 5:15:39 pm
 * Author: Apop85
 * 
 * Copyright under MIT License by Apop85
 */

currentId = "";

// Funktion, um den Schatten eines Fensters ein- oder auszuschalten
function toggleShadow(id, turnOn=true) {
    if (id == "") return;
    element = document.getElementById(id);
    reorderWindows(id);

    if (window.getSelection().type != "Range") {
        if (turnOn || element.style.boxShadow == "") {
            element.style.boxShadow = "2px 2px 2px rgba(25,25,25,.5)";
            element.style.opacity = ".92";
        } else {
            element.style.boxShadow = "";
            element.style.opacity = "";
            currentId = "";
        }
    }
}


// Funktion, um die Reihenfolge der Fenster neu zu sortieren, damit immer dasd zuletzt angeklickte Fenster im Vordergrund ist. 
function reorderWindows(id) {
    elements = document.getElementsByClassName("window");
    windowElement = document.getElementById(id)

    if (windowElement.style.zIndex != 10000) {
        for (let index = 0; index < elements.length; index++) {
            const element = elements[index];

            delta = 10000 - element.style.zIndex;

            if (element.id != id) {
                element.style.zIndex = 10000 - (1 + delta);
            }
        }
        windowElement.style.zIndex = 10000;
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