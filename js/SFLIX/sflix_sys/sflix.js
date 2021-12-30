// Variabeldeklaration
var decodedUriData = null;
var element = null;
var currentFavorites = null;
var favorites = null;
var allowedMediaExtensions = [".mp4", ".ogg", ".mp3", ".jpg", ".jpeg", ".png", ".gif"];
var playListPrefix = "";

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
    if (filename.includes(".mp4") || filename.includes(".ogg")) {
        newFilename = newFilename + " 🎬";
    } else if (filename.includes(".mp3")) {
        newFilename = newFilename + " 🎵";
    } else if (filename.includes("jpeg") || filename.includes("jpg") || filename.includes("png") || filename.includes("gif")){
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

// Lade Favoriten
favorites = localStorage.getItem("favorites");
if (favorites != null) {
    favorites = favorites.split(",");
}

// Falls vorhanden, decodiere Datenstring
if (window.location.href.includes("?=")) {
    decodedUriData = atob(window.location.href.split("?=")[1].split("#")[0]);
}

// Iteriere über alle Schlüsselelemente des data-Arrays
for (var key in data) {
    if (data.hasOwnProperty(key)) {
        // Erstelle Listenelement für jeden Schlüssel
        node = document.createElement("LI");
        link = document.createElement("a")
        textnode = document.createTextNode(key);
        link.appendChild(textnode);
        link.href = "start.html?=" + btoa("MAIN:" + key);
        node.appendChild(link);
        document.getElementById("menu").appendChild(node);
        
    }
}
    // Sortiere Liste
sortList(document.getElementsByClassName('menu')[0]);

// Prüfe, ob aus dem Hauptmenü eine Auswahl getroffen wurde
if (decodedUriData != null && decodedUriData.includes("MAIN:")) {
    // Zerlege URL in "MAIN"-Bestandteil
    var mainContent = replaceSpecialChars(decodedUriData.split("MAIN:")[1].split(",")[0]);
    // Lege leere Playliste an
    playlist = []

    // Iteriere durch alle Elemente des gewählten Schlüssels
    for (var key in data[mainContent]) {
        if (data[mainContent].hasOwnProperty(key)) {
            // Füge alle Elemente eines Schlüssel der Playlist zu
            playlist.push(key);
            node = document.createElement("LI");
            link = document.createElement("a")
            // Entferne HTML-Code aus Schlüsselname
            cleanedKey = replaceSpecialChars(key);

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
                link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",MEDIA:" + key) + "#mediaNav";
                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, text));
            } else {
                link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + key);
                textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, key));
            }
            link.appendChild(textnode);
            node.appendChild(link);
            document.getElementById("submenu").appendChild(node);
            
        }
    }
    // Sortiere Liste
    sortList(document.getElementsByClassName('submenu')[0]);

    // Prüfe, ob eine "LEVEL1"-Auswahl getroffen wurde
    if (decodedUriData.includes("LEVEL1")) {
        // Zerlege URL in "LEVEL1"-Bestandteil
        var level1 = replaceSpecialChars(decodedUriData.split(",LEVEL1:")[1].split(",")[0]);
        playListPrefix += level1;
        // Erstelle Leere Playliste
        playlist = []

        // Iteriere über jeden Schlüsselwert
        for (var key in data[mainContent][level1]) {
            if (data[mainContent][level1].hasOwnProperty(key)) {
                // Füge alle Elemente eines Schlüssel der Playlist zu
                playlist.push(key);
                node = document.createElement("LI");
                link = document.createElement("a");
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);
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
                
                // Prüfe, ob der aktuelle Schlüssel ein Medientyp ist
                if (isMediaFile(allowedMediaExtensions, key)) {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",MEDIA:" + key) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + key);
                }
                node.appendChild(link);
                document.getElementById("subsubmenu").appendChild(node);
            }
        }
        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubmenu')[0]);
    }

    // Prüfe, ob eine "LEVEL2"-Auswahl gefällt wurde
    if (decodedUriData.includes("LEVEL2")) {
        // Zerlege URL in "LEVEL2"-Bestandteil
        var level2 = replaceSpecialChars(decodedUriData.split(",LEVEL2:")[1].split(",")[0]);
        playListPrefix += level2;
        // Erstelle Leere Playliste
        playlist = []

        for (var key in data[mainContent][level1][level2]) {
            if (data[mainContent][level1][level2].hasOwnProperty(key)) {
                // Füge alle Elemente eines Schlüssel der Playlist zu
                playlist.push(key);
                node = document.createElement("LI");
                link = document.createElement("a")
                text = key.split("/")
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);

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
                        link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",MEDIA:" + key) + "#mediaNav";
                } else {
                        link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + key);
                }
                node.appendChild(link);
                document.getElementById("subsubsubmenu").appendChild(node);
            }
        }
        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubsubmenu')[0]);
    }

    // Prüfe, ob eine "LEVEL3"-Auswahl getroffen wurde
    if (decodedUriData.includes("LEVEL3")) {
        // Zerlege URL in "LEVEL3"-Bestandteil
        var level3 = replaceSpecialChars(decodedUriData.split(",LEVEL3:")[1].split(",")[0]);
        playListPrefix += level3;
        // Erstelle Leere Playliste
        playlist = []

        for (var key in data[mainContent][level1][level2][level3]) {
            if (data[mainContent][level1][level2][level3].hasOwnProperty(key)) {
                // Füge alle Elemente eines Schlüssel der Playlist zu
                playlist.push(key);
                node = document.createElement("LI");
                link = document.createElement("a")
                text = key.split("/")
                // Entferne HTML-Code aus Schlüsselname
                cleanedKey = replaceSpecialChars(key);

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
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",MEDIA:" + key) + "#mediaNav";
                } else {
                    link.href = "start.html?=" + btoa("MAIN:" + mainContent + ",LEVEL1:" + level1 + ",LEVEL2:" + level2 + ",LEVEL3:" + level3 + ",LEVEL4:" + key);
                }
                node.appendChild(link);
                document.getElementById("subsubsubsubmenu").appendChild(node);
            }
        }
        // Sortiere Liste
        sortList(document.getElementsByClassName('subsubsubsubmenu')[0]);
    }

    // So lange kein Medientyp ausgewählt wurde, Playliste überschreiben
    if (!decodedUriData.includes("MEDIA")) {
        localStorage.setItem("currentPrefix", playListPrefix);
        localStorage.setItem(playListPrefix + "Playlist", playlist);
    }
} else {
    // Wenn keine Auswahl getroffen wurde, Splashscreen anzeigen
    node = document.getElementById("splashscreen");
    node.style.display = "block";
    var image = document.createElement("img");
    image.src = "sflix_sys/sflix.png";
    node.appendChild(image);
}



// Prüfe, ob ein Medientyp ausgewählt wurde
if (decodedUriData != null && decodedUriData.includes("MEDIA:")) {
    playListPrefix = localStorage.getItem("currentPrefix")
    console.log(playListPrefix);
    // Lade aktuelle Playliste
    var localPlaylist = localStorage.getItem(playListPrefix + "Playlist").split(",");
    // Lese Speicherort aus
    var medialocation = replaceSpecialChars(decodedUriData.split("MEDIA:")[1].split(",")[0]);
    // Lese Playlistenindex aus
    localPlaylist = localPlaylist.sort()
    var currentIndex = localPlaylist.indexOf(medialocation);
    // Aktuelle Auswahl auslesen
    var currentUrl = decodedUriData.split(",MEDIA:")[0];
    // Lese Dateinamen aus
    var mediaName = medialocation.split("/");
    mediaName = mediaName[mediaName.length -1].split("#")[0];

    // Schreibe Medientitel
    textnode = document.createTextNode(removeFileExtension(allowedMediaExtensions, addEmoteByFileExtension(mediaName)));
    document.title = "STEFFLIX - " + removeFileExtension(allowedMediaExtensions, mediaName); 
    document.getElementById("mediaTitle").appendChild(textnode)
    
    // Prüfe, ob die Datei eine Video- oder Musik-Datei ist
    if (medialocation.includes("mp4") || medialocation.includes(".mp3")) {
        // Erstelle Videoelement
        node = document.createElement("video");
        node.src = medialocation;
        node.id = "video";
        node.controls = true;
        

        document.getElementById("media").appendChild(node);

        node.onloadedmetadata = function() {
            // Lese Zeitstempel des ausgewählten Videos aus
            var pastTimestamp = localStorage.getItem("timestamp-" + mediaName)
            if (pastTimestamp != null) {
                // Setze Player auf gespeicherten Zeitstempel
                if (100 / this.duration * pastTimestamp < 95) {
                    document.getElementById("video").currentTime = pastTimestamp;
                } else {
                    localStorage.removeItem("timestamp-" + mediaName)
                }
            }
        };

        // Lese aktuellen Zeitstempel im Video aus
        document.getElementById("video").addEventListener('timeupdate', function() {
            currentTime = parseInt(this.currentTime, 10);
            // Speichere Videoposition alle 10 Sek
            if (currentTime % 10 == 0) {
                localStorage.setItem("timestamp-" + mediaName, currentTime)
            }
        });

    } else if (medialocation.includes("jpg") || medialocation.includes("png")) {
        // Erstelle Bildelement
        element = document.getElementById("media");
        node = document.createElement("img");
        node.src = medialocation;
        node.id = "imagenode" + Date.now();
        
        // Füge Link zu Bild hinzu
        var linkNode = document.createElement("a");
        linkNode.href = medialocation.split("#")[0];
        linkNode.appendChild(node);
        document.getElementById("media").appendChild(linkNode);
    }

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
        node = document.getElementById("prev")
        subnode = document.createElement("a");
        textnode = document.createTextNode("👈 Zurück");
        subnode.appendChild(textnode);
        // Link zu vorherigem Listenelement
        subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex - 1]) + "#mediaNav";
        node.appendChild(subnode);
    }

    // Erstelle Merken-Button
    node = document.getElementById("favorite")
    subnode = document.createElement("a");
    if (favorites == null || !favorites.includes(medialocation)) {
        textnode = document.createTextNode("Merken");
    } else {
        textnode = document.createTextNode("Merken ⭐");
    }
    subnode.appendChild(textnode);
    subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex] + ",FAV:True") + "#mediaNav";
    // subnode.href = localPlaylist[currentIndex];
    node.appendChild(subnode);


    // Prüfe ob nächster Index noch innerhalb der Range ist
    if (currentIndex + 1 <= localPlaylist.length - 1) {
        node = document.getElementById("next");
        subnode = document.createElement("a");
        textnode = document.createTextNode("Vorwärts 👉");
        subnode.appendChild(textnode);
        subnode.href = "start.html?=" + btoa(currentUrl + ",MEDIA:" + localPlaylist[currentIndex + 1]) + "#mediaNav";
        // subnode.href = localPlaylist[currentIndex + 1];
        node.appendChild(subnode);
    }
} else {
    // Platzhalterbild für Leere Seiten erstellen
    image = document.createElement("img");
    image.src = "sflix_sys/sflix_bg.jpg";
    image.id = "mainBackground";
    node = document.getElementById("placeholderImage");
    node.appendChild(image);
}
