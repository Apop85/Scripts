// STEFFLIX - player.js
// Wiedergabe: Player, Playlisten, Zufallswiedergabe, Gesehen, Zeitstempel, Favoriten und Bildbetrachter
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

// Erstellt einen Link, der ein Medium ohne Seitenreload lädt
function setMediaLink(link, decodedData) {
    var targetUrl = buildStartUrl(decodedData);
    if (targetUrl == null) {
        return false;
    }
    link.href = targetUrl;
    link.addEventListener("click", function (event) {
        // Ctrl/Shift/Mittelklick öffnen wie gewohnt einen neuen Tab
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.button !== 0) {
            return;
        }
        event.preventDefault();
        loadDecodedMedia(decodedData, true);
        // Kachel nicht fokussiert lassen, sonst blättern die Pfeiltasten durch die Kacheln
        // statt durch die Bilder bzw. statt im Video zu spulen
        if (link.classList.contains("card")) {
            link.blur();
        }
    });
    return true;
}

// "Gesehen": Videos, die zu mindestens 95 % oder bis zum Ende abgespielt wurden
function isSeen(mediaPath) {
    return getData("seen-" + mediaPath) != null;
}

function markSeen(mediaPath) {
    if (mediaPath && !isSeen(mediaPath)) {
        setData("seen-" + mediaPath, Date.now());
    }
}

// Nächstes Medium nach diesem (im selben Ordner oder in der nächsten Staffel), sonst null.
// Bleibt innerhalb derselben Serie (Kategorie + Serienordner) bzw. bei Dateien direkt
// in einer Kategorie (z.B. Filme) innerhalb dieses Ordners.
function getNextMediaPath(mediaPath) {
    var decodedData = buildMediaData(mediaPath);
    var playlist = buildFolderPlaylist(decodedData);
    var nextData = getNeighbourData(decodedData, playlist, getPlaylistIndex(playlist, mediaPath), 1, true);
    if (nextData == null) {
        return null;
    }
    var nextPath = normalizeMediaLocation(getMediaLocationFromDecodedData(nextData));
    var folders = getMediaFolders(mediaPath);
    var groupPrefix = "./" + folders.slice(0, Math.min(2, folders.length)).join("/") + "/";
    if (folders.length <= 1) {
        // Datei direkt in der Kategorie: nur Einträge desselben Ordners
        return getMediaFolders(nextPath).join("/") === folders.join("/") ? nextPath : null;
    }
    return nextPath.startsWith(groupPrefix) ? nextPath : null;
}

// ===================================================================
// Zuletzt gesehen und Favoriten
// ===================================================================

// Funktion zum Updaten der Liste der zuletzt gesehenen Medienelemente
function setLastPlayed(title, url) {
    var mediaPath = getMediaLocationFromDecodedData(decodeUrlData(url));
    if (mediaPath == null || isImage(mediaPath)) {
        return;
    }

    // Pro Serie/Ordner nur einen Eintrag behalten (z.B. ./Serien/Show)
    var groupPath = mediaPath.split("/").slice(0, 3).join("/");
    var currentPlaylist = (getData("playlast") || "").split(listSeperator).filter(function (entry) {
        if (!entry.includes("|")) {
            return false;
        }
        var loggedMediaPath = getMediaLocationFromDecodedData(decodeUrlData(entry.split("|")[1]));
        if (loggedMediaPath == null) {
            return false;
        }
        return loggedMediaPath !== groupPath && !loggedMediaPath.startsWith(groupPath + "/");
    });

    currentPlaylist.unshift(title + "|" + url);
    // Maximal 20 Einträge (einer pro Serie/Ordner)
    setData("playlast", currentPlaylist.slice(0, 20).join(listSeperator));
}

function getFavorites() {
    var favorites = getData("favorites");
    if (favorites == null || favorites == "") {
        return [];
    }
    return favorites.split(listSeperator).filter(function (entry) {
        return entry !== "";
    });
}

// Setzt oder entfernt den Favoritenstern auf allen Kacheln eines Mediums
function updateMenuStar(mediaPath, isFavorite) {
    if (mediaPath == null) {
        return;
    }
    document.querySelectorAll('.card[data-media="' + CSS.escape(mediaPath) + '"]').forEach(function (card) {
        setCardFavorite(card, isFavorite);
    });
}

// Aktuelles Medium als Favorit setzen oder entfernen
function updateFavorites() {
    var mediaPath = normalizeMediaLocation(getMediaLocationFromDecodedData(getDecodedUrlDataFromWindow()));
    if (mediaPath == null) {
        return;
    }

    var favorites = getFavorites();
    var currentIndex = favorites.indexOf(mediaPath);
    var willBeFavorite = currentIndex < 0;

    if (willBeFavorite) {
        favorites.push(mediaPath);
    } else {
        favorites.splice(currentIndex, 1);
    }

    setData("favorites", favorites.join(listSeperator));
    updateFavoriteButtonState(mediaPath);
    updateMenuStar(mediaPath, willBeFavorite);
}

// Funktion zum Aktualisieren des Favoritenbuttons
function updateFavoriteButtonState(mediaPath) {
    var favoriteNode = document.getElementById("favorite");
    var favoriteLink = favoriteNode.querySelector("a");

    if (favoriteLink == null) {
        favoriteLink = document.createElement("a");
        favoriteLink.setAttribute("role", "button");
        favoriteLink.tabIndex = 0;
        favoriteLink.addEventListener("click", updateFavorites);
        favoriteNode.appendChild(favoriteLink);
    }

    var isFavorite = getFavorites().includes(mediaPath);
    favoriteLink.replaceChildren(createIcon(isFavorite ? "starFilled" : "star"), document.createTextNode(isFavorite ? "Gemerkt" : "Merken"));
    favoriteLink.setAttribute("aria-pressed", isFavorite ? "true" : "false");
    favoriteNode.classList.toggle("isFavorite", isFavorite);
}

// ===================================================================
// Playlisten und Navigation
// ===================================================================

function isListPlaylist(playlistName) {
    return listPlaylists.includes(playlistName);
}

// Erstellt eine Playliste aus dem Ordner des Mediums
function buildFolderPlaylist(decodedData) {
    var folder = getDataNode(getLevels(decodedData));
    if (folder == null || typeof folder !== "object") {
        return [];
    }
    return sortNatural(Object.keys(folder).filter(function (key) {
        return key != "Preview" && isMediaFile(allowedMediaExtensions, key);
    }).map(normalizeMediaLocation));
}

// Liest die Playliste aus dem Speicher oder erstellt sie aus dem Datenbaum
function ensurePlaylistExists(decodedData) {
    var playlistName = getPlaylistNameFromDecodedData(decodedData);
    if (playlistName == null) {
        return buildFolderPlaylist(decodedData);
    }

    var storedPlaylist = getData("playlist-" + playlistName);
    if (storedPlaylist != null && storedPlaylist !== "") {
        return sortNatural(storedPlaylist.split(listSeperator).filter(function (entry) {
            return entry !== "";
        }));
    }

    var playlist = buildFolderPlaylist(decodedData);
    setData("playlist-" + playlistName, playlist.join(listSeperator));
    return playlist;
}

function getPlaylistIndex(playlist, mediaPath) {
    return playlist.map(function (entry) {
        return normalizeMediaLocation(entry.split("|")[0]);
    }).indexOf(mediaPath);
}

// Gibt die URL-Daten eines Playlisteneintrags zurück
function getPlaylistEntryData(entry, playlistName) {
    if (entry.includes("|")) {
        return decodeUrlData(entry.split("|")[1]);
    }
    return buildMediaData(normalizeMediaLocation(entry), playlistName);
}

// Funktion, um den letzten/nächsten Unterordner (z.B. Staffel) zu suchen
function swapSeason(decodedData, direction) {
    var levels = getLevels(decodedData);
    if (levels.length == 0) {
        return null;
    }

    var target = levels.length - 1;
    var parent = getDataNode(levels.slice(0, target));
    if (parent == null || typeof parent !== "object") {
        return null;
    }

    // Nur Ordner mit Medien berücksichtigen (keine Preview-Ordner oder Dateien)
    var seasons = sortNatural(Object.keys(parent).filter(function (key) {
        return key != "Preview" && hasMedia(parent[key], false);
    }));

    var currentIndex = seasons.indexOf(levels[target]);
    if (currentIndex < 0) {
        return null;
    }

    var newSeason = seasons[currentIndex + direction];
    if (newSeason === undefined) {
        return null;
    }

    var episodes = sortNatural(Object.keys(parent[newSeason]).filter(function (key) {
        return isMediaFile(allowedMediaExtensions, key);
    }));
    var newEpisode = direction < 0 ? episodes[episodes.length - 1] : episodes[0];
    var newLevels = levels.slice(0, target).concat([newSeason]);

    return buildLevelPath(newLevels) + listSeperator + "MEDIA:" + normalizeMediaLocation(newEpisode) + listSeperator + "PL:" + newLevels.join("");
}

// Gibt die URL-Daten des vorherigen (-1) oder nächsten (1) Mediums zurück.
// ignoreShuffle: immer die tatsächliche Reihenfolge verwenden (z.B. für "Weiterschauen").
function getNeighbourData(decodedData, playlist, currentIndex, direction, ignoreShuffle) {
    if (currentIndex < 0) {
        return null;
    }
    var playlistName = getPlaylistNameFromDecodedData(decodedData);

    // Zufallswiedergabe: kein Rückfall auf die normale Reihenfolge, null = Anfang/Ende erreicht
    if (!ignoreShuffle && isShuffleEnabled()) {
        return getShuffleNeighbour(playlist, currentIndex, direction, playlistName);
    }

    var targetIndex = currentIndex + direction;

    if (targetIndex >= 0 && targetIndex < playlist.length) {
        return getPlaylistEntryData(playlist[targetIndex], playlistName);
    }
    if (isListPlaylist(playlistName)) {
        return null;
    }
    return swapSeason(decodedData, direction);
}

// ===================================================================
// Zufallswiedergabe
// ===================================================================

// Verlauf der laufenden Zufalls-Playliste. Jedes Medium steht nur einmal darin, "position"
// zeigt auf das laufende. "Zurück"/"Weiter" bewegen sich im Verlauf; am Ende des Verlaufs
// kommt ein noch nicht gespieltes Medium. Sind alle gespielt, ist die Playliste zu Ende.
// Der Verlauf wird gespeichert, damit auch nach einem Neuladen nichts doppelt kommt.
var shuffleState = null;

function getShuffleState(playlistName) {
    if (shuffleState == null) {
        try {
            shuffleState = JSON.parse(getData("shuffleState"));
        } catch (error) {
            shuffleState = null;
        }
    }
    if (shuffleState == null || shuffleState.playlist !== playlistName || !Array.isArray(shuffleState.history)) {
        shuffleState = { playlist: playlistName, history: [], position: -1 };
    }
    return shuffleState;
}

function saveShuffleState() {
    setData("shuffleState", JSON.stringify(shuffleState));
}

// Neuer Durchgang, optional mit dem laufenden Medium als erstem Eintrag
function resetShuffleHistory(playlistName, currentPath) {
    shuffleState = { playlist: playlistName, history: currentPath ? [currentPath] : [], position: currentPath ? 0 : -1 };
    saveShuffleState();
}

function isShuffleEnabled() {
    return getData("shuffle") === "true";
}

function setShuffleEnabled(enabled) {
    setData("shuffle", enabled ? "true" : "false");
    updateShuffleButton();
}

// Merkt sich das laufende Medium. Ist es schon im Verlauf (Zurück/Weiter, Browser-Zurück),
// wird nur die Position verschoben.
function recordShuffleHistory(mediaPath, playlistName) {
    if (!isShuffleEnabled() || mediaPath == null) {
        return;
    }
    var state = getShuffleState(playlistName);
    var index = state.history.indexOf(mediaPath);
    if (index < 0) {
        state.history.push(mediaPath);
        index = state.history.length - 1;
    }
    state.position = index;
    saveShuffleState();
}

function getShuffleNeighbour(playlist, currentIndex, direction, playlistName) {
    var paths = playlist.map(function (entry) {
        return normalizeMediaLocation(entry.split("|")[0]);
    });
    var state = getShuffleState(playlistName);
    var position = state.history.indexOf(paths[currentIndex]);
    if (position < 0) {
        recordShuffleHistory(paths[currentIndex], playlistName);
        position = state.history.indexOf(paths[currentIndex]);
    }

    // Im Verlauf zurück bzw. wieder vorwärts
    var step = state.history[position + direction];
    var stepIndex = step === undefined ? -1 : paths.indexOf(step);
    if (stepIndex >= 0) {
        return getPlaylistEntryData(playlist[stepIndex], playlistName);
    }
    if (direction < 0) {
        return null;
    }

    // Neues, in diesem Durchgang noch nicht gespieltes Medium
    var candidates = [];
    paths.forEach(function (path, index) {
        if (index !== currentIndex && state.history.indexOf(path) < 0) {
            candidates.push(index);
        }
    });
    if (candidates.length == 0) {
        return null;
    }
    var pick = candidates[Math.floor(Math.random() * candidates.length)];
    return getPlaylistEntryData(playlist[pick], playlistName);
}

// Zufall-Schalter in der Player-Leiste
function initShuffleButton() {
    var node = document.getElementById("shuffle");
    var link = document.createElement("a");
    link.setAttribute("role", "button");
    link.tabIndex = 0;
    link.title = "Zufallswiedergabe";
    link.appendChild(createIcon("shuffle"));
    var text = document.createElement("span");
    text.className = "shuffleText";
    text.textContent = "Zufall";
    link.appendChild(text);
    link.addEventListener("click", function () {
        var enable = !isShuffleEnabled();
        setShuffleEnabled(enable);
        // Einschalten startet einen neuen Durchgang ab dem laufenden Medium
        if (enable && lastNavigation != null) {
            resetShuffleHistory(getPlaylistNameFromDecodedData(lastNavigation.decodedData), currentMediaLocation);
        }
        // Vor/Weiter neu berechnen
        if (lastNavigation != null) {
            updatePlaylistNavigation(lastNavigation.decodedData, lastNavigation.playlist, lastNavigation.currentIndex);
        }
    });
    node.appendChild(link);
    updateShuffleButton();
}

function updateShuffleButton() {
    var node = document.getElementById("shuffle");
    var link = node == null ? null : node.querySelector("a");
    if (link == null) {
        return;
    }
    var enabled = isShuffleEnabled();
    node.classList.toggle("active", enabled);
    link.setAttribute("aria-pressed", enabled ? "true" : "false");
    link.setAttribute("aria-label", enabled ? "Zufallswiedergabe ausschalten" : "Zufallswiedergabe einschalten");
}

// Ordnerseite: alle Medien des Ordners (inkl. Unterordner) in zufälliger Reihenfolge abspielen
function startShuffleForFolder(levels) {
    var paths = sortNatural(collectMediaPaths(getDataNode(levels), []));
    if (paths.length == 0) {
        return;
    }
    var entries = [];
    paths.forEach(function (path) {
        var url = buildStartUrl(buildMediaData(path, "shuffleList"));
        if (url != null) {
            entries.push(path + "|" + url);
        }
    });
    setData("playlist-shuffleList", entries.join(listSeperator));
    setShuffleEnabled(true);
    resetShuffleHistory("shuffleList", null);
    var first = entries[Math.floor(Math.random() * entries.length)];
    loadDecodedMedia(decodeUrlData(first.split("|")[1]), true);
}

// Funktion zum Erzeugen eines Navigationslinks ohne Seitenreload
function createNavigationLink(targetNodeId, targetData, direction) {
    var mediaPath = getMediaLocationFromDecodedData(targetData);
    if (mediaPath == null) {
        return;
    }

    var link = document.createElement("a");
    if (!setMediaLink(link, targetData)) {
        return;
    }

    var text = document.createElement("span");
    text.className = "buttonText";
    text.textContent = getDisplayName(mediaPath);

    if (direction < 0) {
        link.appendChild(createIcon("chevronLeft"));
        link.appendChild(text);
        link.setAttribute("aria-label", "Vorheriges: " + text.textContent);
    } else {
        link.appendChild(text);
        link.appendChild(createIcon("chevronRight"));
        link.setAttribute("aria-label", "Nächstes: " + text.textContent);
    }

    document.getElementById(targetNodeId).appendChild(link);
}

// Funktion zum Aktualisieren der Vor-/Zurück-Navigation nach jedem Medienwechsel
// Zuletzt berechnete Navigation (für das Umschalten der Zufallswiedergabe)
var lastNavigation = null;

function updatePlaylistNavigation(decodedData, playlist, currentIndex) {
    lastNavigation = { decodedData: decodedData, playlist: playlist, currentIndex: currentIndex };
    ["prev", "next"].forEach(function (nodeId) {
        var node = document.getElementById(nodeId);
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
    });

    var previousData = getNeighbourData(decodedData, playlist, currentIndex, -1);
    if (previousData != null) {
        createNavigationLink("prev", previousData, -1);
    }

    var nextData = getNeighbourData(decodedData, playlist, currentIndex, 1);
    if (nextData != null) {
        createNavigationLink("next", nextData, 1);
    }

    updateViewerArrows();
}

// ===================================================================
// Bildbetrachter: Pfeile im Vollbild (Desktop) und Wischen (Touch)
// ===================================================================

function isImageShown() {
    var player = document.getElementById("video");
    return player != null && player.tagName == "IMG";
}

function initImageViewer() {
    var mediaNode = document.getElementById("media");
    ["prev", "next"].forEach(function (direction) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "viewerArrow " + direction;
        button.setAttribute("aria-label", direction == "prev" ? "Vorheriges Bild" : "Nächstes Bild");
        button.appendChild(createIcon(direction == "prev" ? "chevronLeft" : "chevronRight"));
        button.addEventListener("click", function (event) {
            // Nicht als Klick aufs Bild werten (würde das Vollbild beenden)
            event.stopPropagation();
            clickNavigation(direction);
        });
        mediaNode.appendChild(button);
    });

    // Wischen nach links/rechts wechselt das Bild
    var playerContainer = document.getElementById("playerContainer");
    var touchStart = null;
    playerContainer.addEventListener("touchstart", function (event) {
        touchStart = event.touches.length == 1 && isImageShown()
            ? { x: event.touches[0].clientX, y: event.touches[0].clientY }
            : null;
    }, { passive: true });
    playerContainer.addEventListener("touchend", function (event) {
        if (touchStart == null) {
            return;
        }
        var touch = event.changedTouches[0];
        var distanceX = touch.clientX - touchStart.x;
        var distanceY = touch.clientY - touchStart.y;
        touchStart = null;
        // Deutlich waagrecht und mindestens 50px, damit normales Scrollen nicht auslöst
        if (Math.abs(distanceX) > 50 && Math.abs(distanceX) > Math.abs(distanceY) * 1.5) {
            clickNavigation(distanceX < 0 ? "next" : "prev");
        }
    });
}

// Pfeile nur zeigen, wenn es ein vorheriges/nächstes Medium gibt
function updateViewerArrows() {
    ["prev", "next"].forEach(function (direction) {
        var button = document.querySelector(".viewerArrow." + direction);
        if (button != null) {
            button.classList.toggle("unavailable", document.querySelector("#" + direction + " a") == null);
        }
    });
}

// Funktion zum Aktualisieren des Medientitels
function updateMediaTitle(mediaName) {
    var title = document.getElementById("mediaTitle");
    title.textContent = stripExtension(mediaName);
    document.title = "STEFFLIX - " + stripExtension(mediaName);
}

// Scrollt so, dass der Player direkt unter der Kopfzeile steht
function scrollToPlayer(smooth) {
    var mediaNode = document.getElementById("media");
    var headerHeight = document.querySelector(".firstLevelWrap").getBoundingClientRect().height;
    var top = mediaNode.getBoundingClientRect().top + window.scrollY - headerHeight - 8;
    window.scrollTo({ top: Math.max(0, top), behavior: smooth ? "smooth" : "auto" });
}

// ===================================================================
// Medienwiedergabe
// ===================================================================

// Erstellt das passende Element für Bild, Video oder Musik
function createMediaElement(mediaUrl) {
    var element;
    var wrapper;

    if (isImage(mediaUrl)) {
        wrapper = document.createElement("a");
        wrapper.id = "imageLink";
        wrapper.addEventListener("click", toggleFullscreenMode);
        element = document.createElement("img");
        element.id = "video";
        element.addEventListener("error", handleMediaError);
        wrapper.appendChild(element);
        return { wrapper: wrapper, element: element };
    }

    if (isMusic(mediaUrl)) {
        element = document.createElement("audio");
        element.className = "audioPlayer";
    } else {
        element = document.createElement("video");
    }
    element.id = "video";
    element.autoplay = true;
    element.controls = true;

    // Gespeicherte Lautstärke laden
    var savedVolume = parseFloat(getData("mediaVolume"));
    if (isNaN(savedVolume)) {
        savedVolume = 1;
    } else if (savedVolume == 0) {
        savedVolume = 0.2;
    }
    element.volume = Math.min(1, Math.max(0, savedVolume));

    element.addEventListener("error", handleMediaError);
    appendMediafunctions(element);
    return { wrapper: element, element: element };
}

// Funktion um das Media-Tag entsprechend dem Medientyp anzupassen.
// Der Player liegt immer allein in #playerContainer.
function adjustMediaType(mediaUrl, node) {
    var wantedTag = isImage(mediaUrl) ? "IMG" : (isMusic(mediaUrl) ? "AUDIO" : "VIDEO");
    if (node != null && node.tagName == wantedTag) {
        return node;
    }

    var created = createMediaElement(mediaUrl);
    document.getElementById("playerContainer").replaceChildren(created.wrapper);
    return created.element;
}

function removePlayer() {
    document.getElementById("playerContainer").replaceChildren();
}

// ===================================================================
// Hinweise bei fehlenden oder defekten Medien
// ===================================================================

function showMediaMessage(text, linkHref, linkText) {
    var node = document.getElementById("mediaMessage");
    node.replaceChildren(document.createTextNode(text));
    if (linkHref) {
        var link = document.createElement("a");
        link.className = "button";
        link.href = linkHref;
        link.textContent = linkText;
        node.appendChild(document.createElement("br"));
        node.appendChild(link);
    }
    node.style.display = "block";
}

function hideMediaMessage() {
    var node = document.getElementById("mediaMessage");
    node.replaceChildren();
    node.style.display = "none";
}

// Bei aktivem Autoplay nach kurzer Pause zum nächsten Medium springen
function skipAfterError() {
    if (document.getElementById("autoplayCheckBox").checked && document.querySelector("#next a") != null) {
        timeoutArray.push(setTimeout(function () {
            clickNavigation("next");
        }, 5000));
    }
}

// Datei steht in data.js, lässt sich aber nicht laden (gelöscht, verschoben, Format)
function handleMediaError(event) {
    var player = event.target;
    if (player.dataset.mediaPath !== currentMediaLocation) {
        return;
    }
    showMediaMessage("«" + stripExtension(getBaseName(currentMediaLocation)) + "» konnte nicht geladen werden. " +
        "Die Datei wurde eventuell verschoben, umbenannt oder gelöscht, oder das Format wird vom Browser nicht unterstützt. " +
        "Bitte updateData.exe ausführen.");
    skipAfterError();
}

// Entfernt ein Medium aus der Liste "Weiterschauen"
function removeFromLastPlayed(mediaPath) {
    var lastPlayed = (getData("playlast") || "").split(listSeperator).filter(function (entry) {
        if (!entry.includes("|")) {
            return false;
        }
        return normalizeMediaLocation(getMediaLocationFromDecodedData(decodeUrlData(entry.split("|")[1]))) !== mediaPath;
    });
    setData("playlast", lastPlayed.join(listSeperator));
}

// ===================================================================
// Zeitstempel (Wiedergabeposition)
// ===================================================================

// Zeitstempel werden pro Pfad innerhalb von STEFFLIX gespeichert (./Serien/Show/S01/S01E01 - Titel.mp4).
// Der Pfad ist relativ, damit die Positionen beim Verschieben des STEFFLIX-Ordners erhalten bleiben.
function getTimestampKey(player) {
    return "timestamp-" + (player.dataset.mediaPath || "");
}

// Bis Version 1.42 wurde nur der Dateiname ohne Endung verwendet
function getLegacyTimestampKey(player) {
    return "timestamp-" + stripExtension(getBaseName(replaceSpecialChars(player.src)).split("#")[0]);
}

// Liest den Zeitstempel und übernimmt einmalig einen alten Wert
function readTimestamp(player) {
    var timestamp = getData(getTimestampKey(player));
    if (timestamp != null) {
        return timestamp;
    }
    var legacyKey = getLegacyTimestampKey(player);
    var legacyTimestamp = getData(legacyKey);
    if (legacyTimestamp != null) {
        setData(getTimestampKey(player), legacyTimestamp);
        removeData(legacyKey);
    }
    return legacyTimestamp;
}

// Lädt ein Medium anhand der URL-Daten. Wird beim Seitenaufruf und bei jedem
// Medienwechsel ohne Seitenreload verwendet.
function loadDecodedMedia(decodedData, pushState) {
    timeoutArray = cancelTimeouts();

    var medialocation = normalizeMediaLocation(getMediaLocationFromDecodedData(decodedData));
    if (medialocation == null) {
        return;
    }

    var wasShowingMedia = currentMediaLocation != null;
    currentMediaLocation = medialocation;
    var playlistName = getPlaylistNameFromDecodedData(decodedData);
    recordShuffleHistory(medialocation, playlistName);
    var localPlaylist = ensurePlaylistExists(decodedData);
    var currentIndex = getPlaylistIndex(localPlaylist, medialocation);

    // Veraltete Ordner-Playliste neu aus den Daten erstellen
    if (currentIndex < 0 && playlistName != null && !isListPlaylist(playlistName)) {
        localPlaylist = buildFolderPlaylist(decodedData);
        currentIndex = getPlaylistIndex(localPlaylist, medialocation);
    }

    var mediaName = getBaseName(medialocation).split("#")[0];
    updateMediaTitle(mediaName);
    hideMediaMessage();

    document.getElementById("media").style.display = "block";

    // Bänder unter dem Player (Ordnerinhalt, Nachbarordner) und laufendes Medium markieren
    renderMediaContext(medialocation);

    // Von einer Ordnerseite aus gestartet: zum Player scrollen
    if (pushState && !wasShowingMedia) {
        requestAnimationFrame(function () {
            scrollToPlayer(true);
        });
    }

    var startUrl = buildStartUrl(decodedData);
    var node = null;

    if (mediaExists(medialocation)) {
        node = adjustMediaType(medialocation, document.getElementById("video"));
        if (node.tagName == "IMG") {
            node.alt = stripExtension(mediaName);
        }
        node.dataset.mediaPath = medialocation;
        if (node.getAttribute("src") !== medialocation) {
            node.src = medialocation;
        }
        setLastPlayed(mediaName, startUrl);
    } else {
        // Medium nicht (mehr) in data.js: Hinweis statt leerem Player
        removePlayer();
        removeFromLastPlayed(medialocation);
        var folderLevels = getLevels(decodedData);
        var folderUrl = getDataNode(folderLevels) != null ? buildStartUrl(buildLevelPath(folderLevels)) : "start.html";
        showMediaMessage("«" + stripExtension(mediaName) + "» wurde nicht gefunden. " +
            "Die Datei wurde eventuell verschoben, umbenannt oder gelöscht. " +
            "Falls die Datei noch existiert, bitte updateData.exe ausführen.", folderUrl, "Zum Ordner");
    }

    if (pushState) {
        history.pushState({}, "", startUrl);
    }

    if (playlistName != null) {
        setData("currentPrefix", playlistName);
        setData("playlist-" + playlistName, localPlaylist.join(listSeperator));
    }

    updateFavoriteButtonState(medialocation);
    // Fehlende Medien können nicht gemerkt werden
    document.getElementById("favorite").style.visibility = node == null ? "hidden" : "";
    document.getElementById("mediaNav").style.display = "flex";
    updatePlaylistNavigation(decodedData, localPlaylist, currentIndex);

    var mediaNode = document.getElementById("media");
    var nextNode = document.getElementById("next");

    if (node == null) {
        skipAfterError();
    }

    if (document.getElementById("autoplayCheckBox").checked) {
        mediaNode.classList.add("autoplaying");

        if (node != null && node.tagName == "IMG") {
            updateOptions(document.getElementById("autoplayCheckBox").checked, document.getElementById("amountOfAutoplay").value, document.getElementById("autoplayDuration").value);
            if (nextNode.childNodes.length > 0 && nextNode.childNodes[0].textContent != "") {
                nextNode.style.animation = "none";
                timeoutArray.push(setTimeout(function () {
                    nextNode.style.animation = "autoplayLoading " + (autoplayDuration - 1 + minTime) + "s";
                }, 1000));
                timeoutArray.push(setTimeout(function () {
                    clickNavigation("next");
                }, (autoplayDuration + minTime) * 1000));
            } else {
                updateOptions(false, 0, document.getElementById("autoplayDuration").value);
            }
        }
    } else {
        mediaNode.classList.remove("autoplaying");
    }
}

function clickNavigation(nodeId) {
    var link = document.querySelector("#" + nodeId + " a");
    if (link != null) {
        link.click();
    }
}

// Funktion, um Subroutinen an das Medienelement anzuhängen
function appendMediafunctions(node) {
    node.onloadedmetadata = function () {
        node.autoplay = true;
        var playRequest = node.play();
        if (playRequest && playRequest.catch) {
            // Browser blockiert Autoplay: Nutzer startet selbst
            playRequest.catch(function () {});
        }
        node.setAttribute("preload", "auto");
        if (!isPopStateNavigation) {
            scrollToPlayer(true);
        }
        // Dauer für den Fortschrittsbalken auf der Startseite speichern
        if (isFinite(this.duration) && this.duration > 0 && node.dataset.mediaPath) {
            setData("timestampDuration-" + node.dataset.mediaPath, Math.round(this.duration));
        }
        // Lese Zeitstempel des ausgewählten Mediums aus
        var pastTimestamp = readTimestamp(node);
        if (pastTimestamp != null) {
            // Prüfe Zeitstempel für Medien die länger als 10 Minuten sind
            if (this.duration >= 600 && 100 / this.duration * pastTimestamp < 95) {
                // Setze Abspielzeitpunkt auf Zeitstempel
                node.currentTime = pastTimestamp;
            } else {
                // Lösche Zeitstempel
                removeData(getTimestampKey(node));
            }
        }
    };

    // Zu Ende abgespielte Videos gelten als gesehen
    node.addEventListener("ended", function () {
        if (node.tagName == "VIDEO") {
            markSeen(node.dataset.mediaPath);
        }
    });

    // Lese aktuellen Zeitstempel im Video aus
    node.addEventListener("timeupdate", function () {
        if (!isFinite(this.duration) || this.duration <= 0) {
            return;
        }
        var player = node;
        var nextNode = document.getElementById("next");
        var timestampKey = getTimestampKey(player);

        var currentTime = parseInt(this.currentTime, 10);
        var currentTimestamp = getData(timestampKey);
        if (currentTimestamp == null) {
            currentTimestamp = 0;
        }
        // Speichere Videoposition alle 10 Sek
        if (currentTime != 0 && currentTimestamp != currentTime && currentTime % 10 == 0) {
            setData(timestampKey, currentTime);
            setData("mediaVolume", player.volume);
        }

        // Videos ab 95 % als gesehen markieren
        if (player.tagName == "VIDEO" && this.currentTime / this.duration >= 0.95) {
            markSeen(player.dataset.mediaPath);
        }

        // Unterscheide zwischen Audio- und Video-Elementen
        var autoplayChecked = document.getElementById("autoplayCheckBox").checked;
        var videoCheck = player.tagName == "VIDEO" && autoplayChecked && this.duration - currentTime <= (autoplayDuration + minTime);
        var audioCheck = player.tagName == "AUDIO" && autoplayChecked && this.duration - currentTime <= 11;
        if (videoCheck || audioCheck) {
            var remaining = this.duration - currentTime;
            var startAnimation = autoplayDuration + minTime;
            var prozent = ((startAnimation - remaining) / minTime) * 100;
            nextNode.style.background = "linear-gradient(to right, var(--sflix-accent) " + Math.min(100, Math.max(0, prozent)) + "%, var(--sflix-surface) 0%)";

            if (this.duration - currentTime <= autoplayDuration + minTime - 10) {
                nextNode.style.background = "";
                updateOptions(autoplayChecked, document.getElementById("amountOfAutoplay").value, document.getElementById("autoplayDuration").value);
                if (nextNode.childNodes.length > 0 && nextNode.childNodes[0].textContent != "") {
                    removeData(timestampKey);
                    // Autoplay springt vor dem Ende weiter: die Folge gilt trotzdem als gesehen
                    if (player.tagName == "VIDEO") {
                        markSeen(player.dataset.mediaPath);
                    }
                    clickNavigation("next");
                } else {
                    updateOptions(false, 0, document.getElementById("autoplayDuration").value);
                }
            }
        }
    });
}

// Funktion, um in den Vollbildmodus zu wechseln (Bilder)
function toggleFullscreenMode() {
    var node = document.getElementById("media");
    if (!node.classList.contains("fullscreened")) {
        node.classList.add("fullscreened");
        document.getElementById("mediaTitle").style.display = "none";
    } else {
        node.classList.remove("fullscreened");
        document.getElementById("mediaTitle").style.display = null;
        setTimeout(function () {
            scrollToPlayer(false);
        }, 400);
    }
}
