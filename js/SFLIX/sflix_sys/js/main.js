// STEFFLIX - main.js
// Start der Anwendung (muss als letzte Datei geladen werden)
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

//  __   __  _______  ______    _______  _______  ______    _______  ___   _______  __   __  __    _  _______
// |  | |  ||       ||    _ |  |  _    ||       ||    _ |  |       ||   | |       ||  | |  ||  |  | ||       |
// |  |_|  ||   _   ||   | ||  | |_|   ||    ___||   | ||  |    ___||   | |_     _||  | |  ||   |_| ||    ___|
// |       ||  | |  ||   |_||_ |       ||   |___ |   |_||_ |   |___ |   |   |   |  |  |_|  ||       ||   | __
// |       ||  |_|  ||    __  ||  _   | |    ___||    __  ||    ___||   |   |   |  |       ||  _    ||   ||  |
//  |     | |       ||   |  | || |_|   ||   |___ |   |  | ||   |___ |   |   |   |  |       || | |   ||   |_| |
//   |___|  |_______||___|  |_||_______||_______||___|  |_||_______||___|   |___|  |_______||_|  |__||_______|

function initEventListeners() {
    // Einstellungen: Anzahl Autoplay steuert die Checkbox
    document.getElementById("amountOfAutoplay").addEventListener("change", function () {
        document.getElementById("autoplayCheckBox").checked = document.getElementById("amountOfAutoplay").value > 0;
    });

    // "Dauer bis Übergang" darf nicht negativ sein
    document.getElementById("autoplayDuration").addEventListener("change", function () {
        if (document.getElementById("autoplayDuration").value < 0) {
            document.getElementById("autoplayDuration").value = 0;
        }
    });

    // "Gilt für": Wert der gewählten Ebene anzeigen (gespeichert wird mit "Speichern")
    document.getElementById("autoplayDurationScope").addEventListener("change", function () {
        var scope = document.getElementById("autoplayDurationScope").value;
        document.getElementById("autoplayDuration").value = getTransitionValueAt(scope, currentMediaLocation);
        updateSettingsBoxScopeHint();
    });

    // Tastatur
    document.addEventListener("keydown", handleButtonKeys);
    document.addEventListener("keydown", handleKeyboardShortcuts);
    initSettingsMenu();
    initModalOutsideClose();
    initSearch();
    initImageViewer();
    initShuffleButton();
    initAutoplayNavButton();
    initPlayerControls();
    initAutoRowResize();
    // Im Fenster von STEFFLIX.exe: Menüeinträge für die Tools, F11, Brücke zum Programm
    initDesktopApp();

    // Neu erstellte Elemente ebenfalls per Tastatur bedienbar machen
    enhanceAccessibility(document.body);
    new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(enhanceAccessibility);
        });
    }).observe(document.body, { childList: true, subtree: true });

    // Zurück/Vorwärts im Browser
    window.addEventListener("popstate", function (event) {
        var decodedData = getDecodedUrlDataFromWindow();
        if (decodedData != null && decodedData.includes("MEDIA:") && currentMediaLocation != null) {
            // Im Player von Medium zu Medium: nur das Medium wechseln
            isPopStateNavigation = true;
            loadDecodedMedia(decodedData, false);
            isPopStateNavigation = false;
            return;
        }
        // Andere Seite (Ordner, Suche, Startseite ...): ohne Neuladen aufbauen, Scrollposition von damals
        var state = event.state;
        showPage(state != null && typeof state.sflixScroll === "number" ? state.sflixScroll : null);
    });

    // Links auf STEFFLIX-Seiten (Ordner, Pfadleiste, Logo ...) ohne Neuladen öffnen
    document.addEventListener("click", handlePageLinkClick);
}

// ===================================================================
// Seitenwechsel ohne Neuladen (seit 2.8.7)
// Bis 2.8.6 lud jede Seite start.html neu: alle Programmdateien und data.js wurden wieder gelesen
// (über ein NAS spürbar) und der Ladescreen erschien jedes Mal. Jetzt wird beim Start der leere
// Aufbau der Seite gemerkt (pageTemplate) und bei jedem Wechsel wiederhergestellt, danach baut die
// passende Funktion (renderCurrentPage) die Seite auf wie beim Start.
// ===================================================================

// Bereiche, die die Seiten verändern (in dieser Reihenfolge direkt nach der Kopfzeile)
var pageRegionIds = ["navigation", "media", "mediaNav", "folderBands"];
var pageTemplate = null;
// true ab dem ersten Wechsel ohne Neuladen (z.B. kein Startbildschirm mehr)
var softNavigation = false;

function capturePageTemplate() {
    pageTemplate = pageRegionIds.map(function (id) {
        return document.getElementById(id).cloneNode(true);
    });
}

function restorePageTemplate() {
    pageRegionIds.forEach(function (id) {
        var old = document.getElementById(id);
        if (old != null) {
            old.remove();
        }
    });
    var previous = document.querySelector(".firstLevelWrap");
    pageTemplate.forEach(function (template) {
        var node = template.cloneNode(true);
        previous.after(node);
        previous = node;
    });
    // Elemente, die initEventListeners beim Start in diese Bereiche einfügt
    initImageViewer();
    initShuffleButton();
    initAutoplayNavButton();
}

// Laufende Wiedergabe beenden und den Zustand des Players zurücksetzen
function stopCurrentMedia() {
    var media = document.getElementById("video");
    if (media != null && (media.tagName == "VIDEO" || media.tagName == "AUDIO")) {
        clearTimeout(media.sflixStallTimer);
        if (media.sflixRecovery != null && media.sflixRecovery.timer != null) {
            clearTimeout(media.sflixRecovery.timer);
        }
        media.sflixRecovery = null;
        try {
            media.pause();
        } catch (error) {
            // schon beendet
        }
        // Verbindung zur Datei schliessen (NAS, Medienserver der App)
        media.removeAttribute("src");
        try {
            media.load();
        } catch (error) {
            // nichts zu tun
        }
    }
    var container = document.getElementById("playerContainer");
    if (container != null) {
        container.replaceChildren();
    }
    // Erst ohne Player beenden: exitCinemaMode und setImageFullscreen scrollen sonst zum (alten) Player
    exitCinemaMode();
    var mediaNode = document.getElementById("media");
    if (mediaNode != null && mediaNode.classList.contains("fullscreened")) {
        mediaNode.classList.remove("fullscreened");
        exitBrowserFullscreen();
    }
    timeoutArray = cancelTimeouts();
    currentMediaLocation = null;
    playerUI = null;
    lastNavigation = null;
    renderedMusicListKey = null;
    durationQueue = [];
    if (durationObserver != null) {
        durationObserver.disconnect();
        durationObserver = null;
    }
}

function resetPage() {
    closeCardGrid();
    modalIds.forEach(function (id) {
        var modal = document.getElementById(id);
        if (modal != null && modal.style.display != "none") {
            closeContainer(id);
        }
    });
    var searchBox = document.getElementById("searchFieldContainer");
    if (searchBox != null && searchBox.style.display != "none") {
        toggleContainer(["searchFieldContainer"], "none");
    }
    stopCurrentMedia();
    restorePageTemplate();
    renderedFolderView = null;
    currentFolderLevels = null;
    Array.prototype.forEach.call(document.getElementById("menu").children, function (item) {
        item.classList.remove("activeMenu");
    });
    document.title = "STEFFLIX";
}

// Scrollposition der aktuellen Seite im Verlauf merken (für Zurück)
function saveScrollPosition(scrollTop) {
    try {
        var state = history.state != null && typeof history.state === "object" ? history.state : {};
        var copy = {};
        Object.keys(state).forEach(function (key) {
            copy[key] = state[key];
        });
        copy.sflixScroll = scrollTop != null ? scrollTop : window.scrollY;
        history.replaceState(copy, "");
    } catch (error) {
        // ohne gemerkte Position
    }
}

// Öffnet eine Seite von STEFFLIX (z.B. buildStartUrl("HELP")) ohne Neuladen
function navigateTo(url) {
    saveScrollPosition();
    history.pushState({}, "", url);
    showPage(null);
}

// Baut die Seite der aktuellen Adresse neu auf; scrollTop = gemerkte Position (Zurück)
function showPage(scrollTop) {
    softNavigation = true;
    resetPage();
    var decodedData = renderCurrentPage();
    var isMedia = decodedData != null && decodedData.includes("MEDIA:");
    if (scrollTop != null) {
        // Nach dem Aufbau, sonst ist die Seite noch zu kurz
        requestAnimationFrame(function () {
            window.scrollTo(0, scrollTop);
        });
    } else if (!isMedia && location.hash.length <= 1) {
        window.scrollTo(0, 0);
    }
}

// Die Scrollposition beim Zurück setzt STEFFLIX selbst (der Browser kennt die neu aufgebaute Seite nicht)
if ("scrollRestoration" in history) {
    history.scrollRestoration = "manual";
}

// Klick auf einen Link zu einer Seite von STEFFLIX: ohne Neuladen öffnen. Links mit eigener
// Behandlung (z.B. Medien über setMediaLink) haben das Neuladen schon verhindert.
function handlePageLinkClick(event) {
    if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
        return;
    }
    var link = event.target && event.target.closest ? event.target.closest("a[href]") : null;
    if (link == null || (link.target && link.target !== "_self") || link.hasAttribute("download")) {
        return;
    }
    var url;
    try {
        url = new URL(link.href, location.href);
    } catch (error) {
        return;
    }
    if (url.protocol !== location.protocol || url.host !== location.host || url.pathname !== location.pathname) {
        return;
    }
    // Nur die Sprungmarke ändert sich: wie gewohnt springen
    if (url.search === location.search && url.hash !== "") {
        return;
    }
    event.preventDefault();
    navigateTo(url.href);
}

function startApplication() {
    // Alle Seiten im Dark-Mode-Design
    document.body.classList.add("darkPage");
    loadSettings();
    // Kachelgrösse und Animationen aus den Einstellungen
    applyDisplaySettings();

    // Lokale Daten updaten
    updateData();

    // Leerer Aufbau der Seite für die Wechsel ohne Neuladen (vor initEventListeners, das Knöpfe einfügt)
    capturePageTemplate();
    initEventListeners();
    updateFeedbackVisibility();
    createMainMenu();
    renderCurrentPage();
}

// Baut die Seite zur aktuellen Adresse auf (beim Start und bei jedem Wechsel); gibt die Daten der Adresse zurück
function renderCurrentPage() {
    var decodedUriData = getDecodedUrlDataFromWindow();

    if (decodedUriData != null && decodedUriData.startsWith("SEARCH:")) {
        showSearchPage(decodedUriData.split("SEARCH:")[1]);
    } else if (decodedUriData == "FAVORITES") {
        showFavoritesPage();
    } else if (decodedUriData != null && decodedUriData.includes("MEDIA:")) {
        // Player-Seite: Medium und Bänder mit Ordnerinhalt
        loadDecodedMedia(decodedUriData, false);
        scrollToPlayer(false);
    } else if (decodedUriData != null && decodedUriData.includes("LEVEL0:")) {
        renderFolderPage(getLevels(decodedUriData).map(replaceSpecialChars));
        window.scrollTo(0, 0);
    } else if (decodedUriData == "HELP") {
        showHelpPage();
    } else if (decodedUriData == "SETTINGS") {
        showSettingsPage();
    } else {
        showStartPage();
    }
    return decodedUriData;
}

initDatabase(function () {
    startApplication();
    // Der Ladescreen wartet auf den Aufbau der Seite (IndexedDB öffnet asynchron, oft erst nach onload)
    markApplicationStarted();
    // STEFFLIX.exe: Seite ist bereit (Datenübernahme, Update-Prüfung)
    sendToDesktop({ cmd: "ready" });
    // Von STEFFLIX.exe im Browser geöffnet: Daten für die Übernahme speichern
    if (isDesktopExportRequest() && !isDesktopApp()) {
        showDesktopExportPage();
    }
});
