// Aufgabe "Update" (bisher updateStefflix.exe + updater.exe) und das Selbst-Update.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Sflix
{
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

        static Version ReadVersion(byte[] data)
        {
            string tmp = Path.Combine(Path.GetTempPath(), "sflix-version-" + Process.GetCurrentProcess().Id + ".exe");
            try
            {
                File.WriteAllBytes(tmp, data);
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(tmp);
                if (fvi.FileMajorPart == 0 && fvi.FileMinorPart == 0 && fvi.FileBuildPart == 0) return null;
                return new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, 0);
            }
            catch { return null; }
            finally { FileOps.TryDelete(tmp); }
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
            int failed = Net.UpdateFiles(Net.WebFiles, App.Root, checksums, ctx);
            if (failed > 0)
                throw new TaskFailedException("Das Update wurde nicht installiert. Bitte später erneut versuchen.");
            Net.RemoveObsoleteFiles(App.Root, ctx);

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
