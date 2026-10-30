package ch.stefflix.app;

import java.util.ArrayDeque;
import java.util.Deque;
import java.util.Locale;

/** Misst je Seitenaufruf, wie viel von der Freigabe kam und wie lange es dauerte (für die Diagnose). */
final class LoadStats {

    private static long start;
    private static int listings, hits, networkFiles;
    private static long networkBytes;
    private static final Deque<String> recent = new ArrayDeque<>();

    private LoadStats() {
    }

    static synchronized void begin() {
        start = System.nanoTime();
        listings = hits = networkFiles = 0;
        networkBytes = 0;
    }

    static synchronized void listing() { listings++; }

    /** Dateien, die beim laufenden Seitenaufruf von der Freigabe kamen (nicht aus dem Zwischenspeicher). */
    static synchronized int networkFiles() { return networkFiles; }

    static synchronized void hit() { hits++; }

    static synchronized void network(long bytes) {
        networkFiles++;
        networkBytes += bytes;
    }

    /** Seite fertig geladen (onPageFinished). */
    static synchronized void finish(String page) {
        if (start == 0) return;
        long ms = (System.nanoTime() - start) / 1_000_000;
        start = 0;
        String line = String.format(Locale.ROOT, "%s: %.1f s, %d aus Zwischenspeicher, %d von der Freigabe (%.1f MB), %d Ordnerlisten",
                page, ms / 1000.0, hits, networkFiles, networkBytes / 1048576.0, listings);
        recent.addFirst(line);
        while (recent.size() > 5) recent.removeLast();
    }

    static synchronized String text() {
        if (recent.isEmpty()) return "  (noch keine Messung)";
        StringBuilder sb = new StringBuilder();
        for (String line : recent) sb.append("  ").append(line).append('\n');
        return sb.toString().trim().isEmpty() ? "" : sb.substring(0, sb.length() - 1);
    }
}
