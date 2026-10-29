// Aufgabe "Folgen umbenennen" (Nachfolger von folgenliste.py und serien_umbenennen.py).
//
// 1. Folgenliste einer Serie aus dem Internet holen: fernsehserien.de (deutsche Titel),
//    sonst TVmaze (englische Titel). Die Auswahl der Serie geschieht im Dialog (EpisodeDialogs.cs).
// 2. Plan erstellen: Folgennummern aus dem Dateinamen (S01E02, 1x02; Doppelfolgen wie S01E01E02,
//    S01E0102 oder 1x01-02 behalten alle Nummern), Zielname "Serie - S01E02 - Titel.ext" im Ordner
//    "Staffel 01". Staffelordner werden angeglichen ("Staffel 1" -> "Staffel 01"), gibt es beide,
//    wird zusammengeführt. Folgen in der falschen Staffel oder direkt im Serienordner werden
//    in den richtigen Staffelordner verschoben.
// 3. Vorschau im Dialog, danach umbenennen. Vorschaubilder ziehen mit, renamed.js wird ergänzt.
// 4. Jeder Lauf wird in sflix_sys\folgen_rueckgaengig.json protokolliert und lässt sich
//    rückgängig machen ("renameundo").

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sflix
{
    // Treffer der Seriensuche
    public class SeriesHit
    {
        public string Id, Title, Year, Info;
    }

    // Folgen einer Serie: Staffel -> Folge -> Titel
    public class EpisodeList
    {
        public string Source;
        public string SeriesTitle;
        public string SeriesId;           // Kennung bei der Quelle (zum Neuladen ohne Suche)
        public DateTime Loaded = DateTime.Now;   // bei gespeicherten Listen: Zeitpunkt des Ladens
        public readonly SortedDictionary<int, SortedDictionary<int, string>> Seasons = new SortedDictionary<int, SortedDictionary<int, string>>();

        public void Add(int season, int episode, string title)
        {
            SortedDictionary<int, string> s;
            if (!Seasons.TryGetValue(season, out s)) { s = new SortedDictionary<int, string>(); Seasons[season] = s; }
            if (!s.ContainsKey(episode)) s[episode] = (title ?? "").Trim();
        }

        // Titel einer Folge; null, wenn die Folge nicht in der Liste steht
        public string Title(int season, int episode)
        {
            SortedDictionary<int, string> s;
            string t;
            if (Seasons.TryGetValue(season, out s) && s.TryGetValue(episode, out t)) return t;
            return null;
        }

        public int Count
        {
            get { int n = 0; foreach (SortedDictionary<int, string> s in Seasons.Values) n += s.Count; return n; }
        }

        // Specials (Staffel 0) nicht verwenden. Hat eine Serie nur Staffel 0, gilt sie als Staffel 1.
        public void DropSpecials()
        {
            if (Seasons.Count == 1 && Seasons.ContainsKey(0))
            {
                SortedDictionary<int, string> only = Seasons[0];
                Seasons.Clear();
                Seasons[1] = only;
            }
            else Seasons.Remove(0);
        }
    }

    // ------------------------------------------------------------------
    // Quellen
    // ------------------------------------------------------------------

    public abstract class EpisodeSource
    {
        public abstract string Name { get; }
        public abstract string Language { get; }
        public abstract List<SeriesHit> Search(string name, CancellationToken token);
        public abstract EpisodeList Load(SeriesHit hit, CancellationToken token);

        // Reihenfolge der Suche: zuerst deutsche Titel
        public static readonly EpisodeSource[] All = { new FernsehserienSource(), new TvmazeSource() };

        public static EpisodeSource ByName(string name)
        {
            foreach (EpisodeSource s in All)
                if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) return s;
            return null;
        }
    }

    // ------------------------------------------------------------------
    // Gespeicherte Folgenlisten (sflix_sys\folgen_cache.json), damit nicht bei jedem Lauf dieselbe
    // Abfrage an die Webseite geht. Gefunden wird eine Liste über den Seriennamen (Ordnername oder
    // gefundener Titel, ohne Gross-/Kleinschreibung, Jahr und Satzzeichen), nicht über den Pfad.
    // Gespeichert wird nur eine Liste mit mindestens einer Folge; ein fehlgeschlagenes Neuladen
    // überschreibt nichts. Kein Ablaufdatum, "Neu laden" ersetzt den alten Stand.
    // ------------------------------------------------------------------
    public static class EpisodeCache
    {
        const string FileName = "folgen_cache.json";

        public static string FilePath { get { return Path.Combine(App.SysDir, FileName); } }

        // "Breaking Bad (2008)" -> "breaking bad"
        public static string Normalize(string name)
        {
            string s = (name ?? "").ToLowerInvariant();
            s = Regex.Replace(s, @"\(\s*(19|20)\d\d\s*\)", " ");
            s = Regex.Replace(s, @"[^\p{L}\p{Nd}]+", " ");
            return s.Trim();
        }

        static List<object> ReadEntries()
        {
            try
            {
                if (!File.Exists(FilePath)) return new List<object>();
                JsonObject root = Json.Parse(File.ReadAllText(FilePath, Encoding.UTF8)) as JsonObject;
                List<object> series = root != null ? root.Arr("series") : null;
                return series ?? new List<object>();
            }
            catch { return new List<object>(); }
        }

        static bool Matches(JsonObject entry, string key)
        {
            if (entry == null || key == "") return false;
            List<object> names = entry.Arr("names");
            if (names != null)
                foreach (object n in names)
                    if (n is string && (string)n == key) return true;
            return Normalize(entry.Str("title")) == key;
        }

        // Gespeicherte Liste für einen Serienordner, sonst null
        public static EpisodeList Find(string folderName)
        {
            string key = Normalize(folderName);
            foreach (object o in ReadEntries())
            {
                JsonObject entry = o as JsonObject;
                if (!Matches(entry, key)) continue;
                EpisodeList list = new EpisodeList();
                list.Source = entry.Str("source");
                list.SeriesId = entry.Str("id");
                list.SeriesTitle = entry.Str("title") ?? folderName;
                DateTime loaded;
                if (DateTime.TryParse(entry.Str("loaded") ?? "", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out loaded))
                    list.Loaded = loaded;
                JsonObject seasons = entry.Obj("seasons");
                if (seasons != null)
                    foreach (string season in seasons.Keys)
                    {
                        JsonObject episodes = seasons.Obj(season);
                        int s;
                        if (episodes == null || !int.TryParse(season, NumberStyles.Integer, CultureInfo.InvariantCulture, out s)) continue;
                        foreach (string episode in episodes.Keys)
                        {
                            int e;
                            if (int.TryParse(episode, NumberStyles.Integer, CultureInfo.InvariantCulture, out e))
                                list.Add(s, e, episodes.Str(episode));
                        }
                    }
                return list.Count > 0 ? list : null;
            }
            return null;
        }

        // Speichert die Liste (ersetzt einen Eintrag derselben Serie). Leere Listen nie.
        public static void Save(string folderName, EpisodeList list, TaskContext ctx)
        {
            if (list == null || list.Count == 0) return;
            try
            {
                List<string> names = new List<string>();
                foreach (string n in new string[] { Normalize(folderName), Normalize(list.SeriesTitle) })
                    if (n != "" && !names.Contains(n)) names.Add(n);

                List<object> kept = new List<object>();
                foreach (object o in ReadEntries())
                {
                    JsonObject entry = o as JsonObject;
                    if (entry == null) continue;
                    bool same = string.Equals(entry.Str("source"), list.Source, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrEmpty(list.SeriesId) && entry.Str("id") == list.SeriesId;
                    foreach (string n in names) if (Matches(entry, n)) same = true;
                    if (!same) kept.Add(entry);
                }

                JsonObject seasons = new JsonObject();
                foreach (KeyValuePair<int, SortedDictionary<int, string>> season in list.Seasons)
                {
                    JsonObject episodes = new JsonObject();
                    foreach (KeyValuePair<int, string> ep in season.Value)
                        episodes[ep.Key.ToString(CultureInfo.InvariantCulture)] = ep.Value ?? "";
                    seasons[season.Key.ToString(CultureInfo.InvariantCulture)] = episodes;
                }
                List<object> nameList = new List<object>();
                foreach (string n in names) nameList.Add(n);
                JsonObject item = new JsonObject();
                item["names"] = nameList;
                item["source"] = list.Source ?? "";
                item["id"] = list.SeriesId ?? "";
                item["title"] = list.SeriesTitle ?? "";
                item["loaded"] = list.Loaded.ToString("o", CultureInfo.InvariantCulture);
                item["seasons"] = seasons;
                kept.Insert(0, item);

                JsonObject root = new JsonObject();
                root["series"] = kept;
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, Json.Write(root, 1), FileOps.Utf8NoBom);
                FileOps.Replace(tmp, FilePath);
            }
            catch (Exception e)
            {
                if (ctx != null) ctx.Log("Warnung: Folgenliste konnte nicht gespeichert werden: " + e.Message);
            }
        }

        // Dieselbe Serie neu von der Quelle laden (ohne Suche). null, wenn das nicht geht.
        public static EpisodeList Reload(EpisodeList cached, CancellationToken token, out string error)
        {
            error = null;
            EpisodeSource src = EpisodeSource.ByName(cached.Source);
            if (src == null || string.IsNullOrEmpty(cached.SeriesId))
            {
                error = "Quelle unbekannt";
                return null;
            }
            try
            {
                SeriesHit hit = new SeriesHit();
                hit.Id = cached.SeriesId;
                hit.Title = cached.SeriesTitle;
                EpisodeList fresh = src.Load(hit, token);
                if (fresh == null || fresh.Count == 0)
                {
                    error = "keine Folgen gefunden";
                    return null;
                }
                return fresh;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
    }

    static class EpisodeHttp
    {
        const string Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) STEFFLIX-Updater (Folgen einer eigenen Serie umbenennen)";

        static EpisodeHttp()
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;   // TLS 1.2
        }

        // Lädt eine Seite. Bremst der Server (429/503), wird kurz gewartet und erneut versucht.
        public static string Get(string url, bool html, CancellationToken token, out Uri finalUri)
        {
            for (int attempt = 0; ; attempt++)
            {
                token.ThrowIfCancellationRequested();
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 20000;
                req.ReadWriteTimeout = 20000;
                req.UserAgent = Agent;
                req.Accept = html ? "text/html" : "application/json";
                req.Headers["Accept-Language"] = "de-DE,de;q=0.9";
                req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                req.AllowAutoRedirect = true;
                using (token.Register(delegate { try { req.Abort(); } catch { } }))
                {
                    try
                    {
                        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                        using (StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                        {
                            finalUri = resp.ResponseUri;
                            return reader.ReadToEnd();
                        }
                    }
                    catch (WebException e)
                    {
                        token.ThrowIfCancellationRequested();
                        HttpWebResponse r = e.Response as HttpWebResponse;
                        int code = r != null ? (int)r.StatusCode : 0;
                        if ((code == 429 || code == 503) && attempt < 3)
                        {
                            int wait = 2 << attempt;
                            int retryAfter;
                            if (int.TryParse(r.Headers["Retry-After"] ?? "", out retryAfter)) wait = Math.Min(retryAfter, 30);
                            token.WaitHandle.WaitOne(wait * 1000);
                            continue;
                        }
                        throw new DownloadException(code > 0 ? "HTTP " + code : "keine Verbindung (" + e.Status + ")");
                    }
                    catch (IOException e)
                    {
                        token.ThrowIfCancellationRequested();
                        throw new DownloadException("Verbindung abgebrochen (" + e.Message + ")");
                    }
                }
            }
        }

        public static string StripTags(string html)
        {
            string text = WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));
            return string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        }
    }

    // fernsehserien.de: deutsche Titel, ohne Schlüssel. Liest die HTML-Seiten (Suche, Episodenguide).
    public class FernsehserienSource : EpisodeSource
    {
        const string Base = "https://www.fernsehserien.de";
        public override string Name { get { return "fernsehserien.de"; } }
        public override string Language { get { return "deutsche Titel"; } }

        public override List<SeriesHit> Search(string name, CancellationToken token)
        {
            Uri final;
            string page = EpisodeHttp.Get(Base + "/suche/" + Uri.EscapeDataString(name), true, token, out final);
            List<SeriesHit> hits = new List<SeriesHit>();
            string path = final != null ? final.AbsolutePath : "/suche";
            if (!path.StartsWith("/suche", StringComparison.OrdinalIgnoreCase))
            {
                // Eindeutiger Treffer: direkt auf die Serienseite weitergeleitet
                Match m = Regex.Match(page, "<h1 class=\"serien-titel\"[^>]*>(.*?)</h1>", RegexOptions.Singleline);
                SeriesHit hit = new SeriesHit();
                hit.Id = path.Trim('/').Split('/')[0];
                hit.Title = m.Success ? EpisodeHttp.StripTags(m.Groups[1].Value) : hit.Id;
                hit.Year = "";
                hit.Info = "";
                hits.Add(hit);
                return hits;
            }
            foreach (Match m in Regex.Matches(page,
                "<li class=\"[^\"]*suchergebnisse-sendung[^\"]*\">\\s*<a [^>]*href=\"/([^\"/]+)\"[^>]*>.*?<dl>(.*?)</dl>", RegexOptions.Singleline))
            {
                List<string> parts = new List<string>();
                foreach (Match p in Regex.Matches(m.Groups[2].Value, "<d[td][^>]*>(.*?)</d[td]>", RegexOptions.Singleline))
                    parts.Add(EpisodeHttp.StripTags(p.Groups[1].Value));
                SeriesHit hit = new SeriesHit();
                hit.Id = m.Groups[1].Value;
                hit.Title = parts.Count > 1 ? string.Join(" - ", parts.GetRange(0, parts.Count - 1).ToArray())
                          : parts.Count == 1 ? parts[0] : hit.Id;
                hit.Info = parts.Count > 1 ? parts[parts.Count - 1] : "";
                Match year = Regex.Match(hit.Info, "\\b(19|20)\\d\\d\\b");
                hit.Year = year.Success ? year.Value : "";
                hits.Add(hit);
            }
            return hits;
        }

        public override EpisodeList Load(SeriesHit hit, CancellationToken token)
        {
            Uri final;
            string page = EpisodeHttp.Get(Base + "/" + hit.Id + "/episodenguide", true, token, out final);
            EpisodeList list = new EpisodeList();
            list.Source = Name;
            list.SeriesTitle = hit.Title;
            list.SeriesId = hit.Id;
            // Jede Folge ist ein Link mit title="<Staffel>.<Folge> <Titel>", z.B. "1.01 Kulturelle Unterschiede"
            foreach (Match m in Regex.Matches(page, "<a role=\"row\"[^>]*\\btitle=\"(\\d+)\\.(\\d+)\\s+([^\"]*)\""))
                list.Add(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), WebUtility.HtmlDecode(m.Groups[3].Value));
            list.DropSpecials();
            return list;
        }
    }

    // TVmaze: ohne Schlüssel, nur englische Titel
    public class TvmazeSource : EpisodeSource
    {
        public override string Name { get { return "TVmaze"; } }
        public override string Language { get { return "englische Titel"; } }

        public override List<SeriesHit> Search(string name, CancellationToken token)
        {
            Uri final;
            string json = EpisodeHttp.Get("https://api.tvmaze.com/search/shows?q=" + Uri.EscapeDataString(name), false, token, out final);
            List<SeriesHit> hits = new List<SeriesHit>();
            List<object> arr = Json.Parse(json) as List<object>;
            if (arr == null) return hits;
            foreach (object o in arr)
            {
                JsonObject entry = o as JsonObject;
                JsonObject show = entry != null ? entry.Obj("show") : null;
                if (show == null) continue;
                JsonObject net = show.Obj("network") ?? show.Obj("webChannel");
                JsonObject country = net != null ? net.Obj("country") : null;
                List<string> info = new List<string>();
                foreach (string s in new string[] { country != null ? country.Str("code") : null, net != null ? net.Str("name") : null, show.Str("language") })
                    if (!string.IsNullOrEmpty(s)) info.Add(s);
                SeriesHit hit = new SeriesHit();
                hit.Id = ((long)show.Num("id", 0)).ToString(CultureInfo.InvariantCulture);
                hit.Title = show.Str("name") ?? hit.Id;
                string premiered = show.Str("premiered") ?? "";
                hit.Year = premiered.Length >= 4 ? premiered.Substring(0, 4) : "";
                hit.Info = string.Join(", ", info.ToArray());
                hits.Add(hit);
            }
            return hits;
        }

        public override EpisodeList Load(SeriesHit hit, CancellationToken token)
        {
            Uri final;
            string json = EpisodeHttp.Get("https://api.tvmaze.com/shows/" + hit.Id + "/episodes", false, token, out final);
            EpisodeList list = new EpisodeList();
            list.Source = Name;
            list.SeriesTitle = hit.Title;
            list.SeriesId = hit.Id;
            List<object> arr = Json.Parse(json) as List<object>;
            if (arr != null)
                foreach (object o in arr)
                {
                    JsonObject ep = o as JsonObject;
                    if (ep == null) continue;
                    double number = ep.Num("number", -1);
                    if (number < 0) continue;   // Specials ohne Nummer
                    list.Add((int)ep.Num("season", 0), (int)number, ep.Str("name"));
                }
            list.DropSpecials();
            return list;
        }
    }

    // ------------------------------------------------------------------
    // Plan
    // ------------------------------------------------------------------

    public class EpisodeOp
    {
        public string Kind;        // "folder" (umbenennen), "merge" (zusammenführen), "rename", "move"
        public string Source;      // vollständiger Pfad
        public string Target;      // vollständiger Pfad
        public int Season, Episode;   // Episode = erste Folge (bei Doppelfolgen die kleinste)
        public List<int> Episodes = new List<int>();   // alle Folgen der Datei (Doppelfolgen: mehrere)
        public string Note = "";
        public string Group;       // gleiche Zielfolge: nur eine Datei darf gewählt sein
        public bool Selected = true;
    }

    public class EpisodePlan
    {
        public string SeriesDir, SeriesName;
        public EpisodeList List;
        public readonly List<EpisodeOp> Ops = new List<EpisodeOp>();
        public readonly List<string> Unmatched = new List<string>();   // "Datei: Grund"
        public readonly List<string> Missing = new List<string>();     // "Staffel 02: E05, E06"
        public int Unchanged;
    }

    public static class EpisodeTask
    {
        // Videos, die umbenannt werden (auch solche, die noch konvertiert werden müssen)
        static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".mp4", ".m4v", ".webm", ".ogv", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m2ts", ".mts",
            ".mpeg", ".mpg", ".3gp", ".vob", ".divx", ".asf", ".f4v",
        };
        static readonly string[] PreviewExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        static readonly Regex DoubleFour = new Regex(@"S(\d{1,3})[ ._-]*E(\d{2})(\d{2})(?!\d)", RegexOptions.IgnoreCase);
        static readonly Regex SeasonEpisode = new Regex(@"S(\d{1,3})[ ._-]*E(\d{1,4})(?!\d)", RegexOptions.IgnoreCase);
        static readonly Regex Cross = new Regex(@"(?<![0-9])(\d{1,2})x(\d{2,3})(?![0-9])", RegexOptions.IgnoreCase);
        // Weitere Teile einer Doppelfolge direkt danach: "E03", "-E03", " E03" oder "-03" bzw. "-1x03".
        // Eine Nummer ohne "E" zählt nur, wenn sie knapp über der letzten liegt (sonst z.B. ein Jahr).
        static readonly Regex MoreWithE = new Regex(@"\G[ ._]*-?[ ._]*E(\d{1,4})(?!\d)", RegexOptions.IgnoreCase);
        static readonly Regex MoreWithDash = new Regex(@"\G[ ._]*-[ ._]*(?:\d{1,2}x)?(\d{1,3})(?![\dp])", RegexOptions.IgnoreCase);
        // Teil-Angabe am Ende eines Titels: "Pilot (1)", "Pilot, Teil 1", "Pilot - Part 2"
        static readonly Regex TitlePart = new Regex(@"^(?<base>.*?)(?<sep>\s*\(\s*(?:Teil|Part|Pt\.?)?\s*|,?\s*(?:-\s*)?(?:Teil|Part|Pt\.?)\s*)(?<n>\d+)(?<end>\s*\)?)$", RegexOptions.IgnoreCase);
        static readonly Regex SeasonFolder = new Regex(@"^(?:staffel|season|s)[ ._-]*0*(\d{1,3})$", RegexOptions.IgnoreCase);

        public const string ReplacedFolderName = "__ersetzt";
        const string UndoFileName = "folgen_rueckgaengig.json";

        public static string UndoFile { get { return Path.Combine(App.SysDir, UndoFileName); } }
        public static bool UndoFileExists { get { return File.Exists(UndoFile); } }

        // Folgennummer aus einem Dateinamen (bei Doppelfolgen die kleinste); false, wenn keine erkannt wird
        public static bool ParseEpisode(string fileName, out int season, out int episode)
        {
            List<int> episodes;
            episode = 0;
            if (!ParseEpisodes(fileName, out season, out episodes)) return false;
            episode = episodes[0];
            return true;
        }

        // Alle Folgen einer Datei, aufsteigend und ohne Doppelte: S01E02 -> [2], S01E02E03, S01E02-E03,
        // S01E02-03, S01E0203, 1x02-03 und 1x02-1x03 -> [2, 3]
        public static bool ParseEpisodes(string fileName, out int season, out List<int> episodes)
        {
            season = 0;
            episodes = new List<int>();
            string stem = Path.GetFileNameWithoutExtension(fileName);
            Match m = DoubleFour.Match(stem);
            int end;
            if (m.Success)
            {
                season = int.Parse(m.Groups[1].Value);
                episodes.Add(int.Parse(m.Groups[2].Value));
                episodes.Add(int.Parse(m.Groups[3].Value));
                end = m.Index + m.Length;
            }
            else
            {
                m = SeasonEpisode.Match(stem);
                if (!m.Success) m = Cross.Match(stem);
                if (!m.Success) return false;
                season = int.Parse(m.Groups[1].Value);
                episodes.Add(int.Parse(m.Groups[2].Value));
                end = m.Index + m.Length;
            }
            // Weitere Teile direkt danach
            while (end < stem.Length)
            {
                Match more = MoreWithE.Match(stem, end);
                if (more.Success)
                {
                    episodes.Add(int.Parse(more.Groups[1].Value));
                    end = more.Index + more.Length;
                    continue;
                }
                more = MoreWithDash.Match(stem, end);
                int last = episodes[episodes.Count - 1];
                int n;
                if (!more.Success || !int.TryParse(more.Groups[1].Value, out n) || n <= last || n > last + 3) break;
                episodes.Add(n);
                end = more.Index + more.Length;
            }
            List<int> sorted = new List<int>();
            foreach (int e in episodes) if (!sorted.Contains(e)) sorted.Add(e);
            sorted.Sort();
            episodes = sorted;
            return true;
        }

        // "S01E02" bzw. "S01E02E03"
        public static string EpisodeLabel(int season, IList<int> episodes)
        {
            StringBuilder sb = new StringBuilder("S" + season.ToString("00", CultureInfo.InvariantCulture));
            foreach (int e in episodes) sb.Append("E" + e.ToString("00", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        // Titel einer Doppelfolge: "Pilot (1)" + "Pilot (2)" -> "Pilot (1+2)", gleiche Titel einmal,
        // sonst "Titel A + Titel B"
        public static string CombineTitles(IList<string> titles)
        {
            List<string> list = new List<string>();
            foreach (string t in titles) if (!string.IsNullOrEmpty(t)) list.Add(t.Trim());
            if (list.Count == 0) return "";
            if (list.Count == 1) return list[0];
            bool allSame = true;
            foreach (string t in list) if (!string.Equals(t, list[0], StringComparison.OrdinalIgnoreCase)) allSame = false;
            if (allSame) return list[0];

            Match first = TitlePart.Match(list[0]);
            if (first.Success && first.Groups["base"].Value.Trim().Length > 0)
            {
                List<string> numbers = new List<string>();
                bool samePattern = true;
                foreach (string t in list)
                {
                    Match p = TitlePart.Match(t);
                    if (!p.Success || !string.Equals(p.Groups["base"].Value.Trim(), first.Groups["base"].Value.Trim(), StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(p.Groups["sep"].Value, first.Groups["sep"].Value, StringComparison.OrdinalIgnoreCase))
                    {
                        samePattern = false;
                        break;
                    }
                    numbers.Add(p.Groups["n"].Value);
                }
                if (samePattern)
                    return first.Groups["base"].Value + first.Groups["sep"].Value + string.Join("+", numbers.ToArray()) + first.Groups["end"].Value;
            }
            return string.Join(" + ", list.ToArray());
        }

        public static string SeasonFolderName(int season)
        {
            return "Staffel " + season.ToString("00", CultureInfo.InvariantCulture);
        }

        // Zielname: "Serie - S01E02 - Titel.ext". ß wird zu ss, Zeichen, die Windows oder btoa()
        // nicht verarbeiten kann, werden ersetzt (wie bei "Daten einlesen").
        public static string BuildName(string series, int season, int episode, string title, string ext)
        {
            return BuildName(series, season, new int[] { episode }, title, ext);
        }

        // Doppelfolgen: "Serie - S01E02E03 - Titel.ext"
        public static string BuildName(string series, int season, IList<int> episodes, string title, string ext)
        {
            string name = series + " - " + EpisodeLabel(season, episodes);
            string cleanTitle = CleanPart(title ?? "");
            if (cleanTitle.Length > 0) name += " - " + cleanTitle;
            name = CleanPart(name);
            return DataTask.SanitizeFileName(name + ext);
        }

        static string CleanPart(string text)
        {
            text = text.Replace("ß", "ss").Replace("ẞ", "SS");
            text = text.Replace(": ", " - ").Replace(":", "-").Replace("/", "-").Replace("\\", "-").Replace("|", "-")
                       .Replace("\"", "'").Replace("?", "").Replace("*", "").Replace("<", "").Replace(">", "");
            return string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }

        public static EpisodePlan Plan(string seriesDir, EpisodeList list)
        {
            seriesDir = Path.GetFullPath(seriesDir).TrimEnd('\\');
            EpisodePlan plan = new EpisodePlan();
            plan.SeriesDir = seriesDir;
            plan.SeriesName = Path.GetFileName(seriesDir);
            plan.List = list;

            // Staffelordner angleichen: "Staffel 1", "Season 1", "S01" -> "Staffel 01"
            Dictionary<string, string> folderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string d in SafeDirs(seriesDir))
            {
                string name = Path.GetFileName(d);
                Match m = SeasonFolder.Match(name);
                if (!m.Success) continue;
                string want = SeasonFolderName(int.Parse(m.Groups[1].Value));
                if (name == want) continue;
                string target = Path.Combine(seriesDir, want);
                EpisodeOp op = new EpisodeOp();
                op.Source = d;
                op.Target = target;
                if (Directory.Exists(target) && !string.Equals(target, d, StringComparison.OrdinalIgnoreCase))
                {
                    op.Kind = "merge";
                    op.Note = "Inhalt mit „" + want + "“ zusammenführen";
                }
                else
                {
                    op.Kind = "folder";
                    op.Note = "Ordner umbenennen";
                }
                plan.Ops.Add(op);
                folderMap[d] = target;
            }

            // Dateien
            List<string> files = new List<string>();
            CollectVideos(seriesDir, files);
            files.Sort(StringComparer.OrdinalIgnoreCase);
            HashSet<string> covered = new HashSet<string>();
            List<EpisodeOp> fileOps = new List<EpisodeOp>();
            // Welche Datei welche Folge enthält (für Konflikte zwischen Doppelfolge und Einzelfolge);
            // unveränderte Dateien zählen mit (ohne Vorgang)
            Dictionary<string, List<string>> claims = new Dictionary<string, List<string>>();
            foreach (string file in files)
            {
                string rel = file.Substring(seriesDir.Length + 1);
                int season;
                List<int> episodes;
                if (!ParseEpisodes(Path.GetFileName(file), out season, out episodes))
                {
                    plan.Unmatched.Add(rel + ": keine Folgennummer (S01E02 oder 1x02) erkannt");
                    continue;
                }
                // Titel aller Teile; der erste Teil muss in der Folgenliste stehen
                List<string> titles = new List<string>();
                foreach (int e in episodes) titles.Add(list.Title(season, e));
                if (titles[0] == null)
                {
                    plan.Unmatched.Add(rel + ": " + EpisodeLabel(season, episodes) + " steht nicht in der Folgenliste");
                    continue;
                }
                foreach (int e in episodes) covered.Add(season + "x" + e);
                string title = CombineTitles(titles);
                string targetDir = Path.Combine(seriesDir, SeasonFolderName(season));
                string target = Path.Combine(targetDir, BuildName(plan.SeriesName, season, episodes, title, Path.GetExtension(file)));
                string dirAfterFolders = MapFolder(Path.GetDirectoryName(file), folderMap);
                foreach (int e in episodes)
                {
                    List<string> c;
                    string key = season + "x" + e;
                    if (!claims.TryGetValue(key, out c)) { c = new List<string>(); claims[key] = c; }
                    if (!c.Contains(target.ToLowerInvariant())) c.Add(target.ToLowerInvariant());
                }
                if (string.Equals(dirAfterFolders, targetDir, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(file) == Path.GetFileName(target))
                {
                    plan.Unchanged++;
                    continue;
                }
                EpisodeOp op = new EpisodeOp();
                op.Source = file;
                op.Target = target;
                op.Season = season;
                op.Episode = episodes[0];
                op.Episodes = episodes;
                bool moves = !string.Equals(dirAfterFolders, targetDir, StringComparison.OrdinalIgnoreCase);
                op.Kind = moves ? "move" : "rename";
                op.Note = moves ? "nach „" + SeasonFolderName(season) + "“ verschieben" : "";
                if (episodes.Count > 1 && episodes[episodes.Count - 1] - episodes[0] != episodes.Count - 1)
                    op.Note = "Hinweis: Folgennummern nicht fortlaufend, Dateiname prüfen" + (op.Note.Length > 0 ? ", " + op.Note : "");
                for (int i = 1; i < titles.Count; i++)
                    if (titles[i] == null)
                        op.Note = "Hinweis: E" + episodes[i].ToString("00") + " steht nicht in der Folgenliste" + (op.Note.Length > 0 ? ", " + op.Note : "");
                fileOps.Add(op);
            }

            // Mehrere Dateien für dieselbe Folge: der Nutzer wählt eine
            Dictionary<string, List<EpisodeOp>> byTarget = new Dictionary<string, List<EpisodeOp>>(StringComparer.OrdinalIgnoreCase);
            foreach (EpisodeOp op in fileOps)
            {
                List<EpisodeOp> l;
                if (!byTarget.TryGetValue(op.Target, out l)) { l = new List<EpisodeOp>(); byTarget[op.Target] = l; }
                l.Add(op);
            }
            HashSet<string> sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files) sources.Add(MapPath(f, folderMap));
            foreach (EpisodeOp op in fileOps)
            {
                List<EpisodeOp> same = byTarget[op.Target];
                // Doppelfolge und Einzelfolge mit gemeinsamer Folge (z.B. S01E02E03 und S01E03)
                int overlap = -1;
                foreach (int e in op.Episodes)
                    if (claims[op.Season + "x" + e].Count > 1) { overlap = e; break; }
                if (same.Count > 1)
                {
                    op.Group = op.Target.ToLowerInvariant();
                    op.Selected = false;
                    op.Note = "Mehrere Dateien für " + EpisodeLabel(op.Season, op.Episodes) + ": eine wählen" +
                              (op.Note.Length > 0 ? ", " + op.Note : "");
                }
                else if (overlap >= 0)
                {
                    // Gruppe nach der ersten gemeinsamen Folge: nur eine der Dateien lässt sich wählen
                    op.Group = "folge:" + op.Season + "x" + overlap;
                    op.Selected = false;
                    op.Note = "E" + overlap.ToString("00") + " steckt in mehreren Dateien (Doppelfolge und Einzelfolge): eine wählen" +
                              (op.Note.Length > 0 ? ", " + op.Note : "");
                }
                else if (File.Exists(op.Target) && !sources.Contains(op.Target))
                {
                    op.Selected = false;
                    op.Note = "Ziel gibt es schon (wird beim Umbenennen nachgefragt)" + (op.Note.Length > 0 ? ", " + op.Note : "");
                }
            }
            plan.Ops.AddRange(fileOps);

            // Fehlende Folgen je Staffel
            foreach (KeyValuePair<int, SortedDictionary<int, string>> season in list.Seasons)
            {
                List<string> missing = new List<string>();
                foreach (int ep in season.Value.Keys)
                    if (!covered.Contains(season.Key + "x" + ep)) missing.Add("E" + ep.ToString("00"));
                if (missing.Count == 0) continue;
                plan.Missing.Add(SeasonFolderName(season.Key) + ": " +
                    (missing.Count == season.Value.Count ? "alle " + missing.Count + " Folgen" : string.Join(", ", missing.ToArray())));
            }
            return plan;
        }

        static IEnumerable<string> SafeDirs(string dir)
        {
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); }
            catch { return new string[0]; }
            List<string> result = new List<string>();
            foreach (string d in dirs)
            {
                if (Folders.Ignore(d) || string.Equals(Path.GetFileName(d), "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(d);
            }
            return result;
        }

        static void CollectVideos(string dir, List<string> result)
        {
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                    if (VideoExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith("._")) result.Add(f);
            }
            catch { return; }
            foreach (string d in SafeDirs(dir)) CollectVideos(d, result);
        }

        static string MapFolder(string dir, Dictionary<string, string> folderMap)
        {
            string mapped;
            return folderMap.TryGetValue(dir.TrimEnd('\\'), out mapped) ? mapped : dir;
        }

        // Pfad nach den Ordner-Umbenennungen (Präfix ersetzen)
        static string MapPath(string path, Dictionary<string, string> folderMap)
        {
            foreach (KeyValuePair<string, string> kv in folderMap)
                if (path.StartsWith(kv.Key + "\\", StringComparison.OrdinalIgnoreCase))
                    return kv.Value + path.Substring(kv.Key.Length);
            return path;
        }

        // ------------------------------------------------------------------
        // Ausführen
        // ------------------------------------------------------------------

        public static void Run(TaskContext ctx, string seriesDir)
        {
            if (string.IsNullOrEmpty(seriesDir) || !Directory.Exists(seriesDir))
                throw new TaskFailedException("Serienordner nicht gefunden: " + seriesDir);
            if (ctx.ChooseEpisodeList == null || ctx.ConfirmEpisodePlan == null)
                throw new TaskFailedException("Folgen umbenennen ist nur über die Oberfläche möglich.");
            seriesDir = Path.GetFullPath(seriesDir).TrimEnd('\\');
            if (string.Equals(seriesDir, Path.GetFullPath(App.Root).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                throw new TaskFailedException("Bitte den Ordner einer einzelnen Serie wählen, nicht den ganzen STEFFLIX-Ordner.");

            ctx.Log("Serienordner: " + seriesDir);
            ctx.Progress(-1, "Folgenliste wählen ...");
            string folderName = Path.GetFileName(seriesDir);
            EpisodeList list = ChooseList(ctx, folderName);
            if (list == null)
            {
                ctx.Log("Keine Folgenliste gewählt, nichts umbenannt.");
                ctx.Summary = "nichts umbenannt";
                return;
            }
            ctx.Log("Folgenliste: " + list.SeriesTitle + " (" + list.Source + "), " + list.Seasons.Count + " Staffeln, " + list.Count + " Folgen");

            EpisodePlan plan = Plan(seriesDir, list);
            ctx.Log("Geplant: " + plan.Ops.Count + " Änderungen, " + plan.Unchanged + " Dateien passen bereits, " +
                    plan.Unmatched.Count + " nicht zugeordnet");
            if (plan.Ops.Count == 0)
            {
                ctx.Log("Alle Dateien heissen bereits richtig.");
                LogLists(ctx, plan);
                ctx.Summary = "alles bereits richtig benannt";
                return;
            }
            if (!ctx.ConfirmEpisodePlan(plan))
            {
                ctx.Log("Abgebrochen, nichts umbenannt.");
                ctx.Summary = "nichts umbenannt";
                return;
            }
            Execute(ctx, plan);
            LogLists(ctx, plan);
        }

        // Gespeicherte Liste verwenden, dieselbe Serie neu laden oder neu suchen. Neu geladene oder
        // gesuchte Listen werden gespeichert (nur mit Folgen, ein Fehler überschreibt nichts).
        static EpisodeList ChooseList(TaskContext ctx, string folderName)
        {
            EpisodeList cached = EpisodeCache.Find(folderName);
            string failure = null;
            while (cached != null && ctx.ChooseCachedEpisodeList != null)
            {
                string choice = ctx.ChooseCachedEpisodeList(cached, failure);
                if (choice == null) return null;
                if (choice == "use")
                {
                    ctx.Log("Gespeicherte Folgenliste vom " + cached.Loaded.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) + " verwendet.");
                    return cached;
                }
                if (choice == "search") break;
                ctx.Progress(-1, "Lade Folgen von „" + cached.SeriesTitle + "“ ...");
                string error;
                EpisodeList fresh = EpisodeCache.Reload(cached, ctx.Token, out error);
                ctx.Token.ThrowIfCancellationRequested();
                if (fresh != null)
                {
                    EpisodeCache.Save(folderName, fresh, ctx);
                    ctx.Log("Folgenliste neu geladen und gespeichert.");
                    return fresh;
                }
                ctx.Log("Warnung: Folgenliste konnte nicht neu geladen werden (" + error + "). Die gespeicherte Liste bleibt erhalten.");
                failure = error;
            }
            EpisodeList list = ctx.ChooseEpisodeList(folderName);
            if (list != null) EpisodeCache.Save(folderName, list, ctx);
            return list;
        }

        static void LogLists(TaskContext ctx, EpisodePlan plan)
        {
            if (plan.Missing.Count > 0)
            {
                ctx.Log("Hinweis: Diese Folgen fehlen im Ordner:");
                foreach (string m in plan.Missing) ctx.Log("  " + m);
            }
            if (plan.Unmatched.Count > 0)
            {
                ctx.Log("Hinweis: Nicht zugeordnet (bleiben unverändert):");
                foreach (string u in plan.Unmatched) ctx.Log("  " + u);
            }
        }

        class Journal
        {
            public readonly List<object> Ops = new List<object>();        // ["move", von, nach] / ["mkdir", pfad] / ["rmdir", pfad]
            public readonly List<object> Pairs = new List<object>();      // renamed.js

            public void Move(string from, string to)
            {
                Ops.Add(new List<object> { "move", from, to });
                string a = UrlOf(from), b = UrlOf(to);
                if (a != null && b != null) Pairs.Add(new List<object> { a, b });
            }
        }

        // "./Serien/Show/Staffel 01" für renamed.js; null ausserhalb des STEFFLIX-Ordners
        static string UrlOf(string path)
        {
            string rel = DataTask.ToRelative(path);
            return string.IsNullOrEmpty(rel) ? null : "./" + rel.Replace('\\', '/');
        }

        static void Execute(TaskContext ctx, EpisodePlan plan)
        {
            Journal journal = new Journal();
            Dictionary<string, string> folderMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool? remembered = null;
            int done = 0, failed = 0, skipped = 0;
            List<EpisodeOp> selected = plan.Ops.FindAll(delegate(EpisodeOp o) { return o.Selected; });
            try
            {
                for (int i = 0; i < selected.Count; i++)
                {
                    ctx.Check();
                    EpisodeOp op = selected[i];
                    ctx.Progress((double)i / selected.Count, "Umbenennen " + (i + 1) + " / " + selected.Count);
                    try
                    {
                        if (op.Kind == "folder")
                        {
                            FileOps.Rename(op.Source, op.Target);
                            journal.Move(op.Source, op.Target);
                            folderMap[op.Source] = op.Target;
                            MoveFolderPreview(plan.SeriesDir, Path.GetFileName(op.Source), Path.GetFileName(op.Target), journal);
                            ctx.Log("  ORDNER  " + Path.GetFileName(op.Source) + "  ->  " + Path.GetFileName(op.Target));
                            done++;
                        }
                        else if (op.Kind == "merge")
                        {
                            int moved = MergeFolder(ctx, op.Source, op.Target, plan.SeriesDir, journal, ref remembered);
                            folderMap[op.Source] = op.Target;
                            // Staffelbild nur übernehmen, wenn der Zielordner noch keines hat
                            MoveFolderPreview(plan.SeriesDir, Path.GetFileName(op.Source), Path.GetFileName(op.Target), journal);
                            ctx.Log("  ZUSAMMENGEFÜHRT  " + Path.GetFileName(op.Source) + "  ->  " + Path.GetFileName(op.Target) + " (" + moved + " Dateien)");
                            done++;
                        }
                        else
                        {
                            string current = MapPath(op.Source, folderMap);
                            if (!File.Exists(current)) { ctx.Log("  FEHLT   " + current + " (übersprungen)"); skipped++; continue; }
                            string targetDir = Path.GetDirectoryName(op.Target);
                            EnsureDir(targetDir, journal);
                            if (File.Exists(op.Target) && !string.Equals(current, op.Target, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!AskReplace(ctx, current, op.Target, ref remembered)) { ctx.Log("  ÜBERSPRUNGEN  " + Path.GetFileName(current) + " (Ziel vorhanden)"); skipped++; continue; }
                                SetAside(op.Target, plan.SeriesDir, journal);
                            }
                            FileOps.Rename(current, op.Target);
                            journal.Move(current, op.Target);
                            MovePreview(current, op.Target, journal);
                            ctx.Log("  OK      " + Path.GetFileName(current) + "  ->  " + RelativeTo(plan.SeriesDir, op.Target));
                            done++;
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e)
                    {
                        failed++;
                        ctx.Log("  FEHLER  " + op.Source + " :: " + e.Message);
                    }
                }
            }
            finally
            {
                // Auch nach einem Abbruch: Rückgängig-Protokoll und Umbenennungen für STEFFLIX sichern
                SaveJournal(plan.SeriesDir, journal);
                if (journal.Pairs.Count > 0)
                {
                    try { DataTask.AppendRenamedPairs(journal.Pairs); }
                    catch (Exception e) { ctx.Log("Hinweis: renamed.js konnte nicht ergänzt werden (" + e.Message + ")"); }
                }
            }
            ctx.Log("Umbenannt: " + done + "   Übersprungen: " + skipped + "   Fehler: " + failed +
                    (journal.Ops.Count > 0 ? "   (rückgängig machbar unter Tools › Folgen umbenennen)" : ""));
            ctx.Summary = done + " umbenannt, " + skipped + " übersprungen, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Datei(en) konnten nicht umbenannt werden");
        }

        static string RelativeTo(string baseDir, string path)
        {
            return path.StartsWith(baseDir + "\\", StringComparison.OrdinalIgnoreCase) ? path.Substring(baseDir.Length + 1) : path;
        }

        static bool AskReplace(TaskContext ctx, string source, string target, ref bool? remembered)
        {
            if (remembered.HasValue) return remembered.Value;
            ConflictAnswer a = ctx.AskConflict != null ? ctx.AskConflict(source, target) : null;
            if (a == null) return false;
            if (a.Remember) remembered = a.Replace;
            return a.Replace;
        }

        // Eine ersetzte Datei wird nicht gelöscht, sondern nach <Serie>\__ersetzt gelegt
        // ("Ablagen leeren" räumt sie auf, "Rückgängig" holt sie zurück)
        static void SetAside(string file, string seriesDir, Journal journal)
        {
            string folder = Path.Combine(seriesDir, ReplacedFolderName);
            EnsureDir(folder, journal);
            string dest = FileOps.UniquePath(folder, Path.GetFileName(file));
            FileOps.Rename(file, dest);
            journal.Ops.Add(new List<object> { "move", file, dest });
        }

        static void EnsureDir(string dir, Journal journal)
        {
            if (Directory.Exists(dir)) return;
            string parent = Path.GetDirectoryName(dir);
            if (parent != null && !Directory.Exists(parent)) EnsureDir(parent, journal);
            Directory.CreateDirectory(dir);
            journal.Ops.Add(new List<object> { "mkdir", dir });
        }

        // Vorschaubild einer Datei mitnehmen: <Ordner>\Preview\<Name>.jpg
        static void MovePreview(string oldFile, string newFile, Journal journal)
        {
            string oldStem = Path.GetFileNameWithoutExtension(oldFile);
            string newStem = Path.GetFileNameWithoutExtension(newFile);
            string oldDir = Path.Combine(Path.GetDirectoryName(oldFile), "Preview");
            string newDir = Path.Combine(Path.GetDirectoryName(newFile), "Preview");
            foreach (string ext in PreviewExtensions)
            {
                string src = Path.Combine(oldDir, oldStem + ext);
                if (!File.Exists(src)) continue;
                string dest = Path.Combine(newDir, newStem + ext);
                if (string.Equals(src, dest, StringComparison.Ordinal)) continue;
                if (File.Exists(dest) && !string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
                EnsureDir(newDir, journal);
                FileOps.Rename(src, dest);
                journal.Ops.Add(new List<object> { "move", src, dest });
            }
        }

        // Bild eines Staffelordners im Preview-Ordner der Serie: Preview\Staffel 1.jpg -> Staffel 01.jpg
        static void MoveFolderPreview(string seriesDir, string oldName, string newName, Journal journal)
        {
            string dir = Path.Combine(seriesDir, "Preview");
            foreach (string ext in PreviewExtensions)
            {
                string src = Path.Combine(dir, oldName + ext);
                string dest = Path.Combine(dir, newName + ext);
                if (!File.Exists(src) || (File.Exists(dest) && !string.Equals(src, dest, StringComparison.OrdinalIgnoreCase))) continue;
                FileOps.Rename(src, dest);
                journal.Ops.Add(new List<object> { "move", src, dest });
            }
        }

        // Inhalt eines Ordners in einen anderen verschieben (Unterordner wie Preview werden
        // zusammengeführt). Der leere Ordner wird danach entfernt.
        static int MergeFolder(TaskContext ctx, string source, string target, string seriesDir, Journal journal, ref bool? remembered)
        {
            int moved = 0;
            EnsureDir(target, journal);
            foreach (string file in Directory.GetFiles(source))
            {
                ctx.Check();
                string dest = Path.Combine(target, Path.GetFileName(file));
                if (File.Exists(dest))
                {
                    if (!AskReplace(ctx, file, dest, ref remembered)) { ctx.Log("  ÜBERSPRUNGEN  " + Path.GetFileName(file) + " (in „" + Path.GetFileName(target) + "“ vorhanden)"); continue; }
                    SetAside(dest, seriesDir, journal);
                }
                FileOps.Rename(file, dest);
                journal.Move(file, dest);
                moved++;
            }
            foreach (string sub in Directory.GetDirectories(source))
            {
                string dest = Path.Combine(target, Path.GetFileName(sub));
                if (!Directory.Exists(dest))
                {
                    FileOps.Rename(sub, dest);
                    journal.Move(sub, dest);
                }
                else moved += MergeFolder(ctx, sub, dest, seriesDir, journal, ref remembered);
            }
            if (Directory.GetFileSystemEntries(source).Length == 0)
            {
                Directory.Delete(source);
                journal.Ops.Add(new List<object> { "rmdir", source });
            }
            return moved;
        }

        static void SaveJournal(string seriesDir, Journal journal)
        {
            if (journal.Ops.Count == 0) return;
            JsonObject o = new JsonObject();
            o["series"] = seriesDir;
            o["created"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            o["ops"] = journal.Ops;
            try { File.WriteAllText(UndoFile, Json.Write(o, 1), FileOps.Utf8NoBom); }
            catch { }
        }

        // ------------------------------------------------------------------
        // Rückgängig
        // ------------------------------------------------------------------

        // Beschreibung des letzten Laufs für die Oberfläche; null, wenn es keinen gibt
        public static string UndoInfo(out string seriesDir)
        {
            seriesDir = null;
            JsonObject o = LoadJournal();
            if (o == null) return null;
            seriesDir = o.Str("series");
            DateTime created;
            string when = DateTime.TryParse(o.Str("created") ?? "", CultureInfo.InvariantCulture, DateTimeStyles.None, out created)
                ? created.ToString("dd.MM.yyyy HH:mm") : "";
            List<object> ops = o.Arr("ops");
            return "Letzter Lauf: " + Path.GetFileName((seriesDir ?? "").TrimEnd('\\')) + (when.Length > 0 ? ", " + when : "") +
                   " (" + (ops != null ? ops.Count : 0) + " Schritte)";
        }

        static JsonObject LoadJournal()
        {
            try
            {
                if (!File.Exists(UndoFile)) return null;
                return Json.Parse(File.ReadAllText(UndoFile, Encoding.UTF8)) as JsonObject;
            }
            catch { return null; }
        }

        public static void Undo(TaskContext ctx)
        {
            JsonObject o = LoadJournal();
            List<object> ops = o != null ? o.Arr("ops") : null;
            if (ops == null || ops.Count == 0)
            {
                ctx.Log("Es gibt nichts rückgängig zu machen.");
                ctx.Summary = "nichts rückgängig zu machen";
                return;
            }
            ctx.Log("Mache das letzte Umbenennen rückgängig: " + o.Str("series"));
            List<object> pairs = new List<object>();
            int done = 0, failed = 0;
            for (int i = ops.Count - 1; i >= 0; i--)
            {
                ctx.Check();
                ctx.Progress((double)(ops.Count - 1 - i) / ops.Count, "Rückgängig " + (ops.Count - i) + " / " + ops.Count);
                List<object> op = ops[i] as List<object>;
                if (op == null || op.Count < 2) continue;
                string kind = op[0] as string;
                string a = op[1] as string;
                string b = op.Count > 2 ? op[2] as string : null;
                try
                {
                    if (kind == "move" && b != null)
                    {
                        if (!FileOps.Exists(b)) { ctx.Log("  FEHLT   " + b + " (übersprungen)"); failed++; continue; }
                        if (FileOps.Exists(a) && !string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                        {
                            ctx.Log("  FEHLER  " + a + " gibt es schon, " + Path.GetFileName(b) + " bleibt");
                            failed++;
                            continue;
                        }
                        string parent = Path.GetDirectoryName(a);
                        if (parent != null) Directory.CreateDirectory(parent);
                        FileOps.Rename(b, a);
                        string ua = UrlOf(a), ub = UrlOf(b);
                        if (ua != null && ub != null) pairs.Add(new List<object> { ub, ua });
                        done++;
                    }
                    else if (kind == "mkdir")
                    {
                        if (Directory.Exists(a) && Directory.GetFileSystemEntries(a).Length == 0) Directory.Delete(a);
                    }
                    else if (kind == "rmdir")
                    {
                        Directory.CreateDirectory(a);
                    }
                }
                catch (Exception e)
                {
                    failed++;
                    ctx.Log("  FEHLER  " + (b ?? a) + " :: " + e.Message);
                }
            }
            if (pairs.Count > 0)
            {
                try { DataTask.AppendRenamedPairs(pairs); }
                catch (Exception e) { ctx.Log("Hinweis: renamed.js konnte nicht ergänzt werden (" + e.Message + ")"); }
            }
            // Erledigt: Protokoll entfernen, damit nicht zweimal rückgängig gemacht wird
            if (failed == 0) FileOps.TryDelete(UndoFile);
            else
            {
                try { FileOps.Replace(UndoFile, UndoFile + ".alt"); } catch { }
            }
            ctx.Log("Zurückgesetzt: " + done + "   Fehler: " + failed);
            ctx.Summary = done + " zurückgesetzt, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Schritt(e) konnten nicht rückgängig gemacht werden");
        }
    }
}
