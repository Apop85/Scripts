// STEFFLIX - core.js
// Grundlagen: Konfiguration, Einstellungen der Oberfläche, Zustand, Dateitypen, Sortierung, URL-Aufbau,
// Datenbaum, Vorschaubilder, Symbole
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

//  _______  _______  _______  _______  _______  ___      ___   __   __
// |       ||       ||       ||       ||       ||   |    |   | |  |_|  |
// |  _____||_     _||    ___||    ___||    ___||   |    |   | |       |
// | |_____   |   |  |   |___ |   |___ |   |___ |   |    |   | |       |
// |_____  |  |   |  |    ___||    ___||    ___||   |___ |   |  |     |
//  _____| |  |   |  |   |___ |   |    |   |    |       ||   | |   _   |
// |_______|  |___|  |_______||___|    |___|    |_______||___| |__| |__|

// ===================================================================
// Konfiguration
// ===================================================================
var listSeperator = "$=$";

// Dateiformate, die der Browser direkt abspielen bzw. anzeigen kann.
// Gross-/Kleinschreibung spielt keine Rolle (JPG = jpg). Muss mit allowedFileTypes in getfilepath.py übereinstimmen.
var mediaTypes = [".mp4", ".m4v", ".webm", ".ogv"];

var musicTypes = [".mp3", ".m4a", ".ogg", ".oga", ".opus", ".aac", ".flac", ".wav"];

var imageTypes = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp"];

var allowedMediaExtensions = mediaTypes.concat(musicTypes, imageTypes);

var minTime = 11;

var licenseFields = ["lic1", "lic2", "lic3", "lic4", "lic5", "lic6", "lic7", "lic8"];

// Playlisten, deren Einträge aus "Pfad|URL" bestehen und aus verschiedenen Ordnern stammen können
var listPlaylists = ["searchResults", "favoritesList", "shuffleList"];

var modalIds = ["settingsBox", "feedbackWrapper", "serialKeyWrapper"];

var repositoryUrl = "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/";

var versionUrl = repositoryUrl + "sflix_sys/version.js";

var newsUrl = repositoryUrl + "sflix_sys/news.json";

// Darstellung der Changelog-Typen (wie bisher in news.txt)
var newsTypeStyles = {
    // Helle Varianten der bisherigen Farben, damit sie auf dunklem Hintergrund lesbar sind
    "BUG": { color: "#e879f9" },
    "BUGFIX": { color: "#4ade80" },
    "FEATURE": { color: "#f87171" },
    "INTERFACE": { color: "#60a5fa" },
    "HOTFIX": { color: "#f87171", bold: true },
    "OPTIMIERUNG": {}
};

var previewGifUrl = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/sflix_sys/sflix.gif";

// Natürliche Sortierung: "Folge 2" vor "Folge 10", S01E02 vor S01E10
var naturalCollator = new Intl.Collator("de", { numeric: true });

// ===================================================================
// Zustand
// ===================================================================
var db = null;

var storageCache = {};

var autoplay = false;

var autoplayAmount = 0;

var autoplayDuration = 0;

var timeoutArray = [];

var isPopStateNavigation = false;

var currentMediaLocation = null;

//  _______  __   __  __    _  ___   _  _______  ___   _______  __    _  _______  __    _
// |       ||  | |  ||  |  | ||   | | ||       ||   | |       ||  |  | ||       ||  |  | |
// |    ___||  | |  ||   |_| ||   |_| ||_     _||   | |   _   ||   |_| ||    ___||   |_| |
// |   |___ |  |_|  ||       ||      _|  |   |  |   | |  | |  ||       ||   |___ |       |
// |    ___||       ||  _    ||     |_   |   |  |   | |  |_|  ||  _    ||    ___||  _    |
// |   |    |       || | |   ||    _  |  |   |  |   | |       || | |   ||   |___ | | |   |
// |___|    |_______||_|  |__||___| |_|  |___|  |___| |_______||_|  |__||_______||_|  |__|

// ===================================================================
// Hilfsfunktionen: Sortierung und Dateinamen
// ===================================================================

function compareNatural(a, b) {
    return naturalCollator.compare(a, b);
}

// Artikel am Anfang, die beim Sortieren und Gruppieren übergangen werden ("Der Pate" unter P)
var leadingArticles = /^(der|die|das|ein|eine|the|a|an)\s+(?=[0-9A-Za-zÀ-ÿ])/i;

// Name ohne führenden Artikel (nur wenn in den Einstellungen eingeschaltet)
function getSortName(name) {
    name = String(name);
    if (!getSetting("ignoreArticles")) {
        return name;
    }
    var stripped = name.replace(leadingArticles, "");
    return stripped === "" ? name : stripped;
}

// Vergleicht Namen oder Pfade ("./Filme/Der Pate.mp4") Abschnitt für Abschnitt, jeweils ohne
// führenden Artikel. Bei Gleichstand entscheidet der vollständige Name.
function compareSortNames(a, b) {
    var partsA = String(a).split("/");
    var partsB = String(b).split("/");
    var length = Math.min(partsA.length, partsB.length);
    for (var index = 0; index < length; index++) {
        var result = compareNatural(getSortName(partsA[index]), getSortName(partsB[index]));
        if (result !== 0) {
            return result;
        }
    }
    return partsA.length - partsB.length || compareNatural(a, b);
}

// Sortiert eine Liste natürlich (in place) und gibt sie zurück. Gilt für Ordnerseiten
// und Playlisten gleichermassen, damit Vor/Zurück der angezeigten Reihenfolge folgt.
function sortNatural(list) {
    return list.sort(compareSortNames);
}

// ===================================================================
// Einstellungen der Oberfläche (Seite "Einstellungen")
// Gespeichert als JSON unter "displaySettings", gehört damit zur Datensicherung.
// ===================================================================
var displaySettingsKey = "displaySettings";

var displaySettingDefaults = {
    cardSize: "medium",       // small, medium, large
    rowSize: 12,              // Kacheln pro Zeile, danach beginnt eine neue Zeile
    showMarkers: true,        // "Gesehen", "Letzte Folge" ... auf Kacheln
    reduceMotion: false,
    skipSplash: false,
    alphaGroups: false,       // Ordnerseiten in Gruppen A–D, E–H ...
    groupExcludeImages: true,
    groupExcludeMusic: false,
    jumpBar: true,            // Sprungleiste zu den Gruppen
    ignoreArticles: true,
    separateFolders: false,   // Ordner in einem eigenen Band über den Medien
    continueMax: 20           // Einträge in "Weiterschauen"
};

var displaySettingLimits = {
    rowSize: [4, 30],
    continueMax: [5, 50]
};

var cardSizes = ["small", "medium", "large"];

var displaySettingsCache = null;

function clampSetting(name, value) {
    var limits = displaySettingLimits[name];
    value = parseInt(value, 10);
    if (isNaN(value)) {
        return displaySettingDefaults[name];
    }
    return Math.min(limits[1], Math.max(limits[0], value));
}

function getDisplaySettings() {
    if (displaySettingsCache != null) {
        return displaySettingsCache;
    }
    var stored = null;
    try {
        stored = JSON.parse(getData(displaySettingsKey) || "null");
    } catch (error) {
        stored = null;
    }
    var settings = {};
    Object.keys(displaySettingDefaults).forEach(function (name) {
        var fallback = displaySettingDefaults[name];
        var value = stored != null && typeof stored === "object" ? stored[name] : undefined;
        if (typeof fallback === "boolean") {
            settings[name] = typeof value === "boolean" ? value : fallback;
        } else if (displaySettingLimits[name]) {
            settings[name] = value == null ? fallback : clampSetting(name, value);
        } else {
            settings[name] = cardSizes.includes(value) ? value : fallback;
        }
    });
    // Vor dem Laden der Datenbank nicht zwischenspeichern (dann gälten nur die Standardwerte)
    if (db != null) {
        displaySettingsCache = settings;
    }
    return settings;
}

function getSetting(name) {
    return getDisplaySettings()[name];
}

function setSetting(name, value) {
    var settings = getDisplaySettings();
    settings[name] = displaySettingLimits[name] ? clampSetting(name, value) : value;
    displaySettingsCache = settings;
    setData(displaySettingsKey, JSON.stringify(settings));
    applyDisplaySettings();
    return settings[name];
}

function resetDisplaySettings() {
    removeData(displaySettingsKey);
    displaySettingsCache = null;
    applyDisplaySettings();
}

// Einstellungen, die per CSS wirken (Kachelgrösse, Animationen)
function applyDisplaySettings() {
    var root = document.documentElement;
    var size = getSetting("cardSize");
    root.classList.toggle("cardsSmall", size === "small");
    root.classList.toggle("cardsLarge", size === "large");
    root.classList.toggle("reduceMotion", getSetting("reduceMotion"));
}

function hasExtension(filename, extensions) {
    var lower = String(filename).toLowerCase();
    for (var index = 0; index < extensions.length; index++) {
        if (lower.endsWith(extensions[index])) {
            return true;
        }
    }
    return false;
}

// Funktion zum Prüfen, ob ein Dateiname einer Videodatei entspricht
function isVideo(filename) {
    return hasExtension(filename, mediaTypes);
}

// Funktion zum Prüfen, ob ein Dateiname einer Bilddatei entspricht
function isImage(filename) {
    return hasExtension(filename, imageTypes);
}

// Funktion zum Prüfen, ob ein Dateiname einer Musikdatei entspricht
function isMusic(filename) {
    return hasExtension(filename, musicTypes);
}

// Prüft, ob ein Dateiname auf eine der Endungen endet
function isMediaFile(whitelist, filename) {
    return hasExtension(filename, whitelist);
}

// Entfernt die Dateiendung am Ende des Namens
function stripExtension(filename) {
    var lower = filename.toLowerCase();
    for (var index = 0; index < allowedMediaExtensions.length; index++) {
        var extension = allowedMediaExtensions[index];
        if (lower.endsWith(extension)) {
            return filename.slice(0, filename.length - extension.length);
        }
    }
    return filename;
}

function getBaseName(path) {
    var parts = String(path).split("/");
    return parts[parts.length - 1];
}

// Anzeigename ohne Pfad und Dateiendung
function getDisplayName(path) {
    return stripExtension(getBaseName(path));
}

// ===================================================================
// Symbole (SVG im Strichstil, ersetzen die früheren Emojis)
// ===================================================================
var iconShapes = {
    video: '<rect x="2.5" y="5" width="19" height="14" rx="2.5"/><path d="M10 9.2v5.6l4.8-2.8z" fill="currentColor" stroke="none"/>',
    music: '<path d="M9 18V5.5l11-2v12.5"/><circle cx="6" cy="18" r="3"/><circle cx="17" cy="16" r="3"/>',
    image: '<rect x="3" y="3.5" width="18" height="17" rx="2.5"/><circle cx="8.5" cy="9" r="1.6"/><path d="M21 15.5l-5.2-5.2L5 20.5"/>',
    folder: '<path d="M3 7.2A2.2 2.2 0 0 1 5.2 5h4.1l2.2 2.2h7.3A2.2 2.2 0 0 1 21 9.4v8.4a2.2 2.2 0 0 1-2.2 2.2H5.2A2.2 2.2 0 0 1 3 17.8z"/>',
    star: '<path d="M12 3.2l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17.2l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z"/>',
    starFilled: '<path d="M12 3.2l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17.2l-5.4 2.9 1-6.1-4.4-4.3 6.1-.9z" fill="currentColor"/>',
    chevronLeft: '<path d="M15 18l-6-6 6-6"/>',
    chevronRight: '<path d="M9 18l6-6-6-6"/>',
    play: '<path d="M7 4.8v14.4l11.5-7.2z" fill="currentColor" stroke="none"/>',
    arrowUp: '<path d="M12 19V5M5.5 11.5L12 5l6.5 6.5"/>',
    close: '<path d="M6.5 6.5l11 11M17.5 6.5l-11 11"/>',
    search: '<circle cx="11" cy="11" r="7"/><path d="M16.5 16.5L21 21"/>',
    check: '<path d="M5 12.5l4.5 4.5L19 7.5"/>',
    shuffle: '<path d="M16 4h4v4M20 4l-6.5 6.5M4 20l6.5-6.5M16 20h4v-4M20 20L4 4"/>',
    download: '<path d="M12 4v11M7 10.5l5 5 5-5M5 20h14"/>',
    upload: '<path d="M12 16V5M7 9.5l5-5 5 5M5 20h14"/>',
    chevronUp: '<path d="M6 15l6-6 6 6"/>',
    chevronDown: '<path d="M6 9l6 6 6-6"/>',
    grip: '<circle cx="9" cy="6" r="1.2" fill="currentColor"/><circle cx="15" cy="6" r="1.2" fill="currentColor"/><circle cx="9" cy="12" r="1.2" fill="currentColor"/><circle cx="15" cy="12" r="1.2" fill="currentColor"/><circle cx="9" cy="18" r="1.2" fill="currentColor"/><circle cx="15" cy="18" r="1.2" fill="currentColor"/>',
    sliders: '<path d="M4 7h10M18 7h2M4 17h4M12 17h8"/><circle cx="16" cy="7" r="2"/><circle cx="10" cy="17" r="2"/>',
    // Mediensteuerung
    pause: '<rect x="6.5" y="5" width="3.6" height="14" rx="1" fill="currentColor" stroke="none"/><rect x="13.9" y="5" width="3.6" height="14" rx="1" fill="currentColor" stroke="none"/>',
    skipPrev: '<path d="M6 5.5v13"/><path d="M19 5.8v12.4L9.2 12z" fill="currentColor" stroke="none"/>',
    skipNext: '<path d="M18 5.5v13"/><path d="M5 5.8v12.4L14.8 12z" fill="currentColor" stroke="none"/>',
    back10: '<path d="M3.5 12a8.5 8.5 0 1 0 8.5-8.5 9.2 9.2 0 0 0-6.4 2.6L3.5 8.2"/><path d="M3.5 3.5v4.7h4.7"/><text x="12.2" y="15.2" text-anchor="middle" font-size="8" font-weight="700" font-family="system-ui, sans-serif" fill="currentColor" stroke="none">10</text>',
    forward10: '<path d="M20.5 12A8.5 8.5 0 1 1 12 3.5a9.2 9.2 0 0 1 6.4 2.6l2.1 2.1"/><path d="M20.5 3.5v4.7h-4.7"/><text x="11.8" y="15.2" text-anchor="middle" font-size="8" font-weight="700" font-family="system-ui, sans-serif" fill="currentColor" stroke="none">10</text>',
    volumeHigh: '<path d="M4 9.3h3.4L12 5.4v13.2l-4.6-3.9H4z" fill="currentColor"/><path d="M15.6 9a4.2 4.2 0 0 1 0 6M18.4 6.2a8.2 8.2 0 0 1 0 11.6"/>',
    volumeLow: '<path d="M4 9.3h3.4L12 5.4v13.2l-4.6-3.9H4z" fill="currentColor"/><path d="M15.6 9a4.2 4.2 0 0 1 0 6"/>',
    volumeMute: '<path d="M4 9.3h3.4L12 5.4v13.2l-4.6-3.9H4z" fill="currentColor"/><path d="M15.8 9.5l5 5M20.8 9.5l-5 5"/>',
    fullscreen: '<path d="M4 9V4h5M15 4h5v5M20 15v5h-5M9 20H4v-5"/>',
    fullscreenExit: '<path d="M9 4v5H4M20 9h-5V4M15 20v-5h5M4 15h5v5"/>',
    pip: '<rect x="2.5" y="4.5" width="19" height="15" rx="2"/><rect x="12" y="11.5" width="7" height="5.5" rx="1" fill="currentColor" stroke="none"/>',
    gear: '<path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/>',
    // Autoplay: Unendlichkeitszeichen mit Play-Symbol (wie das frühere Autoplay-Bild)
    autoplay: '<path d="M12 12c-1.8-2.4-3.6-3.6-5.4-3.6a3.6 3.6 0 0 0 0 7.2c1.8 0 3.6-1.2 5.4-3.6zm0 0c1.8 2.4 3.6 3.6 5.4 3.6a3.6 3.6 0 0 0 0-7.2c-1.8 0-3.6 1.2-5.4 3.6z" stroke-opacity="0.5"/><path d="M9 6.6v10.8l8.6-5.4z" fill="currentColor" stroke="none"/>'
};

// Erstellt ein Symbol als SVG-Element (nur feste Formen aus iconShapes)
function createIcon(name) {
    var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("viewBox", "0 0 24 24");
    svg.setAttribute("fill", "none");
    svg.setAttribute("stroke", "currentColor");
    svg.setAttribute("stroke-width", "2");
    svg.setAttribute("stroke-linecap", "round");
    svg.setAttribute("stroke-linejoin", "round");
    svg.setAttribute("aria-hidden", "true");
    svg.setAttribute("class", "icon icon-" + name);
    svg.innerHTML = iconShapes[name] || "";
    return svg;
}

// Symbolname anhand der Dateiendung
function getMediaIconName(filename) {
    if (isVideo(filename)) {
        return "video";
    } else if (isMusic(filename)) {
        return "music";
    } else if (isImage(filename)) {
        return "image";
    }
    return "folder";
}

// HTML Spezialcharakter in String ersetzen
function replaceSpecialChars(string) {
    return string.replaceAll("%20", " ").replaceAll("%C3%B6", "ö").replaceAll("%C3%BC", "ü").replaceAll("%C3%A4", "ä");
}

// Funktion zum Normalisieren eines Medienpfads
function normalizeMediaLocation(mediaUrl) {
    if (mediaUrl == null) {
        return null;
    }
    if (!mediaUrl.startsWith(".")) {
        mediaUrl = "." + mediaUrl;
    }
    return mediaUrl;
}

// ===================================================================
// Hilfsfunktionen: URL-Daten
// Aufbau: LEVEL0:Serien$=$LEVEL1:Show$=$MEDIA:./Serien/Show/S01E01.mp4$=$PL:SerienShow
// ===================================================================

// Kodiert die Daten für die URL. Gibt null zurück, wenn ein Name Zeichen ausserhalb
// von Latin-1 enthält (updateData.exe ersetzt solche Zeichen).
function encodeUrlData(decodedData) {
    try {
        return btoa(decodedData);
    } catch (error) {
        console.warn("Ungültige Zeichen - bitte updateData.exe ausführen:", decodedData);
        return null;
    }
}

function decodeUrlData(url) {
    if (url == null || !url.includes("?=")) {
        return null;
    }
    try {
        return atob(url.split("?=")[1].split("#")[0]);
    } catch (error) {
        return null;
    }
}

// Funktion zum sicheren Lesen des decodierten URL-Inhalts
function getDecodedUrlDataFromWindow() {
    return decodeUrlData(window.location.href);
}

// Funktion zum Erstellen einer start.html URL aus decodierten Daten
function buildStartUrl(decodedData, withAnchor) {
    var encoded = encodeUrlData(decodedData);
    if (encoded == null) {
        return null;
    }
    return "start.html?=" + encoded + (withAnchor === false ? "" : "#mediaNav");
}

// Gibt die Ordnerebenen der URL als Liste zurück
function getLevels(decodedData) {
    var levels = [];
    if (decodedData == null) {
        return levels;
    }
    while (decodedData.includes("LEVEL" + levels.length + ":")) {
        levels.push(decodedData.split("LEVEL" + levels.length + ":")[1].split(listSeperator)[0]);
    }
    return levels;
}

function buildLevelPath(levels) {
    return levels.map(function (level, index) {
        return "LEVEL" + index + ":" + level;
    }).join(listSeperator);
}

// Funktion um die Level und das Medium aus der URL auszulesen
function extractData(decodedUrl) {
    var levels = {};
    getLevels(decodedUrl).forEach(function (level, index) {
        levels[index] = level;
    });
    var media = null;
    if (decodedUrl.includes("MEDIA:")) {
        media = decodedUrl.split("MEDIA:")[1].split("#")[0].split(listSeperator)[0];
    }
    return { "media": media, "levels": levels };
}

// Funktion zum Lesen des Playlistnamens aus decodierten Daten
function getPlaylistNameFromDecodedData(decodedData) {
    if (decodedData == null || !decodedData.includes(listSeperator + "PL:")) {
        return null;
    }
    return decodedData.split(listSeperator + "PL:")[1].split(listSeperator)[0].split("#")[0];
}

// Funktion zum Lesen des Medienpfads aus decodierten Daten
function getMediaLocationFromDecodedData(decodedData) {
    if (decodedData == null || !decodedData.includes("MEDIA:")) {
        return null;
    }
    return replaceSpecialChars(decodedData.split("MEDIA:")[1].split(listSeperator)[0].split("#")[0]);
}

// Erstellt die URL-Daten für ein Medium anhand seines Pfads (./Serien/Show/S01E01.mp4)
function buildMediaData(mediaPath, playlistName) {
    var folders = mediaPath.replace(/^\.\//, "").split("/");
    folders.pop();
    if (playlistName == null) {
        playlistName = folders.join("");
    }
    return buildLevelPath(folders) + listSeperator + "MEDIA:" + normalizeMediaLocation(mediaPath) + listSeperator + "PL:" + playlistName;
}

// ===================================================================
// Hilfsfunktionen: Datenbaum
// ===================================================================

// Gibt den Teil des Datenbaums für die angegebenen Ebenen zurück (oder null)
function getDataNode(levels) {
    var node = data;
    for (var index = 0; index < levels.length; index++) {
        var level = levels[index];
        if (level.startsWith(".")) {
            level = level.replace(".", "");
        }
        if (node == null || typeof node !== "object" || !node.hasOwnProperty(level)) {
            return null;
        }
        node = node[level];
    }
    return node;
}

// Prüft, ob ein Ordner Medien enthält (optional auch in Unterordnern)
function hasMedia(node, recursive) {
    if (node == null || typeof node !== "object") {
        return false;
    }
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            return true;
        }
        if (recursive && hasMedia(node[key], true)) {
            return true;
        }
    }
    return false;
}

// Prüft, ob ein Medium (noch) in den Daten vorhanden ist
function mediaExists(mediaPath) {
    var folders = mediaPath.replace(/^\.\//, "").split("/");
    folders.pop();
    var folder = getDataNode(folders);
    return folder != null && typeof folder === "object" && folder.hasOwnProperty(mediaPath);
}

// ===================================================================
// Vorschaubilder
// ===================================================================

// updateData.exe 2.00/2.01 hat verkleinerte Kopien in sflix_sys/thumbs abgelegt und deren Pfad in
// data.js eingetragen. Solange eine solche data.js vorhanden ist, wird stattdessen das Original im
// Preview-Ordner verwendet (die Kopien waren immer .jpg, die meisten Originale auch).
// Ab 2.02 verkleinert updateData.exe direkt im Preview-Ordner und trägt diese Bilder ein.
function getOriginalPreviewPath(path) {
    var match = /^\.\/sflix_sys\/thumbs\/(.+)$/.exec(path);
    return match == null ? path : "./" + match[1];
}

// Sucht im Preview-Ordner eines Datenknotens nach einem passenden Bild.
// Zuerst wird ein exakter Namenstreffer gesucht, danach (optional) ein Teiltreffer.
function findPreviewInFolder(folderData, names, exactOnly) {
    if (folderData == null || typeof folderData !== "object" || folderData["Preview"] == null) {
        return null;
    }
    var previews = Object.keys(folderData["Preview"]).map(getOriginalPreviewPath);
    var index, name, previewIndex;

    for (index = 0; index < names.length; index++) {
        name = names[index];
        for (previewIndex = 0; previewIndex < previews.length; previewIndex++) {
            if (name != null && getDisplayName(previews[previewIndex]) === name) {
                return previews[previewIndex];
            }
        }
    }

    if (exactOnly) {
        return null;
    }

    for (index = 0; index < names.length; index++) {
        name = names[index];
        for (previewIndex = 0; previewIndex < previews.length; previewIndex++) {
            if (name != null && getDisplayName(previews[previewIndex]).includes(name)) {
                return previews[previewIndex];
            }
        }
    }
    return null;
}

// Sucht das passende Vorschaubild und gibt dessen Pfad zurück (oder null)
// reason "normal": Menüeintrag, sonst Suche/Favoriten/Weiterschauen (Ordnerbild als Ersatz)
function resolvePreviewSrc(decodedUrl, reason) {
    reason = reason || "normal";
    var extractedData = extractData(decodedUrl);
    var levels = getLevels(decodedUrl);
    var media = extractedData["media"];
    var previewImageSrc = null;

    if (media == null || reason != "normal") {
        // Ordnerbild: das Bild der tiefsten Ebene, die eines hat
        var node = data;
        for (var index = 0; index < levels.length; index++) {
            var level = levels[index].replace(/^\./, "");
            if (node == null || typeof node !== "object" || !node.hasOwnProperty(level)) {
                break;
            }
            var found = findPreviewInFolder(node, [level], false);
            if (found != null) {
                previewImageSrc = found;
            }
            node = node[level];
        }
        if (previewImageSrc == null && media != null) {
            previewImageSrc = findPreviewInFolder(getDataNode(levels), [getDisplayName(media)], false);
        }
    } else {
        // Medium im Menü: nur ein Bild mit identischem Namen verwenden
        previewImageSrc = findPreviewInFolder(getDataNode(levels), [getDisplayName(media)], true);
    }

    if (previewImageSrc == null) {
        return null;
    }

    if (!previewImageSrc.startsWith(".")) {
        previewImageSrc = "." + previewImageSrc;
    }
    return previewImageSrc;
}

// Vorschaubild eines Mediums: zuerst ein Bild mit gleichem Namen, sonst (optional)
// das Bild des tiefsten Ordners, der eines hat (z.B. Serienbild für eine Episode)
function getPreviewForMedia(mediaPath, exactOnly) {
    var folders = getMediaFolders(mediaPath);
    var exact = findPreviewInFolder(getDataNode(folders), [getDisplayName(mediaPath)], true);
    if (exact != null) {
        return exact.startsWith(".") ? exact : "." + exact;
    }
    if (exactOnly || folders.length == 0) {
        return null;
    }
    return resolvePreviewSrc(buildLevelPath(folders), "normal");
}

// Vorschaubild eines Mediums mit Ersatz aus den darüberliegenden Ordnern, vom nächsten zum
// entferntesten: Folge -> Staffel -> Serie. Das Bild der Kategorie (z.B. "Serien" oder "Filme")
// wird nicht verwendet, sonst hätten alle Medien ohne eigenes Bild dasselbe Kategoriebild.
function getInheritedPreview(mediaPath) {
    var exact = getPreviewForMedia(mediaPath, true);
    if (exact != null) {
        return exact;
    }
    var folders = getMediaFolders(mediaPath);
    for (var depth = folders.length - 1; depth >= 1; depth--) {
        var found = findPreviewInFolder(getDataNode(folders.slice(0, depth)), [folders[depth]], false);
        if (found != null) {
            return found.startsWith(".") ? found : "." + found;
        }
    }
    return null;
}

// Ordner eines Medienpfads: ./Serien/Show/S01/x.mp4 -> ["Serien", "Show", "S01"]
function getMediaFolders(mediaPath) {
    var folders = String(mediaPath).replace(/^\.\//, "").split("/");
    folders.pop();
    return folders;
}

function collectMediaPaths(node, result) {
    for (var key in node) {
        if (!node.hasOwnProperty(key)) {
            continue;
        }
        // Preview-Ordner enthalten nur Vorschaubilder, keine Medien
        if (key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            result.push(normalizeMediaLocation(key));
        } else if (node[key] != null && typeof node[key] === "object") {
            collectMediaPaths(node[key], result);
        }
    }
    return result;
}
