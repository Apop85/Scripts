package ch.stefflix.app;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;

import org.json.JSONArray;
import org.json.JSONObject;

import java.nio.charset.StandardCharsets;
import java.security.KeyStore;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Gespeicherte Freigaben. Passwoerter sind mit einem Schluessel aus dem Android-Keystore verschluesselt. */
final class ShareStore {

    static final class Share {
        String id = UUID.randomUUID().toString().replace("-", "").substring(0, 12);
        String name = "", host = "", share = "", path = "", user = "", domain = "", password = "";

        String label() {
            return name.isEmpty() ? location() : name;
        }

        String location() {
            return "\\\\" + host + "\\" + share + (path.isEmpty() ? "" : "\\" + path);
        }

        /** Liest \\server\freigabe\pfad, //server/freigabe/pfad oder smb://server/freigabe/pfad. */
        boolean setLocation(String text) {
            String t = text.trim();
            if (t.toLowerCase().startsWith("smb:")) t = t.substring(4);
            t = t.replace('/', '\\');
            while (t.startsWith("\\")) t = t.substring(1);
            while (t.endsWith("\\")) t = t.substring(0, t.length() - 1);
            String[] parts = t.split("\\\\+", 3);
            if (parts.length < 2 || parts[0].isEmpty() || parts[1].isEmpty()) return false;
            host = parts[0];
            share = parts[1];
            path = parts.length > 2 ? parts[2] : "";
            return true;
        }
    }

    private static final String KEY_ALIAS = "stefflix-shares";
    private final SharedPreferences prefs;

    ShareStore(Context context) {
        prefs = context.getSharedPreferences("shares", Context.MODE_PRIVATE);
    }

    List<Share> load() {
        List<Share> list = new ArrayList<>();
        try {
            JSONArray a = new JSONArray(prefs.getString("list", "[]"));
            for (int i = 0; i < a.length(); i++) {
                JSONObject o = a.getJSONObject(i);
                Share s = new Share();
                s.id = o.getString("id");
                s.name = o.optString("name");
                s.host = o.optString("host");
                s.share = o.optString("share");
                s.path = o.optString("path");
                s.user = o.optString("user");
                s.domain = o.optString("domain");
                s.password = decrypt(o.optString("pw"));
                list.add(s);
            }
        } catch (Exception ignored) {
        }
        return list;
    }

    void save(List<Share> list) {
        JSONArray a = new JSONArray();
        try {
            for (Share s : list) {
                JSONObject o = new JSONObject();
                o.put("id", s.id);
                o.put("name", s.name);
                o.put("host", s.host);
                o.put("share", s.share);
                o.put("path", s.path);
                o.put("user", s.user);
                o.put("domain", s.domain);
                o.put("pw", encrypt(s.password));
                a.put(o);
            }
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
        prefs.edit().putString("list", a.toString()).apply();
    }

    Share find(String id) {
        for (Share s : load()) if (s.id.equals(id)) return s;
        return null;
    }

    String getLast() {
        return prefs.getString("last", "");
    }

    void setLast(String id) {
        prefs.edit().putString("last", id).apply();
    }

    private static SecretKey key() throws Exception {
        KeyStore ks = KeyStore.getInstance("AndroidKeyStore");
        ks.load(null);
        if (ks.containsAlias(KEY_ALIAS)) return (SecretKey) ks.getKey(KEY_ALIAS, null);
        KeyGenerator g = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
        g.init(new KeyGenParameterSpec.Builder(KEY_ALIAS,
                KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .build());
        return g.generateKey();
    }

    private static String encrypt(String plain) throws Exception {
        if (plain == null || plain.isEmpty()) return "";
        Cipher c = Cipher.getInstance("AES/GCM/NoPadding");
        c.init(Cipher.ENCRYPT_MODE, key());
        byte[] ct = c.doFinal(plain.getBytes(StandardCharsets.UTF_8));
        return Base64.encodeToString(c.getIV(), Base64.NO_WRAP) + ":" + Base64.encodeToString(ct, Base64.NO_WRAP);
    }

    private static String decrypt(String text) {
        if (text == null || text.isEmpty()) return "";
        try {
            String[] p = text.split(":");
            Cipher c = Cipher.getInstance("AES/GCM/NoPadding");
            c.init(Cipher.DECRYPT_MODE, key(), new GCMParameterSpec(128, Base64.decode(p[0], Base64.NO_WRAP)));
            return new String(c.doFinal(Base64.decode(p[1], Base64.NO_WRAP)), StandardCharsets.UTF_8);
        } catch (Exception e) {
            return "";
        }
    }
}
