// Aufgabe "Update" (früher updateStefflix.exe + updater.exe) und das Selbst-Update von STEFFLIX.exe.

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
            if (SelfChanged) parts.Add("Desktop " + App.VersionText + " → " + SelfUpdate.Short(SelfRemote));
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
        // {Typ, Text, Komponente (stefflix/updater/app), Version der Komponente oder "", Text ohne "Updater x.y.z:"}
        public readonly System.Collections.Generic.List<string[]> Items = new System.Collections.Generic.List<string[]>();
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
                // Ab STEFFLIX 2.5.0 steht die Version als x.y.z in "name", "version" ist die alte Zahl
                pv.Version = WebVersion.ToName(v.Str("name") ?? v.Str("version"));
                if (pv.Version == null) continue;
                if (v.Arr("items") != null)
                    foreach (object i in v.Arr("items"))
                    {
                        JsonObject item = i as JsonObject;
                        if (item == null) continue;
                        string text = CleanText(item.Str("text"));
                        string component = item.Str("component"), version = item.Str("componentVersion"), shortText = item.Str("short");
                        // news.json vor den getrennten Patchnotes: Komponente aus dem Textanfang
                        if (component == null || shortText == null) ComponentOf(text, out component, out version, out shortText);
                        else shortText = CleanText(shortText);
                        if (component == "stefflix" && version == null) version = pv.Version;
                        pv.Items.Add(new string[] { item.Str("type") ?? "", text, component, version ?? "", shortText });
                    }
                list.Add(pv);
            }
            return list;
        }

        // Versionen, die neuer sind als die installierte. Gibt es keine, die neueste (newer = false).
        public static System.Collections.Generic.List<PatchVersion> Select(System.Collections.Generic.List<PatchVersion> all, string installed, out bool newer)
        {
            System.Collections.Generic.List<PatchVersion> result = new System.Collections.Generic.List<PatchVersion>();
            string local = WebVersion.ToName(installed);
            if (local != null)
                foreach (PatchVersion v in all) if (WebVersion.Compare(v.Version, local) > 0) result.Add(v);
            newer = result.Count > 0;
            if (!newer && all.Count > 0)
            {
                PatchVersion latest = all[0];
                foreach (PatchVersion v in all) if (WebVersion.Compare(v.Version, latest.Version) > 0) latest = v;
                result.Add(latest);
            }
            result.Sort(delegate(PatchVersion a, PatchVersion b) { return WebVersion.Compare(b.Version, a.Version); });
            return result;
        }

        // Seit 4.0.0 "Desktop x.y.z:", vorher "Updater x.y.z:" (gleiche Komponente "updater")
        static readonly Regex UpdaterPrefix = new Regex(@"^(?:Desktop|Updater)(?:\s+(\d+\.\d+\.\d+))?\s*:\s*");
        static readonly Regex AppPrefix = new Regex(@"^(?:Android-App|App)(?:\s+(\d+\.\d+\.\d+))?\s*:\s*");

        // Wie create_news.py: "Desktop x.y.z:" bzw. "Android-App x.y.z:" am Anfang, sonst STEFFLIX
        static void ComponentOf(string text, out string component, out string version, out string shortText)
        {
            Match m = UpdaterPrefix.Match(text);
            component = "updater";
            if (!m.Success) { m = AppPrefix.Match(text); component = "app"; }
            if (!m.Success)
            {
                component = "stefflix";
                version = null;
                shortText = text;
                return;
            }
            version = m.Groups[1].Success ? m.Groups[1].Value : null;
            shortText = text.Substring(m.Length);
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
            foreach (string f in Net.WebFilesFor(checksums))
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

        // version.js: ab 2.5.0 'var versionName = "2.5.0";', bis 2.04 nur 'var version = 2.04;'.
        // Ergebnis immer x.y.z (2.04 -> 2.4.0).
        public static string ParseWebVersion(string text)
        {
            Match n = Regex.Match(text ?? "", @"\bversionName\s*=\s*[""'](\d+(?:\.\d+)*)[""']");
            if (n.Success) return WebVersion.ToName(n.Groups[1].Value);
            Match m = Regex.Match(text ?? "", @"\bversion\s*=\s*[""']?(\d+(?:\.\d+)*)");
            return m.Success ? WebVersion.ToName(m.Groups[1].Value) : null;
        }
    }

    // STEFFLIX-Versionen: ab 2.5.0 x.y.z, bis 2.04 eine Zahl. Umrechnung wie
    // "SFLIX_Tools\Release\versioning.py": alte Zahl = x + y/100 + z/10000.
    public static class WebVersion
    {
        // "2.04" -> "2.4.0", "1.323" -> "1.32.3", "2.0501" -> "2.5.1"; x.y.z bleibt. null bei Unsinn.
        public static string ToName(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string[] parts = text.Trim().Split('.');
            int major, minor = 0, patch = 0;
            if (!int.TryParse(parts[0], out major)) return null;
            if (parts.Length >= 3)
            {
                if (!int.TryParse(parts[1], out minor) || !int.TryParse(parts[2], out patch)) return null;
                return major + "." + minor + "." + patch;
            }
            string dec = parts.Length > 1 ? parts[1] : "";
            if (dec.Length > 0 && !Regex.IsMatch(dec, @"^\d+$")) return null;
            if (dec.Length <= 2) minor = int.Parse(dec.PadRight(2, '0'));
            else if (dec.Length == 3) { minor = int.Parse(dec.Substring(0, 2)); patch = int.Parse(dec.Substring(2, 1)); }
            else { minor = int.Parse(dec.Substring(0, 2)); patch = int.Parse(dec.Substring(2, 2)); }
            return major + "." + minor + "." + patch;
        }

        // positiv, wenn a neuer ist
        public static int Compare(string a, string b)
        {
            string[] pa = (ToName(a) ?? "0.0.0").Split('.'), pb = (ToName(b) ?? "0.0.0").Split('.');
            for (int i = 0; i < 3; i++)
            {
                int d = int.Parse(pa[i]).CompareTo(int.Parse(pb[i]));
                if (d != 0) return d;
            }
            return 0;
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

        // Dateiversion direkt aus den Bytes der EXE (Block VS_FIXEDFILEINFO der Versionsressource).
        // Bis 4.0.4 wurde die EXE dafür in den Temp-Ordner geschrieben: Virenscanner melden eine frische,
        // nicht signierte EXE dort teils als verdächtig und sperren sie, dann schlug das Update still fehl.
        // Gesucht wird nur in der Versionsressource der EXE selbst (über die PE-Kopfdaten), nicht in
        // eingebetteten Dateien wie den WebView2-DLLs, die eigene Versionsblöcke haben.
        public static Version ReadVersion(byte[] data)
        {
            try
            {
                int start, length;
                if (!FindVersionResource(data, out start, out length)) return null;
                // Kennung 0xFEEF04BD, danach dwStrucVersion (0x00010000), dwFileVersionMS, dwFileVersionLS
                for (int i = start; i + 16 <= start + length; i++)
                {
                    if (ReadUInt(data, i) != 0xFEEF04BD || ReadUInt(data, i + 4) != 0x00010000) continue;
                    uint ms = ReadUInt(data, i + 8);
                    uint ls = ReadUInt(data, i + 12);
                    int major = (int)(ms >> 16), minor = (int)(ms & 0xFFFF), build = (int)(ls >> 16);
                    if (major == 0 && minor == 0 && build == 0) return null;
                    return new Version(major, minor, build, 0);
                }
                return null;
            }
            catch { return null; }   // keine gültige EXE
        }

        // Lage der Versionsressource (RT_VERSION = 16) in der Datei
        static bool FindVersionResource(byte[] d, out int start, out int length)
        {
            start = 0; length = 0;
            if (d == null || d.Length < 0x40 || d[0] != 'M' || d[1] != 'Z') return false;
            int pe = (int)ReadUInt(d, 0x3C);
            if (pe <= 0 || pe + 24 > d.Length || ReadUInt(d, pe) != 0x00004550) return false;
            int sections = ReadUShort(d, pe + 6);
            int optSize = ReadUShort(d, pe + 20);
            int opt = pe + 24;
            int magic = ReadUShort(d, opt);
            int dirs = opt + (magic == 0x20B ? 112 : 96);
            uint rsrcRva = ReadUInt(d, dirs + 2 * 8);
            if (rsrcRva == 0) return false;
            int table = opt + optSize;
            int rsrc = RvaToOffset(d, table, sections, rsrcRva);
            if (rsrc < 0) return false;

            // Ebene 1: Typ 16, Ebene 2 und 3: jeweils der erste Eintrag (Name bzw. Sprache)
            uint entry = FindResourceEntry(d, rsrc, rsrc, 16, true);
            for (int level = 0; level < 2 && entry != 0 && (entry & 0x80000000) != 0; level++)
                entry = FindResourceEntry(d, rsrc, rsrc + (int)(entry & 0x7FFFFFFF), 0, false);
            if (entry == 0 || (entry & 0x80000000) != 0) return false;
            int dataEntry = rsrc + (int)entry;
            int offset = RvaToOffset(d, table, sections, ReadUInt(d, dataEntry));
            int size = (int)ReadUInt(d, dataEntry + 4);
            if (offset < 0 || size <= 0 || offset + size > d.Length) return false;
            start = offset; length = size;
            return true;
        }

        // OffsetToData des Eintrags mit der Id (oder des ersten, wenn byId false); 0 = keiner
        static uint FindResourceEntry(byte[] d, int rsrc, int dir, int id, bool byId)
        {
            int named = ReadUShort(d, dir + 12);
            int ids = ReadUShort(d, dir + 14);
            for (int i = 0; i < named + ids; i++)
            {
                int e = dir + 16 + i * 8;
                uint name = ReadUInt(d, e);
                if (!byId || ((name & 0x80000000) == 0 && name == (uint)id)) return ReadUInt(d, e + 4);
            }
            return 0;
        }

        static int RvaToOffset(byte[] d, int table, int sections, uint rva)
        {
            for (int i = 0; i < sections; i++)
            {
                int s = table + i * 40;
                uint va = ReadUInt(d, s + 12);
                uint vsize = Math.Max(ReadUInt(d, s + 8), ReadUInt(d, s + 16));
                if (rva >= va && rva < va + vsize) return (int)(rva - va + ReadUInt(d, s + 20));
            }
            return -1;
        }

        static int ReadUShort(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        static uint ReadUInt(byte[] data, int offset)
        {
            return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
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
            int failed = Net.UpdateFiles(Net.WebFilesFor(checksums), App.Root, checksums, ctx);
            if (failed > 0)
                throw new TaskFailedException("Das Update wurde nicht installiert. Bitte später erneut versuchen.");
            Net.RemoveObsoleteFiles(App.Root, checksums, ctx);
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
