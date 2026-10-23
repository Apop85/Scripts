// STEFFLIX - ui.js
// Bedienung: Fenster, Autoplay-Einstellungen, Feedback und Lizenz, Hamburger-Menü, Tastatur
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

// Funktion zum Öffnen und Schliessen von Containern
function toggleContainer(idArray, displayMode) {
    var nodeId = null;
    var node = null;
    // Chrome/Edge: Ein Video unter einem Fenster fängt sonst die Klicks ab.
    // Auf Seiten ohne Player (Start, Hilfe, Suche) gibt es nichts zu sperren.
    var player = document.getElementById("video");
    var needsVideoWorkaround = player != null && (detectBrowser() == "chrome" || detectBrowser() == "edge");
    for (var key in idArray) {
        nodeId = idArray[key];
        node = document.getElementById(nodeId);
        if (node.style.display == "none") {
            node.style.display = displayMode;
            if (idArray[key] == "searchFieldContainer") {
                document.getElementById("searchField").focus();
            } else if (idArray[key] == "serialKeyWrapper") {
                var license = getData("serial");
                if (license != null) {
                    var serialIndex = 0;
                    for (var index = 0; index < licenseFields.length; index++) {
                        document.getElementById(licenseFields[index]).value = license.slice(serialIndex, serialIndex + 4);
                        serialIndex += 4;
                    }
                }
            }
            // Video-Interaktion einschränken (Chrome Bug Workaround)
            if (needsVideoWorkaround) {
                player.style.pointerEvents = "none";
            }
        } else {
            node.style.display = "none";
            // Video-Interaktion freigeben (Chrome Bug Workaround)
            if (needsVideoWorkaround) {
                player.style.pointerEvents = null;
            }
        }
    }
}

// Schliesst einen Container (für die Escape-Taste)
function closeContainer(id) {
    var node = document.getElementById(id);
    if (node == null || node.style.display == "none") {
        return false;
    }
    node.style.display = "none";
    var player = document.getElementById("video");
    if (player != null) {
        player.style.pointerEvents = null;
    }
    return true;
}

// ===================================================================
// Einstellungen und Autoplay
// ===================================================================

// "Unbegrenzt": Autoplay läuft bis zum Ende der Playliste, die Anzahl wird nicht heruntergezählt
function isAutoplayInfinite() {
    return document.getElementById("autoplayInfinite").checked;
}

// Einstellungen speichern: Autoplay, Anzahl, Dauer, Unbegrenzt
function saveAutoplaySettings(newAutoplay, newAmount, newDuration) {
    setData("settings", [newAutoplay, newAmount, newDuration, isAutoplayInfinite()].join(listSeperator));
}

// Anzahl ist bei "Unbegrenzt" ohne Bedeutung und wird ausgegraut
function updateAutoplayAmountState() {
    var amountField = document.getElementById("amountOfAutoplay");
    amountField.disabled = isAutoplayInfinite();
    amountField.closest(".settingRow").classList.toggle("disabled", amountField.disabled);
}

// Speichern der aktuellen Einstellungen
function applySettings() {
    autoplay = document.getElementById("autoplayCheckBox").checked;
    autoplayAmount = parseInt(document.getElementById("amountOfAutoplay").value) || 0;
    autoplayDuration = parseInt(document.getElementById("autoplayDuration").value) || 0;

    if (!autoplay) {
        autoplayAmount = 0;
    } else if (autoplayAmount == 0 && !isAutoplayInfinite()) {
        autoplay = false;
    }

    var mediaNode = document.getElementById("media");
    var nextNode = document.getElementById("next");
    if (!autoplay) {
        mediaNode.classList.remove("autoplaying");
        nextNode.style.animation = null;
        // Lösche alle gesetzten Timeouts
        timeoutArray = cancelTimeouts();
    } else {
        mediaNode.classList.add("autoplaying");
    }

    saveAutoplaySettings(autoplay, autoplayAmount, autoplayDuration);
    updateOptionFields(autoplay, autoplayAmount, autoplayDuration);
    toggleContainer(["settingsBox"], "none");

    var player = document.getElementById("video");
    if (autoplay && player != null && player.tagName == "IMG") {
        timeoutArray = cancelTimeouts();
        nextNode.style.animation = "autoplayLoading " + (autoplayDuration + minTime) + "s";
        timeoutArray.push(setTimeout(function () {
            clickNavigation("next");
        }, (autoplayDuration + minTime) * 1000));
        updateOptions(autoplay, autoplayAmount, autoplayDuration);
    }
}

// Beim Aktivieren der Autoplayfunktion muss die Anzahl mindestens 1 sein (ausser bei "Unbegrenzt")
function checkAutoplayAmount() {
    var mediaNode = document.getElementById("media");
    if (document.getElementById("autoplayCheckBox").checked) {
        if (document.getElementById("amountOfAutoplay").value <= 0 && !isAutoplayInfinite()) {
            document.getElementById("amountOfAutoplay").value = 1;
        }
        mediaNode.classList.add("autoplaying");
    } else {
        mediaNode.classList.remove("autoplaying");
        document.getElementById("amountOfAutoplay").value = 0;
    }
}

// Funktion, um die Einstellungen anhand abgespielter Medien zu aktualisieren
function updateOptions(newAutoplay, newAmount, newDuration) {
    newAutoplay = newAutoplay === true || newAutoplay === "true";
    newAmount = parseInt(newAmount) || 0;
    newDuration = parseInt(newDuration) || 0;

    // Verringere Anzahl Autoplay um 1 und deaktiviere Autoplay bei 0 (nicht bei "Unbegrenzt")
    if (!isAutoplayInfinite()) {
        newAmount = Math.max(0, newAmount - 1);
        if (newAutoplay && newAmount == 0) {
            newAutoplay = false;
        }
    }

    newDuration = Math.max(0, newDuration);

    // Entferne Autoplay-Klasse
    if (!newAutoplay) {
        var mediaNode = document.getElementById("media");
        if (mediaNode.classList.contains("autoplaying")) {
            mediaNode.classList.remove("autoplaying");
            document.getElementById("next").style.animation = null;
        }
    }

    updateOptionFields(newAutoplay, newAmount, newDuration);
    saveAutoplaySettings(newAutoplay, newAmount, newDuration);
}

// Funktion, um die gespeicherten Einstellungen auf das Einstellungsmenü zu übertragen
function updateOptionFields(newAutoplay, newAmount, newDuration) {
    document.getElementById("autoplayCheckBox").checked = newAutoplay;
    document.getElementById("amountOfAutoplay").value = newAmount;
    document.getElementById("autoplayDuration").value = newDuration;
}

function loadSettings() {
    var settings = getData("settings");

    if (settings == null) {
        autoplay = false;
        autoplayAmount = 0;
        autoplayDuration = 0;
        document.getElementById("autoplayInfinite").checked = false;
        saveAutoplaySettings(autoplay, autoplayAmount, autoplayDuration);
    } else {
        settings = settings.split(listSeperator);
        autoplay = settings[0] != "false";
        autoplayAmount = parseInt(settings[1]) || 0;
        autoplayDuration = parseInt(settings[2]) || 0;
        // Vierter Wert ab 2.00 (ältere Einstellungen haben ihn nicht)
        document.getElementById("autoplayInfinite").checked = settings[3] === "true";
        updateOptionFields(autoplay, autoplayAmount, autoplayDuration);
    }
    updateAutoplayAmountState();
}

// Funktion, um die gesetzten Timeouts abzubrechen
function cancelTimeouts() {
    timeoutArray.forEach(function (element) {
        clearTimeout(element);
    });
    return [];
}

// ===================================================================
// Feedback und Lizenz
// ===================================================================

// Funktion zum Umwandeln von Base64-Strings in Ganzzahlen
function b64toInt(b64string) {
    try {
        var binaryString = window.atob(b64string);
        var value = BigInt(0);
        for (var index = 0; index < binaryString.length; index++) {
            value = (value * BigInt(256)) + BigInt(binaryString.charCodeAt(index));
        }
        return value;
    } catch (error) {
        return null;
    }
}

// Funktion zum Entschlüsseln des Lizenzkeys
function decodeSerial(serial) {
    try {
        // Öffentlicher Schlüssel
        var publicKey = "A45KXDyaWbXfUSyIxXKG8JBXwX+IxqDl";
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

        var publicValue = b64toInt(publicKey);
        var secretValue = b64toInt(serial);

        // Wandle Nummer in Bitstring um und fülle die Bitlänge auf
        var secretBits = secretValue.toString(2);
        var publicBits = publicValue.toString(2);
        while (secretBits.length < publicBits.length) {
            secretBits = "0" + secretBits;
        }
        while (publicBits.length < secretBits.length) {
            publicBits = "0" + publicBits;
        }

        // XOR bitweise
        var privateBits = "";
        for (var bitIndex = 0; bitIndex < publicBits.length; bitIndex++) {
            privateBits += secretBits[bitIndex] == publicBits[bitIndex] ? "0" : "1";
        }

        // Berechne Ganzzahl des privaten Schlüssels
        var privateKey = BigInt("0b" + privateBits);

        // Setze Nachricht zusammen und entschlüssle sie
        var lookupString = privateKey + "" + secretValue;
        var result = "";
        for (var index = 0; index < lookupString.length; index += 2) {
            result += conversionMatrix[lookupString[index]][lookupString[index + 1]];
        }

        // Lese Telegram-Informationen aus
        return [result.split(listSeperator)[0], result.split(listSeperator)[1]];
    } catch (error) {
        return null;
    }
}

// Meldung im Feedback-Fenster (type: "error", "success" oder "info")
function showFeedbackStatus(text, type) {
    var statusNode = document.getElementById("feedbackStatus");
    statusNode.textContent = text;
    statusNode.className = type;
}

// Funktion zum Senden von Telegramnachrichten
function sendMessage() {
    var sender = document.getElementById("feedbackName").value.trim();
    var message = document.getElementById("feedbackText").value.trim();

    var missing = [];
    if (sender == "") {
        missing.push("einen Namen");
    }
    if (message == "") {
        missing.push("eine Nachricht");
    }
    if (missing.length > 0) {
        showFeedbackStatus("Bitte " + missing.join(" und ") + " eingeben.", "error");
        return;
    }

    var serial = getData("serial");
    var telegramConf = serial ? decodeSerial(serial) : null;
    if (telegramConf == null) {
        showFeedbackStatus("Kein gültiger Lizenzschlüssel gespeichert. Die Nachricht wurde nicht verschickt.", "error");
        return;
    }

    // Als Formulardaten senden: Sonderzeichen wie & # + und Zeilenumbrüche bleiben erhalten
    var body = new URLSearchParams();
    body.append("chat_id", telegramConf[1]);
    body.append("text", "ABSENDER: " + sender + "\nNACHRICHT:\n" + message);

    showFeedbackStatus("Nachricht wird gesendet...", "info");
    fetch("https://api.telegram.org/bot" + telegramConf[0] + "/sendMessage", { method: "POST", body: body })
        .then(function (response) {
            if (!response.ok) {
                throw new Error(response.status);
            }
            showFeedbackStatus("Danke! Die Nachricht wurde verschickt.", "success");
            document.getElementById("feedbackText").value = "";
        })
        .catch(function () {
            showFeedbackStatus("Die Nachricht konnte nicht verschickt werden. Bitte Internetverbindung prüfen und später erneut versuchen.", "error");
        });
}

// Prüft, ob sich der Lizenzschlüssel zu Bot-Token und Chat-ID entschlüsseln lässt
function isValidLicense(serial) {
    if (serial == null || serial == "") {
        return false;
    }
    var telegramConf = decodeSerial(serial);
    return telegramConf != null && /^\d+:\S+$/.test(telegramConf[0] || "") && /^-?\d+$/.test(telegramConf[1] || "");
}

// Feedback wird nur angeboten, wenn ein gültiger Lizenzschlüssel gespeichert ist
function updateFeedbackVisibility() {
    var hasLicense = isValidLicense(getData("serial"));
    document.getElementById("feedbackBoxToggler").style.display = hasLicense ? "" : "none";
    if (!hasLicense) {
        document.getElementById("feedbackWrapper").style.display = "none";
    }
}

// Funktion zum Abspeichern des Lizenzschlüssels
function saveLicenseKey() {
    var messageNode = document.getElementById("licenseMsg");
    var licenseKey = "";
    for (var index = 0; index < licenseFields.length; index++) {
        licenseKey += document.getElementById(licenseFields[index]).value.trim();
    }

    if (licenseKey == "") {
        // Leere Felder: Schlüssel entfernen
        removeData("serial");
    } else if (!isValidLicense(licenseKey)) {
        messageNode.textContent = "Lizenzschlüssel ungültig. Bitte Eingabe prüfen.";
        return;
    } else {
        setData("serial", licenseKey);
    }

    messageNode.textContent = "";
    updateFeedbackVisibility();
    toggleContainer(["serialKeyWrapper"], "none");
}

// Funktion, um den verwendeten Browser auszulesen
function detectBrowser() {
    var test = function (regexp) { return regexp.test(window.navigator.userAgent); };
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

// Funktion zum Setzen der Höhe des body-Elements (blendet den Ladescreen aus)
function setHeight() {
    document.getElementById("loadingScreen").style.display = "none";
}

// ===================================================================
// Tastatur: Bedienbarkeit und Tastenkürzel
// ===================================================================

// Macht klickbare Elemente ohne href per Tastatur erreichbar
function enhanceAccessibility(root) {
    if (root == null || root.nodeType !== 1) {
        return;
    }
    var selector = "a:not([href]), #settings";
    var nodes = Array.prototype.slice.call(root.querySelectorAll(selector));
    if (root.matches(selector)) {
        nodes.push(root);
    }
    nodes.forEach(function (node) {
        if (!node.hasAttribute("tabindex")) {
            node.tabIndex = 0;
        }
        if (!node.hasAttribute("role")) {
            node.setAttribute("role", "button");
        }
    });
}

// Enter und Leertaste lösen Elemente mit role="button" aus
function handleButtonKeys(event) {
    var target = event.target;
    if ((event.key !== "Enter" && event.key !== " ") || target.getAttribute == null) {
        return;
    }
    if (target.getAttribute("role") === "button" && target.tagName !== "BUTTON") {
        event.preventDefault();
        target.click();
    }
}

function isTypingTarget(element) {
    if (element == null || element.tagName == null) {
        return false;
    }
    return ["INPUT", "TEXTAREA", "SELECT"].includes(element.tagName) || element.isContentEditable;
}

function isModalOpen() {
    return modalIds.some(function (id) {
        var node = document.getElementById(id);
        return node != null && node.style.display != "none";
    });
}

function isElementVisible(element) {
    var rect = element.getBoundingClientRect();
    return rect.bottom > 0 && rect.top < window.innerHeight;
}

// Schliesst offene Fenster, Menüs und den Bild-Vollbildmodus
function closeOverlays() {
    var closedSomething = false;
    modalIds.concat(["searchFieldContainer"]).forEach(function (id) {
        if (closeContainer(id)) {
            closedSomething = true;
        }
    });
    document.getElementById("settings").classList.remove("open");
    if (!closedSomething && stopDashboardEditing()) {
        closedSomething = true;
    }
    if (!closedSomething && document.getElementById("media").classList.contains("fullscreened")) {
        toggleFullscreenMode();
    }
}

// Tastenkürzel. Greifen nur, wenn nicht gerade in ein Feld getippt wird,
// kein Fenster offen ist und kein Browser-Kürzel (Ctrl/Alt) gedrückt wird.
function handleKeyboardShortcuts(event) {
    if (event.defaultPrevented || event.ctrlKey || event.metaKey || event.altKey) {
        return;
    }

    if (event.key === "Escape") {
        closeOverlays();
        return;
    }

    if (isTypingTarget(event.target) || isModalOpen()) {
        return;
    }

    // Pfeiltasten in den Menübändern der Startseite
    if (handleRowNavigation(event)) {
        return;
    }

    if (event.key === "/") {
        event.preventDefault();
        var searchContainer = document.getElementById("searchFieldContainer");
        if (searchContainer.style.display == "none") {
            searchContainer.style.display = "flex";
        }
        document.getElementById("searchField").focus();
        return;
    }

    var player = document.getElementById("video");
    if (player == null || document.getElementById("media").style.display == "none") {
        return;
    }

    var isPlayable = player.tagName == "VIDEO" || player.tagName == "AUDIO";
    // Hat der Player selbst den Fokus, übernimmt der Browser die Pfeiltasten und die Leertaste
    var targetIsPlayer = event.target === player;
    var targetIsButton = event.target.closest != null && event.target.closest("button, [role='button']") != null;

    switch (event.key) {
        case " ":
        case "k":
        case "K":
            if (!isPlayable || targetIsPlayer || (event.key === " " && targetIsButton)) {
                return;
            }
            event.preventDefault();
            if (player.paused) {
                player.play();
            } else {
                player.pause();
            }
            break;
        case "ArrowLeft":
        case "ArrowRight":
            if (targetIsPlayer) {
                return;
            }
            event.preventDefault();
            var direction = event.key === "ArrowRight" ? 1 : -1;
            if (isPlayable) {
                player.currentTime = Math.max(0, player.currentTime + direction * 10);
            } else {
                // Bilder: vorheriges/nächstes Bild
                clickNavigation(direction > 0 ? "next" : "prev");
            }
            break;
        case "ArrowUp":
        case "ArrowDown":
            // Nur wenn der Player sichtbar ist, sonst normal scrollen
            if (!isPlayable || targetIsPlayer || !isElementVisible(player)) {
                return;
            }
            event.preventDefault();
            var step = event.key === "ArrowUp" ? 0.1 : -0.1;
            player.volume = Math.round(Math.min(1, Math.max(0, player.volume + step)) * 10) / 10;
            break;
        case "f":
        case "F":
            event.preventDefault();
            if (player.tagName == "VIDEO") {
                if (document.fullscreenElement) {
                    document.exitFullscreen();
                } else if (player.requestFullscreen) {
                    player.requestFullscreen();
                }
            } else if (player.tagName == "IMG") {
                toggleFullscreenMode();
            }
            break;
        case "m":
        case "M":
            if (isPlayable) {
                event.preventDefault();
                player.muted = !player.muted;
            }
            break;
        case "n":
        case "N":
            event.preventDefault();
            clickNavigation("next");
            break;
        case "p":
        case "P":
            event.preventDefault();
            clickNavigation("prev");
            break;
    }
}

// Einstellungsmenü per Klick/Tipp öffnen (für Touch-Geräte und Tastatur)
// Fenster aus dem Menü schliessen, wenn daneben geklickt wird.
// pointerdown statt click: der Klick, der ein Fenster öffnet, schliesst es so nicht gleich wieder.
// Der anschliessende Klick wird verschluckt, damit dahinterliegende Kacheln/Links nicht reagieren.
function initModalOutsideClose() {
    var swallowNextClick = false;

    document.addEventListener("pointerdown", function (event) {
        var closedModal = false;
        modalIds.forEach(function (id) {
            var modal = document.getElementById(id);
            if (modal != null && modal.style.display != "none" && !modal.contains(event.target)) {
                closedModal = closeContainer(id) || closedModal;
            }
        });
        // Klicks ins Menü nicht verschlucken (z.B. direkt ein anderes Fenster öffnen)
        if (closedModal && !document.getElementById("settings").contains(event.target)) {
            swallowNextClick = true;
            setTimeout(function () {
                swallowNextClick = false;
            }, 600);
        }
    }, true);

    document.addEventListener("click", function (event) {
        if (swallowNextClick) {
            swallowNextClick = false;
            event.preventDefault();
            event.stopPropagation();
        }
    }, true);
}

function initSettingsMenu() {
    var settingsNode = document.getElementById("settings");
    settingsNode.setAttribute("aria-haspopup", "true");
    settingsNode.setAttribute("aria-label", "Menü");

    // Aufklappen beim Überfahren nur mit Maus. Auf Touch-Geräten bleibt der Hover-Zustand
    // nach einem Tipp sonst bestehen und das Menü verdeckt z.B. das Suchfeld.
    var hoverQuery = window.matchMedia("(hover: hover) and (pointer: fine)");
    var updateHoverClass = function () {
        document.documentElement.classList.toggle("canHover", hoverQuery.matches);
    };
    updateHoverClass();
    if (hoverQuery.addEventListener) {
        hoverQuery.addEventListener("change", updateHoverClass);
    }

    // Tastatur: Menü öffnet sich, sobald der Fokus per Tab hineinkommt, und schliesst beim Verlassen
    settingsNode.addEventListener("focusin", function (event) {
        if (event.target.matches(":focus-visible")) {
            settingsNode.classList.add("open");
        }
    });
    settingsNode.addEventListener("focusout", function () {
        setTimeout(function () {
            if (!settingsNode.contains(document.activeElement)) {
                settingsNode.classList.remove("open");
            }
        }, 0);
    });

    settingsNode.addEventListener("click", function (event) {
        if (event.target === settingsNode) {
            settingsNode.classList.toggle("open");
            if (!settingsNode.classList.contains("open")) {
                settingsNode.blur();
            }
            return;
        }
        // Eintrag gewählt: Menü schliessen und Fokus ins geöffnete Fenster setzen
        settingsNode.classList.remove("open");
        // Das Suchfeld (Handy) behält den Fokus, sonst Fokus vom Menüeintrag nehmen
        var searchContainer = document.getElementById("searchFieldContainer");
        if (document.activeElement && !searchContainer.contains(document.activeElement)) {
            document.activeElement.blur();
        }
        modalIds.some(function (id) {
            var modal = document.getElementById(id);
            if (modal != null && modal.style.display != "none") {
                var focusTarget = modal.querySelector("input, textarea, .button, .closeButton");
                if (focusTarget) {
                    focusTarget.focus();
                }
                return true;
            }
            return false;
        });
    });

    document.addEventListener("click", function (event) {
        if (!settingsNode.contains(event.target)) {
            settingsNode.classList.remove("open");
        }
    });
}
