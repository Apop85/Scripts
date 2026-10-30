package ch.stefflix.app;

import com.hierynomus.msdtyp.AccessMask;
import com.hierynomus.mserref.NtStatus;
import com.hierynomus.msfscc.FileAttributes;
import com.hierynomus.msfscc.fileinformation.FileIdBothDirectoryInformation;
import com.hierynomus.msfscc.fileinformation.FileStandardInformation;
import com.hierynomus.mssmb2.SMB2CreateDisposition;
import com.hierynomus.mssmb2.SMB2ShareAccess;
import com.hierynomus.mssmb2.SMBApiException;
import com.hierynomus.security.bc.BCSecurityProvider;
import com.hierynomus.smbj.SMBClient;
import com.hierynomus.smbj.SmbConfig;
import com.hierynomus.smbj.auth.AuthenticationContext;
import com.hierynomus.smbj.connection.Connection;
import com.hierynomus.smbj.session.Session;
import com.hierynomus.smbj.share.DiskShare;
import com.hierynomus.smbj.share.File;

import java.io.Closeable;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.net.ConnectException;
import java.net.NoRouteToHostException;
import java.net.SocketTimeoutException;
import java.net.UnknownHostException;
import java.util.EnumSet;
import java.util.List;
import java.util.Map;
import java.util.TreeMap;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;

/** Liest Dateien aus einer SMB-Freigabe (SMB2/3). Die Verbindung wird bei Bedarf neu aufgebaut. */
final class SmbSource implements Closeable {

    private static final ExecutorService closer = Executors.newSingleThreadExecutor(r -> {
        Thread t = new Thread(r, "stefflix-close");
        t.setDaemon(true);
        return t;
    });

    private final ShareStore.Share cfg;
    private SMBClient client;
    private Connection connection;
    private DiskShare disk;

    SmbSource(ShareStore.Share cfg) {
        this.cfg = cfg;
    }

    ShareStore.Share share() {
        return cfg;
    }

    private static SmbConfig config() {
        // Kurze Zeitlimits: Bricht die Verbindung ab (z.B. VPN), merkt die App es schnell und verbindet neu
        return SmbConfig.builder()
                .withSecurityProvider(new BCSecurityProvider())
                .withTimeout(20, TimeUnit.SECONDS)
                .withSoTimeout(25, TimeUnit.SECONDS)
                .build();
    }

    private synchronized DiskShare disk() throws IOException {
        if (disk != null && disk.isConnected()) return disk;
        close();
        try {
            client = new SMBClient(config());
            connection = client.connect(cfg.host);
            Session session = connection.authenticate(auth());
            disk = (DiskShare) session.connectShare(cfg.share);
            return disk;
        } catch (IOException e) {
            close();
            throw e;
        } catch (RuntimeException e) {
            close();
            throw new IOException(e.getMessage(), e);
        }
    }

    private AuthenticationContext auth() {
        return cfg.user.isEmpty()
                ? AuthenticationContext.guest()
                : new AuthenticationContext(cfg.user, cfg.password.toCharArray(),
                cfg.domain.isEmpty() ? null : cfg.domain);
    }

    private String smbPath(String rel) {
        String p = rel.replace('/', '\\');
        while (p.startsWith("\\")) p = p.substring(1);
        while (p.endsWith("\\")) p = p.substring(0, p.length() - 1);
        String base = cfg.path.replace('/', '\\');
        if (base.isEmpty()) return p;
        return p.isEmpty() ? base : base + "\\" + p;
    }

    private static boolean isNotFound(SMBApiException e) {
        long status = e.getStatusCode();
        return status == NtStatus.STATUS_OBJECT_NAME_NOT_FOUND.getValue()
                || status == NtStatus.STATUS_OBJECT_PATH_NOT_FOUND.getValue()
                || status == NtStatus.STATUS_OBJECT_NAME_INVALID.getValue()
                || status == NtStatus.STATUS_FILE_IS_A_DIRECTORY.getValue()
                || status == NtStatus.STATUS_NOT_A_DIRECTORY.getValue();
    }

    /** Oeffnet eine Datei zum Lesen. Nicht vorhandene Dateien: FileNotFoundException. */
    File open(String rel) throws IOException {
        String path = smbPath(rel);
        for (int attempt = 0; ; attempt++) {
            try {
                return disk().openFile(path, EnumSet.of(AccessMask.GENERIC_READ), null,
                        SMB2ShareAccess.ALL, SMB2CreateDisposition.FILE_OPEN, null);
            } catch (SMBApiException e) {
                if (isNotFound(e)) throw new FileNotFoundException(path);
                if (attempt > 0) throw new IOException(e.getMessage(), e);
                close();
            } catch (IOException | RuntimeException e) {
                // Verbindung abgebrochen (z.B. nach Standby): einmal neu verbinden
                if (attempt > 0) throw e instanceof IOException ? (IOException) e : new IOException(e.getMessage(), e);
                close();
            }
        }
    }

    /** Eintrag einer Ordnerliste: Grösse und Änderungszeit. */
    static final class Meta {
        final long size, modified;

        Meta(long size, long modified) {
            this.size = size;
            this.modified = modified;
        }

        boolean same(Meta o) {
            return o != null && o.size == size && o.modified == modified;
        }
    }

    /** Dateien eines Ordners (ohne Unterordner), Namen ohne Gross-/Kleinschreibung.
     *  Ein Aufruf für den ganzen Ordner statt mehrerer je Datei. Fehlt der Ordner: leere Liste. */
    Map<String, Meta> list(String relFolder) throws IOException {
        String path = smbPath(relFolder);
        for (int attempt = 0; ; attempt++) {
            try {
                List<FileIdBothDirectoryInformation> entries = disk().list(path);
                Map<String, Meta> map = new TreeMap<>(String.CASE_INSENSITIVE_ORDER);
                for (FileIdBothDirectoryInformation e : entries) {
                    if ((e.getFileAttributes() & FileAttributes.FILE_ATTRIBUTE_DIRECTORY.getValue()) != 0) continue;
                    map.put(e.getFileName(), new Meta(e.getEndOfFile(), e.getLastWriteTime().toEpochMillis()));
                }
                return map;
            } catch (SMBApiException e) {
                if (isNotFound(e)) return new TreeMap<>(String.CASE_INSENSITIVE_ORDER);
                if (attempt > 0) throw new IOException(e.getMessage(), e);
                close();
            } catch (IOException | RuntimeException e) {
                if (attempt > 0) throw e instanceof IOException ? (IOException) e : new IOException(e.getMessage(), e);
                close();
            }
        }
    }

    /**
     * Hält die Verbindung offen: eine kleine Anfrage, damit NAS und VPN sie nicht wegen Leerlauf
     * schliessen. Ist sie trotzdem weg, wird sofort neu verbunden. true: Verbindung steht.
     */
    boolean keepAlive() {
        try {
            disk().fileExists(smbPath("start.html"));
            return true;
        } catch (IOException | RuntimeException e) {
            close();
            try {
                disk();
                return true;
            } catch (IOException | RuntimeException again) {
                return false;
            }
        }
    }

    static long size(File f) {
        return f.getFileInformation(FileStandardInformation.class).getEndOfFile();
    }

    /** Liest size Bytes ab Anfang (Grösse aus der Ordnerliste, ohne eigene Abfrage). */
    static byte[] readAll(File f, long size) throws IOException {
        if (size > Integer.MAX_VALUE - 16) throw new IOException("Datei zu gross");
        byte[] data = new byte[(int) size];
        int read = 0;
        try {
            while (read < data.length) {
                int n = f.read(data, read, read, Math.min(1024 * 1024, data.length - read));
                if (n <= 0) break;
                read += n;
            }
        } catch (RuntimeException e) {
            throw new IOException(e.getMessage(), e);
        }
        if (read < data.length) throw new IOException("Datei unvollständig gelesen");
        return data;
    }

    /** Schliesst im Hintergrund, ohne auf die Antwort des Servers zu warten. */
    static void closeLater(File f) {
        if (f == null) return;
        closer.execute(() -> {
            try { f.closeNoWait(); } catch (Exception ignored) { }
        });
    }

    // ---- Verbindungstest ----

    /** Ergebnis des Verbindungstests. error == null: in Ordnung. */
    static final class TestResult {
        String error;
        long latencyMs = -1;
        double mbPerSecond = -1;
    }

    /** Prüft Schritt für Schritt und nennt den Grund eines Fehlers. Mit speed: zusätzlich Lesegeschwindigkeit. */
    TestResult test(boolean speed) {
        TestResult r = new TestResult();
        close();
        try {
            client = new SMBClient(config());
            try {
                connection = client.connect(cfg.host);
            } catch (IOException | RuntimeException e) {
                r.error = "Server nicht erreichbar: " + cfg.host + "\n" + reason(e);
                return r;
            }
            Session session;
            try {
                session = connection.authenticate(auth());
            } catch (SMBApiException e) {
                long s = e.getStatusCode();
                if (s == NtStatus.STATUS_LOGON_FAILURE.getValue() || s == NtStatus.STATUS_ACCESS_DENIED.getValue()) {
                    r.error = "Anmeldung abgelehnt. Bitte Benutzer, Passwort und Domäne prüfen.";
                } else {
                    r.error = "Anmeldung fehlgeschlagen: " + reason(e);
                }
                return r;
            } catch (RuntimeException e) {
                r.error = "Anmeldung fehlgeschlagen: " + reason(e);
                return r;
            }
            try {
                disk = (DiskShare) session.connectShare(cfg.share);
            } catch (SMBApiException e) {
                long s = e.getStatusCode();
                if (s == NtStatus.STATUS_ACCESS_DENIED.getValue()) {
                    r.error = "Kein Zugriff auf die Freigabe \"" + cfg.share + "\". Bitte die Rechte des Benutzers prüfen.";
                } else {
                    r.error = "Freigabe \"" + cfg.share + "\" nicht gefunden auf " + cfg.host + ".";
                }
                return r;
            } catch (RuntimeException e) {
                r.error = "Freigabe \"" + cfg.share + "\" nicht erreichbar: " + reason(e);
                return r;
            }
            try {
                if (!cfg.path.isEmpty() && !disk.folderExists(smbPath(""))) {
                    r.error = "Ordner \"" + cfg.path + "\" nicht gefunden in der Freigabe \"" + cfg.share + "\".";
                    return r;
                }
                long t0 = System.nanoTime();
                boolean hasStart = disk.fileExists(smbPath("start.html"));
                r.latencyMs = (System.nanoTime() - t0) / 1_000_000;
                if (!hasStart) {
                    r.error = "Verbindung klappt, aber im Ordner liegt keine start.html. Ist das der STEFFLIX-Ordner?";
                    return r;
                }
            } catch (SMBApiException e) {
                if (e.getStatusCode() == NtStatus.STATUS_ACCESS_DENIED.getValue()) {
                    r.error = "Kein Zugriff auf den Ordner. Bitte die Rechte des Benutzers prüfen.";
                } else {
                    r.error = "Ordner nicht lesbar: " + reason(e);
                }
                return r;
            }
            if (speed) measureSpeed(r);
            return r;
        } catch (Exception e) {
            r.error = "Verbindung fehlgeschlagen: " + reason(e);
            return r;
        }
    }

    /** Liest bis 8 MB der grössten Programmdatei (meist data.js) und misst die Geschwindigkeit. */
    private void measureSpeed(TestResult r) {
        String[] candidates = {"sflix_sys/data.js", "sflix_sys/sflix.css", "start.html"};
        for (String rel : candidates) {
            File f = null;
            try {
                f = open(rel);
                long size = Math.min(size(f), 8L * 1024 * 1024);
                if (size < 64 * 1024) continue;
                byte[] buf = new byte[1024 * 1024];
                long done = 0;
                long t0 = System.nanoTime();
                while (done < size) {
                    int n = f.read(buf, done, 0, (int) Math.min(buf.length, size - done));
                    if (n <= 0) break;
                    done += n;
                }
                double seconds = (System.nanoTime() - t0) / 1e9;
                if (seconds > 0 && done > 0) r.mbPerSecond = done / 1048576.0 / seconds;
                return;
            } catch (Exception ignored) {
            } finally {
                if (f != null) try { f.close(); } catch (Exception ignored) { }
            }
        }
    }

    /** Verständlicher Grund eines Verbindungsfehlers (für Hinweise und den Verbindungstest). */
    static String reason(Throwable e) {
        for (Throwable t = e; t != null; t = t.getCause()) {
            if (t instanceof UnknownHostException) return "Servername unbekannt. Adresse prüfen oder VPN einschalten.";
            if (t instanceof NoRouteToHostException || t instanceof ConnectException)
                return "Server antwortet nicht. Ist das Gerät im selben Netz bzw. das VPN verbunden?";
            if (t instanceof SocketTimeoutException) return "Zeitüberschreitung. Der Server antwortet nicht rechtzeitig.";
            if (t instanceof FileNotFoundException) return "Datei nicht gefunden: " + t.getMessage();
            if (t instanceof SMBApiException) {
                long s = ((SMBApiException) t).getStatusCode();
                if (s == NtStatus.STATUS_LOGON_FAILURE.getValue()) return "Anmeldung abgelehnt.";
                if (s == NtStatus.STATUS_ACCESS_DENIED.getValue()) return "Zugriff verweigert.";
                if (s == NtStatus.STATUS_BAD_NETWORK_NAME.getValue()) return "Freigabe nicht gefunden.";
            }
        }
        String m = e.getMessage();
        return m == null || m.isEmpty() ? e.getClass().getSimpleName() : m;
    }

    @Override
    public synchronized void close() {
        try { if (disk != null) disk.close(); } catch (Exception ignored) { }
        try { if (connection != null) connection.close(); } catch (Exception ignored) { }
        try { if (client != null) client.close(); } catch (Exception ignored) { }
        disk = null;
        connection = null;
        client = null;
    }

    /** Liest einen Bereich einer Datei in Bloecken (fuer Untertitel und grosse Dateien). */
    static final class RangeStream extends InputStream {
        private static final int CHUNK = 512 * 1024;
        private final File file;
        private final long end;
        private long pos;
        private final byte[] buf = new byte[CHUNK];
        private int bufPos, bufLen;

        RangeStream(File file, long start, long endExclusive) {
            this.file = file;
            this.pos = start;
            this.end = endExclusive;
        }

        private boolean fill() throws IOException {
            if (pos >= end) return false;
            int want = (int) Math.min(CHUNK, end - pos);
            int n;
            try {
                n = file.read(buf, pos, 0, want);
            } catch (RuntimeException e) {
                throw new IOException(e.getMessage(), e);
            }
            if (n <= 0) return false;
            pos += n;
            bufPos = 0;
            bufLen = n;
            return true;
        }

        @Override
        public int read() throws IOException {
            if (bufPos >= bufLen && !fill()) return -1;
            return buf[bufPos++] & 0xff;
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            if (len == 0) return 0;
            if (bufPos >= bufLen && !fill()) return -1;
            int n = Math.min(len, bufLen - bufPos);
            System.arraycopy(buf, bufPos, b, off, n);
            bufPos += n;
            return n;
        }

        /** Springt nur die Leseposition vor, ohne die übersprungenen Daten über das Netz zu lesen. */
        @Override
        public long skip(long n) {
            if (n <= 0) return 0;
            long buffered = bufLen - bufPos;
            if (n <= buffered) {
                bufPos += (int) n;
                return n;
            }
            bufPos = bufLen;
            long jump = Math.min(n - buffered, Math.max(0, end - pos));
            pos += jump;
            return buffered + jump;
        }

        @Override
        public int available() {
            return bufLen - bufPos;
        }

        @Override
        public void close() {
            closeLater(file);
        }
    }
}
