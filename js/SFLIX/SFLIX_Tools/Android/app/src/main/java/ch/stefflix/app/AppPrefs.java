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

    /** Musik und Ton von Videos laufen weiter (Bildschirm aus, andere App), mit Benachrichtigung. */
    boolean backgroundPlayback() { return p.getBoolean("backgroundPlayback", true); }
    void setBackgroundPlayback(boolean v) { p.edit().putBoolean("backgroundPlayback", v).apply(); }

    /** Bild-im-Bild-Knopf im Player. */
    boolean pictureInPicture() { return p.getBoolean("pictureInPicture", true); }
    void setPictureInPicture(boolean v) { p.edit().putBoolean("pictureInPicture", v).apply(); }

    /** Ab Android 13 einmal nach Benachrichtigungen gefragt (beim ersten Abspielen). */
    boolean notificationsAsked() { return p.getBoolean("notificationsAsked", false); }
    void setNotificationsAsked(boolean v) { p.edit().putBoolean("notificationsAsked", v).apply(); }

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

    /** Zeitpunkt, zu dem ein Update aus der App installiert wurde (0 = keines). Für das Öffnen danach. */
    long updateStarted() { return p.getLong("updateStarted", 0); }
    void setUpdateStarted(long v) { p.edit().putLong("updateStarted", v).commit(); }

    /** Version, für die der Hinweis mit "Später" geschlossen wurde. */
    String dismissedVersion() { return p.getString("dismissedVersion", ""); }
    void setDismissedVersion(String v) { p.edit().putString("dismissedVersion", v).apply(); }
}
