// STEFFLIX - pages.js
// Seiten: Hauptmenü, Ordnerseiten, Suche, Favoriten, Startseite, Update-Hinweis und Hilfe
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

// ===================================================================
// Menü
// ===================================================================

// Markiert den Eintrag einer Ebene in der übergeordneten Liste als aktiv
function markActiveLevel(parentList, level) {
    for (var index = 0; index < parentList.children.length; index++) {
        if (parentList.children[index].dataset.level === level) {
            parentList.children[index].classList.add("activeMenu");
        }
    }
}

// ===================================================================
// Ordnerseiten und Umgebung des Players (Menübänder)
// ===================================================================

// Merkt sich, welcher Ordner gerade als Menübänder angezeigt wird
var renderedFolderView = null;

// Pfadleiste: Start › Serien › Show A › S01
function renderBreadcrumb(container, levels, lastIsLink) {
    var nav = document.createElement("nav");
    nav.className = "breadcrumb";
    nav.setAttribute("aria-label", "Pfad");

    var startLink = document.createElement("a");
    startLink.href = "start.html";
    startLink.textContent = "Start";
    nav.appendChild(startLink);

    levels.forEach(function (level, index) {
        var separator = document.createElement("span");
        separator.className = "breadcrumbSeparator";
        separator.textContent = "›";
        nav.appendChild(separator);

        var isLast = index == levels.length - 1;
        var href = buildStartUrl(buildLevelPath(levels.slice(0, index + 1)));
        var item;
        if (isLast && !lastIsLink || href == null) {
            item = document.createElement("span");
            item.className = "breadcrumbCurrent";
        } else {
            item = document.createElement("a");
            item.href = href;
        }
        item.textContent = level;
        nav.appendChild(item);
    });

    container.appendChild(nav);
}

// Medien (sortiert, als Pfade) und Unterordner mit Inhalt eines Ordners
function getSortedChildren(node) {
    var children = { media: [], folders: [] };
    if (node == null || typeof node !== "object") {
        return children;
    }
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            children.media.push(normalizeMediaLocation(key));
        } else if (hasMedia(node[key], true)) {
            children.folders.push(key);
        }
    }
    sortNatural(children.media);
    sortNatural(children.folders);
    return children;
}

// Kachel für ein Medium, das ohne Seitenreload abgespielt wird
function createPlayableCard(track, mediaPath) {
    return createMediaCard(track, mediaPath, null, {
        playData: buildMediaData(mediaPath),
        subtitle: getMediaTypeLabel(mediaPath),
        inheritPreview: true,
        progress: getMediaProgress(mediaPath)
    });
}

// Füllt ein Band mit den Medien und Unterordnern eines Ordners
function fillFolderTrack(track, levels, node) {
    var children = getSortedChildren(node);
    children.media.forEach(function (mediaPath) {
        createPlayableCard(track, mediaPath);
    });
    children.folders.forEach(function (folder) {
        createFolderCard(track, levels.concat([folder]));
    });
}

// Markiert die Kategorie im Hauptmenü
function markActiveCategory(category) {
    var menu = document.getElementById("menu");
    Array.prototype.forEach.call(menu.children, function (item) {
        item.classList.remove("activeMenu");
    });
    markActiveLevel(menu, category);
}

// Speichert die Playliste eines Ordners (für Vor/Zurück und Autoplay)
function saveFolderPlaylist(levels, mediaPaths) {
    var playlistName = levels.join("");
    setData("currentPrefix", playlistName);
    setData("playlist-" + playlistName, mediaPaths.join(listSeperator));
}

// Angezeigte Ordnerseite und Kacheln pro Zeile beim Aufbau (für "Kacheln pro Zeile automatisch")
var currentFolderLevels = null;
var renderedCardsPerRow = 0;

// Ändert sich die Fensterbreite so, dass mehr oder weniger Kacheln Platz haben, wird die
// Ordnerseite neu verteilt (die Scrollposition bleibt erhalten)
function initAutoRowResize() {
    var timer = null;
    window.addEventListener("resize", function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            if (!getSetting("rowAuto") || currentFolderLevels == null || renderedFolderView !== "folder:" + currentFolderLevels.join("/")) {
                return;
            }
            if (getCardsPerRow() !== renderedCardsPerRow) {
                var scroll = window.scrollY;
                renderFolderPage(currentFolderLevels);
                window.scrollTo(0, scroll);
            }
        }, 200);
    });
}

// Ordnerseite: oben die Medien des Ordners, darunter ein Band pro Unterordner
function renderFolderPage(levels) {
    renderedFolderView = "folder:" + levels.join("/");
    currentFolderLevels = levels;
    renderedCardsPerRow = getCardsPerRow();
    var header = document.getElementById("navigation");
    var bands = document.getElementById("folderBands");
    header.replaceChildren();
    bands.replaceChildren();
    header.className = "folderHeader";
    // Auf Ordnerseiten steht die Pfadleiste oben (bei der Wiedergabe unter den Buttons)
    document.getElementById("media").before(header);

    renderBreadcrumb(header, levels, false);
    var title = document.createElement("h1");
    title.className = "folderTitle";
    title.textContent = levels[levels.length - 1];
    var info = document.createElement("p");
    info.className = "folderInfo";
    header.appendChild(title);
    header.appendChild(info);
    markActiveCategory(levels[0]);

    var node = getDataNode(levels);
    if (node == null || typeof node !== "object") {
        info.textContent = "Dieser Ordner wurde nicht gefunden. Falls er noch existiert, bitte updateData.exe ausführen.";
        return;
    }
    info.textContent = describeFolder(node);

    // Zufällige Wiedergabe über alle Medien dieses Ordners (inkl. Unterordner)
    if (collectMediaPaths(node, []).length > 1) {
        var shuffleButton = document.createElement("button");
        shuffleButton.type = "button";
        shuffleButton.className = "sflixButton primary folderShuffle";
        shuffleButton.appendChild(createIcon("shuffle"));
        shuffleButton.appendChild(document.createTextNode("Zufällig abspielen"));
        shuffleButton.addEventListener("click", function () {
            startShuffleForFolder(levels);
        });
        header.appendChild(shuffleButton);
    }

    var children = getSortedChildren(node);
    if (children.media.length > 0) {
        saveFolderPlaylist(levels, children.media);
    }

    var mediaEntries = children.media.map(function (mediaPath) {
        return { name: getDisplayName(mediaPath), media: mediaPath };
    });
    var folderEntries = children.folders.map(function (folder) {
        return { name: folder, folder: folder };
    });
    var renderEntry = function (track, entry) {
        if (entry.media != null) {
            createPlayableCard(track, entry.media);
        } else {
            createFolderCard(track, levels.concat([entry.folder]));
        }
    };

    if (shouldShowFolderContents(levels, node, children.folders)) {
        // Wenige Unterordner mit Medien (z.B. Staffeln einer Serie): oben die Medien des Ordners,
        // darunter ein Band pro Ordner mit dessen Inhalt
        if (mediaEntries.length > 0) {
            renderFolderEntries(bands, mediaEntries, "Inhalt", node, renderEntry);
        }
        children.folders.forEach(function (folder) {
            var subLevels = levels.concat([folder]);
            var track = createCardRow(bands, folder, buildStartUrl(buildLevelPath(subLevels)));
            fillFolderTrack(track, subLevels, node[folder]);
        });
    } else if (getSetting("separateFolders") && mediaEntries.length > 0 && folderEntries.length > 0) {
        // Einstellung "Ordner getrennt von Medien": oben ein Band mit den Ordnern, darunter die Medien
        renderGroupedBands(bands, folderEntries, "Ordner", renderEntry, true);
        renderFolderEntries(bands, mediaEntries, "Medien", node, renderEntry, true);
    } else if (mediaEntries.length > 0 || folderEntries.length > 0) {
        // Sonst Medien und Ordner gemeinsam alphabetisch (z.B. ein Ordner "Harry Potter"
        // zwischen den Filmen unter H)
        var entries = mediaEntries.concat(folderEntries);
        entries.sort(function (a, b) {
            return compareSortNames(a.name, b.name);
        });
        // Titel ist der gewählte Ordner bzw. die Kategorie (z.B. "Filme")
        renderFolderEntries(bands, entries, levels[levels.length - 1], node, renderEntry);
    }

    if (children.media.length == 0 && children.folders.length == 0) {
        info.textContent = "Dieser Ordner ist leer.";
    }
    refreshRowArrows(bands);
}

// Ein Band pro Unterordner mit dessen Inhalt nur, wenn es wenige Ordner sind, die direkt
// Medien enthalten (Staffeln, Alben). Auf Kategorieseiten (Serien, Filme, ...) nie.
function shouldShowFolderContents(levels, node, folders) {
    if (levels.length <= 1 || folders.length == 0 || folders.length > groupThreshold) {
        return false;
    }
    return folders.every(function (folder) {
        return getSortedChildren(node[folder]).media.length > 0;
    });
}

// Bis zu so vielen Unterordnern bekommt jeder ein eigenes Band mit Inhalt (Staffeln, Alben)
var groupThreshold = 12;

// Ein Band mit mehreren Zeilen. rowSize ist "Kacheln pro Zeile" aus den Einstellungen (Standard 12):
// bis drei Viertel davon eine Zeile (12: bis 9), bis blockRows * rowSize Einträge gleichmässig auf
// blockRows Zeilen verteilt (14: 5 + 5 + 4), darüber jeweils nach rowSize Einträgen eine neue Zeile.
var blockRows = 3;

// Verteilt die Einträge der Reihe nach gleichmässig; die ersten Bänder erhalten den Rest
function splitEvenly(items, bandCount) {
    var bands = [];
    var base = Math.floor(items.length / bandCount);
    var extra = items.length % bandCount;
    var start = 0;
    for (var index = 0; index < bandCount; index++) {
        var size = base + (index < extra ? 1 : 0);
        bands.push(items.slice(start, start + size));
        start += size;
    }
    return bands;
}

// Kacheln, die nebeneinander ganz Platz haben (aus --cards-per-view, je nach Breite und Kachelgrösse)
function getCardsPerRow() {
    var value = parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--cards-per-view"));
    return Math.max(1, Math.floor(isNaN(value) ? 5 : value));
}

// Einträge der Reihe nach auf Zeilen verteilen (siehe blockRows und "Kacheln pro Zeile").
// "Kacheln pro Zeile automatisch": jede Zeile genau so voll, wie Platz ist.
function splitIntoRows(items) {
    if (getSetting("rowAuto")) {
        var perRow = getCardsPerRow();
        var autoRows = [];
        for (var autoStart = 0; autoStart < items.length; autoStart += perRow) {
            autoRows.push(items.slice(autoStart, autoStart + perRow));
        }
        return autoRows.length > 0 ? autoRows : [items];
    }
    var rowSize = getSetting("rowSize");
    var singleRowLimit = Math.round(rowSize * 0.75);
    if (items.length <= singleRowLimit) {
        return [items];
    }
    if (items.length <= blockRows * rowSize) {
        // Bei wenigen Kacheln pro Zeile (z.B. 4) keine fast leeren Zeilen: mindestens ein Drittel davon
        var minPerRow = Math.ceil(rowSize / blockRows);
        return splitEvenly(items, Math.min(blockRows, Math.ceil(items.length / minPerRow)));
    }
    var rows = [];
    for (var start = 0; start < items.length; start += rowSize) {
        rows.push(items.slice(start, start + rowSize));
    }
    return rows;
}

// Ein Band, dessen Kacheln auf mehrere Zeilen umbrechen. Ohne showTitle erscheint "title"
// (z.B. "Filme") nur in der Rasteransicht von "Alle anzeigen". Gibt das Band zurück.
function renderGroupedBands(container, items, title, renderItem, showTitle) {
    var rows = splitIntoRows(items);
    var track = createCardRow(container, title, null);
    var section = track.parentNode.parentNode;
    if (!showTitle) {
        section.classList.add("untitledRow");
        section.querySelector(".rowHeader h2").textContent = "";
    }
    rows.forEach(function (row, index) {
        var rowTrack = index == 0 ? track : addRowTrack(section);
        row.forEach(function (item) {
            renderItem(rowTrack, item);
        });
    });
    return section;
}

// ===================================================================
// Alphabetische Gruppen (Einstellung): A–D, E–H, I–L, M–P, Q–T, U–Z, 0–9 / #
// ===================================================================
var alphabetGroups = [
    { label: "A–D", letters: "ABCD" },
    { label: "E–H", letters: "EFGH" },
    { label: "I–L", letters: "IJKL" },
    { label: "M–P", letters: "MNOP" },
    { label: "Q–T", letters: "QRST" },
    { label: "U–Z", letters: "UVWXYZ" }
];
var otherGroupLabel = "0–9 / #";

// Gruppe eines Namens. Umlaute zählen beim Grundbuchstaben (Ä bei A), führende Artikel
// werden je nach Einstellung übergangen.
function getAlphabetGroup(name) {
    var first = getSortName(name).normalize("NFD").replace(/[̀-ͯ]/g, "").charAt(0).toUpperCase();
    for (var index = 0; index < alphabetGroups.length; index++) {
        if (first !== "" && alphabetGroups[index].letters.indexOf(first) >= 0) {
            return alphabetGroups[index].label;
        }
    }
    return otherGroupLabel;
}

// Teilt sortierte Einträge ({ name, ... }) in Gruppen auf, leere Gruppen fallen weg
function groupAlphabetically(entries) {
    var labels = alphabetGroups.map(function (group) {
        return group.label;
    }).concat([otherGroupLabel]);
    var byLabel = {};
    entries.forEach(function (entry) {
        var label = getAlphabetGroup(entry.name);
        (byLabel[label] = byLabel[label] || []).push(entry);
    });
    return labels.filter(function (label) {
        return byLabel[label] != null;
    }).map(function (label) {
        return { label: label, entries: byLabel[label] };
    });
}

// Serienfolgen (S01E02) bleiben immer in ihrer Reihenfolge. Bilder- und Musikordner lassen sich
// in den Einstellungen ausnehmen.
var episodePattern = /S\d{1,3}\s*E\d{1,4}/i;

function shouldGroupAlphabetically(node, entries) {
    if (!getSetting("alphaGroups")) {
        return false;
    }
    var type = getFolderIconName(node);
    if ((type == "image" && getSetting("groupExcludeImages")) || (type == "music" && getSetting("groupExcludeMusic"))) {
        return false;
    }
    return !entries.some(function (entry) {
        return entry.media != null && episodePattern.test(entry.name);
    });
}

// Zeigt Einträge eines Ordners: in Gruppen, wenn eingeschaltet und es mehr als eine Gruppe gibt,
// sonst in einem Band. Jede Gruppe bricht nach "Kacheln pro Zeile" auf weitere Zeilen um.
function renderFolderEntries(container, entries, title, node, renderItem, showTitle) {
    var groups = shouldGroupAlphabetically(node, entries) ? groupAlphabetically(entries) : [];
    if (groups.length < 2) {
        renderGroupedBands(container, entries, title, renderItem, showTitle);
        return;
    }
    var jumpBar = getSetting("jumpBar") ? createJumpBar(container) : null;
    groups.forEach(function (group) {
        var section = renderGroupedBands(container, group.entries, group.label, renderItem, true);
        section.classList.add("alphabetGroup");
        section.querySelector(".rowHeader h2").appendChild(createRowCount(group.entries.length));
        if (jumpBar != null) {
            addJumpTarget(jumpBar, group.label, section);
        }
    });
}

// Sprungleiste über den Gruppen (A–D | E–H | ...), nur mit vorhandenen Gruppen
function createJumpBar(container) {
    var nav = document.createElement("nav");
    nav.className = "jumpBar";
    nav.setAttribute("aria-label", "Zu einer Gruppe springen");
    container.appendChild(nav);
    return nav;
}

function addJumpTarget(jumpBar, label, section) {
    var button = document.createElement("button");
    button.type = "button";
    button.className = "jumpBarItem";
    button.textContent = label;
    button.addEventListener("click", function () {
        section.scrollIntoView({ behavior: getSetting("reduceMotion") ? "auto" : "smooth", block: "start" });
        var firstCard = section.querySelector(".card");
        if (firstCard != null) {
            firstCard.focus({ preventScroll: true });
        }
    });
    jumpBar.appendChild(button);
}

// Ordner, dessen Bänder unter dem Player gerade aufgebaut sind
var renderedBandsView = null;
// Nur die zuletzt angeforderte Aktion ausführen (schnelles Weiterschalten)
var mediaReadyToken = 0;

// Führt "callback" aus, sobald das angezeigte Medium auf dem Bildschirm ist. So erscheint zuerst
// das neue Bild und erst danach werden die Bänder aufgebaut und ihre Vorschaubilder geladen.
// "Geladen" reicht dafür nicht: Ein grosses Foto muss danach noch dekodiert und gezeichnet werden,
// und das Aufbauen vieler Kacheln würde genau das verzögern. Deshalb: laden -> dekodieren ->
// zwei Bildschirmaktualisierungen abwarten. Rückfall nach 4 Sekunden, falls das Medium hängt.
function whenMainMediaReady(callback) {
    var token = ++mediaReadyToken;
    var done = false;
    var finish = function () {
        if (done || token !== mediaReadyToken) {
            return;
        }
        done = true;
        callback();
    };
    // Erst nach dem nächsten Zeichnen des Bildschirms (das Hauptbild ist dann sichtbar)
    var afterPaint = function () {
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                setTimeout(finish, 0);
            });
        });
    };
    var run = function () {
        if (done || token !== mediaReadyToken) {
            return;
        }
        var player = document.getElementById("video");
        if (player != null && player.tagName == "IMG" && player.decode && player.naturalWidth > 0) {
            player.decode().then(afterPaint, afterPaint);
        } else {
            afterPaint();
        }
    };
    // Erst prüfen, wenn loadDecodedMedia die neue Quelle gesetzt hat (geschieht nach den Bändern)
    setTimeout(function () {
        var player = document.getElementById("video");
        if (player == null) {
            run();
            return;
        }
        var isImg = player.tagName == "IMG";
        if (isImg ? player.complete : player.readyState >= 2) {
            run();
            return;
        }
        (isImg ? ["load", "error"] : ["loadeddata", "error"]).forEach(function (type) {
            player.addEventListener(type, run, { once: true });
        });
        setTimeout(finish, 4000);
    }, 0);
}

// Unter dem Player: weitere Medien aus dem Ordner und Nachbarordner (z.B. andere Staffeln).
// Pfadleiste sofort, die Bänder erst nach dem Laden des Mediums (siehe whenMainMediaReady).
function renderMediaContext(mediaPath) {
    var folders = getMediaFolders(mediaPath);
    // Musik zeigt die Titel in der Playliste neben dem Player statt in einem Band
    var musicList = isMusic(mediaPath);
    var viewKey = "media:" + folders.join("/") + (musicList ? ":music" : "");

    if (renderedFolderView !== viewKey) {
        renderedFolderView = viewKey;
        renderedBandsView = null;
        var header = document.getElementById("navigation");
        header.replaceChildren();
        header.className = "folderHeader playerHeader";
        // Pfadleiste unter den Buttons, damit das Medium ganz oben steht
        document.getElementById("mediaNav").after(header);
        renderBreadcrumb(header, folders, true);
        markActiveCategory(folders[0]);
        // Bänder des alten Ordners sofort entfernen
        document.getElementById("folderBands").replaceChildren();
    }

    whenMainMediaReady(function () {
        if (renderedBandsView !== viewKey) {
            renderedBandsView = viewKey;
            renderMediaBands(folders, musicList);
            // Neues Band: direkt zur aktuellen Kachel springen
            updateCurrentCard(mediaPath, true);
        } else {
            updateCurrentCard(mediaPath, false);
        }
        // Nächstes und vorheriges Bild im Hintergrund laden (player.js)
        preloadNeighbourImages();
    });
}

function renderMediaBands(folders, musicList) {
    var bands = document.getElementById("folderBands");
    bands.replaceChildren();
    var node = getDataNode(folders);
    var folderName = folders.length > 0 ? folders[folders.length - 1] : "STEFFLIX";
    var children = getSortedChildren(node);

    if (children.media.length > 0 && !musicList) {
        var folderUrl = folders.length > 0 ? buildStartUrl(buildLevelPath(folders)) : null;
        var mediaTrack = createCardRow(bands, "Mehr aus " + folderName, folderUrl);
        children.media.forEach(function (path) {
            createPlayableCard(mediaTrack, path);
        });
    }

    if (children.folders.length > 0) {
        var subTrack = createCardRow(bands, "Ordner in " + folderName, null);
        children.folders.forEach(function (folder) {
            createFolderCard(subTrack, folders.concat([folder]));
        });
    }

    // Nachbarordner, z.B. die anderen Staffeln einer Serie
    if (folders.length >= 2) {
        var parentLevels = folders.slice(0, -1);
        var siblings = getSortedChildren(getDataNode(parentLevels)).folders;
        if (siblings.length > 1) {
            var parentName = parentLevels[parentLevels.length - 1];
            var siblingTrack = createCardRow(bands, "Mehr aus " + parentName, buildStartUrl(buildLevelPath(parentLevels)));
            siblings.forEach(function (sibling) {
                var card = createFolderCard(siblingTrack, parentLevels.concat([sibling]));
                if (card != null && sibling === folderName) {
                    card.classList.add("current");
                }
            });
        }
    }
    refreshRowArrows(bands);
}

// Hebt das laufende Medium in den Bändern hervor und scrollt es ins Bild.
// Nur kurze Wege werden animiert: Beim animierten Scrollen über viele Kacheln würde der Browser
// jedes Vorschaubild auf dem Weg laden (bei 2000 Bildern sehr langsam). Weite Sprünge (z.B. Zufall)
// und neue Bänder springen deshalb direkt, dann werden nur die sichtbaren Bilder geladen.
function updateCurrentCard(mediaPath, jump) {
    document.querySelectorAll("#folderBands .card.current[data-media]").forEach(function (card) {
        card.classList.remove("current");
    });
    document.querySelectorAll('#folderBands .card[data-media="' + CSS.escape(mediaPath) + '"]').forEach(function (card) {
        card.classList.add("current");
        var track = card.parentNode;
        requestAnimationFrame(function () {
            var target = Math.max(0, card.offsetLeft - track.clientWidth / 2 + card.clientWidth / 2);
            var far = Math.abs(target - track.scrollLeft) > track.clientWidth * 2;
            track.scrollTo({ left: target, behavior: jump || far ? "auto" : "smooth" });
        });
    });
}

// Erstellt das Hauptmenü
function createMainMenu() {
    var menu = document.getElementById("menu");
    for (var key in data) {
        // Nur Ordner mit Inhalt (keine Preview-Ordner oder einzelne Dateien im Hauptordner)
        if (!data.hasOwnProperty(key) || key == "Preview" || !hasMedia(data[key], true)) {
            continue;
        }
        var targetUrl = buildStartUrl("LEVEL0:" + key);
        if (targetUrl == null) {
            continue;
        }
        var item = document.createElement("li");
        var link = document.createElement("a");
        link.textContent = key;
        link.href = targetUrl;
        item.appendChild(link);
        item.dataset.level = key;
        item.dataset.sort = key;
        menu.appendChild(item);
    }
    // Gleiche Reihenfolge wie die Kategorie-Bänder der Startseite
    applyMenuOrder();
}

// ===================================================================
// Suche
// ===================================================================

// Funktion zum Aufruf der Suchfunktion
function callSearch() {
    startSearch(document.getElementById("searchField").value);
}

// Zeichen ausserhalb von Latin-1 entfernen, damit die URL kodiert werden kann
function cleanSearchTerm(term) {
    return String(term || "").replace(/[^\u0000-ÿ]/g, "").trim();
}

function startSearch(term) {
    var searchTerm = cleanSearchTerm(term);
    if (searchTerm === "") {
        return;
    }
    window.location.href = buildStartUrl("SEARCH:" + searchTerm, false);
}

// Funktion zum Erstellen des Playlisteneintrags für Suchresultate
function createLink(key, coreUrl, playlistName) {
    return normalizeMediaLocation(key) + "|" + buildStartUrl(coreUrl + listSeperator + "PL:" + playlistName);
}

// Funktion zum rekursiven Durchsuchen des Daten-Arrays.
// Findet Medien anhand des Dateinamens und Ordner, die Medien enthalten.
function recursiveSearch(node, searchTerm, levels, results) {
    var term = searchTerm.toLowerCase();
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }

        if (isMediaFile(allowedMediaExtensions, key)) {
            if (levels.length > 0 && getDisplayName(key).toLowerCase().includes(term)) {
                var mediaData = buildLevelPath(levels) + listSeperator + "MEDIA:" + normalizeMediaLocation(key);
                if (encodeUrlData(mediaData) != null) {
                    results.media.push(createLink(key, mediaData, "searchResults"));
                }
            }
        } else if (node[key] != null && typeof node[key] === "object") {
            var folderLevels = levels.concat([key]);
            if (key.toLowerCase().includes(term) && hasMedia(node[key], true)) {
                results.folders.push(folderLevels);
            }
            recursiveSearch(node[key], searchTerm, folderLevels, results);
        }
    }
    return results;
}

// ===================================================================
// Ergebnisseiten (Suche, Favoriten)
// ===================================================================

function createResultSection(title, id) {
    var section = document.createElement("section");
    section.className = "resultSection";
    if (id) {
        section.id = id;
    }
    var heading = document.createElement("h2");
    heading.textContent = title;
    section.appendChild(heading);
    document.getElementById("media").appendChild(section);
    return section;
}

function createCardGrid(section) {
    var grid = document.createElement("div");
    grid.className = "cardGrid";
    section.appendChild(grid);
    return grid;
}

// Suchfeld auf der Ergebnisseite zum Verfeinern der Suche
function createSearchRefineForm(searchTerm) {
    var form = document.createElement("form");
    form.className = "searchRefine";
    form.setAttribute("role", "search");
    var input = document.createElement("input");
    input.type = "search";
    input.value = searchTerm;
    input.placeholder = "Titel, Serien oder Ordner suchen";
    input.setAttribute("aria-label", "Suchbegriff");
    var button = document.createElement("button");
    button.type = "submit";
    button.className = "sflixButton primary";
    button.textContent = "Suchen";
    form.appendChild(input);
    form.appendChild(button);
    form.addEventListener("submit", function (event) {
        event.preventDefault();
        startSearch(input.value);
    });
    return form;
}

// Live-Vorschläge im Suchfeld der Kopfzeile (max. 4 Ordner und 6 Titel)
function renderSearchSuggestions(rawTerm) {
    var list = document.getElementById("searchSuggestions");
    list.replaceChildren();
    var term = cleanSearchTerm(rawTerm);
    if (term.length < 2) {
        return;
    }

    var results = recursiveSearch(data, term, [], { folders: [], media: [] });
    results.folders.sort(function (a, b) {
        return a.length - b.length || compareNatural(a.join("/"), b.join("/"));
    });
    sortNatural(results.media);

    var addSuggestion = function (iconName, title, path, href) {
        if (href == null) {
            return;
        }
        var item = document.createElement("li");
        var link = document.createElement("a");
        link.href = href;
        var icon = document.createElement("span");
        icon.className = "suggestionIcon";
        icon.appendChild(createIcon(iconName));
        var text = document.createElement("span");
        text.className = "suggestionText";
        var titleNode = document.createElement("span");
        titleNode.className = "suggestionTitle";
        titleNode.textContent = title;
        var pathNode = document.createElement("span");
        pathNode.className = "suggestionPath";
        pathNode.textContent = path;
        text.appendChild(titleNode);
        text.appendChild(pathNode);
        link.appendChild(icon);
        link.appendChild(text);
        item.appendChild(link);
        list.appendChild(item);
    };

    results.folders.slice(0, 4).forEach(function (levels) {
        addSuggestion(getFolderIconName(getDataNode(levels)), levels[levels.length - 1], levels.slice(0, -1).join(" › ") || "Kategorie", buildStartUrl(buildLevelPath(levels)));
    });
    results.media.slice(0, 6).forEach(function (entry) {
        var path = entry.split("|")[0];
        addSuggestion(getMediaIconName(path), getDisplayName(path), getMediaFolders(path).join(" › "), buildStartUrl(buildMediaData(path)));
    });

    var total = results.folders.length + results.media.length;
    var allItem = document.createElement("li");
    allItem.className = "suggestionAll";
    var allLink = document.createElement("a");
    allLink.href = buildStartUrl("SEARCH:" + term, false);
    allLink.textContent = total == 0 ? "Keine Treffer" : "Alle " + total + " Treffer anzeigen";
    if (total == 0) {
        allLink.removeAttribute("href");
        allLink.setAttribute("aria-disabled", "true");
    }
    allItem.appendChild(allLink);
    list.appendChild(allItem);
}

function initSearch() {
    var form = document.getElementById("searchForm");
    var field = document.getElementById("searchField");
    var list = document.getElementById("searchSuggestions");
    var timer = null;

    field.addEventListener("input", function () {
        clearTimeout(timer);
        timer = setTimeout(function () {
            renderSearchSuggestions(field.value);
        }, 120);
    });

    // Pfeiltasten: vom Suchfeld in die Vorschläge und zurück
    field.addEventListener("keydown", function (event) {
        var first = list.querySelector("a[href]");
        if (event.key === "ArrowDown" && first != null) {
            event.preventDefault();
            first.focus();
        }
    });
    list.addEventListener("keydown", function (event) {
        if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
            return;
        }
        event.preventDefault();
        var links = Array.prototype.slice.call(list.querySelectorAll("a[href]"));
        var index = links.indexOf(document.activeElement) + (event.key === "ArrowDown" ? 1 : -1);
        if (index < 0) {
            field.focus();
        } else if (index < links.length) {
            links[index].focus();
        }
    });

    // Klick ausserhalb schliesst das Suchfeld
    document.addEventListener("click", function (event) {
        var container = document.getElementById("searchFieldContainer");
        if (container.style.display != "none" && !form.contains(event.target) && !event.target.closest("#searchToggler")) {
            container.style.display = "none";
        }
    });
}

function showSearchPage(searchTerm) {
    document.getElementById("media").style.display = "block";
    document.body.classList.add("darkPage");

    var results = recursiveSearch(data, searchTerm, [], { folders: [], media: [] });
    sortNatural(results.media);
    results.folders.sort(function (a, b) {
        return compareNatural(a.join("/"), b.join("/"));
    });

    setData("playlist-searchResults", results.media.join(listSeperator));

    var total = results.folders.length + results.media.length;
    var section = createResultSection(total + (total == 1 ? " Treffer" : " Treffer") + " für «" + searchTerm + "»");
    section.insertBefore(createSearchRefineForm(searchTerm), section.firstChild);
    if (total == 0) {
        var hint = document.createElement("p");
        hint.textContent = "Nichts gefunden. Versuche einen kürzeren oder anderen Suchbegriff.";
        section.appendChild(hint);
    }

    if (results.folders.length > 0) {
        var folderHeading = document.createElement("h3");
        folderHeading.textContent = "Ordner";
        section.appendChild(folderHeading);
        var folderGrid = createCardGrid(section);
        results.folders.forEach(function (folderLevels) {
            createFolderCard(folderGrid, folderLevels, { subtitle: folderLevels.slice(0, -1).join(" › ") });
        });
    }

    if (results.media.length > 0) {
        var mediaHeading = document.createElement("h3");
        mediaHeading.textContent = "Medien";
        section.appendChild(mediaHeading);
        var mediaGrid = createCardGrid(section);
        results.media.forEach(function (entry) {
            var path = entry.split("|")[0];
            createMediaCard(mediaGrid, path, entry.split("|")[1], { progress: getMediaProgress(path) });
        });
    }
}

// Alle vorhandenen Favoriten, sortiert nach Pfad (damit nach Kategorie gruppiert).
// Speichert dazu die Favoriten-Playliste in derselben Reihenfolge.
function buildFavoriteEntries() {
    var entries = [];
    sortNatural(getFavorites().filter(mediaExists)).forEach(function (path) {
        var url = buildStartUrl(buildMediaData(path, "favoritesList"));
        if (url != null) {
            entries.push({ path: path, url: url });
        }
    });
    setData("playlist-favoritesList", entries.map(function (entry) {
        return entry.path + "|" + entry.url;
    }).join(listSeperator));
    return entries;
}

function showFavoritesPage() {
    document.getElementById("media").style.display = "block";
    document.body.classList.add("darkPage");

    var entries = buildFavoriteEntries();
    var section = createResultSection("Favoriten (" + entries.length + ")");

    if (entries.length == 0) {
        var hint = document.createElement("p");
        hint.textContent = "Noch keine Favoriten. Mit dem Button «Merken» unter einem Medium kannst du Titel hinzufügen.";
        section.appendChild(hint);
        return;
    }

    var actions = document.createElement("div");
    actions.className = "resultActions";
    var playAll = document.createElement("a");
    playAll.className = "sflixButton primary";
    playAll.href = entries[0].url;
    playAll.appendChild(createIcon("play"));
    playAll.appendChild(document.createTextNode("Alle abspielen"));
    actions.appendChild(playAll);
    section.appendChild(actions);

    // Nach Hauptkategorie (Serien, Filme, Musik, ...) unterteilt
    var groups = [];
    entries.forEach(function (entry) {
        var category = getMediaFolders(entry.path)[0] || "Sonstiges";
        if (groups.length == 0 || groups[groups.length - 1].category !== category) {
            groups.push({ category: category, entries: [] });
        }
        groups[groups.length - 1].entries.push(entry);
    });

    groups.forEach(function (group) {
        var heading = document.createElement("h3");
        heading.textContent = group.category + " (" + group.entries.length + ")";
        section.appendChild(heading);
        var grid = createCardGrid(section);
        group.entries.forEach(function (entry) {
            createMediaCard(grid, entry.path, entry.url, {
                subtitle: getMediaFolders(entry.path).slice(1).join(" › ") || getMediaTypeLabel(entry.path),
                progress: getMediaProgress(entry.path)
            });
        });
    });
}

// Leeres Band bleibt unsichtbar, erscheint aber unter "Startseite anpassen",
// damit sich seine Position auch ohne Einträge festlegen lässt
function markRowEmpty(track) {
    var row = track.closest(".cardRow");
    row.classList.add("emptyRow");
    var hint = document.createElement("span");
    hint.className = "emptyRowHint";
    hint.textContent = "zurzeit leer";
    row.querySelector(".rowHeader").appendChild(hint);
}

// Startseite: die zuletzt hinzugefügten Favoriten (max. 20)
function showFavoritesRow(container) {
    var entries = buildFavoriteEntries();
    var track = createCardRow(container, "Favoriten", buildStartUrl("FAVORITES", false), "favorites");
    if (entries.length == 0) {
        markRowEmpty(track);
        return false;
    }
    var order = getFavorites();
    entries.sort(function (a, b) {
        return order.indexOf(b.path) - order.indexOf(a.path);
    });

    entries.slice(0, 20).forEach(function (entry) {
        createMediaCard(track, entry.path, entry.url, { progress: getMediaProgress(entry.path) });
    });
    return true;
}

// ===================================================================
// Startseite (Dashboard)
// ===================================================================

function shuffle(list) {
    for (var index = list.length - 1; index > 0; index--) {
        var other = Math.floor(Math.random() * (index + 1));
        var temp = list[index];
        list[index] = list[other];
        list[other] = temp;
    }
    return list;
}

// Einträge einer Kategorie: Ordner mit Inhalt (Serien, Alben, ...) und Medien,
// die direkt in der Kategorie liegen (z.B. Filme ohne eigenen Ordner)
function getCategoryEntries(category) {
    var node = data[category];
    var entries = [];
    if (node == null || typeof node !== "object") {
        return entries;
    }
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            entries.push({ kind: "media", path: normalizeMediaLocation(key) });
        } else if (hasMedia(node[key], true)) {
            entries.push({ kind: "folder", levels: [category, key] });
        }
    }
    return entries;
}

// Zuletzt abgespielte Medien mit Fortschrittsbalken
function showContinueWatching(container) {
    var entries = [];
    (getData("playlast") || "").split(listSeperator).forEach(function (entry) {
        if (!entry.includes("|")) {
            return;
        }
        var url = entry.split("|")[1];
        var mediaPath = normalizeMediaLocation(getMediaLocationFromDecodedData(decodeUrlData(url)));
        if (mediaPath == null || !mediaExists(mediaPath)) {
            return;
        }
        var nextPath = getNextMediaPath(mediaPath);
        // Fertig geschaut und es gibt eine nächste Folge: stattdessen diese anbieten
        if (isSeen(mediaPath) && nextPath != null) {
            entries.push({ path: nextPath, url: buildStartUrl(buildMediaData(nextPath)), isNext: true, isLast: getNextMediaPath(nextPath) == null });
            return;
        }
        // Letzter Eintrag der Wiedergabeliste (z.B. letzte Folge der letzten Staffel) wird markiert
        entries.push({ path: mediaPath, url: url, isLast: nextPath == null });
    });

    var track = createCardRow(container, "Weiterschauen", null, "continue");
    if (entries.length == 0) {
        markRowEmpty(track);
        return false;
    }

    // Anzahl gemäss Einstellungen (gespeichert werden bis zu 50)
    entries.slice(0, getSetting("continueMax")).forEach(function (entry) {
        var folders = getMediaFolders(entry.path);
        var location = folders.length > 1 ? folders.slice(1).join(" › ") : folders.join(" › ");
        createMediaCard(track, entry.path, entry.url, {
            subtitle: entry.isNext ? "Nächste Folge · " + location : location,
            progress: getMediaProgress(entry.path),
            label: entry.isLast ? (folders.length > 1 ? "Letzte Folge" : "Letzter Titel") : null
        });
    });
    return true;
}

// Pro Kategorie ein Menüband mit bis zu 20 zufälligen Einträgen
function showCategoryRows(container) {
    var shown = false;
    var categories = sortNatural(Object.keys(data).filter(function (key) {
        return key != "Preview" && data[key] != null && typeof data[key] === "object";
    }));

    categories.forEach(function (category) {
        var entries = shuffle(getCategoryEntries(category)).slice(0, 20);
        if (entries.length == 0) {
            return;
        }
        var track = createCardRow(container, category, buildStartUrl("LEVEL0:" + category), "category:" + category);
        entries.forEach(function (entry) {
            if (entry.kind == "folder") {
                createFolderCard(track, entry.levels);
            } else {
                var href = buildStartUrl(buildMediaData(entry.path));
                if (href != null) {
                    // Eigenes Vorschaubild oder das des Ordners darüber, nicht das Bild der ganzen Kategorie
                    createMediaCard(track, entry.path, href, {
                        subtitle: getMediaTypeLabel(entry.path),
                        inheritPreview: true,
                        progress: getMediaProgress(entry.path)
                    });
                }
            }
        });
        shown = true;
    });
    return shown;
}

function showEmptyState(container) {
    var box = document.createElement("section");
    box.className = "emptyState";
    var heading = document.createElement("h2");
    heading.textContent = "Willkommen bei STEFFLIX";
    var text = document.createElement("p");
    text.textContent = "Es wurden noch keine Medien gefunden. Kopiere Videos, Musik oder Bilder in einen Unterordner von STEFFLIX und führe anschliessend updateData.exe aus.";
    var link = document.createElement("a");
    link.className = "sflixButton primary";
    link.href = buildStartUrl("HELP", false);
    link.textContent = "Zur Anleitung";
    box.appendChild(heading);
    box.appendChild(text);
    box.appendChild(link);
    container.appendChild(box);
}

// STEFFLIX entstand als Geschenk für Steffi. An zwei Tagen im Jahr zeigt der Splashscreen
// eine Widmung (und bleibt dafür länger stehen), sonst null.
function getSplashDedication(date) {
    if (date.getMonth() === 0 && date.getDate() === 13) {
        return { text: "Happy Birthday Steffi – Wir vermissen dich", birthday: true };
    }
    if (date.getMonth() === 9 && date.getDate() === 22) {
        return { text: "In Gedenken an Steffi – 1971 – 2022", birthday: false };
    }
    return null;
}

// Lokales Datum als JJJJ-MM-TT (toISOString wäre UTC: kurz nach Mitternacht noch der Vortag)
function getLocalDateKey(date) {
    return date.getFullYear() + "-" + String(date.getMonth() + 1).padStart(2, "0") + "-" + String(date.getDate()).padStart(2, "0");
}

// Splashscreen einmal pro Tag (lokales Datum), immer bildschirmfüllend. Klick überspringt ihn.
// Immer, egal was eingestellt ist:
//   - an den zwei Gedenktagen (13. Januar, 22. Oktober) beim ersten Start des Tages, mit Widmung
//   - beim ersten Start nach einem Update auf eine neue STEFFLIX-Version (lastSplashVersion)
function showSplashscreen() {
    var now = new Date();
    var lastSplashscreen = getData("lastSplashscreen");
    var currentDate = getLocalDateKey(now);
    var lastVersion = getData("lastSplashVersion");
    var newVersion = lastVersion == null || compareVersions(getVersionName(), lastVersion) > 0;
    if (!newVersion) {
        if (getSetting("skipSplash") && getSplashDedication(now) == null) {
            return;
        }
        if (lastSplashscreen != null && lastSplashscreen >= currentDate) {
            return;
        }
    }
    setData("lastSplashVersion", getVersionName());

    var splashNode = document.getElementById("splashscreen");
    var image = document.createElement("img");
    image.src = "sflix_sys/splash.svg";
    image.alt = "";
    splashNode.appendChild(image);

    var dedication = getSplashDedication(new Date());
    if (dedication != null) {
        var dedicationNode = document.createElement("p");
        dedicationNode.className = "splashDedication" + (dedication.birthday ? " birthday" : "");
        dedicationNode.textContent = dedication.text;
        splashNode.appendChild(dedicationNode);
        splashNode.classList.add("withDedication");
    }
    splashNode.style.display = "block";

    var hide = function () {
        splashNode.style.display = "none";
    };
    // Nur das Ende der eigenen Ausblend-Animation zählt (nicht das der Widmung)
    splashNode.addEventListener("animationend", function (event) {
        if (event.target === splashNode) {
            hide();
        }
    });
    splashNode.addEventListener("click", hide);
    setData("lastSplashscreen", currentDate);
}

// ===================================================================
// Update-Hinweis
// ===================================================================

// Ausgeblendet für eine Version: { version: "2.5.0", until: Zeitpunkt in ms, 0 = bis zur nächsten Version }
// (bis 2.04 stand dort die Zahl, z.B. 2.04)
function isUpdateBannerDismissed(newestVersion) {
    var dismissed = null;
    try {
        dismissed = JSON.parse(getData("updateBannerDismissed") || "null");
    } catch (error) {
        dismissed = null;
    }
    var dismissedVersion = dismissed != null ? toVersionName(dismissed.version) : null;
    if (dismissedVersion == null || compareVersions(newestVersion, dismissedVersion) > 0) {
        return false;
    }
    if (dismissed.until === 0) {
        return true;
    }
    return Date.now() < dismissed.until;
}

function dismissUpdateBanner(banner, newestVersion, days, storageKey) {
    setData(storageKey || "updateBannerDismissed", JSON.stringify({
        version: newestVersion,
        until: days > 0 ? Date.now() + days * 24 * 60 * 60 * 1000 : 0
    }));
    banner.classList.add("hiding");
    setTimeout(function () {
        banner.remove();
    }, 300);
}

// Hinweis auf einen neuen Updater: wie beim STEFFLIX-Update, Versionen als Text ("3.2.1")
function isUpdaterBannerDismissed(newestUpdater) {
    var dismissed = null;
    try {
        dismissed = JSON.parse(getData("updaterBannerDismissed") || "null");
    } catch (error) {
        dismissed = null;
    }
    if (dismissed == null || typeof dismissed.version !== "string" || compareVersions(newestUpdater, dismissed.version) > 0) {
        return false;
    }
    return dismissed.until === 0 || Date.now() < dismissed.until;
}

// options (nur für den Updater): { heading, meta, storageKey }
function createUpdateBanner(newestVersion, options) {
    options = options || {};
    var banner = document.createElement("section");
    banner.className = "updateBanner";
    banner.setAttribute("aria-label", "Update verfügbar");

    var icon = document.createElement("div");
    icon.className = "updateIcon";
    icon.appendChild(createIcon("arrowUp"));

    var content = document.createElement("div");
    content.className = "updateContent";
    var heading = document.createElement("h2");
    heading.textContent = options.heading || "STEFFLIX " + newestVersion + " ist verfügbar";
    var meta = document.createElement("p");
    meta.className = "updateMeta";
    meta.textContent = options.meta || "Installiert ist Version " + getVersionName() + ".";
    var newsList = document.createElement("ul");
    newsList.className = "updateNews";
    newsList.style.display = "none";

    var actions = document.createElement("div");
    actions.className = "updateActions";
    var helpLink = document.createElement("a");
    helpLink.className = "sflixButton primary";
    helpLink.href = buildStartUrl("HELP", false) + "#stefflixUpdate";
    helpLink.textContent = "So aktualisierst du";
    actions.appendChild(helpLink);

    var snooze = document.createElement("div");
    snooze.className = "updateSnooze";
    var snoozeLabel = document.createElement("span");
    snoozeLabel.textContent = "Ausblenden:";
    snooze.appendChild(snoozeLabel);
    [["1 Tag", 1], ["1 Woche", 7], ["1 Monat", 30], ["Bis zur nächsten Version", 0]].forEach(function (option) {
        var button = document.createElement("button");
        button.type = "button";
        button.className = "sflixButton ghost small";
        button.textContent = option[0];
        button.addEventListener("click", function () {
            dismissUpdateBanner(banner, newestVersion, option[1], options.storageKey);
        });
        snooze.appendChild(button);
    });
    actions.appendChild(snooze);

    content.appendChild(heading);
    content.appendChild(meta);
    content.appendChild(newsList);
    content.appendChild(actions);
    banner.appendChild(icon);
    banner.appendChild(content);
    return banner;
}

// Neuerungen der fehlenden Versionen im Update-Hinweis anzeigen (max. 5)
function fillUpdateNews(banner, newestVersion) {
    fetchText(newsUrl).then(function (text) {
        var news = null;
        try {
            news = JSON.parse(text);
        } catch (error) {
            return;
        }
        if (news == null || !Array.isArray(news.versions)) {
            return;
        }
        var items = [];
        var installed = getVersionName();
        news.versions.forEach(function (entry) {
            var entryVersion = getNewsVersionName(entry);
            if (entryVersion != null && compareVersions(entryVersion, installed) > 0
                && compareVersions(entryVersion, newestVersion) <= 0 && Array.isArray(entry.items)) {
                items = items.concat(entry.items);
            }
        });
        var list = banner.querySelector(".updateNews");
        items.slice(0, 5).forEach(function (item) {
            if (item == null || typeof item.text !== "string") {
                return;
            }
            var entry = document.createElement("li");
            entry.textContent = item.text;
            list.appendChild(entry);
        });
        if (list.children.length > 0) {
            list.style.display = "";
        }
    });
}

// Startseite: Hinweis auf eine neue STEFFLIX-Version, sonst auf einen neuen Updater
// (abschaltbar unter Einstellungen › Startseite)
function checkForUpdate(container) {
    if (!getSetting("showUpdateHints")) {
        return;
    }
    fetchText(versionUrl).then(function (text) {
        var newestVersion = parseVersionJs(text);
        if (newestVersion != null && compareVersions(newestVersion, getVersionName()) > 0) {
            // Das STEFFLIX-Update bringt auch den neuen Updater mit
            if (!isUpdateBannerDismissed(newestVersion)) {
                var banner = createUpdateBanner(newestVersion);
                container.insertBefore(banner, container.firstChild);
                fillUpdateNews(banner, newestVersion);
            }
            return;
        }
        checkForUpdaterUpdate(container);
    });
}

// Neueste Versionen von Updater und ffmpeg (versions.json auf GitHub), sonst null
function fetchVersions() {
    return fetchText(versionsUrl).then(function (text) {
        try {
            var versions = JSON.parse(text);
            return versions != null && typeof versions === "object" ? versions : null;
        } catch (error) {
            return null;
        }
    });
}

function checkForUpdaterUpdate(container) {
    var info = getUpdaterInfo();
    if (info == null || !info.updater) {
        return;
    }
    fetchVersions().then(function (versions) {
        if (versions == null || typeof versions.updater !== "string" || compareVersions(versions.updater, info.updater) <= 0 || isUpdaterBannerDismissed(versions.updater)) {
            return;
        }
        var banner = createUpdateBanner(versions.updater, {
            heading: "Updater " + versions.updater + " ist verfügbar",
            meta: "Installiert ist Version " + info.updater + ". Beim Start von updateStefflix.exe die Aufgabe \"Update\" ausführen.",
            storageKey: "updaterBannerDismissed"
        });
        container.insertBefore(banner, container.firstChild);
    });
}

// Öffne Hilfeseite
function openHelp() {
    window.location.href = buildStartUrl("HELP", false);
}

// Öffne Favoritenseite
function openFavorites() {
    window.location.href = buildStartUrl("FAVORITES", false);
}

// ===================================================================
// Versionsprüfung und Hilfeseite (asynchron, blockiert den Start nicht)
// ===================================================================

function fetchText(url) {
    if (!window.fetch) {
        return Promise.resolve(null);
    }
    return fetch(url, { cache: "no-store" }).then(function (response) {
        return response.ok ? response.text() : null;
    }).catch(function () {
        return null;
    });
}

// Version eines Eintrags in news.json als x.y.z ("name" ab 2.5.0, sonst die alte Zahl "version")
function getNewsVersionName(entry) {
    if (entry == null) {
        return null;
    }
    return toVersionName(entry.name != null ? entry.name : entry.version);
}

// Startseite: Update-Hinweis, Weiterschauen, pro Kategorie ein Menüband
function showStartPage() {
    var mediaNode = document.getElementById("media");
    mediaNode.style.display = "block";
    document.body.classList.add("darkPage");

    showSplashscreen();

    var dashboard = document.createElement("div");
    dashboard.id = "dashboard";
    mediaNode.appendChild(dashboard);

    checkForUpdate(dashboard);
    showLicenseChangeNotice(dashboard);

    var hasContinue = showContinueWatching(dashboard);
    var hasFavorites = showFavoritesRow(dashboard);
    var hasCategories = showCategoryRows(dashboard);
    if (!hasContinue && !hasFavorites && !hasCategories) {
        showEmptyState(dashboard);
    }

    initDashboardSorting(dashboard);

    // Pfeile erst nach dem Aufbau (Breiten bekannt) ein-/ausblenden
    requestAnimationFrame(function () {
        dashboard.querySelectorAll(".rowTrack").forEach(function (track) {
            track.updateArrows();
        });
    });
}

// ===================================================================
// Startseite anpassen: eigene Reihenfolge der Menübänder
// ===================================================================
var dashboardOrderKey = "dashboardOrder";

function getDashboardRows(dashboard) {
    return Array.prototype.slice.call(dashboard.querySelectorAll(":scope > .cardRow[data-row-key]"));
}

function getSavedRowOrder() {
    try {
        var order = JSON.parse(getData(dashboardOrderKey) || "[]");
        return Array.isArray(order) ? order : [];
    } catch (error) {
        return [];
    }
}

// Speichert die angezeigte Reihenfolge. Bänder, die gerade fehlen (z.B. eine Kategorie
// ohne Medien), behalten ihre gespeicherte Position
// neben ihrem bisherigen Vorgänger, statt aus der Reihenfolge zu fallen.
function saveRowOrder(dashboard) {
    var saved = getSavedRowOrder();
    var order = getDashboardRows(dashboard).map(function (row) {
        return row.dataset.rowKey;
    });
    saved.forEach(function (key, index) {
        if (order.includes(key)) {
            return;
        }
        var previous = saved.slice(0, index).reverse().find(function (candidate) {
            return order.includes(candidate);
        });
        if (previous != null) {
            order.splice(order.indexOf(previous) + 1, 0, key);
        } else {
            order.unshift(key);
        }
    });
    setData(dashboardOrderKey, JSON.stringify(order));
    applyMenuOrder();
}

// Bänder in die gewünschte Reihenfolge bringen. Neue Bänder (z.B. eine neue Kategorie)
// erscheinen an ihrer gewohnten Stelle, ohne die eigene Reihenfolge zu verändern.
function arrangeRows(dashboard, order, defaultRows) {
    arrangeByKeys(defaultRows, function (row) {
        return row.dataset.rowKey;
    }, order).forEach(function (row) {
        dashboard.appendChild(row);
    });
    applyMenuOrder();
}

// Sortiert Elemente nach einer gespeicherten Schlüsselliste. Elemente ohne gespeicherte
// Position kommen hinter das Element, das ihnen in der Standardreihenfolge vorangeht
// (oder ganz nach vorne, z.B. "Weiterschauen").
function arrangeByKeys(defaultItems, getKey, order) {
    var byKey = {};
    defaultItems.forEach(function (item) {
        byKey[getKey(item)] = item;
    });
    var result = order.filter(function (key) {
        return byKey[key] != null;
    }).map(function (key) {
        return byKey[key];
    });

    defaultItems.forEach(function (item, index) {
        if (result.includes(item)) {
            return;
        }
        var previousKnown = defaultItems.slice(0, index).reverse().find(function (candidate) {
            return result.includes(candidate);
        });
        if (previousKnown != null) {
            result.splice(result.indexOf(previousKnown) + 1, 0, item);
        } else {
            result.unshift(item);
        }
    });
    return result;
}

// Hauptmenü in der Reihenfolge der Kategorie-Bänder der Startseite
function applyMenuOrder() {
    var menu = document.getElementById("menu");
    var defaultItems = Array.prototype.slice.call(menu.children).sort(function (a, b) {
        return compareNatural(a.dataset.sort || "", b.dataset.sort || "");
    });
    var order = getSavedRowOrder().filter(function (key) {
        return key.startsWith("category:");
    }).map(function (key) {
        return key.slice("category:".length);
    });
    arrangeByKeys(defaultItems, function (item) {
        return item.dataset.level;
    }, order).forEach(function (item) {
        menu.appendChild(item);
    });
}

function moveRow(dashboard, row, direction) {
    var rows = getDashboardRows(dashboard);
    var target = rows[rows.indexOf(row) + direction];
    if (target == null) {
        return;
    }
    if (direction < 0) {
        dashboard.insertBefore(row, target);
    } else {
        target.after(row);
    }
    saveRowOrder(dashboard);
}

function createRowControlButton(iconName, label) {
    var button = document.createElement("button");
    button.type = "button";
    button.className = "rowControl";
    button.setAttribute("aria-label", label);
    button.title = label;
    button.appendChild(createIcon(iconName));
    return button;
}

// Griff (Ziehen) und Pfeile (Tastatur/Klick) im Kopf eines Bandes
function addRowControls(dashboard, row) {
    var controls = document.createElement("div");
    controls.className = "rowControls";
    var handle = createRowControlButton("grip", "Band verschieben (ziehen)");
    handle.classList.add("rowHandle");
    var upButton = createRowControlButton("chevronUp", "Band nach oben");
    var downButton = createRowControlButton("chevronDown", "Band nach unten");
    controls.appendChild(handle);
    controls.appendChild(upButton);
    controls.appendChild(downButton);
    row.querySelector(".rowHeader").prepend(controls);

    upButton.addEventListener("click", function () {
        moveRow(dashboard, row, -1);
        upButton.focus();
    });
    downButton.addEventListener("click", function () {
        moveRow(dashboard, row, 1);
        downButton.focus();
    });
    handle.addEventListener("keydown", function (event) {
        if (event.key === "ArrowUp" || event.key === "ArrowDown") {
            event.preventDefault();
            moveRow(dashboard, row, event.key === "ArrowUp" ? -1 : 1);
            handle.focus();
        }
    });

    // Ziehen mit Maus oder Finger. Bewegung und Loslassen werden auf der ganzen Seite
    // verfolgt: beim Umsortieren wird das Band im Seitenaufbau verschoben, dabei würde
    // eine Bindung an den Griff (pointer capture) verloren gehen.
    var onMove = function (event) {
        var others = getDashboardRows(dashboard).filter(function (other) {
            return other !== row;
        });
        var before = others.find(function (other) {
            var rect = other.getBoundingClientRect();
            return event.clientY < rect.top + rect.height / 2;
        });
        if (before != null) {
            if (row.nextElementSibling !== before) {
                dashboard.insertBefore(row, before);
            }
        } else if (others.length > 0 && others[others.length - 1].nextElementSibling !== row) {
            others[others.length - 1].after(row);
        }
    };
    var onEnd = function () {
        document.removeEventListener("pointermove", onMove);
        document.removeEventListener("pointerup", onEnd);
        document.removeEventListener("pointercancel", onEnd);
        row.classList.remove("dragging");
        document.body.classList.remove("rowSorting");
        saveRowOrder(dashboard);
    };
    handle.addEventListener("pointerdown", function (event) {
        if (event.button !== 0) {
            return;
        }
        event.preventDefault();
        row.classList.add("dragging");
        document.body.classList.add("rowSorting");
        document.addEventListener("pointermove", onMove);
        document.addEventListener("pointerup", onEnd);
        document.addEventListener("pointercancel", onEnd);
    });
}

function setDashboardEditing(dashboard, editing) {
    dashboard.classList.toggle("editing", editing);
    var toolbar = dashboard.querySelector(".dashboardToolbar");
    toolbar.querySelector(".editStart").style.display = editing ? "none" : "";
    toolbar.querySelector(".editActions").style.display = editing ? "" : "none";
    if (!editing) {
        // Pfeile der Bänder neu berechnen, nachdem sie wieder sichtbar sind
        requestAnimationFrame(function () {
            dashboard.querySelectorAll(".rowTrack").forEach(function (track) {
                track.updateArrows();
            });
        });
    }
}

function initDashboardSorting(dashboard) {
    var defaultRows = getDashboardRows(dashboard);
    arrangeRows(dashboard, getSavedRowOrder(), defaultRows);

    // Anpassen lohnt sich erst ab zwei Bändern
    if (defaultRows.length < 2) {
        return;
    }

    var toolbar = document.createElement("div");
    toolbar.className = "dashboardToolbar";

    var startButton = document.createElement("button");
    startButton.type = "button";
    startButton.className = "sflixButton ghost small editStart";
    startButton.appendChild(createIcon("sliders"));
    startButton.appendChild(document.createTextNode("Startseite anpassen"));

    var actions = document.createElement("div");
    actions.className = "editActions";
    actions.style.display = "none";
    var hint = document.createElement("span");
    hint.className = "editHint";
    hint.textContent = "Bänder am Griff ziehen oder mit den Pfeilen verschieben.";
    var resetButton = document.createElement("button");
    resetButton.type = "button";
    resetButton.className = "sflixButton ghost small";
    resetButton.textContent = "Standard wiederherstellen";
    var doneButton = document.createElement("button");
    doneButton.type = "button";
    doneButton.className = "sflixButton primary small editDone";
    doneButton.textContent = "Fertig";
    actions.appendChild(hint);
    actions.appendChild(resetButton);
    actions.appendChild(doneButton);

    toolbar.appendChild(startButton);
    toolbar.appendChild(actions);
    // Über dem ersten Band (der Update-Hinweis wird später ganz oben eingefügt)
    dashboard.insertBefore(toolbar, getDashboardRows(dashboard)[0]);

    defaultRows.forEach(function (row) {
        addRowControls(dashboard, row);
    });

    startButton.addEventListener("click", function () {
        setDashboardEditing(dashboard, true);
        doneButton.focus();
    });
    doneButton.addEventListener("click", function () {
        setDashboardEditing(dashboard, false);
        startButton.focus();
    });
    resetButton.addEventListener("click", function () {
        removeData(dashboardOrderKey);
        arrangeRows(dashboard, [], defaultRows);
    });
}

// Escape beendet den Bearbeiten-Modus der Startseite
function stopDashboardEditing() {
    var dashboard = document.getElementById("dashboard");
    if (dashboard != null && dashboard.classList.contains("editing")) {
        setDashboardEditing(dashboard, false);
        return true;
    }
    return false;
}

function showHelpPage() {
    document.getElementById("media").style.display = "block";
    document.getElementById("sflixPreview").src = previewGifUrl;
    document.getElementById("help").style.display = "grid";

    var browserName = detectBrowser();
    var helpIds = { "firefox": "firefoxBrowserHelp", "chrome": "chromeBrowserHelp", "edge": "edgeBrowserHelp" };
    document.getElementById(helpIds[browserName] || "otherBrowserHelp").style.display = "block";

    document.getElementById("versionNow").textContent = getVersionName();
    var versionNewNode = document.getElementById("versionNew");
    versionNewNode.textContent = "wird geprüft...";

    var changelogNode = document.createElement("div");
    changelogNode.id = "changelog";
    changelogNode.textContent = "Changelog wird geladen...";
    document.getElementById("help").appendChild(changelogNode);

    fetchText(versionUrl).then(function (text) {
        var newestVersion = parseVersionJs(text);
        versionNewNode.textContent = newestVersion != null ? newestVersion : "unbekannt (keine Internetverbindung?)";
        markVersionCell(versionNewNode, newestVersion != null && compareVersions(newestVersion, getVersionName()) > 0);
    });
    fillToolVersions();

    // Symbole der Player-Leiste in den Erklärungen
    fillHelpIcons(document.getElementById("help"));

    fetchText(newsUrl).then(function (text) {
        var news = null;
        try {
            news = JSON.parse(text);
        } catch (error) {
            news = null;
        }
        if (news == null || !Array.isArray(news.versions)) {
            var notice = document.createElement("p");
            notice.className = "changelogNotice";
            notice.textContent = text == null
                ? "Der Changelog konnte nicht geladen werden. Er wird online abgerufen und braucht deshalb eine Internetverbindung."
                : "Der Changelog konnte nicht gelesen werden.";
            changelogNode.replaceChildren(notice);
            return;
        }
        renderNews(changelogNode, news);
    });

    buildSectionPage("help", "Hilfe", "Medien hinzufügen, Autoplay, Player, Tastenkürzel, Browsereinstellungen, Updates und Einstellungen");
}

// Neuere Version verfügbar: Zelle hervorheben
function markVersionCell(cell, isNewer) {
    cell.classList.toggle("versionNewer", isNewer);
}

// Hilfe: installierte (updater_info.js) und neueste Versionen (versions.json) von Updater und ffmpeg
function fillToolVersions() {
    var info = getUpdaterInfo();
    var unknown = "unbekannt (Updater starten)";
    document.getElementById("updaterVersionNow").textContent = info && info.updater ? info.updater : unknown;
    document.getElementById("ffmpegVersionNow").textContent = info ? (info.ffmpeg || "nicht installiert") : unknown;
    var updaterNew = document.getElementById("updaterVersionNew");
    var ffmpegNew = document.getElementById("ffmpegVersionNew");
    updaterNew.textContent = "wird geprüft...";
    ffmpegNew.textContent = "wird geprüft...";
    fetchVersions().then(function (versions) {
        var offline = "unbekannt";
        updaterNew.textContent = versions && versions.updater ? versions.updater : offline;
        ffmpegNew.textContent = versions && versions.ffmpeg ? versions.ffmpeg : offline;
        if (versions && info) {
            markVersionCell(updaterNew, !!versions.updater && !!info.updater && compareVersions(versions.updater, info.updater) > 0);
            // ffmpeg wird nur bei Bedarf installiert: nur hervorheben, wenn eine ältere installiert ist
            markVersionCell(ffmpegNew, !!versions.ffmpeg && !!info.ffmpeg && versions.ffmpeg !== info.ffmpeg);
        }
    });
}

// Ersetzt <span class="helpIcon" data-icon="..."> durch das Symbol aus iconShapes
function fillHelpIcons(root) {
    root.querySelectorAll(".helpIcon[data-icon]").forEach(function (node) {
        if (node.firstChild == null) {
            node.appendChild(createIcon(node.dataset.icon));
        }
    });
}

// Seite "Einstellungen": gleicher Aufbau wie die Hilfe (Abschnitte und Übersicht)
function showSettingsPage() {
    document.getElementById("media").style.display = "block";
    document.getElementById("settingsPage").style.display = "grid";
    fillHelpIcons(document.getElementById("settingsPage"));
    buildSectionPage("settingsPage", "Einstellungen", "Darstellung, Sortierung, Startseite, Wiedergabe, Datensicherung, Fehlerbehebung und Lizenz");
    initSettingsPage();
    initBackup();
}

// Öffne Einstellungen, optional bei einem Abschnitt (z.B. "datensicherung")
function openSettings(anchor) {
    window.location.href = buildStartUrl("SETTINGS", false) + (anchor ? "#" + anchor : "");
}

// Gliedert eine Seite (Hilfe, Einstellungen) in Abschnitte (eine Karte pro Überschrift) und
// erstellt rechts eine Übersicht, die den aktuell sichtbaren Abschnitt markiert.
function buildSectionPage(pageId, pageTitle, pageInfo) {
    var help = document.getElementById(pageId);
    var content = document.createElement("div");
    content.className = "helpContent";
    var toc = document.createElement("nav");
    toc.className = "helpToc";
    toc.setAttribute("aria-label", "Inhalt der Hilfe");
    var tocTitle = document.createElement("h4");
    tocTitle.textContent = "Inhalt";
    toc.appendChild(tocTitle);

    var section = null;
    var tocLinks = [];
    Array.prototype.slice.call(help.childNodes).forEach(function (node) {
        // Leerzeilen-Abstände zwischen den Punkten werden durch die Karten ersetzt
        if (node.nodeName === "BR") {
            node.remove();
            return;
        }
        if (node.nodeName === "H3") {
            section = document.createElement("section");
            section.className = "helpSection";
            section.id = pageId + "-" + tocLinks.length;
            content.appendChild(section);

            var link = document.createElement("a");
            link.href = "#";
            link.textContent = node.textContent;
            link.dataset.section = section.id;
            (function (target) {
                link.addEventListener("click", function (event) {
                    // Ohne Sprungmarke in der URL scrollen (die Zurück-Logik würde sonst neu laden)
                    event.preventDefault();
                    target.scrollIntoView({ behavior: "smooth", block: "start" });
                });
            })(section);
            toc.appendChild(link);
            tocLinks.push(link);
        }
        if (section == null) {
            // Leerraum vor der ersten Überschrift ergibt keine eigene Karte
            if (node.nodeType === 3 && node.textContent.trim() === "") {
                return;
            }
            section = document.createElement("section");
            section.className = "helpSection";
            content.appendChild(section);
        }
        section.appendChild(node);
    });

    // Seitenkopf wie bei den Ordnerseiten
    var header = document.createElement("div");
    header.className = "helpHeader";
    var breadcrumb = document.createElement("nav");
    breadcrumb.className = "breadcrumb";
    breadcrumb.setAttribute("aria-label", "Pfad");
    var startLink = document.createElement("a");
    startLink.href = "start.html";
    startLink.textContent = "Start";
    var separator = document.createElement("span");
    separator.className = "breadcrumbSeparator";
    separator.textContent = "›";
    var current = document.createElement("span");
    current.className = "breadcrumbCurrent";
    current.textContent = pageTitle;
    breadcrumb.appendChild(startLink);
    breadcrumb.appendChild(separator);
    breadcrumb.appendChild(current);
    var title = document.createElement("h1");
    title.className = "folderTitle";
    title.textContent = pageTitle;
    var info = document.createElement("p");
    info.className = "folderInfo";
    info.textContent = pageInfo;
    header.appendChild(breadcrumb);
    header.appendChild(title);
    header.appendChild(info);
    document.title = "STEFFLIX - " + pageTitle;

    help.appendChild(header);
    help.appendChild(content);
    help.appendChild(toc);

    // Aktiven Abschnitt in der Übersicht hervorheben
    if (window.IntersectionObserver) {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    tocLinks.forEach(function (link) {
                        link.classList.toggle("active", link.dataset.section === entry.target.id);
                    });
                }
            });
        }, { rootMargin: "-90px 0px -65% 0px" });
        content.querySelectorAll(".helpSection").forEach(function (part) {
            observer.observe(part);
        });
    }
    if (tocLinks.length > 0) {
        tocLinks[0].classList.add("active");
    }

    // Direktaufruf mit Sprungmarke (z.B. #stefflixUpdate aus dem Update-Hinweis)
    if (location.hash.length > 1) {
        var target = document.getElementById(location.hash.slice(1));
        if (target != null) {
            requestAnimationFrame(function () {
                (target.closest(".helpSection") || target).scrollIntoView({ block: "start" });
            });
        }
    }
}

// Erstellt eine Liste von Changelog-Einträgen. Alle Texte werden per textContent
// eingefügt, damit aus dem Netz geladene Inhalte keinen Code ausführen können.
function createNewsList(items) {
    var list = document.createElement("ul");
    (items || []).forEach(function (item) {
        if (item == null || typeof item.text !== "string") {
            return;
        }
        var entry = document.createElement("li");
        var type = String(item.type || "").toUpperCase();
        var style = newsTypeStyles[type] || {};

        var typeNode = document.createElement("span");
        typeNode.textContent = type;
        if (style.color) {
            typeNode.style.color = style.color;
        }
        if (style.bold) {
            typeNode.style.fontWeight = "bold";
        }

        var textNode = document.createElement(item.resolved ? "s" : "span");
        textNode.textContent = item.text;

        entry.appendChild(document.createTextNode("["));
        entry.appendChild(typeNode);
        entry.appendChild(document.createTextNode("] "));
        entry.appendChild(textNode);
        list.appendChild(entry);
    });
    return list;
}

function renderNews(container, news) {
    container.replaceChildren();

    if (Array.isArray(news.knownBugs) && news.knownBugs.length > 0) {
        var bugHeading = document.createElement("b");
        bugHeading.textContent = "Bekannte Fehler:";
        container.appendChild(bugHeading);
        container.appendChild(createNewsList(news.knownBugs));
    }

    news.versions.forEach(function (versionEntry) {
        if (versionEntry == null) {
            return;
        }
        var heading = document.createElement("b");
        heading.textContent = "Version " + (getNewsVersionName(versionEntry) || String(versionEntry.version));
        container.appendChild(heading);
        container.appendChild(document.createTextNode(":"));
        container.appendChild(document.createElement("br"));
        container.appendChild(createNewsList(versionEntry.items));
    });
}
