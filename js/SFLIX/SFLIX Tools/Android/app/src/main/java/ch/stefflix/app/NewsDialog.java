package ch.stefflix.app;

import android.app.Activity;
import android.app.AlertDialog;
import android.graphics.Color;
import android.graphics.Typeface;
import android.os.Handler;
import android.os.Looper;
import android.text.SpannableStringBuilder;
import android.text.Spanned;
import android.text.style.ForegroundColorSpan;
import android.text.style.StyleSpan;
import android.util.TypedValue;
import android.view.Gravity;
import android.widget.LinearLayout;
import android.widget.ScrollView;
import android.widget.Switch;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * App-Einstellungen › Neuerungen: Patchnotes aus news.json auf GitHub. Zuerst die der App (nach
 * App-Version gegliedert), die von STEFFLIX und Desktop (STEFFLIX.exe, bis 3.x "Updater") lassen sich
 * darunter einblenden. Felder "component", "componentVersion" und "short" schreibt create_news.py
 * (Desktop weiterhin als "updater"); ältere news.json ohne sie: Komponente aus dem Textanfang
 * ("Desktop x.y.z:", "Updater x.y.z:", "Android-App x.y.z:").
 */
final class NewsDialog {

    static final String NEWS_URL = "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/sflix_sys/news.json";
    private static final Pattern UPDATER = Pattern.compile("^(?:Desktop|Updater)(?:\\s+(\\d+\\.\\d+\\.\\d+))?\\s*:\\s*");
    private static final Pattern APP = Pattern.compile("^(?:Android-App|App)(?:\\s+(\\d+\\.\\d+\\.\\d+))?\\s*:\\s*");
    private static final int RED = 0xffe8261a;
    private static final int MUTED = 0xff9a9aa2;

    private NewsDialog() {
    }

    /** Ein Eintrag: Typ, Komponente, Überschrift (z.B. "Desktop 4.0.0"), Text ohne Anfang. */
    private static final class Item {
        String type, component, title, text;
    }

    static void show(Activity a) {
        Handler ui = new Handler(Looper.getMainLooper());
        TextView loading = new TextView(a);
        loading.setText("Neuerungen werden geladen …");
        loading.setTextColor(MUTED);
        int pad = dp(a, 20);
        loading.setPadding(pad, pad, pad, pad);
        android.widget.FrameLayout holder = new android.widget.FrameLayout(a);
        holder.addView(loading);
        AlertDialog dialog = new AlertDialog.Builder(a)
                .setTitle("Neuerungen")
                .setView(holder)
                .setPositiveButton("Schliessen", null)
                .show();
        new Thread(() -> {
            List<Item> items = null;
            String error = null;
            try {
                byte[] data = AppUpdate.download(NEWS_URL + "?t=" + System.currentTimeMillis(), 4L * 1024 * 1024);
                items = parse(new JSONObject(new String(data, StandardCharsets.UTF_8)));
            } catch (Exception e) {
                error = "Die Neuerungen konnten nicht geladen werden. Sie werden online abgerufen und brauchen eine Internetverbindung.";
            }
            final List<Item> result = items;
            final String message = error;
            ui.post(() -> {
                if (a.isFinishing() || !dialog.isShowing()) return;
                if (result == null) {
                    loading.setText(message);
                    return;
                }
                holder.removeAllViews();
                holder.addView(build(a, result));
            });
        }, "stefflix-news").start();
    }

    private static List<Item> parse(JSONObject root) {
        List<Item> list = new ArrayList<>();
        JSONArray versions = root.optJSONArray("versions");
        if (versions == null) return list;
        for (int v = 0; v < versions.length(); v++) {
            JSONObject version = versions.optJSONObject(v);
            if (version == null) continue;
            String block = version.optString("name", version.optString("version", ""));
            JSONArray entries = version.optJSONArray("items");
            if (entries == null) continue;
            for (int i = 0; i < entries.length(); i++) {
                JSONObject e = entries.optJSONObject(i);
                if (e == null) continue;
                String text = e.optString("text", "");
                Item item = new Item();
                item.type = e.optString("type", "");
                String component = e.optString("component", null);
                String componentVersion = e.has("componentVersion") ? e.optString("componentVersion") : null;
                String shortText = e.optString("short", null);
                if (component == null || shortText == null) {
                    component = "stefflix";
                    shortText = text;
                    componentVersion = null;
                    for (Pattern p : new Pattern[]{UPDATER, APP}) {
                        Matcher m = p.matcher(text);
                        if (m.find()) {
                            component = p == UPDATER ? "updater" : "app";
                            componentVersion = m.group(1);
                            shortText = text.substring(m.end());
                            break;
                        }
                    }
                }
                item.component = component;
                item.text = shortText;
                if ("stefflix".equals(component)) {
                    item.title = "Version " + block;
                } else {
                    item.title = ("updater".equals(component) ? "Desktop " : "Android-App ")
                            + (componentVersion != null && !componentVersion.isEmpty() ? componentVersion : "(mit STEFFLIX " + block + ")");
                }
                list.add(item);
            }
        }
        return list;
    }

    private static ScrollView build(Activity a, List<Item> items) {
        ScrollView scroll = new ScrollView(a);
        LinearLayout page = new LinearLayout(a);
        page.setOrientation(LinearLayout.VERTICAL);
        int pad = dp(a, 20);
        page.setPadding(pad, dp(a, 8), pad, pad);
        scroll.addView(page);

        LinearLayout content = new LinearLayout(a);
        content.setOrientation(LinearLayout.VERTICAL);
        Switch web = toggle(a, "Auch Neuerungen von STEFFLIX zeigen");
        Switch updater = toggle(a, "Auch Neuerungen von STEFFLIX.exe (Desktop) zeigen");
        Runnable fill = () -> {
            content.removeAllViews();
            section(a, content, items, "app", web.isChecked() || updater.isChecked() ? "Neuerungen der App" : null);
            if (web.isChecked()) section(a, content, items, "stefflix", "Neuerungen von STEFFLIX");
            if (updater.isChecked()) section(a, content, items, "updater", "Neuerungen von Desktop");
        };
        web.setOnCheckedChangeListener((b, c) -> fill.run());
        updater.setOnCheckedChangeListener((b, c) -> fill.run());
        page.addView(web);
        page.addView(updater);
        page.addView(content);
        fill.run();
        return scroll;
    }

    private static Switch toggle(Activity a, String label) {
        Switch s = new Switch(a);
        s.setText(label);
        s.setTextColor(0xffdddddd);
        s.setTextSize(TypedValue.COMPLEX_UNIT_SP, 15);
        s.setMinHeight(dp(a, 48));
        s.setGravity(Gravity.CENTER_VERTICAL);
        return s;
    }

    private static void section(Activity a, LinearLayout parent, List<Item> items, String component, String heading) {
        if (heading != null) {
            TextView h = text(a, heading, 19, RED);
            h.setTypeface(Typeface.DEFAULT_BOLD);
            h.setPadding(0, dp(a, 20), 0, dp(a, 2));
            parent.addView(h);
        }
        Map<String, List<Item>> groups = new LinkedHashMap<>();
        for (Item item : items) {
            if (!component.equals(item.component)) continue;
            List<Item> list = groups.get(item.title);
            if (list == null) {
                list = new ArrayList<>();
                groups.put(item.title, list);
            }
            list.add(item);
        }
        if (groups.isEmpty()) {
            parent.addView(text(a, "Keine Neuerungen vorhanden.", 15, MUTED));
            return;
        }
        for (Map.Entry<String, List<Item>> g : groups.entrySet()) {
            TextView title = text(a, g.getKey(), 17, Color.WHITE);
            title.setTypeface(Typeface.DEFAULT_BOLD);
            title.setPadding(0, dp(a, 14), 0, dp(a, 4));
            parent.addView(title);
            for (Item item : g.getValue()) {
                SpannableStringBuilder sb = new SpannableStringBuilder();
                sb.append(item.type);
                sb.setSpan(new ForegroundColorSpan(typeColor(item.type)), 0, item.type.length(), Spanned.SPAN_EXCLUSIVE_EXCLUSIVE);
                sb.setSpan(new StyleSpan(Typeface.BOLD), 0, item.type.length(), Spanned.SPAN_EXCLUSIVE_EXCLUSIVE);
                sb.append("  ").append(item.text);
                TextView t = text(a, "", 15, 0xffd8d8de);
                t.setText(sb);
                t.setPadding(0, dp(a, 3), 0, dp(a, 3));
                parent.addView(t);
            }
        }
    }

    private static int typeColor(String type) {
        switch (type) {
            case "FEATURE": return 0xffff4d57;
            case "BUGFIX": return 0xff6cc87a;
            case "HOTFIX": return 0xfff08c4c;
            case "INTERFACE": return 0xff6ca8f0;
            case "OPTIMIERUNG": return 0xfff0c46c;
            default: return MUTED;
        }
    }

    private static TextView text(Activity a, String s, int sp, int color) {
        TextView t = new TextView(a);
        t.setText(s);
        t.setTextColor(color);
        t.setTextSize(TypedValue.COMPLEX_UNIT_SP, sp);
        return t;
    }

    private static int dp(Activity a, int v) {
        return (int) TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_DIP, v, a.getResources().getDisplayMetrics());
    }
}
