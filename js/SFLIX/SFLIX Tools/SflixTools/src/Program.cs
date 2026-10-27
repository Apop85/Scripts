// Einstieg: STEFFLIX-Ordner prüfen, Aufräumen nach Updates, Oberfläche oder Kommandozeile.
//
// Aufruf:
//   updateStefflix.exe                       Oberfläche
//   updateStefflix.exe --run update,data     Oberfläche, startet die Aufgaben sofort
//   updateStefflix.exe --headless data       ohne Oberfläche (z.B. für die Aufgabenplanung);
//                                            Protokoll in sflix_sys\updater.log, Exit-Code 0 = OK
//   updateStefflix.exe --headless data --data-folder "Serien\Show"
//                                            nur diesen Ordner einlesen (mehrfach angeben möglich,
//                                            relativ zum STEFFLIX-Ordner oder absolut)
//   updateStefflix.exe --install <Ordner>    STEFFLIX ohne Oberfläche in <Ordner> installieren
//   --updated                                (intern) Start nach einem Selbst-Update
// Liegt das Programm nicht in einem STEFFLIX-Ordner, bietet es die Installation an.
// Aufgaben: update, cleanup, merge, convert, preview, data
// Umgebungsvariablen für Tests: SFLIX_DIR (STEFFLIX-Ordner), SFLIX_UPDATE_URL (Update-Server)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("STEFFLIX Updater")]
[assembly: AssemblyProduct("STEFFLIX")]
[assembly: AssemblyDescription("STEFFLIX Updater: Update, Daten einlesen, Vorschaubilder, Zusammenführen, Konvertieren")]
[assembly: AssemblyVersion("3.1.3.0")]
[assembly: AssemblyFileVersion("3.1.3.0")]

namespace Sflix
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static int Main(string[] args)
        {
            bool updated = false;
            bool headless = false;
            string installTo = null;
            List<string> run = null;
            List<string> dataFolders = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--updated") updated = true;
                else if (a == "--data-folder" && i + 1 < args.Length) dataFolders.Add(args[++i]);
                else if (a == "--install" && i + 1 < args.Length) installTo = args[++i];
                else if ((a == "--run" || a == "--headless") && i + 1 < args.Length)
                {
                    headless = a == "--headless";
                    run = ParseTasks(args[++i]);
                }
            }

            string dir = Environment.GetEnvironmentVariable("SFLIX_DIR");
            App.Root = string.IsNullOrEmpty(dir) ? Path.GetDirectoryName(Path.GetFullPath(App.ExePath)) : Path.GetFullPath(dir);

            if (!headless)
            {
                try { SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // Installation ohne Oberfläche (für Skripte und Tests)
            if (installTo != null) return RunHeadlessInstall(installTo);

            // Kein STEFFLIX-Ordner: statt des Updaters die Installation anbieten. Der Updater selbst
            // läuft nur in einem STEFFLIX-Ordner (Schutz davor, in einem fremden Ordner Dateien
            // zu verändern oder umzubenennen).
            if (!App.IsStefflixDir(App.Root))
            {
                if (headless)
                {
                    Console.Error.WriteLine("Dieser Ordner ist kein STEFFLIX-Ordner (start.html oder sflix_sys fehlt): " + App.Root);
                    return 2;
                }
                Application.Run(new InstallForm());
                return 0;
            }

            SelfUpdate.CleanupOld();

            // Eigene Kennung, damit sich mehrere Installationen im Browser nicht beeinflussen
            string idNote = InstallId.Ensure(App.Root);

            // Nur eine Instanz pro STEFFLIX-Ordner. Nach einem Update kurz warten, bis die alte beendet ist.
            using (Mutex mutex = new Mutex(false, "Local\\STEFFLIX-Updater-" + Hash(App.Root.ToLowerInvariant())))
            {
                bool owned = false;
                try { owned = mutex.WaitOne(updated ? 30000 : 0); }
                catch (AbandonedMutexException) { owned = true; }
                if (!owned)
                {
                    if (!headless) MessageBox.Show("Der STEFFLIX Updater läuft bereits.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 3;
                }
                try
                {
                    Settings settings = Settings.Load(Path.Combine(App.SysDir, "updater_settings.json"));
                    if (headless) return RunHeadless(settings, run, idNote, dataFolders);

                    string notice = updated ? "Programm wurde auf Version " + App.VersionText + " aktualisiert." : null;
                    if (idNote != null) notice = notice == null ? idNote : notice + " " + idNote;
                    Application.Run(new MainForm(settings, run, notice));
                    return 0;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        static List<string> ParseTasks(string list)
        {
            List<string> result = new List<string>();
            foreach (string id in MainForm.TaskIds)
                foreach (string part in list.Split(','))
                    if (part.Trim().ToLowerInvariant() == id && !result.Contains(id)) result.Add(id);
            return result;
        }

        static string Hash(string s)
        {
            using (SHA1 sha = SHA1.Create())
                return FileOps.Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Substring(0, 16);
        }

        static int RunHeadlessInstall(string target)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.LogLine = delegate(string line) { Console.WriteLine(line); };
            try
            {
                Directory.CreateDirectory(target);
                InstallTask.Run(ctx, target);
                Console.WriteLine("INSTALLATION ABGESCHLOSSEN");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("FEHLER: " + e.Message);
                return 1;
            }
        }

        static int RunHeadless(Settings settings, List<string> tasks, string idNote, List<string> dataFolders)
        {
            string logFile = Path.Combine(App.SysDir, "updater.log");
            object gate = new object();
            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.Settings = settings;
            ctx.LogLine = delegate(string line)
            {
                lock (gate)
                {
                    try { File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n", FileOps.Utf8NoBom); }
                    catch { }
                }
            };
            ctx.ChooseMerge = MergeTask.DefaultSelection;
            ctx.Ffmpeg = new FfmpegTools(App.Root, settings.GetString("ffmpeg.folder", ""));

            ConvertOptions conv = new ConvertOptions();
            conv.Folder = settings.MediaFolder();
            conv.Parallel = settings.GetInt("convert.parallel", 0);
            conv.DeleteOriginal = settings.GetBool("convert.deleteOriginal", false);
            conv.Preset = settings.GetString("convert.preset", "medium");
            conv.QualitySD = settings.GetInt("convert.qualitySD", 21);
            conv.QualityHD = settings.GetInt("convert.qualityHD", 19);
            conv.QualityFullHD = settings.GetInt("convert.qualityFullHD", 18);
            conv.Quality4K = settings.GetInt("convert.quality4K", 17);

            TaskRunner runner = new TaskRunner();
            runner.Tasks = tasks ?? new List<string>();
            runner.UpdateFfmpeg = settings.GetBool("update.ffmpeg", true);
            runner.MergeFolder = settings.MediaFolder();
            runner.MoveParts = settings.GetBool("merge.moveParts", true);
            runner.Convert = conv;
            runner.PreviewFolder = settings.MediaFolder();
            runner.PreviewVideos = settings.GetBool("preview.videos", true);
            runner.PreviewImages = settings.GetBool("preview.images", false);
            runner.CleanupFolder = settings.MediaFolder();   // ohne Oberfläche: alle Ablagen löschen
            // Ohne --data-folder wird wie bisher alles eingelesen
            runner.DataFolders = dataFolders.Count > 0 ? dataFolders : null;

            ctx.LogLine(App.Title + " " + App.VersionText + " (ohne Oberfläche)  –  " + App.Root);
            if (idNote != null) ctx.LogLine(idNote);
            TaskRunResult result = runner.Run(ctx);
            if (result.Restart && result.Remaining.Count > 0)
            {
                // Die neue Version erledigt den Rest
                ctx.LogLine("Starte neue Version für die restlichen Aufgaben ...");
                string restartArgs = "--updated --headless " + string.Join(",", result.Remaining);
                foreach (string f in dataFolders) restartArgs += " --data-folder " + Proc.Quote(f);
                SelfUpdate.Restart(restartArgs);
            }
            return result.Success ? 0 : 1;
        }
    }
}
