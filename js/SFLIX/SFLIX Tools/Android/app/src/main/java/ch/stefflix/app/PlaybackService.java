package ch.stefflix.app;

import android.app.Notification;
import android.app.Service;
import android.content.Intent;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.IBinder;

/**
 * Vordergrund-Dienst der Art "Medienwiedergabe": hält die App am Leben, solange im Hintergrund etwas
 * läuft, und zeigt die Benachrichtigung von Playback. Wird von Playback gestartet, sobald die
 * Wiedergabe beginnt (aus dem Hintergrund darf Android ihn nicht mehr starten), und nach einer Pause
 * gelöst (Benachrichtigung bleibt und lässt sich wegwischen). Wegwischen der App beendet die Wiedergabe.
 */
public class PlaybackService extends Service {

    private static PlaybackService instance;

    @Override
    public void onCreate() {
        super.onCreate();
        instance = this;
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        Notification n = Playback.serviceNotification();
        if (n == null) n = Playback.placeholderNotification(this);
        try {
            // Nach startForegroundService muss startForeground in jedem Fall folgen
            if (Build.VERSION.SDK_INT >= 29) {
                startForeground(Playback.NOTIFICATION_ID, n, ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PLAYBACK);
            } else {
                startForeground(Playback.NOTIFICATION_ID, n);
            }
        } catch (RuntimeException e) {
            stopSelf();
            return START_NOT_STICKY;
        }
        // Inzwischen pausiert oder beendet
        if (!Playback.wantsForeground()) {
            stopForeground(Playback.notificationVisible() ? STOP_FOREGROUND_DETACH : STOP_FOREGROUND_REMOVE);
            stopSelf();
        }
        return START_NOT_STICKY;
    }

    @Override
    public void onTaskRemoved(Intent rootIntent) {
        // App aus der Übersicht weggewischt: Wiedergabe stoppen
        Playback.dispatch(this, Playback.CMD_QUIT);
        stopForeground(STOP_FOREGROUND_REMOVE);
        stopSelf();
    }

    @Override
    public void onDestroy() {
        if (instance == this) instance = null;
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

    /** Pause: Dienst beenden, Benachrichtigung bleibt (lässt sich dann wegwischen). */
    static void detach() {
        if (instance == null) return;
        instance.stopForeground(STOP_FOREGROUND_DETACH);
        instance.stopSelf();
    }

    /** Ende: Dienst und Benachrichtigung entfernen. */
    static void remove() {
        if (instance == null) return;
        instance.stopForeground(STOP_FOREGROUND_REMOVE);
        instance.stopSelf();
    }
}
