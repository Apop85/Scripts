// Tool "Medien prüfen" (seit 3.3): findet Medien, die der Browser nicht abspielen kann, defekte und
// leere Dateien, Videos ohne Vorschaubild und (seit 3.4) Ordner ohne Medien. Im Ergebnis lassen sich
// Einträge anhaken und gleich löschen, konvertieren bzw. Vorschaubilder erstellen. Ohne Oberfläche wird
// nur das Ergebnis protokolliert.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public static class MediaCheck
    {
        // Spielt der Browser die Datei ab? (Chrome, Edge, Firefox)
        public static bool IsPlayable(MediaFacts f, string ext)
        {
            if (!f.HasVideo)
            {
                // Musik: die Endungen in MediaTypes.ReadyAudio spielen alle Browser, M4A aber nur mit AAC oder MP3 (nicht ALAC)
                if (ext == ".m4a" && f.Probed)
                {
                    if (f.AudioCodecList.Count == 0) return false;
                    foreach (string a in f.AudioCodecList) if (a != "aac" && a != "mp3") return false;
                    return true;
                }
                return MediaTypes.ReadyAudio.Contains(ext) || MediaTypes.ReadyVideo.Contains(ext);
            }
            List<string> audio = f.AudioCodecList;
            if (ext == ".webm")
            {
                bool v = f.VideoCodec == "vp8" || f.VideoCodec == "vp9" || f.VideoCodec == "av1";
                foreach (string a in audio) if (a != "opus" && a != "vorbis") return false;
                return v;
            }
            if (ext == ".ogv") return f.VideoCodec == "theora";
            if (ext != ".mp4" && ext != ".m4v") return false;
            // H.264 nur mit 8 Bit 4:2:0, HEVC (H.265) spielen Firefox und viele PCs nicht
            bool video = f.VideoCodec == "h264" && (f.PixelFormat == "yuv420p" || f.PixelFormat == "yuvj420p");
            bool sound = true;
            foreach (string a in audio) if (a != "aac" && a != "mp3") sound = false;
            return video && sound;
        }

        // Grund in Worten, warum eine Datei nicht abspielbar ist
        public static string Reason(MediaFacts f, string ext)
        {
            if (MediaTypes.NeedsConversion(ext)) return "Format " + ext + " wird vom Browser nicht abgespielt";
            List<string> parts = new List<string>();
            if (f.HasVideo && !(f.VideoCodec == "h264" && (f.PixelFormat == "yuv420p" || f.PixelFormat == "yuvj420p")) && ext != ".webm")
                parts.Add("Video " + f.VideoCodec + (f.PixelFormat.Contains("10") ? " 10 Bit" : ""));
            foreach (string a in f.AudioCodecList)
                if (a != "aac" && a != "mp3" && a != "opus" && a != "vorbis") { parts.Add("Ton " + a); break; }
            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "Codec wird nicht unterstützt";
        }
    }

    public class MediaCheckItem
    {
        public string Path;
        public string Kind;     // "format", "broken", "locked" (nicht lesbar), "gaps" (lückenhaft), "empty", "preview", "folder" (Ordner ohne Medien)
        public string Problem;
        public MediaFacts Facts;
        public List<MediaGap> Gaps;   // Kind "gaps": Stellen und Längen
        public bool SuggestDelete;  // Ordner: zum Löschen vorschlagen (nichts ausser Vorschaubildern darin)
        public bool CanConvert
        {
            get
            {
                string kind = MediaTypes.Kind(System.IO.Path.GetExtension(Path).ToLowerInvariant());
                return Kind == "format" && (kind == "video" || kind == "audio");
            }
        }
    }

    // Eine Datei im Ergebnis: alle Auffälligkeiten und die gewählten Aktionen.
    // Löschen schliesst Konvertieren und Vorschaubild aus, die beiden anderen gehen zusammen.
    public class MediaCheckFile
    {
        public string Path;
        public MediaFacts Facts;
        public List<MediaCheckItem> Problems = new List<MediaCheckItem>();
        public bool Delete, Convert, Preview;

        public bool Has(string kind)
        {
            foreach (MediaCheckItem p in Problems) if (p.Kind == kind) return true;
            return false;
        }

        public bool CanConvert
        {
            get
            {
                foreach (MediaCheckItem p in Problems) if (p.CanConvert) return true;
                return false;
            }
        }

        public bool CanPreview { get { return Has("preview"); } }

        // Vorschlag: defekte und leere Dateien sowie leere Ordner löschen, sonst konvertieren und/oder Vorschaubild erstellen
        public void Suggest()
        {
            bool folder = false;
            foreach (MediaCheckItem p in Problems) if (p.Kind == "folder" && p.SuggestDelete) folder = true;
            Delete = Has("broken") || Has("empty") || folder;
            Convert = !Delete && CanConvert;
            Preview = !Delete && CanPreview;
        }

        // Schwerste Auffälligkeit zuerst (für die Sortierung)
        public int Order
        {
            get
            {
                int o = 9;
                foreach (MediaCheckItem p in Problems) o = Math.Min(o, MediaCheckDialog.Order(p.Kind));
                return o;
            }
        }

        public string ProblemText
        {
            get
            {
                List<string> parts = new List<string>();
                foreach (MediaCheckItem p in Problems) parts.Add(p.Problem);
                return string.Join("; ", parts.ToArray());
            }
        }

        // Gruppiert die Auffälligkeiten je Datei (Reihenfolge der ersten Auffälligkeit)
        public static List<MediaCheckFile> FromItems(List<MediaCheckItem> items)
        {
            List<MediaCheckFile> files = new List<MediaCheckFile>();
            Dictionary<string, MediaCheckFile> byPath = new Dictionary<string, MediaCheckFile>(StringComparer.OrdinalIgnoreCase);
            foreach (MediaCheckItem it in items)
            {
                MediaCheckFile f;
                if (!byPath.TryGetValue(it.Path, out f))
                {
                    f = new MediaCheckFile();
                    f.Path = it.Path;
                    f.Facts = it.Facts;
                    byPath[it.Path] = f;
                    files.Add(f);
                }
                f.Problems.Add(it);
            }
            foreach (MediaCheckFile f in files) f.Suggest();
            return files;
        }
    }

    // Gewählte Dateioperationen aus dem Ergebnis
    public class MediaCheckChoice
    {
        public List<string> Delete = new List<string>();
        public List<string> Convert = new List<string>();
        public List<string> Preview = new List<string>();
        // Zu löschende lückenhafte Dateien: sind lesbar, die erneute Prüfung vor dem Löschen entfällt
        public HashSet<string> Gaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int Count { get { return Delete.Count + Convert.Count + Preview.Count; } }
    }

    // Gewählte Prüfungen von "Medien prüfen" (SFLIX-20261123-100). Standard ohne gespeicherte Einstellung:
    // nur Nicht abspielbar und Fehlende Vorschaubilder.
    public class MediaCheckKinds
    {
        public bool Format = true;     // nicht abspielbar und leere Dateien (Kind format, empty)
        public bool Damaged;           // defekt oder nicht lesbar (broken, locked)
        public bool Gaps;              // Lücken suchen (gaps, DEC-373)
        public bool Preview = true;    // fehlende Vorschaubilder (preview)
        public bool Folders;           // Ordner ohne Medien (folder)
        public bool Dupes;             // Duplikate (DEC-383): danach das Duplikat-Fenster, beim ersten Start aus

        public bool Any { get { return AnyCheck || Dupes; } }
        // Prüfungen mit eigener Ergebnisliste (ohne Duplikate)
        public bool AnyCheck { get { return Format || Damaged || Gaps || Preview || Folders; } }

        public static MediaCheckKinds OnlyDupes()
        {
            MediaCheckKinds k = new MediaCheckKinds();
            k.Format = false;
            k.Preview = false;
            k.Dupes = true;
            return k;
        }
        // ffprobe nötig (Video und M4A untersuchen)
        public bool NeedsProbe { get { return Format || Damaged; } }
        public bool NeedsFfmpeg { get { return Format || Damaged || Gaps; } }

        public static MediaCheckKinds FromSettings(Settings settings)
        {
            MediaCheckKinds k = new MediaCheckKinds();
            k.Format = settings.GetBool("check.format", true);
            k.Damaged = settings.GetBool("check.damaged", false);
            k.Gaps = settings.GetBool("check.gaps", false);
            k.Preview = settings.GetBool("check.preview", true);
            k.Folders = settings.GetBool("check.folders", false);
            k.Dupes = settings.GetBool("check.dupes", false);
            return k;
        }
    }

    public static class MediaCheckTask
    {
        // Ablage für gelöschte Medien (im Ordner der Datei, das Vorschaubild in Preview\__gelöscht)
        public const string DeletedFolderName = "__gelöscht";

        // kinds: gewählte Prüfungen; nur diese laufen (ohne Format/Defekt kein ffprobe). "Lücken suchen" (DEC-373)
        // liest jedes Video ganz; das Ergebnis wird gemerkt (medien_cache.json), der nächste Lauf liest nur
        // neue oder geänderte Dateien.
        // Mit "Duplikate" (DEC-383) folgt nach der Prüfung (bzw. allein) die Duplikatsuche je Ordner. Sie
        // übernimmt die Dateiliste der Prüfung, ausser die Prüfung hat Dateien gelöscht oder verändert.
        public static void Run(TaskContext ctx, List<string> folders, ConvertOptions convert, bool previewVideos, bool previewImages, MediaCheckKinds kinds)
        {
            List<string> roots = Folders.WithoutNested(folders != null && folders.Count > 0 ? folders : new List<string> { App.Root });
            List<FileInfo> listed = null;
            bool changed = false;
            List<string> summary = new List<string>();
            if (kinds.AnyCheck)
            {
                changed = RunChecks(ctx, roots, convert, previewVideos, previewImages, kinds, out listed);
                if (!string.IsNullOrEmpty(ctx.Summary)) summary.Add(ctx.Summary);
            }
            if (!kinds.Dupes) return;
            foreach (string root in roots)
            {
                ctx.Token.ThrowIfCancellationRequested();
                ctx.Log("");
                ctx.Log("Duplikate" + (roots.Count > 1 ? " in " + root : "") + ":");
                ctx.Summary = null;
                DuplicateTask.Run(ctx, root, changed ? null : listed);
                if (!string.IsNullOrEmpty(ctx.Summary)) summary.Add("Duplikate" + (roots.Count > 1 ? " (" + Path.GetFileName(root.TrimEnd('\\')) + ")" : "") + ": " + ctx.Summary);
            }
            ctx.Summary = string.Join("; ", summary.ToArray());
        }

        // Die Prüfungen mit eigener Ergebnisliste. true: Dateien wurden gelöscht, konvertiert o. ä.
        static bool RunChecks(TaskContext ctx, List<string> roots, ConvertOptions convert, bool previewVideos, bool previewImages,
                              MediaCheckKinds kinds, out List<FileInfo> listed)
        {
            listed = null;
            bool gaps = kinds.Gaps;
            if (kinds.NeedsFfmpeg) ctx.Ffmpeg.EnsureInstalled(ctx);
            HashSet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HashSet<string> set in new HashSet<string>[] { MediaTypes.ReadyVideo, MediaTypes.ReadyAudio, MediaTypes.ReadyImage,
                                                                    MediaTypes.ConvertVideo, MediaTypes.ConvertAudio, MediaTypes.ConvertImage })
                extensions.UnionWith(set);

            ctx.Progress(-1, "Suche Medien ...");
            // Dauer je Schritt fürs Protokoll (Vergleich am Netzlaufwerk: welcher Schritt bremst)
            System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
            MediaCache.TakeStats();
            // Mit Grösse und Änderungszeit aus der Auflistung: die Duplikatsuche übernimmt sie (DEC-383)
            List<FileInfo> infos = new List<FileInfo>();
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) throw new TaskFailedException("Ordner nicht gefunden: " + root);
                Folders.CollectFiles(root, extensions, true, infos, ctx.Token);
            }
            infos.RemoveAll(delegate(FileInfo f) { string n = f.Name; return n.StartsWith("._") || n.Contains(".__temp__"); });
            listed = infos;
            List<string> files = new List<string>(infos.Count);
            foreach (FileInfo f in infos) files.Add(f.FullName);
            ctx.Log(files.Count + " Mediendateien gefunden in " + string.Join(", ", roots.ToArray()));
            double tList = clock.Elapsed.TotalSeconds;

            // Parallel prüfen (ffprobe wartet vor allem aufs Lesen), Ergebnisse je Datei in fester Reihenfolge
            List<MediaCheckItem>[] perFile = new List<MediaCheckItem>[files.Count];
            int checkedCount = 0;
            Workers.ForEach(files, Workers.Degree(ctx, 4), ctx.Token, delegate(int i, string file)
            {
                List<MediaCheckItem> found = new List<MediaCheckItem>();
                CheckFile(ctx, file, previewVideos, previewImages, kinds, found);
                perFile[i] = found;
                int n = System.Threading.Interlocked.Increment(ref checkedCount);
                if (n % 5 == 0 || n == files.Count) ctx.Progress((double)n / Math.Max(1, files.Count), "Prüfe " + n + " / " + files.Count);
            });
            MediaCache.Save();
            double tProbe = clock.Elapsed.TotalSeconds;
            List<MediaCheckItem> items = new List<MediaCheckItem>();
            foreach (List<MediaCheckItem> found in perFile) if (found != null) items.AddRange(found);
            if (kinds.Folders)
            {
                ctx.Progress(-1, "Suche leere Ordner ...");
                foreach (string root in roots) FindEmptyFolders(ctx, root, extensions, items);
            }
            double tCheck = clock.Elapsed.TotalSeconds;

            int format = 0, broken = 0, locked = 0, gapped = 0, empty = 0, preview = 0, emptyFolders = 0, logged = 0;
            // Mit Oberfläche steht alles in der Ergebnisliste: im Protokoll nur die ersten Einträge
            int maxLogged = ctx.ChooseMediaCheck != null ? 100 : int.MaxValue;
            foreach (MediaCheckItem it in items)
            {
                if (it.Kind == "format") format++;
                else if (it.Kind == "broken") broken++;
                else if (it.Kind == "locked") locked++;
                else if (it.Kind == "gaps")
                {
                    gapped++;
                    // Gemeinsame Liste beschädigter Dateien für STEFFLIX (DEC-374)
                    DamagedList.Add(it.Path, it.Gaps, DamagedList.SourceCheck);
                }
                else if (it.Kind == "empty") empty++;
                else if (it.Kind == "folder") emptyFolders++;
                else preview++;
                if (logged++ < maxLogged) ctx.Log("  " + (it.Kind == "preview" || it.Kind == "folder" ? "Hinweis: " : it.Kind == "format" || it.Kind == "locked" ? "WARNUNG " : "FEHLER ") + it.Problem + "  –  " + it.Path);
            }
            if (items.Count > maxLogged) ctx.Log("  … und " + (items.Count - maxLogged) + " weitere (alle stehen in der Ergebnisliste)");
            ctx.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Dauer: Auflistung {0:0.0} s, Prüfen {1:0.0} s, leere Ordner {2:0.0} s, gesamt {3:0.0} s",
                    tList, tProbe - tList, tCheck - tProbe, tCheck));
            string stats = MediaCache.TakeStats();
            if (stats != null) ctx.Log(stats);
            // Nur die gewählten Prüfungen nennen
            List<string> counts = new List<string>(), summary = new List<string>();
            if (kinds.Format)
            {
                counts.Add("Nicht abspielbar: " + format); counts.Add("Leer: " + empty);
                summary.Add(format + " nicht abspielbar"); summary.Add(empty + " leer");
            }
            if (kinds.Damaged)
            {
                counts.Add("Defekt: " + broken); if (locked > 0) counts.Add("Nicht lesbar: " + locked);
                summary.Add(broken + " defekt"); if (locked > 0) summary.Add(locked + " nicht lesbar");
            }
            if (gaps) { counts.Add("Lückenhaft: " + gapped); summary.Add(gapped + " lückenhaft"); }
            if (kinds.Preview) { counts.Add("Ohne Vorschaubild: " + preview); summary.Add(preview + " ohne Vorschaubild"); }
            if (kinds.Folders) { counts.Add("Leere Ordner: " + emptyFolders); summary.Add(emptyFolders + " leere Ordner"); }
            ctx.Log(string.Join("   ", counts.ToArray()));
            ctx.Summary = files.Count + " geprüft, " + string.Join(", ", summary.ToArray());
            if (items.Count == 0 || ctx.ChooseMediaCheck == null) return false;

            System.Diagnostics.Stopwatch wait = System.Diagnostics.Stopwatch.StartNew();
            MediaCheckChoice choice = ctx.ChooseMediaCheck(items);
            ctx.WaitSeconds += wait.Elapsed.TotalSeconds;
            if (choice == null || choice.Count == 0) return false;
            // Löschen (erneute Prüfung) und Konvertieren brauchen ffmpeg, auch wenn die Prüfung ohne lief
            ctx.Ffmpeg.EnsureInstalled(ctx);
            // Konvertieren setzt seine eigene Zusammenfassung: danach wieder die der Prüfung, mit den Aktionen
            string own = ctx.Summary;
            try { Execute(ctx, roots, choice, convert); }
            finally
            {
                List<string> done = new List<string>();
                // Gewählte Aktionen (Ergebnis je Datei steht im Protokoll)
                if (choice.Delete.Count > 0) done.Add(choice.Delete.Count + " löschen");
                if (choice.Convert.Count > 0) done.Add(choice.Convert.Count + " konvertieren");
                if (choice.Preview.Count > 0) done.Add(choice.Preview.Count + " Vorschaubild(er)");
                ctx.Summary = own + (done.Count > 0 ? "; gewählt: " + string.Join(", ", done.ToArray()) : "");
            }
            // Vorschaubilder ändern keine Mediendateien; Löschen und Konvertieren schon
            return choice.Delete.Count > 0 || choice.Convert.Count > 0;
        }

        // Reihenfolge: löschen, konvertieren, dann Vorschaubilder (für konvertierte Dateien vom Ergebnis)
        static void Execute(TaskContext ctx, List<string> roots, MediaCheckChoice choice, ConvertOptions convert)
        {
            int failed = 0;
            if (choice.Delete.Count > 0)
            {
                ctx.Log("");
                ctx.Log("Verschiebe " + choice.Delete.Count + " Datei(en) nach „" + DeletedFolderName + "“ ...");
                foreach (string f in choice.Delete)
                {
                    ctx.Check();
                    try
                    {
                        // Als defekt gemeldete Medien vorher neu untersuchen (ohne gemerktes Ergebnis): ein
                        // Aussetzer des Netzlaufwerks bei der Prüfung darf keine gute Datei löschen
                        if (!choice.Gaps.Contains(f) && StillReadable(ctx, f))
                        {
                            ctx.Log("  NICHT GELÖSCHT  " + f + " :: bei erneuter Prüfung lesbar");
                            continue;
                        }
                        MoveToDeleted(f);
                        DamagedList.Remove(f);   // in der Ablage: nicht mehr in der Liste beschädigter Dateien
                        ctx.Log("  GELÖSCHT  " + f + "  ->  " + DeletedFolderName);
                    }
                    catch (Exception e)
                    {
                        failed++;
                        ctx.Log("  FEHLER  " + f + " :: " + e.Message);
                    }
                }
            }

            ConcurrentDictionary<string, List<string>> results = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (choice.Convert.Count > 0)
            {
                ctx.Log("");
                ctx.Log("Konvertiere " + choice.Convert.Count + " Datei(en) ...");
                // Gewählte Musik immer umwandeln, auch wenn "Auch Musik" beim Tool Konvertieren aus ist
                bool audio = convert.Audio;
                convert.Audio = true;
                convert.Results = results;
                try
                {
                    foreach (string root in roots)
                    {
                        HashSet<string> only = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (string f in choice.Convert) if (f.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) only.Add(f);
                        if (only.Count == 0) continue;
                        convert.Folder = root;
                        convert.OnlyFiles = only;
                        // Fehler einzelner Dateien: trotzdem weiter mit den Vorschaubildern
                        try { ConvertTask.Run(ctx, convert); }
                        catch (TaskFailedException) { failed++; }
                        finally { convert.OnlyFiles = null; }
                    }
                }
                finally { convert.Audio = audio; convert.Results = null; }
            }

            if (choice.Preview.Count > 0)
            {
                // Konvertierte Dateien heissen jetzt anders (.mp4), Vorschaubilder für das Ergebnis erstellen
                List<string> targets = new List<string>();
                foreach (string f in choice.Preview)
                {
                    List<string> made;
                    if (results.TryGetValue(f, out made)) targets.AddRange(made);
                    else if (File.Exists(f)) targets.Add(f);
                }
                ctx.Log("");
                ctx.Log("Erstelle " + targets.Count + " Vorschaubild(er) ...");
                PreviewTask.MakeFor(ctx, targets);
            }
            if (failed > 0) throw new TaskFailedException("Nicht alle Dateioperationen waren erfolgreich");
        }

        // Prüft eine Datei (läuft parallel in mehreren Threads, nur lokale Ergebnisse in "found")
        // Nur die gewählten Prüfungen; ffprobe nur für Nicht abspielbar oder Defekt (SFLIX-20261123-100)
        static void CheckFile(TaskContext ctx, string file, bool previewVideos, bool previewImages, MediaCheckKinds kinds, List<MediaCheckItem> found)
        {
            ctx.Check();
            string ext = Path.GetExtension(file).ToLowerInvariant();
            string kind = MediaTypes.Kind(ext);
            MediaFacts facts = MediaFacts.Basic(file);
            if (facts.Size == 0)
            {
                if (kinds.Format) found.Add(Item(file, "empty", "leere Datei (0 Byte)", facts));
                return;
            }
            bool previewWanted = kinds.Preview && ((kind == "video" && previewVideos) || (kind == "image" && previewImages));
            if (MediaTypes.NeedsConversion(ext))
            {
                if (kinds.Format)
                {
                    if (kind == "video") facts.Probe(ctx.Ffmpeg, ctx.Token);
                    found.Add(Item(file, "format", MediaCheck.Reason(facts, ext), facts));
                }
                // Das Vorschaubild gilt auch nach dem Konvertieren (gleicher Name, andere Endung)
                if (previewWanted && !HasPreview(file)) found.Add(Item(file, "preview", "kein Vorschaubild", facts));
                return;
            }
            if (kind == "video")
            {
                if (kinds.NeedsProbe)
                {
                    facts.Probe(ctx.Ffmpeg, ctx.Token);
                    if (facts.ProbeError != null)
                    {
                        // Nicht lesbar: ohne Prüfung "Defekt" nicht melden, auch keine Lückensuche
                        if (kinds.Damaged) found.Add(ProbeFailed(file, facts));
                        return;
                    }
                    if (kinds.Format && !MediaCheck.IsPlayable(facts, ext)) found.Add(Item(file, "format", MediaCheck.Reason(facts, ext), facts));
                }
                if (kinds.Gaps) CheckGaps(ctx, file, facts, found);
                if (previewWanted && !HasPreview(file)) found.Add(Item(file, "preview", "kein Vorschaubild", facts));
            }
            else if (kind == "audio" && ext == ".m4a")
            {
                // M4A kann ALAC enthalten, das der Browser nicht abspielt
                if (!kinds.NeedsProbe) return;
                facts.Probe(ctx.Ffmpeg, ctx.Token);
                if (facts.ProbeError != null)
                {
                    if (kinds.Damaged) found.Add(ProbeFailed(file, facts));
                    return;
                }
                if (kinds.Format && !MediaCheck.IsPlayable(facts, ext)) found.Add(Item(file, "format", MediaCheck.Reason(facts, ext), facts));
            }
            else if (kind == "image" && previewWanted && !HasPreview(file))
            {
                found.Add(Item(file, "preview", "kein Vorschaubild", facts));
            }
        }

        // ---------------------------------------------------------------
        // Leere Ordner (seit 3.4)
        // ---------------------------------------------------------------

        // Ordner ohne Medien, auch nicht in Unterordnern (Preview-, Ablage-, System- und versteckte Ordner
        // zählen nicht). Gemeldet wird nur der oberste solche Ordner, nie der gewählte Ordner selbst und
        // nie eine Kategorie direkt im STEFFLIX-Ordner (z.B. "Filme").
        static void FindEmptyFolders(TaskContext ctx, string root, HashSet<string> extensions, List<MediaCheckItem> items)
        {
            List<string> found = new List<string>();
            ScanFolder(ctx, root, extensions, found, true);
            foreach (string dir in found)
            {
                int previews, others;
                long size;
                CountLeftovers(dir, out previews, out others, out size);
                MediaFacts facts = new MediaFacts();
                facts.Path = dir;
                facts.Size = size;
                string problem = others > 0 ? "Ordner ohne Medien, " + others + " andere Datei" + (others == 1 ? "" : "en")
                               : previews > 0 ? "Ordner ohne Medien, nur Vorschaubilder"
                               : "leerer Ordner";
                MediaCheckItem it = Item(dir, "folder", problem, facts);
                // Andere Dateien (z.B. Dokumente, Untertitel) könnten noch gebraucht werden: nicht vorschlagen
                it.SuggestDelete = others == 0;
                items.Add(it);
            }
        }

        static bool IsAppRoot(string dir)
        {
            return string.Equals(Path.GetFullPath(dir).TrimEnd('\\'), Path.GetFullPath(App.Root).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        // true: Der Ordner enthält Medien (direkt oder darunter). Unterordner ohne Medien werden gemeldet,
        // wenn dieser Ordner Medien hat oder ganz oben steht; sonst meldet ihn der Ordner darüber als Ganzes.
        static bool ScanFolder(TaskContext ctx, string dir, HashSet<string> extensions, List<string> found, bool top)
        {
            ctx.Check();
            // Art und Attribute aus der Auflistung (kein GetAttributes je Ordner)
            FileSystemInfo[] entries;
            try { entries = new DirectoryInfo(dir).GetFileSystemInfos(); }
            catch (UnauthorizedAccessException) { return true; }   // nicht lesbar: lieber nicht melden
            catch (IOException) { return true; }

            bool hasMedia = false;
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();
            foreach (FileSystemInfo e in entries)
            {
                if (e is DirectoryInfo) { dirs.Add((DirectoryInfo)e); continue; }
                if (hasMedia) continue;
                string n = e.Name;
                if (n.StartsWith("._") || n.Contains(".__temp__")) continue;
                if (extensions.Contains(e.Extension.ToLowerInvariant())) hasMedia = true;
            }
            // Die Kategorien direkt im STEFFLIX-Ordner gelten selbst als "oben"
            bool appRoot = IsAppRoot(dir);
            List<string> empty = new List<string>();
            foreach (DirectoryInfo d in dirs)
            {
                // Verknüpfung (Junction, symbolischer Link): nie durchsuchen, nie als leerer Ordner melden und den
                // Ordner darüber nicht als leer werten (vor Folders.Ignore, das Verknüpfungen nur überspringt)
                if ((d.Attributes & FileAttributes.ReparsePoint) != 0) { hasMedia = true; continue; }
                if (Folders.Ignore(d)) continue;
                if (string.Equals(d.Name, "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                if (ScanFolder(ctx, d.FullName, extensions, found, appRoot)) hasMedia = true;
                else empty.Add(d.FullName);
            }
            if ((hasMedia || top) && !appRoot) found.AddRange(empty);
            return hasMedia;
        }

        // Was liegt noch im Ordner? Vorschaubilder, Ablagen (__) und Systemdateien zählen nicht als "andere".
        static void CountLeftovers(string dir, out int previews, out int others, out long size)
        {
            previews = 0; others = 0; size = 0;
            // Grössen aus der Auflistung (kein FileInfo je Datei)
            FileInfo[] all;
            try { all = new DirectoryInfo(dir).GetFiles("*", SearchOption.AllDirectories); }
            catch { return; }
            // FullName ist ein normalisierter voller Pfad: relativ zum ebenso normalisierten Ordner rechnen
            string baseDir = Path.GetFullPath(dir).TrimEnd('\\');
            foreach (FileInfo info in all)
            {
                string f = info.FullName;
                try { size += info.Length; } catch { }
                string rel = f.Length > baseDir.Length ? f.Substring(baseDir.Length).TrimStart('\\') : info.Name;
                string name = Path.GetFileName(f);
                if (name.StartsWith(".") || string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                bool preview = false, storage = false;
                foreach (string seg in rel.Split('\\'))
                {
                    if (string.Equals(seg, "Preview", StringComparison.OrdinalIgnoreCase)) preview = true;
                    if (Folders.IsToolFolder(seg)) storage = true;
                }
                if (storage) continue;
                if (preview) previews++; else others++;
            }
        }

        // Ordner als Ganzes nach <übergeordneter Ordner>\__gelöscht ("Ablagen leeren" löscht endgültig)
        static void MoveFolderToDeleted(string dir)
        {
            string full = Path.GetFullPath(dir).TrimEnd('\\');
            string reason;
            if (IsAppRoot(full) || !Folders.IsInsideRoot(full, out reason))
                throw new IOException("Ordner liegt nicht im Installationsordner: " + full);
            FileOps.MoveIntoFolder(full, Path.Combine(Path.GetDirectoryName(full), DeletedFolderName));
        }

        // Datei nach <Ordner>\__gelöscht, ihr Vorschaubild nach Preview\__gelöscht ("Ablagen leeren" löscht endgültig)
        // Datei mit Inhalt, die ffprobe untersucht (Video, M4A): jetzt lesbar?
        static bool StillReadable(TaskContext ctx, string file)
        {
            if (ctx.Ffmpeg == null || !File.Exists(file)) return false;
            string ext = Path.GetExtension(file).ToLowerInvariant();
            string kind = MediaTypes.Kind(ext);
            if (!(kind == "video" || ext == ".m4a") || MediaTypes.NeedsConversion(ext)) return false;
            try
            {
                if (new FileInfo(file).Length == 0) return false;
                ctx.Ffmpeg.ProbeFresh(file, ctx.Token);
                return true;
            }
            catch (OperationCanceledException) { throw; }
            catch { return false; }
        }

        // Auch für den Downloader (Datei mit Lücke löschen, DEC-373)
        internal static void MoveToDeleted(string file)
        {
            if (Directory.Exists(file))
            {
                MoveFolderToDeleted(file);
                return;
            }
            string dir = Path.GetDirectoryName(file);
            FileOps.MoveIntoFolder(file, Path.Combine(dir, DeletedFolderName));

            // Vorschaubild nur mitnehmen, wenn es kein anderes Medium mit gleichem Namen gibt (z.B. Film.mp4 neben Film.mkv)
            string stem = Path.GetFileNameWithoutExtension(file);
            foreach (string other in Directory.GetFiles(dir, stem + ".*"))
                if (string.Equals(Path.GetFileNameWithoutExtension(other), stem, StringComparison.OrdinalIgnoreCase) &&
                    MediaTypes.Kind(Path.GetExtension(other).ToLowerInvariant()) != null) return;
            FileOps.MovePreviewsInto(file, Path.Combine(Path.Combine(dir, "Preview"), DeletedFolderName), null);
        }

        // ffprobe scheitert: Lässt sich die Datei gar nicht öffnen (von einem anderen Programm gesperrt,
        // keine Berechtigung, Aussetzer des Netzlaufwerks), ist sie nicht defekt, sondern nur gerade nicht
        // lesbar. Eigener Hinweis, nicht zum Löschen vorausgewählt (SFLIX-20261123-079).
        static MediaCheckItem ProbeFailed(string file, MediaFacts facts)
        {
            bool denied = facts.ProbeError.IndexOf("Permission denied", StringComparison.OrdinalIgnoreCase) >= 0;
            if (denied || !CanOpen(file))
                return Item(file, "locked", "nicht lesbar (gesperrt oder keine Berechtigung)", facts);
            return Item(file, "broken", "defekt: " + facts.ProbeError, facts);
        }

        static bool CanOpen(string file)
        {
            try
            {
                using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) { }
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        // Lückensuche (DEC-373): gemerktes Ergebnis oder ein Lesevorgang über die ganze Datei
        static void CheckGaps(TaskContext ctx, string file, MediaFacts facts, List<MediaCheckItem> found)
        {
            List<MediaGap> gaps;
            try { gaps = ctx.Ffmpeg.Gaps(file, ctx.Token); }
            catch (OperationCanceledException) { throw; }
            catch { return; }   // nicht lesbar: meldet ffprobe bei der normalen Prüfung
            if (gaps.Count == 0) return;
            MediaCheckItem it = Item(file, "gaps", "lückenhaft: " + MediaGap.Describe(gaps), facts);
            it.Gaps = gaps;
            found.Add(it);
        }

        static MediaCheckItem Item(string path, string kind, string problem, MediaFacts facts)
        {
            MediaCheckItem it = new MediaCheckItem();
            it.Path = path; it.Kind = kind; it.Problem = problem; it.Facts = facts;
            return it;
        }

        static bool HasPreview(string file)
        {
            string dir = Path.Combine(Path.GetDirectoryName(file), "Preview");
            string stem = Path.GetFileNameWithoutExtension(file);
            foreach (string ext in new string[] { ".jpg", ".jpeg", ".png", ".webp" })
                if (File.Exists(Path.Combine(dir, stem + ext))) return true;
            return false;
        }
    }

    // Ergebnis von "Medien prüfen": eine Zeile je Datei, Aktionen als Kästchen in den ersten drei Spalten
    // (Löschen, Konvertieren, Vorschaubild). "Ausführen" startet alle gewählten Dateioperationen.
    public class MediaCheckDialog : Form
    {
        const int ColDelete = 0, ColConvert = 1, ColPreview = 2;
        static readonly Glyph[] ActionIcons = { Glyph.Trash, Glyph.Convert, Glyph.Photo };

        readonly ListView list;
        readonly VirtualRows view;   // VirtualMode: Tausende Zeilen ohne Wartezeit (SFLIX-20261121-030)
        readonly Button runButton;
        readonly Label info;
        readonly List<MediaCheckFile> files;
        readonly List<ListViewItem> allRows = new List<ListViewItem>();
        readonly Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
        readonly TextBox search;
        readonly Timer searchDelay = new Timer();
        string filterKind;      // null = alle
        MediaCheckChoice result;

        static readonly string[] Kinds = { "broken", "gaps", "locked", "empty", "format", "preview", "folder" };

        static string KindName(string kind)
        {
            return kind == "broken" ? "Defekt" : kind == "gaps" ? "Lückenhaft" : kind == "locked" ? "Nicht lesbar" : kind == "empty" ? "Leer"
                 : kind == "format" ? "Nicht abspielbar" : kind == "folder" ? "Leere Ordner" : "Ohne Vorschaubild";
        }

        public static int Order(string kind)
        {
            return kind == "broken" ? 0 : kind == "gaps" ? 1 : kind == "locked" ? 2 : kind == "empty" ? 3 : kind == "format" ? 4 : kind == "preview" ? 5 : 6;
        }

        public MediaCheckDialog(List<MediaCheckItem> items)
        {
            files = MediaCheckFile.FromItems(items);
            Text = "Medien prüfen – Ergebnis";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1100), Theme.S(620));
            MinimumSize = new Size(Theme.S(760), Theme.S(420));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(files.Count + " Einträge mit Auffälligkeiten. Je Datei wählst du in den ersten drei Spalten: " +
                                          "Löschen (Papierkorb, die Datei kommt nach „" + MediaCheckTask.DeletedFolderName + "“), " +
                                          "Konvertieren (Videos zu MP4, Musik zu MP3, das Original kommt nach „" + ConvertTask.OriginalsFolderName + "“) " +
                                          "und Vorschaubild erstellen (Bildsymbol). Löschen schliesst die anderen aus. Leere Ordner (ohne Medien) " +
                                          "lassen sich nur löschen, sie kommen als Ganzes nach „" + MediaCheckTask.DeletedFolderName + "“ im Ordner darüber. " +
                                          "Vorausgewählt: defekte und leere Dateien sowie Ordner ohne andere Dateien löschen, sonst konvertieren und Vorschaubilder erstellen.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));
            Resize += delegate { intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0); };

            list = new ListView();
            list.View = View.Details;
            list.FullRowSelect = true;
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.ShowItemToolTips = true;
            DarkList.Apply(list);
            // Kästchen und Symbole der Aktionsspalten (nach DarkList, damit sie darüber gezeichnet werden)
            // Zeile nicht als Ganzes zeichnen, sonst kommt DrawSubItem nicht für jede Zelle
            list.DrawItem += delegate(object s, DrawListViewItemEventArgs e) { e.DrawDefault = false; };
            list.DrawSubItem += DrawActionCell;
            list.DrawColumnHeader += DrawActionHeader;
            list.MouseClick += OnListClick;
            list.KeyDown += OnListKey;
            list.Columns.Add("", Theme.S(46), HorizontalAlignment.Center);
            list.Columns.Add("", Theme.S(46), HorizontalAlignment.Center);
            list.Columns.Add("", Theme.S(46), HorizontalAlignment.Center);
            list.Columns.Add("Datei", Theme.S(280));
            list.Columns.Add("Problem", Theme.S(280));
            list.Columns.Add("Grösse", Theme.S(80));
            list.Columns.Add("Video / Ton", Theme.S(140));
            list.Columns.Add("Ordner", Theme.S(220));
            view = VirtualRows.Attach(list);

            // Zeilen einmal erzeugen, Filter und Suche zeigen davon nur einen Teil
            List<MediaCheckFile> sorted = new List<MediaCheckFile>(files);
            sorted.Sort(delegate(MediaCheckFile a, MediaCheckFile b)
            {
                int k = a.Order.CompareTo(b.Order);
                return k != 0 ? k : string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });
            Dictionary<string, string> folders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (MediaCheckFile f in sorted)
            {
                ListViewItem row = new ListViewItem("");
                row.SubItems.Add("");
                row.SubItems.Add("");
                row.SubItems.Add(Path.GetFileName(f.Path));
                ListSort.Add(row, f.ProblemText, f.Order + " " + f.ProblemText);
                ListSort.Add(row, Fmt.Size(Math.Max(0, f.Facts.Size)), Math.Max(0, f.Facts.Size));
                row.SubItems.Add(f.Facts.CodecText);
                string dir = Path.GetDirectoryName(f.Path), rel;
                if (!folders.TryGetValue(dir, out rel)) folders[dir] = rel = DataTask.ToRelative(dir) ?? dir;
                row.SubItems.Add(rel);
                row.ToolTipText = f.Path;
                row.Tag = f;
                // Farbe je Zelle setzen: beim Zeichnen Zelle für Zelle gilt die Farbe der Zeile nicht
                row.UseItemStyleForSubItems = false;
                Color color = f.Has("broken") || f.Has("empty") || f.Has("gaps") ? Color.FromArgb(0xff, 0x6b, 0x6b)
                            : f.Has("format") || f.Has("locked") ? Color.FromArgb(0xf0, 0xc4, 0x6c)
                            : f.Has("folder") ? Theme.Muted : Theme.Text;
                foreach (ListViewItem.ListViewSubItem sub in row.SubItems) sub.ForeColor = color;
                UpdateKeys(row);
                allRows.Add(row);
            }

            // Filter nach Art (mit Anzahl) und Suche nach Datei oder Ordner
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Dock = DockStyle.Top;
            filterBar.AutoSize = true;
            filterBar.WrapContents = true;
            filterBar.Padding = new Padding(Theme.S(12), 0, Theme.S(12), Theme.S(8));
            AddFilterButton(filterBar, null, "Alle (" + files.Count + ")");
            foreach (string kind in Kinds)
            {
                int n = 0;
                foreach (MediaCheckFile f in files) if (f.Has(kind)) n++;
                if (n > 0) AddFilterButton(filterBar, kind, KindName(kind) + " (" + n + ")");
            }
            Label searchLabel = Theme.MakeLabel("Suche:", true);
            searchLabel.Margin = new Padding(Theme.S(16), Theme.S(8), Theme.S(6), 0);
            search = Theme.MakeTextBox();
            search.Width = Theme.S(240);
            search.Margin = new Padding(0, Theme.S(4), 0, 0);
            searchDelay.Interval = 250;
            searchDelay.Tick += delegate { searchDelay.Stop(); ApplyFilter(); };
            search.TextChanged += delegate { searchDelay.Stop(); searchDelay.Start(); };
            filterBar.Controls.Add(searchLabel);
            filterBar.Controls.Add(search);
            FormClosed += delegate { searchDelay.Dispose(); };

            Panel listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            listHost.Controls.Add(list);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button close = Theme.MakeButton("Schliessen", false);
            close.Click += delegate { result = null; DialogResult = DialogResult.Cancel; };
            runButton = Theme.MakeButton("Ausführen", true);
            runButton.Click += delegate { Finish(); };
            Button suggest = Theme.MakeButton("Vorschlag", false);
            suggest.Click += delegate { SetVisible(true); };
            Button none = Theme.MakeButton("Keine", false);
            none.Click += delegate { SetVisible(false); };
            info = Theme.MakeLabel("", true);
            info.Margin = new Padding(0, Theme.S(8), Theme.S(12), 0);
            buttons.Controls.Add(close);
            buttons.Controls.Add(runButton);
            buttons.Controls.Add(none);
            buttons.Controls.Add(suggest);
            buttons.Controls.Add(info);

            Controls.Add(listHost);
            Controls.Add(buttons);
            Controls.Add(filterBar);
            Controls.Add(intro);
            CancelButton = close;
            ApplyFilter();
        }

        // ---------------------------------------------------------------
        // Aktionsspalten
        // ---------------------------------------------------------------

        static bool Applicable(MediaCheckFile f, int col)
        {
            return col == ColDelete || (col == ColConvert && f.CanConvert) || (col == ColPreview && f.CanPreview);
        }

        static bool IsSet(MediaCheckFile f, int col)
        {
            return col == ColDelete ? f.Delete : col == ColConvert ? f.Convert : f.Preview;
        }

        // Aktion setzen: Löschen schliesst die anderen aus und umgekehrt
        static void Set(MediaCheckFile f, int col, bool value)
        {
            if (!Applicable(f, col)) return;
            if (col == ColDelete)
            {
                f.Delete = value;
                if (value) { f.Convert = false; f.Preview = false; }
            }
            else
            {
                if (col == ColConvert) f.Convert = value; else f.Preview = value;
                if (value) f.Delete = false;
            }
        }

        // Sortierschlüssel der Aktionsspalten: gewählt zuerst, dann möglich, dann nicht möglich
        static void UpdateKeys(ListViewItem row)
        {
            MediaCheckFile f = (MediaCheckFile)row.Tag;
            for (int c = ColDelete; c <= ColPreview; c++)
                row.SubItems[c].Tag = !Applicable(f, c) ? 2 : IsSet(f, c) ? 0 : 1;
        }

        void OnListClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            ListViewHitTestInfo hit = list.HitTest(e.Location);
            if (hit.Item == null || hit.SubItem == null) return;
            int col = hit.Item.SubItems.IndexOf(hit.SubItem);
            if (col < ColDelete || col > ColPreview) return;
            Toggle(new ListViewItem[] { hit.Item }, col);
        }

        // Tastatur für die markierten Zeilen: Entf = löschen, K = konvertieren, V = Vorschaubild (jeweils umschalten)
        void OnListKey(object sender, KeyEventArgs e)
        {
            int col = e.KeyCode == Keys.Delete ? ColDelete : e.KeyCode == Keys.K ? ColConvert : e.KeyCode == Keys.V ? ColPreview : -1;
            if (col < 0) return;
            List<ListViewItem> rows = view.Selected();
            if (rows.Count == 0) return;
            Toggle(rows.ToArray(), col);
            e.Handled = true;
        }

        void Toggle(ListViewItem[] rows, int col)
        {
            // Neuer Wert nach der ersten Zeile, für die die Aktion möglich ist
            bool? value = null;
            foreach (ListViewItem row in rows)
            {
                MediaCheckFile f = (MediaCheckFile)row.Tag;
                if (Applicable(f, col)) { value = !IsSet(f, col); break; }
            }
            if (value == null) return;
            foreach (ListViewItem row in rows)
            {
                Set((MediaCheckFile)row.Tag, col, value.Value);
                UpdateKeys(row);
            }
            list.Invalidate();   // VirtualMode: Bounds einer Zeile nur über ihren Index, ganz neu zeichnen ist billig
            UpdateButtons();
        }

        void DrawActionCell(object sender, DrawListViewSubItemEventArgs e)
        {
            if (e.ColumnIndex < ColDelete || e.ColumnIndex > ColPreview) return;
            e.DrawDefault = false;
            MediaCheckFile f = e.Item.Tag as MediaCheckFile;
            bool selected = e.Item.Selected;
            using (SolidBrush b = new SolidBrush(selected ? DarkList.SelectionBack(list) : list.BackColor))
                e.Graphics.FillRectangle(b, e.Bounds);
            if (f == null || !Applicable(f, e.ColumnIndex)) return;
            int size = Theme.S(15);
            Rectangle box = new Rectangle(e.Bounds.X + (e.Bounds.Width - size) / 2, e.Bounds.Y + (e.Bounds.Height - size) / 2, size, size);
            bool on = IsSet(f, e.ColumnIndex);
            Color fill = e.ColumnIndex == ColDelete ? Theme.Red : Color.FromArgb(0x3d, 0x8b, 0x5a);
            SmoothingMode old = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (on)
            {
                using (SolidBrush b = new SolidBrush(fill)) e.Graphics.FillRectangle(b, box);
                using (Pen p = new Pen(Color.White, Math.Max(1.5f, Theme.S(2))))
                {
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                    e.Graphics.DrawLines(p, new PointF[] {
                        new PointF(box.Left + size * 0.22f, box.Top + size * 0.52f),
                        new PointF(box.Left + size * 0.42f, box.Top + size * 0.72f),
                        new PointF(box.Left + size * 0.78f, box.Top + size * 0.30f) });
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Theme.Input)) e.Graphics.FillRectangle(b, box);
                using (Pen p = new Pen(Theme.Muted)) e.Graphics.DrawRectangle(p, box);
            }
            e.Graphics.SmoothingMode = old;
        }

        void DrawActionHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.ColumnIndex < ColDelete || e.ColumnIndex > ColPreview) return;
            // Symbol links (rechts steht bei Sortierung das Dreieck)
            int size = Theme.S(16);
            Rectangle r = new Rectangle(e.Bounds.X + Theme.S(7), e.Bounds.Y + (e.Bounds.Height - size) / 2, size, size);
            Glyphs.Draw(e.Graphics, ActionIcons[e.ColumnIndex], r, Theme.Text, Theme.Input);
        }

        // ---------------------------------------------------------------
        // Filter, Sammelauswahl, Ausführen
        // ---------------------------------------------------------------

        void AddFilterButton(FlowLayoutPanel bar, string kind, string text)
        {
            Button b = Theme.MakeButton(text, false);
            b.Margin = new Padding(0, 0, Theme.S(6), 0);
            b.Click += delegate { filterKind = kind; ApplyFilter(); };
            filterButtons[kind ?? ""] = b;
            bar.Controls.Add(b);
        }

        // Nur die Zeilen der gewählten Art bzw. mit dem Suchtext zeigen (Aktionen bleiben erhalten)
        void ApplyFilter()
        {
            foreach (KeyValuePair<string, Button> p in filterButtons)
            {
                bool active = p.Key == (filterKind ?? "");
                p.Value.BackColor = active ? Theme.Line : Theme.Input;
                p.Value.FlatAppearance.BorderColor = active ? Theme.Muted : Theme.Line;
            }
            string q = search.Text.Trim();
            List<ListViewItem> shown = new List<ListViewItem>();
            foreach (ListViewItem row in allRows)
            {
                MediaCheckFile f = (MediaCheckFile)row.Tag;
                if (filterKind != null && !f.Has(filterKind)) continue;
                if (q.Length > 0 && f.Path.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                shown.Add(row);
            }
            // VirtualMode: nur die Datenliste ersetzen, die ListView fragt die sichtbaren Zeilen ab
            view.SetRows(shown);
            ListSort.Of(list).Apply();
            UpdateButtons();
        }

        // Für alle angezeigten Zeilen: Vorschlag (true) bzw. keine Aktion (false)
        void SetVisible(bool suggest)
        {
            foreach (ListViewItem row in view.Rows)
            {
                MediaCheckFile f = (MediaCheckFile)row.Tag;
                if (suggest) f.Suggest();
                else { f.Delete = false; f.Convert = false; f.Preview = false; }
                UpdateKeys(row);
            }
            list.Invalidate();
            UpdateButtons();
        }

        MediaCheckChoice BuildChoice()
        {
            MediaCheckChoice c = new MediaCheckChoice();
            foreach (MediaCheckFile f in files)
            {
                if (f.Delete)
                {
                    c.Delete.Add(f.Path);
                    if (f.Has("gaps")) c.Gaps.Add(f.Path);
                }
                else
                {
                    if (f.Convert) c.Convert.Add(f.Path);
                    if (f.Preview) c.Preview.Add(f.Path);
                }
            }
            return c;
        }

        void UpdateButtons()
        {
            if (IsDisposed || Disposing || runButton == null || info == null) return;
            MediaCheckChoice c = BuildChoice();
            List<string> parts = new List<string>();
            if (c.Delete.Count > 0) parts.Add(c.Delete.Count + " löschen");
            if (c.Convert.Count > 0) parts.Add(c.Convert.Count + " konvertieren");
            if (c.Preview.Count > 0) parts.Add(c.Preview.Count + " Vorschaubild" + (c.Preview.Count == 1 ? "" : "er"));
            runButton.Enabled = c.Count > 0;
            runButton.Text = c.Count > 0 ? "Ausführen: " + string.Join(", ", parts.ToArray()) : "Ausführen";
            info.Text = view.Rows.Count + " von " + files.Count + " angezeigt";
        }

        void Finish()
        {
            MediaCheckChoice c = BuildChoice();
            if (c.Count == 0) return;
            if (c.Delete.Count > 0 &&
                MessageBox.Show(this, c.Delete.Count + " Datei(en) bzw. Ordner kommen (Dateien samt Vorschaubild) nach „" + MediaCheckTask.DeletedFolderName +
                                "“. Endgültig löscht sie erst „Ablagen leeren“. Fortfahren?",
                                App.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;
            result = c;
            DialogResult = DialogResult.OK;
        }

        public MediaCheckChoice Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowDialog(owner);
            return result;
        }
    }
}
