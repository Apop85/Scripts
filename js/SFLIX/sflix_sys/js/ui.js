// STEFFLIX - ui.js
// Bedienung: Fenster, Autoplay-Einstellungen, Seite "Einstellungen", Feedback und Lizenz, Hamburger-Menü, Tastatur
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
            } else if (idArray[key] == "settingsBox") {
                fillTransitionFields(document.getElementById("autoplayDuration"), document.getElementById("autoplayDurationScope"));
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

// Einstellungen speichern: Autoplay, Anzahl, (früher Dauer), Unbegrenzt.
// Die Dauer bis Übergang gilt seit 2.04 je Ordner (transitionByFolder), der dritte Wert bleibt 0.
function saveAutoplaySettings(newAutoplay, newAmount) {
    setData("settings", [newAutoplay, newAmount, 0, isAutoplayInfinite()].join(listSeperator));
}

// Dauer bis Übergang für das laufende Medium übernehmen (aus dem Ordner bzw. dessen Eltern)
function refreshTransitionForMedia(mediaPath) {
    autoplayDuration = getTransitionFor(mediaPath).seconds;
    document.getElementById("autoplayDuration").value = autoplayDuration;
}

// Füllt ein Feld "Dauer bis Übergang" und die Auswahl "Gilt für" für das laufende Medium.
// scope: gewählte Ebene ("" = alle Ordner), sonst die Vorauswahl
function fillTransitionFields(durationInput, scopeSelect, scope) {
    var mediaPath = currentMediaLocation;
    if (scope == null) {
        scope = getDefaultTransitionScope(mediaPath);
    }
    scopeSelect.replaceChildren();
    getTransitionScopes(mediaPath).forEach(function (entry) {
        var option = document.createElement("option");
        option.value = entry.folder;
        option.textContent = entry.label;
        scopeSelect.appendChild(option);
    });
    scopeSelect.value = scope;
    durationInput.value = getTransitionValueAt(scope, mediaPath);
}

// Wert, der auf einer Ebene gilt: der eigene, sonst der geerbte
function getTransitionValueAt(scope, mediaPath) {
    var map = getTransitionMap();
    if (map.hasOwnProperty(scope)) {
        return map[scope];
    }
    var parts = scope === "" ? [] : scope.split("/");
    for (var depth = parts.length - 1; depth >= 0; depth--) {
        var key = parts.slice(0, depth).join("/");
        if (map.hasOwnProperty(key)) {
            return map[key];
        }
    }
    return 0;
}

// Speichert die Eingabe für die gewählte Ebene und übernimmt sie für das laufende Medium
function saveTransitionFromFields(durationInput, scopeSelect) {
    var seconds = Math.min(transitionMax, Math.max(0, parseInt(durationInput.value, 10) || 0));
    durationInput.value = seconds;
    setTransition(scopeSelect.value, seconds);
    refreshTransitionForMedia(currentMediaLocation);
    restartImageAutoplay();
}

// Bilder: läuft Autoplay schon, mit der neuen Dauer neu starten
function restartImageAutoplay() {
    var player = document.getElementById("video");
    var nextNode = document.getElementById("next");
    if (!autoplay || player == null || player.tagName != "IMG") {
        return;
    }
    timeoutArray = cancelTimeouts();
    nextNode.style.animation = "none";
    void nextNode.offsetWidth;
    nextNode.style.animation = "autoplayLoading " + (autoplayDuration + minTime) + "s";
    timeoutArray.push(setTimeout(function () {
        clickNavigation("next");
    }, (autoplayDuration + minTime) * 1000));
}

// Anzahl ist bei "Unbegrenzt" ohne Bedeutung und wird ausgegraut
function updateAutoplayAmountState() {
    var amountField = document.getElementById("amountOfAutoplay");
    amountField.disabled = isAutoplayInfinite();
    amountField.closest(".settingRow").classList.toggle("disabled", amountField.disabled);
}

// Speichern der Autoplay-Einstellungen (Button "Speichern" im Autoplay-Fenster)
function applySettings() {
    saveTransitionFromFields(document.getElementById("autoplayDuration"), document.getElementById("autoplayDurationScope"));
    commitAutoplaySettings();
    toggleContainer(["settingsBox"], "none");
}

// ===================================================================
// Seite "Einstellungen": jede Änderung wird sofort gespeichert und angewendet
// ===================================================================

// Überträgt die gespeicherten Werte in die Felder der Seite
function fillSettingsFields(page) {
    page.querySelectorAll("input[data-setting]").forEach(function (input) {
        var value = getSetting(input.dataset.setting);
        if (input.type == "checkbox") {
            input.checked = value;
        } else {
            input.value = value;
        }
    });
    page.querySelectorAll(".settingChoice[data-setting]").forEach(function (group) {
        var value = getSetting(group.dataset.setting);
        group.querySelectorAll("button[data-value]").forEach(function (button) {
            var active = button.dataset.value === value;
            button.classList.toggle("active", active);
            button.setAttribute("aria-checked", active ? "true" : "false");
        });
    });
    // Unteroptionen (z.B. "Bilder ausnehmen") nur bei eingeschalteter Hauptoption
    page.querySelectorAll("[data-depends]").forEach(function (row) {
        row.style.display = getSetting(row.dataset.depends) ? "" : "none";
    });
    // Gegenteil: ausgeblendet, solange die Option eingeschaltet ist (z.B. "Kacheln pro Zeile")
    page.querySelectorAll("[data-hides]").forEach(function (row) {
        row.style.display = getSetting(row.dataset.hides) ? "none" : "";
    });
    document.getElementById("nativeControlsCheckBox").checked = useNativeControls();
}

// Vorschau unter "Darstellung": einige Kacheln, damit Grösse und Markierungen sofort sichtbar sind.
// Bevorzugt Videos mit Vorschaubild, gesehene zuerst (zeigen die Markierung).
function renderSettingsPreview() {
    var container = document.getElementById("settingsPreviewRow");
    container.replaceChildren();
    var paths = collectMediaPaths(data, []);
    var videos = paths.filter(isVideo);
    var candidates = (videos.length > 0 ? videos : paths).slice(0, 400).map(function (path) {
        return { path: path, score: (isSeen(path) ? 2 : 0) + (getInheritedPreview(path) != null ? 1 : 0) };
    }).sort(function (a, b) {
        return b.score - a.score;
    }).map(function (entry) {
        return entry.path;
    });
    if (candidates.length == 0) {
        container.closest(".settingsPreview").style.display = "none";
        return;
    }
    var track = createCardRow(container, "Vorschau", null);
    var section = track.closest(".cardRow");
    section.classList.add("untitledRow");
    section.querySelector(".rowHeader").remove();
    candidates.slice(0, 12).forEach(function (path) {
        createMediaCard(track, path, buildStartUrl(buildMediaData(path)), {
            subtitle: getMediaTypeLabel(path),
            inheritPreview: true,
            progress: getMediaProgress(path)
        });
    });
    refreshRowArrows(container);
}

function showSettingsStatus(text) {
    var status = document.getElementById("settingsStatus");
    status.className = "backupInfo";
    status.textContent = text;
}

// Einstellungen › Android-App: In der App selbst (window.stefflixApp, gesetzt von der App)
// dient der Knopf zum Aktualisieren; die APK öffnet sich dort im System-Browser.
function initAndroidSection() {
    if (!window.stefflixApp) {
        return;
    }
    var text = document.getElementById("androidDownloadText");
    var hint = document.getElementById("androidHint");
    if (text != null) {
        text.textContent = "Neueste Version herunterladen";
    }
    if (hint != null) {
        hint.textContent = "Du nutzt STEFFLIX bereits in der App. Für ein Update die neueste APK herunterladen und über die bestehende App installieren. Freigaben und gespeicherte Daten bleiben erhalten.";
    }
}

function initSettingsPage() {
    var page = document.getElementById("settingsPage");
    var afterChange = function (name) {
        fillSettingsFields(page);
        if (["cardSize", "showMarkers"].includes(name)) {
            renderSettingsPreview();
        } else {
            refreshRowArrows(page);
        }
    };

    page.querySelectorAll("input[data-setting]").forEach(function (input) {
        var name = input.dataset.setting;
        input.addEventListener("change", function () {
            if (input.type == "checkbox") {
                setSetting(name, input.checked);
            } else {
                // Ausserhalb des Bereichs wird auf die Grenze gesetzt
                input.value = setSetting(name, input.value);
            }
            afterChange(name);
        });
    });
    page.querySelectorAll(".settingChoice[data-setting]").forEach(function (group) {
        group.querySelectorAll("button[data-value]").forEach(function (button) {
            button.addEventListener("click", function () {
                setSetting(group.dataset.setting, button.dataset.value);
                afterChange(group.dataset.setting);
            });
        });
    });
    document.getElementById("nativeControlsCheckBox").addEventListener("change", function (event) {
        setNativeControls(event.target.checked);
    });
    document.getElementById("resetSettingsButton").addEventListener("click", function () {
        resetDisplaySettings();
        setNativeControls(false);
        fillSettingsFields(page);
        renderSettingsPreview();
        showSettingsStatus("Alle Einstellungen wurden auf den Standard zurückgesetzt.");
    });

    fillSettingsFields(page);
    renderSettingsPreview();
    initTransitionOverview();
    initLicenseSection();
    initAndroidSection();
    initDataCheck();
    initDiagnosis();
}

// ===================================================================
// Einstellungen › Wiedergabe: Dauer bis Übergang je Ordner
// ===================================================================

// Alle Ordner des Datenbaums als "Serien/Show" (für die Auswahl beim Hinzufügen)
function collectFolderPaths(node, prefix, result) {
    if (node == null || typeof node !== "object") {
        return result;
    }
    Object.keys(node).forEach(function (key) {
        if (key == "Preview" || isMediaFile(allowedMediaExtensions, key) || !hasMedia(node[key], true)) {
            return;
        }
        var path = prefix ? prefix + "/" + key : key;
        result.push(path);
        collectFolderPaths(node[key], path, result);
    });
    return result;
}

function folderExists(folder) {
    return folder === "" || getDataNode(folder.split("/")) != null;
}

function showTransitionStatus(text) {
    var status = document.getElementById("transitionStatus");
    status.className = text ? "backupInfo" : "";
    status.textContent = text || "";
}

// Feld für Intro bzw. Abspann eines Ordners. Leer = kein eigener Wert (der Ordner erbt).
function createFolderValueInput(kindName, folder, map, title) {
    var input = document.createElement("input");
    input.type = "number";
    input.min = "0";
    input.max = String(transitionMax);
    input.placeholder = "–";
    input.value = map.hasOwnProperty(folder) ? map[folder] : "";
    input.setAttribute("aria-label", title + " in Sekunden für " + getTransitionLabel(folder));
    input.title = title + " (leer = vom Ordner darüber übernehmen)";
    input.addEventListener("change", function () {
        if (input.value.trim() === "") {
            setFolderValue(kindName, folder, null);
            showTransitionStatus(getTransitionLabel(folder) + ": " + title + " entfernt.");
            return;
        }
        setFolderValue(kindName, folder, input.value);
        input.value = getFolderValueMap(kindName)[folder];
        showTransitionStatus(getTransitionLabel(folder) + ": " + title + " " + input.value + " Sekunden gespeichert.");
    });
    return input;
}

// Übersicht je Ordner: Intro überspringen (nur Videos) und Dauer bis Übergang (Abspann)
function renderTransitionOverview() {
    var transitions = getTransitionMap();
    var intros = getFolderValueMap("intro");
    var folders = Object.keys(transitions).concat(Object.keys(intros).filter(function (folder) {
        return !transitions.hasOwnProperty(folder);
    })).sort(function (a, b) {
        return a === "" ? -1 : (b === "" ? 1 : compareSortNames(a, b));
    });
    document.getElementById("transitionCount").textContent = folders.length;
    var list = document.getElementById("transitionList");
    list.replaceChildren();
    if (folders.length == 0) {
        var empty = document.createElement("p");
        empty.className = "transitionEmpty";
        empty.textContent = "Noch keine Einträge. Überall gilt 0 Sekunden.";
        list.appendChild(empty);
        return;
    }
    var header = document.createElement("div");
    header.className = "transitionRow transitionHeader";
    ["Ordner", "Intro", "Abspann", ""].forEach(function (text) {
        var cell = document.createElement("span");
        cell.textContent = text;
        header.appendChild(cell);
    });
    list.appendChild(header);
    folders.forEach(function (folder) {
        var row = document.createElement("div");
        row.className = "transitionRow";
        var name = document.createElement("span");
        name.className = "transitionFolder";
        name.textContent = getTransitionLabel(folder);
        if (!folderExists(folder)) {
            row.classList.add("missing");
            var hint = document.createElement("small");
            hint.textContent = "Ordner nicht gefunden";
            name.appendChild(hint);
        }
        var introInput = createFolderValueInput("intro", folder, intros, "Intro überspringen");
        var transitionInput = createFolderValueInput("transition", folder, transitions, "Dauer bis Übergang");
        var remove = document.createElement("button");
        remove.type = "button";
        remove.className = "sflixButton ghost small transitionRemove";
        remove.setAttribute("aria-label", "Einträge für " + getTransitionLabel(folder) + " entfernen");
        remove.title = "Entfernen (der Ordner erbt dann wieder)";
        remove.appendChild(createIcon("trash"));
        remove.addEventListener("click", function () {
            setTransition(folder, null);
            setFolderValue("intro", folder, null);
            renderTransitionOverview();
            showTransitionStatus(getTransitionLabel(folder) + " entfernt.");
        });
        row.append(name, introInput, transitionInput, remove);
        list.appendChild(row);
    });
}

function initTransitionOverview() {
    var datalist = document.getElementById("transitionFolderList");
    var labels = {};
    var allOption = document.createElement("option");
    allOption.value = getTransitionLabel("");
    datalist.appendChild(allOption);
    labels[allOption.value] = "";
    collectFolderPaths(data, "", []).forEach(function (folder) {
        var option = document.createElement("option");
        option.value = getTransitionLabel(folder);
        labels[option.value] = folder;
        datalist.appendChild(option);
    });

    var folderInput = document.getElementById("transitionAddFolder");
    var introInput = document.getElementById("transitionAddIntro");
    var secondsInput = document.getElementById("transitionAddSeconds");
    document.getElementById("transitionAddButton").addEventListener("click", function () {
        var label = folderInput.value.trim().replace(/\s*[›>\/\\]\s*/g, " › ");
        if (!labels.hasOwnProperty(label)) {
            showTransitionStatus("Bitte einen Ordner aus der Liste wählen.");
            folderInput.focus();
            return;
        }
        var parts = [];
        if (introInput.value.trim() !== "") {
            setFolderValue("intro", labels[label], introInput.value);
            parts.push("Intro " + getFolderValueMap("intro")[labels[label]] + " s");
        }
        if (secondsInput.value.trim() !== "" || parts.length == 0) {
            setTransition(labels[label], secondsInput.value || 0);
            parts.push("Abspann " + getTransitionMap()[labels[label]] + " s");
        }
        folderInput.value = "";
        introInput.value = "";
        renderTransitionOverview();
        showTransitionStatus(label + ": " + parts.join(", ") + " gespeichert.");
    });
    renderTransitionOverview();
}

// ===================================================================
// Einstellungen › Lizenz
// ===================================================================

// Seit 2.5.0 nur Anzeige: Der Schlüssel wird im Updater eingegeben (sflix_sys/license.js).
function initLicenseSection() {
    var message = document.getElementById("licenseMsg");
    var state = getLicenseState();
    message.className = state.status === "active" ? "licenseActive" : (state.status === "invalid" ? "licenseError" : "");
    message.textContent = getLicenseStatusText();
}

// ===================================================================
// Einstellungen › Fehlerbehebung: Diagnose
// ===================================================================

function buildDiagnosisText() {
    var info = getUpdaterInfo();
    var paths = collectMediaPaths(data, []);
    var count = function (test) {
        return paths.filter(test).length;
    };
    var lines = [
        "STEFFLIX " + getVersionName(),
        "STEFFLIX-ID: " + getInstallIdText(),
        "Lizenz: " + getLicenseStatusText(),
        "Updater: " + (info && info.updater ? info.updater : "unbekannt"),
        "ffmpeg: " + (info ? (info.ffmpeg || "nicht installiert") : "unbekannt"),
        "Browser: " + detectBrowser() + " (" + navigator.userAgent + ")",
        "Speicher: " + (getDatabaseName() === sharedDatabaseName ? "gemeinsam (keine install.js)" : "eigener Speicher dieser Installation") + ", " + getAllDataKeys().length + " Einträge",
        "Medien: " + paths.length + " (Videos " + count(isVideo) + ", Musik " + count(isMusic) + ", Bilder " + count(isImage) + ")",
        "Kategorien: " + Object.keys(data || {}).filter(function (key) { return key != "Preview"; }).length,
        "Bildschirm: " + window.innerWidth + "x" + window.innerHeight + ", Pixelverhältnis " + (window.devicePixelRatio || 1),
        "Einstellungen: " + JSON.stringify(getDisplaySettings())
    ];
    return lines.join("\n");
}

function initDiagnosis() {
    var node = document.getElementById("diagnosisText");
    node.textContent = buildDiagnosisText();
    var button = document.getElementById("copyDiagnosisButton");
    button.prepend(createIcon("copy"));
    button.setAttribute("data-copy", node.textContent);
    button.addEventListener("click", function () {
        button.setAttribute("data-copy", buildDiagnosisText());
        copyAddress(button);
    });
}

// Übernimmt die Autoplay-Felder aus dem Fenster "Einstellungen".
// Wird auch vom Zahnradmenü des Players verwendet (dort ohne Fenster).
function commitAutoplaySettings() {
    autoplay = document.getElementById("autoplayCheckBox").checked;
    autoplayAmount = parseInt(document.getElementById("amountOfAutoplay").value) || 0;

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

    saveAutoplaySettings(autoplay, autoplayAmount);
    updateOptionFields(autoplay, autoplayAmount);

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

// Funktion, um die Einstellungen anhand abgespielter Medien zu aktualisieren.
// Der dritte Wert (Dauer) wird nicht mehr verwendet, sie gilt je Ordner.
function updateOptions(newAutoplay, newAmount) {
    newAutoplay = newAutoplay === true || newAutoplay === "true";
    newAmount = parseInt(newAmount) || 0;

    // Verringere Anzahl Autoplay um 1 und deaktiviere Autoplay bei 0 (nicht bei "Unbegrenzt")
    if (!isAutoplayInfinite()) {
        newAmount = Math.max(0, newAmount - 1);
        if (newAutoplay && newAmount == 0) {
            newAutoplay = false;
        }
    }

    // Entferne Autoplay-Klasse
    if (!newAutoplay) {
        var mediaNode = document.getElementById("media");
        if (mediaNode.classList.contains("autoplaying")) {
            mediaNode.classList.remove("autoplaying");
            document.getElementById("next").style.animation = null;
        }
    }

    updateOptionFields(newAutoplay, newAmount);
    saveAutoplaySettings(newAutoplay, newAmount);
}

// Funktion, um die gespeicherten Einstellungen auf das Einstellungsmenü zu übertragen
function updateOptionFields(newAutoplay, newAmount) {
    document.getElementById("autoplayCheckBox").checked = newAutoplay;
    document.getElementById("amountOfAutoplay").value = newAmount;
    document.getElementById("autoplayDuration").value = autoplayDuration;
    // Autoplay-Anzeige und Zahnradmenü im Player nachführen
    updatePlayerAutoplayState();
}

function loadSettings() {
    var settings = getData("settings");

    if (settings == null) {
        autoplay = false;
        autoplayAmount = 0;
        document.getElementById("autoplayInfinite").checked = false;
        saveAutoplaySettings(autoplay, autoplayAmount);
    } else {
        settings = settings.split(listSeperator);
        autoplay = settings[0] != "false";
        autoplayAmount = parseInt(settings[1]) || 0;
        // Der dritte Wert (frühere Dauer bis Übergang für alles) wird seit 2.04 nicht mehr gelesen:
        // die Dauer gilt je Ordner und ist ohne Eintrag 0.
        // Vierter Wert ab 2.00 (ältere Einstellungen haben ihn nicht)
        document.getElementById("autoplayInfinite").checked = settings[3] === "true";
        updateOptionFields(autoplay, autoplayAmount);
    }
    // Bis ein Medium geladen ist, gilt der Wert für alle Ordner
    refreshTransitionForMedia(null);
    updateAutoplayAmountState();
    document.getElementById("nativeControlsCheckBox").checked = useNativeControls();
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
// Seit 2.5.0: Der Lizenzschlüssel (SFX2-…) wird im Updater eingegeben und steht in
// sflix_sys/license.js: var licenseFile = { key: "SFX2-…", confirmedFor: "<installId>" };
// Gleiches Format wie "SFLIX Tools\Lizenz\create_license.py" und License in Core.cs des Updaters.
// ===================================================================

var licensePrefix = "SFX2";

// Fester Schlüsselstrom (xorshift32) zum Verschleiern des Inhalts
function getLicenseKeystream(length) {
    var state = 0x5f3759df;
    var bytes = [];
    for (var index = 0; index < length; index++) {
        state ^= state << 13;
        state >>>= 0;
        state ^= state >>> 17;
        state ^= state << 5;
        state >>>= 0;
        bytes.push(state & 0xff);
    }
    return bytes;
}

// CRC-16/CCITT-FALSE über die Bytes
function getLicenseCrc(bytes) {
    var crc = 0xffff;
    bytes.forEach(function (value) {
        crc ^= value << 8;
        for (var bit = 0; bit < 8; bit++) {
            crc = (crc & 0x8000) ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff;
        }
    });
    return crc;
}

// Entschlüsselt einen Schlüssel. Ergebnis: { ok: true, webhookId, webhookToken } oder
// { ok: false, reason: "empty" | "old" | "format" | "checksum" }
function decodeLicenseKey(key) {
    var text = String(key || "").replace(/\s+/g, "");
    if (text === "") {
        return { ok: false, reason: "empty" };
    }
    if (text.toUpperCase().indexOf(licensePrefix) !== 0) {
        return { ok: false, reason: "old" };
    }
    var body = text.slice(licensePrefix.length).replace(/-/g, "");
    if (!/^[A-Za-z0-9+\/]+$/.test(body)) {
        return { ok: false, reason: "format" };
    }
    var binary;
    try {
        binary = atob(body + "===".slice((body.length + 3) % 4));
    } catch (error) {
        return { ok: false, reason: "format" };
    }
    if (binary.length !== 61) {
        return { ok: false, reason: "format" };
    }
    var stream = getLicenseKeystream(binary.length);
    var bytes = [];
    for (var index = 0; index < binary.length; index++) {
        bytes.push(binary.charCodeAt(index) ^ stream[index]);
    }
    var crc = (bytes[59] << 8) | bytes[60];
    if (getLicenseCrc(bytes.slice(0, 59)) !== crc) {
        return { ok: false, reason: "checksum" };
    }
    // Webhook-ID: 8 Bytes (big endian) als Dezimalzahl
    var id = BigInt(0);
    for (index = 0; index < 8; index++) {
        id = id * BigInt(256) + BigInt(bytes[index]);
    }
    // Token: 51 Bytes als Base64url ohne Auffüllzeichen (68 Zeichen)
    var tokenBinary = String.fromCharCode.apply(null, bytes.slice(8, 59));
    var token = btoa(tokenBinary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    return { ok: true, webhookId: id.toString(), webhookToken: token };
}

function getInstallIdText() {
    return typeof installId === "string" && /^[0-9a-f]{32}$/.test(installId) ? installId : "ohne Kennung";
}

// Zustand der Lizenz aus license.js:
//   status "none" (kein Schlüssel), "invalid" (Schlüssel unlesbar), "pending" (noch nicht bestätigt),
//   "active" (bestätigt, Feedback möglich). Bei "active"/"pending" zusätzlich webhookUrl.
function getLicenseState() {
    var file = typeof licenseFile !== "undefined" && licenseFile != null && typeof licenseFile === "object" ? licenseFile : null;
    if (file == null || !file.key) {
        return { status: "none" };
    }
    var decoded = decodeLicenseKey(file.key);
    if (!decoded.ok) {
        return { status: "invalid" };
    }
    var url = "https://discord.com/api/webhooks/" + decoded.webhookId + "/" + decoded.webhookToken;
    // Bestätigt für genau diese Installation (eine Kopie des Ordners braucht eine eigene Bestätigung)
    var confirmed = file.confirmedFor != null && file.confirmedFor === getInstallIdText();
    return { status: confirmed ? "active" : "pending", webhookUrl: url };
}

function getLicenseStatusText() {
    var status = getLicenseState().status;
    if (status === "active") {
        return "Lizenz aktiv (im Updater hinterlegt). Das Feedbackformular ist im Menü verfügbar.";
    } else if (status === "pending") {
        return "Lizenzschlüssel hinterlegt, die Bestätigung steht noch aus. Den Updater mit Internetverbindung starten.";
    } else if (status === "invalid") {
        return "Der hinterlegte Lizenzschlüssel ist ungültig. Im Updater unter \"Lizenz\" einen gültigen Schlüssel eingeben.";
    }
    return "Kein Lizenzschlüssel hinterlegt. Den Schlüssel im Updater unter \"Lizenz\" eingeben.";
}

// Meldung im Feedback-Fenster (type: "error", "success" oder "info")
function showFeedbackStatus(text, type) {
    var statusNode = document.getElementById("feedbackStatus");
    statusNode.textContent = text;
    statusNode.className = type;
}

// Verständliche Meldung zu einer Antwort von Discord
function describeFeedbackError(status) {
    if (status === 401 || status === 403 || status === 404) {
        return "Der Lizenzschlüssel ist nicht mehr gültig. Bitte einen neuen Schlüssel anfordern und im Updater eingeben.";
    } else if (status === 429) {
        return "Es wurden gerade zu viele Nachrichten verschickt. Bitte in einer Minute erneut versuchen.";
    } else if (status === 400 || status === 413) {
        return "Die Nachricht wurde abgelehnt (zu lang oder ungültig). Bitte kürzen und erneut versuchen.";
    }
    return "Die Nachricht konnte nicht verschickt werden (Fehler " + status + "). Bitte später erneut versuchen.";
}

// Feedback senden: Text als Nachricht, Diagnose (und überlange Nachrichten) als Anhang
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

    var license = getLicenseState();
    if (license.status !== "active") {
        showFeedbackStatus(getLicenseStatusText() + " Die Nachricht wurde nicht verschickt.", "error");
        return;
    }

    var header = "STEFFLIX-ID: " + getInstallIdText() + " · STEFFLIX " + getVersionName() + " · " + detectBrowser() + "\n";
    var text = header + "**Absender:** " + sender + "\n**Nachricht:**\n" + message;
    var form = new FormData();
    var fileIndex = 0;
    // Discord nimmt höchstens 2000 Zeichen, längere Nachrichten zusätzlich als Datei
    if (text.length > 1900) {
        form.append("files[" + fileIndex++ + "]", new Blob([sender + ":\n" + message], { type: "text/plain;charset=utf-8" }), "nachricht.txt");
        text = text.slice(0, 1800) + "\n… (vollständig in nachricht.txt)";
    }
    if (document.getElementById("feedbackDiagnosis").checked) {
        form.append("files[" + fileIndex++ + "]", new Blob([buildDiagnosisText()], { type: "text/plain;charset=utf-8" }), "diagnose.txt");
    }
    // Keine Erwähnungen (@everyone, @Name) auslösen
    form.append("payload_json", JSON.stringify({ content: text, allowed_mentions: { parse: [] } }));

    showFeedbackStatus("Nachricht wird gesendet...", "info");
    fetch(license.webhookUrl + "?wait=true", { method: "POST", body: form })
        .then(function (response) {
            if (!response.ok) {
                showFeedbackStatus(describeFeedbackError(response.status), "error");
                return;
            }
            showFeedbackStatus("Danke! Die Nachricht wurde verschickt.", "success");
            document.getElementById("feedbackText").value = "";
        })
        .catch(function () {
            showFeedbackStatus("Die Nachricht konnte nicht verschickt werden. Bitte Internetverbindung prüfen und später erneut versuchen.", "error");
        });
}

// Feedback wird nur angeboten, wenn ein bestätigter Lizenzschlüssel hinterlegt ist
function updateFeedbackVisibility() {
    var hasLicense = getLicenseState().status === "active";
    document.getElementById("feedbackBoxToggler").style.display = hasLicense ? "" : "none";
    if (!hasLicense) {
        document.getElementById("feedbackWrapper").style.display = "none";
    }
    // Ein alter Schlüssel (bis 2.04 im Browser gespeichert) wird mit einem neuen nicht mehr gebraucht
    if (hasLicense && getData("serial") != null) {
        removeData("serial");
    }
}

// Hinweis für bisherige Lizenznehmer: Bis 2.04 stand der Schlüssel im Browser ("serial").
// Er gilt nicht mehr. Der Hinweis erscheint, bis er ausgeblendet oder ein neuer Schlüssel hinterlegt wird.
function showLicenseChangeNotice(container) {
    if (getData("serial") == null || getLicenseState().status === "active") {
        return;
    }
    var banner = document.createElement("section");
    banner.className = "updateBanner";
    banner.setAttribute("aria-label", "Hinweis zur Lizenz");
    var icon = document.createElement("div");
    icon.className = "updateIcon";
    icon.appendChild(createIcon("sliders"));
    var content = document.createElement("div");
    content.className = "updateContent";
    var heading = document.createElement("h2");
    heading.textContent = "Die Lizenzierung wurde geändert";
    var meta = document.createElement("p");
    meta.className = "updateMeta";
    meta.textContent = "Dein bisheriger Lizenzschlüssel ist nicht mehr gültig. Bitte einen neuen Schlüssel anfordern und im Updater (updateStefflix.exe) unter \"Lizenz\" eingeben, damit das Feedback wieder verfügbar ist.";
    var actions = document.createElement("div");
    actions.className = "updateActions";
    var more = document.createElement("a");
    more.className = "sflixButton primary";
    more.href = buildStartUrl("SETTINGS", false) + "#lizenz";
    more.textContent = "Mehr erfahren";
    var dismiss = document.createElement("button");
    dismiss.type = "button";
    dismiss.className = "sflixButton ghost";
    dismiss.textContent = "Ausblenden";
    dismiss.addEventListener("click", function () {
        // Der alte Schlüssel ist ohne Nutzen: mit dem Hinweis löschen
        removeData("serial");
        banner.classList.add("hiding");
        setTimeout(function () {
            banner.remove();
        }, 300);
    });
    actions.append(more, dismiss);
    content.append(heading, meta, actions);
    banner.append(icon, content);
    container.insertBefore(banner, container.firstChild);
}

var androidIconPath = "M17.6 9.48l1.84-3.18c.16-.31.04-.69-.26-.85-.29-.15-.65-.06-.83.22l-1.88 3.24c-2.86-1.21-6.08-1.21-8.94 0L5.65 5.67c-.19-.29-.58-.38-.87-.2-.28.18-.37.54-.22.83L6.4 9.48C3.3 11.25 1.28 14.44 1 18h22c-.28-3.56-2.3-6.75-5.4-8.52zM7 15.25c-.69 0-1.25-.56-1.25-1.25s.56-1.25 1.25-1.25 1.25.56 1.25 1.25-.56 1.25-1.25 1.25zm10 0c-.69 0-1.25-.56-1.25-1.25s.56-1.25 1.25-1.25 1.25.56 1.25 1.25-.56 1.25-1.25 1.25z";

// Ankündigung der Android-App (seit 2.5.1): erscheint einmal auf der Startseite und ist
// ab dann als gesehen markiert ("androidAppAnnounced"). In der App selbst nicht nötig.
function showAndroidAppNotice(container) {
    if (getData("androidAppAnnounced") != null) {
        return;
    }
    setData("androidAppAnnounced", getVersionName());
    if (window.stefflixApp) {
        return;
    }
    var banner = document.createElement("section");
    banner.className = "updateBanner appNotice";
    banner.setAttribute("aria-label", "Neu: Android-App");
    var icon = document.createElement("div");
    icon.className = "updateIcon";
    icon.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="' + androidIconPath + '"/></svg>';
    var content = document.createElement("div");
    content.className = "updateContent";
    var heading = document.createElement("h2");
    heading.textContent = "Neu: STEFFLIX für Android";
    var meta = document.createElement("p");
    meta.className = "updateMeta";
    meta.textContent = "STEFFLIX gibt es jetzt auch als App für Handy und Tablet: bildschirmfüllend, ohne Adresszeile und direkt mit dem STEFFLIX-Ordner auf einer Netzwerkfreigabe (z.B. NAS) verbunden.";
    var actions = document.createElement("div");
    actions.className = "updateActions";
    var more = document.createElement("a");
    more.className = "sflixButton primary";
    more.href = buildStartUrl("SETTINGS", false) + "#android";
    more.textContent = "App ansehen";
    var dismiss = document.createElement("button");
    dismiss.type = "button";
    dismiss.className = "sflixButton ghost";
    dismiss.textContent = "Ausblenden";
    dismiss.addEventListener("click", function () {
        banner.classList.add("hiding");
        setTimeout(function () {
            banner.remove();
        }, 300);
    });
    actions.append(more, dismiss);
    content.append(heading, meta, actions);
    banner.append(icon, content);
    container.appendChild(banner);
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

// Hilfe: Adresse einer Einstellungsseite kopieren. Browser lassen about:, chrome:// und edge://
// nicht per Link aus einer Seite öffnen, deshalb wird die Adresse zum Einfügen kopiert.
function copyAddress(button) {
    var text = button.getAttribute("data-copy") || button.textContent;
    var done = function (ok) {
        button.classList.remove("copied", "copyFailed");
        button.classList.add(ok ? "copied" : "copyFailed");
        clearTimeout(button.copyTimer);
        button.copyTimer = setTimeout(function () { button.classList.remove("copied", "copyFailed"); }, 2500);
    };
    var fallback = function () {
        var field = document.createElement("textarea");
        field.value = text;
        field.setAttribute("readonly", "");
        field.style.position = "fixed";
        field.style.opacity = "0";
        document.body.appendChild(field);
        field.select();
        var ok = false;
        try { ok = document.execCommand("copy"); } catch (e) { ok = false; }
        document.body.removeChild(field);
        done(ok);
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(function () { done(true); }, fallback);
    } else {
        fallback();
    }
}

// Blendet den Ladescreen aus (onload von body). Vorschaubilder laden "lazy" und zählen
// nicht zum onload: darum warten, bis die Bilder im sichtbaren Bereich da sind (höchstens 8 s).
function setHeight() {
    var screenNode = document.getElementById("loadingScreen");
    var finished = false;
    var hide = function () {
        if (!finished) {
            finished = true;
            screenNode.style.display = "none";
        }
    };
    var pending = getVisibleLoadingImages();
    if (pending.length === 0) {
        hide();
        return;
    }
    var open = pending.length;
    pending.forEach(function (image) {
        var one = function () {
            image.removeEventListener("load", one);
            image.removeEventListener("error", one);
            open--;
            if (open <= 0) {
                hide();
            }
        };
        image.addEventListener("load", one);
        image.addEventListener("error", one);
        // sichtbare Bilder sofort laden, nicht erst nach dem Ausblenden
        if (image.loading === "lazy") {
            image.loading = "eager";
        }
    });
    setTimeout(hide, 8000);
}

// Noch nicht geladene Bilder, die im Fenster sichtbar sind (auch in Bändern nur der sichtbare Teil)
function getVisibleLoadingImages() {
    var width = window.innerWidth;
    var height = window.innerHeight;
    return Array.prototype.filter.call(document.images, function (image) {
        if (image.complete || !image.getAttribute("src")) {
            return false;
        }
        var rect = image.getBoundingClientRect();
        return rect.width > 0 && rect.height > 0 && rect.bottom > 0 && rect.right > 0
            && rect.top < height && rect.left < width;
    });
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
    // Rasteransicht ("Alle anzeigen") zuerst schliessen
    if (closeCardGrid()) {
        return;
    }
    var closedSomething = false;
    modalIds.concat(["searchFieldContainer"]).forEach(function (id) {
        if (closeContainer(id)) {
            closedSomething = true;
        }
    });
    document.getElementById("settings").classList.remove("open");
    if (!closedSomething && closePlayerMenu()) {
        closedSomething = true;
    }
    if (!closedSomething && stopDashboardEditing()) {
        closedSomething = true;
    }
    // Video-Vollbild ohne Browser-Vollbild (z.B. iPhone); sonst beendet der Browser selbst
    if (!closedSomething && isCinemaMode()) {
        exitCinemaMode();
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
    // Hat der Player selbst den Fokus (nur mit Browser-Steuerung möglich),
    // übernimmt der Browser die Pfeiltasten und die Leertaste
    var targetIsPlayer = event.target === player;
    // Sichtbarer Bereich des Players (das Audio-Element selbst ist unsichtbar)
    var playerArea = player.closest(".sflixPlayer") || player;
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
            revealPlayerControls();
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
                revealPlayerControls();
            } else {
                // Bilder: vorheriges/nächstes Bild
                clickNavigation(direction > 0 ? "next" : "prev");
            }
            break;
        case "ArrowUp":
        case "ArrowDown":
            // Nur wenn der Player sichtbar ist, sonst normal scrollen
            if (!isPlayable || targetIsPlayer || !isElementVisible(playerArea)) {
                return;
            }
            event.preventDefault();
            var step = event.key === "ArrowUp" ? 0.1 : -0.1;
            player.volume = Math.round(Math.min(1, Math.max(0, player.volume + step)) * 10) / 10;
            if (player.volume > 0) {
                player.muted = false;
            }
            revealPlayerControls();
            break;
        case "f":
        case "F":
            event.preventDefault();
            if (player.tagName == "VIDEO") {
                // Vollbild per CSS (unter dem Video kann weitergescrollt werden)
                toggleCinemaMode();
            } else if (player.tagName == "IMG") {
                toggleFullscreenMode();
            }
            break;
        case "m":
        case "M":
            if (isPlayable) {
                event.preventDefault();
                player.muted = !player.muted;
                revealPlayerControls();
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
