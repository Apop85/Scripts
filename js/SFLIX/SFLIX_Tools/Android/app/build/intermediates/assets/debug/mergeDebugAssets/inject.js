// Wird von der App in start.html eingefügt (vor main.js, nach den übrigen Skripten).
// Funktioniert mit jeder STEFFLIX-Version auf der Freigabe. Nutzt aus STEFFLIX nur:
// showSplashscreen, getSplashDedication, checkForUpdate, checkForUpdaterUpdate, initAndroidSection,
// die IDs #settings, #settingsDD, #settingsPageToggler, #androidDownloadButton, #androidHint,
// #loadingScreen (wird beim Ausblenden auf display none gesetzt) und die Klassen menuIcon/menuLabel.
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
    window.sflixAppRefresh = refreshUpdateMark;

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
        "html.inApp.appUpdate #appSettingsToggler .menuLabel::after { content: ''; display: inline-block; width: 10px; height: 10px; margin-left: 8px; border-radius: 50%; background: #e8261a; vertical-align: middle; }"
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
