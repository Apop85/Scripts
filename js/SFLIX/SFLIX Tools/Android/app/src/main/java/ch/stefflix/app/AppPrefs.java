package ch.stefflix.app;

import android.content.Context;
import android.content.SharedPreferences;

/** Einstellungen der App (App-Einstellungen) und Zustand der Update-Prüfung. */
final class AppPrefs {

    static final int[] CACHE_CHOICES_MB = {100, 250, 500};
    static final int[] BUFFER_CHOICES_MB = {32, 64, 128};

    private final SharedPreferences p;

    AppPrefs(Context c) {
        p = c.getApplicationContext().getSharedPreferences("app", Context.MODE_PRIVATE);
    }

    boolean keepScreenOn() { return p.getBoolean("keepScreenOn", true); }
    void setKeepScreenOn(boolean v) { p.edit().putBoolean("keepScreenOn", v).apply(); }

    boolean landscapeFullscreen() { return p.getBoolean("landscapeFullscreen", true); }
    void setLandscapeFullscreen(boolean v) { p.edit().putBoolean("landscapeFullscreen", v).apply(); }

    /** Zwischenspeicher auf dem Gerät (Programmdateien und Vorschaubilder), alle Quellen zusammen. */
    int cacheMb() { return p.getInt("cacheMb", 250); }
    void setCacheMb(int v) { p.edit().putInt("cacheMb", v).apply(); }

    /** Videopuffer im Arbeitsspeicher je laufendem Video bzw. Musikstück. */
    int bufferMb() { return p.getInt("bufferMb", 64); }
    void setBufferMb(int v) { p.edit().putInt("bufferMb", v).apply(); }

    // ---- Update-Prüfung ----
    long lastUpdateCheck() { return p.getLong("updateChecked", 0); }
    String latestVersion() { return p.getString("latestVersion", ""); }
    String latestSha256() { return p.getString("latestSha256", ""); }

    void setLatest(String version, String sha256) {
        p.edit().putLong("updateChecked", System.currentTimeMillis())
                .putString("latestVersion", version)
                .putString("latestSha256", sha256).apply();
    }

    /** Version, für die der Hinweis mit "Später" geschlossen wurde. */
    String dismissedVersion() { return p.getString("dismissedVersion", ""); }
    void setDismissedVersion(String v) { p.edit().putString("dismissedVersion", v).apply(); }
}
