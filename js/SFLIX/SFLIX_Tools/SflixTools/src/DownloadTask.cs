// Reiter "Download" (seit 4.0.5): Videos aus einer m3u8-Adresse (HLS) laden.
// STEFFLIX.exe liest die Playlist selbst und lädt die Teilstücke einzeln in einen lokalen Ordner
// (%LOCALAPPDATA%\STEFFLIX\downloads\<Id>). So lässt sich ein Download echt pausieren und fortsetzen,
// auch nach einem Neustart, fehlerhafte Teilstücke werden einzeln wiederholt, und nach einer neuen
// Adresse (abgelaufener Token) geht es an derselben Stelle weiter. ffmpeg fügt am Schluss nur zusammen
// (-c copy). Danach: prüfen, in den Zielordner verschieben, bei Bedarf konvertieren, Vorschaubild.
// Nur Streams ohne Kopierschutz (AES-128 mit frei abrufbarem Schlüssel ist kein Kopierschutz;
// SAMPLE-AES und KEYFORMAT wie Widevine/FairPlay werden abgelehnt).
//
// Liste der Downloads: sflix_sys\downloads.json (persönlich, nie pushen: .gitignore, Net.ProtectedFiles).

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sflix
{
    public enum DownloadState { Waiting, Running, Paused, Processing, Done, Failed }

    public class DownloadJob
    {
        public string Id;
        public string Url;
        public string Referer = "";
        public string UserAgent = "";
        public string Variant = "";          // Adresse der gewählten Qualität, leer = beste
        public string VariantLabel = "";     // z.B. "1920×1080, 5.2 Mbit/s"
        public string Folder = "";           // Zielordner relativ zum Installationsordner
        public string FileName = "";
        public bool FullCheck;               // vollständiger Dekodiertest
        public DownloadState State = DownloadState.Waiting;
        public int SegmentsDone, SegmentsTotal;
        public long Bytes;
        public double Duration;              // Summe der Teilstücke in Sekunden
        public string Status = "";           // Text in der Liste
        public string Result = "";           // "", "OK", "Warnung", "Fehler"
        public List<string> Messages = new List<string>();
        public string ResultPath = "";
        public DateTime Added = DateTime.Now;

        // Laufzeit (nicht gespeichert)
        internal CancellationTokenSource Cts;
        internal volatile bool PauseRequested;
        internal volatile bool RemoveRequested;

        public string TempDir { get { return Path.Combine(DownloadStore.TempRoot, Id); } }
        public string TargetDir { get { return Path.Combine(App.Root, Folder); } }
        public bool IsActive { get { return State == DownloadState.Waiting || State == DownloadState.Running || State == DownloadState.Processing; } }

        public void AddMessage(string text)
        {
            lock (Messages)
            {
                if (Messages.Count < 200) Messages.Add(text);
            }
        }

        public JsonObject ToJson()
        {
            JsonObject o = new JsonObject();
            o["id"] = Id;
            o["url"] = Url;
            o["referer"] = Referer;
            o["userAgent"] = UserAgent;
            o["variant"] = Variant;
            o["variantLabel"] = VariantLabel;
            o["folder"] = Folder;
            o["fileName"] = FileName;
            o["fullCheck"] = FullCheck;
            o["state"] = State.ToString();
            o["segmentsDone"] = (double)SegmentsDone;
            o["segmentsTotal"] = (double)SegmentsTotal;
            o["bytes"] = (double)Bytes;
            o["duration"] = Duration;
            o["status"] = Status;
            o["result"] = Result;
            List<object> messages = new List<object>();
            lock (Messages) foreach (string m in Messages) messages.Add(m);
            o["messages"] = messages;
            o["resultPath"] = ResultPath;
            o["added"] = Added.ToString("o", CultureInfo.InvariantCulture);
            return o;
        }

        public static DownloadJob FromJson(JsonObject o)
        {
            if (o == null || string.IsNullOrEmpty(o.Str("id")) || string.IsNullOrEmpty(o.Str("url"))) return null;
            DownloadJob j = new DownloadJob();
            j.Id = o.Str("id");
            j.Url = o.Str("url");
            j.Referer = o.Str("referer") ?? "";
            j.UserAgent = o.Str("userAgent") ?? "";
            j.Variant = o.Str("variant") ?? "";
            j.VariantLabel = o.Str("variantLabel") ?? "";
            j.Folder = o.Str("folder") ?? "";
            j.FileName = o.Str("fileName") ?? "";
            j.FullCheck = o.Bool("fullCheck", false);
            DownloadState state;
            j.State = Enum.TryParse(o.Str("state") ?? "", out state) ? state : DownloadState.Paused;
            j.SegmentsDone = (int)o.Num("segmentsDone", 0);
            j.SegmentsTotal = (int)o.Num("segmentsTotal", 0);
            j.Bytes = (long)o.Num("bytes", 0);
            j.Duration = o.Num("duration", 0);
            j.Status = o.Str("status") ?? "";
            j.Result = o.Str("result") ?? "";
            List<object> messages = o.Arr("messages");
            if (messages != null) foreach (object m in messages) if (m is string) j.Messages.Add((string)m);
            j.ResultPath = o.Str("resultPath") ?? "";
            DateTime added;
            if (DateTime.TryParse(o.Str("added") ?? "", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out added)) j.Added = added;
            // Beim letzten Beenden lief er noch: kann fortgesetzt werden
            if (j.State == DownloadState.Running || j.State == DownloadState.Processing)
            {
                j.State = DownloadState.Paused;
                j.Status = "Unterbrochen";
            }
            return j;
        }
    }

    // Liste der Downloads in sflix_sys\downloads.json und lokaler Ordner für die Teilstücke
    public static class DownloadStore
    {
        public const string FileName = "downloads.json";

        public static string FilePath { get { return Path.Combine(App.SysDir, FileName); } }

        public static string TempRoot
        {
            get { return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STEFFLIX"), "downloads"); }
        }

        public static List<DownloadJob> Load()
        {
            List<DownloadJob> jobs = new List<DownloadJob>();
            try
            {
                if (!File.Exists(FilePath)) return jobs;
                JsonObject root = Json.Parse(File.ReadAllText(FilePath, Encoding.UTF8)) as JsonObject;
                List<object> list = root == null ? null : root.Arr("downloads");
                if (list == null) return jobs;
                foreach (object o in list)
                {
                    DownloadJob j = DownloadJob.FromJson(o as JsonObject);
                    if (j != null) jobs.Add(j);
                }
            }
            catch { }
            return jobs;
        }

        public static void Save(List<DownloadJob> jobs)
        {
            // Release-Ordner: nichts schreiben (siehe ReleaseFolder)
            if (App.IsReleaseFolder) return;
            JsonObject root = new JsonObject();
            List<object> list = new List<object>();
            foreach (DownloadJob j in jobs) list.Add(j.ToJson());
            root["downloads"] = list;
            string tmp = FilePath + ".tmp";
            try
            {
                if (jobs.Count == 0)
                {
                    if (File.Exists(FilePath)) FileOps.TryDelete(FilePath);
                    return;
                }
                File.WriteAllText(tmp, Json.Write(root, 2), new UTF8Encoding(false));
                FileOps.Replace(tmp, FilePath);
            }
            catch { FileOps.TryDelete(tmp); }
        }

        // Ordner mit Teilstücken ohne Eintrag in der Liste (z.B. nach einem Absturz) löschen
        public static long RemoveOrphans(List<DownloadJob> jobs)
        {
            long freed = 0;
            try
            {
                if (!Directory.Exists(TempRoot)) return 0;
                HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DownloadJob j in jobs) known.Add(j.Id);
                foreach (string dir in Directory.GetDirectories(TempRoot))
                {
                    if (known.Contains(Path.GetFileName(dir))) continue;
                    freed += FolderSize(dir);
                    DeleteTemp(dir);
                }
            }
            catch { }
            return freed;
        }

        public static long FolderSize(string dir)
        {
            long size = 0;
            try
            {
                if (!Directory.Exists(dir)) return 0;
                foreach (string f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(f).Length; } catch { }
                }
            }
            catch { }
            return size;
        }

        // Lokaler Zwischenordner (nicht im Installationsordner): direkt löschen
        public static void DeleteTemp(string dir)
        {
            try
            {
                string root = Path.GetFullPath(TempRoot).TrimEnd('\\') + "\\";
                string full = Path.GetFullPath(dir);
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return;   // nur unterhalb von TempRoot
                if (Directory.Exists(full)) Directory.Delete(full, true);
            }
            catch { }
        }
    }

    // ------------------------------------------------------------------
    // HLS (m3u8)
    // ------------------------------------------------------------------

    public class HlsVariant
    {
        public string Url;
        public long Bandwidth;
        public string Resolution = "";
        public string AudioGroup = "";

        public string Label
        {
            get
            {
                string res = Resolution.Replace('x', '×');
                string rate = Bandwidth > 0 ? Fmt.Inv(Bandwidth / 1000000.0, "0.0") + " Mbit/s" : "";
                if (res.Length > 0 && rate.Length > 0) return res + ", " + rate;
                return res.Length > 0 ? res : (rate.Length > 0 ? rate : "unbekannt");
            }
        }

        public int Height
        {
            get
            {
                int x = Resolution.IndexOf('x');
                int h;
                return x > 0 && int.TryParse(Resolution.Substring(x + 1), out h) ? h : 0;
            }
        }
    }

    public class HlsSegment
    {
        public string Url;
        public double Duration;
        public string Line;          // #EXTINF-Zeile wie im Original
        public List<string> Before = new List<string>();   // Tags davor (Schlüssel, Init, Diskontinuität), schon umgeschrieben
    }

    public class HlsPlaylist
    {
        public List<HlsSegment> Segments = new List<HlsSegment>();
        public List<string> Header = new List<string>();
        // Zusätzliche Dateien (Schlüssel, Init-Segmente): lokaler Name -> Adresse
        public Dictionary<string, string> Extras = new Dictionary<string, string>();
        public double Duration;
    }

    public class HlsException : Exception
    {
        public HlsException(string message) : base(message) { }
    }

    public static class Hls
    {
        public static bool IsHttpUrl(string url)
        {
            Uri u;
            return Uri.TryCreate((url ?? "").Trim(), UriKind.Absolute, out u) && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
        }

        public static string Fetch(string url, DownloadJob job, CancellationToken token)
        {
            // Ohne BOM, sonst beginnt die erste Zeile nicht mit #EXTM3U
            return Encoding.UTF8.GetString(Http.Get(url, job, token, 2)).TrimStart('﻿');
        }

        public static string Resolve(string baseUrl, string relative)
        {
            return new Uri(new Uri(baseUrl), relative.Trim()).AbsoluteUri;
        }

        // Attribute einer Tag-Zeile: KEY=VALUE,KEY="VALUE, mit Komma"
        public static Dictionary<string, string> Attributes(string line)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int colon = line.IndexOf(':');
            if (colon < 0) return map;
            string s = line.Substring(colon + 1);
            int i = 0;
            while (i < s.Length)
            {
                int eq = s.IndexOf('=', i);
                if (eq < 0) break;
                string key = s.Substring(i, eq - i).Trim();
                int j = eq + 1;
                string value;
                if (j < s.Length && s[j] == '"')
                {
                    int end = s.IndexOf('"', j + 1);
                    if (end < 0) end = s.Length;
                    value = s.Substring(j + 1, end - j - 1);
                    j = end + 1;
                    int comma = s.IndexOf(',', j);
                    i = comma < 0 ? s.Length : comma + 1;
                }
                else
                {
                    int comma = s.IndexOf(',', j);
                    value = comma < 0 ? s.Substring(j) : s.Substring(j, comma - j);
                    i = comma < 0 ? s.Length : comma + 1;
                }
                if (key.Length > 0) map[key] = value.Trim();
            }
            return map;
        }

        static IEnumerable<string> Lines(string text)
        {
            foreach (string raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length > 0) yield return line;
            }
        }

        public static bool IsMaster(string text)
        {
            return text.IndexOf("#EXT-X-STREAM-INF", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Qualitäten einer Master-Playlist, beste zuerst
        public static List<HlsVariant> Variants(string text, string baseUrl, out Dictionary<string, string> audioByGroup)
        {
            List<HlsVariant> list = new List<HlsVariant>();
            audioByGroup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> anyAudio = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            HlsVariant pending = null;
            foreach (string line in Lines(text))
            {
                if (line.StartsWith("#EXT-X-MEDIA:", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> a = Attributes(line);
                    string type, group, uri, def;
                    if (!a.TryGetValue("TYPE", out type) || !type.Equals("AUDIO", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!a.TryGetValue("GROUP-ID", out group) || !a.TryGetValue("URI", out uri) || uri.Length == 0) continue;
                    string url = Resolve(baseUrl, uri);
                    if (!anyAudio.ContainsKey(group)) anyAudio[group] = url;
                    if (a.TryGetValue("DEFAULT", out def) && def.Equals("YES", StringComparison.OrdinalIgnoreCase)) audioByGroup[group] = url;
                }
                else if (line.StartsWith("#EXT-X-STREAM-INF:", StringComparison.OrdinalIgnoreCase))
                {
                    Dictionary<string, string> a = Attributes(line);
                    pending = new HlsVariant();
                    string v;
                    long bw;
                    if (a.TryGetValue("BANDWIDTH", out v) && long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out bw)) pending.Bandwidth = bw;
                    if (a.TryGetValue("RESOLUTION", out v)) pending.Resolution = v;
                    if (a.TryGetValue("AUDIO", out v)) pending.AudioGroup = v;
                }
                else if (!line.StartsWith("#") && pending != null)
                {
                    pending.Url = Resolve(baseUrl, line);
                    list.Add(pending);
                    pending = null;
                }
            }
            foreach (KeyValuePair<string, string> kv in anyAudio)
                if (!audioByGroup.ContainsKey(kv.Key)) audioByGroup[kv.Key] = kv.Value;
            list.Sort(delegate(HlsVariant x, HlsVariant y)
            {
                int c = y.Height.CompareTo(x.Height);
                return c != 0 ? c : y.Bandwidth.CompareTo(x.Bandwidth);
            });
            return list;
        }

        // Media-Playlist lesen. prefix unterscheidet Video ("v") und Ton ("a") im Zwischenordner.
        public static HlsPlaylist Parse(string text, string baseUrl, string prefix)
        {
            HlsPlaylist p = new HlsPlaylist();
            bool ended = false;
            List<string> pendingTags = new List<string>();
            string pendingInf = null;
            double pendingDuration = 0;
            int keyCount = 0, mapCount = 0;
            bool first = true;
            foreach (string line in Lines(text))
            {
                if (first)
                {
                    first = false;
                    if (!line.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
                        throw new HlsException("Die Adresse liefert keine m3u8-Playlist.");
                    continue;
                }
                string upper = line.ToUpperInvariant();
                if (upper.StartsWith("#EXT-X-ENDLIST")) { ended = true; continue; }
                if (upper.StartsWith("#EXT-X-BYTERANGE"))
                    throw new HlsException("Playlists mit Teilbereichen (EXT-X-BYTERANGE) werden nicht unterstützt.");
                if (upper.StartsWith("#EXT-X-KEY:") || upper.StartsWith("#EXT-X-SESSION-KEY:"))
                {
                    Dictionary<string, string> a = Attributes(line);
                    string method, format, uri;
                    a.TryGetValue("METHOD", out method);
                    method = (method ?? "NONE").ToUpperInvariant();
                    if (a.TryGetValue("KEYFORMAT", out format) && !format.Equals("identity", StringComparison.OrdinalIgnoreCase))
                        throw new HlsException("Der Stream ist kopiergeschützt (" + format + ") und kann nicht geladen werden.");
                    if (method != "NONE" && method != "AES-128")
                        throw new HlsException("Der Stream ist kopiergeschützt (" + method + ") und kann nicht geladen werden.");
                    if (upper.StartsWith("#EXT-X-SESSION-KEY:")) continue;
                    if (method == "NONE") { pendingTags.Add("#EXT-X-KEY:METHOD=NONE"); continue; }
                    if (!a.TryGetValue("URI", out uri) || uri.Length == 0) throw new HlsException("Schlüssel ohne Adresse in der Playlist.");
                    string local = prefix + "key" + (keyCount++) + ".bin";
                    p.Extras[local] = Resolve(baseUrl, uri);
                    string tag = "#EXT-X-KEY:METHOD=AES-128,URI=\"" + local + "\"";
                    string iv;
                    if (a.TryGetValue("IV", out iv) && iv.Length > 0) tag += ",IV=" + iv;
                    pendingTags.Add(tag);
                    continue;
                }
                if (upper.StartsWith("#EXT-X-MAP:"))
                {
                    Dictionary<string, string> a = Attributes(line);
                    string uri, range;
                    if (a.TryGetValue("BYTERANGE", out range)) throw new HlsException("Init-Segmente mit Teilbereich werden nicht unterstützt.");
                    if (!a.TryGetValue("URI", out uri) || uri.Length == 0) continue;
                    string local = prefix + "init" + (mapCount++) + ".mp4";
                    p.Extras[local] = Resolve(baseUrl, uri);
                    pendingTags.Add("#EXT-X-MAP:URI=\"" + local + "\"");
                    continue;
                }
                if (upper.StartsWith("#EXT-X-DISCONTINUITY") && !upper.StartsWith("#EXT-X-DISCONTINUITY-SEQUENCE"))
                {
                    pendingTags.Add("#EXT-X-DISCONTINUITY");
                    continue;
                }
                if (upper.StartsWith("#EXT-X-MEDIA-SEQUENCE") || upper.StartsWith("#EXT-X-TARGETDURATION") || upper.StartsWith("#EXT-X-VERSION")
                    || upper.StartsWith("#EXT-X-PLAYLIST-TYPE") || upper.StartsWith("#EXT-X-INDEPENDENT-SEGMENTS") || upper.StartsWith("#EXT-X-DISCONTINUITY-SEQUENCE"))
                {
                    // Medienfolge bleibt erhalten: AES-128 ohne IV nutzt die Nummer des Teilstücks
                    p.Header.Add(line);
                    continue;
                }
                if (upper.StartsWith("#EXTINF:"))
                {
                    pendingInf = line;
                    string d = line.Substring(8);
                    int comma = d.IndexOf(',');
                    if (comma >= 0) d = d.Substring(0, comma);
                    double.TryParse(d.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out pendingDuration);
                    continue;
                }
                if (line.StartsWith("#")) continue;   // übrige Tags werden nicht gebraucht
                HlsSegment seg = new HlsSegment();
                seg.Url = Resolve(baseUrl, line);
                seg.Duration = pendingDuration;
                seg.Line = pendingInf ?? "#EXTINF:" + Fmt.Inv(pendingDuration, "0.###") + ",";
                seg.Before.AddRange(pendingTags);
                pendingTags.Clear();
                pendingInf = null;
                pendingDuration = 0;
                p.Segments.Add(seg);
                p.Duration += seg.Duration;
            }
            if (p.Segments.Count == 0) throw new HlsException("Die Playlist enthält keine Teilstücke.");
            if (!ended) throw new HlsException("Live-Streams (Playlist ohne Ende) werden nicht unterstützt.");
            return p;
        }

        // Lokaler Name eines Teilstücks im Zwischenordner. Immer .ts bzw. .m4s (fMP4 mit Init-Segment):
        // Manche Server tarnen Teilstücke als .jpg o.ä., neuere ffmpeg lehnen solche Endungen ab.
        public static string SegmentName(string prefix, int index, bool fragmentedMp4)
        {
            return prefix + index.ToString("00000", CultureInfo.InvariantCulture) + (fragmentedMp4 ? ".m4s" : ".ts");
        }

        public static bool IsFragmentedMp4(HlsPlaylist p)
        {
            foreach (string name in p.Extras.Keys) if (name.Contains("init")) return true;
            return false;
        }

        // Playlist mit lokalen Dateien für ffmpeg
        public static string LocalPlaylist(HlsPlaylist p, string prefix)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("#EXTM3U\n");
            bool hasTarget = false;
            foreach (string h in p.Header)
            {
                if (h.StartsWith("#EXT-X-TARGETDURATION", StringComparison.OrdinalIgnoreCase)) hasTarget = true;
                sb.Append(h).Append('\n');
            }
            if (!hasTarget)
            {
                double max = 0;
                foreach (HlsSegment s in p.Segments) max = Math.Max(max, s.Duration);
                sb.Append("#EXT-X-TARGETDURATION:").Append((int)Math.Ceiling(max)).Append('\n');
            }
            bool fmp4 = IsFragmentedMp4(p);
            for (int i = 0; i < p.Segments.Count; i++)
            {
                HlsSegment s = p.Segments[i];
                foreach (string tag in s.Before) sb.Append(tag).Append('\n');
                sb.Append(s.Line).Append('\n');
                sb.Append(SegmentName(prefix, i, fmp4)).Append('\n');
            }
            sb.Append("#EXT-X-ENDLIST\n");
            return sb.ToString();
        }
    }

    // HTTP mit den Angaben des Downloads (Referer, User-Agent) und Wiederholungen
    public static class Http
    {
        public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36";

        public class HttpStatusException : Exception
        {
            public int Code;
            public HttpStatusException(int code) : base("HTTP " + code) { Code = code; }
        }

        static HttpWebRequest Request(string url, DownloadJob job)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 30000;
            req.ReadWriteTimeout = 30000;
            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.UserAgent = job != null && job.UserAgent.Length > 0 ? job.UserAgent : DefaultUserAgent;
            if (job != null && job.Referer.Length > 0)
            {
                req.Referer = job.Referer;
                try
                {
                    Uri r = new Uri(job.Referer);
                    req.Headers["Origin"] = r.Scheme + "://" + r.Authority;
                }
                catch { }
            }
            return req;
        }

        // Ganze Antwort; bei Netzfehlern bis retries Mal wiederholt
        public static byte[] Get(string url, DownloadJob job, CancellationToken token, int retries)
        {
            for (int attempt = 0; ; attempt++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    return GetOnce(url, job, token);
                }
                catch (HttpStatusException e)
                {
                    // 403/404/410: Adresse abgelaufen oder falsch, Wiederholen hilft selten
                    bool hopeless = e.Code == 403 || e.Code == 404 || e.Code == 410 || e.Code == 401;
                    if (hopeless && attempt >= 1 || attempt >= retries) throw;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception)
                {
                    if (attempt >= retries) throw;
                }
                token.WaitHandle.WaitOne(Math.Min(8000, 1000 * (1 << attempt)));
            }
        }

        static byte[] GetOnce(string url, DownloadJob job, CancellationToken token)
        {
            HttpWebRequest req = Request(url, job);
            using (token.Register(delegate { try { req.Abort(); } catch { } }))
            {
                try
                {
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
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
                catch (WebException e)
                {
                    token.ThrowIfCancellationRequested();
                    HttpWebResponse r = e.Response as HttpWebResponse;
                    if (r != null) throw new HttpStatusException((int)r.StatusCode);
                    throw new IOException("keine Verbindung (" + e.Status + ")");
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Ablauf der Downloads
    // ------------------------------------------------------------------

    public class DownloadManager
    {
        public readonly List<DownloadJob> Jobs;
        readonly object gate = new object();
        readonly Settings settings;
        // Nachbearbeitung (Konvertieren, Vorschaubild) immer nur eine auf einmal
        readonly object postGate = new object();

        public int MaxParallel = 2;
        public Action Changed;                    // aus beliebigem Thread
        public Action<string> Log;                // Zeile fürs Protokoll
        public Action<string> FolderReady;        // Zielordner (voller Pfad) ist fertig: neu einlesen

        const int SegmentWorkers = 4;
        const int SegmentRetries = 4;

        // Zielordner mit fertigen Downloads, die noch eingelesen werden müssen
        readonly HashSet<string> unreadFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public DownloadManager(Settings settings)
        {
            this.settings = settings;
            Jobs = DownloadStore.Load();
            MaxParallel = Math.Max(1, Math.Min(4, settings.GetInt("download.parallel", 2)));
            // Viele gleichzeitige Verbindungen zum selben Server (Standard in .NET: 2)
            if (ServicePointManager.DefaultConnectionLimit < 32) ServicePointManager.DefaultConnectionLimit = 32;
        }

        void Notify()
        {
            if (Changed != null) { try { Changed(); } catch { } }
        }

        void Write(string line)
        {
            if (Log != null) { try { Log(line); } catch { } }
        }

        public void Save()
        {
            lock (gate) DownloadStore.Save(new List<DownloadJob>(Jobs));
        }

        public bool AnyRunning
        {
            get
            {
                lock (gate)
                {
                    foreach (DownloadJob j in Jobs) if (j.State == DownloadState.Running || j.State == DownloadState.Processing) return true;
                }
                return false;
            }
        }

        public void Add(DownloadJob job)
        {
            lock (gate) Jobs.Add(job);
            Save();
            Notify();
            Schedule();
        }

        // Startet wartende Downloads, bis MaxParallel laufen
        public void Schedule()
        {
            List<DownloadJob> start = new List<DownloadJob>();
            lock (gate)
            {
                int running = 0;
                foreach (DownloadJob j in Jobs) if (j.State == DownloadState.Running || j.State == DownloadState.Processing) running++;
                foreach (DownloadJob j in Jobs)
                {
                    if (running >= MaxParallel) break;
                    if (j.State != DownloadState.Waiting) continue;
                    j.State = DownloadState.Running;
                    j.Status = "Wird gestartet …";
                    j.PauseRequested = false;
                    j.Cts = new CancellationTokenSource();
                    start.Add(j);
                    running++;
                }
            }
            foreach (DownloadJob j in start)
            {
                DownloadJob job = j;
                Thread t = new Thread(delegate() { Run(job); });
                t.IsBackground = true;
                t.Name = "Download " + job.FileName;
                t.Start();
            }
            if (start.Count > 0) Notify();
        }

        public void Pause(DownloadJob job)
        {
            lock (gate)
            {
                if (job.State == DownloadState.Waiting) { job.State = DownloadState.Paused; job.Status = "Pausiert"; }
                else if (job.State == DownloadState.Running && job.Cts != null) { job.PauseRequested = true; job.Status = "Wird pausiert …"; job.Cts.Cancel(); }
            }
            Save();
            Notify();
        }

        // Fortsetzen bzw. Wiederholen: wieder in die Warteschlange
        public void Resume(DownloadJob job)
        {
            lock (gate)
            {
                if (job.State != DownloadState.Paused && job.State != DownloadState.Failed) return;
                job.State = DownloadState.Waiting;
                job.Status = "Wartet";
                job.Result = "";
            }
            Save();
            Notify();
            Schedule();
        }

        // Entfernt den Download samt Zwischendateien (laufend: erst abbrechen)
        public void Remove(DownloadJob job)
        {
            bool running;
            lock (gate)
            {
                running = job.State == DownloadState.Running && job.Cts != null;
                if (running) { job.RemoveRequested = true; job.Cts.Cancel(); }
                else Jobs.Remove(job);
            }
            if (!running) DownloadStore.DeleteTemp(job.TempDir);
            Save();
            Notify();
            Schedule();
        }

        // Neue Adresse (z.B. Token abgelaufen). Die Teilstücke bleiben, wenn die Playlist gleich viele hat.
        public void ChangeUrl(DownloadJob job, string url, string referer, string userAgent)
        {
            lock (gate)
            {
                job.Url = url;
                job.Referer = referer ?? "";
                job.UserAgent = userAgent ?? "";
                // Qualität neu aus der Master-Playlist bestimmen (die alte Adresse der Variante hat den alten Token)
                job.Variant = "";
            }
            Save();
            Notify();
        }

        // Aufräumen: fehlgeschlagene und fertige Einträge entfernen, ihre Zwischendateien löschen
        public long Cleanup()
        {
            List<DownloadJob> removed = new List<DownloadJob>();
            lock (gate)
            {
                foreach (DownloadJob j in Jobs) if (j.State == DownloadState.Failed || j.State == DownloadState.Done) removed.Add(j);
                foreach (DownloadJob j in removed) Jobs.Remove(j);
            }
            long freed = 0;
            foreach (DownloadJob j in removed)
            {
                freed += DownloadStore.FolderSize(j.TempDir);
                DownloadStore.DeleteTemp(j.TempDir);
            }
            List<DownloadJob> rest;
            lock (gate) rest = new List<DownloadJob>(Jobs);
            freed += DownloadStore.RemoveOrphans(rest);
            Save();
            Notify();
            return freed;
        }

        // Belegter Speicher der fehlerhaften und fertigen Downloads (für "Aufräumen")
        public long CleanupSize()
        {
            long size = 0;
            List<DownloadJob> jobs;
            lock (gate) jobs = new List<DownloadJob>(Jobs);
            foreach (DownloadJob j in jobs)
                if (j.State == DownloadState.Failed || j.State == DownloadState.Done) size += DownloadStore.FolderSize(j.TempDir);
            return size;
        }

        // Laufende Downloads anhalten (Programm wird beendet). Sie lassen sich beim nächsten Start fortsetzen.
        public void PauseAll()
        {
            lock (gate)
            {
                foreach (DownloadJob j in Jobs)
                {
                    if (j.State == DownloadState.Running && j.Cts != null) { j.PauseRequested = true; j.Cts.Cancel(); }
                    else if (j.State == DownloadState.Waiting) { j.State = DownloadState.Paused; j.Status = "Pausiert"; }
                }
            }
            Save();
        }

        // ---------------------------------------------------------------

        void SetStatus(DownloadJob job, string text)
        {
            job.Status = text;
            Notify();
        }

        void Run(DownloadJob job)
        {
            CancellationToken token = job.Cts.Token;
            string tag = job.FileName + ": ";
            try
            {
                Directory.CreateDirectory(job.TempDir);
                FfmpegTools ffmpeg = new FfmpegTools(App.Root, FfmpegLocation.Folder(settings));
                if (!ffmpeg.Installed) throw new HlsException("ffmpeg ist nicht installiert (Tools › Einstellungen).");

                // 1. Playlist und Qualität
                SetStatus(job, "Lade Playlist …");
                string text = Hls.Fetch(job.Url, job, token);
                string mediaUrl = job.Url, audioUrl = null;
                if (Hls.IsMaster(text))
                {
                    Dictionary<string, string> audio;
                    List<HlsVariant> variants = Hls.Variants(text, job.Url, out audio);
                    if (variants.Count == 0) throw new HlsException("Die Playlist enthält keine Qualitäten.");
                    HlsVariant chosen = variants[0];
                    if (job.Variant.Length > 0)
                    {
                        foreach (HlsVariant v in variants) if (v.Url == job.Variant || v.Label == job.VariantLabel) { chosen = v; break; }
                    }
                    job.Variant = chosen.Url;
                    job.VariantLabel = chosen.Label;
                    mediaUrl = chosen.Url;
                    if (chosen.AudioGroup.Length > 0) audio.TryGetValue(chosen.AudioGroup, out audioUrl);
                    text = Hls.Fetch(mediaUrl, job, token);
                }
                HlsPlaylist video = Hls.Parse(text, mediaUrl, "v");
                HlsPlaylist sound = audioUrl != null ? Hls.Parse(Hls.Fetch(audioUrl, job, token), audioUrl, "a") : null;

                // Andere Anzahl Teilstücke als beim letzten Mal (z.B. andere Folge): von vorne
                int total = video.Segments.Count + (sound != null ? sound.Segments.Count : 0);
                if (job.SegmentsTotal > 0 && job.SegmentsTotal != total)
                {
                    job.AddMessage("Die Playlist hat jetzt " + total + " statt " + job.SegmentsTotal + " Teilstücke: Download beginnt von vorne.");
                    DownloadStore.DeleteTemp(job.TempDir);
                    Directory.CreateDirectory(job.TempDir);
                    job.Bytes = 0;
                }
                job.SegmentsTotal = total;
                job.Duration = video.Duration;
                Save();

                // 2. Schlüssel, Init-Segmente und Teilstücke
                FetchExtras(job, video, token);
                if (sound != null) FetchExtras(job, sound, token);
                List<KeyValuePair<string, string>> todo = new List<KeyValuePair<string, string>>();
                int done = 0;
                AddSegments(job, video, "v", todo, ref done);
                if (sound != null) AddSegments(job, sound, "a", todo, ref done);
                job.SegmentsDone = done;
                DownloadSegments(job, todo, token);

                // 3. Zusammenfügen
                SetStatus(job, "Wird zusammengefügt …");
                string videoList = Path.Combine(job.TempDir, "video.m3u8");
                File.WriteAllText(videoList, Hls.LocalPlaylist(video, "v"), new UTF8Encoding(false));
                string audioList = null;
                if (sound != null)
                {
                    audioList = Path.Combine(job.TempDir, "audio.m3u8");
                    File.WriteAllText(audioList, Hls.LocalPlaylist(sound, "a"), new UTF8Encoding(false));
                }
                string output = Path.Combine(job.TempDir, "ergebnis.mp4");
                FileOps.TryDelete(output);
                Join(job, ffmpeg, videoList, audioList, output, token);

                // 4. Prüfen
                lock (gate) job.State = DownloadState.Processing;
                SetStatus(job, "Wird geprüft …");
                Verify(job, ffmpeg, output, token);

                // 5. In den Zielordner (Name schon vergeben: mit Zusatz)
                SetStatus(job, "Wird in den Zielordner verschoben …");
                string targetDir = job.TargetDir;
                string reason;
                if (!Folders.IsInsideRoot(targetDir, out reason)) throw new HlsException("Der Zielordner liegt nicht im Installationsordner.");
                Directory.CreateDirectory(targetDir);
                string target = FileOps.UniquePath(targetDir, job.FileName);
                if (!string.Equals(Path.GetFileName(target), job.FileName, StringComparison.OrdinalIgnoreCase))
                    job.AddMessage("„" + job.FileName + "“ gab es schon, gespeichert als „" + Path.GetFileName(target) + "“.");
                FileOps.MoveAcross(output, target, false);
                job.ResultPath = target;

                // 6. Nachbearbeitung: nicht abspielbar -> konvertieren, Vorschaubild
                SetStatus(job, "Vorschaubild …");
                PostProcess(job, ffmpeg, target, token);

                DownloadStore.DeleteTemp(job.TempDir);
                lock (gate)
                {
                    job.State = DownloadState.Done;
                    if (job.Result != "Warnung") job.Result = "OK";
                    job.Status = job.Result == "OK" ? "Fertig" : "Fertig mit Warnungen";
                    unreadFolders.Add(targetDir);
                }
                Write(tag + "fertig (" + Fmt.Size(SafeLength(target)) + ")" + (job.Result == "Warnung" ? ", mit Warnungen" : ""));
            }
            catch (OperationCanceledException)
            {
                lock (gate)
                {
                    if (job.RemoveRequested) Jobs.Remove(job);
                    else { job.State = DownloadState.Paused; job.Status = "Pausiert"; }
                }
                if (job.RemoveRequested) DownloadStore.DeleteTemp(job.TempDir);
            }
            catch (Exception e)
            {
                string message = e is Http.HttpStatusException && (((Http.HttpStatusException)e).Code == 403 || ((Http.HttpStatusException)e).Code == 410)
                    ? "Der Server verweigert den Zugriff (" + e.Message + "). Die Adresse ist vermutlich abgelaufen: mit „URL bearbeiten“ eine neue einfügen und fortsetzen."
                    : e.Message;
                lock (gate)
                {
                    if (job.RemoveRequested) Jobs.Remove(job);
                    else
                    {
                        job.State = DownloadState.Failed;
                        job.Result = "Fehler";
                        job.Status = "Fehler";
                        job.AddMessage(message);
                    }
                }
                if (job.RemoveRequested) DownloadStore.DeleteTemp(job.TempDir);
                else Write(tag + "Fehler: " + message);
            }
            finally
            {
                job.Cts = null;
                Save();
                Notify();
                CheckFolderReady(job);
                Schedule();
            }
        }

        static long SafeLength(string path)
        {
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        void FetchExtras(DownloadJob job, HlsPlaylist p, CancellationToken token)
        {
            foreach (KeyValuePair<string, string> kv in p.Extras)
            {
                string path = Path.Combine(job.TempDir, kv.Key);
                if (File.Exists(path) && new FileInfo(path).Length > 0) continue;
                byte[] data = Http.Get(kv.Value, job, token, SegmentRetries);
                File.WriteAllBytes(path, data);
            }
        }

        static void AddSegments(DownloadJob job, HlsPlaylist p, string prefix, List<KeyValuePair<string, string>> todo, ref int done)
        {
            bool fmp4 = Hls.IsFragmentedMp4(p);
            for (int i = 0; i < p.Segments.Count; i++)
            {
                string name = Hls.SegmentName(prefix, i, fmp4);
                string path = Path.Combine(job.TempDir, name);
                if (File.Exists(path) && new FileInfo(path).Length > 0) done++;
                else todo.Add(new KeyValuePair<string, string>(path, p.Segments[i].Url));
            }
        }

        void DownloadSegments(DownloadJob job, List<KeyValuePair<string, string>> todo, CancellationToken token)
        {
            if (todo.Count == 0) return;
            ConcurrentQueue<KeyValuePair<string, string>> queue = new ConcurrentQueue<KeyValuePair<string, string>>(todo);
            Exception failure = null;
            object failGate = new object();
            DateTime lastNotify = DateTime.MinValue;
            List<Thread> threads = new List<Thread>();
            using (CancellationTokenSource inner = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                for (int w = 0; w < Math.Min(SegmentWorkers, todo.Count); w++)
                {
                    Thread t = new Thread(delegate()
                    {
                        KeyValuePair<string, string> item;
                        while (!inner.IsCancellationRequested && queue.TryDequeue(out item))
                        {
                            try
                            {
                                byte[] data = Http.Get(item.Value, job, inner.Token, SegmentRetries);
                                string part = item.Key + ".part";
                                File.WriteAllBytes(part, data);
                                FileOps.Replace(part, item.Key);
                                lock (failGate)
                                {
                                    job.SegmentsDone++;
                                    job.Bytes += data.Length;
                                    if ((DateTime.Now - lastNotify).TotalMilliseconds > 400)
                                    {
                                        lastNotify = DateTime.Now;
                                        job.Status = "Lädt";
                                        Notify();
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                lock (failGate) { if (failure == null) failure = e; }
                                try { inner.Cancel(); } catch { }
                            }
                        }
                    });
                    t.IsBackground = true;
                    threads.Add(t);
                    t.Start();
                }
                foreach (Thread t in threads) t.Join();
            }
            token.ThrowIfCancellationRequested();
            if (failure != null)
            {
                if (failure is OperationCanceledException) throw new IOException("Download unterbrochen");
                throw failure;
            }
            Save();
        }

        static readonly string[] HlsInput = { "-allowed_extensions", "ALL", "-protocol_whitelist", "file,crypto,data" };

        void Join(DownloadJob job, FfmpegTools ffmpeg, string videoList, string audioList, string output, CancellationToken token)
        {
            List<string> args = new List<string> { "-hide_banner", "-nostdin", "-y", "-v", "warning" };
            args.AddRange(HlsInput);
            args.Add("-i"); args.Add(videoList);
            if (audioList != null)
            {
                args.AddRange(HlsInput);
                args.Add("-i"); args.Add(audioList);
                args.AddRange(new string[] { "-map", "0:v:0", "-map", "1:a:0" });
            }
            else
            {
                args.AddRange(new string[] { "-map", "0:v?", "-map", "0:a?" });
            }
            args.AddRange(new string[] { "-c", "copy", "-movflags", "+faststart", output });
            ProcResult r = Proc.Run(ffmpeg.FfmpegPath, args, token, null);
            List<string> lines = MessageLines(r.StdErr);
            if (r.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
            {
                foreach (string l in lines) job.AddMessage("ffmpeg: " + l);
                throw new HlsException("ffmpeg konnte die Teilstücke nicht zusammenfügen (Code " + r.ExitCode + ").");
            }
            if (lines.Count > 0)
            {
                job.Result = "Warnung";
                foreach (string l in lines) job.AddMessage("Zusammenfügen: " + l);
            }
        }

        static List<string> MessageLines(string stderr)
        {
            List<string> lines = new List<string>();
            foreach (string raw in (stderr ?? "").Split('\n'))
            {
                string l = raw.Trim();
                if (l.Length > 0 && lines.Count < 40) lines.Add(l);
            }
            return lines;
        }

        void Verify(DownloadJob job, FfmpegTools ffmpeg, string output, CancellationToken token)
        {
            MediaInfo info = ffmpeg.Probe(output, token);
            if (info == null || info.Duration <= 0) throw new HlsException("Die fertige Datei ist nicht lesbar.");
            double diff = Math.Abs(info.Duration - job.Duration);
            if (job.Duration > 0 && diff > Math.Max(2.0, job.Duration * 0.01))
            {
                job.Result = "Warnung";
                job.AddMessage("Die Dauer (" + Fmt.Time(info.Duration) + ") weicht von der Playlist ab (" + Fmt.Time(job.Duration) + "). Eventuell fehlt ein Teil.");
            }
            List<double> starts = new List<double> { 0 };
            if (info.Duration > 30) starts.Add(info.Duration / 2);
            if (info.Duration > 60) starts.Add(info.Duration - 15);
            if (job.FullCheck)
            {
                SetStatus(job, "Wird vollständig geprüft …");
                Decode(job, ffmpeg, output, -1, token);
            }
            else
            {
                foreach (double s in starts) Decode(job, ffmpeg, output, s, token);
            }
        }

        // Dekodiertest: start < 0 = ganze Datei, sonst 5 Sekunden ab start
        void Decode(DownloadJob job, FfmpegTools ffmpeg, string file, double start, CancellationToken token)
        {
            List<string> args = new List<string> { "-hide_banner", "-nostdin", "-v", "error" };
            if (start >= 0) { args.Add("-ss"); args.Add(Fmt.Inv(start, "0.###")); }
            args.Add("-i"); args.Add(file);
            if (start >= 0) { args.Add("-t"); args.Add("5"); }
            args.AddRange(new string[] { "-f", "null", "-" });
            ProcResult r = Proc.Run(ffmpeg.FfmpegPath, args, token, null);
            List<string> lines = MessageLines(r.StdErr);
            if (r.ExitCode != 0 || lines.Count > 0)
            {
                job.Result = "Warnung";
                string where = start < 0 ? "Prüfung" : "Prüfung bei " + Fmt.Time(start);
                if (lines.Count == 0) job.AddMessage(where + ": ffmpeg meldet Code " + r.ExitCode + ".");
                foreach (string l in lines) job.AddMessage(where + ": " + l);
            }
        }

        void PostProcess(DownloadJob job, FfmpegTools ffmpeg, string file, CancellationToken token)
        {
            lock (postGate)
            {
                TaskContext ctx = new TaskContext();
                ctx.Token = token;
                ctx.Settings = settings;
                ctx.Ffmpeg = ffmpeg;
                ctx.LogLine = delegate(string line) { Write(job.FileName + ": " + line); };
                ctx.AskConflict = delegate(string source, string target) { return null; };   // Konflikt: überspringen
                List<string> files = new List<string> { file };
                try
                {
                    ConvertOptions conv = new ConvertOptions();
                    conv.Folder = Path.GetDirectoryName(file);
                    conv.OnlyFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { file };
                    conv.Audio = false;
                    conv.Preset = settings.GetString("convert.preset", "medium");
                    conv.QualitySD = settings.GetInt("convert.qualitySD", 21);
                    conv.QualityHD = settings.GetInt("convert.qualityHD", 19);
                    conv.QualityFullHD = settings.GetInt("convert.qualityFullHD", 18);
                    conv.Quality4K = settings.GetInt("convert.quality4K", 17);
                    conv.Results = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    SetStatus(job, "Prüft Abspielbarkeit …");
                    ConvertTask.Run(ctx, conv);
                    List<string> created;
                    if (conv.Results.TryGetValue(file, out created) && created != null && created.Count > 0)
                    {
                        files = created;
                        job.ResultPath = created[0];
                        job.AddMessage("Für den Browser konvertiert.");
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    job.Result = "Warnung";
                    job.AddMessage("Konvertieren: " + e.Message);
                }
                try
                {
                    SetStatus(job, "Vorschaubild …");
                    PreviewTask.MakeFor(ctx, files);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    job.Result = "Warnung";
                    job.AddMessage("Vorschaubild: " + e.Message);
                }
            }
        }

        // Ein Zielordner ist fertig, wenn dort nichts mehr wartet oder läuft (pausierte und
        // fehlgeschlagene halten das Einlesen nicht auf)
        void CheckFolderReady(DownloadJob job)
        {
            string folder = job.TargetDir;
            bool ready;
            lock (gate)
            {
                if (!unreadFolders.Contains(folder)) return;
                ready = true;
                foreach (DownloadJob j in Jobs)
                    if (j.IsActive && string.Equals(j.TargetDir, folder, StringComparison.OrdinalIgnoreCase)) { ready = false; break; }
                if (ready) unreadFolders.Remove(folder);
            }
            if (ready && FolderReady != null)
            {
                try { FolderReady(folder); } catch { }
            }
        }

        // ---------------------------------------------------------------
        // Vorschläge für den Dateinamen
        // ---------------------------------------------------------------

        static readonly Regex EpisodeRx = new Regex(@"S(\d{1,3})\s*E(\d{1,4})", RegexOptions.IgnoreCase);
        static readonly Regex SeasonFolderRx = new Regex(@"^(staffel|season|s)\s*0*(\d{1,3})\b", RegexOptions.IgnoreCase);

        // Nächste fehlende Folge im Ordner, z.B. "S01E05.mp4"; null = kein Serienordner erkennbar
        public static string SuggestName(string folder, IEnumerable<string> reserved)
        {
            int season = 0, maxEpisode = 0;
            Match fm = SeasonFolderRx.Match(Path.GetFileName((folder ?? "").TrimEnd('\\')) ?? "");
            if (fm.Success) season = int.Parse(fm.Groups[2].Value, CultureInfo.InvariantCulture);
            List<string> names = new List<string>();
            try { if (Directory.Exists(folder)) foreach (string f in Directory.GetFiles(folder)) names.Add(Path.GetFileName(f)); } catch { }
            if (reserved != null) names.AddRange(reserved);
            foreach (string n in names)
            {
                Match m = EpisodeRx.Match(n);
                if (!m.Success) continue;
                int s = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                int e = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                if (season == 0) season = s;
                if (s == season) maxEpisode = Math.Max(maxEpisode, e);
            }
            if (season == 0) return null;
            return "S" + season.ToString("00", CultureInfo.InvariantCulture) + "E" + (maxEpisode + 1).ToString("00", CultureInfo.InvariantCulture) + ".mp4";
        }

        // Name für die n-te Adresse (ab 0): mit Folgennummer fortlaufend, sonst mit Zähler
        public static string NumberedName(string name, int index)
        {
            if (index == 0) return name;
            Match m = EpisodeRx.Match(name);
            if (m.Success)
            {
                int e = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) + index;
                string digits = e.ToString(new string('0', Math.Max(2, m.Groups[2].Value.Length)), CultureInfo.InvariantCulture);
                return name.Substring(0, m.Groups[2].Index) + digits + name.Substring(m.Groups[2].Index + m.Groups[2].Length);
            }
            string stem = Path.GetFileNameWithoutExtension(name), ext = Path.GetExtension(name);
            return stem + " " + (index + 1) + ext;
        }

        // Gültiger Dateiname mit .mp4
        public static string CleanFileName(string name)
        {
            string n = (name ?? "").Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c.ToString(), "");
            n = n.Trim().TrimEnd('.');
            if (n.Length == 0) return null;
            if (!n.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) n += ".mp4";
            return n;
        }
    }
}
