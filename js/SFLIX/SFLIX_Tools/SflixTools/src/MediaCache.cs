// Gemerkte ffprobe-Ergebnisse (seit 3.4.2) in sflix_sys\medien_cache.json.
// Je Datei: Grösse und Änderungszeit plus Dauer, Container, Video und Tonspuren, oder der Fehler von
// ffprobe (defekte Datei). Stimmen Grösse und Zeit, braucht "Medien prüfen", "Duplikate finden",
// "Vorschaubilder" usw. kein ffprobe mehr. Eine andere ffmpeg-Version verwirft alles, ebenso der
// Knopf "Gemerkte Prüfergebnisse löschen". Einträge, die 180 Tage nicht gebraucht wurden, fallen weg.
// Thread-sicher (die Tools prüfen parallel). Persönliche Datei: nie pushen, der Updater lädt sie nie.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sflix
{
    public static class MediaCache
    {
        public const string FileName = "medien_cache.json";
        const int MaxUnusedDays = 180;

        static readonly object sync = new object();
        static Dictionary<string, JsonObject> entries;
        static string loadedFor;     // ffmpeg-Version (SHA256 aus version.json), für die die Einträge gelten
        static bool dirty;

        public static string FilePath
        {
            get { return Path.Combine(App.SysDir, FileName); }
        }

        static int Today
        {
            get { return (int)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalDays; }
        }

        // Schlüssel: Pfad relativ zum STEFFLIX-Ordner (bleibt beim Verschieben gültig), sonst der volle Pfad
        static string KeyOf(string path)
        {
            string full = Path.GetFullPath(path);
            string root = Path.GetFullPath(App.Root).TrimEnd('\\') + "\\";
            string key = full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
            return key.ToLowerInvariant();
        }

        static void EnsureLoaded(string ffmpegVersion)
        {
            if (entries != null && loadedFor == ffmpegVersion) return;
            entries = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            dirty = loadedFor != null;   // Wechsel der ffmpeg-Version: leere Datei schreiben
            loadedFor = ffmpegVersion;
            try
            {
                if (!File.Exists(FilePath)) return;
                JsonObject root = Json.Parse(File.ReadAllText(FilePath, Encoding.UTF8)) as JsonObject;
                if (root == null || root.Str("ffmpeg") != ffmpegVersion) { dirty = true; return; }
                JsonObject files = root.Obj("files");
                if (files == null) return;
                foreach (KeyValuePair<string, object> p in files)
                {
                    JsonObject e = p.Value as JsonObject;
                    if (e != null) entries[p.Key] = e;
                }
            }
            catch { entries.Clear(); dirty = true; }
        }

        // Gemerktes Ergebnis für diese Datei (gleiche Grösse und Änderungszeit)? error: Fehler von ffprobe
        public static bool TryGet(FfmpegTools ff, string path, long size, long ticks, out MediaInfo info, out string error)
        {
            info = null;
            error = null;
            string version = ff.InstalledVersion() ?? "";
            lock (sync)
            {
                EnsureLoaded(version);
                JsonObject e;
                if (!entries.TryGetValue(KeyOf(path), out e)) return false;
                if ((long)e.Num("s", -1) != size || e.Str("m") != ticks.ToString()) return false;
                if (e.Str("e") != null)
                {
                    error = e.Str("e");
                }
                else
                {
                    info = new MediaInfo();
                    info.Path = path;
                    info.Duration = e.Num("d", 0);
                    info.Container = e.Str("c") ?? "";
                    info.VideoIndex = (int)e.Num("vi", -1);
                    info.VideoCodec = e.Str("vc") ?? "";
                    info.PixelFormat = e.Str("pf") ?? "";
                    info.Width = (int)e.Num("w", 0);
                    info.Height = (int)e.Num("h", 0);
                    info.Signature = e.Str("sig") ?? "";
                    List<object> audio = e.Arr("a");
                    if (audio != null)
                        foreach (object o in audio)
                        {
                            JsonObject a = o as JsonObject;
                            if (a == null) continue;
                            AudioStream s = new AudioStream();
                            s.Index = (int)a.Num("i", 0);
                            s.Codec = a.Str("c") ?? "";
                            s.Language = a.Str("l");
                            info.Audio.Add(s);
                        }
                }
                if ((int)e.Num("t", 0) != Today) { e["t"] = (double)Today; dirty = true; }
                return true;
            }
        }

        static JsonObject NewEntry(long size, long ticks)
        {
            JsonObject e = new JsonObject();
            e["s"] = (double)size;
            e["m"] = ticks.ToString();      // als Text: Ticks passen nicht genau in eine JSON-Zahl
            e["t"] = (double)Today;
            return e;
        }

        public static void Put(FfmpegTools ff, string path, long size, long ticks, MediaInfo info)
        {
            JsonObject e = NewEntry(size, ticks);
            e["d"] = info.Duration;
            e["c"] = info.Container ?? "";
            e["vi"] = (double)info.VideoIndex;
            e["vc"] = info.VideoCodec ?? "";
            e["pf"] = info.PixelFormat ?? "";
            e["w"] = (double)info.Width;
            e["h"] = (double)info.Height;
            e["sig"] = info.Signature ?? "";
            List<object> audio = new List<object>();
            foreach (AudioStream s in info.Audio)
            {
                JsonObject a = new JsonObject();
                a["i"] = (double)s.Index;
                a["c"] = s.Codec ?? "";
                if (s.Language != null) a["l"] = s.Language;
                audio.Add(a);
            }
            e["a"] = audio;
            Store(ff, path, e);
        }

        public static void PutError(FfmpegTools ff, string path, long size, long ticks, string error)
        {
            JsonObject e = NewEntry(size, ticks);
            e["e"] = error ?? "";
            Store(ff, path, e);
        }

        static void Store(FfmpegTools ff, string path, JsonObject e)
        {
            string version = ff.InstalledVersion() ?? "";
            lock (sync)
            {
                EnsureLoaded(version);
                entries[KeyOf(path)] = e;
                dirty = true;
            }
        }

        // Schreibt geänderte Einträge (am Ende der Tools). Fehler werden ignoriert: dann wird eben neu geprüft.
        public static void Save()
        {
            lock (sync)
            {
                if (entries == null || !dirty) return;
                try
                {
                    JsonObject files = new JsonObject();
                    int today = Today;
                    foreach (KeyValuePair<string, JsonObject> p in entries)
                        if (today - (int)p.Value.Num("t", today) <= MaxUnusedDays) files[p.Key] = p.Value;
                    JsonObject root = new JsonObject();
                    root["ffmpeg"] = loadedFor ?? "";
                    root["files"] = files;
                    string tmp = FilePath + ".tmp";
                    File.WriteAllText(tmp, Json.Write(root, 0), FileOps.Utf8NoBom);
                    FileOps.Replace(tmp, FilePath);
                    dirty = false;
                }
                catch { }
            }
        }

        // Knopf "Gemerkte Prüfergebnisse löschen"
        public static void Clear()
        {
            lock (sync)
            {
                entries = null;
                loadedFor = null;
                dirty = false;
                try { FileOps.Delete(FilePath); } catch { }
            }
        }

        // Anzahl gemerkter Dateien (für die Anzeige in den Einstellungen), ohne die Datei zu laden
        public static long FileSize()
        {
            try { return File.Exists(FilePath) ? new FileInfo(FilePath).Length : 0; }
            catch { return 0; }
        }
    }
}
