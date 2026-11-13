// Wird von der App in start.html eingefügt (vor main.js, nach den übrigen Skripten).
// Funktioniert mit jeder STEFFLIX-Version auf der Freigabe. Nutzt aus STEFFLIX nur:
// showSplashscreen, getSplashDedication, checkForUpdate, checkForUpdaterUpdate, initAndroidSection,
// die IDs #settings, #settingsDD, #settingsPageToggler, #androidDownloadButton, #androidHint,
// #loadingScreen (wird beim Ausblenden auf display none gesetzt) und die Klassen menuIcon/menuLabel.
// Wiedergabe (seit App 1.2.0): das Medium #video (audio oder video, dataset.mediaPath), #mediaTitle,
// die Links #next a / #prev a, getInheritedPreview bzw. getPreviewForMedia und für Bild-im-Bild den
// Knopf .ctrlPip im Player (STEFFLIX ruft video.requestPictureInPicture auf und zeigt den Knopf nur,
// wenn document.pictureInPictureEnabled gilt; beides stellt dieses Skript bereit).
// STEFFLIX ab 2.6.1 meldet zudem StefflixApp.loadingProgress(geladen, alle) aus ui.js. Werden sie in STEFFLIX umbenannt, hier nachführen.
(function () {
    window.stefflixApp = true;
    var app = window.StefflixApp;
    document.documentElement.classList.add("inApp");

    // Die App zeigt ihren eigenen Startbildschirm. Nur an besonderen Tagen zeigt die Seite ihren.
    if (typeof showSplashscreen == "function") {
        var originalSplash = showSplashscreen;
        showSplashscreen = function () {
            if (typeof getSplashDedication == "function" && getSplashDedication(new Date()) != null) {
                originalSplash();
                if (app) app.webSplash();
            }
        };
    }

    // Hinweise auf neue Versionen von STEFFLIX und Updater: in der App nicht installierbar
    if (typeof checkForUpdate == "function") {
        checkForUpdate = function () {};
    }
    if (typeof checkForUpdaterUpdate == "function") {
        checkForUpdaterUpdate = function () {};
    }

    // Einstellungen › Android-App: Verweis auf die App-Einstellungen statt Download
    initAndroidSection = function () {
        var button = document.getElementById("androidDownloadButton");
        var hint = document.getElementById("androidHint");
        if (button && button.parentNode) {
            button.parentNode.style.display = "none";
        }
        if (hint && !document.getElementById("appSettingsButton")) {
            hint.textContent = "Du nutzt STEFFLIX in der App (Version " + (app ? app.version() : "") + "). " +
                "Updates, Quellen, Zwischenspeicher und weitere Einstellungen findest du in den App-Einstellungen.";
            var wrap = document.createElement("div");
            wrap.className = "backupActions";
            var open = document.createElement("a");
            open.id = "appSettingsButton";
            open.className = "sflixButton primary";
            open.textContent = "App-Einstellungen öffnen";
            open.addEventListener("click", function () {
                if (app) app.openSettings();
            });
            wrap.appendChild(open);
            hint.parentNode.insertBefore(wrap, hint.nextSibling);
        }
    };

    // Menü: "Quelle wechseln" und "App-Einstellungen" unter "Einstellungen"
    var icons = {
        source: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 7h13l-3-3M20 17H7l3 3"/></svg>',
        phone: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="6" y="2.5" width="12" height="19" rx="2.5"/><path d="M10.5 18.5h3"/></svg>'
    };

    function closeMenu() {
        var menu = document.getElementById("settings");
        if (menu) {
            menu.classList.remove("open");
        }
    }

    function menuItem(id, label, icon, action) {
        var item = document.createElement("a");
        item.id = id;
        item.innerHTML = '<span class="menuIcon">' + icon + '</span><span class="menuLabel"></span>';
        item.querySelector(".menuLabel").textContent = label;
        item.addEventListener("click", function (event) {
            event.preventDefault();
            event.stopPropagation();
            closeMenu();
            action();
        });
        return item;
    }

    function refreshUpdateMark() {
        var available = false;
        try {
            available = app && app.updateAvailable();
        } catch (e) {
        }
        document.documentElement.classList.toggle("appUpdate", !!available);
    }
    // Nach den App-Einstellungen: roter Punkt und Bild-im-Bild-Knopf (Einstellung kann sich geändert haben)
    window.sflixAppRefresh = function () {
        refreshUpdateMark();
        var pip = pipAvailable();
        document.querySelectorAll(".ctrlPip").forEach(function (button) {
            button.hidden = !pip;
        });
    };

    function addMenuItems() {
        var dropdown = document.getElementById("settingsDD");
        if (!dropdown || document.getElementById("appSourceToggler")) {
            return;
        }
        var source = menuItem("appSourceToggler", "Quelle wechseln", icons.source, function () {
            if (app) app.switchSource();
        });
        var settings = menuItem("appSettingsToggler", "App-Einstellungen", icons.phone, function () {
            if (app) app.openSettings();
        });
        var anchor = document.getElementById("settingsPageToggler");
        if (anchor && anchor.parentNode === dropdown) {
            dropdown.insertBefore(source, anchor.nextSibling);
            dropdown.insertBefore(settings, source.nextSibling);
        } else {
            // Menü anders aufgebaut: am Ende, damit die Einträge nie fehlen
            dropdown.appendChild(source);
            dropdown.appendChild(settings);
        }
        refreshUpdateMark();
    }

    // Display anlassen, solange Videos oder Musik laufen (Medienereignisse steigen nicht auf: Capture)
    function reportPlaying() {
        var playing = false;
        document.querySelectorAll("video, audio").forEach(function (media) {
            if (!media.paused && !media.ended) {
                playing = true;
            }
        });
        if (app) app.setPlaying(playing);
    }
    ["play", "playing", "pause", "ended", "emptied"].forEach(function (type) {
        document.addEventListener(type, reportPlaying, true);
    });

    // ---- Wiedergabe im Hintergrund: Zustand an die App, Befehle aus Benachrichtigung und Sperrbildschirm ----

    function currentMedia() {
        var media = document.getElementById("video");
        return media && (media.tagName == "VIDEO" || media.tagName == "AUDIO") ? media : null;
    }

    function hasSource(media) {
        return !!(media.currentSrc || media.getAttribute("src"));
    }

    // "Serien/Show/Staffel 01/S01E01.mp4" → "Show · Staffel 01" (ohne Kategorie und Datei)
    function folderText(path) {
        var parts = String(path || "").split("/").filter(function (part) {
            return part !== "" && part !== ".";
        });
        parts.pop();
        if (parts.length > 1) {
            parts.shift();
        }
        return parts.join(" · ");
    }

    function fileTitle(path) {
        var name = String(path || "").split("/").pop() || "";
        return name.replace(/\.[^.]+$/, "");
    }

    // Vorschaubild als Pfad in der Seite (die App liest es aus dem Zwischenspeicher)
    function artworkPath(path) {
        var preview = null;
        try {
            if (typeof getInheritedPreview == "function") {
                preview = getInheritedPreview(path);
            } else if (typeof getPreviewForMedia == "function") {
                preview = getPreviewForMedia(path, false);
            }
        } catch (e) {
        }
        if (!preview) {
            return "";
        }
        try {
            return new URL(preview, location.href).pathname;
        } catch (e) {
            return "";
        }
    }

    function mediaState() {
        var media = currentMedia();
        if (!media || !hasSource(media)) {
            return { active: false };
        }
        var path = media.dataset ? media.dataset.mediaPath || "" : "";
        var titleNode = document.getElementById("mediaTitle");
        var title = titleNode && titleNode.textContent.trim() ? titleNode.textContent.trim() : fileTitle(path);
        return {
            active: true,
            playing: !media.paused && !media.ended,
            video: media.tagName == "VIDEO",
            position: Math.round((media.currentTime || 0) * 1000),
            duration: isFinite(media.duration) ? Math.round(media.duration * 1000) : 0,
            rate: media.playbackRate || 1,
            title: title,
            subtitle: folderText(path),
            artwork: artworkPath(path),
            hasNext: document.querySelector("#next a") != null,
            hasPrev: document.querySelector("#prev a") != null,
            width: media.videoWidth || 0,
            height: media.videoHeight || 0
        };
    }

    // force: bei Medienereignissen immer melden (Position), sonst nur bei Änderungen
    var lastMediaKey = "";
    function reportMedia(force) {
        if (!app || !app.mediaState) {
            return;
        }
        var state = mediaState();
        var key = JSON.stringify(Object.assign({}, state, { position: 0 }));
        if (!force && key === lastMediaKey) {
            return;
        }
        lastMediaKey = key;
        try {
            app.mediaState(JSON.stringify(state));
        } catch (e) {
        }
    }
    ["play", "playing", "pause", "ended", "emptied", "loadedmetadata", "durationchange", "seeked", "ratechange"].forEach(function (type) {
        document.addEventListener(type, function (event) {
            if (event.target === currentMedia() || type === "emptied") {
                reportMedia(true);
            }
        }, true);
    });
    // Titel und Navigation ändern sich nach dem Laden, der Player kann verschwinden
    setInterval(function () {
        reportMedia(false);
    }, 2000);

    function playMedia(media) {
        var request = media.play();
        if (request && request.catch) {
            request.catch(function () {});
        }
    }

    function seekMedia(media, seconds) {
        var end = isFinite(media.duration) ? media.duration : Infinity;
        media.currentTime = Math.max(0, Math.min(end, (media.currentTime || 0) + seconds));
    }

    function navigateMedia(id) {
        var link = document.querySelector("#" + id + " a");
        if (link) {
            link.click();
        }
    }

    // Befehle der App (Playback.CMD_*)
    window.sflixAppMedia = function (cmd, arg) {
        var media = currentMedia();
        if (!media) {
            return;
        }
        switch (cmd) {
            case "play":
                playMedia(media);
                break;
            case "pause":
                media.pause();
                break;
            case "toggle":
                if (media.paused) {
                    playMedia(media);
                } else {
                    media.pause();
                }
                break;
            case "next":
                navigateMedia("next");
                break;
            case "prev":
                navigateMedia("prev");
                break;
            case "back":
                seekMedia(media, -10);
                break;
            case "forward":
                seekMedia(media, 10);
                break;
            case "seek":
                media.currentTime = Math.max(0, (Number(arg) || 0) / 1000);
                break;
        }
    };

    // ---- Bild-im-Bild über die App ----
    // Die WebView kennt die Bild-im-Bild-Schnittstelle nicht. Dieses Skript stellt sie bereit, damit der
    // Knopf im Player von STEFFLIX erscheint, und leitet sie an die App weiter. Im kleinen Fenster ist nur
    // das Video zu sehen (Klasse appPip).

    var pipVideo = null;
    var pipScroll = 0;

    function pipAvailable() {
        try {
            return !!(app && app.pipAvailable && app.pipAvailable());
        } catch (e) {
            return false;
        }
    }

    try {
        Object.defineProperty(document, "pictureInPictureEnabled", { configurable: true, get: pipAvailable });
    } catch (e) {
    }
    try {
        Object.defineProperty(document, "pictureInPictureElement", { configurable: true, get: function () {
            return pipVideo;
        } });
    } catch (e) {
    }
    // Das Fenster schliesst der Nutzer selbst (Vergrössern oder Schliessen im Fenster)
    document.exitPictureInPicture = function () {
        return Promise.resolve();
    };
    if (window.HTMLVideoElement) {
        HTMLVideoElement.prototype.requestPictureInPicture = function () {
            if (!pipAvailable() || !app.enterPip(this.videoWidth || 16, this.videoHeight || 9)) {
                return Promise.reject(new Error("Bild-im-Bild ist nicht verfügbar"));
            }
            pipVideo = this;
            return Promise.resolve({});
        };
    }

    window.sflixAppPip = function (active) {
        var root = document.documentElement;
        if (active) {
            var media = currentMedia();
            var video = pipVideo || (media && media.tagName == "VIDEO" ? media : null);
            if (!video) {
                return;
            }
            pipVideo = video;
            pipScroll = window.scrollY;
            video.classList.add("appPipVideo");
            root.classList.add("appPip");
            video.dispatchEvent(new Event("enterpictureinpicture"));
            return;
        }
        root.classList.remove("appPip");
        var left = pipVideo;
        pipVideo = null;
        if (left) {
            left.classList.remove("appPipVideo");
            left.dispatchEvent(new Event("leavepictureinpicture"));
            requestAnimationFrame(function () {
                window.scrollTo(0, pipScroll);
            });
        }
    };

    // Grössere Menüs und Tippflächen (mindestens 48 dp), roter Punkt bei neuer App-Version
    var style = document.createElement("style");
    style.textContent = [
        "html.inApp { --sflix-header-height: 56px; }",
        "html.inApp #searchTextField, html.inApp #settings { height: 56px; width: 56px; background-size: 28px 28px; }",
        "html.inApp #menu li a { font-size: 1.4rem; padding: 0 18px; }",
        "html.inApp #menu li a::after { left: 18px; right: 18px; }",
        "@media (max-width: 800px) { html.inApp #menu li a { font-size: 1.35rem; padding: 0 14px; } html.inApp #menu li a::after { left: 14px; right: 14px; } }",
        "@media (max-width: 600px) { html.inApp #menu { height: 56px; } html.inApp #menu li a { font-size: 1.3rem; } }",
        "@media (max-width: 480px) { html.inApp #menu li a { font-size: 1.2rem; padding: 0 12px; } html.inApp #menu li a::after { left: 12px; right: 12px; } }",
        "html.inApp #settings.open #settingsDD, html.inApp.canHover #settings:hover #settingsDD { width: 310px; top: 56px; }",
        "html.inApp #settingsDD a { min-height: 56px; }",
        "html.inApp .menuLabel { font-size: 1.35rem; }",
        "html.inApp .menuIcon { width: 52px; height: 52px; background-size: 28px 28px; }",
        "html.inApp .menuIcon svg { width: 28px; height: 28px; }",
        "html.inApp.appUpdate #settings::after { content: ''; position: absolute; top: 9px; right: 9px; width: 10px; height: 10px; border-radius: 50%; background: #e8261a; }",
        "html.inApp.appUpdate #appSettingsToggler .menuLabel::after { content: ''; display: inline-block; width: 10px; height: 10px; margin-left: 8px; border-radius: 50%; background: #e8261a; vertical-align: middle; }",
        // Bild-im-Bild: nur das Video, bildfüllend. Transformationen der Eltern würden position: fixed brechen.
        "html.appPip, html.appPip body { background: #000 !important; overflow: hidden !important; }",
        "html.appPip body * { visibility: hidden !important; transform: none !important; filter: none !important; backdrop-filter: none !important; perspective: none !important; contain: none !important; will-change: auto !important; animation: none !important; transition: none !important; }",
        "html.appPip video.appPipVideo { visibility: visible !important; position: fixed !important; left: 0 !important; top: 0 !important; right: auto !important; bottom: auto !important; width: 100vw !important; height: 100vh !important; max-width: none !important; max-height: none !important; margin: 0 !important; object-fit: contain !important; background: #000 !important; z-index: 2147483647 !important; }"
    ].join("\n");

    // Meldet der App, wenn STEFFLIX seinen Ladescreen (#loadingScreen) ausblendet, also die Seite und
    // die sichtbaren Vorschaubilder da sind. Erst dann verschwindet der Startbildschirm der App.
    function watchLoadingScreen() {
        var screen = document.getElementById("loadingScreen");
        if (!screen) {
            if (app && app.webReady) app.webReady();
            return;
        }
        var reported = false;
        var observer = new MutationObserver(check);
        function check() {
            if (!reported && getComputedStyle(screen).display === "none") {
                reported = true;
                observer.disconnect();
                if (app && app.webReady) app.webReady();
            }
        }
        observer.observe(screen, { attributes: true, attributeFilter: ["style", "class"] });
        check();
    }

    function start() {
        document.head.appendChild(style);
        addMenuItems();
        watchLoadingScreen();
    }
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start);
    } else {
        start();
    }
})();
