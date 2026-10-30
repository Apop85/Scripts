package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.Switch;
import android.widget.TextView;
import android.widget.Toast;

import java.util.List;
import java.util.Locale;

/** App-Einstellungen: Quellen, Wiedergabe und Anzeige, Speicher, Verbindung, Über die App. */
public class SettingsActivity extends Activity {

    static final String EXTRA_CURRENT = "current";
    /** Ergebnis: STEFFLIX soll die Hilfe öffnen. */
    static final int RESULT_HELP = RESULT_FIRST_USER + 1;

    private static final int RED = 0xffe8261a;
    private static final int MUTED = 0xff9a9aa2;
    private AppPrefs prefs;
    private ShareStore store;
    private String currentId;
    private LinearLayout sources;
    private TextView cacheValue, bufferValue, testResult, updateStatus;
    private Button updateButton;

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        prefs = new AppPrefs(this);
        store = new ShareStore(this);
        currentId = getIntent().getStringExtra(EXTRA_CURRENT);

        ScrollView scroll = new ScrollView(this);
        scroll.setBackgroundColor(0xff0b0b0d);
        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(dp(20), dp(20), dp(20), dp(32));
        scroll.addView(page);

        TextView title = new TextView(this);
        title.setText("App-Einstellungen");
        title.setTextColor(RED);
        title.setTypeface(Typeface.DEFAULT_BOLD);
        title.setTextSize(TypedValue.COMPLEX_UNIT_SP, 28);
        page.addView(title);

        // ---- Quellen ----
        heading(page, "Quellen");
        sources = new LinearLayout(this);
        sources.setOrientation(LinearLayout.VERTICAL);
        page.addView(sources);
        page.addView(button("Quelle hinzufügen", true, v -> ShareEditor.edit(this, null, null, (s, ok) -> fillSources())));
        note(page, "Beim Start öffnet die App immer die zuletzt geöffnete Quelle.");

        // ---- Wiedergabe und Anzeige ----
        heading(page, "Wiedergabe und Anzeige");
        switchRow(page, "Display nicht ausschalten", "Solange Videos oder Musik laufen",
                prefs.keepScreenOn(), prefs::setKeepScreenOn);
        switchRow(page, "Videos im Vollbild quer anzeigen", "Dreht das Bild im Vollbild ins Querformat",
                prefs.landscapeFullscreen(), prefs::setLandscapeFullscreen);

        // ---- Speicher ----
        heading(page, "Speicher");
        cacheValue = choiceRow(page, "Zwischenspeicher auf dem Gerät",
                "Programmdateien und Vorschaubilder aller Quellen. Seiten laden damit schneller, besonders über VPN.",
                v -> choose("Zwischenspeicher", AppPrefs.CACHE_CHOICES_MB, prefs.cacheMb(), mb -> {
                    prefs.setCacheMb(mb);
                    fillStorage();
                }));
        page.addView(button("Zwischenspeicher leeren", false, v -> {
            FileCache.clearAll(this);
            fillStorage();
            Toast.makeText(this, "Zwischenspeicher geleert", Toast.LENGTH_SHORT).show();
        }));
        bufferValue = choiceRow(page, "Videopuffer",
                "Videos und Musik werden so weit im Voraus geladen. Überbrückt kurze Verbindungsabbrüche. Gilt ab dem nächsten Öffnen einer Quelle.",
                v -> choose("Videopuffer", AppPrefs.BUFFER_CHOICES_MB, prefs.bufferMb(), mb -> {
                    prefs.setBufferMb(mb);
                    fillStorage();
                }));

        // ---- Verbindung ----
        heading(page, "Verbindung");
        page.addView(button("Verbindung testen", false, v -> runTest()));
        testResult = note(page, "Prüft die aktuelle Quelle: Antwortzeit und Lesegeschwindigkeit.");

        // ---- Über die App ----
        heading(page, "Über die App");
        note(page, "STEFFLIX-App " + AppUpdate.installedVersion(this));
        updateStatus = note(page, "");
        updateButton = button("Nach Update suchen", false, v -> checkUpdate());
        page.addView(updateButton);
        page.addView(button("Hilfe zur App", false, v -> {
            setResult(RESULT_HELP);
            finish();
        }));
        page.addView(button("Diagnose kopieren", false, v -> {
            ClipboardManager cm = (ClipboardManager) getSystemService(Context.CLIPBOARD_SERVICE);
            cm.setPrimaryClip(ClipData.newPlainText("STEFFLIX-Diagnose", diagnosis(this, store.find(currentId))));
            Toast.makeText(this, "Diagnose kopiert", Toast.LENGTH_SHORT).show();
        }));

        setContentView(scroll);
        fillSources();
        fillStorage();
        fillUpdate();
    }

    // ---- Inhalte ----

    private void fillSources() {
        sources.removeAllViews();
        List<ShareStore.Share> list = store.load();
        if (list.isEmpty()) note(sources, "Noch keine Quelle eingerichtet.");
        for (ShareStore.Share s : list) {
            boolean current = s.id.equals(currentId);
            LinearLayout row = new LinearLayout(this);
            row.setOrientation(LinearLayout.HORIZONTAL);
            row.setGravity(Gravity.CENTER_VERTICAL);
            row.setMinimumHeight(dp(60));
            row.setPadding(dp(12), dp(8), dp(4), dp(8));
            row.setBackground(card(current));
            LinearLayout texts = new LinearLayout(this);
            texts.setOrientation(LinearLayout.VERTICAL);
            TextView name = text(s.label() + (current ? "  ·  Verbunden" : ""), 17, current ? RED : Color.WHITE);
            texts.addView(name);
            texts.addView(text(s.location(), 13, MUTED));
            row.addView(texts, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f));
            Button edit = new Button(this);
            edit.setText("Bearbeiten");
            edit.setAllCaps(false);
            edit.setTextColor(0xffdddddd);
            edit.setBackgroundColor(Color.TRANSPARENT);
            edit.setOnClickListener(v -> showActions(s));
            row.addView(edit, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, dp(48)));
            row.setOnClickListener(v -> {
                if (!current) switchTo(s);
            });
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
            lp.bottomMargin = dp(8);
            sources.addView(row, lp);
        }
    }

    private void showActions(ShareStore.Share s) {
        new AlertDialog.Builder(this)
                .setTitle(s.label())
                .setItems(new String[]{"Bearbeiten", "Entfernen"}, (d, which) -> {
                    if (which == 0) ShareEditor.edit(this, s, null, (saved, ok) -> {
                        // Geänderte Anmeldung der geöffneten Quelle: mit den neuen Angaben neu öffnen
                        if (saved.id.equals(currentId)) switchTo(saved);
                        else fillSources();
                    });
                    else ShareEditor.confirmDelete(this, s, () -> {
                        if (s.id.equals(currentId)) {
                            // Die geöffnete Quelle gibt es nicht mehr: zur Auswahl
                            startActivity(ShareListActivity.intent(this).putExtra(ShareListActivity.EXTRA_CHOOSE, true));
                            finish();
                        } else {
                            fillSources();
                        }
                    });
                })
                .show();
    }

    private void switchTo(ShareStore.Share s) {
        startActivity(ShareListActivity.intent(this).putExtra(ShareListActivity.EXTRA_OPEN, s.id));
        finish();
    }

    private void fillStorage() {
        long used = FileCache.diskUsage(this);
        cacheValue.setText(String.format(Locale.ROOT, "%d MB, belegt %.0f MB  ›", prefs.cacheMb(), used / 1048576.0));
        bufferValue.setText(prefs.bufferMb() + " MB  ›");
    }

    private void fillUpdate() {
        String available = AppUpdate.available(this);
        if (available != null) {
            updateStatus.setText("Version " + available + " ist verfügbar.");
            updateStatus.setTextColor(RED);
            updateButton.setText("Jetzt aktualisieren");
        } else {
            updateStatus.setText(prefs.lastUpdateCheck() > 0 ? "Die App ist aktuell." : "");
            updateStatus.setTextColor(MUTED);
            updateButton.setText("Nach Update suchen");
        }
    }

    private void checkUpdate() {
        if (AppUpdate.available(this) != null) {
            AppUpdate.install(this);
            return;
        }
        updateStatus.setText("Suche nach Update …");
        updateStatus.setTextColor(MUTED);
        AppUpdate.check(this, true, (latest, error) -> {
            if (isFinishing()) return;
            if (error != null) {
                updateStatus.setText(error);
                return;
            }
            fillUpdate();
        });
    }

    private void runTest() {
        ShareStore.Share s = store.find(currentId);
        if (s == null) {
            testResult.setText("Keine Quelle geöffnet.");
            return;
        }
        testResult.setText("Teste " + s.location() + " …");
        testResult.setTextColor(MUTED);
        Handler ui = new Handler(Looper.getMainLooper());
        new Thread(() -> {
            SmbSource smb = new SmbSource(s);
            SmbSource.TestResult r;
            try {
                r = smb.test(true);
            } finally {
                smb.close();
            }
            final SmbSource.TestResult result = r;
            ui.post(() -> {
                if (isFinishing()) return;
                if (result.error != null) {
                    testResult.setText(result.error);
                    testResult.setTextColor(RED);
                } else {
                    StringBuilder sb = new StringBuilder("Verbindung in Ordnung.\nAntwortzeit: ").append(result.latencyMs).append(" ms");
                    if (result.mbPerSecond > 0) sb.append(String.format(Locale.ROOT, "\nLesegeschwindigkeit: %.1f MB/s", result.mbPerSecond));
                    testResult.setText(sb.toString());
                    testResult.setTextColor(0xff8fd18f);
                }
            });
        }).start();
    }

    /** Angaben der App für die Diagnose (auch in STEFFLIX unter Einstellungen › Fehlerbehebung). */
    static String diagnosis(Context c, ShareStore.Share share) {
        AppPrefs p = new AppPrefs(c);
        StringBuilder sb = new StringBuilder();
        sb.append("App-Version: ").append(AppUpdate.installedVersion(c));
        String available = AppUpdate.available(c);
        if (available != null) sb.append(" (verfügbar: ").append(available).append(')');
        sb.append("\nGerät: ").append(android.os.Build.MANUFACTURER).append(' ').append(android.os.Build.MODEL)
                .append(", Android ").append(android.os.Build.VERSION.RELEASE).append(" (API ").append(android.os.Build.VERSION.SDK_INT).append(')');
        if (share != null) {
            sb.append("\nQuelle: ").append(share.location()).append(share.user.isEmpty() ? " (Gast)" : " (Benutzer " + share.user + ")");
        }
        sb.append(String.format(Locale.ROOT, "\nZwischenspeicher: %d MB, belegt %.0f MB", p.cacheMb(), FileCache.diskUsage(c) / 1048576.0));
        sb.append("\nVideopuffer: ").append(p.bufferMb()).append(" MB");
        sb.append("\nDisplay anlassen: ").append(p.keepScreenOn() ? "ja" : "nein")
                .append(", Vollbild quer: ").append(p.landscapeFullscreen() ? "ja" : "nein");
        sb.append("\nLetzte Seitenaufrufe:\n").append(LoadStats.text());
        return sb.toString();
    }

    // ---- Bausteine der Oberfläche ----

    private interface IntChoice {
        void chosen(int value);
    }

    private interface BoolSetter {
        void set(boolean value);
    }

    private void choose(String title, int[] values, int current, IntChoice chosen) {
        String[] labels = new String[values.length];
        int selected = 0;
        for (int i = 0; i < values.length; i++) {
            labels[i] = values[i] + " MB";
            if (values[i] == current) selected = i;
        }
        new AlertDialog.Builder(this)
                .setTitle(title)
                .setSingleChoiceItems(labels, selected, (d, which) -> {
                    chosen.chosen(values[which]);
                    d.dismiss();
                })
                .setNegativeButton("Abbrechen", null)
                .show();
    }

    private void heading(LinearLayout parent, String label) {
        TextView t = text(label.toUpperCase(Locale.ROOT), 13, MUTED);
        t.setTypeface(Typeface.DEFAULT_BOLD);
        t.setLetterSpacing(0.1f);
        t.setPadding(0, dp(28), 0, dp(10));
        parent.addView(t);
    }

    private TextView note(LinearLayout parent, String s) {
        TextView t = text(s, 14, MUTED);
        t.setPadding(0, dp(6), 0, dp(6));
        parent.addView(t);
        return t;
    }

    private TextView text(String s, int sp, int color) {
        TextView t = new TextView(this);
        t.setText(s);
        t.setTextColor(color);
        t.setTextSize(TypedValue.COMPLEX_UNIT_SP, sp);
        return t;
    }

    private Button button(String label, boolean primary, View.OnClickListener click) {
        Button b = new Button(this);
        b.setText(label);
        b.setAllCaps(false);
        b.setTextSize(TypedValue.COMPLEX_UNIT_SP, 16);
        b.setTextColor(Color.WHITE);
        GradientDrawable bg = new GradientDrawable();
        bg.setCornerRadius(dp(8));
        bg.setColor(primary ? RED : 0xff26262c);
        b.setBackground(bg);
        b.setOnClickListener(click);
        LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52));
        lp.topMargin = dp(8);
        b.setLayoutParams(lp);
        return b;
    }

    private GradientDrawable card(boolean highlighted) {
        GradientDrawable bg = new GradientDrawable();
        bg.setCornerRadius(dp(8));
        bg.setColor(0xff17171b);
        bg.setStroke(dp(1), highlighted ? RED : 0xff2c2c33);
        return bg;
    }

    private void switchRow(LinearLayout parent, String label, String sub, boolean value, BoolSetter setter) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.HORIZONTAL);
        row.setGravity(Gravity.CENTER_VERTICAL);
        row.setMinimumHeight(dp(60));
        LinearLayout texts = new LinearLayout(this);
        texts.setOrientation(LinearLayout.VERTICAL);
        texts.addView(text(label, 17, Color.WHITE));
        texts.addView(text(sub, 13, MUTED));
        row.addView(texts, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f));
        Switch sw = new Switch(this);
        sw.setChecked(value);
        sw.setOnCheckedChangeListener((b, checked) -> setter.set(checked));
        row.addView(sw);
        row.setOnClickListener(v -> sw.toggle());
        parent.addView(row);
    }

    /** Zeile mit Titel, darunter der Wert (antippen zum Ändern) und die Erklärung. Gibt das Wertfeld zurück.
     *  Wert unter dem Titel statt daneben: sonst bricht ein langer Titel auf dem Handy mitten im Wort um. */
    private TextView choiceRow(LinearLayout parent, String label, String sub, View.OnClickListener click) {
        LinearLayout row = new LinearLayout(this);
        row.setOrientation(LinearLayout.VERTICAL);
        row.setMinimumHeight(dp(60));
        row.setPadding(0, dp(8), 0, dp(8));
        row.addView(text(label, 17, Color.WHITE));
        TextView value = text("", 16, RED);
        value.setPadding(0, dp(2), 0, dp(2));
        row.addView(value);
        row.addView(text(sub, 13, MUTED));
        row.setOnClickListener(click);
        parent.addView(row);
        return value;
    }

    private int dp(int v) {
        return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, getResources().getDisplayMetrics());
    }
}
