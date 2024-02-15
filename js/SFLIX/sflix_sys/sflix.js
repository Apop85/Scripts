// Variabeldeklaration
var allowedMediaExtensions = [".mp4", ".ogg", ".mp3", ".jpg", ".jpeg", ".png", ".gif", ".m4a"];
var mediaTypes = [".mp4"];
var musicTypes = [".mp3", ".m4a", ".ogg"];
var imageTypes = [".jpg", ".jpeg", ".png", ".gif"];
var listSeperator = "$=$"
var decodedUriData = null;
var element = null;
var currentFavorites = null;
var favorites = null;
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
var timeoutArray = [];
var gravity = 1;
var maxY = 0;
var minTime = 11;

settings = localStorage.getItem("settings");
if (settings == null) {
    autoplay = false;
    autoplayAmount = 0;
    autoplayDuration = 0;
    localStorage.setItem("settings", [autoplay, autoplayAmount, autoplayDuration].join(listSeperator));
} else {
    settings = settings.split(listSeperator);
    autoplay = settings[0];
    if (autoplay == "false") {
        autoplay = false;
    } else {
        autoplay = true;
    }
    autoplayAmount = parseInt(settings[1]);
    autoplayDuration = parseInt(settings[2]);
    updateOptionFields(autoplay, autoplayAmount, autoplayDuration);
}

//  _______  __   __  __    _  ___   _  _______  ___   _______  __    _  _______  __    _ 
// |       ||  | |  ||  |  | ||   | | ||       ||   | |       ||  |  | ||       ||  |  | |
// |    ___||  | |  ||   |_| ||   |_| ||_     _||   | |   _   ||   |_| ||    ___||   |_| |
// |   |___ |  |_|  ||       ||      _|  |   |  |   | |  | |  ||       ||   |___ |       |
// |    ___||       ||  _    ||     |_   |   |  |   | |  |_|  ||  _    ||    ___||  _    |
// |   |    |       || | |   ||    _  |  |   |  |   | |       || | |   ||   |___ | | |   |
// |___|    |_______||_|  |__||___| |_|  |___|  |___| |_______||_|  |__||_______||_|  |__|                                                    

// Funktion, um die Untermenüs zu erstellen
function createSubmenus(decodedUriData, contentData, currentLink, depth=0, name="submenu", playListPrefix="") {
    if (decodedUriData.includes("LEVEL" + depth)) {
        node = document.createElement("ul");
        node.id = name;
        node.class = name;
        node.style.display = "none";
        document.getElementById("navigation").appendChild(node);

        // Zerlege URL in "LEVEL"-Bestandteil
        const level = replaceSpecialChars(decodedUriData.split("LEVEL" + depth + ":")[1].split(listSeperator)[0].split("#")[0]);
        try {
            nextLevel = replaceSpecialChars(decodedUriData.split("LEVEL" + (depth + 1) + ":")[1].split(listSeperator)[0].split("#")[0]);
        } catch (error) {
            nextLevel = null;
        }

        playListPrefix += level;
        // Erstelle Leere Playliste
        playlist = []

        if (depth > 0) {
            createHochButton(name.replace("sub", ""), name);
        }
	
        // Iteriere über jeden Schlüsselwert
        for (var key in contentData) {
            if (contentData.hasOwnProperty(key) && key != "Preview") {
                node = document.createElement("LI");
                link = document.createElement("a");
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);
                if (!cleanedKey.startsWith(".")) {
                    cleanedKey = "." + cleanedKey;
                }
                previewPath = decodedUriData;

                previewPath = previewPath.split("LEVEL").slice(1,depth+2).join("LEVEL");
                if (previewPath.slice(previewPath.length-3, previewPath.length) == listSeperator) {
                    previewPath = previewPath.slice(0, previewPath.length-3);
                }

                if (!previewPath.startsWith("LEVEL")) {
                    previewPath = "LEVEL" + previewPath;
                }

                if (!isMediaFile(allowedMediaExtensions, cleanedKey)) {
                    addPreviewImage(previewPath + listSeperator + "LEVEL" + (depth + 1) + ":" + cleanedKey, link);
                } else {
                    addPreviewImage(previewPath + listSeperator + "MEDIA:" + cleanedKey, link);
                }

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
                    link.href = "start.html?=" + btoa(currentLink + listSeperator + "MEDIA:" + key + listSeperator + "PL:" + playListPrefix) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa(currentLink + listSeperator + "LEVEL" + (depth + 1) + ":" + key);
                }
                node.appendChild(link);
                node.id = cleanedKey.replace(".", "");
                document.getElementById(name).appendChild(node);
            }
        }
        document.getElementById(level).className += " activeMenu";

        // Sortiere Liste
        sortList(document.getElementById(name));
        document.getElementById(name).style.borderTop = "2px solid #666";
        document.getElementById(name).style.display = "flex";

        if (nextLevel != null) {
            document.getElementById(name).style.display = "none";
            newCurrentLink = currentLink + listSeperator + "LEVEL" + (depth + 1) + ":" + nextLevel;
            createSubmenus(decodedUriData, contentData[nextLevel], newCurrentLink, depth+1, "sub" + name, playListPrefix)
        }
        
        localStorage.setItem("currentPrefix", playListPrefix);
        localStorage.setItem("playlist-" + playListPrefix, playlist.join(listSeperator));
    }
}

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
    return playlist.sort();
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

// Funktion um die Level aus der URL auszuelesen
function extractData(decodedUrl) {
    returnData = {"media": null, "levels": {}}
    // Zerlegen in Dateiname
    if (decodedUrl.includes("LEVEL")) {
        let counter = 0;
        while (decodedUrl.includes("LEVEL" + counter)) {
            level = counter;
            levelContent = decodedUrl.split("LEVEL" + counter + ":")[1].split(listSeperator)[0];
            returnData["levels"][level] = levelContent;
            counter += 1;
        }
    }
    if (decodedUrl.includes("MEDIA")) {
        media = decodedUrl.split("MEDIA:")[1].split("#")[0].split(listSeperator)[0];
        returnData["media"] = media;
    }

    return returnData;
}

// Funktion, um dasm Datenarray auf die Auswahl zu begrenzen
function reduceDataByUrl(currentUrl) {
    let counter = 0;
    returnData = data;
    while (currentUrl.includes("LEVEL" + counter)) {
        lvl = currentUrl.split("LEVEL" + counter + ":")[1].split(listSeperator)[0]
        returnData = returnData[lvl]
        
        counter += 1;
    }
    return returnData
}

// Hinzufügen eines Vorschaubildes, falls vorhanden
function addPreviewImage(decodedUrl, link, reason="normal") {
    
    var previewIsSet = false;
    var previewImageSrc = null;
    
    extractedData = extractData(decodedUrl);
    
    dataReader = data;
    if (extractedData["media"] == null || reason != "normal") {
        for (key in extractedData["levels"]) {
            keyValue = extractedData["levels"][key];
            if (keyValue.startsWith(".")) {
                keyValue = keyValue.replace(".", "");
            }
            
            if (dataReader.hasOwnProperty(keyValue)) {
                if (dataReader.hasOwnProperty("Preview")) {
                    mediaName = extractedData["media"];
                    imageFound = searchPreviewImage(dataReader["Preview"], [mediaName, keyValue], mediaName, reason);
                    if (imageFound != null) {
                        previewImageSrc = imageFound;
                    }
                }
            }
            dataReader = dataReader[keyValue]
        }
        if (previewImageSrc == null && reason != "normal") {
            dataReader = reduceDataByUrl(decodedUrl.split(listSeperator + "MEDIA")[0]);
            mediaName = extractedData["media"];
            mediaName = mediaName.split("/")[mediaName.split("/").length - 1];
            mediaName = removeFileExtension(allowedMediaExtensions, mediaName);
            if (dataReader.hasOwnProperty("Preview")) {
                previewImageSrc = searchPreviewImage(dataReader["Preview"], [mediaName], mediaName, reason);
            }
        }
    } else {
        dataReader = reduceDataByUrl(decodedUrl.split(listSeperator + "MEDIA")[0]);
        mediaName = extractedData["media"];
        mediaName = mediaName.split("/")[mediaName.split("/").length - 1];
        mediaName = removeFileExtension(allowedMediaExtensions, mediaName);
        previewImageSrc = searchPreviewImage(dataReader["Preview"], [mediaName], mediaName, reason);
    }

    if (previewImageSrc != null) {
        previewIsSet = true;
    }

    if (previewIsSet) {
        if (!previewImageSrc.startsWith(".")) {
            previewImageSrc = "." + previewImageSrc;
        }
        // Erstelle Bild-Node
        var imgNode = document.createElement("img");
        imgNode.src = previewImageSrc;
        imgNode.style.height = "auto";
        imgNode.style.width = "30%";
        imgNode.style.boxShadow = "0px 0px 5px 2px white";
        imgNode.style.marginBottom = "10px";
        imgNode.id = mediaName;
        imgNode.loading = "lazy";
        link.style.flexDirection = "column";
        link.style.justifyContent = "center";
        // Füge Bild zu Link-Node hinzu
        link.appendChild(imgNode);
        // Breche Loop ab
        return previewImageSrc;
    }

    return null
}

// Durchsuche Array nach möglichem Vorschaubild
function searchPreviewImage(data, searchKey, filename, type) {
    for (var key in searchKey) {
        if (searchKey[key] != null) {
            for (var datakey in data) {
                if (datakey.includes(searchKey[key])) {
                    if (filename == null || type != "normal") {
                        // Sofern die Vorschau für ein Ordner gesucht wird, jedes Resultat zurückgeben
                        return datakey;
                    } else if (filename.includes(searchKey)) {
                        // Bei Mediendateien nur zurückgeben, wenn der Dateiname übereinstimmt
                        return datakey;
                    }
                }
            }
        }
    }
    return null;
}

// Funktion zum Updaten der Liste der zuletzt gesehenen Medienelementen
function setLastPlayed(title, url) {
    var currentPlaylist = localStorage.getItem("playlast");
    if (currentPlaylist != null) {
        currentPlaylist = currentPlaylist.split(listSeperator);
    }
    if (currentPlaylist == null || currentPlaylist.includes("")) {
        currentPlaylist = [];
    }
    
    var mediaPath = atob(url.split("?=")[1].split("#")[0]).split(listSeperator + "MEDIA:")[1].split(listSeperator)[0].split("/");
    
    var filename = mediaPath[mediaPath.length - 1];
    if (!isImage(filename)) {
        while (mediaPath.length > 3) {
            mediaPath.splice(mediaPath.length - 1, 1);
        }
        mediaPath = mediaPath.join("/");
        
        for (var loggedUrl in currentPlaylist) {
            if (currentPlaylist[loggedUrl].includes("|")) {
                var loggedMediaPath = atob(currentPlaylist[loggedUrl].split("|")[1].split("?=")[1].split("#")[0]).split(listSeperator + "MEDIA:")[1].split(listSeperator)[0];
                // Entferne Eintrag aus "Zuletzt gesehen"
                if (loggedMediaPath.includes(mediaPath)) {
                    currentPlaylist.splice(loggedUrl, 1)
                }
            } else {
                currentPlaylist.splice(loggedUrl, 1)
            }
        }

    
        if (currentPlaylist.length < 5) {
            currentPlaylist.splice(0, 0, title + "|" + url);
        } else {
            currentPlaylist.splice(currentPlaylist.length - 1, 1);
            currentPlaylist.splice(0, 0, title + "|" + url);
        }
        localStorage.setItem("playlast", currentPlaylist.join(listSeperator));
    }
}

// Funktion zum setzen der Höhe des body-Elements
function setHeight() {
    document.getElementById("loadingScreen").style.display = "none";
    // document.getElementById("mainBody").style.minHeight = document.documentElement.scrollHeight + "px";
    // document.getElementById("mainBody").style.minHeight = "min-co";
    // gLoop()
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

// Funktion zum Aufruf der Suchfunktion
function callSearch() {
    var searchTerm = document.getElementById("searchField").value;
    window.location.href = "start.html?=" + btoa("SEARCH:" + searchTerm);
}

// Funktion zum erstellen des Links für die Suchresultate
function createLink(key, coreUrl) {
    var result = null;
    if (!key.startsWith(".")) {
        key = "." + key
    }
    result = key + "|start.html?=" + btoa(coreUrl + listSeperator + "PL:searchResults") + "#mediaNav";
    return result;
}

// Funktion zum rekursiven Durchsuchen des Daten-Arrays
function recursiveSearch(data, searchTerm, currentLink="LEVEL0:", searchResults=[], depth=0, link=null) {
    for (var key in data) {
        if (depth != 0) {
            if (key != "Preview") {
                if (!isMediaFile(allowedMediaExtensions, key)) {
                    link = currentLink + listSeperator + "LEVEL" + depth +":";
                } else {
                    if (!key.startsWith(".")){
                        key = "." + key;
                    }
                    link = currentLink + listSeperator + "MEDIA:" + key;
                    if (key.toLowerCase().includes(searchTerm.toLowerCase())) {
                        searchResults.push(createLink(key, link));
                    }
                }
                if (data.hasOwnProperty(key) && data[key] != null) {
                    searchResults = recursiveSearch(data[key], searchTerm, link + key, searchResults, depth+1);
                }
            }
        } else {
            searchResults = recursiveSearch(data[key], searchTerm, currentLink + key, searchResults, depth+1);
        }
    }
    return searchResults;
}

// Öffne Hilfeseite
function openHelp() {
    window.location.href = "start.html?=" + btoa("HELP");
}

// Hinzufügen/Entfernen von Favoriten
function updateFavorites() {
    // Aktuelles Medium auslesen
    mediaName = atob(window.location.href.split("?=")[1].split("#")[0]).split(listSeperator + "MEDIA:")[1].split(listSeperator)[0];
    
    // Favoriten auslesen
    favorites = localStorage.getItem("favorites");
    if (favorites == null) {
        favorites = [];
    } else {
        favorites = favorites.split(listSeperator);
        if (!Array.isArray(favorites)) {
            favorites = [favorites];
        }
    }

    // Leerer String aus Favoriten entfernen
    if (favorites.includes('')){
        currentIndex = favorites.indexOf("");
        favorites.splice(currentIndex, 1);
    }
    
    
    
    // Stern bei Button hinzufügen/entfernen
    node = document.getElementById("favorite").childNodes[0];
    text = node.innerHTML;
    if (text.includes("⭐")) {
        text = text.replaceAll(" ⭐","");
        node.innerHTML = text;
        document.getElementById("favorite").className = document.getElementById("favorite").className.split("isFavorite").join("");
    } else {
        if (!document.getElementById("favorite").className.includes("isFavorite")) {
            document.getElementById("favorite").className += "isFavorite";
        }
        node.innerHTML += (" ⭐");
    }

    // Punkt vor Verzeichnis entfernen
    if (!mediaName.startsWith(".")) {
        mediaName = "." + mediaName;
    }

    // Stern von Menü hinzufügen/Entfernen
    node = document.getElementById(mediaName.replace(".", "")).childNodes[0];
    text = node.innerHTML;

    // Prüfe, ob ein Vorschaubild enthalten ist
    if (node.innerHTML.includes("lazy")) {
        image = text.split(">")[0] + ">";
        text = text.split(">")[1];
    }
    
    // Stern hinzufügen/entfernen
    if (text.includes("⭐")) {
        text = text.replace("⭐ ", "");
    } else {
        text = "⭐ " + text;
    }

    // Element updaten
    if (node.innerHTML.includes("lazy")) {
        node.innerHTML = image + text;
    } else {
        node.innerHTML = text;
    }

    // Favoriten aktualisieren
    if (favorites.includes(mediaName)) {
        currentIndex = favorites.indexOf(mediaName);
        favorites.splice(currentIndex, 1);
    } else {
        favorites.push(mediaName);
    }
    
    // Speichere aktualisierte Favoritenliste
    localStorage.setItem("favorites", favorites.join(listSeperator));
}

// Funktion, um Container ein- und auszublenden
function toggleContainer(idArray, displayMode) {
    var nodeId = null;
    for (var key in idArray) {
        nodeId = idArray[key];
        node = document.getElementById(nodeId);
        if (node.style.display == "none") {
            node.style.display = displayMode;
            if (idArray[key] == "searchFieldContainer") {
                document.getElementById("searchField").focus();
            } else if (idArray[key] == "serialKeyWrapper") {
                license = localStorage.getItem("serial");
                if (license != null) {
                    serialIndex = 0
                    for (let index = 0; index < licenseFields.length; index++) {
                        document.getElementById(licenseFields[index]).value = license.slice(serialIndex,serialIndex+4);
                        serialIndex += 4;
                    }
                }
            }
            // Video-Interaktion einschränken (Chrome Bug Workaround)
            if (detectBrowser() == "chrome" || detectBrowser() == "edge") {
                document.getElementById("video").style.pointerEvents = "none";
            }
        } else {
            node.style.display = "none";
            // Video-Interaktion freigeben (Chrome Bug Workaround)
            if (detectBrowser() == "chrome" || detectBrowser() == "edge") {
                document.getElementById("video").style.pointerEvents = null;
            }
        }
    }
}

// Funnktion, um das nächste Medium aufzurufen
function playlistForwards(currentIndex, localPlaylist) {
    timeoutArray = cancelTimeouts()

    // Entferne Link-Node und ersetze mit leerem Link
    if (document.getElementById("next").childNodes.length > 0 && document.getElementById("next").childNodes[0].href != null) {
        document.getElementById("next").removeChild(document.getElementById("next").childNodes[0]);
    }
    document.getElementById("next").appendChild(document.createElement("a"));

    if (document.getElementById("prev").childNodes.length > 0 && document.getElementById("prev").childNodes[0].href != null) {
        document.getElementById("prev").removeChild(document.getElementById("prev").childNodes[0]);
    }
    document.getElementById("prev").appendChild(document.createElement("a"));


    localPlaylist = localPlaylist.split(listSeperator);
    if (currentIndex + 1 < localPlaylist.length) {
        mediaUrl = localPlaylist[currentIndex + 1];
        currentPlaylistName = atob(window.location.href.split("?=")[1].split("#")[0]).split(listSeperator + "PL:")[1].split(listSeperator)[0];
        if (currentPlaylistName == "searchResults") {
            mediaUrl = mediaUrl.split("|")[1];
            window.location.href = mediaUrl;
            return null;
        }

        if (!mediaUrl.startsWith(".")) {
            mediaUrl = "." + mediaUrl;
        }
        path = extractFromPath(mediaUrl);
        
        
        // Ändere Item zuvor als Aktiv
        if (!document.getElementById(mediaUrl.replace(".","")).className.includes("activeMenu")) {
            document.getElementById(mediaUrl.replace(".","")).className += "activeMenu";
        }
        mediaName = localPlaylist[currentIndex + 1].split("/");
        mediaName = mediaName[mediaName.length - 1];

        document.title = "STEFFLIX - " + removeFileExtension(allowedMediaExtensions, mediaName); 

        if (!isImage(mediaUrl)) {
            setLastPlayed(mediaName, "start.html?=" + btoa(path) + "#mediaNav");
        }
        history.pushState({}, null, "start.html?=" + btoa(path) + "#mediaNav");

        realLink = false;
        if (currentIndex + 2 < localPlaylist.length) {
            // Diese Staffel
            nextName = localPlaylist[currentIndex + 2].split("/")
            nextName = nextName[nextName.length - 1]
        } else {
            // Nächste Staffel
            nextName = null;
            url = swapSeason(1);
            if (url != null) {
                nextName = url.split("MEDIA:")[1].split(listSeperator)[0].split("/");
                nextName = nextName[nextName.length - 1]
                realLink = true;
            }
        }

        lastName = localPlaylist[currentIndex];
        if (!lastName.startsWith(".")) {
            lastName = "." + lastName;
        }

        // Deaktiviere aktuelles Menüelement
        document.getElementById(lastName.replace(".", "")).className = document.getElementById(lastName.replace(".", "")).className.split("activeMenu").join("");
        lastName = lastName.split("/");
        lastName = removeFileExtension(allowedMediaExtensions, lastName[lastName.length - 1]);


        node = document.getElementById("video");
        // Ändere Titel
        title = document.getElementById("mediaTitle");
        title.innerHTML = removeFileExtension(allowedMediaExtensions, addEmoteByFileExtension(mediaName))
        
        lastIndex = currentIndex + 1;
        // Ändere Zurück-Button
        subnode = document.getElementById("prev").childNodes[0]
        subnode.setAttribute("onclick", "playlistBackwards(" + lastIndex + ",'" + localPlaylist.join(listSeperator) + "');")
        subnode.innerHTML = "👈<span class='buttonText'> " + removeFileExtension(allowedMediaExtensions, lastName) + "</span>";
        
        if (nextName != null) {
            // Ändere Vorwärts-Button
            subnode = document.getElementById("next").childNodes[0]
            if (!realLink) {
                subnode.setAttribute("onclick", "playlistForwards(" + lastIndex + ",'" + localPlaylist.join(listSeperator) + "')")
            } else {
                // subnode.setAttribute("onclick", "");
                subnode.setAttribute("onclick", "loadUrl('start.html?=" + btoa(url) + "')");
            }
            subnode.innerHTML = "<span class='buttonText'>" + removeFileExtension(allowedMediaExtensions, nextName) + " </span>👉";
        }

        node = adjustMediaType(mediaUrl, node);
        if (node.tagName == "IMG") {
            // node.parentNode.href = mediaUrl;
            node.parentNode.setAttribute("onclick", "toggleFullscreenMode()");
        } else {
            // node.parentNode.href = null;
            node.parentNode.setAttribute("onclick", "");
        }
        node.src = mediaUrl;
        // timeoutArray.push(setTimeout('location.href = "#";location.href = "#mediaNav";', 100))


        // Setze Timer für Autoplayfunktion, falls Autoplay aktiviert
        if (document.getElementById("autoplayCheckBox").checked && document.getElementById("video").tagName == "IMG") {
            updateOptions(document.getElementById("autoplayCheckBox").checked, document.getElementById("amountOfAutoplay").value, document.getElementById("autoplayDuration").value);
            // Prüfe, ob ein Link vorhanden ist
            if (document.getElementById('next').childNodes.length > 0 && document.getElementById("next").childNodes[0].innerHTML != "") {
                // Animationsreset
                document.getElementById('next').style.animation = "none";
                timeoutArray.push(setTimeout('document.getElementById("next").style.animation = "autoplayLoading ' + (autoplayDuration-1+minTime) + 's"', 1000));
                // Triggere klick von Next-Link
                timeoutArray.push(setTimeout("document.getElementById('next').childNodes[0].click()", (autoplayDuration + minTime) * 1000));
            } else {
                // Reset der Einstellungen
                updateOptions(false, 0, document.getElementById("autoplayDuration").value);
            }
        }
    }
}

// Funktion, um das letzte Medium aufzurufen
function playlistBackwards(currentIndex, localPlaylist) {
    timeoutArray = cancelTimeouts()

    // Entferne Link-Node und ersetze mit leerem Link
    if (document.getElementById("next").childNodes.length > 0 && document.getElementById("next").childNodes[0].href != null) {
        document.getElementById("next").removeChild(document.getElementById("next").childNodes[0]);
    }
    document.getElementById("next").appendChild(document.createElement("a"));

    if (document.getElementById("prev").childNodes.length > 0 && document.getElementById("prev").childNodes[0].href != null) {
        document.getElementById("prev").removeChild(document.getElementById("prev").childNodes[0]);
    }
    document.getElementById("prev").appendChild(document.createElement("a"));
    
    localPlaylist = localPlaylist.split(listSeperator);
    if (currentIndex - 1 >= 0) {
        mediaUrl = localPlaylist[currentIndex - 1];
        currentPlaylistName = atob(window.location.href.split("?=")[1].split("#")[0]).split(listSeperator + "PL:")[1].split(listSeperator)[0];
        if (currentPlaylistName == "searchResults") {
            mediaUrl = mediaUrl.split("|")[1];
            window.location.href = mediaUrl;
            return null;
        }
        if (!mediaUrl.startsWith(".")) {
            mediaUrl = "." + mediaUrl;
        }
        // Ändere Item zuvor als Aktiv
        if (!document.getElementById(mediaUrl.replace(".","")).className.includes("activeMenu")) {
            document.getElementById(mediaUrl.replace(".","")).className += "activeMenu";
        }
        mediaName = localPlaylist[currentIndex - 1].split("/");
        mediaName = mediaName[mediaName.length - 1];
        
        document.title = "STEFFLIX - " + removeFileExtension(allowedMediaExtensions, mediaName); 

        // playlistName = "";
        // Lese URL aus Pfad aus
        path = extractFromPath(mediaUrl);
        // Speichere in zuletzt gesehen-Liste, sofern kein Bild
        if (!isImage(mediaUrl)) {
            setLastPlayed(mediaName, "start.html?=" + btoa(path) + "#mediaNav");
        }
        history.pushState({}, null, "start.html?=" + btoa(path) + "#mediaNav");

        realLink = false;
        if (currentIndex - 2 >= 0) {
            // Diese Staffel
            lastName = localPlaylist[currentIndex - 2].split("/")
            lastName = removeFileExtension(allowedMediaExtensions, lastName[lastName.length - 1]);
        } else {
            // Letzte Staffel
            lastName = null;
            url = swapSeason(-1);
            if (url != null) {
                lastName = url.split("MEDIA:")[1].split(listSeperator)[0].split("/");
                lastName = lastName[lastName.length - 1];
                realLink = true;
            }
        }

        nextName = localPlaylist[currentIndex];
        if (!nextName.startsWith(".")) {
            nextName = "." + nextName;
        }
        // Deaktiviere aktuelles Menüelement
        document.getElementById(nextName.replace(".", "")).className = document.getElementById(nextName.replace(".", "")).className.split("activeMenu").join("");
        nextName = nextName.split("/");
        nextName = nextName[nextName.length - 1];


        node = document.getElementById("video");
        // Ändere Titel
        title = document.getElementById("mediaTitle");
        title.innerHTML = removeFileExtension(allowedMediaExtensions, addEmoteByFileExtension(mediaName))
        
        lastIndex = currentIndex - 1;
        // Ändere Zurück-Button
        if (lastName != null) {
            subnode = document.getElementById("prev").childNodes[0]
            if (!realLink) {
                subnode.setAttribute("onclick", "playlistBackwards(" + lastIndex + ",'" + localPlaylist.join(listSeperator) + "');")
            } else {
                // subnode.setAttribute("onclick", "");
                subnode.setAttribute("onclick", "loadUrl('start.html?=" + btoa(url) + "')");
            }
            subnode.innerHTML = "👈<span class='buttonText'> " + removeFileExtension(allowedMediaExtensions, lastName) + "</span>";
        }
        
        // Ändere Vorwärts-Button
        subnode = document.getElementById("next").childNodes[0]
        subnode.setAttribute("onclick", "playlistForwards(" + lastIndex + ",'" + localPlaylist.join(listSeperator) + "');")
        subnode.innerHTML = "<span class='buttonText'>" + removeFileExtension(allowedMediaExtensions, nextName) + " </span>👉";
        
        node = adjustMediaType(mediaUrl, node);
        if (node.tagName == "IMG") {
            // node.parentNode.href = mediaUrl;
            node.parentNode.setAttribute("onclick", "toggleFullscreenMode()");
        } else {
            // node.parentNode.href = null;
            node.parentNode.setAttribute("onclick", "");
        }
        node.src = mediaUrl;
        // timeoutArray.push(setTimeout('location.href = "#";location.href = "#mediaNav";', 100))

    }
}

// Funktion um das Media-Tag entsprechend dem Medientyp anzupassen
function adjustMediaType(mediaUrl, node) {
    if (isImage(mediaUrl) && node.tagName != "IMG") {
        topNode = document.createElement("a");
        topNode.id = "imageLink";
        node = document.createElement("img");
        node.id = "video";
        topNode.appendChild(node)
        document.getElementById("media").replaceChild(topNode, document.getElementById("media").childNodes[7]);
    } else if (isVideo(mediaUrl) && node.tagName != "VIDEO") {
        node = document.createElement("video");
        node.autoplay = true;
        node.controls = true;
        appendMediafunctions(node);
        node.id = "video";
        document.getElementById("media").replaceChild(node, document.getElementById("media").childNodes[7]);
    } else if (isMusic(mediaUrl) && node.tagName != "AUDIO") {
        node = document.createElement("audio");
        node.autoplay = true;
        node.controls = true;
        appendMediafunctions(node);
        node.id = "video";
        document.getElementById("media").replaceChild(node, document.getElementById("media").childNodes[7]);
    }

    return node;
}

// Funktion, um den letzten/nächsten Unterordner zu suchen
function swapSeason(direction) {
    // Lese aktuelle Informationen aus
    currentUrl = window.location.href;
    decodedData = atob(currentUrl.split("?=")[1].split("#")[0]);
    playlist = decodedData.split("PL:")[1].split(listSeperator)[0];

    // Lese daten aus URL aus
    extractedData = extractData(decodedData);

    seasonData = data;
    url = "";
    target = Object.keys(extractedData["levels"]).length - 1;
    // Begrenze Datenarray auf Auswahl und erstelle Base-Url
    for (let index = 0; index < target; index++) {
        seasonData = seasonData[extractedData["levels"][index]];
        url += "LEVEL" + index + ":" + extractedData["levels"][index] + listSeperator;
    }

    searchFor = extractedData["levels"][target];

    // Erstelle und sortiere Schlüsselliste
    keyList = [];
    Object.keys(seasonData).forEach(element => {
        keyList.push(element);
    });
    keyList = keyList.sort();

    // Lese aktuellen Index der Staffel aus
    currentIndex = keyList.indexOf(searchFor);

    // Prüfe, ob der neue Index innerhalb des Ranges ist
    if (currentIndex + direction >= 0 && currentIndex + direction < keyList.length) {
        newSeason = keyList[currentIndex + direction];
        // Ergänze URL um neue Staffel
        url += "LEVEL" + target + ":" + newSeason + listSeperator;

        episodeList = Object.keys(seasonData[newSeason]);
        episodeList = episodeList.sort();

        // Lese nächste Episode anhand der Richtung aus
        if (direction < 0) {
            newEpisode = episodeList[episodeList.length - 1];
        } else {
            newEpisode = episodeList[0];
        }
        
        // Ergänze Url um Medienpfad 
        url += "MEDIA:" + newEpisode + listSeperator;

        // Ersetze alten Playlistnamen mit neuem
        newPlaylist = "PL:" + playlist.replace(searchFor, newSeason);
        url += newPlaylist;
        
        return url;
    }

    return null;
}

// Funktion zum extrahieren der Tiefen- und Medieninformationen aus dem Medienlink
function extractFromPath(path) {
    pathArray = path.replace("./", "").split("/");
    srcPath = path.replace("./", "").split("/");
    srcPath = "./" + srcPath.splice(0, srcPath.length - 1).join("/");
    // console.log(srcPath)
    playlist = "";
    depth = 0;
    url="./"
    pathArray.forEach(element => {
        if (!isMediaFile(allowedMediaExtensions, element)) {
            playlist += element;
            url += "LEVEL" + depth + ":" + element + listSeperator;
        } else {
            url += "MEDIA:" + srcPath + "/" + element;
        }

        depth += 1
    });
    if (url.includes("MEDIA")) {
        url += listSeperator + "PL:" + playlist;
    }
    return url;
}

// Lade übergebene URL
function loadUrl(url){
    window.location.href = url + "#mediaNav";
}

// Speichern der aktuellen einstellunen
function applySettings() {
    autoplay = document.getElementById("autoplayCheckBox").checked;
    autoplayAmount = document.getElementById("amountOfAutoplay").value;
    autoplayDuration = document.getElementById("autoplayDuration").value;

    if (!autoplay) {
        autoplayAmount = 0;
    } else if (autoplayAmount == 0) {
        autoplay = false;
    }

    // Entferne Klasse und Animation 
    if (!autoplay) {
        document.getElementById("media").className = document.getElementById("media").className.split("autoplaying").join("");
        if (document.getElementById("next").style.animation != null) {
            document.getElementById("next").style.animation = null;
        }
        // Lösche alle gesetztem Timeouts
        cancelTimeouts()
    } else {
        // Füge Autoplay-Klasse hinzu
        if (!document.getElementById("media").className.includes("autoplaying")) {
            document.getElementById("media").className += "autoplaying";
        }
    }
    
    localStorage.setItem("settings", [autoplay, autoplayAmount, autoplayDuration].join(listSeperator));
    updateOptionFields(autoplay, autoplayAmount, autoplayDuration);
    toggleContainer(['settingsBox'], "none");

    if (autoplay && document.getElementById("video").tagName == "IMG") {
        cancelTimeouts();
        if (document.getElementById("autoplayCheckBox").checked) {
            document.getElementById('next').style.animation = "autoplayLoading " + (autoplayDuration + minTime) + "s";
            timeoutArray.push(setTimeout("document.getElementById('next').childNodes[0].click()", (autoplayDuration + minTime) * 1000));
        }
        updateOptions(autoplay, autoplayAmount, autoplayDuration);
    }
}

// Funktion, um zu prüfen, dass beim aktivieren der Autoplayfunktion, die Anzahl mindestens auf 1 gesetzt wird.
function checkAutoplayAmout() {
    if (document.getElementById("autoplayCheckBox").checked) {
        if (document.getElementById("amountOfAutoplay").value <= 0) {
            document.getElementById("amountOfAutoplay").value = 1;
            if (!document.getElementById("media").className.includes("autoplaying")) {
                document.getElementById("media").className += "autoplaying";
            }
        }
    } else {
        document.getElementById("media").className = document.getElementById("media").className.split("autoplaying").join("");
        document.getElementById("amountOfAutoplay").value = 0;
    }
}

// Funktion, um die Einstellungen anhand abgespielter Medien zu aktualisieren
function updateOptions(autoplay, autoplayAmount, autoplayDuration) {
    // Ändere Datentyp in Boolean
    if (autoplay == "true") {
        autoplay = true;
    } else if (autoplay == "false") {
        autoplay = false;
    }

    // Ändere Datentyp in Integer
    autoplayAmount = parseInt(autoplayAmount);
    autoplayDuration = parseInt(autoplayDuration);
    
    // Verringere Anzahl Autoplay um 1
    if (autoplayAmount > 0) {
        autoplayAmount -= 1;
    } else {
        autoplayAmount = 0
    }
    
    // Deaktiviere Autoplay wenn Anzahl Autoplay = 0
    if (autoplay && autoplayAmount == 0) {
        autoplay = false;
    }
    
    // Setze Wert auf 0 zurück, wenn kleiner 0
    if (autoplayDuration < 0) {
        autoplayDuration = 0;
    }
    
    // Entferne Autoplay-Klasse
    if (!autoplay) {
        if (document.getElementById("media").className.includes("autoplaying")) {
            document.getElementById("media").className = document.getElementById("media").className.split("autoplaying").join("");
            document.getElementById("next").style.animation = null;
        }
    }

    // Aktualisiere Daten
    updateOptionFields(autoplay, autoplayAmount, autoplayDuration);
    localStorage.setItem("settings", [autoplay, autoplayAmount, autoplayDuration].join(listSeperator));
}

// Funktion, um die gespeicherten Einstellungen auf das Einstellungsmenü zu übertragen
function updateOptionFields(autoplay, autoplayAmount, autoplayDuration) {
    document.getElementById("autoplayCheckBox").checked = autoplay;
    document.getElementById("amountOfAutoplay").value = autoplayAmount;
    document.getElementById("autoplayDuration").value = autoplayDuration;
}

// Funktion, um lokale Daten zu löschen
// favorites, playlist, timestamp, playlast, all
function modifyLocalData(dataType) {
    document.getElementById("debugOutput").style.display = "block";
    var node = document.getElementById("debugField");
    var counter = 0;

    node.value = "==========================\nStarte Vorgang\n==========================\n";
    if (dataType == "playlist") {
        playlistPrefix = localStorage.getItem("currentPrefix");
        node.value += "Lösche: currentPrefix\n";
        localStorage.removeItem("currentPrefix");
        counter += 1;
    }

    for (var key in localStorage) {
        if (dataType == "all") {
            node.value += "Lösche: " + key + "\n";
            localStorage.removeItem(key);
            counter += 1;
        } else if (key.includes(dataType)) {
            node.value += "Lösche: " + key + "\n";
            localStorage.removeItem(key);
            counter += 1;
        }
    }

    node.value += "==========================\nVorgang beendet\nEs wurden " + counter + " Einträge gelöscht\n==========================\n";

    document.getElementById("debugOkButton").style.display = "block";
}

// Funktion, um gespeicherte Daten mit der aktuellen Version kompatibel zu machen
function updateData() {
    // Update 1.28 - Anpassen Listenseperator
    if (localStorage.getItem("v1.28") == null) {
        ignoreList = ["mediaVolume", "timestamp", "favorites", "currentPrefix", "playlast"];
        // Prüfe alle lokal gespeicherten Daten
        for (var key in localStorage) {
            if (localStorage.getItem(key) != null) {
                // Ersetze Listenseperator
                if (localStorage.getItem(key).split("Nav,").length > 1) {
                    korrektur = localStorage.getItem(key).split("Nav,").join("Nav" + listSeperator);
                } else {
                    korrektur = localStorage.getItem(key);
                }

                // Prüfe, ob der Name des Items angepasst werden muss
                doOverwrite = true;
                for (var index in ignoreList) {
                    if (key.includes(ignoreList[index])) {
                        doOverwrite = false;
                    }
                }
                
                // Passe Itemname an
                if (doOverwrite) {
                    newKey = "playlist-" + key;
                    korrektur = korrektur.split(",.").join(listSeperator + ".");
                } else {
                    newKey = key;
                }
                
                // Passe Favoritenliste an
                if (key == "favorites") {
                    korrektur = korrektur.split(",.").join(listSeperator + ".");
                }

                korrektur = korrektur.split(listSeperator)

                // Passe URL-Aufbau der gespeicherten Daten an
                for (var index in korrektur) {
                    if (korrektur[index].includes("start.html?=")) {
                        dataPrefix = korrektur[index].split("?=")[0] + "?=";
                        if (korrektur[index].split("#").length > 1) {
                            dataPostfix = "#" + korrektur[index].split("#")[1];
                        } else {
                            dataPostfix = "";
                        }

                        decodedData = atob(korrektur[index].split("?=")[1].split("#")[0]);
                        if (decodedData.includes(",LEVEL") || decodedData.includes(",MEDIA") || decodedData.includes(",PL")) {
                            decodedData = decodedData.replaceAll(",LEVEL", listSeperator + "LEVEL");
                            decodedData = decodedData.replaceAll(",MEDIA", listSeperator + "MEDIA");
                            decodedData = decodedData.replaceAll(",PL", listSeperator + "PL");
                        }

                        newUrl = dataPrefix + btoa(decodedData) + dataPostfix;
                        korrektur[index] = newUrl;
                    }
                }

                korrektur = korrektur.join(listSeperator);
                localStorage.removeItem(key);
                localStorage.setItem(newKey, korrektur);
            }
        }

        localStorage.setItem("v1.28", true)
    }

    // Update 1.33 - Anpassen Levelstruktur
    if (localStorage.getItem("v1.33") == null) {
        counter = 0;
        changeList = ["playlast"];
        for (var searchTerm in changeList) {
            for (var key in localStorage) {
                if (key.includes(changeList[searchTerm])) {
                    savedArray = localStorage.getItem(key).split(listSeperator);
                    newArray = [];
                    for (var item in savedArray) {
                        currentData = savedArray[item];
                        if (currentData.includes("start.html?=")) {
                            filename = currentData.split("|")[0];
                            url = currentData.split("|")[1];
                            urlPrefix = url.split("html?=")[0] + "html?=";

                            try {
                                urlPostfix = "#" + url.split("#")[1];
                            } catch (error) {
                                urlPostfix = "";
                            }

                            encodedData = url.split("?=")[1].split("#")[0];
                            decodedData = atob(encodedData);

                            newData = decodedData.replace("MAIN:", "LEVEL0:");

                            newArray.push(filename + "|" + urlPrefix + btoa(newData) + urlPostfix);
                            counter += 1;
                        }
                    }
                    localStorage.setItem(key, newArray.join(listSeperator));
                }
            }
        }
        if (counter > 0) {
            alert("Aufgrund des Updates 1.33 müssen die STEFFLIX-Daten neu eingelesen werden. Bitte Programm upadteData.exe ausführen!");
        }
        localStorage.setItem("v1.33", true);
    }
}

// Funktion, um Subroutinen an das Medienelement anzuhängen
function appendMediafunctions(node) {
    node.onloadedmetadata = function() {
        node.autoplay = true;
        node.play();
        location.href = "#";
        location.href = "#mediaNav";
        // Lese Dateinamen aus
        medialocation = replaceSpecialChars(document.getElementById("video").src)
        mediaName = medialocation.split("/");
        mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length -1].split("#")[0]);
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
    node.addEventListener('timeupdate', function() {
        medialocation = replaceSpecialChars(document.getElementById("video").src)
        mediaName = medialocation.split("/");
        mediaName = removeFileExtension(allowedMediaExtensions, mediaName[mediaName.length -1].split("#")[0]);

        currentTime = parseInt(this.currentTime, 10);
        currentTimestamp = localStorage.getItem("timestamp-" + mediaName);
        if (currentTimestamp == null) {
            currentTimestamp = 0;
        }
        // Speichere Videoposition alle 10 Sek
        if (currentTime != 0 && currentTimestamp != currentTime && currentTime % 10 == 0) {
            localStorage.setItem("timestamp-" + mediaName, currentTime);
            localStorage.setItem("mediaVolume", document.getElementById("video").volume)
        }

        // Unterscheide zwischen Audio- und Video-Elementen
        videoCheck = document.getElementById("video").tagName == "VIDEO" && document.getElementById("autoplayCheckBox").checked && this.duration - currentTime <= (autoplayDuration + minTime);
        audioCheck = document.getElementById("video").tagName == "AUDIO" && document.getElementById("autoplayCheckBox").checked && this.duration - currentTime <= 11;
        if (videoCheck || audioCheck) {
            prozent = 100 - parseInt(100/10*(this.duration - currentTime - (autoplayDuration + minTime) + 10));
            document.getElementById("next").style.background = "linear-gradient(to right, red " + (prozent+10) + "%, #424141 0%)";
            
            if (this.duration - currentTime <= autoplayDuration + minTime - 10) {
                document.getElementById("next").style.background = "";
                updateOptions(document.getElementById("autoplayCheckBox").checked, document.getElementById("amountOfAutoplay").value, document.getElementById("autoplayDuration").value);
                if (document.getElementById("next").childNodes.length > 0 && document.getElementById("next").childNodes[0].innerHTML != "") {
                    document.getElementById("next").childNodes[0].click();
                } else {
                    updateOptions(false, 0, document.getElementById("autoplayDuration").value);
                }
            }
        }

        if (this.duration > 600) {
            if (100 / this.duration * currentTimestamp > 95) {
                this.style.border = "1px solid red";
            } else {
                this.style.border = "0px solid #222";
            }
        }
    });
}

// Funktion zum umwandlen von Base64-Strings in 64Bit-Integer
function b64toInt(b64string) {
    try {
        var binary_string = window.atob(b64string);
        var len = binary_string.length;
        var bytes = new Uint8Array(len);
        for (var i = 0; i < len; i++) {
            bytes[i] = binary_string.charCodeAt(i);
        }
    
        var value = BigInt(0);
        for (var i = 0; i < bytes.length; i++) {
            value = (value * BigInt(256)) + BigInt(bytes[i]);
        }
        return value;
    } catch (error) {
        return null;
    }
}

// Funktion zum entschlüsseln des Lizenzkeys
function decodeSerial(serial) {
    try {
        // Öffentlicher Schlüssel
        publicKey = "A45KXDyaWbXfUSyIxXKG8JBXwX+IxqDl";
        // Umwandlungstabelle
        var conversionMatrix = {
            0: {0: '9', 1: ' ', 2: '8', 3: 'i', 4: '{', 5: 'G', 6: 'L', 7: '/', 8: 'Ä', 9: 'P'}, 
            1: {0: '+', 1: 'C', 2: '5', 3: 'ü', 4: '$', 5: '4', 6: ',', 7: 'H', 8: 'h', 9: '!'}, 
            2: {0: '-', 1: '.', 2: 'z', 3: 'F', 4: 'p', 5: 'Y', 6: 'E', 7: 'g', 8: 'f', 9: 'Z'}, 
            3: {0: ')', 1: 'y', 2: 'o', 3: 'b', 4: 'd', 5: '~', 6: 's', 7: 'T', 8: '3', 9: 'W'}, 
            4: {0: ':', 1: '[', 2: 'N', 3: '7', 4: 'U', 5: '1', 6: '_', 7: ']', 8: 'S', 9: 'D'}, 
            5: {0: 'A', 1: '}', 2: 'k', 3: 'ä', 4: 'I', 5: 'Ü', 6: 'u', 7: '2', 8: '=', 9: 'M'}, 
            6: {0: 'B', 1: 'K', 2: 'R', 3: 'm', 4: '?', 5: 'v', 6: ';', 7: 'x', 8: 'Q', 9: 'ö'}, 
            7: {0: '6', 1: 't', 2: 'q', 3: 'X', 4: 'j', 5: 'a', 6: '0', 7: 'V', 8: 'O', 9: 'r'}, 
            8: {0: 'w', 1: 'n', 2: 'c', 3: 'Ö', 4: 'J', 5: '%', 6: '}', 7: '*', 8: 'e', 9: '('}, 
            9: {0: '&', 1: 'l'}
        };
        
        // Encodiere Base64 in Ganzzahl
        publicValue = b64toInt(publicKey);
        secretValue = b64toInt(serial);
        
        // Wandle Nummer in Bitstring um
        secretBits = secretValue.toString(2)
        publicBits = publicValue.toString(2)

        // Fülle Bitlänge auf
        while (secretBits.length < publicBits.length) {
            secretBits = "0" + secretBits;
        }
        while (publicBits.length < secretBits.length) {
            publicBits = "0" + publicBits;
        }

        // XOR bitwise
        privateBits = ""
        for (var key in publicBits) {
            if (secretBits[key] == publicBits[key]) {
                privateBits += "0";
            } else {
                privateBits += "1"
            }
        }

        // Berechne Ganzzahl des privaten Schlüssels
        privateKey = BigInt(0)
        for (let index = privateBits.length-1; index >= 0; index--) {
            privateKey = privateKey + (BigInt(privateBits[index]) * BigInt(2**(privateBits.length-1-index)));
        }
        
        // Setze Nachricht zusammen
        lookupString = privateKey + "" + secretValue;
        
        // Entschlüssle Nachricht
        result = "";
        for (let index = 0; index < lookupString.length; index+=2) {
            const topLevel = lookupString[index];
            const lowLevel = lookupString[index+1];
            result += conversionMatrix[topLevel][lowLevel];
        }

        // Lese Telegram-Informationen aus.
        botToken = result.split(listSeperator)[0];
        chatId = result.split(listSeperator)[1];

        return [botToken, chatId];
    } catch (error) {
        return null;
    }
    
}

// Funktion zum Senden von Telegramnachrichten
function sendMessage() {
    document.getElementById("errorMsg").innerHTML = "";
    var sender = document.getElementById("feedbackName").value;
    var message = document.getElementById("feedbackText").value;
    var hasError = false;
    
    if (sender == null || sender == ""){
        document.getElementById("errorMsg").innerHTML += "Name darf nicht Leer sein<br>"
        hasError = true;
    }
    if (message == null || message == ""){
        document.getElementById("errorMsg").innerHTML += "Nachricht darf nicht Leer sein<br>"
        hasError = true;
    }
    
    serial = localStorage.getItem("serial"); 
    
    if (serial == "" || serial == null) {
        document.getElementById("errorMsg").innerHTML += "Kein Lizenzschlüssel angegeben<br>"
        hasError = true;
    }

    if (hasError) {
        document.getElementById("errorMsg").innerHTML += "Nachricht wurde nicht verschickt"
    } else {
        try {
            message = "ABSENDER: " + sender + "%0A" + "NACHRICHT: %0A" + message.split("\n").join("%0A");

            var xhr = new XMLHttpRequest();
            telegramConf = decodeSerial(serial);
            if (telegramConf != null) {
                token = telegramConf[0];
                chatId = telegramConf[1];
                
                xhr.onreadystatechange = function() {
                    if (this.readyState == 4) {
                        if (this.status < 300 && this.status >= 200) {
                            document.getElementById("errorMsg").innerHTML += "Nachricht wurde verschickt";
                            document.getElementById("errorMsg").style.color = "green";
                            document.getElementById("feedbackText").value = "";
                        } else {
                            document.getElementById("errorMsg").innerHTML += "Nachricht konnte nicht verschickt werden";
                            document.getElementById("errorMsg").style.color = "red";
                        }
                    }
                };

                xhr.open('POST', 'https://api.telegram.org/bot' + token + '/sendMessage?chat_id=' + chatId + '&text=' + message, true);
                xhr.send();
                
                
            } else {
                document.getElementById("errorMsg").innerHTML += "Lizenzschlüssel ungültig.<br>"
                document.getElementById("errorMsg").innerHTML += "Nachricht wurde nicht verschickt"
            }
        } catch (error) {
            document.getElementById("errorMsg").innerHTML += "Lizenzschlüssel ungültig.<br>"
            document.getElementById("errorMsg").innerHTML += "Nachricht wurde nicht verschickt"
        }
    }
}

// Funktion zum abspeichern des Lizenzschlüssels
function saveLicenseKey() {
    licenseKey = "";
    for (let index = 0; index < licenseFields.length; index++) {
        licenseKey += document.getElementById(licenseFields[index]).value;
    }
    
    localStorage.setItem("serial", licenseKey);
    toggleContainer(["serialKeyWrapper"], "none");
}

// Funktion, um die gesetzten Timeouts abzubrechen
function cancelTimeouts() {
    timeoutArray.forEach(element => {
        clearTimeout(element);
    });

    return []
}

// Funktion, um in den Vollbildmodus zu wechseln
function toggleFullscreenMode() {
    node = document.getElementById("media");
    if (!node.className.includes("fullscreened")) {
        node.className += " fullscreened";
        document.getElementById("mediaTitle").style.display = "none";
    } else {
        node.className = node.className.split(" fullscreened").join("");
        document.getElementById("mediaTitle").style.display = null;
        setTimeout('location.href = "#";location.href = "#mediaNav";', 400)
    }
}

// Funktion, um den verwendeten Browser auszulesen
function detectBrowser() {
    var test = function(regexp) {return regexp.test(window.navigator.userAgent)}
    switch (true) {
        case test(/edg/i): return "edge";
        case test(/trident/i): return "ie";
        case test(/firefox|fxios/i): return "firefox";
        case test(/opr\//i): return "opera";
        case test(/ucbrowser/i): return "uc browser";
        case test(/samsungbrowser/i): return "samsung browser";
        case test(/chrome|chromium|crios/i): return "chrome";
        case test(/safari/i): return "safari";
        default: return "Other";
    }
}

// Tastendruck auslesen
function checkKey(keyEvent) {
    node = document.getElementById("smiley")
    step = 100
    keyEvent = keyEvent || window.event;
    if (keyEvent.keyCode == '38') {
        newValue = parseInt(node.style.top.replace("px", "")) - step
        node.style.top = newValue + "px"
    }
    else if (keyEvent.keyCode == '40') {
        newValue = parseInt(node.style.top.replace("px", "")) + step
        node.style.top = newValue + "px"
    }
    else if (keyEvent.keyCode == '37') {
        newValue = parseInt(node.style.left.replace("px", "")) - step 
        node.style.left = newValue + "px"
    }
    else if (keyEvent.keyCode == '39') {
        newValue = parseInt(node.style.left.replace("px", "")) + step
        node.style.left = newValue + "px"
    } else {
    }

}

function gLoop() {
    var node = document.getElementById("smiley")
    var canvas = document.getElementsByTagName("body")[0]
    var sRect = node.getBoundingClientRect();
    var cRect = canvas.getBoundingClientRect();
    var curY = parseInt(node.style.top.replace("px", ""))

    if (sRect.bottom < cRect.bottom) {
        node.style.top = parseInt(curY + (1 * gravity)) + "px"
        gravity += 0.1
    } else if (sRect.bottom > cRect.bottom) {
        node.style.top = parseInt(curY - 1) + "px"
        gravity = 0
    } else {
        gravity = 0
    }

    // while (sRect.bottom > maxY) {
    //     curY = parseInt(node.style.top.replace("px", ""))
    //     node.style.top = curY - 1
    // }
    window.requestAnimationFrame(gLoop);
}

//  __   __  _______  ______    _______  _______  ______    _______  ___   _______  __   __  __    _  _______ 
// |  | |  ||       ||    _ |  |  _    ||       ||    _ |  |       ||   | |       ||  | |  ||  |  | ||       |
// |  |_|  ||   _   ||   | ||  | |_|   ||    ___||   | ||  |    ___||   | |_     _||  | |  ||   |_| ||    ___|
// |       ||  | |  ||   |_||_ |       ||   |___ |   |_||_ |   |___ |   |   |   |  |  |_|  ||       ||   | __ 
// |       ||  |_|  ||    __  ||  _   | |    ___||    __  ||    ___||   |   |   |  |       ||  _    ||   ||  |
//  |     | |       ||   |  | || |_|   ||   |___ |   |  | ||   |___ |   |   |   |  |       || | |   ||   |_| |
//   |___|  |_______||___|  |_||_______||_______||___|  |_||_______||___|   |___|  |_______||_|  |__||_______|
// javascript.options.bigint = true;s

var myHeaders = new Headers(); // Currently empty
myHeaders.append('Feature-Policy', 'autoplay self');

// Lokale daten updaten
updateData()

// Lade Favoriten
favorites = localStorage.getItem("favorites");
if (favorites != null) {
    favorites = favorites.split(listSeperator);
}

// Falls vorhanden, decodiere Datenstring
if (window.location.href.includes("?=")) {
    decodedUriData = atob(window.location.href.split("?=")[1].split("#")[0]);
}

// Füge Event-Listener zu Einstellungen hinzu
document.getElementById("amountOfAutoplay").addEventListener("change", function () {
    if (document.getElementById("amountOfAutoplay").value <= 0) {
        document.getElementById("autoplayCheckBox").checked = false;
    } else {
        document.getElementById("autoplayCheckBox").checked = true;
    }
})

// Füge Event-Listener zu Lizenzschlüsselfelder hinzu
licenseFields = ["lic1", "lic2", "lic3", "lic4", "lic5", "lic6", "lic7", "lic8"];
for (let index = 0; index < licenseFields.length; index++) {
    document.getElementById(licenseFields[index]).addEventListener("input", function () {
        if (document.getElementById(licenseFields[index]).value.length == 4 && index+1 < licenseFields.length) {
            document.getElementById(licenseFields[index+1]).focus();
        } else if (index+1 >= licenseFields.length) {
            document.getElementById("saveSerialButton").focus();
        }
    })
}


// Füge Event-Listener zu "Dauer bis Übergang" in Autoplayeinstellungen hinzu
document.getElementById("autoplayDuration").addEventListener("change", function () {
    if (document.getElementById("autoplayDuration").value < 0) {
        document.getElementById("autoplayDuration").value = 0;
    } 
})

// Prüfe Tastendruck
// document.onkeydown = checkKey;

// GLoop
// window.onload = gLoop();
// Playlist Präfix 
// playlistPrefix = "playlist-";

//  __   __  _______  __    _  __   __ 
// |  |_|  ||       ||  |  | ||  | |  |
// |       ||    ___||   |_| ||  | |  |
// |       ||   |___ |       ||  |_|  |
// |       ||    ___||  _    ||       |
// | ||_|| ||   |___ | | |   ||       |
// |_|   |_||_______||_|  |__||_______|
// Iteriere über alle Schlüsselelemente des data-Array
for (var key in data) {
    if (data.hasOwnProperty(key)) {
        // Erstelle Listenelement für jeden Schlüssel
        node = document.createElement("LI");
        link = document.createElement("a");
        textnode = document.createTextNode(key);
        link.appendChild(textnode);
        // link.href = "start.html?=" + btoa("LEVEL0:" + key);
        link.href = "start.html?=" + btoa("LEVEL0:" + key);
        node.appendChild(link);
        node.id = key;
        document.getElementById("menu").appendChild(node);
    }
}
// Sortiere Liste
sortList(document.getElementsByClassName('menu')[0]);


if (decodedUriData != null && decodedUriData.includes("LEVEL0:")) {
    mainContent = replaceSpecialChars(decodedUriData.split("LEVEL0:")[1].split(listSeperator)[0].split("#")[0]);
    createSubmenus(decodedUriData, data[mainContent], "LEVEL0:" + mainContent)
} else if (window.location.href.includes("?=") && decodedUriData.includes("SEARCH:")) {
    //  _______  __   __  _______  __   __  _______ 
    // |       ||  | |  ||       ||  | |  ||       |
    // |  _____||  | |  ||       ||  |_|  ||    ___|
    // | |_____ |  |_|  ||       ||       ||   |___ 
    // |_____  ||       ||      _||       ||    ___|
    //  _____| ||       ||     |_ |   _   ||   |___ 
    // |_______||_______||_______||__| |__||_______|

    document.getElementById("media").style.display = "block";
    // Lese Suchtext aus
    searchTerm = decodedUriData.split("SEARCH:")[1];
    // Starte rekursive Suche
    searchResults = recursiveSearch(data, searchTerm);
    // Speichere Playliste
    localStorage.setItem("playlist-searchResults", searchResults.join(listSeperator));
    wrapperNode = document.createElement("div");
    subnode = document.createElement("p");
    textnode = document.createTextNode("Total Suchresultate: " + searchResults.length);
    subnode.style.padding = "20px";
    subnode.style.color = "white";
    subnode.appendChild(textnode);
    wrapperNode.appendChild(subnode);

    if (!searchResults.length == 0) {
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
            mainContent = replaceSpecialChars(decodedUrl.split("LEVEL0:")[1].split(listSeperator)[0].split("#")[0]);
            if (addPreviewImage(decodedUrl, buttonNode, "suche") != null){
                // Styles für Button ändern
                buttonNode.style.display = "flex";
                buttonNode.style.flexDirection = "row";
                buttonNode.style.alignItems = "center";
                buttonNode.style.justifyContent = "flex-start";
                buttonNode.style.maxHeight = "150px";
                buttonNode.style.textAlign = "left";
            }
            // Erstelle Buttontext
            textNode = document.createTextNode(removeFileExtension(allowedMediaExtensions, lastTitle));
            // Füge Button hinzu
            buttonNode.appendChild(textNode);
            wrapperNode.appendChild(buttonNode);
        }
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
    document.getElementById("media").appendChild(wrapperNode);
} else {
    //  ___      _______  __    _  ______   ___   __    _  _______  _______  _______  _______  _______ 
    // |   |    |   _   ||  |  | ||      | |   | |  |  | ||       ||       ||   _   ||       ||       |
    // |   |    |  |_|  ||   |_| ||  _    ||   | |   |_| ||    ___||    _  ||  |_|  ||    ___||    ___|
    // |   |    |       ||       || | |   ||   | |       ||   | __ |   |_| ||       ||   | __ |   |___ 
    // |   |___ |       ||  _    || |_|   ||   | |  _    ||   ||  ||    ___||       ||   ||  ||    ___|
    // |       ||   _   || | |   ||       ||   | | | |   ||   |_| ||   |    |   _   ||   |_| ||   |___ 
    // |_______||__| |__||_|  |__||______| |___| |_|  |__||_______||___|    |__| |__||_______||_______|
    
    document.getElementById("media").style.display = "block";
                                                            
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
            buttonNode.href = "start.html?=" + btoa("HELP") + "#stefflixUpdate";
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

                                                         
        //  _____     _         _                           
        // |   __|___| |___ ___| |_ ___ ___ ___ ___ ___ ___ 
        // |__   | . | | .'|_ -|   |_ -|  _|  _| -_| -_|   |
        // |_____|  _|_|__,|___|_|_|___|___|_| |___|___|_|_|
        //       |_|                                        
        // Wenn keine Auswahl getroffen wurde, Splashscreen anzeigen
        node = document.getElementById("splashscreen");
        node.style.display = "block";
        image = document.createElement("img");

        image.src = "sflix_sys/sflix.png";
        node.appendChild(image);

        lastPlayed = localStorage.getItem("playlast");
        if (lastPlayed != null) {
            lastPlayed = lastPlayed.split(listSeperator);
        } else {
            lastPlayed = [];
        }

                                                                                           
        //  _____     _     _       _                      _     _ _      __    _     _       
        // |__   |_ _| |___| |_ ___| |_    ___ ___ ___ ___|_|___| | |_   |  |  |_|___| |_ ___ 
        // |   __| | | | -_|  _|- _|  _|  | . | -_|_ -| . | | -_| |  _|  |  |__| |_ -|  _| -_|
        // |_____|___|_|___|_| |___|_|    |_  |___|___|  _|_|___|_|_|    |_____|_|___|_| |___|
        //                                |___|       |_|                                                                                           
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

                var decodedUrl = atob(lastUrl.split("?=")[1].split("#")[0]);
                var imageIsSet = false;

                image = addPreviewImage(decodedUrl, buttonNode, "vorschau");
                if (image != null) {
                    imageIsSet = true;
                }

                if (imageIsSet) {
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
        //  _____ _ _ ___         _____     _ _       
        // |  |  |_| |  _|___ ___|   __|___|_| |_ ___ 
        // |     | | |  _| -_|___|__   | -_| |  _| -_|
        // |__|__|_|_|_| |___|   |_____|___|_|_| |___|
        document.getElementById("media").style.display = "block";
        document.getElementById("sflixPreview").src = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/sflix_sys/sflix.gif";

        // Lade changelog
        news = httpGet("https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/news.txt");

        // Zeige Hilfe an
        node = document.getElementById("help");
        node.style.display = "block";

        browserName = detectBrowser()
        if (browserName == "firefox") {
            document.getElementById("firefoxBrowserHelp").style.display = "block";
        } else if (browserName == "chrome") {
            document.getElementById("chromeBrowserHelp").style.display = "block";
        } else if (browserName == "edge") {
            document.getElementById("edgeBrowserHelp").style.display = "block";
        } else {
            document.getElementById("otherBrowserHelp").style.display = "block";
        }

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
        document.getElementById("help").appendChild(node);
        document.getElementById("changelog").innerHTML = news;
    }
}


//  __   __  _______  ______   ___   _______  __    _ 
// |  |_|  ||       ||      | |   | |       ||  |  | |
// |       ||    ___||  _    ||   | |    ___||   |_| |
// |       ||   |___ | | |   ||   | |   |___ |       |
// |       ||    ___|| |_|   ||   | |    ___||  _    |
// | ||_|| ||   |___ |       ||   | |   |___ | | |   |
// |_|   |_||_______||______| |___| |_______||_|  |__|
// Prüfe, ob ein Medientyp ausgewählt wurde
if (decodedUriData != null && decodedUriData.includes("MEDIA:")) {
    document.getElementById("media").style.display = "block";

    if (decodedUriData.includes(listSeperator + "PL:")) {
        playlistName = decodedUriData.split(listSeperator + "PL:")[1].split(listSeperator)[0].split("#")[0];
    }
    
    // Lade aktuelle Playliste
    localPlaylist = localStorage.getItem("playlist-" + playlistName).split(listSeperator);
    // Lese Speicherort aus
    medialocation = replaceSpecialChars(decodedUriData.split("MEDIA:")[1].split(listSeperator)[0].split("#")[0]);
    // Lese Playlistenindex aus
    localPlaylist = localPlaylist.sort()
    if (medialocation.startsWith(".")) {
        link = medialocation.replace(".","");
    } else {
        link = medialocation;
        medialocation = "." + medialocation;
    }
    document.getElementById(link).className += " activeMenu";

    var cleanedPlaylist = []
    for (var key in localPlaylist){
        cleanedPlaylist.push(localPlaylist[key].split("|")[0])
    }
    const currentIndex = cleanedPlaylist.indexOf(medialocation);
    // Aktuelle Auswahl auslesen
    currentUrl = decodedUriData.split(listSeperator + "MEDIA:")[0];
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
        //  _____ _   _           _ _____         _ _   
        // |  |  |_|_| |___ ___  / |     |_ _ ___|_| |_ 
        // |  |  | | . | -_| . |/ /| | | | | |_ -| | '_|
        //  \___/|_|___|___|___|_/ |_|_|_|___|___|_|_,_|
        // Erstelle Videoelement
        if (isMusic(medialocation)) {
            node = document.createElement("audio");
            node.style.backgroundColor = "#131313";
            node.style.width = "60vw";
            node.style.height = "40vh";
        }else {
            node = document.createElement("video");
        }
        node.src = medialocation;
        node.id = "video";
        node.controls = true;
        
        // Lade gespeicherte Lautstärke
        savedVolume = localStorage.getItem("mediaVolume");
        if (savedVolume == null){
            savedVolume = 1;
        } else if (savedVolume == 0) {
            savedVolume = 0.2;
        }
        node.volume = savedVolume;
        
        document.getElementById("media").appendChild(node);
        appendMediafunctions(node);

        // Detektieren Tastendruck
        document.addEventListener('keydown', logKey);
        function logKey(e) {
            if (`${e.code}` == "ArrowRight") {
                e.preventDefault();
                document.getElementById("video").currentTime += 10;
            }
            if (`${e.code}` == "ArrowLeft") {
                e.preventDefault();
                document.getElementById("video").currentTime -= 10;
            }
            if (`${e.code}` == "ArrowUp") {
                e.preventDefault();
                document.getElementById("video").volume += 0.1;
            }
            if (`${e.code}` == "ArrowDown") {
                e.preventDefault();
                document.getElementById("video").volume -= 0.1;
            }
            // if (`${e.code}` == "f") {
            //     document.getElementById("video").toggleFullscreenMode
            // }
        } 
    } else if (isImage(medialocation)) {
        //  _____ _ _   _         
        // | __  |_| |_| |___ ___ 
        // | __ -| | | . | -_|  _|
        // |_____|_|_|___|___|_|  
        // Erstelle Bildelement
        element = document.getElementById("media");
        node = document.createElement("img");
        node.src = medialocation;
        node.id = "video";
        
        // Füge Link zu Bild hinzu
        linkNode = document.createElement("a");
        linkNode.id = "imageLink";
        // linkNode.href = medialocation.split("#")[0];
        linkNode.setAttribute("onclick", "toggleFullscreenMode()");

        linkNode.appendChild(node);
        document.getElementById("media").appendChild(linkNode);

        if (document.getElementById("autoplayCheckBox").checked) {
            updateOptions(document.getElementById("autoplayCheckBox").checked, document.getElementById("amountOfAutoplay").value, document.getElementById("autoplayDuration").value);
            document.getElementById('next').style.animation = "autoplayLoading " + (autoplayDuration + minTime) + "s";
            timeoutArray.push(setTimeout("document.getElementById('next').childNodes[0].click()", (autoplayDuration + minTime) * 1000));
        }
    }

    setLastPlayed(mediaName, window.location.href)
    // Sichtbarkeit der Navigationsleiste umstellen
    node = document.getElementById("mediaNav");
    node.style.display = "flex";

    //  __   __  _______  ______   ___   _______  __    _  _______  __   __ 
    // |  |_|  ||       ||      | |   | |   _   ||  |  | ||   _   ||  | |  |
    // |       ||    ___||  _    ||   | |  |_|  ||   |_| ||  |_|  ||  |_|  |
    // |       ||   |___ | | |   ||   | |       ||       ||       ||       |
    // |       ||    ___|| |_|   ||   | |       ||  _    ||       ||       |
    // | ||_|| ||   |___ |       ||   | |   _   || | |   ||   _   | |     | 
    // |_|   |_||_______||______| |___| |__| |__||_|  |__||__| |__|  |___|  
    //                _ _         
    //  _____        |_|_|    _   
    // |__   |_ _ ___ _ _ ___| |_ 
    // |   __| | |  _| | |  _| '_|
    // |_____|___|_| |___|___|_,_|
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
        textnode = document.createTextNode(mediaName);
        subnode.appendChild(textnode);
        subnode.innerHTML = "👈 <span class='buttonText'>" + subnode.innerHTML + "</span>"

        subnode.setAttribute("onclick", "playlistBackwards(" + currentIndex + ",'" + localPlaylist.join(listSeperator) + "')")
        node.appendChild(subnode);
    } else {
        // Letzte Staffel
        node = document.getElementById("prev");
        subnode = document.createElement("a");
        urlData = swapSeason(-1);

        if (urlData != null) {
            medialocation = urlData.split("MEDIA:")[1].split(listSeperator)[0];
            // Entferne Pfad und Dateierweiterung und setze als Titel
            textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, medialocation.split("/")[medialocation.split("/").length - 1]))
            subnode.appendChild(textnode);
            subnode.innerHTML = "👈 <span class='buttonText'>" + subnode.innerHTML + "</span>"
            subnode.href = "start.html?=" + btoa(urlData) + "#mediaNav";
            node.appendChild(subnode);
        }
    }

                               
    //  _____         _           
    // |     |___ ___| |_ ___ ___ 
    // | | | | -_|  _| '_| -_|   |
    // |_|_|_|___|_| |_,_|___|_|_|
    // Erstelle Merken-Button
    node = document.getElementById("favorite")
    subnode = document.createElement("a");
    if (favorites == null || !favorites.includes(medialocation)) {
        textnode = document.createTextNode("Merken");
        node.className = node.className.split("isFavorite").join("");
    } else {
        textnode = document.createTextNode("Merken ⭐");
        node.className += "isFavorite";
    }
    subnode.appendChild(textnode);
    subnode.setAttribute("onclick", "updateFavorites()");
    node.appendChild(subnode);

    //        _ _                     
    //  _____|_|_|    _       _       
    // |   | |___ ___| |_ ___| |_ ___ 
    // | | | | .'|  _|   |_ -|  _| -_|
    // |_|___|__,|___|_|_|___|_| |___|
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

        // Lese aktuelle Daten aus
        currentUrl = atob(window.location.href.split("?=")[1].split("#")[0]).split(listSeperator + "MEDIA:")[0];
        playlistName = atob(window.location.href.split("?=")[1].split("#")[0]).split(listSeperator + "PL:")[1].split(listSeperator)[0];
        // Lade Playliste
        localPlaylist = localStorage.getItem("playlist-" + playlistName).split(listSeperator).sort();

        textnode = document.createTextNode(mediaName);
        subnode.appendChild(textnode);
        subnode.innerHTML = "<span class='buttonText'>" + subnode.innerHTML + " </span>👉"


        subnode.setAttribute("onclick", "playlistForwards(" + currentIndex + ",'" + localPlaylist.join(listSeperator) + "')")
        node.appendChild(subnode);
    } else {
        // Nächste Staffel
        urlData = swapSeason(1);

        node = document.getElementById("next");
        subnode = document.createElement("a");
        
        if (urlData != null) {
            medialocation = urlData.split("MEDIA:")[1].split(listSeperator)[0]
            // Entferne Pfad und Dateierweiterung und setze als Titel
            textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, medialocation.split("/")[medialocation.split("/").length - 1]))
            subnode.appendChild(textnode);
            subnode.innerHTML = "<span class='buttonText'>" + subnode.innerHTML + " </span>👉"

            subnode.href = "start.html?=" + btoa(urlData) + "#mediaNav";
            node.appendChild(subnode);
        }
    }

    if (document.getElementById("autoplayCheckBox").checked) {
        if (!document.getElementById("media").className.includes("autoplaying")) {
            document.getElementById("media").className += "autoplaying";
        }
    } else {
        document.getElementById("media").className = document.getElementById("media").className.split("autoplaying").join("");
    }
} else {
    // Platzhalterbild für Leere Seiten erstellen
    document.getElementById("mainBody").style.backgroundImage = "url(sflix_sys/sflix_bg.jpg)";
}

