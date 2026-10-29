package ch.stefflix.app;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import android.text.TextUtils;
import android.util.LruCache;
import android.view.View;
import android.view.ViewGroup;
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
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

/**
 * Zeigt STEFFLIX aus einer Freigabe an: WebView ohne Adresszeile. Alle Anfragen an HOST werden
 * direkt aus der Freigabe beantwortet (mit Range fuer das Springen in Videos), ohne Server.
 * Alle Freigaben teilen sich eine Herkunft; STEFFLIX trennt den Speicher ueber install.js.
 */
public class BrowserActivity extends Activity {

    static final String EXTRA_ID = "id";
    private static final String HOST = "appassets.androidplatform.net";

    private SmbSource smb;
    private String prefix;
    private FrameLayout root;
    private WebView web;
    private View customView;
    private WebChromeClient.CustomViewCallback customCallback;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        ShareStore.Share share = new ShareStore(this).find(getIntent().getStringExtra(EXTRA_ID));
        if (share == null) {
            finish();
            return;
        }
        smb = new SmbSource(share);
        prefix = "/" + share.id + "/";

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
    }

    @Override
    protected void onResume() {
        super.onResume();
        if (web != null) web.onResume();
    }

    @Override
    protected void onDestroy() {
        if (web != null) web.destroy();
        if (smb != null) {
            final SmbSource s = smb;
            new Thread(s::close).start();
        }
        super.onDestroy();
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
    }

    /** Schaltet den Splashscreen der Weboberflaeche ab (die App zeigt ihren eigenen).
     *  An Tagen, an denen STEFFLIX einen besonderen Splashscreen hat, zeigt ihn die Seite selbst. */
    private static final String INJECT = "<script>window.stefflixApp=true;(function(){"
            + "if(typeof showSplashscreen!='function')return;var o=showSplashscreen;"
            + "showSplashscreen=function(){if(typeof getSplashDedication=='function'&&getSplashDedication(new Date())!=null){"
            + "o();if(window.StefflixApp)StefflixApp.webSplash();}};})();</script>\n";

    private static byte[] injectIntoStart(byte[] html) {
        String s = new String(html, StandardCharsets.UTF_8);
        int i = s.indexOf("<script src=\"sflix_sys/js/main.js\"");
        if (i < 0) return html;
        return (s.substring(0, i) + INJECT + s.substring(i)).getBytes(StandardCharsets.UTF_8);
    }

    private final class Bridge {
        @JavascriptInterface
        public void webSplash() {
            runOnUiThread(() -> hideSplash(true));
        }
    }

    @SuppressWarnings("deprecation")
    private void setFullscreen(boolean on) {
        getWindow().getDecorView().setSystemUiVisibility(on
                ? View.SYSTEM_UI_FLAG_FULLSCREEN | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY | View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                : View.SYSTEM_UI_FLAG_VISIBLE);
    }

    private final class Chrome extends WebChromeClient {
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
            hideSplash(false);
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

    // Bilder (v.a. Vorschaubilder) bleiben im Speicher, solange die App läuft. Die WebView speichert
    // abgefangene Antworten nicht und würde sonst bei jedem Seitenwechsel alles neu von der Freigabe lesen.
    private static final int IMAGE_CACHE_MAX_FILE = 2 * 1024 * 1024;
    private static final LruCache<String, byte[]> imageCache = new LruCache<String, byte[]>(48 * 1024 * 1024) {
        @Override
        protected int sizeOf(String key, byte[] value) {
            return value.length;
        }
    };

    private WebResourceResponse imageResponse(String mime, byte[] data) {
        Map<String, String> h = new HashMap<>();
        h.put("Access-Control-Allow-Origin", "*");
        h.put("Content-Length", String.valueOf(data.length));
        return new WebResourceResponse(mime, null, 200, "OK", h, new ByteArrayInputStream(data));
    }

    private static byte[] readAll(File f, long size) throws IOException {
        byte[] data = new byte[(int) size];
        int read = 0;
        try (SmbSource.RangeStream in = new SmbSource.RangeStream(f, 0, size)) {
            int n;
            while (read < data.length && (n = in.read(data, read, data.length - read)) > 0) read += n;
        }
        if (read < data.length) throw new IOException("Datei unvollständig gelesen");
        return data;
    }

    private WebResourceResponse serve(String rel, String range, boolean mainFrame) {
        String mime = mimeType(rel);
        String charset = mime.startsWith("text/") || mime.endsWith("javascript") || mime.endsWith("json") ? "UTF-8" : null;
        boolean image = mime.startsWith("image/") && range == null;
        String cacheKey = prefix + rel;
        if (image) {
            byte[] cached = imageCache.get(cacheKey);
            if (cached != null) return imageResponse(mime, cached);
        }
        try {
            File f = smb.open(rel);
            long size = SmbSource.size(f);
            if (image && size <= IMAGE_CACHE_MAX_FILE) {
                byte[] data = readAll(f, size);
                imageCache.put(cacheKey, data);
                return imageResponse(mime, data);
            }
            if (rel.equalsIgnoreCase("start.html")) {
                byte[] html = readAll(f, size);
                Map<String, String> h = new HashMap<>();
                h.put("Cache-Control", "no-cache");
                return new WebResourceResponse(mime, charset, 200, "OK", h,
                        new ByteArrayInputStream(injectIntoStart(html)));
            }
            long start = 0, end = size - 1;
            int code = 200;
            String reason = "OK";
            Map<String, String> headers = new HashMap<>();
            headers.put("Accept-Ranges", "bytes");
            headers.put("Cache-Control", "no-cache");
            headers.put("Access-Control-Allow-Origin", "*");
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
                if (start >= size || start > end) {
                    f.close();
                    headers.put("Content-Range", "bytes */" + size);
                    return new WebResourceResponse(mime, charset, 416, "Range Not Satisfiable", headers, new ByteArrayInputStream(new byte[0]));
                }
                code = 206;
                reason = "Partial Content";
                headers.put("Content-Range", "bytes " + start + "-" + end + "/" + size);
            }
            headers.put("Content-Length", String.valueOf(Math.max(0, end - start + 1)));
            // Der Strom beginnt immer am Dateianfang: Die WebView überspringt bei Range-Anfragen
            // selbst bis zum Start (InputStream.skip). Begänne er schon bei "start", würde doppelt
            // gesprungen (Spulen falsch, MP4 mit Inhaltsverzeichnis am Ende startet gar nicht).
            return new WebResourceResponse(mime, charset, code, reason, headers,
                    new SmbSource.RangeStream(f, 0, end + 1));
        } catch (FileNotFoundException e) {
            if (mainFrame) return errorPage("STEFFLIX wurde in dieser Freigabe nicht gefunden.", "Es fehlt: " + rel);
            return status(404, "Not Found");
        } catch (IOException | RuntimeException e) {
            if (mainFrame) return errorPage("Keine Verbindung zur Freigabe.", String.valueOf(e.getMessage()));
            return status(502, "Bad Gateway");
        }
    }

    private static WebResourceResponse status(int code, String reason) {
        return new WebResourceResponse("text/plain", "UTF-8", code, reason, new HashMap<>(), new ByteArrayInputStream(new byte[0]));
    }

    private static WebResourceResponse errorPage(String title, String detail) {
        String html = "<!DOCTYPE html><html><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1'>"
                + "<style>body{background:#141414;color:#ddd;font-family:sans-serif;padding:24px}h1{color:#e8261a;font-size:22px}"
                + "a{display:inline-block;margin-top:16px;padding:10px 18px;background:#e8261a;color:#fff;text-decoration:none;border-radius:4px}"
                + "p{color:#999;word-break:break-word}</style></head><body><h1>" + TextUtils.htmlEncode(title) + "</h1><p>"
                + TextUtils.htmlEncode(detail) + "</p><p>Pr&uuml;fe WLAN, Pfad, Benutzer und Passwort.</p>"
                + "<a href='javascript:location.reload()'>Erneut versuchen</a></body></html>";
        return new WebResourceResponse("text/html", "UTF-8", 200, "OK", new HashMap<>(),
                new ByteArrayInputStream(html.getBytes(StandardCharsets.UTF_8)));
    }

    private static String mimeType(String name) {
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
