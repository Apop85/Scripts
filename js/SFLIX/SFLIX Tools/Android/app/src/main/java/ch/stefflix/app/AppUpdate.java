package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageInstaller;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.provider.Settings;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.Locale;

/**
 * Update der App: versions.json auf GitHub nennt Version und SHA256 der STEFFLIX.apk (schreibt
 * release.ps1). Die App prüft höchstens einmal am Tag, lädt die APK auf Wunsch, kontrolliert die
 * Prüfsumme und übergibt sie dem Android-Installer (PackageInstaller, ohne fremde Bibliotheken).
 */
final class AppUpdate {

    static final String VERSIONS_URL = "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/versions.json";
    static final String APK_URL = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/STEFFLIX.apk";
    private static final long DAY_MS = 24L * 60 * 60 * 1000;

    interface Checked {
        /** latest: neueste Version oder "" (keine Angabe). error: null oder Grund. */
        void onChecked(String latest, String error);
    }

    private AppUpdate() {
    }

    static String installedVersion(Context c) {
        try {
            return c.getPackageManager().getPackageInfo(c.getPackageName(), 0).versionName;
        } catch (PackageManager.NameNotFoundException e) {
            return "0.0.0";
        }
    }

    static int compare(String a, String b) {
        String[] x = a.split("\\."), y = b.split("\\.");
        for (int i = 0; i < Math.max(x.length, y.length); i++) {
            int p = i < x.length ? parse(x[i]) : 0, q = i < y.length ? parse(y[i]) : 0;
            if (p != q) return Integer.compare(p, q);
        }
        return 0;
    }

    private static int parse(String s) {
        try {
            return Integer.parseInt(s.trim());
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    /** Bekannte neuere Version (aus der letzten Prüfung) oder null. */
    static String available(Context c) {
        String latest = new AppPrefs(c).latestVersion();
        return !latest.isEmpty() && compare(latest, installedVersion(c)) > 0 ? latest : null;
    }

    /** Prüft im Hintergrund. force = false: nur, wenn die letzte Prüfung über einen Tag her ist. */
    static void check(Context c, boolean force, Checked done) {
        AppPrefs prefs = new AppPrefs(c);
        if (!force && System.currentTimeMillis() - prefs.lastUpdateCheck() < DAY_MS) {
            if (done != null) done.onChecked(prefs.latestVersion(), null);
            return;
        }
        Handler ui = new Handler(Looper.getMainLooper());
        new Thread(() -> {
            String latest = "", error = null;
            try {
                JSONObject o = new JSONObject(new String(download(VERSIONS_URL + "?t=" + System.currentTimeMillis(), 64 * 1024), StandardCharsets.UTF_8));
                JSONObject app = o.optJSONObject("android");
                if (app != null) {
                    latest = app.optString("version", "");
                    prefs.setLatest(latest, app.optString("sha256", "").toLowerCase(Locale.ROOT));
                } else {
                    prefs.setLatest("", "");
                }
            } catch (Exception e) {
                error = "Keine Verbindung zu GitHub (" + e.getClass().getSimpleName() + ")";
            }
            final String l = latest, err = error;
            if (done != null) ui.post(() -> done.onChecked(l, err));
        }).start();
    }

    private static byte[] download(String url, long max) throws IOException {
        HttpURLConnection con = (HttpURLConnection) new URL(url).openConnection();
        con.setConnectTimeout(15000);
        con.setReadTimeout(30000);
        con.setInstanceFollowRedirects(true);
        try {
            int code = con.getResponseCode();
            if (code != 200) throw new IOException("HTTP " + code);
            try (InputStream in = con.getInputStream()) {
                ByteArrayOutputStream out = new ByteArrayOutputStream();
                byte[] buf = new byte[64 * 1024];
                int n;
                while ((n = in.read(buf)) > 0) {
                    out.write(buf, 0, n);
                    if (out.size() > max) throw new IOException("Datei zu gross");
                }
                return out.toByteArray();
            }
        } finally {
            con.disconnect();
        }
    }

    /** Hinweis auf eine neue Version mit "Jetzt aktualisieren" und "Später". */
    static void offer(Activity a, String version, boolean remindLater) {
        new AlertDialog.Builder(a)
                .setTitle("STEFFLIX-App " + version + " ist verfügbar")
                .setMessage("Installiert ist Version " + installedVersion(a) + ". Das Update wird heruntergeladen, geprüft und installiert. Quellen und gespeicherte Daten bleiben erhalten.")
                .setPositiveButton("Jetzt aktualisieren", (d, w) -> install(a))
                .setNegativeButton("Später", (d, w) -> {
                    if (remindLater) new AppPrefs(a).setDismissedVersion(version);
                })
                .show();
    }

    /** Lädt die APK, prüft sie und startet die Installation. */
    static void install(Activity a) {
        if (Build.VERSION.SDK_INT >= 26 && !a.getPackageManager().canRequestPackageInstalls()) {
            new AlertDialog.Builder(a)
                    .setTitle("Installation erlauben")
                    .setMessage("Damit die App sich selbst aktualisieren kann, bei STEFFLIX \"Aus dieser Quelle zulassen\" einschalten und danach nochmals \"Jetzt aktualisieren\" wählen.")
                    .setPositiveButton("Einstellungen öffnen", (d, w) -> {
                        try {
                            a.startActivity(new Intent(Settings.ACTION_MANAGE_UNKNOWN_APP_SOURCES,
                                    Uri.parse("package:" + a.getPackageName())));
                        } catch (Exception ignored) {
                        }
                    })
                    .setNegativeButton("Abbrechen", null)
                    .show();
            return;
        }
        AlertDialog wait = new AlertDialog.Builder(a)
                .setMessage("Update wird heruntergeladen …")
                .setCancelable(false)
                .show();
        String expected = new AppPrefs(a).latestSha256();
        Handler ui = new Handler(Looper.getMainLooper());
        Context app = a.getApplicationContext();
        new Thread(() -> {
            String error = null;
            try {
                byte[] apk = download(APK_URL + "?t=" + System.currentTimeMillis(), 200L * 1024 * 1024);
                String sha = sha256(apk);
                if (expected.isEmpty() || !expected.equals(sha)) {
                    error = "Die heruntergeladene Datei stimmt nicht mit der erwarteten Prüfsumme überein. Bitte später nochmals versuchen.";
                } else {
                    PackageInstaller installer = app.getPackageManager().getPackageInstaller();
                    PackageInstaller.SessionParams params = new PackageInstaller.SessionParams(PackageInstaller.SessionParams.MODE_FULL_INSTALL);
                    params.setAppPackageName(app.getPackageName());
                    int id = installer.createSession(params);
                    try (PackageInstaller.Session session = installer.openSession(id)) {
                        try (OutputStream out = session.openWrite("STEFFLIX.apk", 0, apk.length)) {
                            out.write(apk);
                            session.fsync(out);
                        }
                        Intent result = new Intent(app, InstallReceiver.class);
                        PendingIntent pi = PendingIntent.getBroadcast(app, id, result,
                                PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_MUTABLE);
                        session.commit(pi.getIntentSender());
                    }
                }
            } catch (Exception e) {
                error = "Update fehlgeschlagen: " + e.getMessage();
            }
            final String err = error;
            ui.post(() -> {
                wait.dismiss();
                if (err != null && !a.isFinishing()) {
                    new AlertDialog.Builder(a).setTitle("Update").setMessage(err).setPositiveButton("OK", null).show();
                }
            });
        }).start();
    }

    private static String sha256(byte[] data) throws Exception {
        byte[] h = MessageDigest.getInstance("SHA-256").digest(data);
        StringBuilder sb = new StringBuilder();
        for (byte b : h) sb.append(String.format(Locale.ROOT, "%02x", b));
        return sb.toString();
    }

    /** Rückmeldung des Installers: Bestätigung anzeigen oder Fehler melden. */
    public static final class InstallReceiver extends BroadcastReceiver {
        @Override
        public void onReceive(Context context, Intent intent) {
            int status = intent.getIntExtra(PackageInstaller.EXTRA_STATUS, PackageInstaller.STATUS_FAILURE);
            if (status == PackageInstaller.STATUS_PENDING_USER_ACTION) {
                Intent confirm;
                if (Build.VERSION.SDK_INT >= 33) {
                    confirm = intent.getParcelableExtra(Intent.EXTRA_INTENT, Intent.class);
                } else {
                    //noinspection deprecation
                    confirm = intent.getParcelableExtra(Intent.EXTRA_INTENT);
                }
                if (confirm != null) {
                    confirm.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    context.startActivity(confirm);
                }
            } else if (status != PackageInstaller.STATUS_SUCCESS) {
                String msg = intent.getStringExtra(PackageInstaller.EXTRA_STATUS_MESSAGE);
                Toast.makeText(context, "Update nicht installiert" + (msg != null ? ": " + msg : ""), Toast.LENGTH_LONG).show();
            }
        }
    }
}
