package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.content.Intent;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ListView;
import android.widget.ScrollView;
import android.widget.TextView;

import java.util.ArrayList;
import java.util.List;

/** Startbildschirm: Liste der Freigaben, hinzufuegen, bearbeiten, loeschen. Oeffnet beim Start die zuletzt genutzte. */
public class ShareListActivity extends Activity {

    private static final int RED = 0xffe8261a;
    private ShareStore store;
    private List<ShareStore.Share> shares = new ArrayList<>();
    private ArrayAdapter<String> adapter;
    private TextView empty;

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
        hint.setText("Freigabe antippen zum Öffnen, lange drücken zum Bearbeiten.");
        hint.setTextColor(0xff999999);
        hint.setPadding(0, dp(4), 0, dp(16));
        layout.addView(hint);

        empty = new TextView(this);
        empty.setText("Noch keine Freigabe eingerichtet.");
        empty.setTextColor(0xffcccccc);
        empty.setPadding(0, dp(12), 0, dp(12));
        layout.addView(empty);

        ListView list = new ListView(this);
        adapter = new ArrayAdapter<>(this, android.R.layout.simple_list_item_2, android.R.id.text1, new ArrayList<>()) {
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
        add.setText("Freigabe hinzufügen");
        add.setTextColor(Color.WHITE);
        add.setBackgroundColor(RED);
        add.setOnClickListener(v -> edit(null, null));
        layout.addView(add, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(52)));

        setContentView(layout);
        refresh();

        if (state == null) {
            ShareStore.Share last = store.find(store.getLast());
            if (last != null) open(last);
            else if (shares.isEmpty()) edit(null, null);
        }
    }

    private void refresh() {
        shares = store.load();
        adapter.clear();
        for (ShareStore.Share s : shares) adapter.add(s.label());
        empty.setVisibility(shares.isEmpty() ? View.VISIBLE : View.GONE);
    }

    private void open(ShareStore.Share s) {
        store.setLast(s.id);
        startActivity(new Intent(this, BrowserActivity.class).putExtra(BrowserActivity.EXTRA_ID, s.id));
    }

    private void showActions(ShareStore.Share s) {
        new AlertDialog.Builder(this)
                .setTitle(s.label())
                .setItems(new String[]{"Öffnen", "Bearbeiten", "Löschen"}, (d, which) -> {
                    if (which == 0) open(s);
                    else if (which == 1) edit(s, null);
                    else confirmDelete(s);
                })
                .show();
    }

    private void confirmDelete(ShareStore.Share s) {
        new AlertDialog.Builder(this)
                .setTitle("Freigabe entfernen?")
                .setMessage(s.label() + "\n\nDie Medien in der Freigabe bleiben unverändert.")
                .setPositiveButton("Entfernen", (d, w) -> {
                    List<ShareStore.Share> list = store.load();
                    list.removeIf(x -> x.id.equals(s.id));
                    store.save(list);
                    if (s.id.equals(store.getLast())) store.setLast("");
                    refresh();
                })
                .setNegativeButton("Abbrechen", null)
                .show();
    }

    /** Dialog zum Anlegen oder Bearbeiten. draft = bereits eingegebene Werte nach einem Fehler. */
    private void edit(ShareStore.Share existing, ShareStore.Share draft) {
        ShareStore.Share src = draft != null ? draft : existing;
        LinearLayout form = new LinearLayout(this);
        form.setOrientation(LinearLayout.VERTICAL);
        form.setPadding(dp(20), dp(8), dp(20), 0);

        EditText name = field(form, "Name (optional)", src == null ? "" : src.name, InputType.TYPE_CLASS_TEXT);
        EditText location = field(form, "Ordner, z.B. \\\\NAS\\Medien\\STEFFLIX", src == null ? "" : src.location(),
                InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_URI);
        EditText user = field(form, "Benutzer (leer = Gast)", src == null ? "" : src.user, InputType.TYPE_CLASS_TEXT);
        EditText password = field(form, "Passwort", src == null ? "" : src.password,
                InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        EditText domain = field(form, "Domäne (meist leer)", src == null ? "" : src.domain, InputType.TYPE_CLASS_TEXT);

        ScrollView scroll = new ScrollView(this);
        scroll.addView(form);

        AlertDialog dialog = new AlertDialog.Builder(this)
                .setTitle(existing == null ? "Freigabe hinzufügen" : "Freigabe bearbeiten")
                .setView(scroll)
                .setPositiveButton("Verbinden", null)
                .setNegativeButton("Abbrechen", null)
                .create();
        dialog.setOnShowListener(d -> dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(v -> {
            ShareStore.Share s = new ShareStore.Share();
            if (existing != null) s.id = existing.id;
            s.name = name.getText().toString().trim();
            s.user = user.getText().toString().trim();
            s.password = password.getText().toString();
            s.domain = domain.getText().toString().trim();
            if (!s.setLocation(location.getText().toString())) {
                location.setError("Bitte als \\\\Server\\Freigabe\\Ordner angeben");
                return;
            }
            dialog.dismiss();
            testAndSave(existing, s);
        }));
        dialog.show();
    }

    private EditText field(LinearLayout form, String hint, String value, int type) {
        EditText e = new EditText(this);
        e.setHint(hint);
        e.setText(value);
        e.setInputType(type);
        e.setSingleLine(true);
        form.addView(e);
        return e;
    }

    private void testAndSave(ShareStore.Share existing, ShareStore.Share s) {
        AlertDialog wait = new AlertDialog.Builder(this)
                .setMessage("Verbinde mit " + s.location() + " …")
                .setCancelable(false)
                .show();
        Handler ui = new Handler(Looper.getMainLooper());
        new Thread(() -> {
            String error = null;
            SmbSource smb = new SmbSource(s);
            try {
                smb.test();
            } catch (java.io.FileNotFoundException e) {
                error = "Verbindung klappt, aber im Ordner liegt keine start.html. Ist das der STEFFLIX-Ordner?";
            } catch (Exception e) {
                error = "Verbindung fehlgeschlagen:\n" + e.getMessage();
            } finally {
                smb.close();
            }
            final String err = error;
            ui.post(() -> {
                wait.dismiss();
                if (isFinishing()) return;
                if (err == null) {
                    save(s);
                    open(s);
                } else {
                    new AlertDialog.Builder(this)
                            .setTitle("Freigabe prüfen")
                            .setMessage(err)
                            .setPositiveButton("Ändern", (d, w) -> edit(existing, s))
                            .setNeutralButton("Trotzdem speichern", (d, w) -> save(s))
                            .setNegativeButton("Abbrechen", null)
                            .show();
                }
            });
        }).start();
    }

    private void save(ShareStore.Share s) {
        List<ShareStore.Share> list = store.load();
        boolean replaced = false;
        for (int i = 0; i < list.size(); i++) {
            if (list.get(i).id.equals(s.id)) {
                list.set(i, s);
                replaced = true;
            }
        }
        if (!replaced) list.add(s);
        store.save(list);
        refresh();
    }

    private int dp(int v) {
        return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, getResources().getDisplayMetrics());
    }
}
