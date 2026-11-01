// Einstieg: STEFFLIX-Ordner prüfen, Aufräumen nach Updates, Player, Tools oder Kommandozeile.
//
// Aufruf:
//   STEFFLIX.exe                       STEFFLIX im eigenen Fenster (Player, WebView2)
//   STEFFLIX.exe --tools               nur die Tools (Updates, Importer, Tools, Lizenz, Hilfe)
//   STEFFLIX.exe --run update,data     Tools, startet die Aufgaben sofort
//   STEFFLIX.exe --headless data       ohne Oberfläche (z.B. für die Aufgabenplanung);
//                                      Protokoll in sflix_sys\updater.log, Exit-Code 0 = OK
//   STEFFLIX.exe --headless data --data-folder "Serien\Show"
//                                      nur diesen Ordner einlesen (mehrfach angeben möglich,
//                                      relativ zum STEFFLIX-Ordner oder absolut)
//   STEFFLIX.exe --install <Ordner>    STEFFLIX ohne Oberfläche in <Ordner> installieren
//       --ffmpeg [Ordner]              dazu ffmpeg installieren (ohne Ordner: Standard in AppData)
//       --license <Schlüssel>          dazu den Lizenzschlüssel hinterlegen
//   STEFFLIX.exe --license <Schlüssel> Lizenzschlüssel hinterlegen (ohne Oberfläche)
//   STEFFLIX.exe --license-remove      Lizenzschlüssel entfernen (ohne Oberfläche)
//   --player                           zusätzlich zu --tools/--run auch den Player öffnen
//   --log                              Tools: Protokoll von Anfang an in die Datei schreiben
//                                      (intern nach einem Selbst-Update, wenn es eingeschaltet war)
//   --updated                          (intern) Start nach einem Selbst-Update
//   --migrated <updateStefflix.exe>    (intern) Start nach dem Umstieg auf STEFFLIX.exe
// Liegt das Programm nicht in einem STEFFLIX-Ordner, bietet es die Installation an.
// Bis 3.x hiess das Programm updateStefflix.exe (siehe ExeMigration in Desktop.cs).
// Aufgaben: update, cleanup, merge, convert, preview, data, renameundo (letztes "Folgen umbenennen"
// rückgängig machen); "rename" braucht die Oberfläche (Auswahl der Serie)
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

[assembly: AssemblyTitle("STEFFLIX")]
[assembly: AssemblyProduct("STEFFLIX")]
[assembly: AssemblyDescription("STEFFLIX: Mediathek im eigenen Fenster, Update, Daten einlesen, Importer und Tools")]
[assembly: AssemblyVersion("4.0.3.0")]
[assembly: AssemblyFileVersion("4.0.3.0")]

namespace Sflix
{
    static class Program
    {
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int processId);

        [STAThread]
        static int Main(string[] args)
        {
            // Zuerst: WebView2-Bibliotheken aus der EXE laden können (vor jedem Zugriff auf ihre Typen)
            WebViewLibs.Register();
            return Start(args);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static int Start(string[] args)
        {
            bool updated = false;
            bool headless = false;
            bool logToFile = false;
            bool tools = false, player = false;
            string migratedFrom = null;
            string installTo = null;
            string licenseKey = null;
            bool licenseRemove = false;
            bool withFfmpeg = false;
            string ffmpegFolder = null;
            List<string> run = null;
            List<string> dataFolders = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--updated") updated = true;
                else if (a == "--log") logToFile = true;
                else if (a == "--tools") tools = true;
                else if (a == "--player") player = true;
                else if (a == "--migrated" && i + 1 < args.Length) migratedFrom = args[++i];
                else if (a == "--data-folder" && i + 1 < args.Length) dataFolders.Add(args[++i]);
                else if (a == "--install" && i + 1 < args.Length) installTo = args[++i];
                else if (a == "--license" && i + 1 < args.Length) licenseKey = args[++i];
                else if (a == "--license-remove") licenseRemove = true;
                else if (a == "--ffmpeg")
                {
                    withFfmpeg = true;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ffmpegFolder = args[++i];
                }
                else if ((a == "--run" || a == "--headless") && i + 1 < args.Length)
                {
                    headless = a == "--headless";
                    run = ParseTasks(args[++i]);
                }
            }
            // Lizenz ohne Aufgaben: ebenfalls ohne Oberfläche
            if ((licenseKey != null || licenseRemove) && installTo == null) headless = true;

            string dir = Environment.GetEnvironmentVariable("SFLIX_DIR");
            App.Root = string.IsNullOrEmpty(dir) ? Path.GetDirectoryName(Path.GetFullPath(App.ExePath)) : Path.GetFullPath(dir);

            if (!headless)
            {
                try { SetProcessDPIAware(); } catch { }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }

            // Installation ohne Oberfläche (für Skripte und Tests)
            if (installTo != null) return RunHeadlessInstall(installTo, withFfmpeg, ffmpegFolder, licenseKey);

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

            // Release-Ordner (mit "SFLIX Tools"): nichts ändern und nichts anlegen, erzeugte Dateien löschen
            App.IsReleaseFolder = Net.IsReleaseFolder(App.Root);

            // Umstieg: als updateStefflix.exe (von einem Updater bis 3.x geladen) mit Oberfläche gestartet
            // -> als STEFFLIX.exe weiter. Ohne Oberfläche und im Release-Ordner bleibt der Name.
            if (!headless && installTo == null && !App.IsReleaseFolder && string.IsNullOrEmpty(dir)
                && ExeMigration.RunsAsLegacyName && ExeMigration.Run(args)) return 0;

            SelfUpdate.CleanupOld();
            string releaseNote = App.IsReleaseFolder ? ReleaseFolder.RemoveGenerated(App.Root) : null;
            if (App.IsReleaseFolder && (headless || licenseKey != null || licenseRemove))
            {
                Console.Error.WriteLine(ReleaseFolder.Notice);
                return 4;
            }

            // Eigene Kennung, damit sich mehrere Installationen im Browser nicht beeinflussen
            string idNote = App.IsReleaseFolder ? releaseNote : InstallId.Ensure(App.Root);

            // Nur eine Instanz pro STEFFLIX-Ordner. Nach einem Update kurz warten, bis die alte beendet ist.
            using (Mutex mutex = new Mutex(false, "Local\\STEFFLIX-Updater-" + Hash(App.Root.ToLowerInvariant())))
            {
                bool owned = false;
                try { owned = mutex.WaitOne(updated ? 30000 : 0); }
                catch (AbandonedMutexException) { owned = true; }
                if (!owned)
                {
                    // Läuft schon: dort den Player bzw. die Tools nach vorne holen
                    if (!headless)
                    {
                        try
                        {
                            AllowSetForegroundWindow(-1);   // ASFW_ANY
                            int command = tools || run != null ? DesktopHost.ShowToolsCommand : DesktopHost.ShowPlayerCommand;
                            PostMessage((IntPtr)0xffff, DesktopHost.ActivationMessage(App.Root), (IntPtr)command, IntPtr.Zero);   // HWND_BROADCAST
                        }
                        catch { }
                    }
                    return 3;
                }
                try
                {
                    // Im Release-Ordner nur im Speicher (gilt bis zum Schliessen)
                    Settings settings = Settings.Load(App.IsReleaseFolder ? null : Path.Combine(App.SysDir, "updater_settings.json"));
                    // Bis 3.2 wurde das Protokoll immer geschrieben: alte Datei einmal löschen
                    LogFile.RemoveLegacyOnce(settings);
                    // ffmpeg vom alten Ort (sflix_sys\ffmpeg) in den Standardordner verschieben
                    string ffmpegNote = FfmpegLocation.MigrateLegacy(settings);
                    UpdaterInfo.Write(App.Root, FfmpegLocation.Folder(settings));
                    if (headless)
                    {
                        if (licenseKey != null || licenseRemove) return RunHeadlessLicense(licenseKey, licenseRemove);
                        return RunHeadless(settings, run, idNote, dataFolders);
                    }

                    string notice = updated ? "Programm wurde auf Version " + App.VersionText + " aktualisiert." : null;
                    if (idNote != null) notice = notice == null ? idNote : notice + " " + idNote;
                    if (ffmpegNote != null) notice = notice == null ? ffmpegNote : notice + " " + ffmpegNote;
                    if (migratedFrom != null && !App.IsReleaseFolder)
                    {
                        string migrateNote = ExeMigration.Finish(migratedFrom);
                        if (migrateNote != null) notice = notice == null ? migrateNote : notice + " " + migrateNote;
                    }
                    // Ohne Angaben der Player; --tools bzw. --run öffnen die Tools (mit --player beide).
                    // Nach dem Umstieg aus einem Update des alten Updaters: beide (die Tools mit dem Hinweis).
                    bool openTools = tools || run != null || (updated && migratedFrom != null);
                    bool openPlayer = player || !openTools || migratedFrom != null;
                    DesktopHost host = new DesktopHost(settings, run, notice, logToFile, openPlayer, openTools);
                    if (host.HasWindows) Application.Run(host);
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

        internal static string Hash(string s)
        {
            using (SHA1 sha = SHA1.Create())
                return FileOps.Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Substring(0, 16);
        }

        static int RunHeadlessInstall(string target, bool withFfmpeg, string ffmpegFolder, string licenseKey)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.LogLine = delegate(string line) { Console.WriteLine(line); };
            try
            {
                Directory.CreateDirectory(target);
                InstallOptions options = new InstallOptions();
                options.Ffmpeg = withFfmpeg;
                options.FfmpegFolder = withFfmpeg ? (ffmpegFolder ?? FfmpegLocation.DefaultFolder) : null;
                options.LicenseKey = licenseKey;
                InstallTask.Run(ctx, target, options);
                Console.WriteLine("INSTALLATION ABGESCHLOSSEN");
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("FEHLER: " + e.Message);
                return 1;
            }
        }

        // Lizenzschlüssel ohne Oberfläche hinterlegen bzw. entfernen (mit Nachricht an den Empfänger)
        static int RunHeadlessLicense(string key, bool remove)
        {
            try { Console.OutputEncoding = new UTF8Encoding(false); } catch { }
            string message;
            bool ok = remove ? LicenseStore.Remove(true, out message) : LicenseStore.Save(key, out message);
            Console.WriteLine(message);
            LogFile.Append(LogFile.Stamp(message));
            return ok ? 0 : 1;
        }

        static int RunHeadless(Settings settings, List<string> tasks, string idNote, List<string> dataFolders)
        {
            // Ohne Oberfläche ist die Protokolldatei die einzige Rückmeldung: immer schreiben
            // (begrenzt, siehe LogFile)
            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.Settings = settings;
            ctx.LogLine = delegate(string line) { LogFile.Append(LogFile.Stamp(line)); };
            FileOps.Notice = ctx.Log;
            ctx.ChooseMerge = MergeTask.DefaultSelection;
            ctx.Ffmpeg = new FfmpegTools(App.Root, FfmpegLocation.Folder(settings));
            // Lizenz: ausstehende Bestätigung nachholen
            string licenseNote = LicenseStore.ConfirmPending();
            if (licenseNote != null) ctx.LogLine(licenseNote);

            ConvertOptions conv = new ConvertOptions();
            conv.Folder = App.Root;
            conv.Parallel = settings.GetInt("convert.parallel", 0);
            conv.DeleteOriginal = settings.GetBool("convert.deleteOriginal", false);
            conv.Audio = settings.GetBool("convert.audio", true);
            conv.Preset = settings.GetString("convert.preset", "medium");
            conv.QualitySD = settings.GetInt("convert.qualitySD", 21);
            conv.QualityHD = settings.GetInt("convert.qualityHD", 19);
            conv.QualityFullHD = settings.GetInt("convert.qualityFullHD", 18);
            conv.Quality4K = settings.GetInt("convert.quality4K", 17);

            TaskRunner runner = new TaskRunner();
            runner.Tasks = tasks ?? new List<string>();
            runner.UpdateFfmpeg = settings.GetBool("update.ffmpeg", true);
            // Ohne Oberfläche arbeiten die Tools im ganzen Installationsordner
            List<string> all = new List<string> { App.Root };
            runner.MergeFolders = all;
            runner.MoveParts = settings.GetBool("merge.moveParts", true);
            runner.Convert = conv;
            runner.ConvertFolders = all;
            runner.PreviewFolders = all;
            runner.PreviewVideos = settings.GetBool("preview.videos", true);
            runner.PreviewImages = settings.GetBool("preview.images", false);
            runner.CleanupFolders = all;   // ohne Oberfläche: alle Ablagen im Installationsordner löschen
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
