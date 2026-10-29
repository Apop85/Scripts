// Protokolldatei sflix_sys\updater.log. Seit 3.3 nur auf Wunsch (Schalter im Protokoll, nicht
// gespeichert) bzw. immer ohne Oberfläche. Die Datei wird begrenzt: über 1 MB bleiben die
// letzten 256 KB.

using System;
using System.IO;
using System.Text;

namespace Sflix
{
    public static class LogFile
    {
        public const string FileName = "updater.log";
        const long MaxBytes = 1024 * 1024;
        const int KeepBytes = 256 * 1024;
        static readonly object gate = new object();

        public static string FilePath { get { return Path.Combine(App.SysDir, FileName); } }

        public static string Stamp(string line)
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n";
        }

        // Hängt Text an (Zeilen schon mit Zeitstempel und \r\n). Fehler werden ignoriert.
        public static void Append(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            lock (gate)
            {
                try
                {
                    TrimIfNeeded();
                    File.AppendAllText(FilePath, text, FileOps.Utf8NoBom);
                }
                catch { }
            }
        }

        // Zu grosse Datei auf die letzten 256 KB kürzen (ab dem Anfang einer Zeile)
        static void TrimIfNeeded()
        {
            string file = FilePath;
            if (!File.Exists(file) || new FileInfo(file).Length <= MaxBytes) return;
            string old = File.ReadAllText(file, Encoding.UTF8);
            if (old.Length <= KeepBytes) return;
            string rest = old.Substring(old.Length - KeepBytes);
            int line = rest.IndexOf('\n');
            if (line >= 0 && line + 1 < rest.Length) rest = rest.Substring(line + 1);
            string tmp = file + ".tmp";
            File.WriteAllText(tmp, rest, FileOps.Utf8NoBom);
            FileOps.Replace(tmp, file);
        }

        // Bis 3.2 wurde das Protokoll immer geschrieben. Die alte Datei beim ersten Start von 3.3
        // einmal löschen (gemerkt in updater_settings.json), danach bleibt sie unangetastet.
        public static void RemoveLegacyOnce(Settings settings)
        {
            if (settings == null || settings.GetBool("log.legacyRemoved", false)) return;
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch { }
            settings.SetBool("log.legacyRemoved", true);
            settings.Save();
        }
    }
}
