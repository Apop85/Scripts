// Installation (Portierung von setup.py): richtet STEFFLIX in einem neuen Ordner ein.
//   1. Ordner und sflix_sys anlegen
//   2. Dateien der Weboberfläche und data.js herunterladen (geprüft über checksums.json).
//      Vorhandene Dateien bleiben unverändert.
//   3. sflix_sys\renamed.js anlegen (wird von "Daten einlesen" gefüllt) und sflix_sys\install.js
//      mit eigener Kennung (getrennter Speicher im Browser)
//   4. Dieses Programm als updateStefflix.exe in den Ordner kopieren

using System;
using System.Collections.Generic;
using System.IO;

namespace Sflix
{
    // Zusätzliche Wahl im Setup
    public class InstallOptions
    {
        public bool Ffmpeg;            // ffmpeg mitinstallieren
        public string FfmpegFolder;    // Ordner für ffmpeg (Standard: FfmpegLocation.DefaultFolder)
        public string LicenseKey;      // optional
    }

    public static class InstallTask
    {
        // Anleitung zum Hinzufügen von Medien (wie setup.py)
        public const string GuideQuery = "?=SEVMUA==";

        public static string GuideUrl(string folder)
        {
            return "file:///" + folder.TrimEnd('\\').Replace('\\', '/') + "/start.html" + GuideQuery;
        }

        public static void Run(TaskContext ctx, string target)
        {
            Run(ctx, target, null);
        }

        public static void Run(TaskContext ctx, string target, InstallOptions options)
        {
            target = Path.GetFullPath(target);
            ctx.Log("Installationsordner: " + target);

            ctx.Progress(-1, "Erstelle Ordnerstruktur ...");
            Directory.CreateDirectory(Path.Combine(target, "sflix_sys"));

            ctx.Progress(-1, "Lade Prüfsummen ...");
            JsonObject checksums;
            try { checksums = Net.LoadChecksums(ctx.Token); }
            catch (DownloadException e) { throw new TaskFailedException(e.Message + ". Bitte Internetverbindung prüfen und erneut versuchen."); }

            // Wie setup.py: alles ausser den Programmen, vorhandene Dateien überspringen
            List<string> files = new List<string>();
            files.Add("sflix_sys/data.js");
            files.AddRange(Net.WebFilesFor(checksums));

            int failed = 0, loaded = 0, existing = 0;
            for (int i = 0; i < files.Count; i++)
            {
                ctx.Check();
                string path = files[i];
                string file = Path.Combine(target, path.Replace('/', '\\'));
                ctx.Progress((double)i / files.Count, "Lade " + path);
                if (File.Exists(file))
                {
                    existing++;
                    ctx.Log("  " + path.PadRight(36) + "vorhanden");
                    continue;
                }
                try
                {
                    Net.InstallFile(file, Net.DownloadFile(path, checksums, ctx.Token), false);
                    loaded++;
                    ctx.Log("  " + path.PadRight(36) + "installiert");
                }
                catch (DownloadException e)
                {
                    failed++;
                    ctx.Log("  " + path.PadRight(36) + "FEHLER: " + e.Message);
                }
                catch (IOException e)
                {
                    failed++;
                    ctx.Log("  " + path.PadRight(36) + "FEHLER: " + e.Message);
                }
            }

            // Leere Liste der Umbenennungen
            string renamed = Path.Combine(target, "sflix_sys", "renamed.js");
            if (!File.Exists(renamed))
                File.WriteAllText(renamed, "var renamedPaths = {\"id\": \"\", \"pairs\": []};", FileOps.Utf8NoBom);

            // Eigene Kennung, damit diese Installation ihre Daten im Browser getrennt speichert
            string idNote = InstallId.Ensure(target);
            if (idNote != null) ctx.Log("  " + idNote);

            // Den Updater selbst in den Ordner kopieren (sein Selbst-Update holt neuere Versionen)
            string exe = Path.Combine(target, App.ExeName);
            if (!string.Equals(Path.GetFullPath(App.ExePath), exe, StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(exe))
                {
                    ctx.Log("  " + App.ExeName.PadRight(36) + "vorhanden");
                }
                else
                {
                    File.Copy(App.ExePath, exe);
                    ctx.Log("  " + App.ExeName.PadRight(36) + "installiert (Version " + App.VersionText + ")");
                }
            }

            ctx.Log("Installiert: " + loaded + "   Bereits vorhanden: " + existing + "   Fehler: " + failed);
            if (failed > 0)
                throw new TaskFailedException(failed + " Datei(en) konnten nicht installiert werden. Bitte die Installation erneut ausführen.");

            if (options != null) InstallExtras(ctx, target, options);
            ctx.Log("");
            ctx.Log("Anleitung zum Hinzufügen von Medien (im Browser öffnen):");
            ctx.Log("  " + GuideUrl(target));
        }

        // ffmpeg und Lizenz. Fehler hier brechen die Installation nicht ab (beides geht auch später
        // im Updater), sie stehen als Hinweis im Protokoll.
        static void InstallExtras(TaskContext ctx, string target, InstallOptions options)
        {
            Settings settings = Settings.Load(Path.Combine(target, "sflix_sys", "updater_settings.json"));
            if (options.Ffmpeg)
            {
                string folder = string.IsNullOrEmpty(options.FfmpegFolder) ? FfmpegLocation.DefaultFolder : options.FfmpegFolder;
                // Ein anderer als der Standardordner wird gemerkt
                bool isDefault = string.Equals(Path.GetFullPath(folder).TrimEnd('\\'), FfmpegLocation.DefaultFolder.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
                settings.SetString("ffmpeg.folder", isDefault ? "" : folder);
                settings.Save();
                ctx.Log("");
                ctx.Log("ffmpeg (" + folder + "):");
                FfmpegTools ff = new FfmpegTools(target, folder);
                if (ff.Installed) ctx.Log("  bereits vorhanden (" + (ff.InstalledVersionName() ?? "Version unbekannt") + ")");
                else
                {
                    try { ff.InstallNow(ctx); }
                    catch (Exception e) { ctx.Log("Hinweis: ffmpeg konnte nicht installiert werden (" + e.Message + "). Es wird beim ersten Bedarf geladen."); }
                }
                UpdaterInfo.Write(target, folder);
            }
            if (!string.IsNullOrEmpty(options.LicenseKey))
            {
                ctx.Log("");
                ctx.Log("Lizenz:");
                string message;
                bool ok = LicenseStore.Save(target, options.LicenseKey, out message);
                ctx.Log((ok ? "  " : "Hinweis: ") + message);
            }
        }
    }
}
