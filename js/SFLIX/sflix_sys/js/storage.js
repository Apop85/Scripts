// STEFFLIX - storage.js
// Speicher (IndexedDB), Fehlerbehebung, Datenmigration und Datensicherung
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

// ===================================================================
// Fehlerbehebung und Datenmigration
// ===================================================================

// Bezeichnungen für die Statusmeldung der Fehlerbehebung
var repairLabels = {
    favorites: "Favoriten",
    playlist: "Playlisten",
    timestamp: "Zeitstempel und Gesehen-Markierungen",
    playlast: "\"Weiterschauen\"",
    all: "Alle Daten"
};

// Funktion, um lokale Daten zu löschen
// favorites, playlist, timestamp, playlast, all
function modifyLocalData(dataType) {
    var counter = 0;
    if (dataType == "playlist" && getData("currentPrefix") != null) {
        removeData("currentPrefix");
        counter += 1;
    }

    getAllDataKeys().forEach(function (key) {
        // Bleibt immer erhalten, sonst würden beim nächsten Start die alten gemeinsamen Daten
        // wieder übernommen
        if (key === sharedDataImportedKey) {
            return;
        }
        // "Zeitstempel löschen" entfernt auch die Gesehen-Markierungen
        var isSeenMarker = dataType == "timestamp" && key.startsWith("seen-");
        if (dataType == "all" || key.includes(dataType) || isSeenMarker) {
            removeData(key);
            counter += 1;
        }
    });

    var label = repairLabels[dataType] || dataType;
    var status = document.getElementById("repairStatus");
    status.className = "backupInfo";
    status.textContent = counter == 0
        ? label + ": Es waren keine Einträge vorhanden."
        : label + " gelöscht (" + counter + (counter == 1 ? " Eintrag)." : " Einträge).");
    status.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

// "Alle Daten löschen": vorher eine Sicherung anbieten
function confirmDeleteAllData() {
    var status = document.getElementById("repairStatus");
    status.className = "backupInfo";
    status.replaceChildren(document.createTextNode("Alle gespeicherten Daten löschen? Vorher am besten eine Sicherung erstellen, damit sich Favoriten, Fortschritt und Einstellungen wiederherstellen lassen."));
    var actions = document.createElement("div");
    actions.className = "backupConfirm";
    var exportButton = document.createElement("button");
    exportButton.type = "button";
    exportButton.className = "sflixButton primary small";
    exportButton.textContent = "Sicherung exportieren";
    var deleteButton = document.createElement("button");
    deleteButton.type = "button";
    deleteButton.className = "sflixButton danger small";
    deleteButton.textContent = "Alles löschen";
    var cancelButton = document.createElement("button");
    cancelButton.type = "button";
    cancelButton.className = "sflixButton ghost small";
    cancelButton.textContent = "Abbrechen";
    actions.append(exportButton, deleteButton, cancelButton);
    status.appendChild(actions);
    exportButton.addEventListener("click", function () {
        exportUserData();
        exportButton.textContent = "Sicherung erstellt";
        exportButton.disabled = true;
    });
    deleteButton.addEventListener("click", function () {
        modifyLocalData("all");
    });
    cancelButton.addEventListener("click", function () {
        status.replaceChildren();
        status.className = "";
    });
    status.scrollIntoView({ behavior: "smooth", block: "nearest" });
}

// ===================================================================
// Fehlerbehebung: gespeicherte Daten prüfen und verwaiste Einträge entfernen
// ===================================================================

// Pfad eines "Weiterschauen"-Eintrags (Titel|URL)
function getLastPlayedPath(entry) {
    if (!entry.includes("|")) {
        return null;
    }
    return normalizeMediaLocation(getMediaLocationFromDecodedData(decodeUrlData(entry.split("|")[1])));
}

// Zählt je Art die Einträge und die verwaisten (Medium bzw. Ordner gibt es nicht mehr)
function analyzeStoredData() {
    var result = [];
    var add = function (id, label, total, orphans) {
        result.push({ id: id, label: label, total: total, orphans: orphans });
    };

    var favorites = getFavorites();
    add("favorites", "Favoriten", favorites.length, favorites.filter(function (path) {
        return !mediaExists(path);
    }).length);

    var lastPlayed = (getData("playlast") || "").split(listSeperator).filter(function (entry) {
        return entry !== "";
    });
    add("playlast", "\"Weiterschauen\"", lastPlayed.length, lastPlayed.filter(function (entry) {
        var path = getLastPlayedPath(entry);
        return path == null || !mediaExists(path);
    }).length);

    [["timestamp-./", "timestamp", "Abspielpositionen"], ["seen-./", "seen", "Gesehen-Markierungen"], ["timestampDuration-./", "duration", "Gespeicherte Spieldauer"]].forEach(function (kind) {
        var keys = getAllDataKeys().filter(function (key) {
            return key.startsWith(kind[0]);
        });
        add(kind[1], kind[2], keys.length, keys.filter(function (key) {
            return !mediaExists(key.slice(kind[0].length - 2));
        }).length);
    });

    var transitions = Object.keys(getTransitionMap());
    add("transitions", "Dauer bis Übergang je Ordner", transitions.length, transitions.filter(function (folder) {
        return !folderExists(folder);
    }).length);

    var playlists = getAllDataKeys().filter(function (key) {
        return key.startsWith("playlist-");
    });
    add("playlists", "Playlisten", playlists.length, playlists.filter(function (key) {
        return (getData(key) || "").split(listSeperator).some(function (entry) {
            return entry !== "" && !mediaExists(normalizeMediaLocation(entry.split("|")[0]));
        });
    }).length);
    return result;
}

// Entfernt nur verwaiste Einträge, alles andere bleibt unverändert
function removeOrphanedData() {
    var removed = 0;
    var favorites = getFavorites();
    var keptFavorites = favorites.filter(function (path) {
        return mediaExists(path);
    });
    if (keptFavorites.length !== favorites.length) {
        removed += favorites.length - keptFavorites.length;
        setData("favorites", keptFavorites.join(listSeperator));
    }

    var lastPlayed = (getData("playlast") || "").split(listSeperator).filter(function (entry) {
        return entry !== "";
    });
    var keptLastPlayed = lastPlayed.filter(function (entry) {
        var path = getLastPlayedPath(entry);
        return path != null && mediaExists(path);
    });
    if (keptLastPlayed.length !== lastPlayed.length) {
        removed += lastPlayed.length - keptLastPlayed.length;
        setData("playlast", keptLastPlayed.join(listSeperator));
    }

    getAllDataKeys().forEach(function (key) {
        ["timestamp-./", "seen-./", "timestampDuration-./"].forEach(function (prefix) {
            if (key.startsWith(prefix) && !mediaExists(key.slice(prefix.length - 2))) {
                removeData(key);
                removed += 1;
            }
        });
        // Veraltete Playlisten werden beim nächsten Abspielen neu erstellt
        if (key.startsWith("playlist-") && (getData(key) || "").split(listSeperator).some(function (entry) {
            return entry !== "" && !mediaExists(normalizeMediaLocation(entry.split("|")[0]));
        })) {
            removeData(key);
            removed += 1;
        }
    });

    var transitions = getTransitionMap();
    Object.keys(transitions).forEach(function (folder) {
        if (!folderExists(folder)) {
            delete transitions[folder];
            removed += 1;
        }
    });
    saveTransitionMap(transitions);
    return removed;
}

function showDataCheck() {
    var container = document.getElementById("checkDataResult");
    container.replaceChildren();
    container.className = "backupInfo";
    var analysis = analyzeStoredData();
    var table = document.createElement("table");
    table.className = "dataCheckTable";
    var head = document.createElement("tr");
    ["Art", "Einträge", "Verwaist"].forEach(function (text) {
        var cell = document.createElement("th");
        cell.textContent = text;
        head.appendChild(cell);
    });
    table.appendChild(head);
    var totalOrphans = 0;
    var totalEntries = 0;
    analysis.forEach(function (entry) {
        var row = document.createElement("tr");
        [entry.label, entry.total, entry.orphans].forEach(function (value, index) {
            var cell = document.createElement("td");
            cell.textContent = value;
            if (index == 2 && entry.orphans > 0) {
                cell.className = "dataCheckOrphans";
            }
            row.appendChild(cell);
        });
        table.appendChild(row);
        totalOrphans += entry.orphans;
        totalEntries += entry.total;
    });
    container.appendChild(table);

    var note = document.createElement("p");
    if (totalOrphans == 0) {
        note.textContent = "Alles in Ordnung: Alle Einträge zeigen auf vorhandene Medien und Ordner.";
        container.appendChild(note);
        return;
    }
    note.textContent = totalOrphans + " verwaiste Einträge gefunden. Sie zeigen auf Medien oder Ordner, die in den eingelesenen Daten fehlen. Vor dem Entfernen prüfen, ob alle Laufwerke verbunden sind und die Daten aktuell eingelesen wurden, sonst gehen Einträge verloren, die nur vorübergehend fehlen.";
    if (totalOrphans == totalEntries) {
        note.textContent += " Achtung: Alle Einträge wären betroffen. Das deutet eher auf fehlende Medien hin als auf alte Einträge.";
    }
    container.appendChild(note);
    var actions = document.createElement("div");
    actions.className = "backupConfirm";
    var removeButton = document.createElement("button");
    removeButton.type = "button";
    removeButton.className = "sflixButton danger small";
    removeButton.textContent = "Verwaiste Einträge entfernen (" + totalOrphans + ")";
    removeButton.addEventListener("click", function () {
        var removed = removeOrphanedData();
        showDataCheck();
        var done = document.createElement("p");
        done.className = "dataCheckDone";
        done.textContent = removed + " Einträge entfernt.";
        container.prepend(done);
    });
    actions.appendChild(removeButton);
    container.appendChild(actions);
}

function initDataCheck() {
    document.getElementById("checkDataButton").addEventListener("click", showDataCheck);
}

// Funktion, um gespeicherte Daten mit der aktuellen Version kompatibel zu machen
function updateData() {
    // Update 1.28 - Anpassen Listenseperator
    if (getData("v1.28") == null) {
        var ignoreList = ["mediaVolume", "timestamp", "favorites", "currentPrefix", "playlast"];
        getAllDataKeys().forEach(function (key) {
            if (getData(key) == null) {
                return;
            }
            // Ersetze Listenseperator
            var korrektur = getData(key).split("Nav,").join("Nav" + listSeperator);

            // Prüfe, ob der Name des Items angepasst werden muss
            var doOverwrite = !ignoreList.some(function (ignored) {
                return key.includes(ignored);
            });

            var newKey = key;
            if (doOverwrite) {
                newKey = "playlist-" + key;
                korrektur = korrektur.split(",.").join(listSeperator + ".");
            }

            // Passe Favoritenliste an
            if (key == "favorites") {
                korrektur = korrektur.split(",.").join(listSeperator + ".");
            }

            // Passe URL-Aufbau der gespeicherten Daten an
            korrektur = korrektur.split(listSeperator).map(function (entry) {
                if (!entry.includes("start.html?=")) {
                    return entry;
                }
                var dataPrefix = entry.split("?=")[0] + "?=";
                var dataPostfix = entry.split("#").length > 1 ? "#" + entry.split("#")[1] : "";
                var decodedData = atob(entry.split("?=")[1].split("#")[0]);
                decodedData = decodedData.replaceAll(",LEVEL", listSeperator + "LEVEL");
                decodedData = decodedData.replaceAll(",MEDIA", listSeperator + "MEDIA");
                decodedData = decodedData.replaceAll(",PL", listSeperator + "PL");
                return dataPrefix + btoa(decodedData) + dataPostfix;
            }).join(listSeperator);

            removeData(key);
            setData(newKey, korrektur);
        });

        setData("v1.28", true);
    }

    // Update 1.33 - Anpassen Levelstruktur
    if (getData("v1.33") == null) {
        getAllDataKeys().forEach(function (key) {
            if (!key.includes("playlast")) {
                return;
            }
            var newArray = [];
            getData(key).split(listSeperator).forEach(function (currentData) {
                if (!currentData.includes("start.html?=")) {
                    return;
                }
                var filename = currentData.split("|")[0];
                var url = currentData.split("|")[1];
                var urlPrefix = url.split("html?=")[0] + "html?=";
                var urlPostfix = url.includes("#") ? "#" + url.split("#")[1] : "";
                var decodedData = atob(url.split("?=")[1].split("#")[0]);
                newArray.push(filename + "|" + urlPrefix + btoa(decodedData.replace("MAIN:", "LEVEL0:")) + urlPostfix);
            });
            setData(key, newArray.join(listSeperator));
        });
        // Der frühere Hinweis "bitte updateData.exe ausführen" entfällt: ab 2.00 startet
        // der Updater updateData.exe automatisch
        setData("v1.33", true);
    }

    // Update 1.38 - Autoupdater. Der damalige Hinweis ist nicht mehr nötig (updateData.exe
    // lädt updater.exe selbst nach), die Markierung bleibt für die Datensicherung bestehen.
    setData("v1.38", true);

    // Von updateData.exe umbenannte Dateien nachziehen (bei jedem Start prüfen)
    applyRenamedPaths();

    // Update 2.00 - Zeitstempel pro Pfad, Playlisten neu aufbauen, relative Links
    if (getData("v2.00") == null) {
        migrateLegacyTimestamps();
        removePlaylists();
        normalizeLastPlayedUrls();
        setData("v2.00", true);
    }

    // Ab 2.04: Playlisten neu erstellen, sobald sich die eingelesenen Medien geändert haben
    refreshPlaylistsOnDataChange();
}

// "Weiterschauen" enthielt bis 1.42 teils absolute Adressen (file:///D:/sflix/start.html?=...).
// Diese werden in relative Links umgewandelt, damit sie auch nach dem Verschieben des
// STEFFLIX-Ordners funktionieren. Ungültige Einträge werden entfernt.
function normalizeLastPlayedUrls() {
    var stored = getData("playlast");
    if (stored == null || stored == "") {
        return;
    }
    var entries = [];
    stored.split(listSeperator).forEach(function (entry) {
        if (!entry.includes("|")) {
            return;
        }
        var title = entry.split("|")[0];
        var decodedData = decodeUrlData(entry.split("|")[1]);
        if (decodedData == null || !decodedData.includes("MEDIA:")) {
            return;
        }
        var url = buildStartUrl(decodedData);
        if (url != null) {
            entries.push(title + "|" + url);
        }
    });
    setData("playlast", entries.join(listSeperator));
}

// ===================================================================
// Migration auf Version 2.00
// ===================================================================

function getRenamedPairs() {
    if (typeof renamedPaths === "undefined" || renamedPaths == null || !Array.isArray(renamedPaths.pairs)) {
        return [];
    }
    return renamedPaths.pairs.filter(function (pair) {
        return Array.isArray(pair) && pair.length == 2;
    });
}

// Wendet alle Umbenennungen (Ordner und Dateien, in der Reihenfolge von updateData) auf einen Pfad an
function mapRenamedPath(path, pairs) {
    pairs.forEach(function (pair) {
        if (path === pair[0]) {
            path = pair[1];
        } else if (path.startsWith(pair[0] + "/")) {
            path = pair[1] + path.slice(pair[0].length);
        }
    });
    return path;
}

// Löscht die gespeicherten Playlisten, sie werden beim Öffnen der Ordner neu erstellt
function removePlaylists() {
    getAllDataKeys().forEach(function (key) {
        if (key.startsWith("playlist-")) {
            removeData(key);
        }
    });
}

// Passt Favoriten, Zeitstempel und "Weiterschauen" an umbenannte Dateien an
function applyRenamedPaths() {
    var pairs = getRenamedPairs();
    if (pairs.length == 0 || getData("renamedPathsApplied") === String(renamedPaths.id)) {
        return;
    }

    // Favoriten
    if (getData("favorites") != null) {
        setData("favorites", getFavorites().map(function (path) {
            return mapRenamedPath(path, pairs);
        }).join(listSeperator));
    }

    // Zeitstempel und Dauer im neuen Format (timestamp-./Pfad, timestampDuration-./Pfad)
    getAllDataKeys().forEach(function (key) {
        ["timestamp-", "timestampDuration-", "seen-"].forEach(function (prefix) {
            if (!key.startsWith(prefix + "./")) {
                return;
            }
            var newKey = prefix + mapRenamedPath(key.slice(prefix.length), pairs);
            if (newKey !== key) {
                setData(newKey, getData(key));
                removeData(key);
            }
        });
    });

    // Weiterschauen
    var lastPlayed = (getData("playlast") || "").split(listSeperator).map(function (entry) {
        if (!entry.includes("|")) {
            return entry;
        }
        var decodedData = decodeUrlData(entry.split("|")[1]);
        if (decodedData == null || !decodedData.includes("MEDIA:")) {
            return entry;
        }
        // Ungekürzt lesen: alte Dateinamen können "#" enthalten
        var mediaPath = normalizeMediaLocation(decodedData.split("MEDIA:")[1].split(listSeperator)[0]);
        var newPath = mapRenamedPath(mediaPath, pairs);
        if (newPath === mediaPath) {
            return entry;
        }
        var playlistName = getPlaylistNameFromDecodedData(decodedData);
        var newUrl = buildStartUrl(buildMediaData(newPath, isListPlaylist(playlistName) ? playlistName : null));
        return newUrl == null ? entry : getBaseName(newPath) + "|" + newUrl;
    });
    if (getData("playlast") != null) {
        setData("playlast", lastPlayed.join(listSeperator));
    }

    // Dauer bis Übergang je Ordner (Schlüssel ohne "./", z.B. "Serien/Show/Staffel 01")
    var transitions = getTransitionMap();
    var mappedTransitions = {};
    var transitionsChanged = false;
    Object.keys(transitions).forEach(function (folder) {
        var newFolder = folder === "" ? "" : mapRenamedPath("./" + folder, pairs).replace(/^\.\//, "");
        if (newFolder !== folder) {
            transitionsChanged = true;
        }
        // Zwei alte Ordner im selben neuen (Staffeln zusammengeführt): der bestehende Wert bleibt
        if (!mappedTransitions.hasOwnProperty(newFolder) || newFolder === folder) {
            mappedTransitions[newFolder] = transitions[folder];
        }
    });
    if (transitionsChanged) {
        saveTransitionMap(mappedTransitions);
    }

    removePlaylists();
    setData("renamedPathsApplied", String(renamedPaths.id));
}

// Kurzer Fingerabdruck der eingelesenen Medien (Anzahl und Pfade). Ändert er sich, werden die
// Playlisten neu erstellt, damit Vor/Zurück neue oder entfernte Medien richtig berücksichtigen.
function getDataSignature() {
    var paths = collectMediaPaths(typeof data === "object" && data != null ? data : {}, []);
    var hash = 2166136261;
    paths.forEach(function (path) {
        for (var index = 0; index < path.length; index++) {
            hash ^= path.charCodeAt(index);
            hash = Math.imul(hash, 16777619);
        }
        hash ^= 47;
    });
    return paths.length + "-" + (hash >>> 0).toString(16);
}

function refreshPlaylistsOnDataChange() {
    var signature = getDataSignature();
    var stored = getData("dataSignature");
    if (stored !== signature) {
        // Beim ersten Start mit 2.04 ist noch nichts gespeichert: dann nur merken
        if (stored != null) {
            removePlaylists();
        }
        setData("dataSignature", signature);
    }
}

// Bildet den Dateinamen so nach, wie Version 1.42 ihn für Zeitstempel verwendet hat
// (aus der vom Browser kodierten Video-URL, nur Leerzeichen und ä/ö/ü zurückgewandelt)
function getLegacyMediaName(path) {
    var encoded = Array.from(path).map(function (char) {
        var code = char.charCodeAt(0);
        return (code <= 32 || code > 126 || ' "#<>?`{}'.includes(char)) ? encodeURIComponent(char) : char;
    }).join("");
    return stripExtension(getBaseName(replaceSpecialChars(encoded)));
}

// Ordnet alte Zeitstempel (nur Dateiname) dem Pfad zu, sofern der Name eindeutig ist.
// Mehrdeutige Namen werden beim ersten Abspielen übernommen (readTimestamp).
function migrateLegacyTimestamps() {
    var pairs = getRenamedPairs();
    var candidates = {};
    var addCandidate = function (legacyName, path) {
        candidates[legacyName] = candidates[legacyName] || [];
        if (!candidates[legacyName].includes(path)) {
            candidates[legacyName].push(path);
        }
    };

    collectMediaPaths(data, []).forEach(function (path) {
        addCandidate(getLegacyMediaName(path), path);
    });
    // Umbenannte Dateien: alter Name zeigt auf den neuen Pfad
    pairs.forEach(function (pair) {
        if (isMediaFile(allowedMediaExtensions, pair[0])) {
            addCandidate(getLegacyMediaName(pair[0]), mapRenamedPath(pair[0], pairs));
        }
    });

    getAllDataKeys().forEach(function (key) {
        if (!key.startsWith("timestamp-") || key.startsWith("timestamp-./")) {
            return;
        }
        var paths = candidates[key.slice("timestamp-".length)] || [];
        if (paths.length != 1) {
            return;
        }
        var newKey = "timestamp-" + paths[0];
        if (getData(newKey) == null) {
            setData(newKey, getData(key));
        }
        removeData(key);
    });
}

// ===================================================================
// Speicher (IndexedDB mit Cache)
// ===================================================================

function getData(key) {
    if (storageCache.hasOwnProperty(key)) {
        return storageCache[key];
    }
    return null;
}

function setData(key, value) {
    value = String(value);
    storageCache[key] = value;

    if (!db) {
        return;
    }

    var transaction = db.transaction(["storage"], "readwrite");
    transaction.objectStore("storage").put(value, key);
}

function removeData(key) {
    delete storageCache[key];

    if (!db) {
        return;
    }

    var transaction = db.transaction(["storage"], "readwrite");
    transaction.objectStore("storage").delete(key);
}

function getAllDataKeys() {
    return Object.keys(storageCache);
}

// Der Browser speichert die Daten aller file://-Seiten gemeinsam. Damit sich mehrere
// STEFFLIX-Installationen auf einem PC nicht beeinflussen, nutzt jede ihre eigene Datenbank.
// Die Kennung schreibt updateStefflix.exe in sflix_sys/install.js. Ohne Kennung (älterer
// Updater) gilt wie bisher die gemeinsame Datenbank "STEFFLIX".
var sharedDatabaseName = "STEFFLIX";
var sharedDataImportedKey = "sharedDataImported";

function getDatabaseName() {
    if (typeof installId === "string" && /^[0-9a-f]{32}$/.test(installId)) {
        return sharedDatabaseName + "-" + installId;
    }
    return sharedDatabaseName;
}

function initDatabase(callback) {
    var databaseName = getDatabaseName();
    var request = indexedDB.open(databaseName, 1);

    request.onupgradeneeded = function (event) {
        var database = event.target.result;
        if (!database.objectStoreNames.contains("storage")) {
            database.createObjectStore("storage");
        }
    };

    request.onsuccess = function (event) {
        db = event.target.result;
        requestPersistentStorage();
        loadDatabaseCache(function () {
            var next = function () {
                migrateLocalStorageToIndexedDB(callback);
            };
            if (databaseName !== sharedDatabaseName && getData(sharedDataImportedKey) == null) {
                importSharedDatabase(next);
            } else {
                next();
            }
        });
    };

    request.onerror = function (event) {
        console.error("IndexedDB Fehler", event);
        // Ohne Datenbank trotzdem starten (Daten bleiben nur bis zum Neuladen erhalten)
        if (callback) {
            callback();
        }
    };
}

// Bittet den Browser, die gespeicherten Daten (Sortierung, Weiterschauen, Favoriten ...)
// nicht automatisch zu löschen, wenn der Speicherplatz knapp wird. Nicht jeder Browser
// unterstützt das unter file://, dann bleibt alles wie bisher.
function requestPersistentStorage() {
    try {
        if (navigator.storage && navigator.storage.persist) {
            navigator.storage.persist().catch(function () { });
        }
    } catch (error) {
        // ignorieren
    }
}

function loadDatabaseCache(callback) {
    storageCache = {};

    var transaction = db.transaction(["storage"], "readonly");
    var request = transaction.objectStore("storage").openCursor();

    request.onsuccess = function (event) {
        var cursor = event.target.result;
        if (cursor) {
            storageCache[cursor.key] = cursor.value;
            cursor.continue();
        }
    };

    transaction.oncomplete = function () {
        if (callback) {
            callback();
        }
    };

    transaction.onerror = function (event) {
        console.error("IndexedDB Cache konnte nicht geladen werden", event);
        if (callback) {
            callback();
        }
    };
}

// Einmalig beim ersten Start mit eigener Kennung: die bisher gemeinsamen Daten übernehmen,
// damit nichts verloren geht. Vorhandene Einträge der eigenen Datenbank bleiben unverändert.
// Die gemeinsame Datenbank selbst wird nicht verändert.
function importSharedDatabase(callback) {
    var finished = false;
    var finish = function () {
        if (finished) {
            return;
        }
        finished = true;
        setData(sharedDataImportedKey, new Date().toISOString());
        if (callback) {
            callback();
        }
    };

    var request;
    try {
        request = indexedDB.open(sharedDatabaseName);
    } catch (error) {
        finish();
        return;
    }
    // Gibt es keine gemeinsame Datenbank, wird sie nicht angelegt
    request.onupgradeneeded = function (event) {
        event.target.transaction.abort();
    };
    request.onerror = function (event) {
        event.preventDefault();
        finish();
    };
    request.onsuccess = function (event) {
        var shared = event.target.result;
        if (!shared.objectStoreNames.contains("storage")) {
            shared.close();
            finish();
            return;
        }
        var entries = {};
        var readTransaction = shared.transaction(["storage"], "readonly");
        readTransaction.objectStore("storage").openCursor().onsuccess = function (cursorEvent) {
            var cursor = cursorEvent.target.result;
            if (cursor) {
                entries[cursor.key] = cursor.value;
                cursor.continue();
            }
        };
        readTransaction.oncomplete = function () {
            shared.close();
            var keys = Object.keys(entries).filter(function (key) {
                return !storageCache.hasOwnProperty(key);
            });
            if (keys.length == 0 || !db) {
                finish();
                return;
            }
            var writeTransaction = db.transaction(["storage"], "readwrite");
            var store = writeTransaction.objectStore("storage");
            keys.forEach(function (key) {
                storageCache[key] = entries[key];
                store.put(entries[key], key);
            });
            writeTransaction.oncomplete = finish;
            writeTransaction.onerror = function (writeEvent) {
                console.error("Übernahme der gemeinsamen Daten fehlgeschlagen", writeEvent);
                finish();
            };
        };
        readTransaction.onerror = function () {
            shared.close();
            finish();
        };
    };
}

function migrateLocalStorageToIndexedDB(callback) {
    if (localStorage.length == 0) {
        if (callback) {
            callback();
        }
        return;
    }

    var keys = [];
    for (var index = 0; index < localStorage.length; index++) {
        keys.push(localStorage.key(index));
    }

    var transaction = db.transaction(["storage"], "readwrite");
    var store = transaction.objectStore("storage");

    keys.forEach(function (key) {
        if (!storageCache.hasOwnProperty(key)) {
            var value = localStorage.getItem(key);
            storageCache[key] = value;
            store.put(value, key);
        }
    });

    transaction.oncomplete = function () {
        keys.forEach(function (key) {
            localStorage.removeItem(key);
        });
        if (callback) {
            callback();
        }
    };

    transaction.onerror = function (event) {
        console.error("Migration von localStorage nach IndexedDB fehlgeschlagen", event);
        if (callback) {
            callback();
        }
    };
}

// ===================================================================
// Datensicherung (Hilfeseite): Export und Import der gespeicherten Daten
// ===================================================================
var backupFormat = 1;

// Anpassungen für Sicherungen aus älteren Versionen. Jede Funktion bekommt die Daten
// (Schlüssel -> Text) und gibt sie angepasst zurück. Sie läuft nur für Sicherungen,
// deren Version kleiner ist als "version". Neue Einträge in aufsteigender Reihenfolge
// hinten anfügen, z.B. { version: 2.01, migrate: function (data) { ...; return data; } }
// Sicherungen gibt es erst ab 2.00, darum ist die Liste noch leer.
var backupMigrations = [];

function migrateBackupData(data, backupVersion) {
    var result = {};
    Object.keys(data).forEach(function (key) {
        result[key] = data[key];
    });
    backupMigrations.forEach(function (step) {
        if (backupVersion < step.version) {
            result = step.migrate(result) || result;
        }
    });
    return result;
}

// Markierungen erledigter Migrationen ("v2.00") und umbenannter Pfade
function isMigrationFlag(key) {
    return /^v\d+(\.\d+)*$/.test(key) || key === "renamedPathsApplied" || key === sharedDataImportedKey || key === "dataSignature";
}

function showBackupStatus(text, isError) {
    var status = document.getElementById("backupStatus");
    status.replaceChildren(document.createTextNode(text));
    status.className = isError ? "backupError" : "backupInfo";
    return status;
}

function exportUserData() {
    var entries = {};
    getAllDataKeys().forEach(function (key) {
        entries[key] = getData(key);
    });
    var backup = {
        app: "STEFFLIX",
        format: backupFormat,
        version: version,
        exportedAt: new Date().toISOString(),
        data: entries
    };
    var blob = new Blob([JSON.stringify(backup, null, 2)], { type: "application/json" });
    var url = URL.createObjectURL(blob);
    var link = document.createElement("a");
    link.href = url;
    link.download = "stefflix-sicherung-" + new Date().toISOString().split("T")[0] + ".json";
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(function () {
        URL.revokeObjectURL(url);
    }, 1000);
    showBackupStatus("Sicherung mit " + Object.keys(entries).length + " Einträgen erstellt. Die Datei liegt im Download-Ordner des Browsers.", false);
}

// Prüft die Datei und fragt vor dem Übernehmen nach
function readBackupFile(file) {
    var reader = new FileReader();
    reader.onload = function () {
        var backup = null;
        try {
            backup = JSON.parse(reader.result);
        } catch (error) {
            backup = null;
        }
        if (backup == null || backup.app !== "STEFFLIX" || backup.data == null || typeof backup.data !== "object") {
            showBackupStatus("Diese Datei ist keine gültige STEFFLIX-Sicherung.", true);
            return;
        }
        // Sicherungen aus einer neueren STEFFLIX-Version kennt diese Version nicht
        var backupVersion = parseFloat(backup.version);
        if (!isNaN(backupVersion) && backupVersion > version) {
            showBackupStatus("Diese Sicherung stammt aus STEFFLIX " + formatVersion(backupVersion) + ". Installiert ist " + formatVersion(version) + ". Bitte zuerst STEFFLIX aktualisieren.", true);
            return;
        }
        var data = migrateBackupData(backup.data, isNaN(backupVersion) ? 0 : backupVersion);
        // Migrationsmarkierungen gehören zur installierten Version und werden nicht übernommen
        var keys = Object.keys(data).filter(function (key) {
            return typeof data[key] === "string" && !isMigrationFlag(key);
        });
        var date = backup.exportedAt ? new Date(backup.exportedAt) : null;
        var dateText = date && !isNaN(date) ? date.toLocaleString("de-CH") : "unbekannt";
        var versionText = isNaN(backupVersion) ? "unbekannt" : formatVersion(backupVersion);

        var status = showBackupStatus("Sicherung vom " + dateText + " (STEFFLIX " + versionText + ") mit " + keys.length + " Einträgen. Vorhandene Einträge mit gleichem Namen werden überschrieben, alle anderen bleiben erhalten.", false);
        var actions = document.createElement("div");
        actions.className = "backupConfirm";
        var restoreButton = document.createElement("button");
        restoreButton.type = "button";
        restoreButton.className = "sflixButton primary small";
        restoreButton.textContent = "Wiederherstellen";
        var cancelButton = document.createElement("button");
        cancelButton.type = "button";
        cancelButton.className = "sflixButton ghost small";
        cancelButton.textContent = "Abbrechen";
        actions.appendChild(restoreButton);
        actions.appendChild(cancelButton);
        status.appendChild(actions);

        cancelButton.addEventListener("click", function () {
            showBackupStatus("Import abgebrochen.", false);
        });
        restoreButton.addEventListener("click", function () {
            restoreUserData(data, keys);
        });
    };
    reader.onerror = function () {
        showBackupStatus("Die Datei konnte nicht gelesen werden.", true);
    };
    reader.readAsText(file);
}

// Schreibt alle Einträge in einer Transaktion und lädt danach neu
function restoreUserData(data, keys) {
    keys.forEach(function (key) {
        storageCache[key] = data[key];
    });
    // Umbenennungen aus updateData.exe beim nächsten Start auch auf die
    // wiederhergestellten Pfade anwenden
    delete storageCache.renamedPathsApplied;
    var finish = function () {
        showBackupStatus(keys.length + " Einträge wiederhergestellt. STEFFLIX wird neu geladen...", false);
        setTimeout(function () {
            location.reload();
        }, 1200);
    };
    if (!db) {
        finish();
        return;
    }
    var transaction = db.transaction(["storage"], "readwrite");
    var store = transaction.objectStore("storage");
    keys.forEach(function (key) {
        store.put(data[key], key);
    });
    store.delete("renamedPathsApplied");
    transaction.oncomplete = finish;
    transaction.onerror = function () {
        showBackupStatus("Beim Wiederherstellen ist ein Fehler aufgetreten.", true);
    };
}

function initBackup() {
    var exportButton = document.getElementById("exportDataButton");
    var importButton = document.getElementById("importDataButton");
    var fileInput = document.getElementById("importDataFile");
    exportButton.prepend(createIcon("download"));
    importButton.prepend(createIcon("upload"));

    exportButton.addEventListener("click", exportUserData);
    importButton.addEventListener("click", function () {
        fileInput.value = "";
        fileInput.click();
    });
    fileInput.addEventListener("change", function () {
        if (fileInput.files.length > 0) {
            readBackupFile(fileInput.files[0]);
        }
    });
}
