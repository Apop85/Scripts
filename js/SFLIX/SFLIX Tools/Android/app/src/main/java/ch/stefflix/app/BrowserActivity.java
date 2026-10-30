package ch.stefflix.app;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.content.pm.ActivityInfo;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.text.TextUtils;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;

import com.hierynomus.smbj.share.File;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Zeigt STEFFLIX aus einer Freigabe an: WebView ohne Adresszeile. Alle Anfragen an HOST werden
 * direkt aus der Freigabe beantwortet (über den Zwischenspeicher FileCache, mit Range fuer grosse
 * Dateien), ohne Server. Alle Freigaben teilen sich eine Herkunft; STEFFLIX trennt den Speicher ueber install.js.
 */
public class BrowserActivity extends Activity implements MediaServer.Listener {

    static final String EXTRA_ID = "id";
    private static final String HOST = "appassets.androidplatform.net";
    private static final int REQUEST_SETTINGS = 1;

    private ShareStore.Share share;
    private AppPrefs prefs;
    private SmbSource smb;
    private FileCache cache;
    private MediaServer media;
    private String prefix;
    private FrameLayout root;
    private WebView web;
    private View customView;
    private WebChromeClient.CustomViewCallback customCallback;

    /** Eine Seite wurde schon einmal fertig geladen (danach führt ein Fehler nicht mehr direkt zur Auswahl). */
    private volatile boolean everLoaded;
    private volatile boolean mainLoadFailed;
    private AlertDialog connectionDialog;
    private final long[] recentFailures = new long[3];
    private int failureIndex;
    private boolean updateChecked;

    /**
     * Verbindung warm halten: Solange die App sichtbar ist, alle KEEP_ALIVE_MS eine kleine Anfrage an
     * das NAS und die Listen der Programmordner auffrischen (jünger als 30 s, siehe FileCache). So
     * schliessen NAS und VPN die Verbindung nicht wegen Leerlauf, und kein Seitenaufruf wartet darauf.
     * Nach einer Pause (Bildschirm aus, andere App) wird sofort im Hintergrund neu verbunden.
     */
    private static final long KEEP_ALIVE_MS = 25_000;
    private static final long LISTING_MAX_AGE_MS = 20_000;
    private final ExecutorService background = Executors.newFixedThreadPool(3, r -> {
        Thread t = new Thread(r, "stefflix-warmup");
        t.setDaemon(true);
        return t;
    });
    private long pausedAt;
    private final Runnable keepAlive = new Runnable() {
        @Override
        public void run() {
            refreshConnection();
            root.postDelayed(this, KEEP_ALIVE_MS);
        }
    };

    private void refreshConnection() {
        if (background.isShutdown()) return;
        background.execute(() -> {
            if (smb.keepAlive()) cache.warmUp(background, LISTING_MAX_AGE_MS);
        });
    }

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        share = new ShareStore(this).find(getIntent().getStringExtra(EXTRA_ID));
        if (share == null) {
            finish();
            return;
        }
        prefs = new AppPrefs(this);
        smb = new SmbSource(share);
        cache = new FileCache(this, smb, share.id, prefs.cacheMb() * 1024L * 1024L);
        prefix = "/" + share.id + "/";
        // Verbindung und Ordnerlisten schon während der Startanimation, alle gleichzeitig
        cache.warmUp(background, LISTING_MAX_AGE_MS);
        try {
            media = new MediaServer(smb, prefs.bufferMb(), this);
        } catch (IOException e) {
            media = null; // Medien laufen dann über die abgefangenen Anfragen
        }

        root = new FrameLayout(this);
        root.setBackgroundColor(Color.BLACK);
        web = new WebView(this);
        web.setBackgroundColor(Color.BLACK);
        root.addView(web, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        setContentView(root);

        WebSettings s = web.getSettings();
        s.setJavaScriptEnabled(true);
        s.setDomStorageEnabled(true);
        s.setDatabaseEnabled(true);
        s.setMediaPlaybackRequiresUserGesture(false);
        s.setAllowFileAccess(false);
        s.setAllowContentAccess(false);
        // Medien kommen von http://127.0.0.1 (MediaServer), die Seite von https
        s.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
        s.setSupportMultipleWindows(false);
        s.setUseWideViewPort(true);
        s.setLoadWithOverviewMode(true);
        // Systemschriftgroesse ignorieren: sie vergroessert nur die Schrift, nicht die festen
        // Zeilenhoehen in STEFFLIX, und schneidet so z.B. Kacheltitel ab
        s.setTextZoom(100);

        // Keine Linkvorschau und kein Kontextmenue bei langem Druck
        web.setLongClickable(false);
        web.setOnLongClickListener(v -> true);
        web.setHapticFeedbackEnabled(false);

        web.setWebViewClient(new Client());
        web.setWebChromeClient(new Chrome());
        web.addJavascriptInterface(new Bridge(), "StefflixApp");

        if (state == null || web.restoreState(state) == null) {
            // Nur beim Oeffnen, nicht beim Zurueckkehren zur Startseite
            showSplash();
            web.loadUrl("https://" + HOST + prefix + "start.html");
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle out) {
        super.onSaveInstanceState(out);
        if (web != null) web.saveState(out);
    }

    @Override
    protected void onPause() {
        super.onPause();
        if (web != null) web.onPause();
        if (root != null) root.removeCallbacks(keepAlive);
        pausedAt = System.currentTimeMillis();
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (web != null) web.onResume();
        if (root == null) return;
        // Nach einer Pause ist die Verbindung oft weg: gleich im Hintergrund neu aufbauen
        if (pausedAt > 0 && System.currentTimeMillis() - pausedAt > LISTING_MAX_AGE_MS) refreshConnection();
        root.removeCallbacks(keepAlive);
        root.postDelayed(keepAlive, KEEP_ALIVE_MS);
    }

    @Override
    protected void onDestroy() {
        if (root != null) root.removeCallbacks(keepAlive);
        background.shutdownNow();
        if (connectionDialog != null) connectionDialog.dismiss();
        if (web != null) web.destroy();
        if (media != null) media.close();
        if (smb != null) {
            final SmbSource s = smb;
            new Thread(s::close).start();
        }
        super.onDestroy();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST_SETTINGS || web == null) return;
        cache.setMaxDiskBytes(prefs.cacheMb() * 1024L * 1024L);
        if (!prefs.keepScreenOn()) getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
        web.evaluateJavascript("window.sflixAppRefresh && sflixAppRefresh();", null);
        if (resultCode == SettingsActivity.RESULT_HELP) {
            web.evaluateJavascript("typeof openHelp == 'function' && openHelp();", null);
        }
    }

    @SuppressWarnings("deprecation")
    @Override
    public void onBackPressed() {
        if (customView != null) {
            customCallback.onCustomViewHidden();
            hideCustomView();
        } else if (web != null && web.canGoBack()) {
            web.goBack();
        } else {
            super.onBackPressed();
        }
    }

    private void hideCustomView() {
        if (customView == null) return;
        root.removeView(customView);
        customView = null;
        web.setVisibility(View.VISIBLE);
        setFullscreen(false);
        setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_UNSPECIFIED);
    }

    // ---- Splashscreen: sofort aus den App-Dateien, bleibt bis Animation und start.html fertig sind ----

    private static final long SPLASH_MS = 2900; // wie splash-out in sflix.css (72 % von 4 s)
    private WebView splash;
    private boolean pageReady, splashDone;

    @SuppressLint("SetJavaScriptEnabled")
    private void showSplash() {
        splash = new WebView(this);
        splash.setBackgroundColor(0xff0b0b0d);
        splash.getSettings().setAllowFileAccess(true); // nur fuer file:///android_asset
        splash.setOnTouchListener((v, e) -> {
            if (e.getAction() == android.view.MotionEvent.ACTION_UP) {
                splashDone = true;
                hideSplash(false);
            }
            return true;
        });
        root.addView(splash, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
        splash.loadUrl("file:///android_asset/splash.html");
        root.postDelayed(() -> {
            splashDone = true;
            hideSplash(false);
        }, SPLASH_MS);
    }

    private void hideSplash(boolean force) {
        if (splash == null || (!force && !(pageReady && splashDone))) return;
        final WebView s = splash;
        splash = null;
        s.animate().alpha(0f).setDuration(300).withEndAction(() -> {
            root.removeView(s);
            s.destroy();
        });
        checkForAppUpdate();
    }

    // ---- In start.html eingefügte Skripte ----

    private static String injectScript;

    /** assets/inject.js: Menüeinträge, grössere Menüs, Update-Hinweise aus, Splashscreen der Seite aus. */
    private String injectScript() {
        if (injectScript == null) {
            try (InputStream in = getAssets().open("inject.js")) {
                ByteArrayOutputStream out = new ByteArrayOutputStream();
                byte[] buf = new byte[16384];
                int n;
                while ((n = in.read(buf)) > 0) out.write(buf, 0, n);
                injectScript = "<script>" + out.toString("UTF-8") + "</script>\n";
            } catch (IOException e) {
                injectScript = "<script>window.stefflixApp=true;</script>\n";
            }
        }
        return injectScript;
    }

    /** Leitet Videos und Musik (src von audio/video) auf MediaServer um. Für STEFFLIX sehen src und
     *  getAttribute("src") aus wie ohne Umleitung (wichtig für Zeitstempel und Vergleiche). */
    private static String mediaHook(String base) {
        return "<script>(function(){var base=" + org.json.JSONObject.quote(base) + ";"
                + "var P=HTMLMediaElement.prototype,d=Object.getOwnPropertyDescriptor(P,'src'),"
                + "c=Object.getOwnPropertyDescriptor(P,'currentSrc'),g=Element.prototype.getAttribute,"
                + "sa=Element.prototype.setAttribute;"
                + "function map(v){v=String(v);return(/^[a-z][a-z0-9+.-]*:/i.test(v)||v.indexOf('/')===0)?v:base+v;}"
                + "function abs(v){try{return new URL(v,location.href).href;}catch(e){return v;}}"
                + "Object.defineProperty(P,'src',{configurable:true,enumerable:true,"
                + "get:function(){return this.__sflixSrc!==undefined?abs(this.__sflixSrc):d.get.call(this);},"
                + "set:function(v){this.__sflixSrc=String(v);d.set.call(this,map(v));}});"
                + "Object.defineProperty(P,'currentSrc',{configurable:true,enumerable:true,"
                + "get:function(){return this.__sflixSrc!==undefined?abs(this.__sflixSrc):c.get.call(this);}});"
                + "Element.prototype.getAttribute=function(n){if(this instanceof HTMLMediaElement&&"
                + "this.__sflixSrc!==undefined&&String(n).toLowerCase()==='src')return this.__sflixSrc;"
                + "return g.call(this,n);};"
                + "Element.prototype.setAttribute=function(n,v){if(this instanceof HTMLMediaElement&&"
                + "String(n).toLowerCase()==='src'){this.src=v;return;}return sa.call(this,n,v);};"
                + "})();</script>\n";
    }

    private byte[] injectIntoStart(byte[] html) {
        String s = new String(html, StandardCharsets.UTF_8);
        int i = s.indexOf("<script src=\"sflix_sys/js/main.js\"");
        if (i < 0) return html;
        String hook = media != null ? mediaHook(media.base()) : "";
        return (s.substring(0, i) + hook + injectScript() + s.substring(i)).getBytes(StandardCharsets.UTF_8);
    }

    // ---- Brücke zu STEFFLIX (window.StefflixApp) ----

    private final class Bridge {
        @JavascriptInterface
        public void webSplash() {
            runOnUiThread(() -> hideSplash(true));
        }

        @JavascriptInterface
        public void openSettings() {
            runOnUiThread(() -> startActivityForResult(new Intent(BrowserActivity.this, SettingsActivity.class)
                    .putExtra(SettingsActivity.EXTRA_CURRENT, share.id), REQUEST_SETTINGS));
        }

        @JavascriptInterface
        public void switchSource() {
            runOnUiThread(() -> {
                startActivity(ShareListActivity.intent(BrowserActivity.this).putExtra(ShareListActivity.EXTRA_CHOOSE, true));
                finish();
            });
        }

        @JavascriptInterface
        public boolean updateAvailable() {
            return AppUpdate.available(BrowserActivity.this) != null;
        }

        @JavascriptInterface
        public String version() {
            return AppUpdate.installedVersion(BrowserActivity.this);
        }

        @JavascriptInterface
        public void setPlaying(boolean playing) {
            runOnUiThread(() -> {
                if (playing && prefs.keepScreenOn()) {
                    getWindow().addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
                } else {
                    getWindow().clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);
                }
            });
        }

        @JavascriptInterface
        public String diagnosis() {
            return SettingsActivity.diagnosis(BrowserActivity.this, share);
        }
    }

    // ---- App-Update ----

    private void checkForAppUpdate() {
        if (updateChecked) return;
        updateChecked = true;
        AppUpdate.check(this, false, (latest, error) -> {
            if (isFinishing() || web == null) return;
            web.evaluateJavascript("window.sflixAppRefresh && sflixAppRefresh();", null);
            String available = AppUpdate.available(this);
            if (available != null && !available.equals(prefs.dismissedVersion())) {
                AppUpdate.offer(this, available, true);
            }
        });
    }

    // ---- Verbindungsfehler ----

    /** Beim ersten Laden: zurück zur Auswahl der Quellen, mit Hinweis. */
    private void backToSources(String reason) {
        runOnUiThread(() -> {
            if (isFinishing()) return;
            startActivity(ShareListActivity.intent(this)
                    .putExtra(ShareListActivity.EXTRA_ERROR, reason)
                    .putExtra(ShareListActivity.EXTRA_ERROR_ID, share.id));
            finish();
        });
    }

    /** Während der Nutzung: Hinweis mit "Erneut versuchen" und "Zur Quellenauswahl". */
    private void showConnectionLost(String reason) {
        runOnUiThread(() -> {
            if (isFinishing() || (connectionDialog != null && connectionDialog.isShowing())) return;
            connectionDialog = new AlertDialog.Builder(this)
                    .setTitle("Verbindung unterbrochen")
                    .setMessage("Die Verbindung zu " + share.label() + " ist unterbrochen.\n" + reason)
                    .setPositiveButton("Erneut versuchen", (d, w) -> {
                        cache.invalidate();
                        new Thread(smb::close).start();
                        web.reload();
                    })
                    .setNegativeButton("Zur Quellenauswahl", (d, w) -> {
                        startActivity(ShareListActivity.intent(this).putExtra(ShareListActivity.EXTRA_CHOOSE, true));
                        finish();
                    })
                    .show();
        });
    }

    @Override
    public void onMediaConnectionLost(Exception e) {
        showConnectionLost(SmbSource.reason(e));
    }

    /** Mehrere fehlgeschlagene Dateien kurz hintereinander: Verbindung ist weg. */
    private void subresourceFailed(Exception e) {
        long now = System.currentTimeMillis();
        boolean lost;
        synchronized (recentFailures) {
            recentFailures[failureIndex] = now;
            failureIndex = (failureIndex + 1) % recentFailures.length;
            long oldest = recentFailures[failureIndex];
            lost = oldest > 0 && now - oldest < 20_000;
        }
        cache.invalidate();
        if (lost && everLoaded) showConnectionLost(SmbSource.reason(e));
    }

    @SuppressWarnings("deprecation")
    private void setFullscreen(boolean on) {
        getWindow().getDecorView().setSystemUiVisibility(on
                ? View.SYSTEM_UI_FLAG_FULLSCREEN | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY | View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                : View.SYSTEM_UI_FLAG_VISIBLE);
    }

    private final class Chrome extends WebChromeClient {
        // Ohne eigenes Bild zeigt die WebView vor dem Start ein graues Bild mit Play-Symbol,
        // das über dem STEFFLIX-Player liegt. Ein durchsichtiges Bild blendet es aus.
        @Override
        public android.graphics.Bitmap getDefaultVideoPoster() {
            return android.graphics.Bitmap.createBitmap(1, 1, android.graphics.Bitmap.Config.ARGB_8888);
        }

        @Override
        public void onShowCustomView(View view, CustomViewCallback callback) {
            if (customView != null) {
                callback.onCustomViewHidden();
                return;
            }
            customView = view;
            customCallback = callback;
            root.addView(view, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.MATCH_PARENT));
            web.setVisibility(View.GONE);
            setFullscreen(true);
            if (prefs.landscapeFullscreen()) {
                setRequestedOrientation(ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE);
            }
        }

        @Override
        public void onHideCustomView() {
            hideCustomView();
        }
    }

    private final class Client extends WebViewClient {
        @Override
        public void onPageFinished(WebView view, String url) {
            pageReady = true;
            if (!mainLoadFailed) everLoaded = true;
            LoadStats.finish(url != null && url.contains("?") ? "Seite" : "Startseite");
            hideSplash(false);
            // Ohne Startanimation (z.B. nach dem Wiederherstellen) hier auf ein App-Update prüfen
            if (splash == null && everLoaded) checkForAppUpdate();
        }

        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            Uri u = request.getUrl();
            if (HOST.equals(u.getHost())) return false;
            // Fremde Seiten (z.B. GitHub aus der Hilfe) im normalen Browser oeffnen
            String scheme = u.getScheme();
            if ("http".equals(scheme) || "https".equals(scheme)) {
                try {
                    startActivity(new Intent(Intent.ACTION_VIEW, u));
                } catch (Exception ignored) {
                }
            }
            return true;
        }

        @Override
        public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
            Uri u = request.getUrl();
            if (!HOST.equals(u.getHost())) return null;
            String path = u.getPath() == null ? "" : u.getPath();
            if (!path.startsWith(prefix)) return status(404, "Not Found");
            String rel = path.substring(prefix.length());
            if (rel.isEmpty()) rel = "start.html";
            String range = null;
            for (Map.Entry<String, String> h : request.getRequestHeaders().entrySet()) {
                if ("range".equalsIgnoreCase(h.getKey())) range = h.getValue();
            }
            return serve(rel, range, request.isForMainFrame());
        }
    }

    private static WebResourceResponse bytesResponse(String mime, String charset, byte[] data) {
        Map<String, String> h = new HashMap<>();
        h.put("Access-Control-Allow-Origin", "*");
        h.put("Cache-Control", "no-cache");
        h.put("Content-Length", String.valueOf(data.length));
        return new WebResourceResponse(mime, charset, 200, "OK", h, new ByteArrayInputStream(data));
    }

    private WebResourceResponse serve(String rel, String range, boolean mainFrame) {
        String mime = mimeType(rel);
        String charset = mime.startsWith("text/") || mime.endsWith("javascript") || mime.endsWith("json") ? "UTF-8" : null;
        boolean isStart = rel.equalsIgnoreCase("start.html");
        if (mainFrame) {
            mainLoadFailed = false;
            LoadStats.begin();
        }
        try {
            if (range == null) {
                // Kleine und mittlere Dateien (Programmdateien, data.js, Bilder): Zwischenspeicher
                byte[] data = cache.read(rel);
                if (data != null) {
                    return bytesResponse(mime, charset, isStart ? injectIntoStart(data) : data);
                }
            }
            // Grosse Dateien und Bereiche (z.B. Untertitel, Medien ohne MediaServer): direkt lesen
            File f = smb.open(rel);
            long size = SmbSource.size(f);
            int code = 200;
            String reason = "OK";
            Map<String, String> headers = new HashMap<>();
            headers.put("Accept-Ranges", "bytes");
            headers.put("Cache-Control", "no-cache");
            headers.put("Access-Control-Allow-Origin", "*");
            long[] r = parseRange(range, size);
            if (r == null) {
                SmbSource.closeLater(f);
                headers.put("Content-Range", "bytes */" + size);
                return new WebResourceResponse(mime, charset, 416, "Range Not Satisfiable", headers, new ByteArrayInputStream(new byte[0]));
            }
            long start = r[0], end = r[1];
            if (range != null && size > 0) {
                code = 206;
                reason = "Partial Content";
                headers.put("Content-Range", "bytes " + start + "-" + end + "/" + size);
            }
            headers.put("Content-Length", String.valueOf(Math.max(0, end - start + 1)));
            return new WebResourceResponse(mime, charset, code, reason, headers,
                    new SmbSource.RangeStream(f, start, end + 1));
        } catch (FileNotFoundException e) {
            if (mainFrame) {
                mainLoadFailed = true;
                String msg = isStart ? "Im Ordner liegt keine start.html. Ist das der STEFFLIX-Ordner?" : "Es fehlt: " + rel;
                if (!everLoaded) backToSources(msg);
                return errorPage("STEFFLIX wurde in dieser Quelle nicht gefunden.", msg);
            }
            return status(404, "Not Found");
        } catch (IOException | RuntimeException e) {
            if (mainFrame) {
                mainLoadFailed = true;
                cache.invalidate();
                String msg = SmbSource.reason(e);
                if (!everLoaded) backToSources(msg);
                else showConnectionLost(msg);
                return errorPage("Keine Verbindung zur Quelle.", msg);
            }
            subresourceFailed(e);
            return status(502, "Bad Gateway");
        }
    }

    private static WebResourceResponse status(int code, String reason) {
        return new WebResourceResponse("text/plain", "UTF-8", code, reason, new HashMap<>(), new ByteArrayInputStream(new byte[0]));
    }

    private static WebResourceResponse errorPage(String title, String detail) {
        String html = "<!DOCTYPE html><html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1'>"
                + "<style>body{background:#141414;color:#ddd;font-family:sans-serif;padding:24px}h1{color:#e8261a;font-size:22px}"
                + "a{display:inline-block;margin:16px 12px 0 0;padding:12px 20px;background:#e8261a;color:#fff;text-decoration:none;border-radius:4px}"
                + "a.second{background:#333}p{color:#999;word-break:break-word}</style></head><body><h1>" + TextUtils.htmlEncode(title) + "</h1><p>"
                + TextUtils.htmlEncode(detail) + "</p><p>Pr&uuml;fe WLAN bzw. VPN, Pfad, Benutzer und Passwort.</p>"
                + "<a href='javascript:location.reload()'>Erneut versuchen</a>"
                + "<a class='second' href='javascript:StefflixApp.switchSource()'>Zur Quellenauswahl</a></body></html>";
        return new WebResourceResponse("text/html", "UTF-8", 200, "OK", new HashMap<>(),
                new ByteArrayInputStream(html.getBytes(StandardCharsets.UTF_8)));
    }

    /** Liest "bytes=a-b", "bytes=a-" oder "bytes=-n". Ohne Range: ganze Datei. null = nicht erfüllbar. */
    static long[] parseRange(String range, long size) {
        long start = 0, end = size - 1;
        if (range != null && range.startsWith("bytes=") && size > 0) {
            try {
                String r = range.substring(6).split(",")[0].trim();
                int dash = r.indexOf('-');
                String a = r.substring(0, dash).trim(), b = r.substring(dash + 1).trim();
                if (a.isEmpty()) {
                    start = Math.max(0, size - Long.parseLong(b));
                } else {
                    start = Long.parseLong(a);
                    if (!b.isEmpty()) end = Math.min(Long.parseLong(b), size - 1);
                }
            } catch (RuntimeException e) {
                start = 0;
                end = size - 1;
            }
            if (start >= size || start > end) return null;
        }
        return new long[]{start, end};
    }

    static String mimeType(String name) {
        int dot = name.lastIndexOf('.');
        String ext = dot < 0 ? "" : name.substring(dot + 1).toLowerCase(Locale.ROOT);
        switch (ext) {
            case "html": case "htm": return "text/html";
            case "js": return "application/javascript";
            case "css": return "text/css";
            case "json": return "application/json";
            case "txt": return "text/plain";
            case "vtt": return "text/vtt";
            case "svg": return "image/svg+xml";
            case "png": return "image/png";
            case "jpg": case "jpeg": return "image/jpeg";
            case "gif": return "image/gif";
            case "webp": return "image/webp";
            case "bmp": return "image/bmp";
            case "ico": return "image/x-icon";
            case "mp4": case "m4v": return "video/mp4";
            case "webm": return "video/webm";
            case "mkv": return "video/x-matroska";
            case "mp3": return "audio/mpeg";
            case "m4a": case "aac": return "audio/mp4";
            case "ogg": case "oga": return "audio/ogg";
            case "wav": return "audio/wav";
            case "flac": return "audio/flac";
            case "woff": return "font/woff";
            case "woff2": return "font/woff2";
            default: return "application/octet-stream";
        }
    }
}
