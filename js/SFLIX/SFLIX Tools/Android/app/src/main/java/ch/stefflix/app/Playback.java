package ch.stefflix.app;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.drawable.Icon;
import android.media.AudioManager;
import android.media.MediaMetadata;
import android.media.session.MediaSession;
import android.media.session.PlaybackState;
import android.net.Uri;
import android.net.wifi.WifiManager;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.os.PowerManager;
import android.os.SystemClock;

import org.json.JSONObject;

/**
 * Wiedergabe von Videos und Musik aus Sicht von Android. STEFFLIX meldet den Zustand über inject.js
 * (StefflixApp.mediaState), Befehle gehen über Host.mediaCommand zurück an die Seite (sflixAppMedia).
 * <ul>
 * <li>MediaSession: Steuerung auf dem Sperrbildschirm, in der Benachrichtigung und über Bluetooth.</li>
 * <li>Benachrichtigung mit Zurück, 10 s zurück, Abspielen/Pause, 10 s vor, Weiter (nur mit "Wiedergabe im
 *     Hintergrund"); während der Wiedergabe über PlaybackService im Vordergrund, nach einer Pause gelöst,
 *     dann lässt sie sich wegwischen (= Beenden).</li>
 * <li>Pause beim Trennen der Kopfhörer. Den Audiofokus (Pause bei Anruf, danach weiter) holt die WebView
 *     selbst; eine eigene Anfrage nähme ihn ihr weg und sie würde sofort pausieren.</li>
 * <li>Wakelock und WLAN-Lock während der Wiedergabe im Hintergrund, damit der Strom vom NAS nicht abbricht.</li>
 * </ul>
 * Alle Methoden laufen auf dem UI-Thread.
 */
final class Playback {

    static final String CMD_PLAY = "play";
    static final String CMD_PAUSE = "pause";
    static final String CMD_TOGGLE = "toggle";
    static final String CMD_NEXT = "next";
    static final String CMD_PREV = "prev";
    static final String CMD_BACK = "back";
    static final String CMD_FORWARD = "forward";
    static final String CMD_SEEK = "seek";
    /** Pause und Benachrichtigung entfernen (Wegwischen der Benachrichtigung). */
    static final String CMD_STOP = "stop";
    /** App weggewischt: Pause, Benachrichtigung weg, Seite schliessen. */
    static final String CMD_QUIT = "quit";

    static final String EXTRA_CMD = "cmd";
    static final int NOTIFICATION_ID = 7;
    private static final String CHANNEL = "playback";
    /** Nach einer Pause so lange im Vordergrund bleiben: Beim Wechsel zum nächsten Medium (Autoplay)
     *  pausiert die Seite kurz, und aus dem Hintergrund darf der Dienst nicht neu starten. */
    private static final long DETACH_DELAY_MS = 15_000;
    private static final long WAKE_LOCK_MS = 6 * 60 * 60 * 1000L;
    private static final int ARTWORK_MAX_PX = 512;

    interface Host {
        /** Befehl an die Seite (sflixAppMedia in inject.js). */
        void mediaCommand(String cmd, long arg);

        /** Zustand geändert (Knöpfe im Bild-im-Bild-Fenster nachführen). */
        void playbackChanged();

        /** App weggewischt. */
        void quit();
    }

    private static Playback current;

    private final Context context;
    private final Host host;
    private final AppPrefs prefs;
    private final FileCache cache;
    private final String shareId;
    private final String prefix;
    private final Handler ui = new Handler(Looper.getMainLooper());
    private final MediaSession session;
    private final NotificationManager notifications;
    private final PowerManager.WakeLock wakeLock;
    private final WifiManager.WifiLock wifiLock;

    private boolean active, playing, video, hasNext, hasPrev;
    private long position, duration;
    private float rate = 1f;
    private int width, height;
    private String title = "", subtitle = "", artworkRel = "", metadataKey = "";
    private Bitmap artwork;
    private Notification lastNotification;

    private boolean noisyRegistered;
    private boolean notificationShown, dismissed, foreground, released;

    private final Runnable detach = this::detachForeground;

    private final BroadcastReceiver noisy = new BroadcastReceiver() {
        @Override
        public void onReceive(Context c, Intent intent) {
            // Kopfhörer getrennt (Kabel oder Bluetooth): nicht über den Lautsprecher weiterspielen
            if (playing) host.mediaCommand(CMD_PAUSE, 0);
        }
    };

    Playback(Context context, Host host, AppPrefs prefs, FileCache cache, String shareId) {
        this.context = context;
        this.host = host;
        this.prefs = prefs;
        this.cache = cache;
        this.shareId = shareId;
        this.prefix = "/" + shareId + "/";
        notifications = context.getSystemService(NotificationManager.class);
        session = new MediaSession(context, "STEFFLIX");
        session.setCallback(new Callback(), ui);
        session.setSessionActivity(openIntent());
        PowerManager pm = (PowerManager) context.getSystemService(Context.POWER_SERVICE);
        wakeLock = pm.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, "stefflix:playback");
        wakeLock.setReferenceCounted(false);
        wifiLock = createWifiLock(context);
        createChannel(context);
        current = this;
    }

    // ---- Zustand aus STEFFLIX ----

    boolean isActive() { return active; }
    boolean isPlaying() { return playing; }
    boolean isVideo() { return video; }
    int videoWidth() { return width; }
    int videoHeight() { return height; }

    /** Zustand aus inject.js (mediaState): active, playing, video, position/duration (ms), rate,
     *  title, subtitle, artwork (Pfad in der Seite), hasNext, hasPrev, width, height. */
    void update(JSONObject s) {
        if (released) return;
        if (!s.optBoolean("active")) {
            deactivate();
            host.playbackChanged();
            return;
        }
        active = true;
        playing = s.optBoolean("playing");
        video = s.optBoolean("video");
        position = Math.max(0, s.optLong("position"));
        duration = Math.max(0, s.optLong("duration"));
        rate = (float) s.optDouble("rate", 1);
        if (!(rate > 0)) rate = 1f;
        hasNext = s.optBoolean("hasNext");
        hasPrev = s.optBoolean("hasPrev");
        width = s.optInt("width");
        height = s.optInt("height");
        title = s.optString("title", "");
        subtitle = s.optString("subtitle", "");
        String art = toRelative(s.optString("artwork", ""));
        if (!art.equals(artworkRel)) loadArtwork(art);

        if (playing) dismissed = false;
        setNoisy(playing);
        updateLocks();
        session.setActive(true);
        pushMetadata();
        pushState();
        updateNotification();
        host.playbackChanged();
    }

    /** Einstellungen geändert (Wiedergabe im Hintergrund) oder Benachrichtigungen erlaubt. */
    void refresh() {
        if (released) return;
        updateLocks();
        updateNotification();
    }

    void release() {
        if (released) return;
        deactivate();
        released = true;
        session.release();
        if (current == this) current = null;
    }

    private void deactivate() {
        active = false;
        playing = false;
        setNoisy(false);
        updateLocks();
        removeNotification();
        if (!released) {
            session.setPlaybackState(new PlaybackState.Builder()
                    .setState(PlaybackState.STATE_NONE, 0, 0f, SystemClock.elapsedRealtime()).build());
            session.setActive(false);
        }
        artworkRel = "";
        artwork = null;
        metadataKey = "";
    }

    /** "/<Quelle>/Ordner/Preview/Bild%20x.jpg" → "Ordner/Preview/Bild x.jpg" (wie in FileCache). */
    private String toRelative(String path) {
        if (path == null || !path.startsWith(prefix)) return "";
        return Uri.decode(path.substring(prefix.length()));
    }

    // ---- Befehle ----

    /** Aus Benachrichtigung, Bild-im-Bild-Fenster und Dienst (UI-Thread). */
    static void dispatch(Context c, String cmd) {
        Playback p = current;
        if (p != null && !p.released) {
            p.command(cmd, 0);
        } else {
            // Seite schon geschlossen: übrig gebliebene Benachrichtigung entfernen
            PlaybackService.remove();
            c.getSystemService(NotificationManager.class).cancel(NOTIFICATION_ID);
        }
    }

    private void command(String cmd, long arg) {
        if (CMD_STOP.equals(cmd) || CMD_QUIT.equals(cmd)) {
            if (playing) host.mediaCommand(CMD_PAUSE, 0);
            dismissed = true;
            removeNotification();
            if (CMD_QUIT.equals(cmd)) host.quit();
            return;
        }
        if (CMD_PLAY.equals(cmd) || CMD_TOGGLE.equals(cmd)) dismissed = false;
        host.mediaCommand(cmd, arg);
    }

    private final class Callback extends MediaSession.Callback {
        @Override public void onPlay() { command(CMD_PLAY, 0); }
        @Override public void onPause() { command(CMD_PAUSE, 0); }
        @Override public void onSkipToNext() { command(CMD_NEXT, 0); }
        @Override public void onSkipToPrevious() { command(CMD_PREV, 0); }
        @Override public void onFastForward() { command(CMD_FORWARD, 0); }
        @Override public void onRewind() { command(CMD_BACK, 0); }
        @Override public void onSeekTo(long pos) { command(CMD_SEEK, pos); }
        @Override public void onStop() { command(CMD_STOP, 0); }
        @Override public void onCustomAction(String action, Bundle extras) { command(action, 0); }
    }

    // ---- Kopfhörer ----

    private void setNoisy(boolean on) {
        if (on == noisyRegistered) return;
        if (on) {
            IntentFilter filter = new IntentFilter(AudioManager.ACTION_AUDIO_BECOMING_NOISY);
            if (Build.VERSION.SDK_INT >= 33) {
                context.registerReceiver(noisy, filter, Context.RECEIVER_NOT_EXPORTED);
            } else {
                context.registerReceiver(noisy, filter);
            }
        } else {
            try {
                context.unregisterReceiver(noisy);
            } catch (IllegalArgumentException ignored) {
            }
        }
        noisyRegistered = on;
    }

    // ---- Wakelock und WLAN ----

    @SuppressWarnings("deprecation")
    private static WifiManager.WifiLock createWifiLock(Context c) {
        WifiManager wm = (WifiManager) c.getApplicationContext().getSystemService(Context.WIFI_SERVICE);
        if (wm == null) return null;
        WifiManager.WifiLock lock = wm.createWifiLock(WifiManager.WIFI_MODE_FULL_HIGH_PERF, "stefflix:playback");
        lock.setReferenceCounted(false);
        return lock;
    }

    private void updateLocks() {
        boolean want = playing && !released && prefs.backgroundPlayback();
        try {
            if (want) {
                wakeLock.acquire(WAKE_LOCK_MS);
                if (wifiLock != null && !wifiLock.isHeld()) wifiLock.acquire();
            } else {
                if (wakeLock.isHeld()) wakeLock.release();
                if (wifiLock != null && wifiLock.isHeld()) wifiLock.release();
            }
        } catch (RuntimeException ignored) {
        }
    }

    // ---- MediaSession ----

    private void pushMetadata() {
        String key = title + "\n" + subtitle + "\n" + duration + "\n" + System.identityHashCode(artwork);
        if (key.equals(metadataKey)) return;
        metadataKey = key;
        MediaMetadata.Builder b = new MediaMetadata.Builder()
                .putString(MediaMetadata.METADATA_KEY_TITLE, title)
                .putString(MediaMetadata.METADATA_KEY_DISPLAY_TITLE, title)
                .putString(MediaMetadata.METADATA_KEY_ARTIST, subtitle)
                .putString(MediaMetadata.METADATA_KEY_DISPLAY_SUBTITLE, subtitle)
                .putLong(MediaMetadata.METADATA_KEY_DURATION, duration > 0 ? duration : -1);
        if (artwork != null) {
            b.putBitmap(MediaMetadata.METADATA_KEY_ART, artwork);
            b.putBitmap(MediaMetadata.METADATA_KEY_ALBUM_ART, artwork);
        }
        session.setMetadata(b.build());
    }

    private void pushState() {
        long actions = PlaybackState.ACTION_PLAY | PlaybackState.ACTION_PAUSE | PlaybackState.ACTION_PLAY_PAUSE
                | PlaybackState.ACTION_STOP | PlaybackState.ACTION_REWIND | PlaybackState.ACTION_FAST_FORWARD;
        if (duration > 0) actions |= PlaybackState.ACTION_SEEK_TO;
        if (hasNext) actions |= PlaybackState.ACTION_SKIP_TO_NEXT;
        if (hasPrev) actions |= PlaybackState.ACTION_SKIP_TO_PREVIOUS;
        // Ab Android 13 baut das System die Steuerung aus diesen Angaben: Zurück, Abspielen, Weiter
        // und zwei eigene Knöpfe (10 s zurück/vor)
        PlaybackState state = new PlaybackState.Builder()
                .setActions(actions)
                .setState(playing ? PlaybackState.STATE_PLAYING : PlaybackState.STATE_PAUSED,
                        position, playing ? rate : 0f, SystemClock.elapsedRealtime())
                .addCustomAction(new PlaybackState.CustomAction.Builder(CMD_BACK, "10 s zurück", R.drawable.ic_replay).build())
                .addCustomAction(new PlaybackState.CustomAction.Builder(CMD_FORWARD, "10 s vor", R.drawable.ic_forward).build())
                .build();
        session.setPlaybackState(state);
    }

    // ---- Vorschaubild ----

    private void loadArtwork(String rel) {
        artworkRel = rel;
        artwork = null;
        if (rel.isEmpty()) return;
        new Thread(() -> {
            Bitmap bmp = null;
            try {
                byte[] data = cache.read(rel);
                if (data != null) bmp = decode(data);
            } catch (Exception | OutOfMemoryError ignored) {
            }
            final Bitmap result = bmp;
            ui.post(() -> {
                if (released || result == null || !rel.equals(artworkRel)) return;
                artwork = result;
                pushMetadata();
                if (notificationShown) updateNotification();
            });
        }, "stefflix-artwork").start();
    }

    private static Bitmap decode(byte[] data) {
        BitmapFactory.Options bounds = new BitmapFactory.Options();
        bounds.inJustDecodeBounds = true;
        BitmapFactory.decodeByteArray(data, 0, data.length, bounds);
        int sample = 1;
        while (Math.max(bounds.outWidth, bounds.outHeight) / (sample * 2) >= ARTWORK_MAX_PX) sample *= 2;
        BitmapFactory.Options o = new BitmapFactory.Options();
        o.inSampleSize = sample;
        return BitmapFactory.decodeByteArray(data, 0, data.length, o);
    }

    // ---- Benachrichtigung und Dienst ----

    static Notification serviceNotification() {
        Playback p = current;
        return p == null ? null : p.lastNotification;
    }

    static boolean wantsForeground() {
        Playback p = current;
        return p != null && p.foreground;
    }

    static boolean notificationVisible() {
        Playback p = current;
        return p != null && p.notificationShown && !p.dismissed;
    }

    /** Für den Dienst, falls die Wiedergabe schon vor seinem Start geendet hat. */
    static Notification placeholderNotification(Context c) {
        createChannel(c);
        return new Notification.Builder(c, CHANNEL)
                .setSmallIcon(R.drawable.ic_notification)
                .setContentTitle("STEFFLIX")
                .setShowWhen(false)
                .build();
    }

    private static void createChannel(Context c) {
        NotificationManager nm = c.getSystemService(NotificationManager.class);
        if (nm == null || nm.getNotificationChannel(CHANNEL) != null) return;
        NotificationChannel channel = new NotificationChannel(CHANNEL, "Wiedergabe", NotificationManager.IMPORTANCE_LOW);
        channel.setDescription("Steuerung für Musik und Videos, die im Hintergrund laufen");
        channel.setShowBadge(false);
        channel.setLockscreenVisibility(Notification.VISIBILITY_PUBLIC);
        nm.createNotificationChannel(channel);
    }

    static PendingIntent commandIntent(Context c, String cmd) {
        // Eigene Aktion je Befehl, sonst fasst Android die PendingIntents zusammen
        Intent i = new Intent(c, PlaybackReceiver.class)
                .setAction("ch.stefflix.app.MEDIA_" + cmd)
                .putExtra(EXTRA_CMD, cmd);
        return PendingIntent.getBroadcast(c, cmd.hashCode(), i, PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
    }

    /** Antippen der Benachrichtigung: zurück zur geöffneten Seite (BrowserActivity ist singleTask). */
    private PendingIntent openIntent() {
        Intent i = new Intent(context, BrowserActivity.class)
                .putExtra(BrowserActivity.EXTRA_ID, shareId)
                .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        return PendingIntent.getActivity(context, 1, i, PendingIntent.FLAG_IMMUTABLE | PendingIntent.FLAG_UPDATE_CURRENT);
    }

    private Notification.Action action(int icon, String label, String cmd) {
        return new Notification.Action.Builder(Icon.createWithResource(context, icon), label, commandIntent(context, cmd)).build();
    }

    private Notification buildNotification() {
        Notification.Builder b = new Notification.Builder(context, CHANNEL)
                .setSmallIcon(R.drawable.ic_notification)
                .setContentTitle(title.isEmpty() ? "STEFFLIX" : title)
                .setContentText(subtitle)
                .setContentIntent(openIntent())
                .setDeleteIntent(commandIntent(context, CMD_STOP))
                .setVisibility(Notification.VISIBILITY_PUBLIC)
                .setCategory(Notification.CATEGORY_TRANSPORT)
                .setOngoing(playing)
                .setShowWhen(false)
                .setOnlyAlertOnce(true)
                .addAction(action(R.drawable.ic_skip_prev, "Zurück", CMD_PREV))
                .addAction(action(R.drawable.ic_replay, "10 s zurück", CMD_BACK))
                .addAction(playing ? action(R.drawable.ic_pause, "Pause", CMD_PAUSE) : action(R.drawable.ic_play, "Abspielen", CMD_PLAY))
                .addAction(action(R.drawable.ic_forward, "10 s vor", CMD_FORWARD))
                .addAction(action(R.drawable.ic_skip_next, "Weiter", CMD_NEXT))
                .setStyle(new Notification.MediaStyle()
                        .setMediaSession(session.getSessionToken())
                        .setShowActionsInCompactView(0, 2, 4));
        if (artwork != null) b.setLargeIcon(artwork);
        if (Build.VERSION.SDK_INT >= 31) b.setForegroundServiceBehavior(Notification.FOREGROUND_SERVICE_IMMEDIATE);
        return b.build();
    }

    private void updateNotification() {
        if (released || !active || dismissed || !prefs.backgroundPlayback() || (!playing && !notificationShown)) {
            if (notificationShown || foreground) removeNotification();
            return;
        }
        Notification n = buildNotification();
        lastNotification = n;
        notificationShown = true;
        if (playing) {
            ui.removeCallbacks(detach);
            if (!foreground) {
                try {
                    context.startForegroundService(new Intent(context, PlaybackService.class));
                    foreground = true;
                } catch (RuntimeException e) {
                    // Android erlaubt den Start gerade nicht (App im Hintergrund): nur die Benachrichtigung
                }
            }
            notifications.notify(NOTIFICATION_ID, n);
        } else {
            notifications.notify(NOTIFICATION_ID, n);
            if (foreground) {
                ui.removeCallbacks(detach);
                ui.postDelayed(detach, DETACH_DELAY_MS);
            }
        }
    }

    /** Nach einer Pause: Dienst lösen, Benachrichtigung bleibt und lässt sich wegwischen. */
    private void detachForeground() {
        if (playing || !foreground) return;
        foreground = false;
        PlaybackService.detach();
        if (lastNotification != null && notificationShown && !dismissed) {
            notifications.notify(NOTIFICATION_ID, buildNotification());
        }
    }

    private void removeNotification() {
        ui.removeCallbacks(detach);
        foreground = false;
        notificationShown = false;
        lastNotification = null;
        PlaybackService.remove();
        notifications.cancel(NOTIFICATION_ID);
    }
}
