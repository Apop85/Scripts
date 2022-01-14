// Variabeldeklaration
var decodedUriData = null;
var element = null;
var currentFavorites = null;
var favorites = null;
var allowedMediaExtensions = [".mp4", ".ogg", ".mp3", ".jpg", ".jpeg", ".png", ".gif", ".m4a"];
var mediaTypes = [".mp4"];
var musicTypes = [".mp3", ".m4a", ".ogg"];
var imageTypes = [".jpg", ".jpeg", ".png", ".gif"];
var playListPrefix = "";
var mainContent = null;
var level1 = null;
var level2 = null;
var level3 = null;
var newestVersion = null;
var image = null;
var buttonNode = null;
var lastPlayed = null;
var wrapperNode = null;
var lastTitle = null;
var lastUrl = null;
var news = null;
var playlistName = null;
var localPlaylist = null;
var medialocation = null;
var currentIndex = null;
var currentUrl = null;
var mediaName = null;
var pastTimestamp = null;
var currentTimestamp = null;
var linkNode = null;
var node = null;
var subnode = null;
var searchTerm = null;
var searchResults = null;
var seasonList = null;

// Funktion zum auslesen von Inhalten von anderen URLs
function httpGet(theUrl) {
    let xmlhttp;
    
    if (window.XMLHttpRequest) { // code for IE7+, Firefox, Chrome, Opera, Safari
        xmlhttp=new XMLHttpRequest();
    } else { // code for IE6, IE5
        xmlhttp=new ActiveXObject("Microsoft.XMLHTTP");
    }
    
    xmlhttp.onreadystatechange=function() {
        if (xmlhttp.readyState==4 && xmlhttp.status==200) {
            return xmlhttp.responseText;
        }
    }
    xmlhttp.open("GET", theUrl, false);
    xmlhttp.send();
    
    if (xmlhttp.status == 200) {
        return xmlhttp.response;
    } else {
        return null
    }
}

// Funktion zum Prüfen, ob ein Dateiname einer Videodatei entspricht
function isVideo(filename) {
    for (index in mediaTypes) {
        if (filename.includes(mediaTypes[index])) {
            return true;
        }
    }
    return false;
}

// Funktion zum Prüfen, ob ein Dateiname einer Bilddatei entspricht
function isImage(filename) {
    for (index in imageTypes) {
        if (filename.includes(imageTypes[index])) {
            return true;
        }
    }
    return false;
}

// Funktion zum Prüfen, ob ein Dateiname einer Musikdatei entspricht
function isMusic(filename) {
    for (index in musicTypes) {
        if (filename.includes(musicTypes[index])) {
            return true;
        }
    }
    return false;
}

// Sortiere UL-Objekt alphabetisch
function sortList(ul){
    // Variabeldeklaration
    var list = []
    var table = {}
    var text = ""
    var counter = 0
    var playlist = []
    var new_ul = ul.cloneNode(false);

    // Prüfe alle Kindelemente
    for (var key in ul.childNodes) {
        // Prüfe, ob ausgewähltes Element ein Listenelement ist
        if (ul.childNodes[key].localName == "li"){
            playlist.push(ul.childNodes[key].firstChild.href)
            // Extrahiere Text
            text = ul.childNodes[key].textContent;
            // Füge Schlüsselwert zu Liste hinzu und entferne Emotes
            list.push(text.replace("⭐ ", "").replace(" 🎬", "").replace(" 🎵", "").replace(" 📷", ""));
            table[text.replace("⭐ ", "").replace(" 🎬", "").replace(" 🎵", "").replace(" 📷", "")] = ul.childNodes[counter];
            counter += 1
        }
    }

    // Sortiere Liste
    list = list.sort()
    if (list.includes("Aufklappen")) {
        var hochElement = list.indexOf("Aufklappen");
        if (hochElement != 0) {
            list.splice(hochElement, 1)
            list.unshift("Aufklappen")
        }
    }
    
    // Erstelle Sortierte UL-Liste
    for (var i = 0; i < list.length; i++) {
        new_ul.appendChild(table[list[i]]);
    }

    // Ersetze unsortiertes UL-Element mit sortiertem
    ul.parentNode.replaceChild(new_ul, ul);
    return playlist
}

// Funktion zum hinzufügen von Emotes zu Dateinamen
function addEmoteByFileExtension(filename) {
    var newFilename = filename;
    // Füge Emote anhand Dateiendung hinzu
    if (isVideo(filename)) {
        newFilename = newFilename + " 🎬";
    } else if (isMusic(filename)) {
        newFilename = newFilename + " 🎵";
    } else if (isImage(filename)){
        newFilename = newFilename + " 📷";
    }

    return newFilename;
}

// Funktion zur prüfung, ob eine Datei im Browser abspielbar ist
function isMediaFile(whitelist, filename) {
    for (var item in whitelist) {
        if (filename.includes(whitelist[item])) {
            return true;
        }
    }
    return false;
}

// Funktion zum Entfernen der Dateierweiterungen
function removeFileExtension(extensionList, filename) {
    var newFilename = filename;
    for (extension in extensionList) {
        newFilename = newFilename.replaceAll(extensionList[extension], "")
    }

    return newFilename
}

// HTML Spezialcharakter in String ersetzen
function replaceSpecialChars(string) {
    return string.replaceAll("%20", " ").replaceAll("%C3%B6", "ö").replaceAll("%C3%BC", "ü").replaceAll("%C3%A4", "ä")
}

// Hinzufügen eines Vorschaubildes, falls vorhanden
function addPreviewImage(data, link, cleanedKey) {
    if (cleanedKey.startsWith(".")) {
        cleanedKey = cleanedKey.replace(".", "")
    }
    // Prüfe, ob Preview-Ordner vorhanden ist
    if (data.hasOwnProperty("Preview")){
        // Iteriere über alle Vorschaubilder
        for (previewImageSrc in data["Preview"]) {
            // Prüfe ob ein Bild mit identischem Namen wie das Medienfile vorhanden ist
            if (previewImageSrc.includes(removeFileExtension(allowedMediaExtensions, cleanedKey.split("/")[cleanedKey.split("/").length-1]) + ".jpg")) {
                if (!previewImageSrc.startsWith(".")) {
                    previewImageSrc = "." + previewImageSrc
                }
                
                // Erstelle Bild-Node
                var imgNode = document.createElement("img");
                imgNode.src = previewImageSrc;
                imgNode.style.height = "auto";
                imgNode.style.width = "30%";
                imgNode.style.boxShadow = "0px 0px 5px 2px white";
                imgNode.style.marginBottom = "10px";
                imgNode.id = removeFileExtension(allowedMediaExtensions, cleanedKey.split("/")[cleanedKey.split("/").length-1]);
                link.style.flexDirection = "column";
                link.style.justifyContent = "center";
                // Füge Bild zu Link-Node hinzu
                link.appendChild(imgNode);
                // Breche Loop ab
                return previewImageSrc;
            }
        }
    }
}

// Funktion zum Updaten der Liste der zuletzt gesehenen Medienelementen
function setLastPlayed(title, url) {
    var currentPlaylist = localStorage.getItem("playlast");
    if (currentPlaylist != null) {
        currentPlaylist = currentPlaylist.split(",");
    }
    if (currentPlaylist == null || currentPlaylist.includes("")) {
        currentPlaylist = [];
    }
    

    var mediaPath = atob(url.split("?=")[1].split("#")[0]).split(",MEDIA:")[1].split(",")[0].split("/");
    var filename = mediaPath[mediaPath.length - 1];
    if (!isImage(filename)) {
        mediaPath.splice(mediaPath.length - 1, 1);
        mediaPath.splice(mediaPath.length - 1, 1);
        mediaPath = mediaPath.join("/");
        
        for (var loggedUrl in currentPlaylist) {
            if (currentPlaylist[loggedUrl].includes("|")) {
                var loggedMediaPath = atob(currentPlaylist[loggedUrl].split("|")[1].split("?=")[1].split("#")[0]).split(",MEDIA:")[1].split(",")[0];
            } else {
                currentPlaylist.splice(loggedUrl, 1)
            }
            
            // Entferne Eintrag aus "Zuletzt gesehen"
            if (loggedMediaPath.includes(mediaPath)) {
                currentPlaylist.splice(loggedUrl, 1)
            }
        }
    
        if (currentPlaylist.length < 5) {
            currentPlaylist.splice(0, 0, title + "|" + url);
        } else {
            currentPlaylist.splice(currentPlaylist.length - 1, 1);
            currentPlaylist.splice(0, 0, title + "|" + url);
        }
        localStorage.setItem("playlast", currentPlaylist);
    }
}

// Funktion zum setzen der Höhe des body-Elements
function setHeight() {
    document.getElementById("mainBody").style.minHeight = document.documentElement.scrollHeight + "px";
}

// Funktion zum öffnen und schliessen der Untermenüs
function toggleSubmenu(id, currentNodeId) {
    // Ein/Ausblenden des Menüs
    if (document.getElementById(id).style.display == "none") {
        document.getElementById(id).style.display = "flex";
    } else {
        document.getElementById(id).style.display = "none";
    }

    document.getElementById(currentNodeId).scrollIntoView();

    // Ein/Ausblenden des Buttons
    if (document.getElementById(currentNodeId).childNodes[0].innerHTML == "Aufklappen") {
        document.getElementById(currentNodeId).childNodes[0].innerHTML = "Zuklappen";
    } else {
        document.getElementById(currentNodeId).childNodes[0].innerHTML = "Aufklappen";
    }
}

// Einfügen des Buttons zum öffnen und schliessen der Untermenüs
function createHochButton(upperId, currentId) {
    node = document.createElement("LI");
    link = document.createElement("a");
    textnode = document.createTextNode("Aufklappen");
    link.appendChild(textnode);
    node.appendChild(link);
    node.id = upperId+currentId;
    link.setAttribute("onclick", "toggleSubmenu('" + upperId + "', '" + node.id + "')");
    node.style.width = "100%";
    node.style.display = "block";
    node.className = "UP";
    document.getElementById(currentId).appendChild(node);
}

// Funktion zur Anzeige des Suchfelds
function toggleSearchField(){
    var node = document.getElementById("searchFieldContainer");
    if (node.style.display == "none") {
        node.style.display = "flex";
        document.getElementById("searchField").focus();
    } else {
        node.style.display = "none";
    }
}

// Funktion zum Aufruf der Suchfunktion
function callSearch() {
    var searchTerm = document.getElementById("searchField").value;
    window.location.href = "start.html?=" + btoa("SEARCH:" + searchTerm);
}

// Funktion zum erstellen des Links für die Suchresultate
function createLink(key, coreUrl, depth) {
    var result = null;
    if (!key.startsWith(".")) {
        key = "." + key
    }
    if (!isMediaFile(allowedMediaExtensions, key)) {
        result = key + "|" + "start.html?=" + btoa(coreUrl + ",LEVEL" + depth + ":" + key);
    } else {
        if (isVideo(key) || isMusic(key)) {
            result = key + "|" + "start.html?=" + btoa(coreUrl + ",MEDIA:" + key + ",PL:searchResults") + "#mediaNav";
            // VORSCHAUBILD
        } else if (isImage(key)) {
            result = key + "|" + "start.html?=" + btoa(coreUrl + ",MEDIA:" + key + ",PL:searchResults") + "#mediaNav";
        }
    }

    return result;
}

// Funktion zum rekursiven Durchsuchen des Daten-Arrays
function recursiveSearch(data, searchTerm, currentLink="MAIN:", searchResults=[], depth=0) {
    for (var key in data) {
        if (depth != 0) {
            var link = currentLink + ",LEVEL" + depth + ":" + key;
            // Prüfe, ob Suchbegriff in aktuellem Schlüssel vorkommt
            if (key.toLowerCase().includes(searchTerm.toLowerCase()) && key != "Preview") {
                // Erstelle Eintrag in Resultatliste
                searchResults.push(createLink(key, link, depth));
            }
            // Prüfe, ob weitere, tiefer liegende Daten vorhanden sind
            if (data.hasOwnProperty(key) && key != "Preview") {
                searchResults = recursiveSearch(data[key], searchTerm, link, searchResults, depth+1);
            }
        } else {
            searchResults = recursiveSearch(data[key], searchTerm, currentLink + key, searchResults, depth+1);
        }
    }
    return searchResults;
}

function openHelp() {
    window.location.href = "start.html?=" + btoa("HELP");
}

// Lade Favoriten
favorites = localStorage.getItem("favorites");
if (favorites != null) {
    favorites = favorites.split(",");
}

// Falls vorhanden, decodiere Datenstring
if (window.location.href.includes("?=")) {
    decodedUriData = atob(window.location.href.split("?=")[1].split("#")[0]);
}

// Iteriere über alle Schlüsselelemente des data-Array
for (var key in data) {
    if (data.hasOwnProperty(key)) {
        // Erstelle Listenelement für jeden Schlüssel
        node = document.createElement("LI");
        link = document.createElement("a");
        textnode = document.createTextNode(key);
        link.appendChild(textnode);
        link.href = "start.html?=" + btoa("MAIN:" + key);
        node.appendChild(link);
        node.id = key;
        document.getElementById("menu").appendChild(node);
        
    }
}
    // Sortiere Liste
sortList(document.getElementsByClassName('menu')[0]);

// Prüfe, ob aus dem Hauptmenü eine Auswahl getroffen wurde
if (decodedUriData != null && decodedUriData.includes("MAIN:")) {
    // Zerlege URL in "MAIN"-Bestandteil
    mainContent = replaceSpecialChars(decodedUriData.split("MAIN:")[1].split(",")[0].split("#")[0]);
    playListPrefix += mainContent;
    // Lege leere Playliste an
    playlist = []

    // Iteriere durch alle Elemente des gewählten Schlüssels
    for (var key in data[mainContent]) {
        if (data[mainContent].hasOwnProperty(key) && key != "Preview") {
            node = document.createElement("LI");
            link = document.createElement("a")
            // Entferne HTML-Code aus Schlüsselname
            cleanedKey = replaceSpecialChars(key);
            if (!cleanedKey.startsWith(".")) {
                cleanedKey = "." + cleanedKey;
            }
            var mainContentPreviewImage = addPreviewImage(data[mainContent], link, cleanedKey);

            // Prüfe, ob der aktuelle Schlüssel ein Medientyp ist
            if (isMediaFile(allowedMediaExtensions, key)) {
                text = key.split("/");
                // Füge Favoritenstern hinzu, wenn Mediendatei in Favoritenliste vorhanden ist.
                if ((favorites != null && favorites.includes(cleanedKey))) {
                    text = "⭐ " + text[text.length - 1];
                } else {
                    text = text[text.length - 1];
                }
                
                // Füge Emote anhand Dateiendung hinzu
                text = addEmoteByFileExtension(text);
                if (!key.startsWith(".")) {
                    key = "." + key
                }
                // Füge alle Elemente eines Schlüssel der Playlist zu
                playlist.push(key);

                // Erstelle Link
                link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",MEDIA:" + key + ",PL:" + playListPrefix) + "#mediaNav";
                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, text));
            } else {
                link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + key);
                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, key));
            }
            link.appendChild(textnode);
            node.appendChild(link);
            node.id = cleanedKey.replace(".", "");
            document.getElementById("submenu").appendChild(node);
        }
    }
    document.getElementById(mainContent).className += " activeMenu";
    // Sortiere Liste
    sortList(document.getElementsByClassName('submenu')[0]);
    document.getElementById("submenu").style.borderTop = "2px solid #666";

    // Prüfe, ob eine "LEVEL1"-Auswahl getroffen wurde
    if (decodedUriData.includes("LEVEL1")) {
        document.getElementById("submenu").style.display = "none";
        // Zerlege URL in "LEVEL1"-Bestandteil
        level1 = replaceSpecialChars(decodedUriData.split(",LEVEL1:")[1].split(",")[0].split("#")[0]);
        playListPrefix += level1;
        // Erstelle Leere Playliste
        playlist = []

        createHochButton("submenu", "subsubmenu");
	
        // Iteriere über jeden Schlüsselwert
        for (var key in data[mainContent][level1]) {
            if (data[mainContent][level1].hasOwnProperty(key) && key != "Preview") {
                node = document.createElement("LI");
                link = document.createElement("a");
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);
                if (!cleanedKey.startsWith(".")) {
                    cleanedKey = "." + cleanedKey;
                }

                // Füge Vorschaubild hinzu
                addPreviewImage(data[mainContent][level1], link, cleanedKey);

                text = key.split("/");
                
                // Füge Favoritenstern hinzu, wenn Mediendatei in Favoritenliste vorhanden ist.
                if ((favorites != null && favorites.includes(cleanedKey))){
                    text = "⭐ " + text[text.length - 1].replace(level1, "");
                } else {
                    text = text[text.length - 1].replace(level1, "");
                }
                
                // Füge Emote anhand Dateiendung hinzu
                text = addEmoteByFileExtension(text);

                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, text));
                link.appendChild(textnode);
                text = addEmoteByFileExtension(text);
                
                // Prüfe, ob der aktuelle Schlüssel ein Medientyp ist
                if (isMediaFile(allowedMediaExtensions, key)) {
                    if (!key.startsWith(".")) {
                        key = "." + key
                    }
                    // Füge alle Medienelemente eines Schlüssel der Playlist zu
                    playlist.push(key);

                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",MEDIA:" + key + ",PL:" + playListPrefix) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + key);
                }
                node.appendChild(link);
                node.id = cleanedKey.replace(".", "");
                document.getElementById("subsubmenu").appendChild(node);
            }
        }
        document.getElementById(level1).className += " activeMenu";

        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubmenu')[0]);
        document.getElementById("subsubmenu").style.borderTop = "2px solid #666";
    }

    // Prüfe, ob eine "LEVEL2"-Auswahl gefällt wurde
    if (decodedUriData.includes("LEVEL2")) {
        document.getElementById("subsubmenu").style.display = "none";

        // Zerlege URL in "LEVEL2"-Bestandteil
        level2 = replaceSpecialChars(decodedUriData.split(",LEVEL2:")[1].split(",")[0].split("#")[0]);
        playListPrefix += level2;
        // Erstelle Leere Playliste
        playlist = []

        createHochButton("subsubmenu", "subsubsubmenu");

        for (var key in data[mainContent][level1][level2]) {
            if (data[mainContent][level1][level2].hasOwnProperty(key) && key != "Preview") {
                node = document.createElement("LI");
                link = document.createElement("a")
                text = key.split("/")
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);
                if (!cleanedKey.startsWith(".")) {
                    cleanedKey = "." + cleanedKey;
                }

                // Füge Vorschaubild hinzu
                addPreviewImage(data[mainContent][level1][level2], link, cleanedKey);

                // Füge Favoritenstern hinzu, wenn Mediendatei in Favoritenliste vorhanden ist.
                if (favorites != null && favorites.includes(cleanedKey) && isMediaFile(allowedMediaExtensions, key)){
                    text = "⭐ " + text[text.length - 1].replace(level1, "").replace(level2, "");
                } else {
                    text = text[text.length - 1].replace(level1, "").replace(level2, "");
                }
                
                // Füge Emote anhand Dateiendung hinzu
                text = addEmoteByFileExtension(text);

                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, text));
                link.appendChild(textnode);
                
                // Prüfe, ob der aktuelle Schlüssel ein Medientyp ist
                if (isMediaFile(allowedMediaExtensions, key)) {
                    if (!key.startsWith(".")) {
                        key = "." + key
                    }

                    // Füge alle Elemente eines Schlüssel der Playlist zu
                    playlist.push(key);

                    // Erstelle Link
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",MEDIA:" + key + ",PL:" + playListPrefix) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + key);
                }
                node.appendChild(link);
                node.id = cleanedKey.replace(".", "");
                document.getElementById("subsubsubmenu").appendChild(node);
            }
        }
        document.getElementById(level2).className += " activeMenu";

        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubsubmenu')[0]);
        document.getElementById("subsubsubmenu").style.borderTop = "2px solid #666";
    }

    // Prüfe, ob eine "LEVEL3"-Auswahl getroffen wurde
    if (decodedUriData.includes("LEVEL3")) {
        document.getElementById("subsubsubmenu").style.display = "none";

        // Zerlege URL in "LEVEL3"-Bestandteil
        level3 = replaceSpecialChars(decodedUriData.split(",LEVEL3:")[1].split(",")[0].split("#")[0]);
        playListPrefix += level3;
        // Erstelle Leere Playliste
        playlist = []

        createHochButton("subsubsubmenu", "subsubsubsubmenu");

        for (var key in data[mainContent][level1][level2][level3]) {
            if (data[mainContent][level1][level2][level3].hasOwnProperty(key) && key != "Preview") {
                node = document.createElement("LI");
                link = document.createElement("a")
                text = key.split("/")
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);
                if (!cleanedKey.startsWith(".")) {
                    cleanedKey = "." + cleanedKey;
                }

                // Füge Vorschaubild hinzu
                addPreviewImage(data[mainContent][level1][level2][level3], link, cleanedKey);

                // Füge Favoritenstern hinzu, wenn Mediendatei in Favoritenliste vorhanden ist.
                if ((favorites != null && favorites.includes(cleanedKey) && isMediaFile(allowedMediaExtensions, key))){
                    text = "⭐ " + text[text.length - 1].replace(level1, "").replace(level2, "").replace(level3, "");
                } else {
                    text = text[text.length - 1].replace(level1, "").replace(level2, "").replace(level3, "");
                }
                
                // Füge Emote anhand Dateiendung hinzu
                text = addEmoteByFileExtension(text);

                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, text));
                link.appendChild(textnode);
                
                // Prüfe, ob der aktuelle Schlüssel ein Medientyp ist
                if (isMediaFile(allowedMediaExtensions, key)) {
                    if (!key.startsWith(".")) {
                        key = "." + key
                    }
                    // Füge alle Elemente eines Schlüssel der Playlist zu
                    playlist.push(key);

                    // Erstelle Medienlink
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + level3 + ",MEDIA:" + key + ",PL:" + playListPrefix) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + level3 + ",LEVEL4:" + key);
                }
                node.appendChild(link);
                node.id = cleanedKey.replace(".", "");
                document.getElementById("subsubsubsubmenu").appendChild(node);
            }
        }
        document.getElementById(level3).className += " activeMenu";

        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubsubsubmenu')[0]);
        document.getElementById("subsubsubsubmenu").style.borderTop = "2px solid #666";
    }

    // So lange kein Medientyp ausgewählt wurde, Playliste überschreiben
    // if (!decodedUriData.includes("MEDIA")) {
    localStorage.setItem("currentPrefix", playListPrefix);
    localStorage.setItem(playListPrefix, playlist);
    // }
} else if (window.location.href.includes("?=") && decodedUriData.includes("SEARCH:")) {
    // Lese Suchtext aus
    searchTerm = decodedUriData.split("SEARCH:")[1];
    // searchResults = searchForTerm();
    searchResults = recursiveSearch(data, searchTerm)
    localStorage.setItem("searchResults", searchResults)
    wrapperNode = document.createElement("div");
    subnode = document.createElement("p");
    textnode = document.createTextNode("Total Suchresultate: " + searchResults.length);
    subnode.style.padding = "20px";
    subnode.style.color = "white";
    subnode.appendChild(textnode);
    wrapperNode.appendChild(subnode);

    for (var lastUrlIndex in searchResults) {
        // Lese Titel und URL aus
        lastTitle = searchResults[lastUrlIndex].split("|")[0].split("/");
        lastTitle = lastTitle[lastTitle.length - 1];
        lastUrl = searchResults[lastUrlIndex].split("|")[1];

        node = document.getElementById("media");
        
        // Erstelle Button
        buttonNode = document.createElement("a");
        buttonNode.className = "button";
        buttonNode.href = lastUrl;

        // Decodiere übergebene URL
        var decodedUrl = atob(lastUrl.split("?=")[1].split("#")[0]);
        // Lese die Hauptkategorie aus
        mainContent = replaceSpecialChars(decodedUrl.split("MAIN:")[1].split(",")[0].split("#")[0]);
        if (decodedUrl.includes("LEVEL1")){
            // Lese die level1-Eintrag aus
            level1 = replaceSpecialChars(decodedUrl.split(",LEVEL1:")[1].split(",")[0].split("#")[0]);
            // Füge Vorschaubild in Button ein
            addPreviewImage(data[mainContent], buttonNode, level1);
            // Styles für Button ändern
            buttonNode.style.display = "flex";
            buttonNode.style.flexDirection = "row";
            buttonNode.style.alignItems = "center";
            buttonNode.style.justifyContent = "start";
            buttonNode.style.maxHeight = "150px";
            buttonNode.style.textAlign = "left";
        }
        // Erstelle Buttontext
        textNode = document.createTextNode(removeFileExtension(allowedMediaExtensions, lastTitle));
        // Füge Button hinzu
        buttonNode.appendChild(textNode);
        wrapperNode.appendChild(buttonNode);
    }
    // Füge Button-Styles hinzu
    wrapperNode.style.display = "flex";
    wrapperNode.style.flexDirection = "column";
    wrapperNode.style.width = "50vw";
    wrapperNode.style.padding = "20px";
    wrapperNode.style.backgroundColor = "black";
    wrapperNode.style.marginLeft = "auto";
    wrapperNode.style.marginRight = "auto";
    wrapperNode.id = "lastPlayedWrapper";
    node.appendChild(wrapperNode);
} else {
    // Lese neuste Version von GIT-Repository aus
    newestVersion = httpGet("https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/version.js");
    if (newestVersion != null) {
        newestVersion = parseFloat(newestVersion.split("var version = ")[1]);
    }

    if (!window.location.href.includes(btoa("HELP"))) {
        // Prüfe, ob Version aktuell ist
        if (newestVersion != null && newestVersion > version) {
            // Erstelle Hilfe-Link
            node = document.getElementById("media");
            wrapperNode = document.createElement("div"); 
            // Erstelle Update-Button
            buttonNode = document.createElement("a");
            buttonNode.className = "button";
            buttonNode.href = "start.html?=" + btoa("HELP");
            textNode = document.createTextNode("STEFFLIX UPDATE VERFÜGBAR");
            buttonNode.appendChild(textNode);
            wrapperNode.appendChild(buttonNode);

            // Füge Button-Styles hinzu
            wrapperNode.style.display = "flex";
            wrapperNode.style.flexDirection = "column";
            wrapperNode.style.width = "50vw";
            wrapperNode.style.marginLeft = "auto";
            wrapperNode.style.marginRight = "auto";
            wrapperNode.style.padding = "20px";
            wrapperNode.style.marginBottom = "20px";
            wrapperNode.style.backgroundColor = "yellowgreen";
            wrapperNode.style.id = "helpBox";

            node.appendChild(wrapperNode);
        }


        // Wenn keine Auswahl getroffen wurde, Splashscreen anzeigen
        node = document.getElementById("splashscreen");
        node.style.display = "block";
        image = document.createElement("img");

        image.src = "sflix_sys/sflix.png";
        node.appendChild(image);

        lastPlayed = localStorage.getItem("playlast");
        if (lastPlayed != null) {
            lastPlayed = lastPlayed.split(",");
        } else {
            lastPlayed = [];
        }

        // Erstelle Liste von kürzlich gespielten Inhalten
        if (lastPlayed != null && !lastPlayed.includes("")) {
            wrapperNode = document.createElement("div");

            for (var lastUrlIndex in lastPlayed) {
                // Lese Titel und URL aus
                lastTitle = lastPlayed[lastUrlIndex].split("|")[0];
                lastUrl = lastPlayed[lastUrlIndex].split("|")[1];
                node = document.getElementById("media");
                
                // Erstelle Button
                buttonNode = document.createElement("a");
                buttonNode.className = "button";
                buttonNode.href = lastUrl;

                // Decodiere übergebene URL
                var decodedUrl = atob(lastUrl.split("?=")[1].split("#")[0]);
                // Lese die Hauptkategorie aus
                mainContent = replaceSpecialChars(decodedUrl.split("MAIN:")[1].split(",")[0].split("#")[0]);
                if (decodedUrl.includes("LEVEL1")){
                    // Lese die level1-Eintrag aus
                    level1 = replaceSpecialChars(decodedUrl.split(",LEVEL1:")[1].split(",")[0].split("#")[0]);
                    // Füge Vorschaubild in Button ein
                    addPreviewImage(data[mainContent], buttonNode, level1);
                    // Styles für Button ändern
                    buttonNode.style.display = "flex";
                    buttonNode.style.flexDirection = "row";
                    buttonNode.style.alignItems = "center";
                    buttonNode.style.justifyContent = "start";
                    buttonNode.style.maxHeight = "150px";
                    buttonNode.style.textAlign = "left";
                }
                // Erstelle Buttontext
                textNode = document.createTextNode(removeFileExtension(allowedMediaExtensions, lastTitle) + " fortsetzen");
                // Füge Button hinzu
                buttonNode.appendChild(textNode);
                wrapperNode.appendChild(buttonNode);
            }
            // Füge Button-Styles hinzu
            wrapperNode.style.display = "flex";
            wrapperNode.style.flexDirection = "column";
            wrapperNode.style.width = "50vw";
            wrapperNode.style.padding = "20px";
            wrapperNode.style.backgroundColor = "black";
            wrapperNode.style.marginLeft = "auto";
            wrapperNode.style.marginRight = "auto";
            wrapperNode.id = "lastPlayedWrapper";
            node.appendChild(wrapperNode);
        }
    
    } else {
        news = httpGet("https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/news.txt");

        // Zeige Hilfe an
        node = document.getElementById("help");
        node.style.display = "block";

        // Füge aktuelle Version ein
        node = document.getElementById("versionNow");
        textnode = document.createTextNode(version)
        node.appendChild(textnode);
        
        // Füge neuste Version ein
        node = document.getElementById("versionNew");
        textnode = document.createTextNode(newestVersion)
        node.appendChild(textnode);

        node = document.createElement("div");
        node.id = "changelog";
        // textnode = document.createTextNode(news);
        // node.appendChild(textnode);
        document.getElementById("help").appendChild(node);
        document.getElementById("changelog").innerHTML = news;
    }
}



// Prüfe, ob ein Medientyp ausgewählt wurde
if (decodedUriData != null && decodedUriData.includes("MEDIA:")) {
    if (decodedUriData.includes(",PL:")) {
        playlistName = decodedUriData.split(",PL:")[1].split(",")[0].split("#")[0];
    }
    // Lade aktuelle Playliste
    localPlaylist = localStorage.getItem(playlistName).split(",");
    // Lese Speicherort aus
    medialocation = replaceSpecialChars(decodedUriData.split("MEDIA:")[1].split(",")[0].split("#")[0]);
    // Lese Playlistenindex aus
    localPlaylist = localPlaylist.sort()
    document.getElementById(medialocation.replace(".","")).className += " activeMenu";

    var cleanedPlaylist = []
    for (var key in localPlaylist){
        cleanedPlaylist.push(localPlaylist[key].split("|")[0])
    }
    currentIndex = cleanedPlaylist.indexOf(medialocation);
    // Aktuelle Auswahl auslesen
    currentUrl = decodedUriData.split(",MEDIA:")[0];
    // Lese Dateinamen aus
    mediaName = medialocation.split("/");
    mediaName = mediaName[mediaName.length -1].split("#")[0];

    // Schreibe Medientitel
    textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, addEmoteByFileExtension(mediaName)));
    document.title = "STEFFLIX - " + removeFileExtension(allowedMediaExtensions, mediaName); 
    document.getElementById("mediaTitle").appendChild(textnode);
    document.getElementById("mediaTitle").style.borderBottom = "1px solid rgb(221, 221, 221)";
    
    // Prüfe, ob die Datei eine Video- oder Musik-Datei ist
    if (isVideo(medialocation) || isMusic(medialocation)) {
        // Erstelle Videoelement
        node = document.createElement("video");
        node.src = medialocation;
        node.id = "video";
        node.controls = true;
        // node.muted =true;
        // node.focus;

        document.getElementById("media").appendChild(node);

        node.onloadedmetadata = function() {
            node.autoplay = true;
            // Lese Zeitstempel des ausgewählten Videos aus
            pastTimestamp = localStorage.getItem("timestamp-" + mediaName)
            if (pastTimestamp != null) {
                // Prüfe Zeitstempel für Medien die länger als 10 Minuten sind
                if (this.duration >= 600) {
                    // Setze Player auf gespeicherten Zeitstempel
                    if (100 / this.duration * pastTimestamp < 95) {
                        // Setze Abspeilzeitpunkt auf Zeitstempel
                        document.getElementById("video").currentTime = pastTimestamp;
                    } else {
                        // Lösche Zeitstempel
                        localStorage.removeItem("timestamp-" + mediaName);
                    }
                } else {
                    // Lösche Zeitstempel
                    localStorage.removeItem("timestamp-" + mediaName);
                }
            }
        };

        // Lese aktuellen Zeitstempel im Video aus
        document.getElementById("video").addEventListener('timeupdate', function() {
            pastTimestamp = localStorage.getItem("timestamp-" + mediaName)
            currentTime = parseInt(this.currentTime, 10);
            currentTimestamp = localStorage.getItem("timestamp-" + mediaName);
            if (currentTimestamp == null) {
                currentTimestamp = 0;
            }
            // Speichere Videoposition alle 10 Sek
            if (currentTimestamp != currentTime && currentTime % 10 == 0) {
                localStorage.setItem("timestamp-" + mediaName, currentTime);
            }

            if (this.duration > 600) {
                if (100 / this.duration * pastTimestamp > 95) {
                    this.style.border = "1px solid red";
                } else {
                    this.style.border = "0px solid #222";
                }
            }
        });
    } else if (isImage(medialocation)) {
        // Erstelle Bildelement
        element = document.getElementById("media");
        node = document.createElement("img");
        node.src = medialocation;
        node.id = "imagenode" + Date.now();
        
        // Füge Link zu Bild hinzu
        linkNode = document.createElement("a");
        linkNode.href = medialocation.split("#")[0];
        linkNode.appendChild(node);
        document.getElementById("media").appendChild(linkNode);
    }

    setLastPlayed(mediaName, window.location.href)

    // Prüfe, ob der Merken-Button gedrückt wurde
    if (decodedUriData.includes(",FAV:True")) {
        // Prüfe, ob bereits favoriten vorhanden sind
        if (favorites != null) {
            // Lese Favoriten aus
            currentFavorites = localStorage.getItem("favorites").split(",");
            if (!currentFavorites.includes(medialocation)) {
                // Füge Favorit hinzu, falls noch nicht vorhanden
                currentFavorites.push(medialocation);
            } else {
                // Lösche Favorit, wenn bereits vorhanden.
                currentFavorites.splice(currentFavorites.indexOf(medialocation), 1);
            }
        } else {
            // Wenn noch keine Favoriten vorhanden sind, erstelle Array
            currentFavorites = [medialocation];
        }
        if (currentFavorites == []){
            // Entferne gespeicherte Favoriten bei leerem Array
            localStorage.removeItem("favorites")
        } else {
            // Speichere Favoriten lokal
            localStorage.setItem("favorites", currentFavorites);
        }
        // Überschreibe bisherige Favoritenliste
        favorites = currentFavorites;
    }
    
    // Sichtbarkeit der Navigationsleiste umstellen
    node = document.getElementById("mediaNav");
    node.style.display = "flex";

    
    // Prüfe, ob der letzte Index grösser oder gleich 0 ist
    if (currentIndex - 1 >= 0) {
        // Erstelle Zurück-Button
        node = document.getElementById("prev");
        subnode = document.createElement("a");
        if (!localPlaylist[currentIndex - 1].includes("start.html")) {
            mediaName = localPlaylist[currentIndex - 1].split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        } else {
            mediaName = localPlaylist[currentIndex - 1].split("|")[0];
            mediaName = mediaName.split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        }
        textnode = document.createTextNode("👈 " + mediaName);
        subnode.appendChild(textnode);
        // Link zu vorherigem Listenelement
        if (!localPlaylist[currentIndex - 1].includes("start.html?=")){
            subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex - 1] + ",PL:" + playlistName) + "#mediaNav";
        } else {
            subnode.href = localPlaylist[currentIndex - 1].split("|")[1];
        }
        node.appendChild(subnode);
    } else {
        // Letzte Staffel
        node = document.getElementById("prev");
        subnode = document.createElement("a");
        medialocation = null;
        currentUrl = null;


        // Lese den Medienpfad aus
        if (!localPlaylist[currentIndex].includes("start.html")) {
            mediaName = localPlaylist[currentIndex].split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        } else {
            mediaName = localPlaylist[currentIndex].split("|")[0];
            mediaName = mediaName.split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        }
        
        seasonList = [];
        mediaList = [];
        if (mainContent != null && level1 != null && level2 != null && level3 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent][level1][level2]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level3);
            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex - 1 > 0) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][level1][level2][seasonList[keyIndex - 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[0];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + level1 + level2 + seasonList[keyIndex - 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + seasonList[keyIndex + 1] + ",MEDIA:." + medialocation; + ",PL:" + newPlaylistName 
            }
        } else if (mainContent != null && level1 != null && level2 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent][level1]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level2);

            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex - 1 > 0) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][level1][seasonList[keyIndex - 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[mediaList.length - 1];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + level1 + seasonList[keyIndex - 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + seasonList[keyIndex - 1] + ",MEDIA:." + medialocation + ",PL:" + newPlaylistName;
            }
        } else if (mainContent != null && level1 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level1);
            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex - 1 > 0) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][seasonList[keyIndex - 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[0];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + seasonList[keyIndex - 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + seasonList[keyIndex - 1] + ",MEDIA:." + medialocation + ",PL:" + newPlaylistName;
            }
        }

        if (medialocation != null) {
            // Entferne Pfad und Dateierweiterung und setze als Titel
            textnode = document.createTextNode("👈 " + removeFileExtension(allowedMediaExtensions, medialocation.split("/")[medialocation.split("/").length - 1]))
            subnode.appendChild(textnode);
            subnode.href = "start.html?=" + btoa(currentUrl) + "#mediaNav";
            node.appendChild(subnode);
        }
    }

    // Erstelle Merken-Button
    node = document.getElementById("favorite")
    subnode = document.createElement("a");
    if (favorites == null || !favorites.includes(medialocation)) {
        textnode = document.createTextNode("Merken");
        node.className = "";
    } else {
        textnode = document.createTextNode("Merken ⭐");
        node.className = "isFavorite";
    }
    subnode.appendChild(textnode);
    subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex] + ",FAV:True" + ",PL:" + playlistName) + "#mediaNav";
    node.appendChild(subnode);

    // Prüfe ob nächster Index noch innerhalb der Range ist
    if (currentIndex + 1 <= localPlaylist.length - 1) {
        node = document.getElementById("next");
        subnode = document.createElement("a");
        if (!localPlaylist[currentIndex + 1].includes("start.html")) {
            mediaName = localPlaylist[currentIndex + 1].split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        } else {
            mediaName = localPlaylist[currentIndex + 1].split("|")[0];
            mediaName = mediaName.split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        }
        currentUrl = atob(window.location.href.split("?=")[1].split("#")[0]).split(",MEDIA:")[0]
        localPlaylist = localStorage.getItem(playlistName).split(",")

        textnode = document.createTextNode(mediaName + " 👉");
        subnode.appendChild(textnode);
        if (!localPlaylist[currentIndex + 1].includes("start.html?=")) {
            subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex + 1] + ",PL:" + playlistName) + "#mediaNav";
        } else {
            subnode.href = localPlaylist[currentIndex + 1].split("|")[1]
        }
        node.appendChild(subnode);
    } else {
        // Nächste Staffel
        node = document.getElementById("next");
        subnode = document.createElement("a");
        medialocation = null;
        currentUrl = null;

        // Lese den Medienpfad aus
        if (!localPlaylist[currentIndex].includes("start.html")) {
            mediaName = localPlaylist[currentIndex].split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        } else {
            mediaName = localPlaylist[currentIndex].split("|")[0];
            mediaName = mediaName.split("/");
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length - 1]);
        }

        seasonList = [];
        mediaList = [];
        if (mainContent != null && level1 != null && level2 != null && level3 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent][level1][level2]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level3);
            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex + 1 < seasonList.length) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][level1][level2][seasonList[keyIndex + 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[0];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + level1 + level2 + seasonList[keyIndex + 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + seasonList[keyIndex + 1] + ",MEDIA:." + medialocation; + ",PL:" + newPlaylistName 
            }
        } else if (mainContent != null && level1 != null && level2 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent][level1]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level2);
            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex + 1 < seasonList.length) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][level1][seasonList[keyIndex + 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[0];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + level1 + seasonList[keyIndex + 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + seasonList[keyIndex + 1] + ",MEDIA:." + medialocation + ",PL:" + newPlaylistName;
            }
        } else if (mainContent != null && level1 != null) {
            // Erstelle Schlüsselliste
            for (var key in data[mainContent]) {
                seasonList.push(key);
                seasonList.sort();
            }
            // Lese akteuellen Index aus
            keyIndex = seasonList.indexOf(level1);
            // Prüfe, ob eine weitere Staffel vorhanden ist
            if (keyIndex != -1 && keyIndex + 1 < seasonList.length) {
                // Erstelle Liste aller Episoden
                for (var key in data[mainContent][seasonList[keyIndex + 1]]) {
                    mediaList.push(key);
                };
                // Lese erste Folge der nächsten Staffel aus
                medialocation = mediaList.sort()[0];
                // Definiere newPlaylistname
                newPlaylistName = mainContent + seasonList[keyIndex + 1];
                // Setze aktuelle URL zusammen
                currentUrl = "MAIN:" + mainContent + ",LEVEL1:" + seasonList[keyIndex + 1] + ",MEDIA:." + medialocation + ",PL:" + newPlaylistName;
            }
        }

        if (medialocation != null) {
            // Entferne Pfad und Dateierweiterung und setze als Titel
            textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, medialocation.split("/")[medialocation.split("/").length - 1])  + " 👉")
            subnode.appendChild(textnode);
            subnode.href = "start.html?=" + btoa(currentUrl) + "#mediaNav";
            node.appendChild(subnode);
        }
    }
} else {
    // Platzhalterbild für Leere Seiten erstellen
    document.getElementById("mainBody").style.backgroundImage = "url(sflix_sys/sflix_bg.jpg)";
}
