// STEFFLIX - cards.js
// Kacheln und Menübänder (Scrollen, Ziehen, Pfeiltasten)
// Ladereihenfolge (start.html): core.js, storage.js, cards.js, pages.js, player.js, ui.js, main.js. Alle Funktionen sind global;
// beim Laden wird nur in main.js Code ausgeführt.

function refreshRowArrows(container) {
    requestAnimationFrame(function () {
        container.querySelectorAll(".rowTrack").forEach(function (track) {
            track.updateArrows();
        });
    });
}

function createRowCount(count) {
    var countNode = document.createElement("span");
    countNode.className = "rowCount";
    countNode.textContent = count;
    return countNode;
}

// ===================================================================
// Kacheln (Startseite, Suche, Favoriten)
// ===================================================================

function hashString(text) {
    var hash = 0;
    for (var index = 0; index < text.length; index++) {
        hash = (hash * 31 + text.charCodeAt(index)) | 0;
    }
    return Math.abs(hash);
}

// Symbol für einen Ordner anhand des ersten enthaltenen Mediums
function getFolderIconName(node) {
    if (node == null || typeof node !== "object") {
        return "folder";
    }
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            return getMediaIconName(key);
        }
    }
    for (var folder in node) {
        if (node.hasOwnProperty(folder) && folder != "Preview" && node[folder] != null && typeof node[folder] === "object") {
            var iconName = getFolderIconName(node[folder]);
            if (iconName != "folder") {
                return iconName;
            }
        }
    }
    return "folder";
}

// Fortschritt eines Mediums (0 bis 1) aus Zeitstempel und gespeicherter Dauer
function getMediaProgress(mediaPath) {
    var position = parseFloat(getData("timestamp-" + mediaPath));
    var duration = parseFloat(getData("timestampDuration-" + mediaPath));
    if (isNaN(position) || isNaN(duration) || duration <= 0) {
        return null;
    }
    return Math.min(1, Math.max(0, position / duration));
}

// Erstellt eine Kachel. Alle Kacheln sind gleich gross: Vorschau immer 16:9
// (Bild oder Farbverlauf mit Symbol), Titel max. 2 Zeilen, Untertitel 1 Zeile.
// options: href, title, subtitle, previewSrc, placeholderIcon, progress (0-1), seed
// Farbverlauf mit Symbol und Titel (ohne Vorschaubild oder wenn es nicht lädt)
function createCardPlaceholder(options) {
    var hue = hashString(options.seed || options.title) % 360;
    var placeholder = document.createElement("div");
    placeholder.className = "cardPlaceholder";
    placeholder.style.background = "linear-gradient(135deg, hsl(" + hue + ", 45%, 32%), hsl(" + ((hue + 50) % 360) + ", 55%, 13%))";
    var icon = document.createElement("span");
    icon.className = "cardPlaceholderIcon";
    icon.appendChild(createIcon(options.placeholderIcon || "video"));
    var label = document.createElement("span");
    label.className = "cardPlaceholderText";
    label.textContent = options.title;
    placeholder.appendChild(icon);
    placeholder.appendChild(label);
    return placeholder;
}

function createCard(container, options) {
    var card = document.createElement("a");
    card.className = "card";
    card.href = options.href;
    card.title = options.title;
    card.draggable = false;

    var mediaNode = document.createElement("div");
    mediaNode.className = "cardMedia";

    if (options.previewSrc) {
        var image = document.createElement("img");
        image.className = "cardImage";
        // "lazy" vor src setzen, sonst kann der Browser das Bild sofort laden
        image.loading = "lazy";
        image.decoding = "async";
        // Vorschaubild fehlt oder ist beschädigt: Platzhalter statt kaputtem Bild
        image.addEventListener("error", function () {
            if (image.parentNode === mediaNode) {
                mediaNode.replaceChild(createCardPlaceholder(options), image);
            }
        }, { once: true });
        var previewSrc = options.previewSrc;
        if (options.originalPreview) {
            if (window.StefflixApp) {
                // App: verkleinertes Foto von der App statt des Originals (SFLIX-20261125-148)
                previewSrc += (previewSrc.indexOf("?") < 0 ? "?" : "&") + "sflixThumb=640";
            } else {
                image.dataset.original = "true";
            }
        }
        if (options.originalPreview && window.StefflixApp && window.IntersectionObserver) {
            // Erst laden, wenn die Kachel sichtbar wird, sichtbare zuerst (SFLIX-20261125-152)
            queueAppThumbnail(image, previewSrc);
        } else {
            image.src = previewSrc;
        }
        image.alt = "";
        image.draggable = false;
        mediaNode.appendChild(image);
    } else {
        mediaNode.appendChild(createCardPlaceholder(options));
    }

    if (options.progress != null) {
        var percent = Math.round(options.progress * 100);
        var progress = document.createElement("div");
        progress.className = "cardProgress";
        progress.setAttribute("role", "progressbar");
        progress.setAttribute("aria-valuenow", percent);
        progress.setAttribute("aria-valuemin", 0);
        progress.setAttribute("aria-valuemax", 100);
        progress.setAttribute("aria-label", percent + "% gesehen");
        var fill = document.createElement("div");
        fill.className = "cardProgressFill";
        fill.style.width = Math.max(percent, 2) + "%";
        progress.appendChild(fill);
        mediaNode.appendChild(progress);
    }

    var body = document.createElement("div");
    body.className = "cardBody";
    var titleNode = document.createElement("span");
    titleNode.className = "cardTitle";
    titleNode.textContent = options.title;
    var subtitleNode = document.createElement("span");
    subtitleNode.className = "cardSubtitle";
    subtitleNode.textContent = options.subtitle || "";
    body.appendChild(titleNode);
    body.appendChild(subtitleNode);

    card.appendChild(mediaNode);
    card.appendChild(body);
    container.appendChild(card);
    return card;
}

// Kachel für ein Medium (Suche, Favoriten, Weiterschauen, Startseite, Ordnerseiten)
// options.playData: Medium beim Klick ohne Seitenreload abspielen
function createMediaCard(container, mediaPath, href, options) {
    options = options || {};
    if (options.playData) {
        href = buildStartUrl(options.playData);
    }
    if (href == null) {
        return null;
    }

    var folders = getMediaFolders(mediaPath);
    // Fotos sind ihr eigenes Vorschaubild (ausser es gibt ein Vorschaubild mit gleichem Namen)
    // inheritPreview: ohne eigenes Bild das der Staffel bzw. Serie (nicht das der Kategorie)
    var previewSrc = isImage(mediaPath) ? (getPreviewForMedia(mediaPath, true) || mediaPath)
        : (options.inheritPreview ? getInheritedPreview(mediaPath) : getPreviewForMedia(mediaPath, options.exactPreview));

    // Markierungen ("Gesehen", "Letzte Folge") lassen sich in den Einstellungen ausblenden.
    // Gespeichert wird "Gesehen" trotzdem.
    var showMarkers = getSetting("showMarkers");
    // Gesehen, aber nochmals angefangen (Position unter seenShare): Fortschritt statt "Gesehen",
    // wie bei Weiterschauen (SFLIX-20261127-173)
    var restarted = options.progress != null && options.progress < seenShare;
    var seen = showMarkers && isVideo(mediaPath) && isSeen(mediaPath) && !restarted;
    var label = showMarkers ? options.label : null;
    var card = createCard(container, {
        href: href,
        title: options.title || getDisplayName(mediaPath),
        subtitle: options.subtitle != null ? options.subtitle : (folders.length > 1 ? folders.slice(1).join(" › ") : folders.join(" › ")),
        previewSrc: previewSrc,
        placeholderIcon: getMediaIconName(mediaPath),
        // Gesehene Videos zeigen einen Haken statt des Fortschrittsbalkens (ausser nochmals angefangen)
        progress: seen ? null : options.progress,
        seed: mediaPath,
        // Foto ohne Vorschaubild: die Kachel zeigt das Original (oft mehrere MB)
        originalPreview: previewSrc === mediaPath
    });
    card.dataset.media = mediaPath;
    setCardFavorite(card, getFavorites().includes(mediaPath));
    if (seen || label) {
        addCardLabel(card, label || "Gesehen", seen && !label);
    }
    if (options.playData) {
        setMediaLink(card, options.playData);
    }
    return card;
}

// Hinweis oben links auf der Kachel (z.B. "Gesehen" mit Haken oder "Letzte Folge")
function addCardLabel(card, text, withCheck) {
    var label = document.createElement("span");
    label.className = "cardLabel";
    if (withCheck) {
        label.appendChild(createIcon("check"));
    }
    label.appendChild(document.createTextNode(text));
    card.querySelector(".cardMedia").appendChild(label);
}

// Favoriten-Stern oben rechts auf der Kachel
function setCardFavorite(card, isFavorite) {
    var badge = card.querySelector(".cardBadge");
    if (isFavorite && badge == null) {
        badge = document.createElement("span");
        badge.className = "cardBadge";
        badge.appendChild(createIcon("starFilled"));
        badge.setAttribute("aria-label", "Favorit");
        card.querySelector(".cardMedia").appendChild(badge);
    } else if (!isFavorite && badge != null) {
        badge.remove();
    }
}

// Kachel für einen Ordner (z.B. eine Serie oder ein Album)
function createFolderCard(container, folderLevels, options) {
    options = options || {};
    var levelPath = buildLevelPath(folderLevels);
    var href = buildStartUrl(levelPath);
    if (href == null) {
        return null;
    }
    var node = getDataNode(folderLevels);
    // Ohne eigenes Vorschaubild: bei Bilderordnern das erste Bild, sonst ein Vorschaubild von weiter unten
    var previewSrc = resolvePreviewSrc(levelPath, "normal");
    var firstImage = previewSrc == null ? getFirstImageInFolder(node) : null;
    previewSrc = previewSrc || firstImage || getPreviewFromSubfolders(node, levelPath);
    var card = createCard(container, {
        href: href,
        title: folderLevels[folderLevels.length - 1],
        subtitle: options.subtitle != null ? options.subtitle : describeFolder(node),
        previewSrc: previewSrc,
        placeholderIcon: getFolderIconName(node),
        seed: levelPath,
        // Erstes Foto als Titelbild ist ein Original (oft mehrere MB): in der App verkleinert, sonst
        // nicht vorladen (SFLIX-20261125-154)
        originalPreview: firstImage != null && previewSrc === firstImage
    });
    if (getSetting("showMarkers") && isFolderSeen(node)) {
        addCardLabel(card, "Gesehen", true);
    }
    return card;
}

// Ein Ordner gilt als gesehen, wenn er Videos enthält (auch in Unterordnern) und alle
// davon gesehen sind. Musik und Bilder haben keine Gesehen-Markierung und zählen nicht.
function isFolderSeen(node) {
    var videos = collectMediaPaths(node, []).filter(isVideo);
    return videos.length > 0 && videos.every(isSeen);
}

// Erstes Bild eines Ordners (natürlich sortiert, "Bild 2" vor "Bild 10"). Liegen keine
// Bilder direkt im Ordner, wird in den Unterordnern gesucht (ebenfalls sortiert).
function getFirstImageInFolder(node) {
    if (node == null || typeof node !== "object") {
        return null;
    }
    var keys = Object.keys(node).filter(function (key) {
        return key != "Preview";
    });
    var images = sortNatural(keys.filter(function (key) {
        return isImage(key) && isMediaFile(allowedMediaExtensions, key);
    }));
    if (images.length > 0) {
        return normalizeMediaLocation(images[0]);
    }
    var folders = sortNatural(keys.filter(function (key) {
        return node[key] != null && typeof node[key] === "object";
    }));
    for (var index = 0; index < folders.length; index++) {
        var found = getFirstImageInFolder(node[folders[index]]);
        if (found != null) {
            return found;
        }
    }
    return null;
}

// Ordner ohne eigenes Vorschaubild (seit 2.6.2): sucht die Preview-Ordner darunter ohne Tiefenbegrenzung,
// die nächstgelegenen zuerst (Ebene für Ebene, beginnend mit dem eigenen Preview-Ordner). Aus der ersten
// Ebene mit Bildern wird eines gewählt, das für diesen Ordner immer dasselbe ist (aus seinem Pfad berechnet).
function getPreviewFromSubfolders(node, seed) {
    if (node == null || typeof node !== "object") {
        return null;
    }
    var level = [node];
    while (level.length > 0) {
        var found = [];
        var next = [];
        level.forEach(function (folder) {
            var preview = folder["Preview"];
            if (preview != null && typeof preview === "object") {
                Object.keys(preview).forEach(function (path) {
                    if (isImage(path)) {
                        found.push(getOriginalPreviewPath(path));
                    }
                });
            }
            Object.keys(folder).forEach(function (key) {
                if (key != "Preview" && folder[key] != null && typeof folder[key] === "object") {
                    next.push(folder[key]);
                }
            });
        });
        if (found.length > 0) {
            var pick = sortNatural(found)[hashString(seed) % found.length];
            return pick.startsWith(".") ? pick : "." + pick;
        }
        level = next;
    }
    return null;
}

// Kurzbeschreibung eines Ordners: "3 Ordner" oder "12 Medien"
function describeFolder(node) {
    var folders = 0;
    var media = 0;
    for (var key in node) {
        if (!node.hasOwnProperty(key) || key == "Preview") {
            continue;
        }
        if (isMediaFile(allowedMediaExtensions, key)) {
            media += 1;
        } else if (hasMedia(node[key], true)) {
            folders += 1;
        }
    }
    if (folders > 0) {
        return folders + " Ordner";
    }
    return media + (media == 1 ? " Medium" : " Medien");
}

function getMediaTypeLabel(path) {
    if (isVideo(path)) {
        return "Video";
    } else if (isMusic(path)) {
        return "Musik";
    } else if (isImage(path)) {
        return "Bild";
    }
    return "";
}

// ===================================================================
// Menübänder (horizontal scrollbar wie bei Streamingdiensten)
// ===================================================================

// Erstellt ein Menüband und gibt den Container für die Kacheln zurück
// rowKey (optional): eindeutiger Name des Bandes für die eigene Sortierung der Startseite
function createCardRow(parent, title, linkHref, rowKey) {
    var section = document.createElement("section");
    section.className = "cardRow";
    if (rowKey) {
        section.dataset.rowKey = rowKey;
    }

    var header = document.createElement("div");
    header.className = "rowHeader";
    var heading = document.createElement("h2");
    heading.textContent = title;
    header.appendChild(heading);
    if (linkHref) {
        // Es gibt eine Seite mit dem ganzen Inhalt (z.B. die Kategorie): dorthin
        var link = document.createElement("a");
        link.className = "rowLink";
        link.href = linkHref;
        link.textContent = "Alle anzeigen ›";
        header.appendChild(link);
    } else {
        // Sonst alle Kacheln des Bandes als Raster
        var gridButton = document.createElement("button");
        gridButton.type = "button";
        gridButton.className = "rowLink";
        gridButton.textContent = "Alle anzeigen ›";
        gridButton.addEventListener("click", function () {
            openCardGrid(section, title);
        });
        header.appendChild(gridButton);
    }

    section.appendChild(header);
    parent.appendChild(section);
    return addRowTrack(section);
}

// Fügt einem Band eine (weitere) Zeile hinzu und gibt deren Container für die Kacheln zurück
function addRowTrack(section) {
    var viewport = document.createElement("div");
    viewport.className = "rowViewport atStart";
    var track = document.createElement("div");
    track.className = "rowTrack";
    var previousButton = createRowArrow("prev", "chevronLeft", "Zurück blättern");
    var nextButton = createRowArrow("next", "chevronRight", "Weiter blättern");
    viewport.appendChild(previousButton);
    viewport.appendChild(track);
    viewport.appendChild(nextButton);
    section.appendChild(viewport);

    initRowScrolling(viewport, track, previousButton, nextButton);
    observeTrackForPreload(track);
    return track;
}

// ===================================================================
// Vorladen in Bändern: Der Browser lädt "lazy"-Bilder in einem seitlich scrollenden Band erst kurz
// bevor sie sichtbar werden. Liegt ein Band in der Nähe des sichtbaren Bereichs, werden seine Bilder
// bis zwei Bandbreiten links und rechts vom sichtbaren Teil geladen. Das wirkt bei jeder Art zu
// scrollen (Pfeile, Ziehen, Wischen, Tasten), weil es nur von der Scrollposition abhängt.
// ===================================================================
var bandPreloadObserver = null;

function observeTrackForPreload(track) {
    if (!window.IntersectionObserver) {
        return;
    }
    if (bandPreloadObserver == null) {
        bandPreloadObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    bandPreloadObserver.unobserve(entry.target);
                    startTrackPreload(entry.target);
                }
            });
        }, { rootMargin: "400px 0px" });
    }
    bandPreloadObserver.observe(track);
}

// Beobachter je Band (startTrackPreload) und resize-Listener (initRowScrolling); resetPage gibt sie frei
var trackPreloadObservers = [];
var rowResizeHandlers = [];

// Beim Seitenwechsel: Listener und Beobachter der alten Bänder entfernen (sonst bleiben die
// Bänder samt Kacheln im Speicher, bis die Fenstergrösse sich ändert)
function releaseRowBands() {
    trackPreloadObservers.forEach(function (observer) {
        observer.disconnect();
    });
    trackPreloadObservers = [];
    if (bandPreloadObserver != null) {
        bandPreloadObserver.disconnect();
    }
    rowResizeHandlers.forEach(function (handler) {
        window.removeEventListener("resize", handler);
    });
    rowResizeHandlers = [];
    releaseAppThumbnails();
}

// ===================================================================
// App: Kachelbilder von Fotos ohne Vorschaubild (?sflixThumb, SFLIX-20261125-152). Die App liest dafür
// jedes Original ganz von der Quelle. Darum lädt nur, was sichtbar ist (oder knapp davor), höchstens
// APP_THUMB_PARALLEL gleichzeitig. Wer wegscrollt, verlässt die Warteschlange; beim Seitenwechsel
// (z. B. Bildbetrachter) wird sie geleert, damit die Bandbreite dem Betrachter gehört.
// ===================================================================
var APP_THUMB_PARALLEL = 3;
var appThumbQueue = [];
var appThumbActive = 0;
var appThumbGeneration = 0;
var appThumbObserver = null;

function queueAppThumbnail(image, src) {
    image.dataset.thumbSrc = src;
    if (appThumbObserver == null) {
        appThumbObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var index = appThumbQueue.indexOf(entry.target);
                if (entry.isIntersecting) {
                    if (index < 0) {
                        appThumbQueue.push(entry.target);
                    }
                } else if (index >= 0) {
                    appThumbQueue.splice(index, 1);
                }
            });
            pumpAppThumbnails();
        }, { rootMargin: "150px" });
    }
    appThumbObserver.observe(image);
}

function pumpAppThumbnails() {
    while (appThumbActive < APP_THUMB_PARALLEL && appThumbQueue.length > 0) {
        startAppThumbnail(appThumbQueue.shift());
    }
}

function startAppThumbnail(image) {
    var generation = appThumbGeneration;
    appThumbObserver.unobserve(image);
    appThumbActive++;
    var finish = function () {
        image.removeEventListener("load", finish);
        image.removeEventListener("error", finish);
        if (generation === appThumbGeneration) {
            appThumbActive--;
            pumpAppThumbnails();
        }
    };
    image.addEventListener("load", finish);
    image.addEventListener("error", finish);
    image.loading = "eager";
    image.src = image.dataset.thumbSrc;
}

function releaseAppThumbnails() {
    if (appThumbObserver != null) {
        appThumbObserver.disconnect();
    }
    appThumbQueue = [];
    appThumbActive = 0;
    appThumbGeneration++;
}

function startTrackPreload(track) {
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                observer.unobserve(entry.target);
                if (entry.target.loading === "lazy") {
                    entry.target.loading = "eager";
                }
            }
        });
    }, { root: track, rootMargin: "0px 200%" });
    // Originalfotos (ohne Vorschaubild) nicht vorladen: Sie lädt der Browser erst kurz vor dem Sichtbarwerden,
    // sonst nähmen sie dem Bildbetrachter die Bandbreite weg (Netzlaufwerk, App; DEC-385)
    track.querySelectorAll("img.cardImage:not([data-original])").forEach(function (image) {
        observer.observe(image);
    });
    trackPreloadObservers.push(observer);
}

// ===================================================================
// Rasteransicht ("Alle anzeigen" bei Bändern ohne eigene Seite)
// ===================================================================

// Die Kacheln werden für die Dauer der Ansicht ins Raster verschoben (nicht kopiert), damit
// sie sich genau gleich verhalten. Beim Schliessen kommen sie zurück in ihre Zeilen.
var openGrid = null;

function openCardGrid(section, title) {
    closeCardGrid(true);
    // Vor dem Verschieben der Kacheln: Danach ist die Seite oft kürzer als das Fenster und der
    // Browser setzt die Scrollposition auf 0 (SFLIX-20261125-156)
    var scrollTop = window.scrollY;
    var overlay = document.createElement("div");
    overlay.id = "cardGrid";
    overlay.setAttribute("role", "dialog");
    overlay.setAttribute("aria-label", title);

    var header = document.createElement("div");
    header.className = "cardGridHeader";
    var heading = document.createElement("h2");
    heading.textContent = title;
    var closeButton = document.createElement("button");
    closeButton.type = "button";
    closeButton.className = "cardGridClose";
    closeButton.setAttribute("aria-label", "Schliessen");
    closeButton.appendChild(createIcon("close"));
    closeButton.addEventListener("click", closeCardGrid);
    header.appendChild(heading);
    header.appendChild(closeButton);

    var grid = document.createElement("div");
    grid.className = "cardGridItems";
    var moved = [];
    section.querySelectorAll(".rowTrack").forEach(function (track) {
        Array.prototype.slice.call(track.children).forEach(function (card) {
            moved.push({ card: card, track: track });
            grid.appendChild(card);
        });
    });
    heading.appendChild(createRowCount(moved.length));

    // Nach dem Klick auf eine Kachel (abspielen, Ordner öffnen) die Ansicht schliessen
    grid.addEventListener("click", function (event) {
        if (event.target.closest(".card") != null) {
            setTimeout(closeCardGrid, 0);
        }
    });

    overlay.appendChild(header);
    overlay.appendChild(grid);
    document.body.appendChild(overlay);
    document.body.classList.add("gridOpen");
    openGrid = { overlay: overlay, moved: moved, section: section, scrollTop: scrollTop };
    closeButton.focus({ preventScroll: true });
    // Eigener Verlaufseintrag: Zurück (Android-Taste, Maus, Browser) schliesst zuerst das Raster
    // (main.js pushOverlayHistory, DEC-106)
    pushOverlayHistory("grid", scrollTop);
}

// Schliesst die Rasteransicht; true, wenn eine offen war. fromHistory: durch Zurück ausgelöst
function closeCardGrid(fromHistory) {
    if (openGrid == null) {
        return false;
    }
    if (fromHistory !== true) {
        // X, Esc, Kachel: eigenen Verlaufseintrag entfernen
        popOverlayHistory("grid");
    }
    var current = openGrid;
    openGrid = null;
    current.moved.forEach(function (entry) {
        entry.track.appendChild(entry.card);
    });
    current.overlay.remove();
    document.body.classList.remove("gridOpen");
    // Scrollposition von vor dem Öffnen (SFLIX-20261125-156)
    window.scrollTo(0, current.scrollTop);
    refreshRowArrows(current.section);
    return true;
}

function createRowArrow(direction, iconName, label) {
    var button = document.createElement("button");
    button.type = "button";
    button.className = "rowArrow " + direction;
    button.appendChild(createIcon(iconName));
    button.setAttribute("aria-label", label);
    // Tastatur nutzt die Pfeiltasten direkt auf den Kacheln
    button.tabIndex = -1;
    return button;
}

// Blättern per Pfeil-Buttons, Ziehen mit der Maus und Wischen (nativ)
function initRowScrolling(viewport, track, previousButton, nextButton) {
    var updateArrows = function () {
        viewport.classList.toggle("atStart", track.scrollLeft <= 2);
        viewport.classList.toggle("atEnd", track.scrollLeft + track.clientWidth >= track.scrollWidth - 2);
    };
    track.updateArrows = updateArrows;
    track.addEventListener("scroll", updateArrows, { passive: true });
    // Nach einem Seitenwechsel (ohne Neuladen) ist das Band weg: Listener entfernen
    // (zusätzlich gibt resetPage über releaseRowBands alle frei)
    var onResize = function () {
        if (!track.isConnected && !document.body.classList.contains("gridOpen")) {
            window.removeEventListener("resize", onResize);
            var index = rowResizeHandlers.indexOf(onResize);
            if (index >= 0) {
                rowResizeHandlers.splice(index, 1);
            }
            return;
        }
        updateArrows();
    };
    window.addEventListener("resize", onResize);
    rowResizeHandlers.push(onResize);

    previousButton.addEventListener("click", function () {
        track.scrollBy({ left: -track.clientWidth * 0.9, behavior: "smooth" });
    });
    nextButton.addEventListener("click", function () {
        track.scrollBy({ left: track.clientWidth * 0.9, behavior: "smooth" });
    });

    // Mit gedrückter Maustaste ziehen
    var drag = null;
    var suppressClick = false;
    track.addEventListener("pointerdown", function (event) {
        if (event.pointerType !== "mouse" || event.button !== 0) {
            return;
        }
        suppressClick = false;
        // Reste eines Ziehens, dessen Ende nicht ankam (ohne Capture ausserhalb losgelassen), blockierten sonst Klicks
        track.classList.remove("dragging");
        drag = { x: event.clientX, scrollLeft: track.scrollLeft, moved: false, pointerId: event.pointerId };
    });
    track.addEventListener("pointermove", function (event) {
        if (drag == null) {
            return;
        }
        // Maustaste ausserhalb des Bandes losgelassen: nicht mehr ziehen
        if ((event.buttons & 1) === 0) {
            endDrag(true);
            return;
        }
        var distance = event.clientX - drag.x;
        if (!drag.moved && Math.abs(distance) > 6) {
            drag.moved = true;
            track.classList.add("dragging");
            try {
                track.setPointerCapture(drag.pointerId);
            } catch (error) {
                // Ohne Capture funktioniert das Ziehen innerhalb des Bandes trotzdem
            }
        }
        if (drag.moved) {
            track.scrollLeft = drag.scrollLeft - distance;
        }
    });
    // releasedOutside: Taste ausserhalb des Bandes losgelassen, es folgt kein Klick, der zu schlucken wäre
    var endDrag = function (releasedOutside) {
        if (drag != null && drag.moved) {
            track.classList.remove("dragging");
            suppressClick = releasedOutside !== true;
        }
        drag = null;
    };
    track.addEventListener("pointerup", function () { endDrag(false); });
    track.addEventListener("pointercancel", function () { endDrag(false); });
    // Nach dem Ziehen keinen Klick auf die Kachel auslösen (nur Mausklicks; Tastatur-Klicks haben detail 0)
    track.addEventListener("click", function (event) {
        if (suppressClick && event.detail !== 0) {
            event.preventDefault();
            event.stopPropagation();
            suppressClick = false;
        }
    }, true);
}

// Pfeiltasten auf einer Kachel: links/rechts im Band, hoch/runter ins nächste Band
function handleRowNavigation(event) {
    if (!["ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"].includes(event.key) || event.target.closest == null) {
        return false;
    }
    var card = event.target.closest(".card");
    if (card == null || !card.parentNode.classList.contains("rowTrack")) {
        return false;
    }

    var track = card.parentNode;
    var cards = Array.prototype.slice.call(track.children);
    var target = null;

    if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
        target = cards[cards.indexOf(card) + (event.key === "ArrowRight" ? 1 : -1)] || null;
        event.preventDefault();
    } else {
        var tracks = Array.prototype.slice.call(document.querySelectorAll(".rowTrack"));
        var otherTrack = tracks[tracks.indexOf(track) + (event.key === "ArrowDown" ? 1 : -1)];
        if (otherTrack == null) {
            return true;
        }
        // Kachel, die horizontal am nächsten liegt
        var left = card.getBoundingClientRect().left;
        var bestDistance = Infinity;
        Array.prototype.forEach.call(otherTrack.children, function (candidate) {
            var distance = Math.abs(candidate.getBoundingClientRect().left - left);
            if (distance < bestDistance) {
                bestDistance = distance;
                target = candidate;
            }
        });
        event.preventDefault();
    }

    if (target != null) {
        target.focus({ preventScroll: true });
        target.scrollIntoView({ block: "nearest", inline: "nearest", behavior: "smooth" });
    }
    return true;
}

// Markierung: Datei vollständig geladen (Prüfung am Ende von start.html)
var sflixFileEndCards = true;
