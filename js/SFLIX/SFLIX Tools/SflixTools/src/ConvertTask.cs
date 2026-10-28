// Aufgabe "Videos für STEFFLIX konvertieren" (Portierung von convertVideos.py).
//
// Wandelt Videos in browserkompatible MP4 (H.264, AAC, yuv420p) um. Das Ergebnis ersetzt das
// Original im selben Ordner, das Original kommt nach <Ordner>\__konvertiert\ (oder wird auf
// Wunsch gelöscht). Bei mehreren Tonspuren entsteht pro Spur eine Datei "<Name>_<Sprache>.mp4",
// weil der Browser keine Tonspur auswählen kann.
//
// - Bereits kompatible Videos werden übersprungen und gemerkt (kein erneutes Untersuchen).
// - Passende Spuren werden nur kopiert (z.B. H.264 aus MKV), der Rest neu kodiert.
// - Encoder: NVIDIA, Intel, AMD oder CPU, automatisch erkannt; scheitert die Grafikkarte an
//   einem Video, wird es mit der CPU wiederholt.
// - Jedes Ergebnis wird geprüft (Codecs, Pixelformat, Länge), erst dann wird ersetzt.
// - Fortsetzen nach Abbruch: Fertige Videos liegen schon als MP4 vor, unfertige werden neu gemacht.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Sflix
{
    public class ConvertOptions
    {
        public string Folder;
        public int Parallel;            // 0 = automatisch
        public bool DeleteOriginal;
        public string Preset = "medium";
        public int QualitySD = 21, QualityHD = 19, QualityFullHD = 18, Quality4K = 17;
    }

    public class ConvertTask
    {
        public const string OriginalsFolderName = "__konvertiert";
        static readonly HashSet<string> VideoExtensions = new HashSet<string> {
            ".mp4", ".mkv", ".avi", ".mov", ".m4v", ".wmv", ".flv", ".webm", ".ts", ".mpeg", ".mpg",
        };

        readonly TaskContext ctx;
        readonly ConvertOptions opt;
        string encoder;

        // Fortschritt
        readonly object gate = new object();
        int total, finished, converted, compatible, failed;
        readonly Dictionary<int, double> active = new Dictionary<int, double>();   // Worker -> Anteil
        readonly Dictionary<int, string> activeName = new Dictionary<int, string>();

        // Bereits kompatible Dateien (Pfad|Grösse|Zeit), damit sie nicht jedes Mal untersucht werden
        string cacheFile;
        HashSet<string> knownCompatible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        readonly ConcurrentDictionary<string, bool> stillCompatible = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        ConvertTask(TaskContext ctx, ConvertOptions opt)
        {
            this.ctx = ctx;
            this.opt = opt;
        }

        public static void Run(TaskContext ctx, ConvertOptions opt)
        {
            new ConvertTask(ctx, opt).Execute();
        }

        void Execute()
        {
            if (!Directory.Exists(opt.Folder)) throw new TaskFailedException("Ordner nicht gefunden: " + opt.Folder);
            ctx.Ffmpeg.EnsureInstalled(ctx);

            ctx.Progress(-1, "Suche Videos ...");
            List<string> files = new List<string>();
            Folders.CollectFiles(opt.Folder, VideoExtensions, true, files, ctx.Token);
            files.RemoveAll(delegate(string f)
            {
                string name = Path.GetFileName(f);
                return name.StartsWith("._") || name.Contains(".__temp__");
            });
            files.Sort(StringComparer.OrdinalIgnoreCase);
            total = files.Count;
            ctx.Log(total + " Videos gefunden in " + opt.Folder);
            if (total == 0) return;

            LoadCache();

            ctx.Progress(-1, "Prüfe Grafikkarte ...");
            encoder = ctx.Ffmpeg.DetectEncoder(ctx.Token);
            int parallel = opt.Parallel > 0 ? opt.Parallel
                         : encoder == "libx264" ? Math.Max(1, Environment.ProcessorCount / 2) : 2;
            ctx.Log("Encoder: " + FfmpegTools.EncoderName(encoder) + ", " + parallel + " Videos gleichzeitig");

            ConcurrentQueue<string> queue = new ConcurrentQueue<string>(files);
            Stopwatch watch = Stopwatch.StartNew();
            List<Thread> workers = new List<Thread>();
            for (int w = 0; w < parallel; w++)
            {
                int id = w;
                Thread t = new Thread(delegate() { Worker(id, queue); });
                t.IsBackground = true;
                t.Name = "Konvertieren " + id;
                workers.Add(t);
                t.Start();
            }
            foreach (Thread t in workers) t.Join();

            SaveCache();
            ctx.Check();
            ctx.Log("Konvertiert: " + converted + "   Bereits kompatibel: " + compatible + "   Fehler: " + failed
                    + "   Dauer: " + Fmt.Time(watch.Elapsed.TotalSeconds));
            ctx.Summary = converted + " konvertiert, " + compatible + " bereits kompatibel, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Video(s) konnten nicht konvertiert werden");
        }

        void Worker(int id, ConcurrentQueue<string> queue)
        {
            string file;
            while (!ctx.Token.IsCancellationRequested && queue.TryDequeue(out file))
            {
                lock (gate) { active[id] = 0; activeName[id] = Path.GetFileName(file); }
                ReportProgress();
                string result;
                try
                {
                    result = ProcessSource(id, file);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    ctx.Log("  FEHLER  " + file + " :: " + e.Message);
                    result = "error";
                }
                lock (gate)
                {
                    finished++;
                    if (result == "ok") converted++;
                    else if (result == "compatible") compatible++;
                    else if (result == "error") failed++;
                    active.Remove(id);
                    activeName.Remove(id);
                }
                ReportProgress();
            }
            lock (gate) { active.Remove(id); activeName.Remove(id); }
        }

        void ReportProgress()
        {
            double fraction;
            string status;
            lock (gate)
            {
                double sum = finished;
                foreach (double d in active.Values) sum += d;
                fraction = total > 0 ? sum / total : 1;
                string current = "";
                foreach (KeyValuePair<int, string> kv in activeName)
                {
                    double d;
                    active.TryGetValue(kv.Key, out d);
                    current = kv.Value + (d > 0 ? " (" + (int)(d * 100) + " %)" : "");
                    break;
                }
                status = "Konvertiere " + Math.Min(finished + 1, total) + " / " + total + (current.Length > 0 ? ": " + current : "");
            }
            ctx.Progress(fraction, status);
        }

        // ---------------------------------------------------------------
        // Ein Video
        // ---------------------------------------------------------------

        string CacheKey(string path)
        {
            FileInfo fi = new FileInfo(path);
            return path + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
        }

        // ---------------------------------------------------------------
        // Gemeinsam mit dem Import genutzt
        // ---------------------------------------------------------------

        // Liegt das Video schon browserkompatibel vor? (MP4-Container mit .mp4/.m4v, H.264 in
        // yuv420p, Ton AAC oder kein Ton, höchstens eine Tonspur)
        public static bool IsBrowserReady(MediaInfo info, string extension)
        {
            string ext = (extension ?? "").ToLowerInvariant();
            bool mp4Container = (ext == ".mp4" || ext == ".m4v") && info.Container.Contains("mp4");
            bool videoOk = info.VideoCodec == "h264" && info.PixelFormat == "yuv420p";
            bool audioOk = true;
            foreach (AudioStream a in info.Audio) if (a.Codec != "aac") audioOk = false;
            return mp4Container && videoOk && audioOk && info.Audio.Count <= 1;
        }

        // Dateinamen ohne Endung: eine Datei pro Tonspur ("Name_deu", "Name_eng"), bei einer
        // oder keiner Tonspur einfach "Name"
        static List<string> TrackFileNames(string stem, List<AudioStream> tracks)
        {
            List<string> names = new List<string>();
            HashSet<string> usedSuffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks.Count <= 1) { names.Add(stem); continue; }
                string lang = tracks[i] == null ? null : tracks[i].Language;
                string suffix = !string.IsNullOrEmpty(lang) && lang != "und" ? lang : "track" + (i + 1);
                string baseSuffix = suffix;
                int n = 2;
                while (usedSuffixes.Contains(suffix)) { suffix = baseSuffix + n; n++; }
                usedSuffixes.Add(suffix);
                names.Add(stem + "_" + suffix);
            }
            return names;
        }

        // Für den Import: Konverter ohne Ordnerdurchlauf
        public static ConvertTask ForImport(TaskContext ctx, ConvertOptions opt)
        {
            return new ConvertTask(ctx, opt);
        }

        // Wandelt ein Video in browserkompatible MP4-Dateien im Zielordner um (pro Tonspur eine).
        // replaceExisting(ziel): true = vorhandene Zieldatei ersetzen, false = Video überspringen.
        // Gibt die erzeugten Dateien zurück, oder null, wenn das Video übersprungen wurde.
        public List<string> ConvertInto(MediaInfo info, string targetDir, string stem,
                                        Func<string, bool> replaceExisting, Action<double> progress)
        {
            if (info.VideoIndex < 0) throw new Exception("keine Videospur gefunden");
            if (encoder == null)
            {
                encoder = ctx.Ffmpeg.DetectEncoder(ctx.Token);
                ctx.Log("  Encoder: " + FfmpegTools.EncoderName(encoder));
            }

            List<AudioStream> tracks = new List<AudioStream>(info.Audio);
            if (tracks.Count == 0) tracks.Add(null);
            List<string> names = TrackFileNames(stem, tracks);
            List<string> finals = new List<string>();
            List<string> temps = new List<string>();
            foreach (string name in names)
            {
                string final = Path.Combine(targetDir, name + ".mp4");
                if (FileOps.Exists(final) && !replaceExisting(final)) return null;
                finals.Add(final);
                temps.Add(Path.Combine(targetDir, name + ".__temp__.mp4"));
            }

            Directory.CreateDirectory(targetDir);
            try
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    int trackNr = i;
                    ConvertTrack(info, tracks[i], temps[i], delegate(double f)
                    {
                        if (progress != null) progress((trackNr + Math.Min(1, f)) / tracks.Count);
                    });
                }
                for (int i = 0; i < temps.Count; i++) FileOps.Replace(temps[i], finals[i]);
            }
            catch
            {
                foreach (string t in temps) FileOps.TryDelete(t);
                throw;
            }
            return finals;
        }

        // ---------------------------------------------------------------

        string ProcessSource(int workerId, string source)
        {
            string key = CacheKey(source);
            if (knownCompatible.Contains(key))
            {
                stillCompatible[key] = true;
                return "compatible";
            }

            MediaInfo info = ctx.Ffmpeg.Probe(source, ctx.Token);
            if (info.VideoIndex < 0)
            {
                ctx.Log("  FEHLER  " + source + " :: keine Videospur gefunden");
                return "error";
            }

            if (IsBrowserReady(info, Path.GetExtension(source)))
            {
                stillCompatible[key] = true;
                return "compatible";
            }

            // Eine Datei pro Tonspur (bzw. eine ohne Ton)
            string dir = Path.GetDirectoryName(source);
            string stem = Path.GetFileNameWithoutExtension(source);
            List<AudioStream> tracks = new List<AudioStream>(info.Audio);
            if (tracks.Count == 0) tracks.Add(null);
            List<string> names = TrackFileNames(stem, tracks);

            List<string> finals = new List<string>();
            List<string> temps = new List<string>();
            for (int i = 0; i < tracks.Count; i++)
            {
                string name = names[i];
                string final = Path.Combine(dir, name + ".mp4");
                if (FileOps.Exists(final) && !string.Equals(final, source, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Log("  FEHLER  " + source + " :: Zieldatei existiert bereits: " + Path.GetFileName(final));
                    return "error";
                }
                finals.Add(final);
                temps.Add(Path.Combine(dir, name + ".__temp__.mp4"));
            }

            List<string> used = new List<string>();
            try
            {
                for (int i = 0; i < tracks.Count; i++)
                {
                    int trackNr = i;
                    string how = ConvertTrack(info, tracks[i], temps[i], delegate(double f)
                    {
                        lock (gate) { active[workerId] = (trackNr + Math.Min(1, f)) / tracks.Count; }
                        ReportProgress();
                    });
                    used.Add(how);
                }
            }
            catch
            {
                foreach (string t in temps) FileOps.TryDelete(t);
                throw;
            }

            // Alles geprüft: Original sichern (oder löschen), Ergebnisse an ihren Platz
            string movedOriginal = null;
            try
            {
                if (opt.DeleteOriginal)
                {
                    File.Delete(source);
                }
                else
                {
                    string storage = Path.Combine(dir, OriginalsFolderName);
                    Directory.CreateDirectory(storage);
                    movedOriginal = FileOps.UniquePath(storage, Path.GetFileName(source));
                    FileOps.Rename(source, movedOriginal);
                }
            }
            catch (Exception e)
            {
                foreach (string t in temps) FileOps.TryDelete(t);
                throw new Exception("Original kann nicht verschoben werden (" + e.Message + ")");
            }

            for (int i = 0; i < temps.Count; i++)
            {
                try
                {
                    FileOps.Rename(temps[i], finals[i]);
                }
                catch (Exception e)
                {
                    // Zurück auf den alten Stand
                    for (int j = 0; j < i; j++) { try { FileOps.Rename(finals[j], temps[j]); } catch { } }
                    foreach (string t in temps) FileOps.TryDelete(t);
                    if (movedOriginal != null) { try { FileOps.Rename(movedOriginal, source); } catch { } }
                    throw new Exception("Ergebnis kann nicht umbenannt werden (" + e.Message + ")");
                }
            }

            StringBuilder results = new StringBuilder();
            for (int i = 0; i < finals.Count; i++)
            {
                if (i > 0) results.Append(", ");
                results.Append(Path.GetFileName(finals[i])).Append(" [").Append(used[i]).Append("]");
            }
            ctx.Log("  OK      " + source + "  ->  " + results);
            return "ok";
        }

        // Erzeugt eine geprüfte MP4 mit Video und einer Tonspur. Gibt zurück, wie (kopiert/Encoder).
        string ConvertTrack(MediaInfo info, AudioStream audio, string temp, Action<double> progress)
        {
            bool videoCopy = info.VideoCodec == "h264" && info.PixelFormat == "yuv420p";
            bool audioCopy = audio == null || audio.Codec == "aac";
            string error = null;

            // 1. Passende Spuren nur kopieren (schnell, verlustfrei)
            if (videoCopy)
            {
                if (TryRun(info, audio, temp, null, audioCopy, progress, out error)) return audioCopy ? "kopiert" : "Video kopiert";
            }

            // 2. Neu kodieren mit dem erkannten Encoder, 3. notfalls mit der CPU
            if (TryRun(info, audio, temp, encoder, false, progress, out error)) return encoder;
            if (encoder != "libx264")
            {
                ctx.Log("  Hinweis " + Path.GetFileName(info.Path) + ": " + encoder + " fehlgeschlagen, wiederhole mit libx264");
                if (TryRun(info, audio, temp, "libx264", false, progress, out error)) return "libx264";
            }
            throw new Exception(error ?? "Konvertierung fehlgeschlagen");
        }

        // videoEncoder == null -> Video kopieren
        bool TryRun(MediaInfo info, AudioStream audio, string temp, string videoEncoder, bool audioCopy,
                    Action<double> progress, out string error)
        {
            List<string> args = new List<string> {
                "-hide_banner", "-nostdin", "-loglevel", "error", "-y",
                "-i", info.Path,
                "-map", "0:" + info.VideoIndex,
            };
            if (audio != null) { args.Add("-map"); args.Add("0:" + audio.Index); }
            else args.Add("-an");

            if (videoEncoder == null)
            {
                args.AddRange(new string[] { "-c:v", "copy" });
            }
            else
            {
                args.AddRange(new string[] { "-c:v", videoEncoder, "-pix_fmt", FfmpegTools.PixelFormatFor(videoEncoder) });
                args.AddRange(FfmpegTools.QualityOptions(videoEncoder, Quality(info), opt.Preset));
            }
            if (audio != null)
            {
                if (audioCopy) args.AddRange(new string[] { "-c:a", "copy" });
                else args.AddRange(new string[] { "-c:a", "aac", "-b:a", "160k" });
            }
            args.AddRange(new string[] {
                "-map_metadata", "0", "-map_chapters", "0",
                "-movflags", "+faststart",
                "-fps_mode", "passthrough",
                "-progress", "pipe:1", "-nostats",
                temp,
            });

            double duration = info.Duration;
            ProcResult r = Proc.Run(ctx.Ffmpeg.FfmpegPath, args, ctx.Token, delegate(string line)
            {
                if (duration <= 0 || !line.StartsWith("out_time_us=")) return;
                long us;
                if (long.TryParse(line.Substring(12), NumberStyles.Integer, CultureInfo.InvariantCulture, out us))
                    progress(us / 1e6 / duration);
            });

            if (r.ExitCode != 0)
            {
                FileOps.TryDelete(temp);
                string err = r.StdErr.Trim();
                if (err.Length > 400) err = err.Substring(err.Length - 400);
                error = "ffmpeg: " + (err.Length > 0 ? err : "Fehler " + r.ExitCode);
                return false;
            }

            error = Validate(info, audio != null, temp);
            if (error != null)
            {
                FileOps.TryDelete(temp);
                return false;
            }
            return true;
        }

        // null = in Ordnung, sonst die Begründung
        string Validate(MediaInfo src, bool expectAudio, string target)
        {
            if (!File.Exists(target)) return "keine Ausgabedatei";
            MediaInfo dst;
            try { dst = ctx.Ffmpeg.Probe(target, ctx.Token); }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { return "Ergebnis nicht lesbar (" + e.Message + ")"; }

            if (dst.VideoCodec != "h264") return "Ergebnis hat Videocodec " + dst.VideoCodec;
            if (dst.PixelFormat != "yuv420p") return "Ergebnis hat Pixelformat " + dst.PixelFormat;
            if (expectAudio && (dst.Audio.Count != 1 || dst.Audio[0].Codec != "aac")) return "Tonspur fehlt oder ist nicht AAC";
            if (!expectAudio && dst.Audio.Count != 0) return "unerwartete Tonspur";
            if (src.Duration > 0 && Math.Abs(src.Duration - dst.Duration) > Math.Max(1.0, src.Duration * 0.005))
                return "Länge passt nicht: " + Fmt.Time(dst.Duration) + " statt " + Fmt.Time(src.Duration);
            return null;
        }

        int Quality(MediaInfo info)
        {
            long pixels = (long)info.Width * info.Height;
            if (pixels >= 3840L * 2160) return opt.Quality4K;
            if (pixels >= 1920L * 1080) return opt.QualityFullHD;
            if (pixels >= 1280L * 720) return opt.QualityHD;
            return opt.QualitySD;
        }

        // ---------------------------------------------------------------
        // Merkliste kompatibler Dateien
        // ---------------------------------------------------------------

        void LoadCache()
        {
            cacheFile = Path.Combine(App.SysDir, "konvertieren_kompatibel.json");
            try
            {
                if (!File.Exists(cacheFile)) return;
                List<object> list = Json.Parse(File.ReadAllText(cacheFile, Encoding.UTF8)) as List<object>;
                if (list == null) return;
                foreach (object o in list) if (o is string) knownCompatible.Add((string)o);
            }
            catch { }
        }

        void SaveCache()
        {
            // Nur Dateien behalten, die in diesem Lauf (noch) kompatibel waren. Einträge anderer
            // Ordner bleiben erhalten, damit ein Lauf auf einem Unterordner nichts vergisst.
            List<object> list = new List<object>();
            string prefix = opt.Folder.TrimEnd('\\') + "\\";
            foreach (string k in knownCompatible)
                if (!k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) list.Add(k);
            foreach (string k in stillCompatible.Keys) list.Add(k);
            try
            {
                string tmp = cacheFile + ".tmp";
                File.WriteAllText(tmp, Json.Write(list), FileOps.Utf8NoBom);
                FileOps.Replace(tmp, cacheFile);
            }
            catch { }
        }
    }
}
