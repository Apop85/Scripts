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
            preloadNeighbourImages();
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
        lastNavigation.prevPath = normalizeMediaLocation(getMediaLocationFromDecodedData(previousData));
    }

    var nextData = getNeighbourData(decodedData, playlist, currentIndex, 1);
    if (nextData != null) {
        createNavigationLink("next", nextData, 1);
        lastNavigation.nextPath = normalizeMediaLocation(getMediaLocationFromDecodedData(nextData));
    }

    updateViewerArrows();
    updatePlayerNavButtons();
}

// ===================================================================
// Nächstes und vorheriges Bild vorladen
// ===================================================================

// Das nächste und das vorherige Medium stehen schon fest (auch in der Zufallswiedergabe, wo das
// nächste beim Anzeigen des laufenden Mediums ausgewählt wird). Sind es Bilder, werden sie im
// Hintergrund geladen und dekodiert, damit sie beim Weiter- bzw. Zurückschalten sofort aus dem
// Speicher des Browsers kommen. Aufruf erst, wenn das laufende Medium angezeigt wird
// (renderMediaContext), damit das Vorladen dem laufenden Bild keine Zeit wegnimmt.
// Zuerst das nächste Bild (häufigere Richtung), dann das vorherige. Höchstens diese zwei.
var preloadedImages = {};

function startImagePreload(path) {
    var image = new Image();
    image.decoding = "async";
    image.fetchPriority = "low";
    image.addEventListener("load", function () {
        if (preloadedImages[path] === image && image.decode) {
            image.decode().catch(function () {});
        }
    });
    // Gleiche Schreibweise wie node.src in loadDecodedMedia, damit der Browser die Datei wiedererkennt
    image.src = path;
    return image;
}

function preloadNeighbourImages() {
    var wanted = [];
    if (lastNavigation != null) {
        [lastNavigation.nextPath, lastNavigation.prevPath].forEach(function (path) {
            if (path != null && path !== currentMediaLocation && wanted.indexOf(path) < 0 && isImage(path) && mediaExists(path)) {
                wanted.push(path);
            }
        });
    }
    // Nicht mehr benötigte Vorladungen abbrechen. Ein Bild, das inzwischen angezeigt wird,
    // nicht abbrechen (gleiche Datei wie im Player).
    Object.keys(preloadedImages).forEach(function (path) {
        if (wanted.indexOf(path) < 0) {
            if (path !== currentMediaLocation) {
                preloadedImages[path].removeAttribute("src");
            }
            delete preloadedImages[path];
        }
    });
    wanted.forEach(function (path) {
        if (preloadedImages[path] == null) {
            preloadedImages[path] = startImagePreload(path);
        }
    });
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
    var playerTitle = getPlayerTitle(currentMediaLocation, stripExtension(mediaName));
    title.textContent = playerTitle;
    // Browser-Tab mit vollem Namen
    document.title = "STEFFLIX - " + stripExtension(mediaName);
    updatePlayerTitle(playerTitle);
}

// Titel im Player ohne vorangestellten Serien- bzw. Ordnernamen (steht schon in der Pfadleiste):
// "Die Simpsons - S01E01 - Folgentitel" -> "S01E01 - Folgentitel". Der Kategorieordner
// (z.B. "Serien") zählt nicht. Ohne Bindestrich nach dem Namen bleibt der Titel unverändert.
function getPlayerTitle(mediaPath, name) {
    var folders = getMediaFolders(mediaPath || "");
    for (var depth = folders.length - 1; depth >= 1; depth--) {
        var prefix = folders[depth];
        if (name.length > prefix.length && name.toLowerCase().startsWith(prefix.toLowerCase())) {
            var after = name.slice(prefix.length);
            var rest = after.replace(/^\s*-\s*/, "");
            if (rest !== after && rest.trim() !== "") {
                return rest;
            }
        }
    }
    return name;
}

// Scrollt so, dass der Player direkt unter der Kopfzeile steht
function scrollToPlayer(smooth) {
    // Vollbild: Kopfzeile ist ausgeblendet, das Video beginnt ganz oben
    if (isCinemaMode()) {
        var container = document.getElementById("playerContainer");
        window.scrollTo({ top: container.getBoundingClientRect().top + window.scrollY, behavior: smooth ? "smooth" : "auto" });
        return;
    }
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
        // Vorrang vor den Vorschaubildern in den Bändern
        element.fetchPriority = "high";
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
    // Eigene Steuerung (buildPlayerShell), ausser die Browser-Steuerung ist eingestellt
    element.controls = useNativeControls();
    element.setAttribute("playsinline", "");

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
    return { wrapper: buildPlayerShell(element), element: element };
}

// Funktion um das Media-Tag entsprechend dem Medientyp anzupassen.
// Der Player liegt immer allein in #playerContainer.
function adjustMediaType(mediaUrl, node) {
    var wantedTag = isImage(mediaUrl) ? "IMG" : (isMusic(mediaUrl) ? "AUDIO" : "VIDEO");
    if (node != null && node.tagName == wantedTag) {
        return node;
    }

    // Vollbild beim Wechsel zwischen Bild und Video beibehalten (der Browser bleibt im Vollbild),
    // Musik hat kein Vollbild
    if (wantedTag == "IMG" && isCinemaMode()) {
        document.documentElement.classList.remove("playerCinema");
        document.getElementById("media").classList.add("fullscreened");
        document.getElementById("mediaTitle").style.display = "none";
    } else if (wantedTag == "VIDEO" && isImageFullscreen()) {
        document.getElementById("media").classList.remove("fullscreened");
        document.getElementById("mediaTitle").style.display = null;
        document.documentElement.classList.add("playerCinema");
    } else if (wantedTag == "AUDIO") {
        exitCinemaMode();
        setImageFullscreen(false);
    }
    var created = createMediaElement(mediaUrl);
    document.getElementById("playerContainer").replaceChildren(created.wrapper);
    if (wantedTag != "IMG") {
        syncPlayerState();
    }
    return created.element;
}

function removePlayer() {
    exitCinemaMode();
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
            if (node.tagName != "IMG") {
                onPlayerMediaChanged(node);
            }
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
    // Video/Musik zeigen Autoplay in der eigenen Leiste, Bilder weiterhin unter dem Bild
    mediaNode.classList.toggle("hasPlayerControls", node != null && node.tagName != "IMG");

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
            // Gewechselt wird bei "Dauer bis Übergang" + 1 Sekunde vor dem Ende
            showAutoplayCountdown(Math.ceil(remaining - (autoplayDuration + minTime - 10)), prozent);

            if (this.duration - currentTime <= autoplayDuration + minTime - 10) {
                nextNode.style.background = "";
                hideAutoplayCountdown();
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
        } else {
            hideAutoplayCountdown();
        }
    });
}

// ===================================================================
// Eigene Mediensteuerung (Video und Musik)
// ===================================================================

// Wählbare Geschwindigkeiten (nur über das Zahnradmenü, jedes Medium startet mit 1x)
var playbackRates = [0.5, 1, 1.25, 1.5, 1.75];
// Nach so vielen Millisekunden ohne Bewegung blendet sich die Leiste aus (nur Video)
var controlsHideDelay = 2500;
// Doppeltipp: maximaler Abstand zwischen zwei Tipps
var doubleTapDelay = 300;
// Zustand des aktuellen Players (Elemente, Timer)
var playerUI = null;
// Vollbild wurde von STEFFLIX angefordert (und nicht z.B. per F11)
var cinemaRequestedFullscreen = false;

// Browser-Steuerung statt eigener Steuerung (Einstellung)
function useNativeControls() {
    return getData("nativeControls") === "true";
}

// Liefert den Player-Zustand, wenn er zum aktuell angezeigten Medium gehört
function getPlayerUI() {
    var media = document.getElementById("video");
    return playerUI != null && playerUI.media === media ? playerUI : null;
}

function createPlayerElement(tagName, className, text) {
    var element = document.createElement(tagName);
    if (className) {
        element.className = className;
    }
    if (text != null) {
        element.textContent = text;
    }
    return element;
}

function createControlButton(className, iconName, label) {
    var button = createPlayerElement("button", "ctrlButton " + className);
    button.type = "button";
    setControlButton(button, iconName, label);
    return button;
}

function setControlButton(button, iconName, label) {
    if (button.dataset.icon !== iconName) {
        button.replaceChildren(createIcon(iconName));
        button.dataset.icon = iconName;
    }
    button.setAttribute("aria-label", label);
    button.title = label;
}

// 75 -> 1:15, 3725 -> 1:02:05
function formatMediaTime(seconds) {
    if (!isFinite(seconds) || seconds < 0) {
        seconds = 0;
    }
    seconds = Math.floor(seconds);
    var hours = Math.floor(seconds / 3600);
    var minutes = Math.floor((seconds % 3600) / 60);
    var rest = seconds % 60;
    var minuteText = hours > 0 ? String(minutes).padStart(2, "0") : String(minutes);
    return (hours > 0 ? hours + ":" : "") + minuteText + ":" + String(rest).padStart(2, "0");
}

function hasMediaDuration(media) {
    return isFinite(media.duration) && media.duration > 0;
}

// Baut den Player rund um das Video- bzw. Audio-Element
function buildPlayerShell(media) {
    var isAudio = media.tagName == "AUDIO";
    var shell = createPlayerElement("div", "sflixPlayer " + (isAudio ? "isAudio" : "isVideo") + " controlsVisible");
    var ui = {
        shell: shell, media: media, isAudio: isAudio,
        hideTimer: null, clickTimer: null, tapTimer: null, rippleTimer: null,
        lastTap: null, lastTouchTime: 0, dragging: false, rippleAmount: 0, frame: null
    };
    shell.appendChild(media);

    if (!isAudio) {
        // Klickfläche über dem Video: Klick = Pause, Doppelklick = Vollbild, Tipp = Leiste ein/aus
        ui.surface = createPlayerElement("div", "playerSurface");
        ui.top = createPlayerElement("div", "playerTop");
        ui.title = createPlayerElement("span", "playerTitle");
        ui.top.appendChild(ui.title);
        ui.center = createPlayerElement("div", "playerCenter");
        ui.bigPlay = createPlayerElement("span", "bigPlay");
        ui.bigPlay.appendChild(createIcon("play"));
        ui.spinner = createPlayerElement("span", "playerSpinner");
        ui.center.append(ui.bigPlay, ui.spinner);
        ui.ripple = createPlayerElement("div", "seekRipple");
        shell.append(ui.surface, ui.top, ui.center, ui.ripple);
    } else {
        // Musik: Vorschaubild über der Leiste (unscharfe Kopie füllt den Hintergrund)
        ui.artwork = createPlayerElement("div", "audioArtwork");
        ui.artwork.hidden = true;
        ui.artworkBackdrop = createPlayerElement("img", "artworkBackdrop");
        ui.artworkImage = createPlayerElement("img", "artworkImage");
        ui.artworkBackdrop.alt = "";
        ui.artworkImage.alt = "";
        ui.artwork.append(ui.artworkBackdrop, ui.artworkImage);
        shell.appendChild(ui.artwork);
    }

    // Autoplay-Countdown: "Nächstes in 8 s"
    ui.countdown = createPlayerElement("div", "autoplayCountdown");
    ui.countdown.hidden = true;
    ui.countdownNext = createPlayerElement("button", "countdownNext");
    ui.countdownNext.type = "button";
    ui.countdownFill = createPlayerElement("span", "countdownFill");
    ui.countdownLabel = createPlayerElement("span", "countdownLabel");
    ui.countdownName = createPlayerElement("span", "countdownName");
    ui.countdownNext.append(ui.countdownFill, ui.countdownLabel, ui.countdownName);
    ui.countdownCancel = createControlButton("countdownCancel", "close", "Autoplay ausschalten");
    ui.countdown.append(ui.countdownNext, ui.countdownCancel);

    // Steuerleiste
    ui.controls = createPlayerElement("div", "playerControls");
    ui.progress = createPlayerElement("div", "playerProgress");
    ui.progress.tabIndex = 0;
    ui.progress.setAttribute("role", "slider");
    ui.progress.setAttribute("aria-label", "Wiedergabeposition");
    ui.progress.setAttribute("aria-valuemin", "0");
    var track = createPlayerElement("div", "progressTrack");
    ui.progressBuffered = createPlayerElement("div", "progressBuffered");
    ui.progressPlayed = createPlayerElement("div", "progressPlayed");
    ui.progressThumb = createPlayerElement("div", "progressThumb");
    track.append(ui.progressBuffered, ui.progressPlayed, ui.progressThumb);
    ui.progressTooltip = createPlayerElement("div", "progressTooltip");
    ui.progress.append(track, ui.progressTooltip);

    var row = createPlayerElement("div", "controlRow");
    var left = createPlayerElement("div", "controlGroup");
    var right = createPlayerElement("div", "controlGroup controlGroupRight");

    ui.prevButton = createControlButton("ctrlPrev", "skipPrev", "Vorheriges (P)");
    ui.playButton = createControlButton("ctrlPlay", "play", "Abspielen (K)");
    ui.nextButton = createControlButton("ctrlNext", "skipNext", "Nächstes (N)");
    ui.backButton = createControlButton("ctrlSeek", "back10", "10 Sekunden zurück (Pfeil links)");
    ui.forwardButton = createControlButton("ctrlSeek", "forward10", "10 Sekunden vor (Pfeil rechts)");

    ui.volumeGroup = createPlayerElement("div", "volumeGroup");
    ui.muteButton = createControlButton("ctrlMute", "volumeHigh", "Ton aus (M)");
    ui.volumeSlider = createPlayerElement("input", "volumeSlider");
    ui.volumeSlider.type = "range";
    ui.volumeSlider.min = "0";
    ui.volumeSlider.max = "1";
    ui.volumeSlider.step = "0.05";
    ui.volumeSlider.setAttribute("aria-label", "Lautstärke");
    ui.volumeGroup.append(ui.muteButton, ui.volumeSlider);

    ui.time = createPlayerElement("span", "timeDisplay", "0:00 / 0:00");
    left.append(ui.prevButton, ui.playButton, ui.nextButton, ui.backButton, ui.forwardButton, ui.volumeGroup, ui.time);

    ui.autoplayButton = createControlButton("ctrlAutoplay", "autoplay", "Autoplay");
    ui.settingsButton = createControlButton("ctrlSettings", "gear", "Einstellungen");
    [ui.autoplayButton, ui.settingsButton].forEach(function (button) {
        button.setAttribute("aria-haspopup", "true");
        button.setAttribute("aria-expanded", "false");
    });
    right.append(ui.autoplayButton, ui.settingsButton);
    if (!isAudio) {
        ui.pipButton = createControlButton("ctrlPip", "pip", "Bild im Bild");
        ui.fullscreenButton = createControlButton("ctrlFullscreen", "fullscreen", "Vollbild (F)");
        // Firefox und Safari auf dem iPhone kennen die Bild-im-Bild-Schnittstelle nicht
        ui.pipButton.hidden = !document.pictureInPictureEnabled;
        right.append(ui.pipButton, ui.fullscreenButton);
    }
    row.append(left, right);
    ui.controls.append(ui.progress, row);

    // Zwei Menüs: Autoplay (Autoplay-Symbol) und Einstellungen (Zahnrad). Es ist immer nur eines offen.
    ui.autoplayMenu = buildAutoplayMenu(ui);
    ui.settingsMenu = buildSettingsMenu(ui);
    ui.menus = [
        { menu: ui.autoplayMenu, button: ui.autoplayButton },
        { menu: ui.settingsMenu, button: ui.settingsButton }
    ];
    shell.append(ui.countdown, ui.controls, ui.autoplayMenu, ui.settingsMenu);

    playerUI = ui;
    bindPlayerEvents(ui);
    applyNativeControls(ui, useNativeControls());
    updatePlayButton(ui);
    updateVolumeControls(ui);
    return shell;
}

// Zustand übernehmen, sobald der Player in der Seite ist (getPlayerUI braucht das Element in der Seite)
function syncPlayerState() {
    updatePlayerAutoplayState();
    updatePlayerNavButtons();
    updateFullscreenButton();
}

function createPlayerMenu(label) {
    var menu = createPlayerElement("div", "playerMenu");
    menu.hidden = true;
    menu.setAttribute("role", "dialog");
    menu.setAttribute("aria-label", label);
    return menu;
}

function addMenuSwitch(menu, label, hint) {
    var row = createPlayerElement("label", "settingRow");
    var text = createPlayerElement("span", null, label);
    if (hint) {
        text.appendChild(createPlayerElement("small", null, hint));
    }
    var input = createPlayerElement("input");
    input.type = "checkbox";
    row.append(text, input);
    menu.appendChild(row);
    return input;
}

function addMenuNumber(menu, label) {
    var row = createPlayerElement("label", "settingRow");
    var input = createPlayerElement("input");
    input.type = "number";
    input.min = "0";
    row.append(createPlayerElement("span", null, label), input);
    menu.appendChild(row);
    return input;
}

// Autoplay-Menü (Klick auf das Autoplay-Symbol). Änderungen gelten sofort und
// landen in denselben Einstellungen wie im Fenster "Autoplay" des Hauptmenüs.
function buildAutoplayMenu(ui) {
    var menu = createPlayerMenu("Autoplay-Einstellungen");
    menu.appendChild(createPlayerElement("div", "playerMenuHeading", "Autoplay"));
    ui.menuAutoplay = addMenuSwitch(menu, "Automatisch vorwärts");
    ui.menuInfinite = addMenuSwitch(menu, "Unbegrenzt", "Bis zum Ende der Playliste");
    ui.menuAmount = addMenuNumber(menu, "Anzahl abzuspielender Medien");
    ui.menuDuration = addMenuNumber(menu, "Dauer bis Übergang (Sekunden)");

    ui.menuAutoplay.addEventListener("change", function () {
        document.getElementById("autoplayCheckBox").checked = ui.menuAutoplay.checked;
        checkAutoplayAmount();
        commitAutoplaySettings();
    });
    ui.menuInfinite.addEventListener("change", function () {
        document.getElementById("autoplayInfinite").checked = ui.menuInfinite.checked;
        updateAutoplayAmountState();
        checkAutoplayAmount();
        commitAutoplaySettings();
    });
    ui.menuAmount.addEventListener("change", function () {
        var amount = Math.max(0, parseInt(ui.menuAmount.value) || 0);
        document.getElementById("amountOfAutoplay").value = amount;
        if (!isAutoplayInfinite()) {
            document.getElementById("autoplayCheckBox").checked = amount > 0;
        }
        commitAutoplaySettings();
    });
    ui.menuDuration.addEventListener("change", function () {
        document.getElementById("autoplayDuration").value = Math.max(0, parseInt(ui.menuDuration.value) || 0);
        commitAutoplaySettings();
    });
    return menu;
}

// Zahnradmenü: Geschwindigkeit, Download, Browser-Steuerung
function buildSettingsMenu(ui) {
    var menu = createPlayerMenu("Player-Einstellungen");
    menu.appendChild(createPlayerElement("div", "playerMenuHeading", "Geschwindigkeit"));
    ui.speedButtons = [];
    var speedRow = createPlayerElement("div", "speedOptions");
    speedRow.setAttribute("role", "group");
    speedRow.setAttribute("aria-label", "Wiedergabegeschwindigkeit");
    playbackRates.forEach(function (rate) {
        var button = createPlayerElement("button", "speedOption", String(rate).replace(".", ",") + "x");
        button.type = "button";
        button.dataset.rate = rate;
        button.addEventListener("click", function () {
            ui.media.playbackRate = rate;
        });
        ui.speedButtons.push(button);
        speedRow.appendChild(button);
    });
    menu.appendChild(speedRow);

    menu.appendChild(createPlayerElement("div", "playerMenuHeading", "Weitere"));
    ui.downloadLink = createPlayerElement("a", "playerMenuLink");
    ui.downloadLink.append(createIcon("download"), createPlayerElement("span", null, "Herunterladen"));
    menu.appendChild(ui.downloadLink);
    ui.menuNative = addMenuSwitch(menu, "Browser-Steuerung verwenden", "Statt der STEFFLIX-Steuerung");

    ui.menuNative.addEventListener("change", function () {
        setNativeControls(ui.menuNative.checked);
    });
    return menu;
}

function bindPlayerEvents(ui) {
    var media = ui.media;

    // Wiedergabe
    ui.playButton.addEventListener("click", function () {
        togglePlayback(ui);
    });
    ui.prevButton.addEventListener("click", function () {
        clickNavigation("prev");
    });
    ui.nextButton.addEventListener("click", function () {
        clickNavigation("next");
    });
    ui.backButton.addEventListener("click", function () {
        seekPlayerBy(ui, -10);
    });
    ui.forwardButton.addEventListener("click", function () {
        seekPlayerBy(ui, 10);
    });

    // Lautstärke
    ui.muteButton.addEventListener("click", function () {
        if (media.muted || media.volume == 0) {
            media.muted = false;
            if (media.volume == 0) {
                media.volume = 0.2;
            }
        } else {
            media.muted = true;
        }
    });
    ui.volumeSlider.addEventListener("input", function () {
        var volume = parseFloat(ui.volumeSlider.value);
        media.volume = volume;
        media.muted = volume == 0;
    });

    ui.countdownNext.addEventListener("click", function () {
        clickNavigation("next");
    });
    ui.countdownCancel.addEventListener("click", function () {
        setAutoplayFromPlayer(false);
    });

    // Autoplay-Symbol öffnet das Autoplay-Menü, das Zahnrad die übrigen Einstellungen
    ui.menus.forEach(function (entry) {
        entry.button.addEventListener("click", function () {
            togglePlayerMenu(ui, entry.menu, entry.menu.hidden);
        });
    });
    // Klick neben das Menü schliesst es. Der Klick selbst wird verschluckt, damit z.B. auf dem
    // Handy nicht die Kachel unter dem abgedunkelten Hintergrund geöffnet wird.
    // Die Menüknöpfe selbst wechseln direkt zum anderen Menü.
    document.addEventListener("pointerdown", function (event) {
        var open = getOpenPlayerMenu(ui);
        var onMenuButton = ui.menus.some(function (entry) {
            return entry.button.contains(event.target);
        });
        if (open != null && !open.menu.contains(event.target) && !onMenuButton) {
            closePlayerMenus(ui);
            swallowNextClick();
        }
    });

    if (!ui.isAudio) {
        ui.pipButton.addEventListener("click", function () {
            if (document.pictureInPictureElement) {
                document.exitPictureInPicture().catch(function () {});
            } else if (media.requestPictureInPicture) {
                media.requestPictureInPicture().catch(function () {});
            }
        });
        ui.fullscreenButton.addEventListener("click", function () {
            toggleCinemaMode();
        });
        media.addEventListener("enterpictureinpicture", function () {
            ui.pipButton.classList.add("active");
        });
        media.addEventListener("leavepictureinpicture", function () {
            ui.pipButton.classList.remove("active");
        });
        bindSurfaceEvents(ui);
    }

    // Musik: Klick auf das Bild startet oder pausiert (nicht auf den Countdown im Bild)
    if (ui.isAudio) {
        ui.artwork.addEventListener("click", function (event) {
            if (event.target.closest(".autoplayCountdown") == null) {
                togglePlayback(ui);
            }
        });
    }

    // Zeitleiste
    bindProgressEvents(ui);

    // Zustand des Mediums
    media.addEventListener("play", function () {
        updatePlayButton(ui);
        startProgressLoop(ui);
        showPlayerControls(ui);
    });
    media.addEventListener("pause", function () {
        updatePlayButton(ui);
        updateProgress(ui);
        showPlayerControls(ui);
    });
    media.addEventListener("ended", function () {
        updatePlayButton(ui);
        showPlayerControls(ui);
    });
    media.addEventListener("waiting", function () {
        ui.shell.classList.add("isWaiting");
    });
    ["playing", "canplay", "seeked", "pause", "emptied"].forEach(function (type) {
        media.addEventListener(type, function () {
            ui.shell.classList.remove("isWaiting");
        });
    });
    ["timeupdate", "durationchange", "loadedmetadata", "progress", "seeked", "emptied"].forEach(function (type) {
        media.addEventListener(type, function () {
            updateProgress(ui);
        });
    });
    media.addEventListener("volumechange", function () {
        updateVolumeControls(ui);
        // Lautstärke sofort merken (Stummschalten wird nicht gespeichert)
        if (!media.muted && media.volume > 0) {
            setData("mediaVolume", media.volume);
        }
    });
    media.addEventListener("ratechange", function () {
        updateSpeedButtons(ui);
    });

    // Leiste einblenden bei Mausbewegung oder Tastaturbedienung
    ui.shell.addEventListener("pointermove", function (event) {
        if (event.pointerType == "mouse") {
            showPlayerControls(ui);
        }
    });
    ui.shell.addEventListener("pointerleave", function (event) {
        if (event.pointerType == "mouse") {
            scheduleHideControls(ui, 600);
        }
    });
    ui.shell.addEventListener("focusin", function () {
        showPlayerControls(ui);
    });
}

// Klick, Doppelklick und Tipps auf das Video
function bindSurfaceEvents(ui) {
    var surface = ui.surface;

    // Maus: Klick = Abspielen/Pause, Doppelklick = Vollbild
    surface.addEventListener("click", function (event) {
        if (Date.now() - ui.lastTouchTime < 800) {
            return;
        }
        if (isPlayerMenuOpen(ui)) {
            closePlayerMenus(ui);
            return;
        }
        if (event.detail > 1) {
            clearTimeout(ui.clickTimer);
            ui.clickTimer = null;
            if (event.detail == 2) {
                toggleCinemaMode();
            }
            return;
        }
        // Kurz warten, ob ein Doppelklick folgt (sonst würde das Video kurz pausieren)
        ui.clickTimer = setTimeout(function () {
            ui.clickTimer = null;
            togglePlayback(ui);
        }, 220);
    });

    // Touch: Tipp = Leiste ein/aus, Doppeltipp links/rechts = 10 Sekunden, Mitte = Vollbild
    surface.addEventListener("pointerup", function (event) {
        if (event.pointerType == "mouse") {
            return;
        }
        ui.lastTouchTime = Date.now();
        if (isPlayerMenuOpen(ui)) {
            closePlayerMenus(ui);
            return;
        }
        var rect = surface.getBoundingClientRect();
        var position = (event.clientX - rect.left) / rect.width;
        var zone = position < 0.35 ? -1 : (position > 0.65 ? 1 : 0);
        var now = Date.now();

        if (ui.lastTap != null && now - ui.lastTap.time < doubleTapDelay && ui.lastTap.zone == zone) {
            clearTimeout(ui.tapTimer);
            if (zone == 0) {
                ui.lastTap = null;
                toggleCinemaMode();
            } else {
                // Weitere Tipps auf derselben Seite spulen weiter
                ui.lastTap = { time: now, zone: zone };
                seekPlayerBy(ui, zone * 10, true);
            }
            return;
        }

        ui.lastTap = { time: now, zone: zone };
        clearTimeout(ui.tapTimer);
        ui.tapTimer = setTimeout(function () {
            if (ui.shell.classList.contains("controlsVisible")) {
                hidePlayerControls(ui);
            } else {
                showPlayerControls(ui);
            }
        }, doubleTapDelay);
    });
}

// Zeitleiste: Klicken, Ziehen, Zeit beim Überfahren
function bindProgressEvents(ui) {
    var progress = ui.progress;

    var ratioFromEvent = function (event) {
        var rect = progress.getBoundingClientRect();
        return Math.min(1, Math.max(0, (event.clientX - rect.left) / rect.width));
    };
    var seekToEvent = function (event) {
        if (hasMediaDuration(ui.media)) {
            ui.media.currentTime = ratioFromEvent(event) * ui.media.duration;
            updateProgress(ui);
        }
    };
    var showTooltip = function (event) {
        if (!hasMediaDuration(ui.media)) {
            return;
        }
        var ratio = ratioFromEvent(event);
        ui.progressTooltip.textContent = formatMediaTime(ratio * ui.media.duration);
        ui.progressTooltip.style.left = (ratio * 100) + "%";
        progress.classList.add("showTooltip");
    };

    progress.addEventListener("pointerdown", function (event) {
        if (event.button !== 0) {
            return;
        }
        event.preventDefault();
        ui.dragging = true;
        progress.classList.add("dragging");
        progress.setPointerCapture(event.pointerId);
        seekToEvent(event);
        showTooltip(event);
    });
    progress.addEventListener("pointermove", function (event) {
        if (ui.dragging) {
            seekToEvent(event);
        }
        if (ui.dragging || event.pointerType == "mouse") {
            showTooltip(event);
        }
    });
    var stopDragging = function () {
        ui.dragging = false;
        progress.classList.remove("dragging");
        progress.classList.remove("showTooltip");
        scheduleHideControls(ui);
    };
    progress.addEventListener("pointerup", stopDragging);
    progress.addEventListener("pointercancel", stopDragging);
    progress.addEventListener("pointerleave", function () {
        if (!ui.dragging) {
            progress.classList.remove("showTooltip");
        }
    });
    // Pfeiltasten spulen über die Tastenkürzel, hier nur Anfang/Ende
    progress.addEventListener("keydown", function (event) {
        if (!hasMediaDuration(ui.media)) {
            return;
        }
        if (event.key === "Home") {
            event.preventDefault();
            ui.media.currentTime = 0;
        } else if (event.key === "End") {
            event.preventDefault();
            ui.media.currentTime = ui.media.duration;
        }
    });
}

function togglePlayback(ui) {
    if (ui.media.paused || ui.media.ended) {
        var request = ui.media.play();
        if (request && request.catch) {
            request.catch(function () {});
        }
    } else {
        ui.media.pause();
    }
}

// Spult um "seconds" vor oder zurück. showRipple: Anzeige links/rechts im Video (Doppeltipp)
function seekPlayerBy(ui, seconds, showRipple) {
    var media = ui.media;
    var target = media.currentTime + seconds;
    if (hasMediaDuration(media)) {
        target = Math.min(media.duration, target);
    }
    media.currentTime = Math.max(0, target);
    updateProgress(ui);
    if (showRipple && ui.ripple) {
        var direction = seconds < 0 ? -1 : 1;
        // Mehrere Doppeltipps hintereinander zählen zusammen (+10, +20, ...)
        if (!ui.ripple.classList.contains("visible") || ui.rippleDirection !== direction) {
            ui.rippleAmount = 0;
        }
        ui.rippleDirection = direction;
        ui.rippleAmount += Math.abs(seconds);
        ui.ripple.textContent = (direction < 0 ? "-" : "+") + ui.rippleAmount + " s";
        ui.ripple.classList.toggle("left", direction < 0);
        ui.ripple.classList.toggle("right", direction > 0);
        ui.ripple.classList.add("visible");
        clearTimeout(ui.rippleTimer);
        ui.rippleTimer = setTimeout(function () {
            ui.ripple.classList.remove("visible");
        }, 700);
    }
}

// Leiste ein- und ausblenden
function showPlayerControls(ui) {
    if (ui == null) {
        return;
    }
    ui.shell.classList.add("controlsVisible");
    scheduleHideControls(ui);
}

function hidePlayerControls(ui) {
    // Audio: Leiste bleibt immer sichtbar. Pausiert oder bei offenem Menü ebenfalls.
    if (ui.isAudio || ui.media.paused || ui.dragging || isPlayerMenuOpen(ui)) {
        return;
    }
    ui.shell.classList.remove("controlsVisible");
}

function scheduleHideControls(ui, delay) {
    clearTimeout(ui.hideTimer);
    if (ui.isAudio) {
        return;
    }
    ui.hideTimer = setTimeout(function () {
        // Maus über der Leiste: noch nicht ausblenden (nur mit Maus, auf Touch bleibt :hover hängen)
        if (delay == null && document.documentElement.classList.contains("canHover") && ui.controls.matches(":hover")) {
            scheduleHideControls(ui);
            return;
        }
        hidePlayerControls(ui);
    }, delay != null ? delay : controlsHideDelay);
}

// Für Tastenkürzel: Leiste kurz zeigen
function revealPlayerControls() {
    showPlayerControls(getPlayerUI());
}

// Öffnet "menu" (und schliesst das andere) oder schliesst bei open=false alle Menüs
function togglePlayerMenu(ui, menu, open) {
    if (open) {
        refreshPlayerMenu(ui);
    }
    ui.menus.forEach(function (entry) {
        var show = open && entry.menu === menu;
        entry.menu.hidden = !show;
        entry.button.classList.toggle("menuActive", show);
        entry.button.setAttribute("aria-expanded", show ? "true" : "false");
    });
    ui.shell.classList.toggle("menuOpen", open);
    if (open) {
        showPlayerControls(ui);
    } else {
        scheduleHideControls(ui);
    }
}

function closePlayerMenus(ui) {
    togglePlayerMenu(ui, null, false);
}

// Offenes Menü als { menu, button } oder null
function getOpenPlayerMenu(ui) {
    return ui.menus.filter(function (entry) {
        return !entry.menu.hidden;
    })[0] || null;
}

function isPlayerMenuOpen(ui) {
    return getOpenPlayerMenu(ui) != null;
}

// Verschluckt den nächsten Klick (kurz nach dem Schliessen des Menüs)
function swallowNextClick() {
    var handler = function (event) {
        event.preventDefault();
        event.stopPropagation();
        remove();
    };
    var remove = function () {
        document.removeEventListener("click", handler, true);
    };
    document.addEventListener("click", handler, true);
    setTimeout(remove, 600);
}

// Für die Escape-Taste
function closePlayerMenu() {
    var ui = getPlayerUI();
    var open = ui == null ? null : getOpenPlayerMenu(ui);
    if (open == null) {
        return false;
    }
    closePlayerMenus(ui);
    open.button.focus();
    return true;
}

// Überträgt die Einstellungen ins Autoplay- und Zahnradmenü
function refreshPlayerMenu(ui) {
    if (ui == null) {
        return;
    }
    ui.menuAutoplay.checked = document.getElementById("autoplayCheckBox").checked;
    ui.menuInfinite.checked = isAutoplayInfinite();
    ui.menuAmount.value = document.getElementById("amountOfAutoplay").value;
    ui.menuAmount.disabled = isAutoplayInfinite();
    ui.menuAmount.closest(".settingRow").classList.toggle("disabled", ui.menuAmount.disabled);
    ui.menuDuration.value = document.getElementById("autoplayDuration").value;
    ui.menuNative.checked = useNativeControls();
    updateSpeedButtons(ui);
}

function updateSpeedButtons(ui) {
    ui.speedButtons.forEach(function (button) {
        var active = parseFloat(button.dataset.rate) === ui.media.playbackRate;
        button.classList.toggle("active", active);
        button.setAttribute("aria-pressed", active ? "true" : "false");
    });
}

function updatePlayButton(ui) {
    var playing = !ui.media.paused && !ui.media.ended;
    setControlButton(ui.playButton, playing ? "pause" : "play", playing ? "Pause (K)" : "Abspielen (K)");
    ui.shell.classList.toggle("isPaused", !playing);
}

function updateVolumeControls(ui) {
    var media = ui.media;
    var volume = media.muted ? 0 : media.volume;
    var icon = volume == 0 ? "volumeMute" : (volume < 0.5 ? "volumeLow" : "volumeHigh");
    setControlButton(ui.muteButton, icon, volume == 0 ? "Ton an (M)" : "Ton aus (M)");
    ui.volumeSlider.value = volume;
    ui.volumeSlider.style.setProperty("--fill", (volume * 100) + "%");
}

function updateProgress(ui) {
    var media = ui.media;
    var duration = hasMediaDuration(media) ? media.duration : 0;
    var current = media.currentTime || 0;
    var ratio = duration > 0 ? Math.min(1, current / duration) : 0;
    ui.progressPlayed.style.width = (ratio * 100) + "%";
    ui.progressThumb.style.left = (ratio * 100) + "%";

    // Geladener Bereich, in dem die aktuelle Position liegt
    var bufferedEnd = 0;
    for (var index = 0; index < media.buffered.length; index++) {
        if (media.buffered.start(index) <= current + 0.5) {
            bufferedEnd = Math.max(bufferedEnd, media.buffered.end(index));
        }
    }
    ui.progressBuffered.style.width = (duration > 0 ? Math.min(100, bufferedEnd / duration * 100) : 0) + "%";

    ui.time.textContent = formatMediaTime(current) + " / " + formatMediaTime(duration);
    ui.progress.setAttribute("aria-valuemax", String(Math.round(duration)));
    ui.progress.setAttribute("aria-valuenow", String(Math.round(current)));
    ui.progress.setAttribute("aria-valuetext", formatMediaTime(current) + " von " + formatMediaTime(duration));
}

// Flüssige Zeitleiste während der Wiedergabe (timeupdate kommt nur ca. 4x pro Sekunde)
function startProgressLoop(ui) {
    if (ui.frame != null) {
        return;
    }
    var step = function () {
        if (ui.media.paused || ui.media.ended || getPlayerUI() !== ui) {
            ui.frame = null;
            return;
        }
        updateProgress(ui);
        ui.frame = requestAnimationFrame(step);
    };
    ui.frame = requestAnimationFrame(step);
}

// Vor/Zurück-Knöpfe nur, wenn es ein vorheriges/nächstes Medium gibt
function updatePlayerNavButtons() {
    var ui = getPlayerUI();
    if (ui == null) {
        return;
    }
    [["prev", ui.prevButton, "Vorheriges"], ["next", ui.nextButton, "Nächstes"]].forEach(function (entry) {
        var link = document.querySelector("#" + entry[0] + " a");
        entry[1].disabled = link == null;
        var name = link == null ? "" : link.textContent.trim();
        var label = entry[2] + (name ? ": " + name : "") + " (" + (entry[0] == "prev" ? "P" : "N") + ")";
        entry[1].setAttribute("aria-label", label);
        entry[1].title = label;
    });
}

function updatePlayerTitle(text) {
    var ui = getPlayerUI();
    if (ui != null && ui.title) {
        ui.title.textContent = text;
    }
}

// Autoplay-Anzeige in der Leiste: ausgegraut, wenn Autoplay aus ist
function updatePlayerAutoplayState() {
    var ui = getPlayerUI();
    if (ui == null) {
        return;
    }
    var enabled = document.getElementById("autoplayCheckBox").checked;
    // "active" = Autoplay an (Symbol hell), "menuActive" = Menü offen
    ui.autoplayButton.classList.toggle("active", enabled);
    var label = (enabled ? "Autoplay ist an" : "Autoplay ist aus") + " (Einstellungen)";
    ui.autoplayButton.setAttribute("aria-label", label);
    ui.autoplayButton.title = label;
    if (!enabled) {
        hideAutoplayCountdown();
    }
    if (isPlayerMenuOpen(ui)) {
        refreshPlayerMenu(ui);
    }
}

// Autoplay aus der Leiste oder dem Countdown ein-/ausschalten
function setAutoplayFromPlayer(enabled) {
    document.getElementById("autoplayCheckBox").checked = enabled;
    checkAutoplayAmount();
    commitAutoplaySettings();
    if (!enabled) {
        document.getElementById("next").style.background = "";
    }
}

// Countdown bis zum nächsten Medium (secondsLeft, Fortschritt 0-100)
function showAutoplayCountdown(secondsLeft, percent) {
    var ui = getPlayerUI();
    var nextLink = document.querySelector("#next a");
    if (ui == null || nextLink == null) {
        hideAutoplayCountdown();
        return;
    }
    ui.countdownLabel.textContent = "Nächstes in " + Math.max(0, secondsLeft) + " s";
    ui.countdownName.textContent = nextLink.textContent.trim();
    ui.countdownFill.style.width = Math.min(100, Math.max(0, percent)) + "%";
    ui.countdown.hidden = false;
}

function hideAutoplayCountdown() {
    var ui = getPlayerUI();
    if (ui != null) {
        ui.countdown.hidden = true;
    }
}

// Neues Medium im bestehenden Player: Geschwindigkeit 1x, Download-Link, Anzeige zurücksetzen
function onPlayerMediaChanged(media) {
    var ui = getPlayerUI();
    if (ui == null || ui.media !== media) {
        return;
    }
    media.defaultPlaybackRate = 1;
    media.playbackRate = 1;
    ui.downloadLink.href = media.getAttribute("src");
    ui.downloadLink.setAttribute("download", getBaseName(media.dataset.mediaPath || ""));
    hideAutoplayCountdown();
    updateProgress(ui);
    updatePlayButton(ui);
    updatePlayerTitle(document.getElementById("mediaTitle").textContent);
    updateAudioArtwork(ui);
    showPlayerControls(ui);
}

// Musik: Vorschaubild des Titels, sonst des Albums/Ordners (wie bei den Kacheln)
function updateAudioArtwork(ui) {
    if (!ui.isAudio) {
        return;
    }
    var preview = getPreviewForMedia(ui.media.dataset.mediaPath || "", false);
    var hasArtwork = preview != null;
    ui.shell.classList.toggle("hasArtwork", hasArtwork);
    ui.artwork.hidden = !hasArtwork;
    // Countdown liegt im Bild (wie beim Video), ohne Bild über der Leiste
    if (hasArtwork) {
        ui.artwork.appendChild(ui.countdown);
    } else {
        ui.shell.insertBefore(ui.countdown, ui.controls);
    }
    ui.artworkToken = (ui.artworkToken || 0) + 1;
    if (!hasArtwork) {
        ui.artworkImage.removeAttribute("src");
        ui.artworkBackdrop.removeAttribute("src");
        return;
    }
    // Vorschaubild in Originalgrösse aus dem Preview-Ordner
    ui.artworkImage.src = preview;
    ui.artworkBackdrop.src = preview;
}

// Umschalten zwischen eigener und Browser-Steuerung
function applyNativeControls(ui, enabled) {
    ui.media.controls = enabled;
    ui.shell.classList.toggle("nativeControls", enabled);
    if (enabled) {
        closePlayerMenus(ui);
    }
}

function setNativeControls(enabled) {
    setData("nativeControls", enabled ? "true" : "false");
    var checkbox = document.getElementById("nativeControlsCheckBox");
    if (checkbox != null) {
        checkbox.checked = enabled;
    }
    var ui = getPlayerUI();
    if (ui != null) {
        applyNativeControls(ui, enabled);
    }
}

// ===================================================================
// Vollbild für Videos: per CSS, damit unter dem Video weitergescrollt werden kann.
// Zusätzlich wechselt der Browser in den Vollbildmodus (falls möglich).
// ===================================================================

function isCinemaMode() {
    return document.documentElement.classList.contains("playerCinema");
}

function getFullscreenElement() {
    return document.fullscreenElement || document.webkitFullscreenElement || null;
}

function toggleCinemaMode() {
    if (isCinemaMode()) {
        exitCinemaMode();
    } else {
        enterCinemaMode();
    }
}

function enterCinemaMode() {
    var ui = getPlayerUI();
    if (ui == null || ui.isAudio || isCinemaMode()) {
        return;
    }
    document.documentElement.classList.add("playerCinema");
    requestBrowserFullscreen();
    updateFullscreenButton();
    scrollToPlayer(false);
    showPlayerControls(ui);
}

// Schaltet den Browser in den Vollbildmodus (ganze Seite, damit weitergescrollt werden kann).
// iPhone: keine Vollbild-Schnittstelle, dann bleibt es beim CSS-Vollbild.
function requestBrowserFullscreen() {
    var root = document.documentElement;
    var request = root.requestFullscreen || root.webkitRequestFullscreen;
    if (!request || getFullscreenElement() != null) {
        return;
    }
    try {
        cinemaRequestedFullscreen = true;
        var result = request.call(root);
        if (result && result.catch) {
            result.catch(function () {
                cinemaRequestedFullscreen = false;
            });
        }
    } catch (error) {
        cinemaRequestedFullscreen = false;
    }
}

// Beendet den Browser-Vollbildmodus, aber nur wenn STEFFLIX ihn eingeschaltet hat (nicht F11)
function exitBrowserFullscreen() {
    if (cinemaRequestedFullscreen && getFullscreenElement() != null) {
        var exit = document.exitFullscreen || document.webkitExitFullscreen;
        if (exit) {
            var result = exit.call(document);
            if (result && result.catch) {
                result.catch(function () {});
            }
        }
    }
    cinemaRequestedFullscreen = false;
}

function exitCinemaMode() {
    if (!isCinemaMode()) {
        return;
    }
    document.documentElement.classList.remove("playerCinema");
    exitBrowserFullscreen();
    updateFullscreenButton();
    if (document.getElementById("video") != null) {
        requestAnimationFrame(function () {
            scrollToPlayer(false);
        });
    }
}

function updateFullscreenButton() {
    var ui = getPlayerUI();
    if (ui == null || ui.fullscreenButton == null) {
        return;
    }
    var active = isCinemaMode();
    setControlButton(ui.fullscreenButton, active ? "fullscreenExit" : "fullscreen", active ? "Vollbild beenden (F)" : "Vollbild (F)");
}

// Browser-Vollbild beendet (z.B. Escape): Video- bzw. Bild-Vollbild ebenfalls beenden
function handleFullscreenChange() {
    if (getFullscreenElement() == null && cinemaRequestedFullscreen) {
        cinemaRequestedFullscreen = false;
        exitCinemaMode();
        if (isImageFullscreen()) {
            setImageFullscreen(false);
        }
    } else if (isCinemaMode()) {
        // Fenstergrösse hat sich geändert: Video wieder oben ausrichten
        requestAnimationFrame(function () {
            scrollToPlayer(false);
        });
    }
}

function initPlayerControls() {
    document.addEventListener("fullscreenchange", handleFullscreenChange);
    document.addEventListener("webkitfullscreenchange", handleFullscreenChange);
    // Tab-Taste: ausgeblendete Leiste wieder zeigen, damit die Knöpfe erreichbar sind
    document.addEventListener("keydown", function (event) {
        if (event.key === "Tab") {
            revealPlayerControls();
        }
    });
}

// Vollbild für Bilder: Bild auf der ganzen Fläche, der Browser wechselt ebenfalls in den Vollbildmodus
function isImageFullscreen() {
    return document.getElementById("media").classList.contains("fullscreened");
}

function toggleFullscreenMode() {
    setImageFullscreen(!isImageFullscreen());
}

function setImageFullscreen(active) {
    var node = document.getElementById("media");
    if (active == isImageFullscreen()) {
        return;
    }
    if (active) {
        node.classList.add("fullscreened");
        document.getElementById("mediaTitle").style.display = "none";
        requestBrowserFullscreen();
    } else {
        node.classList.remove("fullscreened");
        document.getElementById("mediaTitle").style.display = null;
        exitBrowserFullscreen();
        setTimeout(function () {
            scrollToPlayer(false);
        }, 400);
    }
}
