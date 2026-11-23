// Gemeinsame Liste beschädigter Dateien (DEC-374, seit 4.2.0): sflix_sys\damaged.js, von start.html wie
// renamed.js geladen und in den Einstellungen von STEFFLIX angezeigt. Quellen: Downloader (Lücke, die
// nicht repariert werden konnte), "Medien prüfen" (Art "lückenhaft") und der Player (Brücke "damaged").
//
//   var damagedMedia = {"version": 1, "items": [{"path": "./Serien/…/S01E01.mp4", "size": <Bytes>,
//       "gaps": [{"at": <s>, "length": <s oder null>, "source": "download"|"check"|"player", "date": "<ISO UTC>"}]}]};
//
// Pfade wie in data.js (relativ, ./ und /). Ein Eintrag je Datei, Funde an derselben Stelle (±3 s)
// zusammengefasst. Beim Schreiben fallen Einträge weg, deren Datei fehlt oder deren Grösse sich geändert
// hat; höchstens 2000 Einträge (die ältesten fallen weg). Schreiben atomar über .tmp, UTF-8 ohne BOM.
// Dazu sflix_sys\player.log: eine Zeile je Fund des Players, über 256 KB fällt die ältere Hälfte weg.
// Beide sind persönliche Dateien der Installation (nie herunterladen, ersetzen oder pushen).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sflix
{
    public static class DamagedList
    {
        public const string FileName = "damaged.js", LogName = "player.log";
        public const string SourceDownload = "download", SourceCheck = "check", SourcePlayer = "player";
        const string Prefix = "var damagedMedia = ";
        const int MaxItems = 2000;
        const long MaxLogBytes = 256 * 1024;
        const double SameSpot = 3.0;

        static readonly object sync = new object();

        public static string FilePath { get { return Path.Combine(App.SysDir, FileName); } }
        public static string LogPath { get { return Path.Combine(App.SysDir, LogName); } }

        // Leere Liste (InstallTask legt sie wie renamed.js an)
        public static string EmptyContent { get { return Prefix + "{\"version\": 1, \"items\": []};\n"; } }

        // Voller Pfad im Installationsordner -> Pfad wie in data.js ("./Filme/Film.mp4"); null ausserhalb
        public static string ToWebPath(string full)
        {
            string rel = DataTask.ToRelative(full);
            if (string.IsNullOrEmpty(rel)) return null;
            return "./" + rel.Replace('\\', '/');
        }

        // Pfad aus der Weboberfläche -> voller Pfad, nur innerhalb des Installationsordners (sonst null)
        public static string FromWebPath(string web)
        {
            if (string.IsNullOrEmpty(web)) return null;
            string rel = web.StartsWith("./") ? web.Substring(2) : web.TrimStart('/');
            rel = rel.Replace('/', '\\');
            if (rel.Length == 0 || rel.IndexOf(':') >= 0 || rel.StartsWith("\\")) return null;
            foreach (string seg in rel.Split('\\')) if (seg == "..") return null;
            try
            {
                string full = Path.GetFullPath(Path.Combine(App.Root, rel));
                string reason;
                return Folders.IsInsideRoot(Path.GetDirectoryName(full), out reason) ? full : null;
            }
            catch { return null; }
        }

        // Funde einer Datei eintragen (length null = unbekannt)
        public static void Add(string fullPath, List<MediaGap> gaps, string source)
        {
            if (gaps == null || gaps.Count == 0) return;
            Update(delegate(List<JsonObject> items)
            {
                string web = ToWebPath(fullPath);
                if (web == null) return false;
                long size;
                try { size = new FileInfo(fullPath).Length; }
                catch { return false; }
                JsonObject item = Find(items, web);
                if (item != null && (long)item.Num("size", -1) != size) { items.Remove(item); item = null; }   // andere Datei
                if (item == null)
                {
                    item = new JsonObject();
                    item["path"] = web;
                    item["size"] = (double)size;
                    item["gaps"] = new List<object>();
                    items.Add(item);
                }
                List<object> list = item.Arr("gaps");
                string date = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
                foreach (MediaGap g in gaps)
                {
                    JsonObject same = null;
                    foreach (object o in list)
                    {
                        JsonObject e = o as JsonObject;
                        if (e != null && Math.Abs(e.Num("at", -100) - g.At) <= SameSpot) { same = e; break; }
                    }
                    if (same == null) { same = new JsonObject(); list.Add(same); }
                    same["at"] = Math.Round(g.At, 1);
                    // Länge bleibt bekannt, wenn ein späterer Fund (Player) sie nicht kennt
                    if (g.Length > 0) same["length"] = Math.Round(g.Length, 1);
                    else if (same["length"] == null) same["length"] = null;
                    same["source"] = source;
                    same["date"] = date;
                }
                return true;
            });
        }

        public static void Add(string fullPath, double at, double length, string source)
        {
            Add(fullPath, new List<MediaGap> { new MediaGap(at, length) }, source);
        }

        // Eintrag einer Datei entfernen (in die Ablage verschoben, im Player "Aus Liste entfernen")
        public static void Remove(string fullPath)
        {
            string web = ToWebPath(fullPath);
            if (web == null) return;
            RemoveWeb(web);
        }

        public static void RemoveWeb(string web)
        {
            Update(delegate(List<JsonObject> items)
            {
                JsonObject item = Find(items, web);
                if (item == null) return false;
                items.Remove(item);
                return true;
            });
        }

        // Fund des Players (Brücke "damaged"): Liste ergänzen, Zeile in player.log
        public static void ReportFromPlayer(string web, double at, double skipped)
        {
            string full = FromWebPath(web);
            if (full == null || !File.Exists(full) || double.IsNaN(at) || at < 0) return;
            Add(full, new List<MediaGap> { new MediaGap(at, 0) }, SourcePlayer);
            WriteLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "  Beschädigt: " + ToWebPath(full)
                     + " bei " + MinSec(at) + ", " + Fmt.Inv(skipped > 0 ? skipped : 5, "0") + " s übersprungen");
        }

        static string MinSec(double seconds)
        {
            long s = (long)Math.Floor(Math.Max(0, seconds));
            return (s / 60) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        static JsonObject Find(List<JsonObject> items, string web)
        {
            foreach (JsonObject i in items) if (string.Equals(i.Str("path"), web, StringComparison.OrdinalIgnoreCase)) return i;
            return null;
        }

        // Lesen, ändern, bereinigen, schreiben unter einer Sperre. change: true = geändert
        static void Update(Func<List<JsonObject>, bool> change)
        {
            if (App.IsReleaseFolder) return;
            lock (sync)
            {
                try
                {
                    List<JsonObject> items = Load();
                    bool changed = change(items);
                    changed |= Clean(items);
                    if (changed) Save(items);
                }
                catch { }
            }
        }

        static List<JsonObject> Load()
        {
            List<JsonObject> items = new List<JsonObject>();
            if (!File.Exists(FilePath)) return items;
            string text = File.ReadAllText(FilePath, Encoding.UTF8).Trim();
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end < start) return items;
            JsonObject root = Json.Parse(text.Substring(start, end - start + 1)) as JsonObject;
            List<object> list = root == null ? null : root.Arr("items");
            if (list == null) return items;
            foreach (object o in list)
            {
                JsonObject i = o as JsonObject;
                if (i != null && !string.IsNullOrEmpty(i.Str("path")) && i.Arr("gaps") != null) items.Add(i);
            }
            return items;
        }

        // Fehlende oder veränderte Dateien entfernen, höchstens MaxItems (die ältesten fallen weg)
        static bool Clean(List<JsonObject> items)
        {
            int before = items.Count;
            items.RemoveAll(delegate(JsonObject i)
            {
                string full = FromWebPath(i.Str("path"));
                if (full == null) return true;
                try
                {
                    FileInfo fi = new FileInfo(full);
                    return !fi.Exists || fi.Length != (long)i.Num("size", -1) || i.Arr("gaps").Count == 0;
                }
                catch { return false; }   // Netzlaufwerk kurz weg: Eintrag behalten
            });
            if (items.Count > MaxItems)
            {
                items.Sort(delegate(JsonObject a, JsonObject b) { return string.CompareOrdinal(Latest(b), Latest(a)); });
                items.RemoveRange(MaxItems, items.Count - MaxItems);
            }
            return items.Count != before;
        }

        static string Latest(JsonObject item)
        {
            string latest = "";
            foreach (object o in item.Arr("gaps"))
            {
                JsonObject g = o as JsonObject;
                string d = g == null ? null : g.Str("date");
                if (d != null && string.CompareOrdinal(d, latest) > 0) latest = d;
            }
            return latest;
        }

        static void Save(List<JsonObject> items)
        {
            JsonObject root = new JsonObject();
            root["version"] = 1.0;
            List<object> list = new List<object>();
            foreach (JsonObject i in items) list.Add(i);
            root["items"] = list;
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, Prefix + Json.Write(root, 0) + ";\n", FileOps.Utf8NoBom);
            FileOps.Replace(tmp, FilePath);
        }

        static void WriteLog(string line)
        {
            if (App.IsReleaseFolder) return;
            lock (sync)
            {
                try
                {
                    if (File.Exists(LogPath) && new FileInfo(LogPath).Length > MaxLogBytes)
                    {
                        string[] lines = File.ReadAllLines(LogPath, Encoding.UTF8);
                        string[] rest = new string[lines.Length - lines.Length / 2];
                        Array.Copy(lines, lines.Length / 2, rest, 0, rest.Length);
                        string tmp = LogPath + ".tmp";
                        File.WriteAllText(tmp, string.Join("\n", rest) + "\n", FileOps.Utf8NoBom);
                        FileOps.Replace(tmp, LogPath);
                    }
                    File.AppendAllText(LogPath, line + "\n", FileOps.Utf8NoBom);
                }
                catch { }
            }
        }
    }
}
