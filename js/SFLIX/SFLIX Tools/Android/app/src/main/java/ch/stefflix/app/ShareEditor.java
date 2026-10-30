package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.os.Handler;
import android.os.Looper;
import android.text.InputType;
import android.util.TypedValue;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.ScrollView;

import java.util.List;

/** Dialoge zum Anlegen, Bearbeiten und Entfernen von Quellen (Startbildschirm und App-Einstellungen). */
final class ShareEditor {

    interface Saved {
        /** connected: Verbindungstest erfolgreich (sonst "Trotzdem speichern"). */
        void onSaved(ShareStore.Share share, boolean connected);
    }

    private ShareEditor() {
    }

    /** Dialog zum Anlegen oder Bearbeiten. draft = bereits eingegebene Werte nach einem Fehler. */
    static void edit(Activity a, ShareStore.Share existing, ShareStore.Share draft, Saved saved) {
        ShareStore.Share src = draft != null ? draft : existing;
        LinearLayout form = new LinearLayout(a);
        form.setOrientation(LinearLayout.VERTICAL);
        form.setPadding(dp(a, 20), dp(a, 8), dp(a, 20), 0);

        EditText name = field(a, form, "Name (optional)", src == null ? "" : src.name, InputType.TYPE_CLASS_TEXT);
        EditText location = field(a, form, "Ordner, z.B. \\\\NAS\\Medien\\STEFFLIX", src == null ? "" : src.location(),
                InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_URI);
        EditText user = field(a, form, "Benutzer (leer = Gast)", src == null ? "" : src.user, InputType.TYPE_CLASS_TEXT);
        EditText password = field(a, form, "Passwort", src == null ? "" : src.password,
                InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_PASSWORD);
        EditText domain = field(a, form, "Domäne (meist leer)", src == null ? "" : src.domain, InputType.TYPE_CLASS_TEXT);

        ScrollView scroll = new ScrollView(a);
        scroll.addView(form);

        AlertDialog dialog = new AlertDialog.Builder(a)
                .setTitle(existing == null ? "Quelle hinzufügen" : "Quelle bearbeiten")
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
            testAndSave(a, existing, s, saved);
        }));
        dialog.show();
    }

    private static EditText field(Activity a, LinearLayout form, String hint, String value, int type) {
        EditText e = new EditText(a);
        e.setHint(hint);
        e.setText(value);
        // Zuerst einzeilig, dann der Typ: setSingleLine danach würde die Passwort-Verdeckung aufheben
        e.setSingleLine(true);
        e.setInputType(type);
        if ((type & InputType.TYPE_TEXT_VARIATION_PASSWORD) != 0) {
            e.setTransformationMethod(android.text.method.PasswordTransformationMethod.getInstance());
        }
        form.addView(e);
        return e;
    }

    private static void testAndSave(Activity a, ShareStore.Share existing, ShareStore.Share s, Saved saved) {
        AlertDialog wait = new AlertDialog.Builder(a)
                .setMessage("Verbinde mit " + s.location() + " …")
                .setCancelable(false)
                .show();
        Handler ui = new Handler(Looper.getMainLooper());
        new Thread(() -> {
            SmbSource smb = new SmbSource(s);
            SmbSource.TestResult r;
            try {
                r = smb.test(false);
            } finally {
                smb.close();
            }
            final SmbSource.TestResult result = r;
            ui.post(() -> {
                wait.dismiss();
                if (a.isFinishing()) return;
                if (result.error == null) {
                    save(a, s);
                    if (saved != null) saved.onSaved(s, true);
                } else {
                    new AlertDialog.Builder(a)
                            .setTitle("Verbindung prüfen")
                            .setMessage(result.error)
                            .setPositiveButton("Ändern", (d, w) -> edit(a, existing, s, saved))
                            .setNeutralButton("Trotzdem speichern", (d, w) -> {
                                save(a, s);
                                if (saved != null) saved.onSaved(s, false);
                            })
                            .setNegativeButton("Abbrechen", null)
                            .show();
                }
            });
        }).start();
    }

    static void save(Activity a, ShareStore.Share s) {
        ShareStore store = new ShareStore(a);
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
    }

    static void confirmDelete(Activity a, ShareStore.Share s, Runnable deleted) {
        new AlertDialog.Builder(a)
                .setTitle("Quelle entfernen?")
                .setMessage(s.label() + "\n\nDie Medien in der Freigabe bleiben unverändert.")
                .setPositiveButton("Entfernen", (d, w) -> {
                    ShareStore store = new ShareStore(a);
                    List<ShareStore.Share> list = store.load();
                    list.removeIf(x -> x.id.equals(s.id));
                    store.save(list);
                    if (s.id.equals(store.getLast())) store.setLast("");
                    if (deleted != null) deleted.run();
                })
                .setNegativeButton("Abbrechen", null)
                .show();
    }

    static int dp(Activity a, int v) {
        return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, a.getResources().getDisplayMetrics());
    }
}
