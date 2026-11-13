package ch.stefflix.app;

import android.content.Context;
import android.view.View;
import android.webkit.WebView;

/**
 * WebView, die sich während der Wiedergabe im Hintergrund weiter als sichtbar meldet. Sonst hält
 * Chromium Videos an, sobald die App nicht mehr sichtbar ist, und drosselt die Skripte von STEFFLIX
 * (Autoplay zum nächsten Medium). Gesetzt von BrowserActivity in onPause, zurückgesetzt in onResume.
 */
final class PlayerWebView extends WebView {

    private boolean keepVisible;

    PlayerWebView(Context context) {
        super(context);
    }

    /** true: Fensterwechsel (App im Hintergrund, Bildschirm aus) nicht an die Seite weitergeben.
     *  Beim Zurücksetzen wird nichts erzwungen: Die nächste echte Meldung des Fensters gilt wieder. */
    void setKeepVisible(boolean keep) {
        keepVisible = keep;
    }

    @Override
    protected void onWindowVisibilityChanged(int visibility) {
        super.onWindowVisibilityChanged(keepVisible ? View.VISIBLE : visibility);
    }
}
