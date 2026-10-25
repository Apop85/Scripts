// Aufgabe "Ablagen leeren": löscht die Ablage-Ordner der Werkzeuge (Name beginnt mit "__",
// z.B. __teile, __konvertiert, Preview\__gelöscht, Preview\__original) samt Inhalt endgültig.
// Vorher wird angezeigt, was gelöscht wird; nur Ausgewähltes wird gelöscht.

using System;
using System.Collections.Generic;
using System.IO;

namespace Sflix
{
    public class CleanupFolder
    {
        public string Path;
        public int Files;
        public long Size;
        public string Problem;   // z.B. kein Zugriff beim Zählen
    }

    public static class CleanupTask
    {
        public static bool IsStorageFolder(string name)
        {
            return name.StartsWith("__", StringComparison.Ordinal);
        }

        public static void Run(TaskContext ctx, string root)
        {
            if (!Directory.Exists(root)) throw new TaskFailedException("Ordner nicht gefunden: " + root);

            ctx.Progress(-1, "Suche Ablagen ...");
            List<CleanupFolder> found = new List<CleanupFolder>();
            Find(root, found, ctx);
            if (found.Count == 0)
            {
                ctx.Log("Keine Ablagen (__teile, __konvertiert, __gelöscht, __original ...) gefunden in " + root);
                return;
            }

            long total = 0;
            int totalFiles = 0;
            foreach (CleanupFolder f in found) { total += f.Size; totalFiles += f.Files; }
            ctx.Log(found.Count + " Ablage(n) gefunden: " + totalFiles + " Dateien, " + Fmt.Size(total));

            List<CleanupFolder> selected = ctx.ChooseCleanup != null ? ctx.ChooseCleanup(found) : found;
            if (selected == null || selected.Count == 0)
            {
                ctx.Log("Nichts gelöscht.");
                return;
            }

            int deleted = 0, failed = 0;
            long freed = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                ctx.Check();
                CleanupFolder f = selected[i];
                ctx.Progress((double)i / selected.Count, "Lösche " + f.Path);
                try
                {
                    DeleteTree(f.Path);
                    deleted++;
                    freed += f.Size;
                    ctx.Log("  GELÖSCHT  " + f.Path + "  (" + f.Files + " Dateien, " + Fmt.Size(f.Size) + ")");
                }
                catch (Exception e)
                {
                    failed++;
                    ctx.Log("  FEHLER    " + f.Path + " :: " + e.Message);
                }
            }
            ctx.Log("Gelöscht: " + deleted + " Ablage(n), " + Fmt.Size(freed) + " frei   Fehler: " + failed);
            if (failed > 0) throw new TaskFailedException(failed + " Ablage(n) konnten nicht (vollständig) gelöscht werden");
        }

        // Sucht Ablagen, ohne in sie hineinzugehen
        static void Find(string dir, List<CleanupFolder> result, TaskContext ctx)
        {
            ctx.Check();
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); }
            catch (UnauthorizedAccessException) { return; }
            catch (IOException) { return; }

            foreach (string d in dirs)
            {
                string name = System.IO.Path.GetFileName(d);
                if (IsStorageFolder(name))
                {
                    CleanupFolder f = new CleanupFolder();
                    f.Path = d;
                    try { Measure(d, f); }
                    catch (Exception e) { f.Problem = e.Message; }
                    result.Add(f);
                    continue;
                }
                if (Folders.Skip(name)) continue;   // sflix_sys, Papierkorb, Systemordner
                // Verknüpfungen/Junctions nicht verfolgen
                if ((File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0) continue;
                Find(d, result, ctx);
            }
        }

        static void Measure(string dir, CleanupFolder f)
        {
            foreach (string file in Directory.GetFiles(dir))
            {
                f.Files++;
                f.Size += new FileInfo(file).Length;
            }
            foreach (string sub in Directory.GetDirectories(dir))
            {
                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                Measure(sub, f);
            }
        }

        // Löscht einen Ordner samt Inhalt, auch schreibgeschützte Dateien.
        // Verknüpfungen (Junctions) werden nur entfernt, ihr Ziel bleibt unberührt.
        static void DeleteTree(string dir)
        {
            foreach (string sub in Directory.GetDirectories(dir))
            {
                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) Directory.Delete(sub, false);
                else DeleteTree(sub);
            }
            foreach (string file in Directory.GetFiles(dir))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            File.SetAttributes(dir, FileAttributes.Directory);
            Directory.Delete(dir, false);
        }
    }
}
