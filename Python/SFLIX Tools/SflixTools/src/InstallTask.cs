// Installation (Portierung von setup.py): richtet STEFFLIX in einem neuen Ordner ein.
//   1. Ordner und sflix_sys anlegen
//   2. Dateien der Weboberfläche und data.js herunterladen (geprüft über checksums.json).
//      Vorhandene Dateien bleiben unverändert.
//   3. sflix_sys\renamed.js anlegen (wird von "Daten einlesen" gefüllt)
//   4. Dieses Programm als updateStefflix.exe in den Ordner kopieren

using System;
using System.Collections.Generic;
using System.IO;

namespace Sflix
{
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
            files.AddRange(Net.WebFiles);

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
            ctx.Log("");
            ctx.Log("Anleitung zum Hinzufügen von Medien (im Browser öffnen):");
            ctx.Log("  " + GuideUrl(target));
        }
    }
}
