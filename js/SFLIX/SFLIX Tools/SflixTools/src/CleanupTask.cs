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
            Run(ctx, new List<string> { root });
        }

        // Durchsucht alle Ordner und zeigt die Ablagen gemeinsam zur Auswahl. Gelöscht wird nur
        // innerhalb des Installationsordners, damit nicht versehentlich andere Daten verschwinden.
        public static void Run(TaskContext ctx, IList<string> roots)
        {
            ctx.Progress(-1, "Suche Ablagen ...");
            List<CleanupFolder> found = new List<CleanupFolder>();
            List<string> searched = new List<string>();
            foreach (string root in Folders.WithoutNested(roots))
            {
                string reason;
                if (!Directory.Exists(root)) { ctx.Log("Hinweis: Ordner nicht gefunden, übersprungen: " + root); continue; }
                if (!Folders.IsInsideRoot(root, out reason))
                {
                    ctx.Log("Hinweis: " + root + " " + reason + ", übersprungen. Ablagen werden nur im Installationsordner geleert.");
                    continue;
                }
                searched.Add(root);
                Find(root, found, ctx);
            }
            if (searched.Count == 0) throw new TaskFailedException("Kein gültiger Ordner im Installationsordner gewählt");
            if (found.Count == 0)
            {
                ctx.Log("Keine Ablagen (__teile, __konvertiert, __gelöscht, __original ...) gefunden in " + string.Join(", ", searched.ToArray()));
                ctx.Summary = "keine Ablagen gefunden";
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
                ctx.Summary = "nichts gelöscht";
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
                    // Nochmals prüfen, unmittelbar vor dem Löschen
                    string reason;
                    if (!Folders.IsInsideRoot(f.Path, out reason)) throw new IOException("nicht gelöscht, " + reason);
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
            ctx.Summary = deleted + " Ablage(n) gelöscht, " + Fmt.Size(freed) + " frei, " + failed + " Fehler";
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
                // Verknüpfungen/Junctions nie verfolgen und nie als Ablage löschen (sie können
                // auf Ordner ausserhalb von STEFFLIX zeigen)
                bool link;
                try { link = (File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0; }
                catch { continue; }
                if (link)
                {
                    if (IsStorageFolder(name)) ctx.Log("Hinweis: " + d + " ist eine Verknüpfung und wird nicht geleert.");
                    continue;
                }
                if (IsStorageFolder(name))
                {
                    CleanupFolder f = new CleanupFolder();
                    f.Path = d;
                    try { Measure(d, f); }
                    catch (Exception e) { f.Problem = e.Message; }
                    result.Add(f);
                    continue;
                }
                if (Folders.Ignore(d)) continue;   // sflix_sys, Papierkorb, System- und versteckte Ordner
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
