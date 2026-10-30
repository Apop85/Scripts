package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Context;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Bundle;
import android.util.TypedValue;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ListView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.List;

/**
 * Startbildschirm: Liste der Quellen, hinzufuegen, bearbeiten, loeschen. Oeffnet beim Start die
 * zuletzt genutzte. Kommt die App wegen eines Verbindungsfehlers hierher zurueck, steht oben ein
 * Hinweis mit "Erneut versuchen".
 */
public class ShareListActivity extends Activity {

    /** Auswahl zeigen, nichts automatisch oeffnen ("Quelle wechseln"). */
    static final String EXTRA_CHOOSE = "choose";
    /** Hinweis oben (Verbindungsfehler) und die Quelle, die "Erneut versuchen" oeffnet. */
    static final String EXTRA_ERROR = "error";
    static final String EXTRA_ERROR_ID = "errorId";
    /** Diese Quelle sofort oeffnen (Wechsel aus den App-Einstellungen). */
    static final String EXTRA_OPEN = "open";

    private static final int RED = 0xffe8261a;
    private ShareStore store;
    private List<ShareStore.Share> shares = new ArrayList<>();
    private ArrayAdapter<String> adapter;
    private TextView empty;
    private LinearLayout banner;
    private TextView bannerText;
    private String retryId;

    /** Zurueck zur Auswahl, z.B. aus der WebView oder den App-Einstellungen. */
    static Intent intent(Context c) {
        return new Intent(c, ShareListActivity.class)
                .addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
    }

    @Override
    protected void onCreate(Bundle state) {
        super.onCreate(state);
        store = new ShareStore(this);

        LinearLayout layout = new LinearLayout(this);
        layout.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(20);
        layout.setPadding(pad, pad, pad, pad);

        TextView title = new TextView(this);
        title.setText("STEFFLIX");
        title.setTextColor(RED);
        title.setTypeface(Typeface.DEFAULT_BOLD);
        title.setTextSize(TypedValue.COMPLEX_UNIT_SP, 34);
        title.setLetterSpacing(0.08f);
        layout.addView(title);

        TextView hint = new TextView(this);
        hint.setText("Quelle antippen zum Öffnen, lange drücken zum Bearbeiten.");
        hint.setTextColor(0xff999999);
        hint.setPadding(0, dp(4), 0, dp(16));
        layout.addView(hint);

        banner = new LinearLayout(this);
        banner.setOrientation(LinearLayout.VERTICAL);
        banner.setPadding(dp(14), dp(12), dp(14), dp(12));
        GradientDrawable bg = new GradientDrawable();
        bg.setColor(0xff2a1513);
        bg.setStroke(dp(1), 0xffa81a0f);
        bg.setCornerRadius(dp(8));
        banner.setBackground(bg);
        bannerText = new TextView(this);
        bannerText.setTextColor(0xffeeeeee);
        bannerText.setTextSize(TypedValue.COMPLEX_UNIT_SP, 15);
        banner.addView(bannerText);
        Button retry = new Button(this);
        retry.setText("Erneut versuchen");
        retry.setTextColor(Color.WHITE);
        retry.setBackgroundColor(RED);
        retry.setOnClickListener(v -> {
            ShareStore.Share s = store.find(retryId);
            banner.setVisibility(View.GONE);
            if (s != null) open(s);
        });
        LinearLayout.LayoutParams rp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WRAP_CONTENT, dp(44));
        rp.topMargin = dp(10);
        banner.addView(retry, rp);
        banner.setVisibility(View.GONE);
        LinearLayout.LayoutParams bp = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT);
        bp.bottomMargin = dp(12);
        layout.addView(banner, bp);

        empty = new TextView(this);
        empty.setText("Noch keine Quelle eingerichtet.");
        empty.setTextColor(0xffcccccc);
        empty.setPadding(0, dp(12), 0, dp(12));
        layout.addView(empty);

        ListView list = new ListView(this);
        adapter = new ArrayAdapter<String>(this, android.R.layout.simple_list_item_2, android.R.id.text1, new ArrayList<>()) {
            @Override
            public View getView(int position, View convertView, ViewGroup parent) {
                View v = super.getView(position, convertView, parent);
                ShareStore.Share s = shares.get(position);
                TextView t1 = v.findViewById(android.R.id.text1);
                TextView t2 = v.findViewById(android.R.id.text2);
                t1.setText(s.label());
                t1.setTextColor(Color.WHITE);
                t2.setText(s.location());
                t2.setTextColor(0xff999999);
                return v;
            }
        };
        list.setAdapter(adapter);
        list.setOnItemClickListener((p, v, pos, id) -> open(shares.get(pos)));
        list.setOnItemLongClickListener((p, v, pos, id) -> {
            showActions(shares.get(pos));
            return true;
        });
        layout.addView(list, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f));

        Button add = new Button(this);
        add.setText("Quelle hinzufügen");
        add.setTextColor(Color.WHITE);
        add.setBackgroundColor(RED);
        add.setOnClickListener(v -> ShareEditor.edit(this, null, null, this::onSaved));
        layout.addView(add, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52)));

        setContentView(layout);
        refresh();

        if (state == null && !handle(getIntent())) {
            ShareStore.Share last = store.find(store.getLast());
            if (last != null) open(last);
            else if (shares.isEmpty()) ShareEditor.edit(this, null, null, this::onSaved);
        }
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        refresh();
        handle(intent);
    }

    @Override
    protected void onResume() {
        super.onResume();
        refresh();
    }

    /** Wertet Wechsel, Fehler und "Auswahl zeigen" aus. true: nichts automatisch oeffnen. */
    private boolean handle(Intent intent) {
        if (intent == null) return false;
        String open = intent.getStringExtra(EXTRA_OPEN);
        if (open != null) {
            intent.removeExtra(EXTRA_OPEN);
            ShareStore.Share s = store.find(open);
            if (s != null) {
                banner.setVisibility(View.GONE);
                open(s);
                return true;
            }
        }
        String error = intent.getStringExtra(EXTRA_ERROR);
        if (error != null) {
            retryId = intent.getStringExtra(EXTRA_ERROR_ID);
            ShareStore.Share s = store.find(retryId);
            bannerText.setText((s != null ? s.label() + " ist nicht erreichbar.\n" : "") + error);
            banner.setVisibility(s != null ? View.VISIBLE : View.GONE);
            return true;
        }
        if (intent.getBooleanExtra(EXTRA_CHOOSE, false)) {
            banner.setVisibility(View.GONE);
            return true;
        }
        return false;
    }

    private void refresh() {
        shares = store.load();
        adapter.clear();
        for (ShareStore.Share s : shares) adapter.add(s.label());
        empty.setVisibility(shares.isEmpty() ? View.VISIBLE : View.GONE);
    }

    private void onSaved(ShareStore.Share s, boolean connected) {
        refresh();
        if (connected) open(s);
    }

    private void open(ShareStore.Share s) {
        store.setLast(s.id);
        banner.setVisibility(View.GONE);
        startActivity(new Intent(this, BrowserActivity.class).putExtra(BrowserActivity.EXTRA_ID, s.id));
    }

    private void showActions(ShareStore.Share s) {
        new AlertDialog.Builder(this)
                .setTitle(s.label())
                .setItems(new String[]{"Öffnen", "Bearbeiten", "Entfernen"}, (d, which) -> {
                    if (which == 0) open(s);
                    else if (which == 1) ShareEditor.edit(this, s, null, this::onSaved);
                    else ShareEditor.confirmDelete(this, s, this::refresh);
                })
                .show();
    }

    private int dp(int v) {
        return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, getResources().getDisplayMetrics());
    }
}
