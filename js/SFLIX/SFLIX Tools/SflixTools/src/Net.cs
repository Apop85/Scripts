// Download- und Prüffunktionen (Portierung von sflix_update.py).
//
// Jede heruntergeladene Datei wird geprüft:
//   1. HTTP-Status 200 und nicht leer
//   2. .exe-Dateien müssen echte Windows-Programme sein (MZ- und PE-Kennung)
//   3. Textdateien müssen gültiges UTF-8 sein
//   4. Die SHA256-Prüfsumme muss mit checksums.json übereinstimmen
// Erst wenn alle Prüfungen bestanden sind, wird die vorhandene Datei ersetzt.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Sflix
{
    public class DownloadException : Exception
    {
        public DownloadException(string message) : base(message) { }
    }

    public static class Net
    {
        public static string RawUrl = "https://raw.githubusercontent.com/Apop85/Scripts/master/js/SFLIX/";
        public static string BinUrl = "https://github.com/Apop85/Scripts/raw/master/js/SFLIX/";

        public const string ChecksumFile = "checksums.json";
        public const string FfmpegManifest = "ffmpeg.json";
        const int TimeoutMs = 120000;

        static Net()
        {
            // Zum Testen eines Updates vor dem Veröffentlichen, z.B.:
            //   set SFLIX_UPDATE_URL=http://127.0.0.1:8000/
            string over = Environment.GetEnvironmentVariable("SFLIX_UPDATE_URL");
            if (!string.IsNullOrEmpty(over)) RawUrl = BinUrl = over.TrimEnd('/') + "/";
            // GitHub verlangt TLS 1.2
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
        }

        // Dateien der Weboberfläche, die jede Installation braucht
        public static readonly string[] WebFiles =
        {
            "sflix_sys/sflix.css",
            "sflix_sys/js/core.js",
            "sflix_sys/js/storage.js",
            "sflix_sys/js/cards.js",
            "sflix_sys/js/pages.js",
            "sflix_sys/js/player.js",
            "sflix_sys/js/ui.js",
            "sflix_sys/js/main.js",
            "sflix_sys/splash.svg",
            "sflix_sys/sflixLogo.png",
            "sflix_sys/sflixLogo.svg",
            "sflix_sys/version.js",
            "start.html",
        };

        // Dateien früherer Versionen, die nach einem erfolgreichen Update gelöscht werden.
        // updateData.exe und updater.exe sind in diesem Programm aufgegangen.
        public static readonly string[] ObsoleteFiles =
        {
            "sflix_sys/sflix.js", "sflix_sys/sflix_bg.jpg", "updateData.exe", "updater.exe",
        };

        static readonly string[] TextExtensions = { ".js", ".css", ".html", ".txt", ".json", ".svg" };

        public static string UrlFor(string path)
        {
            return (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? BinUrl : RawUrl) + path;
        }

        public static bool IsTextFile(string path)
        {
            string p = path.ToLowerInvariant();
            foreach (string e in TextExtensions) if (p.EndsWith(e)) return true;
            return false;
        }

        // SHA256 wie in sflix_update.py: bei Textdateien CRLF -> LF, damit Windows-Arbeitskopie
        // und GitHub dieselbe Summe ergeben.
        public static string Checksum(string path, byte[] data)
        {
            if (IsTextFile(path))
            {
                MemoryStream ms = new MemoryStream(data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == '\r' && i + 1 < data.Length && data[i + 1] == '\n') continue;
                    ms.WriteByte(data[i]);
                }
                data = ms.ToArray();
            }
            return FileOps.Sha256(data);
        }

        static HttpWebRequest Request(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.UserAgent = "STEFFLIX-Updater/" + App.VersionText;
            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return req;
        }

        static HttpWebResponse Open(HttpWebRequest req)
        {
            try
            {
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    int code = (int)resp.StatusCode;
                    resp.Close();
                    throw new DownloadException("HTTP " + code);
                }
                return resp;
            }
            catch (WebException e)
            {
                HttpWebResponse r = e.Response as HttpWebResponse;
                if (r != null) throw new DownloadException("HTTP " + (int)r.StatusCode);
                throw new DownloadException("keine Verbindung (" + e.Status + ")");
            }
        }

        public static byte[] Get(string url, CancellationToken token)
        {
            HttpWebRequest req = Request(url);
            using (token.Register(delegate { try { req.Abort(); } catch { } }))
            {
                try
                {
                    using (HttpWebResponse resp = Open(req))
                    using (Stream s = resp.GetResponseStream())
                    {
                        MemoryStream ms = new MemoryStream();
                        byte[] buf = new byte[81920];
                        int n;
                        while ((n = s.Read(buf, 0, buf.Length)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            ms.Write(buf, 0, n);
                        }
                        return ms.ToArray();
                    }
                }
                catch (IOException e)
                {
                    token.ThrowIfCancellationRequested();
                    throw new DownloadException("Verbindung abgebrochen (" + e.Message + ")");
                }
                catch (WebException e)
                {
                    token.ThrowIfCancellationRequested();
                    throw new DownloadException("keine Verbindung (" + e.Status + ")");
                }
            }
        }

        // Grosse Datei direkt auf die Festplatte laden, SHA256 nebenbei berechnen.
        public static string GetToFile(string url, string target, CancellationToken token, Action<long, long> progress)
        {
            HttpWebRequest req = Request(url);
            using (token.Register(delegate { try { req.Abort(); } catch { } }))
            {
                try
                {
                    using (HttpWebResponse resp = Open(req))
                    using (Stream s = resp.GetResponseStream())
                    using (FileStream fs = File.Create(target))
                    using (SHA256 sha = SHA256.Create())
                    {
                        long total = resp.ContentLength;
                        long done = 0;
                        byte[] buf = new byte[256 * 1024];
                        int n;
                        while ((n = s.Read(buf, 0, buf.Length)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            fs.Write(buf, 0, n);
                            sha.TransformBlock(buf, 0, n, null, 0);
                            done += n;
                            if (progress != null) progress(done, total);
                        }
                        sha.TransformFinalBlock(buf, 0, 0);
                        return FileOps.Hex(sha.Hash);
                    }
                }
                catch (IOException e)
                {
                    token.ThrowIfCancellationRequested();
                    throw new DownloadException("Verbindung abgebrochen (" + e.Message + ")");
                }
                catch (WebException e)
                {
                    token.ThrowIfCancellationRequested();
                    throw new DownloadException("keine Verbindung (" + e.Status + ")");
                }
            }
        }

        // Lädt checksums.json. Ohne Prüfsummen wird nichts installiert.
        public static JsonObject LoadChecksums(CancellationToken token)
        {
            byte[] data;
            try
            {
                data = Get(RawUrl + ChecksumFile, token);
            }
            catch (DownloadException e)
            {
                throw new DownloadException("Prüfsummen nicht erreichbar (" + e.Message + ")");
            }
            try
            {
                JsonObject o = Json.Parse(new UTF8Encoding(false, true).GetString(data)) as JsonObject;
                if (o == null) throw new FormatException();
                return o;
            }
            catch (Exception)
            {
                throw new DownloadException("Prüfsummendatei ist beschädigt");
            }
        }

        // Lädt eine Datei herunter und gibt den geprüften Inhalt zurück.
        public static byte[] DownloadFile(string path, JsonObject checksums, CancellationToken token)
        {
            string expected = checksums.Str(path);
            if (expected == null) throw new DownloadException("keine Prüfsumme vorhanden");

            byte[] data = Get(UrlFor(path), token);
            if (data.Length == 0) throw new DownloadException("leere Datei");

            if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !FileOps.IsWindowsExe(data))
                throw new DownloadException("keine gültige Programmdatei");

            if (IsTextFile(path))
            {
                try { new UTF8Encoding(false, true).GetString(data); }
                catch (DecoderFallbackException) { throw new DownloadException("ungültige Textdatei"); }
            }

            if (Checksum(path, data) != expected) throw new DownloadException("Prüfsumme stimmt nicht");
            return data;
        }

        // Schreibt zuerst <ziel>.new und ersetzt dann das Original. Das Original wird zu .old
        // umbenannt (geht auch bei laufenden Programmen). Gibt zurück, ob .old angelegt wurde.
        public static bool InstallFile(string target, byte[] data, bool backup)
        {
            string folder = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

            string temp = target + ".new";
            try
            {
                File.WriteAllBytes(temp, data);
            }
            catch
            {
                FileOps.TryDelete(temp);
                throw;
            }

            string old = target + ".old";
            bool hasBackup = false;
            if (backup && File.Exists(target))
            {
                FileOps.Replace(target, old);
                hasBackup = true;
            }

            try
            {
                FileOps.Replace(temp, target);
            }
            catch
            {
                if (hasBackup) FileOps.Replace(old, target);
                throw;
            }
            return hasBackup;
        }

        public static bool IsUpToDate(string path, string target, JsonObject checksums)
        {
            string expected = checksums.Str(path);
            if (expected == null || !File.Exists(target)) return false;
            try { return Checksum(path, File.ReadAllBytes(target)) == expected; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        public static void RestoreFile(string target, bool hasBackup)
        {
            string old = target + ".old";
            if (hasBackup && File.Exists(old)) FileOps.Replace(old, target);
            else if (!hasBackup && File.Exists(target)) File.Delete(target);
        }

        // Lädt und installiert nach dem Alles-oder-nichts-Prinzip:
        //   1. Alle Dateien herunterladen und prüfen. Schlägt eine fehl, wird nichts verändert.
        //   2. Alle installieren. Schlägt eine fehl, werden die bereits installierten zurückgesetzt.
        // Gibt die Anzahl fehlgeschlagener Dateien zurück (0 = Erfolg).
        public static int UpdateFiles(IList<string> paths, string baseDir, JsonObject checksums, TaskContext ctx)
        {
            List<KeyValuePair<string, byte[]>> downloads = new List<KeyValuePair<string, byte[]>>();
            int failed = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                ctx.Check();
                string path = paths[i];
                string target = Path.Combine(baseDir, path.Replace('/', '\\'));
                ctx.Progress((double)i / paths.Count, "Lade " + path);

                if (IsUpToDate(path, target, checksums))
                {
                    ctx.Log("  " + path.PadRight(36) + "aktuell");
                    continue;
                }
                try
                {
                    downloads.Add(new KeyValuePair<string, byte[]>(path, DownloadFile(path, checksums, ctx.Token)));
                    ctx.Log("  " + path.PadRight(36) + "geladen");
                }
                catch (DownloadException e)
                {
                    failed++;
                    ctx.Log("  " + path.PadRight(36) + "FEHLER: " + e.Message);
                }
            }

            if (failed > 0)
            {
                ctx.Log(failed + " Datei(en) fehlerhaft. Es wurden keine Dateien verändert.");
                return failed;
            }

            List<KeyValuePair<string, bool>> installed = new List<KeyValuePair<string, bool>>();
            foreach (KeyValuePair<string, byte[]> d in downloads)
            {
                string target = Path.Combine(baseDir, d.Key.Replace('/', '\\'));
                try
                {
                    installed.Add(new KeyValuePair<string, bool>(target, InstallFile(target, d.Value, true)));
                    ctx.Log("  " + d.Key.PadRight(36) + "installiert");
                }
                catch (Exception e)
                {
                    ctx.Log("  " + d.Key.PadRight(36) + "FEHLER: " + e.Message);
                    ctx.Log("Installation wird rückgängig gemacht...");
                    int restoreFailed = 0;
                    for (int i = installed.Count - 1; i >= 0; i--)
                    {
                        try { RestoreFile(installed[i].Key, installed[i].Value); }
                        catch { restoreFailed++; }
                    }
                    ctx.Log(restoreFailed == 0 ? "Alter Stand wiederhergestellt." : "FEHLER beim Zurücksetzen von " + restoreFailed + " Datei(en)");
                    return downloads.Count;
                }
            }
            return 0;
        }

        // Sicherungen (*.old) früherer Updates entfernen: im STEFFLIX-Ordner, in sflix_sys und
        // sflix_sys\js (dort legen alte und neue Updater sie an). Medienordner bleiben unberührt.
        // Gibt die Anzahl entfernter Dateien zurück.
        public static int RemoveOldBackups(string baseDir)
        {
            int removed = 0;
            string[] folders = { baseDir, Path.Combine(baseDir, "sflix_sys"), Path.Combine(baseDir, "sflix_sys", "js") };
            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder)) continue;
                string[] files;
                try { files = Directory.GetFiles(folder, "*.old"); }
                catch { continue; }
                foreach (string f in files)
                {
                    // GetFiles("*.old") findet auch z.B. "x.older": nur echte .old
                    if (!f.EndsWith(".old", StringComparison.OrdinalIgnoreCase)) continue;
                    try { File.Delete(f); removed++; }
                    catch { }   // z.B. noch gesperrt: beim nächsten Start erneut
                }
            }
            return removed;
        }

        public static void RemoveObsoleteFiles(string baseDir, TaskContext ctx)
        {
            foreach (string name in ObsoleteFiles)
            {
                string path = Path.Combine(baseDir, name.Replace('/', '\\'));
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        ctx.Log("  Veraltete Datei entfernt: " + name);
                    }
                }
                catch { }
            }
        }
    }
}
