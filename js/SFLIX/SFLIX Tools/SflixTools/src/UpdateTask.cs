// Aufgabe "Update" (bisher updateStefflix.exe + updater.exe) und das Selbst-Update.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Sflix
{
    // Ergebnis der Update-Prüfung beim Start: was ist installiert, was liegt auf GitHub
    public class UpdateInfo
    {
        public bool WebChanged;
        public string WebLocal, WebRemote;          // STEFFLIX-Version aus version.js
        public Version SelfRemote;                  // neuere Updater-Version, sonst null
        public string SelfAvailable;                // Updater-Version auf GitHub
        public bool FfmpegChanged;
        public string FfmpegLocal;                  // null, wenn ffmpeg nicht installiert ist
        public string FfmpegRemote;                 // Version laut ffmpeg.json

        public bool SelfChanged { get { return SelfRemote != null; } }
        public bool Any { get { return WebChanged || SelfChanged || FfmpegChanged; } }

        public string Summary()
        {
            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
            if (WebChanged) parts.Add("STEFFLIX " + WebText());
            if (SelfChanged) parts.Add("Updater " + App.VersionText + " → " + SelfUpdate.Short(SelfRemote));
            if (FfmpegChanged) parts.Add("ffmpeg " + FfmpegLocal + " → " + FfmpegRemote);
            return string.Join(", ", parts.ToArray());
        }

        // "2.02 → 2.03", bei gleicher Nummer "2.02 (neue Dateien)"
        public string WebText()
        {
            string local = WebLocal ?? "fehlt", remote = WebRemote ?? "?";
            return local == remote ? local + " (neue Dateien)" : local + " → " + remote;
        }
    }

    // Patchnotes aus sflix_sys/news.json auf GitHub
    public class PatchVersion
    {
        public string Version;
        public readonly System.Collections.Generic.List<string[]> Items = new System.Collections.Generic.List<string[]>();   // {Typ, Text}
    }

    public static class PatchNotes
    {
        public static System.Collections.Generic.List<PatchVersion> Load(CancellationToken token)
        {
            byte[] data = Net.Get(Net.RawUrl + "sflix_sys/news.json", token);
            JsonObject root = Json.Parse(new UTF8Encoding(false).GetString(data)) as JsonObject;
            System.Collections.Generic.List<PatchVersion> list = new System.Collections.Generic.List<PatchVersion>();
            if (root == null || root.Arr("versions") == null) throw new FormatException("Patchnotes sind beschädigt");
            foreach (object o in root.Arr("versions"))
            {
                JsonObject v = o as JsonObject;
                if (v == null || v.Str("version") == null) continue;
                PatchVersion pv = new PatchVersion();
                pv.Version = v.Str("version");
                if (v.Arr("items") != null)
                    foreach (object i in v.Arr("items"))
                    {
                        JsonObject item = i as JsonObject;
                        if (item == null) continue;
                        pv.Items.Add(new string[] { item.Str("type") ?? "", CleanText(item.Str("text")) });
                    }
                list.Add(pv);
            }
            return list;
        }

        // Versionen, die neuer sind als die installierte. Gibt es keine, die neueste (newer = false).
        public static System.Collections.Generic.List<PatchVersion> Select(System.Collections.Generic.List<PatchVersion> all, string installed, out bool newer)
        {
            System.Collections.Generic.List<PatchVersion> result = new System.Collections.Generic.List<PatchVersion>();
            double local = Num(installed);
            if (local > 0)
                foreach (PatchVersion v in all) if (Num(v.Version) > local) result.Add(v);
            newer = result.Count > 0;
            if (!newer && all.Count > 0)
            {
                PatchVersion latest = all[0];
                foreach (PatchVersion v in all) if (Num(v.Version) > Num(latest.Version)) latest = v;
                result.Add(latest);
            }
            result.Sort(delegate(PatchVersion a, PatchVersion b) { return Num(b.Version).CompareTo(Num(a.Version)); });
            return result;
        }

        static double Num(string v)
        {
            double d;
            return double.TryParse(v ?? "", System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d) ? d : 0;
        }

        // HTML-Reste entfernen (news.txt enthält z.B. &amp;)
        static string CleanText(string s)
        {
            s = Regex.Replace(s ?? "", "<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(s).Trim();
        }
    }

    public static class UpdateCheck
    {
        // Installierte STEFFLIX-Version aus sflix_sys\version.js (null, wenn unbekannt)
        public static string LocalWebVersion()
        {
            try { return ParseWebVersion(File.ReadAllText(Path.Combine(App.Root, "sflix_sys", "version.js"), Encoding.UTF8)); }
            catch (IOException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
        }

        public static UpdateInfo Run(FfmpegTools ffmpeg, CancellationToken token)
        {
            JsonObject checksums = Net.LoadChecksums(token);
            UpdateInfo u = new UpdateInfo();

            // STEFFLIX: sind alle Dateien der Weboberfläche auf dem Stand der Prüfsummen?
            u.WebLocal = LocalWebVersion();
            foreach (string f in Net.WebFiles)
            {
                if (checksums.Str(f) == null) continue;
                if (!Net.IsUpToDate(f, Path.Combine(App.Root, f.Replace('/', '\\')), checksums)) { u.WebChanged = true; break; }
            }
            u.WebRemote = u.WebLocal;
            if (u.WebChanged)
            {
                // Neue Versionsnummer direkt aus der version.js auf GitHub
                try { u.WebRemote = ParseWebVersion(Encoding.UTF8.GetString(Net.DownloadFile("sflix_sys/version.js", checksums, token))); }
                catch (DownloadException) { u.WebRemote = null; }
            }

            // Updater
            Version v;
            string msg;
            if (SelfUpdate.FindNewer(checksums, token, out v, out msg) != null) u.SelfRemote = v;
            // Ohne Download (gleiche Prüfsumme) ist die Version auf GitHub die installierte
            u.SelfAvailable = v != null ? SelfUpdate.Short(v) : App.VersionText;

            // ffmpeg: Update nur anbieten, wenn es schon installiert ist (sonst wird es bei Bedarf geladen)
            TaskContext quiet = new TaskContext();
            quiet.Token = token;
            quiet.LogLine = delegate { };
            FfmpegManifest m = FfmpegTools.LoadManifest(checksums, quiet) ?? FfmpegManifest.BuiltIn();
            u.FfmpegRemote = m.Version;
            if (ffmpeg.Installed)
            {
                u.FfmpegLocal = ffmpeg.InstalledVersionName() ?? "?";
                u.FfmpegChanged = !string.Equals(ffmpeg.InstalledVersion(), m.Sha256, StringComparison.OrdinalIgnoreCase);
            }
            return u;
        }

        // version.js: "var version = 2.02;"
        public static string ParseWebVersion(string text)
        {
            Match m = Regex.Match(text ?? "", @"version\s*=\s*[""']?(\d+(?:\.\d+)*)");
            return m.Success ? m.Groups[1].Value : null;
        }
    }

    public static class SelfUpdate
    {
        // Prüft, ob auf dem Server eine neuere Version dieses Programms liegt.
        // Gibt den geprüften Inhalt zurück, sonst null. version erhält die neue Versionsnummer.
        public static byte[] FindNewer(JsonObject checksums, CancellationToken token, out Version version, out string message)
        {
            version = null;
            string expected = checksums.Str(App.ExeName);
            if (expected == null) { message = "keine Prüfsumme für " + App.ExeName; return null; }

            string own = FileOps.Sha256File(App.ExePath);
            if (own == expected) { message = "aktuell (" + App.VersionText + ")"; return null; }

            byte[] data = Net.DownloadFile(App.ExeName, checksums, token);
            version = ReadVersion(data);
            // Nur echte neuere Versionen installieren. Verhindert z.B., dass eine ältere
            // Programmversion auf dem Server diese Version ersetzt.
            if (version == null || version <= App.Version)
            {
                message = "Server hat keine neuere Version (" + (version == null ? "unbekannt" : Short(version)) + ")";
                return null;
            }
            message = "neue Version " + Short(version);
            return data;
        }

        public static string Short(Version v)
        {
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        static Version ReadVersion(byte[] data)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "sflix-version-" + Process.GetCurrentProcess().Id + ".exe");
            try
            {
                File.WriteAllBytes(tmp, data);
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(tmp);
                if (fvi.FileMajorPart == 0 && fvi.FileMinorPart == 0 && fvi.FileBuildPart == 0) return null;
                return new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, 0);
            }
            catch { return null; }
            finally { FileOps.TryDelete(tmp); }
        }

        // Ersetzt die laufende EXE: neue Datei als .new schreiben, laufende in .old umbenennen
        // (das erlaubt Windows auch bei laufenden Programmen), .new an ihre Stelle.
        // Die .old wird beim nächsten Start gelöscht.
        public static void Install(byte[] data)
        {
            Net.InstallFile(App.ExePath, data, true);
        }

        // Startet die (neue) EXE. args z.B. "--updated --run data,preview"
        public static void Restart(string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo(App.ExePath, args);
            psi.UseShellExecute = false;
            psi.WorkingDirectory = App.Root;
            Process.Start(psi);
        }

        // Beim Start: Überbleibsel eines früheren Updates entfernen. Die alte EXE kann noch
        // einen Moment laufen, deshalb mehrere Versuche.
        public static void CleanupOld()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string old = App.ExePath + ".old";
                for (int i = 0; i < 40 && File.Exists(old); i++)
                {
                    try { File.Delete(old); }
                    catch { Thread.Sleep(500); }
                }
                // Übrige Sicherungen (*.old) alter und neuer Updates
                Net.RemoveOldBackups(App.Root);
            });
        }
    }

    public static class UpdateTask
    {
        public static void Run(TaskContext ctx, bool includeFfmpeg)
        {
            ctx.Progress(-1, "Lade Prüfsummen ...");
            JsonObject checksums;
            try
            {
                checksums = Net.LoadChecksums(ctx.Token);
            }
            catch (DownloadException e)
            {
                throw new TaskFailedException(e.Message + ". Es wurden keine Dateien verändert.");
            }

            ctx.Log("STEFFLIX-Dateien:");
            int failed = Net.UpdateFiles(Net.WebFiles, App.Root, checksums, ctx);
            if (failed > 0)
                throw new TaskFailedException("Das Update wurde nicht installiert. Bitte später erneut versuchen.");
            Net.RemoveObsoleteFiles(App.Root, ctx);
            int oldRemoved = Net.RemoveOldBackups(App.Root);
            if (oldRemoved > 0) ctx.Log("  Sicherungen (*.old) entfernt: " + oldRemoved);

            if (includeFfmpeg)
            {
                ctx.Log("ffmpeg:");
                try
                {
                    ctx.Ffmpeg.Update(checksums, ctx);
                }
                catch (DownloadException e)
                {
                    // ffmpeg-Fehler verhindern das Programm-Update nicht
                    ctx.Log("  FEHLER: " + e.Message + " (ffmpeg bleibt unverändert)");
                }
            }

            ctx.Log("Programm:");
            ctx.Progress(-1, "Prüfe Programmversion ...");
            Version version;
            string message;
            byte[] data;
            try
            {
                data = SelfUpdate.FindNewer(checksums, ctx.Token, out version, out message);
            }
            catch (DownloadException e)
            {
                throw new TaskFailedException(App.ExeName + ": " + e.Message);
            }
            ctx.Log("  " + App.ExeName.PadRight(36) + message);
            if (data != null)
            {
                SelfUpdate.Install(data);
                ctx.Log("  " + App.ExeName.PadRight(36) + "installiert, Programm startet neu");
                ctx.RestartRequested = true;
            }
        }
    }
}
