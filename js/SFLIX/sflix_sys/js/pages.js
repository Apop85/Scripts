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
        exactPreview: true,
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

// Ordnerseite: oben die Medien des Ordners, darunter ein Band pro Unterordner
function renderFolderPage(levels) {
    renderedFolderView = "folder:" + levels.join("/");
    var header = document.getElementById("navigation");
    var bands = document.getElementById("folderBands");
    header.replaceChildren();
    bands.replaceChildren();
    header.className = "folderHeader";

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

    // Medien direkt im Ordner (z.B. Filme ohne eigenen Ordner)
    if (children.media.length > 0) {
        renderGroupedBands(bands, children.media, getDisplayName, children.folders.length > 0 ? "Inhalt" : "Alle Titel", function (track, mediaPath) {
            createPlayableCard(track, mediaPath);
        });
        saveFolderPlaylist(levels, children.media);
    }

    if (shouldShowFolderContents(levels, node, children.folders)) {
        // Wenige Unterordner mit Medien (z.B. Staffeln einer Serie): ein Band pro Ordner mit Inhalt
        children.folders.forEach(function (folder) {
            var subLevels = levels.concat([folder]);
            var track = createCardRow(bands, folder, buildStartUrl(buildLevelPath(subLevels)));
            fillFolderTrack(track, subLevels, node[folder]);
        });
    } else if (children.folders.length > 0) {
        // Viele oder verschachtelte Ordner (z.B. alle Serien): nur die Ordner, alphabetisch gruppiert
        renderGroupedBands(bands, children.folders, function (folder) {
            return folder;
        }, "Alle " + levels[levels.length - 1], function (track, folder) {
            createFolderCard(track, levels.concat([folder]));
        });
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

// Ab dieser Anzahl werden Einträge alphabetisch auf mehrere Bänder verteilt
var groupThreshold = 12;

var alphabetGroups = [
    { label: "0 – 9", pattern: /[0-9]/ },
    { label: "A – D", pattern: /[a-d]/ },
    { label: "E – H", pattern: /[e-h]/ },
    { label: "I – L", pattern: /[i-l]/ },
    { label: "M – P", pattern: /[m-p]/ },
    { label: "Q – T", pattern: /[q-t]/ },
    { label: "U – Z", pattern: /[u-z]/ }
];

// Gruppe anhand des ersten Buchstabens (Umlaute zählen zum Grundbuchstaben: Ä -> A)
function getAlphabetGroup(name) {
    var firstChar = String(name).normalize("NFD").replace(/[̀-ͯ]/g, "").trim().charAt(0).toLowerCase();
    for (var index = 0; index < alphabetGroups.length; index++) {
        if (alphabetGroups[index].pattern.test(firstChar)) {
            return alphabetGroups[index].label;
        }
    }
    return "Sonstige";
}

// Zeigt Einträge in einem Band oder, bei vielen Einträgen, alphabetisch gruppiert
// in mehreren Bändern (0 – 9, A – D, ...). Leere Gruppen werden ausgelassen.
function renderGroupedBands(container, items, getName, singleTitle, renderItem) {
    if (items.length <= groupThreshold) {
        var track = createCardRow(container, singleTitle, null);
        items.forEach(function (item) {
            renderItem(track, item);
        });
        return;
    }

    var groups = {};
    items.forEach(function (item) {
        var label = getAlphabetGroup(getName(item));
        groups[label] = groups[label] || [];
        groups[label].push(item);
    });

    alphabetGroups.map(function (group) {
        return group.label;
    }).concat(["Sonstige"]).forEach(function (label) {
        if (groups[label] == null) {
            return;
        }
        var groupTrack = createCardRow(container, label, null);
        groupTrack.parentNode.parentNode.querySelector(".rowHeader h2").appendChild(createRowCount(groups[label].length));
        groups[label].forEach(function (item) {
            renderItem(groupTrack, item);
        });
    });
}

// Unter dem Player: weitere Medien aus dem Ordner und Nachbarordner (z.B. andere Staffeln)
function renderMediaContext(mediaPath) {
    var folders = getMediaFolders(mediaPath);
    var viewKey = "media:" + folders.join("/");

    if (renderedFolderView !== viewKey) {
        renderedFolderView = viewKey;
        var header = document.getElementById("navigation");
        var bands = document.getElementById("folderBands");
        header.replaceChildren();
        bands.replaceChildren();
        header.className = "folderHeader playerHeader";
        renderBreadcrumb(header, folders, true);
        markActiveCategory(folders[0]);

        var node = getDataNode(folders);
        var folderName = folders.length > 0 ? folders[folders.length - 1] : "STEFFLIX";
        var children = getSortedChildren(node);

        if (children.media.length > 0) {
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

    updateCurrentCard(mediaPath);
}

// Hebt das laufende Medium in den Bändern hervor und scrollt es ins Bild
function updateCurrentCard(mediaPath) {
    document.querySelectorAll("#folderBands .card.current[data-media]").forEach(function (card) {
        card.classList.remove("current");
    });
    document.querySelectorAll('#folderBands .card[data-media="' + CSS.escape(mediaPath) + '"]').forEach(function (card) {
        card.classList.add("current");
        var track = card.parentNode;
        requestAnimationFrame(function () {
            track.scrollTo({ left: Math.max(0, card.offsetLeft - track.clientWidth / 2 + card.clientWidth / 2), behavior: "smooth" });
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

// Startseite: die zuletzt hinzugefügten Favoriten (max. 20)
function showFavoritesRow(container) {
    var entries = buildFavoriteEntries();
    if (entries.length == 0) {
        return false;
    }
    var order = getFavorites();
    entries.sort(function (a, b) {
        return order.indexOf(b.path) - order.indexOf(a.path);
    });

    var track = createCardRow(container, "Meine Favoriten", buildStartUrl("FAVORITES", false), "favorites");
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

    if (entries.length == 0) {
        return false;
    }

    var track = createCardRow(container, "Weiterschauen", null, "continue");
    entries.forEach(function (entry) {
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
                    // Nur das eigene Vorschaubild, nicht das Bild der ganzen Kategorie
                    createMediaCard(track, entry.path, href, {
                        subtitle: getMediaTypeLabel(entry.path),
                        exactPreview: true,
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

// Splashscreen einmal pro Tag, immer bildschirmfüllend. Klick überspringt ihn.
function showSplashscreen() {
    var lastSplashscreen = getData("lastSplashscreen");
    var currentDate = new Date().toISOString().split("T")[0];
    if (lastSplashscreen != null && lastSplashscreen >= currentDate) {
        return;
    }

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

// Ausgeblendet für eine Version: { version: 2.1, until: Zeitpunkt in ms, 0 = bis zur nächsten Version }
function isUpdateBannerDismissed(newestVersion) {
    var dismissed = null;
    try {
        dismissed = JSON.parse(getData("updateBannerDismissed") || "null");
    } catch (error) {
        dismissed = null;
    }
    if (dismissed == null || typeof dismissed.version !== "number" || newestVersion > dismissed.version) {
        return false;
    }
    if (dismissed.until === 0) {
        return true;
    }
    return Date.now() < dismissed.until;
}

function dismissUpdateBanner(banner, newestVersion, days) {
    setData("updateBannerDismissed", JSON.stringify({
        version: newestVersion,
        until: days > 0 ? Date.now() + days * 24 * 60 * 60 * 1000 : 0
    }));
    banner.classList.add("hiding");
    setTimeout(function () {
        banner.remove();
    }, 300);
}

function createUpdateBanner(newestVersion) {
    var banner = document.createElement("section");
    banner.className = "updateBanner";
    banner.setAttribute("aria-label", "Update verfügbar");

    var icon = document.createElement("div");
    icon.className = "updateIcon";
    icon.appendChild(createIcon("arrowUp"));

    var content = document.createElement("div");
    content.className = "updateContent";
    var heading = document.createElement("h2");
    heading.textContent = "STEFFLIX " + formatVersion(newestVersion) + " ist verfügbar";
    var meta = document.createElement("p");
    meta.className = "updateMeta";
    meta.textContent = "Installiert ist Version " + formatVersion(version) + ".";
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
            dismissUpdateBanner(banner, newestVersion, option[1]);
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
        news.versions.forEach(function (entry) {
            var entryVersion = entry != null ? parseFloat(entry.version) : NaN;
            if (entryVersion > version && entryVersion <= newestVersion && Array.isArray(entry.items)) {
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

function checkForUpdate(container) {
    fetchText(versionUrl).then(function (text) {
        var newestVersion = parseVersion(text);
        if (newestVersion == null || newestVersion <= version || isUpdateBannerDismissed(newestVersion)) {
            return;
        }
        var banner = createUpdateBanner(newestVersion);
        container.insertBefore(banner, container.firstChild);
        fillUpdateNews(banner, newestVersion);
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

// 2.00 wird als Zahl 2 gespeichert, daher mit mindestens zwei Nachkommastellen anzeigen (1.323 bleibt 1.323)
function formatVersion(value) {
    var text = String(value);
    var decimals = text.includes(".") ? text.split(".")[1].length : 0;
    return Number(value).toFixed(Math.max(2, decimals));
}

function parseVersion(text) {
    if (text == null || !text.includes("var version = ")) {
        return null;
    }
    var parsed = parseFloat(text.split("var version = ")[1]);
    return isNaN(parsed) ? null : parsed;
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

function saveRowOrder(dashboard) {
    setData(dashboardOrderKey, JSON.stringify(getDashboardRows(dashboard).map(function (row) {
        return row.dataset.rowKey;
    })));
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

    document.getElementById("versionNow").textContent = formatVersion(version);
    var versionNewNode = document.getElementById("versionNew");
    versionNewNode.textContent = "wird geprüft...";

    var changelogNode = document.createElement("div");
    changelogNode.id = "changelog";
    changelogNode.textContent = "Changelog wird geladen...";
    document.getElementById("help").appendChild(changelogNode);

    fetchText(versionUrl).then(function (text) {
        var newestVersion = parseVersion(text);
        versionNewNode.textContent = newestVersion != null ? formatVersion(newestVersion) : "unbekannt (keine Internetverbindung?)";
    });

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

    buildHelpSections();
    initBackup();
}

// Gliedert die Hilfe in Abschnitte (eine Karte pro Überschrift) und erstellt
// rechts eine Übersicht, die den aktuell sichtbaren Abschnitt markiert.
function buildHelpSections() {
    var help = document.getElementById("help");
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
            section.id = "help-" + tocLinks.length;
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
    current.textContent = "Hilfe";
    breadcrumb.appendChild(startLink);
    breadcrumb.appendChild(separator);
    breadcrumb.appendChild(current);
    var title = document.createElement("h1");
    title.className = "folderTitle";
    title.textContent = "Hilfe";
    var info = document.createElement("p");
    info.className = "folderInfo";
    info.textContent = "Medien hinzufügen, Autoplay, Tastenkürzel, Updates, Datensicherung und Fehlerbehebung";
    header.appendChild(breadcrumb);
    header.appendChild(title);
    header.appendChild(info);
    document.title = "STEFFLIX - Hilfe";

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
        heading.textContent = "Version " + String(versionEntry.version);
        container.appendChild(heading);
        container.appendChild(document.createTextNode(":"));
        container.appendChild(document.createElement("br"));
        container.appendChild(createNewsList(versionEntry.items));
    });
}
