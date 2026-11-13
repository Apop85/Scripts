package ch.stefflix.app;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/** Knöpfe in der Benachrichtigung und im Bild-im-Bild-Fenster (Playback.commandIntent). */
public class PlaybackReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {
        String cmd = intent.getStringExtra(Playback.EXTRA_CMD);
        if (cmd != null) Playback.dispatch(context, cmd);
    }
}
