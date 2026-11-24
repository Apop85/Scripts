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
        // Lücken, die nach den Reparaturrunden blieben (DEC-373). GapPending: der Benutzer hat noch nicht
        // zwischen Behalten und Löschen gewählt (Knöpfe in der Liste, kein Dialog)
        public List<MediaGap> Gaps = new List<MediaGap>();
        public bool GapPending;

        public string GapText { get { return "Lücke: " + MediaGap.Describe(Gaps); } }

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
            if (Gaps.Count > 0)
            {
                List<object> gaps = new List<object>();
                foreach (MediaGap g in Gaps) gaps.Add(new List<object> { Math.Round(g.At, 2), Math.Round(g.Length, 2) });
                o["gaps"] = gaps;
                o["gapPending"] = GapPending;
            }
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
            List<object> gaps = o.Arr("gaps");
            if (gaps != null)
                foreach (object g in gaps)
                {
                    List<object> pair = g as List<object>;
                    if (pair != null && pair.Count == 2 && pair[0] is double && pair[1] is double) j.Gaps.Add(new MediaGap((double)pair[0], (double)pair[1]));
                }
            j.GapPending = j.Gaps.Count > 0 && o.Bool("gapPending", false);
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

        // Einträge aus downloads.json, die sich nicht lesen liessen (z.B. ohne id/url): bleiben unverändert erhalten
        static readonly List<object> Unreadable = new List<object>();

        public static int UnreadableCount { get { lock (Unreadable) return Unreadable.Count; } }

        // error != null: Datei vorhanden, aber nicht lesbar (Netzlaufwerk weg, beschädigt). Dann darf weder
        // die Liste überschrieben noch ein Ordner mit Teilstücken gelöscht werden (seit 4.1.0).
        public static List<DownloadJob> Load(out string error)
        {
            List<DownloadJob> jobs = new List<DownloadJob>();
            error = null;
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    // Ordner nicht erreichbar (Netzlaufwerk weg): nicht wie "keine Liste" behandeln (SFLIX-20261123-079)
                    if (!Directory.Exists(App.SysDir)) throw new DirectoryNotFoundException("Ordner sflix_sys nicht erreichbar");
                    if (!File.Exists(FilePath)) return jobs;
                    JsonObject root = Json.Parse(File.ReadAllText(FilePath, Encoding.UTF8)) as JsonObject;
                    List<object> list = root == null ? null : root.Arr("downloads");
                    if (list == null) throw new InvalidDataException("unbekanntes Format");
                    List<object> unreadable = new List<object>();
                    foreach (object o in list)
                    {
                        DownloadJob j = DownloadJob.FromJson(o as JsonObject);
                        if (j != null) jobs.Add(j);
                        else unreadable.Add(o);
                    }
                    // Unlesbare Einträge nicht still verwerfen: unverändert merken und beim Speichern wieder
                    // mitschreiben, ihre Teilstücke nicht als verwaist löschen (SFLIX-20261123-079)
                    lock (Unreadable) { Unreadable.Clear(); Unreadable.AddRange(unreadable); }
                    return jobs;
                }
                catch (Exception e)
                {
                    jobs.Clear();
                    if (attempt >= 2) { error = e.Message; return jobs; }
                    Thread.Sleep(500 * (attempt + 1));
                }
            }
        }

        public static void Save(List<DownloadJob> jobs)
        {
            // Release-Ordner: nichts schreiben (siehe ReleaseFolder)
            if (App.IsReleaseFolder) return;
            JsonObject root = new JsonObject();
            List<object> list = new List<object>();
            foreach (DownloadJob j in jobs) list.Add(j.ToJson());
            lock (Unreadable) list.AddRange(Unreadable);
            root["downloads"] = list;
            string tmp = FilePath + ".tmp";
            try
            {
                if (list.Count == 0)
                {
                    if (File.Exists(FilePath)) FileOps.TryDelete(FilePath);
                    return;
                }
                File.WriteAllText(tmp, Json.Write(root, 2), new UTF8Encoding(false));
                FileOps.Replace(tmp, FilePath);
            }
            catch { FileOps.TryDelete(tmp); }
        }

        // Der Zwischenordner liegt für alle Installationen gemeinsam unter %LOCALAPPDATA%. Jeder Ordner
        // eines Downloads merkt sich seine Installation; verwaist gelöscht werden nur eigene Ordner und
        // ältere ohne Markierung nach 30 Tagen (SFLIX-20261123-090).
        const string OwnerFile = "stefflix_installation.txt";
        const int UnmarkedDays = 30;

        static string OwnerKey { get { return Path.GetFullPath(App.Root).TrimEnd('\\'); } }

        public static void MarkOwner(string dir)
        {
            try
            {
                string file = Path.Combine(dir, OwnerFile);
                if (!File.Exists(file)) File.WriteAllText(file, OwnerKey, new UTF8Encoding(false));
            }
            catch { }
        }

        // Darf ein Ordner ohne Eintrag in der eigenen Liste gelöscht werden?
        static bool IsOwnOrphan(string dir)
        {
            string file = Path.Combine(dir, OwnerFile);
            if (File.Exists(file))
                return string.Equals(File.ReadAllText(file, Encoding.UTF8).Trim(), OwnerKey, StringComparison.OrdinalIgnoreCase);
            return (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(dir)).TotalDays > UnmarkedDays;
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
                lock (Unreadable)
                    foreach (object o in Unreadable)
                    {
                        JsonObject u = o as JsonObject;
                        string id = u != null ? u.Str("id") : null;
                        if (!string.IsNullOrEmpty(id)) known.Add(id);
                    }
                foreach (string dir in Directory.GetDirectories(TempRoot))
                {
                    if (known.Contains(Path.GetFileName(dir))) continue;
                    try { if (!IsOwnOrphan(dir)) continue; }
                    catch { continue; }
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
        public bool Encrypted;       // AES-128: nur die Länge lässt sich prüfen
        public double Start;         // Beginn in Sekunden (Summe der Dauern davor)
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
            bool first = true, encrypted = false;
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
                    encrypted = method != "NONE";
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
                seg.Encrypted = encrypted;
                seg.Start = p.Duration;
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

        // Prüft ein geladenes Teilstück (DEC-373): nicht leer, keine Textantwort (HTML, JSON, XML),
        // unverschlüsseltes .ts mit Sync-Byte 0x47 im 188-Byte-Raster (ID3-Kopf am Anfang erlaubt),
        // .m4s mit lückenloser Box-Kette, verschlüsselt nur die Länge (Vielfaches von 16).
        // Ergebnis: Problem in Worten oder null.
        public static string SegmentProblem(byte[] data, bool fmp4, bool encrypted)
        {
            if (data == null || data.Length == 0) return "leer";
            if (LooksLikeText(data)) return "Textantwort statt Video";
            if (encrypted) return data.Length % 16 == 0 ? null : "unvollständig (verschlüsselt, Länge passt nicht)";
            if (fmp4) return BoxChainOk(data, 0) ? null : "unvollständig (MP4-Boxen brechen ab)";
            int offset = Id3Length(data);
            if (offset >= data.Length) return "kein Video (nur ID3)";
            if ((data.Length - offset) % 188 != 0) return "unvollständig (Länge kein Vielfaches von 188 Byte)";
            for (int i = offset; i < data.Length; i += 188)
                if (data[i] != 0x47) return "beschädigt (Sync-Byte fehlt bei Byte " + i + ")";
            return null;
        }

        // Init-Segment (fMP4): Box-Kette; Schlüssel: genau 16 Byte
        public static string ExtraProblem(string name, byte[] data)
        {
            if (data == null || data.Length == 0) return "leer";
            if (name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)) return data.Length == 16 ? null : "Schlüssel hat nicht 16 Byte";
            if (LooksLikeText(data)) return "Textantwort statt Video";
            return BoxChainOk(data, 0) ? null : "unvollständig (MP4-Boxen brechen ab)";
        }

        static bool LooksLikeText(byte[] data)
        {
            int i = 0;
            if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF) i = 3;   // UTF-8-BOM
            while (i < data.Length && i < 64 && (data[i] == ' ' || data[i] == '\t' || data[i] == '\r' || data[i] == '\n')) i++;
            if (i >= data.Length) return true;   // nur Leerzeichen
            if (data[i] != '<' && data[i] != '{' && data[i] != '[') return false;
            string head = Encoding.ASCII.GetString(data, i, Math.Min(32, data.Length - i)).ToLowerInvariant();
            return head.StartsWith("<!doctype") || head.StartsWith("<html") || head.StartsWith("<?xml") || head.StartsWith("<head")
                || head.StartsWith("<body") || head.StartsWith("{\"") || head.StartsWith("{ \"") || head.StartsWith("[{") || head.StartsWith("{}");
        }

        // Länge eines ID3v2-Kopfs am Anfang (0 ohne)
        static int Id3Length(byte[] d)
        {
            if (d.Length < 10 || d[0] != 'I' || d[1] != 'D' || d[2] != '3') return 0;
            int size = (d[6] & 0x7f) << 21 | (d[7] & 0x7f) << 14 | (d[8] & 0x7f) << 7 | (d[9] & 0x7f);
            return 10 + size + ((d[5] & 0x10) != 0 ? 10 : 0);
        }

        // MP4-Boxen lückenlos bis zum Ende
        static bool BoxChainOk(byte[] d, int offset)
        {
            long pos = offset;
            int boxes = 0;
            while (pos < d.Length)
            {
                if (pos + 8 > d.Length) return false;
                long size = (long)d[pos] << 24 | (long)d[pos + 1] << 16 | (long)d[pos + 2] << 8 | d[pos + 3];
                for (int k = 4; k < 8; k++) { byte c = d[pos + k]; if (c < 0x20 || c > 0x7e) return false; }   // Typ: 4 Zeichen
                if (size == 1)
                {
                    if (pos + 16 > d.Length) return false;
                    size = 0;
                    for (int k = 8; k < 16; k++) size = (size << 8) | d[pos + k];
                    if (size < 16) return false;
                }
                else if (size == 0) return true;   // bis zum Ende
                else if (size < 8) return false;
                if (size > d.Length - pos) return false;   // Box reicht über das Ende (auch: riesige 64-Bit-Grösse)
                pos += size;
                boxes++;
            }
            return pos == d.Length && boxes > 0;
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

        // Antwort kürzer als Content-Length (Verbindungsabbruch); wird wie die HTML-Antwort gemeldet
        public class IncompleteResponseException : IOException
        {
            public IncompleteResponseException(long got, long expected)
                : base("unvollständig (" + got + " von " + expected + " Byte)") { }
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

        // Dateiname aus der Adresse für Meldungen (ohne Abfrage, sie kann Schlüssel enthalten)
        static string FileNameOf(string url)
        {
            try
            {
                string name = Path.GetFileName(new Uri(url).AbsolutePath);
                return string.IsNullOrEmpty(name) ? "Teilstück" : "„" + name + "“";
            }
            catch { return "Teilstück"; }
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
                    bool hopeless = e.Code == 403 || e.Code == 404 || e.Code == 410 || e.Code == 401 || e.Code == 400;
                    if (hopeless && attempt >= 1 || attempt >= retries) throw;
                }
                catch (OperationCanceledException) { throw; }
                catch (IncompleteResponseException e)
                {
                    if (attempt >= retries) throw;
                    if (job != null) job.AddMessage(FileNameOf(url) + ": Antwort " + e.Message + ", wird neu geladen.");
                }
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
                        // Abgebrochene Antwort (kürzer als angekündigt) nicht als fertig übernehmen (DEC-373).
                        // Komprimiert übertragen: Content-Length gilt für die komprimierten Bytes.
                        if (resp.ContentLength >= 0 && string.IsNullOrEmpty(resp.ContentEncoding) && ms.Length != resp.ContentLength)
                            throw new IncompleteResponseException(ms.Length, resp.ContentLength);
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
        public readonly List<DownloadJob> Jobs;   // nur unter gate ändern; von aussen Snapshot() verwenden

        // Kopie der Liste (die Liste wird im Hintergrund geladen und von den Download-Threads geändert)
        public List<DownloadJob> Snapshot()
        {
            lock (gate) return new List<DownloadJob>(Jobs);
        }
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

        // downloads.json ist gelesen. Vorher wird nicht gespeichert (würde die Liste überschreiben) und
        // werden keine verwaisten Zwischenordner gelöscht (gehörten sonst zu noch nicht geladenen Einträgen).
        volatile bool loaded;
        bool savePending;
        volatile bool storeBroken;   // downloads.json vorhanden, aber nicht lesbar
        public bool Loaded { get { return loaded; } }

        public DownloadManager(Settings settings)
        {
            this.settings = settings;
            Jobs = new List<DownloadJob>();
            MaxParallel = Math.Max(1, Math.Min(4, settings.GetInt("download.parallel", 2)));
            // Viele gleichzeitige Verbindungen zum selben Server (Standard in .NET: 2)
            if (ServicePointManager.DefaultConnectionLimit < 32) ServicePointManager.DefaultConnectionLimit = 32;
        }

        // downloads.json liegt im Installationsordner (oft Netzlaufwerk): im Hintergrund lesen. Erst nach dem
        // Setzen von Log, Changed und FolderReady aufrufen, sonst gehen Hinweise beim Laden verloren
        // (SFLIX-20261123-090).
        int started;
        public void Start()
        {
            if (Interlocked.Exchange(ref started, 1) != 0) return;
            ThreadPool.QueueUserWorkItem(delegate { LoadJobs(); });
        }

        void LoadJobs()
        {
            string error;
            List<DownloadJob> stored = DownloadStore.Load(out error);
            bool save;
            lock (gate)
            {
                // Vor dem Laden hinzugefügte Downloads bleiben hinten
                Jobs.InsertRange(0, stored);
                storeBroken = error != null;
                loaded = true;
                save = savePending;
            }
            if (error != null)
            {
                Write("FEHLER: " + DownloadStore.FileName + " nicht lesbar (" + error + "). Die Liste wird in dieser Sitzung nicht gespeichert, "
                      + "Zwischendateien bleiben erhalten. STEFFLIX neu starten, sobald die Datei erreichbar ist.");
                Notify();
                return;
            }
            int unreadable = DownloadStore.UnreadableCount;
            if (unreadable > 0)
                Write("Hinweis: " + unreadable + " Eintrag/Einträge in " + DownloadStore.FileName + " nicht lesbar. Sie bleiben unverändert in der Datei, ihre Zwischendateien werden nicht gelöscht.");
            if (save) Save();
            Notify();
            // Verwaiste Zwischendateien (z.B. nach einem Absturz)
            List<DownloadJob> all;
            lock (gate) all = new List<DownloadJob>(Jobs);
            long freed = DownloadStore.RemoveOrphans(all);
            if (freed > 0) Write("verwaiste Zwischendateien gelöscht (" + Fmt.Size(freed) + ")");
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
            lock (gate)
            {
                if (!loaded) { savePending = true; return; }
                if (storeBroken) return;   // nicht lesbare Liste nicht überschreiben
                DownloadStore.Save(new List<DownloadJob>(Jobs));
            }
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

        // Fortsetzen: wieder in die Warteschlange, vorhandene Teilstücke bleiben (auch nach "URL bearbeiten"
        // bei einem Fehler). "Wiederholen" nach einem Fehler: Restart.
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

        // "Wiederholen" nach einem Fehler: alle Zwischendateien des Downloads verwerfen (Teilstücke,
        // Schlüssel, Zwischenergebnis; nur im eigenen Ordner unter TempRoot, siehe DeleteTemp) und
        // komplett neu laden, Playlist inklusive. Beschädigte Teilstücke werden so nicht wiederverwendet.
        // Löschen im Hintergrund (viele Dateien), danach in die Warteschlange.
        public void Restart(DownloadJob job)
        {
            lock (gate)
            {
                if (job.State != DownloadState.Failed || !Jobs.Contains(job)) return;
                job.Status = "Wird zurückgesetzt …";
            }
            Notify();
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (!string.IsNullOrEmpty(job.Id)) DownloadStore.DeleteTemp(job.TempDir);
                lock (gate)
                {
                    // Inzwischen entfernt oder aufgeräumt: nichts mehr tun
                    if (job.State != DownloadState.Failed || !Jobs.Contains(job)) return;
                    job.SegmentsDone = 0;
                    job.SegmentsTotal = 0;
                    job.Bytes = 0;
                    job.Result = "";
                    job.ResultPath = "";
                    job.AddMessage("Wiederholen: alle Teilstücke werden neu geladen.");
                    job.State = DownloadState.Waiting;
                    job.Status = "Wartet";
                }
                Write(job.FileName + ": wird von vorne geladen");
                Save();
                Notify();
                Schedule();
            });
        }

        // Entfernt den Download samt Zwischendateien (laufend: erst abbrechen)
        public void Remove(DownloadJob job)
        {
            bool running;
            lock (gate)
            {
                // Auch in der Nachbearbeitung erst abbrechen: sonst würden die Zwischendateien während
                // Prüfen bzw. Verschieben gelöscht
                running = (job.State == DownloadState.Running || job.State == DownloadState.Processing) && job.Cts != null;
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

        // Lücke nach den Reparaturrunden (DEC-373): Datei behalten. Der Eintrag in damaged.js bleibt.
        public void KeepGap(DownloadJob job)
        {
            lock (gate)
            {
                if (!job.GapPending) return;
                job.GapPending = false;
                job.Status = "Fertig, " + job.GapText + " (behalten)";
                job.AddMessage("Datei mit Lücke behalten.");
            }
            Write(job.FileName + ": Datei mit Lücke behalten");
            Save();
            Notify();
        }

        // Lücke: Datei in die Ablage „__gelöscht“ wie bei „Medien prüfen“, Eintrag aus damaged.js entfernen.
        // Im Hintergrund (Netzlaufwerk), danach wird der Zielordner neu eingelesen.
        public void DeleteGapFile(DownloadJob job)
        {
            string file;
            lock (gate)
            {
                if (!job.GapPending) return;
                job.GapPending = false;
                job.Status = "Wird in die Ablage verschoben …";
                file = job.ResultPath;
            }
            Notify();
            ThreadPool.QueueUserWorkItem(delegate
            {
                string message;
                try
                {
                    string reason;
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) message = "Datei nicht mehr vorhanden";
                    else if (!Folders.IsInsideRoot(Path.GetDirectoryName(file), out reason)) message = "Datei liegt nicht im Installationsordner";
                    else
                    {
                        MediaCheckTask.MoveToDeleted(file);
                        message = null;
                    }
                    DamagedList.Remove(file);
                }
                catch (Exception e) { message = e.Message; }
                lock (gate)
                {
                    if (message == null)
                    {
                        job.Result = "Fehler";
                        job.Status = "Gelöscht (" + job.GapText + ")";
                        job.AddMessage("Datei mit Lücke nach „" + MediaCheckTask.DeletedFolderName + "“ verschoben.");
                        unreadFolders.Add(job.TargetDir);
                    }
                    else
                    {
                        job.GapPending = true;
                        job.Status = job.GapText;
                        job.AddMessage("Löschen nicht möglich: " + message);
                    }
                }
                Write(job.FileName + (message == null ? ": Datei mit Lücke nach „" + MediaCheckTask.DeletedFolderName + "“ verschoben" : ": Löschen nicht möglich (" + message + ")"));
                Save();
                Notify();
                CheckFolderReady(job);
            });
        }

        // Aufräumen: fehlgeschlagene und fertige Einträge entfernen, ihre Zwischendateien löschen
        // (fertige mit offener Entscheidung zur Lücke bleiben)
        public long Cleanup()
        {
            List<DownloadJob> removed = new List<DownloadJob>();
            lock (gate)
            {
                foreach (DownloadJob j in Jobs) if ((j.State == DownloadState.Failed || j.State == DownloadState.Done) && !j.GapPending) removed.Add(j);
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
            if (loaded && !storeBroken) freed += DownloadStore.RemoveOrphans(rest);
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
                if ((j.State == DownloadState.Failed || j.State == DownloadState.Done) && !j.GapPending) size += DownloadStore.FolderSize(j.TempDir);
            return size;
        }

        // Laufende Downloads anhalten (Programm wird beendet). Sie lassen sich beim nächsten Start fortsetzen.
        public void PauseAll()
        {
            lock (gate)
            {
                foreach (DownloadJob j in Jobs)
                {
                    // Auch in der Nachbearbeitung (Prüfen, Konvertieren): sonst liefe ffmpeg nach dem Beenden weiter
                    if ((j.State == DownloadState.Running || j.State == DownloadState.Processing) && j.Cts != null) { j.PauseRequested = true; j.Cts.Cancel(); }
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
                DownloadStore.MarkOwner(job.TempDir);
                FfmpegTools ffmpeg = new FfmpegTools(App.Root, FfmpegLocation.Folder(settings));
                if (!ffmpeg.Installed) throw new HlsException("ffmpeg ist nicht installiert (Tools › Einstellungen).");

                // Während der Nachbearbeitung pausiert (z.B. Programm beendet): die Datei liegt schon im
                // Zielordner, nur noch nachbearbeiten (sonst entstünde "Name (2).mp4")
                string targetDir = job.TargetDir;
                string target;
                if (job.ResultPath.Length > 0 && File.Exists(job.ResultPath))
                {
                    target = job.ResultPath;
                    lock (gate) job.State = DownloadState.Processing;
                    job.AddMessage("Fortgesetzt: Die Datei liegt schon im Zielordner, es folgt nur noch die Nachbearbeitung.");
                    goto postProcess;
                }

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
                    DownloadStore.MarkOwner(job.TempDir);
                    job.Bytes = 0;
                }
                job.SegmentsTotal = total;
                job.Duration = video.Duration;
                Save();

                // 2. Schlüssel, Init-Segmente und Teilstücke
                FetchExtras(job, video, token);
                if (sound != null) FetchExtras(job, sound, token);
                // Vorhandene Teilstücke (Fortsetzen) werden geprüft, fehlerhafte neu geladen (DEC-373)
                SetStatus(job, "Prüft geladene Teilstücke …");
                List<SegmentFile> todo = new List<SegmentFile>();
                int done = 0;
                AddSegments(job, video, "v", todo, ref done);
                if (sound != null) AddSegments(job, sound, "a", todo, ref done);
                job.SegmentsDone = done;
                DownloadSegments(job, todo, token);

                // 3. Zusammenfügen, Lücken suchen; betroffene Teilstücke neu laden (höchstens GapRounds Runden)
                string output = Path.Combine(job.TempDir, "ergebnis.mp4");
                List<MediaGap> gaps = JoinAndScan(job, ffmpeg, video, sound, output, token);
                for (int round = 1; gaps.Count > 0 && round <= GapRounds; round++)
                {
                    List<SegmentFile> again = SegmentsAt(job, video, "v", gaps);
                    if (sound != null) again.AddRange(SegmentsAt(job, sound, "a", gaps));
                    job.AddMessage("Lücke " + MediaGap.Describe(gaps) + ": " + again.Count + " Teilstück(e) werden neu geladen (Runde " + round + " von " + GapRounds + ").");
                    Write(tag + "Lücke " + MediaGap.Describe(gaps) + ", lade " + again.Count + " Teilstück(e) neu");
                    foreach (SegmentFile s in again) FileOps.TryDelete(s.Path);
                    job.SegmentsDone = Math.Max(0, job.SegmentsDone - again.Count);
                    DownloadSegments(job, again, token);
                    gaps = JoinAndScan(job, ffmpeg, video, sound, output, token);
                }
                job.Gaps = gaps;
                job.GapPending = gaps.Count > 0;
                if (gaps.Count > 0)
                {
                    job.Result = "Warnung";
                    job.AddMessage("Die Lücke bleibt nach " + GapRounds + " Runden (" + MediaGap.Describe(gaps) + "). Die Datei wird trotzdem abgelegt: "
                                   + "in der Liste „Lücke behalten“ oder „Datei löschen“ wählen.");
                }

                // 4. Prüfen
                lock (gate) job.State = DownloadState.Processing;
                SetStatus(job, "Wird geprüft …");
                Verify(job, ffmpeg, output, token);

                // 5. In den Zielordner (Name schon vergeben: mit Zusatz)
                SetStatus(job, "Wird in den Zielordner verschoben …");
                string reason;
                if (!Folders.IsInsideRoot(targetDir, out reason)) throw new HlsException("Der Zielordner liegt nicht im Installationsordner.");
                Directory.CreateDirectory(targetDir);
                target = FileOps.UniquePath(targetDir, job.FileName);
                if (!string.Equals(Path.GetFileName(target), job.FileName, StringComparison.OrdinalIgnoreCase))
                    job.AddMessage("„" + job.FileName + "“ gab es schon, gespeichert als „" + Path.GetFileName(target) + "“.");
                FileOps.MoveAcross(output, target, false);
                job.ResultPath = target;

                // 6. Nachbearbeitung: nicht abspielbar -> konvertieren, Vorschaubild
            postProcess:
                SetStatus(job, "Vorschaubild …");
                PostProcess(job, ffmpeg, target, token);

                // Verbliebene Lücke: in die gemeinsame Liste beschädigter Dateien (DEC-374), mit der Datei
                // nach dem Konvertieren
                if (job.Gaps.Count > 0 && File.Exists(job.ResultPath)) DamagedList.Add(job.ResultPath, job.Gaps, DamagedList.SourceDownload);

                DownloadStore.DeleteTemp(job.TempDir);
                lock (gate)
                {
                    job.State = DownloadState.Done;
                    if (job.Result != "Warnung") job.Result = "OK";
                    job.Status = job.GapPending ? job.GapText : job.Result == "OK" ? "Fertig" : "Fertig mit Warnungen";
                    unreadFolders.Add(targetDir);
                }
                Write(tag + "fertig (" + Fmt.Size(SafeLength(target)) + ")" + (job.GapPending ? ", " + job.GapText : job.Result == "Warnung" ? ", mit Warnungen" : ""));
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
                string message = HttpMessage(e);
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

        // Verständliche Meldung zu HTTP-Fehlern (SFLIX-20261123-090: 400, 404, 410 wie 403 erklären)
        static string HttpMessage(Exception e)
        {
            Http.HttpStatusException h = e as Http.HttpStatusException;
            if (h == null) return e.Message;
            const string renew = "Die Adresse ist vermutlich abgelaufen: mit „URL bearbeiten“ eine neue einfügen und fortsetzen.";
            switch (h.Code)
            {
                case 401:
                case 403:
                    return "Der Server verweigert den Zugriff (" + e.Message + "). " + renew;
                case 400:
                case 404:
                case 410:
                    return "Der Server kennt die Adresse nicht (mehr) (" + e.Message + "). " + renew;
                default:
                    return e.Message;
            }
        }

        static long SafeLength(string path)
        {
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        // Ein Teilstück im Zwischenordner
        class SegmentFile
        {
            public string Path, Url;
            public int Number;            // 1-basiert, für Meldungen
            public bool Fmp4, Encrypted;
        }

        const int GapRounds = 2;

        // Schlüssel und Init-Segmente; vorhandene werden geprüft, fehlerhafte neu geladen (DEC-373)
        void FetchExtras(DownloadJob job, HlsPlaylist p, CancellationToken token)
        {
            foreach (KeyValuePair<string, string> kv in p.Extras)
            {
                string path = Path.Combine(job.TempDir, kv.Key);
                if (File.Exists(path) && Hls.ExtraProblem(kv.Key, File.ReadAllBytes(path)) == null) continue;
                for (int attempt = 0; ; attempt++)
                {
                    byte[] data = Http.Get(kv.Value, job, token, SegmentRetries);
                    string problem = Hls.ExtraProblem(kv.Key, data);
                    if (problem == null) { File.WriteAllBytes(path, data); break; }
                    if (attempt >= SegmentRetries) throw new HlsException("„" + kv.Key + "“ ist fehlerhaft (" + problem + ") und liess sich nicht korrekt laden.");
                    job.AddMessage(kv.Key + ": " + problem + ", wird neu geladen.");
                    token.WaitHandle.WaitOne(1000 * (attempt + 1));
                    token.ThrowIfCancellationRequested();
                }
            }
        }

        // Fehlende und fehlerhafte Teilstücke in todo, gültige zählen als fertig (bisher galt jede Datei
        // über 0 Byte als fertig; DEC-373)
        static void AddSegments(DownloadJob job, HlsPlaylist p, string prefix, List<SegmentFile> todo, ref int done)
        {
            bool fmp4 = Hls.IsFragmentedMp4(p);
            int broken = 0;
            for (int i = 0; i < p.Segments.Count; i++)
            {
                SegmentFile s = Segment(job, p, prefix, i, fmp4);
                if (File.Exists(s.Path) && new FileInfo(s.Path).Length > 0)
                {
                    if (Hls.SegmentProblem(File.ReadAllBytes(s.Path), fmp4, s.Encrypted) == null) { done++; continue; }
                    FileOps.TryDelete(s.Path);
                    broken++;
                }
                todo.Add(s);
            }
            if (broken > 0) job.AddMessage(broken + " geladene(s) Teilstück(e) fehlerhaft, werden neu geladen.");
        }

        static SegmentFile Segment(DownloadJob job, HlsPlaylist p, string prefix, int i, bool fmp4)
        {
            SegmentFile s = new SegmentFile();
            s.Path = Path.Combine(job.TempDir, Hls.SegmentName(prefix, i, fmp4));
            s.Url = p.Segments[i].Url;
            s.Number = i + 1;
            s.Fmp4 = fmp4;
            s.Encrypted = p.Segments[i].Encrypted;
            return s;
        }

        // Teilstücke, die eine Lücke berühren (über die Dauern der Playlist, mit 1 s Spielraum)
        static List<SegmentFile> SegmentsAt(DownloadJob job, HlsPlaylist p, string prefix, List<MediaGap> gaps)
        {
            bool fmp4 = Hls.IsFragmentedMp4(p);
            List<SegmentFile> list = new List<SegmentFile>();
            for (int i = 0; i < p.Segments.Count; i++)
            {
                HlsSegment seg = p.Segments[i];
                foreach (MediaGap g in gaps)
                    if (seg.Start <= g.At + g.Length + 1 && seg.Start + seg.Duration >= g.At - 1) { list.Add(Segment(job, p, prefix, i, fmp4)); break; }
            }
            return list;
        }

        // Zusammenfügen und Lücken suchen. Scheitert die Suche, gilt die Datei als ohne Lücke (die
        // Prüfung danach meldet weiter Abweichungen der Dauer)
        List<MediaGap> JoinAndScan(DownloadJob job, FfmpegTools ffmpeg, HlsPlaylist video, HlsPlaylist sound, string output, CancellationToken token)
        {
            SetStatus(job, "Wird zusammengefügt …");
            string videoList = Path.Combine(job.TempDir, "video.m3u8");
            File.WriteAllText(videoList, Hls.LocalPlaylist(video, "v"), new UTF8Encoding(false));
            string audioList = null;
            if (sound != null)
            {
                audioList = Path.Combine(job.TempDir, "audio.m3u8");
                File.WriteAllText(audioList, Hls.LocalPlaylist(sound, "a"), new UTF8Encoding(false));
            }
            FileOps.TryDelete(output);
            Join(job, ffmpeg, videoList, audioList, output, token);
            SetStatus(job, "Sucht Lücken …");
            try { return ffmpeg.ScanGaps(output, -1, token); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                job.AddMessage("Lückensuche nicht möglich: " + e.Message);
                return new List<MediaGap>();
            }
        }

        void DownloadSegments(DownloadJob job, List<SegmentFile> todo, CancellationToken token)
        {
            if (todo.Count == 0) return;
            ConcurrentQueue<SegmentFile> queue = new ConcurrentQueue<SegmentFile>(todo);
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
                        SegmentFile item;
                        while (!inner.IsCancellationRequested && queue.TryDequeue(out item))
                        {
                            try
                            {
                                // Jedes Teilstück prüfen, fehlerhafte neu laden (DEC-373)
                                byte[] data;
                                for (int attempt = 0; ; attempt++)
                                {
                                    data = Http.Get(item.Url, job, inner.Token, SegmentRetries);
                                    string problem = Hls.SegmentProblem(data, item.Fmp4, item.Encrypted);
                                    if (problem == null) break;
                                    if (attempt >= SegmentRetries)
                                        throw new HlsException("Teilstück " + item.Number + " ist fehlerhaft (" + problem + ") und liess sich nicht korrekt laden.");
                                    job.AddMessage("Teilstück " + item.Number + ": " + problem + ", wird neu geladen.");
                                    inner.Token.WaitHandle.WaitOne(1000 * (attempt + 1));
                                    inner.Token.ThrowIfCancellationRequested();
                                }
                                string part = item.Path + ".part";
                                File.WriteAllBytes(part, data);
                                FileOps.Replace(part, item.Path);
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
