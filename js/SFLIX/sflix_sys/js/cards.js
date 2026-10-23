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
        image.src = options.previewSrc;
        image.alt = "";
        image.loading = "lazy";
        image.decoding = "async";
        image.draggable = false;
        mediaNode.appendChild(image);
    } else {
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
        mediaNode.appendChild(placeholder);
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
    var previewSrc = isImage(mediaPath) ? (getPreviewForMedia(mediaPath, true) || mediaPath) : getPreviewForMedia(mediaPath, options.exactPreview);

    var seen = isVideo(mediaPath) && isSeen(mediaPath);
    var card = createCard(container, {
        href: href,
        title: options.title || getDisplayName(mediaPath),
        subtitle: options.subtitle != null ? options.subtitle : (folders.length > 1 ? folders.slice(1).join(" › ") : folders.join(" › ")),
        previewSrc: previewSrc,
        placeholderIcon: getMediaIconName(mediaPath),
        // Gesehene Videos zeigen einen Haken statt des Fortschrittsbalkens
        progress: seen ? null : options.progress,
        seed: mediaPath
    });
    card.dataset.media = mediaPath;
    setCardFavorite(card, getFavorites().includes(mediaPath));
    if (seen || options.label) {
        addCardLabel(card, options.label || "Gesehen", seen && !options.label);
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
    return createCard(container, {
        href: href,
        title: folderLevels[folderLevels.length - 1],
        subtitle: options.subtitle != null ? options.subtitle : describeFolder(node),
        previewSrc: resolvePreviewSrc(levelPath, "normal"),
        placeholderIcon: getFolderIconName(node),
        seed: levelPath
    });
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
// Menübänder (horizontal scrollbar wie bei Netflix)
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
        var link = document.createElement("a");
        link.className = "rowLink";
        link.href = linkHref;
        link.textContent = "Alle anzeigen ›";
        header.appendChild(link);
    }

    var viewport = document.createElement("div");
    viewport.className = "rowViewport atStart";
    var track = document.createElement("div");
    track.className = "rowTrack";
    var previousButton = createRowArrow("prev", "chevronLeft", "Zurück blättern");
    var nextButton = createRowArrow("next", "chevronRight", "Weiter blättern");
    viewport.appendChild(previousButton);
    viewport.appendChild(track);
    viewport.appendChild(nextButton);

    section.appendChild(header);
    section.appendChild(viewport);
    parent.appendChild(section);

    initRowScrolling(viewport, track, previousButton, nextButton);
    return track;
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
    window.addEventListener("resize", updateArrows);

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
        drag = { x: event.clientX, scrollLeft: track.scrollLeft, moved: false, pointerId: event.pointerId };
    });
    track.addEventListener("pointermove", function (event) {
        if (drag == null) {
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
    var endDrag = function () {
        if (drag != null && drag.moved) {
            track.classList.remove("dragging");
            suppressClick = true;
        }
        drag = null;
    };
    track.addEventListener("pointerup", endDrag);
    track.addEventListener("pointercancel", endDrag);
    // Nach dem Ziehen keinen Klick auf die Kachel auslösen
    track.addEventListener("click", function (event) {
        if (suppressClick) {
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
