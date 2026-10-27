// Aufgabe "Medien importieren": kopiert oder verschiebt Videos, Musik und Bilder in eine
// Kategorie des STEFFLIX-Ordners (z.B. Serien\Meine Serie\Staffel 1).
//
// - Ordner werden mitsamt ihrer Struktur übernommen (abschaltbar, dann landen alle Dateien
//   direkt im Zielordner).
// - Nicht browserkompatible Dateien werden konvertiert: Videos zu MP4 (H.264/AAC, wie
//   "Videos für STEFFLIX konvertieren"), Musik zu MP3, Bilder zu JPG. Mit "Konvertieren" werden
//   zusätzlich MP4-Videos mit unpassenden Codecs umgewandelt.
// - Kopieren (Standard): das Original bleibt am Ursprungsort.
//   Verschieben: das Original wird verschoben; wurde es konvertiert, kommt es nach
//   <Zielordner>\__konvertiert. Leer gewordene Quellordner werden entfernt.
// - Gibt es eine Zieldatei schon, wird nachgefragt: ersetzen oder überspringen (merkbar).

using System;
using System.Collections.Generic;
using System.IO;

namespace Sflix
{
    public class ImportOptions
    {
        public List<string> Sources = new List<string>();
        public string Category;       // Ordner im STEFFLIX-Ordner, z.B. "Serien"
        public string SubFolder = ""; // relativ zur Kategorie, z.B. "Meine Serie\Staffel 1"
        public bool KeepStructure = true;
        public bool Convert;
        public bool Move;
        public ConvertOptions Video = new ConvertOptions();
    }

    public class ConflictAnswer
    {
        public bool Replace;
        public bool Remember;
    }

    // Dateitypen: was STEFFLIX direkt abspielt und was erst konvertiert werden muss
    public static class MediaTypes
    {
        public static readonly HashSet<string> ReadyVideo = Set(".mp4", ".m4v", ".webm", ".ogv");
        public static readonly HashSet<string> ReadyAudio = Set(".mp3", ".m4a", ".ogg", ".oga", ".opus", ".aac", ".flac", ".wav");
        public static readonly HashSet<string> ReadyImage = Set(".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp");
        public static readonly HashSet<string> ConvertVideo = Set(".mkv", ".avi", ".mov", ".wmv", ".flv", ".ts", ".m2ts", ".mts",
                                                                  ".mpeg", ".mpg", ".3gp", ".vob", ".divx", ".asf", ".f4v");
        public static readonly HashSet<string> ConvertAudio = Set(".wma", ".aiff", ".aif", ".ape", ".ac3", ".dts", ".amr", ".mka", ".wv");
        public static readonly HashSet<string> ConvertImage = Set(".tif", ".tiff", ".heic", ".heif", ".jfif");

        static HashSet<string> Set(params string[] items)
        {
            return new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
        }

        public static string Kind(string ext)
        {
            if (ReadyVideo.Contains(ext) || ConvertVideo.Contains(ext)) return "video";
            if (ReadyAudio.Contains(ext) || ConvertAudio.Contains(ext)) return "audio";
            if (ReadyImage.Contains(ext) || ConvertImage.Contains(ext)) return "image";
            return null;
        }

        public static bool NeedsConversion(string ext)
        {
            return ConvertVideo.Contains(ext) || ConvertAudio.Contains(ext) || ConvertImage.Contains(ext);
        }
    }

    public class ImportItem
    {
        public string Source;
        public string RelDir = "";   // Unterordner im Ziel (Struktur der Quelle)
    }

    public class ImportTask
    {
        public const string OriginalsFolderName = ConvertTask.OriginalsFolderName;

        readonly TaskContext ctx;
        readonly ImportOptions opt;
        ConvertTask converter;
        bool? remembered;   // gemerkte Antwort bei vorhandenen Zieldateien: true = ersetzen
        int imported, converted, skipped, failed;

        ImportTask(TaskContext ctx, ImportOptions opt)
        {
            this.ctx = ctx;
            this.opt = opt;
        }

        public static void Run(TaskContext ctx, ImportOptions opt)
        {
            new ImportTask(ctx, opt).Execute();
        }

        // ---------------------------------------------------------------
        // Planung (auch von der Oberfläche für den Hinweis genutzt)
        // ---------------------------------------------------------------

        // Alle Mediendateien der Auswahl mit ihrem Unterordner im Ziel
        public static List<ImportItem> Plan(IList<string> sources, bool keepStructure, out int ignored)
        {
            ignored = 0;
            List<ImportItem> items = new List<ImportItem>();
            List<string> files = new List<string>();
            foreach (string s in sources)
            {
                if (File.Exists(s)) files.Add(Path.GetFullPath(s));
            }

            // Einzelne Dateien: Struktur relativ zu ihrem gemeinsamen Ordner
            string common = CommonDirectory(files);
            foreach (string f in files)
            {
                if (MediaTypes.Kind(Path.GetExtension(f)) == null) { ignored++; continue; }
                ImportItem item = new ImportItem();
                item.Source = f;
                item.RelDir = keepStructure && common != null ? Relative(common, Path.GetDirectoryName(f)) : "";
                items.Add(item);
            }

            // Ordner: der gewählte Ordner selbst wird mit übernommen
            foreach (string s in sources)
            {
                if (!Directory.Exists(s)) continue;
                string folder = Path.GetFullPath(s);
                if (Path.GetPathRoot(folder) != folder) folder = folder.TrimEnd('\\');   // D:\ bleibt D:\
                string baseDir = Path.GetDirectoryName(folder) ?? folder;
                Walk(folder, baseDir, keepStructure, items, ref ignored);
            }
            return items;
        }

        static void Walk(string dir, string baseDir, bool keepStructure, List<ImportItem> items, ref int ignored)
        {
            string[] files, dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException) { return; }
            catch (IOException) { return; }

            bool isPreview = string.Equals(Path.GetFileName(dir), "Preview", StringComparison.OrdinalIgnoreCase);
            foreach (string f in files)
            {
                if (MediaTypes.Kind(Path.GetExtension(f)) == null) { ignored++; continue; }
                // Ohne Struktur würden Vorschaubilder zwischen den Medien landen
                if (isPreview && !keepStructure) continue;
                ImportItem item = new ImportItem();
                item.Source = f;
                item.RelDir = keepStructure ? Relative(baseDir, dir) : "";
                items.Add(item);
            }
            foreach (string d in dirs)
            {
                if (Folders.Skip(Path.GetFileName(d))) continue;
                Walk(d, baseDir, keepStructure, items, ref ignored);
            }
        }

        static string CommonDirectory(List<string> files)
        {
            if (files.Count == 0) return null;
            string common = Path.GetDirectoryName(files[0]);
            foreach (string f in files)
            {
                string d = Path.GetDirectoryName(f);
                while (common != null && !(d + "\\").StartsWith(common.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                    common = Path.GetDirectoryName(common);
            }
            return common;
        }

        static string Relative(string baseDir, string dir)
        {
            string b = baseDir.TrimEnd('\\') + "\\";
            string d = dir.TrimEnd('\\') + "\\";
            if (!d.StartsWith(b, StringComparison.OrdinalIgnoreCase)) return "";
            return d.Substring(b.Length).TrimEnd('\\');
        }

        // ---------------------------------------------------------------
        // Ausführung
        // ---------------------------------------------------------------

        void Execute()
        {
            if (opt.Sources.Count == 0) throw new TaskFailedException("Keine Dateien oder Ordner zum Importieren ausgewählt.");
            if (string.IsNullOrEmpty(opt.Category)) throw new TaskFailedException("Keine Kategorie gewählt.");
            string targetRoot = Path.Combine(App.Root, opt.Category, (opt.SubFolder ?? "").Trim().Trim('\\'));

            int ignored;
            List<ImportItem> items = Plan(opt.Sources, opt.KeepStructure, out ignored);
            ctx.Log(items.Count + " Mediendateien gefunden" + (ignored > 0 ? ", " + ignored + " andere Dateien werden nicht importiert" : ""));
            ctx.Log("Ziel: " + targetRoot + (opt.Move ? "  (verschieben)" : "  (kopieren)") + (opt.Convert ? ", konvertieren" : ""));
            if (items.Count == 0) return;

            bool needsFfmpeg = opt.Convert;
            foreach (ImportItem it in items) if (MediaTypes.NeedsConversion(Path.GetExtension(it.Source))) needsFfmpeg = true;
            if (needsFfmpeg) ctx.Ffmpeg.EnsureInstalled(ctx);

            for (int i = 0; i < items.Count; i++)
            {
                ctx.Check();
                ImportItem it = items[i];
                double baseFraction = (double)i / items.Count;
                ctx.Progress(baseFraction, "Importiere " + (i + 1) + " / " + items.Count + ": " + Path.GetFileName(it.Source));
                try
                {
                    ImportOne(it, Path.Combine(targetRoot, it.RelDir), delegate(double f)
                    {
                        ctx.Progress(baseFraction + Math.Min(1, f) / items.Count,
                                     "Konvertiere " + (i + 1) + " / " + items.Count + ": " + Path.GetFileName(it.Source) + " (" + (int)(f * 100) + " %)");
                    });
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception e)
                {
                    failed++;
                    ctx.Log("  FEHLER  " + it.Source + " :: " + e.Message);
                }
            }

            if (opt.Move) RemoveEmptySourceFolders();

            ctx.Log("Importiert: " + imported + " (davon konvertiert: " + converted + ")   Übersprungen: " + skipped + "   Fehler: " + failed);
            if (failed > 0) throw new TaskFailedException(failed + " Datei(en) konnten nicht importiert werden");
        }

        void ImportOne(ImportItem it, string targetDir, Action<double> progress)
        {
            string src = it.Source;
            string ext = Path.GetExtension(src).ToLowerInvariant();
            string kind = MediaTypes.Kind(ext);
            string stem = Path.GetFileNameWithoutExtension(src);
            bool mustConvert = MediaTypes.NeedsConversion(ext);

            if (mustConvert && !opt.Convert)
            {
                skipped++;
                ctx.Log("  ÜBERSPRUNGEN  " + src + " :: nicht browserkompatibel, Konvertieren ist ausgeschaltet");
                return;
            }

            if (kind == "video" && opt.Convert)
            {
                MediaInfo info = ctx.Ffmpeg.Probe(src, ctx.Token);
                bool ready = !mustConvert && (ext == ".webm" || ext == ".ogv" || ConvertTask.IsBrowserReady(info, ext));
                if (!ready)
                {
                    if (converter == null) converter = ConvertTask.ForImport(ctx, opt.Video);
                    List<string> results = converter.ConvertInto(info, targetDir, stem, AskReplace, progress);
                    if (results == null) { skipped++; ctx.Log("  ÜBERSPRUNGEN  " + src + " :: Zieldatei vorhanden"); return; }
                    FinishConverted(src, targetDir, results);
                    return;
                }
            }
            else if (kind == "audio" && mustConvert)
            {
                ConvertSingle(src, targetDir, stem + ".mp3", new string[] {
                    "-map", "0:a:0", "-c:a", "libmp3lame", "-q:a", "2", "-map_metadata", "0", "-id3v2_version", "3" });
                return;
            }
            else if (kind == "image" && mustConvert)
            {
                ConvertSingle(src, targetDir, stem + ".jpg", new string[] { "-frames:v", "1", "-q:v", "2" });
                return;
            }

            Transfer(src, Path.Combine(targetDir, Path.GetFileName(src)));
        }

        // Musik oder Bild mit ffmpeg in eine Zieldatei umwandeln
        void ConvertSingle(string src, string targetDir, string targetName, string[] codecArgs)
        {
            string target = Path.Combine(targetDir, targetName);
            if (FileOps.Exists(target) && !AskReplace(target))
            {
                skipped++;
                ctx.Log("  ÜBERSPRUNGEN  " + src + " :: " + targetName + " ist schon vorhanden");
                return;
            }
            Directory.CreateDirectory(targetDir);
            string temp = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(targetName) + ".__temp__" + Path.GetExtension(targetName));
            List<string> args = new List<string> { "-hide_banner", "-nostdin", "-loglevel", "error", "-y", "-i", src };
            args.AddRange(codecArgs);
            args.Add(temp);
            try
            {
                ProcResult r = Proc.Run(ctx.Ffmpeg.FfmpegPath, args, ctx.Token, null);
                if (r.ExitCode != 0 || !File.Exists(temp) || new FileInfo(temp).Length == 0)
                {
                    string err = r.StdErr.Trim();
                    if (err.Length > 300) err = err.Substring(err.Length - 300);
                    throw new Exception("Konvertierung fehlgeschlagen" + (err.Length > 0 ? ": " + err : ""));
                }
                FileOps.Replace(temp, target);
            }
            catch
            {
                FileOps.TryDelete(temp);
                throw;
            }
            FinishConverted(src, targetDir, new List<string> { target });
        }

        // Nach einer Konvertierung: Original beim Verschieben nach __konvertiert, sonst liegen lassen
        void FinishConverted(string src, string targetDir, List<string> results)
        {
            imported++;
            converted++;
            string note = "";
            if (opt.Move)
            {
                string storage = Path.Combine(targetDir, OriginalsFolderName);
                Directory.CreateDirectory(storage);
                FileOps.MoveAcross(src, FileOps.UniquePath(storage, Path.GetFileName(src)), false);
                note = "  (Original nach " + OriginalsFolderName + ")";
            }
            List<string> names = results.ConvertAll(Path.GetFileName);
            ctx.Log("  KONVERTIERT  " + src + "  ->  " + string.Join(", ", names) + note);
        }

        void Transfer(string src, string target)
        {
            if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                skipped++;
                ctx.Log("  ÜBERSPRUNGEN  " + src + " :: liegt bereits am Ziel");
                return;
            }
            bool replace = false;
            if (FileOps.Exists(target))
            {
                if (!AskReplace(target))
                {
                    skipped++;
                    ctx.Log("  ÜBERSPRUNGEN  " + src + " :: " + Path.GetFileName(target) + " ist schon vorhanden");
                    return;
                }
                replace = true;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (opt.Move)
            {
                FileOps.MoveAcross(src, target, replace);
            }
            else
            {
                // Erst als Zwischendatei kopieren, damit nie eine halbe Datei am Ziel liegt
                string temp = target + ".__temp__";
                try
                {
                    File.Copy(src, temp, true);
                    FileOps.Replace(temp, target);
                }
                catch
                {
                    FileOps.TryDelete(temp);
                    throw;
                }
            }
            imported++;
            ctx.Log("  " + (opt.Move ? "VERSCHOBEN" : "KOPIERT") + "  " + src + "  ->  " + target);
        }

        // Zieldatei vorhanden: nachfragen (oder die gemerkte Antwort verwenden)
        bool AskReplace(string target)
        {
            if (remembered.HasValue) return remembered.Value;
            ConflictAnswer a = ctx.AskConflict != null ? ctx.AskConflict(target) : null;
            if (a == null) return false;
            if (a.Remember) remembered = a.Replace;
            return a.Replace;
        }

        // Beim Verschieben: leer gewordene Ordner der Auswahl entfernen (nur wirklich leere)
        void RemoveEmptySourceFolders()
        {
            foreach (string s in opt.Sources)
                if (Directory.Exists(s)) RemoveIfEmpty(Path.GetFullPath(s));
        }

        static bool RemoveIfEmpty(string dir)
        {
            bool empty = true;
            try
            {
                foreach (string d in Directory.GetDirectories(dir))
                    if (!RemoveIfEmpty(d)) empty = false;
                if (Directory.GetFiles(dir).Length > 0) empty = false;
                if (empty) Directory.Delete(dir, false);
            }
            catch { return false; }
            return empty;
        }
    }
}
