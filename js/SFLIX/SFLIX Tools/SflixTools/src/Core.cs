// Gemeinsame Grundlagen: Einstellungen, Kontext einer laufenden Aufgabe, Ordnerregeln.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Sflix
{
    public static class App
    {
        public const string ExeName = "updateStefflix.exe";
        public const string Title = "STEFFLIX Updater";

        public static string Root;      // STEFFLIX-Ordner (enthält start.html und sflix_sys)
        public static string SysDir { get { return Path.Combine(Root, "sflix_sys"); } }

        public static Version Version
        {
            get { return typeof(App).Assembly.GetName().Version; }
        }

        public static string VersionText
        {
            get { Version v = Version; return v.Major + "." + v.Minor + "." + v.Build; }
        }

        public static string ExePath
        {
            get { return typeof(App).Assembly.Location; }
        }

        public static bool IsStefflixDir(string folder)
        {
            return File.Exists(Path.Combine(folder, "start.html")) && Directory.Exists(Path.Combine(folder, "sflix_sys"));
        }
    }

    // Kennung der Installation in sflix_sys\install.js. Der Browser speichert die Daten aller
    // file://-Seiten gemeinsam; mit der Kennung nutzt jede STEFFLIX-Installation eine eigene
    // Datenbank (Sortierung, Weiterschauen, Favoriten ...) und beeinflusst die anderen nicht.
    // Die Datei merkt sich auch den Ordner:
    //   - Ordner verschoben/umbenannt (alter Ordner hat diese Kennung nicht mehr): Kennung bleibt
    //   - Ordner kopiert (alter Ordner hat noch dieselbe Kennung): die Kopie bekommt eine neue
    public static class InstallId
    {
        const string FileName = "install.js";
        static readonly System.Text.RegularExpressions.Regex IdPattern =
            new System.Text.RegularExpressions.Regex("var installId = \"([0-9a-f]{32})\"");
        static readonly System.Text.RegularExpressions.Regex FolderPattern =
            new System.Text.RegularExpressions.Regex("var installFolder = \"((?:[^\"\\\\]|\\\\.)*)\"");

        public static string FilePath(string root)
        {
            return Path.Combine(root, "sflix_sys", FileName);
        }

        // Liest Kennung und Ordner; null, wenn die Datei fehlt oder ungültig ist
        public static string Read(string root, out string folder)
        {
            folder = null;
            try
            {
                string file = FilePath(root);
                if (!File.Exists(file)) return null;
                string text = File.ReadAllText(file, Encoding.UTF8);
                System.Text.RegularExpressions.Match id = IdPattern.Match(text);
                if (!id.Success) return null;
                System.Text.RegularExpressions.Match f = FolderPattern.Match(text);
                if (f.Success) folder = f.Groups[1].Value.Replace("\\\\", "\\").Replace("\\\"", "\"");
                return id.Groups[1].Value;
            }
            catch { return null; }
        }

        // Legt die Kennung an, falls nötig. Gibt eine Meldung zurück, wenn etwas geändert wurde.
        public static string Ensure(string root)
        {
            try
            {
                // Dateizugriffe mit dem vollen Pfad (bei D:\ wäre "D:" ein relativer Pfad),
                // Vergleiche ohne abschliessendes "\"
                string rootPath = Path.GetFullPath(root);
                root = rootPath.TrimEnd('\\');
                string folder;
                string id = Read(rootPath, out folder);
                string reason = null;
                if (id == null)
                {
                    reason = "Eigene Kennung für diese Installation angelegt (sflix_sys\\install.js).";
                }
                else if (folder == null || !string.Equals(folder.TrimEnd('\\'), root, StringComparison.OrdinalIgnoreCase))
                {
                    string otherFolder;
                    bool copied = folder != null && Directory.Exists(folder)
                        && App.IsStefflixDir(folder) && Read(folder, out otherFolder) == id;
                    if (copied)
                    {
                        id = null;
                        reason = "Kopie einer anderen Installation erkannt: neue Kennung angelegt, damit sich beide nicht beeinflussen.";
                    }
                    else
                    {
                        reason = folder == null ? null : "Installation wurde verschoben, Kennung bleibt erhalten.";
                    }
                }
                else
                {
                    return null;   // alles aktuell
                }

                if (id == null) id = Guid.NewGuid().ToString("N");
                string escaped = rootPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string content =
                    "// Kennung dieser STEFFLIX-Installation (vom Updater erzeugt). Jede Installation\r\n" +
                    "// speichert damit ihre Daten im Browser getrennt von anderen Installationen.\r\n" +
                    "var installId = \"" + id + "\";\r\n" +
                    "var installFolder = \"" + escaped + "\";\r\n";
                string file = FilePath(rootPath);
                string tmp = file + ".tmp";
                File.WriteAllText(tmp, content, FileOps.Utf8NoBom);
                FileOps.Replace(tmp, file);
                return reason;
            }
            catch (Exception e)
            {
                return "Kennung der Installation konnte nicht geschrieben werden: " + e.Message;
            }
        }
    }

    // sflix_sys\updater_info.js: installierte Versionen von Updater und ffmpeg für die Hilfe und den
    // Update-Hinweis in STEFFLIX (der Browser kann die EXE nicht lesen). Wird bei jedem Start und nach
    // einer ffmpeg-Installation geschrieben, nur wenn sich etwas geändert hat. Nicht pushen.
    public static class UpdaterInfo
    {
        public const string FileName = "updater_info.js";

        public static void Write(string root, string ffmpegFolder)
        {
            try
            {
                string ffmpeg = null;
                try { ffmpeg = new FfmpegTools(root, ffmpegFolder).InstalledVersionName(); }
                catch { }
                string content =
                    "// Vom Updater geschrieben: installierte Versionen (für die Hilfe in STEFFLIX)\r\n" +
                    "var updaterInfo = { \"updater\": \"" + App.VersionText + "\", \"ffmpeg\": \"" +
                    (ffmpeg ?? "").Replace("\\", "").Replace("\"", "") + "\" };\r\n";
                string file = Path.Combine(root, "sflix_sys", FileName);
                if (File.Exists(file) && File.ReadAllText(file, Encoding.UTF8) == content) return;
                string tmp = file + ".tmp";
                File.WriteAllText(tmp, content, FileOps.Utf8NoBom);
                FileOps.Replace(tmp, file);
            }
            catch { }   // nur eine Anzeige in STEFFLIX, darf den Updater nie stören
        }
    }

    // Ordner, die keine Aufgabe durchsucht
    public static class Folders
    {
        // Windows-Systemordner, Papierkörbe und Vorschau-Ordner von NAS (QNAP: @Recycle,
        // Synology: #recycle und @eaDir), Entwicklungsordner und STEFFLIX selbst
        static readonly string[] SystemDirs = {
            "System Volume Information", "$RECYCLE.BIN", "@Recycle", "#recycle", "@eaDir", ".vscode", "sflix_sys"
        };

        // Ordner mit "__" im Namen sind Ablagen der Werkzeuge (__teile, __konvertiert, __gelöscht)
        public static bool IsToolFolder(string name)
        {
            return name.Contains("__");
        }

        public static bool Skip(string name)
        {
            foreach (string s in SystemDirs)
                if (string.Equals(name, s, StringComparison.OrdinalIgnoreCase)) return true;
            return IsToolFolder(name);
        }

        // Versteckter Ordner: Name beginnt mit "." (z.B. von NAS oder Linux) oder Windows-Attribut
        // "Versteckt" bzw. "System"
        public static bool IsHidden(string path)
        {
            string name = Path.GetFileName(path.TrimEnd('\\'));
            if (name.StartsWith(".")) return true;
            if (Path.GetPathRoot(path) == path) return false;   // Laufwerke sind immer "versteckt"
            try { return (File.GetAttributes(path) & (FileAttributes.Hidden | FileAttributes.System)) != 0; }
            catch { return false; }
        }

        // Ordner überspringen: Ablage, System- oder Papierkorb-Ordner oder versteckt
        public static bool Ignore(string path)
        {
            return Skip(Path.GetFileName(path.TrimEnd('\\'))) || IsHidden(path);
        }

        // Liegt der Ordner wirklich im STEFFLIX-Ordner? Nein, wenn er ausserhalb liegt oder der Weg
        // dorthin über eine Verknüpfung (Junction, symbolischer Link) führt, die nach aussen zeigen kann.
        public static bool IsInsideRoot(string folder, out string reason)
        {
            reason = null;
            string rel;
            try { rel = DataTask.ToRelative(folder); }
            catch (Exception e) { reason = e.Message; return false; }
            if (rel == null) { reason = "liegt ausserhalb des Installationsordners"; return false; }
            string dir = Path.GetFullPath(App.Root);
            foreach (string seg in rel.Split('\\'))
            {
                if (seg.Length == 0) continue;
                dir = Path.Combine(dir, seg);
                try
                {
                    if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) { reason = "führt über eine Verknüpfung"; return false; }
                }
                catch (Exception e) { reason = e.Message; return false; }
            }
            return true;
        }

        // Ordnerliste ohne Doppelte und ohne Ordner, die in einem anderen gewählten liegen
        public static List<string> WithoutNested(IEnumerable<string> folders)
        {
            List<string> full = new List<string>();
            foreach (string f in folders)
            {
                string p = Path.GetFullPath(f).TrimEnd('\\');
                bool known = false;
                foreach (string k in full) if (string.Equals(k, p, StringComparison.OrdinalIgnoreCase)) known = true;
                if (!known) full.Add(p);
            }
            List<string> result = new List<string>();
            foreach (string a in full)
            {
                bool covered = false;
                foreach (string b in full)
                    if (a.StartsWith(b + "\\", StringComparison.OrdinalIgnoreCase)) covered = true;
                // Laufwerk (D:) wieder mit "\", sonst wäre es ein relativer Pfad
                if (!covered) result.Add(a.EndsWith(":") ? a + "\\" : a);
            }
            return result;
        }

        // Liegt der Ordner (relativ zu root) in einem Ordner, der übersprungen wird?
        public static bool IgnoreAny(string root, string relative)
        {
            string dir = root;
            foreach (string seg in relative.Split('\\'))
            {
                if (seg.Length == 0) continue;
                dir = Path.Combine(dir, seg);
                if (Ignore(dir)) return true;
            }
            return false;
        }

        // Rekursiv alle Dateien mit passender Endung. Preview-Ordner optional überspringen.
        public static void CollectFiles(string dir, ICollection<string> extensions, bool skipPreview, List<string> result, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string[] files, dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException) { return; }

            foreach (string f in files)
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (extensions.Contains(ext)) result.Add(f);
            }
            foreach (string d in dirs)
            {
                string name = Path.GetFileName(d);
                if (Ignore(d)) continue;
                if (skipPreview && string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                CollectFiles(d, extensions, skipPreview, result, token);
            }
        }
    }

    public class Settings
    {
        JsonObject data = new JsonObject();
        string path;

        public static Settings Load(string file)
        {
            Settings s = new Settings();
            s.path = file;
            try
            {
                if (File.Exists(file))
                {
                    JsonObject o = Json.Parse(File.ReadAllText(file, Encoding.UTF8)) as JsonObject;
                    if (o != null) s.data = o;
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, Json.Write(data, 2), FileOps.Utf8NoBom);
                FileOps.Replace(tmp, path);
            }
            catch { }
        }

        public bool GetBool(string key, bool fallback) { return data.Bool(key, fallback); }
        public void SetBool(string key, bool value) { data[key] = value; }

        public int GetInt(string key, int fallback) { return (int)data.Num(key, fallback); }
        public void SetInt(string key, int value) { data[key] = (double)value; }

        public string GetString(string key, string fallback)
        {
            string s = data.Str(key);
            return string.IsNullOrEmpty(s) ? fallback : s;
        }
        public void SetString(string key, string value) { data[key] = value; }

        // Ordner aus den Einstellungen; leer oder nicht vorhanden -> STEFFLIX-Ordner
        public string GetFolder(string key)
        {
            string s = data.Str(key);
            return string.IsNullOrEmpty(s) ? App.Root : s;
        }
    }

    // Wird jeder Aufgabe übergeben: Protokoll, Fortschritt, Abbruch, Rückfragen an die Oberfläche.
    public class TaskContext
    {
        public CancellationToken Token;
        public Settings Settings;
        public Action<string> LogLine;
        public Action<double, string> ProgressSink;   // Anteil 0..1 (negativ = unbestimmt), Statustext
        public Func<List<MergeGroup>, List<MergeGroup>> ChooseMerge;
        public Func<List<CleanupFolder>, List<CleanupFolder>> ChooseCleanup;   // null = alle löschen
        public Func<string, ConflictAnswer> AskConflict;                       // null = überspringen
        public Func<string, EpisodeList> ChooseEpisodeList;   // Folgen umbenennen: Suchbegriff -> Liste, null = abgebrochen
        // Gespeicherte Folgenliste gefunden (zweiter Wert: Neuladen ist eben fehlgeschlagen):
        // "use", "reload", "search" oder null = abbrechen
        public Func<EpisodeList, string, string> ChooseCachedEpisodeList;
        public Func<EpisodePlan, bool> ConfirmEpisodePlan;     // Vorschau; true = umbenennen (Auswahl in Ops[i].Selected)
        public bool RestartRequested;                  // Programm wurde aktualisiert -> neu starten
        public string Summary;                         // kurzes Ergebnis der Aufgabe für die Zusammenfassung

        public FfmpegTools Ffmpeg;

        public void Log(string line)
        {
            if (LogLine != null) LogLine(line);
        }

        public void Progress(double fraction, string status)
        {
            if (ProgressSink != null) ProgressSink(fraction, status);
        }

        public void Check()
        {
            Token.ThrowIfCancellationRequested();
        }
    }

    public class TaskFailedException : Exception
    {
        public TaskFailedException(string message) : base(message) { }
    }
}
