package ch.stefflix.app;

import android.net.Uri;

import com.hierynomus.smbj.share.File;

import java.io.BufferedInputStream;
import java.io.ByteArrayOutputStream;
import java.io.Closeable;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.ServerSocket;
import java.net.Socket;
import java.nio.charset.StandardCharsets;
import java.security.SecureRandom;
import java.util.Arrays;
import java.util.Locale;
import java.util.concurrent.ArrayBlockingQueue;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Semaphore;
import java.util.concurrent.TimeUnit;

/**
 * Kleiner HTTP-Server nur auf 127.0.0.1 fuer Videos und Musik. Die WebView spielt Medien damit wie
 * von einem normalen Webserver ab (Range/206 fuer Springen und MP4 mit Inhaltsverzeichnis am Ende).
 * Ein zufaelliger Schluessel im Pfad verhindert Zugriffe anderer Apps auf dem Geraet.
 *
 * Videopuffer: Ein Hintergrund-Thread liest voraus (insgesamt hoechstens bufferMb fuer alle laufenden
 * Anfragen). Bricht die Verbindung ab, verbindet er bis RETRY_MS lang neu und liest an derselben
 * Stelle weiter. Kurze Abbrueche ueberbrueckt so der Puffer, ohne dass die Wiedergabe stockt.
 */
final class MediaServer implements Closeable {

    private static final int CHUNK = 512 * 1024;
    private static final long RETRY_MS = 30_000;

    interface Listener {
        /** Die Verbindung blieb trotz Neuverbinden weg. */
        void onMediaConnectionLost(Exception e);
    }

    private final SmbSource smb;
    private final String token;
    private final ServerSocket server;
    private final ExecutorService pool = Executors.newCachedThreadPool();
    private final Semaphore budget;
    private final Listener listener;
    private volatile boolean running = true;

    MediaServer(SmbSource smb, int bufferMb, Listener listener) throws IOException {
        this.smb = smb;
        this.listener = listener;
        this.budget = new Semaphore(Math.max(4, bufferMb * 1024 * 1024 / CHUNK));
        byte[] r = new byte[16];
        new SecureRandom().nextBytes(r);
        StringBuilder sb = new StringBuilder();
        for (byte b : r) sb.append(String.format(Locale.ROOT, "%02x", b));
        token = sb.toString();
        server = new ServerSocket(0, 50, InetAddress.getByName("127.0.0.1"));
        Thread t = new Thread(this::acceptLoop, "stefflix-media");
        t.setDaemon(true);
        t.start();
    }

    /** Basisadresse, entspricht dem STEFFLIX-Ordner (mit Schraegstrich am Ende). */
    String base() {
        return "http://127.0.0.1:" + server.getLocalPort() + "/" + token + "/";
    }

    private void acceptLoop() {
        while (running) {
            try {
                Socket s = server.accept();
                pool.execute(() -> handle(s));
            } catch (IOException e) {
                if (!running) return;
            }
        }
    }

    private static String readLine(InputStream in) throws IOException {
        ByteArrayOutputStream line = new ByteArrayOutputStream();
        int c;
        while ((c = in.read()) != -1) {
            if (c == '\n') break;
            if (c != '\r') line.write(c);
            if (line.size() > 16384) throw new IOException("Zeile zu lang");
        }
        if (c == -1 && line.size() == 0) return null;
        return line.toString("ISO-8859-1");
    }

    private void handle(Socket socket) {
        File file = null;
        try (Socket s = socket) {
            s.setSoTimeout(30000);
            InputStream in = new BufferedInputStream(s.getInputStream());
            OutputStream out = s.getOutputStream();
            String request = readLine(in);
            if (request == null) return;
            String range = null;
            String header;
            while ((header = readLine(in)) != null && !header.isEmpty()) {
                int colon = header.indexOf(':');
                if (colon > 0 && header.substring(0, colon).trim().equalsIgnoreCase("range")) {
                    range = header.substring(colon + 1).trim();
                }
            }
            String[] parts = request.split(" ");
            if (parts.length < 2) return;
            boolean head = parts[0].equals("HEAD");
            if (!head && !parts[0].equals("GET")) {
                status(out, 405, "Method Not Allowed");
                return;
            }
            String raw = parts[1];
            int q = raw.indexOf('?');
            if (q >= 0) raw = raw.substring(0, q);
            String prefix = "/" + token + "/";
            if (!raw.startsWith(prefix)) {
                status(out, 403, "Forbidden");
                return;
            }
            String rel = Uri.decode(raw.substring(prefix.length()));

            try {
                file = smb.open(rel);
            } catch (FileNotFoundException e) {
                status(out, 404, "Not Found");
                return;
            }
            long size = SmbSource.size(file);
            long[] r = BrowserActivity.parseRange(range, size);
            StringBuilder h = new StringBuilder();
            if (r == null) {
                h.append("HTTP/1.1 416 Range Not Satisfiable\r\nContent-Range: bytes */").append(size)
                        .append("\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                out.write(h.toString().getBytes(StandardCharsets.ISO_8859_1));
                return;
            }
            long start = r[0], end = r[1];
            boolean partial = range != null && size > 0;
            h.append(partial ? "HTTP/1.1 206 Partial Content\r\n" : "HTTP/1.1 200 OK\r\n");
            h.append("Content-Type: ").append(BrowserActivity.mimeType(rel)).append("\r\n");
            h.append("Content-Length: ").append(Math.max(0, end - start + 1)).append("\r\n");
            if (partial) h.append("Content-Range: bytes ").append(start).append('-').append(end).append('/').append(size).append("\r\n");
            h.append("Accept-Ranges: bytes\r\nAccess-Control-Allow-Origin: *\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n");
            out.write(h.toString().getBytes(StandardCharsets.ISO_8859_1));
            if (!head && size > 0) {
                Prefetch data = new Prefetch(rel, file, start, end + 1);
                file = null; // gehoert jetzt dem Puffer
                try (InputStream body = data) {
                    byte[] buf = new byte[256 * 1024];
                    int n;
                    while ((n = body.read(buf)) > 0) out.write(buf, 0, n);
                }
            }
            out.flush();
        } catch (IOException ignored) {
            // Player hat die Verbindung beendet (z.B. beim Springen) oder die Freigabe ist weg
        } finally {
            if (file != null) SmbSource.closeLater(file);
        }
    }

    private static void status(OutputStream out, int code, String text) throws IOException {
        out.write(("HTTP/1.1 " + code + " " + text + "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n")
                .getBytes(StandardCharsets.ISO_8859_1));
    }

    private static final byte[] EOF = new byte[0];

    /** Liest im Hintergrund voraus und verbindet bei einem Abbruch neu. */
    private final class Prefetch extends InputStream {
        private final String rel;
        private final long end;
        private final BlockingQueue<byte[]> queue = new ArrayBlockingQueue<>(1024);
        private volatile boolean closed;
        private volatile IOException error;
        private int held; // Puffer-Erlaubnisse, die noch in der Warteschlange stecken
        private byte[] current;
        private int currentPos;

        Prefetch(String rel, File first, long start, long endExclusive) {
            this.rel = rel;
            this.end = endExclusive;
            Thread t = new Thread(() -> produce(first, start), "stefflix-prefetch");
            t.setDaemon(true);
            t.start();
        }

        private void produce(File first, long start) {
            File f = first;
            long pos = start;
            try {
                while (!closed && pos < end) {
                    if (!budget.tryAcquire(500, TimeUnit.MILLISECONDS)) continue;
                    synchronized (this) {
                        held++;
                    }
                    int want = (int) Math.min(CHUNK, end - pos);
                    byte[] b = new byte[want];
                    int n;
                    try {
                        n = f.read(b, pos, 0, want);
                    } catch (RuntimeException e) {
                        release(1);
                        SmbSource.closeLater(f);
                        f = reconnect(e);
                        if (f == null) return;
                        continue;
                    }
                    if (n <= 0) {
                        release(1);
                        break;
                    }
                    pos += n;
                    queue.put(n == want ? b : Arrays.copyOf(b, n));
                }
            } catch (InterruptedException ignored) {
            } finally {
                if (f != null) SmbSource.closeLater(f);
                queue.offer(EOF);
                if (closed) {
                    synchronized (this) {
                        budget.release(held);
                        held = 0;
                    }
                }
            }
        }

        /** Verbindet neu, bis RETRY_MS vorbei sind. null: aufgegeben (Fehler ist gesetzt). */
        private File reconnect(Exception cause) throws InterruptedException {
            long until = System.currentTimeMillis() + RETRY_MS;
            Exception last = cause;
            while (!closed && System.currentTimeMillis() < until) {
                smb.close();
                try {
                    return smb.open(rel);
                } catch (IOException e) {
                    last = e;
                    Thread.sleep(2000);
                }
            }
            if (!closed) {
                error = new IOException(SmbSource.reason(last), last);
                if (listener != null) listener.onMediaConnectionLost(last);
            }
            return null;
        }

        private synchronized void release(int count) {
            int n = Math.min(count, held);
            held -= n;
            if (n > 0) budget.release(n);
        }

        private boolean next() throws IOException {
            while (true) {
                if (closed) return false;
                byte[] b;
                try {
                    b = queue.poll(500, TimeUnit.MILLISECONDS);
                } catch (InterruptedException e) {
                    throw new IOException("unterbrochen");
                }
                if (b == null) continue;
                if (b == EOF) {
                    queue.offer(EOF);
                    if (error != null) throw error;
                    return false;
                }
                release(1);
                current = b;
                currentPos = 0;
                return true;
            }
        }

        @Override
        public int read() throws IOException {
            byte[] one = new byte[1];
            return read(one, 0, 1) <= 0 ? -1 : one[0] & 0xff;
        }

        @Override
        public int read(byte[] b, int off, int len) throws IOException {
            if (len == 0) return 0;
            if ((current == null || currentPos >= current.length) && !next()) return -1;
            int n = Math.min(len, current.length - currentPos);
            System.arraycopy(current, currentPos, b, off, n);
            currentPos += n;
            return n;
        }

        @Override
        public void close() {
            closed = true;
            queue.clear();
            synchronized (this) {
                budget.release(held);
                held = 0;
            }
        }
    }

    @Override
    public void close() {
        running = false;
        try { server.close(); } catch (IOException ignored) { }
        pool.shutdownNow();
    }
}
