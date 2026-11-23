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

    // Prüfsummen geladen, aber abgelehnt (Signatur ungültig, Datei beschädigt, älterer Stand). Anders als
    // "nicht erreichbar" darf danach auch keine eingebaute ffmpeg-Version installiert werden (SFLIX-20261123-090).
    public class ChecksumsRejectedException : DownloadException
    {
        public ChecksumsRejectedException(string message) : base(message) { }
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

        // ---------------------------------------------------------------
        // Dateilisten aus dem Release (seit 3.4.1)
        // ---------------------------------------------------------------
        // create_checksums.py schreibt beide Listen zusätzlich in checksums.json. So kommen neue oder
        // entfallene Dateien der Weboberfläche ohne neuen Updater zu den Nutzern. Fehlt eine Liste
        // (älteres Release), gelten WebFiles bzw. ObsoleteFiles oben.
        public const string WebFilesKey = "_webFiles", ObsoleteFilesKey = "_obsoleteFiles";

        // Persönliche Dateien einer Installation: werden nie heruntergeladen, ersetzt oder gelöscht
        static readonly string[] ProtectedFiles = {
            "sflix_sys/data.js", "sflix_sys/install.js", "sflix_sys/license.js", "sflix_sys/renamed.js",
            "sflix_sys/updater_info.js", "sflix_sys/updater_settings.json", "sflix_sys/folgen_cache.json",
            "sflix_sys/folgen_rueckgaengig.json", "sflix_sys/updater.log", "sflix_sys/" + MediaCache.FileName,
            "sflix_sys/" + DownloadStore.FileName, "sflix_sys/" + DamagedList.FileName, "sflix_sys/" + DamagedList.LogName,
        };

        // Endungen, die zur Weboberfläche gehören dürfen (keine Programme, keine Medien)
        static readonly string[] WebExtensions = {
            ".html", ".js", ".css", ".svg", ".png", ".jpg", ".jpeg", ".webp", ".gif", ".ico", ".json", ".txt", ".woff", ".woff2",
        };

        // Darf ein Pfad aus der Liste des Release angefasst werden? Nur start.html und Dateien in sflix_sys
        // (nicht im ffmpeg-Ordner), keine "..", Laufwerke oder Backslashes, keine persönlichen Dateien.
        public static bool IsAllowedWebPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length > 200) return false;
            if (path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0 || path.StartsWith("/")) return false;
            foreach (string seg in path.Split('/'))
                if (seg.Length == 0 || seg == "." || seg == ".." || seg.Trim() != seg) return false;
            foreach (char c in path)
                if (c < 32 || "<>\"|?*".IndexOf(c) >= 0) return false;
            string lower = path.ToLowerInvariant();
            foreach (string p in ProtectedFiles) if (lower == p) return false;
            if (lower == "start.html") return true;
            if (!lower.StartsWith("sflix_sys/") || lower.StartsWith("sflix_sys/ffmpeg/")) return false;
            foreach (string ext in WebExtensions) if (lower.EndsWith(ext)) return true;
            return false;
        }

        // Liste aus checksums.json, nur erlaubte Pfade; null, wenn sie fehlt oder leer ist
        static List<string> ListFrom(JsonObject checksums, string key)
        {
            List<object> raw = checksums == null ? null : checksums.Arr(key);
            if (raw == null) return null;
            List<string> list = new List<string>();
            foreach (object o in raw)
            {
                string p = o as string;
                if (p != null && IsAllowedWebPath(p) && !list.Contains(p)) list.Add(p);
            }
            return list.Count > 0 ? list : null;
        }

        // Dateien der Weboberfläche: aus dem Release (nur mit Prüfsumme), sonst die eingebaute Liste
        public static List<string> WebFilesFor(JsonObject checksums)
        {
            List<string> remote = ListFrom(checksums, WebFilesKey);
            if (remote == null) return new List<string>(WebFiles);
            remote.RemoveAll(delegate(string p) { return checksums.Str(p) == null; });
            return remote.Count > 0 ? remote : new List<string>(WebFiles);
        }

        // Veraltete Dateien: eingebaute Liste plus die des Release (nie eine aktuelle Datei)
        public static List<string> ObsoleteFilesFor(JsonObject checksums)
        {
            List<string> list = new List<string>(ObsoleteFiles);
            List<string> remote = ListFrom(checksums, ObsoleteFilesKey);
            if (remote != null)
                foreach (string p in remote) if (!list.Contains(p)) list.Add(p);
            List<string> current = WebFilesFor(checksums);
            list.RemoveAll(delegate(string p) { return current.Contains(p); });
            return list;
        }

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

        public const string SignatureFile = "checksums.sig";

        // Prüft die Signatur von checksums.json (DEC-356): Base64, PKCS#1 v1.5 mit SHA-256 über die Bytes
        // mit vereinheitlichten Zeilenenden (CRLF -> LF). Gültig, wenn einer der Schlüssel passt.
        public static bool VerifyChecksums(byte[] json, byte[] signatureFile, string[] publicKeys)
        {
            if (json == null || signatureFile == null || publicKeys == null) return false;
            byte[] signature;
            try { signature = Convert.FromBase64String(Encoding.ASCII.GetString(signatureFile).Trim()); }
            catch (FormatException) { return false; }
            byte[] normalized = NormalizeLineEnds(json);
            foreach (string key in publicKeys)
            {
                try
                {
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.PersistKeyInCsp = false;
                        rsa.FromXmlString(key);
                        using (SHA256CryptoServiceProvider sha = new SHA256CryptoServiceProvider())
                            if (rsa.VerifyData(normalized, sha, signature)) return true;
                    }
                }
                catch (CryptographicException) { }
            }
            return false;
        }

        static byte[] NormalizeLineEnds(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data.Length);
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == '\r' && i + 1 < data.Length && data[i + 1] == '\n') continue;
                ms.WriteByte(data[i]);
            }
            return ms.ToArray();
        }

        // Release-Nummer in checksums.json (DEC-372): 14 Ziffern, streng steigend, mitsigniert. Die höchste
        // angenommene merkt sich jede Installation in updater_settings.json.
        public const string SerialKey = "_serial", SerialSetting = "update.serial";

        // Einstellungen der laufenden Installation (setzt Program.cs). Dort merkt sich LoadChecksums die
        // Release-Nummer; das Setup in einen anderen Ordner übergibt die Einstellungen der Zielinstallation.
        public static Settings SerialStore;
        static readonly object serialGate = new object();

        public static JsonObject LoadChecksums(CancellationToken token)
        {
            return LoadChecksums(token, SerialStore);
        }

        // Lädt checksums.json und prüft die Signatur (checksums.sig, seit 4.1.0). Ohne gültige Signatur
        // wird nichts installiert: das gilt für Update, Setup und ffmpeg (alle holen die Prüfsummen hier).
        // Danach die Release-Nummer: kein Stand, der älter ist als ein schon angenommener (seit 4.1.4).
        public static JsonObject LoadChecksums(CancellationToken token, Settings serialStore)
        {
            JsonObject checksums = LoadSignedChecksums(token);
            CheckSerial(checksums, serialStore);
            return checksums;
        }

        // 14 Ziffern, sonst null (ungültiges Format gilt wie fehlend)
        static string ValidSerial(string s)
        {
            if (s == null || s.Length != 14) return null;
            foreach (char c in s) if (c < '0' || c > '9') return null;
            return s;
        }

        // Erst nach gültiger Signatur und erfolgreichem Lesen: kleiner als die gespeicherte Nummer oder
        // fehlend, obwohl eine gespeichert ist -> nichts installieren. Höher -> speichern.
        // Ohne gespeicherte Nummer wird jeder gültig signierte Stand angenommen.
        static void CheckSerial(JsonObject checksums, Settings store)
        {
            if (store == null) return;
            string remote = ValidSerial(checksums.Str(SerialKey));
            lock (serialGate)
            {
                string known = ValidSerial(store.GetString(SerialSetting, null));
                if (known != null && (remote == null || string.CompareOrdinal(remote, known) < 0))
                    throw new ChecksumsRejectedException("Der Server liefert einen älteren Stand der Prüfsummen, als hier schon angenommen wurde "
                        + "(Release-Nummer " + (remote ?? "fehlt") + ", angenommen " + known + "): es wird nichts installiert. "
                        + "Kurz nach einer neuen Version kann das vorübergehend sein: später erneut versuchen.");
                if (remote != null && (known == null || string.CompareOrdinal(remote, known) > 0))
                {
                    store.SetString(SerialSetting, remote);
                    store.Save();
                }
            }
        }

        static JsonObject LoadSignedChecksums(CancellationToken token)
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
            if (UpdateKeys.PublicKeys.Length == 0)
                throw new DownloadException("In diesem Programm ist kein Schlüssel für signierte Updates eingebaut (Entwicklerversion): es wird nichts installiert");
            byte[] signature;
            try
            {
                signature = Get(RawUrl + SignatureFile, token);
            }
            catch (DownloadException e)
            {
                throw new DownloadException("Signatur der Prüfsummen nicht erreichbar (" + e.Message + "). Aus Sicherheitsgründen wird nichts installiert. "
                    + "Kurz nach einer neuen Version kann das vorübergehend sein: später erneut versuchen.");
            }
            if (!VerifyChecksums(data, signature, UpdateKeys.PublicKeys))
                throw new ChecksumsRejectedException("Die Signatur der Prüfsummen ist ungültig: es wird nichts installiert. "
                    + "Meist ist das vorübergehend (der Server liefert kurz nach einer neuen Version noch alte Dateien): später erneut versuchen.");
            try
            {
                JsonObject o = Json.Parse(new UTF8Encoding(false, true).GetString(data)) as JsonObject;
                if (o == null) throw new FormatException();
                return o;
            }
            catch (Exception)
            {
                throw new ChecksumsRejectedException("Prüfsummendatei ist beschädigt");
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
                try { FileOps.Replace(target, old); }
                catch { FileOps.TryDelete(temp); throw; }   // kein <ziel>.new liegen lassen
                hasBackup = true;
            }

            try
            {
                FileOps.Replace(temp, target);
            }
            catch
            {
                FileOps.TryDelete(temp);
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
            if (hasBackup)
            {
                // Fehlt die Sicherung (z.B. von einem zweiten Start gelöscht), als Fehler melden statt still weiter
                if (!File.Exists(old)) throw new IOException("Sicherung fehlt: " + old);
                FileOps.Replace(old, target);
            }
            else if (File.Exists(target)) File.Delete(target);
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
        // Seit 4.1.0 nur eigene Sicherungen: im Installationsordner (oft der Medien-Hauptordner) nur die der
        // Programmdateien, in sflix_sys nur die von Dateien der Weboberfläche (nie persönliche Dateien).
        static readonly string[] ProgramFiles = { "STEFFLIX.exe", "updateStefflix.exe", "updater.exe", "updateData.exe", "start.html" };

        public static bool IsOwnBackup(string baseDir, string file)
        {
            if (!file.EndsWith(".old", StringComparison.OrdinalIgnoreCase)) return false;   // "*.old" findet auch "x.older"
            string original = file.Substring(0, file.Length - 4);
            string folder = Path.GetFullPath(Path.GetDirectoryName(file)).TrimEnd('\\');
            string root = Path.GetFullPath(baseDir).TrimEnd('\\');
            if (string.Equals(folder, root, StringComparison.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(original);
                foreach (string p in ProgramFiles) if (string.Equals(name, p, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            }
            if (!folder.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)) return false;
            string rel = Path.GetFullPath(original).Substring(root.Length + 1).Replace('\\', '/');
            return IsAllowedWebPath(rel);
        }

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
                    if (!IsOwnBackup(baseDir, f)) continue;
                    try { File.Delete(f); removed++; }
                    catch { }   // z.B. noch gesperrt: beim nächsten Start erneut
                }
            }
            return removed;
        }

        // Entwicklungsordner im Release-Ordner (seit 4.0.6 "SFLIX_Tools", vorher "SFLIX Tools"; beide werden erkannt).
        public static readonly string[] DevFolderNames = { "SFLIX_Tools", "SFLIX Tools" };

        // Release-Ordner des Entwicklers (mit "SFLIX_Tools\Release\release.ps1"): Dort müssen die veralteten
        // Dateien bleiben, alte Installationen laden sie noch (check_release.py bricht sonst ab).
        public static bool IsReleaseFolder(string baseDir)
        {
            // Leerer Pfad würde gegen das Arbeitsverzeichnis aufgelöst
            if (string.IsNullOrEmpty(baseDir)) return false;
            try
            {
                foreach (string dev in DevFolderNames)
                {
                    if (File.Exists(Path.Combine(baseDir, dev + "\\Release\\release.ps1"))) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static void RemoveObsoleteFiles(string baseDir, JsonObject checksums, TaskContext ctx)
        {
            if (IsReleaseFolder(baseDir))
            {
                ctx.Log("  Release-Ordner: veraltete Dateien bleiben erhalten (alte Installationen laden sie noch).");
                return;
            }
            foreach (string name in ObsoleteFilesFor(checksums))
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
