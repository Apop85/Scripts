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
                root = Path.GetFullPath(root).TrimEnd('\\');
                string folder;
                string id = Read(root, out folder);
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
                string escaped = root.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string content =
                    "// Kennung dieser STEFFLIX-Installation (vom Updater erzeugt). Jede Installation\r\n" +
                    "// speichert damit ihre Daten im Browser getrennt von anderen Installationen.\r\n" +
                    "var installId = \"" + id + "\";\r\n" +
                    "var installFolder = \"" + escaped + "\";\r\n";
                string file = FilePath(root);
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

    // Ordner, die keine Aufgabe durchsucht
    public static class Folders
    {
        static readonly string[] SystemDirs = { "System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys" };

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
                if (Skip(name)) continue;
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

        // Der eine Medienordner für Zusammenführen, Konvertieren und Vorschaubilder.
        // Leer -> STEFFLIX-Ordner. Ältere Einstellungen mit getrennten Ordnern werden übernommen.
        public string MediaFolder()
        {
            foreach (string key in new string[] { "media.folder", "convert.folder", "merge.folder", "preview.folder" })
            {
                string s = data.Str(key);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return App.Root;
        }

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
        public bool RestartRequested;                  // Programm wurde aktualisiert -> neu starten

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
