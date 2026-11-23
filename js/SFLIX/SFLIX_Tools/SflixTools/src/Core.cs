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
        // Seit 4.0.0 heisst das Programm STEFFLIX.exe (Player-Fenster und Tools). Updater bis 3.x laden
        // weiterhin updateStefflix.exe; startet diese Datei, benennt sie sich um (ExeMigration).
        public const string ExeName = "STEFFLIX.exe";
        public const string LegacyExeName = "updateStefflix.exe";
        public const string Title = "STEFFLIX";

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

        // Release-Ordner des Entwicklers (seit 3.5.0, gesetzt in Program): Optionen, die Dateien von
        // STEFFLIX, Updater oder App ändern, sind gesperrt, und der Updater legt keine Dateien an.
        public static bool IsReleaseFolder;
    }

    // Dateien, die der Updater in einer Installation anlegt. Im Release-Ordner entstehen sie nicht und
    // werden beim Start gelöscht, damit sie nicht ins Repository gelangen. Nie dabei: data.js (Muster
    // im Repository) und license.js (persönlicher Schlüssel).
    public static class ReleaseFolder
    {
        public const string Notice = "Release-Ordner: Update, Daten einlesen, Importer, Tools und Lizenz sind gesperrt, "
            + "weil sie Dateien im Repository ändern würden. Zum Testen SFLIX_DIR auf eine Testumgebung setzen.";
        public const string ShortNotice = "Release-Ordner: Optionen, die Dateien im Repository ändern, sind gesperrt.";

        static readonly string[] GeneratedFiles =
        {
            "install.js", "updater_info.js", "updater_settings.json", "updater.log", "renamed.js",
            "folgen_cache.json", "medien_cache.json", "folgen_rueckgaengig.json", "downloads.json",
            "damaged.js", "player.log",
        };

        // Löscht die erzeugten Dateien in sflix_sys. Ergebnis: Hinweis für das Protokoll oder null.
        public static string RemoveGenerated(string root)
        {
            List<string> removed = new List<string>();
            foreach (string name in GeneratedFiles)
            {
                string path = Path.Combine(Path.Combine(root, "sflix_sys"), name);
                try
                {
                    if (File.Exists(path))
                    {
                        FileOps.Delete(path);
                        removed.Add(name);
                    }
                }
                catch { }
            }
            return removed.Count == 0 ? null : "Release-Ordner: vom Updater angelegte Dateien gelöscht: " + string.Join(", ", removed.ToArray()) + ".";
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
            if (App.IsReleaseFolder && string.Equals(Path.GetFullPath(root), Path.GetFullPath(App.Root), StringComparison.OrdinalIgnoreCase)) return;
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

        // Ordner, deren Name mit "__" beginnt, sind Ablagen der Werkzeuge (__teile, __konvertiert, __gelöscht).
        // Seit 4.1.0 dieselbe Regel wie "Ablagen leeren" (vorher: "__" irgendwo im Namen).
        public static bool IsToolFolder(string name)
        {
            return name.StartsWith("__");
        }

        // "__" mitten im Namen: wurde bis 4.0.x übersprungen, seit 4.1.0 eingelesen (Hinweis im Protokoll)
        public static bool HasInnerDoubleUnderscore(string name)
        {
            return !name.StartsWith("__") && name.Contains("__");
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

        // Verknüpfung auf einen Ordner (Junction, symbolischer Link): seit 4.1.0 nie durchsucht (DEC-359), ihr
        // Ziel kann ausserhalb des Installationsordners liegen oder fehlen
        public static bool IsLink(string path)
        {
            if (Path.GetPathRoot(path) == path) return false;
            try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
            catch { return false; }
        }

        // Ordner überspringen: Ablage, System- oder Papierkorb-Ordner, versteckt oder Verknüpfung
        public static bool Ignore(string path)
        {
            return Skip(Path.GetFileName(path.TrimEnd('\\'))) || IsHidden(path) || IsLink(path);
        }

        // Wie Ignore(path), aber mit den Attributen aus der Auflistung (DirectoryInfo.GetFileSystemInfos):
        // kein weiterer Zugriff je Eintrag. Nur für Einträge in einem Ordner, nie für Laufwerkswurzeln.
        public static bool Ignore(FileSystemInfo info)
        {
            string name = info.Name;
            if (Skip(name) || name.StartsWith(".")) return true;
            try { return (info.Attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0; }
            catch { return false; }
        }

        // Liegt der Ordner wirklich im STEFFLIX-Ordner? Nein, wenn er ausserhalb liegt oder der Weg
        // dorthin über eine Verknüpfung (Junction, symbolischer Link) führt, die nach aussen zeigen kann.
        // Noch nicht vorhandene Ordner sind erlaubt (werden danach angelegt): geprüft werden nur die
        // vorhandenen Pfadteile; ab dem ersten fehlenden gibt es keine Verknüpfung mehr.
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
                catch (FileNotFoundException) { break; }
                catch (DirectoryNotFoundException) { break; }
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

        public static bool IsPreviewFolder(string name)
        {
            return string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase);
        }

        // Gemeinsamer Walker (seit 4.1.1): besucht dir und rekursiv alle Unterordner, die nicht übersprungen
        // werden (Ignore: Ablagen, System, versteckt, Verknüpfung; mit skipPreview auch Preview-Ordner).
        // visit bekommt den Ordner, seine Dateien und seine Unterordner (aus einer Auflistung, ohne weiteren
        // Zugriff je Eintrag; die Unterordner ungefiltert) und darf mit false das Absteigen in diesen
        // Ordner verhindern. Reihenfolge: erst der Ordner, dann die Unterordner. Nicht lesbare Ordner:
        // ohne Recht still übersprungen, sonst (Pfad zu lang, Netzlaufwerk weg) mit Hinweis im Protokoll.
        public static void Walk(string dir, bool skipPreview, CancellationToken token, Func<DirectoryInfo, List<FileInfo>, List<DirectoryInfo>, bool> visit)
        {
            token.ThrowIfCancellationRequested();
            FileSystemInfo[] entries;
            DirectoryInfo self;
            try
            {
                self = new DirectoryInfo(dir);
                entries = self.GetFileSystemInfos();
            }
            catch (UnauthorizedAccessException) { return; }
            catch (Exception e)
            {
                // Pfad zu lang, Verknüpfung ohne Ziel, Netzlaufwerk weg: Ordner melden, weiter mit dem Rest
                if (!(e is IOException || e is System.Security.SecurityException || e is ArgumentException)) throw;
                Action<string> notice = FileOps.Notice;
                if (notice != null) notice("  Hinweis: Ordner nicht lesbar, übersprungen (" + e.Message + "): " + dir);
                return;
            }

            List<FileInfo> files = new List<FileInfo>();
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();
            foreach (FileSystemInfo e in entries)
            {
                DirectoryInfo d = e as DirectoryInfo;
                if (d != null) { dirs.Add(d); continue; }
                FileInfo f = e as FileInfo;
                if (f != null) files.Add(f);
            }
            if (!visit(self, files, dirs)) return;
            foreach (DirectoryInfo d in dirs)
            {
                if (Ignore(d)) continue;
                if (skipPreview && IsPreviewFolder(d.Name)) continue;
                Walk(d.FullName, skipPreview, token, visit);
            }
        }

        // Rekursiv alle Dateien mit passender Endung. Preview-Ordner optional überspringen.
        public static void CollectFiles(string dir, ICollection<string> extensions, bool skipPreview, List<string> result, CancellationToken token)
        {
            List<FileInfo> infos = new List<FileInfo>();
            CollectFiles(dir, extensions, skipPreview, infos, token);
            foreach (FileInfo f in infos) result.Add(f.FullName);
        }

        // Wie oben, aber mit FileInfo: Grösse und Änderungszeit stammen aus der Auflistung (in .NET 4
        // füllt DirectoryInfo.GetFileSystemInfos die Objekte mit den Daten des Verzeichnisses), ohne
        // weiteren Zugriff je Datei. Reihenfolge wie bisher: erst die Dateien, dann die Unterordner.
        public static void CollectFiles(string dir, ICollection<string> extensions, bool skipPreview, List<FileInfo> result, CancellationToken token)
        {
            Walk(dir, skipPreview, token, delegate(DirectoryInfo self, List<FileInfo> files, List<DirectoryInfo> subs)
            {
                foreach (FileInfo f in files)
                    if (extensions.Contains(f.Extension.ToLowerInvariant())) result.Add(f);
                return true;
            });
        }
    }

    // Vorschaubilder (seit 4.1.1 an einer Stelle): Grösse 480×270, JPEG-Qualität, JPEG-Encoder
    public static class Previews
    {
        public const int Width = 480;
        public const int Height = 270;
        public const long Quality = 80;          // Vorschaubilder (Daten einlesen, Vorschaubilder erstellen)
        public const long AlbumQuality = 85;     // Albumbilder mit unscharfem Rand

        static System.Drawing.Imaging.ImageCodecInfo jpegCodec;

        public static System.Drawing.Imaging.ImageCodecInfo JpegCodec
        {
            get
            {
                if (jpegCodec == null)
                    foreach (System.Drawing.Imaging.ImageCodecInfo c in System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders())
                        if (c.MimeType == "image/jpeg") jpegCodec = c;
                return jpegCodec;
            }
        }

        // Neue Parameter je Aufruf (EncoderParameters sind nicht für gleichzeitige Nutzung gedacht)
        public static System.Drawing.Imaging.EncoderParameters JpegParameters(long quality)
        {
            System.Drawing.Imaging.EncoderParameters enc = new System.Drawing.Imaging.EncoderParameters(1);
            enc.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            return enc;
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

        // Datei der Einstellungen (null im Release-Ordner)
        public string FilePath { get { return path; } }

        // Ohne Pfad (Release-Ordner) gelten die Einstellungen nur bis zum Schliessen
        public void Save()
        {
            if (path == null) return;
            // Auch aus Hintergrund-Threads (Release-Nummer in Net.CheckSerial): Lesen, Schreiben und
            // Speichern laufen unter derselben Sperre
            lock (gate)
            {
                try
                {
                    string tmp = path + ".tmp";
                    File.WriteAllText(tmp, Json.Write(data, 2), FileOps.Utf8NoBom);
                    FileOps.Replace(tmp, path);
                }
                catch { }
            }
        }

        readonly object gate = new object();

        public bool GetBool(string key, bool fallback) { lock (gate) return data.Bool(key, fallback); }
        public void SetBool(string key, bool value) { lock (gate) data[key] = value; }

        public int GetInt(string key, int fallback) { lock (gate) return (int)data.Num(key, fallback); }
        public void SetInt(string key, int value) { lock (gate) data[key] = (double)value; }

        public string GetString(string key, string fallback)
        {
            string s;
            lock (gate) s = data.Str(key);
            return string.IsNullOrEmpty(s) ? fallback : s;
        }
        public void SetString(string key, string value) { lock (gate) data[key] = value; }
    }

    // Wird jeder Aufgabe übergeben: Protokoll, Fortschritt, Abbruch, Rückfragen an die Oberfläche.
    public class TaskContext
    {
        public CancellationToken Token;
        public Settings Settings;
        public Action<string> LogLine;
        public Action<double, string> ProgressSink;   // Anteil 0..1 (negativ = unbestimmt), Statustext
        public Func<List<MergeGroup>, List<MergeGroup>> ChooseMerge;
        public Func<List<CleanupFolder>, CleanupChoice> ChooseCleanup;   // null = alle löschen
        // Zieldatei vorhanden: (neue Datei bzw. Quelle, vorhandene Zieldatei) -> Antwort; null = überspringen
        public Func<string, string, ConflictAnswer> AskConflict;
        public Func<List<MediaCheckItem>, MediaCheckChoice> ChooseMediaCheck;  // Medien prüfen: null = nur protokollieren
        public Func<List<DupeGroup>, DupeChoice> ChooseDuplicates;             // Duplikate: null = nur protokollieren
        public Func<List<AlbumCandidate>, bool> ChooseAlbumCovers;            // Albumbilder: Auswahl in den Kandidaten, false = abgebrochen
        // Folgen umbenennen: (Suchbegriff, Quelle für den Anfang oder null) -> Liste, null = abgebrochen
        public Func<string, string, EpisodeList> ChooseEpisodeList;
        // Gespeicherte Folgenliste gefunden (zweiter Wert: Neuladen ist eben fehlgeschlagen; dritter: true =
        // in dieser Sitzung gemerkte Liste beim Quellenwechsel): "use", "reload", "search" oder null = abbrechen
        public Func<EpisodeList, string, bool, string> ChooseCachedEpisodeList;
        public Func<EpisodePlan, bool> ConfirmEpisodePlan;     // Vorschau; true = umbenennen (Auswahl in Ops[i].Selected)
        // Ja/Nein-Rückfrage (Text) vor einem riskanten Schritt; null (ohne Oberfläche) = Nein
        public Func<string, bool> Confirm;
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
