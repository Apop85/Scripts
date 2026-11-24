// Tool "Duplikate finden" (seit 3.3):
//   - identische Dateien: gleiche Grösse und gleiche SHA256-Prüfsumme
//   - gleiche Folge: dieselbe Staffel und Folge innerhalb einer Serie (S01E01 = 1x01 = S1E1 = S01E1),
//     erkannt mit EpisodeTask.ParseEpisodes. Serie = Ordner direkt unter der Kategorie
//     (z.B. Serien\Meine Serie); verglichen wird nie über verschiedene Serien hinweg. Gleich benannte
//     Serienordner an verschiedenen Orten (Kopien) gelten als dieselbe Serie, ohne Vorauswahl.
// Im Dialog sind die auszusortierenden Dateien angehakt (Vorauswahl: höhere Auflösung bzw. grössere
// Datei bleibt, bei identischen der kürzeste Pfad; Doppelfolgen-Überschneidungen ohne Vorauswahl).
// Sie kommen samt Vorschaubild nach "__duplikate" neben der Datei. renamed.js führt Favoriten und
// Fortschritt zur behaltenen Datei. Ohne Oberfläche wird nur protokolliert.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public class DupeGroup
    {
        public string Title;
        public bool Identical;
        public bool SameName;                                 // gleicher Dateiname (ohne Endung)
        public bool Overlap;                                  // Doppelfolge überschneidet Einzelfolge
        public bool OtherFolders;                             // gleiche Folge aus gleich benannten Serienordnern an verschiedenen Orten
        public List<string> Files = new List<string>();
        public List<MediaFacts> Facts = new List<MediaFacts>();
        public HashSet<string> Remove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // Vorauswahl
    }

    public class DupeChoice
    {
        public List<string> Remove = new List<string>();
        public bool ConvertKept;
    }

    public static class DuplicateTask
    {
        public const string FolderName = "__duplikate";

        public static void Run(TaskContext ctx, string folder)
        {
            if (!Directory.Exists(folder)) throw new TaskFailedException("Ordner nicht gefunden: " + folder);
            ctx.Ffmpeg.EnsureInstalled(ctx);

            HashSet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HashSet<string> set in new HashSet<string>[] { MediaTypes.ReadyVideo, MediaTypes.ReadyAudio, MediaTypes.ReadyImage,
                                                                    MediaTypes.ConvertVideo, MediaTypes.ConvertAudio, MediaTypes.ConvertImage })
                extensions.UnionWith(set);
            ctx.Progress(-1, "Suche Medien ...");
            // Dauer je Schritt fürs Protokoll (Vergleich am Netzlaufwerk: welcher Schritt bremst)
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
            MediaCache.TakeStats();
            // Grösse und Änderungszeit aus der Auflistung (kein FileInfo je Datei)
            List<FileInfo> infos = new List<FileInfo>();
            Folders.CollectFiles(folder, extensions, true, infos, ctx.Token);
            infos.RemoveAll(delegate(FileInfo f) { string n = f.Name; return n.StartsWith("._") || n.Contains(".__temp__"); });
            List<string> files = new List<string>();
            Dictionary<string, FileInfo> infoOf = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (FileInfo f in infos) { files.Add(f.FullName); infoOf[f.FullName] = f; }
            ctx.Log(files.Count + " Mediendateien gefunden in " + folder);
            double tList = clock.Elapsed.TotalSeconds;

            List<DupeGroup> groups = new List<DupeGroup>();
            Dictionary<string, int> inIdentical = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);   // Datei -> Gruppe
            double tQuick, tFull;
            FindIdentical(ctx, files, infoOf, groups, inIdentical, clock, out tQuick, out tFull);
            FindEpisodes(ctx, folder, files, groups, inIdentical);
            FindSameName(folder, files, groups, inIdentical);
            double tGroups = clock.Elapsed.TotalSeconds;

            // Angaben für den Vergleich und die Vorauswahl: parallel untersuchen (gemerkte Ergebnisse
            // aus MediaCache brauchen kein ffprobe), danach in fester Reihenfolge den Gruppen zuordnen
            List<string> toProbe = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in groups) foreach (string f in g.Files) if (seen.Add(f)) toProbe.Add(f);
            MediaFacts[] probed = new MediaFacts[toProbe.Count];
            int probedCount = 0;
            Workers.ForEach(toProbe, Workers.Degree(ctx, 4), ctx.Token, delegate(int i, string f)
            {
                FileInfo known;
                MediaFacts facts = infoOf.TryGetValue(f, out known) ? MediaFacts.Basic(known) : MediaFacts.Basic(f);
                facts.Probe(ctx.Ffmpeg, ctx.Token);
                probed[i] = facts;
                int n = System.Threading.Interlocked.Increment(ref probedCount);
                if (n % 5 == 0 || n == toProbe.Count) ctx.Progress((double)n / Math.Max(1, toProbe.Count), "Untersuche " + n + " / " + toProbe.Count);
            });
            MediaCache.Save();
            double tProbe = clock.Elapsed.TotalSeconds;
            ctx.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Dauer: Auflistung {0:0.0} s, Schnelltest {1:0.0} s, Prüfsummen {2:0.0} s, Gruppen {3:0.0} s, Untersuchen {4:0.0} s ({5} Dateien), gesamt {6:0.0} s",
                    tList, tQuick - tList, tFull - tQuick, tGroups - tFull, tProbe - tGroups, toProbe.Count, tProbe));
            string stats = MediaCache.TakeStats();
            if (stats != null) ctx.Log(stats);
            Dictionary<string, MediaFacts> factsOf = new Dictionary<string, MediaFacts>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < toProbe.Count; i++) factsOf[toProbe[i]] = probed[i];
            foreach (DupeGroup g in groups)
            {
                foreach (string f in g.Files) g.Facts.Add(factsOf[f]);
                Preselect(g);
            }

            int identical = 0, episodes = 0, names = 0, logged = 0;
            // Mit Oberfläche steht alles in der Auswahl: im Protokoll nur die ersten Gruppen
            int maxLogged = ctx.ChooseDuplicates != null ? 50 : int.MaxValue;
            foreach (DupeGroup g in groups)
            {
                if (g.Identical) identical++; else if (g.SameName) names++; else episodes++;
                if (logged++ >= maxLogged) continue;
                ctx.Log("  " + g.Title);
                for (int i = 0; i < g.Files.Count; i++)
                    ctx.Log("     " + (g.Remove.Contains(g.Files[i]) ? "aussortieren  " : "behalten      ") + g.Files[i] +
                            "  (" + Fmt.Size(g.Facts[i].Size) + ", " + g.Facts[i].ResolutionText + ")");
            }
            if (groups.Count > maxLogged) ctx.Log("  … und " + (groups.Count - maxLogged) + " weitere Gruppen (alle stehen in der Auswahl)");
            ctx.Log("Gruppen: " + identical + " mit identischen Dateien, " + episodes + " mit gleicher Folge, " + names + " mit gleichem Namen");
            ctx.Summary = identical + " identisch, " + episodes + " gleiche Folge, " + names + " gleicher Name";

            // Doppelte Ordner (DEC-379): Paare mit mindestens 3 gemeinsamen Gruppen
            List<DupeFolderPair> folderPairs = DupeFolders.Find(folder, files, groups);
            if (folderPairs.Count > 0)
            {
                ctx.Log("Doppelte Ordner: " + folderPairs.Count);
                foreach (DupeFolderPair p in folderPairs)
                    ctx.Log("  " + DupeFolders.Display(p.Left) + " (" + p.Percent(true) + " %)  <->  " + DupeFolders.Display(p.Right) + " (" + p.Percent(false) + " %), " +
                            p.Identical.Count + " identisch" + (p.Similar.Count > 0 ? ", " + p.Similar.Count + " gleiche Folge oder gleicher Name" : ""));
            }
            if (groups.Count == 0 || ctx.ChooseDuplicates == null) return;

            DupeChoice choice = ctx.ChooseDuplicates(groups, folderPairs);
            if (choice == null) { ctx.Log("Keine Dateien aussortiert."); return; }

            // Aussortieren
            List<object> pairs = new List<object>();
            int moved = 0, failed = 0;
            HashSet<string> kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Eine Datei kann in zwei Gruppen stehen (identisch und gleiche Folge): nur einmal verschieben
            HashSet<string> movedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in groups)
            {
                string keep = null;
                foreach (string f in g.Files) if (!choice.Remove.Contains(f)) { kept.Add(f); if (keep == null) keep = f; }
                foreach (string f in g.Files)
                {
                    if (!choice.Remove.Contains(f) || !movedFiles.Add(f)) continue;
                    ctx.Check();
                    try
                    {
                        MoveAside(f);
                        moved++;
                        ctx.Log("  AUSSORTIERT  " + f + "  ->  " + FolderName);
                        // Favoriten, Fortschritt und "Weiterschauen" gehen auf die behaltene Datei über
                        string from = UrlOf(f), to = keep != null ? UrlOf(keep) : null;
                        if (from != null && to != null) pairs.Add(new List<object> { from, to });
                    }
                    catch (Exception e)
                    {
                        failed++;
                        ctx.Log("  FEHLER  " + f + " :: " + e.Message);
                    }
                }
            }
            if (pairs.Count > 0)
            {
                try { DataTask.AppendRenamedPairs(pairs); }
                catch (Exception e) { ctx.Log("Hinweis: renamed.js konnte nicht ergänzt werden (" + e.Message + ")"); }
            }
            ctx.Log("Aussortiert: " + moved + "   Fehler: " + failed);
            ctx.Summary = moved + " aussortiert, " + failed + " Fehler";

            // Behaltene Videos, die der Browser nicht abspielen kann, gleich konvertieren
            if (choice.ConvertKept)
            {
                HashSet<string> convert = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (DupeGroup g in groups)
                    for (int i = 0; i < g.Files.Count; i++)
                        if (kept.Contains(g.Files[i]) && g.Facts[i].Playable == false &&
                            MediaTypes.Kind(Path.GetExtension(g.Files[i]).ToLowerInvariant()) == "video")
                            convert.Add(g.Files[i]);
                if (convert.Count > 0)
                {
                    ctx.Log("");
                    ctx.Log("Konvertiere " + convert.Count + " behaltene(s) Video(s) ...");
                    ConvertOptions opt = new ConvertOptions();
                    opt.Folder = folder;
                    opt.OnlyFiles = convert;
                    if (ctx.Settings != null)
                    {
                        opt.Parallel = ctx.Settings.GetInt("convert.parallel", 0);
                        opt.DeleteOriginal = ctx.Settings.GetBool("convert.deleteOriginal", false);
                    }
                    ConvertTask.Run(ctx, opt);
                }
            }
            if (failed > 0) throw new TaskFailedException(failed + " Datei(en) konnten nicht aussortiert werden");
        }

        static string UrlOf(string path)
        {
            string rel = DataTask.ToRelative(path);
            return string.IsNullOrEmpty(rel) ? null : "./" + rel.Replace('\\', '/');
        }

        // Datei und Vorschaubild nach <Ordner>\__duplikate (bzw. __duplikate\Preview)
        static void MoveAside(string file)
        {
            string storage = Path.Combine(Path.GetDirectoryName(file), FolderName);
            string dest = FileOps.MoveIntoFolder(file, storage);
            // Vorschaubild unter dem Namen in der Ablage (z.B. "Film (2).jpg" zu "Film (2).mp4")
            FileOps.MovePreviewsInto(file, Path.Combine(storage, "Preview"), Path.GetFileNameWithoutExtension(dest));
        }

        // Gleiche Grösse, dann gleiche Prüfsumme
        const int QuickBlock = 1024 * 1024;

        // SHA256 über je 1 MB vom Anfang, aus der Mitte und vom Ende (kleine Dateien: die ganze Datei)
        static string QuickFingerprint(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024))
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                long size = fs.Length;
                if (size <= QuickBlock * 3) return FileOps.Hex(sha.ComputeHash(fs));
                byte[] buf = new byte[QuickBlock];
                foreach (long offset in new long[] { 0, size / 2 - QuickBlock / 2, size - QuickBlock })
                {
                    fs.Position = offset;
                    int read = 0, n;
                    while (read < buf.Length && (n = fs.Read(buf, read, buf.Length - read)) > 0) read += n;
                    sha.TransformBlock(buf, 0, read, null, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return FileOps.Hex(sha.Hash);
            }
        }

        static void FindIdentical(TaskContext ctx, List<string> files, Dictionary<string, FileInfo> infoOf, List<DupeGroup> groups, Dictionary<string, int> inIdentical,
                                  System.Diagnostics.Stopwatch clock, out double tQuick, out double tFull)
        {
            Dictionary<long, List<string>> bySize = new Dictionary<long, List<string>>();
            foreach (string f in files)
            {
                long size;
                FileInfo info;
                if (!infoOf.TryGetValue(f, out info)) continue;
                try { size = info.Length; } catch { continue; }
                if (size <= 0) continue;
                List<string> l;
                if (!bySize.TryGetValue(size, out l)) { l = new List<string>(); bySize[size] = l; }
                l.Add(f);
            }
            List<List<string>> candidates = new List<List<string>>();
            foreach (List<string> l in bySize.Values) if (l.Count > 1) candidates.Add(l);

            // 1. Schnelltest (seit 3.4.2): je 1 MB vom Anfang, aus der Mitte und vom Ende, parallel.
            //    Dateien gleicher Grösse unterscheiden sich fast immer schon dort.
            List<string> quickFiles = new List<string>();
            foreach (List<string> l in candidates) quickFiles.AddRange(l);
            string[] quick = new string[quickFiles.Count];
            int quickDone = 0;
            Workers.ForEach(quickFiles, Workers.Degree(ctx, 4), ctx.Token, delegate(int i, string f)
            {
                try { quick[i] = QuickFingerprint(f); }
                catch (Exception e) { ctx.Log("Hinweis: " + f + " nicht lesbar (" + e.Message + ")"); }
                int n = System.Threading.Interlocked.Increment(ref quickDone);
                if (n % 5 == 0 || n == quickFiles.Count) ctx.Progress((double)n / Math.Max(1, quickFiles.Count), "Schnelltest " + n + " / " + quickFiles.Count);
            });
            Dictionary<string, string> quickOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < quickFiles.Count; i++) if (quick[i] != null) quickOf[quickFiles[i]] = quick[i];
            tQuick = clock.Elapsed.TotalSeconds;

            // 2. Volle Prüfsumme nur bei gleichem Schnelltest, eine Datei nach der anderen (liest ganze Dateien)
            List<List<string>> fullCandidates = new List<List<string>>();
            foreach (List<string> l in candidates)
            {
                Dictionary<string, List<string>> byQuick = new Dictionary<string, List<string>>();
                foreach (string f in l)
                {
                    string q;
                    if (!quickOf.TryGetValue(f, out q)) continue;
                    List<string> same;
                    if (!byQuick.TryGetValue(q, out same)) { same = new List<string>(); byQuick[q] = same; }
                    same.Add(f);
                }
                foreach (List<string> same in byQuick.Values) if (same.Count > 1) fullCandidates.Add(same);
            }
            int done = 0, total = 0;
            foreach (List<string> l in fullCandidates) total += l.Count;
            foreach (List<string> l in fullCandidates)
            {
                Dictionary<string, List<string>> byHash = new Dictionary<string, List<string>>();
                foreach (string f in l)
                {
                    ctx.Check();
                    ctx.Progress((double)done++ / Math.Max(1, total), "Prüfsumme " + done + " / " + total + ": " + Path.GetFileName(f));
                    string hash;
                    // Kleine Dateien hat der Schnelltest schon ganz gelesen
                    if (infoOf[f].Length <= QuickBlock * 3) hash = quickOf[f];
                    else
                    {
                        try { hash = FileOps.Sha256File(f); } catch (Exception e) { ctx.Log("Hinweis: " + f + " nicht lesbar (" + e.Message + ")"); continue; }
                    }
                    List<string> same;
                    if (!byHash.TryGetValue(hash, out same)) { same = new List<string>(); byHash[hash] = same; }
                    same.Add(f);
                }
                foreach (List<string> same in byHash.Values)
                {
                    if (same.Count < 2) continue;
                    DupeGroup g = new DupeGroup();
                    g.Identical = true;
                    g.Title = "Identisch: " + Path.GetFileName(same[0]) + (same.Count > 2 ? " (" + same.Count + " Dateien)" : "");
                    g.Files.AddRange(same);
                    foreach (string f in same) inIdentical[f] = groups.Count;
                    groups.Add(g);
                }
            }
            tFull = clock.Elapsed.TotalSeconds;
        }

        // Serie eines Videos, vom Staffelordner aus bestimmt: Liegt die Folge in einem Staffelordner
        // ("Staffel 1", "Season 01 HD" …), ist die Serie der Ordner darüber, sonst der Ordner der
        // Folge selbst. So bleiben Serien in Unterordnern (Serien\Star Trek\TOS\Staffel 1 und
        // Serien\Star Trek\TNG\Staffel 1) getrennt. null = keine Serie (Datei direkt im
        // Installationsordner oder in einer Kategorie: dort liegen verschiedene Serien lose).
        static string SeriesKey(string root, string file)
        {
            string dir = Path.GetDirectoryName(file).TrimEnd('\\');
            int season;
            string series = EpisodeTask.IsSeasonFolder(Path.GetFileName(dir), out season) ? Path.GetDirectoryName(dir) : dir;
            if (string.IsNullOrEmpty(series)) return null;
            string rel = DataTask.ToRelative(series);
            if (rel != null && rel.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries).Length < 2) return null;
            return series.TrimEnd('\\').ToLowerInvariant();
        }

        static void FindEpisodes(TaskContext ctx, string root, List<string> files, List<DupeGroup> groups, Dictionary<string, int> inIdentical)
        {
            // (Serie, Staffel, Folge) -> Dateien; danach zusammenhängende Dateien zu Gruppen verbinden
            Dictionary<string, List<string>> byEpisode = new Dictionary<string, List<string>>();
            Dictionary<string, List<int>> episodesOf = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> seasonOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> seriesOf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                if (MediaTypes.Kind(Path.GetExtension(f).ToLowerInvariant()) != "video") continue;
                string seriesPath = SeriesKey(root, f);
                if (seriesPath == null) continue;
                // Verglichen wird über den Namen des Serienordners: auch Kopien an anderem Ort
                // (Serien\Polarstern und Serien\Kopie\Polarstern) gelten als dieselbe Serie (SFLIX-20261124-119).
                // Verschiedene Serien heissen verschieden (Star Trek\TOS und Star Trek\TNG bleiben getrennt).
                string series = Path.GetFileName(seriesPath);
                seriesOf[f] = seriesPath;
                int season;
                List<int> eps;
                if (!EpisodeTask.ParseEpisodes(Path.GetFileName(f), out season, out eps) || eps == null || eps.Count == 0) continue;
                episodesOf[f] = eps;
                seasonOf[f] = season;
                foreach (int e in eps)
                {
                    string key = series + "|" + season + "|" + e;
                    List<string> l;
                    if (!byEpisode.TryGetValue(key, out l)) { l = new List<string>(); byEpisode[key] = l; }
                    l.Add(f);
                }
            }
            // Union-Find über gemeinsame Folgen
            Dictionary<string, string> parent = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Func<string, string> find = null;
            find = delegate(string x)
            {
                string p;
                if (!parent.TryGetValue(x, out p) || string.Equals(p, x, StringComparison.OrdinalIgnoreCase)) return x;
                string r = find(p);
                parent[x] = r;
                return r;
            };
            foreach (List<string> l in byEpisode.Values)
            {
                if (l.Count < 2) continue;
                foreach (string f in l) if (!parent.ContainsKey(f)) parent[f] = f;
                string first = find(l[0]);
                for (int i = 1; i < l.Count; i++)
                {
                    string r = find(l[i]);
                    if (!string.Equals(r, first, StringComparison.OrdinalIgnoreCase)) parent[r] = first;
                }
            }
            Dictionary<string, List<string>> clusters = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            // find() verkürzt die Wege in parent, darum über eine Kopie der Schlüssel laufen
            foreach (string f in new List<string>(parent.Keys))
            {
                string r = find(f);
                List<string> c;
                if (!clusters.TryGetValue(r, out c)) { c = new List<string>(); clusters[r] = c; }
                c.Add(f);
            }
            foreach (List<string> c in clusters.Values)
            {
                if (c.Count < 2) continue;
                // Alle Dateien in derselben Gruppe "identisch": schon oben gemeldet
                int firstGroup;
                bool sameIdentical = inIdentical.TryGetValue(c[0], out firstGroup);
                foreach (string f in c)
                {
                    int g2;
                    if (!inIdentical.TryGetValue(f, out g2) || g2 != firstGroup) sameIdentical = false;
                }
                if (sameIdentical) continue;
                c.Sort(StringComparer.OrdinalIgnoreCase);
                DupeGroup g = new DupeGroup();
                string firstSet = string.Join(",", episodesOf[c[0]].ConvertAll(delegate(int e) { return e.ToString(); }).ToArray());
                foreach (string f in c)
                {
                    string set = string.Join(",", episodesOf[f].ConvertAll(delegate(int e) { return e.ToString(); }).ToArray());
                    if (set != firstSet || seasonOf[f] != seasonOf[c[0]]) g.Overlap = true;
                    // Gleich benannte Serienordner an verschiedenen Orten: keine Vorauswahl (könnten verschiedene Serien sein)
                    if (!string.Equals(seriesOf[f], seriesOf[c[0]], StringComparison.OrdinalIgnoreCase)) g.OtherFolders = true;
                }
                List<int> all = new List<int>();
                foreach (string f in c) foreach (int e in episodesOf[f]) if (!all.Contains(e)) all.Add(e);
                all.Sort();
                g.Title = "Gleiche Folge " + EpisodeTask.EpisodeLabel(seasonOf[c[0]], all) + ": " +
                          Path.GetFileName(Path.GetDirectoryName(c[0])) + (g.Overlap ? " (Doppelfolge, bitte selbst wählen)" :
                          g.OtherFolders ? " (Serienordner an verschiedenen Orten, bitte selbst wählen)" : "");
                g.Files.AddRange(c);
                groups.Add(g);
            }
        }

        // Gleicher Dateiname ohne Endung (gross/klein egal), z.B. "Film.mp4" in zwei Ordnern oder
        // "Film.mp4" neben "Film.mkv". Preview-, Ablage-, System- und versteckte Ordner sind schon
        // beim Sammeln ausgeschlossen. Keine Gruppe, die schon als identisch gemeldet ist oder
        // genau dieselben Dateien wie eine andere Gruppe hat.
        static void FindSameName(string root, List<string> files, List<DupeGroup> groups, Dictionary<string, int> inIdentical)
        {
            Dictionary<string, List<string>> byName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> displayName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                string name = Path.GetFileNameWithoutExtension(f).Trim();
                if (name.Length == 0) continue;
                // Folgen ("S01E01.mp4") heissen in jeder Serie gleich: nur innerhalb derselben Serie vergleichen
                string key = name;
                int season;
                List<int> eps;
                if (EpisodeTask.ParseEpisodes(Path.GetFileName(f), out season, out eps) && eps != null && eps.Count > 0)
                    key = name + "|" + (SeriesKey(root, f) ?? Path.GetDirectoryName(f).ToLowerInvariant());
                List<string> l;
                if (!byName.TryGetValue(key, out l)) { l = new List<string>(); byName[key] = l; displayName[key] = name; }
                l.Add(f);
            }
            foreach (KeyValuePair<string, List<string>> kv in byName)
            {
                List<string> c = kv.Value;
                if (c.Count < 2) continue;
                int firstGroup;
                bool sameIdentical = inIdentical.TryGetValue(c[0], out firstGroup);
                foreach (string f in c)
                {
                    int g2;
                    if (!inIdentical.TryGetValue(f, out g2) || g2 != firstGroup) sameIdentical = false;
                }
                if (sameIdentical) continue;
                c.Sort(StringComparer.OrdinalIgnoreCase);
                bool known = false;
                foreach (DupeGroup other in groups)
                {
                    if (other.Files.Count != c.Count) continue;
                    bool same = true;
                    foreach (string f in c) if (!other.Files.Exists(delegate(string o) { return string.Equals(o, f, StringComparison.OrdinalIgnoreCase); })) same = false;
                    if (same) known = true;
                }
                if (known) continue;
                DupeGroup g = new DupeGroup();
                g.SameName = true;
                g.Title = "Gleicher Name: " + displayName[kv.Key] + (c.Count > 2 ? " (" + c.Count + " Dateien)" : "");
                g.Files.AddRange(c);
                groups.Add(g);
            }
        }

        static void Preselect(DupeGroup g)
        {
            g.Remove.Clear();
            if (g.Overlap || g.OtherFolders || g.Files.Count < 2) return;
            int best = 0;
            for (int i = 1; i < g.Files.Count; i++)
            {
                MediaFacts a = g.Facts[i], b = g.Facts[best];
                bool better;
                if (g.Identical)
                {
                    // Abspielbare Endung vor nicht abspielbarer (gleicher Inhalt als .mkv), dann
                    // kürzester Pfad, bei Gleichstand die ältere Datei
                    bool pa = a.Playable != false, pb = b.Playable != false;
                    better = (pa && !pb) || (pa == pb && (g.Files[i].Length < g.Files[best].Length ||
                             (g.Files[i].Length == g.Files[best].Length && a.Modified < b.Modified)));
                }
                else
                {
                    // Abspielbare vor nicht abspielbaren, dann höhere Auflösung, dann grössere Datei
                    bool pa = a.Playable != false, pb = b.Playable != false;
                    better = (pa && !pb) || (pa == pb && (a.Pixels > b.Pixels || (a.Pixels == b.Pixels && a.Size > b.Size)));
                }
                if (better) best = i;
            }
            for (int i = 0; i < g.Files.Count; i++) if (i != best) g.Remove.Add(g.Files[i]);
        }
    }

    // Auswahl der Duplikate: angehakt = aussortieren
    public class DuplicateDialog : Form
    {
        readonly CompareView view;
        readonly DarkCheckBox convertKept;
        readonly Label info;
        readonly List<DupeGroup> groups;
        DupeChoice result;
        bool syncing;

        public DuplicateDialog(List<DupeGroup> groups, List<DupeFolderPair> folderPairs, FfmpegTools ff)
        {
            this.groups = groups;
            bool withFolders = folderPairs != null && folderPairs.Count > 0;
            Text = "Duplikate finden – Auswahl";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1180), Theme.S(withFolders ? 820 : 640));
            MinimumSize = new Size(Theme.S(760), Theme.S(420));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(groups.Count + " Gruppe(n) gefunden. Angehakte Dateien kommen samt Vorschaubild nach „" + DuplicateTask.FolderName +
                                          "“ („Ablagen leeren“ löscht sie später). Vorausgewählt ist die Datei mit höherer Auflösung bzw. die grössere, " +
                                          "bessere Werte sind grün. Bei Doppelfolgen bitte selbst wählen." +
                                          (withFolders ? " Liegen ganze Ordner doppelt vor, wählst du oben je Ordnerpaar, welche Seite bleibt; " +
                                                         "aussortiert werden nur die doppelten Dateien." : "") +
                                          " Rechtsklick auf eine Datei: alles aus ihrem Ordner aussortieren oder behalten.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));

            view = new CompareView(ff, true, true);
            view.CheckBoxes = true;
            view.Dock = DockStyle.Fill;
            view.GroupTitles = new List<string>();
            List<List<CompareEntry>> entries = new List<List<CompareEntry>>();
            // Eine Datei, die in irgendeiner Gruppe behalten wird, ist überall "behalten"
            HashSet<string> keptAnywhere = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in groups) foreach (string f in g.Files) if (!g.Remove.Contains(f)) keptAnywhere.Add(f);
            foreach (DupeGroup g in groups) g.Remove.RemoveWhere(delegate(string f) { return keptAnywhere.Contains(f); });
            // Vorauswahl für "einzeln entscheiden"
            foreach (DupeGroup g in groups) initialRemove.UnionWith(g.Remove);
            foreach (DupeGroup g in groups)
            {
                view.GroupTitles.Add(g.Title);
                List<CompareEntry> list = new List<CompareEntry>();
                for (int i = 0; i < g.Files.Count; i++)
                {
                    CompareEntry e = new CompareEntry();
                    e.Path = g.Files[i];
                    e.Facts = g.Facts[i];
                    e.Label = g.Remove.Contains(e.Path) ? "aussortieren" : "behalten";
                    // Grund der Gruppe: gleicher Inhalt, gleiche Folge oder gleicher Dateiname
                    e.Reason = g.Identical ? "Datei" : g.SameName ? "Dateiname" : "Folge";
                    e.Note = DataTask.ToRelative(Path.GetDirectoryName(e.Path)) ?? Path.GetDirectoryName(e.Path);
                    e.Tag = g.Remove.Contains(e.Path);
                    list.Add(e);
                }
                entries.Add(list);
            }
            view.Rendered = delegate { IndexRows(); UpdateInfo(); };
            view.SetGroups(entries);
            view.List.ItemChecked += delegate(object s, ItemCheckedEventArgs e)
            {
                // Während die Tabelle neu aufgebaut oder erstmals angezeigt wird, meldet die Liste jedes
                // Häkchen einzeln: das sind keine Änderungen des Benutzers
                if (!ready || view.Rendering || e.Item == null) return;
                CompareEntry ce = e.Item.Tag as CompareEntry;
                if (ce == null) return;
                ce.Label = e.Item.Checked ? "aussortieren" : "behalten";
                ce.Tag = e.Item.Checked;
                if (e.Item.Text != ce.Label) e.Item.Text = ce.Label;
                // Dieselbe Datei in anderen Gruppen gleich anhaken (sonst behalten und aussortieren zugleich)
                List<ListViewItem> same;
                if (!syncing && rowsByPath.TryGetValue(ce.Path, out same) && same.Count > 1)
                {
                    syncing = true;
                    try
                    {
                        foreach (ListViewItem other in same)
                            if (other != e.Item && other.Checked != e.Item.Checked) other.Checked = e.Item.Checked;
                    }
                    finally { syncing = false; }
                }
                if (!bulk) UpdateInfo();
            };

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            host.Controls.Add(view);

            convertKept = new DarkCheckBox();
            convertKept.Text = "Behaltene Videos, die der Browser nicht abspielen kann, danach konvertieren";
            convertKept.ForeColor = Theme.Text;
            convertKept.Switch = true;
            convertKept.Checked = true;
            convertKept.FitText();
            convertKept.Margin = new Padding(0, Theme.S(8), Theme.S(16), 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button cancel = Theme.MakeButton("Abbrechen", false);
            cancel.Click += delegate { result = null; DialogResult = DialogResult.Cancel; };
            Button ok = Theme.MakeButton("Aussortieren", true);
            ok.Click += delegate { Finish(); };
            Button none = Theme.MakeButton("Keine", false);
            none.Click += delegate
            {
                bulk = true;
                view.List.BeginUpdate();
                try { foreach (ListViewItem it in view.List.Items) if (it.Checked) it.Checked = false; }
                finally { view.List.EndUpdate(); bulk = false; }
                UpdateInfo();
            };
            info = Theme.MakeLabel("", true);
            info.Margin = new Padding(0, Theme.S(8), Theme.S(12), 0);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            buttons.Controls.Add(none);
            buttons.Controls.Add(info);
            buttons.Controls.Add(convertKept);

            Controls.Add(host);
            Controls.Add(buttons);
            if (withFolders) Controls.Add(BuildFolderBlock(folderPairs));
            Controls.Add(intro);
            BuildFolderMenu();
            CancelButton = cancel;
            Shown += delegate { ready = true; UpdateInfo(); };
        }

        readonly HashSet<string> initialRemove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Font boldFont;
        bool fitting;

        // Block "Doppelte Ordner" (DEC-379): je Paar die Ordner mit Anteil doppelter Dateien, Knöpfe
        // "links behalten", "rechts behalten", "einzeln entscheiden", Wahl für gleiche Folgen und die
        // aufklappbaren Unterordner. Höchstens etwa ein Drittel der Höhe, sonst mit Bildlauf.
        Control BuildFolderBlock(List<DupeFolderPair> pairs)
        {
            Panel outer = new Panel();
            outer.Dock = DockStyle.Top;
            outer.Padding = new Padding(Theme.S(12), 0, Theme.S(12), Theme.S(8));
            boldFont = new Font(Font, FontStyle.Bold);
            Disposed += delegate { boldFont.Dispose(); };
            Label title = Theme.MakeLabel("Doppelte Ordner (" + pairs.Count + ")", false);
            title.Font = boldFont;
            title.Dock = DockStyle.Top;
            title.Padding = new Padding(0, 0, 0, Theme.S(4));
            Panel scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            FlowLayoutPanel rows = new FlowLayoutPanel();
            rows.FlowDirection = FlowDirection.TopDown;
            rows.WrapContents = false;
            rows.AutoSize = true;
            rows.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rows.Margin = new Padding(0);
            List<FlowLayoutPanel> rowPanels = new List<FlowLayoutPanel>();
            Action relayout = delegate { FitFolderBlock(outer, title, scroll, rows, rowPanels); };
            foreach (DupeFolderPair pair in pairs)
            {
                FlowLayoutPanel row = BuildPairRow(pair, relayout);
                rowPanels.Add(row);
                rows.Controls.Add(row);
            }
            scroll.Controls.Add(rows);
            outer.Controls.Add(scroll);
            outer.Controls.Add(title);
            scroll.Resize += delegate { relayout(); };
            Load += delegate { relayout(); };
            return outer;
        }

        void FitFolderBlock(Panel outer, Label title, Panel scroll, FlowLayoutPanel rows, List<FlowLayoutPanel> rowPanels)
        {
            if (fitting) return;
            fitting = true;
            try
            {
                int width = Math.Max(Theme.S(300), scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - Theme.S(2));
                foreach (FlowLayoutPanel row in rowPanels)
                {
                    row.MinimumSize = new Size(width, 0);
                    row.MaximumSize = new Size(width, 0);
                    foreach (Control c in row.Controls)
                        if (c is Label) c.MaximumSize = new Size(width - row.Padding.Horizontal, 0);
                }
                int wanted = rows.PreferredSize.Height + title.PreferredHeight + outer.Padding.Vertical;
                outer.Height = Math.Min(wanted, Math.Max(Theme.S(140), ClientSize.Height / 3));
            }
            finally { fitting = false; }
        }

        FlowLayoutPanel BuildPairRow(DupeFolderPair pair, Action relayout)
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.FlowDirection = FlowDirection.TopDown;
            row.WrapContents = false;
            row.AutoSize = true;
            row.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            row.BackColor = Theme.Card;
            row.Padding = new Padding(Theme.S(10), Theme.S(8), Theme.S(10), Theme.S(8));
            row.Margin = new Padding(0, 0, 0, Theme.S(6));

            Label left = Theme.MakeLabel("", false);
            Label right = Theme.MakeLabel("", false);
            Label status = Theme.MakeLabel("", true);
            status.Visible = false;
            status.Margin = new Padding(0, Theme.S(6), 0, 0);
            Action refresh = delegate
            {
                left.Text = "Links: " + DupeFolders.Display(pair.Left) + "  –  " + SideText(pair, true);
                right.Text = "Rechts: " + DupeFolders.Display(pair.Right) + "  –  " + SideText(pair, false);
            };
            refresh();

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.AutoSize = true;
            actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            actions.WrapContents = true;
            actions.Margin = new Padding(0, Theme.S(6), 0, 0);
            Button keepLeft = Theme.MakeButton("links behalten", false);
            Button keepRight = Theme.MakeButton("rechts behalten", false);
            Button single = Theme.MakeButton("einzeln entscheiden", false);
            Action<string> show = delegate(string text)
            {
                status.Text = text;
                status.Visible = text.Length > 0;
                relayout();
            };
            keepLeft.Click += delegate { show(ApplyPair(pair, true)); };
            keepRight.Click += delegate { show(ApplyPair(pair, false)); };
            single.Click += delegate
            {
                Dictionary<string, bool> want = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (DupeGroup g in pair.Groups()) foreach (string f in g.Files) want[f] = initialRemove.Contains(f);
                ApplyChecks(want);
                show("Vorauswahl wiederhergestellt, bitte in der Liste unten je Datei wählen.");
            };
            actions.Controls.Add(keepLeft);
            actions.Controls.Add(keepRight);
            actions.Controls.Add(single);
            Action updateButtons = delegate
            {
                bool any = pair.Groups().Count > 0;
                keepLeft.Enabled = keepRight.Enabled = single.Enabled = any;
            };
            if (pair.Similar.Count > 0)
            {
                DarkCheckBox similar = new DarkCheckBox();
                similar.Text = "Gleiche Folge und gleichen Namen einbeziehen (" + pair.Similar.Count + " Gruppe(n), verschiedene Dateien)";
                similar.ForeColor = Theme.Text;
                similar.Checked = false;
                similar.FitText();
                similar.Margin = new Padding(Theme.S(8), Theme.S(6), 0, 0);
                similar.CheckedChanged += delegate
                {
                    pair.IncludeSimilar = similar.Checked;
                    refresh();
                    updateButtons();
                    relayout();
                };
                actions.Controls.Add(similar);
            }
            updateButtons();

            Label subs = null;
            bool showSubs = pair.SubFolders.Count > 1 || (pair.SubFolders.Count == 1 &&
                !(string.Equals(pair.SubFolders[0].Left, pair.Left, StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(pair.SubFolders[0].Right, pair.Right, StringComparison.OrdinalIgnoreCase)));
            if (showSubs)
            {
                subs = Theme.MakeLabel("", true);
                List<string> lines = new List<string>();
                foreach (DupeSubFolder sf in pair.SubFolders)
                    lines.Add(RelativeTo(sf.Left, pair.Left) + "  ↔  " + RelativeTo(sf.Right, pair.Right) + "  (" + sf.Shared + ")");
                subs.Text = string.Join("\n", lines.ToArray());
                subs.Visible = false;
                subs.Margin = new Padding(Theme.S(8), Theme.S(6), 0, 0);
                Button toggle = Theme.MakeButton("Unterordner (" + pair.SubFolders.Count + ")", false);
                Label target = subs;
                toggle.Click += delegate
                {
                    target.Visible = !target.Visible;
                    toggle.Text = target.Visible ? "Unterordner ausblenden" : "Unterordner (" + pair.SubFolders.Count + ")";
                    relayout();
                };
                actions.Controls.Add(toggle);
            }

            row.Controls.Add(left);
            row.Controls.Add(right);
            row.Controls.Add(actions);
            if (subs != null) row.Controls.Add(subs);
            row.Controls.Add(status);
            return row;
        }

        static string SideText(DupeFolderPair pair, bool left)
        {
            int total = left ? pair.LeftFiles : pair.RightFiles;
            return pair.Dupes(left) + " von " + total + " Datei(en) doppelt (" + pair.Percent(left) + " %)";
        }

        // Unterordner relativ zum Ordner des Paars ("." = der Ordner selbst)
        static string RelativeTo(string folder, string top)
        {
            string f = folder.TrimEnd('\\'), t = top.TrimEnd('\\');
            if (string.Equals(f, t, StringComparison.OrdinalIgnoreCase)) return ".";
            return f.StartsWith(t + "\\", StringComparison.OrdinalIgnoreCase) ? f.Substring(t.Length + 1) : DupeFolders.Display(f);
        }

        string ApplyPair(DupeFolderPair pair, bool keepLeft)
        {
            Dictionary<string, bool> want = pair.Keep(keepLeft);
            ApplyChecks(want);
            int removed = 0;
            foreach (bool r in want.Values) if (r) removed++;
            string text = (keepLeft ? "Links behalten: " : "Rechts behalten: ") + removed + " doppelte Datei(en) " +
                          (keepLeft ? "rechts" : "links") + " zum Aussortieren angehakt.";
            int better = pair.BetterRemoved(keepLeft);
            if (better > 0) text += " Hinweis: In " + better + " Gruppe(n) hat die aussortierte Datei die höhere Auflösung.";
            return text;
        }

        // Häkchen setzen (true = aussortieren), in allen Zeilen der Datei
        void ApplyChecks(Dictionary<string, bool> want)
        {
            bulk = true;
            syncing = true;
            view.List.BeginUpdate();
            try
            {
                foreach (ListViewItem it in view.List.Items)
                {
                    CompareEntry ce = it.Tag as CompareEntry;
                    bool c;
                    if (ce == null || !want.TryGetValue(ce.Path, out c)) continue;
                    if (it.Checked != c) it.Checked = c;
                }
            }
            finally { view.List.EndUpdate(); syncing = false; bulk = false; }
            UpdateInfo();
        }

        // Rechtsklick in der Einzelliste: alles aus dem Ordner der Datei aussortieren oder behalten,
        // in allen Gruppen, in denen der Ordner vorkommt
        void BuildFolderMenu()
        {
            ContextMenuStrip menu = DarkMenu.Create();
            string menuFolder = null;
            ToolStripMenuItem remove = new ToolStripMenuItem("Alles aus diesem Ordner aussortieren");
            ToolStripMenuItem keep = new ToolStripMenuItem("Alles aus diesem Ordner behalten");
            remove.Click += delegate { if (menuFolder != null) FolderChoice(menuFolder, true); };
            keep.Click += delegate { if (menuFolder != null) FolderChoice(menuFolder, false); };
            menu.Items.Add(remove);
            menu.Items.Add(keep);
            Disposed += delegate { menu.Dispose(); };
            view.List.MouseUp += delegate(object s, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right) return;
                ListViewItem hit = view.List.GetItemAt(e.X, e.Y);
                CompareEntry ce = hit != null ? hit.Tag as CompareEntry : null;
                if (ce == null) return;
                menuFolder = Path.GetDirectoryName(ce.Path);
                menu.Show(view.List, e.Location);
            };
            // Auch über die Tastatur: Kontextmenütaste oder Umschalt+F10 an der markierten Zeile (SFLIX-20261124-119)
            view.List.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Apps && !(e.KeyCode == Keys.F10 && e.Shift)) return;
                ListViewItem it = view.List.FocusedItem;
                if (it == null && view.List.SelectedItems.Count > 0) it = view.List.SelectedItems[0];
                CompareEntry ce = it != null ? it.Tag as CompareEntry : null;
                if (ce == null) return;
                e.Handled = true;
                e.SuppressKeyPress = true;
                menuFolder = Path.GetDirectoryName(ce.Path);
                Rectangle r = it.Bounds;
                menu.Show(view.List, new Point(r.Left + Theme.S(24), r.Bottom));
            };
        }

        void FolderChoice(string folder, bool remove)
        {
            Dictionary<string, bool> current = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (CompareEntry e in view.Entries) current[e.Path] = e.Tag is bool && (bool)e.Tag;
            Dictionary<string, bool> want = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            int skipped = 0;
            foreach (DupeGroup g in groups)
            {
                List<string> inFolder = g.Files.FindAll(delegate(string f) { return string.Equals(Path.GetDirectoryName(f), folder, StringComparison.OrdinalIgnoreCase); });
                if (inFolder.Count == 0) continue;
                if (remove)
                {
                    // Mindestens eine Datei der Gruppe ausserhalb des Ordners muss bleiben
                    bool otherKept = false;
                    foreach (string f in g.Files)
                    {
                        if (inFolder.Contains(f)) continue;
                        bool r, w;
                        bool removed = want.TryGetValue(f, out w) ? w : (current.TryGetValue(f, out r) && r);
                        if (!removed) otherKept = true;
                    }
                    if (!otherKept) { skipped++; continue; }
                }
                foreach (string f in inFolder) want[f] = remove;
            }
            ApplyChecks(want);
            if (skipped > 0)
                MessageBox.Show(this, "In " + skipped + " Gruppe(n) bliebe keine Datei übrig. Dort ist die Auswahl unverändert.",
                                App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Zeilen je Datei (eine Datei kann in mehreren Gruppen stehen), nach jedem Neuaufbau
        Dictionary<string, List<ListViewItem>> rowsByPath = new Dictionary<string, List<ListViewItem>>(StringComparer.OrdinalIgnoreCase);
        bool bulk, ready;

        void IndexRows()
        {
            rowsByPath = new Dictionary<string, List<ListViewItem>>(StringComparer.OrdinalIgnoreCase);
            if (view == null) return;
            foreach (ListViewItem it in view.List.Items)
            {
                CompareEntry ce = it.Tag as CompareEntry;
                if (ce == null) continue;
                List<ListViewItem> l;
                if (!rowsByPath.TryGetValue(ce.Path, out l)) rowsByPath[ce.Path] = l = new List<ListViewItem>();
                l.Add(it);
            }
        }

        void UpdateInfo()
        {
            // Beim Schliessen meldet die Liste noch Änderungen, während sie abgebaut wird
            if (info == null || view == null || IsDisposed || Disposing || !Visible) return;
            try
            {
                // Aus den Einträgen zählen (Tag = angehakt), nicht aus der Liste: das ginge bei tausenden Zeilen langsam.
                // Eine Datei in mehreren Gruppen zählt einmal.
                HashSet<string> chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                long bytes = 0;
                foreach (CompareEntry e in view.Entries)
                    if (e.Tag is bool && (bool)e.Tag && chosen.Add(e.Path)) bytes += Math.Max(0, e.Facts.Size);
                info.Text = chosen.Count + " Datei(en) aussortieren (" + Fmt.Size(bytes) + ")";
            }
            catch (Exception) { }
        }

        void Finish()
        {
            result = new DupeChoice();
            foreach (CompareEntry e in view.CheckedEntries()) result.Remove.Add(e.Path);
            // Schutz: aus einer Gruppe nie alle Dateien aussortieren
            foreach (DupeGroup g in groups)
            {
                bool any = false;
                foreach (string f in g.Files) if (!result.Remove.Contains(f)) any = true;
                if (!any)
                {
                    MessageBox.Show(this, "In der Gruppe „" + g.Title + "“ sind alle Dateien angehakt. Mindestens eine muss bleiben.",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    result = null;
                    return;
                }
            }
            result.ConvertKept = convertKept.Checked;
            DialogResult = DialogResult.OK;
        }

        public DupeChoice Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowDialog(owner);
            return result;
        }
    }
}
