// Tool "Duplikate finden" (seit 3.3):
//   - identische Dateien: gleiche Grösse und gleiche SHA256-Prüfsumme
//   - gleiche Folge: dieselbe Staffel und Folge innerhalb einer Serie (S01E01 = 1x01 = S1E1 = S01E1),
//     erkannt mit EpisodeTask.ParseEpisodes. Serie = Ordner direkt unter der Kategorie
//     (z.B. Serien\Meine Serie); verglichen wird nie über verschiedene Serien hinweg.
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
            List<string> files = new List<string>();
            Folders.CollectFiles(folder, extensions, true, files, ctx.Token);
            files.RemoveAll(delegate(string f) { string n = Path.GetFileName(f); return n.StartsWith("._") || n.Contains(".__temp__"); });
            ctx.Log(files.Count + " Mediendateien gefunden in " + folder);

            List<DupeGroup> groups = new List<DupeGroup>();
            Dictionary<string, int> inIdentical = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);   // Datei -> Gruppe
            FindIdentical(ctx, files, groups, inIdentical);
            FindEpisodes(ctx, folder, files, groups, inIdentical);
            FindSameName(files, groups, inIdentical);

            // Angaben für den Vergleich und die Vorauswahl
            int done = 0, total = 0;
            foreach (DupeGroup g in groups) total += g.Files.Count;
            foreach (DupeGroup g in groups)
            {
                foreach (string f in g.Files)
                {
                    ctx.Check();
                    ctx.Progress((double)done++ / Math.Max(1, total), "Untersuche " + Path.GetFileName(f));
                    MediaFacts facts = MediaFacts.Basic(f);
                    facts.Probe(ctx.Ffmpeg, ctx.Token);
                    g.Facts.Add(facts);
                }
                Preselect(g);
            }

            int identical = 0, episodes = 0, names = 0;
            foreach (DupeGroup g in groups)
            {
                if (g.Identical) identical++; else if (g.SameName) names++; else episodes++;
                ctx.Log("  " + g.Title);
                for (int i = 0; i < g.Files.Count; i++)
                    ctx.Log("     " + (g.Remove.Contains(g.Files[i]) ? "aussortieren  " : "behalten      ") + g.Files[i] +
                            "  (" + Fmt.Size(g.Facts[i].Size) + ", " + g.Facts[i].ResolutionText + ")");
            }
            ctx.Log("Gruppen: " + identical + " mit identischen Dateien, " + episodes + " mit gleicher Folge, " + names + " mit gleichem Namen");
            ctx.Summary = identical + " identisch, " + episodes + " gleiche Folge, " + names + " gleicher Name";
            if (groups.Count == 0 || ctx.ChooseDuplicates == null) return;

            DupeChoice choice = ctx.ChooseDuplicates(groups);
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
            string dir = Path.GetDirectoryName(file);
            string storage = Path.Combine(dir, FolderName);
            Directory.CreateDirectory(storage);
            string dest = FileOps.UniquePath(storage, Path.GetFileName(file));
            FileOps.Rename(file, dest);
            string stem = Path.GetFileNameWithoutExtension(file);
            foreach (string ext in new string[] { ".jpg", ".jpeg", ".png", ".webp" })
            {
                string preview = Path.Combine(Path.Combine(dir, "Preview"), stem + ext);
                if (!File.Exists(preview)) continue;
                string previewStore = Path.Combine(storage, "Preview");
                Directory.CreateDirectory(previewStore);
                try { FileOps.Rename(preview, FileOps.UniquePath(previewStore, Path.GetFileNameWithoutExtension(dest) + ext)); }
                catch { }
            }
        }

        // Gleiche Grösse, dann gleiche Prüfsumme
        static void FindIdentical(TaskContext ctx, List<string> files, List<DupeGroup> groups, Dictionary<string, int> inIdentical)
        {
            Dictionary<long, List<string>> bySize = new Dictionary<long, List<string>>();
            foreach (string f in files)
            {
                long size;
                try { size = new FileInfo(f).Length; } catch { continue; }
                if (size <= 0) continue;
                List<string> l;
                if (!bySize.TryGetValue(size, out l)) { l = new List<string>(); bySize[size] = l; }
                l.Add(f);
            }
            List<List<string>> candidates = new List<List<string>>();
            foreach (List<string> l in bySize.Values) if (l.Count > 1) candidates.Add(l);
            int done = 0, total = 0;
            foreach (List<string> l in candidates) total += l.Count;
            foreach (List<string> l in candidates)
            {
                Dictionary<string, List<string>> byHash = new Dictionary<string, List<string>>();
                foreach (string f in l)
                {
                    ctx.Check();
                    ctx.Progress((double)done++ / Math.Max(1, total), "Prüfsumme " + done + " / " + total + ": " + Path.GetFileName(f));
                    string hash;
                    try { hash = FileOps.Sha256File(f); } catch (Exception e) { ctx.Log("Hinweis: " + f + " nicht lesbar (" + e.Message + ")"); continue; }
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
        }

        // Serie eines Videos: im Installationsordner Kategorie\Serie, sonst der erste Ordner unter dem
        // gewählten Ordner. null = keine Serie (Datei direkt in der Kategorie)
        static string SeriesKey(string root, string file)
        {
            string dir = Path.GetDirectoryName(file);
            string rel = DataTask.ToRelative(dir);
            string[] parts;
            if (rel != null)
            {
                parts = rel.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 ? (parts[0] + "\\" + parts[1]).ToLowerInvariant() : null;
            }
            string baseDir = root.TrimEnd('\\');
            if (!dir.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase)) return null;
            parts = dir.Substring(baseDir.Length).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 1 ? parts[0].ToLowerInvariant() : baseDir.ToLowerInvariant();
        }

        static void FindEpisodes(TaskContext ctx, string root, List<string> files, List<DupeGroup> groups, Dictionary<string, int> inIdentical)
        {
            // (Serie, Staffel, Folge) -> Dateien; danach zusammenhängende Dateien zu Gruppen verbinden
            Dictionary<string, List<string>> byEpisode = new Dictionary<string, List<string>>();
            Dictionary<string, List<int>> episodesOf = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> seasonOf = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                if (MediaTypes.Kind(Path.GetExtension(f).ToLowerInvariant()) != "video") continue;
                string series = SeriesKey(root, f);
                if (series == null) continue;
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
                }
                List<int> all = new List<int>();
                foreach (string f in c) foreach (int e in episodesOf[f]) if (!all.Contains(e)) all.Add(e);
                all.Sort();
                g.Title = "Gleiche Folge " + EpisodeTask.EpisodeLabel(seasonOf[c[0]], all) + ": " +
                          Path.GetFileName(Path.GetDirectoryName(c[0])) + (g.Overlap ? " (Doppelfolge, bitte selbst wählen)" : "");
                g.Files.AddRange(c);
                groups.Add(g);
            }
        }

        // Gleicher Dateiname ohne Endung (gross/klein egal), z.B. "Film.mp4" in zwei Ordnern oder
        // "Film.mp4" neben "Film.mkv". Preview-, Ablage-, System- und versteckte Ordner sind schon
        // beim Sammeln ausgeschlossen. Keine Gruppe, die schon als identisch gemeldet ist oder
        // genau dieselben Dateien wie eine andere Gruppe hat.
        static void FindSameName(List<string> files, List<DupeGroup> groups, Dictionary<string, int> inIdentical)
        {
            Dictionary<string, List<string>> byName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                string name = Path.GetFileNameWithoutExtension(f).Trim();
                if (name.Length == 0) continue;
                List<string> l;
                if (!byName.TryGetValue(name, out l)) { l = new List<string>(); byName[name] = l; }
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
                g.Title = "Gleicher Name: " + kv.Key + (c.Count > 2 ? " (" + c.Count + " Dateien)" : "");
                g.Files.AddRange(c);
                groups.Add(g);
            }
        }

        static void Preselect(DupeGroup g)
        {
            g.Remove.Clear();
            if (g.Overlap || g.Files.Count < 2) return;
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

        public DuplicateDialog(List<DupeGroup> groups, FfmpegTools ff)
        {
            this.groups = groups;
            Text = "Duplikate finden – Auswahl";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1180), Theme.S(640));
            MinimumSize = new Size(Theme.S(760), Theme.S(420));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(groups.Count + " Gruppe(n) gefunden. Angehakte Dateien kommen samt Vorschaubild nach „" + DuplicateTask.FolderName +
                                          "“ („Ablagen leeren“ löscht sie später). Vorausgewählt ist die Datei mit höherer Auflösung bzw. die grössere, " +
                                          "bessere Werte sind grün. Bei Doppelfolgen bitte selbst wählen.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));

            view = new CompareView(ff, true);
            view.CheckBoxes = true;
            view.Dock = DockStyle.Fill;
            view.GroupTitles = new List<string>();
            List<List<CompareEntry>> entries = new List<List<CompareEntry>>();
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
                    e.Note = DataTask.ToRelative(Path.GetDirectoryName(e.Path)) ?? Path.GetDirectoryName(e.Path);
                    e.Tag = g.Remove.Contains(e.Path);
                    list.Add(e);
                }
                entries.Add(list);
            }
            view.SetGroups(entries);
            view.List.ItemChecked += delegate(object s, ItemCheckedEventArgs e)
            {
                CompareEntry ce = e.Item.Tag as CompareEntry;
                if (ce == null) return;
                ce.Label = e.Item.Checked ? "aussortieren" : "behalten";
                e.Item.Text = ce.Label;
                UpdateInfo();
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
            none.Click += delegate { foreach (ListViewItem it in view.List.Items) it.Checked = false; };
            info = Theme.MakeLabel("", true);
            info.Margin = new Padding(0, Theme.S(8), Theme.S(12), 0);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            buttons.Controls.Add(none);
            buttons.Controls.Add(info);
            buttons.Controls.Add(convertKept);

            Controls.Add(host);
            Controls.Add(buttons);
            Controls.Add(intro);
            CancelButton = cancel;
            UpdateInfo();
        }

        void UpdateInfo()
        {
            int n = view.CheckedEntries().Count;
            long bytes = 0;
            foreach (CompareEntry e in view.CheckedEntries()) bytes += Math.Max(0, e.Facts.Size);
            info.Text = n + " Datei(en) aussortieren (" + Fmt.Size(bytes) + ")";
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
