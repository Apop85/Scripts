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

    // Lizenzschlüsselfelder: automatisch ins nächste Feld springen
    licenseFields.forEach(function (fieldId, index) {
        document.getElementById(fieldId).addEventListener("input", function () {
            if (document.getElementById(fieldId).value.length == 4 && index + 1 < licenseFields.length) {
                document.getElementById(licenseFields[index + 1]).focus();
            } else if (index + 1 >= licenseFields.length) {
                document.getElementById("saveSerialButton").focus();
            }
        });
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

    // Neu erstellte Elemente ebenfalls per Tastatur bedienbar machen
    enhanceAccessibility(document.body);
    new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(enhanceAccessibility);
        });
    }).observe(document.body, { childList: true, subtree: true });

    // Zurück/Vorwärts im Browser
    window.addEventListener("popstate", function () {
        var decodedData = getDecodedUrlDataFromWindow();
        if (decodedData == null || !decodedData.includes("MEDIA:")) {
            // Zurück zur Ordnerseite o.ä.: Seite neu aufbauen
            location.reload();
            return;
        }
        isPopStateNavigation = true;
        loadDecodedMedia(decodedData, false);
        isPopStateNavigation = false;
    });
}

function startApplication() {
    // Alle Seiten im Dark-Mode-Design
    document.body.classList.add("darkPage");
    loadSettings();

    // Lokale Daten updaten
    updateData();

    initEventListeners();
    updateFeedbackVisibility();
    createMainMenu();

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
    } else {
        showStartPage();
    }
}

initDatabase(function () {
    startApplication();
});
