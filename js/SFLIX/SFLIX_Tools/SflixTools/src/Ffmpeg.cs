// ffmpeg/ffprobe: Installation, Update, Untersuchen von Videos, Wahl des Encoders.
//
// ffmpeg liegt seit 3.3 standardmässig in %LOCALAPPDATA%\STEFFLIX\ffmpeg (je Benutzer und PC,
// lokal auch bei STEFFLIX auf einem NAS) oder in einem im Setup bzw. unter Tools › Einstellungen
// gewählten Ordner (ffmpeg.folder). Bis 3.2 lag es in sflix_sys\ffmpeg, von dort wird es beim
// Start verschoben (FfmpegLocation). Es wird beim Setup oder beim ersten Bedarf von einem
// offiziellen Build (gyan.dev, über GitHub-Releases) geladen. Welche Version, steht in
// ffmpeg.json im STEFFLIX-Repository (selbst über checksums.json abgesichert). Ist die Datei
// dort nicht vorhanden, gilt die hier eingebaute Version.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace Sflix
{
    public class FfmpegManifest
    {
        public string Version;
        public string Url;
        public string Sha256;

        public static FfmpegManifest BuiltIn()
        {
            FfmpegManifest m = new FfmpegManifest();
            m.Version = "9.0.2";
            m.Url = "https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.zip";
            m.Sha256 = "60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba";
            return m;
        }

        public static FfmpegManifest FromJson(JsonObject o)
        {
            if (o == null) return null;
            FfmpegManifest m = new FfmpegManifest();
            m.Version = o.Str("version");
            m.Url = o.Str("url");
            m.Sha256 = (o.Str("sha256") ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(m.Version) || string.IsNullOrEmpty(m.Url) || m.Sha256.Length != 64) return null;
            if (!m.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return null;
            return m;
        }

        public JsonObject ToJson()
        {
            JsonObject o = new JsonObject();
            o["version"] = Version;
            o["url"] = Url;
            o["sha256"] = Sha256;
            return o;
        }
    }

    // Ergebnis von ffprobe in handlicher Form
    public class MediaInfo
    {
        public string Path;
        public double Duration;          // Sekunden, 0 = unbekannt
        public string Container = "";    // format_name, z.B. "mov,mp4,m4a,3gp,3g2,mj2"
        public int VideoIndex = -1;      // Stream-Index des Hauptvideos (ohne Coverbilder)
        public string VideoCodec = "";
        public string PixelFormat = "";
        public int Width, Height;
        public List<AudioStream> Audio = new List<AudioStream>();
        // Für den Formatvergleich beim Zusammenfügen (Codec, Auflösung, Ton)
        public string Signature = "";
    }

    // Lücke in einem Video (DEC-373): Stelle und Länge in Sekunden
    public class MediaGap
    {
        public double At, Length;

        public MediaGap(double at, double length) { At = at; Length = length; }

        // "1.2 s bei 0:12:34"; Stelle abgerundet wie in der Weboberfläche (formatMediaTime), nie nach dem Beginn
        public string Text { get { return Fmt.Inv(Length, "0.0") + " s bei " + Fmt.Time(Math.Floor(At)); } }

        public static string Describe(List<MediaGap> gaps)
        {
            if (gaps == null || gaps.Count == 0) return "";
            List<string> parts = new List<string>();
            foreach (MediaGap g in gaps) { if (parts.Count == 3) { parts.Add("…"); break; } parts.Add(g.Text); }
            return string.Join(", ", parts.ToArray());
        }
    }

    public class AudioStream
    {
        public int Index;
        public string Codec = "";
        public string Language;
    }

    // Wo ffmpeg liegt bzw. hin installiert wird
    public static class FfmpegLocation
    {
        // Standard: %LOCALAPPDATA%\STEFFLIX\ffmpeg
        public static string DefaultFolder
        {
            get
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(Path.Combine(local, "STEFFLIX"), "ffmpeg");
            }
        }

        // Bis 3.2: im STEFFLIX-Ordner
        public static string LegacyFolder { get { return Path.Combine(App.SysDir, "ffmpeg"); } }

        public static bool IsInstalledIn(string folder)
        {
            return !string.IsNullOrEmpty(folder) && File.Exists(Path.Combine(folder, "ffmpeg.exe")) && File.Exists(Path.Combine(folder, "ffprobe.exe"));
        }

        // Gewählter Ordner aus den Einstellungen (leer = keiner)
        public static string Chosen(Settings settings)
        {
            return settings == null ? "" : settings.GetString("ffmpeg.folder", "");
        }

        // Verwendeter Ordner: gewählter Ordner, Standardordner, alter Ort (wo ffmpeg installiert ist).
        // Ist es nirgends installiert: der gewählte Ordner, falls es ihn auf diesem PC gibt, sonst
        // der Standardordner (dort wird es bei Bedarf installiert).
        public static string Folder(Settings settings)
        {
            string chosen = Chosen(settings);
            if (IsInstalledIn(chosen)) return chosen;
            if (IsInstalledIn(DefaultFolder)) return DefaultFolder;
            if (IsInstalledIn(LegacyFolder)) return LegacyFolder;
            if (chosen.Length > 0 && Directory.Exists(chosen)) return chosen;
            return DefaultFolder;
        }

        // Ein ffmpeg am alten Ort gilt als installiert und wird in den Standardordner verschoben
        // (nur wenn kein eigener Ordner gewählt ist und dort noch keines liegt). Klappt das nicht,
        // bleibt es am alten Ort und wird von dort verwendet. Gibt eine Meldung zurück oder null.
        public static string MigrateLegacy(Settings settings)
        {
            string legacy = LegacyFolder;
            if (!IsInstalledIn(legacy) || Chosen(settings).Length > 0 || IsInstalledIn(DefaultFolder)) return null;
            string target = DefaultFolder;
            try
            {
                Directory.CreateDirectory(target);
                foreach (string file in Directory.GetFiles(legacy))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                if (!IsInstalledIn(target)) throw new IOException("Kopie unvollständig");
                // Erst nach der vollständigen Kopie am alten Ort löschen
                try { Directory.Delete(legacy, true); }
                catch { }
                return "ffmpeg wurde nach " + target + " verschoben.";
            }
            catch (Exception e)
            {
                try
                {
                    foreach (string name in new string[] { "ffmpeg.exe", "ffprobe.exe", "version.json" })
                        FileOps.TryDelete(Path.Combine(target, name));
                }
                catch { }
                return "Hinweis: ffmpeg konnte nicht nach " + target + " verschoben werden (" + e.Message + "), es wird weiter aus sflix_sys\\ffmpeg verwendet.";
            }
        }
    }

    public class FfmpegTools
    {
        public string Dir;
        public string FfmpegPath { get { return Path.Combine(Dir, "ffmpeg.exe"); } }
        public string FfprobePath { get { return Path.Combine(Dir, "ffprobe.exe"); } }
        string VersionFile { get { return Path.Combine(Dir, "version.json"); } }

        readonly ConcurrentDictionary<string, MediaInfo> probeCache = new ConcurrentDictionary<string, MediaInfo>();
        string encoder;

        // folder: Ordner von ffmpeg (FfmpegLocation.Folder); leer = alter Ort im STEFFLIX-Ordner
        public FfmpegTools(string root, string folder)
        {
            Dir = !string.IsNullOrEmpty(folder) ? folder : Path.Combine(root, "sflix_sys", "ffmpeg");
        }

        public bool Installed
        {
            get { return File.Exists(FfmpegPath) && File.Exists(FfprobePath); }
        }

        // Wie InstalledVersion, aber nur einmal je Objekt gelesen (ein Lauf der Tools; MediaCache fragt
        // sonst je Datei version.json ab). Nach einer Installation über dieses Objekt neu gelesen.
        volatile string cachedVersion;
        volatile bool versionCached;

        public string CachedVersion()
        {
            if (!versionCached)
            {
                cachedVersion = InstalledVersion();
                versionCached = true;
            }
            return cachedVersion;
        }

        public string InstalledVersion()
        {
            try
            {
                JsonObject o = Json.Parse(File.ReadAllText(VersionFile, Encoding.UTF8)) as JsonObject;
                return o == null ? null : o.Str("sha256");
            }
            catch { return null; }
        }

        // Versionsnummer der installierten ffmpeg-Version (z.B. 9.0.2), null wenn unbekannt
        public string InstalledVersionName()
        {
            try
            {
                JsonObject o = Json.Parse(File.ReadAllText(VersionFile, Encoding.UTF8)) as JsonObject;
                return o == null ? null : o.Str("version");
            }
            catch { return null; }
        }

        // Stellt sicher, dass ffmpeg da ist (lädt es bei Bedarf einmalig herunter).
        public void EnsureInstalled(TaskContext ctx)
        {
            if (Installed) return;
            ctx.Log("ffmpeg ist noch nicht vorhanden und wird einmalig heruntergeladen.");
            Install(ManifestOrBuiltIn(ctx, Net.SerialStore), ctx);
        }

        // ffmpeg.json vom Server; ist der Server nicht erreichbar, die eingebaute Version (steckt in der EXE).
        // Sind die Prüfsummen erreichbar, aber abgelehnt (Signatur, älterer Stand): nichts installieren
        // (SFLIX-20261123-090, DEC-375).
        static FfmpegManifest ManifestOrBuiltIn(TaskContext ctx, Settings serialStore)
        {
            JsonObject checksums;
            try { checksums = Net.LoadChecksums(ctx.Token, serialStore); }
            catch (ChecksumsRejectedException e)
            {
                ctx.Log("  " + e.Message);
                throw new TaskFailedException("ffmpeg wurde nicht installiert: " + e.Message.TrimEnd('.') + ".");
            }
            catch (DownloadException e)
            {
                ctx.Log("  " + e.Message.TrimEnd('.') + ". Es wird die im Programm eingebaute ffmpeg-Version geladen.");
                return FfmpegManifest.BuiltIn();
            }
            return LoadManifest(checksums, ctx) ?? FfmpegManifest.BuiltIn();
        }

        // ffmpeg.json vom Server (geprüft über checksums.json). null, wenn nicht vorhanden.
        public static FfmpegManifest LoadManifest(JsonObject checksums, TaskContext ctx)
        {
            if (checksums.Str(Net.FfmpegManifest) == null) return null;
            try
            {
                byte[] data = Net.DownloadFile(Net.FfmpegManifest, checksums, ctx.Token);
                FfmpegManifest m = FfmpegManifest.FromJson(Json.Parse(Encoding.UTF8.GetString(data)) as JsonObject);
                if (m == null) ctx.Log("  ffmpeg.json ist unvollständig");
                return m;
            }
            catch (DownloadException e)
            {
                ctx.Log("  ffmpeg.json: " + e.Message);
                return null;
            }
            catch (FormatException)
            {
                ctx.Log("  ffmpeg.json ist beschädigt");
                return null;
            }
        }

        // Aktualisiert ffmpeg, falls der Server eine andere Version vorgibt.
        public void Update(JsonObject checksums, TaskContext ctx)
        {
            FfmpegManifest m = LoadManifest(checksums, ctx);
            if (m == null)
            {
                // ffmpeg.json ist eingetragen, aber nicht lesbar: ein installiertes ffmpeg nicht durch die
                // eingebaute (womöglich ältere) Version ersetzen (SFLIX-20261123-079)
                if (Installed && checksums.Str(Net.FfmpegManifest) != null)
                {
                    ctx.Log("  ffmpeg bleibt unverändert (ffmpeg.json nicht lesbar)");
                    return;
                }
                m = FfmpegManifest.BuiltIn();
            }
            if (Installed && string.Equals(InstalledVersion(), m.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                ctx.Log("  ffmpeg " + m.Version + " ist aktuell");
                return;
            }
            // Ein selbst hineinkopiertes ffmpeg (ohne version.json des Updaters) bleibt unverändert
            if (Installed && !File.Exists(VersionFile))
            {
                ctx.Log("  ffmpeg wird aus einem eigenen Ordner verwendet (" + Dir + ") und nicht aktualisiert");
                return;
            }
            Install(m, ctx);
        }

        // Installiert ffmpeg (Setup, Knopf "Installieren"). Lädt die Version aus ffmpeg.json.
        public void InstallNow(TaskContext ctx)
        {
            InstallNow(ctx, Net.SerialStore);
        }

        // serialStore: Einstellungen der Installation, in der die Release-Nummer gemerkt wird (DEC-372)
        public void InstallNow(TaskContext ctx, Settings serialStore)
        {
            Install(ManifestOrBuiltIn(ctx, serialStore), ctx);
        }

        void Install(FfmpegManifest m, TaskContext ctx)
        {
            Directory.CreateDirectory(Dir);
            string zip = Path.Combine(Dir, "download.zip.part");
            ctx.Log("  Lade ffmpeg " + m.Version + " ...");
            string hash;
            try
            {
                hash = Net.GetToFile(m.Url, zip, ctx.Token, delegate(long done, long total)
                {
                    if (total > 0) ctx.Progress((double)done / total, "Lade ffmpeg " + m.Version + "  " + Fmt.Size(done) + " / " + Fmt.Size(total));
                    else ctx.Progress(-1, "Lade ffmpeg " + m.Version + "  " + Fmt.Size(done));
                });
            }
            catch
            {
                FileOps.TryDelete(zip);
                throw;
            }

            try
            {
                if (!string.Equals(hash, m.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new DownloadException("ffmpeg: Prüfsumme stimmt nicht");

                ctx.Progress(-1, "Entpacke ffmpeg ...");
                string[] names = { "ffmpeg.exe", "ffprobe.exe" };
                using (ZipArchive archive = ZipFile.OpenRead(zip))
                {
                    foreach (string name in names)
                    {
                        ZipArchiveEntry entry = null;
                        foreach (ZipArchiveEntry e in archive.Entries)
                            if (e.FullName.EndsWith("/bin/" + name, StringComparison.OrdinalIgnoreCase)) entry = e;
                        if (entry == null) throw new DownloadException("ffmpeg: " + name + " fehlt im Archiv");
                        string tmp = Path.Combine(Dir, name + ".new");
                        entry.ExtractToFile(tmp, true);
                        if (!FileOps.IsWindowsExeFile(tmp)) throw new DownloadException("ffmpeg: " + name + " ist keine gültige Programmdatei");
                    }
                }

                foreach (string name in names)
                {
                    string target = Path.Combine(Dir, name);
                    if (File.Exists(target)) FileOps.Replace(target, target + ".old");
                    FileOps.Replace(target + ".new", target);
                    FileOps.TryDelete(target + ".old");
                }
                File.WriteAllText(VersionFile, Json.Write(m.ToJson(), 2), FileOps.Utf8NoBom);
                encoder = null;
                versionCached = false;
                ctx.Log("  ffmpeg " + m.Version + " installiert");
                // Für die Versionsanzeige in der Hilfe von STEFFLIX
                UpdaterInfo.Write(App.Root, Dir);
            }
            finally
            {
                FileOps.TryDelete(zip);
                FileOps.TryDelete(Path.Combine(Dir, "ffmpeg.exe.new"));
                FileOps.TryDelete(Path.Combine(Dir, "ffprobe.exe.new"));
            }
        }

        // ---------------------------------------------------------------
        // ffprobe
        // ---------------------------------------------------------------

        // Untersucht eine Datei. Pro Datei (Pfad, Grösse, Änderungszeit) nur einmal: im Arbeitsspeicher
        // und seit 3.4.2 auch über Programmstarts hinweg (MediaCache, sflix_sys\medien_cache.json).
        public MediaInfo Probe(string path, CancellationToken token)
        {
            FileInfo fi = new FileInfo(path);
            long size = fi.Length, ticks = fi.LastWriteTimeUtc.Ticks;
            string key = path + "|" + size + "|" + ticks;
            MediaInfo cached;
            if (probeCache.TryGetValue(key, out cached)) { MediaCache.CountHit(); return cached; }
            string storedError;
            if (MediaCache.TryGet(this, path, size, ticks, out cached, out storedError))
            {
                if (storedError != null) throw new Exception(storedError);
                probeCache[key] = cached;
                return cached;
            }

            ProcResult r = Proc.Run(FfprobePath, new string[] {
                "-v", "error", "-print_format", "json", "-show_format", "-show_streams", path }, token, null);
            if (r.ExitCode != 0)
            {
                string err = r.StdErr.Trim();
                string message = "ffprobe: " + (err.Length > 0 ? LastLine(err) : "Fehler " + r.ExitCode);
                // Nur echte Dateifehler merken, keine vorübergehenden Lesefehler (NAS kurz weg, Datei gesperrt)
                if (!IsTransientError(err)) MediaCache.PutError(this, path, size, ticks, message);
                throw new Exception(message);
            }
            JsonObject data = null;
            try { data = Json.Parse(r.StdOut) as JsonObject; } catch (FormatException) { }
            if (data == null || data.Obj("format") == null)
            {
                MediaCache.PutError(this, path, size, ticks, "ffprobe liefert keine Daten");
                throw new Exception("ffprobe liefert keine Daten");
            }

            MediaInfo info = new MediaInfo();
            info.Path = path;
            JsonObject format = data.Obj("format");
            info.Duration = format.Num("duration", 0);
            info.Container = format.Str("format_name") ?? "";

            StringBuilder sig = new StringBuilder();
            List<object> streams = data.Arr("streams") ?? new List<object>();
            foreach (object so in streams)
            {
                JsonObject s = so as JsonObject;
                if (s == null) continue;
                string type = s.Str("codec_type");
                if (type == "video")
                {
                    JsonObject disp = s.Obj("disposition");
                    bool cover = disp != null && disp.Num("attached_pic", 0) == 1;
                    if (cover || info.VideoIndex >= 0) continue;
                    info.VideoIndex = (int)s.Num("index", 0);
                    info.VideoCodec = s.Str("codec_name") ?? "";
                    info.PixelFormat = s.Str("pix_fmt") ?? "";
                    info.Width = (int)s.Num("width", 0);
                    info.Height = (int)s.Num("height", 0);
                    sig.Append("v:" + info.VideoCodec + ":" + info.Width + "x" + info.Height + ";");
                }
                else if (type == "audio")
                {
                    AudioStream a = new AudioStream();
                    a.Index = (int)s.Num("index", 0);
                    a.Codec = s.Str("codec_name") ?? "";
                    JsonObject tags = s.Obj("tags");
                    a.Language = tags == null ? null : tags.Str("language");
                    info.Audio.Add(a);
                    sig.Append("a:" + a.Codec + ":" + s.Str("sample_rate") + ":" + s.Str("channels") + ";");
                }
            }
            info.Signature = sig.ToString();
            probeCache[key] = info;
            MediaCache.Put(this, path, size, ticks, info);
            return info;
        }

        // ---------------------------------------------------------------
        // Lückensuche (DEC-373): Zeitstempel (DTS) der Pakete der Videospur ohne Dekodieren. Eine Lücke
        // ist ein Abstand zweier Pakete, der den üblichen Bildabstand (Median) um mindestens GapThreshold
        // übersteigt. Die Paketdauer taugt nicht: der MP4-Muxer verlängert das letzte Bild vor einer Lücke
        // bis zum nächsten (mit SFLIX_Tools\Test\make_gap_video.py kalibriert).
        // ---------------------------------------------------------------

        public const double GapThreshold = 0.5;
        static readonly ConcurrentDictionary<string, object> gapLocks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Mit gemerktem Ergebnis (medien_cache.json, überlebt einen Wechsel der ffmpeg-Version). Dieselbe
        // Datei wird auch bei gleichzeitigen Aufgaben nur einmal gelesen.
        public List<MediaGap> Gaps(string path, CancellationToken token)
        {
            object gate = gapLocks.GetOrAdd(Path.GetFullPath(path), delegate { return new object(); });
            lock (gate)
            {
                FileInfo fi = new FileInfo(path);
                long size = fi.Length, ticks = fi.LastWriteTimeUtc.Ticks;
                List<MediaGap> gaps;
                if (MediaCache.TryGetGaps(this, path, size, ticks, out gaps)) return gaps;
                int video = -1;
                try { video = Probe(path, token).VideoIndex; }
                catch (OperationCanceledException) { throw; }
                catch { }
                gaps = ScanGaps(path, video, token);
                MediaCache.PutGaps(this, path, size, ticks, gaps);
                return gaps;
            }
        }

        // Ohne gemerktes Ergebnis (Downloader: Datei ist gerade entstanden). videoIndex < 0: erste Videospur.
        public List<MediaGap> ScanGaps(string path, int videoIndex, CancellationToken token)
        {
            string streams = videoIndex >= 0 ? videoIndex.ToString(CultureInfo.InvariantCulture) : "v:0";
            ProcResult r = Proc.Run(FfprobePath, new string[] {
                "-v", "error", "-select_streams", streams, "-show_entries", "packet=dts_time", "-of", "csv=p=0", path }, token, null);
            if (r.ExitCode != 0)
            {
                string err = r.StdErr.Trim();
                throw new Exception("ffprobe: " + (err.Length > 0 ? LastLine(err) : "Fehler " + r.ExitCode));
            }
            return FindGaps(r.StdOut);
        }

        // Ausgabe von ffprobe (eine Zeit je Zeile) -> Lücken
        public static List<MediaGap> FindGaps(string packetTimes)
        {
            List<double> times = new List<double>();
            foreach (string raw in (packetTimes ?? "").Split('\n'))
            {
                string line = raw.Trim();
                int comma = line.IndexOf(',');
                if (comma >= 0) line = line.Substring(0, comma);
                double t;
                if (double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out t)) times.Add(t);
            }
            List<MediaGap> gaps = new List<MediaGap>();
            List<double> diffs = new List<double>();
            for (int i = 1; i < times.Count; i++) if (times[i] > times[i - 1]) diffs.Add(times[i] - times[i - 1]);
            if (diffs.Count == 0) return gaps;
            diffs.Sort();
            double step = diffs[diffs.Count / 2];
            for (int i = 1; i < times.Count; i++)
            {
                double extra = times[i] - times[i - 1] - step;
                if (extra >= GapThreshold) gaps.Add(new MediaGap(times[i - 1] + step, extra));
            }
            return gaps;
        }

        // Lesefehler, die beim nächsten Mal weg sein können: nicht als "defekt" merken
        static bool IsTransientError(string err)
        {
            string e = err.ToLowerInvariant();
            return e.Contains("permission denied") || e.Contains("no such file") || e.Contains("input/output error")
                || e.Contains("resource temporarily unavailable") || e.Contains("i/o error") || e.Contains("network")
                || e.Contains("invalid argument") || e.Contains("connection reset") || e.Contains("connection refused")
                || e.Contains("timed out") || e.Contains("host is down") || e.Contains("stale file handle")
                || e.Contains("broken pipe") || e.Contains("device not configured") || e.Contains("operation not permitted")
                || e.Length == 0;
        }

        // Ohne gemerkte Ergebnisse neu untersuchen (z.B. vor dem Löschen einer als defekt gemeldeten Datei)
        public MediaInfo ProbeFresh(string path, CancellationToken token)
        {
            MediaCache.Forget(this, path);
            List<string> stale = new List<string>();
            foreach (string k in probeCache.Keys) if (k.StartsWith(path + "|", StringComparison.OrdinalIgnoreCase)) stale.Add(k);
            foreach (string k in stale) { MediaInfo dummy; probeCache.TryRemove(k, out dummy); }
            return Probe(path, token);
        }

        static string LastLine(string s)
        {
            string[] lines = s.Replace("\r", "").Split('\n');
            for (int i = lines.Length - 1; i >= 0; i--) if (lines[i].Trim().Length > 0) return lines[i].Trim();
            return s;
        }

        // ---------------------------------------------------------------
        // Encoder
        // ---------------------------------------------------------------

        // Bester funktionierender H.264-Encoder: NVIDIA, Intel, AMD, sonst libx264 (CPU).
        // "ffmpeg -encoders" reicht nicht, die Windows-Builds enthalten immer alle
        // GPU-Encoder. Deshalb wird jeder mit einem kurzen Testbild ausprobiert.
        public string DetectEncoder(CancellationToken token)
        {
            if (encoder != null) return encoder;
            string[] candidates = { "h264_nvenc", "h264_qsv", "h264_amf" };
            foreach (string c in candidates)
            {
                ProcResult r = Proc.Run(FfmpegPath, new string[] {
                    "-hide_banner", "-v", "error", "-f", "lavfi", "-i", "color=black:s=256x256:d=0.2",
                    "-frames:v", "1", "-pix_fmt", PixelFormatFor(c), "-c:v", c, "-f", "null", "-" }, token, null);
                if (r.ExitCode == 0) { encoder = c; return c; }
            }
            encoder = "libx264";
            return encoder;
        }

        public static string EncoderName(string enc)
        {
            switch (enc)
            {
                case "h264_nvenc": return "NVIDIA NVENC (Grafikkarte)";
                case "h264_qsv": return "Intel QuickSync (Grafikkarte)";
                case "h264_amf": return "AMD AMF (Grafikkarte)";
                default: return "libx264 (Prozessor)";
            }
        }

        // QuickSync arbeitet mit nv12 (ebenfalls 4:2:0, ffprobe meldet danach yuv420p)
        public static string PixelFormatFor(string enc)
        {
            return enc == "h264_qsv" ? "nv12" : "yuv420p";
        }

        // Qualitätsparameter je Encoder. -preset medium kennen nur libx264 und QuickSync.
        public static string[] QualityOptions(string enc, int quality, string preset)
        {
            string q = quality.ToString(CultureInfo.InvariantCulture);
            switch (enc)
            {
                case "h264_nvenc": return new string[] { "-preset", "p5", "-tune", "hq", "-rc", "vbr", "-cq", q, "-b:v", "0" };
                case "h264_qsv": return new string[] { "-preset", preset, "-global_quality", q };
                case "h264_amf": return new string[] { "-quality", "balanced", "-rc", "cqp", "-qp_i", q, "-qp_p", q, "-qp_b", q };
                default: return new string[] { "-crf", q, "-preset", preset };
            }
        }
    }
}
