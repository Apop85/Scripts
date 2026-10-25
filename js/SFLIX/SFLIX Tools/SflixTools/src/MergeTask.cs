// Aufgabe "Gesplittete Medien zusammenführen" (Portierung von teile_zusammenfuegen.py).
//
// Findet Videos wie "Titel pt1.mp4", "Titel pt2.mp4" und fügt sie zu "Titel.mp4" zusammen.
// Alle Teile müssen im selben Ordner liegen, dieselbe Endung haben und lückenlos ab 1
// nummeriert sein. Zuerst werden alle Funde in einer Liste zur Auswahl angezeigt, danach
// wird alles Ausgewählte am Stück verarbeitet.
//   1. Gleiches Format (Codec, Auflösung, Ton): verlustfrei aneinanderhängen, sonst neu kodieren.
//   2. Die Länge des Ergebnisses wird mit der Summe der Teile verglichen.
//   3. Erst wenn alles passt, werden die Teile nach __teile verschoben (falls eingestellt).
// Vorhandene Dateien werden nie überschrieben.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Sflix
{
    public class MergeGroup
    {
        public string Target;
        public List<string> Parts = new List<string>();
        public List<MediaInfo> Infos = new List<MediaInfo>();
        public double TotalDuration;
        public long TotalSize;
        public bool NeedsReencode;
        public bool HasAudio;
        public int Width = 1280, Height = 720;
        public string Problem;     // gesetzt = kann nicht verarbeitet werden (wird nur angezeigt)
    }

    public static class MergeTask
    {
        public const string PartsFolderName = "__teile";
        const double ToleranceSeconds = 2.0;
        static readonly HashSet<string> VideoExtensions = new HashSet<string> { ".mp4", ".m4v", ".webm", ".ogv" };
        static readonly Regex PartRe = new Regex(@"^(?<titel>.*?)[\s._-]*(?:pt|part)[\s._-]*(?<nr>[0-9]{1,2})$",
                                                 RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        static string Norm(string name)
        {
            return name.Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        public static void Run(TaskContext ctx, string folder, bool moveParts)
        {
            if (!Directory.Exists(folder)) throw new TaskFailedException("Ordner nicht gefunden: " + folder);
            ctx.Ffmpeg.EnsureInstalled(ctx);

            ctx.Progress(-1, "Suche gesplittete Videos ...");
            List<MergeGroup> groups = FindGroups(folder, ctx);
            ctx.Log(groups.Count + " zusammengehörige Teil-Gruppen gefunden in " + folder);
            if (groups.Count == 0) return;

            for (int i = 0; i < groups.Count; i++)
            {
                ctx.Progress((double)i / groups.Count, "Untersuche " + Path.GetFileName(groups[i].Target));
                Inspect(groups[i], ctx);
            }

            List<MergeGroup> selected = ctx.ChooseMerge != null ? ctx.ChooseMerge(groups) : DefaultSelection(groups);
            if (selected == null || selected.Count == 0)
            {
                ctx.Log("Nichts zum Zusammenfügen ausgewählt.");
                return;
            }

            int ok = 0, skipped = 0, failed = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                ctx.Check();
                MergeGroup g = selected[i];
                ctx.Progress((double)i / selected.Count, "Füge zusammen " + (i + 1) + " / " + selected.Count + ": " + Path.GetFileName(g.Target));
                string status = Process(g, moveParts, ctx, i + 1, selected.Count);
                if (status == "ok") ok++; else if (status == "skip") skipped++; else failed++;
            }
            ctx.Log("Zusammengefügt: " + ok + "   Übersprungen: " + skipped + "   Fehler: " + failed);
            if (failed > 0) throw new TaskFailedException(failed + " Video(s) konnten nicht zusammengefügt werden");
        }

        // Ohne Oberfläche: alles, was ohne Neukodieren geht
        public static List<MergeGroup> DefaultSelection(List<MergeGroup> groups)
        {
            List<MergeGroup> result = new List<MergeGroup>();
            foreach (MergeGroup g in groups) if (g.Problem == null && !g.NeedsReencode) result.Add(g);
            return result;
        }

        // ---------------------------------------------------------------
        // Suchen
        // ---------------------------------------------------------------

        class Candidate
        {
            public string Title;
            public string Ext;
            public SortedDictionary<int, List<string>> Parts = new SortedDictionary<int, List<string>>();
        }

        static List<MergeGroup> FindGroups(string root, TaskContext ctx)
        {
            List<MergeGroup> groups = new List<MergeGroup>();
            Walk(root, groups, ctx);
            groups.Sort(delegate(MergeGroup a, MergeGroup b) { return string.CompareOrdinal(a.Target, b.Target); });
            return groups;
        }

        static void Walk(string dir, List<MergeGroup> groups, TaskContext ctx)
        {
            ctx.Check();
            string[] files, dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException) { return; }

            Dictionary<string, Candidate> candidates = new Dictionary<string, Candidate>();
            List<string> order = new List<string>();
            foreach (string path in files)
            {
                string file = Path.GetFileName(path);
                string stem, ext;
                FileOps.SplitExt(file, out stem, out ext);
                if (!VideoExtensions.Contains(ext.ToLowerInvariant())) continue;
                Match m = PartRe.Match(stem);
                if (!m.Success || m.Groups["titel"].Value.Trim().Length == 0) continue;
                string title = m.Groups["titel"].Value.TrimEnd(' ', '.', '_', '-');
                string key = Norm(title) + "|" + ext.ToLowerInvariant();
                Candidate c;
                if (!candidates.TryGetValue(key, out c))
                {
                    c = new Candidate();
                    c.Title = title;
                    c.Ext = ext;
                    candidates[key] = c;
                    order.Add(key);
                }
                int nr = int.Parse(m.Groups["nr"].Value, CultureInfo.InvariantCulture);
                if (!c.Parts.ContainsKey(nr)) c.Parts[nr] = new List<string>();
                c.Parts[nr].Add(file);
            }

            foreach (string key in order)
            {
                Candidate c = candidates[key];
                List<int> numbers = new List<int>(c.Parts.Keys);
                bool complete = numbers.Count >= 2;
                for (int i = 0; i < numbers.Count && complete; i++) if (numbers[i] != i + 1) complete = false;
                if (!complete)
                {
                    // Ein einzelnes "pt1" ist meist kein gesplittetes Video -> nicht melden
                    if (numbers.Count >= 2 || !(numbers.Count == 1 && numbers[0] == 1))
                        ctx.Log("  [UNVOLLSTÄNDIG] " + Path.Combine(dir, c.Title) + ": Teile " + string.Join(", ", numbers) + " - übersprungen");
                    continue;
                }
                bool ambiguous = false;
                foreach (int n in numbers) if (c.Parts[n].Count > 1) ambiguous = true;
                if (ambiguous)
                {
                    ctx.Log("  [MEHRDEUTIG] " + Path.Combine(dir, c.Title) + ": ein Teil existiert mehrfach - übersprungen");
                    continue;
                }
                MergeGroup g = new MergeGroup();
                g.Target = Path.Combine(dir, c.Title + c.Ext);
                foreach (int n in numbers) g.Parts.Add(Path.Combine(dir, c.Parts[n][0]));
                groups.Add(g);
            }

            foreach (string d in dirs)
            {
                // __teile (und andere Ablagen der Werkzeuge) nicht durchsuchen
                if (Folders.Skip(Path.GetFileName(d))) continue;
                Walk(d, groups, ctx);
            }
        }

        static void Inspect(MergeGroup g, TaskContext ctx)
        {
            foreach (string part in g.Parts)
            {
                try
                {
                    MediaInfo info = ctx.Ffmpeg.Probe(part, ctx.Token);
                    g.Infos.Add(info);
                    g.TotalDuration += info.Duration;
                    g.TotalSize += new FileInfo(part).Length;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    g.Problem = Path.GetFileName(part) + " kann nicht gelesen werden (" + e.Message + ")";
                    return;
                }
            }
            if (FileOps.Exists(g.Target))
            {
                g.Problem = "Zieldatei gibt es schon (nichts wird überschrieben)";
                return;
            }
            g.HasAudio = true;
            foreach (MediaInfo i in g.Infos)
            {
                if (i.Signature != g.Infos[0].Signature) g.NeedsReencode = true;
                if (i.Audio.Count == 0) g.HasAudio = false;
            }
            if (g.Infos[0].Width > 0)
            {
                g.Width = g.Infos[0].Width;
                g.Height = g.Infos[0].Height;
            }
        }

        // ---------------------------------------------------------------
        // Zusammenfügen
        // ---------------------------------------------------------------

        static string Process(MergeGroup g, bool moveParts, TaskContext ctx, int nr, int count)
        {
            ctx.Log("[" + nr + "/" + count + "] " + g.Target + (g.NeedsReencode ? "  (neu kodieren)" : ""));
            if (FileOps.Exists(g.Target))
            {
                ctx.Log("  Zieldatei gibt es inzwischen - übersprungen (nichts überschrieben).");
                return "skip";
            }
            foreach (string p in g.Parts)
            {
                if (!File.Exists(p))
                {
                    ctx.Log("  Ein Teil fehlt inzwischen - übersprungen.");
                    return "error";
                }
            }

            string stem, ext;
            FileOps.SplitExt(g.Target, out stem, out ext);
            string temp = stem + ".__temp__" + ext;
            double newDuration;
            try
            {
                Concat(g, temp, ctx);
                newDuration = ctx.Ffmpeg.Probe(temp, ctx.Token).Duration;
                if (newDuration <= 0 || Math.Abs(newDuration - g.TotalDuration) > Math.Max(ToleranceSeconds, g.TotalDuration * 0.005))
                    throw new Exception("Länge passt nicht: Ergebnis " + Fmt.Time(newDuration) + ", erwartet " + Fmt.Time(g.TotalDuration));
                FileOps.Rename(temp, g.Target);
            }
            catch (OperationCanceledException)
            {
                FileOps.TryDelete(temp);
                throw;
            }
            catch (Exception e)
            {
                FileOps.TryDelete(temp);   // nur die eigene, unfertige Zwischendatei
                ctx.Log("  FEHLER: " + e.Message);
                ctx.Log("  Die Teile sind unverändert.");
                return "error";
            }

            ctx.Log("  fertig: " + Path.GetFileName(g.Target) + " (" + Fmt.Time(newDuration) + ", " + Fmt.Size(new FileInfo(g.Target).Length) + ")");
            if (moveParts)
            {
                string storage = Path.Combine(Path.GetDirectoryName(g.Target), PartsFolderName);
                Directory.CreateDirectory(storage);
                foreach (string p in g.Parts)
                    FileOps.Rename(p, FileOps.UniquePath(storage, Path.GetFileName(p)));
                ctx.Log("  Teile verschoben nach " + PartsFolderName + "\\");
            }
            return "ok";
        }

        static void Concat(MergeGroup g, string target, TaskContext ctx)
        {
            string ffmpeg = ctx.Ffmpeg.FfmpegPath;
            List<string> args = new List<string> { "-hide_banner", "-loglevel", "error", "-y" };
            string listFile = null;
            try
            {
                if (!g.NeedsReencode)
                {
                    listFile = Path.Combine(Path.GetTempPath(), "sflix-teile-" + Guid.NewGuid().ToString("N") + ".txt");
                    StringBuilder sb = new StringBuilder();
                    foreach (string p in g.Parts)
                        sb.Append("file '").Append(Path.GetFullPath(p).Replace("'", "'\\''")).Append("'\n");
                    File.WriteAllText(listFile, sb.ToString(), FileOps.Utf8NoBom);
                    args.AddRange(new string[] { "-f", "concat", "-safe", "0", "-i", listFile, "-map", "0", "-c", "copy" });
                }
                else
                {
                    foreach (string p in g.Parts) { args.Add("-i"); args.Add(p); }
                    List<string> filters = new List<string>();
                    StringBuilder inputs = new StringBuilder();
                    for (int i = 0; i < g.Parts.Count; i++)
                    {
                        // alle Teile auf die Auflösung von Teil 1 bringen
                        filters.Add("[" + i + ":v:0]scale=" + g.Width + ":" + g.Height + ":force_original_aspect_ratio=decrease,"
                                    + "pad=" + g.Width + ":" + g.Height + ":(ow-iw)/2:(oh-ih)/2,setsar=1[v" + i + "]");
                        inputs.Append("[v" + i + "]");
                        if (g.HasAudio) inputs.Append("[" + i + ":a:0]");
                    }
                    filters.Add(inputs + "concat=n=" + g.Parts.Count + ":v=1:a=" + (g.HasAudio ? 1 : 0) + "[v]" + (g.HasAudio ? "[a]" : ""));
                    args.AddRange(new string[] { "-filter_complex", string.Join(";", filters), "-map", "[v]" });
                    // Codecs passend zum Container (H.264/AAC passt nicht in WebM/OGV)
                    string lowerTarget = target.ToLowerInvariant();
                    if (lowerTarget.EndsWith(".webm"))
                    {
                        if (g.HasAudio) args.AddRange(new string[] { "-map", "[a]", "-c:a", "libopus", "-b:a", "160k" });
                        args.AddRange(new string[] { "-c:v", "libvpx-vp9", "-crf", "32", "-b:v", "0", "-row-mt", "1", "-pix_fmt", "yuv420p" });
                    }
                    else if (lowerTarget.EndsWith(".ogv"))
                    {
                        if (g.HasAudio) args.AddRange(new string[] { "-map", "[a]", "-c:a", "libvorbis", "-q:a", "5" });
                        args.AddRange(new string[] { "-c:v", "libtheora", "-q:v", "7" });
                    }
                    else
                    {
                        if (g.HasAudio) args.AddRange(new string[] { "-map", "[a]", "-c:a", "aac", "-b:a", "192k" });
                        args.AddRange(new string[] { "-c:v", "libx264", "-crf", "20", "-preset", "medium", "-pix_fmt", "yuv420p" });
                    }
                }
                string lower = target.ToLowerInvariant();
                if (lower.EndsWith(".mp4") || lower.EndsWith(".m4v")) args.AddRange(new string[] { "-movflags", "+faststart" });
                args.Add(target);

                ProcResult r = Proc.Run(ffmpeg, args, ctx.Token, null);
                if (r.ExitCode != 0)
                {
                    string err = r.StdErr.Trim();
                    if (err.Length > 800) err = err.Substring(err.Length - 800);
                    throw new Exception(err.Length > 0 ? err : "ffmpeg-Fehler " + r.ExitCode);
                }
            }
            finally
            {
                if (listFile != null) FileOps.TryDelete(listFile);
            }
        }
    }
}
