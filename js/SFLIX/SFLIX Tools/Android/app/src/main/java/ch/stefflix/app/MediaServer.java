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
import java.util.Locale;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

/**
 * Kleiner HTTP-Server nur auf 127.0.0.1 fuer Videos und Musik. Die WebView spielt Medien damit wie
 * von einem normalen Webserver ab (Range/206 fuer Springen und MP4 mit Inhaltsverzeichnis am Ende).
 * Ein zufaelliger Schluessel im Pfad verhindert Zugriffe anderer Apps auf dem Geraet.
 */
final class MediaServer implements Closeable {

    private final SmbSource smb;
    private final String token;
    private final ServerSocket server;
    private final ExecutorService pool = Executors.newCachedThreadPool();
    private volatile boolean running = true;

    MediaServer(SmbSource smb) throws IOException {
        this.smb = smb;
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
                SmbSource.RangeStream data = new SmbSource.RangeStream(file, start, end + 1);
                file = null; // schliesst jetzt der Strom
                try (InputStream body = data) {
                    byte[] buf = new byte[256 * 1024];
                    int n;
                    while ((n = body.read(buf)) > 0) out.write(buf, 0, n);
                }
            }
            out.flush();
        } catch (IOException ignored) {
            // Player hat die Verbindung beendet (z.B. beim Springen)
        } finally {
            if (file != null) {
                try { file.close(); } catch (Exception ignored) { }
            }
        }
    }

    private static void status(OutputStream out, int code, String text) throws IOException {
        out.write(("HTTP/1.1 " + code + " " + text + "\r\nContent-Length: 0\r\nConnection: close\r\n\r\n")
                .getBytes(StandardCharsets.ISO_8859_1));
    }

    @Override
    public void close() {
        running = false;
        try { server.close(); } catch (IOException ignored) { }
        pool.shutdownNow();
    }
}
