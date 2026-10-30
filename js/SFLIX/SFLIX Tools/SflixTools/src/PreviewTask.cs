// Aufgabe "Vorschaubilder erstellen" (Portierung von PreviewMaker).
//
// Erstellt wahlweise für Videos (Bild aus der Mitte) und/oder Bilder (zugeschnitten) ein
// 480x270-JPEG (Qualität 80) unter <Ordner>\Preview\<Dateiname>.jpg. Vorhandene
// Vorschaubilder bleiben unverändert. Vorschaubilder ohne gleichnamige Mediendatei (Video,
// Musik, Bild) bzw. ohne gleichnamigen Ordner kommen nach Preview\__gelöscht.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace Sflix
{
    public static class PreviewTask
    {
        const int Width = 480;
        const int Height = 270;
        const long JpegQuality = 80;
        static readonly HashSet<string> VideoExtensions = new HashSet<string> { ".mp4", ".m4v", ".webm", ".ogv" };
        static readonly HashSet<string> ImageExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp" };
        // Ein Vorschaubild gehört zu einer Datei gleichen Namens mit einer dieser Endungen (oder zu einem Ordner)
        static readonly string[] MediaExtensions = {
            ".mp4", ".m4v", ".webm", ".ogv",
            ".mp3", ".m4a", ".ogg", ".oga", ".opus", ".aac", ".flac", ".wav",
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif", ".bmp",
        };
        const string TrashDirName = "__gelöscht";

        public static void Run(TaskContext ctx, string root, bool forVideos, bool forImages)
        {
            if (!Directory.Exists(root)) throw new TaskFailedException("Ordner nicht gefunden: " + root);
            if (!forVideos && !forImages) throw new TaskFailedException("Weder „für Videos“ noch „für Bilder“ ausgewählt");
            ctx.Ffmpeg.EnsureInstalled(ctx);

            ctx.Progress(-1, "Suche Medien ...");
            HashSet<string> wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (forVideos) wanted.UnionWith(VideoExtensions);
            if (forImages) wanted.UnionWith(ImageExtensions);
            List<string> videos = new List<string>();
            List<string> previewDirs = new List<string>();
            Collect(root, wanted, videos, previewDirs, ctx);
            int imageCount = videos.FindAll(delegate(string f) { return ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()); }).Count;
            ctx.Log((forVideos ? (videos.Count - imageCount) + " Videos" : "") + (forVideos && forImages ? " und " : "") +
                    (forImages ? imageCount + " Bilder" : "") + " gefunden in " + root);

            int failed = 0;
            int deleted = CleanOrphans(previewDirs, ctx, ref failed);

            int created, skipped, makeFailed;
            MakeMany(ctx, videos, out created, out skipped, out makeFailed);
            failed += makeFailed;

            ctx.Log("Erstellt: " + created + "   Schon vorhanden: " + skipped +
                    "   Nach " + TrashDirName + " verschoben: " + deleted + "   Fehler: " + failed);
            ctx.Summary = created + " erstellt, " + skipped + " vorhanden, " + deleted + " verschoben, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Vorschaubild(er) konnten nicht erstellt werden");
        }

        // Vorschaubilder für bestimmte Dateien (z.B. aus "Medien prüfen"). Vorhandene bleiben.
        public static void MakeFor(TaskContext ctx, List<string> files)
        {
            ctx.Ffmpeg.EnsureInstalled(ctx);
            int created, skipped, failed;
            MakeMany(ctx, files, out created, out skipped, out failed);
            ctx.Log("Vorschaubilder erstellt: " + created + "   Fehler: " + failed);
            if (failed > 0) throw new TaskFailedException(failed + " Vorschaubild(er) konnten nicht erstellt werden");
        }

        // Erstellt fehlende Vorschaubilder, seit 3.4.2 parallel (automatisch 2: ffmpeg dekodiert je ein Bild).
        // Vorhandene bleiben unverändert (z.B. von einem Video neben einem gleichnamigen Bild).
        // Das Protokoll erscheint in der Reihenfolge der Dateien, auch wenn sie anders fertig werden.
        static void MakeMany(TaskContext ctx, List<string> files, out int created, out int skipped, out int failed)
        {
            ImageCodecInfo jpeg = null;
            foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                if (c.MimeType == "image/jpeg") jpeg = c;
            EncoderParameters enc = new EncoderParameters(1);
            enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);

            string tmpBase = Path.Combine(Path.GetTempPath(), "sflix-preview-" + Process.GetCurrentProcess().Id + "-");
            string[] logLines = new string[files.Count];
            int[] result = new int[files.Count];          // 1 erstellt, 2 vorhanden, 3 Fehler
            object logSync = new object();
            int nextLog = 0, done = 0;
            try
            {
                Workers.ForEach(files, Workers.Degree(ctx, 2), ctx.Token, delegate(int i, string file)
                {
                    string previewDir = Path.Combine(Path.GetDirectoryName(file), "Preview");
                    string target = Path.Combine(previewDir, Path.GetFileNameWithoutExtension(file) + ".jpg");
                    string line = "";
                    if (File.Exists(target))
                    {
                        result[i] = 2;
                    }
                    else
                    {
                        // Eigene Zwischendatei je Thread
                        string tmpBmp = tmpBase + System.Threading.Thread.CurrentThread.ManagedThreadId + ".bmp";
                        try
                        {
                            MakePreview(file, previewDir, target, tmpBmp, jpeg, enc, ctx);
                            result[i] = 1;
                            line = "  OK      " + file;
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            result[i] = 3;
                            line = "  FEHLER  " + file + " :: " + ex.Message;
                        }
                        finally
                        {
                            FileOps.TryDelete(tmpBmp);
                        }
                    }
                    lock (logSync)
                    {
                        logLines[i] = line;
                        while (nextLog < files.Count && logLines[nextLog] != null)
                        {
                            if (logLines[nextLog].Length > 0) ctx.Log(logLines[nextLog]);
                            nextLog++;
                        }
                        done++;
                        ctx.Progress((double)done / Math.Max(1, files.Count), "Vorschaubild " + done + " / " + files.Count);
                    }
                });
            }
            finally
            {
                MediaCache.Save();
            }
            created = 0; skipped = 0; failed = 0;
            foreach (int r in result)
            {
                if (r == 1) created++;
                else if (r == 2) skipped++;
                else if (r == 3) failed++;
            }
        }

        static void Collect(string dir, HashSet<string> wanted, List<string> result, List<string> previewDirs, TaskContext ctx)
        {
            ctx.Check();
            try
            {
                // Die Bilder in Preview-Ordnern sind selbst Vorschaubilder und bekommen keine
                bool isPreview = string.Equals(Path.GetFileName(dir), "Preview", StringComparison.OrdinalIgnoreCase);
                if (!isPreview)
                    foreach (string f in Directory.GetFiles(dir))
                        if (wanted.Contains(Path.GetExtension(f).ToLowerInvariant())) result.Add(f);
                foreach (string d in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(d);
                    // __teile, __konvertiert, __gelöscht usw. bekommen keine Vorschaubilder
                    if (Folders.Ignore(d)) continue;
                    if (string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase)) previewDirs.Add(d);
                    Collect(d, wanted, result, previewDirs, ctx);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        // Verschiebt Preview\<name>.jpg nach Preview\__gelöscht, wenn neben dem Preview-Ordner
        // weder eine Mediendatei <name>.<endung> (Video, Musik, Bild) noch ein Ordner <name> liegt.
        static int CleanOrphans(List<string> previewDirs, TaskContext ctx, ref int failed)
        {
            int deleted = 0;
            foreach (string dir in previewDirs)
            {
                string parent = Path.GetDirectoryName(dir);
                string trash = Path.Combine(dir, TrashDirName);
                string[] files;
                try { files = Directory.GetFiles(dir); } catch { continue; }

                foreach (string jpg in files)
                {
                    if (!string.Equals(Path.GetExtension(jpg), ".jpg", StringComparison.OrdinalIgnoreCase)) continue;
                    string baseName = Path.GetFileNameWithoutExtension(jpg);
                    bool keep = Directory.Exists(Path.Combine(parent, baseName));
                    foreach (string ext in MediaExtensions)
                        if (!keep && File.Exists(Path.Combine(parent, baseName + ext))) keep = true;
                    if (keep) continue;

                    try
                    {
                        Directory.CreateDirectory(trash);
                        string dest = Path.Combine(trash, Path.GetFileName(jpg));
                        if (File.Exists(dest))
                            dest = Path.Combine(trash, baseName + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg");
                        FileOps.Rename(jpg, dest);
                        deleted++;
                        ctx.Log("  VERSCHOBEN  " + jpg + "  ->  " + TrashDirName);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        ctx.Log("  FEHLER beim Verschieben  " + jpg + " :: " + ex.Message);
                    }
                }
            }
            return deleted;
        }

        static void MakePreview(string video, string previewDir, string target, string tmpBmp,
                                ImageCodecInfo jpeg, EncoderParameters enc, TaskContext ctx)
        {
            string vf = "scale=" + Width + ":" + Height + ":force_original_aspect_ratio=increase,crop=" + Width + ":" + Height + ",setsar=1";
            FileOps.TryDelete(tmpBmp);

            // Bilder: direkt zuschneiden (bei animierten Bildern das erste Bild)
            if (ImageExtensions.Contains(Path.GetExtension(video).ToLowerInvariant()))
            {
                Proc.Run(ctx.Ffmpeg.FfmpegPath, new string[] {
                    "-v", "error", "-y", "-i", video, "-frames:v", "1", "-vf", vf, tmpBmp }, ctx.Token, null);
            }
            else
            {
                // Videos: Bild aus der Mitte
                double duration = 0;
                try { duration = ctx.Ffmpeg.Probe(video, ctx.Token).Duration; }
                catch (OperationCanceledException) { throw; }
                catch { }
                double mid = duration / 2.0;
                Proc.Run(ctx.Ffmpeg.FfmpegPath, new string[] {
                    "-v", "error", "-y", "-ss", mid.ToString("0.###", CultureInfo.InvariantCulture),
                    "-i", video, "-frames:v", "1", "-vf", vf, tmpBmp }, ctx.Token, null);
            }
            if (!File.Exists(tmpBmp))
            {
                // Springen kann bei beschädigten Dateien scheitern: dann das erste Bild nehmen
                Proc.Run(ctx.Ffmpeg.FfmpegPath, new string[] {
                    "-v", "error", "-y", "-i", video, "-frames:v", "1", "-vf", vf, tmpBmp }, ctx.Token, null);
            }
            if (!File.Exists(tmpBmp)) throw new Exception("ffmpeg konnte kein Bild erzeugen");

            Directory.CreateDirectory(previewDir);
            using (Image img = Image.FromFile(tmpBmp))
                img.Save(target, jpeg, enc);
        }
    }
}
