package ch.stefflix.app;

import android.content.Context;
import android.util.LruCache;

import com.hierynomus.smbj.share.File;

import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;

/**
 * Zwischenspeicher für Programmdateien (start.html, Skripte, CSS, data.js …) und Vorschaubilder.
 *
 * Statt jede Datei einzeln auf der Freigabe zu öffnen (mehrere Hin- und Rückwege je Datei, über VPN
 * jeweils 30 bis 80 ms), prüft die App Grösse und Änderungszeit über eine Liste je Ordner. Unveränderte
 * Dateien kommen aus dem Arbeitsspeicher oder vom Gerät. Ordner der Programmdateien werden höchstens
 * alle 30 Sekunden neu gelistet, alle übrigen (z.B. Preview) einmal je Sitzung.
 */
final class FileCache {

    /** Grösste Datei, die zwischengespeichert wird (data.js grosser Mediatheken passt). */
    static final long MAX_FILE = 24L * 1024 * 1024;
    private static final long PROGRAM_TTL_MS = 30_000;
    private static final long MEMORY_FILE = 8L * 1024 * 1024;

    /** Arbeitsspeicher, gemeinsam für alle Quellen, solange die App läuft. */
    private static final LruCache<String, Entry> memory = new LruCache<String, Entry>(40 * 1024 * 1024) {
        @Override
        protected int sizeOf(String key, Entry value) {
            return value.data.length;
        }
    };

    private static final class Entry {
        final SmbSource.Meta meta;
        final byte[] data;

        Entry(SmbSource.Meta meta, byte[] data) {
            this.meta = meta;
            this.data = data;
        }
    }

    private static final class Listing {
        final long fetched;
        final Map<String, SmbSource.Meta> files;

        Listing(long fetched, Map<String, SmbSource.Meta> files) {
            this.fetched = fetched;
            this.files = files;
        }
    }

    private final SmbSource smb;

    /** Wird beim Lesen von der Freigabe benachrichtigt (Startbildschirm der App). */
    interface Listener {
        void onNetworkRead(String rel, long done, long total);
    }

    private volatile Listener listener;

    void setListener(Listener l) {
        listener = l;
    }
    private final String shareId;
    private final java.io.File dir;
    private volatile long maxDiskBytes;
    private final Map<String, Listing> listings = new HashMap<>();
    private final Map<String, Object> listingLocks = new HashMap<>();

    FileCache(Context context, SmbSource smb, String shareId, long maxDiskBytes) {
        this.smb = smb;
        this.shareId = shareId;
        this.maxDiskBytes = maxDiskBytes;
        this.dir = new java.io.File(root(context), shareId);
        //noinspection ResultOfMethodCallIgnored
        dir.mkdirs();
    }

    /** Neue Grösse aus den App-Einstellungen. */
    void setMaxDiskBytes(long bytes) {
        maxDiskBytes = bytes;
    }

    static java.io.File root(Context context) {
        return new java.io.File(context.getCacheDir(), "sources");
    }

    /** Belegter Platz aller Quellen auf dem Gerät. */
    static long diskUsage(Context context) {
        long sum = 0;
        java.io.File[] sources = root(context).listFiles();
        if (sources == null) return 0;
        for (java.io.File s : sources) {
            java.io.File[] files = s.listFiles();
            if (files != null) for (java.io.File f : files) sum += f.length();
        }
        return sum;
    }

    /** Leert den Zwischenspeicher aller Quellen (Gerät und Arbeitsspeicher). */
    static void clearAll(Context context) {
        memory.evictAll();
        java.io.File[] sources = root(context).listFiles();
        if (sources == null) return;
        for (java.io.File s : sources) {
            java.io.File[] files = s.listFiles();
            if (files != null) for (java.io.File f : files) //noinspection ResultOfMethodCallIgnored
                f.delete();
        }
    }

    /** Vergisst die Ordnerlisten, z.B. nach einem Verbindungsabbruch oder "Erneut versuchen". */
    void invalidate() {
        synchronized (listings) {
            listings.clear();
        }
    }

    private static boolean isProgramFolder(String folder) {
        String f = folder.toLowerCase(Locale.ROOT);
        return f.isEmpty() || f.equals("sflix_sys") || f.startsWith("sflix_sys/");
    }

    private Object lockFor(String folder) {
        synchronized (listingLocks) {
            Object lock = listingLocks.get(folder);
            if (lock == null) {
                lock = new Object();
                listingLocks.put(folder, lock);
            }
            return lock;
        }
    }

    /** Grösse und Änderungszeit aus der Ordnerliste, null wenn die Datei dort nicht steht. */
    private SmbSource.Meta lookup(String rel) throws IOException {
        int slash = rel.lastIndexOf('/');
        String folder = slash < 0 ? "" : rel.substring(0, slash);
        String name = rel.substring(slash + 1);
        long maxAge = isProgramFolder(folder) ? PROGRAM_TTL_MS : Long.MAX_VALUE;
        return listing(folder, maxAge, true).files.get(name);
    }

    /** Ordnerliste, neu geholt wenn sie älter als maxAgeMs ist. Gleichzeitige Anfragen warten aufeinander. */
    private Listing listing(String folder, long maxAgeMs, boolean forPage) throws IOException {
        String key = folder.toLowerCase(Locale.ROOT);
        synchronized (lockFor(key)) {
            Listing l;
            synchronized (listings) {
                l = listings.get(key);
            }
            long now = System.currentTimeMillis();
            if (l == null || now - l.fetched > maxAgeMs) {
                l = new Listing(now, smb.list(folder));
                if (forPage) LoadStats.listing();
                synchronized (listings) {
                    listings.put(key, l);
                }
            }
            return l;
        }
    }

    /** Ordner der Programmdateien: start.html, sflix_sys (data.js …) und sflix_sys/js. */
    private static final String[] PROGRAM_FOLDERS = {"", "sflix_sys", "sflix_sys/js"};

    /**
     * Holt die Listen der Programmordner im Hintergrund, alle gleichzeitig (baut dabei die Verbindung
     * auf). Listen, die jünger als maxAgeMs sind, bleiben. So muss ein Seitenaufruf nicht darauf warten.
     */
    void warmUp(java.util.concurrent.Executor pool, long maxAgeMs) {
        for (String folder : PROGRAM_FOLDERS) {
            pool.execute(() -> {
                try {
                    listing(folder, maxAgeMs, false);
                } catch (IOException | RuntimeException ignored) {
                    // Keine Verbindung: der nächste Seitenaufruf meldet es
                }
            });
        }
    }

    /**
     * Inhalt einer Datei aus Arbeitsspeicher, Gerät oder Freigabe.
     * null: zu gross zum Zwischenspeichern (dann direkt lesen). FileNotFoundException: fehlt.
     */
    byte[] read(String rel) throws IOException {
        SmbSource.Meta meta = lookup(rel);
        if (meta == null) {
            // Nicht in der Liste (z.B. andere Unicode-Schreibweise eines Umlauts): direkt versuchen
            return readDirect(rel);
        }
        if (meta.size > MAX_FILE) return null;
        String memKey = shareId + "/" + rel.toLowerCase(Locale.ROOT);
        Entry e = memory.get(memKey);
        if (e != null && e.meta.same(meta)) {
            LoadStats.hit();
            return e.data;
        }
        java.io.File file = new java.io.File(dir, fileName(rel, meta));
        if (file.isFile() && file.length() == meta.size) {
            byte[] data = readFile(file, (int) meta.size);
            if (data != null) {
                //noinspection ResultOfMethodCallIgnored
                file.setLastModified(System.currentTimeMillis());
                remember(memKey, meta, data);
                LoadStats.hit();
                return data;
            }
        }
        File f = smb.open(rel);
        byte[] data;
        try {
            final Listener l = listener;
            data = SmbSource.readAll(f, meta.size, l == null ? null : (done, total) -> l.onNetworkRead(rel, done, total));
        } finally {
            SmbSource.closeLater(f);
        }
        LoadStats.network(data.length);
        remember(memKey, meta, data);
        store(file, data);
        return data;
    }

    private byte[] readDirect(String rel) throws IOException {
        File f = smb.open(rel);
        try {
            long size = SmbSource.size(f);
            if (size > MAX_FILE) return null;
            final Listener l = listener;
            byte[] data = SmbSource.readAll(f, size, l == null ? null : (done, total) -> l.onNetworkRead(rel, done, total));
            LoadStats.network(data.length);
            return data;
        } finally {
            SmbSource.closeLater(f);
        }
    }

    private void remember(String key, SmbSource.Meta meta, byte[] data) {
        if (data.length <= MEMORY_FILE) memory.put(key, new Entry(meta, data));
    }

    private static String fileName(String rel, SmbSource.Meta meta) {
        try {
            MessageDigest md = MessageDigest.getInstance("SHA-1");
            byte[] h = md.digest((rel.toLowerCase(Locale.ROOT) + "|" + meta.size + "|" + meta.modified)
                    .getBytes(StandardCharsets.UTF_8));
            StringBuilder sb = new StringBuilder();
            for (byte b : h) sb.append(String.format(Locale.ROOT, "%02x", b));
            return sb.toString();
        } catch (Exception e) {
            return Integer.toHexString(rel.hashCode()) + "_" + meta.size + "_" + meta.modified;
        }
    }

    private static byte[] readFile(java.io.File file, int size) {
        byte[] data = new byte[size];
        try (FileInputStream in = new FileInputStream(file)) {
            int read = 0;
            while (read < size) {
                int n = in.read(data, read, size - read);
                if (n <= 0) return null;
                read += n;
            }
            return data;
        } catch (IOException e) {
            return null;
        }
    }

    private void store(java.io.File file, byte[] data) {
        if (maxDiskBytes <= 0) return;
        java.io.File tmp = new java.io.File(dir, file.getName() + ".tmp");
        try (FileOutputStream out = new FileOutputStream(tmp)) {
            out.write(data);
        } catch (IOException e) {
            //noinspection ResultOfMethodCallIgnored
            tmp.delete();
            return;
        }
        if (!tmp.renameTo(file)) {
            //noinspection ResultOfMethodCallIgnored
            tmp.delete();
            return;
        }
        trimIfNeeded(file.getParentFile().getParentFile(), maxDiskBytes);
    }

    private static long lastTrimCheck;

    /** Löscht die am längsten nicht mehr gebrauchten Dateien, bis höchstens 90 % belegt sind. */
    private static synchronized void trimIfNeeded(java.io.File root, long max) {
        long now = System.currentTimeMillis();
        if (now - lastTrimCheck < 10_000) return;
        lastTrimCheck = now;
        List<java.io.File> all = new ArrayList<>();
        long sum = 0;
        java.io.File[] sources = root.listFiles();
        if (sources == null) return;
        for (java.io.File s : sources) {
            java.io.File[] files = s.listFiles();
            if (files == null) continue;
            all.addAll(Arrays.asList(files));
            for (java.io.File f : files) sum += f.length();
        }
        if (sum <= max) return;
        all.sort((a, b) -> Long.compare(a.lastModified(), b.lastModified()));
        long target = max * 9 / 10;
        for (java.io.File f : all) {
            if (sum <= target) break;
            long len = f.length();
            if (f.delete()) sum -= len;
        }
    }
}
