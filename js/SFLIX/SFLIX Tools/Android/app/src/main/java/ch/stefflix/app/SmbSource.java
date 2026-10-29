package ch.stefflix.app;

import com.hierynomus.msdtyp.AccessMask;
import com.hierynomus.mserref.NtStatus;
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
import java.util.EnumSet;
import java.util.concurrent.TimeUnit;

/** Liest Dateien aus einer SMB-Freigabe (SMB2/3). Die Verbindung wird bei Bedarf neu aufgebaut. */
final class SmbSource implements Closeable {

    private final ShareStore.Share cfg;
    private SMBClient client;
    private Connection connection;
    private DiskShare disk;

    SmbSource(ShareStore.Share cfg) {
        this.cfg = cfg;
    }

    private synchronized DiskShare disk() throws IOException {
        if (disk != null && disk.isConnected()) return disk;
        close();
        try {
            SmbConfig conf = SmbConfig.builder()
                    .withSecurityProvider(new BCSecurityProvider())
                    .withTimeout(30, TimeUnit.SECONDS)
                    .withSoTimeout(60, TimeUnit.SECONDS)
                    .build();
            client = new SMBClient(conf);
            connection = client.connect(cfg.host);
            AuthenticationContext auth = cfg.user.isEmpty()
                    ? AuthenticationContext.guest()
                    : new AuthenticationContext(cfg.user, cfg.password.toCharArray(),
                    cfg.domain.isEmpty() ? null : cfg.domain);
            Session session = connection.authenticate(auth);
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

    private String smbPath(String rel) {
        String p = rel.replace('/', '\\');
        while (p.startsWith("\\")) p = p.substring(1);
        String base = cfg.path.replace('/', '\\');
        return base.isEmpty() ? p : base + "\\" + p;
    }

    /** Oeffnet eine Datei zum Lesen. Nicht vorhandene Dateien: FileNotFoundException. */
    File open(String rel) throws IOException {
        String path = smbPath(rel);
        for (int attempt = 0; ; attempt++) {
            try {
                return disk().openFile(path, EnumSet.of(AccessMask.GENERIC_READ), null,
                        SMB2ShareAccess.ALL, SMB2CreateDisposition.FILE_OPEN, null);
            } catch (SMBApiException e) {
                long status = e.getStatusCode();
                if (status == NtStatus.STATUS_OBJECT_NAME_NOT_FOUND.getValue()
                        || status == NtStatus.STATUS_OBJECT_PATH_NOT_FOUND.getValue()
                        || status == NtStatus.STATUS_OBJECT_NAME_INVALID.getValue()
                        || status == NtStatus.STATUS_FILE_IS_A_DIRECTORY.getValue()) {
                    throw new FileNotFoundException(path);
                }
                if (attempt > 0) throw new IOException(e.getMessage(), e);
                close();
            } catch (IOException | RuntimeException e) {
                // Verbindung abgebrochen (z.B. nach Standby): einmal neu verbinden
                if (attempt > 0) throw e instanceof IOException ? (IOException) e : new IOException(e.getMessage(), e);
                close();
            }
        }
    }

    static long size(File f) {
        return f.getFileInformation(FileStandardInformation.class).getEndOfFile();
    }

    /** Prueft, ob die Freigabe erreichbar ist und start.html enthaelt. */
    void test() throws IOException {
        File f = open("start.html");
        f.close();
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

    /** Liest einen Bereich einer Datei in Bloecken, damit Videos schnell laden. */
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
            try { file.close(); } catch (Exception ignored) { }
        }
    }
}
