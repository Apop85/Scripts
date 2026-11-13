// PreviewMaker: creates a 480x270 JPEG (quality 80) from the middle of every video
// in the folder where the EXE lives (and all subfolders).
// Output: <video folder>\Preview\<video name>.jpg -- existing previews are skipped.
// Previews without a matching video or folder of the same name are moved to Preview\__gelöscht.
// ffmpeg.exe is embedded as a resource and unpacked to %TEMP% on first use.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

class PreviewMaker
{
    const int Width = 480;
    const int Height = 270;
    const long JpegQuality = 80;
    static readonly string[] Extensions = { ".mp4", ".m4v", ".webm", ".ogv" };
    static readonly string[] SkipDirs = { "$RECYCLE.BIN", "System Volume Information" };

    static string ffmpeg;
    static string tmpBmp;

    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        bool pause = Array.IndexOf(args, "/nopause") < 0;

        string root = AppDomain.CurrentDomain.BaseDirectory;
        string logFile = Path.Combine(root, "PreviewMaker.log");
        int created = 0, skipped = 0, failed = 0, deleted = 0;

        try
        {
            Console.WriteLine("Durchsuche: " + root);
            ffmpeg = ExtractFfmpeg();
            tmpBmp = Path.Combine(Path.GetTempPath(), "PreviewMaker-" + Process.GetCurrentProcess().Id + ".bmp");

            ImageCodecInfo jpeg = null;
            foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                if (c.MimeType == "image/jpeg") jpeg = c;
            EncoderParameters enc = new EncoderParameters(1);
            enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JpegQuality);

            List<string> videos = new List<string>();
            List<string> previewDirs = new List<string>();
            CollectVideos(root, videos, previewDirs);
            Console.WriteLine(videos.Count + " Videos gefunden.");
            Console.WriteLine();

            deleted = CleanOrphans(previewDirs, logFile, ref failed);
            if (deleted > 0) Console.WriteLine();

            for (int i = 0; i < videos.Count; i++)
            {
                string video = videos[i];
                string previewDir = Path.Combine(Path.GetDirectoryName(video), "Preview");
                string target = Path.Combine(previewDir, Path.GetFileNameWithoutExtension(video) + ".jpg");
                string prefix = "[" + (i + 1) + "/" + videos.Count + "] ";

                if (File.Exists(target)) { skipped++; continue; }

                try
                {
                    MakePreview(video, previewDir, target, jpeg, enc);
                    created++;
                    Console.WriteLine(prefix + "OK      " + video);
                }
                catch (Exception ex)
                {
                    failed++;
                    string msg = prefix + "FEHLER  " + video + " :: " + ex.Message;
                    Console.WriteLine(msg);
                    try { File.AppendAllText(logFile, DateTime.Now.ToString("s") + "  " + msg + Environment.NewLine, Encoding.UTF8); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Abbruch: " + ex.Message);
            failed++;
        }
        finally
        {
            try { if (tmpBmp != null && File.Exists(tmpBmp)) File.Delete(tmpBmp); } catch { }
        }

        Console.WriteLine();
        Console.WriteLine("Fertig. Erstellt: " + created + "   Uebersprungen (schon vorhanden): " + skipped +
                          "   Nach " + TrashDirName + " verschoben: " + deleted + "   Fehler: " + failed);
        if (failed > 0) Console.WriteLine("Details zu Fehlern: " + logFile);

        if (pause && !Console.IsInputRedirected)
        {
            Console.WriteLine("Taste druecken zum Beenden...");
            Console.ReadKey(true);
        }
        return failed > 0 ? 1 : 0;
    }

    static void CollectVideos(string dir, List<string> result, List<string> previewDirs)
    {
        try
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (Array.IndexOf(Extensions, ext) >= 0) result.Add(f);
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                string name = Path.GetFileName(d);
                bool skip = false;
                foreach (string s in SkipDirs)
                    if (string.Equals(name, s, StringComparison.OrdinalIgnoreCase)) skip = true;
                if (skip) continue;
                if (string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase)) previewDirs.Add(d);
                CollectVideos(d, result, previewDirs);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    // "ö" = o-umlaut; escaped so the source compiles the same with any codepage.
    const string TrashDirName = "__gelöscht";

    // Moves Preview\<name>.jpg to Preview\__gelöscht unless the parent folder contains
    // a video <name>.<video ext> or a folder <name>.
    static int CleanOrphans(List<string> previewDirs, string logFile, ref int failed)
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
                // Keep if a folder (e.g. a season) or a video with the same name exists next to Preview.
                bool keep = Directory.Exists(Path.Combine(parent, baseName));
                foreach (string ext in Extensions)
                    if (!keep && File.Exists(Path.Combine(parent, baseName + ext))) keep = true;
                if (keep) continue;

                try
                {
                    Directory.CreateDirectory(trash);
                    string dest = Path.Combine(trash, Path.GetFileName(jpg));
                    // Don't overwrite an earlier moved file with the same name.
                    if (File.Exists(dest))
                        dest = Path.Combine(trash, baseName + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".jpg");
                    File.Move(jpg, dest);
                    deleted++;
                    Console.WriteLine("VERSCHOBEN  " + jpg + "  ->  " + TrashDirName);
                }
                catch (Exception ex)
                {
                    failed++;
                    string msg = "FEHLER beim Verschieben  " + jpg + " :: " + ex.Message;
                    Console.WriteLine(msg);
                    try { File.AppendAllText(logFile, DateTime.Now.ToString("s") + "  " + msg + Environment.NewLine, Encoding.UTF8); } catch { }
                }
            }
        }
        return deleted;
    }

    static void MakePreview(string video, string previewDir, string target, ImageCodecInfo jpeg, EncoderParameters enc)
    {
        double mid = GetDuration(video) / 2.0;
        string vf = "scale=" + Width + ":" + Height + ":force_original_aspect_ratio=increase,crop=" + Width + ":" + Height + ",setsar=1";

        if (File.Exists(tmpBmp)) File.Delete(tmpBmp);
        Run("-v error -y -ss " + mid.ToString("0.###", CultureInfo.InvariantCulture) +
            " -i \"" + video + "\" -frames:v 1 -vf " + vf + " \"" + tmpBmp + "\"");
        if (!File.Exists(tmpBmp))
            // Seeking can fail on damaged files; fall back to the first frame.
            Run("-v error -y -i \"" + video + "\" -frames:v 1 -vf " + vf + " \"" + tmpBmp + "\"");
        if (!File.Exists(tmpBmp)) throw new Exception("ffmpeg konnte kein Bild erzeugen");

        Directory.CreateDirectory(previewDir);
        using (Image img = Image.FromFile(tmpBmp))
            img.Save(target, jpeg, enc);
    }

    // Reads "Duration: HH:MM:SS.xx" from ffmpeg's info output. Returns 0 if unknown.
    static double GetDuration(string video)
    {
        string info = Run("-hide_banner -i \"" + video + "\"");
        Match m = Regex.Match(info, @"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)");
        if (!m.Success) return 0;
        return int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60 +
               double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
    }

    // Runs ffmpeg and returns its stderr output.
    static string Run(string arguments)
    {
        ProcessStartInfo psi = new ProcessStartInfo(ffmpeg, arguments);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardError = true;
        psi.StandardErrorEncoding = Encoding.UTF8;
        using (Process p = Process.Start(psi))
        {
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            return err;
        }
    }

    static string ExtractFfmpeg()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        using (Stream s = asm.GetManifestResourceStream("ffmpeg.exe"))
        {
            string dir = Path.Combine(Path.GetTempPath(), "PreviewMaker");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "ffmpeg-" + s.Length + ".exe");
            if (File.Exists(path) && new FileInfo(path).Length == s.Length) return path;

            Console.WriteLine("Entpacke ffmpeg (einmalig)...");
            string tmp = path + "." + Process.GetCurrentProcess().Id + ".tmp";
            using (FileStream fs = File.Create(tmp)) s.CopyTo(fs);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return path;
        }
    }
}
